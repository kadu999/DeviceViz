using UnityEngine;
using UnityEngine.UI;

namespace DeviceViz
{
    /// <summary>
    /// GPU-based color heatmap layer.
    /// Uploads raw int[] via ComputeBuffer, renders to RenderTexture via ComputeShader.
    /// </summary>
    public class ColorLayer : VizLayer
    {
        [Header("Render")]
        [SerializeField] private FilterMode _filterMode = FilterMode.Point;

        [Header("Max Value")]
        [SerializeField] private int _maxValue = 128;

        private RawImage _image;
        private RenderTexture _rt;

        // Compute
        private ComputeShader _cs;
        private int _colorKernel;
        private ComputeBuffer _dataBuffer;

        private int _width, _height;

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

            _cs.SetBuffer(_colorKernel, "DataBuffer", _dataBuffer);
            _cs.SetTexture(_colorKernel, "Output", _rt);
            _cs.SetInt("Width", _width);
            _cs.SetInt("Height", _height);
            _cs.SetFloat("InvMaxValue", 1f / Mathf.Max(1, _maxValue));
            _cs.Dispatch(_colorKernel,
                Mathf.CeilToInt(_width / 8f),
                Mathf.CeilToInt(_height / 8f),
                1);
        }

        // ─── Internal ────────────────────────

        void Awake()
        {
            _image = GetComponent<RawImage>();

            _cs = Resources.Load<ComputeShader>("MatrixHeatmap_Color");
            if (_cs == null)
            {
                Debug.LogError("ColorLayer: MatrixHeatmap_Color compute shader not found");
                enabled = false;
                return;
            }
            _colorKernel = _cs.FindKernel("CSMain");
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

            _rt = new RenderTexture(_width, _height, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = _filterMode,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true
            };
            _rt.Create();

            if (_image) _image.texture = _rt;
        }

        void UploadData(int[] source)
        {
            _dataBuffer.SetData(source);
        }

#if UNITY_EDITOR
        void OnValidate() { }
#endif
    }
}
