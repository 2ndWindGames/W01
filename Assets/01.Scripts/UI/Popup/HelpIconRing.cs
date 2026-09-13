using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI.Popup
{
    /// <summary>A thin ring with an empty center, so the game remains visible through it.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HelpIconRing : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            var rect = GetPixelAdjustedRect();
            float outer = Mathf.Min(rect.width, rect.height) * .5f;
            if (outer <= 0f) return;
            float inner = Mathf.Max(0f, outer - 2.5f);
            const int segments = 64;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vertices.AddVert(rect.center + direction * outer, color, Vector2.zero);
                vertices.AddVert(rect.center + direction * inner, color, Vector2.zero);
            }
            for (int i = 0; i < segments; i++)
            {
                int current = i * 2;
                int next = ((i + 1) % segments) * 2;
                vertices.AddTriangle(current, next, current + 1);
                vertices.AddTriangle(current + 1, next, next + 1);
            }
        }
    }
}
