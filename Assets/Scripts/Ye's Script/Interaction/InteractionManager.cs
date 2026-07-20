using System.Collections.Generic;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    private sealed class InteractionEntry
    {
        public IReactable Reactable;
        public InteractionCategory Category;
        public long EnterOrder;
    }

    [Header("References")]
    [SerializeField] private InteractionSensor sensor;
    [SerializeField] private InteractionRollBoxUI rollBoxUI;

    [Header("Direct Input Fallback")]
    [SerializeField] private bool pollKeyboardInput;
    [SerializeField] private bool pollMouseWheelInput = true;
    [SerializeField] private KeyCode primaryInteractKey = KeyCode.Return;
    [SerializeField] private bool enableAlternativeKey = true;
    [SerializeField] private KeyCode alternativeInteractKey = KeyCode.E;
    [SerializeField] private KeyCode previousSelectionKey = KeyCode.W;
    [SerializeField] private KeyCode alternatePreviousSelectionKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode nextSelectionKey = KeyCode.S;
    [SerializeField] private KeyCode alternateNextSelectionKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode pointerModeKey = KeyCode.LeftAlt;
    [SerializeField] private KeyCode alternatePointerModeKey = KeyCode.RightAlt;
    [SerializeField] private bool invertMouseWheel;

    [Header("Behaviour")]
    [SerializeField] private bool selectNeighbourAfterRemoval = true;
    [SerializeField, Min(0.1f)] private float cleanupInterval = 0.5f;

    private readonly List<InteractionEntry> entries = new();
    private readonly Dictionary<IReactable, InteractionEntry> entryMap = new();
    private readonly List<InteractionDisplayData> displayData = new();

    private long nextEnterOrder;
    private IReactable selectedReactable;
    private int selectedIndex = -1;
    private float nextCleanupTime;
    private int lastExecuteFrame = -1;
    private bool pointerModeWasActive;

    public int Count => entries.Count;
    public IReactable CurrentSelection => selectedReactable;

    private void Reset()
    {
        sensor = GetComponentInChildren<InteractionSensor>();
    }

    private void Awake()
    {
        if (sensor == null)
        {
            sensor = GetComponentInChildren<InteractionSensor>();
        }
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener<GameModeChangedInfo>(
            E_EventType.E_GameMode_Changed,
            OnGameModeChanged);

        if (sensor != null)
        {
            sensor.ReactableEntered += HandleReactableEntered;
            sensor.ReactableExited += HandleReactableExited;
        }

        if (rollBoxUI != null)
        {
            rollBoxUI.OptionClicked += HandleOptionClicked;
            rollBoxUI.OptionHovered += HandleOptionHovered;
            rollBoxUI.OptionExited += HandleOptionExited;
            rollBoxUI.OptionSliderSelected += HandleOptionSliderSelected;
        }
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<GameModeChangedInfo>(
            E_EventType.E_GameMode_Changed,
            OnGameModeChanged);

        if (sensor != null)
        {
            sensor.ReactableEntered -= HandleReactableEntered;
            sensor.ReactableExited -= HandleReactableExited;
        }

        if (rollBoxUI != null)
        {
            rollBoxUI.OptionClicked -= HandleOptionClicked;
            rollBoxUI.OptionHovered -= HandleOptionHovered;
            rollBoxUI.OptionExited -= HandleOptionExited;
            rollBoxUI.OptionSliderSelected -= HandleOptionSliderSelected;
        }

        UnsubscribeAllReactables();
    }

    private void Start()
    {
        RefreshWholeUI();
    }

    private void Update()
    {
        if (GameModeManager.Instance.CurrentCapabilities.canInteract)
        {
            RefreshPointerModeState();
            HandleDirectInput();
        }
        else
        {
            HideInteractionUI();
        }

        if (Time.unscaledTime >= nextCleanupTime)
        {
            nextCleanupTime = Time.unscaledTime + cleanupInterval;
            sensor?.CleanupInvalidEntries();
            RemoveInvalidEntries();
        }
    }

    public void SubmitInteractInput()
    {
        if (!GameModeManager.Instance.CurrentCapabilities.canInteract || IsPointerModeActive())
        {
            return;
        }

        ExecuteSelected();
    }

    public void SubmitNextSelectionInput()
    {
        if (!GameModeManager.Instance.CurrentCapabilities.canInteract || IsPointerModeActive())
        {
            return;
        }

        MoveSelection(1);
    }

    public void SubmitPreviousSelectionInput()
    {
        if (!GameModeManager.Instance.CurrentCapabilities.canInteract || IsPointerModeActive())
        {
            return;
        }

        MoveSelection(-1);
    }

    private void HandleDirectInput()
    {
        if (entries.Count == 0)
        {
            return;
        }

        if (IsPointerModeActive())
        {
            return;
        }

        if (pollMouseWheelInput)
        {
            float scrollDelta = Input.mouseScrollDelta.y;
            if (scrollDelta > 0.01f)
            {
                MoveSelection(invertMouseWheel ? 1 : -1);
            }
            else if (scrollDelta < -0.01f)
            {
                MoveSelection(invertMouseWheel ? -1 : 1);
            }
        }

        if (Input.GetKeyDown(previousSelectionKey) ||
            Input.GetKeyDown(alternatePreviousSelectionKey))
        {
            MoveSelection(-1);
        }
        else if (Input.GetKeyDown(nextSelectionKey) ||
                 Input.GetKeyDown(alternateNextSelectionKey))
        {
            MoveSelection(1);
        }

        if (!pollKeyboardInput)
        {
            return;
        }

        bool interactPressed =
            Input.GetKeyDown(primaryInteractKey) ||
            (enableAlternativeKey && Input.GetKeyDown(alternativeInteractKey));

        if (interactPressed)
        {
            ExecuteSelected();
        }
    }

    private void HandleReactableEntered(IReactable reactable)
    {
        AddReactable(reactable);
    }

    private void HandleReactableExited(IReactable reactable)
    {
        RemoveReactable(reactable);
    }

    private void AddReactable(IReactable reactable)
    {
        if (!GameModeManager.Instance.CurrentCapabilities.canInteract)
        {
            return;
        }

        if (!IsValidReactable(reactable) || entryMap.ContainsKey(reactable))
        {
            return;
        }

        if (!reactable.CanInteract(gameObject))
        {
            return;
        }

        InteractionEntry entry = new()
        {
            Reactable = reactable,
            Category = reactable.Category,
            EnterOrder = nextEnterOrder++
        };

        entryMap.Add(reactable, entry);
        entries.Add(entry);
        SubscribeToReactable(reactable);
        SortStable();

        if (!IsValidReactable(selectedReactable))
        {
            SelectReactable(entries[0].Reactable);
        }
        else
        {
            RestoreSelectedIndex();
        }

        RefreshWholeUI();
    }

    private void RemoveReactable(IReactable reactable)
    {
        if (reactable == null || !entryMap.TryGetValue(reactable, out InteractionEntry entry))
        {
            return;
        }

        int removedIndex = entries.IndexOf(entry);
        bool removedCurrent = ReferenceEquals(selectedReactable, reactable);

        UnsubscribeFromReactable(reactable);
        entryMap.Remove(reactable);
        entries.Remove(entry);

        if (removedCurrent)
        {
            reactable.OnDeselected();
            selectedReactable = null;
            selectedIndex = -1;

            if (entries.Count > 0)
            {
                int nextIndex = selectNeighbourAfterRemoval
                    ? Mathf.Clamp(removedIndex, 0, entries.Count - 1)
                    : 0;
                SelectReactable(entries[nextIndex].Reactable);
            }
        }
        else
        {
            RestoreSelectedIndex();
        }

        RefreshWholeUI();
    }

    private void HandleReactableStateChanged(IReactable reactable)
    {
        if (!entryMap.TryGetValue(reactable, out InteractionEntry entry))
        {
            return;
        }

        if (!IsValidReactable(reactable) || !reactable.CanInteract(gameObject))
        {
            RemoveReactable(reactable);
            return;
        }

        if (entry.Category != reactable.Category)
        {
            entry.Category = reactable.Category;
            SortStable();
            RestoreSelectedIndex();
        }

        RefreshWholeUI();
    }

    private void MoveSelection(int direction)
    {
        if (entries.Count == 0)
        {
            return;
        }

        int startIndex = selectedIndex >= 0 ? selectedIndex : 0;
        int nextIndex = (startIndex + direction) % entries.Count;
        if (nextIndex < 0)
        {
            nextIndex += entries.Count;
        }

        SelectReactable(entries[nextIndex].Reactable);
        rollBoxUI?.SetSelected(selectedIndex, true);
    }

    private void ExecuteSelected()
    {
        if (!GameModeManager.Instance.CurrentCapabilities.canInteract)
        {
            return;
        }

        if (lastExecuteFrame == Time.frameCount)
        {
            return;
        }

        lastExecuteFrame = Time.frameCount;

        if (!IsValidReactable(selectedReactable))
        {
            RemoveInvalidEntries();
            return;
        }

        if (!selectedReactable.CanInteract(gameObject))
        {
            RemoveReactable(selectedReactable);
            return;
        }

        selectedReactable.Interact(gameObject);
    }

    private void HandleOptionHovered(int index)
    {
        SelectByIndex(index, false);
    }

    private void HandleOptionExited(int index)
    {
        if (IsPointerModeActive() && index == selectedIndex)
        {
            rollBoxUI?.SetSelected(-1, false);
        }
    }

    private void HandleOptionClicked(int index)
    {
        SelectByIndex(index, false);
        ExecuteSelected();
    }

    private void HandleOptionSliderSelected(int index)
    {
        SelectByIndex(index, true);
    }

    private void SelectByIndex(int index, bool scrollIntoView)
    {
        if (index < 0 || index >= entries.Count)
        {
            return;
        }

        SelectReactable(entries[index].Reactable);
        rollBoxUI?.SetSelected(selectedIndex, scrollIntoView);
    }

    private void SelectReactable(IReactable reactable)
    {
        if (ReferenceEquals(selectedReactable, reactable))
        {
            RestoreSelectedIndex();
            return;
        }

        if (IsValidReactable(selectedReactable))
        {
            selectedReactable.OnDeselected();
        }

        selectedReactable = reactable;
        RestoreSelectedIndex();

        if (IsValidReactable(selectedReactable))
        {
            selectedReactable.OnSelected();
        }
    }

    private void RestoreSelectedIndex()
    {
        selectedIndex = -1;
        if (!IsValidReactable(selectedReactable))
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            if (ReferenceEquals(entries[i].Reactable, selectedReactable))
            {
                selectedIndex = i;
                return;
            }
        }
    }

    private void RefreshWholeUI()
    {
        if (rollBoxUI == null)
        {
            return;
        }

        if (!GameModeManager.Instance.CurrentCapabilities.showInteractionUI)
        {
            HideInteractionUI();
            return;
        }

        if (entries.Count > 0 && !IsValidReactable(selectedReactable))
        {
            SelectReactable(entries[0].Reactable);
        }

        displayData.Clear();
        foreach (InteractionEntry entry in entries)
        {
            displayData.Add(new InteractionDisplayData(
                entry.Reactable.OptionName,
                entry.Reactable.Category));
        }

        rollBoxUI.SetOptions(displayData, selectedIndex);
    }

    private void OnGameModeChanged(GameModeChangedInfo info)
    {
        if (info == null)
        {
            return;
        }

        if (!info.newCapabilities.canInteract ||
            !info.newCapabilities.showInteractionUI)
        {
            HideInteractionUI();
            return;
        }

        RemoveInvalidEntries();
        RefreshWholeUI();
    }

    private void HideInteractionUI()
    {
        displayData.Clear();
        rollBoxUI?.SetOptions(displayData, -1);
    }

    private void SortStable()
    {
        entries.Sort((left, right) =>
        {
            int categoryCompare = left.Category.CompareTo(right.Category);
            if (categoryCompare != 0)
            {
                return categoryCompare;
            }

            return left.EnterOrder.CompareTo(right.EnterOrder);
        });
    }

    private void RemoveInvalidEntries()
    {
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            IReactable reactable = entries[i].Reactable;
            if (!IsValidReactable(reactable) || !reactable.CanInteract(gameObject))
            {
                RemoveReactable(reactable);
            }
        }
    }

    private void SubscribeToReactable(IReactable reactable)
    {
        if (reactable is IReactableStateNotifier notifier)
        {
            notifier.StateChanged += HandleReactableStateChanged;
        }
    }

    private void UnsubscribeFromReactable(IReactable reactable)
    {
        if (reactable is IReactableStateNotifier notifier)
        {
            notifier.StateChanged -= HandleReactableStateChanged;
        }
    }

    private void UnsubscribeAllReactables()
    {
        foreach (InteractionEntry entry in entries)
        {
            UnsubscribeFromReactable(entry.Reactable);
        }
    }

    private static bool IsValidReactable(IReactable reactable)
    {
        if (reactable == null)
        {
            return false;
        }

        return reactable is not UnityEngine.Object unityObject || unityObject != null;
    }

    private bool IsPointerModeActive()
    {
        return Input.GetKey(pointerModeKey) || Input.GetKey(alternatePointerModeKey);
    }

    private void RefreshPointerModeState()
    {
        bool pointerModeActive = IsPointerModeActive();
        if (pointerModeActive == pointerModeWasActive)
        {
            return;
        }

        pointerModeWasActive = pointerModeActive;
        if (pointerModeActive)
        {
            rollBoxUI?.SetSelected(-1, false);
        }
        else
        {
            if (entries.Count > 0 && !IsValidReactable(selectedReactable))
            {
                SelectReactable(entries[0].Reactable);
            }

            rollBoxUI?.SetSelected(selectedIndex, false);
        }
    }
}
