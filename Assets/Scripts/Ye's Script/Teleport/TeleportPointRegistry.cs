using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TeleportPointRegistry : MonoBehaviour
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

    private readonly Dictionary<string, TeleportPoint> pointMap = new Dictionary<string, TeleportPoint>();
    private readonly HashSet<string> registeredIds = new HashSet<string>();

    public event Action<TeleportPoint> PointRegistered;
    public event Action<TeleportPoint> RespawnPointChanged;

    public TeleportPoint CurrentRespawnPoint => GetPoint(currentRespawnPointId) ?? defaultRespawnPoint;
    public TeleportPoint DefaultRespawnPoint => defaultRespawnPoint;

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
    }
}
