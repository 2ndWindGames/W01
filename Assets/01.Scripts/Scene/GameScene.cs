using System.Collections;
using System.Collections.Generic;
using _01.Scripts.Game;
using _01.Scripts.Manager;
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
		private const float SmallTargetScale = 0.64f;
		private const float MediumTargetScale = 0.82f;
		private const float LargeTargetScale = 1.00f;
#if UNITY_EDITOR
		public static bool SuppressRecordPersistenceForQa { get; set; }
#endif
		
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
		private GameplayComboMusic m_ComboMusic;
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

		private TextMeshProUGUI m_ResultText;
		private Coroutine m_NewBestPromptRoutine;
		
		private Button m_StartButton;
		private Button m_RetryButton;
		
		private int m_Score;
		[HideInInspector] public int bestScore;
		private int m_Combo;
		private int m_LastComboMusicTier = -1;
		private int m_Hits;
		private int m_MaxCombo;
		private int m_Misses;
		private int m_BombsTapped;
		private float m_FeverRemaining;
		private int m_NextFeverCombo;
		private float m_RoundElapsed;
		private int m_PaceStage = 1;
		private bool m_SequenceActive;
		private int m_SequenceNextOrder;
		private float m_NextSequenceAt = float.PositiveInfinity;
		private float m_PhaseCueRemaining;
		
		
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
			m_ComboMusic = GetComponent<GameplayComboMusic>();
			if (m_ComboMusic == null) m_ComboMusic = gameObject.AddComponent<GameplayComboMusic>();
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
			m_ResultText = m_UiGamePopup.GetTextResult();
			
			bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
			m_BestText.text = bestScore.ToString("00");
			

			m_StartButton = m_UiGamePopup.GetButtonStart();
			m_RetryButton = m_UiGamePopup.GetButtonRetry();

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
			if (m_PhaseCueRemaining > 0f)
			{
				m_PhaseCueRemaining = Mathf.Max(0f, m_PhaseCueRemaining - Time.deltaTime);
				if (m_PhaseCueRemaining <= 0f) CompletePhaseCue();
				return;
			}
			
			m_RoundElapsed += Time.deltaTime;
			m_Timer.Tick(Time.deltaTime);
			// Tick can finish the round synchronously. Do not run gameplay visuals after Result cleanup.
			if (mGameFlow.State != GameFlowState.Playing) return;
			UpdateFever();
			UpdateTimerVisual();
			int stage = RoundPacing.Stage(m_RoundElapsed, mConfig);
			if (stage > m_PaceStage)
			{
				m_PaceStage = stage;
				BeginPhaseCue(stage);
				Managers.Sound.Play(Define.Sound.Effect, "SFX/Speed_Up", .45f);
				return;
			}
			if (m_PaceStage == 3 && !m_SequenceActive && m_RoundElapsed >= m_NextSequenceAt
			    && m_Timer.Remaining > mConfig.sequenceLifetimeSeconds + .5f)
				StartSequence();
			RefreshComboPresentation();
		}

		private void BeginPhaseCue(int stage)
		{
			ClearTargets();
			m_PhaseCueRemaining = stage == 1 ? 1.4f : 1.75f;
			m_UiGamePopup.ShowSignalPhase(stage);
		}

		private void CompletePhaseCue()
		{
			m_PhaseCueRemaining = 0f;
			if (mGameFlow.State != GameFlowState.Playing) return;
			if (m_PaceStage == 3)
			{
				StartSequence();
				return;
			}
			RefillTargets();
		}
		
		private void OnDestroy()
		{
			if (m_LastComboMusicTier > 0 && m_ComboMusic != null) m_ComboMusic.SetTier(0);
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
				while (!m_SequenceActive && m_ActiveTargets.Count > DesiredTargetCount())
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
			// A sequence owns all three targets until it is completed or broken.
			if (mGameFlow.State != GameFlowState.Playing || m_SequenceActive || m_PhaseCueRemaining > 0f) return;
			int desired = DesiredTargetCount();
			while (m_ActiveTargets.Count < desired)
			{
				if (!SpawnTarget()) break;
			}
		}

		private int DesiredTargetCount()
		{
			int phaseCount = m_PaceStage >= 2 ? Mathf.Max(2, mConfig.initialTargetCount) : mConfig.initialTargetCount;
			return m_FeverRemaining > 0f ? Mathf.Max(phaseCount + 1, mConfig.feverTargetCount) : phaseCount;
		}
		
		// ReSharper disable Unity.PerformanceAnalysis
		private bool SpawnTarget() => SpawnTarget(null, 0, 0f);

		private bool SpawnTarget(TapTargetType? forcedType, int sequenceOrder, float lifetimeOverride)
			=> SpawnTargetWithSize(forcedType, sequenceOrder, lifetimeOverride, null);

		// The explicit tier also lets editor QA exercise each reward deterministically.
		private bool SpawnTargetWithSize(TapTargetType? forcedType, int sequenceOrder,
			float lifetimeOverride, TargetSizeTier? forcedSizeTier)
		{
			if (mGameFlow.State != GameFlowState.Playing) return false;
			TapTargetType type = forcedType ?? ChooseTargetType();
			TargetSizeTier sizeTier = type == TapTargetType.Bomb ? TargetSizeTier.Medium
				: forcedSizeTier ?? (TargetSizeTier)Random.Range(0, 3);
			float scale = ScaleForSize(sizeTier);
			if (!TryGetSpawnPosition(scale, out Vector3 spawnPosition)) return false;
			GameObject instance = mTargetPool.Spawn(spawnPosition, Quaternion.identity);
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

			float progress = RoundPacing.Progress(m_RoundElapsed, mConfig);
			float lifetime = lifetimeOverride > 0f ? lifetimeOverride
				: RoundPacing.Lifetime(m_RoundElapsed, type, m_FeverRemaining > 0f, mConfig);

			Color color = type == TapTargetType.Quick ? TargetColors[1]
				: type == TapTargetType.TimeBonus ? new Color(1f, 0.78f, 0.2f)
				: type == TapTargetType.Bomb ? new Color(1f, 0.2f, 0.3f)
				: TargetColors[m_Hits % TargetColors.Length];
			
			var sprite = target.GetSprite(type);
			target.SetVisual(sprite, color);
			
			target.Bind(type, lifetime, scale, HandleTargetTapped, HandleTargetMissed, sizeTier);
			if (sequenceOrder > 0)
				target.SetSequenceOrder(sequenceOrder, m_UiGamePopup.GetTextStatus().font);
			target.SetPace(Mathf.Lerp(1f, 1.65f, progress));
			target.SetFeverMode(m_FeverRemaining > 0f);
			target.SetPaused(IsGameplayPaused);
			m_ActiveTargets.Add(target);
			// A pooled target can be tapped again before the next physics step.
			// Publish its new collider position before another pointer is raycast.
			Physics2D.SyncTransforms();
			return true;
		}

		private static float ScaleForSize(TargetSizeTier tier) => tier == TargetSizeTier.Small
			? SmallTargetScale : tier == TargetSizeTier.Large ? LargeTargetScale : MediumTargetScale;

		private static int ScoreSizeBonus(TargetSizeTier tier) => tier == TargetSizeTier.Small ? 2
			: tier == TargetSizeTier.Medium ? 1 : 0;

		private static int TimeSizeBonus(TargetSizeTier tier) => tier == TargetSizeTier.Small ? 3
			: tier == TargetSizeTier.Medium ? 2 : 1;
		
		private TapTargetType ChooseTargetType()
		{
			float roll = Random.value;
			if (m_PaceStage == 1) return roll < .12f ? TapTargetType.TimeBonus : TapTargetType.Normal;
			bool hasSafeTarget = false;
			foreach (CircleTarget active in m_ActiveTargets)
				if (active != null && active.Type != TapTargetType.Bomb) { hasSafeTarget = true; break; }
			if (hasSafeTarget && roll < .14f) return TapTargetType.Bomb;
			if (roll < .36f) return TapTargetType.Quick;
			if (roll < .44f) return TapTargetType.TimeBonus;
			return TapTargetType.Normal;
		}

		private void StartSequence()
		{
			ClearTargets();
			m_SequenceActive = true;
			m_SequenceNextOrder = 1;
			for (int order = 1; order <= 3; order++)
			{
				if (SpawnTarget(TapTargetType.Normal, order, mConfig.sequenceLifetimeSeconds)) continue;
				ClearTargets();
				m_SequenceActive = false;
				m_SequenceNextOrder = 0;
				m_NextSequenceAt = m_RoundElapsed + mConfig.sequenceIntervalSeconds;
				RefillTargets();
				return;
			}
		}

		private void CompleteSequence()
		{
			m_SequenceActive = false;
			m_SequenceNextOrder = 0;
			m_NextSequenceAt = m_RoundElapsed + mConfig.sequenceIntervalSeconds;
			m_UiGamePopup.ShowSequenceComplete();
		}

		private void FailSequence(CircleTarget target, bool wrongOrder)
		{
			int lostCombo = m_Combo;
			m_TapFeedback.ShowMiss(target, lostCombo);
			m_Misses++;
			m_Combo = 0;
			m_NextFeverCombo = mConfig.feverCombo;
			EndFever(false);
			if (wrongOrder) m_UiGamePopup.ShowSequenceFailure(lostCombo);
			else m_UiGamePopup.ShowComboFailure(lostCombo, false);
			DespawnActiveTargets();
			m_NextSequenceAt = m_RoundElapsed + mConfig.sequenceIntervalSeconds;
			RefreshComboPresentation();
			RefillTargets();
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
			m_UiGamePopup?.PauseComboFeedback(paused);
		}

        private void HandleTargetTapped(CircleTarget target)
        {
            if (IsGameplayPaused || mGameFlow.State != GameFlowState.Playing)
            {
                return;
            }

            bool sequenceTap = m_SequenceActive && target.SequenceOrder > 0;
            if (sequenceTap && target.SequenceOrder != m_SequenceNextOrder)
            {
				FailSequence(target, true);
				return;
            }
            if (sequenceTap) m_SequenceNextOrder++;
            m_TapFeedback.Show(target, m_FeverRemaining > 0f);
            if (target.Type == TapTargetType.Bomb)
            {
				int lostCombo = m_Combo;
				m_BombsTapped++;
				m_Combo = 0;
				m_NextFeverCombo = mConfig.feverCombo;
				EndFever(false);
                m_Timer.AddTime(-2f);
				// AddTime can complete the round and clear every target synchronously.
				if (mGameFlow.State != GameFlowState.Playing) return;
				m_UiGamePopup.ShowComboFailure(lostCombo, true);
				RefreshComboPresentation();
                mTargetPool.Despawn(target.gameObject);
                m_ActiveTargets.Remove(target);
                RefillTargets();
                return;
            }

            m_Hits++;
            m_Combo++;
            m_MaxCombo = Mathf.Max(m_MaxCombo, m_Combo);
            int baseScore = sequenceTap ? 2 : target.Type == TapTargetType.Quick ? 3
                : target.Type == TapTargetType.TimeBonus ? 2
                : mConfig.scorePerTap;
            if (target.Type != TapTargetType.TimeBonus)
                baseScore += ScoreSizeBonus(target.SizeTier);
            int bonusSeconds = target.Type == TapTargetType.TimeBonus
                ? TimeSizeBonus(target.SizeTier) : 0;
            int multiplier = GetScoreMultiplier();
            int earnedScore = baseScore * multiplier;
            if (sequenceTap && m_SequenceNextOrder > 3) earnedScore += 5;
            m_Score += earnedScore;
            m_TapFeedback.ShowScore(target, earnedScore, bonusSeconds);
            if (target.Type == TapTargetType.TimeBonus)
            {
                m_Timer.AddTime(bonusSeconds);
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
			RefreshComboPresentation();
            mTargetPool.Despawn(target.gameObject);
            m_ActiveTargets.Remove(target);
            if (sequenceTap && m_SequenceNextOrder > 3) CompleteSequence();
            RefillTargets();
        }

		private int GetScoreMultiplier() => m_Combo >= 50 ? 3 : m_Combo >= 10 ? 2 : 1;

		private void RefreshComboPresentation()
		{
			m_UiGamePopup.UpdateComboState(m_Combo, GetScoreMultiplier(), m_NextFeverCombo,
				mConfig.feverCombo, m_FeverRemaining, mConfig.feverMaximumDuration, m_PaceStage,
				mGameFlow.State == GameFlowState.Playing);
			int musicTier = mGameFlow.State != GameFlowState.Playing ? 0
				: m_Combo >= 50 ? 2 : m_Combo >= 10 ? 1 : 0;
			if (musicTier != m_LastComboMusicTier)
			{
				m_ComboMusic?.SetTier(musicTier);
				m_LastComboMusicTier = musicTier;
			}
		}

		private void HandleTargetMissed(CircleTarget target)
        {
            if (IsGameplayPaused || mGameFlow.State != GameFlowState.Playing)
            {
                return;
            }
			if (m_SequenceActive && target.SequenceOrder > 0)
			{
				FailSequence(target, false);
				return;
			}

            if (target.Type != TapTargetType.Bomb)
            {
				int lostCombo = m_Combo;
				m_TapFeedback.ShowMiss(target, lostCombo);
				m_Misses++;
				m_Combo = 0;
				m_NextFeverCombo = mConfig.feverCombo;
				EndFever(false);
				m_UiGamePopup.ShowComboFailure(lostCombo, false);
				RefreshComboPresentation();
            }

            mTargetPool.Despawn(target.gameObject);
            m_ActiveTargets.Remove(target);
            RefillTargets();
        }
		
		private bool TryGetSpawnPosition(float targetScale, out Vector3 position)
		{
			// Pick the farthest candidate so a second target does not block the third.
			// Prefer separate glows, but allow glow fringes to meet when the field is crowded.
			Vector3 best = Vector3.zero;
			float bestClearance = -1f;
			int samples = m_ActiveTargets.Count == 0 ? 1 : 64;
			for (int attempt = 0; attempt < samples; attempt++)
			{
				Vector3 candidate = SampleSpawnPosition(targetScale);
				float nearestClearance = float.PositiveInfinity;
				foreach (CircleTarget active in m_ActiveTargets)
				{
					if (active == null) continue;
					float combinedScale = targetScale + active.NominalScale;
					float clearance = (candidate - active.transform.position).sqrMagnitude
						/ (combinedScale * combinedScale);
					nearestClearance = Mathf.Min(nearestClearance, clearance);
				}
				if (nearestClearance <= bestClearance) continue;
				best = candidate;
				bestClearance = nearestClearance;
			}
			// A target's solid face is smaller than 0.70 × the combined nominal scales.
			position = best;
			return bestClearance >= 0.70f * 0.70f;
		}

		private Vector3 SampleSpawnPosition(float targetScale)
		{
			if (mSpawnArea != null)
			{
				float margin = Mathf.Max(mSpawnMargin, targetScale * 0.9f);
				Vector2 point = mSpawnArea.GetRandomPointInsideWorld(margin);
				return new Vector3(point.x, point.y, 0f);
			}

			var halfHeight = m_MainCamera.orthographicSize;
			var halfWidth = halfHeight * Mathf.Max(0.55f, m_MainCamera.aspect);
			float edgeMargin = Mathf.Max(0.7f, targetScale * 0.9f);
			var minX = -Mathf.Max(0f, halfWidth - edgeMargin);
			var maxX = Mathf.Max(0f, halfWidth - edgeMargin);
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
			if (!isResult && m_NewBestPromptRoutine != null)
			{
				StopCoroutine(m_NewBestPromptRoutine);
				m_NewBestPromptRoutine = null;
			}
			RefreshComboPresentation();

			
			m_UiGamePopup.SetStatus(isReady
				? GameLocalization.T("READY  •  TAP START TO BEGIN", "준비  •  시작 버튼을 누르세요")
				: isPlaying
					? GameLocalization.T("TAP THE GLOWING TARGETS", "빛나는 타겟을 터치하세요")
					: GameLocalization.T("RESULT", "결과"),
				isResult ? new Color(1f, 0.78f, 0.38f) : Color.white);
			m_StartButton.gameObject.SetActive(isReady);
			m_RetryButton.gameObject.SetActive(isResult);
			m_ResultText.transform.parent.gameObject.SetActive(isResult);

			if (isReady)
			{
				EndFever(false);
				m_ComboMusic?.SetTier(0);
				m_LastComboMusicTier = 0;
				Managers.Sound.Play(Define.Sound.Bgm, "BGM/Game_NeonLobby", 0.38f);
				m_Timer.Stop();
				m_TimerText.text = mConfig.roundDuration.ToString("0.0");
				m_TimerText.color = Color.white;
				// m_TimerCard.color = PanelColor;
				ClearTargets();
			}
			else if (isPlaying)
			{
				m_RoundInProgress = true;
				Managers.Sound.Play(Define.Sound.Bgm, "BGM/Gameplay_NeonRush", 0.32f);
				m_ComboMusic?.SetTier(0);
				m_LastComboMusicTier = 0;
				Managers.Sound.Play(Define.Sound.Effect, "SFX/Round_Start", 0.58f);
				BeginPhaseCue(1);
			}
			else if (isResult)
			{
				EndFever(false);
				m_ComboMusic?.SetTier(0);
				m_LastComboMusicTier = 0;
				Managers.Sound.Play(Define.Sound.Bgm, "BGM/Game_NeonLobby", 0.38f);
				m_Timer.Stop();
				m_TimerText.text = Mathf.Max(0f, m_Timer.Remaining).ToString("0.0");
				m_TimerText.color = Color.white;
				ClearTargets();
				bool isNewPersonalBest = m_Score > bestScore;
				Managers.Sound.Play(Define.Sound.Effect,
					isNewPersonalBest ? "SFX/New_Best" : "SFX/Round_Complete", 0.62f);
				bestScore = Mathf.Max(bestScore, m_Score);
				bool persistResult = true;
#if UNITY_EDITOR
				persistResult = !SuppressRecordPersistenceForQa;
#endif
				if (persistResult)
				{
					PlayerPrefs.SetInt(BestScoreKey, bestScore);
					PlayerPrefs.Save();
				}
				m_BestText.text = bestScore.ToString("00");
				m_ResultText.text = "<color=#FFE36E><b>" + GameLocalization.T("SCORE  ", "점수  ") + m_Score.ToString("00") + "</b></color>"
				                              + GameLocalization.T("\nBEST  ", "\n최고 기록  ") + bestScore.ToString("00")
				                              + GameLocalization.T("\nMAX STREAK  ", "\n최대 연속 터치  ") + m_MaxCombo
				                              + GameLocalization.T("\nACCURACY  ", "\n정확도  ") + GetAccuracy().ToString("0") + "%"
				                              + "<color=#FF8AFF>" + GameLocalization.T("\nGRADE  ", "\n등급  ") + GetGrade() + "</color>";
				m_UiGamePopup.PlayResultReveal(isNewPersonalBest);

				if (isNewPersonalBest && persistResult)
				{
					m_NewBestPromptRoutine = StartCoroutine(ShowNewBestPromptAfterReveal(m_Score));
				}
				if (m_RoundInProgress)
				{
					m_RoundInProgress = false;
					m_AdsManager?.RecordCompletedRound();
				}
			}
		}

		private IEnumerator ShowNewBestPromptAfterReveal(int score)
		{
			yield return new WaitForSecondsRealtime(.95f);
			m_NewBestPromptRoutine = null;
			if (mGameFlow == null || mGameFlow.State != GameFlowState.Result || m_UiGamePopup == null) yield break;
			RegisterNewPersonalBest(score);
		}

		private void RegisterNewPersonalBest(int score)
		{
			try
			{
				// Nickname entry is local; the reveal coroutine has already checked that this result is still open.
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
			m_TapFeedback?.Clear();
			DespawnActiveTargets();
			m_PhaseCueRemaining = 0f;
		}

		private void DespawnActiveTargets()
		{
			mTargetPool.DespawnAll();
			m_ActiveTargets.Clear();
			m_SequenceActive = false;
			m_SequenceNextOrder = 0;
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
			m_RoundElapsed = 0f;
			m_PaceStage = 1;
			m_NextSequenceAt = float.PositiveInfinity;
			m_PhaseCueRemaining = 0f;
			m_FeverRemaining = 0f;
			m_NextFeverCombo = mConfig.feverCombo;
			m_ScoreText.text = "00";
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
