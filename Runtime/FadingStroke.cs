using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 消退绘制 — 纯渲染组件，不依赖任何 UI 组件。
/// 外部调用 Update/Render，用 texture 属性拿到渲染结果。
///
/// 用法：
///   var fading = gameObject.AddComponent<FadingStroke>();
///   fading.Emit(new Vector2(0.5f, 0.3f));
///   rawImage.texture = fading.texture;
/// </summary>
namespace DeviceViz
{
public class FadingStroke : MonoBehaviour
{
    [Header("画板")]
    [SerializeField] RawImage target;

    [Header("画板")]
    [SerializeField] int canvasWidth = 1024;
    [SerializeField] int canvasHeight = 1024;

    [Header("绘制")]
    [SerializeField] Color drawColor = Color.white;
    [SerializeField] float brushRadius = 4f;
    [SerializeField] float fadeDuration = 3f;
    [SerializeField] int maxPoints = 100000;

    /// <summary>画好的纹理，赋值给 RawImage 显示</summary>
    public RenderTexture texture => rt;

    /// <summary>输入一个点 (UV 0~1)</summary>
    public void Emit(Vector2 uv)
    {
        records[tail] = new Record { uv = uv, stamp = clock };
        tail = (tail + 1) % maxPoints;
        if (count < maxPoints) count++;
        else head = (head + 1) % maxPoints; // 满则覆盖最旧的
    }

    /// <summary>清空画布</summary>
    public void Clear()
    {
        head = tail = count = 0;
        clock = 0;
        System.Array.Clear(records, 0, records.Length);
    }

    // ── 内部 ────────────────────────────────────────────────

    private struct Record { public Vector2 uv; public float stamp; }
    private Record[] records;

    private int head;   // 最早有效记录的 index
    private int tail;   // 下一个写入位置
    private int count;  // 有效记录数

    private RenderTexture rt;
    private Material      material;
    private ComputeBuffer buffer;
    private Vector3[] gpuData;
    private float clock;
    private float invFade; // 缓存 1 / fadeDuration

    public int CanvasWidth { get { return canvasWidth; } }
    public int CanvasHeight { get { return canvasHeight; } }

    void Awake()
    {
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

        target.texture = rt;
    }

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

        // 从头部淘汰过期记录（O(1) per record，无内存移位）
        while (count > 0 && records[head].stamp < cutoff)
        {
            head = (head + 1) % maxPoints;
            count--;
        }

        if (count == 0) return;

        // 单遍构建 GPU 数据（无需 Clamp01 — 过期记录已被移除）
        for (int i = 0; i < count; i++)
        {
            int idx = (head + i) % maxPoints;
            var r = records[idx];
            float alpha = 1f - (clock - r.stamp) * invFade;
            gpuData[i] = new Vector3(
                r.uv.x * canvasWidth,
                r.uv.y * canvasHeight,
                alpha
            );
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
