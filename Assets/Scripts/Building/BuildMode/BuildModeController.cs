using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildModeController : MonoBehaviour
{
    public static BuildModeController Instance { get; private set; }

    [SerializeField] private PlacementController placementController;
    [SerializeField] private NavMeshPlacementGridVisual gridVisual;
    [SerializeField] private List<BuildObjectEntry> buildObjects = new();
    [SerializeField] private bool exitBuildModeAfterPlacement = true;

    private bool wasPlacing;

    public bool IsBuildMode { get; private set; }
    public IReadOnlyList<BuildObjectEntry> BuildObjects => buildObjects;

    private void Awake()
    {
        Instance = this;

        if (placementController == null)
        {
            placementController = FindAnyObjectByType<PlacementController>();
        }

        if (gridVisual == null)
        {
            gridVisual = FindAnyObjectByType<NavMeshPlacementGridVisual>();
        }
    }

    // Ye build placement input bridge: test hotkey for rune-to-placement flow.
    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener(E_EventType.E_Build_TestStartItem1, StartTestSlot1Placement);
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Build_TestStartItem1, StartTestSlot1Placement);
    }

    private void Start()
    {
        IsBuildMode = false;
        wasPlacing = false;
        InputMgr.Instance.SetBuildInputCaptured(false);

        if (gridVisual != null)
        {
            gridVisual.SetVisible(false);
        }
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

    public bool TryBuildObject(int index)
    {
        BuildableItemData item = GetBuildObject(index);
        if (item == null)
        {
            Debug.LogWarning($"[Ye Build] Build object index {index} was not found.");
            return false;
        }

        Debug.Log($"[Ye Build] Confirmed build object {index} placement request ({item.DisplayName}).");
        StartPlacement(item);
        return placementController != null && placementController.IsPlacing;
    }

    private void StartTestSlot1Placement()
    {
        TryBuildObject(1);
    }

    private BuildableItemData GetBuildObject(int index)
    {
        for (int i = 0; i < buildObjects.Count; i++)
        {
            BuildObjectEntry entry = buildObjects[i];
            if (entry.Index == index)
            {
                return entry.Item;
            }
        }

        return null;
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
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_Camera_InputEnable, true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (gridVisual != null)
        {
            gridVisual.SetVisible(enabled);
        }

        if (!enabled)
        {
            wasPlacing = false;

            if (placementController != null)
            {
                placementController.CancelPlacement();
            }
        }
    }

    [Serializable]
    public class BuildObjectEntry
    {
        [SerializeField] private int index = 1;
        [SerializeField] private BuildableItemData item;

        public int Index => index;
        public BuildableItemData Item => item;
    }
}
