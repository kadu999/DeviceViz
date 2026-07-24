using UnityEngine;
using UnityEngine.UI;

namespace DeviceViz
{
    /// <summary>
    /// GPU-based digit overlay layer.
    /// Uploads raw int[] via ComputeBuffer, renders digits to hi-res RenderTexture via ComputeShader.
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

        // Compute
        private ComputeShader _cs;
        private int _digitKernel;
        private ComputeBuffer _dataBuffer;

        private int _width, _height;

        public Color textColor
        {
            get => _textColor;
            set
            {
                _textColor = value;
                if (_cs) _cs.SetVector("TextColor", value);
            }
        }

        // ─── VizLayer overrides ──────────────

        public override void UpdateData(int[] data, int width, int height)
        {
            if (_width != width || _height != height)
            {
                _width = width;
                _height = height;
                CreateRT();
                CreateBuffer(width * height);
            }
            UploadData(data);
        }

        public override void Clear()
        {
            if (_rt == null) return;
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = prev;
        }

        public override void Render()
        {
            if (_cs == null || _rt == null || _dataBuffer == null) return;

            _cs.SetBuffer(_digitKernel, "DataBuffer", _dataBuffer);
            _cs.SetTexture(_digitKernel, "Output", _rt);
            _cs.SetInt("Width", _width);
            _cs.SetInt("Height", _height);
            _cs.SetInt("OutputWidth", _resolution);
            _cs.SetInt("OutputHeight", _resolution);
            _cs.SetVector("TextColor", _textColor);
            _cs.Dispatch(_digitKernel,
                Mathf.CeilToInt(_resolution / 8f),
                Mathf.CeilToInt(_resolution / 8f),
                1);
        }

        // ─── Internal ────────────────────────

        void Awake()
        {
            _image = GetComponent<RawImage>();

            _cs = Resources.Load<ComputeShader>("MatrixHeatmap_Digits");
            if (_cs == null)
            {
                Debug.LogError("DigitLayer: MatrixHeatmap_Digits compute shader not found");
                enabled = false;
                return;
            }
            _digitKernel = _cs.FindKernel("CSMain");

            if (_digitAtlas == null) _digitAtlas = Resources.Load<Texture2D>("digit_atlas");
            if (_digitAtlas)
            {
                _digitAtlas.filterMode = FilterMode.Point;
                _cs.SetTexture(_digitKernel, "DigitAtlas", _digitAtlas);
            }
        }

        void OnEnable()  { if (_image) _image.enabled = true; }
        void OnDisable() { if (_image) _image.enabled = false; }

        void OnDestroy()
        {
            if (_rt != null) { _rt.Release(); Destroy(_rt); }
            ReleaseBuffer();
        }

        void CreateBuffer(int count)
        {
            ReleaseBuffer();
            _dataBuffer = new ComputeBuffer(count, sizeof(int));
        }

        void ReleaseBuffer()
        {
            if (_dataBuffer != null)
            {
                _dataBuffer.Release();
                _dataBuffer = null;
            }
        }

        void CreateRT()
        {
            if (_rt != null) { _rt.Release(); Destroy(_rt); }

            _rt = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = _filterMode,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true
            };
            _rt.Create();

            if (_cs)
            {
                _cs.SetFloat("_GridSizeX", _width);
                _cs.SetFloat("_GridSizeY", _height);
                _cs.SetFloat("_CellGridSize", _width);
            }

            if (_image) _image.texture = _rt;
        }

        void UploadData(int[] source)
        {
            _dataBuffer.SetData(source);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            _resolution = Mathf.Max(64, _resolution);
        }
#endif
    }
}
