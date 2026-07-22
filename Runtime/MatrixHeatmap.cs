using DevicePipe;
using UnityEngine;
using UnityEngine.UI;

namespace DeviceViz
{
public class MatrixHeatmap : MonoBehaviour
{
    [System.Flags]
    public enum DisplayMode
    {
        None = 0,
        Digits = 1,
        Color = 2,
        TouchMarkers = 4,
        FadingStroke = 8,
        Both = Digits | Color,
        All = Digits | Color | TouchMarkers | FadingStroke
    }

    [Header("RawImage 引用")]
    public RawImage colorLayer;
    public RawImage digitLayer;

    [Header("消退笔迹")]
    public FadingStroke fadingStroke;

    [Header("图集")]
    public Texture2D digitAtlas;

    [Header("数字渲染分辨率")]
    public int digitResolution = 3200;

    [Header("显示")]
    public DisplayMode displayMode = DisplayMode.Both;

    [Header("颜色")]
    public Color textColor = Color.white;

    // ─── 外部接口 ─────────────────────────
    [System.NonSerialized] public Texture2D colorTex;

    /// <summary>替换默认颜色算法。参数 (data, w, h, maxValue, outColors)</summary>
    [System.NonSerialized] public System.Action<int[], int, int, int, Color[]> colorUpdate;

    // ─── 公开属性 ─────────────────────────
    public int gridW => _w;
    public int gridH => _h;

    // 最大值
    public int maxValue = 100;

    // ─── 私有 ─────────────────────────────
    private Material _digitMat;
    private RenderTexture _digitRT;
    private Texture2D _dataTex;
    private int[] _data;
    private Color[] _pixels, _colors;
    private int _w, _h;
    private bool _built;
    private DisplayMode _prevMode;
    private TouchMarkerOverlay _overlay;

    #if UNITY_EDITOR
    void OnValidate()
    {
        if (displayMode == _prevMode) return;
        _prevMode = displayMode;
        ApplyMode();
    }
    #endif

    void Awake()
    {
        _prevMode = displayMode;
        if (colorLayer == null) CreateLayer(ref colorLayer, "ColorLayer");
        if (digitLayer == null) CreateLayer(ref digitLayer, "DigitLayer");
    }

    void Start()
    {
        _overlay = new TouchMarkerOverlay(transform, 20);
    }

    void CreateLayer(ref RawImage layer, string name)
    {
        var go = new GameObject(name, typeof(RawImage));
        go.transform.SetParent(transform, false);
        layer = go.GetComponent<RawImage>();
        var rt = layer.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void Init(int w, int h)
    {
        if (_built) Cleanup();

        _w = w; _h = h;
        _data = new int[w * h];
        _pixels = new Color[w * h];
        _colors = new Color[w * h];

        _dataTex = new Texture2D(w, h, TextureFormat.RGBAFloat, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        colorTex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        _digitRT = new RenderTexture(digitResolution, digitResolution, 0, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Point
        };

        var s = Resources.Load<Shader>("MatrixHeatmap_Digits");
        if (s == null) { Debug.LogError("找不到 MatrixHeatmap_Digits shader"); return; }
        _digitMat = new Material(s);
        _digitMat.SetTexture("_DataTex", _dataTex);
        _digitMat.SetTexture("_DigitAtlas", digitAtlas);
        if (digitAtlas) digitAtlas.filterMode = FilterMode.Point;
        _digitMat.SetFloat("_GridSizeX", w);
        _digitMat.SetFloat("_GridSizeY", h);
        _digitMat.SetFloat("_CellGridSize", w);
        _digitMat.SetColor("_TextColor", textColor);

        colorLayer.texture = colorTex;
        digitLayer.texture = _digitRT;
        colorLayer.color = Color.white;
        digitLayer.color = Color.white;
        colorLayer.transform.SetSiblingIndex(0);
        digitLayer.transform.SetSiblingIndex(1);

        _built = true;
        UpdateColorTex();
        RenderDigits();
        ApplyMode();
    }

    // ─── 颜色 ─────────────────────────────

    void UpdateColorTex()
    {
        if (!_built) return;
        bool show = displayMode.HasFlag(DisplayMode.Color) || displayMode.HasFlag(DisplayMode.Digits);
        if (!show) return;

        if (colorUpdate != null)
        {
            colorUpdate(_data, _w, _h, maxValue, _colors);
        }
        else
        {
            float inv = maxValue > 0 ? 1f / maxValue : 0;
            for (int i = 0; i < _colors.Length; i++)
            {
                int x = i % _w, y = i / _w;
                _colors[i] = Color.Lerp(Color.white, Color.black, _data[i] * inv);
            }
        }

        colorTex.SetPixels(_colors);
        colorTex.Apply();
    }

    // ─── 数字 ─────────────────────────────

    void RenderDigits()
    {
        if (_digitMat == null || _digitRT == null) return;
        _digitMat.SetTexture("_DataTex", _dataTex);
        var prev = RenderTexture.active;
        RenderTexture.active = _digitRT;
        GL.Clear(true, true, Color.clear);
        Graphics.Blit(_dataTex, _digitRT, _digitMat);
        RenderTexture.active = prev;
    }

    // ─── 开关 ─────────────────────────────

    void ApplyMode()
    {
        bool colorOn = displayMode.HasFlag(DisplayMode.Color);
        bool digitOn = displayMode.HasFlag(DisplayMode.Digits);
        if (colorLayer) colorLayer.enabled = colorOn;
        if (digitLayer) digitLayer.enabled = digitOn;
    }

    public void SetDisplayMode(DisplayMode mode)
    {
        displayMode = mode;
        ApplyMode();
        if (_built && (mode.HasFlag(DisplayMode.Color) || mode.HasFlag(DisplayMode.Digits)))
            UpdateColorTex();
    }

    // ─── 数据 ─────────────────────────────

    public void UpdateData(int[] newData, int width, int height)
    {
        if (!_built || width != _w || height != _h)
            Init(width, height);

        float inv999 = 1f / 999f;
        for (int r = 0; r < height; r++)
            for (int c = 0; c < width; c++)
            {
                int idx = r * width + c;
                int v = Mathf.Clamp(newData[idx], 0, 999);
                _data[idx] = v;
                _pixels[idx] = new Color(v * inv999, 0, 0, 0);
            }

        _dataTex.SetPixels(_pixels);
        _dataTex.Apply();
        UpdateColorTex();
        RenderDigits();

        bool needTouches = displayMode.HasFlag(DisplayMode.TouchMarkers)
                        || displayMode.HasFlag(DisplayMode.FadingStroke);
        if (needTouches)
        {
            var touches = PressureAnalyzer.GetPressureInfo(newData, width, height);
            UpdateTouches(touches);
        }
    }

    public void EmitFadingStrokes(PressureInfo[] touches)
    {
        if (fadingStroke == null || touches == null || touches.Length == 0)
            return;
        if (_w <= 0 || _h <= 0)
            return;

        float invW = 1f / _w;
        float invH = 1f / _h;

        for (int i = 0; i < touches.Length; i++)
        {
            var t = touches[i];
            fadingStroke.Emit(new Vector2(t.y * invH, t.x * invW));
        }
    }

    void UpdateTouches(PressureInfo[] touches)
    {
        if (displayMode.HasFlag(DisplayMode.TouchMarkers))
            _overlay?.Update(touches, _w, _h);
        if (displayMode.HasFlag(DisplayMode.FadingStroke))
            EmitFadingStrokes(touches);
    }

    public Vector2Int ScreenToCell(Vector2 screenPos)
    {
        if (!_built || digitLayer == null) return Vector2Int.zero;
        var rt = digitLayer.rectTransform;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, null, out local);
        var size = rt.rect.size;
        return new Vector2Int(
            Mathf.Clamp(Mathf.FloorToInt((local.x / size.x + 0.5f) * _w), 0, _w - 1),
            Mathf.Clamp(Mathf.FloorToInt((local.y / size.y + 0.5f) * _h), 0, _h - 1)
        );
    }

    public void Apply()
    {
        if (!_built) return;
        _dataTex.Apply();
        colorTex.Apply();
        RenderDigits();
    }

    void Cleanup()
    {
        if (colorTex) { Destroy(colorTex); colorTex = null; }
        if (_digitMat) { Destroy(_digitMat); _digitMat = null; }
        if (_dataTex) { Destroy(_dataTex); _dataTex = null; }
        if (_digitRT != null) { _digitRT.Release(); Destroy(_digitRT); _digitRT = null; }
        _built = false;
    }

    void OnDestroy() { Cleanup(); }
}
}
