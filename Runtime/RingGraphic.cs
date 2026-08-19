using UnityEngine;
using UnityEngine.UI;

namespace DeviceViz
{
    /// <summary>
    /// Hollow ring (annulus) UI graphic with a fixed stroke thickness, plus an
    /// optional fixed-size filled center dot.
    /// The outer diameter follows the RectTransform size (so the ring grows/shrinks
    /// with the touch radius), while the stroke width stays constant — mirroring
    /// cv2.circle(..., thickness=2) + cv2.circle(..., radius=3, thickness=-1)
    /// in ring_pressure_viewer.py.
    /// </summary>
    public class RingGraphic : MaskableGraphic
    {
        [SerializeField] private float _thickness = 2f;
        [SerializeField, Range(0f, 1f)] private float _outerFactor = 0.5f;
        [SerializeField] private int _segments = 48;
        [SerializeField] private bool _drawCenterDot;
        [SerializeField] private float _centerDotRadius = 3f;
        [SerializeField] private Color _centerDotColor = Color.white;

        public float Thickness
        {
            get => _thickness;
            set { _thickness = Mathf.Max(0.01f, value); SetVerticesDirty(); }
        }

        public bool DrawCenterDot
        {
            get => _drawCenterDot;
            set { _drawCenterDot = value; SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var r = rectTransform.rect;
            float cx = r.center.x, cy = r.center.y;
            var uv = Vector2.zero;

            // ── Ring (annulus) ──
            float outer = Mathf.Min(r.width, r.height) * _outerFactor;
            if (outer > 0f)
            {
                float inner = Mathf.Max(0f, outer - _thickness);
                int seg = Mathf.Max(4, _segments);
                int baseIdx = 0;
                for (int i = 0; i < seg; i++)
                {
                    float a0 = i * (Mathf.PI * 2f) / seg;
                    float a1 = (i + 1) * (Mathf.PI * 2f) / seg;
                    float c0 = Mathf.Cos(a0), s0 = Mathf.Sin(a0);
                    float c1 = Mathf.Cos(a1), s1 = Mathf.Sin(a1);

                    vh.AddVert(new UIVertex { position = new Vector2(cx + outer * c0, cy + outer * s0), color = color, uv0 = uv });
                    vh.AddVert(new UIVertex { position = new Vector2(cx + outer * c1, cy + outer * s1), color = color, uv0 = uv });
                    vh.AddVert(new UIVertex { position = new Vector2(cx + inner * c1, cy + inner * s1), color = color, uv0 = uv });
                    vh.AddVert(new UIVertex { position = new Vector2(cx + inner * c0, cy + inner * s0), color = color, uv0 = uv });

                    vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
                    vh.AddTriangle(baseIdx, baseIdx + 2, baseIdx + 3);
                    baseIdx += 4;
                }
            }

            // ── Center dot (fixed radius, independent of the ring size) ──
            if (_drawCenterDot && _centerDotRadius > 0f)
            {
                int seg = Mathf.Max(8, _segments / 2);
                int centerIdx = vh.currentVertCount;
                vh.AddVert(new UIVertex { position = new Vector2(cx, cy), color = _centerDotColor, uv0 = uv });
                int firstRing = vh.currentVertCount;
                for (int i = 0; i <= seg; i++)
                {
                    float a = i * (Mathf.PI * 2f) / seg;
                    vh.AddVert(new UIVertex
                    {
                        position = new Vector2(cx + _centerDotRadius * Mathf.Cos(a),
                                               cy + _centerDotRadius * Mathf.Sin(a)),
                        color = _centerDotColor,
                        uv0 = uv,
                    });
                }
                for (int i = 0; i < seg; i++)
                {
                    vh.AddTriangle(centerIdx, firstRing + i, firstRing + i + 1);
                }
            }
        }
    }
}
