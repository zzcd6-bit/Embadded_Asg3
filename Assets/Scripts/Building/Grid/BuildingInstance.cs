using System.Collections.Generic;
using UnityEngine;

public class BuildingInstance : MonoBehaviour
{
    [SerializeField] private BuildableItemData item;
    [SerializeField] private Vector2Int pivotCell;
    [SerializeField] private Vector2Int size = Vector2Int.one;
    [SerializeField] private int rotationSteps;
    [SerializeField] private bool isPlacementCompleted;

    public BuildableItemData Item => item;
    public Vector2Int PivotCell => pivotCell;
    public Vector2Int Size => size;
    public int RotationSteps => rotationSteps;
    public bool IsPlacementCompleted => isPlacementCompleted;
    public IReadOnlyList<Vector2Int> OccupiedCells => occupiedCells;

    private readonly List<Vector2Int> occupiedCells = new();

    //Stores placement metadata and the cells occupied by this building.
    //存储该建筑的摆放元数据以及占用的格子。
    public void Initialize(BuildableItemData item, Vector2Int pivotCell, Vector2Int size, int rotationSteps, IEnumerable<Vector2Int> cells)
    {
        this.item = item;
        this.pivotCell = pivotCell;
        this.size = size;
        this.rotationSteps = rotationSteps;
        isPlacementCompleted = false;
        occupiedCells.Clear();
        occupiedCells.AddRange(cells);
    }

    //Marks the building as fully placed and ready for editing interactions.
    //将建筑标记为已完成摆放，可进行编辑交互。
    public void MarkPlacementCompleted()
    {
        isPlacementCompleted = true;
    }

    //Marks the building as temporarily being edited.
    //将建筑标记为正在临时编辑中。
    public void MarkPlacementEditing()
    {
        isPlacementCompleted = false;
    }

    //Marks an existing scene building as completed when it was not placed by the system.
    //将非系统摆放的场景既有建筑标记为已完成。
    public void MarkExistingAsCompleted()
    {
        isPlacementCompleted = true;
    }
}
