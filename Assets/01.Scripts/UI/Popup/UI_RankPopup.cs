using System;
using _01.Scripts.UI.SubItem;
using SWGUnity2DCore.Manager;
using SWGUnity2DCore.Util;
using SWGUnity2DCore.Util.Logging;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Leaderboards.Models;

namespace _01.Scripts.UI.Popup
{
    public class UI_RankPopup : UI_Popup
    {
        private Transform m_Content;
        private TextMeshProUGUI m_Title;
		private ScrollRect m_ScrollRect;
		private TextMeshProUGUI m_MyRankingLabel;
		private UI_RankingItem m_MyRankingRow;
        
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
            PopupPresentation.Prepare(transform);
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
            
			m_ScrollRect = GetComponentInChildren<ScrollRect>(true);
			m_Content = m_ScrollRect != null ? m_ScrollRect.content : null;
            if (m_Content == null)
            {
                GameLog.Debug("Fail to find gameobject: Content");
                return false;
            }

			CreateMyRankingRow();
            
            LoadRankings();
            return true;
        }

        private async void LoadRankings()
        {
			try
			{
				string currentPlayerId = await Managers.Rank.GetPlayerIdAsync();
				var rankingsTask = Managers.Rank.GetTopNPlayers(10);
				var myRankingTask = Managers.Rank.GetMyScoreAsync();
				await System.Threading.Tasks.Task.WhenAll(rankingsTask, myRankingTask);
				var rankings = rankingsTask.Result;
				PlayerRankResult myRanking = myRankingTask.Result;
                // 조회 중 팝업이 닫혀 파괴된 경우 아이템을 생성하지 않습니다.
				if (this == null || m_Content == null)
					return;

				if (rankings == null)
				{
					SetTitle(GameLocalization.T("RANKING UNAVAILABLE", "랭킹을 불러올 수 없습니다"));
				}
				else
				{
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

				if (!myRanking.IsAvailable)
					ShowMyRankingUnavailable();
				else if (!myRanking.HasScore)
					ShowMyRankingNoRecord();
				else
				{
					LeaderboardEntry entry = myRanking.Entry;
					ShowMyRanking(entry.Rank + 1,
						RankManager.GetNickname(entry.Metadata, "NONAME"), entry.Score);
				}
            }
            catch (Exception e)
            {
				SetTitle(GameLocalization.T("RANKING UNAVAILABLE", "랭킹을 불러올 수 없습니다"));
				ShowMyRankingUnavailable();
                Debug.LogException(e);
            }
        }

		private void CreateMyRankingRow()
		{
			if (m_MyRankingRow != null) return;
			CreateMyRankingLabel();
			m_MyRankingRow = Managers.UI.MakeSubItem<UI_RankingItem>(transform, "itemRanking");
			m_MyRankingRow.gameObject.name = "MyRanking";
			m_MyRankingRow.Initialize();
			var rect = (RectTransform)m_MyRankingRow.transform;
			rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
			rect.anchoredPosition = new Vector2(0f, -342f);
			rect.sizeDelta = new Vector2(620f, 110f);
			Transform close = transform.Find("btnClose");
			if (close != null) close.SetAsLastSibling();
			ShowMyRankingLoading();
		}

		private void CreateMyRankingLabel()
		{
			if (m_MyRankingLabel != null) return;
			var labelObject = new GameObject("txtMyRankLabel", typeof(RectTransform),
				typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Outline));
			labelObject.layer = gameObject.layer;
			var rect = (RectTransform)labelObject.transform;
			rect.SetParent(transform, false);
			rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
			rect.anchoredPosition = new Vector2(0f, -260f);
			rect.sizeDelta = new Vector2(620f, 32f);

			m_MyRankingLabel = labelObject.GetComponent<TextMeshProUGUI>();
			m_MyRankingLabel.text = GameLocalization.T("MY RANK", "내 순위");
			m_MyRankingLabel.alignment = TextAlignmentOptions.Center;
			m_MyRankingLabel.fontSize = 28f;
			m_MyRankingLabel.enableAutoSizing = true;
			m_MyRankingLabel.fontSizeMin = 22f;
			m_MyRankingLabel.fontSizeMax = 28f;
			m_MyRankingLabel.fontStyle = FontStyles.Bold;
			m_MyRankingLabel.textWrappingMode = TextWrappingModes.NoWrap;
			m_MyRankingLabel.color = new Color(1f, 0.84f, 0.24f, 1f);
			m_MyRankingLabel.raycastTarget = false;
			GameLocalization.ApplyFont(m_MyRankingLabel);

			var outline = labelObject.GetComponent<Outline>();
			outline.effectColor = new Color(0.04f, 0.75f, 1f, 0.8f);
			outline.effectDistance = new Vector2(1.5f, -1.5f);
			outline.useGraphicAlpha = true;
		}

		private void ShowMyRanking(int rank, string nickname, double score)
		{
			if (m_MyRankingRow != null) m_MyRankingRow.SetPinnedProfile(rank, nickname, score);
		}

		private void ShowMyRankingLoading()
		{
			ShowMyRankingStatus(GameLocalization.T("LOADING", "불러오는 중"));
		}

		private void ShowMyRankingNoRecord()
		{
			ShowMyRankingStatus(GameLocalization.T("NO RECORD", "기록 없음"));
		}

		private void ShowMyRankingUnavailable()
		{
			ShowMyRankingStatus(GameLocalization.T("UNAVAILABLE", "확인 불가"));
		}

		private void ShowMyRankingStatus(string status)
		{
			if (m_MyRankingRow != null) m_MyRankingRow.SetCurrentPlayerStatus(status);
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
