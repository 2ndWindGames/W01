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
            Button resume = FindRequired("btnHelpClose").GetComponent<Button>();
            m_ResumeLabel = resume.GetComponentInChildren<TextMeshProUGUI>(true);

            foreach (string name in new[] { "Normal", "Quick", "Time", "Bomb", "Fever" })
                m_Cards.Add((RectTransform)FindRequired("Card" + name));

            m_OpenButton.onClick.AddListener(Open);
            dismiss.onClick.AddListener(Close);
            resume.onClick.AddListener(Close);
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
                    startRect.rect.xMax + 100f, startRect.rect.center.y);
            }

            m_Modal.SetActive(false);
            m_OpenButton.gameObject.SetActive(true);
            RefreshText();
            GameLocalization.ApplyFont(this);
        }

        public void Open()
        {
            if (!isActiveAndEnabled || IsOpen || m_Game == null || Managers.Ads.IsShowingFullScreenAd) return;
            m_Game.SetHelpOpen(true);
            m_OpenButton.gameObject.SetActive(false);
            m_Modal.SetActive(true);
            RefreshText();
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
                "PULSE: build a streak · SCAN: tap safe, avoid bomb.\nSURGE: tap 1 → 2 → 3; mistakes or misses reset the streak.\nTap cards to preview effects. Play pauses here.",
                "펄스: 연속 터치 · 스캔: 안전 타깃 터치, 폭탄 피하기\n서지: 1 → 2 → 3 순서대로. 틀리거나 놓치면 연속 기록 초기화\n카드를 눌러 효과 확인 · 도움말을 보는 동안 일시정지");
            m_Hint.fontSize = 24f;
            m_Hint.fontSizeMax = 24f;
            m_Hint.fontSizeMin = 20f;
            m_Hint.enableAutoSizing = true;
            m_ResumeLabel.text = GameLocalization.T("BACK TO GAME", "게임으로 돌아가기");

            SetCardText("Normal", GameLocalization.T("NEON", "네온"), GameLocalization.T(
                $"Small +{config.scorePerTap + 2} · Mid +{config.scorePerTap + 1} · Large +{config.scorePerTap}.\nStreak 10: ×2 · Streak 50: ×3\nMissing a safe target resets your streak.",
                $"작게 +{config.scorePerTap + 2}점 · 중간 +{config.scorePerTap + 1}점 · 크게 +{config.scorePerTap}점\n10연속: 2배 · 50연속: 3배\n놓치면 연속 기록 초기화 · 폭탄 제외"));
            SetCardText("Quick", GameLocalization.T("QUICK", "퀵"), GameLocalization.T(
                "Small +5 · Mid +4 · Large +3.\nDisappears faster—tap quickly!",
                "작게 +5점 · 중간 +4점 · 크게 +3점\n빨리 사라지니 서둘러 터치하세요!"));
            SetCardText("Time", GameLocalization.T("TIME BONUS", "시간 보너스"), GameLocalization.T(
                "Small +3s · Mid +2s · Large +1s.\n+2 points at every size.",
                "작게 +3초 · 중간 +2초 · 크게 +1초\n크기와 관계없이 +2점"));
            SetCardText("Bomb", GameLocalization.T("BOMB · AVOID", "폭탄 · 피하세요"), GameLocalization.T(
                "Always appears with a safe target.\nTap: −2 seconds and a streak reset.\nLeave it untouched; tap the safe target.",
                "항상 안전 타깃과 함께 등장해요.\n누르면 −2초, 연속 기록 초기화.\n폭탄을 피하고 안전 타깃을 터치하세요."));
            SetCardText("Fever", GameLocalization.T("FEVER", "피버"), GameLocalization.T(
                $"{config.feverCombo} hits: extra targets for {config.feverDuration:0} seconds.\nHits extend fever; combo sets the score.\nA miss or bomb ends fever!",
                $"{config.feverCombo}연속: 타깃이 늘어나는 {config.feverDuration:0}초 피버.\n터치할수록 연장 · 점수 배수는 콤보와 동일\n놓치거나 폭탄을 누르면 피버 종료!"));
        }

        private void SetCardText(string name, string heading, string description)
        {
            Transform card = FindRequired("Card" + name);
            card.Find("Name").GetComponent<TextMeshProUGUI>().text = heading;
            card.Find("Description").GetComponent<TextMeshProUGUI>().text = description;
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
            m_Title.rectTransform.anchoredPosition = new Vector2(-48f, -116f);
            m_Title.rectTransform.sizeDelta = new Vector2(size.x - 256f, 72f);
            m_Hint.rectTransform.sizeDelta = new Vector2(size.x - 160f, 106f);
            RectTransform viewport = (RectTransform)m_Scroll.transform;
            viewport.anchoredPosition = new Vector2(0f, -58f);
            viewport.sizeDelta = new Vector2(-64f, -500f);
            if (m_Scroll.verticalScrollbar != null)
            {
                RectTransform track = (RectTransform)m_Scroll.verticalScrollbar.transform;
                track.anchoredPosition = new Vector2(-16f, -58f);
                track.sizeDelta = new Vector2(8f, -500f);
            }
            float contentHeight = m_Cards.Count * (CardHeight + CardGap) - CardGap;
            m_Content.sizeDelta = new Vector2(size.x - 84f, contentHeight);
            m_Scroll.vertical = contentHeight > size.y - 500f + 1f;
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
