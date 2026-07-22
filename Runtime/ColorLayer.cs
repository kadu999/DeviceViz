using UnityEngine;
using UnityEngine.UI;

namespace DeviceViz
{
    /// <summary>
    /// GPU-based color heatmap layer.
    /// Encodes int[] data, uploads to a data texture, and renders via color ramp shader.
    /// </summary>
    public class ColorLayer : VizLayer
    {
        [Header("Render")]
        [SerializeField] private FilterMode _filterMode = FilterMode.Point;

        [Header("Max Value")]
        [SerializeField] private int _maxValue = 128;

        private RenderTexture _rt;
        private Material _material;
        private Texture2D _dataTex;
        private Color[] _pixels;
        private int _width, _height;

        public override Texture texture => _rt;

        // ─── Public API ──────────────────────

        /// <summary>Upload raw data and encode to the internal data texture.</summary>
        public override void UpdateData(int[] data, int width, int height)
        {
            if (_width != width || _height != height)
            {
                _width = width;
                _height = height;
                CreateRT();
                CreateDataTex();
            }
            EncodeToTexture(data, _dataTex, _pixels, width, height);
        }

        /// <summary>Render one frame to the output texture.</summary>
        public void Render()
        {
            if (_material == null || _rt == null || _dataTex == null) return;

            _material.SetTexture("_DataTex", _dataTex);

            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            GL.Clear(true, true, Color.clear);
            Graphics.Blit(_dataTex, _rt, _material);
            RenderTexture.active = prev;
        }

        public override void Clear()
        {
            if (_rt == null) return;
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = prev;
        }

        // ─── Internal ────────────────────────

        void Awake()
        {
            var shader = Resources.Load<Shader>("MatrixHeatmap_Color");
            if (shader == null)
            {
                Debug.LogError("ColorLayer: Hidden/MatrixHeatmap_Color shader not found");
                enabled = false;
                return;
            }
            _material = new Material(shader);
        }

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

            _rt = new RenderTexture(_width, _height, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = _filterMode,
                wrapMode = TextureWrapMode.Clamp
            };
            _rt.Create();

            if (_target) _target.texture = _rt;
        }

        // ─── Encoding ────────────────────────

        void EncodeToTexture(int[] source, Texture2D target, Color[] buffer, int width, int height)
        {
            float inv = 1f / Mathf.Max(1, _maxValue);
            for (int r = 0; r < height; r++)
                for (int c = 0; c < width; c++)
                {
                    int idx = r * width + c;
                    int v = Mathf.Clamp(source[idx], 0, _maxValue);
                    buffer[idx] = new Color(v * inv, 0, 0, 0);
                }
            target.SetPixels(buffer);
            target.Apply();
        }

    #if UNITY_EDITOR
        void OnValidate() { }
    #endif
    }
}
