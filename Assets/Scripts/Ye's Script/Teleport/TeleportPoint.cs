using System;
using UnityEngine;

[DisallowMultipleComponent]
public class TeleportPoint : MonoBehaviour, IReactable, IReactableStateNotifier
{
    [Header("Identity")]
    [SerializeField] private string pointId;
    [SerializeField] private string displayName = "Teleport Point";
    [SerializeField] private InteractionCategory category = InteractionCategory.Normal;

    [Header("Spawn")]
    [SerializeField] private Transform spawnTransform;
    [SerializeField] private bool isDefaultRespawnPoint;

    [Header("Registration")]
    [SerializeField] private bool setRespawnOnRegister = true;
    [SerializeField] private bool setRespawnOnTeleportArrival = true;

    [Header("Registered Interaction")]
    [SerializeField] private bool healWhenRegistered = true;
    [SerializeField] private bool fullHeal = true;
    [SerializeField] private int healAmount = 100;
    [SerializeField] private bool restoreInkWhenRegistered;
    [SerializeField] private bool fullRestoreInk;
    [SerializeField] private int restoreInkAmount = 20;

    [Header("References")]
    [SerializeField] private TeleportPointRegistry registry;
    [SerializeField] private PlayerTeleportService teleportService;
    [SerializeField] private PlayerResourceController resourceController;

    public event Action<IReactable> StateChanged;

    public string PointId => pointId;
    public string DisplayName => displayName;
    public bool IsDefaultRespawnPoint => isDefaultRespawnPoint;
    public Transform SpawnTransform => spawnTransform != null ? spawnTransform : transform;

    public string OptionName
    {
        get
        {
            bool registered = IsRegistered();
            return registered ? $"Rest: {displayName}" : $"Register: {displayName}";
        }
    }

    public InteractionCategory Category => category;
    public Transform InteractionPoint => transform;

    private void Reset()
    {
        spawnTransform = transform;
        if (string.IsNullOrWhiteSpace(pointId))
        {
            pointId = gameObject.name;
        }
    }

    private void Awake()
    {
        if (spawnTransform == null)
        {
            spawnTransform = transform;
        }

        if (string.IsNullOrWhiteSpace(pointId))
        {
            pointId = gameObject.name;
        }

        ResolveReferences();
        registry?.RegisterScenePoint(this);
    }

    private void Start()
    {
        ResolveReferences();

        if (isDefaultRespawnPoint && registry != null)
        {
            registry.RegisterPoint(this, true);
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        return isActiveAndEnabled && gameObject.activeInHierarchy;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
            return;

        ResolveReferences();

        bool wasRegistered = IsRegistered();

        if (!wasRegistered)
        {
            registry?.RegisterPoint(this, setRespawnOnRegister);
            StateChanged?.Invoke(this);
            return;
        }

        ApplyRegisteredInteractionReward();
    }

    public bool TeleportPlayerHere(bool setAsRespawnPoint = true)
    {
        ResolveReferences();

        if (teleportService == null)
            return false;

        teleportService.TeleportTo(SpawnTransform);

        if (setAsRespawnPoint && setRespawnOnTeleportArrival)
        {
            registry?.SetRespawnPoint(this);
        }

        return true;
    }

    public bool TeleportPlayerTo(TeleportPoint destination)
    {
        if (destination == null || !IsRegistered(destination))
            return false;

        return destination.TeleportPlayerHere(true);
    }

    public bool TeleportPlayerToNextRegisteredPoint()
    {
        ResolveReferences();

        if (registry == null)
            return false;

        if (!registry.TryGetNextRegisteredPoint(this, out TeleportPoint nextPoint))
            return false;

        return TeleportPlayerTo(nextPoint);
    }

    public void OnSelected()
    {
    }

    public void OnDeselected()
    {
    }

    public void RefreshInteractionState()
    {
        StateChanged?.Invoke(this);
    }

    private void ApplyRegisteredInteractionReward()
    {
        if (resourceController == null)
        {
            ResolveReferences();
        }

        if (resourceController == null)
            return;

        if (healWhenRegistered)
        {
            if (fullHeal)
            {
                resourceController.FullHeal();
            }
            else
            {
                resourceController.HealHp(healAmount);
            }
        }

        if (restoreInkWhenRegistered)
        {
            if (fullRestoreInk)
            {
                resourceController.FullRestoreInk();
            }
            else
            {
                resourceController.RestoreInk(restoreInkAmount);
            }
        }
    }

    private bool IsRegistered()
    {
        ResolveReferences();
        return registry != null && registry.IsRegistered(this);
    }

    private bool IsRegistered(TeleportPoint point)
    {
        ResolveReferences();
        return registry != null && registry.IsRegistered(point);
    }

    private void ResolveReferences()
    {
        if (registry == null)
        {
            registry = TeleportPointRegistry.Instance;
        }

        if (registry == null)
        {
            registry = FindAnyObjectByType<TeleportPointRegistry>();
        }

        if (teleportService == null)
        {
            teleportService = PlayerTeleportService.Instance;
        }

        if (teleportService == null)
        {
            teleportService = FindAnyObjectByType<PlayerTeleportService>();
        }

        if (resourceController == null)
        {
            resourceController = PlayerResourceController.Instance;
        }

        if (resourceController == null)
        {
            resourceController = FindAnyObjectByType<PlayerResourceController>();
        }
    }
}
