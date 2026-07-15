using UnityEngine;

[DisallowMultipleComponent]
public class CharacterProfileUIController :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerCharacterStatsController
        statsController;

    [SerializeField]
    private ActionPlayerController playerController;

    [Header("Behaviour")]
    [SerializeField]
    private bool pauseGameWhenOpen = true;

    private EventCenter eventCenter;
    private InputMgr inputMgr;

    private bool isOpen;

    private bool previousGameplayControlEnabled;

    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockMode;

    private float previousTimeScale;
    private float previousFixedDeltaTime;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        inputMgr =
            InputMgr.Instance;

        eventCenter =
            EventCenter.Instance;

        inputMgr.ChangeKeyboardInfo(
            E_EventType.E_Input_CharacterPanel,
            KeyCode.B,
            InputInfo.E_InputType.Down
        );

        inputMgr.StartOrCloseInputMgr(true);

        eventCenter.AddEventListener(
            E_EventType.E_Input_CharacterPanel,
            ToggleCharacterPanel
        );
    }

    private void OnDisable()
    {
        if (eventCenter != null)
        {
            eventCenter.RemoveEventListener(
                E_EventType.E_Input_CharacterPanel,
                ToggleCharacterPanel
            );
        }

        if (isOpen)
        {
            CloseCharacterPanel();
        }

        eventCenter = null;
        inputMgr = null;
    }

    private void ResolveReferences()
    {
        if (statsController == null)
        {
            statsController =
                GetComponent<
                    PlayerCharacterStatsController>();
        }

        if (statsController == null)
        {
            statsController =
                GetComponentInChildren<
                    PlayerCharacterStatsController>(true);
        }

        if (playerController == null)
        {
            playerController =
                GetComponent<
                    ActionPlayerController>();
        }

        if (playerController == null)
        {
            playerController =
                GetComponentInParent<
                    ActionPlayerController>();
        }
    }

    private void ToggleCharacterPanel()
    {
        if (isOpen)
        {
            CloseCharacterPanel();
            return;
        }

        OpenCharacterPanel();
    }

    private void OpenCharacterPanel()
    {
        ResolveReferences();

        if (statsController == null)
        {
            Debug.LogWarning(
                "[CharacterProfileUIController] " +
                "StatsController not found.",
                this
            );

            return;
        }

        if (!statsController.IsInitialized &&
            !statsController.Init())
        {
            return;
        }

        if (playerController != null &&
            !playerController.GameplayControlEnabled)
        {
            return;
        }

        isOpen = true;

        previousCursorVisible =
            Cursor.visible;

        previousCursorLockMode =
            Cursor.lockState;

        previousTimeScale =
            Time.timeScale;

        previousFixedDeltaTime =
            Time.fixedDeltaTime;

        if (playerController != null)
        {
            previousGameplayControlEnabled =
                playerController
                    .GameplayControlEnabled;

            playerController
                .SetGameplayControlEnabled(false);
        }

        EventCenter.Instance.EventTrigger<bool>(
            E_EventType.E_Player_ControlEnable,
            false
        );

        EventCenter.Instance.EventTrigger<bool>(
            E_EventType.E_Player_CombatEnable,
            false
        );

        EventCenter.Instance.EventTrigger<bool>(
            E_EventType.E_Camera_InputEnable,
            false
        );

        Cursor.visible = true;
        Cursor.lockState =
            CursorLockMode.None;

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 0f;
        }

        UIMgr.Instance.ShowPanel<
            CharacterProfilePanel
        >(
            E_UILayer.System,
            panel =>
            {
                panel.Bind(
                    statsController,
                    CloseCharacterPanel
                );
            },
            true
        );

        CharacterProfilePanel panel =
            UIMgr.Instance.GetPanel<
                CharacterProfilePanel>();

        if (panel == null)
        {
            CloseCharacterPanel();
        }
    }

    public void CloseCharacterPanel()
    {
        if (!isOpen)
            return;

        isOpen = false;

        UIMgr.Instance.HidePanel<
            CharacterProfilePanel>();

        if (pauseGameWhenOpen)
        {
            Time.timeScale =
                previousTimeScale;

            Time.fixedDeltaTime =
                previousFixedDeltaTime;
        }

        Cursor.visible =
            previousCursorVisible;

        Cursor.lockState =
            previousCursorLockMode;

        if (playerController != null)
        {
            playerController
                .SetGameplayControlEnabled(
                    previousGameplayControlEnabled
                );
        }

        if (previousGameplayControlEnabled)
        {
            EventCenter.Instance.EventTrigger<bool>(
                E_EventType.E_Player_ControlEnable,
                true
            );

            EventCenter.Instance.EventTrigger<bool>(
                E_EventType.E_Player_CombatEnable,
                true
            );

            EventCenter.Instance.EventTrigger<bool>(
                E_EventType.E_Camera_InputEnable,
                true
            );
        }
    }
}