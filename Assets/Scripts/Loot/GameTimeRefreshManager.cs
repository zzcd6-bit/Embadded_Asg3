using System;
using System.Collections.Generic;
using UnityEngine;

public class GameTimeRefreshManager : MonoBehaviour, IGameSaveModule
{
    public static GameTimeRefreshManager Instance { get; private set; }

    [Header("Refresh")]
    [SerializeField, Min(60f)] private float defaultRefreshSeconds = 10800f;
    [SerializeField, Min(5f)] private float lowFrequencyCheckSeconds = 60f;

    [Header("Test")]
    [SerializeField] private KeyCode clearCacheKey = KeyCode.P;
    [SerializeField] private bool clearSaveFileWithCache = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private readonly Dictionary<string, double> nextRefreshById = new Dictionary<string, double>();
    private readonly List<TimedPickupSpawner> spawners = new List<TimedPickupSpawner>();
    private float nextCheckTime;

    public float DefaultRefreshSeconds => defaultRefreshSeconds;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RegisterSceneSpawners();
        RefreshReadySpawners();
    }

    private void Update()
    {
        if (Input.GetKeyDown(clearCacheKey))
        {
            ClearCacheAndLog();
        }

        if (Time.unscaledTime < nextCheckTime)
            return;

        nextCheckTime = Time.unscaledTime + lowFrequencyCheckSeconds;
        RefreshReadySpawners();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RegisterSpawner(TimedPickupSpawner spawner)
    {
        if (spawner == null || spawners.Contains(spawner))
            return;

        spawners.Add(spawner);
    }

    public void UnregisterSpawner(TimedPickupSpawner spawner)
    {
        if (spawner == null)
            return;

        spawners.Remove(spawner);
    }

    public bool ShouldSpawnNow(string id, bool spawnImmediatelyIfNeverSpawned)
    {
        if (string.IsNullOrWhiteSpace(id))
            return spawnImmediatelyIfNeverSpawned;

        if (!nextRefreshById.TryGetValue(id, out double nextRefreshUtcSeconds))
        {
            return spawnImmediatelyIfNeverSpawned;
        }

        return GetUtcNowSeconds() >= nextRefreshUtcSeconds;
    }

    public void ScheduleRefresh(string id, float refreshSeconds)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        double nextRefresh = GetUtcNowSeconds() + Mathf.Max(60f, refreshSeconds);
        nextRefreshById[id] = nextRefresh;

        if (debugLog)
        {
            Debug.Log($"[GameTimeRefreshManager] Scheduled refresh. Id={id}, NextUtc={nextRefresh}", this);
        }
    }

    public void ClearRefresh(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        nextRefreshById.Remove(id);
    }

    public void CaptureGameSaveData(GameSaveData saveData)
    {
        if (saveData == null)
            return;

        if (saveData.timedRefresh == null)
        {
            saveData.timedRefresh = new TimedRefreshSaveData();
        }

        saveData.timedRefresh.entries.Clear();

        foreach (KeyValuePair<string, double> pair in nextRefreshById)
        {
            saveData.timedRefresh.entries.Add(new TimedRefreshEntrySaveData
            {
                id = pair.Key,
                nextRefreshUtcSeconds = pair.Value
            });
        }
    }

    public void RestoreGameSaveData(GameSaveData saveData)
    {
        nextRefreshById.Clear();

        if (saveData != null && saveData.timedRefresh != null && saveData.timedRefresh.entries != null)
        {
            for (int i = 0; i < saveData.timedRefresh.entries.Count; i++)
            {
                TimedRefreshEntrySaveData entry = saveData.timedRefresh.entries[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.id))
                {
                    nextRefreshById[entry.id] = entry.nextRefreshUtcSeconds;
                }
            }
        }

        RegisterSceneSpawners();
        RefreshReadySpawners();
    }

    public void ClearCacheAndLog()
    {
        int cleared = nextRefreshById.Count;
        nextRefreshById.Clear();

        RegisterSceneSpawners();

        for (int i = 0; i < spawners.Count; i++)
        {
            if (spawners[i] != null)
            {
                spawners[i].ClearRefreshCache();
            }
        }

        if (clearSaveFileWithCache && PlayerSaveManager.Instance != null)
        {
            PlayerSaveManager.Instance.ClearSaveCache();
        }

        Debug.Log($"[GameTimeRefreshManager] Test key P cleared timed refresh cache. Entries={cleared}, Spawners={spawners.Count}", this);
    }

    private void RegisterSceneSpawners()
    {
        TimedPickupSpawner[] sceneSpawners = FindObjectsByType<TimedPickupSpawner>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < sceneSpawners.Length; i++)
        {
            RegisterSpawner(sceneSpawners[i]);
        }
    }

    private void RefreshReadySpawners()
    {
        for (int i = spawners.Count - 1; i >= 0; i--)
        {
            TimedPickupSpawner spawner = spawners[i];
            if (spawner == null)
            {
                spawners.RemoveAt(i);
                continue;
            }

            spawner.TrySpawnIfReady();
        }
    }

    private static double GetUtcNowSeconds()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
