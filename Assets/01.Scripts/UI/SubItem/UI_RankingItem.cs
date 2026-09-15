using SWGUnity2DCore.UI;
using SWGUnity2DCore.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI.SubItem
{
    public class UI_RankingItem : UI_Base
    {
		private enum RowStyle
		{
			Standard,
			CurrentInList,
			PinnedCurrent
		}

		private static readonly Color CurrentRankColor = new Color(1f, 0.85f, 0.2f, 1f);
		private static readonly Color CurrentNameColor = new Color(0.3f, 1f, 0.95f, 1f);
		private static readonly Color CurrentScoreColor = new Color(1f, 0.45f, 0.9f, 1f);
		private static readonly Color CurrentListBackgroundColor = new Color(0.42f, 0.08f, 0.62f, 0.95f);
		private static readonly Color PinnedBackgroundColor = new Color(0.04f, 0.48f, 0.72f, 1f);
		private static readonly Color PinnedNameColor = new Color(1f, 0.9f, 0.25f, 1f);

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

			var rankText = GetText((int)Texts.txtRank);
			var nicknameText = GetText((int)Texts.txtNicName);
			var scoreText = GetText((int)Texts.txtScore);
			rankText.textWrappingMode = TextWrappingModes.NoWrap;
			rankText.enableAutoSizing = true;
			rankText.fontSizeMin = 16f;
			rankText.fontSizeMax = 36f;
			nicknameText.textWrappingMode = TextWrappingModes.NoWrap;
			nicknameText.overflowMode = TextOverflowModes.Ellipsis;
			nicknameText.richText = false;
			nicknameText.parseCtrlCharacters = false;
			scoreText.textWrappingMode = TextWrappingModes.NoWrap;
			scoreText.enableAutoSizing = true;
			scoreText.fontSizeMin = 16f;
			scoreText.fontSizeMax = 36f;
			
			m_Background = GetComponent<Image>();
			if (m_Background != null)
				m_DefaultBackgroundColor = m_Background.color;
			
            IsInitialized = true;
        }

        public void SetProfile(int entryRank, string nickname, double entryScore, bool isCurrentPlayer = false)
        {
			SetProfile(entryRank, nickname, entryScore,
				isCurrentPlayer ? RowStyle.CurrentInList : RowStyle.Standard);
		}

		public void SetPinnedProfile(int entryRank, string nickname, double entryScore)
		{
			SetProfile(entryRank, nickname, entryScore, RowStyle.PinnedCurrent);
		}

		private void SetProfile(int entryRank, string nickname, double entryScore, RowStyle rowStyle)
		{
			Initialize();
			var rankText = GetText((int)Texts.txtRank);
			var nicknameText = GetText((int)Texts.txtNicName);
			GameLocalization.ApplyNicknameFont(nicknameText);
			var scoreText = GetText((int)Texts.txtScore);

			rankText.text = entryRank.ToString();
			string displayName = (nickname ?? "").Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
			nicknameText.text = rowStyle == RowStyle.CurrentInList
				? "[" + GameLocalization.T("YOU", "나") + "]" + (string.IsNullOrWhiteSpace(displayName) ? "" : " " + displayName)
				: displayName;
			scoreText.text = entryScore.ToString("0");

			ApplyStyle(rowStyle, rankText, nicknameText, scoreText);
		}

		private void ApplyStyle(RowStyle rowStyle, TMP_Text rankText, TMP_Text nicknameText, TMP_Text scoreText)
		{
			bool isCurrentInList = rowStyle == RowStyle.CurrentInList;
			bool isPinned = rowStyle == RowStyle.PinnedCurrent;
			rankText.color = isCurrentInList ? CurrentRankColor : Color.white;
			nicknameText.color = isCurrentInList ? CurrentNameColor : isPinned ? PinnedNameColor : Color.white;
			scoreText.color = isCurrentInList ? CurrentScoreColor : Color.white;
			FontStyles style = rowStyle != RowStyle.Standard || GameLocalization.IsKorean
				? FontStyles.Bold
				: FontStyles.Normal;
			rankText.fontStyle = nicknameText.fontStyle = scoreText.fontStyle = style;

			if (m_Background != null)
			{
				m_Background.color = isCurrentInList
					? CurrentListBackgroundColor
					: isPinned ? PinnedBackgroundColor : m_DefaultBackgroundColor;
			}
		}

		public void SetCurrentPlayerStatus(string status)
		{
			Initialize();
			var rankText = GetText((int)Texts.txtRank);
			var nicknameText = GetText((int)Texts.txtNicName);
			GameLocalization.ApplyNicknameFont(nicknameText);
			var scoreText = GetText((int)Texts.txtScore);
			rankText.text = "--";
			nicknameText.text = GameLocalization.T("YOU", "나");
			scoreText.text = status ?? "";
			ApplyStyle(RowStyle.PinnedCurrent, rankText, nicknameText, scoreText);
		}
    }
}
