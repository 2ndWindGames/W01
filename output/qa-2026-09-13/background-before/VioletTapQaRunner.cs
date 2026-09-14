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

    private static void Begin()
    {
        SessionState.SetBool(PendingKey, false);
        runDirectory = Path.Combine(Root, "run-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(runDirectory);
        Results.Clear();
        Errors.Clear();
        previousTimeScale = Time.timeScale;
        previousRandom = UnityEngine.Random.state;
        SessionState.SetBool("VioletTap.QA.HadBest", PlayerPrefs.HasKey(BestKey));
        SessionState.SetInt("VioletTap.QA.Best", PlayerPrefs.GetInt(BestKey));
        foreach (string key in new[] { "VioletTap.Audio.EffectEnabled", TapHaptics.EnabledKey })
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
        routine = statusOnly ? StatusScenarios() : feedbackOnly ? FeedbackScenarios() : nicknameOnly ? NicknameScenarios() : Scenarios();
    }

    private static void Tick()
    {
        if (routine == null)
        {
            string layoutRequest = Path.Combine(Root, "CAPTURE_LAYOUT_QA");
            if (EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(layoutRequest))
            {
                File.Delete(layoutRequest);
                CaptureLayout();
                return;
            }
            string statusRequest = Path.Combine(Root, "RUN_STATUS_QA");
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(statusRequest))
            {
                File.Delete(statusRequest);
                RunStatusCheck();
                return;
            }
            string feedbackRequest = Path.Combine(Root, "RUN_FEEDBACK_QA");
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(feedbackRequest))
            {
                File.Delete(feedbackRequest);
                RunFeedbackCheck();
                return;
            }
            string nicknameRequest = Path.Combine(Root, "RUN_NICKNAME_QA");
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(nicknameRequest))
            {
                File.Delete(nicknameRequest);
                RunNicknameCheck();
                return;
            }
            string request = Path.Combine(Root, "RUN_QA");
            if (!EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(request))
            {
                File.Delete(request);
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
        RestoreBest();
        foreach (string key in new[] { "VioletTap.Audio.EffectEnabled", TapHaptics.EnabledKey })
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
