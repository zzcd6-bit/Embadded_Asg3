using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class TimedPickupSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject pickupPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform spawnedParent;
    [SerializeField] private GameObject currentPickup;
    [SerializeField] private bool spawnImmediatelyIfNeverSpawned = true;

    [Header("Respawn Time")]
    [SerializeField, Min(60f)] private float respawnSeconds = 10800f;
    [SerializeField] private string spawnerId;

    [Header("Events")]
    [SerializeField] private UnityEvent<GameObject> onSpawned = new();

    private PickupItem subscribedPickup;

    public string SpawnerId => spawnerId;
    public GameObject CurrentPickup => currentPickup;
    public UnityEvent<GameObject> OnSpawned => onSpawned;

    private void Awake()
    {
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        SubscribeToCurrentPickup();
    }

    private void OnEnable()
    {
        GameTimeRefreshManager.Instance?.RegisterSpawner(this);
        SubscribeToCurrentPickup();
    }

    private void Start()
    {
        ResolveManager()?.RegisterSpawner(this);
        TrySpawnIfReady();
    }

    public bool TrySpawnIfReady()
    {
        if (currentPickup != null)
        {
            return false;
        }

        GameTimeRefreshManager manager = ResolveManager();
        bool shouldSpawn = manager == null
            ? spawnImmediatelyIfNeverSpawned
            : manager.ShouldSpawnNow(spawnerId, spawnImmediatelyIfNeverSpawned);

        if (!shouldSpawn)
        {
            return false;
        }

        Spawn();
        return true;
    }

    public GameObject Spawn()
    {
        if (pickupPrefab == null)
        {
            Debug.LogWarning("[TimedPickupSpawner] Pickup prefab is missing.", this);
            return null;
        }

        Transform origin = spawnPoint != null ? spawnPoint : transform;
        currentPickup = Instantiate(
            pickupPrefab,
            origin.position,
            origin.rotation,
            spawnedParent);

        ResolveManager()?.ClearRefresh(spawnerId);
        SubscribeToCurrentPickup();
        onSpawned?.Invoke(currentPickup);
        return currentPickup;
    }

    public void ClearCurrentPickup()
    {
        UnsubscribeFromCurrentPickup();
        currentPickup = null;
        ResolveManager()?.ScheduleRefresh(spawnerId, respawnSeconds);
    }

    public void ClearRefreshCache()
    {
        ResolveManager()?.ClearRefresh(spawnerId);
        ClearLegacyPlayerPrefsCache();
        TrySpawnIfReady();
    }

    private void HandlePickupCollected(PickupItem pickup)
    {
        if (pickup == null || pickup.gameObject != currentPickup)
        {
            return;
        }

        ClearCurrentPickup();
    }

    private void SubscribeToCurrentPickup()
    {
        UnsubscribeFromCurrentPickup();

        if (currentPickup == null)
        {
            return;
        }

        subscribedPickup = currentPickup.GetComponent<PickupItem>();
        if (subscribedPickup != null)
        {
            subscribedPickup.Collected += HandlePickupCollected;
        }
    }

    private void UnsubscribeFromCurrentPickup()
    {
        if (subscribedPickup != null)
        {
            subscribedPickup.Collected -= HandlePickupCollected;
            subscribedPickup = null;
        }
    }

    private GameTimeRefreshManager ResolveManager()
    {
        if (GameTimeRefreshManager.Instance != null)
        {
            return GameTimeRefreshManager.Instance;
        }

        GameTimeRefreshManager manager = FindAnyObjectByType<GameTimeRefreshManager>();
        if (manager != null)
        {
            return manager;
        }

        return new GameObject("GameTimeRefreshManager").AddComponent<GameTimeRefreshManager>();
    }

    private void ClearLegacyPlayerPrefsCache()
    {
        if (string.IsNullOrWhiteSpace(spawnerId))
            return;

        PlayerPrefs.DeleteKey("TimedPickupSpawner.NextRespawnUtc." + spawnerId);
        PlayerPrefs.Save();
    }

    private void OnDisable()
    {
        GameTimeRefreshManager.Instance?.UnregisterSpawner(this);
        UnsubscribeFromCurrentPickup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(spawnerId))
        {
            spawnerId = Guid.NewGuid().ToString("N");
        }

        if (respawnSeconds < 60f)
        {
            respawnSeconds = 60f;
        }
    }
#endif
}
