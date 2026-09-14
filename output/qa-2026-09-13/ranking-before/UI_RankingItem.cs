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
            BindText(typeof(Texts));
			GameLocalization.ApplyFont(this);
			m_Background = GetComponent<Image>();
			if (m_Background != null)
				m_DefaultBackgroundColor = m_Background.color;
			
            IsInitialized = true;
        }

        public void SetProfile(int entryRank, string nickname, double entryScore, bool isCurrentPlayer = false)
        {
			var rankText = GetText((int)Texts.txtRank);
			var nicknameText = GetText((int)Texts.txtNicName);
			GameLocalization.ApplyNicknameFont(nicknameText);
			var scoreText = GetText((int)Texts.txtScore);

			rankText.text = isCurrentPlayer ? "▶ " + entryRank.ToString("00") : entryRank.ToString("00");
			nicknameText.text = isCurrentPlayer
				? "★  " + nickname + "  [" + GameLocalization.T("YOU", "나") + "]"
				: nickname;
			scoreText.text = entryScore.ToString("000");

			rankText.color = isCurrentPlayer ? new Color(1f, 0.85f, 0.2f, 1f) : Color.white;
			nicknameText.color = isCurrentPlayer ? new Color(0.3f, 1f, 0.95f, 1f) : Color.white;
			scoreText.color = isCurrentPlayer ? new Color(1f, 0.45f, 0.9f, 1f) : Color.white;
			rankText.fontStyle = isCurrentPlayer ? FontStyles.Bold : FontStyles.Normal;
			nicknameText.fontStyle = isCurrentPlayer ? FontStyles.Bold : FontStyles.Normal;
			scoreText.fontStyle = isCurrentPlayer ? FontStyles.Bold : FontStyles.Normal;

			if (m_Background != null)
			{
				m_Background.color = isCurrentPlayer
					? new Color(0.42f, 0.08f, 0.62f, 0.95f)
					: m_DefaultBackgroundColor;
			}
        }
    }
}
