using UnityEngine;
using UnityEngine.Tilemaps;

public enum PivotMode { Center, MinY, MaxY }

/// <summary>
/// 按 Y 为 Tilemap 设置 sortingOrder，与 YSortRenderer 共用公式，使整块 Tilemap 与 Player 按位置正确穿插。
/// 使用 Chunk 模式即可，无需 Individual。
/// </summary>
[RequireComponent(typeof(TilemapRenderer))]
public class TilemapYSort : MonoBehaviour
{
    [Header("Sort Pivot")]
    [Tooltip("不指定则使用 Tilemap Bounds 的 Y，见 Pivot Mode")]
    [SerializeField] private Transform sortPivot;
    [Tooltip("Center=Bounds中心 MinY=最下边缘(柜台前沿) MaxY=最上边缘")]
    [SerializeField] private PivotMode pivotMode = PivotMode.Center;

    [Header("Order - 需与 YSortRenderer 保持一致")]
    [SerializeField] private bool lowerYInFront = true;
    [SerializeField] private int baseOrder = 0;
    [SerializeField] private int orderOffset = 0;
    [SerializeField] [Min(1)] private int precision = 1000;

    private TilemapRenderer _tilemapRenderer;
    private Tilemap _tilemap;

    private void Awake()
    {
        _tilemapRenderer = GetComponent<TilemapRenderer>();
        _tilemap = GetComponent<Tilemap>();
    }

    private void LateUpdate()
    {
        float y = GetSortY();
        int sign = lowerYInFront ? -1 : 1;
        int order = baseOrder + Mathf.RoundToInt(y * precision * sign) + orderOffset;
        _tilemapRenderer.sortingOrder = order;
    }

    private float GetSortY()
    {
        if (sortPivot != null)
            return sortPivot.position.y;

        if (_tilemap != null && _tilemap.cellBounds.size != Vector3Int.zero)
        {
            var b = _tilemap.cellBounds;
            int yCell = pivotMode == PivotMode.MinY ? b.yMin : (pivotMode == PivotMode.MaxY ? b.yMax : (b.yMin + b.yMax) / 2);
            var cell = new Vector3Int((b.xMin + b.xMax) / 2, yCell, (b.zMin + b.zMax) / 2);
            return _tilemap.GetCellCenterWorld(cell).y;
        }

        return transform.position.y;
    }
}
