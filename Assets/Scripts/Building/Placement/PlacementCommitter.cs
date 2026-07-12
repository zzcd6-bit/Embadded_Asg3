using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;

public class PlacementCommitter : MonoBehaviour
{
    [SerializeField] private GridPlacementSystem gridPlacementSystem;
    [SerializeField] private bool rebakeNavMeshOnChanges = true;
    [SerializeField] private string walkableBuildingLayerName = "Walkable Building";
    [SerializeField] private NavMeshSurface[] navMeshSurfaces;

    private Coroutine navMeshRebuildRoutine;

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
        ApplyNavMeshBuildSettings(building, item);
        EnsureWorldEditButton(building);
        EnsureDeleteReactable(building);
        EnsureInteractionCollider(building);
        AddColliderGuard(building);
        ScheduleNavMeshRebuild();
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
            ScheduleNavMeshRebuild();
        }
    }

    private void ScheduleNavMeshRebuild()
    {
        if (!rebakeNavMeshOnChanges)
        {
            return;
        }

        if (navMeshRebuildRoutine != null)
        {
            StopCoroutine(navMeshRebuildRoutine);
        }

        navMeshRebuildRoutine = StartCoroutine(RebuildNavMeshNextFrame());
    }

    private IEnumerator RebuildNavMeshNextFrame()
    {
        yield return null;

        BuildConfiguredNavMeshSurfaces();
        navMeshRebuildRoutine = null;
    }

    private void BuildConfiguredNavMeshSurfaces()
    {
        if (navMeshSurfaces != null && navMeshSurfaces.Length > 0)
        {
            for (int i = 0; i < navMeshSurfaces.Length; i++)
            {
                if (navMeshSurfaces[i] != null)
                {
                    navMeshSurfaces[i].BuildNavMesh();
                }
            }

            return;
        }

        foreach (NavMeshSurface surface in NavMeshSurface.activeSurfaces)
        {
            if (surface != null)
            {
                surface.BuildNavMesh();
            }
        }
    }

    private void ApplyNavMeshBuildSettings(BuildingInstance building, BuildableItemData item)
    {
        if (building == null || item == null)
        {
            return;
        }

        NavMeshModifier modifier = building.GetComponent<NavMeshModifier>();
        if (modifier == null)
        {
            modifier = building.gameObject.AddComponent<NavMeshModifier>();
        }

        modifier.applyToChildren = true;
        modifier.ignoreFromBuild = !item.ContributesWalkableNavMesh;

        if (item.ContributesWalkableNavMesh)
        {
            EnsureWalkableNavMeshProxy(building, item);
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

    private void EnsureWalkableNavMeshProxy(BuildingInstance building, BuildableItemData item)
    {
        if (gridPlacementSystem == null || !TryGetLayer(walkableBuildingLayerName, out int walkableLayer))
        {
            return;
        }

        Transform proxyTransform = building.transform.Find("__BuildingNavMeshProxy");
        GameObject proxyObject;
        if (proxyTransform == null)
        {
            proxyObject = new GameObject("__BuildingNavMeshProxy");
            proxyObject.transform.SetParent(building.transform, false);
            proxyObject.AddComponent<BuildingNavMeshProxy>();
        }
        else
        {
            proxyObject = proxyTransform.gameObject;
        }

        proxyObject.layer = walkableLayer;
        proxyObject.transform.localRotation = Quaternion.identity;

        BoxCollider proxyCollider = proxyObject.GetComponent<BoxCollider>();
        if (proxyCollider == null)
        {
            proxyCollider = proxyObject.AddComponent<BoxCollider>();
        }

        Bounds? renderBounds = CalculateRenderBounds(building.gameObject);
        float proxyThickness = 0.12f;
        Vector2Int rotatedSize = GridPlacementSystem.GetRotatedSize(item.Size, building.RotationSteps);
        Vector3 worldSize = new(
            rotatedSize.x * gridPlacementSystem.CellSize,
            proxyThickness,
            rotatedSize.y * gridPlacementSystem.CellSize
        );

        Vector3 worldCenter = building.transform.position;
        if (renderBounds.HasValue)
        {
            Bounds bounds = renderBounds.Value;
            worldCenter = new Vector3(bounds.center.x, bounds.max.y - proxyThickness * 0.5f, bounds.center.z);
        }

        proxyObject.transform.position = worldCenter;
        proxyObject.transform.localScale = Vector3.one;
        proxyCollider.center = Vector3.zero;
        proxyCollider.size = new Vector3(
            Mathf.Max(0.05f, worldSize.x / Mathf.Max(0.0001f, proxyObject.transform.lossyScale.x)),
            proxyThickness / Mathf.Max(0.0001f, proxyObject.transform.lossyScale.y),
            Mathf.Max(0.05f, worldSize.z / Mathf.Max(0.0001f, proxyObject.transform.lossyScale.z))
        );
        proxyCollider.isTrigger = false;
    }

    private static bool TryGetLayer(string layerName, out int layer)
    {
        layer = LayerMask.NameToLayer(layerName);
        return layer >= 0;
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
