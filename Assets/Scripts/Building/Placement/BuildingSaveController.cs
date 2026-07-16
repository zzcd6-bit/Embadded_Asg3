using System.Collections.Generic;
using UnityEngine;

public class BuildingSaveController : MonoBehaviour, IGameSaveModule
{
    [Header("References")]
    [SerializeField] private PlacementCommitter placementCommitter;

    [Header("Catalog")]
    [SerializeField] private List<BuildableItemData> buildableItems = new List<BuildableItemData>();

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private readonly Dictionary<string, BuildableItemData> itemMap = new Dictionary<string, BuildableItemData>();

    private void Awake()
    {
        ResolveReferences();
        RebuildItemMap();
    }

    public void CaptureGameSaveData(GameSaveData saveData)
    {
        if (saveData == null)
            return;

        if (saveData.building == null)
        {
            saveData.building = new BuildingSaveData();
        }

        saveData.building.placedBuildings.Clear();

        PlayerPlacedBuildingMarker[] markers = FindObjectsByType<PlayerPlacedBuildingMarker>(
            FindObjectsInactive.Exclude
        );

        for (int i = 0; i < markers.Length; i++)
        {
            BuildingInstance building = markers[i].GetComponent<BuildingInstance>();
            if (building == null || !building.IsPlacementCompleted || building.Item == null)
                continue;

            saveData.building.placedBuildings.Add(new PlacedBuildingSaveData
            {
                itemId = building.Item.ItemId,
                position = building.transform.position,
                eulerAngles = building.transform.eulerAngles,
                pivotCell = building.PivotCell,
                rotationSteps = building.RotationSteps
            });
        }

        if (debugLog)
        {
            Debug.Log($"[BuildingSaveController] Captured buildings: {saveData.building.placedBuildings.Count}", this);
        }
    }

    public void RestoreGameSaveData(GameSaveData saveData)
    {
        if (saveData == null || saveData.building == null)
            return;

        ResolveReferences();
        RebuildItemMap();

        if (placementCommitter == null)
        {
            Debug.LogWarning("[BuildingSaveController] PlacementCommitter is missing. Building restore skipped.", this);
            return;
        }

        ClearExistingSavedBuildings();

        for (int i = 0; i < saveData.building.placedBuildings.Count; i++)
        {
            PlacedBuildingSaveData buildingData = saveData.building.placedBuildings[i];
            if (buildingData == null || string.IsNullOrWhiteSpace(buildingData.itemId))
                continue;

            if (!itemMap.TryGetValue(buildingData.itemId, out BuildableItemData item) || item == null)
            {
                Debug.LogWarning($"[BuildingSaveController] Missing buildable item id: {buildingData.itemId}", this);
                continue;
            }

            Quaternion rotation = Quaternion.Euler(buildingData.eulerAngles);
            BuildingInstance building = placementCommitter.CreateBuilding(item, buildingData.position, rotation);
            placementCommitter.Commit(building, item, buildingData.pivotCell, buildingData.rotationSteps);
        }

        if (debugLog)
        {
            Debug.Log($"[BuildingSaveController] Restored buildings: {saveData.building.placedBuildings.Count}", this);
        }
    }

    private void ResolveReferences()
    {
        if (placementCommitter == null)
        {
            placementCommitter = FindAnyObjectByType<PlacementCommitter>();
        }
    }

    private void RebuildItemMap()
    {
        itemMap.Clear();
        AddBuildModeCatalogItems();

        for (int i = 0; i < buildableItems.Count; i++)
        {
            BuildableItemData item = buildableItems[i];
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                continue;

            itemMap[item.ItemId] = item;
        }
    }

    private void AddBuildModeCatalogItems()
    {
        BuildModeController buildModeController = BuildModeController.Instance;
        if (buildModeController == null)
        {
            buildModeController = FindAnyObjectByType<BuildModeController>();
        }

        if (buildModeController == null)
            return;

        IReadOnlyList<BuildModeController.BuildObjectEntry> entries = buildModeController.BuildObjects;
        for (int i = 0; i < entries.Count; i++)
        {
            BuildableItemData item = entries[i].Item;
            if (item != null && !buildableItems.Contains(item))
            {
                buildableItems.Add(item);
            }
        }
    }

    private void ClearExistingSavedBuildings()
    {
        if (placementCommitter == null)
            return;

        PlayerPlacedBuildingMarker[] markers = FindObjectsByType<PlayerPlacedBuildingMarker>(
            FindObjectsInactive.Include
        );

        for (int i = 0; i < markers.Length; i++)
        {
            BuildingInstance building = markers[i].GetComponent<BuildingInstance>();
            if (building != null)
            {
                placementCommitter.Delete(building);
            }
        }
    }
}
