using DevicePipe;
using UnityEngine;
using UnityEngine.UI;

namespace DeviceViz
{
    /// <summary>
    /// Matrix heatmap coordinator — manages display mode and
    /// coordinates ColorLayer, DigitLayer, TouchMarkerOverlay, and FadingStrokeLayer.
    /// </summary>
    public class MatrixHeatmap : MonoBehaviour
    {
        [System.Flags]
        public enum DisplayMode
        {
            None = 0,
            Color = 1,
            Digits = 2,
            TouchMarkers = 4,
            FadingStroke = 8,
            All = Digits | Color | TouchMarkers | FadingStroke
        }

        [Header("Layers")]
        public ColorLayer colorLayer;
        public DigitLayer digitLayer;
        public FadingStrokeLayer fadingLayer;

        [Header("显示")]
        public DisplayMode displayMode = DisplayMode.Color | DisplayMode.Digits;

        [Header("颜色")]
        public Color textColor = Color.white;

        // ─── 公开属性 ─────────────────────────
        public int gridW => _w;
        public int gridH => _h;

        // ─── 私有 ─────────────────────────────
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
        }

        void Start()
        {
            _overlay = new TouchMarkerOverlay(transform, 20);
        }

        void Init(int w, int h)
        {
            if (_built) Cleanup();

            _w = w; _h = h;
            _built = true;

            digitLayer.textColor = textColor;

            colorLayer.transform.SetSiblingIndex(0);
            digitLayer.transform.SetSiblingIndex(1);

            ApplyMode();
        }

        // ─── 渲染 ─────────────────────────────

        void Render()
        {
            if (!_built) return;
            colorLayer.Render();
            digitLayer.Render();
        }

        // ─── 开关 ─────────────────────────────

        void ApplyMode()
        {
            if (colorLayer) colorLayer.visible = displayMode.HasFlag(DisplayMode.Color);
            if (digitLayer) digitLayer.visible = displayMode.HasFlag(DisplayMode.Digits);
        }

        public void SetDisplayMode(DisplayMode mode)
        {
            displayMode = mode;
            ApplyMode();
            if (_built) Render();
        }

        // ─── 数据 ─────────────────────────────

        public void UpdateData(int[] newData, int width, int height)
        {
            if (!_built || width != _w || height != _h)
                Init(width, height);

            colorLayer.UpdateData(newData, width, height);
            digitLayer.UpdateData(newData, width, height);
            Render();

            bool needTouches = displayMode.HasFlag(DisplayMode.TouchMarkers)
                            || displayMode.HasFlag(DisplayMode.FadingStroke);
            if (needTouches)
            {
                var touches = PressureAnalyzer.GetPressureInfo(newData, width, height);
                UpdateTouches(touches);
            }
        }

        void UpdateTouches(PressureInfo[] touches)
        {
            if (displayMode.HasFlag(DisplayMode.TouchMarkers))
                _overlay?.Update(touches, _w, _h);
            if (displayMode.HasFlag(DisplayMode.FadingStroke))
                fadingLayer.UpdateTouches(touches, _w, _h);
        }

        void Cleanup()
        {
            _built = false;
        }

        void OnDestroy() { Cleanup(); }
    }
}
