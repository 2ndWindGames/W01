using System;
using System.Collections.Generic;
using _01.Scripts.Game;
using _01.Scripts.Scene;
using SWGUnity2DCore.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI.Popup
{
    /// <summary>Dedicated, scrollable target guide. Opening it preserves the round.</summary>
    public sealed class UI_GameHelp : MonoBehaviour
    {
        private const float CardHeight = 270f;
        private const float CardGap = 18f;
        private static readonly Color SurfaceColor = new Color(.035f, .014f, .095f);
        private static readonly Color AccentColor = new Color(.15f, .9f, 1f);
        private static readonly Color VioletColor = new Color(.66f, .16f, 1f);
        private GameScene m_Game;
        private GameObject m_Modal;
        private RectTransform m_Panel;
        private RectTransform m_Viewport;
        private RectTransform m_Content;
        private ScrollRect m_Scroll;
        private readonly List<RectTransform> m_Cards = new();
        private TextMeshProUGUI m_Title;
        private TextMeshProUGUI m_Hint;
        private TextMeshProUGUI m_EffectLabel;
        private TextMeshProUGUI m_HapticsLabel;
        private TextMeshProUGUI m_ResumeLabel;
        private Button m_OpenButton;
        private Button m_DismissButton;
        private Vector2 m_LayoutSize;
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
            help.CreateOpener(root, parent.GetComponent<UI_GamePopup>().GetButtonStart());
            return help;
        }

        private void CreateOpener(RectTransform root, Button start)
        {
            // Keep the glyph inside the neon rail, with a larger invisible touch area.
            var startRect = (RectTransform)start.transform;
            var position = startRect.anchoredPosition + new Vector2(
                startRect.rect.xMax + 68f, startRect.rect.center.y);
            var hitArea = Rect("btnHelp", root, new Vector2(128, 128), position);
            hitArea.anchorMin = hitArea.anchorMax = new Vector2(.5f, 0f);
            hitArea.gameObject.AddComponent<Image>().color = Color.clear;
            // Offset only the visual so the generous hit area stays clear of Start/Retry.
            var icon = Rect("QuestionIcon", hitArea, new Vector2(76, 76), new Vector2(-16f, 0f));
            var ring = icon.gameObject.AddComponent<HelpIconRing>();
            ring.color = new Color(.4f, .4f, .4f, .7f);
            ring.raycastTarget = false;
            var label = Label(icon, "QuestionMark", "?", 48f, new Vector2(62, 62), Vector2.zero,
                new Color(.58f, .58f, .58f, .72f));
            // A punctuation icon keeps the same weight and shape in every language.
            label.font = TMP_Settings.defaultFontAsset;
            label.fontStyle = FontStyles.Bold;
            label.outlineColor = new Color(.28f, .28f, .28f, .7f);
            label.outlineWidth = .12f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            m_OpenButton = hitArea.gameObject.AddComponent<Button>();
            StyleIconButton(m_OpenButton, label);
            m_OpenButton.onClick.AddListener(Open);
        }

        public void Open()
        {
            if (!isActiveAndEnabled || IsOpen || m_Game == null || Managers.Ads.IsShowingInterstitial) return;
            if (m_Modal == null) Build();
            m_Game.SetHelpOpen(true);
            m_OpenButton.gameObject.SetActive(false);
            m_Modal.SetActive(true);
            m_ResumeLabel.text = GameLocalization.T("BACK TO GAME", "게임으로 돌아가기");
            RefreshSettings();
            FitPanel();
            Canvas.ForceUpdateCanvases();
            m_Scroll.StopMovement();
            m_Scroll.verticalNormalizedPosition = 1f;
        }

        public void Close()
        {
            if (m_Modal != null) m_Modal.SetActive(false);
            if (m_Game != null) m_Game.SetHelpOpen(false);
            if (m_OpenButton != null) m_OpenButton.gameObject.SetActive(true);
        }

        private void OnDisable() => Close();

        private void Update()
        {
            if (!IsOpen) return;
            FitPanel();
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        private void FitPanel()
        {
            var available = ((RectTransform)transform).rect.size;
            var size = new Vector2(Mathf.Min(960f, available.x - 64f), Mathf.Min(2080f, available.y - 96f));
            size = Vector2.Max(size, new Vector2(400f, 720f));
            if (size == m_LayoutSize) return;
            m_LayoutSize = size;
            m_Panel.sizeDelta = size;
            m_Panel.localScale = Vector3.one;
            m_Title.rectTransform.sizeDelta = new Vector2(size.x - 224f, 72f);
            m_Hint.rectTransform.sizeDelta = new Vector2(size.x - 96f, 84f);
            m_Content.sizeDelta = new Vector2(size.x - 84f, m_Cards.Count * (CardHeight + CardGap) - CardGap);
            float cardWidth = m_Content.sizeDelta.x;
            for (int i = 0; i < m_Cards.Count; i++)
            {
                var card = m_Cards[i];
                card.sizeDelta = new Vector2(cardWidth, CardHeight);
                card.anchoredPosition = new Vector2(0f, -CardHeight * .5f - i * (CardHeight + CardGap));
                ((RectTransform)card.Find("Icon")).anchoredPosition = new Vector2(-cardWidth * .5f + 98f, 0f);
                ((RectTransform)card.Find("Name")).sizeDelta = new Vector2(cardWidth - 232f, 54f);
                ((RectTransform)card.Find("Description")).sizeDelta = new Vector2(cardWidth - 232f, 154f);
            }
            float toggleWidth = (size.x - 84f) * .5f;
            PlaceBottom(m_EffectLabel.transform.parent, new Vector2(toggleWidth, 132f), new Vector2(-(toggleWidth + 20f) * .5f, 252f));
            PlaceBottom(m_HapticsLabel.transform.parent, new Vector2(toggleWidth, 132f), new Vector2((toggleWidth + 20f) * .5f, 252f));
            PlaceBottom(m_ResumeLabel.transform.parent, new Vector2(size.x - 64f, 136f), new Vector2(0f, 100f));
        }

        private void Build()
        {
            var modalRect = Rect("HelpModal", transform, Vector2.zero, Vector2.zero);
            Stretch(modalRect);
            m_Modal = modalRect.gameObject;
            var shade = m_Modal.AddComponent<Image>();
            shade.color = new Color(.008f, .004f, .03f, 1f);

            m_Panel = Rect("HelpPanel", modalRect, new Vector2(960, 2080), Vector2.zero);
            var panel = Surface(m_Panel, new Color(.023f, .008f, .068f), VioletColor, 34f);
            panel.BorderWidth = 3f;
            panel.InnerLine = true;
            panel.GlowWidth = 18f;
            panel.BottomColor = new Color(.035f, .012f, .085f);
            var headerLine = Rect("HeaderSignal", m_Panel, Vector2.zero, Vector2.zero);
            headerLine.anchorMin = new Vector2(0f, 1f);
            headerLine.anchorMax = Vector2.one;
            headerLine.offsetMin = new Vector2(48f, -110f);
            headerLine.offsetMax = new Vector2(-48f, -107f);
            Surface(headerLine, AccentColor, AccentColor, 0f).raycastTarget = false;

            m_Title = Label(m_Panel, "Title", GameLocalization.T("NEON FIELD GUIDE", "네온 터치 가이드"), 44,
                new Vector2(736, 72), new Vector2(-64, -56), new Color(.88f, .78f, 1f));
            m_Title.rectTransform.anchorMin = m_Title.rectTransform.anchorMax = new Vector2(.5f, 1f);
            m_Title.alignment = TextAlignmentOptions.MidlineLeft;
            m_Title.enableAutoSizing = true;
            m_Title.fontSizeMin = 32f;
            m_Title.fontSizeMax = 44f;
            m_Title.textWrappingMode = TextWrappingModes.NoWrap;
            m_Hint = Label(m_Panel, "Hint", GameLocalization.T(
                "Tap a card to try its sound and vibration.\nYour game stays paused while this guide is open.",
                "카드를 눌러 소리·진동을 체험하세요.\n도움말을 보는 동안 게임은 멈춥니다."), 27,
                new Vector2(864, 84), new Vector2(0, -156), new Color(.72f, .74f, .88f));
            m_Hint.rectTransform.anchorMin = m_Hint.rectTransform.anchorMax = new Vector2(.5f, 1f);
            m_Hint.alignment = TextAlignmentOptions.MidlineLeft;
            var dismissRect = Rect("btnHelpDismiss", m_Panel, new Vector2(112, 96), new Vector2(-84, -64));
            dismissRect.anchorMin = dismissRect.anchorMax = Vector2.one;
            dismissRect.gameObject.AddComponent<Image>().color = Color.clear;
            var dismissLabel = Label(dismissRect, "Label", "×", 48f, new Vector2(72, 72), Vector2.zero,
                new Color(.62f, .62f, .62f));
            dismissLabel.font = TMP_Settings.defaultFontAsset;
            dismissLabel.fontStyle = FontStyles.Bold;
            // The shared font has a cyan outline; this icon must stay neutral grey.
            dismissLabel.outlineColor = new Color(.62f, .62f, .62f);
            dismissLabel.textWrappingMode = TextWrappingModes.NoWrap;
            m_DismissButton = dismissRect.gameObject.AddComponent<Button>();
            StyleIconButton(m_DismissButton, dismissLabel);
            m_DismissButton.onClick.AddListener(Close);

            m_Viewport = Rect("CardViewport", m_Panel, Vector2.zero, Vector2.zero);
            Stretch(m_Viewport);
            m_Viewport.offsetMin = new Vector2(32f, 350f);
            m_Viewport.offsetMax = new Vector2(-32f, -218f);
            m_Viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .001f);
            m_Viewport.gameObject.AddComponent<RectMask2D>();
            m_Content = Rect("Cards", m_Viewport, Vector2.zero, Vector2.zero);
            m_Content.anchorMin = m_Content.anchorMax = new Vector2(.5f, 1f);
            m_Content.pivot = new Vector2(.5f, 1f);
            m_Scroll = m_Viewport.gameObject.AddComponent<ScrollRect>();
            m_Scroll.viewport = m_Viewport;
            m_Scroll.content = m_Content;
            m_Scroll.horizontal = false;
            m_Scroll.movementType = ScrollRect.MovementType.Clamped;
            m_Scroll.scrollSensitivity = 65f;
            m_Scroll.decelerationRate = .08f;

            var track = Rect("ScrollTrack", m_Panel, new Vector2(8f, 0f), Vector2.zero);
            track.anchorMin = new Vector2(1f, 0f);
            track.anchorMax = Vector2.one;
            track.offsetMin = new Vector2(-20f, 350f);
            track.offsetMax = new Vector2(-12f, -218f);
            Surface(track, new Color(.08f, .025f, .15f), Color.clear, 4f).BorderWidth = 0f;
            var handle = Rect("Handle", track, Vector2.zero, Vector2.zero);
            Stretch(handle);
            var handleGraphic = Surface(handle, AccentColor, Color.clear, 4f);
            handleGraphic.BorderWidth = 0f;
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleGraphic;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            m_Scroll.verticalScrollbar = scrollbar;
            m_Scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            var config = m_Game.Config;
            Card("Normal", "target_normal", GameLocalization.T("NEON", "네온"),
                GameLocalization.T($"+{config.scorePerTap} point before it fades.\nStreak 5: ×2 · Streak 20: ×3\nMissing a safe target resets your streak.",
                    $"사라지기 전에 터치하면 +{config.scorePerTap}점.\n5연속: 2배 · 20연속: 3배\n놓치면 연속 기록 초기화 · 폭탄 제외"),
                AccentColor, TapTargetType.Normal, false);
            Card("Quick", "target_quick", GameLocalization.T("QUICK", "퀵"),
                GameLocalization.T("+3 points · Disappears faster.\nTap quickly to keep your streak!",
                    "+3점 · 더 빨리 사라져요.\n빠르게 터치해서 연속 기록을 이어가세요!"),
                new Color(.73f, .6f, 1f), TapTargetType.Quick, false);
            Card("Time", "target_time", GameLocalization.T("TIME BONUS", "시간 보너스"),
                GameLocalization.T("+2 points and +1 second.\nKeep the round going!",
                    "+2점과 남은 시간 +1초.\n플레이 시간을 늘려보세요!"),
                new Color(1f, .82f, .38f), TapTargetType.TimeBonus, false);
            Card("Bomb", "target_danger", GameLocalization.T("BOMB · AVOID", "폭탄 · 피하세요"),
                GameLocalization.T("Tap: −2 seconds and a streak reset.\nLet bombs disappear untouched.",
                    "누르면 −2초, 연속 기록이 초기화돼요.\n터치하지 말고 사라지게 두세요."),
                new Color(1f, .48f, .54f), TapTargetType.Bomb, false);
            Card("Fever", "target_normal", GameLocalization.T("FEVER", "피버"),
                GameLocalization.T($"{config.feverCombo} hits: ×{config.feverScoreMultiplier} points for {config.feverDuration:0} seconds.\n{config.feverTargetCount} targets; each hit extends fever.\nA miss or bomb ends fever!",
                    $"{config.feverCombo}연속 터치하면 {config.feverDuration:0}초간 점수 {config.feverScoreMultiplier}배.\n타깃 {config.feverTargetCount}개 · 터치할수록 피버 연장\n놓치거나 폭탄을 누르면 피버 종료!"),
                new Color(1f, .54f, .92f), TapTargetType.Normal, true);

            var effect = Button(m_Panel, "btnHelpEffects", "", new Vector2(438, 132), new Vector2(-229, 252), () =>
            {
                Managers.SetEffectEnabled(!Managers.IsEffectEnabled);
                RefreshSettings();
                if (Managers.IsEffectEnabled) TapFeedback.PlayCue(TapTargetType.Normal, false);
            });
            m_EffectLabel = effect.GetComponentInChildren<TextMeshProUGUI>();
            var haptics = Button(m_Panel, "btnHelpHaptics", "", new Vector2(438, 132), new Vector2(229, 252), () =>
            {
                TapHaptics.SetEnabled(!TapHaptics.IsEnabled);
                RefreshSettings();
                TapHaptics.Play(TapTargetType.Normal);
            });
            m_HapticsLabel = haptics.GetComponentInChildren<TextMeshProUGUI>();
            var resume = Button(m_Panel, "btnHelpClose", "", new Vector2(896, 136), new Vector2(0, 100), Close, true);
            m_ResumeLabel = resume.GetComponentInChildren<TextMeshProUGUI>();
        }

        private void RefreshSettings()
        {
            m_EffectLabel.text = GameLocalization.T("SOUND", "효과음") + (Managers.IsEffectEnabled ? "  ON" : "  OFF");
            m_HapticsLabel.text = GameLocalization.T("VIBRATION", "진동") + (TapHaptics.IsEnabled ? "  ON" : "  OFF");
            m_EffectLabel.color = Managers.IsEffectEnabled ? Color.white : new Color(.64f, .71f, .8f);
            m_HapticsLabel.color = TapHaptics.IsEnabled ? Color.white : new Color(.64f, .71f, .8f);
        }

        private void Card(string name, string sprite, string title, string description, Color accent,
            TapTargetType type, bool fever)
        {
            var card = Rect("Card" + name, m_Content, new Vector2(876, CardHeight), Vector2.zero);
            card.anchorMin = card.anchorMax = new Vector2(.5f, 1f);
            m_Cards.Add(card);
            var background = Surface(card, SurfaceColor, Color.Lerp(SurfaceColor, accent, .7f), 24f);
            background.GlowWidth = 5f;
            background.BottomColor = new Color(.018f, .008f, .05f);
            var button = card.gameObject.AddComponent<Button>();
            StyleButton(button, background);
            button.onClick.AddListener(() => TapFeedback.PlayCue(type, fever));
            var icon = Rect("Icon", card, new Vector2(140, 140), Vector2.zero).gameObject.AddComponent<Image>();
            icon.sprite = Resources.Load<Sprite>("UI/NeonSignalPack/Targets/" + sprite);
            icon.color = fever ? accent : Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var heading = Label(card, "Name", title, 34, new Vector2(644, 54), new Vector2(84, 82), accent);
            heading.alignment = TextAlignmentOptions.MidlineLeft;
            var body = Label(card, "Description", description, 32, new Vector2(644, 154), new Vector2(84, -24), new Color(.88f, .93f, 1f));
            body.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private Button Button(Transform parent, string name, string title, Vector2 size, Vector2 position, Action clicked, bool primary = false)
        {
            var rect = Rect(name, parent, size, position);
            Color fill = primary ? new Color(.11f, .025f, .24f) : SurfaceColor;
            var image = Surface(rect, fill, primary ? AccentColor : VioletColor, 22f);
            image.InnerLine = primary;
            image.GlowWidth = primary ? 10f : 5f;
            var button = rect.gameObject.AddComponent<Button>();
            StyleButton(button, image);
            button.onClick.AddListener(() => clicked());
            var label = Label(rect, "Label", title, 34, size - new Vector2(48, 32), Vector2.zero,
                Color.white);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(24, 16);
            label.rectTransform.offsetMax = new Vector2(-24, -16);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return button;
        }

        private static HelpSurface Surface(RectTransform rect, Color fill, Color border, float radius)
        {
            var surface = rect.gameObject.AddComponent<HelpSurface>();
            surface.color = fill;
            surface.BorderColor = border;
            surface.Radius = radius;
            surface.CutCorners = true;
            return surface;
        }

        private static void StyleIconButton(Button button, Graphic graphic)
        {
            StyleButton(button, graphic);
            var colors = button.colors;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(1f, 1f, 1f, .7f);
            button.colors = colors;
        }

        private static void StyleButton(Button button, Graphic graphic)
        {
            button.targetGraphic = graphic;
            var colors = button.colors;
            // Selectable tints the graphic's existing fill; white preserves that color.
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.88f, .96f, 1f);
            colors.pressedColor = new Color(.65f, .8f, .95f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = .08f;
            button.colors = colors;
        }

        private static void PlaceBottom(Transform target, Vector2 size, Vector2 position)
        {
            var rect = (RectTransform)target;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string value, float font,
            Vector2 size, Vector2 position, Color color)
        {
            var label = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = font;
            label.fontStyle = GameLocalization.IsKorean || font >= 34f ? FontStyles.Bold : FontStyles.Normal;
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
