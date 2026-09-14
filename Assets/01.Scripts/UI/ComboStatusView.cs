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
        private float punch;
        private float failureRemaining;
        private float failureDuration;
        private float shake;
        private bool paused;
        private Vector2 basePosition;
        private int lastCombo = -1, lastMultiplier, lastNextFever, lastStage, lastFeverTenth = -1;
        private bool lastPlaying;

        private void Awake() => basePosition = card.anchoredPosition;
        public void SetPaused(bool value) => paused = value;

        public void SetState(int combo, int multiplier, int nextFever, int feverStep, float feverTime,
            float feverMaximum, int speedStage, bool playing)
        {
            int tenth = Mathf.CeilToInt(feverTime * 10f);
            if (combo == lastCombo && multiplier == lastMultiplier && nextFever == lastNextFever
                && speedStage == lastStage && tenth == lastFeverTenth && playing == lastPlaying) return;
            lastCombo = combo; lastMultiplier = multiplier; lastNextFever = nextFever;
            lastStage = speedStage; lastFeverTenth = tenth; lastPlaying = playing;
            card.gameObject.SetActive(playing);
            if (!playing) { ClearFailure(); previousCombo = 0; punch = 0f; return; }
            if (combo > previousCombo) punch = 1f;
            previousCombo = combo;
            bool fever = feverTime > 0f;
            var accent = fever ? new Color(1f, .28f, .9f)
                : multiplier >= 2 ? new Color(1f, .8f, .18f) : new Color(.2f, .9f, 1f);
            panel.color = new Color(accent.r * .17f, accent.g * .17f, accent.b * .17f, .98f);
            comboLabel.text = combo.ToString("00") + "  COMBO";
            comboLabel.color = accent;
            multiplierLabel.text = "x" + multiplier + GameLocalization.T(" SCORE", " 점수");
            multiplierLabel.color = accent;
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
            failureDuration = lostCombo >= 5 || bomb ? .85f : .5f;
            failureRemaining = failureDuration;
            shake = lostCombo >= 5 || bomb ? 14f : 7f;
            failureLabel.text = bomb ? GameLocalization.T("BOMB!  −2 SEC", "폭탄!  −2초")
                : lostCombo > 0 ? lostCombo + GameLocalization.T(" COMBO LOST!", " 콤보 끊김!")
                : GameLocalization.T("MISSED!", "아깝다!");
            failureRoot.gameObject.SetActive(true);
            failureGroup.alpha = 1f;
        }

        public void ClearFailure()
        {
            failureRemaining = 0f;
            if (failureRoot != null) failureRoot.gameObject.SetActive(false);
            if (card != null) { card.anchoredPosition = basePosition; card.localScale = Vector3.one; }
        }

        private void Update()
        {
            if (paused) return;
            punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 5f);
            card.localScale = Vector3.one * (1f + .065f * punch);
            if (failureRemaining <= 0f) return;
            failureRemaining = Mathf.Max(0f, failureRemaining - Time.deltaTime);
            float age = failureDuration - failureRemaining;
            float fade = Mathf.Clamp01(failureRemaining / .28f);
            failureGroup.alpha = fade;
            // Shake the status card, never the world/camera or the player's touch targets.
            card.anchoredPosition = basePosition + Vector2.right * (Mathf.Sin(age * 55f) * shake * Mathf.Exp(-age * 10f));
            failureLabel.rectTransform.localScale = Vector3.one * (1f + .15f * Mathf.Exp(-age * 12f));
            foreach (Image edge in failureEdges)
                edge.color = new Color(1f, .09f, .18f, .6f * Mathf.Exp(-age * 4f));
            if (failureRemaining <= 0f) ClearFailure();
        }

        private void OnDisable() => ClearFailure();
    }
}
