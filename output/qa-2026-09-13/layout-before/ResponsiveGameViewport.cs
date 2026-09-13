using UnityEngine;

namespace _01.Scripts.UI
{
    /// <summary>Keeps the portrait playfield usable inside a resizable, edge-to-edge window.</summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ResponsiveGameViewport : MonoBehaviour
    {
        [SerializeField, Range(0.3f, 1f)] private float maximumAspect = 9f / 16f;
        [SerializeField] private Color surroundColor = new Color(0.025f, 0.035f, 0.085f, 1f);
        private Camera viewCamera;
        private Camera surroundCamera;
        private Rect originalRect;
        private float originalSize;
        private float bannerHeight;

        private void OnEnable()
        {
            viewCamera = GetComponent<Camera>();
            originalRect = viewCamera.rect;
            originalSize = viewCamera.orthographicSize;
            // Clear the whole window, including cutouts and the area beside the portrait field.
            var background = new GameObject("Screen Surround", typeof(Camera));
            background.transform.SetParent(transform, false);
            surroundCamera = background.GetComponent<Camera>();
            surroundCamera.clearFlags = CameraClearFlags.SolidColor;
            surroundCamera.backgroundColor = surroundColor;
            surroundCamera.cullingMask = 0;
            surroundCamera.depth = viewCamera.depth - 100f;
            surroundCamera.targetDisplay = viewCamera.targetDisplay;
            surroundCamera.allowHDR = false;
            surroundCamera.allowMSAA = false;
            Apply();
        }

        /// <param name="pixels">Banner top measured from the window bottom, including the bottom safe inset.</param>
        public void SetBannerTop(float pixels)
        {
            bannerHeight = pixels > 0f ? Mathf.Max(0f, pixels - Screen.safeArea.yMin) : 0f;
            Apply();
        }

        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (viewCamera == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect pixels = CalculatePixelRect(Screen.width, Screen.height, Screen.safeArea,
                bannerHeight, maximumAspect);
            var normalized = new Rect(pixels.x / Screen.width, pixels.y / Screen.height,
                pixels.width / Screen.width, pixels.height / Screen.height);
            if (viewCamera.rect != normalized) viewCamera.rect = normalized;
            if (viewCamera.orthographic)
            {
                // Narrow split-screen windows must not crop the world-space tap targets.
                float aspect = pixels.width / pixels.height;
                viewCamera.orthographicSize = originalSize * Mathf.Max(1f, (1080f / 2340f) / aspect);
            }
        }

        public static Rect CalculatePixelRect(float width, float height, Rect safe, float banner, float maxAspect)
        {
            width = Mathf.Max(1f, width);
            height = Mathf.Max(1f, height);
            float left = Mathf.Clamp(safe.xMin, 0f, width);
            float right = Mathf.Clamp(safe.xMax, 0f, width);
            float bottom = Mathf.Clamp(safe.yMin, 0f, height);
            float top = Mathf.Clamp(safe.yMax, 0f, height);
            // Some devices report an empty safe area while rotating or restoring the window.
            if (right <= left || top <= bottom)
            {
                left = bottom = 0f;
                right = width;
                top = height;
            }
            bottom += Mathf.Clamp(banner, 0f, (top - bottom) * 0.25f);
            float availableWidth = right - left;
            float playWidth = Mathf.Min(availableWidth, (top - bottom) * Mathf.Max(0.1f, maxAspect));
            return new Rect(left + (availableWidth - playWidth) * 0.5f, bottom, playWidth, top - bottom);
        }

        private void OnDisable()
        {
            if (viewCamera != null)
            {
                viewCamera.rect = originalRect;
                viewCamera.orthographicSize = originalSize;
            }
            if (surroundCamera != null) Destroy(surroundCamera.gameObject);
        }
    }
}
