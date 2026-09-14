using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI.Intro
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class IntroBackgroundLaserAnimator : MonoBehaviour
    {
        private const string ShaderPath = "UI/Intro/Shaders/IntroBackgroundLaser";

        [SerializeField, Range(0.05f, 2f)] private float speed = 0.24f;
        [SerializeField, Range(0f, 2f)] private float strength = 0.55f;
        [SerializeField, Range(0.005f, 0.2f)] private float width = 0.025f;

        private Image targetImage;
        private Material originalMaterial;
        private Material runtimeMaterial;

        private void OnEnable()
        {
            targetImage = GetComponent<Image>();
            originalMaterial = targetImage.material;
            Shader shader = Resources.Load<Shader>(ShaderPath);
            if (shader == null)
            {
                Debug.LogError($"Intro background laser shader was not found at Resources/{ShaderPath}.", this);
                enabled = false;
                return;
            }

            runtimeMaterial = new Material(shader) { name = "Intro Background Laser (Runtime)" };
            ApplyProperties();
            targetImage.material = runtimeMaterial;
        }

        private void OnValidate()
        {
            if (runtimeMaterial != null) ApplyProperties();
        }

        private void OnDisable()
        {
            if (targetImage != null) targetImage.material = originalMaterial;
            if (runtimeMaterial == null) return;
            if (Application.isPlaying) Destroy(runtimeMaterial);
            else DestroyImmediate(runtimeMaterial);
            runtimeMaterial = null;
        }

        private void ApplyProperties()
        {
            runtimeMaterial.SetFloat("_LaserSpeed", speed);
            runtimeMaterial.SetFloat("_LaserStrength", strength);
            runtimeMaterial.SetFloat("_LaserWidth", width);
        }
    }
}
