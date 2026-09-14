using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using _01.Scripts.Game;
using _01.Scripts.Scene;
using _01.Scripts.UI;
using _01.Scripts.UI.Popup;
using _01.Scripts.UI.SubItem;
using SWGUnity2DCore.Manager;
using SWGUnity2DCore.Util;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Real-time six-hour Editor soak. Uses local inputs/data; never publishes scores or buys anything.</summary>
[InitializeOnLoad]
public static class VioletTapComboQa
{
    private const string Root = "output/qa-combo-6h";
    private const string Pending = "VioletTap.ComboQA.Pending";
    private static IEnumerator routine;
    private static bool longRun;
    private static int lastFrame;
    private static double nextReport;
    private static string language;
    private static bool background;
    private static bool adsDisabled;
    private static bool effectsMuted;
    private static bool musicMuted;
    private static float timeScale;
    private static UnityEngine.Random.State randomState;
    private static readonly List<string> Checks = new();
    [Serializable] private sealed class Progress
    {
        public string state;
        public string startedUtc;
        public string updatedUtc;
        public string endedUtc;
        public double activeSeconds;
        public int timingVersion;
        public double legacyStartupAllowanceSeconds;
        public double verifiedFullSpeedSeconds;
        public float currentTimeScale;
        public int frames;
        public int rounds;
        public int contacts;
        public int checks;
        public long managedBytes;
        public string error;
    }
    private static Progress progress;

    static VioletTapComboQa()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) Begin();
            if (state == PlayModeStateChange.ExitingPlayMode && routine != null) Finish("Play mode was stopped before completion");
        };
        AssemblyReloadEvents.beforeAssemblyReload += () =>
        {
            if (routine != null) Finish("Script reload interrupted QA; active seconds remain recorded");
        };
    }

    private static void Tick()
    {
        if (routine == null)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(Root + "/SMOKE_QA")) { File.Delete(Root + "/SMOKE_QA"); Start(false); }
            else if (File.Exists(Root + "/START_6H_QA")) { File.Delete(Root + "/START_6H_QA"); Start(true); }
            else if (File.Exists(Root + "/RESUME_6H_QA")) { File.Delete(Root + "/RESUME_6H_QA"); Start(true, true); }
            return;
        }
        if (lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;
        progress.frames++;
        if (longRun && Mathf.Approximately(Time.timeScale, 1f))
        {
            double frameSeconds = Mathf.Min(Time.unscaledDeltaTime, .25f);
            progress.activeSeconds += frameSeconds;
            progress.verifiedFullSpeedSeconds += frameSeconds;
        }
        try
        {
            if (File.Exists(Root + "/STOP_QA")) { File.Delete(Root + "/STOP_QA"); Finish("Stopped by request"); return; }
            if (!routine.MoveNext()) { Finish(null); return; }
            if (EditorApplication.timeSinceStartup > nextReport) { Save(); nextReport = EditorApplication.timeSinceStartup + 30; }
        }
        catch (Exception error) { Finish(error.ToString()); }
    }

    [MenuItem("Tools/VioletTap/QA/Combo Smoke Test")]
    public static void Smoke() => Start(false);
    [MenuItem("Tools/VioletTap/QA/Start Six Hour Soak")]
    public static void Soak() => Start(true);
    private static void Start(bool soak, bool resume = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        Directory.CreateDirectory(Root);
        SessionState.SetBool("VioletTap.ComboQA.Long", soak);
        SessionState.SetBool("VioletTap.ComboQA.Resume", resume);
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
    private static void Begin()
    {
        SessionState.SetBool(Pending, false);
        longRun = SessionState.GetBool("VioletTap.ComboQA.Long", false);
        progress = new Progress { state = longRun ? "RUNNING_6H" : "SMOKE_RUNNING", startedUtc = DateTime.UtcNow.ToString("O"), timingVersion = 1 };
        if (longRun && SessionState.GetBool("VioletTap.ComboQA.Resume", false) && File.Exists(Root + "/soak-status.json"))
        {
            progress = JsonUtility.FromJson<Progress>(File.ReadAllText(Root + "/soak-status.json"));
            progress.state = "RUNNING_6H";
            progress.error = null;
            progress.endedUtc = null;
            if (progress.timingVersion == 0)
            {
                // Preserve the recorded counter from the one legacy soak session. Its first
                // 30-second checkpoint had already reached gameplay; reserve a conservative
                // full minute for the initial setup that also counted timeScale=0 frames.
                progress.legacyStartupAllowanceSeconds = 60d;
                progress.verifiedFullSpeedSeconds = Math.Max(0d, progress.activeSeconds - 60d);
                progress.timingVersion = 1;
                File.AppendAllText(Root + "/sessions.log", DateTime.UtcNow.ToString("O")
                    + " CLOCK_MIGRATION recordedActiveSeconds=" + progress.activeSeconds
                    + " verifiedFullSpeedSeconds=" + progress.verifiedFullSpeedSeconds
                    + " legacyStartupAllowanceSeconds=60\n");
            }
        }
        File.AppendAllText(Root + "/sessions.log", DateTime.UtcNow.ToString("O") + " " + progress.state
            + " activeSeconds=" + progress.activeSeconds + "\n");
        Checks.Clear();
        language = EditorPrefs.GetString("VioletTap.EditorLanguage", "System");
        background = Application.runInBackground;
        Application.runInBackground = true;
        timeScale = Time.timeScale;
        randomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(9132026);
        Time.timeScale = 1f;
        GameScene.SuppressRecordPersistenceForQa = true;
        Application.logMessageReceived += OnLog;
        routine = Run();
        Save();
    }

    private static IEnumerator Run()
    {
        for (int i = 0; i < 10; i++) yield return null;
        adsDisabled = Managers.Ads.AdsDisabled;
        Managers.Ads.SetAdsDisabled(true);
        musicMuted = !Managers.IsBgmEnabled;
        effectsMuted = !Managers.IsEffectEnabled;
        // Let long-running QA exercise sources without playing six hours of audio aloud.
        if (longRun) { Managers.Sound.SetMuted(Define.Sound.Bgm, true); Managers.Sound.SetMuted(Define.Sound.Effect, true); }
        foreach (string locale in new[] { "Korean", "English" })
        {
            EditorPrefs.SetString("VioletTap.EditorLanguage", locale);
            Managers.Scene.ChangeScene(W01SceneType.Game);
            for (int i = 0; i < 8; i++) yield return null;
            var game = Object.FindFirstObjectByType<GameScene>();
            var ui = Object.FindFirstObjectByType<UI_GamePopup>();
            var view = ui.GetComponentInChildren<ComboStatusView>(true);
            Check(view != null && !view.card.gameObject.activeSelf, locale + " authored combo HUD hidden at Ready");
            Capture("ready-" + locale);
            for (int i = 0; i < 3; i++) yield return null;
            CheckStatusBelowHud(ui, locale + " Ready");
            game.StartRound();
            Check(!ui.GetTextStatus().enabled, locale + " combo card replaces overlapping top instruction");
            Time.timeScale = 0f;
            for (int i = 0; i < 5; i++) TapAs(game, TapTargetType.Normal);
            Check(view.comboLabel.text.Contains("5 COMBO!") && view.multiplierLabel.text.Contains("x2"), locale + " 5 combo shows x2 and counter");
            Check(Mathf.Approximately(view.progressFill.fillAmount, .5f), locale + " half-filled fever meter");
            Time.timeScale = 1f;
            double settle = EditorApplication.timeSinceStartup + .3;
            while (EditorApplication.timeSinceStartup < settle) yield return null;
            Time.timeScale = 0f;
            Canvas.ForceUpdateCanvases();
            Capture("combo-" + locale);
            for (int i = 0; i < 3; i++) yield return null;
            for (int i = 0; i < 5; i++) TapAs(game, TapTargetType.Normal);
            Check(Get<float>(game, "m_FeverRemaining") > 0f && Targets(game).Count == 2, locale + " 10 combo triggers fever");
            Invoke(game, "RefreshComboPresentation");
            Check(view.progressLabel.text.Contains(GameLocalization.T("FEVER", "피버")), locale + " fever countdown replaces charge meter");
            Invoke(game, "HandleTargetMissed", Targets(game).First(t => t.Type != TapTargetType.Bomb));
            Check(Get<int>(game, "m_Combo") == 0 && Get<float>(game, "m_FeverRemaining") == 0f, locale + " miss ends combo and fever");
            Check(view.failureRoot.gameObject.activeSelf && view.failureLabel.text.Contains("10"), locale + " failure displays exact lost combo");
            Check(Targets(game).Count == 1, locale + " failed fever returns to normal target count");
            Capture("combo-break-" + locale);
            for (int i = 0; i < 3; i++) yield return null;
            for (int i = 0; i < 5; i++) TapAs(game, TapTargetType.Normal);
            TapAs(game, TapTargetType.Bomb);
            Check(Get<int>(game, "m_Combo") == 0 && view.failureLabel.text.Contains(GameLocalization.T("BOMB", "폭탄")), locale + " bomb break feedback");
            float early = RoundPacing.Lifetime(0, TapTargetType.Normal, false, game.Config);
            float late = RoundPacing.Lifetime(30, TapTargetType.Normal, false, game.Config);
            Check(early > late * 2f && late >= game.Config.minimumReactionSeconds, "Difficulty ramps substantially with a reaction floor");
            Set(game, "m_RoundElapsed", 24f);
            ui.ShowPaceIncrease(3);
            Check(!ui.GetTextStatus().enabled, locale + " speed cue leaves the gameplay status text hidden");
            Check(view.failureRoot.gameObject.activeSelf
                && view.failureLabel.text == GameLocalization.T("SPEED UP!  LEVEL 3", "스피드 업!  단계 3"),
                locale + " speed level appears in the impact cue");
            float before = Get<float>(game, "m_RoundElapsed");
            TapAs(game, TapTargetType.TimeBonus);
            Check(Get<float>(game, "m_RoundElapsed") == before, "Time bonus cannot reverse difficulty");
            foreach (TapTargetType type in Enum.GetValues(typeof(TapTargetType)))
                Check(RoundPacing.Lifetime(600, type, true, game.Config) >= .38f, "Reaction floor: " + type);
            game.SetHelpOpen(true);
            float elapsed = Get<float>(game, "m_RoundElapsed");
            Time.timeScale = 1f;
            double until = EditorApplication.timeSinceStartup + .3;
            while (EditorApplication.timeSinceStartup < until) yield return null;
            Check(Mathf.Approximately(Get<float>(game, "m_RoundElapsed"), elapsed), "Help pause freezes difficulty clock");
            game.SetHelpOpen(false);
            game.GetComponent<GameFlow>().FinishGame();
            for (int i = 0; i < 4; i++) yield return null;
            Check(Targets(game).Count == 0 && !view.card.gameObject.activeSelf && !view.failureRoot.gameObject.activeSelf,
                "Result clears targets and combo/failure HUD");
            Check(ui.GetTextStatus().enabled && ui.GetTextStatus().text == GameLocalization.T("RESULT", "결과"),
                locale + " result title appears after the speed cue");
            Capture("result-" + locale);
            for (int i = 0; i < 3; i++) yield return null;
            CheckStatusBelowHud(ui, locale + " Result");
            Check(!game.GetComponentsInChildren<SpriteRenderer>().Any(r => r.gameObject.activeInHierarchy), "Result clears all touch and miss afterimages");
            game.RetryRound();
            for (int i = 0; i < 3; i++) yield return null;
            ui.ShowPaceIncrease(2);
            string pendingMessage = GameLocalization.T("PURCHASE PENDING", "구매 대기 중");
            Invoke(ui, "OnPurchaseStatus", pendingMessage);
            game.GetComponent<GameFlow>().FinishGame();
            for (int i = 0; i < 3; i++) yield return null;
            Check(ui.GetTextStatus().enabled && ui.GetTextStatus().text == pendingMessage,
                locale + " local purchase-status notice survives round completion");
            var ranking = Ranking(locale);
            while (ranking.MoveNext()) yield return ranking.Current;
        }
        if (!longRun) yield break;
        Time.timeScale = 1f;
        while (progress.verifiedFullSpeedSeconds < 6 * 60 * 60)
        {
            EditorPrefs.SetString("VioletTap.EditorLanguage", progress.rounds % 2 == 0 ? "Korean" : "English");
            Managers.Scene.ChangeScene(W01SceneType.Game);
            for (int i = 0; i < 8; i++) yield return null;
            var game = Object.FindFirstObjectByType<GameScene>();
            var flow = game.GetComponent<GameFlow>();
            game.StartRound();
            double nextTap = EditorApplication.timeSinceStartup + .3;
            double roundStart = EditorApplication.timeSinceStartup;
            bool pausedOnce = false;
            while (flow.State == GameFlowState.Playing)
            {
                if (progress.verifiedFullSpeedSeconds >= 21600) { flow.FinishGame(); break; }
                if (EditorApplication.timeSinceStartup - roundStart > 180) throw new Exception("Round did not terminate within 180 real seconds");
                if (!pausedOnce && Get<float>(game, "m_RoundElapsed") > 8f)
                {
                    pausedOnce = true;
                    game.SetHelpOpen(true);
                    float saved = Get<float>(game, "m_RoundElapsed");
                    for (int i = 0; i < 12; i++) yield return null;
                    Check(Mathf.Approximately(saved, Get<float>(game, "m_RoundElapsed")), "Soak pause preserves pacing");
                    game.SetHelpOpen(false);
                }
                if (EditorApplication.timeSinceStartup >= nextTap)
                {
                    var targets = Targets(game);
                    var target = targets.FirstOrDefault(t => t.Type != TapTargetType.Bomb);
                    if (target == null && progress.rounds % 7 == 0) target = targets.FirstOrDefault();
                    if (target != null) Contact(target);
                    // Deliberate missed windows alternate with fast contact bursts.
                    nextTap = EditorApplication.timeSinceStartup + (progress.contacts % 13 == 0 ? 1.7 : UnityEngine.Random.Range(.12f, .38f));
                }
                yield return null;
            }
            for (int i = 0; i < 8; i++) yield return null;
            Check(Targets(game).Count == 0, "Soak round leaves no targets");
            Check(!Object.FindFirstObjectByType<UI_GamePopup>().GetComponentInChildren<ComboStatusView>(true).card.gameObject.activeSelf,
                "Soak result hides combo HUD");
            progress.rounds++;
            if (progress.rounds % 20 == 0) { Capture("soak-round-" + progress.rounds); Save(); }
        }
    }

    private static void CheckStatusBelowHud(UI_GamePopup ui, string phase)
    {
        Canvas.ForceUpdateCanvases();
        Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var points = corners.Select(point => (Vector2)Camera.main.WorldToScreenPoint(point)).ToArray();
            return Rect.MinMaxRect(points.Min(point => point.x), points.Min(point => point.y),
                points.Max(point => point.x), points.Max(point => point.y));
        }
        Rect statusRect = ScreenRect(ui.GetTextStatus().rectTransform);
        float hudBottom = new[] { ui.GetTextScore(), ui.GetTextTime(), ui.GetTextBest() }
            .Min(label => ScreenRect((RectTransform)label.transform.parent).yMin);
        Checks.Add($"STATUS {phase} top={statusRect.yMax:F2} HUD bottom={hudBottom:F2}");
        Check(statusRect.yMax <= hudBottom - 4f, phase + " status clears the score/time/best HUD");
    }

    private static IEnumerator Ranking(string locale)
    {
        var panel = Object.Instantiate(Resources.Load<GameObject>("Prefabs/UI/Popup/UI_Rankpopup"));
        panel.GetComponent<UI_RankPopup>().enabled = false;
        Managers.UI.SetCanvas(panel);
        GameLocalization.ApplyFont(panel.transform);
        typeof(UI_RankPopup).Assembly.GetType("_01.Scripts.UI.Popup.PopupPresentation").GetMethod("Prepare")
            .Invoke(null, new object[] { panel.transform });
        foreach (var text in panel.GetComponentsInChildren<TMP_Text>())
        {
            if (text.name == "txtTitle") text.text = GameLocalization.T("RANKING", "랭킹");
            if (text.name == "txtClose") text.text = GameLocalization.T("CLOSE", "닫기");
        }
        var scroll = panel.GetComponentInChildren<ScrollRect>();
        var rows = new List<UI_RankingItem>();
        for (int i = 0; i < 10; i++)
        {
            var row = Managers.UI.MakeSubItem<UI_RankingItem>(scroll.content, "itemRanking");
            row.SetProfile(i + 1, i == 0 ? new string(locale == "Korean" ? '가' : 'W', 50) : "NONAME", i == 0 ? 2147483647d : 999999, i == 0);
            rows.Add(row);
        }
        for (int i = 0; i < 5; i++) yield return null;
        Canvas.ForceUpdateCanvases();
        foreach (var row in rows)
        {
            var score = row.GetComponentsInChildren<TMP_Text>().Single(t => t.name == "txtScore");
            score.ForceMeshUpdate();
            Check(!score.isTextTruncated, locale + " full ten-digit ranking score");
            var corners = new Vector3[4];
            score.rectTransform.GetWorldCorners(corners);
            float right = scroll.viewport.InverseTransformPoint(corners[2]).x;
            float left = scroll.viewport.InverseTransformPoint(corners[0]).x;
            Check(right < scroll.viewport.rect.xMax - 5 && left > scroll.viewport.rect.xMin,
                locale + " score column lies inside the actual list mask with padding");
        }
        Capture("ranking-" + locale);
        for (int i = 0; i < 3; i++) yield return null;
        scroll.verticalNormalizedPosition = 0f;
        for (int i = 0; i < 3; i++) yield return null;
        Capture("ranking-bottom-" + locale);
        Object.Destroy(panel);
        yield return null;
    }

    private static void Contact(CircleTarget target)
    {
        Physics2D.SyncTransforms();
        var pointer = new PointerEventData(EventSystem.current) { position = Camera.main.WorldToScreenPoint(target.transform.position), button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        var first = hits.FirstOrDefault();
        var handler = first.gameObject == null ? null : ExecuteEvents.GetEventHandler<IPointerDownHandler>(first.gameObject);
        Check(handler == target.gameObject, "Target receives contact through combo/effect UI");
        ExecuteEvents.ExecuteHierarchy(first.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        progress.contacts++;
    }
    private static void TapAs(GameScene game, TapTargetType type)
    {
        var target = Targets(game)[0];
        target.Bind(type, 100f, .82f, t => Invoke(game, "HandleTargetTapped", t), t => Invoke(game, "HandleTargetMissed", t));
        target.SetVisual(target.GetSprite(type), Color.white);
        target.OnPointerDown(null);
    }
    private static List<CircleTarget> Targets(GameScene game) => Get<List<CircleTarget>>(game, "m_ActiveTargets");
    private static T Get<T>(object obj, string field) => (T)obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
    private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
    private static void Invoke(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, args);
    private static void Capture(string name) => ScreenCapture.CaptureScreenshot(Root + "/" + name + ".png");
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        progress.checks++;
        if (Checks.Count < 500) Checks.Add("PASS " + message);
    }
    private static void OnLog(string condition, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            progress.error = condition + "\n" + stack;
    }
    private static void Save()
    {
        progress.updatedUtc = DateTime.UtcNow.ToString("O");
        progress.managedBytes = GC.GetTotalMemory(false);
        progress.currentTimeScale = Time.timeScale;
        File.WriteAllText(Root + "/status.json", JsonUtility.ToJson(progress, true));
        if (longRun) File.WriteAllText(Root + "/soak-status.json", JsonUtility.ToJson(progress, true));
        if (!string.IsNullOrEmpty(progress.error)) throw new Exception(progress.error);
    }
    private static void Finish(string error)
    {
        routine = null;
        Application.logMessageReceived -= OnLog;
        progress.error = error ?? progress.error;
        progress.state = progress.error == null ? (longRun ? "COMPLETED_6H" : "SMOKE_PASSED") : "FAILED_OR_INTERRUPTED";
        progress.endedUtc = DateTime.UtcNow.ToString("O");
        progress.updatedUtc = progress.endedUtc;
        File.WriteAllText(Root + "/status.json", JsonUtility.ToJson(progress, true));
        if (longRun) File.WriteAllText(Root + "/soak-status.json", JsonUtility.ToJson(progress, true));
        File.AppendAllText(Root + "/sessions.log", progress.endedUtc + " " + progress.state
            + " activeSeconds=" + progress.activeSeconds + " error=" + progress.error + "\n");
        File.WriteAllLines(Root + "/" + (longRun ? "soak-checks.txt" : "smoke-checks.txt"), Checks);
        GameScene.SuppressRecordPersistenceForQa = false;
        Time.timeScale = timeScale;
        Application.runInBackground = background;
        UnityEngine.Random.state = randomState;
        EditorPrefs.SetString("VioletTap.EditorLanguage", language);
        Managers.Ads.SetAdsDisabled(adsDisabled);
        Managers.Sound.SetMuted(Define.Sound.Bgm, musicMuted);
        Managers.Sound.SetMuted(Define.Sound.Effect, effectsMuted);
        EditorApplication.ExitPlaymode();
    }
}
