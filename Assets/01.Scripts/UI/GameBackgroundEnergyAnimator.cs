using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class GameBackgroundEnergyAnimator : MonoBehaviour
    {
        private const string ShaderPath = "UI/NeonSignalPack/Shaders/GameBackgroundEnergy";

        [Header("Neon flow")]
        [SerializeField, Range(0.02f, 1f)] private float flowSpeed = 0.35f;
        [SerializeField, Range(0f, 1f)] private float flowStrength = 0.75f;
        [Header("Edge sparks")]
        [SerializeField, Min(0.5f)] private float minimumSparkInterval = 0.8f;
        [SerializeField, Min(0.5f)] private float maximumSparkInterval = 1.8f;
        [SerializeField, Range(0.05f, 0.3f)] private float sparkDuration = 0.20f;
        [SerializeField, Range(0f, 1.5f)] private float sparkStrength = 1.1f;

        private Image targetImage;
        private Material originalMaterial;
        private Material runtimeMaterial;
        private float nextSparkTime;
        private float sparkStartTime;
        private bool sparkActive;

        private void OnEnable()
        {
            targetImage = GetComponent<Image>();
            originalMaterial = targetImage.material;
            Shader shader = Resources.Load<Shader>(ShaderPath);
            if (shader == null)
            {
                Debug.LogError($"Game background energy shader was not found at Resources/{ShaderPath}.", this);
                enabled = false;
                return;
            }

            runtimeMaterial = new Material(shader) { name = "Game Background Energy (Runtime)" };
            runtimeMaterial.SetFloat("_ArcActive", 0f);
            ApplySettings();
            targetImage.material = runtimeMaterial;
            sparkActive = false;
            ScheduleNextSpark();
        }

        private void Update()
        {
            if (runtimeMaterial == null) return;

            float now = Time.unscaledTime;
            if (!sparkActive && now >= nextSparkTime)
            {
                sparkActive = true;
                sparkStartTime = now;
                runtimeMaterial.SetFloat("_ArcX", Random.value < 0.5f
                    ? Random.Range(0.045f, 0.10f) : Random.Range(0.90f, 0.955f));
                runtimeMaterial.SetFloat("_ArcY", Random.Range(0.22f, 0.72f));
                runtimeMaterial.SetFloat("_ArcLength", Random.Range(0.10f, 0.22f));
                runtimeMaterial.SetFloat("_ArcSeed", Random.Range(0f, 1000f));
                runtimeMaterial.SetColor("_ArcColor", Random.value < 0.5f
                    ? new Color(0.35f, 0.85f, 1f) : new Color(0.8f, 0.48f, 1f));
            }

            if (!sparkActive) return;
            float progress = (now - sparkStartTime) / sparkDuration;
            if (progress >= 1f)
            {
                runtimeMaterial.SetFloat("_ArcActive", 0f);
                sparkActive = false;
                ScheduleNextSpark();
                return;
            }

            float crackle = 0.75f + 0.45f * Mathf.Abs(Mathf.Sin(progress * 6f * Mathf.PI));
            float flash = Mathf.Sin(progress * Mathf.PI) * crackle * sparkStrength;
            runtimeMaterial.SetFloat("_ArcActive", flash);
        }

        private void OnValidate()
        {
            maximumSparkInterval = Mathf.Max(maximumSparkInterval, minimumSparkInterval);
            if (runtimeMaterial != null) ApplySettings();
        }

        private void OnDisable()
        {
            if (targetImage != null) targetImage.material = originalMaterial;
            if (runtimeMaterial == null) return;
            if (Application.isPlaying) Destroy(runtimeMaterial);
            else DestroyImmediate(runtimeMaterial);
            runtimeMaterial = null;
        }

        private void ScheduleNextSpark()
        {
            nextSparkTime = Time.unscaledTime + Random.Range(minimumSparkInterval, maximumSparkInterval);
        }

        private void ApplySettings()
        {
            runtimeMaterial.SetFloat("_FlowSpeed", flowSpeed);
            runtimeMaterial.SetFloat("_FlowStrength", flowStrength);
        }
    }
}
