using DevicePipe;
using UnityEngine;
using UnityEngine.UI;

namespace DeviceViz
{
    /// <summary>
    /// Touch marker overlay layer.
    /// Renders pressure touch points the same way ring_pressure_viewer.py does:
    /// a hollow ring whose diameter follows the touch radius (stroke width fixed,
    /// min radius 4px), a fixed white center dot, and per-touch colors taken from
    /// an HSV palette cycled by index (touch list is sorted by pressure desc).
    /// </summary>
    public class TouchMarkerLayer : VizLayer
    {
        [Header("Markers")]
        [SerializeField] private int _maxMarkers = 20;

        private RectTransform[] _markers;
        private RingGraphic[] _rings;
        private Color[] _palette;

        // ─── VizLayer overrides ──────────────

        public override bool needsTouches => true;

        public override void UpdateData(int[] newData, int width, int height) { }

        public override void UpdateTouches(PressureInfo[] touches, int width, int height)
        {
            if (_markers == null)
            {
                return;
            }

            var rt = (RectTransform)transform;
            float scale = rt.rect.width / width;

            for (int i = 0; i < _markers.Length; i++)
            {
                if (i < touches.Length)
                {
                    var t = touches[i];
                    _markers[i].anchoredPosition = new Vector2(t.y * scale, t.x * scale);
                    // python: radius = max(int(r * scale), 4) → diameter = 2 * radius
                    float diameter = Mathf.Max(t.radius * 2f * scale, 8f);
                    _markers[i].sizeDelta = Vector2.one * diameter;
                    // python: color = traj_colors[i % MAX_TRACKS] (opaque)
                    _rings[i].color = _palette[i % _palette.Length];
                    _markers[i].gameObject.SetActive(true);
                }
                else
                {
                    _markers[i].gameObject.SetActive(false);
                }
            }
        }

        public override void Clear()
        {
            if (_markers == null) return;
            foreach (var m in _markers)
                if (m) m.gameObject.SetActive(false);
        }

        // ─── Internal ────────────────────────

        void Awake()
        {
            // python traj_colors: HSV(h = i / MAX_TRACKS, 255, 255) → BGR
            _palette = new Color[_maxMarkers];
            for (int i = 0; i < _maxMarkers; i++)
                _palette[i] = Color.HSVToRGB(i / (float)_maxMarkers, 1f, 1f);

            _markers = new RectTransform[_maxMarkers];
            _rings = new RingGraphic[_maxMarkers];
            for (int i = 0; i < _maxMarkers; i++)
                (_markers[i], _rings[i]) = CreateMarker(i);
        }

        (RectTransform, RingGraphic) CreateMarker(int i)
        {
            var go = new GameObject($"Touch{i}",
                                    typeof(RectTransform),
                                    typeof(CanvasRenderer),
                                    typeof(RingGraphic));
            go.transform.SetParent(transform, false);
            go.transform.SetAsLastSibling();
            var ring = go.GetComponent<RingGraphic>();
            ring.raycastTarget = false;
            // python: cv2.circle(overlay, center, 3, (255,255,255), -1) — white center dot
            ring.DrawCenterDot = true;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            go.SetActive(false);
            return (rt, ring);
        }
    }
}
