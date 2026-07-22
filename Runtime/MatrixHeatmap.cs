using DevicePipe;
using UnityEngine;

namespace DeviceViz
{
    /// <summary>
    /// Matrix heatmap coordinator — manages display mode and coordinates all VizLayers.
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
        public TouchMarkerLayer touchMarkerLayer;

        [Header("显示")]
        public DisplayMode displayMode = DisplayMode.Color | DisplayMode.Digits;


        // ─── 私有 ─────────────────────────────
        private VizLayer[] _layers;
        private int _w, _h;
        private bool _built;
        private DisplayMode _prevMode;

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
            var list = new System.Collections.Generic.List<VizLayer>(4);
            if (colorLayer) list.Add(colorLayer);
            if (digitLayer) list.Add(digitLayer);
            if (touchMarkerLayer) list.Add(touchMarkerLayer);
            if (fadingLayer) list.Add(fadingLayer);
            _layers = list.ToArray();
        }

        void Init(int w, int h)
        {
            if (_built) Cleanup();

            _w = w; _h = h;
            _built = true;

            ApplyMode();
        }

        // ─── 渲染 ─────────────────────────────

        void RenderLayers()
        {
            if (!_built) return;
            foreach (var l in _layers) l.Render();
        }

        // ─── 开关 ─────────────────────────────

        void ApplyMode()
        {
            if (colorLayer) colorLayer.enabled = displayMode.HasFlag(DisplayMode.Color);
            if (digitLayer) digitLayer.enabled = displayMode.HasFlag(DisplayMode.Digits);
        }

        public void SetDisplayMode(DisplayMode mode)
        {
            displayMode = mode;
            ApplyMode();
            if (_built) RenderLayers();
        }

        // ─── 数据 ─────────────────────────────

        public void UpdateData(int[] newData, int width, int height)
        {
            if (!_built || width != _w || height != _h)
                Init(width, height);

            foreach (var l in _layers) l.UpdateData(newData, width, height);

            bool needTouches = false;
            foreach (var l in _layers) { if (l.needsTouches) { needTouches = true; break; } }

            if (needTouches)
            {
                var touches = PressureAnalyzer.GetPressureInfo(newData, width, height);
                foreach (var l in _layers) l.UpdateTouches(touches, _w, _h);
            }

            RenderLayers();
        }

        void Cleanup()
        {
            _built = false;
        }

        void OnDestroy() { Cleanup(); }
    }
}
