using System;
using DevicePipe;
using UnityEngine;
using UnityEngine.UI;

namespace DeviceViz
{
    /// <summary>
    /// Matrix heatmap coordinator — manages display mode and coordinates all VizLayers.
    /// </summary>
    public class MatrixHeatmap : MonoBehaviour
    {
        [Header("Layers")]
        public ColorLayer colorLayer;
        public DigitLayer digitLayer;
        public FadingStrokeLayer fadingLayer;
        public TouchMarkerLayer touchMarkerLayer;
        public ChessPieceLayer chessPieceLayer;

        [Header("Layer Toggles")]
        public bool showColor = true;
        public bool showDigits = true;
        public bool showTouchMarkers;
        public bool showFadingStroke;
        public bool showChessPieces;

        [Header("UI")]
        public bool createUI = true;

        // ─── 私有 ─────────────────────────────
        private VizLayer[] _layers;
        private int _w, _h;
        private bool _built;

        void Awake()
        {
            var list = new System.Collections.Generic.List<VizLayer>(5);
            if (colorLayer) list.Add(colorLayer);
            if (digitLayer) list.Add(digitLayer);
            if (touchMarkerLayer) list.Add(touchMarkerLayer);
            if (fadingLayer) list.Add(fadingLayer);
            if (chessPieceLayer) list.Add(chessPieceLayer);
            _layers = list.ToArray();
            ApplyMode();
        }

        void Start()
        {
            if (createUI) BuildUI();
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
            foreach (var l in _layers)
                if (l.gameObject.activeInHierarchy) l.Render();
        }

        // ─── 开关 ─────────────────────────────

        void ApplyMode()
        {
            if (colorLayer) colorLayer.gameObject.SetActive(showColor);
            if (digitLayer) digitLayer.gameObject.SetActive(showDigits);
            if (touchMarkerLayer) touchMarkerLayer.gameObject.SetActive(showTouchMarkers);
            if (fadingLayer) fadingLayer.gameObject.SetActive(showFadingStroke);
            if (chessPieceLayer) chessPieceLayer.gameObject.SetActive(showChessPieces);
        }

        // ─── 数据 ─────────────────────────────

        public void UpdateData(int[] newData, int width, int height)
        {
            if (!_built || width != _w || height != _h)
                Init(width, height);

            foreach (var l in _layers)
                if (l.gameObject.activeInHierarchy) l.UpdateData(newData, width, height);

            bool needTouches = false;
            foreach (var l in _layers)
            { if (l.gameObject.activeInHierarchy && l.needsTouches) { needTouches = true; break; } }

            if (needTouches)
            {
                var touches = PressureAnalyzer.GetPressureInfo(newData, width, height);
                foreach (var l in _layers)
                    if (l.gameObject.activeInHierarchy) l.UpdateTouches(touches, width, height);
            }

            bool needPieces = false;
            foreach (var l in _layers)
            { if (l.gameObject.activeInHierarchy && l.needsPieces) { needPieces = true; break; } }

            if (needPieces)
            {
                var pieces = PressureAnalyzer.GetChessPieceInfo(newData, width, height);
                foreach (var l in _layers)
                    if (l.gameObject.activeInHierarchy) l.UpdatePieces(pieces, width, height);
            }

            RenderLayers();
        }

        // ─── UI ────────────────────────────────

        void BuildUI()
        {
            var panel = new GameObject("TogglePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(this.transform, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(1, 1);
            prt.pivot = new Vector2(0, 1);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(140, 190);
            prt.SetAsLastSibling();
            panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            AddToggle(panel.transform, "Color",  showColor,        v => { showColor = v; ApplyMode(); });
            AddToggle(panel.transform, "Digits", showDigits,       v => { showDigits = v; ApplyMode(); });
            AddToggle(panel.transform, "Touch",  showTouchMarkers, v => { showTouchMarkers = v; ApplyMode(); });
            AddToggle(panel.transform, "Fading", showFadingStroke, v => { showFadingStroke = v; ApplyMode(); });
            AddToggle(panel.transform, "Pieces", showChessPieces,  v => { showChessPieces = v; ApplyMode(); });
        }

        void AddToggle(Transform parent, string label, bool initial, Action<bool> onChange)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Toggle), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120, 28);

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.25f, 0.25f, 0.3f);

            // Label
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var text = labelGo.GetComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var toggle = go.GetComponent<Toggle>();
            toggle.isOn = initial;
            toggle.onValueChanged.AddListener(v =>
            {
                bg.color = v ? new Color(0.15f, 0.50f, 0.20f) : new Color(0.25f, 0.25f, 0.3f);
                onChange(v);
            });
            // Init color
            bg.color = initial ? new Color(0.15f, 0.50f, 0.20f) : new Color(0.25f, 0.25f, 0.3f);
        }

        void Cleanup()
        {
            _built = false;
        }

        void OnDestroy() { Cleanup(); }
    }
}
