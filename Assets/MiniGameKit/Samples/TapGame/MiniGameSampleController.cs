using System.Collections.Generic;
using MiniGameKit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniGameKit.Samples.TapGame
{
    public sealed class MiniGameSampleController : MonoBehaviour
    {
        private const string BestScoreKey = "MiniGameKit.TapGame.BestScore";

        private static readonly Color CameraColor = new Color(0.025f, 0.035f, 0.08f);
        private static readonly Color BoardFrameColor = new Color(0.18f, 0.26f, 0.55f, 0.32f);
        private static readonly Color BoardColor = new Color(0.035f, 0.055f, 0.13f, 0.98f);
        private static readonly Color PanelColor = new Color(0.055f, 0.08f, 0.18f, 0.94f);
        private static readonly Color ButtonColor = new Color(0.15f, 0.55f, 0.95f, 1f);
        private static readonly Color MutedTextColor = new Color(0.58f, 0.67f, 0.82f);
        private static readonly Color[] TargetColors =
        {
            new Color(0.24f, 0.83f, 1f),
            new Color(0.66f, 0.42f, 1f),
            new Color(1f, 0.38f, 0.68f),
            new Color(0.32f, 1f, 0.72f)
        };

        [SerializeField]
        private GameFlow m_GameFlow;

        [SerializeField]
        private GameObjectPool m_TargetPool;

        [SerializeField]
        private GameConfig m_Config;

        private readonly CountdownTimer m_Timer = new CountdownTimer();
        private readonly List<MiniGameTarget> m_ActiveTargets = new List<MiniGameTarget>();

        private Camera m_MainCamera;
        private Sprite m_SolidSprite;
        private Sprite m_TargetSprite;
        private Image m_TimerCard;
        private Image m_ResultPanel;
        private Text m_StatusText;
        private Text m_ScoreText;
        private Text m_BestText;
        private Text m_TimerText;
        private Text m_ComboText;
        private Text m_HintText;
        private Text m_ResultText;
        private Button m_StartButton;
        private Button m_RetryButton;
        private int m_Score;
        private int m_BestScore;
        private int m_Combo;
        private int m_Hits;
        private int m_MaxCombo;
        private int m_Misses;
        private int m_BombsTapped;
        private float m_FeverRemaining;
        private bool m_FeverTriggered;

        private void Awake()
        {
            m_SolidSprite = CreateSolidSprite();
            m_TargetSprite = CreateCircleSprite(96);

            EnsureSceneServices();
            EnsureConfig();
            BuildPlayfield();
            BuildView();
            SetupPool();

            m_BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
            m_BestText.text = m_BestScore.ToString("00");

            m_Timer.Completed += HandleTimerCompleted;
            m_GameFlow.StateChanged += HandleFlowStateChanged;
            HandleFlowStateChanged(m_GameFlow.State);
        }

        private void Update()
        {
            if (m_GameFlow.State != GameFlowState.Playing)
            {
                return;
            }

            m_Timer.Tick(Time.deltaTime);
            UpdateFever();
            UpdateTimerVisual();
        }

        private void OnDestroy()
        {
            if (m_GameFlow != null)
            {
                m_GameFlow.StateChanged -= HandleFlowStateChanged;
            }

            m_Timer.Completed -= HandleTimerCompleted;
        }

        private void StartRound()
        {
            if (m_GameFlow.State != GameFlowState.Ready)
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
            m_ComboText.text = "STREAK x0";
            m_ResultText.text = string.Empty;
            m_GameFlow.StartGame();
            m_Timer.Start(m_Config.roundDuration);
            UpdateTimerVisual();
        }

        private void RetryRound()
        {
            if (m_GameFlow.State != GameFlowState.Result)
            {
                return;
            }

            m_GameFlow.Retry();
        }

        private void HandleTimerCompleted()
        {
            m_GameFlow.FinishGame();
        }

        private void HandleFlowStateChanged(GameFlowState state)
        {
            bool isReady = state == GameFlowState.Ready;
            bool isPlaying = state == GameFlowState.Playing;
            bool isResult = state == GameFlowState.Result;

            m_StatusText.text = isReady
                ? "READY  •  TAP START TO BEGIN"
                : isPlaying
                    ? "TAP THE GLOWING TARGETS"
                    : "ROUND COMPLETE";
            m_StatusText.color = isResult ? new Color(1f, 0.78f, 0.38f) : Color.white;
            m_StartButton.gameObject.SetActive(isReady);
            m_RetryButton.gameObject.SetActive(isResult);
            m_ResultPanel.gameObject.SetActive(isResult);
            m_HintText.gameObject.SetActive(!isResult);

            if (isReady)
            {
                m_Timer.Stop();
                m_TimerText.text = m_Config.roundDuration.ToString("0.0");
                m_TimerCard.color = PanelColor;
                m_ComboText.text = "STREAK x0";
                ClearTargets();
            }
            else if (isPlaying)
            {
                ClearTargets();
                for (int i = 0; i < m_Config.initialTargetCount; i++)
                {
                    SpawnTarget();
                }
            }
            else if (isResult)
            {
                m_Timer.Stop();
                ClearTargets();
                m_BestScore = Mathf.Max(m_BestScore, m_Score);
                PlayerPrefs.SetInt(BestScoreKey, m_BestScore);
                PlayerPrefs.Save();
                m_BestText.text = m_BestScore.ToString("00");
                m_ResultText.text = "SCORE  " + m_Score.ToString("00")
                    + "\nBEST  " + m_BestScore.ToString("00")
                    + "\nMAX STREAK  " + m_MaxCombo
                    + "\nACCURACY  " + GetAccuracy().ToString("0") + "%"
                    + "\nGRADE  " + GetGrade();
            }
        }

        private void HandleTargetTapped(MiniGameTarget target)
        {
            if (m_GameFlow.State != GameFlowState.Playing)
            {
                return;
            }

            if (target.Type == TapTargetType.Bomb)
            {
                m_BombsTapped++;
                m_Combo = 0;
                m_Timer.AddTime(-2f);
                m_ComboText.text = "BOMB!  -2.0 SEC";
                m_TargetPool.Despawn(target.gameObject);
                m_ActiveTargets.Remove(target);
                RefillTargets();
                return;
            }

            m_Hits++;
            m_Combo++;
            m_MaxCombo = Mathf.Max(m_MaxCombo, m_Combo);
            int baseScore = target.Type == TapTargetType.Quick ? 3
                : target.Type == TapTargetType.TimeBonus ? 2
                : m_Config.scorePerTap;
            int multiplier = m_Combo >= 20 ? 3 : m_Combo >= 5 ? 2 : 1;
            m_Score += baseScore * multiplier;
            if (target.Type == TapTargetType.TimeBonus)
            {
                m_Timer.AddTime(1f);
            }

            if (!m_FeverTriggered && m_Combo >= m_Config.feverCombo)
            {
                m_FeverTriggered = true;
                m_FeverRemaining = m_Config.feverDuration;
            }
            m_ScoreText.text = m_Score.ToString("00");
            m_ComboText.text = m_FeverRemaining > 0f ? "FEVER!  x" + multiplier + "  •  STREAK " + m_Combo
                : target.Type == TapTargetType.TimeBonus ? "+1.0 SEC  •  STREAK " + m_Combo
                : "STREAK " + m_Combo + "  •  SCORE x" + multiplier;
            m_TargetPool.Despawn(target.gameObject);
            m_ActiveTargets.Remove(target);
            RefillTargets();
        }

        private void HandleTargetMissed(MiniGameTarget target)
        {
            if (m_GameFlow.State != GameFlowState.Playing)
            {
                return;
            }

            if (target.Type != TapTargetType.Bomb)
            {
                m_Misses++;
                m_Combo = 0;
                m_ComboText.text = "MISSED  •  STREAK LOST";
            }

            m_TargetPool.Despawn(target.gameObject);
            m_ActiveTargets.Remove(target);
            RefillTargets();
        }

        private void SpawnTarget()
        {
            GameObject instance = m_TargetPool.Spawn(GetSpawnPosition(), Quaternion.identity);
            if (instance == null)
            {
                return;
            }

            MiniGameTarget target = instance.GetComponent<MiniGameTarget>();
            if (target == null)
            {
                Debug.LogError("The target prefab needs MiniGameTarget.", instance);
                m_TargetPool.Despawn(instance);
                return;
            }

            TapTargetType type = ChooseTargetType();
            float progress = 1f - Mathf.Clamp01(m_Timer.Remaining / Mathf.Max(0.1f, m_Config.roundDuration));
            float lifetime = Mathf.Lerp(m_Config.startingTargetLifetime, m_Config.minimumTargetLifetime, progress);
            if (type == TapTargetType.Quick)
            {
                lifetime *= 0.62f;
            }

            float scale = Mathf.Lerp(0.82f, m_Config.minimumTargetScale, progress);
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
            float elapsed = m_Config.roundDuration - m_Timer.Remaining;
            float roll = Random.value;
            if (elapsed >= 15f && roll < 0.12f) return TapTargetType.Bomb;
            if (elapsed >= 8f && roll < 0.32f) return TapTargetType.Quick;
            if (roll < 0.39f) return TapTargetType.TimeBonus;
            return TapTargetType.Normal;
        }

        private void RefillTargets()
        {
            int desired = m_FeverRemaining > 0f ? m_Config.feverTargetCount : m_Config.initialTargetCount;
            while (m_ActiveTargets.Count < desired)
            {
                SpawnTarget();
            }
        }

        private void UpdateFever()
        {
            if (m_FeverRemaining <= 0f)
            {
                return;
            }

            m_FeverRemaining = Mathf.Max(0f, m_FeverRemaining - Time.deltaTime);
            m_StatusText.text = m_FeverRemaining > 0f ? "FEVER  " + m_FeverRemaining.ToString("0.0") : "TAP THE GLOWING TARGETS";
            m_StatusText.color = m_FeverRemaining > 0f ? new Color(0.85f, 0.5f, 1f) : Color.white;
            if (m_FeverRemaining <= 0f && m_ActiveTargets.Count > m_Config.initialTargetCount)
            {
                while (m_ActiveTargets.Count > m_Config.initialTargetCount)
                {
                    MiniGameTarget extra = m_ActiveTargets[m_ActiveTargets.Count - 1];
                    m_ActiveTargets.RemoveAt(m_ActiveTargets.Count - 1);
                    m_TargetPool.Despawn(extra.gameObject);
                }
            }
            else
            {
                RefillTargets();
            }
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

        private Vector3 GetSpawnPosition()
        {
            float halfHeight = m_MainCamera.orthographicSize;
            float halfWidth = halfHeight * Mathf.Max(0.55f, m_MainCamera.aspect);
            float minX = -Mathf.Max(1.15f, halfWidth - 0.7f);
            float maxX = Mathf.Max(1.15f, halfWidth - 0.7f);
            float minY = -halfHeight + 1.45f;
            float maxY = halfHeight - 2.35f;
            return new Vector3(
                Random.Range(minX, maxX),
                Random.Range(Mathf.Min(minY, maxY - 0.5f), maxY),
                0f);
        }

        private void ClearTargets()
        {
            m_TargetPool.DespawnAll();
            m_ActiveTargets.Clear();
        }

        private void EnsureSceneServices()
        {
            if (m_GameFlow == null)
            {
                m_GameFlow = gameObject.AddComponent<GameFlow>();
            }

            if (m_TargetPool == null)
            {
                m_TargetPool = gameObject.AddComponent<GameObjectPool>();
            }

            m_MainCamera = Camera.main;
            if (m_MainCamera == null)
            {
                GameObject cameraObject = new GameObject("PrototypeCamera");
                m_MainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            m_MainCamera.orthographic = true;
            m_MainCamera.orthographicSize = 5f;
            m_MainCamera.backgroundColor = CameraColor;
            m_MainCamera.clearFlags = CameraClearFlags.SolidColor;
            m_MainCamera.transform.position = new Vector3(0f, 0f, -10f);
            if (m_MainCamera.GetComponent<Physics2DRaycaster>() == null)
            {
                m_MainCamera.gameObject.AddComponent<Physics2DRaycaster>();
            }

            if (EventSystem.current == null)
            {
                GameObject eventSystem = new GameObject("PrototypeEventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }
        }

        private void EnsureConfig()
        {
            if (m_Config == null)
            {
                m_Config = ScriptableObject.CreateInstance<GameConfig>();
            }
        }

        private void SetupPool()
        {
            GameObject targetPrefab = m_Config.targetPrefab;
            if (targetPrefab == null)
            {
                targetPrefab = CreatePrototypeTarget();
            }

            m_TargetPool.Initialize(targetPrefab, m_TargetPool.transform, m_Config.initialTargetCount);
        }

        private GameObject CreatePrototypeTarget()
        {
            GameObject target = new GameObject("PrototypeTargetPrefab");
            target.transform.SetParent(m_TargetPool.transform, false);
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

        private void BuildPlayfield()
        {
            CreateWorldSprite("PrototypePlayfieldFrame", new Vector3(8.7f, 7.4f, 1f), BoardFrameColor, -20);
            CreateWorldSprite("PrototypePlayfield", new Vector3(8.35f, 7.05f, 1f), BoardColor, -19);
        }

        private void BuildView()
        {
            GameObject canvasObject = new GameObject("PrototypeCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            CreatePanel(canvas.transform, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 230f),
                new Color(0.025f, 0.04f, 0.1f, 0.94f));
            CreateText(canvas.transform, "VIOLET TAP", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -28f), new Vector2(700f, 58f), 34, Color.white, TextAnchor.MiddleCenter);
            CreateText(canvas.transform, "30-SECOND REFLEX OVERDRIVE", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -78f), new Vector2(700f, 34f), 15, MutedTextColor, TextAnchor.MiddleCenter);

            CreateStatCard(canvas.transform, "SCORE", new Vector2(-300f, -150f), out m_ScoreText);
            CreateStatCard(canvas.transform, "BEST", new Vector2(0f, -150f), out m_BestText);
            CreateStatCard(canvas.transform, "TIME", new Vector2(300f, -150f), out m_TimerText, out m_TimerCard);

            m_StatusText = CreateText(canvas.transform, "READY", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -270f), new Vector2(720f, 62f), 19, Color.white, TextAnchor.MiddleCenter);
            m_ComboText = CreateText(canvas.transform, "STREAK x0", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 184f), new Vector2(720f, 46f), 24, new Color(0.46f, 0.9f, 1f), TextAnchor.MiddleCenter);
            m_HintText = CreateText(canvas.transform, "VIOLET +3  •  GOLD +1 SEC  •  AVOID RED", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 140f), new Vector2(900f, 40f), 15, MutedTextColor, TextAnchor.MiddleCenter);

            m_ResultPanel = CreatePanel(canvas.transform, "ResultPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650f, 430f), PanelColor);
            m_ResultText = CreateText(m_ResultPanel.transform, string.Empty, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, 26, Color.white, TextAnchor.MiddleCenter);

            m_StartButton = CreateButton(canvas.transform, "START ROUND", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 72f), ButtonColor);
            m_StartButton.onClick.AddListener(StartRound);
            m_RetryButton = CreateButton(canvas.transform, "PLAY AGAIN", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 72f), new Color(0.62f, 0.34f, 0.95f));
            m_RetryButton.onClick.AddListener(RetryRound);
        }

        private void CreateStatCard(Transform parent, string label, Vector2 position, out Text value)
        {
            Image card = CreatePanel(parent, label + "Card", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 0.5f), position, new Vector2(250f, 82f), PanelColor);
            CreateText(card.transform, label, Vector2.zero, Vector2.one,
                new Vector2(0f, 18f), new Vector2(210f, 24f), 13, MutedTextColor, TextAnchor.MiddleCenter);
            value = CreateText(card.transform, "00", Vector2.zero, Vector2.one,
                new Vector2(0f, -13f), new Vector2(220f, 48f), 31, Color.white, TextAnchor.MiddleCenter);
        }

        private void CreateStatCard(Transform parent, string label, Vector2 position, out Text value, out Image card)
        {
            card = CreatePanel(parent, label + "Card", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 0.5f), position, new Vector2(250f, 82f), PanelColor);
            CreateText(card.transform, label, Vector2.zero, Vector2.one,
                new Vector2(0f, 18f), new Vector2(210f, 24f), 13, MutedTextColor, TextAnchor.MiddleCenter);
            value = CreateText(card.transform, "00", Vector2.zero, Vector2.one,
                new Vector2(0f, -13f), new Vector2(220f, 48f), 31, Color.white, TextAnchor.MiddleCenter);
        }

        private Image CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 position, Vector2 size, Color color)
        {
            GameObject panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);
            Image image = panelObject.AddComponent<Image>();
            image.sprite = m_SolidSprite;
            image.color = color;
            SetRect(image.rectTransform, anchorMin, anchorMax, pivot, position, size);
            return image;
        }

        private Text CreateText(Transform parent, string value, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(value + "Text");
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(2f, -2f);
            SetRect(text.rectTransform, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), position, size);
            return text;
        }

        private Button CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Color color)
        {
            Image background = CreatePanel(parent, label + "Button", anchorMin, anchorMax,
                new Vector2(0.5f, 0.5f), position, new Vector2(360f, 86f), color);
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            CreateText(background.transform, label, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, 21, Color.white, TextAnchor.MiddleCenter);
            Shadow shadow = background.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(0f, -5f);
            return button;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void UpdateTimerVisual()
        {
            float remaining = Mathf.Max(0f, m_Timer.Remaining);
            m_TimerText.text = remaining.ToString("0.0");
            bool urgent = remaining <= 5f && m_GameFlow.State == GameFlowState.Playing;
            m_TimerText.color = urgent ? new Color(1f, 0.4f, 0.43f) : Color.white;
            m_TimerCard.color = urgent ? new Color(0.36f, 0.08f, 0.16f, 0.96f) : PanelColor;
        }

        private GameObject CreateWorldSprite(string name, Vector3 scale, Color color, int sortingOrder)
        {
            GameObject spriteObject = new GameObject(name);
            spriteObject.transform.SetParent(transform, false);
            spriteObject.transform.localPosition = new Vector3(0f, -0.25f, 1f);
            spriteObject.transform.localScale = scale;
            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = m_SolidSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return spriteObject;
        }

        private static Sprite CreateSolidSprite()
        {
            return Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f), 1f);
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
    }
}
