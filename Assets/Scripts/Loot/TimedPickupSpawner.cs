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
    [SerializeField, Min(1f)] private float respawnSeconds = 60f;
    [SerializeField, Min(0.1f)] private float checkInterval = 1f;
    [SerializeField] private bool persistWithPlayerPrefs = true;
    [SerializeField] private string spawnerId;

    [Header("Events")]
    [SerializeField] private UnityEvent<GameObject> onSpawned = new();

    private const string PrefsPrefix = "TimedPickupSpawner.NextRespawnUtc.";

    private double nextRespawnUtcSeconds = -1d;
    private float nextCheckTime;
    private PickupItem subscribedPickup;

    public GameObject CurrentPickup => currentPickup;
    public UnityEvent<GameObject> OnSpawned => onSpawned;

    private string PrefsKey => PrefsPrefix + spawnerId;

    private void Awake()
    {
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        LoadNextRespawnTime();
        SubscribeToCurrentPickup();
    }

    private void OnEnable()
    {
        SubscribeToCurrentPickup();
    }

    private void Start()
    {
        TrySpawnIfReady();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.unscaledTime + checkInterval;

        if (currentPickup == null && nextRespawnUtcSeconds < 0d)
        {
            ScheduleNextRespawn();
        }

        TrySpawnIfReady();
    }

    public bool TrySpawnIfReady()
    {
        if (currentPickup != null)
        {
            return false;
        }

        if (!ShouldSpawnNow())
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

        nextRespawnUtcSeconds = -1d;
        ClearStoredRespawnTime();
        SubscribeToCurrentPickup();
        onSpawned?.Invoke(currentPickup);
        return currentPickup;
    }

    public void ClearCurrentPickup()
    {
        UnsubscribeFromCurrentPickup();
        currentPickup = null;
        ScheduleNextRespawn();
    }

    private bool ShouldSpawnNow()
    {
        if (nextRespawnUtcSeconds < 0d)
        {
            return spawnImmediatelyIfNeverSpawned;
        }

        return GetUtcNowSeconds() >= nextRespawnUtcSeconds;
    }

    private void ScheduleNextRespawn()
    {
        nextRespawnUtcSeconds = GetUtcNowSeconds() + Mathf.Max(1f, respawnSeconds);
        SaveNextRespawnTime();
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

    private void LoadNextRespawnTime()
    {
        if (!persistWithPlayerPrefs || string.IsNullOrWhiteSpace(spawnerId))
        {
            nextRespawnUtcSeconds = -1d;
            return;
        }

        string value = PlayerPrefs.GetString(PrefsKey, string.Empty);
        if (double.TryParse(value, out double storedValue))
        {
            nextRespawnUtcSeconds = storedValue;
        }
    }

    private void SaveNextRespawnTime()
    {
        if (!persistWithPlayerPrefs || string.IsNullOrWhiteSpace(spawnerId))
        {
            return;
        }

        PlayerPrefs.SetString(PrefsKey, nextRespawnUtcSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
    }

    private void ClearStoredRespawnTime()
    {
        if (!persistWithPlayerPrefs || string.IsNullOrWhiteSpace(spawnerId))
        {
            return;
        }

        PlayerPrefs.DeleteKey(PrefsKey);
        PlayerPrefs.Save();
    }

    private static double GetUtcNowSeconds()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private void OnDisable()
    {
        UnsubscribeFromCurrentPickup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(spawnerId))
        {
            spawnerId = Guid.NewGuid().ToString("N");
        }

        if (respawnSeconds < 1f)
        {
            respawnSeconds = 1f;
        }

        if (checkInterval < 0.1f)
        {
            checkInterval = 0.1f;
        }
    }
#endif
}
