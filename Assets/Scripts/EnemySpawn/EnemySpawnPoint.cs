using UnityEngine;

public enum EnemyRefreshMode
{
    RefreshAfterTeleport = 0,
    QuestCondition = 1
}

[DisallowMultipleComponent]
public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string spawnPointId;

    [Header("Refresh")]
    [SerializeField] private EnemyRefreshMode refreshMode = EnemyRefreshMode.RefreshAfterTeleport;
    [SerializeField] private string questConditionId;

    [Header("Spawn")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnTransform;
    [SerializeField] private Transform spawnedParent;
    [SerializeField] private GameObject currentEnemy;

    [Header("Debug")]
    [SerializeField] private bool debugLog;

    public string SpawnPointId => spawnPointId;
    public EnemyRefreshMode RefreshMode => refreshMode;
    public string QuestConditionId => questConditionId;

    private void Awake()
    {
        if (spawnTransform == null)
        {
            spawnTransform = transform;
        }
    }

    private void OnEnable()
    {
        ResolveManager().RegisterSpawnPoint(this);
    }

    private void Start()
    {
        ResolveManager().RegisterSpawnPoint(this);
    }

    private void OnDisable()
    {
        EnemySpawnManager.Instance?.UnregisterSpawnPoint(this);
    }

    public void Refresh()
    {
        ClearCurrentEnemy();
        Spawn();
    }

    public GameObject Spawn()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning($"[EnemySpawnPoint] Enemy prefab is missing. Id={spawnPointId}", this);
            return null;
        }

        Transform origin = spawnTransform != null ? spawnTransform : transform;
        currentEnemy = Instantiate(enemyPrefab, origin.position, origin.rotation, spawnedParent);

        if (debugLog)
        {
            Debug.Log($"[EnemySpawnPoint] Enemy spawned. Id={spawnPointId}, Enemy={currentEnemy.name}", this);
        }

        return currentEnemy;
    }

    public void ClearCurrentEnemy()
    {
        if (currentEnemy == null)
            return;

        Destroy(currentEnemy);
        currentEnemy = null;
    }

    private EnemySpawnManager ResolveManager()
    {
        if (EnemySpawnManager.Instance != null)
        {
            return EnemySpawnManager.Instance;
        }

        EnemySpawnManager manager = FindAnyObjectByType<EnemySpawnManager>();
        if (manager != null)
        {
            return manager;
        }

        return new GameObject("EnemySpawnManager").AddComponent<EnemySpawnManager>();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(spawnPointId))
        {
            spawnPointId = gameObject.name;
        }

        if (spawnTransform == null)
        {
            spawnTransform = transform;
        }
    }
#endif
}
