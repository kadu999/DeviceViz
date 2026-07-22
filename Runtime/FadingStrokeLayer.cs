using DevicePipe;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 消退绘制 — 纯渲染组件。
/// 外部调用 Emit 输入点，每帧在 LateUpdate 中渲染到 RenderTexture。
///
/// 用法：
///   var fading = gameObject.AddComponent<FadingStrokeLayer>();
///   fading.Emit(new Vector2(0.5f, 0.3f));
/// </summary>
namespace DeviceViz
{
    public class FadingStrokeLayer : VizLayer
    {
        [Header("画板")]
        [SerializeField] int canvasWidth = 1024;
        [SerializeField] int canvasHeight = 1024;

        [Header("绘制")]
        [SerializeField] Color drawColor = Color.white;
        [SerializeField] float brushRadius = 4f;
        [SerializeField] float fadeDuration = 3f;
        [SerializeField] int maxPoints = 100000;

        // ─── Public ───────────────────────────

        public int CanvasWidth  { get { return canvasWidth; } }
        public int CanvasHeight { get { return canvasHeight; } }

        public void Emit(Vector2 uv)
        {
            if (records == null)
                return;
            records[tail] = new Record { uv = uv, stamp = clock };
            tail = (tail + 1) % maxPoints;
            if (count < maxPoints) count++;
            else head = (head + 1) % maxPoints;
        }

        // ─── VizLayer overrides ──────────────

        public override bool needsTouches => true;

        public override void UpdateData(int[] newData, int width, int height) { }

        public override void UpdateTouches(PressureInfo[] touches, int width, int height)
        {
            if (touches == null || touches.Length == 0 || width <= 0 || height <= 0)
                return;

            float invW = 1f / width;
            float invH = 1f / height;

            for (int i = 0; i < touches.Length; i++)
            {
                var t = touches[i];
                Emit(new Vector2(t.y * invH, t.x * invW));
            }
        }

        public override void Clear()
        {
            head = tail = count = 0;
            clock = 0;
            System.Array.Clear(records, 0, records.Length);
        }

        // ─── Internal ────────────────────────

        private struct Record { public Vector2 uv; public float stamp; }
        private Record[] records;

        private int head, tail, count;
        private RenderTexture rt;
        private Material material;
        private ComputeBuffer buffer;
        private Vector3[] gpuData;
        private RawImage _image;
        private float clock, invFade;

        void Awake()
        {
            _image = GetComponent<RawImage>();
            records = new Record[maxPoints];
            gpuData  = new Vector3[maxPoints];
            invFade  = 1f / fadeDuration;

            rt = new RenderTexture(canvasWidth, canvasHeight, 0, RenderTextureFormat.ARGB32);
            rt.Create();

            var shader = Shader.Find("Custom/DrawingPoint");
            if (shader == null) { enabled = false; return; }
            material = new Material(shader);
            material.SetColor("_DrawColor", drawColor);
            material.SetFloat("_LineWidth", brushRadius * 2f);
            material.SetVector("_CanvasSize", new Vector2(canvasWidth, canvasHeight));

            buffer = new ComputeBuffer(maxPoints, sizeof(float) * 3);
            material.SetBuffer("_PointBuffer", buffer);

            if (_image) _image.texture = rt;
        }

        void OnEnable()  { if (_image) _image.enabled = true; }
        void OnDisable() { if (_image) _image.enabled = false; }

        void OnDestroy()
        {
            buffer?.Release();
            if (material != null) Destroy(material);
            if (rt != null) rt.Release();
        }

        void LateUpdate()
        {
            if (count == 0) return;

            clock += Time.deltaTime;
            float cutoff = clock - fadeDuration;

            while (count > 0 && records[head].stamp < cutoff)
            {
                head = (head + 1) % maxPoints;
                count--;
            }

            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                int idx = (head + i) % maxPoints;
                var r = records[idx];
                float alpha = 1f - (clock - r.stamp) * invFade;
                gpuData[i] = new Vector3(r.uv.x * canvasWidth, r.uv.y * canvasHeight, alpha);
            }

            buffer.SetData(gpuData, 0, 0, count);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, Color.clear);
            material.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Quads, count * 4);
            RenderTexture.active = prev;
        }

    #if UNITY_EDITOR
        void OnValidate()
        {
            canvasWidth   = Mathf.Max(64, canvasWidth);
            canvasHeight  = Mathf.Max(64, canvasHeight);
            brushRadius   = Mathf.Max(0.5f, brushRadius);
            fadeDuration  = Mathf.Max(0.1f, fadeDuration);
            maxPoints     = Mathf.Max(1, maxPoints);
            invFade       = 1f / fadeDuration;
        }
    #endif
    }
}
