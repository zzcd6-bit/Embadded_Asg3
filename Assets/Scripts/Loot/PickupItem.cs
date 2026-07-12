using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PickupItem : MonoBehaviour, IReactable, IReactableStateNotifier
{
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
    public UnityEvent<GameObject> OnCollected => onCollected;

    public string OptionName => optionName;
    public InteractionCategory Category => category;
    public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;

    public bool CanInteract(GameObject interactor)
    {
        return collectable && !IsCollected && isActiveAndEnabled && gameObject.activeInHierarchy;
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
}
