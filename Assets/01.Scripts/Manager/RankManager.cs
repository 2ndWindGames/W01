using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.Purchasing;
using Object = UnityEngine.Object;

namespace _01.Scripts.Manager
{
	public class RankManager
	{
		private GameObject m_RankRoot = null;
		private Task m_InitializationTask;

		// 닉네임 등 랭킹에 함께 표시할 정보를 점수 제출 전에 설정합니다.
		public Dictionary<string, object> PlayerMetadata { get; } = new Dictionary<string, object>();

		public static string GetNickname(string metadata, string fallback)
		{
			if (string.IsNullOrWhiteSpace(metadata)) return fallback;
			try
			{
				var value = JObject.Parse(metadata)["nickname"];
				var nickname = value?.Type == JTokenType.String ? value.Value<string>() : null;
				return string.IsNullOrWhiteSpace(nickname) ? fallback : nickname;
			}
			catch (JsonException)
			{
				return fallback;
			}
		}
		
		private const string leaderBoardId = "violettap_ranking_01"; 
		
		public async void Init()
		{
			try
			{
				await EnsureInitializedAsync();
			}
			catch (Exception e)
			{
				Debug.LogError("리더보드 초기화 오류: " + e);
			}
		}

		private Task EnsureInitializedAsync()
		{
			// 동시에 들어온 요청은 같은 초기화 작업을 기다립니다. 실패하면 재시도합니다.
			if (m_InitializationTask == null || m_InitializationTask.IsFaulted ||
				m_InitializationTask.IsCanceled ||
				(m_InitializationTask.IsCompleted && !AuthenticationService.Instance.IsSignedIn))
			{
				m_InitializationTask = InitializeAsync();
			}

			return m_InitializationTask;
		}

		private async Task InitializeAsync()
		{
			if (m_RankRoot == null)
			{
				m_RankRoot = GameObject.Find("@RankRoot");
				if (m_RankRoot == null)
					m_RankRoot = new GameObject { name = "@RankRoot" };

				Object.DontDestroyOnLoad(m_RankRoot);
			}

			if (string.IsNullOrWhiteSpace(Application.cloudProjectId))
			{
				throw new InvalidOperationException(
					"Unity Cloud 프로젝트가 연결되지 않았습니다. Project Settings > Services에서 " +
					"리더보드가 있는 Unity Cloud 프로젝트를 연결한 뒤 다시 실행하세요.");
			}

			await UnityServices.InitializeAsync();
			if (!AuthenticationService.Instance.IsSignedIn)
			{
				await AuthenticationService.Instance.SignInAnonymouslyAsync();
				
				// 3. 로그인한 플레이어의 ID 확인
				string playerId = AuthenticationService.Instance.PlayerId;
				Debug.Log($"내 플레이어 ID: {playerId}");
			}
		}

		public void SetProfile(string nickName, string country, string icon)
		{
			PlayerMetadata.Clear();
			PlayerMetadata["nickname"] = nickName;
			PlayerMetadata["country"] = country;
			PlayerMetadata["icon"] = icon;
		}
		
		public async void SubmitScore(int score)
		{
			try
			{
				var metadata = new Dictionary<string, object>(PlayerMetadata);
				await EnsureInitializedAsync();
				await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderBoardId, score,
					new AddPlayerScoreOptions { Metadata = metadata.Count > 0 ? metadata : null });
				Debug.Log("점수가 성공적으로 제출되었습니다.");
			}
			catch (Exception e)
			{
				Debug.LogError("점수 제출 오류: " + e.Message);
			}
		}
		
		// 조회 성공 시 목록(기록이 없으면 빈 목록), 실패 시 null을 반환합니다.
		public async Task<List<LeaderboardEntry>> GetTopNPlayers(int topN)
		{
			try
			{
				await EnsureInitializedAsync();
				var topScores = await LeaderboardsService.Instance.GetScoresAsync(leaderBoardId, new GetScoresOptions
				{
					Limit = topN,
					IncludeMetadata = true
				});

				if (topScores != null && topScores.Results.Count > 0)
				{
					foreach (var score in topScores.Results)
					{
						var nickname = GetNickname(score.Metadata, score.PlayerId);
						Debug.Log($"순위 {score.Rank + 1}: 닉네임 = {nickname}, 유저 ID = {score.PlayerId}, 점수 = {score.Score}");
					}
				}
				else
				{
					Debug.Log("리더보드에 데이터가 없습니다.");
				}

				return topScores?.Results ?? new List<LeaderboardEntry>();
			}
			catch (Exception e)
			{
				Debug.LogError($"리더보드 데이터를 가져오는 중 오류가 발생했습니다: {e.Message}");
				return null;
			}
		}
		
		public async void GetMyScore()
		{
			try
			{
				// 현재 유저의 점수를 가져옵니다.
				await EnsureInitializedAsync();
				var myScore = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderBoardId,
					new GetPlayerScoreOptions { IncludeMetadata = true });

				if (myScore != null)
				{
					Debug.Log($"닉네임: {GetNickname(myScore.Metadata, myScore.PlayerId)}, 내 점수: {myScore.Score}");
				}
				else
				{
					Debug.Log("내 점수를 찾을 수 없습니다.");
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"리더보드 데이터를 가져오는 중 오류가 발생했습니다: {e.Message}");
			}
		}
		
		private async void GetMyRank()
		{
			try
			{
				await EnsureInitializedAsync();
				var myRank = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderBoardId,
					new GetPlayerScoreOptions { IncludeMetadata = true });

				Debug.Log(myRank != null ? $"내 등수: {myRank.Rank + 1}" : "등수를 찾을 수 없습니다.");
			}
			catch (Exception e)
			{
				Debug.LogError($"리더보드 데이터를 가져오는 중 오류가 발생했습니다: {e.Message}");
			}
		}
		
		public async void GetInfoByPlayerIds()
		{
			try
			{
				// 다른 플레이어 id
				await EnsureInitializedAsync();
				var otherPlayerIds = new List<string> { "플레이어 아이디1", "플레이어 아이디2" };
				try
				{
					// 다른 플레이어의 정보를 조회합니다.
					var scoresResponse = await LeaderboardsService.Instance.GetScoresByPlayerIdsAsync(leaderBoardId, otherPlayerIds,
						new GetScoresByPlayerIdsOptions { IncludeMetadata = true });

					if (scoresResponse != null)
					{
						// 플레이어 ID, 랭크, 점수, 티어 출력
						Debug.Log(JsonConvert.SerializeObject(scoresResponse));
					}
					else
					{
						Debug.Log("플레이어를 찾을 수 없습니다.");
					}
				}
				catch (Exception e)
				{
					Debug.LogError($"정보 조회 중 오류 발생: {e.Message}");
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"리더보드 데이터를 가져오는 중 오류가 발생했습니다: {e.Message}");
			}
		}
		
	}
}
