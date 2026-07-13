using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PickupItem : MonoBehaviour, IReactable, IReactableStateNotifier, IGameSaveModule
{
    [Header("Identity")]
    [SerializeField] private string pickupId;
    [SerializeField] private bool persistCollectedState = true;

    [Header("Interaction")]
    [SerializeField] private string optionName = "Pick Up";
    [SerializeField] private InteractionCategory category = InteractionCategory.Pickup;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private bool collectable = true;

    [Header("Behaviour")]
    [SerializeField] private bool destroyOnCollect = true;

    [Header("Events")]
    [SerializeField] private UnityEvent<GameObject> onCollected = new();

    public event Action<IReactable> StateChanged;
    public event Action<PickupItem> Collected;

    public bool IsCollected { get; private set; }
    public string PickupId => pickupId;
    public UnityEvent<GameObject> OnCollected => onCollected;

    public string OptionName => optionName;
    public InteractionCategory Category => category;
    public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;

    public bool CanInteract(GameObject interactor)
    {
        return collectable && !IsCollected && isActiveAndEnabled && gameObject.activeInHierarchy;
    }

    private void Awake()
    {
        EnsureId();
    }

    public void Interact(GameObject interactor)
    {
        TryCollect(interactor);
    }

    public bool TryCollect(GameObject collector)
    {
        if (!CanInteract(collector))
        {
            return false;
        }

        IsCollected = true;
        if (persistCollectedState)
        {
            WorldStateSaveController.MarkPickupCollected(pickupId);
        }

        onCollected?.Invoke(collector);
        Collected?.Invoke(this);
        StateChanged?.Invoke(this);

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }

        return true;
    }

    public void SetCollectable(bool value)
    {
        if (collectable == value)
        {
            return;
        }

        collectable = value;
        StateChanged?.Invoke(this);
    }

    public void OnSelected()
    {
    }

    public void OnDeselected()
    {
    }

    private void OnDisable()
    {
        StateChanged?.Invoke(this);
    }

    private void OnDestroy()
    {
        StateChanged?.Invoke(this);
    }

    public void CaptureGameSaveData(GameSaveData saveData)
    {
        if (!persistCollectedState || saveData == null || !IsCollected || string.IsNullOrWhiteSpace(pickupId))
            return;

        if (saveData.worldState == null)
        {
            saveData.worldState = new WorldStateSaveData();
        }

        if (!saveData.worldState.collectedPickupIds.Contains(pickupId))
        {
            saveData.worldState.collectedPickupIds.Add(pickupId);
        }
    }

    public void RestoreGameSaveData(GameSaveData saveData)
    {
        if (!persistCollectedState || saveData == null || saveData.worldState == null || string.IsNullOrWhiteSpace(pickupId))
            return;

        if (!saveData.worldState.collectedPickupIds.Contains(pickupId))
            return;

        IsCollected = true;
        gameObject.SetActive(false);
        StateChanged?.Invoke(this);
    }

    private void EnsureId()
    {
        if (string.IsNullOrWhiteSpace(pickupId))
        {
            pickupId = gameObject.scene.name + "/" + gameObject.name;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureId();
    }
#endif
}
