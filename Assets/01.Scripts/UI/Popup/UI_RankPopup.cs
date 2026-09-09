using System;
using _01.Scripts.Manager;
using _01.Scripts.UI.SubItem;
using _01.Scripts.Util;
using _01.Scripts.Util.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI.Popup
{
    public class UI_RankPopup : UI_Popup
    {
        private Transform m_Content;
        
        enum Buttons
        {
            btnClose
        }
        
        public override bool Init()
        {
            if (!base.Init())
                return false;
			
            BindButton(typeof(Buttons));
            GetButton((int)Buttons.btnClose).gameObject.BindEvent(OnClickCloseButton);
            
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
                var rankings = await Managers.Rank.GetTopNPlayers(10);
                // 조회 중 팝업이 닫혀 파괴된 경우 아이템을 생성하지 않습니다.
                if (this == null || m_Content == null || rankings == null)
                    return;

                foreach (var entry in rankings)
                {
                    var nickname = RankManager.GetNickname(entry.Metadata, entry.PlayerId);
                    GameLog.Debug($"{entry.Rank + 1}위 / {nickname} / {entry.Score}");

                    var rankingItem = Managers.UI.MakeSubItem<UI_RankingItem>(m_Content, "itemRanking");
                    rankingItem.Initialize();
                    rankingItem.SetProfile(entry.Rank + 1, nickname, entry.Score);
                    
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        private void OnClickCloseButton()
        {
            GameLog.Debug("OnClickCloseButton");
            Managers.UI.ClosePopupUI(this);
        }
    }
}
