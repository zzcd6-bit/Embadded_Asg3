using UnityEngine;

public class PlacementCommitter : MonoBehaviour
{
    [SerializeField] private GridPlacementSystem gridPlacementSystem;

    //Assigns the grid system used for occupancy writes.
    //分配用于写入占用状态的网格系统。
    public void Initialize(GridPlacementSystem grid)
    {
        gridPlacementSystem = grid;
    }

    //Instantiates a final building object from item data.
    //根据物品数据实例化最终建筑物体。
    public BuildingInstance CreateBuilding(BuildableItemData item, Vector3 position, Quaternion rotation)
    {
        GameObject placedObject = Instantiate(item.BuildingPrefab, position, rotation);
        BuildingInstance building = placedObject.GetComponent<BuildingInstance>();
        if (building == null)
        {
            building = placedObject.AddComponent<BuildingInstance>();
        }

        return building;
    }

    //Commits a building to the grid and enables post-placement behavior.
    //将建筑提交到网格，并启用放置后的行为。
    public void Commit(BuildingInstance building, BuildableItemData item, Vector2Int pivotCell, int rotationSteps)
    {
        if (building == null || item == null || gridPlacementSystem == null)
        {
            return;
        }

        building.Initialize(
            item,
            pivotCell,
            item.Size,
            rotationSteps,
            gridPlacementSystem.GetOccupiedCells(pivotCell, item.Size, rotationSteps)
        );

        gridPlacementSystem.OccupyCells(pivotCell, item.Size, rotationSteps, building);
        EnsurePlayerPlacedMarker(building);
        EnsureWorldEditButton(building);
        EnsureDeleteReactable(building);
        EnsureInteractionCollider(building);
        AddColliderGuard(building);
    }

    private static void EnsurePlayerPlacedMarker(BuildingInstance building)
    {
        if (building.GetComponent<PlayerPlacedBuildingMarker>() == null)
        {
            building.gameObject.AddComponent<PlayerPlacedBuildingMarker>();
        }
    }

    //Restores an edited building to its original transform and grid cells.
    //将编辑中的建筑恢复到原始 Transform 和网格占用。
    public void Restore(BuildingInstance building, BuildableItemData item, Vector2Int pivotCell, int rotationSteps, Vector3 position, Quaternion rotation)
    {
        if (building == null)
        {
            return;
        }

        building.transform.SetPositionAndRotation(position, rotation);
        Commit(building, item, pivotCell, rotationSteps);
    }

    //Deletes a building instance from the scene.
    //从场景中删除建筑实例。
    public void Delete(BuildingInstance building)
    {
        if (building != null)
        {
            if (gridPlacementSystem != null)
            {
                gridPlacementSystem.ClearOccupied(building);
            }

            Destroy(building.gameObject);
        }
    }

    //Adds temporary collision protection after placement.
    //在放置完成后添加临时碰撞保护。
    private static void AddColliderGuard(BuildingInstance building)
    {
        BuildingPlacementColliderGuard colliderGuard = building.GetComponent<BuildingPlacementColliderGuard>();
        if (colliderGuard == null)
        {
            colliderGuard = building.gameObject.AddComponent<BuildingPlacementColliderGuard>();
        }

        colliderGuard.Begin(building);
    }

    private void EnsureDeleteReactable(BuildingInstance building)
    {
        BuildingDeleteReactable deleteReactable = building.GetComponent<BuildingDeleteReactable>();
        if (deleteReactable == null)
        {
            deleteReactable = building.gameObject.AddComponent<BuildingDeleteReactable>();
        }

        deleteReactable.Initialize(building, this);
    }

    private static void EnsureInteractionCollider(BuildingInstance building)
    {
        if (building.GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        Bounds? renderBounds = CalculateRenderBounds(building.gameObject);
        BoxCollider collider = building.gameObject.AddComponent<BoxCollider>();
        if (!renderBounds.HasValue)
        {
            collider.size = Vector3.one;
            return;
        }

        Bounds bounds = renderBounds.Value;
        Vector3 localCenter = building.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = building.transform.InverseTransformVector(bounds.size);
        collider.center = localCenter;
        collider.size = new Vector3(
            Mathf.Abs(localSize.x),
            Mathf.Abs(localSize.y),
            Mathf.Abs(localSize.z)
        );
    }

    private static Bounds? CalculateRenderBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return null;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    //Ensures the building owns a world-space edit button.
    //确保建筑拥有自己的世界空间编辑按钮。
    private static void EnsureWorldEditButton(BuildingInstance building)
    {
        BuildingWorldEditButton editButton = building.GetComponentInChildren<BuildingWorldEditButton>(true);
        if (editButton == null)
        {
            editButton = building.gameObject.AddComponent<BuildingWorldEditButton>();
        }

        editButton.RefreshReferences();
    }
}
