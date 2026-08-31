using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniGameKit.Samples.TapGame
{
    public sealed class MiniGameTarget : MonoBehaviour, IPointerClickHandler
    {
        private Action<MiniGameTarget> m_OnTapped;
        private SpriteRenderer m_Renderer;
        private SpriteRenderer m_GlowRenderer;
        private Vector3 m_BaseScale;
        private float m_PulsePhase;

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
            transform.localScale = m_BaseScale * pulse;
            if (m_GlowRenderer != null)
            {
                Color glowColor = m_GlowRenderer.color;
                glowColor.a = 0.11f + (pulse - 0.92f) * 0.7f;
                m_GlowRenderer.color = glowColor;
            }
        }

        public void Bind(Action<MiniGameTarget> onTapped)
        {
            m_OnTapped = onTapped;
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
            m_OnTapped?.Invoke(this);
        }

        private void OnDisable()
        {
            m_OnTapped = null;
            transform.localScale = m_BaseScale;
        }
    }
}
