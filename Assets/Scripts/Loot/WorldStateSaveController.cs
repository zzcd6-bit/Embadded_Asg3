using System.Collections.Generic;
using UnityEngine;

public class WorldStateSaveController : MonoBehaviour, IGameSaveModule
{
    public static WorldStateSaveController Instance { get; private set; }

    private static readonly HashSet<string> StaticOpenedChestIds = new HashSet<string>();
    private static readonly HashSet<string> StaticCollectedPickupIds = new HashSet<string>();

    private readonly HashSet<string> openedChestIds = new HashSet<string>();
    private readonly HashSet<string> collectedPickupIds = new HashSet<string>();

    private void Awake()
    {
        Instance = this;
    }

    public static WorldStateSaveController EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        WorldStateSaveController existing = FindAnyObjectByType<WorldStateSaveController>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject controllerObject = new GameObject("WorldStateSaveController");
        Instance = controllerObject.AddComponent<WorldStateSaveController>();
        return Instance;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void MarkChestOpened(string chestId)
    {
        if (string.IsNullOrWhiteSpace(chestId))
            return;

        StaticOpenedChestIds.Add(chestId);
        EnsureInstance();

        if (Instance != null)
        {
            Instance.openedChestIds.Add(chestId);
        }
    }

    public static void MarkPickupCollected(string pickupId)
    {
        if (string.IsNullOrWhiteSpace(pickupId))
            return;

        StaticCollectedPickupIds.Add(pickupId);
        EnsureInstance();

        if (Instance != null)
        {
            Instance.collectedPickupIds.Add(pickupId);
        }
    }

    public void CaptureGameSaveData(GameSaveData saveData)
    {
        if (saveData == null)
            return;

        if (saveData.worldState == null)
        {
            saveData.worldState = new WorldStateSaveData();
        }

        foreach (string chestId in StaticOpenedChestIds)
        {
            openedChestIds.Add(chestId);
        }

        foreach (string pickupId in StaticCollectedPickupIds)
        {
            collectedPickupIds.Add(pickupId);
        }

        foreach (string chestId in openedChestIds)
        {
            if (!saveData.worldState.openedChestIds.Contains(chestId))
            {
                saveData.worldState.openedChestIds.Add(chestId);
            }
        }

        foreach (string pickupId in collectedPickupIds)
        {
            if (!saveData.worldState.collectedPickupIds.Contains(pickupId))
            {
                saveData.worldState.collectedPickupIds.Add(pickupId);
            }
        }
    }

    public void RestoreGameSaveData(GameSaveData saveData)
    {
        openedChestIds.Clear();
        collectedPickupIds.Clear();
        StaticOpenedChestIds.Clear();
        StaticCollectedPickupIds.Clear();

        if (saveData == null || saveData.worldState == null)
            return;

        for (int i = 0; i < saveData.worldState.openedChestIds.Count; i++)
        {
            string chestId = saveData.worldState.openedChestIds[i];
            if (!string.IsNullOrWhiteSpace(chestId))
            {
                openedChestIds.Add(chestId);
                StaticOpenedChestIds.Add(chestId);
            }
        }

        for (int i = 0; i < saveData.worldState.collectedPickupIds.Count; i++)
        {
            string pickupId = saveData.worldState.collectedPickupIds[i];
            if (!string.IsNullOrWhiteSpace(pickupId))
            {
                collectedPickupIds.Add(pickupId);
                StaticCollectedPickupIds.Add(pickupId);
            }
        }
    }
}
