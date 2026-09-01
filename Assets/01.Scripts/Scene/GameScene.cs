using System.Collections.Generic;
using _01.Scripts.Manager;
using _01.Scripts.UI.Popup;
using MiniGameKit;
using MiniGameKit.Samples.TapGame;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace _01.Scripts.Scene
{
	public class GameScene : BaseScene
	{
		private static readonly Color[] TargetColors =
		{
			new Color(0.24f, 0.83f, 1f),
			new Color(0.66f, 0.42f, 1f),
			new Color(1f, 0.38f, 0.68f),
			new Color(0.32f, 1f, 0.72f)
		};
		
		private readonly CountdownTimer m_Timer = new();
		private readonly List<MiniGameTarget> m_ActiveTargets = new();
		
		[SerializeField]
		private GameFlow mGameFlow;
		
		[SerializeField]
		private GameObjectPool mTargetPool;
		
		[SerializeField]
		private GameConfig mConfig;
		
		private UI_GamePopup m_UiGamePopup;
		
		private Camera m_MainCamera;
		private Sprite m_TargetSprite;
		
		private TextMeshProUGUI m_BestText;
		private TextMeshProUGUI m_StatusText;
		private TextMeshProUGUI m_ScoreText;
		private TextMeshProUGUI m_TimerText;
		
		private Button m_StartButton;
		
		private int m_Score;
		[HideInInspector] public int bestScore;
		private int m_Combo;
		private int m_Hits;
		private int m_MaxCombo;
		private int m_Misses;
		private int m_BombsTapped;
		private float m_FeverRemaining;
		private bool m_FeverTriggered;
		
		
		protected override bool Init()
		{
			if (!base.Init())
				return false;

			
			sceneType = Define.Scene.Game;
			m_UiGamePopup = Managers.UI.ShowPopupUI<UI_GamePopup>();
			m_UiGamePopup.Initialize();
			
			Debug.Log("Init");
			InitGameScene();
			
			return true;
		}

		private void InitGameScene()
		{
			EnsureSceneServices();
			EnsureConfig();
			SetupPool();

			m_BestText = m_UiGamePopup.GetTextBest();
			m_StatusText = m_UiGamePopup.GetTextStatus();
			m_TimerText = m_UiGamePopup.GetTextTime();
			m_ScoreText = m_UiGamePopup.GetTextScore();

			m_StartButton = m_UiGamePopup.GetButtonStart();
			
			// m_UiGamePopup.BindEventStartButton(StartRound);
			// m_BestText.text = BestScore.ToString("00");

			m_Timer.Completed += HandleTimerCompleted;
			mGameFlow.StateChanged += HandleFlowStateChanged;
			HandleFlowStateChanged(mGameFlow.State);
		}
		
		private void EnsureSceneServices()
		{
			if (mGameFlow == null)
			{
				mGameFlow = gameObject.AddComponent<GameFlow>();
			}

			if (mTargetPool == null)
			{
				mTargetPool = gameObject.AddComponent<GameObjectPool>();
			}
		}

		private void EnsureConfig()
		{
			if (mConfig == null)
			{
				mConfig = ScriptableObject.CreateInstance<GameConfig>();
			}
		}
		
		private void SetupPool()
		{
			GameObject targetPrefab = mConfig.targetPrefab;
			if (targetPrefab == null)
			{
				targetPrefab = CreatePrototypeTarget();
			}

			mTargetPool.Initialize(targetPrefab, mTargetPool.transform, mConfig.initialTargetCount);
		}
		
		private GameObject CreatePrototypeTarget()
		{
			GameObject target = new GameObject("prfTarget");
			target.transform.SetParent(mTargetPool.transform, false);
			target.transform.localScale = Vector3.one * 0.82f;

			SpriteRenderer glowRenderer = new GameObject("Glow").AddComponent<SpriteRenderer>();
			glowRenderer.transform.SetParent(target.transform, false);
			glowRenderer.sprite = m_TargetSprite;
			glowRenderer.color = new Color(0.3f, 0.85f, 1f, 0.16f);
			glowRenderer.transform.localScale = Vector3.one * 1.7f;
			glowRenderer.sortingOrder = -1;

			SpriteRenderer renderer = target.AddComponent<SpriteRenderer>();
			renderer.sprite = m_TargetSprite;
			renderer.color = TargetColors[0];
			renderer.sortingOrder = 0;

			CircleCollider2D collider = target.AddComponent<CircleCollider2D>();
			collider.radius = 0.5f;
			target.AddComponent<MiniGameTarget>();
			target.SetActive(false);
			return target;
		}
		
		private void Update()
		{
			if (mGameFlow.State != GameFlowState.Playing)
			{
				return;
			}
			
			m_Timer.Tick(Time.deltaTime);
			UpdateFever();
			UpdateTimerVisual();
		}
		
		private void UpdateFever()
		{
			if (m_FeverRemaining <= 0f)
			{
				return;
			}

			m_FeverRemaining = Mathf.Max(0f, m_FeverRemaining - Time.deltaTime);

			
			m_TimerText.text = m_FeverRemaining > 0f ? "FEVER  " + m_FeverRemaining.ToString("0.0") : "TAP THE GLOWING TARGETS";
			m_TimerText.color = m_FeverRemaining > 0f ? new Color(0.85f, 0.5f, 1f) : Color.white;
			
			if (m_FeverRemaining <= 0f && m_ActiveTargets.Count > mConfig.initialTargetCount)
			{
				while (m_ActiveTargets.Count > mConfig.initialTargetCount)
				{
					var extra = m_ActiveTargets[^1];
					m_ActiveTargets.RemoveAt(m_ActiveTargets.Count - 1);
					mTargetPool.Despawn(extra.gameObject);
				}
			}
			else
			{
				RefillTargets();
			}
		}
		
		private void UpdateTimerVisual()
		{
			float remaining = Mathf.Max(0f, m_Timer.Remaining);
			
			var uiText = m_UiGamePopup.GetTextTime();
			uiText.text = remaining.ToString("0.0");
			
			// bool urgent = remaining <= 5f && m_GameFlow.State == GameFlowState.Playing;
			// m_TimerText.color = urgent ? new Color(1f, 0.4f, 0.43f) : Color.white;
			// m_TimerCard.color = urgent ? new Color(0.36f, 0.08f, 0.16f, 0.96f) : PanelColor;
		}
		
		private void RefillTargets()
		{
			int desired = m_FeverRemaining > 0f ? mConfig.feverTargetCount : mConfig.initialTargetCount;
			while (m_ActiveTargets.Count < desired)
			{
				SpawnTarget();
			}
		}
		
		// ReSharper disable Unity.PerformanceAnalysis
		private void SpawnTarget()
		{
			GameObject instance = mTargetPool.Spawn(GetSpawnPosition(), Quaternion.identity);
			if (!instance)
			{
				return;
			}

			MiniGameTarget target = instance.GetComponent<MiniGameTarget>();
			if (!target)
			{
				Debug.LogError("The target prefab needs MiniGameTarget.", instance);
				mTargetPool.Despawn(instance);
				return;
			}

			TapTargetType type = ChooseTargetType();
			float progress = 1f - Mathf.Clamp01(m_Timer.Remaining / Mathf.Max(0.1f, mConfig.roundDuration));
			float lifetime = Mathf.Lerp(mConfig.startingTargetLifetime, mConfig.minimumTargetLifetime, progress);
			if (type == TapTargetType.Quick)
			{
				lifetime *= 0.62f;
			}

			float scale = Mathf.Lerp(0.82f, mConfig.minimumTargetScale, progress);
			Color color = type == TapTargetType.Quick ? TargetColors[1]
				: type == TapTargetType.TimeBonus ? new Color(1f, 0.78f, 0.2f)
				: type == TapTargetType.Bomb ? new Color(1f, 0.2f, 0.3f)
				: TargetColors[m_Hits % TargetColors.Length];
			target.SetVisual(m_TargetSprite, color);
			target.Bind(type, lifetime, scale, HandleTargetTapped, HandleTargetMissed);
			m_ActiveTargets.Add(target);
		}
		
		private TapTargetType ChooseTargetType()
		{
			float elapsed = mConfig.roundDuration - m_Timer.Remaining;
			float roll = Random.value;
			if (elapsed >= 15f && roll < 0.12f) return TapTargetType.Bomb;
			if (elapsed >= 8f && roll < 0.32f) return TapTargetType.Quick;
			if (roll < 0.39f) return TapTargetType.TimeBonus;
			return TapTargetType.Normal;
		}
		
		        private void HandleTargetTapped(MiniGameTarget target)
        {
            if (mGameFlow.State != GameFlowState.Playing)
            {
                return;
            }

            if (target.Type == TapTargetType.Bomb)
            {
                m_BombsTapped++;
                m_Combo = 0;
                m_Timer.AddTime(-2f);
                // m_ComboText.text = "BOMB!  -2.0 SEC";
                mTargetPool.Despawn(target.gameObject);
                m_ActiveTargets.Remove(target);
                RefillTargets();
                return;
            }

            m_Hits++;
            m_Combo++;
            m_MaxCombo = Mathf.Max(m_MaxCombo, m_Combo);
            int baseScore = target.Type == TapTargetType.Quick ? 3
                : target.Type == TapTargetType.TimeBonus ? 2
                : mConfig.scorePerTap;
            int multiplier = m_Combo >= 20 ? 3 : m_Combo >= 5 ? 2 : 1;
            m_Score += baseScore * multiplier;
            if (target.Type == TapTargetType.TimeBonus)
            {
                m_Timer.AddTime(1f);
            }

            if (!m_FeverTriggered && m_Combo >= mConfig.feverCombo)
            {
                m_FeverTriggered = true;
                m_FeverRemaining = mConfig.feverDuration;
            }
            m_ScoreText.text = m_Score.ToString("00");
            // m_ComboText.text = m_FeverRemaining > 0f ? "FEVER!  x" + multiplier + "  •  STREAK " + m_Combo
            //     : target.Type == TapTargetType.TimeBonus ? "+1.0 SEC  •  STREAK " + m_Combo
            //     : "STREAK " + m_Combo + "  •  SCORE x" + multiplier;
            mTargetPool.Despawn(target.gameObject);
            m_ActiveTargets.Remove(target);
            RefillTargets();
        }

        private void HandleTargetMissed(MiniGameTarget target)
        {
            if (mGameFlow.State != GameFlowState.Playing)
            {
                return;
            }

            if (target.Type != TapTargetType.Bomb)
            {
                m_Misses++;
                m_Combo = 0;
                // m_ComboText.text = "MISSED  •  STREAK LOST";
            }

            mTargetPool.Despawn(target.gameObject);
            m_ActiveTargets.Remove(target);
            RefillTargets();
        }
		
		private Vector3 GetSpawnPosition()
		{
			var halfHeight = m_MainCamera.orthographicSize;
			var halfWidth = halfHeight * Mathf.Max(0.55f, m_MainCamera.aspect);
			var minX = -Mathf.Max(1.15f, halfWidth - 0.7f);
			var maxX = Mathf.Max(1.15f, halfWidth - 0.7f);
			var minY = -halfHeight + 1.45f;
			var maxY = halfHeight - 2.35f;
			
			return new Vector3(
				Random.Range(minX, maxX),
				Random.Range(Mathf.Min(minY, maxY - 0.5f), maxY),
				0f);
		}
		
		// ReSharper disable Unity.PerformanceAnalysis
		private void HandleTimerCompleted()
		{
			mGameFlow.FinishGame();
		}
		
		private void HandleFlowStateChanged(GameFlowState state)
		{
			var isReady = state == GameFlowState.Ready;
			var isPlaying = state == GameFlowState.Playing;
			var isResult = state == GameFlowState.Result;

			
			m_StatusText.text = isReady
				? "READY  •  TAP START TO BEGIN"
				: isPlaying
					? "TAP THE GLOWING TARGETS"
					: "ROUND COMPLETE";
			m_StatusText.color = isResult ? new Color(1f, 0.78f, 0.38f) : Color.white;
			m_StartButton.gameObject.SetActive(isReady);
			// m_RetryButton.gameObject.SetActive(isResult);
			// m_ResultPanel.gameObject.SetActive(isResult);
			// m_HintText.gameObject.SetActive(!isResult);

			if (isReady)
			{
				m_Timer.Stop();
				m_TimerText.text = mConfig.roundDuration.ToString("0.0");
				// m_TimerCard.color = PanelColor;
				// m_ComboText.text = "STREAK x0";
				ClearTargets();
			}
			else if (isPlaying)
			{
				ClearTargets();
				for (int i = 0; i < mConfig.initialTargetCount; i++)
				{
					SpawnTarget();
				}
			}
			else if (isResult)
			{
				m_Timer.Stop();
				ClearTargets();
				bestScore = Mathf.Max(bestScore, m_Score);
				// PlayerPrefs.SetInt(BestScoreKey, m_BestScore);
				// PlayerPrefs.Save();
				m_BestText.text = bestScore.ToString("00");
				// m_ResultText.text = "SCORE  " + m_Score.ToString("00")
				//                               + "\nBEST  " + m_BestScore.ToString("00")
				//                               + "\nMAX STREAK  " + m_MaxCombo
				//                               + "\nACCURACY  " + GetAccuracy().ToString("0") + "%"
				//                               + "\nGRADE  " + GetGrade();
			}
		}
		
		private void ClearTargets()
		{
			mTargetPool.DespawnAll();
			m_ActiveTargets.Clear();
		}
		
		public void StartRound()
		{
			if (mGameFlow.State != GameFlowState.Ready)
			{
				return;
			}

			m_Score = 0;
			m_Combo = 0;
			m_Hits = 0;
			m_MaxCombo = 0;
			m_Misses = 0;
			m_BombsTapped = 0;
			m_FeverRemaining = 0f;
			m_FeverTriggered = false;
			m_ScoreText.text = "00";
			// m_ComboText.text = "STREAK x0";
			// m_ResultText.text = string.Empty;
			mGameFlow.StartGame();
			m_Timer.Start(mConfig.roundDuration);
			UpdateTimerVisual();
		}

		private void RetryRound()
		{
			if (mGameFlow.State != GameFlowState.Result)
			{
				return;
			}

			mGameFlow.Retry();
		}
	}
}
