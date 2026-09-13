using System.Collections.Generic;
using _01.Scripts.Game;
using _01.Scripts.UI;
using SWGUnity2DCore.Pool;
using _01.Scripts.UI.Popup;
using SWGUnity2DCore;
using SWGUnity2DCore.Manager;
using SWGUnity2DCore.Scene;
using SWGUnity2DCore.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace _01.Scripts.Scene
{
	public class GameScene : BaseScene
	{
		private const string BestScoreKey = "MiniGameKit.TapGame.BestScore";
		
		private static readonly Color[] TargetColors =
		{
			new Color(0.24f, 0.83f, 1f),
			new Color(0.66f, 0.42f, 1f),
			new Color(1f, 0.38f, 0.68f),
			new Color(0.32f, 1f, 0.72f)
		};
		
		private readonly CountdownTimer m_Timer = new();
		private readonly List<CircleTarget> m_ActiveTargets = new();

		[SerializeField] private Collider2D mSpawnArea;
		[SerializeField, Min(0f)] private float mSpawnMargin = 0.5f;
		
		[SerializeField]
		private GameFlow mGameFlow;
		
		[SerializeField]
		private GameObjectPool mTargetPool;
		
		[SerializeField]
		private GameConfig mConfig;
		
		private UI_GamePopup m_UiGamePopup;
		
		private Camera m_MainCamera;
		private ResponsiveGameViewport m_Viewport;
		private W01AdsManager m_AdsManager;
		private bool m_AdsAttached;
		private bool m_RoundInProgress;
		private Color m_OriginalCameraColor;
		private int m_LastCountdownSecond = -1;
		private bool m_OwnsConfig;
		private TapFeedback m_TapFeedback;
		private bool m_ApplicationPaused;
		private bool m_ApplicationFocused = true;
		public bool IsHelpOpen { get; private set; }
		public bool IsNicknamePromptOpen { get; private set; }
		public bool IsGameplayPaused => IsHelpOpen || IsNicknamePromptOpen || m_ApplicationPaused || !m_ApplicationFocused;
		public GameConfig Config => mConfig;
		
		private Sprite m_TargetSprite;
		
		private TextMeshProUGUI m_BestText;
		private TextMeshProUGUI m_ScoreText;
		private TextMeshProUGUI m_TimerText;

		private TextMeshProUGUI m_ComboText;
		private TextMeshProUGUI m_ResultText;
		
		private Button m_StartButton;
		private Button m_RetryButton;
		
		private int m_Score;
		[HideInInspector] public int bestScore;
		private int m_Combo;
		private int m_Hits;
		private int m_MaxCombo;
		private int m_Misses;
		private int m_BombsTapped;
		private float m_FeverRemaining;
		private int m_NextFeverCombo;
		
		
		protected override bool Init()
		{
			if (!base.Init())
				return false;

			InitGameScene();
			return true;
		}

		private void InitGameScene()
		{
			sceneType = _01.Scripts.Scene.W01SceneType.Game;
			m_UiGamePopup = Managers.UI.ShowPopupUI<UI_GamePopup>();
			m_UiGamePopup.Initialize();
			
			m_TargetSprite = CreateCircleSprite(96);
			m_TapFeedback = new GameObject("Touch Feedback").AddComponent<TapFeedback>();
			m_TapFeedback.transform.SetParent(transform, false);
			m_TapFeedback.Initialize(m_TargetSprite);
			
			EnsureSceneServices();
			m_Viewport = m_MainCamera.GetComponent<ResponsiveGameViewport>();
			if (m_Viewport == null) m_Viewport = m_MainCamera.gameObject.AddComponent<ResponsiveGameViewport>();
			m_OriginalCameraColor = m_MainCamera.backgroundColor;
			m_AdsManager = Managers.Ads;
			m_AdsManager.BannerHeightChanged += ApplyBannerSpace;
			m_AdsAttached = true;
			m_AdsManager.EnterGameScene();
			EnsureConfig();
			SetupPool();
			
			m_BestText = m_UiGamePopup.GetTextBest();
			m_TimerText = m_UiGamePopup.GetTextTime();
			m_ScoreText = m_UiGamePopup.GetTextScore();
			m_ComboText = m_UiGamePopup.GetTextCombo();
			m_ResultText = m_UiGamePopup.GetTextResult();
			
			bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
			m_BestText.text = bestScore.ToString("00");
			

			m_StartButton = m_UiGamePopup.GetButtonStart();
			m_RetryButton = m_UiGamePopup.GetButtonRetry();

			m_Timer.Completed += HandleTimerCompleted;
			mGameFlow.StateChanged += HandleFlowStateChanged;
			m_ComboText.text = GameLocalization.T("STREAK x0", "연속 터치 x0");
				
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

			m_MainCamera = Camera.main;
			if (m_MainCamera == null)
			{
				GameObject cameraObject = new GameObject("MainCamera");
				m_MainCamera = cameraObject.AddComponent<Camera>();
				cameraObject.tag = "MainCamera";
			}
		}

		private void EnsureConfig()
		{
			if (mConfig == null)
			{
				mConfig = ScriptableObject.CreateInstance<GameConfig>();
				m_OwnsConfig = true;
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
			glowRenderer.sortingOrder = 99;

			SpriteRenderer renderer = target.AddComponent<SpriteRenderer>();
			renderer.sprite = m_TargetSprite;
			renderer.color = TargetColors[0];
			renderer.sortingOrder = 100;

			CircleCollider2D collider = target.AddComponent<CircleCollider2D>();
			collider.radius = 0.5f;
			target.AddComponent<CircleTarget>();
			target.SetActive(false);
			return target;
		}
		
		private void Update()
		{
			if (IsGameplayPaused || mGameFlow.State != GameFlowState.Playing)
			{
				return;
			}
			
			m_Timer.Tick(Time.deltaTime);
			// Tick can finish the round synchronously. Do not run gameplay visuals after Result cleanup.
			if (mGameFlow.State != GameFlowState.Playing) return;
			UpdateFever();
			UpdateTimerVisual();
		}
		
		private void OnDestroy()
		{
			if (m_AdsAttached && m_AdsManager != null)
			{
				m_AdsManager.BannerHeightChanged -= ApplyBannerSpace;
				m_AdsManager.ExitGameScene();
			}
			m_AdsAttached = false;
			m_AdsManager = null;
			if (mGameFlow != null)
			{
				mGameFlow.StateChanged -= HandleFlowStateChanged;
			}

			m_Timer.Completed -= HandleTimerCompleted;
			if (m_TargetSprite != null)
			{
				Destroy(m_TargetSprite.texture);
				Destroy(m_TargetSprite);
			}
			if (m_OwnsConfig && mConfig != null) Destroy(mConfig);
		}

		private void ApplyBannerSpace(float pixels)
		{
			if (m_Viewport != null) m_Viewport.SetBannerTop(pixels);
		}
		
		private void UpdateFever()
		{
			if (m_FeverRemaining <= 0f)
			{
				return;
			}

			m_FeverRemaining = Mathf.Max(0f, m_FeverRemaining - Time.deltaTime);
			float normalized = Mathf.Clamp01(m_FeverRemaining / Mathf.Max(0.1f, mConfig.feverDuration));
			float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f);
			m_MainCamera.backgroundColor = Color.Lerp(m_OriginalCameraColor,
				new Color(0.16f, 0.015f, 0.26f), 0.3f + pulse * 0.25f);
			m_UiGamePopup.SetFeverPresentation(true, Mathf.Max(normalized, pulse));

			
			m_UiGamePopup.SetFeverTime(m_FeverRemaining);
			
			if (m_FeverRemaining <= 0f)
			{
				EndFever();
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
			
			bool urgent = remaining > 0f && remaining <= 5f;
			m_TimerText.color = urgent ? new Color(1f, 0.43f, 0.45f) : Color.white;
			int second = Mathf.CeilToInt(remaining);
			if (urgent && second != m_LastCountdownSecond)
				Managers.Sound.Play(Define.Sound.Effect, "SFX/Timer_Tick", 0.27f);
			m_LastCountdownSecond = second;
		}
		
		private void RefillTargets()
		{
			// A bomb can end the round inside its tap callback before that callback reaches here.
			if (mGameFlow.State != GameFlowState.Playing) return;
			int desired = m_FeverRemaining > 0f ? mConfig.feverTargetCount : mConfig.initialTargetCount;
			while (m_ActiveTargets.Count < desired)
			{
				if (!SpawnTarget()) break;
			}
		}
		
		// ReSharper disable Unity.PerformanceAnalysis
		private bool SpawnTarget()
		{
			if (mGameFlow.State != GameFlowState.Playing) return false;
			GameObject instance = mTargetPool.Spawn(GetSpawnPosition(), Quaternion.identity);
			if (!instance)
			{
				return false;
			}

			CircleTarget target = instance.GetComponent<CircleTarget>();
			if (!target)
			{
				Debug.LogError("The target prefab needs CircleTarget.", instance);
				mTargetPool.Despawn(instance);
				return false;
			}

			TapTargetType type = ChooseTargetType();
			float progress = 1f - Mathf.Clamp01(m_Timer.Remaining / Mathf.Max(0.1f, mConfig.roundDuration));
			float lifetime = Mathf.Lerp(mConfig.startingTargetLifetime, mConfig.minimumTargetLifetime, progress);
			if (m_FeverRemaining > 0f) lifetime *= mConfig.feverTargetLifetimeMultiplier;
			if (type == TapTargetType.Quick)
			{
				lifetime *= 0.62f;
			}

			float scale = Mathf.Lerp(0.82f, mConfig.minimumTargetScale, progress);
			Color color = type == TapTargetType.Quick ? TargetColors[1]
				: type == TapTargetType.TimeBonus ? new Color(1f, 0.78f, 0.2f)
				: type == TapTargetType.Bomb ? new Color(1f, 0.2f, 0.3f)
				: TargetColors[m_Hits % TargetColors.Length];
			
			var sprite = target.GetSprite(type);
			target.SetVisual(sprite, color);
			
			target.Bind(type, lifetime, scale, HandleTargetTapped, HandleTargetMissed);
			target.SetFeverMode(m_FeverRemaining > 0f);
			target.SetPaused(IsGameplayPaused);
			m_ActiveTargets.Add(target);
			return true;
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
		
		public void SetHelpOpen(bool open)
		{
			IsHelpOpen = open;
			ApplyPauseState();
		}

		public void SetNicknamePromptOpen(bool open)
		{
			IsNicknamePromptOpen = open;
			ApplyPauseState();
		}

		private void OnApplicationPause(bool paused)
		{
			m_ApplicationPaused = paused;
			ApplyPauseState();
		}

		private void OnApplicationFocus(bool focused)
		{
			// Switching Editor panels must not pause local Game View QA.
			if (Application.isEditor) return;
			m_ApplicationFocused = focused;
			ApplyPauseState();
		}

		private void ApplyPauseState()
		{
			// Each reason owns its pause; clearing one must not clear the others.
			bool paused = IsGameplayPaused;
			foreach (CircleTarget target in m_ActiveTargets)
				if (target != null) target.SetPaused(paused);
			m_TapFeedback?.SetPaused(paused);
		}

		private void HandleTargetTapped(CircleTarget target)
        {
            if (IsGameplayPaused || mGameFlow.State != GameFlowState.Playing)
            {
                return;
            }

            m_TapFeedback.Show(target, m_FeverRemaining > 0f);
            if (target.Type == TapTargetType.Bomb)
            {
				m_BombsTapped++;
				m_Combo = 0;
				m_NextFeverCombo = mConfig.feverCombo;
                m_Timer.AddTime(-2f);
				// AddTime can complete the round and clear every target synchronously.
				if (mGameFlow.State != GameFlowState.Playing) return;
				m_ComboText.text = GameLocalization.T("BOMB!  -2.0 SEC", "폭탄!  -2.0초");
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
            int multiplier = GetScoreMultiplier();
            m_Score += baseScore * multiplier;
            if (target.Type == TapTargetType.TimeBonus)
            {
                m_Timer.AddTime(1f);
            }

			if (m_FeverRemaining > 0f)
			{
				m_FeverRemaining = Mathf.Min(mConfig.feverMaximumDuration,
					m_FeverRemaining + mConfig.feverBonusTimePerTap);
			}
			else if (m_Combo >= m_NextFeverCombo)
			{
				StartFever();
			}
            m_ScoreText.text = m_Score.ToString("00");
			UpdateComboText(target.Type == TapTargetType.TimeBonus);
            mTargetPool.Despawn(target.gameObject);
            m_ActiveTargets.Remove(target);
            RefillTargets();
        }

		private int GetScoreMultiplier() => m_FeverRemaining > 0f
			? Mathf.Max(2, mConfig.feverScoreMultiplier)
			: m_Combo >= 20 ? 3 : m_Combo >= 5 ? 2 : 1;

		private void UpdateComboText(bool timeBonus = false)
		{
			int multiplier = GetScoreMultiplier();
			m_ComboText.text = m_FeverRemaining > 0f
				? GameLocalization.T("FEVER!  x", "피버!  x") + multiplier + GameLocalization.T("  •  STREAK ", "  •  연속 터치 ") + m_Combo
				: timeBonus
					? GameLocalization.T("+1.0 SEC  •  STREAK ", "+1.0초  •  연속 터치 ") + m_Combo
					: GameLocalization.T("STREAK ", "연속 터치 ") + m_Combo + GameLocalization.T("  •  SCORE x", "  •  점수 x") + multiplier;
		}

		private void HandleTargetMissed(CircleTarget target)
        {
            if (IsGameplayPaused || mGameFlow.State != GameFlowState.Playing)
            {
                return;
            }

            if (target.Type != TapTargetType.Bomb)
            {
				if (m_Combo > 0) Managers.Sound.Play(Define.Sound.Effect, "SFX/Target_Miss", 0.32f);
				m_Misses++;
				m_Combo = 0;
				m_NextFeverCombo = mConfig.feverCombo;
				m_ComboText.text = GameLocalization.T("MISSED  •  STREAK LOST", "놓침  •  연속 터치 초기화");
            }

            mTargetPool.Despawn(target.gameObject);
            m_ActiveTargets.Remove(target);
            RefillTargets();
        }
		
		private Vector3 GetSpawnPosition()
		{
			// Fever targets must remain distinguishable, especially when one is a bomb.
			float spacing = Mathf.Max(0.82f, mConfig.minimumTargetScale) * 1.65f;
			Vector3 best = Vector3.zero;
			float bestDistance = -1f;
			for (int attempt = 0; attempt < 32; attempt++)
			{
				Vector3 candidate = SampleSpawnPosition();
				float nearest = float.PositiveInfinity;
				foreach (CircleTarget active in m_ActiveTargets)
					if (active != null) nearest = Mathf.Min(nearest, (candidate - active.transform.position).sqrMagnitude);
				if (nearest >= spacing * spacing) return candidate;
				if (nearest > bestDistance) { best = candidate; bestDistance = nearest; }
			}
			// A very small/custom spawn area can be unable to fit all configured targets.
			return best;
		}

		private Vector3 SampleSpawnPosition()
		{
			if (mSpawnArea != null)
			{
				Vector2 point = mSpawnArea.GetRandomPointInsideWorld(mSpawnMargin);
				return new Vector3(point.x, point.y, 0f);
			}

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

			
			m_UiGamePopup.SetStatus(isReady
				? GameLocalization.T("READY  •  TAP START TO BEGIN", "준비  •  시작 버튼을 누르세요")
				: isPlaying
					? GameLocalization.T("TAP THE GLOWING TARGETS", "빛나는 타겟을 터치하세요")
					: GameLocalization.T("ROUND COMPLETE", "게임 종료"),
				isResult ? new Color(1f, 0.78f, 0.38f) : Color.white);
			m_StartButton.gameObject.SetActive(isReady);
			m_RetryButton.gameObject.SetActive(isResult);
			// m_ResultPanel.gameObject.SetActive(isResult);
			// m_HintText.gameObject.SetActive(!isResult);

			if (isReady)
			{
				EndFever(false);
				Managers.Sound.Play(Define.Sound.Bgm, "BGM/Game_NeonLobby", 0.38f);
				m_Timer.Stop();
				m_TimerText.text = mConfig.roundDuration.ToString("0.0");
				m_TimerText.color = Color.white;
				// m_TimerCard.color = PanelColor;
				m_ComboText.text = GameLocalization.T("STREAK x0", "연속 터치 x0");
				ClearTargets();
			}
			else if (isPlaying)
			{
				m_RoundInProgress = true;
				Managers.Sound.Play(Define.Sound.Bgm, "BGM/Gameplay_NeonRush", 0.32f);
				Managers.Sound.Play(Define.Sound.Effect, "SFX/Round_Start", 0.58f);
				ClearTargets();
				for (int i = 0; i < mConfig.initialTargetCount; i++)
				{
					SpawnTarget();
				}
			}
			else if (isResult)
			{
				EndFever(false);
				Managers.Sound.Play(Define.Sound.Bgm, "BGM/Game_NeonLobby", 0.38f);
				m_Timer.Stop();
				m_TimerText.text = Mathf.Max(0f, m_Timer.Remaining).ToString("0.0");
				m_TimerText.color = Color.white;
				m_ComboText.text = string.Empty;
				ClearTargets();
				bool isNewPersonalBest = m_Score > bestScore;
				Managers.Sound.Play(Define.Sound.Effect,
					isNewPersonalBest ? "SFX/New_Best" : "SFX/Round_Complete", 0.62f);
				bestScore = Mathf.Max(bestScore, m_Score);
				PlayerPrefs.SetInt(BestScoreKey, bestScore);
				PlayerPrefs.Save();
				m_BestText.text = bestScore.ToString("00");
				m_ResultText.text = GameLocalization.T("SCORE  ", "점수  ") + m_Score.ToString("00")
				                              + GameLocalization.T("\nBEST  ", "\n최고 기록  ") + bestScore.ToString("00")
				                              + GameLocalization.T("\nMAX STREAK  ", "\n최대 연속 터치  ") + m_MaxCombo
				                              + GameLocalization.T("\nACCURACY  ", "\n정확도  ") + GetAccuracy().ToString("0") + "%"
				                              + GameLocalization.T("\nGRADE  ", "\n등급  ") + GetGrade();

				if (isNewPersonalBest)
				{
					RegisterNewPersonalBest(m_Score);
				}
				if (m_RoundInProgress)
				{
					m_RoundInProgress = false;
					m_AdsManager?.RecordCompletedRound();
				}
			}
		}

		private async void RegisterNewPersonalBest(int score)
		{
			try
			{
				await Managers.Rank.GetPlayerIdAsync();
				const string defaultNickname = "NONAME";

				if (this == null || m_UiGamePopup == null) return;
				m_UiGamePopup.ShowNicknamePrompt(defaultNickname, nickname =>
				{
					string registeredName = string.IsNullOrWhiteSpace(nickname) ? defaultNickname : nickname.Trim();
					Managers.Rank.SetProfile(registeredName, string.Empty, string.Empty);
					Managers.Rank.SubmitScore(score);
				});
			}
			catch (System.Exception exception)
			{
				Debug.LogError("최고 기록 등록 준비 오류: " + exception.Message);
			}
		}
		
		private void ClearTargets()
		{
			mTargetPool.DespawnAll();
			m_ActiveTargets.Clear();
		}
		
		public void StartRound()
		{
			if (IsGameplayPaused) return;
			if (m_AdsManager != null && m_AdsManager.IsShowingInterstitial) return;
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
			m_LastCountdownSecond = -1;
			m_FeverRemaining = 0f;
			m_NextFeverCombo = mConfig.feverCombo;
			m_ScoreText.text = "00";
			m_ComboText.text = GameLocalization.T("STREAK x0", "연속 터치 x0");
			m_ResultText.text = string.Empty;
			m_Timer.Start(mConfig.roundDuration);
			mGameFlow.StartGame();
			UpdateTimerVisual();
		}

		private void StartFever()
		{
			m_FeverRemaining = mConfig.feverDuration;
			m_NextFeverCombo = m_Combo + Mathf.Max(2, mConfig.feverCombo);
			Managers.Sound.Play(Define.Sound.Effect, "SFX/Fever_Start", 0.40f);
			m_UiGamePopup.SetStatus(GameLocalization.T("FEVER MODE! KEEP TAPPING!", "피버 모드! 계속 터치하세요!"),
				new Color(0.95f, 0.65f, 1f));
			foreach (CircleTarget activeTarget in m_ActiveTargets)
				activeTarget.SetFeverMode(true);
			RefillTargets();
		}

		private void EndFever(bool playSound = true)
		{
			bool wasActive = m_FeverRemaining > 0f ||
				(m_UiGamePopup != null && m_MainCamera != null && m_MainCamera.backgroundColor != m_OriginalCameraColor);
			m_FeverRemaining = 0f;
			if (m_MainCamera != null) m_MainCamera.backgroundColor = m_OriginalCameraColor;
			if (m_UiGamePopup != null) m_UiGamePopup.SetFeverPresentation(false);
			foreach (CircleTarget activeTarget in m_ActiveTargets)
				activeTarget.SetFeverMode(false);
			if (wasActive && mGameFlow != null && mGameFlow.State == GameFlowState.Playing)
			{
				m_UiGamePopup.SetStatus(GameLocalization.T("TAP THE GLOWING TARGETS", "빛나는 타겟을 터치하세요"), Color.white);
				UpdateComboText();
			}
			if (wasActive && playSound) Managers.Sound.Play(Define.Sound.Effect, "SFX/Fever_End", 0.43f);
		}

		public void RetryRound()
		{
			if (IsGameplayPaused) return;
			if (m_AdsManager != null && m_AdsManager.IsShowingInterstitial) return;
			if (mGameFlow.State != GameFlowState.Result)
			{
				return;
			}

			mGameFlow.Retry();
		}
		
		private static Sprite CreateCircleSprite(int size)
		{
			Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
			texture.name = "PrototypeTargetCircle";
			texture.filterMode = FilterMode.Bilinear;
			texture.wrapMode = TextureWrapMode.Clamp;
			Color[] pixels = new Color[size * size];
			float center = (size - 1) * 0.5f;
			float radius = center - 1f;
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
					float alpha = Mathf.Clamp01(radius + 1f - distance);
					pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
				}
			}

			texture.SetPixels(pixels);
			texture.Apply(false, true);
			return Sprite.Create(texture, new Rect(0f, 0f, size, size),
				new Vector2(0.5f, 0.5f), size);
		}
		
		private float GetAccuracy()
		{
			int attempts = m_Hits + m_Misses + m_BombsTapped;
			return attempts > 0 ? m_Hits * 100f / attempts : 0f;
		}
		
		private string GetGrade()
		{
			float accuracy = GetAccuracy();
			if (m_Score >= 70 && accuracy >= 92f) return "S";
			if (m_Score >= 50 && accuracy >= 85f) return "A";
			if (m_Score >= 30 && accuracy >= 72f) return "B";
			return m_Score >= 15 ? "C" : "D";
		}
	}
}
