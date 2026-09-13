using System;
using _01.Scripts.UI.SubItem;
using SWGUnity2DCore.Manager;
using SWGUnity2DCore.Util;
using SWGUnity2DCore.Util.Logging;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _01.Scripts.UI.Popup
{
    public class UI_RankPopup : UI_Popup
    {
        private Transform m_Content;
        private TextMeshProUGUI m_Title;
        
        enum Buttons
        {
            btnClose
        }
        
        public override bool Init()
        {
            if (!base.Init())
                return false;
			
            BindButton(typeof(Buttons));
			GameLocalization.ApplyFont(this);
            GetButton((int)Buttons.btnClose).gameObject.BindEvent(OnClickCloseButton);
			foreach (var label in GetComponentsInChildren<TextMeshProUGUI>(true))
			{
				if (label.name == "txtTitle")
				{
					m_Title = label;
					label.text = GameLocalization.T("LOADING RANKING...", "랭킹 불러오는 중...");
				}
				else if (label.name == "txtClose") label.text = GameLocalization.T("CLOSE", "닫기");
			}
            
            var scrollRect = GetComponentInChildren<ScrollRect>(true);
            m_Content = scrollRect != null ? scrollRect.content : null;
            if (m_Content == null)
            {
                GameLog.Debug("Fail to find gameobject: Content");
                return false;
            }
            
            LoadRankings();
            return true;
        }

        private async void LoadRankings()
        {
            try
            {
				string currentPlayerId = await Managers.Rank.GetPlayerIdAsync();
                var rankings = await Managers.Rank.GetTopNPlayers(10);
                // 조회 중 팝업이 닫혀 파괴된 경우 아이템을 생성하지 않습니다.
                if (this == null || m_Content == null || rankings == null)
				{
					SetTitle(GameLocalization.T("RANKING UNAVAILABLE", "랭킹을 불러올 수 없습니다"));
                    return;
				}

				SetTitle(rankings.Count == 0
					? GameLocalization.T("NO SCORES YET", "아직 등록된 기록이 없습니다")
					: GameLocalization.T("RANKING", "랭킹"));

                foreach (var entry in rankings)
                {
                    var nickname = RankManager.GetNickname(entry.Metadata, "NONAME");
                    GameLog.Debug($"{entry.Rank + 1}위 / {nickname} / {entry.Score}");

                    var rankingItem = Managers.UI.MakeSubItem<UI_RankingItem>(m_Content, "itemRanking");
                    rankingItem.Initialize();
					bool isCurrentPlayer = string.Equals(entry.PlayerId, currentPlayerId,
						StringComparison.Ordinal);
                    rankingItem.SetProfile(entry.Rank + 1, nickname, entry.Score, isCurrentPlayer);
                    
                }
            }
            catch (Exception e)
            {
				SetTitle(GameLocalization.T("RANKING UNAVAILABLE", "랭킹을 불러올 수 없습니다"));
                Debug.LogException(e);
            }
        }

		private void SetTitle(string value)
		{
			if (this != null && m_Title != null) m_Title.text = value;
		}
        
        private void OnClickCloseButton()
        {
            GameLog.Debug("OnClickCloseButton");
            Managers.UI.ClosePopupUI(this);
        }
    }
}
