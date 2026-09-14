using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI
{
    /// <summary>Short result entrance, using the prefab panel and the active TMP font atlas.</summary>
    [RequireComponent(typeof(CanvasGroup), typeof(Image))]
    public sealed class ResultRevealAnimator : MonoBehaviour
    {
        private RectTransform panelRect;
        private RectTransform bodyRect;
        private RectTransform retryRect;
        private RectTransform headingRect;
        private CanvasGroup group;
        private Image panelImage;
        private TextMeshProUGUI body;
        private TextMeshProUGUI headingLabel;
        private Material glowMaterial;
        private Vector3 panelBaseScale;
        private Vector3 bodyBaseScale;
        private Vector3 retryBaseScale;
        private Vector3 headingBaseScale;
        private Color bodyBaseColor;
        private float headingBaseFontSize;
        private bool headingBaseAutoSizing;
        private FontStyles headingBaseStyle;
        private float age;
        private bool revealing;
        private bool hasPlayed;
        private bool newBest;

        private void Awake()
        {
            panelRect = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
            panelImage = GetComponent<Image>();
            body = GetComponentInChildren<TextMeshProUGUI>(true);
            bodyRect = body.rectTransform;
            panelBaseScale = panelRect.localScale;
            bodyBaseScale = bodyRect.localScale;
            bodyBaseColor = body.color;
            group.blocksRaycasts = false;
        }

        public void Play(Button retry, TextMeshProUGUI heading, bool isNewBest)
        {
            retryRect = retry == null ? null : (RectTransform)retry.transform;
            headingRect = heading == null ? null : heading.rectTransform;
            headingLabel = heading;
            if (retryRect != null) retryBaseScale = retryRect.localScale;
            if (headingRect != null)
            {
                headingBaseScale = headingRect.localScale;
                headingBaseFontSize = headingLabel.fontSize;
                headingBaseAutoSizing = headingLabel.enableAutoSizing;
                headingBaseStyle = headingLabel.fontStyle;
                headingLabel.enableAutoSizing = false;
                headingLabel.fontSize = 58f;
                headingLabel.fontStyle |= FontStyles.Bold;
            }
            bodyBaseColor = body.color;
            newBest = isNewBest;
            hasPlayed = true;
            revealing = true;
            age = 0f;
            group.alpha = 0f;
            panelRect.localScale = panelBaseScale * .72f;
            bodyRect.localScale = bodyBaseScale * .9f;
            if (retryRect != null) retryRect.localScale = retryBaseScale * .85f;
            if (headingRect != null) headingRect.localScale = headingBaseScale * .8f;
            EnsureGlowMaterial();
        }

        private void Update()
        {
            if (!revealing) return;
            age += Time.unscaledDeltaTime;
            float enter = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / .28f));
            float pop = Mathf.Sin(Mathf.Clamp01(age / .46f) * Mathf.PI) * .16f;
            group.alpha = Mathf.Clamp01(age / .18f);
            panelRect.localScale = panelBaseScale * (.72f + .28f * enter + pop);
            bodyRect.localScale = bodyBaseScale * (.9f + .1f * enter);
            float pulse = .5f + .5f * Mathf.Sin(age * 22f);
            panelImage.color = Color.Lerp(new Color(1f, .63f, 1f), Color.white, enter * (.75f + .25f * pulse));
            body.color = Color.Lerp(bodyBaseColor, Color.white, .22f * pulse);
            if (glowMaterial != null && glowMaterial.HasProperty("_GlowColor"))
                glowMaterial.SetColor("_GlowColor", newBest
                    ? new Color(1f, .77f, .22f, .55f + .25f * pulse)
                    : new Color(.32f, .82f, 1f, .38f + .25f * pulse));
            if (headingRect != null)
                headingRect.localScale = headingBaseScale * (.8f + .2f * enter + pop * .65f);
            if (retryRect != null)
            {
                float retryEnter = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - .28f) / .32f));
                retryRect.localScale = retryBaseScale * (.85f + .15f * retryEnter);
            }
            if (age < 1.1f) return;
            revealing = false;
            group.alpha = 1f;
            panelRect.localScale = panelBaseScale;
            bodyRect.localScale = bodyBaseScale;
            body.color = bodyBaseColor;
            panelImage.color = Color.white;
            if (retryRect != null) retryRect.localScale = retryBaseScale;
            if (headingRect != null) headingRect.localScale = headingBaseScale;
        }

        private void EnsureGlowMaterial()
        {
            if (glowMaterial != null && body.fontSharedMaterial == glowMaterial) return;
            if (glowMaterial != null) Destroy(glowMaterial);
            if (body.fontSharedMaterial == null) return;
            glowMaterial = new Material(body.fontSharedMaterial) { name = "Result Reveal Glow" };
            glowMaterial.hideFlags = HideFlags.DontSave;
            if (glowMaterial.HasProperty("_OutlineWidth")) glowMaterial.SetFloat("_OutlineWidth", .08f);
            if (glowMaterial.HasProperty("_OutlineColor"))
                glowMaterial.SetColor("_OutlineColor", new Color(.4f, .8f, 1f, .9f));
            if (glowMaterial.HasProperty("_GlowOuter")) glowMaterial.SetFloat("_GlowOuter", .14f);
            if (glowMaterial.HasProperty("_GlowPower")) glowMaterial.SetFloat("_GlowPower", .55f);
            glowMaterial.EnableKeyword("GLOW_ON");
            body.fontSharedMaterial = glowMaterial;
        }

        private void OnDisable()
        {
            if (!hasPlayed) return;
            revealing = false;
            if (group != null) group.alpha = 1f;
            if (panelRect != null) panelRect.localScale = panelBaseScale;
            if (bodyRect != null) bodyRect.localScale = bodyBaseScale;
            if (body != null) body.color = bodyBaseColor;
            if (panelImage != null) panelImage.color = Color.white;
            if (retryRect != null) retryRect.localScale = retryBaseScale;
            if (headingRect != null) headingRect.localScale = headingBaseScale;
            if (headingLabel != null)
            {
                headingLabel.fontSize = headingBaseFontSize;
                headingLabel.enableAutoSizing = headingBaseAutoSizing;
                headingLabel.fontStyle = headingBaseStyle;
            }
        }

        private void OnDestroy()
        {
            if (glowMaterial != null) Destroy(glowMaterial);
        }
    }
}
