using System;
using _01.Scripts.Game;
using _01.Scripts.Scene;
using SWGUnity2DCore.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI.Popup
{
    /// <summary>In-game target guide and feedback controls. Opening it preserves the round.</summary>
    public sealed class UI_GameHelp : MonoBehaviour
    {
        private GameScene m_Game;
        private GameObject m_Modal;
        private RectTransform m_Panel;
        private TextMeshProUGUI m_EffectLabel;
        private TextMeshProUGUI m_HapticsLabel;
        private TextMeshProUGUI m_ResumeLabel;
        private Button m_OpenButton;
        public bool IsOpen => m_Modal != null && m_Modal.activeSelf;

        public static UI_GameHelp Create(Transform parent, GameScene game)
        {
            var root = Rect("GameHelp", parent, Vector2.zero, Vector2.zero);
            Stretch(root);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingLayerID = parent.GetComponent<Canvas>().sortingLayerID;
            canvas.sortingOrder = 900;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var help = root.gameObject.AddComponent<UI_GameHelp>();
            help.m_Game = game;
            help.m_OpenButton = help.Button(root, "btnHelp", GameLocalization.T("HELP", "도움말"),
                new Vector2(150, 90), new Vector2(-150, -75), help.Open);
            var buttonRect = (RectTransform)help.m_OpenButton.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = Vector2.one;
            return help;
        }

        public void Open()
        {
            if (!isActiveAndEnabled || IsOpen || m_Game == null || Managers.Ads.IsShowingInterstitial) return;
            if (m_Modal == null) Build();
            m_Game.SetHelpOpen(true);
            m_OpenButton.gameObject.SetActive(false);
            m_Modal.SetActive(true);
            m_ResumeLabel.text = GameLocalization.T("BACK TO GAME", "게임으로 돌아가기");
            GameLocalization.ApplyFont(m_ResumeLabel);
            RefreshSettings();
            FitPanel();
        }

        public void Close()
        {
            if (m_Modal != null) m_Modal.SetActive(false);
            if (m_Game != null) m_Game.SetHelpOpen(false);
            if (m_OpenButton != null) m_OpenButton.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            Close();
        }

        private void Update()
        {
            if (!IsOpen) return;
            FitPanel();
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        private void FitPanel()
        {
            var available = ((RectTransform)transform).rect.size;
            float scale = Mathf.Min(1f, Mathf.Min((available.x - 48f) / 1040f, (available.y - 80f) / 1800f));
            m_Panel.localScale = Vector3.one * Mathf.Max(.1f, scale);
        }

        private void Build()
        {
            var modalRect = Rect("HelpModal", transform, Vector2.zero, Vector2.zero);
            Stretch(modalRect);
            m_Modal = modalRect.gameObject;
            var shade = m_Modal.AddComponent<Image>();
            shade.color = new Color(.005f, .008f, .035f, .94f);

            m_Panel = Rect("HelpPanel", modalRect, new Vector2(1040, 1800), Vector2.zero);
            var background = m_Panel.gameObject.AddComponent<Image>();
            background.sprite = Resources.Load<Sprite>("UI/NeonSignalPack/NineSlice/Panels/panel_popup");
            background.type = Image.Type.Sliced;
            background.color = Color.white;

            Label(m_Panel, "Title", GameLocalization.T("NEON FIELD GUIDE", "네온 터치 가이드"), 49,
                new Vector2(900, 85), new Vector2(0, 790), new Color(.4f, .9f, 1f));
            Label(m_Panel, "Hint", GameLocalization.T("GAME PAUSED · TAP A CARD TO TRY ITS SOUND & VIBRATION",
                "플레이 일시정지 · 카드를 눌러 소리와 진동을 체험하세요"), 25,
                new Vector2(900, 65), new Vector2(0, 712), new Color(.7f, .78f, .92f));

            var config = m_Game.Config;
            Card("Normal", "target_normal", GameLocalization.T("NEON", "네온"),
                GameLocalization.T($"+{config.scorePerTap} point · Tap before it fades.\nStreak 5: ×2 / Streak 20: ×3",
                    $"+{config.scorePerTap}점 · 사라지기 전에 터치!\n5연속: 2배 / 20연속: 3배"),
                new Color(.3f, .9f, 1f), 525, TapTargetType.Normal, false);
            Card("Quick", "target_quick", GameLocalization.T("QUICK", "퀵"),
                GameLocalization.T("+3 points · Fades faster.\nReact quickly to keep your streak!",
                    "+3점 · 더 빨리 사라져요.\n빠르게 터치해서 연속 기록을 이어가세요!"),
                new Color(.7f, .52f, 1f), 295, TapTargetType.Quick, false);
            Card("Time", "target_time", GameLocalization.T("TIME BONUS", "시간 보너스"),
                GameLocalization.T("+2 points and +1 second.\nKeep the round going!",
                    "+2점과 남은 시간 +1초.\n플레이 시간을 늘려보세요!"),
                new Color(1f, .82f, .3f), 65, TapTargetType.TimeBonus, false);
            Card("Bomb", "target_danger", GameLocalization.T("BOMB · AVOID", "폭탄 · 피하세요"),
                GameLocalization.T("Tap: −2 seconds and streak reset.\nLet it disappear safely.",
                    "누르면 −2초, 연속 기록이 초기화돼요.\n터치하지 말고 사라지게 두세요."),
                new Color(1f, .4f, .48f), -165, TapTargetType.Bomb, false);
            Card("Fever", "target_normal", GameLocalization.T("FEVER", "피버"),
                GameLocalization.T($"{config.feverCombo} hits in a row: ×{config.feverScoreMultiplier} for {config.feverDuration:0} sec!\n{config.feverTargetCount} targets · Each hit extends fever.\nMiss a target: your streak resets.",
                    $"{config.feverCombo}연속 터치: {config.feverDuration:0}초간 점수 {config.feverScoreMultiplier}배!\n타깃 {config.feverTargetCount}개 · 터치할수록 피버 연장.\n일반·퀵·시간 타깃을 놓치면 연속 기록 초기화."),
                new Color(1f, .44f, .92f), -395, TapTargetType.Normal, true);

            var effect = Button(m_Panel, "btnHelpEffects", "", new Vector2(400, 100), new Vector2(-220, -620), () =>
            {
                Managers.SetEffectEnabled(!Managers.IsEffectEnabled);
                RefreshSettings();
                if (Managers.IsEffectEnabled) TapFeedback.PlayCue(TapTargetType.Normal, false);
            });
            m_EffectLabel = effect.GetComponentInChildren<TextMeshProUGUI>();
            var haptics = Button(m_Panel, "btnHelpHaptics", "", new Vector2(400, 100), new Vector2(220, -620), () =>
            {
                TapHaptics.SetEnabled(!TapHaptics.IsEnabled);
                RefreshSettings();
                TapHaptics.Play(TapTargetType.Normal);
            });
            m_HapticsLabel = haptics.GetComponentInChildren<TextMeshProUGUI>();
            var resume = Button(m_Panel, "btnHelpClose", "", new Vector2(640, 115), new Vector2(0, -765), Close);
            m_ResumeLabel = resume.GetComponentInChildren<TextMeshProUGUI>();
        }

        private void RefreshSettings()
        {
            GameLocalization.ApplyFont(m_EffectLabel);
            GameLocalization.ApplyFont(m_HapticsLabel);
            m_EffectLabel.text = GameLocalization.T("SOUND", "효과음") + (Managers.IsEffectEnabled ? "  ON" : "  OFF");
            m_HapticsLabel.text = GameLocalization.T("VIBRATION", "진동") + (TapHaptics.IsEnabled ? "  ON" : "  OFF");
            m_EffectLabel.color = Managers.IsEffectEnabled ? Color.white : new Color(.6f, .65f, .75f);
            m_HapticsLabel.color = TapHaptics.IsEnabled ? Color.white : new Color(.6f, .65f, .75f);
        }

        private void Card(string name, string sprite, string title, string description, Color accent,
            float y, TapTargetType type, bool fever)
        {
            var card = Rect("Card" + name, m_Panel, new Vector2(900, 214), new Vector2(0, y));
            var background = card.gameObject.AddComponent<Image>();
            background.color = new Color(.055f, .055f, .14f, .97f);
            var button = card.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => TapFeedback.PlayCue(type, fever));
            var stripe = Rect("Accent", card, new Vector2(5, 178), new Vector2(-442, 0)).gameObject.AddComponent<Image>();
            stripe.color = accent;
            stripe.raycastTarget = false;
            var icon = Rect("Icon", card, new Vector2(155, 155), new Vector2(-338, 0)).gameObject.AddComponent<Image>();
            icon.sprite = Resources.Load<Sprite>("UI/NeonSignalPack/Targets/" + sprite);
            icon.color = fever ? accent : Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var heading = Label(card, "Name", title, 35, new Vector2(635, 52), new Vector2(105, 62), accent);
            heading.alignment = TextAlignmentOptions.MidlineLeft;
            var body = Label(card, "Description", description, 28, new Vector2(635, 135), new Vector2(105, -27), Color.white);
            body.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private Button Button(Transform parent, string name, string title, Vector2 size, Vector2 position, Action clicked)
        {
            var rect = Rect(name, parent, size, position);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Resources.Load<Sprite>("UI/NeonSignalPack/NineSlice/Buttons/button_primary_normal");
            image.type = Image.Type.Sliced;
            var button = rect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => clicked());
            Label(rect, "Label", title, size.y <= 90 ? 26 : 31, size - new Vector2(28, 16), Vector2.zero, Color.white);
            return button;
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string value, float font,
            Vector2 size, Vector2 position, Color color)
        {
            var label = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = font;
            label.fontStyle = GameLocalization.IsKorean || font >= 35f ? FontStyles.Bold : FontStyles.Normal;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            GameLocalization.ApplyFont(label);
            return label;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = 5;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
