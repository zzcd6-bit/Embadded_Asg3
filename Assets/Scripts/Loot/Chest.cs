using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Chest : MonoBehaviour, IReactable, IReactableStateNotifier, IGameSaveModule
{
    [Header("Identity")]
    [SerializeField] private string chestId;

    [Header("Interaction")]
    [SerializeField] private string openOptionName = "Open Chest";
    [SerializeField] private string lockedOptionName = "Locked Chest";
    [SerializeField] private InteractionCategory category = InteractionCategory.Normal;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private bool showInteractionWhenLocked = true;

    [Header("State")]
    [SerializeField] private bool canOpen = true;
    [SerializeField] private bool isOpen;

    [Header("Reward")]
    [SerializeField] private DropReward dropReward;
    [SerializeField] private Transform dropOrigin;

    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private string openedBool = "IsOpen";

    [Header("Events")]
    [SerializeField] private UnityEvent onUnlocked = new();
    [SerializeField] private UnityEvent onOpened = new();
    [SerializeField] private UnityEvent onOpenFailed = new();

    public event Action<IReactable> StateChanged;

    public bool CanOpen => canOpen;
    public bool IsOpen => isOpen;
    public string ChestId => chestId;
    public UnityEvent OnUnlocked => onUnlocked;
    public UnityEvent OnOpened => onOpened;
    public UnityEvent OnOpenFailed => onOpenFailed;

    public string OptionName => canOpen ? openOptionName : lockedOptionName;
    public InteractionCategory Category => category;
    public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;

    private void Reset()
    {
        dropReward = GetComponent<DropReward>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        EnsureId();
        ResolveReferences();
        ApplyAnimatorState();
    }

    public bool TryUnlock()
    {
        if (isOpen || canOpen)
        {
            return false;
        }

        canOpen = true;
        onUnlocked?.Invoke();
        StateChanged?.Invoke(this);
        return true;
    }

    public bool TryOpen()
    {
        return TryOpen(null);
    }

    public bool TryOpen(GameObject interactor)
    {
        if (isOpen || !canOpen)
        {
            onOpenFailed?.Invoke();
            StateChanged?.Invoke(this);
            return false;
        }

        isOpen = true;
        WorldStateSaveController.MarkChestOpened(chestId);
        ApplyAnimatorState();
        SpawnReward();
        onOpened?.Invoke();
        StateChanged?.Invoke(this);
        return true;
    }

    public bool CanInteract(GameObject interactor)
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy || isOpen)
        {
            return false;
        }

        return canOpen || showInteractionWhenLocked;
    }

    public void Interact(GameObject interactor)
    {
        TryOpen(interactor);
    }

    public void OnSelected()
    {
    }

    public void OnDeselected()
    {
    }

    private void SpawnReward()
    {
        ResolveReferences();
        if (dropReward == null)
        {
            return;
        }

        Vector3 origin = dropOrigin != null ? dropOrigin.position : transform.position;
        dropReward.DropAt(origin);
    }

    private void ApplyAnimatorState()
    {
        if (animator == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(openedBool))
        {
            animator.SetBool(openedBool, isOpen);
        }

        if (isOpen && !string.IsNullOrEmpty(openTrigger))
        {
            animator.SetTrigger(openTrigger);
        }
    }

    private void ResolveReferences()
    {
        if (dropReward == null)
        {
            dropReward = GetComponent<DropReward>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
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
        if (saveData == null || !isOpen || string.IsNullOrWhiteSpace(chestId))
            return;

        if (saveData.worldState == null)
        {
            saveData.worldState = new WorldStateSaveData();
        }

        if (!saveData.worldState.openedChestIds.Contains(chestId))
        {
            saveData.worldState.openedChestIds.Add(chestId);
        }
    }

    public void RestoreGameSaveData(GameSaveData saveData)
    {
        if (saveData == null || saveData.worldState == null || string.IsNullOrWhiteSpace(chestId))
            return;

        bool shouldBeOpen = saveData.worldState.openedChestIds.Contains(chestId);
        if (!shouldBeOpen)
            return;

        isOpen = true;
        ApplyAnimatorState();
        StateChanged?.Invoke(this);
    }

    private void EnsureId()
    {
        if (string.IsNullOrWhiteSpace(chestId))
        {
            chestId = gameObject.scene.name + "/" + gameObject.name;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureId();
    }
#endif
}
