using SWGUnity2DCore.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI
{
    /// <summary>Presentation only: score rules and fever ownership stay in GameScene.</summary>
    public sealed class ComboStatusView : MonoBehaviour
    {
        public RectTransform card;
        public Image panel;
        public Image progressFill;
        public TextMeshProUGUI comboLabel;
        public TextMeshProUGUI multiplierLabel;
        public TextMeshProUGUI progressLabel;
        public RectTransform failureRoot;
        public CanvasGroup failureGroup;
        public TextMeshProUGUI failureLabel;
        public Image[] failureEdges;
        private int previousCombo;
        private float impactRemaining;
        private CanvasGroup impactGroup;
        private Material comboMaterial;
        private Material streakMaterial;
        private Material feedbackMaterial;
        private Color impactAccent;
        private Color feedbackAccent;
        private float failureRemaining;
        private float failureDuration;
        private float shake;
        private bool paused;
        private Vector2 basePosition;
        private Vector2 failureBasePosition;
        private Vector2 failureLabelBasePosition;
        private Vector2 failureLabelBaseSize;
        private Vector2 failureLabelBaseAnchor;
        private float failureLabelBaseFontSize;
        private float failureLabelBaseFontSizeMax;
        private bool failureLabelBaseAutoSizing;
        private bool paceActive;
        private int lastCombo = -1, lastMultiplier, lastNextFever, lastStage, lastFeverTenth = -1;
        private bool lastPlaying;

        private void Awake()
        {
            // The prefab supplies the typography; the card is now a brief hit cue in the playfield.
            card.anchorMin = card.anchorMax = new Vector2(.5f, 1f);
            card.anchoredPosition = new Vector2(0f, -550f);
            card.sizeDelta = new Vector2(860f, 180f);
            basePosition = card.anchoredPosition;
            comboLabel.rectTransform.anchoredPosition = new Vector2(0f, 35f);
            comboLabel.rectTransform.sizeDelta = new Vector2(820f, 95f);
            comboLabel.enableAutoSizing = false;
            comboLabel.fontSize = 72f;
            comboLabel.alignment = TextAlignmentOptions.Center;
            multiplierLabel.rectTransform.anchoredPosition = new Vector2(0f, -43f);
            multiplierLabel.rectTransform.sizeDelta = new Vector2(820f, 54f);
            multiplierLabel.enableAutoSizing = false;
            multiplierLabel.fontSize = 30f;
            multiplierLabel.alignment = TextAlignmentOptions.Center;
            failureBasePosition = failureRoot.anchoredPosition;
            failureLabelBasePosition = failureLabel.rectTransform.anchoredPosition;
            failureLabelBaseSize = failureLabel.rectTransform.sizeDelta;
            failureLabelBaseAnchor = failureLabel.rectTransform.anchorMin;
            failureLabelBaseFontSize = failureLabel.fontSize;
            failureLabelBaseFontSizeMax = failureLabel.fontSizeMax;
            failureLabelBaseAutoSizing = failureLabel.enableAutoSizing;
            impactGroup = card.GetComponent<CanvasGroup>();
            if (impactGroup == null) impactGroup = card.gameObject.AddComponent<CanvasGroup>();
            impactGroup.blocksRaycasts = false;
            impactGroup.interactable = false;
            foreach (Image image in card.GetComponentsInChildren<Image>(true)) image.enabled = false;
            progressLabel.gameObject.SetActive(false);
            card.gameObject.SetActive(false);
        }
        public void SetPaused(bool value) => paused = value;

        public void SetState(int combo, int multiplier, int nextFever, int feverStep, float feverTime,
            float feverMaximum, int speedStage, bool playing)
        {
            int tenth = Mathf.CeilToInt(feverTime * 10f);
            if (combo == lastCombo && multiplier == lastMultiplier && nextFever == lastNextFever
                && speedStage == lastStage && tenth == lastFeverTenth && playing == lastPlaying) return;
            lastCombo = combo; lastMultiplier = multiplier; lastNextFever = nextFever;
            lastStage = speedStage; lastFeverTenth = tenth; lastPlaying = playing;
            if (!playing) { ClearFailure(); previousCombo = 0; impactRemaining = 0f; card.gameObject.SetActive(false); return; }
            if (combo > previousCombo)
            {
                impactRemaining = .68f;
                impactGroup.alpha = 0f;
                card.anchoredPosition = basePosition;
                card.localScale = Vector3.one * .72f;
                card.gameObject.SetActive(true);
            }
            else if (combo == 0)
            {
                impactRemaining = 0f;
                card.gameObject.SetActive(false);
            }
            previousCombo = combo;
            bool fever = feverTime > 0f;
            var accent = fever ? new Color(1f, .28f, .9f)
                : multiplier >= 2 ? new Color(1f, .8f, .18f) : new Color(.2f, .9f, 1f);
            comboLabel.text = combo + " COMBO!";
            comboLabel.color = Color.white;
            multiplierLabel.text = GameLocalization.T("STREAK  ", "연속 터치  ") + combo
                + (multiplier > 1 ? "   x" + multiplier : "");
            multiplierLabel.color = Color.white;
            impactAccent = accent;
            if (combo > 0 && card.gameObject.activeSelf)
            {
                EnsureGlowMaterial(comboLabel, ref comboMaterial, .12f, .19f);
                EnsureGlowMaterial(multiplierLabel, ref streakMaterial, .08f, .11f);
            }
            float fill = fever ? Mathf.Clamp01(feverTime / Mathf.Max(1f, feverMaximum))
                : Mathf.Clamp01((combo - Mathf.Max(0, nextFever - feverStep)) / (float)Mathf.Max(1, feverStep));
            progressFill.fillAmount = fill;
            progressFill.color = accent;
            progressLabel.text = fever ? GameLocalization.T("FEVER  ", "피버  ") + feverTime.ToString("0.0") + "s"
                : GameLocalization.T("FEVER IN ", "피버까지 ") + Mathf.Max(0, nextFever - combo)
                    + GameLocalization.T(" TAPS", "콤보");
            progressLabel.text += GameLocalization.T("   •   SPEED ", "   •   속도 ") + speedStage;
        }

        public void ShowFailure(int lostCombo, bool bomb)
        {
            ClearFailure();
            paceActive = false;
            failureDuration = lostCombo >= 5 || bomb ? .85f : .5f;
            failureRemaining = failureDuration;
            shake = lostCombo >= 5 || bomb ? 14f : 7f;
            feedbackAccent = new Color(1f, .27f, .36f);
            failureLabel.text = bomb ? GameLocalization.T("BOMB!  −2 SEC", "폭탄!  −2초")
                : lostCombo > 0 ? lostCombo + GameLocalization.T(" COMBO LOST!", " 콤보 끊김!")
                : GameLocalization.T("MISSED!", "아깝다!");
            failureLabel.color = feedbackAccent;
            EnsureGlowMaterial(failureLabel, ref feedbackMaterial, .08f, .14f);
            failureRoot.gameObject.SetActive(true);
            failureGroup.alpha = 1f;
        }

        public void ShowPaceIncrease(int stage)
        {
            ClearFailure();
            paceActive = true;
            failureDuration = 1.15f;
            failureRemaining = failureDuration;
            feedbackAccent = new Color(1f, .82f, .18f);
            failureLabel.text = GameLocalization.T("SPEED UP!  LEVEL ", "스피드 업!  단계 ") + stage;
            failureLabel.color = feedbackAccent;
            failureLabel.rectTransform.anchorMin = failureLabel.rectTransform.anchorMax = new Vector2(.5f, .5f);
            failureLabel.rectTransform.anchoredPosition = new Vector2(0f, 120f);
            failureLabel.rectTransform.sizeDelta = new Vector2(980f, 160f);
            failureLabel.fontSize = 68f;
            failureLabel.fontSizeMax = 68f;
            failureLabel.enableAutoSizing = true;
            EnsureGlowMaterial(failureLabel, ref feedbackMaterial, .12f, .2f);
            failureRoot.gameObject.SetActive(true);
            failureGroup.alpha = 0f;
        }

        public void ClearFailure()
        {
            failureRemaining = 0f;
            if (failureRoot != null) failureRoot.gameObject.SetActive(false);
            if (failureRoot != null) failureRoot.anchoredPosition = failureBasePosition;
            if (failureLabel != null)
            {
                failureLabel.rectTransform.anchorMin = failureLabel.rectTransform.anchorMax = failureLabelBaseAnchor;
                failureLabel.rectTransform.anchoredPosition = failureLabelBasePosition;
                failureLabel.rectTransform.sizeDelta = failureLabelBaseSize;
                failureLabel.rectTransform.localScale = Vector3.one;
                failureLabel.fontSize = failureLabelBaseFontSize;
                failureLabel.fontSizeMax = failureLabelBaseFontSizeMax;
                failureLabel.enableAutoSizing = failureLabelBaseAutoSizing;
            }
            paceActive = false;
        }

        private void Update()
        {
            if (paused) return;
            if (impactRemaining > 0f)
            {
                impactRemaining = Mathf.Max(0f, impactRemaining - Time.deltaTime);
                float impactAge = .68f - impactRemaining;
                float appear = Mathf.Clamp01(impactAge / .09f);
                float impactFade = Mathf.Clamp01(impactRemaining / .2f);
                impactGroup.alpha = Mathf.Min(appear, impactFade);
                float overshoot = Mathf.Sin(Mathf.Clamp01(impactAge / .22f) * Mathf.PI) * .34f;
                card.localScale = Vector3.one * (Mathf.Lerp(.72f, 1f, appear) + overshoot);
                card.anchoredPosition = basePosition + Vector2.up * (impactAge * 68f);
                AnimateGlow(comboMaterial, impactAccent, impactAge, .72f);
                AnimateGlow(streakMaterial, impactAccent, impactAge, .42f);
                AnimateShine(impactAge);
                if (impactRemaining <= 0f) card.gameObject.SetActive(false);
            }
            if (failureRemaining <= 0f) return;
            failureRemaining = Mathf.Max(0f, failureRemaining - Time.deltaTime);
            float age = failureDuration - failureRemaining;
            float fade = Mathf.Clamp01(failureRemaining / .28f);
            AnimateGlow(feedbackMaterial, feedbackAccent, age, paceActive ? .9f : .6f);
            if (paceActive)
            {
                float appear = Mathf.Clamp01(age / .12f);
                failureGroup.alpha = Mathf.Min(appear, fade);
                float pop = Mathf.Sin(Mathf.Clamp01(age / .32f) * Mathf.PI) * .34f;
                failureLabel.rectTransform.localScale = Vector3.one * (Mathf.Lerp(.65f, 1f, appear) + pop);
                failureLabel.rectTransform.anchoredPosition = new Vector2(0f, 120f + age * 36f);
                failureLabel.color = Color.Lerp(feedbackAccent, Color.white, .28f + .25f * Mathf.Sin(age * 30f));
            }
            else
            {
                failureGroup.alpha = fade;
                failureRoot.anchoredPosition = failureBasePosition + Vector2.right * (Mathf.Sin(age * 55f) * shake * Mathf.Exp(-age * 10f));
                failureLabel.rectTransform.localScale = Vector3.one * (1f + .15f * Mathf.Exp(-age * 12f));
            }
            foreach (Image edge in failureEdges)
                edge.color = paceActive
                    ? new Color(.32f, .88f, 1f, .75f * Mathf.Exp(-age * 2.4f) * fade)
                    : new Color(1f, .09f, .18f, .6f * Mathf.Exp(-age * 4f));
            if (failureRemaining <= 0f) ClearFailure();
        }

        private void OnDisable() => ClearFailure();

        private static void EnsureGlowMaterial(TextMeshProUGUI label, ref Material instance, float outline, float glow)
        {
            if (instance == null || label.fontSharedMaterial != instance)
            {
                if (instance != null) Destroy(instance);
                if (label.fontSharedMaterial == null) return;
                instance = new Material(label.fontSharedMaterial) { name = label.name + " Impact Glow" };
                instance.hideFlags = HideFlags.DontSave;
                instance.EnableKeyword("GLOW_ON");
                label.fontSharedMaterial = instance;
            }
            if (instance.HasProperty("_OutlineWidth")) instance.SetFloat("_OutlineWidth", outline);
            if (instance.HasProperty("_OutlineSoftness")) instance.SetFloat("_OutlineSoftness", .04f);
            if (instance.HasProperty("_GlowOuter")) instance.SetFloat("_GlowOuter", glow);
            if (instance.HasProperty("_GlowPower")) instance.SetFloat("_GlowPower", .55f);
        }

        private static void AnimateGlow(Material material, Color accent, float age, float strength)
        {
            if (material == null) return;
            float pulse = .65f + .35f * Mathf.Sin(age * 27f);
            Color glow = new Color(accent.r, accent.g, accent.b, strength * pulse);
            if (material.HasProperty("_OutlineColor")) material.SetColor("_OutlineColor", glow);
            if (material.HasProperty("_GlowColor")) material.SetColor("_GlowColor", glow);
        }

        private void AnimateShine(float age)
        {
            comboLabel.ForceMeshUpdate();
            TMP_TextInfo info = comboLabel.textInfo;
            float sweep = Mathf.Lerp(-340f, 340f, Mathf.Clamp01(age / .54f));
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible) continue;
                TMP_MeshInfo mesh = info.meshInfo[character.materialReferenceIndex];
                int vertex = character.vertexIndex;
                float center = (mesh.vertices[vertex].x + mesh.vertices[vertex + 2].x) * .5f;
                float shine = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Abs(center - sweep) / 100f);
                Color32 tint = Color.Lerp(impactAccent, Color.white, shine);
                for (int corner = 0; corner < 4; corner++) mesh.colors32[vertex + corner] = tint;
            }
            comboLabel.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        private void OnDestroy()
        {
            if (comboMaterial != null) Destroy(comboMaterial);
            if (streakMaterial != null) Destroy(streakMaterial);
            if (feedbackMaterial != null) Destroy(feedbackMaterial);
        }
    }
}
