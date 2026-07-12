using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class GameObjectUnityEvent : UnityEvent<GameObject>
{
}

public class ReactableObject : MonoBehaviour, IReactable, IReactableStateNotifier
{
    [Header("Display")]
    [SerializeField] private string optionName = "Interact";
    [SerializeField] private InteractionCategory category = InteractionCategory.Normal;
    [SerializeField] private Transform interactionPoint;

    [Header("State")]
    [SerializeField] private bool interactable = true;
    [SerializeField] private bool disableAfterInteraction;

    [Header("Events")]
    [SerializeField] private GameObjectUnityEvent onInteract = new();
    [SerializeField] private UnityEvent onSelected = new();
    [SerializeField] private UnityEvent onDeselected = new();

    public event Action<IReactable> StateChanged;

    public GameObjectUnityEvent OnInteractEvent => onInteract;

    public string OptionName => optionName;
    public InteractionCategory Category => category;
    public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;

    public bool CanInteract(GameObject interactor)
    {
        return interactable && isActiveAndEnabled && gameObject.activeInHierarchy;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        onInteract?.Invoke(interactor);

        if (disableAfterInteraction)
        {
            interactable = false;
            StateChanged?.Invoke(this);
        }
    }

    public void OnSelected()
    {
        onSelected?.Invoke();
    }

    public void OnDeselected()
    {
        onDeselected?.Invoke();
    }

    public void SetInteractable(bool value)
    {
        if (interactable == value)
        {
            return;
        }

        interactable = value;
        StateChanged?.Invoke(this);
    }

    public void SetOptionName(string value)
    {
        if (optionName == value)
        {
            return;
        }

        optionName = value;
        StateChanged?.Invoke(this);
    }

    public void SetCategory(InteractionCategory value)
    {
        if (category == value)
        {
            return;
        }

        category = value;
        StateChanged?.Invoke(this);
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
