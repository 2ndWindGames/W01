using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI.Popup
{
    /// <summary>Resolution-independent surfaces for the guide, with no decorative image padding.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HelpSurface : MaskableGraphic
    {
        public float Radius = 24f;
        public float BorderWidth = 2f;
        public Color BorderColor = new Color(.2f, .3f, .45f, 1f);
        public bool CutCorners;
        public bool InnerLine;
        public float GlowWidth;
        public Color BottomColor = Color.clear;

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Rect rect = GetPixelAdjustedRect();
            for (int layer = 4; layer > 0 && GlowWidth > 0f; layer--)
            {
                float spread = GlowWidth * layer / 4f;
                var glow = BorderColor;
                glow.a *= .025f * (5 - layer);
                Fill(vertices, Inset(rect, -spread), Radius + spread, glow, glow, CutCorners);
            }
            float border = Mathf.Clamp(BorderWidth, 0f, Mathf.Min(rect.width, rect.height) * .5f);
            if (border > 0f) Fill(vertices, rect, Radius, BorderColor, BorderColor, CutCorners);
            var bottom = BottomColor.a > 0f ? BottomColor : color;
            Fill(vertices, Inset(rect, border), Mathf.Max(0f, Radius - border), color, bottom, CutCorners);
            if (InnerLine)
            {
                float inset = border + 7f;
                var line = Color.Lerp(color, BorderColor, .35f);
                Fill(vertices, Inset(rect, inset), Mathf.Max(0f, Radius - inset), line, line, CutCorners);
                Fill(vertices, Inset(rect, inset + 1.5f), Mathf.Max(0f, Radius - inset - 1.5f), color, bottom, CutCorners);
            }
        }

        private static Rect Inset(Rect rect, float amount) =>
            new Rect(rect.x + amount, rect.y + amount, rect.width - amount * 2f, rect.height - amount * 2f);

        private static void Fill(VertexHelper vertices, Rect rect, float radius, Color top, Color bottom, bool cutCorners)
        {
            if (rect.width <= 0f || rect.height <= 0f) return;
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * .5f);
            int segments = cutCorners ? 1 : 12;
            int center = vertices.currentVertCount;
            vertices.AddVert(rect.center, Color.Lerp(bottom, top, .5f), Vector2.zero);
            for (int corner = 0; corner < 4; corner++)
            {
                var origin = new Vector2(corner < 2 ? rect.xMax - radius : rect.xMin + radius,
                    corner == 0 || corner == 3 ? rect.yMax - radius : rect.yMin + radius);
                for (int step = 0; step <= segments; step++)
                {
                    float angle = (90f - corner * 90f - step * 90f / segments) * Mathf.Deg2Rad;
                    var point = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    vertices.AddVert(point, Color.Lerp(bottom, top, (point.y - rect.yMin) / rect.height), Vector2.zero);
                }
            }
            int count = 4 * (segments + 1);
            for (int i = 0; i < count; i++)
                vertices.AddTriangle(center, center + 1 + i, center + 1 + (i + 1) % count);
        }
    }
}
