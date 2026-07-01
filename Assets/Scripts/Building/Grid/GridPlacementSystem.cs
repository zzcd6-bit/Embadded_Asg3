using System.Collections.Generic;
using UnityEngine;

public class GridPlacementSystem : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector3 origin;

    private readonly Dictionary<Vector2Int, CellData> cells = new();

    public float CellSize => Mathf.Max(0.01f, cellSize);

    //Converts a world position to a grid cell coordinate.
    //将世界坐标转换为网格 Cell 坐标。
    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        Vector3 local = worldPosition - origin - transform.position;
        return new Vector2Int(
            Mathf.RoundToInt(local.x / CellSize),
            Mathf.RoundToInt(local.z / CellSize)
        );
    }

    //Converts a grid cell coordinate to its world position.
    //将网格 Cell 坐标转换为对应世界坐标。
    public Vector3 CellToWorld(Vector2Int cell)
    {
        return transform.position + origin + new Vector3(cell.x * CellSize, 0f, cell.y * CellSize);
    }

    //Converts a world position to a footprint-aware pivot cell.
    //将世界坐标转换为考虑占地尺寸的 pivot cell。
    public Vector2Int WorldToPivotCell(Vector3 worldPosition, Vector2Int size, int rotationSteps)
    {
        Vector2Int footprintSize = GetRotatedSize(size, rotationSteps);
        Vector2 pivotOffset = GetPivotOffset(footprintSize);
        Vector3 local = worldPosition - origin - transform.position;

        return new Vector2Int(
            Mathf.RoundToInt(local.x / CellSize - pivotOffset.x),
            Mathf.RoundToInt(local.z / CellSize - pivotOffset.y)
        );
    }

    //Converts a footprint-aware pivot cell to its snapped world position.
    //将考虑占地尺寸的 pivot cell 转换为吸附后的世界坐标。
    public Vector3 PivotCellToWorld(Vector2Int pivotCell, Vector2Int size, int rotationSteps)
    {
        Vector2Int footprintSize = GetRotatedSize(size, rotationSteps);
        Vector2 pivotOffset = GetPivotOffset(footprintSize);
        return transform.position
            + origin
            + new Vector3((pivotCell.x + pivotOffset.x) * CellSize, 0f, (pivotCell.y + pivotOffset.y) * CellSize);
    }

    //Gets or creates the state data for a cell.
    //获取或创建指定格子的状态数据。
    public CellData GetCellData(Vector2Int cell)
    {
        if (!cells.TryGetValue(cell, out CellData data))
        {
            data = new CellData();
            cells.Add(cell, data);
        }

        return data;
    }

    //Calculates every cell covered by a footprint at the pivot cell.
    //根据 pivot cell 计算该占地区域覆盖的所有格子。
    public IReadOnlyList<Vector2Int> GetOccupiedCells(Vector2Int pivotCell, Vector2Int size, int rotationSteps)
    {
        Vector2Int footprintSize = GetRotatedSize(size, rotationSteps);
        List<Vector2Int> result = new(footprintSize.x * footprintSize.y);

        int startX = pivotCell.x - Mathf.FloorToInt((footprintSize.x - 1) * 0.5f);
        int startY = pivotCell.y - Mathf.FloorToInt((footprintSize.y - 1) * 0.5f);

        for (int x = 0; x < footprintSize.x; x++)
        {
            for (int y = 0; y < footprintSize.y; y++)
            {
                result.Add(new Vector2Int(startX + x, startY + y));
            }
        }

        return result;
    }

    //Returns whether all covered cells are free and unblocked.
    //判断覆盖的所有格子是否均为空闲且未阻挡。
    public bool CanPlace(Vector2Int pivotCell, Vector2Int size, int rotationSteps)
    {
        IReadOnlyList<Vector2Int> occupiedCells = GetOccupiedCells(pivotCell, size, rotationSteps);
        for (int i = 0; i < occupiedCells.Count; i++)
        {
            CellData data = GetCellData(occupiedCells[i]);
            if (data.isOccupied || data.isBlocked)
            {
                return false;
            }
        }

        return true;
    }

    //Marks all covered cells as occupied by the building.
    //将覆盖的所有格子标记为被该建筑占用。
    public void OccupyCells(Vector2Int pivotCell, Vector2Int size, int rotationSteps, BuildingInstance building)
    {
        IReadOnlyList<Vector2Int> occupiedCells = GetOccupiedCells(pivotCell, size, rotationSteps);
        for (int i = 0; i < occupiedCells.Count; i++)
        {
            CellData data = GetCellData(occupiedCells[i]);
            data.isOccupied = true;
            data.occupiedBy = building;
        }
    }

    //Sets whether a cell is blocked by a non-building rule.
    //设置某个格子是否被非建筑规则阻挡。
    public void SetBlocked(Vector2Int cell, bool isBlocked)
    {
        GetCellData(cell).isBlocked = isBlocked;
    }

    //Clears cells currently occupied by the given building.
    //清除当前由指定建筑占用的格子。
    public void ClearOccupied(BuildingInstance building)
    {
        if (building == null)
        {
            return;
        }

        foreach (Vector2Int cell in building.OccupiedCells)
        {
            CellData data = GetCellData(cell);
            if (data.occupiedBy == building)
            {
                data.isOccupied = false;
                data.occupiedBy = null;
            }
        }
    }

    //Clears specific cells if they belong to the given building.
    //在指定格子属于该建筑时清除它们的占用状态。
    public void ClearCells(IEnumerable<Vector2Int> targetCells, BuildingInstance building)
    {
        foreach (Vector2Int cell in targetCells)
        {
            CellData data = GetCellData(cell);
            if (data.occupiedBy == building)
            {
                data.isOccupied = false;
                data.occupiedBy = null;
            }
        }
    }

    //Returns the footprint size after quarter-turn rotation.
    //返回按 90 度步进旋转后的占地尺寸。
    public static Vector2Int GetRotatedSize(Vector2Int size, int rotationSteps)
    {
        Vector2Int safeSize = new(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
        return Mathf.Abs(rotationSteps) % 2 == 0
            ? safeSize
            : new Vector2Int(safeSize.y, safeSize.x);
    }

    //Returns the half-cell pivot offset required by even-sized footprints.
    //返回偶数尺寸占地区域所需的半格 pivot 偏移。
    private static Vector2 GetPivotOffset(Vector2Int footprintSize)
    {
        return new Vector2(
            footprintSize.x % 2 == 0 ? 0.5f : 0f,
            footprintSize.y % 2 == 0 ? 0.5f : 0f
        );
    }

    //Draws a local grid preview while the grid system is selected.
    //选中网格系统时绘制本地网格预览。
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 0.7f, 0.45f);
        Vector3 baseOrigin = transform.position + origin;
        int radius = 10;

        for (int x = -radius; x <= radius; x++)
        {
            Vector3 from = baseOrigin + new Vector3(x * CellSize, 0f, -radius * CellSize);
            Vector3 to = baseOrigin + new Vector3(x * CellSize, 0f, radius * CellSize);
            Gizmos.DrawLine(from, to);
        }

        for (int y = -radius; y <= radius; y++)
        {
            Vector3 from = baseOrigin + new Vector3(-radius * CellSize, 0f, y * CellSize);
            Vector3 to = baseOrigin + new Vector3(radius * CellSize, 0f, y * CellSize);
            Gizmos.DrawLine(from, to);
        }
    }
}
