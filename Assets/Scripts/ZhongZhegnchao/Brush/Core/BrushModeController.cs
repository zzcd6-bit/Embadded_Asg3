using UnityEngine;

public class BrushModeController : MonoBehaviour
{
    [Header("Brush Mode")]
    public float brushTimeScale = 0.15f;
    public float transitionDuration = 0.35f;

    [Header("New Player")]
    [SerializeField] private ActionPlayerController newPlayerController;

    [Header("画画模式冷却")]
    public BrushModeCooldownController cooldownController;

    public bool IsBrushMode { get; private set; }

    private void Awake()
    {
        if (newPlayerController == null)
        {
            newPlayerController = FindAnyObjectByType<ActionPlayerController>();
        }

        if (cooldownController == null)
        {
            cooldownController = GetComponent<BrushModeCooldownController>();
        }

        if (cooldownController == null)
        {
            cooldownController = GetComponentInChildren<BrushModeCooldownController>();
        }

        if (cooldownController == null)
        {
            cooldownController = GetComponentInParent<BrushModeCooldownController>();
        }
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener<GameModeChangedInfo>(
            E_EventType.E_GameMode_Changed,
            OnGameModeChanged
        );

        EventCenter.Instance.AddEventListener(
            E_EventType.E_Brush_Enter,
            RequestEnterBrushMode
        );

        EventCenter.Instance.AddEventListener(
            E_EventType.E_Brush_Exit,
            RequestExitBrushMode
        );

        EventCenter.Instance.AddEventListener(
            E_EventType.E_Brush_RequestExit,
            RequestExitBrushMode
        );

        if (GameModeManager.Instance.CurrentMode == GameModeState.BrushBulletTime)
        {
            EnterBrushModeInternal();
        }
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<GameModeChangedInfo>(
            E_EventType.E_GameMode_Changed,
            OnGameModeChanged
        );

        EventCenter.Instance.RemoveEventListener(
            E_EventType.E_Brush_Enter,
            RequestEnterBrushMode
        );

        EventCenter.Instance.RemoveEventListener(
            E_EventType.E_Brush_Exit,
            RequestExitBrushMode
        );

        EventCenter.Instance.RemoveEventListener(
            E_EventType.E_Brush_RequestExit,
            RequestExitBrushMode
        );
    }

    private void RequestEnterBrushMode()
    {
        if (cooldownController != null && !cooldownController.CanEnterBrushMode())
        {
            Debug.Log(
                $"[BrushModeController] Brush mode is cooling down. Remaining: {cooldownController.RemainingCooldown:F1}s",
                this
            );

            return;
        }

        if (IsBrushMode)
        {
            return;
        }

        GameModeManager.Instance.RequestMode(
            GameModeState.BrushBulletTime,
            this
        );
    }

    private void RequestExitBrushMode()
    {
        GameModeManager.Instance.ExitMode(GameModeState.BrushBulletTime);
    }

    private void OnGameModeChanged(GameModeChangedInfo info)
    {
        if (info == null)
        {
            return;
        }

        if (info.newMode == GameModeState.BrushBulletTime)
        {
            EnterBrushModeInternal();
            return;
        }

        if (IsBrushMode)
        {
            ExitBrushModeInternal();
        }
    }

    private void EnterBrushModeInternal()
    {
        if (IsBrushMode)
        {
            return;
        }

        IsBrushMode = true;

        GameTimeScaleService.Instance.SetGameModeTimeScale(
            brushTimeScale,
            100,
            transitionDuration
        );

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;

        EventCenter.Instance.EventTrigger<bool>(
            E_EventType.E_Brush_ModeChanged,
            true
        );
    }

    private void ExitBrushModeInternal()
    {
        if (!IsBrushMode)
        {
            return;
        }

        EventCenter.Instance.EventTrigger(E_EventType.E_Brush_DrawEnd);

        if (cooldownController != null)
        {
            cooldownController.StartCooldown();
        }

        IsBrushMode = false;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        EventCenter.Instance.EventTrigger<bool>(
            E_EventType.E_Brush_ModeChanged,
            false
        );
    }
}
