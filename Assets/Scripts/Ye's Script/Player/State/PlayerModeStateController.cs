using System.Collections.Generic;
using UnityEngine;

public enum PlayerModeState
{
    Default,
    Dialogue
}

[DisallowMultipleComponent]
public class PlayerModeStateController : MonoBehaviour
{
    public static PlayerModeStateController Instance { get; private set; }

    [Header("State")]
    [SerializeField] private PlayerModeState currentState = PlayerModeState.Default;

    [Header("References")]
    [SerializeField] private GameObject playerRoot;
    [SerializeField] private GameObject brushSystemRoot;
    [SerializeField] private BuildModeController buildModeController;
    [SerializeField] private InteractionManager interactionManager;
    [SerializeField] private GameObject interactionUiRoot;
    [SerializeField] private GameObject playerBarsHudRoot;

    [Header("Dialogue Rules")]
    [SerializeField] private bool hideFromEnemiesInDialogue = true;
    [SerializeField] private string hiddenLayerName = "Ignore Raycast";

    private PlayerInputReceiver inputReceiver;
    private PlayerLocomotion locomotion;
    private PlayerCombatInputHandler combatInputHandler;
    private PlayerCameraController cameraController;
    private BrushModeController brushModeController;

    private readonly Dictionary<GameObject, int> originalLayers = new();

    private bool cachedForDialogue;
    private bool originalInputReceiverEnabled;
    private bool originalLocomotionEnabled;
    private bool originalCombatInputHandlerEnabled;
    private bool originalBuildModeControllerEnabled;
    private bool originalInteractionManagerEnabled;
    private bool originalBrushSystemActive;
    private bool originalInteractionUiActive;
    private bool originalPlayerBarsHudActive;

    public PlayerModeState CurrentState => currentState;
    public bool IsInDialogue => currentState == PlayerModeState.Dialogue;
    public static bool IsDialogueMode => Instance != null && Instance.IsInDialogue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PlayerModeStateController] Multiple instances found. Keeping the latest instance.", this);
        }

        Instance = this;
        ResolveReferences();
        currentState = PlayerModeState.Default;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetState(PlayerModeState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        if (newState == PlayerModeState.Dialogue)
        {
            EnterDialogueState();
        }
        else
        {
            ExitDialogueState();
        }
    }

    public void EnterDialogueState()
    {
        ResolveReferences();

        if (currentState == PlayerModeState.Dialogue)
        {
            return;
        }

        CacheDialogueState();
        currentState = PlayerModeState.Dialogue;

        if (buildModeController != null)
        {
            buildModeController.SetBuildMode(false);
            buildModeController.enabled = false;
        }

        if (brushModeController != null && brushModeController.IsBrushMode)
        {
            EventCenter.Instance.EventTrigger(E_EventType.E_Brush_RequestExit);
        }

        if (brushSystemRoot != null)
        {
            brushSystemRoot.SetActive(false);
        }

        if (combatInputHandler != null)
        {
            combatInputHandler.enabled = false;
        }

        if (inputReceiver != null)
        {
            inputReceiver.enabled = false;
        }

        if (locomotion != null)
        {
            locomotion.CanMove = false;
            locomotion.enabled = false;
        }

        if (cameraController != null)
        {
            cameraController.SetCameraInputEnabled(true);
            cameraController.SetCursorLocked(true);
        }

        if (interactionManager != null)
        {
            interactionManager.enabled = false;
        }

        if (interactionUiRoot != null)
        {
            interactionUiRoot.SetActive(false);
        }

        if (playerBarsHudRoot != null)
        {
            playerBarsHudRoot.SetActive(false);
        }

        if (hideFromEnemiesInDialogue)
        {
            ApplyHiddenLayer();
        }
    }

    public void ExitDialogueState()
    {
        if (currentState != PlayerModeState.Dialogue)
        {
            return;
        }

        RestoreLayers();

        if (brushSystemRoot != null)
        {
            brushSystemRoot.SetActive(originalBrushSystemActive);
        }

        if (buildModeController != null)
        {
            buildModeController.enabled = originalBuildModeControllerEnabled;
        }

        if (inputReceiver != null)
        {
            inputReceiver.enabled = originalInputReceiverEnabled;
        }

        if (combatInputHandler != null)
        {
            combatInputHandler.enabled = originalCombatInputHandlerEnabled;
        }

        if (locomotion != null)
        {
            locomotion.enabled = originalLocomotionEnabled;
            locomotion.CanMove = originalLocomotionEnabled;
        }

        if (interactionManager != null)
        {
            interactionManager.enabled = originalInteractionManagerEnabled;
        }

        if (interactionUiRoot != null)
        {
            interactionUiRoot.SetActive(originalInteractionUiActive);
        }

        if (playerBarsHudRoot != null)
        {
            playerBarsHudRoot.SetActive(originalPlayerBarsHudActive);
        }

        if (cameraController != null)
        {
            cameraController.SetCameraInputEnabled(true);
            cameraController.SetCursorLocked(true);
        }

        cachedForDialogue = false;
        currentState = PlayerModeState.Default;
    }

    private void ResolveReferences()
    {
        if (playerRoot == null)
        {
            ActionPlayerController playerController =
                FindAnyObjectByType<ActionPlayerController>(FindObjectsInactive.Include);
            if (playerController != null)
            {
                playerRoot = playerController.gameObject;
            }
        }

        if (playerRoot == null && PlayerResourceController.Instance != null)
        {
            playerRoot = PlayerResourceController.Instance.gameObject;
        }

        if (playerRoot != null)
        {
            inputReceiver = playerRoot.GetComponent<PlayerInputReceiver>();
            locomotion = playerRoot.GetComponent<PlayerLocomotion>();
            combatInputHandler = playerRoot.GetComponent<PlayerCombatInputHandler>();
            cameraController = playerRoot.GetComponent<PlayerCameraController>();
        }

        if (brushModeController == null)
        {
            brushModeController = FindAnyObjectByType<BrushModeController>(FindObjectsInactive.Include);
        }

        if (brushSystemRoot == null && brushModeController != null)
        {
            brushSystemRoot = brushModeController.gameObject;
        }

        if (buildModeController == null)
        {
            buildModeController = BuildModeController.Instance;
        }

        if (buildModeController == null)
        {
            buildModeController = FindAnyObjectByType<BuildModeController>(FindObjectsInactive.Include);
        }

        if (interactionManager == null)
        {
            interactionManager = FindAnyObjectByType<InteractionManager>(FindObjectsInactive.Include);
        }

        if (interactionUiRoot == null)
        {
            interactionUiRoot = GameObject.Find("InteractionPromptUI");
        }

        if (playerBarsHudRoot == null)
        {
            playerBarsHudRoot = GameObject.Find("Ye_PlayerBarsHUD_Canvas");
        }
    }

    private void CacheDialogueState()
    {
        if (cachedForDialogue)
        {
            return;
        }

        originalInputReceiverEnabled = inputReceiver != null && inputReceiver.enabled;
        originalLocomotionEnabled = locomotion != null && locomotion.enabled;
        originalCombatInputHandlerEnabled = combatInputHandler != null && combatInputHandler.enabled;
        originalBuildModeControllerEnabled = buildModeController != null && buildModeController.enabled;
        originalInteractionManagerEnabled = interactionManager != null && interactionManager.enabled;
        originalBrushSystemActive = brushSystemRoot != null && brushSystemRoot.activeSelf;
        originalInteractionUiActive = interactionUiRoot != null && interactionUiRoot.activeSelf;
        originalPlayerBarsHudActive = playerBarsHudRoot != null && playerBarsHudRoot.activeSelf;
        cachedForDialogue = true;
    }

    private void ApplyHiddenLayer()
    {
        if (playerRoot == null)
        {
            return;
        }

        int hiddenLayer = LayerMask.NameToLayer(hiddenLayerName);
        if (hiddenLayer < 0)
        {
            Debug.LogWarning($"[PlayerModeStateController] Layer '{hiddenLayerName}' was not found.", this);
            return;
        }

        originalLayers.Clear();
        Transform[] children = playerRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            GameObject childObject = children[i].gameObject;
            originalLayers[childObject] = childObject.layer;
            childObject.layer = hiddenLayer;
        }
    }

    private void RestoreLayers()
    {
        foreach (KeyValuePair<GameObject, int> entry in originalLayers)
        {
            if (entry.Key != null)
            {
                entry.Key.layer = entry.Value;
            }
        }

        originalLayers.Clear();
    }
}
