using UnityEngine;

[DisallowMultipleComponent]
public class GameModeManager : SingletonAutoMono<GameModeManager>
{
    [Header("State")]
    [SerializeField] private GameModeState currentMode = GameModeState.Exploration;

    [Header("Compatibility")]
    [SerializeField] private bool emitLegacyControlEvents = true;

    private GameModeCapabilities currentCapabilities;

    public GameModeState CurrentMode => currentMode;

    public GameModeCapabilities CurrentCapabilities
    {
        get
        {
            EnsureCapabilities();
            return currentCapabilities;
        }
    }

    public bool RequestMode(
        GameModeState newMode,
        Object requester = null,
        bool force = false)
    {
        EnsureCapabilities();

        if (currentMode == newMode)
        {
            return true;
        }

        if (!force && !CanTransitionTo(newMode))
        {
            if (requester != null)
            {
                Debug.Log(
                    $"[GameModeManager] Transition denied: {currentMode} -> {newMode}.",
                    requester);
            }

            return false;
        }

        GameModeState oldMode = currentMode;
        GameModeCapabilities oldCapabilities = currentCapabilities.Clone();
        GameModeCapabilities newCapabilities = CreateCapabilities(newMode);

        currentMode = newMode;
        currentCapabilities = newCapabilities;

        ApplyModeSideEffects(newCapabilities);

        GameModeChangedInfo changedInfo = new()
        {
            oldMode = oldMode,
            newMode = newMode,
            oldCapabilities = oldCapabilities,
            newCapabilities = newCapabilities.Clone()
        };

        EventCenter.Instance.EventTrigger(
            E_EventType.E_GameMode_Changed,
            changedInfo);

        return true;
    }

    public bool ExitMode(GameModeState mode, GameModeState fallbackMode = GameModeState.Exploration)
    {
        if (currentMode != mode)
        {
            return true;
        }

        return RequestMode(fallbackMode, this, true);
    }

    public bool IsCurrentMode(GameModeState mode)
    {
        return currentMode == mode;
    }

    private void Awake()
    {
        EnsureCapabilities();
        ApplyModeSideEffects(currentCapabilities);
    }

    private void EnsureCapabilities()
    {
        currentCapabilities ??= CreateCapabilities(currentMode);
    }

    private bool CanTransitionTo(GameModeState newMode)
    {
        if (newMode == GameModeState.Exploration)
        {
            return true;
        }

        GameModeCapabilities capabilities = CurrentCapabilities;

        switch (newMode)
        {
            case GameModeState.BrushBulletTime:
                return capabilities.canEnterBrush;

            case GameModeState.Build:
                return capabilities.canEnterBuild;

            case GameModeState.Menu:
                return capabilities.canOpenMenu;
        }

        int currentPriority = GetModePriority(currentMode);
        int newPriority = GetModePriority(newMode);

        return newPriority >= currentPriority;
    }

    private void ApplyModeSideEffects(GameModeCapabilities capabilities)
    {
        GameTimeScaleService.Instance.SetGameModeTimeScale(
            capabilities.timeScale,
            capabilities.timeScalePriority,
            capabilities.timeScaleTransitionDuration);

        if (capabilities.manageCursor)
        {
            Cursor.lockState = capabilities.lockCursor
                ? CursorLockMode.Locked
                : CursorLockMode.None;
            Cursor.visible = capabilities.showCursor;
        }

        if (!emitLegacyControlEvents)
        {
            return;
        }

        EventCenter.Instance.EventTrigger<bool>(
            E_EventType.E_Player_ControlEnable,
            capabilities.canMove);

        EventCenter.Instance.EventTrigger<bool>(
            E_EventType.E_Player_CombatEnable,
            capabilities.canAttack);

        EventCenter.Instance.EventTrigger<bool>(
            E_EventType.E_Camera_InputEnable,
            capabilities.canLook);
    }

    private static GameModeCapabilities CreateCapabilities(GameModeState mode)
    {
        GameModeCapabilities capabilities = new();

        switch (mode)
        {
            case GameModeState.Combat:
                capabilities.inputOwner = GameInputOwner.Gameplay;
                break;

            case GameModeState.BrushBulletTime:
                capabilities.inputOwner = GameInputOwner.Brush;
                capabilities.canMove = false;
                capabilities.canLook = false;
                capabilities.canAttack = false;
                capabilities.canLockOn = false;
                capabilities.canEnterBrush = false;
                capabilities.canDrawBrush = true;
                capabilities.canEnterBuild = false;
                capabilities.canInteract = false;
                capabilities.canOpenMenu = false;
                capabilities.showInteractionUI = false;
                capabilities.lockCursor = false;
                capabilities.showCursor = false;
                capabilities.timeScale = 0.15f;
                capabilities.timeScalePriority = 100;
                capabilities.timeScaleTransitionDuration = 0.35f;
                break;

            case GameModeState.Build:
                capabilities.inputOwner = GameInputOwner.Build;
                capabilities.canMove = false;
                capabilities.canAttack = false;
                capabilities.canLockOn = false;
                capabilities.canEnterBrush = false;
                capabilities.canUseBuildPlacement = true;
                capabilities.canInteract = false;
                capabilities.canOpenMenu = false;
                capabilities.showInteractionUI = false;
                break;

            case GameModeState.Dialogue:
                capabilities.inputOwner = GameInputOwner.Dialogue;
                capabilities.canMove = false;
                capabilities.canAttack = false;
                capabilities.canLockOn = false;
                capabilities.canEnterBrush = false;
                capabilities.canEnterBuild = false;
                capabilities.canInteract = false;
                capabilities.canOpenMenu = false;
                capabilities.canTakeDamage = false;
                capabilities.canRecoverInk = false;
                capabilities.canBeDetectedByEnemy = false;
                capabilities.showInteractionUI = false;
                capabilities.showPlayerBars = false;
                break;

            case GameModeState.Cutscene:
                capabilities.inputOwner = GameInputOwner.Cutscene;
                capabilities.canMove = false;
                capabilities.canLook = false;
                capabilities.canAttack = false;
                capabilities.canLockOn = false;
                capabilities.canEnterBrush = false;
                capabilities.canEnterBuild = false;
                capabilities.canInteract = false;
                capabilities.canOpenMenu = false;
                capabilities.canTakeDamage = false;
                capabilities.canRecoverInk = false;
                capabilities.canBeDetectedByEnemy = false;
                capabilities.showInteractionUI = false;
                capabilities.showPlayerBars = false;
                break;

            case GameModeState.Menu:
                capabilities.inputOwner = GameInputOwner.UI;
                capabilities.canMove = false;
                capabilities.canLook = false;
                capabilities.canAttack = false;
                capabilities.canLockOn = false;
                capabilities.canEnterBrush = false;
                capabilities.canEnterBuild = false;
                capabilities.canInteract = false;
                capabilities.canTakeDamage = false;
                capabilities.canRecoverInk = false;
                capabilities.canBeDetectedByEnemy = false;
                capabilities.showInteractionUI = false;
                capabilities.lockCursor = false;
                capabilities.showCursor = true;
                capabilities.timeScale = 0f;
                capabilities.timeScalePriority = 200;
                break;

            case GameModeState.Dead:
                capabilities.inputOwner = GameInputOwner.None;
                capabilities.canMove = false;
                capabilities.canLook = false;
                capabilities.canAttack = false;
                capabilities.canLockOn = false;
                capabilities.canEnterBrush = false;
                capabilities.canEnterBuild = false;
                capabilities.canInteract = false;
                capabilities.canOpenMenu = false;
                capabilities.canTakeDamage = false;
                capabilities.canRecoverInk = false;
                capabilities.canBeDetectedByEnemy = false;
                capabilities.showInteractionUI = false;
                capabilities.timeScalePriority = 300;
                break;
        }

        return capabilities;
    }

    private static int GetModePriority(GameModeState mode)
    {
        switch (mode)
        {
            case GameModeState.Dead:
                return 1000;
            case GameModeState.Cutscene:
                return 900;
            case GameModeState.Dialogue:
                return 800;
            case GameModeState.Menu:
                return 700;
            case GameModeState.BrushBulletTime:
            case GameModeState.Build:
                return 500;
            case GameModeState.Combat:
                return 100;
            default:
                return 0;
        }
    }
}
