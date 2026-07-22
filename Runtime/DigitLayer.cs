using UnityEngine;
using UnityEngine.UI;

namespace DeviceViz
{
    /// <summary>
    /// GPU-based digit overlay layer.
    /// Encodes int[] data, uploads to a data texture, and renders via digit atlas shader.
    /// </summary>
    public class DigitLayer : VizLayer
    {
        [Header("Digit Atlas")]
        [SerializeField] private Texture2D _digitAtlas;

        [Header("Appearance")]
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private int _resolution = 1024;

        [Header("Render")]
        [SerializeField] private FilterMode _filterMode = FilterMode.Point;

        private RawImage _image;
        private RenderTexture _rt;
        private Material _material;
        private Texture2D _dataTex;
        private Color[] _pixels;
        private int _width, _height;

        public Color textColor
        {
            get => _textColor;
            set { _textColor = value; if (_material) _material.SetColor("_TextColor", value); }
        }

        // ─── VizLayer overrides ──────────────

        public override MatrixHeatmap.DisplayMode modeFlag => MatrixHeatmap.DisplayMode.Digits;

        public override void UpdateData(int[] data, int width, int height)
        {
            if (_width != width || _height != height)
            {
                _width = width;
                _height = height;
                CreateRT();
                CreateDataTex();
            }
            EncodeToTexture(data);
        }

        public override void Clear()
        {
            if (_rt == null) return;
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = prev;
        }

        // ─── Public API ──────────────────────

        public override void Render()
        {
            if (_material == null || _rt == null || _dataTex == null) return;

            _material.SetTexture("_DataTex", _dataTex);

            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            GL.Clear(true, true, Color.clear);
            Graphics.Blit(_dataTex, _rt, _material);
            RenderTexture.active = prev;
        }

        // ─── Internal ────────────────────────

        void Awake()
        {
            _image = GetComponent<RawImage>();

            var shader = Resources.Load<Shader>("MatrixHeatmap_Digits");
            if (shader == null)
            {
                Debug.LogError("DigitLayer: MatrixHeatmap_Digits shader not found");
                enabled = false;
                return;
            }
            _material = new Material(shader);
            if (_digitAtlas == null) _digitAtlas = Resources.Load<Texture2D>("digit_atlas");
            _digitAtlas.filterMode = FilterMode.Point;
            _material.SetTexture("_DigitAtlas", _digitAtlas);
            _material.SetColor("_TextColor", _textColor);
        }

        void OnEnable()  { if (_image) _image.enabled = true; }
        void OnDisable() { if (_image) _image.enabled = false; }

        void OnDestroy()
        {
            if (_material) Destroy(_material);
            if (_rt != null) { _rt.Release(); Destroy(_rt); }
            if (_dataTex) { Destroy(_dataTex); _dataTex = null; }
        }

        void CreateDataTex()
        {
            if (_dataTex) { Destroy(_dataTex); _dataTex = null; }
            _pixels = new Color[_width * _height];
            _dataTex = new Texture2D(_width, _height, TextureFormat.RGBAFloat, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        void CreateRT()
        {
            if (_rt != null) { _rt.Release(); Destroy(_rt); }

            _rt = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = _filterMode,
                wrapMode = TextureWrapMode.Clamp
            };
            _rt.Create();

            if (_material)
            {
                _material.SetFloat("_GridSizeX", _width);
                _material.SetFloat("_GridSizeY", _height);
                _material.SetFloat("_CellGridSize", _width);
            }

            if (_image) _image.texture = _rt;
        }

        void EncodeToTexture(int[] source)
        {
            float inv = 1f / 999f;
            for (int r = 0; r < _height; r++)
                for (int c = 0; c < _width; c++)
                {
                    int idx = r * _width + c;
                    int v = Mathf.Clamp(source[idx], 0, 999);
                    _pixels[idx] = new Color(v * inv, 0, 0, 0);
                }
            _dataTex.SetPixels(_pixels);
            _dataTex.Apply();
        }

    #if UNITY_EDITOR
        void OnValidate()
        {
            _resolution = Mathf.Max(64, _resolution);
            if (_material) _material.SetColor("_TextColor", _textColor);
        }
    #endif
    }
}
