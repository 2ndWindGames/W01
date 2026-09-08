using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI.Intro
{
    [DisallowMultipleComponent]
    public sealed class IntroMotionAnimator : MonoBehaviour
    {
        [Header("Motion")]
        [SerializeField] private float titleFloatDistance = 10f;
        [SerializeField] private float titleFloatSpeed = 0.55f;
        [SerializeField] private float backgroundDriftDistance = 6f;
        [SerializeField] private float backgroundDriftSpeed = 0.12f;
        [SerializeField] private float startPulseSpeed = 1.15f;
        [SerializeField, Range(0f, 0.1f)] private float startScalePulse = 0.018f;
        [SerializeField, Range(0f, 1f)] private float startMinimumAlpha = 0.62f;

        private RectTransform background;
        private RectTransform title;
        private RectTransform startButton;
        private CanvasGroup titleCanvasGroup;
        private CanvasGroup startCanvasGroup;
        private Vector2 backgroundBasePosition;
        private Vector2 titleBasePosition;
        private Vector3 titleBaseScale;
        private Vector3 startBaseScale;
        private float enabledTime;
        private bool initialized;

        private void OnEnable()
        {
            if (initialized)
                enabledTime = Time.unscaledTime;
        }

        public void Initialize()
        {
            if (initialized)
                return;

            background = FindRectTransform("img_bg");
            title = FindRectTransform("img_title");
            startButton = FindRectTransform("btn_start");
            var energyCore = FindRectTransform("img_energy_core");

            if (background != null)
                backgroundBasePosition = background.anchoredPosition;

            if (title != null)
            {
                titleBasePosition = title.anchoredPosition;
                titleBaseScale = title.localScale;
                titleCanvasGroup = GetOrAddCanvasGroup(title.gameObject);
            }

            if (startButton != null)
            {
                startBaseScale = startButton.localScale;
                startCanvasGroup = GetOrAddCanvasGroup(startButton.gameObject);
            }

            if (energyCore != null && energyCore.GetComponent<Image>() != null &&
                energyCore.GetComponent<IntroCoreAnimator>() == null)
            {
                energyCore.gameObject.AddComponent<IntroCoreAnimator>();
            }

            enabledTime = Time.unscaledTime;
            initialized = true;
        }

        private void LateUpdate()
        {
            if (!initialized)
                return;

            var time = Time.unscaledTime;
            var elapsed = time - enabledTime;
            var entrance = EaseOutCubic(Mathf.Clamp01(elapsed / 0.65f));

            if (background != null)
            {
                var drift = Mathf.Sin(time * backgroundDriftSpeed * Mathf.PI * 2f) *
                            backgroundDriftDistance;
                background.anchoredPosition = backgroundBasePosition + Vector2.up * drift;
            }

            if (title != null)
            {
                var wave = Mathf.Sin(time * titleFloatSpeed * Mathf.PI * 2f);
                title.anchoredPosition = titleBasePosition +
                                         Vector2.up * (wave * titleFloatDistance + (1f - entrance) * 55f);
                title.localRotation = Quaternion.Euler(0f, 0f, wave * 0.25f);
                title.localScale = titleBaseScale * (1f + wave * 0.006f);
                titleCanvasGroup.alpha = entrance;
            }

            if (startButton != null)
            {
                var pulse = (Mathf.Sin(time * startPulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
                startButton.localScale = startBaseScale * (1f + pulse * startScalePulse);
                startCanvasGroup.alpha = Mathf.Lerp(startMinimumAlpha, 1f, pulse) * entrance;
            }
        }

        private void OnDisable()
        {
            if (!initialized)
                return;

            if (background != null)
                background.anchoredPosition = backgroundBasePosition;

            if (title != null)
            {
                title.anchoredPosition = titleBasePosition;
                title.localRotation = Quaternion.identity;
                title.localScale = titleBaseScale;
                titleCanvasGroup.alpha = 1f;
            }

            if (startButton != null)
            {
                startButton.localScale = startBaseScale;
                startCanvasGroup.alpha = 1f;
            }
        }

        private RectTransform FindRectTransform(string objectName)
        {
            foreach (var child in GetComponentsInChildren<RectTransform>(true))
            {
                if (child.name == objectName)
                    return child;
            }

            Debug.LogWarning($"Intro motion target '{objectName}' was not found.", this);
            return null;
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            var canvasGroup = target.GetComponent<CanvasGroup>();
            return canvasGroup != null ? canvasGroup : target.AddComponent<CanvasGroup>();
        }

        private static float EaseOutCubic(float value)
        {
            return 1f - Mathf.Pow(1f - value, 3f);
        }
    }
}
