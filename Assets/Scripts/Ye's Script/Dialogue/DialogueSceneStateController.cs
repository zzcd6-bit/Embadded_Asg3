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
    private PlayerModeStateController playerModeStateController;
    private bool subscribed;

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
        ApplyDialogueCursorState();
    }

    private void HandleConversationEnded(Transform actor)
    {
        RestoreDialogueReactables();
        if (playerModeStateController != null)
        {
            playerModeStateController.ExitDialogueState();
        }

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

    private void ApplyDialogueCursorState()
    {
        if (!manageCursor)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
