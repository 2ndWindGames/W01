using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI.Intro
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class IntroCoreAnimator : MonoBehaviour
    {
        private const string ShaderResourcePath = "UI/Intro/Shaders/IntroCoreWobble";

        [Header("Transform Animation")]
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField, Range(0f, 0.15f)] private float scalePulse = 0.025f;
        [SerializeField] private float scalePulseSpeed = 1.2f;

        [Header("Shader Animation")]
        [SerializeField, Range(0f, 0.04f)] private float wobbleStrength = 0.009f;
        [SerializeField] private float wobbleSpeed = 1.4f;
        [SerializeField, Range(0f, 0.3f)] private float twistStrength = 0.075f;
        [SerializeField, Range(0f, 0.5f)] private float brightnessPulse = 0.12f;
        [SerializeField] private float brightnessPulseSpeed = 1.8f;

        private RectTransform rectTransform;
        private Image targetImage;
        private Material runtimeMaterial;
        private Material originalMaterial;
        private Vector3 baseScale;

        private void OnEnable()
        {
            rectTransform = (RectTransform)transform;
            targetImage = GetComponent<Image>();
            baseScale = rectTransform.localScale;
            originalMaterial = targetImage.material;

            var shader = Resources.Load<Shader>(ShaderResourcePath);
            if (shader == null)
            {
                Debug.LogError($"Intro core shader was not found at Resources/{ShaderResourcePath}.", this);
                enabled = false;
                return;
            }

            runtimeMaterial = new Material(shader)
            {
                name = "Intro Core Wobble (Runtime)"
            };
            ApplyMaterialProperties();
            targetImage.material = runtimeMaterial;
        }

        private void Update()
        {
            var time = Time.unscaledTime;
            rectTransform.Rotate(0f, 0f, -rotationSpeed * Time.unscaledDeltaTime);
            var pulse = 1f + Mathf.Sin(time * scalePulseSpeed * Mathf.PI * 2f) * scalePulse;
            rectTransform.localScale = baseScale * pulse;
        }

        private void OnValidate()
        {
            if (runtimeMaterial != null)
                ApplyMaterialProperties();
        }

        private void OnDisable()
        {
            if (targetImage != null)
                targetImage.material = originalMaterial;

            if (rectTransform != null)
                rectTransform.localScale = baseScale;

            if (runtimeMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(runtimeMaterial);
                else
                    DestroyImmediate(runtimeMaterial);
            }
        }

        private void ApplyMaterialProperties()
        {
            runtimeMaterial.SetFloat("_WobbleStrength", wobbleStrength);
            runtimeMaterial.SetFloat("_WobbleSpeed", wobbleSpeed);
            runtimeMaterial.SetFloat("_TwistStrength", twistStrength);
            runtimeMaterial.SetFloat("_PulseAmount", brightnessPulse);
            runtimeMaterial.SetFloat("_PulseSpeed", brightnessPulseSpeed);
        }
    }
}
