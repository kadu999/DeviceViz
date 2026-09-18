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
        public TShapeLayer tShapeLayer;

        [Header("Layer Toggles")]
        public bool showColor = true;
        public bool showDigits = true;
        public bool showTouchMarkers;
        public bool showFadingStroke;
        public bool showChessPieces = true;

        // T-shaped stamps (印章) carry the 4-bit piece identity.  Left ON so the
        // stamp exclusion mask is built every frame, matching the python viewer
        // where show_tshapes defaults to True.
        public bool showTShapes = true;

        [Header("UI")]
        public bool createUI = true;


        // ON by default: matches the python viewer's render pipeline — touch
        // detection uses threshold 12 and touches inside detected piece
        // circles are excluded, so pieces and touches stay visually distinct.
        public bool enableFilter = true;

        // ─── 私有 ─────────────────────────────
        private VizLayer[] _layers;
        private int _w, _h;
        private bool _built;

        void Awake()
        {
            // Created on demand so the MatrixHeatmap prefab does not have to be
            // re-authored just to gain the stamp layer.
            if (tShapeLayer == null) tShapeLayer = CreateTShapeLayer();

            var list = new System.Collections.Generic.List<VizLayer>(6);
            if (colorLayer) list.Add(colorLayer);
            if (digitLayer) list.Add(digitLayer);
            if (touchMarkerLayer) list.Add(touchMarkerLayer);
            if (fadingLayer) list.Add(fadingLayer);
            if (chessPieceLayer) list.Add(chessPieceLayer);
            if (tShapeLayer) list.Add(tShapeLayer);
            _layers = list.ToArray();
            ApplyMode();
        }

        TShapeLayer CreateTShapeLayer()
        {
            var go = new GameObject("TShapeLayer", typeof(RectTransform), typeof(TShapeLayer));
            go.transform.SetParent(transform, false);
            go.transform.SetAsLastSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;      // stretch like the other overlay layers
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go.GetComponent<TShapeLayer>();
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

            // Resolution changed (or first frame): the stamp detector's temporal EMA
            // and exclusion mask belong to the previous stream.
            TShapeDetector.ResetState();

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
            if (tShapeLayer) tShapeLayer.gameObject.SetActive(showTShapes);
            TShapeDetector.Enabled = showTShapes;
        }

        // ─── 数据 ─────────────────────────────

        public void UpdateData(int[] newData, int width, int height)
        {
            if (!_built || width != _w || height != _h)
                Init(width, height);

            foreach (var l in _layers)
                if (l.gameObject.activeInHierarchy) l.UpdateData(newData, width, height);

            // ── Stamps first. ──
            // Two reasons this must precede the touch and piece passes:
            //   1. its exclusion mask is consulted by both (python :659-662);
            //   2. the detector keeps a temporal EMA, and this is the single
            //      per-frame call that contract allows.
            var tShapes = TShapeDetector.GetTShapes(newData, width, height);

            bool needTShapes = false;
            foreach (var l in _layers)
            { if (l.gameObject.activeInHierarchy && l.needsTShapes) { needTShapes = true; break; } }

            if (needTShapes)
            {
                foreach (var l in _layers)
                    if (l.gameObject.activeInHierarchy) l.UpdateTShapes(tShapes, width, height);
            }

            bool needTouches = false;
            foreach (var l in _layers)
            { if (l.gameObject.activeInHierarchy && l.needsTouches) { needTouches = true; break; } }

            if (needTouches)
            {
                var touches = PressureAnalyzer.GetPressureInfo(newData, width, height, RadiusMode.Direction, enableFilter);
                foreach (var l in _layers)
                    if (l.gameObject.activeInHierarchy) l.UpdateTouches(touches, width, height);
            }

            bool needPieces = false;
            foreach (var l in _layers)
            { if (l.gameObject.activeInHierarchy && l.needsPieces) { needPieces = true; break; } }

            if (needPieces)
            {
                var pieces = PressureAnalyzer.GetPieceInfo(newData, width, height);
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
            prt.sizeDelta = new Vector2(140, 220);
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
            AddToggle(panel.transform, "Stamps", showTShapes,      v => { showTShapes = v; ApplyMode(); });
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
