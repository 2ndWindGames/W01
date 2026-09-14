using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI.Intro
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class IntroTitleCurrentAnimator : MonoBehaviour
    {
        private const string ShaderResourcePath = "UI/Intro/Shaders/IntroTitleCurrent";

        [SerializeField, Range(0f, 1f)] private float flowStrength = 0.78f;
        [SerializeField, Range(0f, 1f)] private float sparkStrength = 0.76f;

        private static readonly int EffectTimeId = Shader.PropertyToID("_EffectTime");
        private static readonly int FlowStrengthId = Shader.PropertyToID("_FlowStrength");
        private static readonly int SparkStrengthId = Shader.PropertyToID("_SparkStrength");

        private Image titleImage;
        private Material originalMaterial;
        private Material effectMaterial;

        private void OnEnable()
        {
            titleImage = GetComponent<Image>();
            originalMaterial = titleImage.material;
            var shader = Resources.Load<Shader>(ShaderResourcePath);
            if (shader == null)
            {
                Debug.LogError($"Intro title shader was not found at Resources/{ShaderResourcePath}.", this);
                enabled = false;
                return;
            }

            effectMaterial = new Material(shader) { name = "Intro Title Current (Runtime)" };
            effectMaterial.SetFloat(FlowStrengthId, flowStrength);
            effectMaterial.SetFloat(SparkStrengthId, sparkStrength);
            titleImage.material = effectMaterial;
        }

        private void Update()
        {
            if (effectMaterial != null)
                effectMaterial.SetFloat(EffectTimeId, Time.unscaledTime);
        }

        private void OnDisable()
        {
            if (titleImage != null)
                titleImage.material = originalMaterial;
            if (effectMaterial == null)
                return;

            if (Application.isPlaying)
                Destroy(effectMaterial);
            else
                DestroyImmediate(effectMaterial);
            effectMaterial = null;
        }
    }
}
