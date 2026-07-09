using UnityEngine;

public class BuildModeController : MonoBehaviour
{
    private const string DefaultSlot1ResourcePath = "BuildingData/Items/Slot1Bridge";

    public static BuildModeController Instance { get; private set; }

    [SerializeField] private PlacementController placementController;
    [SerializeField] private BuildableItemData slot1Item;
    [SerializeField] private bool exitBuildModeAfterPlacement = true;

    private bool wasPlacing;

    public bool IsBuildMode { get; private set; }

    private void Awake()
    {
        Instance = this;

        if (placementController == null)
        {
            placementController = FindAnyObjectByType<PlacementController>();
        }
    }

    // Ye build placement input bridge: test hotkey for rune-to-placement flow.
    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener(E_EventType.E_Build_StartItem1, StartSlot1Placement);
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Build_StartItem1, StartSlot1Placement);
    }

    private void Start()
    {
        IsBuildMode = false;
        wasPlacing = false;
        InputMgr.Instance.SetBuildInputCaptured(false);
    }

    private void Update()
    {
        if (!exitBuildModeAfterPlacement || !IsBuildMode || placementController == null)
        {
            return;
        }

        bool isPlacing = placementController.IsPlacing;
        if (wasPlacing && !isPlacing)
        {
            SetBuildMode(false);
        }

        wasPlacing = isPlacing;
    }

    public void StartPlacement(BuildableItemData item)
    {
        if (item == null || placementController == null)
        {
            return;
        }

        SetBuildMode(true);
        placementController.StartPlacement(item);
        wasPlacing = placementController.IsPlacing;
    }

    private void StartSlot1Placement()
    {
        StartPlacement(GetSlot1Item());
    }

    private BuildableItemData GetSlot1Item()
    {
        if (slot1Item == null)
        {
            slot1Item = Resources.Load<BuildableItemData>(DefaultSlot1ResourcePath);
        }

        return slot1Item;
    }

    public void SetBuildMode(bool enabled)
    {
        if (IsBuildMode == enabled)
        {
            return;
        }

        IsBuildMode = enabled;

        // Ye build placement input bridge: capture shared controls only while build mode is active.
        InputMgr.Instance.SetBuildInputCaptured(enabled);

        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_Player_ControlEnable, !enabled);
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_Player_CombatEnable, !enabled);
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_Camera_InputEnable, !enabled);
        Cursor.lockState = enabled ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = enabled;

        if (!enabled)
        {
            wasPlacing = false;

            if (placementController != null)
            {
                placementController.CancelPlacement();
            }
        }
    }
}
