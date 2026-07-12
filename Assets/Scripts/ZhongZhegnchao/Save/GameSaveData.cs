using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public int version = 1;
    public string sceneName;
    public PlayerSaveData player = new PlayerSaveData();
    public TeleportSaveData teleport = new TeleportSaveData();
    public BuildingSaveData building = new BuildingSaveData();
    public TimedRefreshSaveData timedRefresh = new TimedRefreshSaveData();
    public WorldStateSaveData worldState = new WorldStateSaveData();
}

[Serializable]
public class TeleportSaveData
{
    public List<string> registeredPointIds = new List<string>();
    public string currentRespawnPointId;
}

[Serializable]
public class BuildingSaveData
{
    public List<PlacedBuildingSaveData> placedBuildings = new List<PlacedBuildingSaveData>();
}

[Serializable]
public class PlacedBuildingSaveData
{
    public string itemId;
    public Vector3 position;
    public Vector3 eulerAngles;
    public Vector2Int pivotCell;
    public int rotationSteps;
}

[Serializable]
public class TimedRefreshSaveData
{
    public List<TimedRefreshEntrySaveData> entries = new List<TimedRefreshEntrySaveData>();
}

[Serializable]
public class TimedRefreshEntrySaveData
{
    public string id;
    public double nextRefreshUtcSeconds;
}

[Serializable]
public class WorldStateSaveData
{
    public List<string> openedChestIds = new List<string>();
    public List<string> collectedPickupIds = new List<string>();
}
