using DevicePipe;
using UnityEngine;
using UnityEngine.UI;

namespace DeviceViz
{
    /// <summary>
    /// Chess piece visualization layer.
    /// Renders detected solid pieces as orange circles sized by piece radius,
    /// with the stable tracker ID drawn at the piece center.
    /// (Ring/ellipse rendering was removed together with ring detection in DevicePipe.)
    /// </summary>
    public class ChessPieceLayer : VizLayer
    {
        [Header("Pieces")]
        [SerializeField] private int _maxPieces = 20;
        [SerializeField] private int _idFontSize = 13;

        private RectTransform[] _circles;
        private Image[] _circleImages;
        private RectTransform[] _centers;
        private Image[] _centerImages;
        private RectTransform[] _idRects;
        private Text[] _idTexts;

        // Per-slot state to avoid redundant native calls / text mesh rebuilds
        private bool[] _slotActive;
        private int[] _lastIds;

        private static Sprite _circleSprite;
        private static Sprite _centerSprite;
        private static Font _font;

        // ─── VizLayer overrides ──────────────

        public override bool needsPieces => true;

        public override void UpdateData(int[] newData, int width, int height) { }

        public override void UpdatePieces(PieceInfo[] pieces, int width, int height)
        {
            if (_circles == null) return;

            var rt = (RectTransform)transform;
            float scale = rt.rect.width / width;
            int count = pieces?.Length ?? 0;

            for (int i = 0; i < _maxPieces; i++)
            {
                bool active = i < count;
                if (active != _slotActive[i])
                {
                    _slotActive[i] = active;
                    _circles[i].gameObject.SetActive(active);
                    _centers[i].gameObject.SetActive(active);
                    _idRects[i].gameObject.SetActive(active);
                }
                if (!active) continue;

                var p = pieces[i];

                // Sensor coords → UI coords (x/y swap matching TouchMarkerLayer)
                float uix = p.pos_y * scale;
                float uiy = p.pos_x * scale;

                // ── Piece body: orange circle, diameter = 2 × radius ──
                _circles[i].anchoredPosition = new Vector2(uix, uiy);
                _circles[i].sizeDelta = Vector2.one * (p.radius * 2f * scale);
                _circleImages[i].color = CircleColor(p.radius);

                // ── Center: yellow dot ──
                _centers[i].anchoredPosition = new Vector2(uix, uiy);
                _centers[i].sizeDelta = Vector2.one * Mathf.Max(6f, p.radius * scale * 0.5f);

                // ── ID label (skip text reset when unchanged — avoids mesh rebuild) ──
                _idRects[i].anchoredPosition = new Vector2(uix, uiy);
                if (_lastIds[i] != p.id)
                {
                    _lastIds[i] = p.id;
                    _idTexts[i].text = p.id.ToString();
                }
            }
        }

        public override void Clear()
        {
            if (_circles == null) return;
            for (int i = 0; i < _maxPieces; i++)
            {
                _slotActive[i] = false;
                if (_circles[i]) _circles[i].gameObject.SetActive(false);
                if (_centers[i]) _centers[i].gameObject.SetActive(false);
                if (_idRects[i]) _idRects[i].gameObject.SetActive(false);
                _lastIds[i] = 0;
            }
        }

        void OnEnable() { }
        void OnDisable() { Clear(); }

        // ─── Internal ────────────────────────

        void Awake()
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _circles = new RectTransform[_maxPieces];
            _circleImages = new Image[_maxPieces];
            _centers = new RectTransform[_maxPieces];
            _centerImages = new Image[_maxPieces];
            _idRects = new RectTransform[_maxPieces];
            _idTexts = new Text[_maxPieces];
            _slotActive = new bool[_maxPieces];
            _lastIds = new int[_maxPieces];

            for (int i = 0; i < _maxPieces; i++)
            {
                (_circles[i], _circleImages[i]) = CreateChild<Image>($"Piece{i}_Circle", CircleSprite);
                (_centers[i], _centerImages[i]) = CreateChild<Image>($"Piece{i}_Center", CenterSprite);
                (_idRects[i], _idTexts[i]) = CreateChild<Text>($"Piece{i}_Id", null);
                _idTexts[i].text = "";
                _idTexts[i].font = _font;
                _idTexts[i].fontSize = _idFontSize;
                _idTexts[i].color = Color.white;
                _idTexts[i].alignment = TextAnchor.MiddleCenter;
                // Same anchor setup as the circle (CreateChild anchors at bottom-left),
                // with a small explicit rect — Stretch() would anchor to the panel center
                // and the label would land half a panel away from the piece.
                _idRects[i].sizeDelta = new Vector2(40f, 20f);
            }
        }

        // Slightly vary orange by radius so overlapping pieces are easier to tell apart
        static Color CircleColor(float radius)
        {
            float t = Mathf.Clamp01((radius - 4f) / 10f);
            return new Color(1f, 0.75f - 0.25f * t, 0.1f, 0.7f);
        }

        (RectTransform, T) CreateChild<T>(string name, Sprite sprite) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(T));
            go.transform.SetParent(transform, false);
            go.transform.SetAsLastSibling();
            var comp = go.GetComponent<T>();
            if (comp is Graphic g)
            {
                g.raycastTarget = false;
                if (sprite != null && comp is Image img) img.sprite = sprite;
            }
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            go.SetActive(false);
            return (rect, comp);
        }

        // ─── Procedural sprites ──────────────

        static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite == null)
                {
                    int s = 128;
                    var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
                    float r = s * 0.5f;
                    var colors = new Color[s * s];
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                        {
                            float d = Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r));
                            colors[y * s + x] = d <= r ? Color.white : Color.clear;
                        }
                    tex.SetPixels(colors);
                    tex.Apply();
                    _circleSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
                }
                return _circleSprite;
            }
        }

        static Sprite CenterSprite
        {
            get
            {
                if (_centerSprite == null)
                {
                    int s = 32;
                    var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
                    float r = s * 0.5f;
                    var colors = new Color[s * s];
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                            colors[y * s + x] = (x - r) * (x - r) + (y - r) * (y - r) <= r * r ? Color.white : Color.clear;
                    tex.SetPixels(colors);
                    tex.Apply();
                    _centerSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
                }
                return _centerSprite;
            }
        }
    }
}
