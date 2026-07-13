using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TeleportPointRegistry : MonoBehaviour, IGameSaveModule
{
    public static TeleportPointRegistry Instance { get; private set; }

    [Header("Discovery")]
    [SerializeField] private bool autoDiscoverScenePoints = true;
    [SerializeField] private List<TeleportPoint> scenePoints = new List<TeleportPoint>();

    [Header("Respawn")]
    [SerializeField] private TeleportPoint defaultRespawnPoint;
    [SerializeField] private string currentRespawnPointId;

    [Header("Registered Point Ids")]
    [SerializeField] private List<string> registeredPointIds = new List<string>();

    [Header("Debug Memory")]
    [SerializeField] private bool enableClearMemoryHotkey = true;
    [SerializeField] private KeyCode clearMemoryKey = KeyCode.P;
    [SerializeField] private bool clearSaveCacheWithHotkey = true;

    private readonly Dictionary<string, TeleportPoint> pointMap = new Dictionary<string, TeleportPoint>();
    private readonly HashSet<string> registeredIds = new HashSet<string>();

    public event Action<TeleportPoint> PointRegistered;
    public event Action<TeleportPoint> RespawnPointChanged;

    public TeleportPoint CurrentRespawnPoint => GetPoint(currentRespawnPointId) ?? defaultRespawnPoint;
    public TeleportPoint DefaultRespawnPoint => defaultRespawnPoint;
    public string CurrentRespawnPointId => currentRespawnPointId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[TeleportPointRegistry] Multiple instances found. Keeping the latest instance.", this);
        }

        Instance = this;

        if (autoDiscoverScenePoints)
        {
            DiscoverScenePoints();
        }

        RebuildLookup();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!enableClearMemoryHotkey || !Input.GetKeyDown(clearMemoryKey))
            return;

        ClearTeleportMemory();

        if (clearSaveCacheWithHotkey && PlayerSaveManager.Instance != null)
        {
            PlayerSaveManager.Instance.ClearSaveCache();
        }
    }

    public void DiscoverScenePoints()
    {
        TeleportPoint[] foundPoints = FindObjectsByType<TeleportPoint>(
            FindObjectsInactive.Include
        );

        for (int i = 0; i < foundPoints.Length; i++)
        {
            RegisterScenePoint(foundPoints[i]);
        }
    }

    public void RegisterScenePoint(TeleportPoint point)
    {
        if (point == null || scenePoints.Contains(point))
            return;

        scenePoints.Add(point);
    }

    public void RebuildLookup()
    {
        pointMap.Clear();
        registeredIds.Clear();

        for (int i = 0; i < scenePoints.Count; i++)
        {
            TeleportPoint point = scenePoints[i];
            if (point == null || string.IsNullOrWhiteSpace(point.PointId))
                continue;

            pointMap[point.PointId] = point;

            if (point.IsDefaultRespawnPoint && defaultRespawnPoint == null)
            {
                defaultRespawnPoint = point;
            }
        }

        for (int i = 0; i < registeredPointIds.Count; i++)
        {
            string pointId = registeredPointIds[i];
            if (!string.IsNullOrWhiteSpace(pointId))
            {
                registeredIds.Add(pointId);
            }
        }
    }

    public bool IsRegistered(TeleportPoint point)
    {
        return point != null && IsRegistered(point.PointId);
    }

    public bool IsRegistered(string pointId)
    {
        return !string.IsNullOrWhiteSpace(pointId) && registeredIds.Contains(pointId);
    }

    public bool RegisterPoint(TeleportPoint point, bool setAsRespawnPoint)
    {
        if (point == null || string.IsNullOrWhiteSpace(point.PointId))
            return false;

        RegisterScenePoint(point);
        pointMap[point.PointId] = point;

        bool added = registeredIds.Add(point.PointId);
        if (added && !registeredPointIds.Contains(point.PointId))
        {
            registeredPointIds.Add(point.PointId);
        }

        if (added)
        {
            PointRegistered?.Invoke(point);
        }

        if (setAsRespawnPoint)
        {
            SetRespawnPoint(point);
        }

        return added;
    }

    public void SetRespawnPoint(TeleportPoint point)
    {
        if (point == null || string.IsNullOrWhiteSpace(point.PointId))
            return;

        RegisterScenePoint(point);
        pointMap[point.PointId] = point;

        if (!IsRegistered(point))
        {
            RegisterPoint(point, false);
        }

        if (currentRespawnPointId == point.PointId)
            return;

        currentRespawnPointId = point.PointId;
        RespawnPointChanged?.Invoke(point);
    }

    public TeleportPoint GetPoint(string pointId)
    {
        if (string.IsNullOrWhiteSpace(pointId))
            return null;

        if (pointMap.TryGetValue(pointId, out TeleportPoint point))
        {
            return point;
        }

        return null;
    }

    public List<TeleportPoint> GetRegisteredPoints()
    {
        List<TeleportPoint> result = new List<TeleportPoint>();

        for (int i = 0; i < registeredPointIds.Count; i++)
        {
            TeleportPoint point = GetPoint(registeredPointIds[i]);
            if (point != null)
            {
                result.Add(point);
            }
        }

        return result;
    }

    public bool TryGetNextRegisteredPoint(TeleportPoint fromPoint, out TeleportPoint nextPoint)
    {
        List<TeleportPoint> registeredPoints = GetRegisteredPoints();
        nextPoint = null;

        if (registeredPoints.Count == 0)
            return false;

        int startIndex = fromPoint != null ? registeredPoints.IndexOf(fromPoint) : -1;

        for (int offset = 1; offset <= registeredPoints.Count; offset++)
        {
            int index = (startIndex + offset) % registeredPoints.Count;
            TeleportPoint candidate = registeredPoints[index];

            if (candidate != null && candidate != fromPoint)
            {
                nextPoint = candidate;
                return true;
            }
        }

        return false;
    }

    public bool TryTeleportTo(string pointId, bool requireRegistered = true, bool setAsRespawnPoint = true)
    {
        TeleportPoint point = GetPoint(pointId);
        return TryTeleportTo(point, requireRegistered, setAsRespawnPoint);
    }

    public bool TryTeleportTo(TeleportPoint point, bool requireRegistered = true, bool setAsRespawnPoint = true)
    {
        if (point == null)
            return false;

        RegisterScenePoint(point);

        if (!string.IsNullOrWhiteSpace(point.PointId))
        {
            pointMap[point.PointId] = point;
        }

        if (requireRegistered && !IsRegistered(point))
            return false;

        return point.TeleportPlayerHere(setAsRespawnPoint);
    }

    public bool TryTeleportToNextRegistered(TeleportPoint fromPoint = null)
    {
        if (!TryGetNextRegisteredPoint(fromPoint, out TeleportPoint nextPoint))
            return false;

        return TryTeleportTo(nextPoint, true, true);
    }

    public bool TryRegisterPoint(string pointId, bool setAsRespawnPoint = true)
    {
        return RegisterPoint(GetPoint(pointId), setAsRespawnPoint);
    }

    public void ClearTeleportMemory()
    {
        registeredPointIds.Clear();
        registeredIds.Clear();
        currentRespawnPointId = string.Empty;
        RefreshAllPointInteractionStates();

        Debug.Log("[TeleportPointRegistry] Teleport memory cleared.", this);
    }

    public void RefreshAllPointInteractionStates()
    {
        for (int i = 0; i < scenePoints.Count; i++)
        {
            if (scenePoints[i] != null)
            {
                scenePoints[i].RefreshInteractionState();
            }
        }
    }

    public List<string> ExportRegisteredPointIds()
    {
        return new List<string>(registeredPointIds);
    }

    public void ImportRegisteredPointIds(List<string> pointIds, string respawnPointId)
    {
        registeredPointIds.Clear();

        if (pointIds != null)
        {
            for (int i = 0; i < pointIds.Count; i++)
            {
                string pointId = pointIds[i];
                if (!string.IsNullOrWhiteSpace(pointId) && !registeredPointIds.Contains(pointId))
                {
                    registeredPointIds.Add(pointId);
                }
            }
        }

        currentRespawnPointId = respawnPointId;
        RebuildLookup();
        RefreshAllPointInteractionStates();
    }

    public void CaptureGameSaveData(GameSaveData saveData)
    {
        if (saveData == null)
            return;

        if (saveData.teleport == null)
        {
            saveData.teleport = new TeleportSaveData();
        }

        saveData.teleport.registeredPointIds = ExportRegisteredPointIds();
        saveData.teleport.currentRespawnPointId = currentRespawnPointId;
    }

    public void RestoreGameSaveData(GameSaveData saveData)
    {
        if (saveData == null || saveData.teleport == null)
            return;

        ImportRegisteredPointIds(
            saveData.teleport.registeredPointIds,
            saveData.teleport.currentRespawnPointId
        );

        RefreshAllPointInteractionStates();
    }
}
