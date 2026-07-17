using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueSceneStateController : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialogueUiObject;
    [SerializeField] private string dialogueUiObjectName = "Dialogue UI Canvas";

    [Header("Interaction")]
    [SerializeField] private bool disableDialogueReactablesDuringConversation = true;

    [Header("Cursor")]
    [SerializeField] private bool manageCursor = true;
    [SerializeField] private bool lockCursorWhenNotInDialogue = true;
    [SerializeField] private bool enforceNormalCursorEveryFrame;

    [Header("Dialogue Selection")]
    [SerializeField] private KeyCode submitResponseKey = KeyCode.E;
    [SerializeField] private KeyCode previousResponseKey = KeyCode.W;
    [SerializeField] private KeyCode nextResponseKey = KeyCode.S;
    [SerializeField] private KeyCode pointerModeKey = KeyCode.LeftAlt;
    [SerializeField] private KeyCode alternatePointerModeKey = KeyCode.RightAlt;
    [SerializeField] private float scrollThreshold = 0.1f;
    [SerializeField] private bool focusFirstSelectable = true;

    [Header("Sentence Skip")]
    [SerializeField] private string canSkipSentenceFieldName = "canSkipSentence";
    [SerializeField] private bool defaultCanSkipSentence = true;
    [SerializeField] private KeyCode skipSentenceKey = KeyCode.E;
    [SerializeField] private KeyCode alternateSkipSentenceKey = KeyCode.Space;
    [SerializeField] private int skipSentenceMouseButton = 0;

    private readonly List<DialogueSystemReactable> disabledReactables = new();
    private readonly List<Selectable> dialogueSelectables = new();
    private PlayerModeStateController playerModeStateController;
    private bool subscribed;
    private bool dialoguePointerMode;

    private void OnEnable()
    {
        Subscribe();
        ResolvePlayerModeStateController();
        BindSceneDialogueUi();
        ApplyNormalCursorState();
    }

    private void Start()
    {
        Subscribe();
        ResolvePlayerModeStateController();
        BindSceneDialogueUi();
        if (DialogueManager.isConversationActive)
        {
            HandleConversationStarted(null);
        }
        else
        {
            ApplyNormalCursorState();
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
        RestoreDialogueReactables();
        if (playerModeStateController != null)
        {
            playerModeStateController.ExitDialogueState();
        }
    }

    private void Update()
    {
        if (DialogueManager.isConversationActive)
        {
            HandleDialogueInput();
            return;
        }

        if (!manageCursor || !enforceNormalCursorEveryFrame || DialogueManager.isConversationActive)
        {
            return;
        }

        if (lockCursorWhenNotInDialogue &&
            (Cursor.lockState != CursorLockMode.Locked || Cursor.visible))
        {
            ApplyNormalCursorState();
        }
    }

    private void Subscribe()
    {
        if (subscribed || DialogueManager.instance == null)
        {
            return;
        }

        DialogueManager.instance.conversationStarted += HandleConversationStarted;
        DialogueManager.instance.conversationEnded += HandleConversationEnded;
        subscribed = true;
    }

    private void BindSceneDialogueUi()
    {
        if (DialogueManager.instance == null)
        {
            return;
        }

        if (dialogueUiObject == null && !string.IsNullOrWhiteSpace(dialogueUiObjectName))
        {
            dialogueUiObject = GameObject.Find(dialogueUiObjectName);
        }

        if (dialogueUiObject == null)
        {
            Debug.LogWarning("[DialogueSceneStateController] Dialogue UI object is missing.", this);
            return;
        }

        if (dialogueUiObject.GetComponentInChildren(typeof(IDialogueUI), true) == null)
        {
            Debug.LogWarning(
                "[DialogueSceneStateController] Dialogue UI object does not contain an IDialogueUI component.",
                dialogueUiObject);
            return;
        }

        if (!dialogueUiObject.activeSelf)
        {
            dialogueUiObject.SetActive(true);
        }

        DialogueManager.instance.UseDialogueUI(dialogueUiObject);
    }

    private void Unsubscribe()
    {
        if (!subscribed || DialogueManager.instance == null)
        {
            subscribed = false;
            return;
        }

        DialogueManager.instance.conversationStarted -= HandleConversationStarted;
        DialogueManager.instance.conversationEnded -= HandleConversationEnded;
        subscribed = false;
    }

    private void HandleConversationStarted(Transform actor)
    {
        ResolvePlayerModeStateController();
        if (playerModeStateController != null)
        {
            playerModeStateController.EnterDialogueState();
        }

        DisableDialogueReactables();
        dialoguePointerMode = false;
        ApplyDialogueCursorState(false);
    }

    private void HandleConversationEnded(Transform actor)
    {
        RestoreDialogueReactables();
        if (playerModeStateController != null)
        {
            playerModeStateController.ExitDialogueState();
        }

        dialoguePointerMode = false;
        ApplyNormalCursorState();
    }

    private void ResolvePlayerModeStateController()
    {
        if (playerModeStateController != null)
        {
            return;
        }

        playerModeStateController = GetComponent<PlayerModeStateController>();
        if (playerModeStateController == null)
        {
            playerModeStateController = FindAnyObjectByType<PlayerModeStateController>(FindObjectsInactive.Include);
        }
    }

    private void DisableDialogueReactables()
    {
        if (!disableDialogueReactablesDuringConversation)
        {
            return;
        }

        disabledReactables.Clear();
        DialogueSystemReactable[] reactables =
            FindObjectsByType<DialogueSystemReactable>(
                FindObjectsInactive.Include);

        foreach (DialogueSystemReactable reactable in reactables)
        {
            if (reactable == null || !reactable.CanInteract(null))
            {
                continue;
            }

            disabledReactables.Add(reactable);
            reactable.SetInteractable(false);
        }
    }

    private void RestoreDialogueReactables()
    {
        for (int i = 0; i < disabledReactables.Count; i++)
        {
            if (disabledReactables[i] != null)
            {
                disabledReactables[i].SetInteractable(true);
            }
        }

        disabledReactables.Clear();
    }

    private void HandleDialogueInput()
    {
        bool pointerMode =
            Input.GetKey(pointerModeKey) ||
            Input.GetKey(alternatePointerModeKey);

        if (pointerMode != dialoguePointerMode)
        {
            dialoguePointerMode = pointerMode;
            ApplyDialogueCursorState(dialoguePointerMode);
        }

        if (focusFirstSelectable)
        {
            EnsureDialogueSelection();
        }

        if (IsSentenceSkipInputDown() && TrySkipCurrentSentence())
        {
            return;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > scrollThreshold || Input.GetKeyDown(previousResponseKey))
        {
            MoveDialogueSelection(-1);
        }
        else if (scroll < -scrollThreshold || Input.GetKeyDown(nextResponseKey))
        {
            MoveDialogueSelection(1);
        }

        if (Input.GetKeyDown(submitResponseKey))
        {
            SubmitDialogueSelection();
        }
    }

    private void EnsureDialogueSelection()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || dialogueUiObject == null)
        {
            return;
        }

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (IsUsableDialogueSelectable(selected))
        {
            return;
        }

        Selectable selectable = FindFirstDialogueSelectable();
        if (selectable != null)
        {
            eventSystem.SetSelectedGameObject(selectable.gameObject);
        }
    }

    private void MoveDialogueSelection(int direction)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || dialogueUiObject == null)
        {
            return;
        }

        RefreshDialogueSelectables();
        if (dialogueSelectables.Count == 0)
        {
            return;
        }

        GameObject selectedObject = eventSystem.currentSelectedGameObject;
        int selectedIndex = -1;
        for (int i = 0; i < dialogueSelectables.Count; i++)
        {
            if (dialogueSelectables[i] != null &&
                dialogueSelectables[i].gameObject == selectedObject)
            {
                selectedIndex = i;
                break;
            }
        }

        if (selectedIndex < 0)
        {
            selectedIndex = direction > 0 ? -1 : 0;
        }

        int nextIndex = selectedIndex + direction;
        if (nextIndex < 0)
        {
            nextIndex = dialogueSelectables.Count - 1;
        }
        else if (nextIndex >= dialogueSelectables.Count)
        {
            nextIndex = 0;
        }

        eventSystem.SetSelectedGameObject(dialogueSelectables[nextIndex].gameObject);
    }

    private void SubmitDialogueSelection()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        EnsureDialogueSelection();
        GameObject selected = eventSystem.currentSelectedGameObject;
        if (!IsUsableDialogueSelectable(selected))
        {
            return;
        }

        BaseEventData eventData = new(eventSystem);
        ExecuteEvents.Execute(selected, eventData, ExecuteEvents.submitHandler);
    }

    private bool IsSentenceSkipInputDown()
    {
        return Input.GetKeyDown(skipSentenceKey) ||
               Input.GetKeyDown(alternateSkipSentenceKey) ||
               Input.GetMouseButtonDown(skipSentenceMouseButton);
    }

    private bool TrySkipCurrentSentence()
    {
        if (!CanSkipCurrentSentence() || IsResponseMenuActive())
        {
            return false;
        }

        AbstractDialogueUI dialogueUi = GetActiveDialogueUi();
        if (dialogueUi == null || !dialogueUi.isOpen)
        {
            return false;
        }

        dialogueUi.OnContinue();
        return true;
    }

    private bool CanSkipCurrentSentence()
    {
        ConversationState state = DialogueManager.currentConversationState;
        DialogueEntry entry = state != null && state.subtitle != null
            ? state.subtitle.dialogueEntry
            : null;

        if (entry == null || entry.fields == null)
        {
            return defaultCanSkipSentence;
        }

        string fieldValue = Field.LookupValue(entry.fields, canSkipSentenceFieldName);
        return string.IsNullOrWhiteSpace(fieldValue)
            ? defaultCanSkipSentence
            : Tools.StringToBool(fieldValue);
    }

    private bool IsResponseMenuActive()
    {
        RefreshDialogueSelectables();
        for (int i = 0; i < dialogueSelectables.Count; i++)
        {
            Selectable selectable = dialogueSelectables[i];
            if (selectable == null)
            {
                continue;
            }

            if (selectable.GetComponent<UnityUIResponseButton>() != null ||
                selectable.GetComponent<StandardUIResponseButton>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private AbstractDialogueUI GetActiveDialogueUi()
    {
        if (dialogueUiObject == null)
        {
            return null;
        }

        return dialogueUiObject.GetComponentInChildren<AbstractDialogueUI>(true);
    }

    private Selectable FindFirstDialogueSelectable()
    {
        RefreshDialogueSelectables();
        return dialogueSelectables.Count > 0 ? dialogueSelectables[0] : null;
    }

    private void RefreshDialogueSelectables()
    {
        dialogueSelectables.Clear();
        if (dialogueUiObject == null)
        {
            return;
        }

        Selectable[] selectables = dialogueUiObject.GetComponentsInChildren<Selectable>(false);
        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            if (selectable != null &&
                selectable.IsActive() &&
                selectable.IsInteractable())
            {
                dialogueSelectables.Add(selectable);
            }
        }
    }

    private bool IsUsableDialogueSelectable(GameObject selected)
    {
        if (selected == null || dialogueUiObject == null)
        {
            return false;
        }

        if (!selected.transform.IsChildOf(dialogueUiObject.transform))
        {
            return false;
        }

        Selectable selectable = selected.GetComponent<Selectable>();
        return selectable != null &&
               selectable.IsActive() &&
               selectable.IsInteractable();
    }

    private void ApplyDialogueCursorState(bool pointerMode)
    {
        if (!manageCursor)
        {
            return;
        }

        if (playerModeStateController != null)
        {
            playerModeStateController.SetDialoguePointerMode(pointerMode);
            return;
        }

        Cursor.lockState = pointerMode ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = pointerMode;
    }

    private void ApplyNormalCursorState()
    {
        if (!manageCursor)
        {
            return;
        }

        Cursor.lockState = lockCursorWhenNotInDialogue
            ? CursorLockMode.Locked
            : CursorLockMode.None;
        Cursor.visible = !lockCursorWhenNotInDialogue;
    }
}
