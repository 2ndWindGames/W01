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
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Object = UnityEngine.Object;

/// <summary>Editor-only regression scenarios. Never buys IAP or submits a leaderboard score.</summary>
[InitializeOnLoad]
public static class VioletTapQaRunner
{
    private const string PendingKey = "VioletTap.QA.Pending";
    private const string BestKey = "MiniGameKit.TapGame.BestScore";
    private static readonly string Root = Path.GetFullPath("output/qa-2026-09-13");
    private static readonly List<string> Results = new();
    private static readonly List<string> Errors = new();
    private static IEnumerator routine;
    private static string runDirectory;
    private static double started;
    private static int lastFrame = -1;
    private static float previousTimeScale;
    private static UnityEngine.Random.State previousRandom;
    private static bool? previousAdsDisabled;

    static VioletTapQaRunner()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false)) Begin();
            if (state == PlayModeStateChange.ExitingPlayMode && routine != null) Finish("Interrupted by leaving Play mode");
        };
    }

    [MenuItem("Tools/VioletTap/QA/Run regression suite")]
    public static void Run()
    {
        if (routine != null || EditorApplication.isCompiling) return;
        SessionState.SetBool(PendingKey, true);
        if (EditorApplication.isPlaying) Begin();
        else EditorApplication.EnterPlaymode();
    }

    [MenuItem("Tools/VioletTap/QA/Check nickname input")]
    public static void RunNicknameCheck()
    {
        SessionState.SetBool("VioletTap.QA.NicknameOnly", true);
        Run();
    }

    [MenuItem("Tools/VioletTap/QA/Check touch feedback and help")]
    public static void RunFeedbackCheck()
    {
        SessionState.SetBool("VioletTap.QA.FeedbackOnly", true);
        Run();
    }

    [MenuItem("Tools/VioletTap/QA/Check status and help lifecycle")]
    public static void RunStatusCheck()
    {
        SessionState.SetBool("VioletTap.QA.StatusOnly", true);
        Run();
    }

    [MenuItem("Tools/VioletTap/QA/Check application pause and resume")]
    public static void RunBackgroundCheck()
    {
        SessionState.SetBool("VioletTap.QA.BackgroundOnly", true);
        Run();
    }

    [MenuItem("Tools/VioletTap/QA/Check repeated scene transitions")]
    public static void RunSceneCycleCheck()
    {
        SessionState.SetBool("VioletTap.QA.SceneCyclesOnly", true);
        Run();
    }

    [MenuItem("Tools/VioletTap/QA/Check settings and ranking popups")]
    public static void RunPopupCheck()
    {
        SessionState.SetBool("VioletTap.QA.PopupsOnly", true);
        Run();
    }

    [MenuItem("Tools/VioletTap/QA/Check ranking rows with local data")]
    public static void RunRankingCheck()
    {
        SessionState.SetBool("VioletTap.QA.RankingOnly", true);
        Run();
    }

    [MenuItem("Tools/VioletTap/QA/Check repeated rounds and contact positions")]
    public static void RunRoundCheck()
    {
        SessionState.SetBool("VioletTap.QA.RoundsOnly", true);
        Run();
    }

    [MenuItem("Tools/VioletTap/QA/Check nickname modal lifecycle")]
    public static void RunModalCheck()
    {
        SessionState.SetBool("VioletTap.QA.ModalOnly", true);
        Run();
    }

    [MenuItem("Tools/VioletTap/QA/Check HUD score readability")]
    public static void RunHudCheck()
    {
        SessionState.SetBool("VioletTap.QA.HudOnly", true);
        Run();
    }

    [MenuItem("Tools/VioletTap/QA/Check gameplay captions and fever transitions")]
    public static void RunCaptionCheck()
    {
        SessionState.SetBool("VioletTap.QA.CaptionsOnly", true);
        Run();
    }

    [MenuItem("Tools/VioletTap/QA/Check interrupted music fades and mute persistence")]
    public static void RunMusicCheck()
    {
        SessionState.SetBool("VioletTap.QA.MusicOnly", true);
        Run();
    }

    [MenuItem("Tools/VioletTap/QA/Check redesigned help layout")]
    public static void RunHelpLayoutCheck()
    {
        SessionState.SetBool("VioletTap.QA.HelpLayoutOnly", true);
        Run();
    }

    private static void Begin()
    {
        SessionState.SetBool(PendingKey, false);
        runDirectory = Path.Combine(Root, "run-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(runDirectory);
        Results.Clear();
        Errors.Clear();
        previousTimeScale = Time.timeScale;
        previousRandom = UnityEngine.Random.state;
        previousAdsDisabled = null;
        SessionState.SetBool("VioletTap.QA.HadBest", PlayerPrefs.HasKey(BestKey));
        SessionState.SetInt("VioletTap.QA.Best", PlayerPrefs.GetInt(BestKey));
        foreach (string key in new[] { "VioletTap.Audio.BgmEnabled", "VioletTap.Audio.EffectEnabled", TapHaptics.EnabledKey })
        {
            SessionState.SetBool("VioletTap.QA.Had." + key, PlayerPrefs.HasKey(key));
            SessionState.SetInt("VioletTap.QA.Value." + key, PlayerPrefs.GetInt(key, 1));
        }
        SessionState.SetString("VioletTap.QA.Language", EditorPrefs.GetString("VioletTap.EditorLanguage", "System"));
        Application.logMessageReceived += OnLog;
        started = EditorApplication.timeSinceStartup;
        bool nicknameOnly = SessionState.GetBool("VioletTap.QA.NicknameOnly", false);
        SessionState.SetBool("VioletTap.QA.NicknameOnly", false);
        bool feedbackOnly = SessionState.GetBool("VioletTap.QA.FeedbackOnly", false);
        SessionState.SetBool("VioletTap.QA.FeedbackOnly", false);
        bool statusOnly = SessionState.GetBool("VioletTap.QA.StatusOnly", false);
        SessionState.SetBool("VioletTap.QA.StatusOnly", false);
        bool backgroundOnly = SessionState.GetBool("VioletTap.QA.BackgroundOnly", false);
        SessionState.SetBool("VioletTap.QA.BackgroundOnly", false);
        bool sceneCyclesOnly = SessionState.GetBool("VioletTap.QA.SceneCyclesOnly", false);
        SessionState.SetBool("VioletTap.QA.SceneCyclesOnly", false);
        bool popupsOnly = SessionState.GetBool("VioletTap.QA.PopupsOnly", false);
        SessionState.SetBool("VioletTap.QA.PopupsOnly", false);
        bool rankingOnly = SessionState.GetBool("VioletTap.QA.RankingOnly", false);
        SessionState.SetBool("VioletTap.QA.RankingOnly", false);
        bool roundsOnly = SessionState.GetBool("VioletTap.QA.RoundsOnly", false);
        SessionState.SetBool("VioletTap.QA.RoundsOnly", false);
        bool modalOnly = SessionState.GetBool("VioletTap.QA.ModalOnly", false);
        SessionState.SetBool("VioletTap.QA.ModalOnly", false);
        bool hudOnly = SessionState.GetBool("VioletTap.QA.HudOnly", false);
        SessionState.SetBool("VioletTap.QA.HudOnly", false);
        bool captionsOnly = SessionState.GetBool("VioletTap.QA.CaptionsOnly", false);
        SessionState.SetBool("VioletTap.QA.CaptionsOnly", false);
        bool musicOnly = SessionState.GetBool("VioletTap.QA.MusicOnly", false);
        SessionState.SetBool("VioletTap.QA.MusicOnly", false);
        bool helpLayoutOnly = SessionState.GetBool("VioletTap.QA.HelpLayoutOnly", false);
        SessionState.SetBool("VioletTap.QA.HelpLayoutOnly", false);
        routine = helpLayoutOnly ? HelpLayoutScenarios() : musicOnly ? MusicScenarios() : captionsOnly ? CaptionScenarios() : hudOnly ? HudScenarios() : modalOnly ? ModalScenarios() : roundsOnly ? RoundScenarios() : rankingOnly ? RankingScenarios() : popupsOnly ? PopupScenarios() : sceneCyclesOnly ? SceneCycleScenarios() : backgroundOnly ? BackgroundScenarios() : statusOnly ? StatusScenarios() : feedbackOnly ? FeedbackScenarios() : nicknameOnly ? NicknameScenarios() : Scenarios();
    }

    private static void Tick()
    {
        if (routine == null)
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating
                && TryConsumeRequest(Path.Combine(Root, "REFRESH_QA")))
            {
                AssetDatabase.Refresh();
                return;
            }
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating
                && TryConsumeRequest(Path.Combine(Root, "CAPTURE_ANDROID_SETTINGS_QA")))
            {
                CaptureAndroidSettings();
                return;
            }
            string roundsRequest = Path.Combine(Root, "RUN_ROUNDS_QA");
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating
                && TryConsumeRequest(Path.Combine(Root, "RUN_HELP_LAYOUT_QA")))
            {
                RunHelpLayoutCheck();
                return;
            }
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating
                && TryConsumeRequest(Path.Combine(Root, "RUN_MUSIC_QA")))
            {
                RunMusicCheck();
                return;
            }
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating
                && TryConsumeRequest(Path.Combine(Root, "RUN_CAPTIONS_QA")))
            {
                RunCaptionCheck();
                return;
            }
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating
                && TryConsumeRequest(Path.Combine(Root, "RUN_HUD_QA")))
            {
                RunHudCheck();
                return;
            }
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating
                && TryConsumeRequest(Path.Combine(Root, "RUN_MODAL_QA")))
            {
                RunModalCheck();
                return;
            }
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && TryConsumeRequest(roundsRequest))
            {
                RunRoundCheck();
                return;
            }
            string rankingRequest = Path.Combine(Root, "RUN_RANKING_QA");
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && TryConsumeRequest(rankingRequest))
            {
                RunRankingCheck();
                return;
            }
            string popupsRequest = Path.Combine(Root, "RUN_POPUPS_QA");
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && TryConsumeRequest(popupsRequest))
            {
                RunPopupCheck();
                return;
            }
            string sceneCyclesRequest = Path.Combine(Root, "RUN_SCENE_CYCLES_QA");
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && TryConsumeRequest(sceneCyclesRequest))
            {
                RunSceneCycleCheck();
                return;
            }
            string backgroundRequest = Path.Combine(Root, "RUN_BACKGROUND_QA");
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && TryConsumeRequest(backgroundRequest))
            {
                RunBackgroundCheck();
                return;
            }
            string layoutRequest = Path.Combine(Root, "CAPTURE_LAYOUT_QA");
            if (EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && TryConsumeRequest(layoutRequest))
            {
                CaptureLayout();
                return;
            }
            string statusRequest = Path.Combine(Root, "RUN_STATUS_QA");
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && TryConsumeRequest(statusRequest))
            {
                RunStatusCheck();
                return;
            }
            string feedbackRequest = Path.Combine(Root, "RUN_FEEDBACK_QA");
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && TryConsumeRequest(feedbackRequest))
            {
                RunFeedbackCheck();
                return;
            }
            string nicknameRequest = Path.Combine(Root, "RUN_NICKNAME_QA");
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && TryConsumeRequest(nicknameRequest))
            {
                RunNicknameCheck();
                return;
            }
            string request = Path.Combine(Root, "RUN_QA");
            if (!EditorApplication.isCompiling && !EditorApplication.isUpdating && TryConsumeRequest(request))
            {
                Run();
            }
            return;
        }
        if (!EditorApplication.isPlaying || lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;
        try
        {
            if (EditorApplication.timeSinceStartup - started > 90) throw new TimeoutException("QA exceeded 90 seconds");
            if (!routine.MoveNext()) Finish(null);
        }
        catch (Exception exception) { Finish(exception.ToString()); }
    }

    private static bool TryConsumeRequest(string path)
    {
        if (!File.Exists(path)) return false;
        try
        {
            File.Delete(path);
            return true;
        }
        catch (IOException)
        {
            // The writer may still hold its handle this frame; retry after it closes.
            return false;
        }
    }

    [MenuItem("Tools/VioletTap/QA/Capture Android build settings")]
    public static void CaptureAndroidSettings()
    {
        var android = UnityEditor.Build.NamedBuildTarget.Android;
        var report = new List<string>
        {
            "Active target: " + EditorUserBuildSettings.activeBuildTarget,
            "Backend: " + PlayerSettings.GetScriptingBackend(android),
            "Architectures: " + PlayerSettings.Android.targetArchitectures,
            "Managed stripping: " + PlayerSettings.GetManagedStrippingLevel(android),
            "Application ID: " + PlayerSettings.GetApplicationIdentifier(android),
            "Version: " + PlayerSettings.bundleVersion + " (" + PlayerSettings.Android.bundleVersionCode + ")",
            "Minimum API: " + PlayerSettings.Android.minSdkVersion,
            "Target API: " + PlayerSettings.Android.targetSdkVersion,
            "Build app bundle: " + EditorUserBuildSettings.buildAppBundle,
            "Development build: " + EditorUserBuildSettings.development,
            "Custom keystore configured: " + PlayerSettings.Android.useCustomKeystore,
        };
        foreach (var scene in EditorBuildSettings.scenes.Where(s => s.enabled))
            report.Add("Enabled scene: " + scene.path + " exists=" + File.Exists(scene.path));
        var lastBuild = UnityEditor.Build.Reporting.BuildReport.GetLatestReport();
        if (lastBuild != null)
        {
            var summary = lastBuild.summary;
            report.Add("Last build platform: " + summary.platform);
            report.Add("Last build result: " + summary.result);
            report.Add("Last build end timestamp (as reported): " + summary.buildEndedAt.ToString("O")
                + " kind=" + summary.buildEndedAt.Kind);
            report.Add("Last build output: " + summary.outputPath);
            report.Add("Last build reported total bytes: " + summary.totalSize);
            if (File.Exists(summary.outputPath)) report.Add("Existing output file bytes: " + new FileInfo(summary.outputPath).Length);
            report.Add("Last build errors: " + summary.totalErrors);
            report.Add("Last build warnings: " + summary.totalWarnings);
        }
        string path = Path.Combine(Root, "android-settings-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
        File.WriteAllLines(path, report);
        Debug.Log("VioletTap Android settings captured: " + path);
    }

    [MenuItem("Tools/VioletTap/QA/Capture current layout")]
    public static void CaptureLayout()
    {
        if (!EditorApplication.isPlaying) return;
        Canvas.ForceUpdateCanvases();
        string directory = Path.Combine(Root, "layout-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(directory);
        var report = new List<string>
        {
            $"Screen {Screen.width}x{Screen.height} safeArea={Screen.safeArea} orientation={Screen.orientation}",
            $"Editor GameView main size={Handles.GetMainGameViewSize()}",
            $"TimeScale={Time.timeScale} effects={Managers.IsEffectEnabled} haptics={TapHaptics.IsEnabled}"
        };
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            report.Add($"CAMERA {camera.name} rect={camera.rect} pixels={camera.pixelRect} size={camera.orthographicSize}");
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas))
            report.Add($"CANVAS {canvas.name} mode={canvas.renderMode} camera={canvas.worldCamera?.name} scale={canvas.scaleFactor} rect={((RectTransform)canvas.transform).rect}");
        var help = Object.FindFirstObjectByType<UI_GameHelp>();
        report.Add("HelpOpen=" + (help != null && help.IsOpen));
        foreach (var label in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
        {
            if (!label.gameObject.activeInHierarchy) continue;
            label.ForceMeshUpdate();
            report.Add((label.isTextOverflowing ? "FAIL " : "PASS ") + "Text fits: " + label.transform.parent.name + "/" + label.name);
        }
        foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
        {
            if (!button.gameObject.activeInHierarchy) continue;
            var canvas = button.GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var rect = camera == null ? new Rect(0, 0, Screen.width, Screen.height) : camera.pixelRect;
            var corners = new Vector3[4];
            ((RectTransform)button.transform).GetWorldCorners(corners);
            var points = corners.Select(c => RectTransformUtility.WorldToScreenPoint(camera, c)).ToArray();
            bool contained = points.All(p => p.x >= rect.xMin - 1 && p.x <= rect.xMax + 1 && p.y >= rect.yMin - 1 && p.y <= rect.yMax + 1);
            report.Add((contained ? "PASS " : "FAIL ") + "Button inside viewport: " + button.name);
            var hits = new List<RaycastResult>();
            var pointer = new PointerEventData(EventSystem.current) { position = (points[0] + points[2]) * .5f };
            EventSystem.current.RaycastAll(pointer, hits);
            string top = hits.Count == 0 ? "none" : hits[0].gameObject.name;
            report.Add("RAYCAST " + button.name + " => " + top);
        }
        File.WriteAllLines(Path.Combine(directory, "layout.txt"), report);
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, "screen.png"));
        File.WriteAllText(Path.Combine(Root, "latest-layout.txt"), directory);
        Debug.Log("VioletTap layout captured: " + directory);
    }

    private static IEnumerator HelpLayoutScenarios()
    {
        for (int i = 0; i < 8; i++) yield return null;
        previousAdsDisabled = Managers.Ads.AdsDisabled;
        Managers.Ads.SetAdsDisabled(true);
        Time.timeScale = 0f;
        foreach (string language in new[] { "English", "Korean" })
        {
            EditorPrefs.SetString("VioletTap.EditorLanguage", language);
            Managers.Scene.ChangeScene(W01SceneType.Game);
            for (int i = 0; i < 6; i++) yield return null;
            var game = Object.FindFirstObjectByType<GameScene>();
            var ui = Object.FindFirstObjectByType<UI_GamePopup>();
            var help = Object.FindFirstObjectByType<UI_GameHelp>();
            game.bestScore = 100000;
            var opener = help.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "btnHelp");
            Rect ScreenRect(RectTransform rect)
            {
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                var points = corners.Select(c => (Vector2)Camera.main.WorldToScreenPoint(c)).ToArray();
                return Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.y), points.Max(p => p.x), points.Max(p => p.y));
            }
            Canvas.ForceUpdateCanvases();
            var openerRect = ScreenRect((RectTransform)opener.transform);
            foreach (var control in ui.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                if (control == opener) continue;
                Check(!openerRect.Overlaps(ScreenRect((RectTransform)control.transform)), language + " help does not overlap " + control.name);
            }
            Check(openerRect.height >= 60f, language + " help has a large screen-space touch area");
            Check(opener.GetComponentInChildren<TMP_Text>().text == "?", language + " help uses only a question icon");
            Check(Mathf.Abs(openerRect.center.y - ScreenRect((RectTransform)ui.GetButtonStart().transform).center.y) < 1f,
                language + " help and start share the same footer baseline");
            Capture("help-entry-" + language.ToLowerInvariant());
            for (int i = 0; i < 3; i++) yield return null;
            ClickVisible(ui.GetButtonStart().gameObject, language + " original start control receives input");
            ClickVisible(opener.gameObject, language + " footer help receives input");
            var timer = Get<CountdownTimer>(game, "m_Timer");
            float remaining = timer.Remaining;
            Time.timeScale = 1f;
            for (int i = 0; i < 8; i++) yield return null;
            Check(help.IsOpen && game.IsGameplayPaused && Mathf.Approximately(remaining, timer.Remaining), language + " guide preserves the active round");
            foreach (var label in help.GetComponentsInChildren<TMP_Text>())
            {
                label.ForceMeshUpdate();
                Check(!label.isTextOverflowing, language + " guide text fits: " + label.transform.parent.name + "/" + label.name);
            }
            Capture("help-top-" + language.ToLowerInvariant());
            for (int i = 0; i < 3; i++) yield return null;
            var scroll = help.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            int score = Get<int>(game, "m_Score");
            foreach (string name in new[] { "CardNormal", "CardQuick", "CardTime", "CardBomb", "CardFever" })
            {
                var card = help.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == name);
                float maxScroll = Mathf.Max(0f, scroll.content.rect.height - scroll.viewport.rect.height);
                float y = -((RectTransform)card.transform).anchoredPosition.y;
                scroll.verticalNormalizedPosition = maxScroll > 0f ? 1f - Mathf.Clamp01((y - scroll.viewport.rect.height * .5f) / maxScroll) : 1f;
                Canvas.ForceUpdateCanvases();
                yield return null;
                ClickVisible(card.gameObject, language + " scroll exposes preview " + name);
            }
            scroll.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
            var scrollEvent = new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0f, -30f) };
            ExecuteEvents.Execute(scroll.gameObject, scrollEvent, ExecuteEvents.scrollHandler);
            for (int i = 0; i < 3; i++) yield return null;
            Check(scroll.content.rect.height <= scroll.viewport.rect.height || scroll.verticalNormalizedPosition < .01f,
                language + " scrolling reaches the last card");
            Check(Get<int>(game, "m_Score") == score && Mathf.Approximately(remaining, timer.Remaining), language + " previews and scrolling never advance gameplay");
            Capture("help-bottom-" + language.ToLowerInvariant());
            for (int i = 0; i < 3; i++) yield return null;
            var buttons = help.GetComponentsInChildren<UnityEngine.UI.Button>();
            var effects = buttons.Single(b => b.name == "btnHelpEffects");
            bool originalEffects = Managers.IsEffectEnabled;
            ClickVisible(effects.gameObject, language + " fixed effects control receives input");
            Check(Managers.IsEffectEnabled != originalEffects, language + " effects setting toggles");
            ClickVisible(effects.gameObject, language + " effects setting restores");
            var haptics = buttons.Single(b => b.name == "btnHelpHaptics");
            bool originalHaptics = TapHaptics.IsEnabled;
            ClickVisible(haptics.gameObject, language + " fixed haptics control receives input");
            Check(TapHaptics.IsEnabled != originalHaptics, language + " haptics setting toggles");
            ClickVisible(haptics.gameObject, language + " haptics setting restores");
            ClickVisible(buttons.Single(b => b.name == "btnHelpClose").gameObject, language + " fixed return control receives input");
            for (int i = 0; i < 3; i++) yield return null;
            Check(!help.IsOpen && !game.IsGameplayPaused && timer.Remaining < remaining, language + " return resumes the round");
            help.Open();
            yield return null;
            Check(scroll.content.rect.height <= scroll.viewport.rect.height || scroll.verticalNormalizedPosition >= .99f,
                language + " reopened guide starts at the first card");
            var helpRect = (RectTransform)help.transform;
            helpRect.anchorMin = helpRect.anchorMax = new Vector2(.5f, .5f);
            helpRect.sizeDelta = new Vector2(1080f, 1600f);
            for (int i = 0; i < 3; i++) yield return null;
            Canvas.ForceUpdateCanvases();
            Check(scroll.content.rect.height > scroll.viewport.rect.height, language + " shorter available height enables guide scrolling");
            ExecuteEvents.Execute(scroll.gameObject, scrollEvent, ExecuteEvents.scrollHandler);
            for (int i = 0; i < 3; i++) yield return null;
            ClickVisible(help.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "CardFever").gameObject,
                language + " scrolling a shorter guide exposes the last preview");
            Capture("help-compact-" + language.ToLowerInvariant());
            for (int i = 0; i < 3; i++) yield return null;
            help.Close();
            help.Open();
            yield return null;
            Check(scroll.verticalNormalizedPosition >= .99f, language + " compact guide resets its scroll on reopen");
            ClickVisible(help.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "btnHelpDismiss").gameObject,
                language + " header close control receives input");
            Time.timeScale = 0f;
        }
        Managers.Scene.ChangeScene(W01SceneType.Intro);
        for (int i = 0; i < 6; i++) yield return null;
        Check(Errors.Count == 0, "No runtime errors during redesigned help QA");
    }

    private static IEnumerator MusicScenarios()
    {
        for (int i = 0; i < 8; i++) yield return null;
        previousAdsDisabled = Managers.Ads.AdsDisabled;
        Managers.Ads.SetAdsDisabled(true);
        Time.timeScale = 0f;
        var sound = Managers.Sound;
        var voices = GameObject.Find("@SoundRoot").GetComponentsInChildren<AudioSource>();
        var music = voices.Where(s => s.loop).ToArray();
        var effects = voices.Where(s => !s.loop).ToArray();
        Managers.SetBgmEnabled(false);
        Managers.SetEffectEnabled(true);
        sound.Play(Define.Sound.Bgm, "BGM/Game_NeonLobby", .38f);
        double until = EditorApplication.timeSinceStartup + .45;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        var lobby = music.Single(s => s.clip != null && s.clip.name == "Game_NeonLobby");
        Check(music.All(s => s.mute) && effects.All(s => !s.mute), "Music mute stays independent of effects");
        sound.Play(Define.Sound.Bgm, "BGM/Gameplay_NeonRush", .32f);
        until = EditorApplication.timeSinceStartup + .07;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(music.Count(s => s.isPlaying) == 2, "Return request happens while both music streams are crossfading");
        int sampleBefore = lobby.timeSamples;
        float volumeBefore = lobby.volume;
        sound.Play(Define.Sound.Bgm, "BGM/Game_NeonLobby", .38f);
        bool phaseKept = sampleBefore > 0 && lobby.timeSamples >= sampleBefore;
        bool volumeKept = Mathf.Abs(lobby.volume - volumeBefore) < .001f;
        Results.Add($"FADE RETURN samples={sampleBefore}->{lobby.timeSamples} volume={volumeBefore}->{lobby.volume} phaseKept={phaseKept} volumeKept={volumeKept}");
        until = EditorApplication.timeSinceStartup + .4;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(music.Count(s => s.isPlaying) == 1 && lobby.isPlaying && Mathf.Abs(lobby.volume - .38f) < .001f,
            "Interrupted fade settles to one lobby stream at the requested volume");
        sampleBefore = lobby.timeSamples;
        Managers.SetBgmEnabled(true);
        Check(music.All(s => !s.mute) && lobby.timeSamples >= sampleBefore,
            "Unmuting music preserves its advancing playback position");
        Managers.SetEffectEnabled(false);
        sound.Play(Define.Sound.Effect, "SFX/New_Best", .62f);
        Check(effects.All(s => s.mute) && effects.Any(s => s.isPlaying) && music.All(s => !s.mute),
            "New effects inherit effects mute without muting music");
        Managers.SetBgmEnabled(false);
        Managers.Scene.ChangeScene(W01SceneType.Game);
        for (int i = 0; i < 6; i++) yield return null;
        var game = Object.FindFirstObjectByType<GameScene>();
        game.bestScore = 100000;
        Check(!Managers.IsBgmEnabled && !Managers.IsEffectEnabled && voices.All(s => s.mute),
            "Both disabled preferences and voice mutes survive entering Game");
        game.StartRound();
        for (int i = 0; i < 2; i++) yield return null;
        Check(music.Any(s => s.clip != null && s.clip.name == "Gameplay_NeonRush") && voices.All(s => s.mute),
            "Round music and new start effects remain muted");
        sound.Stop(Define.Sound.Bgm);
        until = EditorApplication.timeSinceStartup + .4;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(music.All(s => !s.isPlaying && s.clip == null), "Stopping mid-fade cannot restart an outgoing stream");
        sound.Play(Define.Sound.Bgm, "BGM/Gameplay_NeonRush", .32f);
        until = EditorApplication.timeSinceStartup + .4;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(music.Count(s => s.isPlaying) == 1 && music.All(s => s.mute), "Music can restart after Stop while retaining mute");
        Managers.Scene.ChangeScene(W01SceneType.Intro);
        for (int i = 0; i < 6; i++) yield return null;
        Check(!Managers.IsBgmEnabled && !Managers.IsEffectEnabled && voices.All(s => s.mute),
            "Returning to Intro preserves both disabled settings");
        Check(phaseKept && volumeKept, "Returning to the outgoing music preserves phrase and volume continuity");
        Check(Errors.Count == 0, "No runtime errors during interrupted music and mute QA");
    }

    private static IEnumerator CaptionScenarios()
    {
        for (int i = 0; i < 8; i++) yield return null;
        previousAdsDisabled = Managers.Ads.AdsDisabled;
        Managers.Ads.SetAdsDisabled(true);
        Time.timeScale = 0f;
        bool layoutFits = true;
        bool entryMatches = true;
        bool exitMatches = true;
        foreach (string language in new[] { "English", "Korean" })
        {
            EditorPrefs.SetString("VioletTap.EditorLanguage", language);
            Managers.Scene.ChangeScene(W01SceneType.Game);
            for (int i = 0; i < 6; i++) yield return null;
            var game = Object.FindFirstObjectByType<GameScene>();
            var ui = Object.FindFirstObjectByType<UI_GamePopup>();
            game.bestScore = 100000;
            game.StartRound();
            void InspectCaption(string state)
            {
                var label = ui.GetTextCombo();
                label.ForceMeshUpdate();
                var corners = new Vector3[4];
                label.rectTransform.GetWorldCorners(corners);
                bool inView = corners.All(c => Camera.main.pixelRect.Contains((Vector2)Camera.main.WorldToScreenPoint(c)));
                bool fits = !label.isTextOverflowing && inView;
                layoutFits &= fits;
                Results.Add($"CAPTION {language} {state} text={label.text} fits={fits} lines={label.textInfo.lineCount} rect={label.rectTransform.rect} rendered={label.textBounds}");
            }
            foreach (var type in new[] { TapTargetType.Normal, TapTargetType.TimeBonus })
            {
                Set(game, "m_Combo", 998);
                Set(game, "m_NextFeverCombo", 100000);
                Tap(game, type);
                InspectCaption(type.ToString());
            }
            Set(game, "m_Combo", game.Config.feverCombo - 1);
            Set(game, "m_NextFeverCombo", game.Config.feverCombo);
            int before = Get<int>(game, "m_Score");
            Tap(game, TapTargetType.Normal);
            Check(Get<int>(game, "m_Score") - before == 2 * game.Config.scorePerTap,
                language + " fever entry preserves the triggering hit's pre-fever award");
            InspectCaption("FeverEntry");
            entryMatches &= ui.GetTextCombo().text.Contains("x" + game.Config.feverScoreMultiplier);
            Set(game, "m_Combo", 998);
            before = Get<int>(game, "m_Score");
            Tap(game, TapTargetType.Normal);
            Check(Get<int>(game, "m_Score") - before == game.Config.feverScoreMultiplier * game.Config.scorePerTap,
                language + " subsequent fever hit uses the active score multiplier");
            InspectCaption("FeverLongStreak");
            Capture("caption-fever-" + language.ToLowerInvariant());
            for (int i = 0; i < 3; i++) yield return null;
            Set(game, "m_FeverRemaining", .01f);
            Time.timeScale = 1f;
            while (Get<float>(game, "m_FeverRemaining") > 0f) yield return null;
            Time.timeScale = 0f;
            InspectCaption("FeverExit");
            exitMatches &= !ui.GetTextCombo().text.Contains(GameLocalization.T("FEVER", "피버"));
            var missed = Targets(game)[0];
            missed.Bind(TapTargetType.Normal, 100f, .82f, null, null);
            Invoke(game, "HandleTargetMissed", missed);
            InspectCaption("Miss");
            Capture("caption-miss-" + language.ToLowerInvariant());
            for (int i = 0; i < 3; i++) yield return null;
            Tap(game, TapTargetType.Bomb);
            InspectCaption("Bomb");
        }
        Managers.Scene.ChangeScene(W01SceneType.Intro);
        for (int i = 0; i < 6; i++) yield return null;
        Check(layoutFits, "Gameplay captions fit their rect and viewport in both languages");
        Check(entryMatches, "Fever entry caption reflects the now-active multiplier");
        Check(exitMatches, "Fever expiry removes the stale fever caption");
        Check(Errors.Count == 0, "No runtime errors during gameplay caption QA");
    }

    private static IEnumerator HudScenarios()
    {
        for (int i = 0; i < 8; i++) yield return null;
        previousAdsDisabled = Managers.Ads.AdsDisabled;
        Managers.Ads.SetAdsDisabled(true);
        Time.timeScale = 0f;
        bool numericFits = true;
        bool resultFits = true;
        foreach (string language in new[] { "English", "Korean" })
        {
            EditorPrefs.SetString("VioletTap.EditorLanguage", language);
            PlayerPrefs.SetInt(BestKey, 100000);
            Managers.Scene.ChangeScene(W01SceneType.Game);
            for (int i = 0; i < 6; i++) yield return null;
            var game = Object.FindFirstObjectByType<GameScene>();
            var ui = Object.FindFirstObjectByType<UI_GamePopup>();
            Time.timeScale = 0f;
            Check(ui.GetTextBest().text == "100000", language + " loads the saved best into the HUD");
            foreach (int value in new[] { 0, 191, 999, 9999, 100000 })
            {
                foreach (var label in new[] { ui.GetTextScore(), ui.GetTextBest() })
                {
                    label.text = value.ToString("00");
                    label.ForceMeshUpdate();
                    var card = (RectTransform)label.transform.parent;
                    var renderedMin = card.InverseTransformPoint(label.transform.TransformPoint(label.textBounds.min));
                    var renderedMax = card.InverseTransformPoint(label.transform.TransformPoint(label.textBounds.max));
                    bool fits = !label.isTextOverflowing && label.textInfo.lineCount == 1
                        && label.textInfo.characterInfo.Take(label.textInfo.characterCount).All(c => c.isVisible)
                        && renderedMin.x >= card.rect.xMin + 20f
                        && renderedMax.x <= card.rect.xMax - 20f;
                    Results.Add($"HUD {language} {label.name} value={value} fits={fits} font={label.font.name} size={label.fontSize} color={label.color} rect={label.rectTransform.rect} rendered={label.textBounds}");
                    numericFits &= fits;
                }
            }
            Capture("hud-values-" + language.ToLowerInvariant());
            for (int i = 0; i < 3; i++) yield return null;
            game.StartRound();
            Set(game, "m_Score", 999);
            Tap(game, TapTargetType.Quick);
            Check(ui.GetTextScore().text == "1002", language + " updates the HUD across the four-digit boundary");
            Set(game, "m_MaxCombo", 999);
            Get<GameFlow>(game, "mGameFlow").FinishGame();
            Time.timeScale = 1f;
            double until = EditorApplication.timeSinceStartup + .4;
            while (EditorApplication.timeSinceStartup < until) yield return null;
            var result = ui.GetTextResult();
            result.ForceMeshUpdate();
            Results.Add($"RESULT {language} text={result.text.Replace("\n", " | ")} overflow={result.isTextOverflowing} lines={result.textInfo.lineCount} font={result.fontSize} rect={result.rectTransform.rect} preferred={result.preferredWidth}x{result.preferredHeight}");
            resultFits &= !result.isTextOverflowing && result.textInfo.lineCount == 5;
            Capture("hud-result-" + language.ToLowerInvariant());
            for (int i = 0; i < 3; i++) yield return null;
        }
        Managers.Scene.ChangeScene(W01SceneType.Intro);
        for (int i = 0; i < 6; i++) yield return null;
        Check(numericFits, "Scores and best values through 100000 fit as complete single lines");
        Check(resultFits, "Four-digit results and three-digit streaks fit their panel");
        Check(Errors.Count == 0, "No runtime errors during HUD readability QA");
    }

    private static IEnumerator ModalScenarios()
    {
        for (int i = 0; i < 8; i++) yield return null;
        previousAdsDisabled = Managers.Ads.AdsDisabled;
        Managers.Ads.SetAdsDisabled(true);
        Managers.Scene.ChangeScene(W01SceneType.Game);
        for (int i = 0; i < 6; i++) yield return null;
        var game = Object.FindFirstObjectByType<GameScene>();
        var ui = Object.FindFirstObjectByType<UI_GamePopup>();
        var help = Object.FindFirstObjectByType<UI_GameHelp>();
        var timer = Get<CountdownTimer>(game, "m_Timer");
        game.bestScore = 100000;
        game.StartRound();
        help.Open();
        int confirmations = 0;
        string confirmed = null;
        ui.ShowNicknamePrompt("<b>안녕</b>", value => { confirmations++; confirmed = value; });
        for (int i = 0; i < 3; i++) yield return null;
        var input = ui.GetComponentInChildren<TMP_InputField>();
        float remaining = timer.Remaining;
        float lifetime = Get<float>(Targets(game)[0], "m_RemainingLifetime");
        double until = EditorApplication.timeSinceStartup + .3;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Capture("nickname-during-round");
        for (int i = 0; i < 3; i++) yield return null;
        Check(!help.IsOpen && game.IsGameplayPaused && Mathf.Approximately(remaining, timer.Remaining)
            && Mathf.Approximately(lifetime, Get<float>(Targets(game)[0], "m_RemainingLifetime")),
            "Nickname replacing help keeps round and target timers paused");
        input.ForceLabelUpdate();
        input.textComponent.ForceMeshUpdate();
        Results.Add("Nickname parsed codepoints: " + string.Join(" ", input.textComponent.GetParsedText().Select(c => ((int)c).ToString("X4"))));
        Check(input.textComponent.GetParsedText().TrimEnd('\u200B') == "<b>안녕</b>", "Nickname input displays markup as literal text");
        var modal = input.GetComponentInParent<Canvas>().transform;
        foreach (var button in ui.GetComponentsInChildren<UnityEngine.UI.Button>())
        {
            if (button.transform.IsChildOf(modal)) continue;
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(EventSystem.current)
                { position = RectTransformUtility.WorldToScreenPoint(Camera.main, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Check(hits.Count > 0 && hits[0].gameObject.transform.IsChildOf(modal), "Nickname blocks underlying " + button.name);
        }
        int score = Get<int>(game, "m_Score");
        Targets(game)[0].OnPointerDown(null);
        Check(Get<int>(game, "m_Score") == score, "Nickname modal rejects direct target contacts");
        Invoke(game, "OnApplicationPause", true);
        ClickVisible(ui.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "btnNicknameConfirm").gameObject,
            "Nickname confirmation receives input");
        Invoke(ui, "ConfirmNickname");
        Check(confirmations == 1 && confirmed == "<b>안녕</b>" && !input.gameObject.activeInHierarchy,
            "Nickname callback is literal and runs exactly once");
        Check(game.IsGameplayPaused, "Confirming nickname preserves the independent application pause");
        Invoke(game, "OnApplicationPause", false);
        remaining = timer.Remaining;
        until = EditorApplication.timeSinceStartup + .15;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(!game.IsGameplayPaused && timer.Remaining < remaining, "Round resumes after all modal and app pauses clear");
        var flow = Get<GameFlow>(game, "mGameFlow");
        flow.FinishGame();
        ui.ShowNicknamePrompt("RESULT", null);
        game.RetryRound();
        Check(flow.State == GameFlowState.Result, "Open nickname prevents restarting behind the result modal");
        Invoke(ui, "ConfirmNickname");
        game.RetryRound();
        ui.ShowNicknamePrompt("READY", null);
        game.StartRound();
        Check(flow.State == GameFlowState.Ready, "Open nickname prevents starting a round behind the modal");
        Invoke(ui, "ConfirmNickname");
        game.StartRound();
        ui.ShowNicknamePrompt("STALE", _ => confirmations++);
        ui.gameObject.SetActive(false);
        for (int i = 0; i < 2; i++) yield return null;
        Check(!game.IsGameplayPaused && Get<Action<string>>(ui, "mNicknameConfirmed") == null,
            "Disabling game UI releases nickname pause and callback");
        ui.ShowNicknamePrompt("INACTIVE", _ => confirmations++);
        Check(!game.IsGameplayPaused && Get<Action<string>>(ui, "mNicknameConfirmed") == null,
            "Inactive UI rejects a delayed record prompt");
        ui.gameObject.SetActive(true);
        for (int i = 0; i < 2; i++) yield return null;
        Check(!input.gameObject.activeInHierarchy, "Re-enabling UI does not resurrect a stale record prompt");
        ui.ShowNicknamePrompt("FRESH", _ => confirmations++);
        for (int i = 0; i < 3; i++) yield return null;
        Check(input.isFocused && input.text == "FRESH" && game.IsGameplayPaused, "Fresh prompt can reopen after UI lifecycle change");
        Managers.Scene.ChangeScene(W01SceneType.Intro);
        for (int i = 0; i < 6; i++) yield return null;
        Check(Errors.Count == 0, "No runtime errors in nickname modal lifecycle QA");
    }

    private static IEnumerator RoundScenarios()
    {
        for (int i = 0; i < 8; i++) yield return null;
        previousAdsDisabled = Managers.Ads.AdsDisabled;
        Managers.Ads.SetAdsDisabled(true);
        PlayerPrefs.SetInt(BestKey, 100000);
        Managers.Scene.ChangeScene(W01SceneType.Game);
        for (int i = 0; i < 6; i++) yield return null;
        Time.timeScale = 0f;
        var game = Object.FindFirstObjectByType<GameScene>();
        var flow = Get<GameFlow>(game, "mGameFlow");
        var timer = Get<CountdownTimer>(game, "m_Timer");
        var pool = Get<SWGUnity2DCore.Pool.GameObjectPool>(game, "mTargetPool");
        for (int round = 0; round < 30; round++)
        {
            game.StartRound();
            game.StartRound();
            game.RetryRound();
            Check(flow.State == GameFlowState.Playing && Targets(game).Count == game.Config.initialTargetCount
                && Get<int>(game, "m_Score") == 0 && Mathf.Approximately(timer.Remaining, game.Config.roundDuration),
                "Round " + round + " begins once with clean score, time and targets");
            for (int contact = 0; contact < 12; contact++)
            {
                var target = Targets(game)[0];
                Physics2D.SyncTransforms();
                Canvas.ForceUpdateCanvases();
                var oldPosition = target.transform.position;
                var pointer = new PointerEventData(EventSystem.current)
                {
                    position = Camera.main.WorldToScreenPoint(oldPosition),
                    button = PointerEventData.InputButton.Left
                };
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                Check(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerDownHandler>(hits[0].gameObject) == target.gameObject,
                    "Round " + round + " contact " + contact + " reaches its visible target");
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerDownHandler);
                hits.Clear();
                EventSystem.current.RaycastAll(pointer, hits);
                var next = hits.Count == 0 ? null : hits[0].gameObject.GetComponent<CircleTarget>();
                if (next != null)
                {
                    var collider = next.GetComponent<CircleCollider2D>();
                    float distance = Vector2.Distance(next.transform.InverseTransformPoint(oldPosition), collider.offset);
                    Results.Add($"CONTACT REQUERY round={round} tap={contact} localDistance={distance} radius={collider.radius}");
                    Check(distance <= collider.radius + .01f, "Recycled contact never remains clickable at its previous position");
                }
                yield return null;
            }
            Check(Get<int>(game, "m_Hits") == 12 && Targets(game).Distinct().Count() == Targets(game).Count,
                "Round " + round + " counts each contact once and has unique targets");
            var stale = Targets(game)[0];
            if (round % 2 == 0) timer.Tick(1000f);
            else
            {
                timer.AddTime(1f - timer.Remaining);
                Tap(game, TapTargetType.Bomb);
            }
            int score = Get<int>(game, "m_Score");
            stale.OnPointerDown(null);
            flow.FinishGame();
            Check(flow.State == GameFlowState.Result && !timer.IsRunning && Targets(game).Count == 0
                && Get<int>(game, "m_Score") == score && Get<float>(game, "m_FeverRemaining") == 0f,
                "Round " + round + " ends once and rejects stale contacts");
            Check(Get<ICollection>(pool, "m_Instances").Count <= game.Config.feverTargetCount + 1,
                "Round " + round + " target pool remains bounded");
            game.RetryRound();
            game.RetryRound();
            Check(flow.State == GameFlowState.Ready && Targets(game).Count == 0,
                "Round " + round + " repeated retry stays ready");
            yield return null;
        }
        var sources = GameObject.Find("@SoundRoot").GetComponentsInChildren<AudioSource>();
        Check(sources.Length == 14, "Repeated rounds retain the fixed audio voice pool");
        Managers.Scene.ChangeScene(W01SceneType.Intro);
        for (int i = 0; i < 6; i++) yield return null;
        Check(Errors.Count == 0, "No runtime errors during repeated round QA");
    }

    private static IEnumerator RankingScenarios()
    {
        for (int i = 0; i < 8; i++) yield return null;
        foreach (string language in new[] { "Korean", "English" })
        {
            EditorPrefs.SetString("VioletTap.EditorLanguage", language);
            Managers.Scene.ChangeScene(W01SceneType.Intro);
            for (int i = 0; i < 6; i++) yield return null;
            // Disable Start on the popup before the next frame to keep these rows entirely local.
            var panel = Object.Instantiate(Resources.Load<GameObject>("Prefabs/UI/Popup/UI_Rankpopup"));
            panel.GetComponent<UI_RankPopup>().enabled = false;
            Managers.UI.SetCanvas(panel);
            GameLocalization.ApplyFont(panel.transform);
            typeof(UI_RankPopup).Assembly.GetType("_01.Scripts.UI.Popup.PopupPresentation")
                .GetMethod("Prepare").Invoke(null, new object[] { panel.transform });
            panel.GetComponentsInChildren<TMP_Text>().Single(t => t.name == "txtTitle").text = GameLocalization.T("RANKING", "랭킹");
            panel.GetComponentsInChildren<TMP_Text>().Single(t => t.name == "txtClose").text = GameLocalization.T("CLOSE", "닫기");
            var scroll = panel.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            var rows = new List<UI_RankingItem>();
            string[] names = { "망펭", new string('가', 50), new string('W', 50), "<b>Hi</b>", "Donut", "바이올렛탭", "Soso", "Neon", "Tap", "LastPlayer" };
            for (int i = 0; i < names.Length; i++)
            {
                var row = Managers.UI.MakeSubItem<UI_RankingItem>(scroll.content, "itemRanking");
                row.Initialize();
                row.SetProfile(i + 1, names[i], i == 0 ? 2147483647d : 10000 - i, i == 1);
                rows.Add(row);
            }
            for (int i = 0; i < 4; i++) yield return null;
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;
            Capture("ranking-rows-" + language.ToLowerInvariant());
            for (int i = 0; i < 3; i++) yield return null;
            foreach (var row in rows)
            {
                foreach (var label in row.GetComponentsInChildren<TMP_Text>())
                {
                    label.ForceMeshUpdate();
                    var rect = label.rectTransform.rect;
                    bool contained = label.textInfo.characterInfo.Take(label.textInfo.characterCount).Any(c => c.isVisible)
                        && label.textInfo.characterInfo.Take(label.textInfo.characterCount).Where(c => c.isVisible)
                        .All(c => c.bottomLeft.x >= rect.xMin - 1f && c.topRight.x <= rect.xMax + 1f
                            && c.bottomLeft.y >= rect.yMin - 1f && c.topRight.y <= rect.yMax + 1f);
                    Check(contained, language + " visible glyphs stay within " + label.name + ": " + label.text);
                    if (label.name == "txtRank") Check(label.font.HasCharacters(label.text), language + " ranking font includes all rank characters");
                    if (label.name == "txtScore") Check(!label.isTextTruncated, language + " complete score remains visible: " + label.text);
                }
            }
            var literal = rows[3].GetComponentsInChildren<TMP_Text>().Single(t => t.name == "txtNicName");
            Check(literal.GetParsedText() == names[3], language + " nickname markup displays literally");
            var selfName = rows[1].GetComponentsInChildren<TMP_Text>().Single(t => t.name == "txtNicName");
            Check(selfName.GetParsedText().StartsWith(GameLocalization.T("[YOU]", "[나]")), language + " current-player marker survives long nickname truncation");
            var wrappedName = rows[4].GetComponentsInChildren<TMP_Text>().Single(t => t.name == "txtNicName");
            rows[4].SetProfile(5, "Line\nBreak\tName", 9996);
            wrappedName.ForceMeshUpdate();
            Check(wrappedName.textInfo.lineCount == 1, language + " nickname control characters cannot create extra rows");
            rows[4].SetProfile(5, names[4], 9996);
            var viewport = scroll.viewport;
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(Camera.main, viewport.TransformPoint(viewport.rect.center)),
                scrollDelta = new Vector2(0f, -4000f)
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Check(hits.Count > 0 && ExecuteEvents.GetEventHandler<IScrollHandler>(hits[0].gameObject) == scroll.gameObject,
                language + " ranking rows route pointer scrolling to the list");
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.scrollHandler);
            for (int i = 0; i < 4; i++) yield return null;
            var lastName = rows[9].GetComponentsInChildren<TMP_Text>().Single(t => t.name == "txtNicName");
            var lastCenter = lastName.rectTransform.TransformPoint(lastName.rectTransform.rect.center);
            Check(scroll.verticalNormalizedPosition < .01f && RectTransformUtility.RectangleContainsScreenPoint(viewport,
                RectTransformUtility.WorldToScreenPoint(Camera.main, lastCenter), Camera.main), language + " scrolling reaches the last ranking row");
            Capture("ranking-bottom-" + language.ToLowerInvariant());
            for (int i = 0; i < 3; i++) yield return null;
            Object.Destroy(panel);
            for (int i = 0; i < 3; i++) yield return null;
        }
        Check(Errors.Count == 0, "No runtime errors during local ranking row QA");
    }

    private static IEnumerator PopupScenarios()
    {
        for (int i = 0; i < 8; i++) yield return null;
        foreach (string language in new[] { "Korean", "English" })
        {
            EditorPrefs.SetString("VioletTap.EditorLanguage", language);
            Managers.Scene.ChangeScene(W01SceneType.Intro);
            for (int i = 0; i < 6; i++) yield return null;
            var intro = Managers.UI.FindPopup<UI_IntroPopup>();
            Managers.SetBgmEnabled(true);
            Managers.SetEffectEnabled(true);
            var soundPopup = Managers.UI.ShowPopupUI<UI_SoundPopup>();
            for (int i = 0; i < 4; i++) yield return null;
            CheckPopupBlocksIntro(soundPopup, intro, language + " sound");
            foreach (var label in soundPopup.GetComponentsInChildren<TMP_Text>())
            {
                label.ForceMeshUpdate();
                Check(!label.isTextOverflowing, language + " sound text fits: " + label.name);
            }
            Capture("sound-" + language.ToLowerInvariant());
            for (int i = 0; i < 3; i++) yield return null;
            var buttons = soundPopup.GetComponentsInChildren<UnityEngine.UI.Button>();
            ClickVisible(buttons.Single(b => b.name == "btnBgm").gameObject, language + " music toggle receives input");
            Check(!Managers.IsBgmEnabled && Managers.Sound.IsMuted(Define.Sound.Bgm), language + " music OFF persists and mutes playback");
            ClickVisible(buttons.Single(b => b.name == "btnEffect").gameObject, language + " effects toggle receives input");
            Check(!Managers.IsEffectEnabled && Managers.Sound.IsMuted(Define.Sound.Effect), language + " effects OFF persists and mutes playback");
            ClickVisible(buttons.Single(b => b.name == "btnClose").gameObject, language + " sound close receives input");
            for (int i = 0; i < 3; i++) yield return null;
            Check(soundPopup == null && Managers.UI.PeekPopupUI<UI_IntroPopup>() == intro,
                language + " sound close returns to the original intro");
            soundPopup = Managers.UI.ShowPopupUI<UI_SoundPopup>();
            for (int i = 0; i < 4; i++) yield return null;
            Check(soundPopup.GetComponentsInChildren<TMP_Text>().Count(t => t.text.Contains("OFF")) == 2,
                language + " reopened settings show both saved OFF states");
            buttons = soundPopup.GetComponentsInChildren<UnityEngine.UI.Button>();
            ClickVisible(buttons.Single(b => b.name == "btnBgm").gameObject, language + " music can be re-enabled");
            ClickVisible(buttons.Single(b => b.name == "btnEffect").gameObject, language + " effects can be re-enabled");
            Check(Managers.IsBgmEnabled && Managers.IsEffectEnabled, language + " both audio settings return ON");
            ClickVisible(buttons.Single(b => b.name == "btnClose").gameObject, language + " reopened sound closes");
            for (int i = 0; i < 3; i++) yield return null;

            var ranking = Managers.UI.ShowPopupUI<UI_RankPopup>();
            for (int i = 0; i < 6; i++) yield return null;
            CheckPopupBlocksIntro(ranking, intro, language + " ranking");
            var title = Get<TextMeshProUGUI>(ranking, "m_Title");
            string[] states = language == "Korean"
                ? new[] { "랭킹", "랭킹 불러오는 중...", "랭킹을 불러올 수 없습니다", "아직 등록된 기록이 없습니다" }
                : new[] { "RANKING", "LOADING RANKING...", "RANKING UNAVAILABLE", "NO SCORES YET" };
            foreach (string state in states)
            {
                Invoke(ranking, "SetTitle", state);
                title.ForceMeshUpdate();
                Check(!title.isTextOverflowing && title.textInfo.lineCount == 1,
                    language + " ranking state fits on one line: " + state);
            }
            Capture("ranking-empty-" + language.ToLowerInvariant());
            for (int i = 0; i < 3; i++) yield return null;
            ClickVisible(ranking.GetComponentInChildren<UnityEngine.UI.Button>().gameObject, language + " ranking close receives input");
            for (int i = 0; i < 3; i++) yield return null;
            Check(ranking == null && Managers.UI.PeekPopupUI<UI_IntroPopup>() == intro,
                language + " ranking close restores the intro popup");
        }
        Check(Errors.Count == 0, "No runtime errors during settings and ranking popup QA");
    }

    private static void CheckPopupBlocksIntro(UI_Popup popup, UI_IntroPopup intro, string description)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var button in intro.GetComponentsInChildren<UnityEngine.UI.Button>())
        {
            var hits = new List<RaycastResult>();
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(Camera.main, rect.TransformPoint(rect.rect.center))
            };
            EventSystem.current.RaycastAll(pointer, hits);
            Check(hits.Count > 0 && hits[0].gameObject.transform.IsChildOf(popup.transform),
                description + " blocks underlying " + button.name);
        }
    }

    private static IEnumerator SceneCycleScenarios()
    {
        for (int i = 0; i < 8; i++) yield return null;
        Time.timeScale = 0f;
        for (int cycle = 0; cycle < 65; cycle++)
        {
            Managers.Scene.ChangeScene(W01SceneType.Game);
            for (int i = 0; i < 6; i++) yield return null;
            var game = Object.FindFirstObjectByType<GameScene>();
            var ui = Object.FindFirstObjectByType<UI_GamePopup>();
            game.bestScore = 100000;
            game.StartRound();
            var target = Targets(game)[0];
            Physics2D.SyncTransforms();
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = Camera.main.WorldToScreenPoint(target.transform.position),
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Results.Add($"CYCLE {cycle} popupOrder={ui.GetComponent<Canvas>().sortingOrder} stack={Get<ICollection>(Managers.UI, "m_PopupStack").Count} hits="
                + string.Join(" > ", hits.Take(4).Select(hit => hit.gameObject.name + "@" + hit.sortingOrder)));
            Check(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerDownHandler>(hits[0].gameObject) == target.gameObject,
                "Target receives the top pointer hit after scene cycle " + cycle);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Check(Get<int>(game, "m_Score") > 0, "Routed target contact scores after scene cycle " + cycle);
            Invoke(game, "StartFever");
            var help = Object.FindFirstObjectByType<UI_GameHelp>();
            help.Open();
            Managers.Scene.ChangeScene(W01SceneType.Intro);
            for (int i = 0; i < 6; i++) yield return null;
            Check(Object.FindObjectsByType<GameScene>(FindObjectsSortMode.None).Length == 0
                && Object.FindObjectsByType<TapFeedback>(FindObjectsSortMode.None).Length == 0
                && Object.FindObjectsByType<UI_GameHelp>(FindObjectsSortMode.None).Length == 0,
                "Leaving an open guide releases game and touch objects " + cycle);
            Check(Resources.FindObjectsOfTypeAll<Texture2D>().Count(t => t.name == "PrototypeTargetCircle") == 0,
                "Procedural target texture is released after scene cycle " + cycle);
            var roots = Object.FindObjectsByType<SoundPlayback>(FindObjectsSortMode.None);
            Check(roots.Length == 1 && roots[0].GetComponentsInChildren<AudioSource>().Length == 14,
                "Scene cycling keeps one bounded audio voice pool " + cycle);
        }
        Check(Get<ICollection>(Managers.UI, "m_PopupStack").Count == 1, "Popup registry contains only the current intro");
        double until = EditorApplication.timeSinceStartup + .7;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        var sources = GameObject.Find("@SoundRoot").GetComponentsInChildren<AudioSource>();
        Check(sources.Count(s => s.loop && s.isPlaying) == 1
            && sources.Single(s => s.loop && s.isPlaying).clip.name == "Intro_NeonAwakening",
            "Final scene has one intro music stream after crossfade");
        Check(Errors.Count == 0, "No runtime errors during repeated scene transitions");
    }

    private static IEnumerator BackgroundScenarios()
    {
        for (int i = 0; i < 8; i++) yield return null;
        Time.timeScale = 0f;
        Managers.Scene.ChangeScene(W01SceneType.Game);
        for (int i = 0; i < 8; i++) yield return null;
        var game = Object.FindFirstObjectByType<GameScene>();
        var help = Object.FindFirstObjectByType<UI_GameHelp>();
        game.bestScore = 100000;
        game.StartRound();
        Invoke(game, "StartFever");
        var timer = Get<CountdownTimer>(game, "m_Timer");
        var target = Targets(game)[0];
        target.Bind(TapTargetType.Normal, 100f, .82f, t => Invoke(game, "HandleTargetTapped", t), t => Invoke(game, "HandleTargetMissed", t));
        var feedback = game.GetComponentInChildren<TapFeedback>();
        feedback.Show(target, true);
        var burst = feedback.transform.Find("Tap Burst 0");
        float remaining = timer.Remaining;
        float fever = Get<float>(game, "m_FeverRemaining");
        float life = Get<float>(target, "m_RemainingLifetime");
        int score = Get<int>(game, "m_Score");
        game.SendMessage("OnApplicationPause", true, SendMessageOptions.DontRequireReceiver);
        Time.timeScale = 1f;
        double until = EditorApplication.timeSinceStartup + .7;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(Mathf.Approximately(timer.Remaining, remaining), "Application pause freezes round time");
        Check(Mathf.Approximately(Get<float>(game, "m_FeverRemaining"), fever), "Application pause freezes fever time");
        Check(Mathf.Approximately(Get<float>(target, "m_RemainingLifetime"), life), "Application pause freezes target lifetime");
        Check(burst.gameObject.activeSelf, "Application pause preserves an active touch burst");
        target.OnPointerDown(null);
        Check(Get<int>(game, "m_Score") == score && Get<Action<CircleTarget>>(target, "m_OnTapped") != null,
            "Background contacts cannot score or consume the resumed contact");
        Check(Time.timeScale == 1f, "Application pause leaves global time scale unchanged");

        help.Open();
        help.Close();
        for (int i = 0; i < 4; i++) yield return null;
        Check(Mathf.Approximately(timer.Remaining, remaining) && Get<bool>(target, "m_Paused"),
            "Closing help cannot override an active application pause");
        help.Open();
        game.SendMessage("OnApplicationPause", false, SendMessageOptions.DontRequireReceiver);
        for (int i = 0; i < 4; i++) yield return null;
        Check(help.IsOpen && Mathf.Approximately(timer.Remaining, remaining) && Get<bool>(target, "m_Paused"),
            "Application resume cannot override an open help panel");
        help.Close();
        for (int i = 0; i < 4; i++) yield return null;
        Check(timer.Remaining < remaining && Get<float>(target, "m_RemainingLifetime") < life,
            "Timer and targets resume after every pause reason clears");
        target.OnPointerDown(null);
        Check(Get<int>(game, "m_Score") > score, "Preserved target accepts its first contact after resume");

        for (int cycle = 0; cycle < 6; cycle++)
        {
            remaining = timer.Remaining;
            game.SendMessage("OnApplicationPause", true, SendMessageOptions.DontRequireReceiver);
            game.SendMessage("OnApplicationPause", true, SendMessageOptions.DontRequireReceiver);
            for (int i = 0; i < 3; i++) yield return null;
            Check(Mathf.Approximately(timer.Remaining, remaining), "Repeated application pause is stable " + cycle);
            game.SendMessage("OnApplicationPause", false, SendMessageOptions.DontRequireReceiver);
            for (int i = 0; i < 3; i++) yield return null;
            Check(timer.Remaining < remaining, "Repeated application resume progresses " + cycle);
        }
        Time.timeScale = 0f;
        Invoke(game, "HandleTimerCompleted");
        game.SendMessage("OnApplicationPause", true, SendMessageOptions.DontRequireReceiver);
        game.RetryRound();
        Check(Get<GameFlow>(game, "mGameFlow").State == GameFlowState.Result, "Background retry is rejected");
        game.SendMessage("OnApplicationPause", false, SendMessageOptions.DontRequireReceiver);
        game.RetryRound();
        game.SendMessage("OnApplicationPause", true, SendMessageOptions.DontRequireReceiver);
        game.StartRound();
        Check(Get<GameFlow>(game, "mGameFlow").State == GameFlowState.Ready, "Background round start is rejected");
        game.SendMessage("OnApplicationPause", false, SendMessageOptions.DontRequireReceiver);
        game.StartRound();
        Check(Get<GameFlow>(game, "mGameFlow").State == GameFlowState.Playing, "Ready round starts after foreground resume");
        Managers.Scene.ChangeScene(W01SceneType.Intro);
        for (int i = 0; i < 5; i++) yield return null;
        Check(Errors.Count == 0, "No runtime errors during pause/resume QA");
    }

    private static IEnumerator StatusScenarios()
    {
        for (int i = 0; i < 8; i++) yield return null;
        Time.timeScale = 0f;
        Managers.Scene.ChangeScene(W01SceneType.Game);
        for (int i = 0; i < 8; i++) yield return null;
        var game = Object.FindFirstObjectByType<GameScene>();
        var ui = Object.FindFirstObjectByType<UI_GamePopup>();
        var help = Object.FindFirstObjectByType<UI_GameHelp>();
        game.bestScore = 100000;
        var status = ui.GetTextStatus();
        string ready = status.text;
        Color readyColor = status.color;
        Invoke(ui, "ShowTransientStatus", "FIRST TEST NOTICE");
        Invoke(ui, "ShowTransientStatus", "SECOND TEST NOTICE");
        Check(status.text == "SECOND TEST NOTICE", "Newest transient notice is displayed immediately");
        double until = EditorApplication.timeSinceStartup + 2.8;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(status.text == ready && status.color == readyColor, "Overlapping notices restore READY instead of a stale notice");

        string playing = GameLocalization.T("TAP THE GLOWING TARGETS", "빛나는 타겟을 터치하세요");
        Invoke(ui, "ShowTransientStatus", "START TEST NOTICE");
        game.StartRound();
        until = EditorApplication.timeSinceStartup + 2.8;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(status.text == playing && status.color == Color.white, "Notice expiry preserves the new PLAYING state");

        Invoke(ui, "ShowTransientStatus", "FEVER TEST NOTICE");
        Invoke(game, "StartFever");
        until = EditorApplication.timeSinceStartup + 2.8;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(status.text == GameLocalization.T("FEVER MODE! KEEP TAPPING!", "피버 모드! 계속 터치하세요!")
            && status.color == new Color(.95f, .65f, 1f), "Notice expiry preserves FEVER guidance and color");
        Invoke(ui, "ShowTransientStatus", "FEVER END TEST NOTICE");
        Invoke(game, "EndFever", false);
        until = EditorApplication.timeSinceStartup + 2.8;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(status.text == playing && status.color == Color.white, "Notice expiry preserves the end of FEVER");

        Invoke(ui, "ShowTransientStatus", "RESULT TEST NOTICE");
        Invoke(game, "HandleTimerCompleted");
        until = EditorApplication.timeSinceStartup + 2.8;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(status.text == GameLocalization.T("ROUND COMPLETE", "게임 종료")
            && status.color == new Color(1f, .78f, .38f), "Notice expiry preserves RESULT guidance and color");
        Invoke(ui, "ShowTransientStatus", "RETRY TEST NOTICE");
        game.RetryRound();
        until = EditorApplication.timeSinceStartup + 2.8;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(status.text == ready && status.color == readyColor, "Notice expiry preserves READY after retry");
        game.StartRound();

        bool effects = Managers.IsEffectEnabled;
        bool haptics = TapHaptics.IsEnabled;
        var timer = Get<CountdownTimer>(game, "m_Timer");
        float remaining = timer.Remaining;
        for (int i = 0; i < 10; i++)
        {
            help.Open();
            help.Open();
            Check(help.IsOpen && game.IsHelpOpen, "Repeated help open pauses round " + i);
            help.Close();
            help.Close();
            Check(!help.IsOpen && !game.IsHelpOpen, "Repeated help close resumes round " + i);
            yield return null;
        }
        Check(Time.timeScale == 0f && timer.Remaining == remaining, "Help cycles preserve global time scale and round time");
        Check(Managers.IsEffectEnabled == effects && TapHaptics.IsEnabled == haptics, "Help cycles preserve sound and vibration preferences");
        Check(help.GetComponentsInChildren<Canvas>(true).Length == 1, "Help cycles reuse the existing canvas");
        help.Open();
        help.gameObject.SetActive(false);
        Check(!game.IsHelpOpen, "Disabling help releases round pause");
        help.Open();
        Check(!help.IsOpen && !game.IsHelpOpen, "Inactive help rejects reopening");
        help.gameObject.SetActive(true);
        Check(!help.IsOpen && !game.IsHelpOpen, "Re-enabled help cannot leave a visible unpaused modal");
        help.Open();
        Check(help.IsOpen && game.IsHelpOpen, "Help opens normally after re-enable");
        help.Close();

        Invoke(ui, "ShowTransientStatus", "DISABLE TEST NOTICE");
        ui.gameObject.SetActive(false);
        ui.gameObject.SetActive(true);
        Check(status.text == playing, "Disabled popup clears its transient notice");
        Invoke(ui, "ShowTransientStatus", "REOPEN TEST NOTICE");
        until = EditorApplication.timeSinceStartup + 2.8;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(status.text == playing, "Re-enabled popup can display and clear a new notice");
        Check(Errors.Count == 0, "No runtime errors in status and help lifecycle checks");
    }

    private static IEnumerator FeedbackScenarios()
    {
        for (int i = 0; i < 8; i++) yield return null;
        Results.Add($"INITIAL AUDIO effectsEnabled={Managers.IsEffectEnabled}, musicEnabled={Managers.IsBgmEnabled}, listenerVolume={AudioListener.volume}, listenerPause={AudioListener.pause}");
        Time.timeScale = 0f;
        EditorPrefs.SetString("VioletTap.EditorLanguage", "Korean");
        Managers.Scene.ChangeScene(W01SceneType.Game);
        for (int i = 0; i < 8; i++) yield return null;
        var game = Object.FindFirstObjectByType<GameScene>();
        var help = Object.FindFirstObjectByType<UI_GameHelp>();
        var sound = Managers.Sound;
        sound.SetMuted(Define.Sound.Bgm, true);
        sound.SetMuted(Define.Sound.Effect, false);
        game.bestScore = 100000;
        game.StartRound();
        Set(game, "m_NextFeverCombo", 1000);
        var audioRoot = GameObject.Find("@SoundRoot");
        foreach (var source in audioRoot.GetComponentsInChildren<AudioSource>().Where(s => !s.loop))
            source.gameObject.AddComponent<VioletTapAudioProbe>();
        foreach (bool fever in new[] { false, true })
        {
            Set(game, "m_FeverRemaining", fever ? 5f : 0f);
            foreach (TapTargetType type in Enum.GetValues(typeof(TapTargetType)))
            {
                sound.Stop(Define.Sound.Effect);
                foreach (var probe in audioRoot.GetComponentsInChildren<VioletTapAudioProbe>()) probe.Peak = 0f;
                Tap(game, type);
                string expected = TapFeedback.Cue(type, fever).Substring(4);
                var voice = audioRoot.GetComponentsInChildren<AudioSource>().Single(s => s.clip != null && s.clip.name == expected);
                Check(voice.isPlaying && !voice.mute && voice.volume >= .6f && voice.clip.loadState == AudioDataLoadState.Loaded,
                    "Contact starts loaded audible cue: " + expected);
                var audioProbe = voice.GetComponent<VioletTapAudioProbe>();
                Check(audioProbe != null, "Audio-thread probe is attached to " + expected);
                double end = EditorApplication.timeSinceStartup + .4;
                while (EditorApplication.timeSinceStartup < end)
                {
                    yield return null;
                    if (audioProbe.Peak > .001f) break;
                }
                float peak = audioProbe.Peak;
                Results.Add("SOURCE AUDIO " + expected + " peak=" + peak.ToString("0.00000"));
                Check(peak > .001f, "Audio samples are produced for " + expected);
            }
        }
        Capture("tap-impact");
        var feedback = game.GetComponentInChildren<TapFeedback>();
        Check(feedback.GetComponentsInChildren<SpriteRenderer>(true).Length == 70, "Tap bursts reuse a fixed pool");

        var helpButton = help.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "btnHelp");
        ClickVisible(helpButton.gameObject, "Help button is reachable above gameplay raycasts");
        Check(help.IsOpen && game.IsHelpOpen, "Help opens and pauses the round");
        Time.timeScale = 1f;
        var timer = Get<CountdownTimer>(game, "m_Timer");
        float remaining = timer.Remaining;
        float feverRemaining = Get<float>(game, "m_FeverRemaining");
        var target = Targets(game)[0];
        float life = Get<float>(target, "m_RemainingLifetime");
        int score = Get<int>(game, "m_Score");
        double waitUntil = EditorApplication.timeSinceStartup + .65;
        while (EditorApplication.timeSinceStartup < waitUntil) yield return null;
        target.OnPointerDown(null);
        Check(Mathf.Approximately(timer.Remaining, remaining) && Mathf.Approximately(Get<float>(game, "m_FeverRemaining"), feverRemaining),
            "Help freezes round time and fever time");
        Check(Mathf.Approximately(Get<float>(target, "m_RemainingLifetime"), life) && Get<int>(game, "m_Score") == score,
            "Help freezes target lifetime and blocks scoring through the overlay");
        foreach (var label in help.GetComponentsInChildren<TMP_Text>())
        {
            label.ForceMeshUpdate();
            Check(!label.isTextOverflowing, "Korean help label fits: " + label.transform.parent.name + "/" + label.name);
        }
        Capture("help-korean");
        for (int i = 0; i < 3; i++) yield return null;
        var buttons = help.GetComponentsInChildren<UnityEngine.UI.Button>();
        var card = buttons.Single(b => b.name == "CardFever");
        ClickVisible(card.gameObject, "Fever card preview accepts a click");
        Check(Get<int>(game, "m_Score") == score, "Help preview never changes score");
        var fxButton = buttons.Single(b => b.name == "btnHelpEffects");
        Managers.SetEffectEnabled(true);
        fxButton.onClick.Invoke();
        Check(!Managers.IsEffectEnabled && sound.IsMuted(Define.Sound.Effect), "Help SOUND OFF mutes effects");
        fxButton.onClick.Invoke();
        Check(Managers.IsEffectEnabled && !sound.IsMuted(Define.Sound.Effect), "Help SOUND ON restores effects");
        var hapticButton = buttons.Single(b => b.name == "btnHelpHaptics");
        TapHaptics.SetEnabled(true);
        hapticButton.onClick.Invoke();
        Check(!TapHaptics.IsEnabled, "Help vibration preference can be disabled");
        hapticButton.onClick.Invoke();
        Check(TapHaptics.IsEnabled, "Help vibration preference can be enabled");
        ClickVisible(buttons.Single(b => b.name == "btnHelpClose").gameObject, "Help close button is reachable");
        Check(!help.IsOpen && !game.IsHelpOpen, "Closing help resumes the round");
        for (int i = 0; i < 4; i++) yield return null;
        Check(timer.Remaining < remaining && Get<float>(target, "m_RemainingLifetime") < life, "Timer and targets continue from their paused values");

        Time.timeScale = 0f;
        EditorPrefs.SetString("VioletTap.EditorLanguage", "English");
        Managers.Scene.ChangeScene(W01SceneType.Game);
        for (int i = 0; i < 8; i++) yield return null;
        help = Object.FindFirstObjectByType<UI_GameHelp>();
        help.Open();
        for (int i = 0; i < 3; i++) yield return null;
        foreach (var label in help.GetComponentsInChildren<TMP_Text>())
        {
            label.ForceMeshUpdate();
            Check(!label.isTextOverflowing, "English help label fits: " + label.transform.parent.name + "/" + label.name);
        }
        Capture("help-english");
        for (int i = 0; i < 3; i++) yield return null;
        EditorPrefs.SetString("VioletTap.EditorLanguage", SessionState.GetString("VioletTap.QA.Language", "System"));
        if (File.Exists(Path.Combine(Root, "KEEP_FEEDBACK_PREVIEW")))
        {
            Managers.Scene.ChangeScene(W01SceneType.Game);
            for (int i = 0; i < 8; i++) yield return null;
            Object.FindFirstObjectByType<UI_GameHelp>().Open();
        }
        Check(Errors.Count == 0, "No runtime errors in feedback/help checks");
    }

    private static void ClickVisible(GameObject target, string description)
    {
        Canvas.ForceUpdateCanvases();
        var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(Camera.main, target.transform.position) };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Check(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == target, description);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    private static IEnumerator NicknameScenarios()
    {
        for (int i = 0; i < 8; i++) yield return null;
        Managers.Scene.ChangeScene(W01SceneType.Game);
        for (int i = 0; i < 6; i++) yield return null;
        var ui = Object.FindFirstObjectByType<UI_GamePopup>();
        string confirmed = null;
        int confirmations = 0;
        ui.ShowNicknamePrompt("NONAME", value => { confirmed = value; confirmations++; });
        for (int i = 0; i < 4; i++) yield return null;
        Canvas.ForceUpdateCanvases();
        TMP_InputField input = ui.GetComponentInChildren<TMP_InputField>();
        Check(input != null && input.interactable && !input.readOnly, "Nickname field is editable");
        var hits = new List<RaycastResult>();
        var pointer = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(Camera.main,
                input.transform.TransformPoint(((RectTransform)input.transform).rect.center)),
            button = PointerEventData.InputButton.Left
        };
        EventSystem.current.RaycastAll(pointer, hits);
        Results.Add("RAYCAST " + string.Join(" > ", hits.Select(hit => hit.gameObject.name + "@" + hit.sortingOrder)));
        Check(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == input.gameObject,
            "Nickname field receives the pointer ahead of the spawn collider");
        EventSystem.current.SetSelectedGameObject(null);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
        for (int i = 0; i < 3; i++) yield return null;
        Check(input.isFocused && EventSystem.current.currentSelectedGameObject == input.gameObject,
            "Clicking the field retains keyboard focus");
        input.ProcessEvent(Event.KeyboardEvent("^a"));
        foreach (char c in "Violet") input.ProcessEvent(new Event { type = EventType.KeyDown, character = c });
        Check(input.text == "Violet", "Keyboard characters replace the default name");
        input.ProcessEvent(Event.KeyboardEvent("backspace"));
        Check(input.text == "Viole", "Backspace edits the nickname");
        foreach (char c in "한글") input.ProcessEvent(new Event { type = EventType.KeyDown, character = c });
        Check(input.text == "Viole한글", "Korean characters are accepted");
        input.ForceLabelUpdate();
        Check(input.textComponent.font.HasCharacter('한', true, true) && input.textComponent.font.HasCharacter('글', true, true),
            "Nickname font renders Hangul in the current interface language");
        input.textComponent.ForceMeshUpdate();
        for (int i = 0; i < 2; i++) yield return null;
        Capture("nickname-input");
        for (int i = 0; i < 3; i++) yield return null;
        var confirmButton = ui.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "btnNicknameConfirm");
        pointer.position = RectTransformUtility.WorldToScreenPoint(Camera.main, confirmButton.transform.position);
        hits.Clear();
        EventSystem.current.RaycastAll(pointer, hits);
        Check(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == confirmButton.gameObject,
            "Register button receives the pointer ahead of the spawn collider");
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Check(confirmed == "Viole한글" && confirmations == 1, "Confirmation forwards edited text exactly once to a local test callback");
        Check(!input.gameObject.activeInHierarchy, "Confirmation closes the dialog");
        ui.ShowNicknamePrompt("AGAIN", _ => confirmations++);
        for (int i = 0; i < 3; i++) yield return null;
        Check(input.isFocused && input.text == "AGAIN", "Reopened dialog focuses the same input correctly");
        Check(Errors.Count == 0, "No runtime errors in nickname input checks");
        if (File.Exists(Path.Combine(Root, "KEEP_NICKNAME_PREVIEW")))
        {
            ui.ShowNicknamePrompt("NONAME", _ => Debug.Log("Nickname UI preview confirmed locally; no score uploaded."));
            foreach (TMP_Text label in ui.GetComponentsInChildren<TMP_Text>())
                if (label.name == "Title") label.text = "INPUT TEST";
        }
    }

    private static IEnumerator Scenarios()
    {
        for (int i = 0; i < 8; i++) yield return null;
        Time.timeScale = 0f;
        int initialPopupOrder = Get<int>(Managers.UI, "m_InitialOrder");
        var abandonedPopup = Managers.UI.ShowPopupUI<UI_SoundPopup>();
        Object.Destroy(abandonedPopup.gameObject);
        for (int i = 0; i < 3; i++) yield return null;
        Check(Managers.UI.FindPopup<UI_SoundPopup>() == null, "Destroyed popup does not prevent reopening its type");
        Managers.Scene.ChangeScene(W01SceneType.Intro);
        for (int i = 0; i < 6; i++) yield return null;
        Check(Managers.UI.FindPopup<UI_IntroPopup>().GetComponent<Canvas>().sortingOrder == initialPopupOrder,
            "Scene cleanup tolerates destroyed entries and restores the configured UI order");
        Managers.UI.ShowPopupUI<UI_SoundPopup>();
        Managers.Scene.ChangeScene(W01SceneType.Game);
        for (int i = 0; i < 6; i++) yield return null;
        Check(Managers.UI.FindPopup<UI_GamePopup>().GetComponent<Canvas>().sortingOrder == initialPopupOrder,
            "Leaving before popup initialization cannot shift the next scene UI order");
        Managers.Scene.ChangeScene(W01SceneType.Intro);
        for (int i = 0; i < 6; i++) yield return null;
        UnityEngine.Random.InitState(9132026);
        SoundManager sound = Managers.Sound;
        bool musicMuted = sound.IsMuted(Define.Sound.Bgm);
        bool effectsMuted = sound.IsMuted(Define.Sound.Effect);
        // Keep the stored preferences untouched; inspect routing with every voice muted.
        sound.SetMuted(Define.Sound.Bgm, true);
        sound.SetMuted(Define.Sound.Effect, true);
        foreach (string path in Directory.GetFiles("Assets/Resources/Sounds", "*.wav", SearchOption.AllDirectories))
        {
            string resource = path.Replace('\\', '/').Substring("Assets/Resources/".Length);
            AudioClip clip = Resources.Load<AudioClip>(resource.Substring(0, resource.Length - 4));
            Check(clip != null && clip.length > 0 && clip.channels == 2, "Imported stereo clip: " + Path.GetFileName(path));
            var importer = (AudioImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            var settings = importer.ContainsSampleSettingsOverride("Android")
                ? importer.GetOverrideSampleSettings("Android") : importer.defaultSampleSettings;
            bool music = resource.StartsWith("Sounds/BGM/");
            var expectedLoadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            Check(settings.compressionFormat == (music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM)
                && settings.loadType == expectedLoadType && settings.preloadAudioData == !music
                && clip.loadType == expectedLoadType && !importer.forceToMono,
                "Android audio import policy: " + Path.GetFileName(path));
        }
        Check(IAPManager.RemoveAdsProductId == "com.secondwindgames.violettap.removeads_v2", "IAP product ID preserved");
        Check(Mathf.Abs(sound.GetAudioClipLength("SFX/Target_NeonTap") - sound.GetAudioClipLength("Sounds/SFX/Target_NeonTap")) < .001f,
            "Clip length accepts the same resource paths as playback");
        var root = GameObject.Find("@SoundRoot");
        sound.Stop(Define.Sound.Effect);
        sound.Play(Define.Sound.Effect, "SFX/New_Best", .21f, .9f);
        var first = root.GetComponentsInChildren<AudioSource>().Single(s => s.clip != null && s.clip.name == "New_Best");
        sound.Play(Define.Sound.Effect, "SFX/Target_Quick", .75f, 1.12f);
        Check(Mathf.Approximately(first.volume, .21f) && Mathf.Approximately(first.pitch, .9f), "Overlapping effects preserve earlier gain and pitch");
        Check(root.GetComponentsInChildren<AudioSource>().Where(s => !s.loop).All(s => s.mute), "SFX mute covers all voices");
        for (int i = 0; i < 64; i++) sound.Play(Define.Sound.Effect, "SFX/New_Best", .1f);
        Check(root.GetComponentsInChildren<AudioSource>().Length == 14, "Burst playback stays within 12 effects plus 2 music voices");
        sound.Stop(Define.Sound.Effect);
        Check(root.GetComponentsInChildren<AudioSource>().Where(s => !s.loop).All(s => !s.isPlaying && s.clip == null), "Stop effects clears every voice");

        sound.Play(Define.Sound.Bgm, "BGM/Game_NeonLobby", .38f);
        double until = EditorApplication.timeSinceStartup + .7;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        var bgm = root.GetComponentsInChildren<AudioSource>().Single(s => s.loop && s.isPlaying);
        Check(bgm.mute && Mathf.Abs(bgm.volume - .38f) < .01f, "Music crossfade completes while gameplay time is paused");
        int sample = bgm.timeSamples;
        sound.Play(Define.Sound.Bgm, "BGM/Game_NeonLobby", .38f);
        Check(bgm.timeSamples >= sample && sample > 0, "Requesting current BGM does not restart the track");
        sound.Play(Define.Sound.Bgm, "BGM/Gameplay_NeonRush", .46f);
        until = EditorApplication.timeSinceStartup + .7;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(root.GetComponentsInChildren<AudioSource>().Count(s => s.loop && s.isPlaying) == 1, "Crossfade stops the outgoing stream");

        GameObject probeObject = new GameObject("QA Target", typeof(SpriteRenderer), typeof(CircleTarget));
        CircleTarget probe = probeObject.GetComponent<CircleTarget>();
        int taps = 0, misses = 0;
        probe.Bind(TapTargetType.Normal, 1f, 1f, _ => taps++, _ => misses++);
        probe.OnPointerDown(null);
        probe.OnPointerDown(null);
        Check(taps == 1, "An unbound repeated pointer click cannot score twice");
        probe.Bind(TapTargetType.Normal, 1f, 1f, _ => taps++, _ => misses++);
        Set(probe, "m_RemainingLifetime", 0f);
        Invoke(probe, "Update");
        probe.OnPointerDown(null);
        Check(taps == 1 && misses == 1, "Expired targets cannot accept a stale click");
        foreach (TapTargetType type in Enum.GetValues(typeof(TapTargetType)))
            Check(probe.GetSprite(type) != null, "Target sprite: " + type);
        Object.Destroy(probeObject);

        foreach (Vector2 size in new[] { new Vector2(1080, 2340), new Vector2(1440, 2960), new Vector2(1080, 1920), new Vector2(1536, 2048), new Vector2(2400, 1080), new Vector2(540, 2340) })
        {
            Rect safe = new Rect(0, 42, size.x, size.y - 108);
            Rect view = ResponsiveGameViewport.CalculatePixelRect(size.x, size.y, safe, 90, 9f / 16f);
            Check(view.width > 0 && view.height > 0 && view.xMin >= safe.xMin && view.xMax <= safe.xMax + .1f
                && view.yMin >= safe.yMin + 90 && view.yMax <= safe.yMax + .1f, "Safe area/banner geometry: " + size);
        }

        Rect staleSafe = ResponsiveGameViewport.CalculatePixelRect(1920, 1080, new Rect(0, 0, 1440, 2898), 0, 9f / 16f);
        Check(Mathf.Approximately(staleSafe.center.x, 960) && Mathf.Approximately(staleSafe.height, 1080),
            "Stale portrait safe area cannot shift a landscape viewport");
        Rect notchSafe = ResponsiveGameViewport.CalculatePixelRect(1920, 1080, new Rect(84, 30, 1836, 1050), 90, 9f / 16f);
        Check(Mathf.Approximately(notchSafe.center.x, 1002) && Mathf.Approximately(notchSafe.yMin, 120),
            "Valid asymmetric cutout and banner insets remain honored");
        Rect emptySafe = ResponsiveGameViewport.CalculatePixelRect(1080, 2340, Rect.zero, 0, 9f / 16f);
        Check(emptySafe == new Rect(0, 0, 1080, 2340), "Empty safe area falls back to the current display");
        Rect invalidSafe = ResponsiveGameViewport.CalculatePixelRect(1920, 1080, new Rect(float.NaN, 0, 1440, 2898), 0, 9f / 16f);
        Check(invalidSafe == staleSafe, "Non-finite safe area cannot produce invalid camera coordinates");
        Rect narrowView = ResponsiveGameViewport.CalculatePixelRect(540, 2340, new Rect(0, 0, 540, 2340), 0, 9f / 16f);
        Check(narrowView == new Rect(0, 585, 540, 1170), "Narrow window keeps artwork and HUD together at the original playfield ratio");
        Rect narrowBanner = ResponsiveGameViewport.CalculatePixelRect(540, 2340, new Rect(0, 30, 540, 2280), 90, 9f / 16f);
        Check(narrowBanner == new Rect(0, 630, 540, 1170), "Narrow playfield centers above the banner and inside safe insets");
        Rect phoneView = ResponsiveGameViewport.CalculatePixelRect(1440, 2960, new Rect(0, 0, 1440, 2898), 0, 9f / 16f);
        Check(phoneView == new Rect(0, 0, 1440, 2898), "Normal portrait layout keeps its existing dimensions");

        Managers.Scene.ChangeScene(W01SceneType.Game);
        for (int i = 0; i < 6; i++) yield return null;
        GameScene game = Object.FindFirstObjectByType<GameScene>();
        Check(game != null, "Game scene loads");
		// The deterministic round below scores 20: keep it below the in-memory best.
		game.bestScore = Mathf.Max(117, game.bestScore);
        GameFlow flow = game.GetComponent<GameFlow>();
        var ui = Object.FindFirstObjectByType<UI_GamePopup>();
        var timer = Get<CountdownTimer>(game, "m_Timer");
        game.StartRound();
        Check(flow.State == GameFlowState.Playing && Targets(game).Count == 1, "Ready starts a populated round");
        float before = timer.Remaining;
        Tap(game, TapTargetType.TimeBonus);
        Check(Mathf.Abs(timer.Remaining - before - 1) < .01f, "Time target grants one second");
        Check(Get<int>(game, "m_Score") == 2, "Time target grants two base points");
        for (int i = 0; i < 9; i++) Tap(game, TapTargetType.Normal);
        Check(Get<float>(game, "m_FeverRemaining") > 0 && Targets(game).Count == 2, "Ten-hit streak starts fever with two targets");
        int scoreBefore = Get<int>(game, "m_Score");
        Tap(game, TapTargetType.Normal);
        Check(Get<int>(game, "m_Score") == scoreBefore + 3, "Fever awards its score multiplier");
        float minimumSpacing = float.PositiveInfinity;
        MethodInfo spawnPosition = typeof(GameScene).GetMethod("GetSpawnPosition", BindingFlags.Instance | BindingFlags.NonPublic);
        for (int i = 0; i < 500; i++)
        {
            Vector3 candidate = (Vector3)spawnPosition.Invoke(game, null);
            foreach (CircleTarget active in Targets(game))
                minimumSpacing = Mathf.Min(minimumSpacing, Vector3.Distance(candidate, active.transform.position));
        }
        Check(minimumSpacing >= 1.35f, "500 candidate spawns keep fever targets separated");
        Invoke(game, "UpdateFever");
        Capture("fever");
        for (int i = 0; i < 3; i++) yield return null;
        Set(game, "m_FeverRemaining", .00001f);
        Time.timeScale = 1f;
        for (int i = 0; i < 3; i++) yield return null;
        Time.timeScale = 0f;
        Check(Get<float>(game, "m_FeverRemaining") == 0 && Targets(game).Count == 1, "Fever expiry removes the extra target");
        Check(ui.GetTextStatus().text == GameLocalization.T("TAP THE GLOWING TARGETS", "빛나는 타겟을 터치하세요"), "Fever expiry restores normal guidance");
        timer.Start(1f);
        Tap(game, TapTargetType.Bomb);
        Check(flow.State == GameFlowState.Result && Targets(game).Count == 0 && timer.Remaining == 0, "Bomb at one second ends round without refilling targets");
        RestoreBest();
        Capture("result");
        for (int i = 0; i < 3; i++) yield return null;
        game.RetryRound();
        game.StartRound();
        CircleTarget initial = Targets(game)[0];
        Check(initial.Type != TapTargetType.Bomb && initial.Type != TapTargetType.Quick
            && Get<float>(initial, "m_Lifetime") > 1.7f, "Retry spawns at starting difficulty with a fresh timer");
        timer.Tick(100f);
        Check(flow.State == GameFlowState.Result && Targets(game).Count == 0, "Natural timer completion clears gameplay");
        RestoreBest();

        Managers.Scene.ChangeScene(W01SceneType.Intro);
        for (int i = 0; i < 6; i++) yield return null;
        Check(Resources.FindObjectsOfTypeAll<Texture2D>().Count(t => t.name == "PrototypeTargetCircle") == 0,
            "Leaving the game releases generated target textures");
        Check(!Resources.FindObjectsOfTypeAll<AudioClip>().Any(c => c.name == "FeverStart" || c.name == "FeverEnd"),
            "Fever uses imported assets without leaked runtime clips");
        until = EditorApplication.timeSinceStartup + .75;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Capture("intro");
        for (int i = 0; i < 4; i++) yield return null;
        sound.SetMuted(Define.Sound.Bgm, musicMuted);
        sound.SetMuted(Define.Sound.Effect, effectsMuted);
        Check(Errors.Count == 0, "No runtime errors during the suite");
    }

    private static void Tap(GameScene game, TapTargetType type)
    {
        CircleTarget target = Targets(game)[0];
        target.Bind(type, 100f, .82f, t => Invoke(game, "HandleTargetTapped", t), t => Invoke(game, "HandleTargetMissed", t));
        target.SetVisual(target.GetSprite(type), Color.white);
        target.OnPointerDown(null);
    }

    private static List<CircleTarget> Targets(GameScene game) => Get<List<CircleTarget>>(game, "m_ActiveTargets");
    private static T Get<T>(object obj, string name) => (T)obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
    private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
    private static void Invoke(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, args);
    private static void Capture(string label) => ScreenCapture.CaptureScreenshot(Path.Combine(runDirectory, label + ".png"));
    private static void Check(bool condition, string description)
    {
        Results.Add((condition ? "PASS " : "FAIL ") + description);
        if (!condition) throw new InvalidOperationException(description);
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Errors.Add(message + "\n" + stack);
    }

    private static void RestoreBest()
    {
        if (SessionState.GetBool("VioletTap.QA.HadBest", false)) PlayerPrefs.SetInt(BestKey, SessionState.GetInt("VioletTap.QA.Best", 0));
        else PlayerPrefs.DeleteKey(BestKey);
        PlayerPrefs.Save();
    }

    private static void Finish(string failure)
    {
        routine = null;
        foreach (var audioProbe in Object.FindObjectsByType<VioletTapAudioProbe>(FindObjectsSortMode.None)) Object.Destroy(audioProbe);
        Application.logMessageReceived -= OnLog;
        Time.timeScale = previousTimeScale;
        UnityEngine.Random.state = previousRandom;
        if (previousAdsDisabled.HasValue)
        {
            Managers.Ads.SetAdsDisabled(previousAdsDisabled.Value);
            previousAdsDisabled = null;
        }
        RestoreBest();
        foreach (string key in new[] { "VioletTap.Audio.BgmEnabled", "VioletTap.Audio.EffectEnabled", TapHaptics.EnabledKey })
        {
            if (SessionState.GetBool("VioletTap.QA.Had." + key, false)) PlayerPrefs.SetInt(key, SessionState.GetInt("VioletTap.QA.Value." + key, 1));
            else PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
        EditorPrefs.SetString("VioletTap.EditorLanguage", SessionState.GetString("VioletTap.QA.Language", "System"));
        CoreServices.Sound.SetMuted(Define.Sound.Bgm, !Managers.IsBgmEnabled);
        CoreServices.Sound.SetMuted(Define.Sound.Effect, !Managers.IsEffectEnabled);
        if (failure != null) Results.Add("FAIL " + failure);
        Results.AddRange(Errors.Select(error => "RUNTIME ERROR " + error));
        File.WriteAllLines(Path.Combine(runDirectory, "results.txt"), Results);
        File.WriteAllText(Path.Combine(Root, "latest-run.txt"), runDirectory);
        Debug.Log($"VioletTap QA: {Results.Count(r => r.StartsWith("PASS "))} passed; report: {runDirectory}");
        if (failure != null || (!File.Exists(Path.Combine(Root, "KEEP_NICKNAME_PREVIEW")) && !File.Exists(Path.Combine(Root, "KEEP_FEEDBACK_PREVIEW")))) EditorApplication.ExitPlaymode();
    }
}
