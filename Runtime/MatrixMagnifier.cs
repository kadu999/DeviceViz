using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 窗口拖拽缩放控件：直接引用一个 RectTransform，用滚轮缩放，放大才能拖动，缩回原位自动归位。
/// 挂到 Canvas 下的空 GameObject 上，把要控制的窗口拖给 targetRT。
/// </summary>
public class MatrixMagnifier : MonoBehaviour
{
    [Header("目标窗口")]
    public RectTransform targetRT;

    [Header("缩放")]
    public float wheelSpeed = 100f;
    public float maxZoom = 4f;

    private Vector2 _originSize;
    private Vector2 _originPos;
    private Vector2 _dragOffset;

    void Start()
    {
        if (targetRT == null)
        {
            Debug.LogError("MatrixMagnifier: targetRT 未赋值");
            return;
        }
        _originSize = targetRT.sizeDelta;
        _originPos = targetRT.anchoredPosition;

        // 挂事件到目标上
        var et = targetRT.gameObject.GetComponent<EventTrigger>();
        if (et == null) et = targetRT.gameObject.AddComponent<EventTrigger>();
        et.triggers.Clear();

        var begin = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        begin.callback.AddListener((d) => OnBeginDrag((PointerEventData)d));
        et.triggers.Add(begin);

        var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        drag.callback.AddListener((d) => OnDrag((PointerEventData)d));
        et.triggers.Add(drag);
    }

    void Update()
    {
        if (targetRT == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRT, Mouse.current.position.ReadValue(), null, out local);
            var size = targetRT.rect.size;
            float px = local.x / size.x + 0.5f;
            float py = local.y / size.y + 0.5f;
            if (px < 0 || px > 1 || py < 0 || py > 1) return;

            float factor = scroll > 0
                ? 1f + scroll * wheelSpeed * 0.01f
                : 1f / (1f - scroll * wheelSpeed * 0.01f);
            var maxSize = _originSize * maxZoom;
            var newSize = new Vector2(
                Mathf.Clamp(targetRT.sizeDelta.x * factor, _originSize.x, maxSize.x),
                Mathf.Clamp(targetRT.sizeDelta.y * factor, _originSize.y, maxSize.y)
            );

            var pivot = targetRT.pivot;
            var delta = newSize - targetRT.sizeDelta;
            var mouseUV = new Vector2(px, py);
            targetRT.sizeDelta = newSize;
            targetRT.anchoredPosition -= new Vector2(delta.x * (mouseUV.x - pivot.x), delta.y * (mouseUV.y - pivot.y));

            if (!IsZoomed())
                targetRT.anchoredPosition = _originPos;
        }
    }

    void OnBeginDrag(PointerEventData d)
    {
        if (!IsZoomed()) { d.pointerDrag = null; return; }
        _dragOffset = (Vector2)targetRT.anchoredPosition - d.position;
    }

    void OnDrag(PointerEventData d)
    {
        if (!IsZoomed()) return;
        targetRT.anchoredPosition = d.position + _dragOffset;
    }

    bool IsZoomed()
    {
        return targetRT.sizeDelta.x > _originSize.x + 1 || targetRT.sizeDelta.y > _originSize.y + 1;
    }
}
