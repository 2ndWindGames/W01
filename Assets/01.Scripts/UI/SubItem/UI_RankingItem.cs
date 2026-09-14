using SWGUnity2DCore.UI;
using SWGUnity2DCore.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI.SubItem
{
    public class UI_RankingItem : UI_Base
    {
        enum Texts
        {
            txtRank,
            txtNicName,
            txtScore
        }
        
        public bool IsInitialized { get; private set; }
		private Image m_Background;
		private Color m_DefaultBackgroundColor;

        
        public override bool Init()
        {
            if (!base.Init())
                return false;


            if (IsInitialized)
            {
                return true;
            } 
			
            Initialize();
            return true;
        }
        
        public void Initialize()
        {
            if (IsInitialized) return;
            BindText(typeof(Texts));
			GameLocalization.ApplyFont(this);
			ConfigureLabel(GetText((int)Texts.txtRank), 0f, .19f, 20f, 8f, TextAlignmentOptions.Midline, true);
			ConfigureLabel(GetText((int)Texts.txtNicName), .19f, .62f, 4f, 12f, TextAlignmentOptions.MidlineLeft, false);
			ConfigureLabel(GetText((int)Texts.txtScore), .62f, 1f, 8f, 24f, TextAlignmentOptions.MidlineRight, true);
			m_Background = GetComponent<Image>();
			if (m_Background != null)
				m_DefaultBackgroundColor = m_Background.color;
			
            IsInitialized = true;
        }

        private static void ConfigureLabel(TextMeshProUGUI label, float start, float end, float leftPadding,
            float rightPadding, TextAlignmentOptions alignment, bool autoSize)
        {
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(start, .5f);
            rect.anchorMax = new Vector2(end, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2((leftPadding - rightPadding) * .5f, 0f);
            rect.sizeDelta = new Vector2(-leftPadding - rightPadding, 70f);
            label.margin = Vector4.zero;
            label.alignment = alignment;
            label.richText = false;
            label.parseCtrlCharacters = false;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.enableAutoSizing = autoSize;
            label.fontSize = label.fontSizeMax = 36f;
            label.fontSizeMin = 18f;
        }

        public void SetProfile(int entryRank, string nickname, double entryScore, bool isCurrentPlayer = false)
        {
			Initialize();
			var rankText = GetText((int)Texts.txtRank);
			var nicknameText = GetText((int)Texts.txtNicName);
			GameLocalization.ApplyNicknameFont(nicknameText);
			var scoreText = GetText((int)Texts.txtScore);

			rankText.text = entryRank.ToString("00");
			string displayName = (nickname ?? "").Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
			nicknameText.text = isCurrentPlayer
				? "[" + GameLocalization.T("YOU", "나") + "] " + displayName
				: displayName;
			scoreText.text = entryScore.ToString("000");

			rankText.color = isCurrentPlayer ? new Color(1f, 0.85f, 0.2f, 1f) : Color.white;
			nicknameText.color = isCurrentPlayer ? new Color(0.3f, 1f, 0.95f, 1f) : Color.white;
			scoreText.color = isCurrentPlayer ? new Color(1f, 0.45f, 0.9f, 1f) : Color.white;
			FontStyles style = isCurrentPlayer || GameLocalization.IsKorean ? FontStyles.Bold : FontStyles.Normal;
			rankText.fontStyle = nicknameText.fontStyle = scoreText.fontStyle = style;

			if (m_Background != null)
			{
				m_Background.color = isCurrentPlayer
					? new Color(0.42f, 0.08f, 0.62f, 0.95f)
					: m_DefaultBackgroundColor;
			}
        }
    }
}
