using UnityEngine;
using UnityEngine.Rendering;

public class YSortRenderer : MonoBehaviour
{
    [Header("Sort Pivot")]
    [SerializeField] private Transform sortPivot;
    [Tooltip("MinY=最下边缘(脚下石头/柜台前沿) MaxY=最上边缘 Center=用 pivot/transform")]
    [SerializeField] private PivotMode pivotMode = PivotMode.Center;

    [Header("Order")]
    [Tooltip("设为 false 时不再覆盖 sortingOrder，交由 Custom Axis 按 Y 与 Tilemap 等物体穿插排序")]
    [SerializeField] private bool useExplicitOrder = true;
    [SerializeField] private bool lowerYInFront = true;
    [SerializeField] private int baseOrder = 0;
    [SerializeField] private int orderOffset = 0;
    [SerializeField] [Min(1)] private int precision = 1000;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;
    [SerializeField] [Min(0.05f)] private float debugLogInterval = 0.5f;

    private SpriteRenderer[] renderers;
    private SortingGroup sortingGroup;
    private float nextDebugLogTime;

    private void Awake()
    {
        sortingGroup = GetComponent<SortingGroup>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    public void SetSortPivot(Transform pivot)
    {
        sortPivot = pivot;
    }

    private void LateUpdate()
    {
        if (!useExplicitOrder)
            return;

        float y = GetSortY();
        int directionalSign = lowerYInFront ? -1 : 1;
        int order = baseOrder + Mathf.RoundToInt(y * precision * directionalSign) + orderOffset;

        if (sortingGroup != null)
        {
            sortingGroup.sortingOrder = order;
            LogDebugIfNeeded(transform, y, order, true);
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].sortingOrder = order;
        }

        LogDebugIfNeeded(transform, y, order, false);
    }

    private float GetSortY()
    {
        if (pivotMode == PivotMode.Center)
        {
            var pivot = sortPivot != null ? sortPivot : transform;
            return pivot.position.y;
        }
        if (renderers != null && renderers.Length > 0 && renderers[0] != null)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var b = r.bounds;
                if (b.min.y < minY) minY = b.min.y;
                if (b.max.y > maxY) maxY = b.max.y;
            }
            return pivotMode == PivotMode.MinY ? minY : maxY;
        }
        return (sortPivot != null ? sortPivot : transform).position.y;
    }

    [ContextMenu("Debug Print YSort Once")]
    private void DebugPrintOnce()
    {
        float y = GetSortY();
        int directionalSign = lowerYInFront ? -1 : 1;
        int order = baseOrder + Mathf.RoundToInt(y * precision * directionalSign) + orderOffset;
        PrintDebugLine(transform, y, order, sortingGroup != null);
    }

    private void LogDebugIfNeeded(Transform pivot, float y, int order, bool usingSortingGroup)
    {
        if (!debugLogs) return;
        if (Time.unscaledTime < nextDebugLogTime) return;
        nextDebugLogTime = Time.unscaledTime + debugLogInterval;
        PrintDebugLine(pivot, y, order, usingSortingGroup);
    }

    private void PrintDebugLine(Transform t, float y, int order, bool usingSortingGroup)
    {
        string layerName = "Unknown";
        if (usingSortingGroup && sortingGroup != null)
            layerName = SortingLayer.IDToName(sortingGroup.sortingLayerID);
        else if (renderers != null && renderers.Length > 0 && renderers[0] != null)
            layerName = SortingLayer.IDToName(renderers[0].sortingLayerID);

        Debug.Log(
            $"[YSortRenderer] {name} sortY={y:0.000} rootY={transform.position.y:0.000} order={order} layer={layerName} usingSortingGroup={usingSortingGroup}");
    }
}
