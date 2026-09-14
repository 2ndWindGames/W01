using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI
{
    /// <summary>CanvasScaler normally uses the full display even when its camera has a smaller viewport.</summary>
    [AddComponentMenu("UI/Viewport Canvas Scaler")]
    public sealed class ViewportCanvasScaler : CanvasScaler
    {
        protected override void HandleScaleWithScreenSize()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas.renderMode != RenderMode.ScreenSpaceCamera || canvas.worldCamera == null)
            {
                base.HandleScaleWithScreenSize();
                return;
            }

            Vector2 size = canvas.worldCamera.pixelRect.size;
            float widthScale = size.x / Mathf.Max(1f, referenceResolution.x);
            float heightScale = size.y / Mathf.Max(1f, referenceResolution.y);
            float scale = screenMatchMode == ScreenMatchMode.Expand ? Mathf.Min(widthScale, heightScale)
                : screenMatchMode == ScreenMatchMode.Shrink ? Mathf.Max(widthScale, heightScale)
                : Mathf.Pow(Mathf.Max(0.0001f, widthScale), 1f - matchWidthOrHeight)
                    * Mathf.Pow(Mathf.Max(0.0001f, heightScale), matchWidthOrHeight);
            SetScaleFactor(Mathf.Max(0.01f, scale));
            SetReferencePixelsPerUnit(referencePixelsPerUnit);
        }
    }
}
