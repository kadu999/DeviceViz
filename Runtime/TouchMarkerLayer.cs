using DevicePipe;
using UnityEngine;
using UnityEngine.UI;

namespace DeviceViz
{
    /// <summary>
    /// Touch marker overlay layer.
    /// Renders pressure touch points as colored circles using UI Images.
    /// </summary>
    public class TouchMarkerLayer : VizLayer
    {
        [Header("Markers")]
        [SerializeField] private int _maxMarkers = 20;
        [SerializeField] private Color _markerColor = new Color(1, 0, 0, 1);

        public override Texture texture => null;

        private RectTransform[] _markers;
        private Image[] _images;
        private static Sprite _circle;

        // ─── VizLayer overrides ──────────────

        public override void UpdateData(int[] newData, int width, int height) { }

        public override void UpdateTouches(PressureInfo[] touches, int width, int height)
        {
            var rt = (RectTransform)transform;
            float scale = rt.rect.width / width;

            for (int i = 0; i < _markers.Length; i++)
            {
                if (i < touches.Length)
                {
                    var t = touches[i];
                    _markers[i].anchoredPosition = new Vector2(t.y * scale, t.x * scale);
                    _markers[i].sizeDelta = Vector2.one * t.radius * 2f * scale;
                    _images[i].color = new Color(_markerColor.r, _markerColor.g, _markerColor.b, t.pressure / 100f);
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
            _markers = new RectTransform[_maxMarkers];
            _images = new Image[_maxMarkers];
            for (int i = 0; i < _maxMarkers; i++)
                (_markers[i], _images[i]) = CreateMarker(i);
        }

        (RectTransform, Image) CreateMarker(int i)
        {
            var go = new GameObject($"Touch{i}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsLastSibling();
            var img = go.GetComponent<Image>();
            img.sprite = CircleSprite;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            go.SetActive(false);
            return (rt, img);
        }

        static Sprite CircleSprite
        {
            get
            {
                if (_circle == null)
                {
                    int s = 64;
                    var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
                    var colors = new Color[s * s];
                    float r = s / 2f;
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                            colors[y * s + x] = (x - r) * (x - r) + (y - r) * (y - r) <= r * r ? Color.white : Color.clear;
                    tex.SetPixels(colors);
                    tex.Apply();
                    _circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
                }
                return _circle;
            }
        }
    }
}
