using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class CharacterProfileUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerCharacterStatsController statsController;

    [SerializeField]
    private ActionPlayerController playerController;

    private EventCenter eventCenter;
    private InputMgr inputMgr;

    private bool isOpen;
    public bool IsOpen => isOpen;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        inputMgr = InputMgr.Instance;
        eventCenter = EventCenter.Instance;

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

    private void Update()
    {
        HandleEscapeInput();
    }

    private void HandleEscapeInput()
    {
        if (!isOpen)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            CloseCharacterPanel();
        }
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
            statsController = GetComponent<PlayerCharacterStatsController>();
        }

        if (statsController == null)
        {
            statsController = GetComponentInChildren<PlayerCharacterStatsController>(true);
        }

        if (playerController == null)
        {
            playerController = GetComponent<ActionPlayerController>();
        }

        if (playerController == null)
        {
            playerController = GetComponentInParent<ActionPlayerController>();
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
                "[CharacterProfileUIController] StatsController not found.",
                this
            );

            return;
        }

        if (!statsController.IsInitialized && !statsController.Init())
        {
            return;
        }

        if (!GameModeManager.Instance.CurrentCapabilities.canOpenMenu)
        {
            return;
        }

        if (!GameModeManager.Instance.RequestMode(GameModeState.Menu, this))
        {
            return;
        }

        isOpen = true;

        UIMgr.Instance.ShowPanel<CharacterProfilePanel>(
            E_UILayer.System,
            panel =>
            {
                panel.Bind(statsController, CloseCharacterPanel);
            },
            true
        );

        CharacterProfilePanel panel = UIMgr.Instance.GetPanel<CharacterProfilePanel>();

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

        UIMgr.Instance.HidePanel<CharacterProfilePanel>();

        GameModeManager.Instance.ExitMode(GameModeState.Menu);
    }
}
