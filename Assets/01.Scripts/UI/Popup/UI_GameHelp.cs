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
    /// <summary>Prefab-backed target guide. Opening it preserves the round.</summary>
    public sealed class UI_GameHelp : MonoBehaviour
    {
        private const string PrefabPath = "Prefabs/UI/Popup/UI_HelpPopup";
        private const float CardHeight = 270f;
        private const float CardGap = 18f;

        private GameScene m_Game;
        private GameObject m_Modal;
        private RectTransform m_Panel;
        private RectTransform m_Content;
        private ScrollRect m_Scroll;
        private readonly List<RectTransform> m_Cards = new();
        private TextMeshProUGUI m_Title;
        private TextMeshProUGUI m_Hint;
        private TextMeshProUGUI m_EffectLabel;
        private TextMeshProUGUI m_HapticsLabel;
        private TextMeshProUGUI m_ResumeLabel;
        private Button m_OpenButton;
        private Vector2 m_LayoutSize;

        public bool IsOpen => m_Modal != null && m_Modal.activeSelf;

        public static UI_GameHelp Create(Transform parent, GameScene game)
        {
            GameObject prefab = Resources.Load<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"UI_HelpPopup prefab missing at Resources/{PrefabPath}.");
                return null;
            }

            GameObject instance = Instantiate(prefab, parent, false);
            UI_GameHelp help = instance.GetComponent<UI_GameHelp>();
            if (help == null)
            {
                Debug.LogError("UI_HelpPopup prefab is missing UI_GameHelp.", instance);
                Destroy(instance);
                return null;
            }

            Canvas parentCanvas = parent.GetComponent<Canvas>();
            Canvas helpCanvas = instance.GetComponent<Canvas>();
            if (parentCanvas != null && helpCanvas != null)
                helpCanvas.sortingLayerID = parentCanvas.sortingLayerID;
            help.Initialize(game, parent.GetComponent<UI_GamePopup>()?.GetButtonStart());
            return help;
        }

        private void Initialize(GameScene game, Button start)
        {
            m_Game = game;
            m_Modal = FindRequired("HelpModal").gameObject;
            m_Panel = (RectTransform)FindRequired("HelpPanel");
            m_Content = (RectTransform)FindRequired("Cards");
            m_Scroll = FindRequired("CardViewport").GetComponent<ScrollRect>();
            m_Title = FindRequired("Title").GetComponent<TextMeshProUGUI>();
            m_Hint = FindRequired("Hint").GetComponent<TextMeshProUGUI>();
            m_OpenButton = FindRequired("btnHelp").GetComponent<Button>();
            Button dismiss = FindRequired("btnHelpDismiss").GetComponent<Button>();
            Button effects = FindRequired("btnHelpEffects").GetComponent<Button>();
            Button haptics = FindRequired("btnHelpHaptics").GetComponent<Button>();
            Button resume = FindRequired("btnHelpClose").GetComponent<Button>();
            m_EffectLabel = effects.GetComponentInChildren<TextMeshProUGUI>(true);
            m_HapticsLabel = haptics.GetComponentInChildren<TextMeshProUGUI>(true);
            m_ResumeLabel = resume.GetComponentInChildren<TextMeshProUGUI>(true);

            foreach (string name in new[] { "Normal", "Quick", "Time", "Bomb", "Fever" })
                m_Cards.Add((RectTransform)FindRequired("Card" + name));

            m_OpenButton.onClick.AddListener(Open);
            dismiss.onClick.AddListener(Close);
            resume.onClick.AddListener(Close);
            effects.onClick.AddListener(ToggleEffects);
            haptics.onClick.AddListener(ToggleHaptics);
            BindCard("Normal", TapTargetType.Normal, false);
            BindCard("Quick", TapTargetType.Quick, false);
            BindCard("Time", TapTargetType.TimeBonus, false);
            BindCard("Bomb", TapTargetType.Bomb, false);
            BindCard("Fever", TapTargetType.Normal, true);

            if (start != null)
            {
                RectTransform startRect = (RectTransform)start.transform;
                RectTransform opener = (RectTransform)m_OpenButton.transform;
                opener.anchoredPosition = startRect.anchoredPosition + new Vector2(
                    startRect.rect.xMax + 68f, startRect.rect.center.y);
            }

            m_Modal.SetActive(false);
            m_OpenButton.gameObject.SetActive(true);
            RefreshText();
            RefreshSettings();
            GameLocalization.ApplyFont(this);
        }

        public void Open()
        {
            if (!isActiveAndEnabled || IsOpen || m_Game == null || Managers.Ads.IsShowingInterstitial) return;
            m_Game.SetHelpOpen(true);
            m_OpenButton.gameObject.SetActive(false);
            m_Modal.SetActive(true);
            RefreshText();
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

        private void ToggleEffects()
        {
            Managers.SetEffectEnabled(!Managers.IsEffectEnabled);
            RefreshSettings();
            if (Managers.IsEffectEnabled) TapFeedback.PlayCue(TapTargetType.Normal, false);
        }

        private void ToggleHaptics()
        {
            TapHaptics.SetEnabled(!TapHaptics.IsEnabled);
            RefreshSettings();
            TapHaptics.Play(TapTargetType.Normal);
        }

        private void BindCard(string name, TapTargetType type, bool fever)
        {
            FindRequired("Card" + name).GetComponent<Button>().onClick.AddListener(
                () => TapFeedback.PlayCue(type, fever));
        }

        private void RefreshText()
        {
            var config = m_Game.Config;
            m_Title.text = GameLocalization.T("NEON FIELD GUIDE", "네온 터치 가이드");
            m_Hint.text = GameLocalization.T(
                "Tap a card to try its sound and vibration.\nYour game stays paused while this guide is open.",
                "카드를 눌러 소리·진동을 체험하세요.\n도움말을 보는 동안 게임은 멈춥니다.");
            m_ResumeLabel.text = GameLocalization.T("BACK TO GAME", "게임으로 돌아가기");

            SetCardText("Normal", GameLocalization.T("NEON", "네온"), GameLocalization.T(
                $"+{config.scorePerTap} point before it fades.\nStreak 5: ×2 · Streak 20: ×3\nMissing a safe target resets your streak.",
                $"사라지기 전에 터치하면 +{config.scorePerTap}점.\n5연속: 2배 · 20연속: 3배\n놓치면 연속 기록 초기화 · 폭탄 제외"));
            SetCardText("Quick", GameLocalization.T("QUICK", "퀵"), GameLocalization.T(
                "+3 points · Disappears faster.\nTap quickly to keep your streak!",
                "+3점 · 더 빨리 사라져요.\n빠르게 터치해서 연속 기록을 이어가세요!"));
            SetCardText("Time", GameLocalization.T("TIME BONUS", "시간 보너스"), GameLocalization.T(
                "+2 points and +1 second.\nKeep the round going!",
                "+2점과 남은 시간 +1초.\n플레이 시간을 늘려보세요!"));
            SetCardText("Bomb", GameLocalization.T("BOMB · AVOID", "폭탄 · 피하세요"), GameLocalization.T(
                "Tap: −2 seconds and a streak reset.\nLet bombs disappear untouched.",
                "누르면 −2초, 연속 기록이 초기화돼요.\n터치하지 말고 사라지게 두세요."));
            SetCardText("Fever", GameLocalization.T("FEVER", "피버"), GameLocalization.T(
                $"{config.feverCombo} hits: ×{config.feverScoreMultiplier} points for {config.feverDuration:0} seconds.\n{config.feverTargetCount} targets; each hit extends fever.\nA miss or bomb ends fever!",
                $"{config.feverCombo}연속 터치하면 {config.feverDuration:0}초간 점수 {config.feverScoreMultiplier}배.\n타깃 {config.feverTargetCount}개 · 터치할수록 피버 연장\n놓치거나 폭탄을 누르면 피버 종료!"));
        }

        private void SetCardText(string name, string heading, string description)
        {
            Transform card = FindRequired("Card" + name);
            card.Find("Name").GetComponent<TextMeshProUGUI>().text = heading;
            card.Find("Description").GetComponent<TextMeshProUGUI>().text = description;
        }

        private void RefreshSettings()
        {
            m_EffectLabel.text = GameLocalization.T("SOUND", "효과음") + (Managers.IsEffectEnabled ? "  ON" : "  OFF");
            m_HapticsLabel.text = GameLocalization.T("VIBRATION", "진동") + (TapHaptics.IsEnabled ? "  ON" : "  OFF");
            m_EffectLabel.color = Managers.IsEffectEnabled ? Color.white : new Color(.64f, .71f, .8f);
            m_HapticsLabel.color = TapHaptics.IsEnabled ? Color.white : new Color(.64f, .71f, .8f);
        }

        private void FitPanel()
        {
            Vector2 available = ((RectTransform)transform).rect.size;
            Vector2 size = new Vector2(Mathf.Min(960f, available.x - 64f), Mathf.Min(2080f, available.y - 96f));
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
                RectTransform card = m_Cards[i];
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

        private static void PlaceBottom(Transform target, Vector2 size, Vector2 position)
        {
            RectTransform rect = (RectTransform)target;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private Transform FindRequired(string objectName)
        {
            foreach (Transform item in GetComponentsInChildren<Transform>(true))
                if (item.name == objectName) return item;
            throw new InvalidOperationException($"UI_HelpPopup prefab is missing '{objectName}'.");
        }
    }
}
