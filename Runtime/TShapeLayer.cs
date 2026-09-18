using DevicePipe;
using UnityEngine;
using UnityEngine.UI;

namespace DeviceViz
{
    /// <summary>
    /// T-shaped stamp ("印章") overlay layer.
    ///
    /// Draws what the python viewer's render loop draws for detect_tshapes
    /// (ring_pressure_viewer.py:718-748): a red junction dot, the yellow bar, the
    /// cyan stem, the magenta ID sampling pair and green endpoint dots, plus the
    /// decoded stamp identity ("ID:15 S:0") next to the junction.
    ///
    /// Lines are plain rotated <see cref="Image"/> quads, so no custom Graphic is
    /// needed.  Sensor coordinates follow <see cref="TShapeInfo"/>: Row is the slow
    /// index, Col the fast one — hence the same (Col → UI x, Row → UI y) mapping
    /// TouchMarkerLayer and ChessPieceLayer use.
    /// </summary>
    public class TShapeLayer : VizLayer
    {
        [Header("Stamps")]
        [SerializeField] private int _maxShapes = 8;
        [SerializeField] private float _lineThickness = 2f;
        [SerializeField] private int _idFontSize = 13;
        [SerializeField] private bool _showIdSamplePoints = true;

        private RectTransform[] _roots;
        private RectTransform[] _barLines, _stemLines, _idLines;
        private Image[] _barImages, _stemImages, _idImages;
        private RectTransform[] _junctionDots, _stemDots;
        private Image[] _junctionImages, _stemImages2;
        private RectTransform[] _endpointDots;
        private Image[] _endpointImages;
        private RectTransform[] _idRects;
        private Text[] _idTexts;

        private bool[] _slotActive;
        private int[] _lastIds;
        private int[] _lastShapes;

        private static Sprite _dotSprite;
        private static Sprite _quadSprite;
        private static Font _font;

        public override bool needsTShapes => true;

        public override void UpdateData(int[] newData, int width, int height) { }

        // ─── Rendering ───────────────────────

        public override void UpdateTShapes(TShapeInfo[] shapes, int width, int height)
        {
            if (_roots == null) return;

            var rt = (RectTransform)transform;
            float scale = rt.rect.width / Mathf.Max(1, width);
            int count = shapes?.Length ?? 0;
            if (count > _maxShapes) count = _maxShapes;

            for (int i = 0; i < _maxShapes; i++)
            {
                bool active = i < count;
                if (active != _slotActive[i])
                {
                    _slotActive[i] = active;
                    _roots[i].gameObject.SetActive(active);
                }
                if (!active) continue;

                var s = shapes[i];

                // ── junction / stem tip dots ──
                Place(_junctionDots[i], s.junctionCol, s.junctionRow, scale);
                _junctionDots[i].sizeDelta = Vector2.one * 8f;
                Place(_stemDots[i], s.stemCol, s.stemRow, scale);
                _stemDots[i].sizeDelta = Vector2.one * 8f;

                // ── bar (yellow) and stem (cyan) ──
                SetLine(_barLines[i], s.bar0Col, s.bar0Row, s.bar1Col, s.bar1Row, scale);
                SetLine(_stemLines[i], s.junctionCol, s.junctionRow, s.stemCol, s.stemRow, scale);

                // ── the two ID sampling pairs (magenta) ──
                if (_showIdSamplePoints)
                {
                    SetLine(_idLines[i * 2], s.p0Col, s.p0Row, s.p1Col, s.p1Row, scale);
                    SetLine(_idLines[i * 2 + 1], s.p2Col, s.p2Row, s.p3Col, s.p3Row, scale);
                }

                // ── endpoint dots (green) ──
                if (_showIdSamplePoints)
                {
                    Place(_endpointDots[i * 3], s.ep0Col, s.ep0Row, scale);
                    Place(_endpointDots[i * 3 + 1], s.ep1Col, s.ep1Row, scale);
                    Place(_endpointDots[i * 3 + 2], s.ep2Col, s.ep2Row, scale);
                    for (int k = 0; k < 3; k++)
                        _endpointDots[i * 3 + k].sizeDelta = Vector2.one * 6f;
                }

                // ── identity label ──
                Place(_idRects[i], s.junctionCol, s.junctionRow, scale);
                _idRects[i].anchoredPosition += new Vector2(0f, 16f);
                if (_lastIds[i] != s.id || _lastShapes[i] != s.shape)
                {
                    _lastIds[i] = s.id;
                    _lastShapes[i] = s.shape;
                    _idTexts[i].text = $"ID:{s.id} S:{s.shape}";
                }
            }
        }

        public override void Clear()
        {
            if (_roots == null) return;
            for (int i = 0; i < _maxShapes; i++)
            {
                _slotActive[i] = false;
                if (_roots[i]) _roots[i].gameObject.SetActive(false);
                _lastIds[i] = -1;
                _lastShapes[i] = -1;
            }
        }

        void OnDisable() { Clear(); }

        // ─── Internal ────────────────────────

        static void Place(RectTransform rt, int col, int row, float scale)
        {
            rt.anchoredPosition = new Vector2(col * scale, row * scale);
        }

        void SetLine(RectTransform rt, int c0, int r0, int c1, int r1, float scale)
        {
            float x0 = c0 * scale, y0 = r0 * scale;
            float x1 = c1 * scale, y1 = r1 * scale;
            float dx = x1 - x0, dy = y1 - y0;
            rt.anchoredPosition = new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
            rt.sizeDelta = new Vector2(Mathf.Sqrt(dx * dx + dy * dy), _lineThickness);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
        }

        void Awake()
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _roots = new RectTransform[_maxShapes];
            _barLines = new RectTransform[_maxShapes];
            _barImages = new Image[_maxShapes];
            _stemLines = new RectTransform[_maxShapes];
            _stemImages = new Image[_maxShapes];
            _idLines = new RectTransform[_maxShapes * 2];
            _idImages = new Image[_maxShapes * 2];
            _junctionDots = new RectTransform[_maxShapes];
            _junctionImages = new Image[_maxShapes];
            _stemDots = new RectTransform[_maxShapes];
            _stemImages2 = new Image[_maxShapes];
            _endpointDots = new RectTransform[_maxShapes * 3];
            _endpointImages = new Image[_maxShapes * 3];
            _idRects = new RectTransform[_maxShapes];
            _idTexts = new Text[_maxShapes];
            _slotActive = new bool[_maxShapes];
            _lastIds = new int[_maxShapes];
            _lastShapes = new int[_maxShapes];

            for (int i = 0; i < _maxShapes; i++)
            {
                // one root per stamp keeps activate/deactivate to a single call
                var rootGo = new GameObject($"TShape{i}", typeof(RectTransform));
                rootGo.transform.SetParent(transform, false);
                var root = (RectTransform)rootGo.transform;
                root.anchorMin = root.anchorMax = Vector2.zero;
                root.pivot = new Vector2(0.5f, 0.5f);
                root.sizeDelta = Vector2.zero;
                _roots[i] = root;
                _lastIds[i] = -1;
                _lastShapes[i] = -1;

                (_barLines[i], _barImages[i]) = Child<Image>(root, $"Bar{i}", QuadSprite);
                _barImages[i].color = new Color(1f, 1f, 0f, 0.95f);
                (_stemLines[i], _stemImages[i]) = Child<Image>(root, $"Stem{i}", QuadSprite);
                _stemImages[i].color = new Color(0f, 1f, 1f, 0.95f);

                for (int k = 0; k < 2; k++)
                {
                    (_idLines[i * 2 + k], _idImages[i * 2 + k]) =
                        Child<Image>(root, $"Id{i}_{k}", QuadSprite);
                    _idImages[i * 2 + k].color = new Color(1f, 0f, 1f, 0.9f);
                }

                for (int k = 0; k < 3; k++)
                {
                    (_endpointDots[i * 3 + k], _endpointImages[i * 3 + k]) =
                        Child<Image>(root, $"Ep{i}_{k}", DotSprite);
                    _endpointImages[i * 3 + k].color = new Color(0f, 1f, 0f, 0.95f);
                }

                (_junctionDots[i], _junctionImages[i]) = Child<Image>(root, $"Junction{i}", DotSprite);
                _junctionImages[i].color = new Color(1f, 0f, 0f, 1f);

                (_stemDots[i], _stemImages2[i]) = Child<Image>(root, $"StemTip{i}", DotSprite);
                _stemImages2[i].color = new Color(1f, 0.8f, 0f, 1f);

                (_idRects[i], _idTexts[i]) = Child<Text>(root, $"IdLabel{i}", null);
                _idTexts[i].text = "";
                _idTexts[i].font = _font;
                _idTexts[i].fontSize = _idFontSize;
                _idTexts[i].color = Color.white;
                _idTexts[i].alignment = TextAnchor.MiddleCenter;
                _idRects[i].sizeDelta = new Vector2(90f, 20f);

                // _showIdSamplePoints is an authoring flag, resolved once here so the
                // per-frame path never has to toggle sub-objects.
                for (int k = 0; k < 2; k++)
                    _idLines[i * 2 + k].gameObject.SetActive(_showIdSamplePoints);
                for (int k = 0; k < 3; k++)
                    _endpointDots[i * 3 + k].gameObject.SetActive(_showIdSamplePoints);

                rootGo.SetActive(false);
            }
        }

        (RectTransform, T) Child<T>(RectTransform parent, string name, Sprite sprite) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(T));
            go.transform.SetParent(parent, false);
            var comp = go.GetComponent<T>();
            if (comp is Graphic g)
            {
                g.raycastTarget = false;
                if (sprite != null && comp is Image img) img.sprite = sprite;
            }
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            return (rect, comp);
        }

        // ─── Procedural sprites ──────────────

        static Sprite QuadSprite
        {
            get
            {
                if (_quadSprite == null)
                {
                    var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    _quadSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                }
                return _quadSprite;
            }
        }

        static Sprite DotSprite
        {
            get
            {
                if (_dotSprite == null)
                {
                    int s = 32;
                    var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
                    float r = s * 0.5f;
                    var colors = new Color[s * s];
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                            colors[y * s + x] =
                                (x - r) * (x - r) + (y - r) * (y - r) <= r * r
                                    ? Color.white : Color.clear;
                    tex.SetPixels(colors);
                    tex.Apply();
                    _dotSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
                }
                return _dotSprite;
            }
        }
    }
}
