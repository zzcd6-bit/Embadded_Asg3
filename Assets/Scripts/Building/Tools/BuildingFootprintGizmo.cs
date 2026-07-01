using UnityEngine;

[ExecuteAlways]
public class BuildingFootprintGizmo : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BuildableItemData sourceItem;
    [SerializeField] private bool syncSizeFromSourceItem = true;

    [Header("Footprint")]
    [SerializeField] private Vector2Int size = Vector2Int.one;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector3 localCenterOffset;
    [SerializeField] private float localGroundHeight;

    [Header("Gizmo")]
    [SerializeField] private bool drawWhenNotSelected = true;
    [SerializeField] private Color cellColor = new(0.15f, 0.75f, 0.55f, 0.18f);
    [SerializeField] private Color lineColor = new(0.15f, 0.95f, 0.75f, 0.9f);
    [SerializeField] private Color borderColor = new(1f, 0.9f, 0.28f, 1f);
    [SerializeField] private Color pivotColor = new(1f, 0.35f, 0.18f, 1f);

    public BuildableItemData SourceItem => sourceItem;
    public Vector2Int Size => SafeSize(size);
    public float CellSize => Mathf.Max(0.01f, cellSize);
    public Vector3 LocalCenterOffset => localCenterOffset;
    public float LocalGroundHeight => localGroundHeight;

    //Keeps footprint values safe and optionally synced from item data.
    //保持占地数据有效，并可选择从物品数据同步。
    private void OnValidate()
    {
        if (syncSizeFromSourceItem && sourceItem != null)
        {
            size = sourceItem.Size;
        }

        size = SafeSize(size);
        cellSize = Mathf.Max(0.01f, cellSize);
    }

    //Draws the footprint even when the object is not selected if enabled.
    //开启时即使物体未被选中也绘制占地范围。
    private void OnDrawGizmos()
    {
        if (drawWhenNotSelected)
        {
            DrawFootprint();
        }
    }

    //Draws the footprint while the object is selected.
    //物体被选中时绘制占地范围。
    private void OnDrawGizmosSelected()
    {
        DrawFootprint();
    }

    //Copies footprint size from the linked buildable item.
    //从关联的可建造物品复制占地尺寸。
    public void SyncFromSourceItem()
    {
        if (sourceItem != null)
        {
            size = sourceItem.Size;
        }
    }

    //Draws the full footprint grid in local prefab space.
    //在 prefab 本地空间绘制完整占地网格。
    private void DrawFootprint()
    {
        Vector2Int safeSize = SafeSize(size);
        float safeCellSize = Mathf.Max(0.01f, cellSize);
        float width = safeSize.x * safeCellSize;
        float depth = safeSize.y * safeCellSize;
        float y = localGroundHeight;
        Vector3 centerOffset = localCenterOffset + Vector3.up * y;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        DrawCells(safeSize, safeCellSize, width, depth, centerOffset);
        DrawGridLines(safeSize, safeCellSize, width, depth, centerOffset);
        DrawPivot(centerOffset, safeCellSize);

        Gizmos.matrix = previousMatrix;
    }

    //Draws translucent filled cells for the footprint.
    //为占地区域绘制半透明填充格子。
    private void DrawCells(Vector2Int footprintSize, float safeCellSize, float width, float depth, Vector3 centerOffset)
    {
        Gizmos.color = cellColor;

        float startX = -width * 0.5f + safeCellSize * 0.5f;
        float startZ = -depth * 0.5f + safeCellSize * 0.5f;

        for (int x = 0; x < footprintSize.x; x++)
        {
            for (int z = 0; z < footprintSize.y; z++)
            {
                Vector3 cellCenter = centerOffset + new Vector3(startX + x * safeCellSize, 0f, startZ + z * safeCellSize);
                Gizmos.DrawCube(cellCenter, new Vector3(safeCellSize, 0.012f, safeCellSize));
            }
        }
    }

    //Draws footprint grid lines and outer border.
    //绘制占地网格线和外边框。
    private void DrawGridLines(Vector2Int footprintSize, float safeCellSize, float width, float depth, Vector3 centerOffset)
    {
        float minX = -width * 0.5f;
        float maxX = width * 0.5f;
        float minZ = -depth * 0.5f;
        float maxZ = depth * 0.5f;

        Gizmos.color = lineColor;
        for (int x = 0; x <= footprintSize.x; x++)
        {
            float lineX = minX + x * safeCellSize;
            Gizmos.DrawLine(centerOffset + new Vector3(lineX, 0f, minZ), centerOffset + new Vector3(lineX, 0f, maxZ));
        }

        for (int z = 0; z <= footprintSize.y; z++)
        {
            float lineZ = minZ + z * safeCellSize;
            Gizmos.DrawLine(centerOffset + new Vector3(minX, 0f, lineZ), centerOffset + new Vector3(maxX, 0f, lineZ));
        }

        Gizmos.color = borderColor;
        Vector3 a = centerOffset + new Vector3(minX, 0f, minZ);
        Vector3 b = centerOffset + new Vector3(maxX, 0f, minZ);
        Vector3 c = centerOffset + new Vector3(maxX, 0f, maxZ);
        Vector3 d = centerOffset + new Vector3(minX, 0f, maxZ);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }

    //Draws the pivot marker and forward direction marker.
    //绘制 pivot 标记和前方方向标记。
    private void DrawPivot(Vector3 centerOffset, float safeCellSize)
    {
        float markerSize = safeCellSize * 0.18f;

        Gizmos.color = pivotColor;
        Gizmos.DrawSphere(centerOffset, markerSize);
        Gizmos.DrawLine(centerOffset + Vector3.left * safeCellSize * 0.35f, centerOffset + Vector3.right * safeCellSize * 0.35f);
        Gizmos.DrawLine(centerOffset + Vector3.back * safeCellSize * 0.35f, centerOffset + Vector3.forward * safeCellSize * 0.35f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(centerOffset, centerOffset + Vector3.forward * safeCellSize * 0.75f);
    }

    //Clamps footprint size so each axis is at least one cell.
    //限制占地尺寸，确保每个轴至少为一格。
    private static Vector2Int SafeSize(Vector2Int value)
    {
        return new Vector2Int(Mathf.Max(1, value.x), Mathf.Max(1, value.y));
    }
}
