using UnityEngine;

public class BuildingGridObstacle : MonoBehaviour
{
    [SerializeField] private GridPlacementSystem gridPlacementSystem;
    [SerializeField] private Collider obstacleCollider;
    [SerializeField] private bool clearOnDestroy = true;

    private Bounds registeredBounds;
    private bool isRegistered;

    private void Awake()
    {
        if (gridPlacementSystem == null)
        {
            gridPlacementSystem = FindAnyObjectByType<GridPlacementSystem>();
        }

        if (obstacleCollider == null)
        {
            obstacleCollider = GetComponentInChildren<Collider>();
        }
    }

    private void OnEnable()
    {
        Register();
    }

    private void OnDisable()
    {
        if (clearOnDestroy)
        {
            Unregister();
        }
    }

    public void Register()
    {
        if (gridPlacementSystem == null || obstacleCollider == null)
        {
            return;
        }

        registeredBounds = obstacleCollider.bounds;
        gridPlacementSystem.SetBlockedWorldBounds(registeredBounds, true);
        isRegistered = true;
    }

    public void Unregister()
    {
        if (!isRegistered || gridPlacementSystem == null)
        {
            return;
        }

        gridPlacementSystem.SetBlockedWorldBounds(registeredBounds, false);
        isRegistered = false;
    }
}
