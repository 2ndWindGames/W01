using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _01.Scripts.Game
{
    public enum TapTargetType
    {
        Normal,
        Quick,
        TimeBonus,
        Bomb
    }

    public enum TargetSizeTier
    {
        Small,
        Medium,
        Large
    }

    public sealed class CircleTarget : MonoBehaviour, IPointerDownHandler
    {
        private Action<CircleTarget> m_OnTapped;
        private Action<CircleTarget> m_OnMissed;
        private SpriteRenderer m_Renderer;
        private SpriteRenderer m_GlowRenderer;
        private TextMeshPro m_SequenceLabel;
        private Vector3 m_BaseScale;
        private float m_PulsePhase;
        private float m_Lifetime;
        private float m_RemainingLifetime;
        private bool m_FeverMode;
        private bool m_Paused;
        private float m_Pace = 1f;
        
        private static readonly Sprite[] s_TargetSprites = new Sprite[4];
        private static readonly string[] s_TargetResourcePaths =
        {
            "UI/NeonSignalPack/Targets/target_normal",
            "UI/NeonSignalPack/Targets/target_quick",
            "UI/NeonSignalPack/Targets/target_time",
            "UI/NeonSignalPack/Targets/target_danger"
        };
        private static bool s_TargetLoadErrorLogged;

        public TapTargetType Type { get; private set; }
        public TargetSizeTier SizeTier { get; private set; } = TargetSizeTier.Medium;
        public float NominalScale => m_BaseScale.x;
        public int SequenceOrder { get; private set; }

        private void Awake()
        {
            m_Renderer = GetComponent<SpriteRenderer>();
            m_GlowRenderer = transform.Find("Glow")?.GetComponent<SpriteRenderer>();
            m_BaseScale = transform.localScale;
        }

        private void OnEnable()
        {
            m_PulsePhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            transform.localScale = m_BaseScale;
            ClearSequenceOrder();
        }

        private void Update()
        {
            if (m_Paused) return;
            float pulseSpeed = (m_FeverMode ? 9.5f : 5.5f) * m_Pace;
            float pulseAmount = m_FeverMode ? 0.14f : 0.08f;
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed + m_PulsePhase) * pulseAmount;
            float lifeRatio = m_Lifetime > 0f ? Mathf.Clamp01(m_RemainingLifetime / m_Lifetime) : 1f;
            float warningPulse = lifeRatio < 0.3f ? 1f + Mathf.Sin(Time.time * 18f) * 0.08f : 1f;
            transform.localScale = m_BaseScale * pulse * warningPulse;
            if (m_GlowRenderer != null)
            {
                Color glowColor = m_GlowRenderer.color;
                glowColor.a = (m_FeverMode ? 0.25f : 0.11f) + (pulse - 0.92f) * 0.7f;
                m_GlowRenderer.color = glowColor;
            }

            if (m_FeverMode)
                transform.Rotate(0f, 0f, 32f * Time.deltaTime);

            m_RemainingLifetime -= Time.deltaTime;
            if (m_RemainingLifetime <= 0f)
            {
                var missed = m_OnMissed;
                m_OnMissed = null;
                m_OnTapped = null;
                missed?.Invoke(this);
            }
        }

        public void Bind(TapTargetType type, float lifetime, float scale,
            Action<CircleTarget> onTapped, Action<CircleTarget> onMissed,
            TargetSizeTier sizeTier = TargetSizeTier.Medium)
        {
            Type = type;
            SizeTier = type == TapTargetType.Bomb ? TargetSizeTier.Medium : sizeTier;
            m_Lifetime = Mathf.Max(0.1f, lifetime);
            m_RemainingLifetime = m_Lifetime;
            m_BaseScale = Vector3.one * scale;
            transform.localScale = m_BaseScale;
            m_OnTapped = onTapped;
            m_OnMissed = onMissed;
            m_Paused = false;
            ClearSequenceOrder();
        }

        public void SetSequenceOrder(int order, TMP_FontAsset font)
        {
            if (order <= 0)
            {
                ClearSequenceOrder();
                return;
            }

            if (m_SequenceLabel == null)
            {
                var labelObject = new GameObject("Sequence Order");
                labelObject.transform.SetParent(transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 0f, -0.02f);
                labelObject.transform.localScale = Vector3.one * 0.1f;
                m_SequenceLabel = labelObject.AddComponent<TextMeshPro>();
                m_SequenceLabel.alignment = TextAlignmentOptions.Center;
                m_SequenceLabel.fontSize = 40f;
                m_SequenceLabel.fontStyle = FontStyles.Bold;
                m_SequenceLabel.color = new Color(1f, 0.97f, 0.78f, 1f);
                m_SequenceLabel.textWrappingMode = TextWrappingModes.NoWrap;
                m_SequenceLabel.rectTransform.sizeDelta = new Vector2(10f, 10f);
            }

            SequenceOrder = order;
            // Keep numbered sequence labels legible on the smallest target.
            m_SequenceLabel.transform.localScale = Vector3.one * (0.082f / Mathf.Max(0.5f, NominalScale));
            m_SequenceLabel.font = font != null ? font : TMP_Settings.defaultFontAsset;
            m_SequenceLabel.outlineColor = new Color32(5, 10, 26, 255);
            m_SequenceLabel.outlineWidth = 0.22f;
            m_SequenceLabel.text = order.ToString();
            MeshRenderer labelRenderer = m_SequenceLabel.GetComponent<MeshRenderer>();
            if (labelRenderer != null && m_Renderer != null)
            {
                labelRenderer.sortingLayerID = m_Renderer.sortingLayerID;
                labelRenderer.sortingOrder = m_Renderer.sortingOrder + 1;
            }

            m_SequenceLabel.gameObject.SetActive(true);
        }

        private void ClearSequenceOrder()
        {
            SequenceOrder = 0;
            if (m_SequenceLabel != null)
                m_SequenceLabel.gameObject.SetActive(false);
        }

        public void SetVisual(Sprite sprite, Color color)
        {
            if (m_Renderer != null)
            {
                m_Renderer.sprite = sprite;
                // m_Renderer.color = color;
                m_Renderer.color = Color.white;
            }

            if (m_GlowRenderer != null)
            {
                m_GlowRenderer.sprite = sprite;
                m_GlowRenderer.color = new Color(1f, 1f, 1f, 0.14f);
            }
        }

        public Sprite GetSprite(TapTargetType type)
        {
            int index = (int)type;
            if (index < 0 || index >= s_TargetSprites.Length)
            {
                index = 0;
            }

            if (s_TargetSprites[index] == null)
            {
                s_TargetSprites[index] = Resources.Load<Sprite>(s_TargetResourcePaths[index]);
            }

            if (s_TargetSprites[index] == null && !s_TargetLoadErrorLogged)
            {
                Debug.LogError("Neon Signal target sprites could not be loaded from Resources/UI/NeonSignalPack/Targets.", this);
                s_TargetLoadErrorLogged = true;
            }

            return s_TargetSprites[index];
        }

        public void SetFeverMode(bool active)
        {
            m_FeverMode = active;
        }

        public void SetPaused(bool paused) => m_Paused = paused;
        public void SetPace(float pace) => m_Pace = Mathf.Clamp(pace, 1f, 2f);

        // Respond on contact. Waiting for release made fast taps feel disconnected.
        public void OnPointerDown(PointerEventData eventData)
        {
            if (m_Paused || (eventData != null && eventData.button != PointerEventData.InputButton.Left)) return;
            var tapped = m_OnTapped;
            if (tapped == null) return;
            m_OnTapped = null;
            m_OnMissed = null;
            tapped?.Invoke(this);
        }

        private void OnDisable()
        {
            m_OnTapped = null;
            m_OnMissed = null;
            m_FeverMode = false;
            m_Paused = false;
            transform.rotation = Quaternion.identity;
            m_Pace = 1f;
            SizeTier = TargetSizeTier.Medium;
            transform.localScale = m_BaseScale;
            ClearSequenceOrder();
        }
    }
}
