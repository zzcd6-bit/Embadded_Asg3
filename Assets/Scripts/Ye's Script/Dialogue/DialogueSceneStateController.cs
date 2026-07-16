using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;

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

    private readonly List<DialogueSystemReactable> disabledReactables = new();
    private bool subscribed;

    private void OnEnable()
    {
        Subscribe();
        BindSceneDialogueUi();
        ApplyNormalCursorState();
    }

    private void Start()
    {
        Subscribe();
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
    }

    private void Update()
    {
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
        DisableDialogueReactables();
        ApplyDialogueCursorState();
    }

    private void HandleConversationEnded(Transform actor)
    {
        RestoreDialogueReactables();
        ApplyNormalCursorState();
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
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

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

    private void ApplyDialogueCursorState()
    {
        if (!manageCursor)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
