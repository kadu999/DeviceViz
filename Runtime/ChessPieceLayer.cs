using DevicePipe;
using UnityEngine;
using UnityEngine.UI;

namespace DeviceViz
{
    /// <summary>
    /// Chess piece visualization layer.
    /// Renders detected solid pieces as orange circles sized by piece radius.
    /// (Ring/ellipse rendering was removed together with ring detection in DevicePipe.)
    /// </summary>
    public class ChessPieceLayer : VizLayer
    {
        [Header("Pieces")]
        [SerializeField] private int _maxPieces = 20;

        private RectTransform[] _circles;
        private Image[] _circleImages;
        private RectTransform[] _centers;
        private Image[] _centerImages;

        private static Sprite _circleSprite;
        private static Sprite _centerSprite;

        // ─── VizLayer overrides ──────────────

        public override bool needsPieces => true;

        public override void UpdateData(int[] newData, int width, int height) { }

        public override void UpdatePieces(PieceInfo[] pieces, int width, int height)
        {
            if (_circles == null) return;

            var rt = (RectTransform)transform;
            float scale = rt.rect.width / width;

            for (int i = 0; i < _maxPieces; i++)
            {
                if (i < (pieces?.Length ?? 0))
                {
                    var p = pieces[i];

                    // Sensor coords → UI coords (x/y swap matching TouchMarkerLayer)
                    float uix = p.pos_y * scale;
                    float uiy = p.pos_x * scale;

                    // ── Piece body: orange circle, diameter = 2 × radius ──
                    _circles[i].anchoredPosition = new Vector2(uix, uiy);
                    _circles[i].sizeDelta = Vector2.one * (p.radius * 2f * scale);
                    _circleImages[i].color = new Color(1f, 0.65f, 0f, 0.7f);
                    _circles[i].gameObject.SetActive(true);

                    // ── Center: yellow dot ──
                    _centers[i].anchoredPosition = new Vector2(uix, uiy);
                    float dotSize = Mathf.Max(6f, p.radius * scale * 0.5f);
                    _centers[i].sizeDelta = Vector2.one * dotSize;
                    _centerImages[i].color = new Color(1f, 1f, 0f, 0.9f);
                    _centers[i].gameObject.SetActive(true);
                }
                else
                {
                    _circles[i].gameObject.SetActive(false);
                    _centers[i].gameObject.SetActive(false);
                }
            }
        }

        public override void Clear()
        {
            if (_circles == null) return;
            for (int i = 0; i < _maxPieces; i++)
            {
                if (_circles[i]) _circles[i].gameObject.SetActive(false);
                if (_centers[i]) _centers[i].gameObject.SetActive(false);
            }
        }

        void OnEnable() { }
        void OnDisable() { Clear(); }

        // ─── Internal ────────────────────────

        void Awake()
        {
            _circles = new RectTransform[_maxPieces];
            _circleImages = new Image[_maxPieces];
            _centers = new RectTransform[_maxPieces];
            _centerImages = new Image[_maxPieces];

            for (int i = 0; i < _maxPieces; i++)
            {
                (_circles[i], _circleImages[i]) = CreateChild($"Piece{i}_Circle", CircleSprite);
                (_centers[i], _centerImages[i]) = CreateChild($"Piece{i}_Center", CenterSprite);
            }
        }

        (RectTransform, Image) CreateChild(string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsLastSibling();
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            go.SetActive(false);
            return (rect, img);
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
