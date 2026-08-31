using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniGameKit.Samples.TapGame
{
    public enum TapTargetType
    {
        Normal,
        Quick,
        TimeBonus,
        Bomb
    }

    public sealed class MiniGameTarget : MonoBehaviour, IPointerClickHandler
    {
        private Action<MiniGameTarget> m_OnTapped;
        private Action<MiniGameTarget> m_OnMissed;
        private SpriteRenderer m_Renderer;
        private SpriteRenderer m_GlowRenderer;
        private Vector3 m_BaseScale;
        private float m_PulsePhase;
        private float m_Lifetime;
        private float m_RemainingLifetime;

        public TapTargetType Type { get; private set; }

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
        }

        private void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * 5.5f + m_PulsePhase) * 0.08f;
            float lifeRatio = m_Lifetime > 0f ? Mathf.Clamp01(m_RemainingLifetime / m_Lifetime) : 1f;
            float warningPulse = lifeRatio < 0.3f ? 1f + Mathf.Sin(Time.time * 18f) * 0.08f : 1f;
            transform.localScale = m_BaseScale * pulse * warningPulse;
            if (m_GlowRenderer != null)
            {
                Color glowColor = m_GlowRenderer.color;
                glowColor.a = 0.11f + (pulse - 0.92f) * 0.7f;
                m_GlowRenderer.color = glowColor;
            }

            m_RemainingLifetime -= Time.deltaTime;
            if (m_RemainingLifetime <= 0f)
            {
                var missed = m_OnMissed;
                m_OnMissed = null;
                missed?.Invoke(this);
            }
        }

        public void Bind(TapTargetType type, float lifetime, float scale,
            Action<MiniGameTarget> onTapped, Action<MiniGameTarget> onMissed)
        {
            Type = type;
            m_Lifetime = Mathf.Max(0.1f, lifetime);
            m_RemainingLifetime = m_Lifetime;
            m_BaseScale = Vector3.one * scale;
            transform.localScale = m_BaseScale;
            m_OnTapped = onTapped;
            m_OnMissed = onMissed;
        }

        public void SetVisual(Sprite sprite, Color color)
        {
            if (m_Renderer != null)
            {
                m_Renderer.sprite = sprite;
                m_Renderer.color = color;
            }

            if (m_GlowRenderer != null)
            {
                m_GlowRenderer.sprite = sprite;
                m_GlowRenderer.color = new Color(color.r, color.g, color.b, 0.16f);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var tapped = m_OnTapped;
            m_OnTapped = null;
            m_OnMissed = null;
            tapped?.Invoke(this);
        }

        private void OnDisable()
        {
            m_OnTapped = null;
            m_OnMissed = null;
            transform.localScale = m_BaseScale;
        }
    }
}
