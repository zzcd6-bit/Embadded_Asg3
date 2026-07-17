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
        EventCenter.Instance.AddEventListener<GameModeChangedInfo>(
            E_EventType.E_GameMode_Changed,
            OnGameModeChanged);

        EventCenter.Instance.AddEventListener(E_EventType.E_Build_TestStartItem1, StartTestSlot1Placement);

        SetBuildModeInternal(GameModeManager.Instance.CurrentMode == GameModeState.Build);
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<GameModeChangedInfo>(
            E_EventType.E_GameMode_Changed,
            OnGameModeChanged);

        EventCenter.Instance.RemoveEventListener(E_EventType.E_Build_TestStartItem1, StartTestSlot1Placement);
    }

    private void Start()
    {
        IsBuildMode = false;
        wasPlacing = false;

        if (gridVisual != null)
        {
            gridVisual.SetVisible(false);
        }

        SetBuildModeInternal(GameModeManager.Instance.CurrentMode == GameModeState.Build);
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

        if (!GameModeManager.Instance.RequestMode(GameModeState.Build, this))
        {
            return;
        }

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
        if (enabled)
        {
            GameModeManager.Instance.RequestMode(GameModeState.Build, this);
            return;
        }

        GameModeManager.Instance.ExitMode(GameModeState.Build);
    }

    private void OnGameModeChanged(GameModeChangedInfo info)
    {
        if (info == null)
        {
            return;
        }

        SetBuildModeInternal(info.newMode == GameModeState.Build);
    }

    private void SetBuildModeInternal(bool enabled)
    {
        if (IsBuildMode == enabled)
        {
            return;
        }

        IsBuildMode = enabled;

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
