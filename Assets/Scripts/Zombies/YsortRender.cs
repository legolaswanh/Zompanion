using UnityEngine;
using UnityEngine.Rendering;

public class YSortRenderer : MonoBehaviour
{
    [Header("Sort Pivot")]
    [SerializeField] private Transform sortPivot;

    [Header("Order")]
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
        Transform pivot = sortPivot != null ? sortPivot : transform;
        float y = pivot.position.y;
        int directionalSign = lowerYInFront ? -1 : 1;
        int order = baseOrder + Mathf.RoundToInt(y * precision * directionalSign) + orderOffset;

        if (sortingGroup != null)
        {
            sortingGroup.sortingOrder = order;
            LogDebugIfNeeded(pivot, y, order, true);
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].sortingOrder = order;
        }

        LogDebugIfNeeded(pivot, y, order, false);
    }

    [ContextMenu("Debug Print YSort Once")]
    private void DebugPrintOnce()
    {
        Transform pivot = sortPivot != null ? sortPivot : transform;
        float y = pivot.position.y;
        int directionalSign = lowerYInFront ? -1 : 1;
        int order = baseOrder + Mathf.RoundToInt(y * precision * directionalSign) + orderOffset;
        PrintDebugLine(pivot, y, order, sortingGroup != null);
    }

    private void LogDebugIfNeeded(Transform pivot, float y, int order, bool usingSortingGroup)
    {
        if (!debugLogs) return;
        if (Time.unscaledTime < nextDebugLogTime) return;
        nextDebugLogTime = Time.unscaledTime + debugLogInterval;
        PrintDebugLine(pivot, y, order, usingSortingGroup);
    }

    private void PrintDebugLine(Transform pivot, float y, int order, bool usingSortingGroup)
    {
        string layerName = "Unknown";
        if (usingSortingGroup && sortingGroup != null)
            layerName = SortingLayer.IDToName(sortingGroup.sortingLayerID);
        else if (renderers != null && renderers.Length > 0 && renderers[0] != null)
            layerName = SortingLayer.IDToName(renderers[0].sortingLayerID);

        Debug.Log(
            $"[YSortRenderer] {name} pivot={pivot.name} pivotY={y:0.000} rootY={transform.position.y:0.000} localPivotY={pivot.localPosition.y:0.000} order={order} layer={layerName} usingSortingGroup={usingSortingGroup}");
    }
}
