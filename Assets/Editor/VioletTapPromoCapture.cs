#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using _01.Scripts.Game;
using _01.Scripts.Scene;
using SWGUnity2DCore.Manager;
using SWGUnity2DCore.Util;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

// Editor-only recording utility. Never changes gameplay rules or persists scores.
[InitializeOnLoad]
public static class VioletTapPromoCapture
{
    internal static readonly string Root = Path.GetFullPath("output/promo-production");
    const string Pending = "VioletTap.PromoCapture.Pending";
    static VioletTapPromoCapture()
    {
        EditorApplication.update += Check;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Offline Promo Capture").AddComponent<VioletTapPromoCaptureBehaviour>();
        };
    }
    static void Check()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string request = Path.Combine(Root, "RECORD_PROMO");
        if (!File.Exists(request)) return;
        File.Delete(request);
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
}

internal sealed class VioletTapPromoCaptureBehaviour : MonoBehaviour
{
    const int Fps = 30;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    string oldLanguage, directory;
    bool oldBackground, oldAds, saved, restored;
    int oldFps, oldVsync, oldCapture, frame;
    float oldScale, oldVolume;
    UnityEngine.Random.State oldRandom;
    Process encoder;
    StreamWriter telemetry;
    [Serializable] class Sample
    {
        public int frame, score, combo, phase, seq, width, height;
        public float time, fever, remaining, elapsed;
        public string kind, target, state;
    }
    void Start() { DontDestroyOnLoad(gameObject); StartCoroutine(Run()); }
    IEnumerator Run()
    {
        Save();
        var steps = RecordAll();
        while (true)
        {
            bool next; object current = null;
            try { next = steps.MoveNext(); if (next) current = steps.Current; }
            catch (Exception e)
            {
                File.WriteAllText(Path.Combine(VioletTapPromoCapture.Root, "capture-error.txt"), e.ToString());
                UnityEngine.Debug.LogError(e);
                try { CloseEncoder(); } catch { /* Preserve original capture error. */ }
                Restore(); EditorApplication.ExitPlaymode(); yield break;
            }
            if (!next) break;
            yield return current;
        }
        Restore();
        File.WriteAllText(Path.Combine(VioletTapPromoCapture.Root, "capture-complete.txt"), directory);
        EditorApplication.ExitPlaymode();
    }
    IEnumerator RecordAll()
    {
        directory = Path.Combine(VioletTapPromoCapture.Root, "capture-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(VioletTapPromoCapture.Root, "active-capture.txt"), directory);
        foreach (string locale in new[] { "ko", "en" })
        {
            EditorPrefs.SetString("VioletTap.EditorLanguage", locale == "ko" ? "Korean" : "English");
            Managers.Scene.ChangeScene(W01SceneType.Intro);
            for (int i = 0; i < 35; i++) yield return null;
            UnityEngine.Random.InitState(260915);
            if (Screen.width < 640 || Screen.height < Screen.width * 1.7f)
                throw new InvalidOperationException("Need portrait Game View, got " + Screen.width + "x" + Screen.height);
            BeginEncoder(locale);
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            var eof = new WaitForEndOfFrame();
            GameScene game = null;
            bool changedScene = false, roundStarted = false, bombTapped = false;
            float nextTap = 7.5f, resultAt = -1f;
            int lastCombo = 0, lastStage = 0;
            float lastFever = 0;
            for (frame = 0; frame < 95 * Fps; frame++)
            {
                yield return eof;
                float t = frame / (float)Fps;
                texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
                texture.Apply(false, false);
                byte[] bytes = texture.GetRawTextureData();
                encoder.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
                if (frame % 90 == 0)
                {
                    File.WriteAllText(Path.Combine(VioletTapPromoCapture.Root, "capture-status.txt"), locale + " " + t.ToString("F1") + "s " + directory);
                    File.WriteAllBytes(Path.Combine(directory, locale + "-preview.jpg"), texture.EncodeToJPG(85));
                }
                if (!changedScene && t >= 4f) { Managers.Scene.ChangeScene(W01SceneType.Game); changedScene = true; }
                if (changedScene && game == null) game = Object.FindFirstObjectByType<GameScene>();
                if (game == null) continue;
                if (!roundStarted && t >= 6.5f) { game.StartRound(); roundStarted = true; Log(game, "round_start"); }
                if (!roundStarted) continue;
                var flow = Read<GameFlow>(game, "mGameFlow");
                int combo = Read<int>(game, "m_Combo"), phase = Read<int>(game, "m_PaceStage");
                float fever = Read<float>(game, "m_FeverRemaining");
                if (lastCombo > 0 && combo == 0) Log(game, "combo_break");
                if (lastFever <= 0 && fever > 0) Log(game, "fever_start");
                if (lastFever > 0 && fever <= 0) Log(game, "fever_end");
                if (phase != lastStage) Log(game, "phase");
                lastCombo = combo; lastStage = phase; lastFever = fever;
                if (frame % 3 == 0) Log(game, "state");
                if (flow.State == GameFlowState.Result)
                {
                    if (resultAt < 0) { resultAt = t; Log(game, "result"); }
                    if (t > resultAt + 4f) break;
                    continue;
                }
                if (game.IsGameplayPaused) throw new InvalidOperationException("Gameplay paused during capture; keep Unity active.");
                float elapsed = Read<float>(game, "m_RoundElapsed");
                if (elapsed > 35f || t < nextTap || Read<float>(game, "m_PhaseCueRemaining") > 0f) continue;
                var targets = Read<List<CircleTarget>>(game, "m_ActiveTargets");
                CircleTarget chosen = null;
                if (elapsed > 31f && !bombTapped)
                {
                    chosen = targets.FirstOrDefault(x => x != null && x.Type == TapTargetType.Bomb);
                    bombTapped = chosen != null;
                }
                if (chosen == null)
                {
                    var numbered = targets.Where(x => x != null && x.SequenceOrder > 0).OrderBy(x => x.SequenceOrder).FirstOrDefault();
                    chosen = numbered ?? targets.Where(x => x != null && x.Type != TapTargetType.Bomb)
                        .OrderBy(x => Read<float>(x, "m_RemainingLifetime")).FirstOrDefault();
                }
                if (chosen == null) continue;
                Log(game, "tap", chosen); Contact(chosen);
                nextTap = t + (elapsed < 8f ? .23f : .105f);
            }
            CloseEncoder(); Object.Destroy(texture);
            File.WriteAllText(Path.Combine(directory, locale + "-source-notes.txt"),
                "Real Unity Game View frames, " + Screen.width + "x" + Screen.height + ", 30 fps.\n"
                + "Offline frame stepping at 1/30 second; playback preserves game simulation speed.\n"
                + "Input via EventSystem raycasts; unmodified GameConfig, spawn, target lifetimes and score rules.\n"
                + "No scores persisted/submitted. Ads hidden during capture. Audio reconstructed from shipped BGM/SFX.\n"
                + "Frames: " + (frame + 1));
        }
    }
    void Log(GameScene game, string kind, CircleTarget target = null)
    {
        telemetry.WriteLine(JsonUtility.ToJson(new Sample { frame = frame, time = frame / (float)Fps, kind = kind,
            width = Screen.width, height = Screen.height, combo = Read<int>(game, "m_Combo"),
            phase = Read<int>(game, "m_PaceStage"), fever = Read<float>(game, "m_FeverRemaining"),
            score = Read<int>(game, "m_Score"), elapsed = Read<float>(game, "m_RoundElapsed"),
            remaining = Read<CountdownTimer>(game, "m_Timer").Remaining,
            state = Read<GameFlow>(game, "mGameFlow").State.ToString(),
            target = target == null ? "" : target.Type.ToString(), seq = target == null ? 0 : target.SequenceOrder }));
    }
    void BeginEncoder(string locale)
    {
        string ffmpeg = Directory.GetFiles(Path.Combine(VioletTapPromoCapture.Root, "python/imageio_ffmpeg/binaries"), "ffmpeg*.exe").Single();
        encoder = Process.Start(new ProcessStartInfo(ffmpeg,
            "-y -hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size "
            + Screen.width + "x" + Screen.height + " -framerate 30 -i pipe:0 -an -vf vflip "
            + "-c:v libx264 -preset veryfast -crf 17 -pix_fmt yuv420p -movflags +faststart \""
            + Path.Combine(directory, locale + "-gameplay.mp4") + "\"")
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardError = true });
        telemetry = new StreamWriter(Path.Combine(directory, locale + "-events.jsonl"));
    }
    void CloseEncoder()
    {
        telemetry?.Dispose(); telemetry = null;
        if (encoder == null) return;
        encoder.StandardInput.Close(); string errors = encoder.StandardError.ReadToEnd();
        encoder.WaitForExit(); int exit = encoder.ExitCode; encoder.Dispose(); encoder = null;
        if (exit != 0) throw new InvalidOperationException("Capture encoder: " + errors);
    }
    static T Read<T>(object value, string field) => (T)value.GetType().GetField(field, Private).GetValue(value);
    static void Contact(CircleTarget target)
    {
        Physics2D.SyncTransforms();
        var pointer = new PointerEventData(EventSystem.current) { position = Camera.main.WorldToScreenPoint(target.transform.position), button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
        var hit = hits.FirstOrDefault();
        if (hit.gameObject == null || ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit.gameObject) != target.gameObject)
            throw new InvalidOperationException("Target blocked from actual input raycast");
        ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerDownHandler);
    }
    void Save()
    {
        oldLanguage = EditorPrefs.GetString("VioletTap.EditorLanguage", "System");
        oldBackground = Application.runInBackground; oldFps = Application.targetFrameRate;
        oldVsync = QualitySettings.vSyncCount; oldCapture = Time.captureFramerate;
        oldScale = Time.timeScale; oldVolume = AudioListener.volume; oldRandom = UnityEngine.Random.state;
        oldAds = Managers.Ads.AdsDisabled; saved = true;
        Managers.Ads.SetAdsDisabled(true); GameScene.SuppressRecordPersistenceForQa = true;
        Application.runInBackground = true; Application.targetFrameRate = 30; QualitySettings.vSyncCount = 0;
        Time.timeScale = 1f; Time.captureFramerate = Fps; AudioListener.volume = 0f;
    }
    void Restore()
    {
        if (!saved || restored) return; restored = true;
        EditorPrefs.SetString("VioletTap.EditorLanguage", oldLanguage);
        Managers.Ads.SetAdsDisabled(oldAds); GameScene.SuppressRecordPersistenceForQa = false;
        Application.runInBackground = oldBackground; Application.targetFrameRate = oldFps;
        QualitySettings.vSyncCount = oldVsync; Time.captureFramerate = oldCapture;
        Time.timeScale = oldScale; AudioListener.volume = oldVolume; UnityEngine.Random.state = oldRandom;
    }
    void OnDestroy() { if (encoder != null) CloseEncoder(); Restore(); }
}
#endif
