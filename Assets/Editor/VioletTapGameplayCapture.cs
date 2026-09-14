#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using _01.Scripts.Game;
using _01.Scripts.Scene;
using SWGUnity2DCore.Manager;
using SWGUnity2DCore.Util;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

/// <summary>A one-shot, editor-only gameplay recording. Never submits a score or changes assets.</summary>
[InitializeOnLoad]
public static class VioletTapGameplayCapture
{
    internal static readonly string Root = Path.GetFullPath("output/qa-2026-09-13/gameplay-video");
    private const string PendingKey = "VioletTap.Video.Pending";
    private const string EncodeKey = "VioletTap.Video.EncodeDirectory";

    static VioletTapGameplayCapture()
    {
        EditorApplication.update += CheckRequest;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += EncodeIfPending;
    }

    [MenuItem("Tools/VioletTap/Capture Gameplay Video (10 and 50 Combo)")]
    public static void Record()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(PendingKey, true);
        EditorApplication.EnterPlaymode();
    }

    private static void CheckRequest()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode) return;
        // Domain reload can swallow the one-shot edit-mode callback. Retry from the idle editor tick.
        if (!string.IsNullOrEmpty(SessionState.GetString(EncodeKey, string.Empty)))
        {
            EncodeIfPending();
            return;
        }
        string request = Path.Combine(Root, "RECORD_GAMEPLAY_QA");
        if (!File.Exists(request)) return;
        File.Delete(request);
        Record();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
        {
            SessionState.SetBool(PendingKey, false);
            new GameObject("VioletTap Gameplay Video Fixture")
                .AddComponent<VioletTapGameplayCaptureBehaviour>();
        }
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += EncodeIfPending;
    }

    internal static void QueueEncoding(string directory)
    {
        SessionState.SetString(EncodeKey, directory);
        EditorApplication.ExitPlaymode();
    }

    private static void EncodeIfPending()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        string directory = SessionState.GetString(EncodeKey, string.Empty);
        if (string.IsNullOrEmpty(directory)) return;
        SessionState.SetString(EncodeKey, string.Empty);
        string output = Path.Combine(directory, "violet-tap-gameplay.mp4");
        if (File.Exists(output) && new FileInfo(output).Length > 10000)
        {
            WriteCompletion(directory, output);
            return;
        }
        string ffmpeg = FindFfmpeg();
        if (ffmpeg == null)
        {
            File.WriteAllText(Path.Combine(directory, "capture-failed.txt"),
                "FFmpeg was not found on PATH or in VIOLETTAP_FFMPEG. "
                + "The captured frames and audio remain in this directory.");
            return;
        }

        try
        {
            string args = "-y -hide_banner -loglevel error -f concat -safe 0 -i frames.ffconcat "
                + "-i audio.wav -vf scale=540:1170:flags=lanczos -r 24 -vsync vfr "
                + "-c:v libx264 -preset medium -crf 20 -pix_fmt yuv420p "
                + "-c:a aac -strict -2 -b:a 160k -shortest \"" + output + "\"";
            var start = new ProcessStartInfo(ffmpeg, args)
            {
                WorkingDirectory = directory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            using (Process process = Process.Start(start))
            {
                string errors = process.StandardError.ReadToEnd();
                process.WaitForExit(120000);
                File.WriteAllText(Path.Combine(directory, "ffmpeg.log"), errors);
                if (process.ExitCode != 0 || !File.Exists(output) || new FileInfo(output).Length < 10000)
                    throw new InvalidOperationException("FFmpeg failed (exit " + process.ExitCode + "): " + errors);
            }
            WriteCompletion(directory, output);
            Debug.Log("[VioletTap] Gameplay video: " + output);
        }
        catch (Exception exception)
        {
            File.WriteAllText(Path.Combine(directory, "capture-failed.txt"), exception.ToString());
            Debug.LogError("[VioletTap] Gameplay video encoding failed: " + exception);
        }
    }

    private static void WriteCompletion(string directory, string output)
    {
        File.WriteAllText(Path.Combine(Root, "latest-video.txt"), output);
        File.WriteAllText(Path.Combine(directory, "capture-complete.txt"),
            "Recorded and encoded: " + DateTime.Now.ToString("O") + Environment.NewLine + output);
    }

    private static string FindFfmpeg()
    {
        string configured = Environment.GetEnvironmentVariable("VIOLETTAP_FFMPEG");
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured)) return configured;
        string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (string entry in path.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;
            string candidate = Path.Combine(entry.Trim().Trim('"'), "ffmpeg.exe");
            if (File.Exists(candidate)) return candidate;
        }
        string localFallback = @"C:\KMPlayer\ffmpeg.exe";
        return File.Exists(localFallback) ? localFallback : null;
    }
}

internal sealed class VioletTapGameplayCaptureBehaviour : MonoBehaviour
{
    private const string BgmKey = "VioletTap.Audio.BgmEnabled";
    private const string EffectKey = "VioletTap.Audio.EffectEnabled";
    private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<string> frames = new List<string>();
    private readonly List<double> frameTimes = new List<double>();
    private readonly GameplayAudioBuffer audioBuffer = new GameplayAudioBuffer();
    private string directory;
    private bool restored;
    private bool fixtureSaved;
    private bool oldBgmPresent;
    private bool oldEffectPresent;
    private int oldBgm;
    private int oldEffect;
    private string oldLanguage;
    private bool oldAdsDisabled;
    private bool oldRunInBackground;
    private int oldTargetFrameRate;
    private int oldVsync;
    private float oldTimeScale;
    private UnityEngine.Random.State oldRandom;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(RecordRoutine());
    }

    private IEnumerator RecordRoutine()
    {
        directory = Path.Combine(VioletTapGameplayCapture.Root,
            "run-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(directory);
        IEnumerator steps = RecordSteps();
        while (true)
        {
            bool hasNext;
            object current = null;
            try
            {
                hasNext = steps.MoveNext();
                if (hasNext) current = steps.Current;
            }
            catch (Exception exception)
            {
                audioBuffer.StopAndWrite(Path.Combine(directory, "audio.wav"));
                File.WriteAllText(Path.Combine(directory, "capture-failed.txt"), exception.ToString());
                Debug.LogError("[VioletTap] Gameplay capture failed: " + exception);
                RestoreFixtureState();
                EditorApplication.ExitPlaymode();
                yield break;
            }
            if (!hasNext) yield break;
            yield return current;
        }
    }

    private IEnumerator RecordSteps()
    {
            SaveAndSetFixtureState();
            for (int i = 0; i < 8; i++) yield return null;
            if (Screen.width < 400 || Screen.height < Screen.width * 1.8f)
                throw new InvalidOperationException("Select a portrait Game View before recording. Current size: "
                    + Screen.width + "x" + Screen.height);

            var pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            var timer = Stopwatch.StartNew();
            var endOfFrame = new WaitForEndOfFrame();
            GameScene game = null;
            bool switchedToGame = false;
            bool startedRound = false;
            bool sawTen = false;
            bool sawFifty = false;
            bool brokeCombo = false;
            double gameReadyAt = 0d;
            double tenHoldUntil = 0d;
            double fiftyHoldUntil = 0d;
            double finishAt = double.PositiveInfinity;
            double nextTapAt = 0d;
            int lastCombo = 0;
            audioBuffer.Start(AudioSettings.outputSampleRate);

            while (timer.Elapsed.TotalSeconds < 45d)
            {
                yield return endOfFrame;
                double elapsed = timer.Elapsed.TotalSeconds;
                AttachAudioListener();
                if (frames.Count == 0 || elapsed - frameTimes[frameTimes.Count - 1] >= 1d / 24d)
                    CaptureFrame(pixels, elapsed);

                if (!switchedToGame && elapsed >= 3.0d)
                {
                    Managers.Scene.ChangeScene(W01SceneType.Game);
                    switchedToGame = true;
                    gameReadyAt = elapsed;
                }
                if (switchedToGame && game == null)
                    game = Object.FindFirstObjectByType<GameScene>();
                if (game != null && !startedRound && elapsed - gameReadyAt >= 0.85d)
                {
                    game.bestScore = 100000;
                    game.StartRound();
                    startedRound = true;
                    nextTapAt = elapsed + 1.48d;
                }
                if (startedRound && game != null)
                {
                    ExtendTargetLifetimes(game);
                    int combo = Get<int>(game, "m_Combo");
                    if (sawTen && !sawFifty && combo < lastCombo)
                        throw new InvalidOperationException("Combo dropped before reaching 50; recording aborted.");
                    if (!sawTen && combo >= 10)
                    {
                        sawTen = true;
                        tenHoldUntil = elapsed + 2.0d;
                        nextTapAt = tenHoldUntil;
                    }
                    if (!sawFifty && combo >= 50)
                    {
                        sawFifty = true;
                        fiftyHoldUntil = elapsed + 2.2d;
                    }
                    if (sawFifty && !brokeCombo && elapsed >= fiftyHoldUntil)
                    {
                        CircleTarget missed = Get<List<CircleTarget>>(game, "m_ActiveTargets")
                            .FirstOrDefault(target => target != null && target.Type != TapTargetType.Bomb);
                        if (missed != null)
                        {
                            Invoke(game, "HandleTargetMissed", missed);
                            brokeCombo = true;
                            finishAt = elapsed + 1.1d;
                        }
                    }
                    if (!sawFifty && elapsed >= nextTapAt && elapsed >= tenHoldUntil
                        && Get<float>(game, "m_PhaseCueRemaining") <= 0f)
                    {
                        CircleTarget target = ChooseTarget(game);
                        if (target != null)
                        {
                            target.OnPointerDown(null);
                            nextTapAt = elapsed + 0.17d;
                        }
                    }
                    lastCombo = Get<int>(game, "m_Combo");
                }
                if (elapsed >= finishAt) break;
            }
            audioBuffer.StopAndWrite(Path.Combine(directory, "audio.wav"));
            Object.Destroy(pixels);
            if (!sawTen || !sawFifty || !brokeCombo)
                throw new InvalidOperationException("Recording did not show all combo states. "
                    + "10=" + sawTen + ", 50=" + sawFifty + ", reset=" + brokeCombo);
            WriteFrameManifest();
            File.WriteAllLines(Path.Combine(directory, "capture-notes.txt"), new[]
            {
                "Editor-only scripted playthrough with real Game View frames and mixed game audio.",
                "Includes intro, start cue, actual target taps, sustained 10/50 combo visuals, and combo reset.",
                "Target lifetimes were extended in memory during capture to keep the showcase deterministic.",
                "Frames: " + frames.Count,
                "Duration seconds: " + timer.Elapsed.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture),
                "Game View: " + Screen.width + "x" + Screen.height,
                "Audio PCM bytes: " + audioBuffer.ByteCount,
            });
            RestoreFixtureState();
            VioletTapGameplayCapture.QueueEncoding(directory);
    }

    private void SaveAndSetFixtureState()
    {
        oldBgmPresent = PlayerPrefs.HasKey(BgmKey);
        oldEffectPresent = PlayerPrefs.HasKey(EffectKey);
        oldBgm = PlayerPrefs.GetInt(BgmKey, 1);
        oldEffect = PlayerPrefs.GetInt(EffectKey, 1);
        oldLanguage = EditorPrefs.GetString("VioletTap.EditorLanguage", "System");
        oldRunInBackground = Application.runInBackground;
        oldTargetFrameRate = Application.targetFrameRate;
        oldVsync = QualitySettings.vSyncCount;
        oldTimeScale = Time.timeScale;
        oldRandom = UnityEngine.Random.state;
        oldAdsDisabled = Managers.Ads.AdsDisabled;
        fixtureSaved = true;
        Managers.Ads.SetAdsDisabled(true);
        GameScene.SuppressRecordPersistenceForQa = true;
        EditorPrefs.SetString("VioletTap.EditorLanguage", "Korean");
        Managers.SetBgmEnabled(true);
        Managers.SetEffectEnabled(true);
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Time.timeScale = 1f;
        UnityEngine.Random.InitState(14092026);
    }

    private void RestoreFixtureState()
    {
        if (restored || !fixtureSaved) return;
        restored = true;
        GameScene.SuppressRecordPersistenceForQa = false;
        Managers.Ads.SetAdsDisabled(oldAdsDisabled);
        if (oldBgmPresent) PlayerPrefs.SetInt(BgmKey, oldBgm);
        else PlayerPrefs.DeleteKey(BgmKey);
        if (oldEffectPresent) PlayerPrefs.SetInt(EffectKey, oldEffect);
        else PlayerPrefs.DeleteKey(EffectKey);
        PlayerPrefs.Save();
        Managers.Sound.SetMuted(Define.Sound.Bgm, oldBgm == 0);
        Managers.Sound.SetMuted(Define.Sound.Effect, oldEffect == 0);
        EditorPrefs.SetString("VioletTap.EditorLanguage", oldLanguage);
        Application.runInBackground = oldRunInBackground;
        Application.targetFrameRate = oldTargetFrameRate;
        QualitySettings.vSyncCount = oldVsync;
        Time.timeScale = oldTimeScale;
        UnityEngine.Random.state = oldRandom;
    }

    private void OnDestroy()
    {
        if (directory != null && !restored) RestoreFixtureState();
    }

    private void AttachAudioListener()
    {
        AudioListener listener = Object.FindFirstObjectByType<AudioListener>();
        if (listener == null) return;
        var capture = listener.GetComponent<VioletTapAudioCapture>();
        if (capture == null) capture = listener.gameObject.AddComponent<VioletTapAudioCapture>();
        capture.Buffer = audioBuffer;
    }

    private void CaptureFrame(Texture2D pixels, double elapsed)
    {
        pixels.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
        pixels.Apply(false, false);
        string name = "frame_" + frames.Count.ToString("D5") + ".jpg";
        File.WriteAllBytes(Path.Combine(directory, name), pixels.EncodeToJPG(83));
        frames.Add(name);
        frameTimes.Add(elapsed);
    }

    private void WriteFrameManifest()
    {
        if (frames.Count < 100) throw new InvalidOperationException("Too few gameplay frames: " + frames.Count);
        var lines = new List<string> { "ffconcat version 1.0" };
        for (int i = 0; i < frames.Count; i++)
        {
            lines.Add("file '" + frames[i] + "'");
            double duration = i + 1 < frames.Count
                ? Math.Max(0.01d, frameTimes[i + 1] - frameTimes[i]) : 1d / 24d;
            lines.Add("duration " + duration.ToString("F6", CultureInfo.InvariantCulture));
        }
        lines.Add("file '" + frames[frames.Count - 1] + "'");
        File.WriteAllLines(Path.Combine(directory, "frames.ffconcat"), lines, Encoding.ASCII);
    }

    private static void ExtendTargetLifetimes(GameScene game)
    {
        foreach (CircleTarget target in Get<List<CircleTarget>>(game, "m_ActiveTargets"))
        {
            if (target == null || target.Type == TapTargetType.Bomb) continue;
            var field = typeof(CircleTarget).GetField("m_RemainingLifetime", PrivateInstance);
            float remaining = (float)field.GetValue(target);
            if (remaining < 3f) field.SetValue(target, 3f);
        }
    }

    private static CircleTarget ChooseTarget(GameScene game)
    {
        var targets = Get<List<CircleTarget>>(game, "m_ActiveTargets");
        CircleTarget sequence = targets.Where(t => t != null && t.SequenceOrder > 0)
            .OrderBy(t => t.SequenceOrder).FirstOrDefault();
        return sequence ?? targets.FirstOrDefault(t => t != null && t.Type != TapTargetType.Bomb);
    }

    private static T Get<T>(object target, string field) =>
        (T)target.GetType().GetField(field, PrivateInstance).GetValue(target);

    private static void Invoke(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, PrivateInstance).Invoke(target, args);
}

internal sealed class VioletTapAudioCapture : MonoBehaviour
{
    public GameplayAudioBuffer Buffer;
    private void OnAudioFilterRead(float[] data, int channels) => Buffer?.Append(data, channels);
}

internal sealed class GameplayAudioBuffer
{
    private readonly object gate = new object();
    private readonly MemoryStream data = new MemoryStream();
    private bool recording;
    private int sampleRate;
    private int channels = 2;
    public long ByteCount { get { lock (gate) return data.Length; } }

    public void Start(int rate)
    {
        lock (gate)
        {
            sampleRate = rate;
            recording = true;
        }
    }

    public void Append(float[] samples, int channelCount)
    {
        lock (gate)
        {
            if (!recording) return;
            channels = channelCount;
            byte[] pcm = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short value = (short)Mathf.RoundToInt(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);
                pcm[i * 2] = (byte)value;
                pcm[i * 2 + 1] = (byte)(value >> 8);
            }
            data.Write(pcm, 0, pcm.Length);
        }
    }

    public void StopAndWrite(string path)
    {
        lock (gate)
        {
            if (sampleRate == 0) return;
            recording = false;
            using (var output = new BinaryWriter(File.Create(path)))
            {
                int length = checked((int)data.Length);
                output.Write(Encoding.ASCII.GetBytes("RIFF"));
                output.Write(36 + length);
                output.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                output.Write(16);
                output.Write((short)1);
                output.Write((short)channels);
                output.Write(sampleRate);
                output.Write(sampleRate * channels * 2);
                output.Write((short)(channels * 2));
                output.Write((short)16);
                output.Write(Encoding.ASCII.GetBytes("data"));
                output.Write(length);
                output.Write(data.ToArray());
            }
        }
    }
}
#endif
