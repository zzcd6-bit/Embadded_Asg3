using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance { get; private set; }

    [Header("Discovery")]
    [SerializeField] private bool autoDiscoverSceneSpawnPoints = true;
    [SerializeField] private bool refreshTeleportSpawnsOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private readonly List<EnemySpawnPoint> spawnPoints = new List<EnemySpawnPoint>();

    private void Awake()
    {
        Instance = this;

        if (autoDiscoverSceneSpawnPoints)
        {
            DiscoverSceneSpawnPoints();
        }
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener(E_EventType.E_Player_Teleported, RefreshTeleportSpawnPoints);
    }

    private void Start()
    {
        if (refreshTeleportSpawnsOnStart)
        {
            RefreshTeleportSpawnPoints();
        }
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Player_Teleported, RefreshTeleportSpawnPoints);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RegisterSpawnPoint(EnemySpawnPoint spawnPoint)
    {
        if (spawnPoint == null || spawnPoints.Contains(spawnPoint))
            return;

        spawnPoints.Add(spawnPoint);
    }

    public void UnregisterSpawnPoint(EnemySpawnPoint spawnPoint)
    {
        if (spawnPoint == null)
            return;

        spawnPoints.Remove(spawnPoint);
    }

    public void DiscoverSceneSpawnPoints()
    {
        EnemySpawnPoint[] found = FindObjectsByType<EnemySpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < found.Length; i++)
        {
            RegisterSpawnPoint(found[i]);
        }
    }

    public void RefreshTeleportSpawnPoints()
    {
        int refreshed = RefreshByMode(EnemyRefreshMode.RefreshAfterTeleport, null);

        if (debugLog)
        {
            Debug.Log($"[EnemySpawnManager] Teleport refresh complete. Count={refreshed}", this);
        }
    }

    public void RefreshQuestConditionSpawnPoints(string questConditionId)
    {
        int refreshed = RefreshByMode(EnemyRefreshMode.QuestCondition, questConditionId);

        if (debugLog)
        {
            Debug.Log($"[EnemySpawnManager] Quest refresh complete. Condition={questConditionId}, Count={refreshed}", this);
        }
    }

    private int RefreshByMode(EnemyRefreshMode mode, string questConditionId)
    {
        int count = 0;

        for (int i = spawnPoints.Count - 1; i >= 0; i--)
        {
            EnemySpawnPoint spawnPoint = spawnPoints[i];
            if (spawnPoint == null)
            {
                spawnPoints.RemoveAt(i);
                continue;
            }

            if (spawnPoint.RefreshMode != mode)
                continue;

            if (mode == EnemyRefreshMode.QuestCondition
                && !string.Equals(spawnPoint.QuestConditionId, questConditionId, System.StringComparison.Ordinal))
            {
                continue;
            }

            spawnPoint.Refresh();
            count++;
        }

        return count;
    }
}
