using PixelCrushers.DialogueSystem;
using System;
using DialogueSystemTriggerWrapper = PixelCrushers.DialogueSystem.Wrappers.DialogueSystemTrigger;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class DialogueSystemReactable : MonoBehaviour, IReactable, IReactableStateNotifier
{
    [Header("Display")]
    [SerializeField] private string optionName = "Talk";
    [SerializeField] private InteractionCategory category = InteractionCategory.Normal;
    [SerializeField] private Transform interactionPoint;

    [Header("Dialogue System")]
    [SerializeField] private DialogueSystemTriggerWrapper dialogueTrigger;
    [SerializeField] private string conversationTitle;
    [SerializeField] private bool interactable = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onSelected = new();
    [SerializeField] private UnityEvent onDeselected = new();

    public string OptionName => optionName;
    public InteractionCategory Category => category;
    public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;

    public event Action<IReactable> StateChanged;

    private void Reset()
    {
        dialogueTrigger = GetComponent<DialogueSystemTriggerWrapper>();
    }

    private void Awake()
    {
        if (dialogueTrigger == null)
        {
            dialogueTrigger = GetComponent<DialogueSystemTriggerWrapper>();
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        return interactable &&
               isActiveAndEnabled &&
               gameObject.activeInHierarchy &&
               (dialogueTrigger != null || !string.IsNullOrWhiteSpace(conversationTitle));
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        Transform actor = interactor != null ? interactor.transform : null;
        if (dialogueTrigger != null)
        {
            dialogueTrigger.TryStart(actor, actor);
            return;
        }

        DialogueManager.StartConversation(conversationTitle, actor, transform);
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

    public void ConfigureConversation(string title)
    {
        conversationTitle = title;
    }
}
