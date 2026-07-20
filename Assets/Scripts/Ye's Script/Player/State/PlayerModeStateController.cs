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

    [SerializeField] private PlayerCameraController cameraController;

    public PlayerModeState CurrentState =>
        GameModeManager.Instance.CurrentMode == GameModeState.Dialogue
            ? PlayerModeState.Dialogue
            : PlayerModeState.Default;

    public bool IsInDialogue =>
        GameModeManager.Instance.CurrentMode == GameModeState.Dialogue;

    public static bool IsDialogueMode =>
        GameModeManager.Instance.CurrentMode == GameModeState.Dialogue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[PlayerModeStateController] Multiple instances found. Keeping the latest instance.",
                this);
        }

        Instance = this;
        ResolveCameraController();
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
        if (newState == PlayerModeState.Dialogue)
        {
            EnterDialogueState();
            return;
        }

        ExitDialogueState();
    }

    public void EnterDialogueState()
    {
        GameModeManager.Instance.RequestMode(GameModeState.Dialogue, this);
    }

    public void ExitDialogueState()
    {
        GameModeManager.Instance.ExitMode(GameModeState.Dialogue);
    }

    public void SetDialoguePointerMode(bool pointerMode)
    {
        if (!IsInDialogue)
        {
            return;
        }

        ResolveCameraController();

        if (cameraController != null)
        {
            cameraController.SetCameraInputEnabled(!pointerMode);
            cameraController.SetCursorLocked(!pointerMode);
            return;
        }

        Cursor.lockState = pointerMode ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = pointerMode;
    }

    private void ResolveCameraController()
    {
        if (cameraController != null)
        {
            return;
        }

        ActionPlayerController playerController =
            FindAnyObjectByType<ActionPlayerController>(FindObjectsInactive.Include);

        if (playerController != null)
        {
            cameraController = playerController.GetComponent<PlayerCameraController>();
        }
    }
}
