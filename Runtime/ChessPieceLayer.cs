using DevicePipe;
using UnityEngine;
using UnityEngine.UI;

namespace DeviceViz
{
    /// <summary>
    /// Chess piece visualization layer.
    /// Renders detected rings as green ellipses with yellow centers and magenta direction arrows.
    /// </summary>
    public class ChessPieceLayer : VizLayer
    {
        [Header("Pieces")]
        [SerializeField] private int _maxPieces = 20;

        private RectTransform[] _rings;
        private Image[] _ringImages;
        private RectTransform[] _centers;
        private Image[] _centerImages;
        private RectTransform[] _arrows;
        private Image[] _arrowImages;

        private static Sprite _ringSprite;
        private static Sprite _centerSprite;
        private static Sprite _arrowSprite;

        // ─── VizLayer overrides ──────────────

        public override bool needsPieces => true;

        public override void UpdateData(int[] newData, int width, int height) { }

        public override void UpdatePieces(ChessPieceInfo[] pieces, int width, int height)
        {
            if (_rings == null) return;

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

                    // ── Ring: green ellipse ──
                    _rings[i].anchoredPosition = new Vector2(uix, uiy);
                    _rings[i].sizeDelta = new Vector2(p.major * scale, p.minor * scale);
                    _rings[i].localRotation = Quaternion.Euler(0f, 0f, p.angle);
                    _ringImages[i].color = new Color(0f, 1f, 0f, 0.7f);
                    _rings[i].gameObject.SetActive(true);

                    // ── Center: yellow dot ──
                    _centers[i].anchoredPosition = new Vector2(uix, uiy);
                    float dotSize = Mathf.Max(6f, p.radius * scale * 0.5f);
                    _centers[i].sizeDelta = Vector2.one * dotSize;
                    _centerImages[i].color = new Color(1f, 1f, 0f, 0.9f);
                    _centers[i].gameObject.SetActive(true);

                    // ── Arrow: magenta direction line ──
                    // Direction in sensor space (dir_x, dir_y) → UI space (dir_y, dir_x)
                    float uidx = p.dir_y * scale;
                    float uidy = p.dir_x * scale;
                    float arrowLen = Mathf.Sqrt(uidx * uidx + uidy * uidy);
                    if (arrowLen > 1f)
                    {
                        float arrowAngle = Mathf.Atan2(uidy, uidx) * Mathf.Rad2Deg;
                        _arrows[i].anchoredPosition = new Vector2(uix, uiy);
                        _arrows[i].sizeDelta = new Vector2(arrowLen, Mathf.Max(2f, dotSize * 0.4f));
                        _arrows[i].localRotation = Quaternion.Euler(0f, 0f, arrowAngle);
                        _arrowImages[i].color = new Color(1f, 0f, 1f, 0.9f);
                        _arrows[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        _arrows[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    _rings[i].gameObject.SetActive(false);
                    _centers[i].gameObject.SetActive(false);
                    _arrows[i].gameObject.SetActive(false);
                }
            }
        }

        public override void Clear()
        {
            if (_rings == null) return;
            for (int i = 0; i < _maxPieces; i++)
            {
                if (_rings[i]) _rings[i].gameObject.SetActive(false);
                if (_centers[i]) _centers[i].gameObject.SetActive(false);
                if (_arrows[i]) _arrows[i].gameObject.SetActive(false);
            }
        }

        void OnEnable() { }
        void OnDisable() { Clear(); }

        // ─── Internal ────────────────────────

        void Awake()
        {
            _rings = new RectTransform[_maxPieces];
            _ringImages = new Image[_maxPieces];
            _centers = new RectTransform[_maxPieces];
            _centerImages = new Image[_maxPieces];
            _arrows = new RectTransform[_maxPieces];
            _arrowImages = new Image[_maxPieces];

            for (int i = 0; i < _maxPieces; i++)
            {
                (_rings[i], _ringImages[i]) = CreateChild($"Piece{i}_Ring", RingSprite);
                (_centers[i], _centerImages[i]) = CreateChild($"Piece{i}_Center", CenterSprite);
                (_arrows[i], _arrowImages[i]) = CreateChild($"Piece{i}_Arrow", ArrowSprite);
                // Arrow pivot is left-center (0, 0.5f)
                _arrows[i].pivot = new Vector2(0f, 0.5f);
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

        static Sprite RingSprite
        {
            get
            {
                if (_ringSprite == null)
                {
                    int s = 128;
                    float thickness = 4f;
                    var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
                    float center = s * 0.5f;
                    float outerR = center;
                    float innerR = outerR - thickness;
                    var colors = new Color[s * s];
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                        {
                            float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                            colors[y * s + x] = (dist <= outerR && dist >= innerR) ? Color.white : Color.clear;
                        }
                    tex.SetPixels(colors);
                    tex.Apply();
                    _ringSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
                }
                return _ringSprite;
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

        static Sprite ArrowSprite
        {
            get
            {
                if (_arrowSprite == null)
                {
                    int w = 128, h = 8;
                    var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    var colors = new Color[w * h];
                    for (int i = 0; i < colors.Length; i++)
                        colors[i] = Color.white;
                    tex.SetPixels(colors);
                    tex.Apply();
                    // Pivot at left-center: (0, 0.5)
                    _arrowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f));
                }
                return _arrowSprite;
            }
        }
    }
}
