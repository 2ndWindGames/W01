using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _01.Scripts.UI
{
    /// <summary>Ready-screen neon pulse and immediate press response for the start button.</summary>
    [RequireComponent(typeof(Button), typeof(Image))]
    public sealed class StartButtonImpact : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private RectTransform buttonRect;
        private Image buttonImage;
        private TextMeshProUGUI label;
        private Material glowMaterial;
        private Vector3 baseScale;
        private float phase;
        private bool pressed;

        private void Awake()
        {
            buttonRect = (RectTransform)transform;
            buttonImage = GetComponent<Image>();
            label = GetComponentInChildren<TextMeshProUGUI>(true);
            baseScale = buttonRect.localScale;
            buttonImage.color = Color.white;
            if (label == null) return;
            label.fontStyle |= FontStyles.Bold;
            label.fontSize = 70f;
            label.characterSpacing = 3f;
            label.raycastTarget = false;
        }

        private void OnEnable()
        {
            pressed = false;
            phase = 0f;
        }

        private void Update()
        {
            if (label == null) return;
            EnsureGlowMaterial();
            phase += Time.unscaledDeltaTime;
            float wave = .5f + .5f * Mathf.Sin(phase * 6.6f);
            float scale = pressed ? .94f : 1.01f + wave * .035f;
            buttonRect.localScale = Vector3.Lerp(buttonRect.localScale, baseScale * scale,
                1f - Mathf.Exp(-15f * Time.unscaledDeltaTime));
            buttonImage.color = Color.Lerp(new Color(.78f, .94f, 1f), Color.white, wave);
            label.color = Color.Lerp(new Color(.82f, .97f, 1f), Color.white, wave);
            if (glowMaterial != null && glowMaterial.HasProperty("_GlowColor"))
                glowMaterial.SetColor("_GlowColor", new Color(.38f, .84f, 1f, pressed ? .85f : .38f + .35f * wave));
        }

        private void EnsureGlowMaterial()
        {
            if (glowMaterial != null && label.fontSharedMaterial == glowMaterial) return;
            if (glowMaterial != null) Destroy(glowMaterial);
            if (label.fontSharedMaterial == null) return;
            glowMaterial = new Material(label.fontSharedMaterial) { name = "Start Button Glow" };
            glowMaterial.hideFlags = HideFlags.DontSave;
            if (glowMaterial.HasProperty("_OutlineWidth")) glowMaterial.SetFloat("_OutlineWidth", .1f);
            if (glowMaterial.HasProperty("_OutlineColor"))
                glowMaterial.SetColor("_OutlineColor", new Color(.1f, .77f, 1f, .9f));
            if (glowMaterial.HasProperty("_GlowOuter")) glowMaterial.SetFloat("_GlowOuter", .15f);
            if (glowMaterial.HasProperty("_GlowPower")) glowMaterial.SetFloat("_GlowPower", .55f);
            glowMaterial.EnableKeyword("GLOW_ON");
            label.fontSharedMaterial = glowMaterial;
        }

        public void OnPointerDown(PointerEventData eventData) => pressed = true;
        public void OnPointerUp(PointerEventData eventData) => pressed = false;
        public void OnPointerExit(PointerEventData eventData) => pressed = false;

        private void OnDisable()
        {
            pressed = false;
            if (buttonRect != null) buttonRect.localScale = baseScale;
            if (buttonImage != null) buttonImage.color = Color.white;
        }

        private void OnDestroy()
        {
            if (glowMaterial != null) Destroy(glowMaterial);
        }
    }
}
