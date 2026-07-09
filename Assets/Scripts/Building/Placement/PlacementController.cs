using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera placementCamera;
    [SerializeField] private GridPlacementSystem gridPlacementSystem;
    [SerializeField] private PlacementPreviewController previewController;
    [SerializeField] private PlacementCommitter placementCommitter;

    [Header("Raycast")]
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float raycastDistance = 500f;
    [SerializeField] private bool ignorePointerOverUI = true;

    [Header("Validation")]
    [SerializeField] private float placementCheckInterval = 0.1f;

    private BuildableItemData currentItem;
    private Quaternion currentRotation = Quaternion.identity;
    private Vector3 currentPlacementPosition;
    private Vector2Int currentPivotCell;
    private int currentRotationSteps;
    private bool hasPlacementPosition;
    private float nextPlacementCheckTime;
    private int placementStartedFrame = -1;
    private BuildingInstance editingBuilding;
    private Vector2Int originalPivotCell;
    private int originalRotationSteps;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    public bool IsPlacing => previewController != null && previewController.HasPreview;

    private Camera ActiveCamera
    {
        get
        {
            if (placementCamera == null)
            {
                placementCamera = Camera.main;
            }

            return placementCamera;
        }
    }

    private void Awake()
    {
        if (gridPlacementSystem == null)
        {
            gridPlacementSystem = FindAnyObjectByType<GridPlacementSystem>();
        }

        if (previewController == null)
        {
            previewController = GetComponent<PlacementPreviewController>();
        }

        if (previewController == null)
        {
            previewController = gameObject.AddComponent<PlacementPreviewController>();
        }

        if (placementCommitter == null)
        {
            placementCommitter = GetComponent<PlacementCommitter>();
        }

        if (placementCommitter == null)
        {
            placementCommitter = gameObject.AddComponent<PlacementCommitter>();
        }

        placementCommitter.Initialize(gridPlacementSystem);
    }

    // Ye build placement input bridge: placement commands now come from InputMgr/EventCenter.
    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener(E_EventType.E_Build_PlacementConfirm, HandleConfirmInput);
        EventCenter.Instance.AddEventListener(E_EventType.E_Build_PlacementCancel, HandleCancelInput);
        EventCenter.Instance.AddEventListener(E_EventType.E_Build_PlacementRotate, HandleRotateInput);
        EventCenter.Instance.AddEventListener(E_EventType.E_Build_PlacementDelete, HandleDeleteInput);
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Build_PlacementConfirm, HandleConfirmInput);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Build_PlacementCancel, HandleCancelInput);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Build_PlacementRotate, HandleRotateInput);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Build_PlacementDelete, HandleDeleteInput);
    }

    private void Update()
    {
        if (!IsPlacing)
        {
            return;
        }

        UpdatePreviewPosition();
    }

    public void StartPlacement(BuildableItemData item)
    {
        if (item == null || item.BuildingPrefab == null || gridPlacementSystem == null)
        {
            return;
        }

        CancelPlacement();
        currentItem = item;
        currentRotation = Quaternion.identity;
        currentRotationSteps = 0;
        BeginPlacementSession();
        previewController.CreateNewPreview(item);
        UpdatePreviewPosition();
        ValidateCurrentPlacement(true);
    }

    public void StartEditing(BuildingInstance building)
    {
        if (building == null || !building.IsPlacementCompleted || gridPlacementSystem == null)
        {
            return;
        }

        CancelPlacement();
        editingBuilding = building;
        currentItem = building.Item;
        currentPivotCell = building.PivotCell;
        currentRotationSteps = building.RotationSteps;
        currentRotation = building.transform.rotation;
        currentPlacementPosition = building.transform.position;
        originalPivotCell = building.PivotCell;
        originalRotationSteps = building.RotationSteps;
        originalPosition = building.transform.position;
        originalRotation = building.transform.rotation;
        hasPlacementPosition = true;
        BeginPlacementSession();

        gridPlacementSystem.ClearOccupied(building);
        building.MarkPlacementEditing();
        previewController.UseExistingAsPreview(building);
        ValidateCurrentPlacement(true);
    }

    public void ConfirmPlacement()
    {
        if (!CanCommitCurrentPlacement())
        {
            ValidateCurrentPlacement(true);
            return;
        }

        BuildingInstance building = editingBuilding != null
            ? editingBuilding
            : placementCommitter.CreateBuilding(currentItem, currentPlacementPosition, currentRotation);

        previewController.RestoreVisual();
        previewController.RestoreEditingColliders();

        if (editingBuilding == null)
        {
            previewController.DestroyPreview();
        }

        placementCommitter.Commit(building, currentItem, currentPivotCell, currentRotationSteps);
        ClearPlacementState();
    }

    public void CancelPlacement()
    {
        if (!IsPlacing)
        {
            ClearPlacementState();
            return;
        }

        if (editingBuilding != null)
        {
            RestoreEditedBuilding();
            return;
        }

        previewController.DestroyPreview();
        ClearPlacementState();
    }

    public void RotatePreview()
    {
        currentRotation *= Quaternion.Euler(0f, 90f, 0f);
        currentRotationSteps = (currentRotationSteps + 1) % 4;
        previewController.SetTransform(currentPlacementPosition, currentRotation);
        ValidateCurrentPlacement(true);
    }

    public void DeleteEditingBuilding()
    {
        if (editingBuilding == null)
        {
            return;
        }

        BuildingInstance building = editingBuilding;
        previewController.RestoreEditingColliders();
        ClearPlacementState();
        placementCommitter.Delete(building);
    }

    private void BeginPlacementSession()
    {
        hasPlacementPosition = false;
        nextPlacementCheckTime = 0f;
        placementStartedFrame = Time.frameCount;
    }

    private void UpdatePreviewPosition()
    {
        if (ActiveCamera == null)
        {
            return;
        }

        if (ignorePointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector3 mousePosition = Input.mousePosition;
        Ray ray = ActiveCamera.ScreenPointToRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            SetPreviewPositionFromWorld(hit.point);
            ValidateCurrentPlacement(false);
        }
    }

    // Ye build placement input bridge: mirrors the old polling guard for the frame placement starts.
    private bool CanHandlePlacementEvent()
    {
        return IsPlacing && Time.frameCount != placementStartedFrame;
    }

    // Ye build placement input bridge: confirm placement via E_Build_PlacementConfirm.
    private void HandleConfirmInput()
    {
        if (CanHandlePlacementEvent() && !IsPointerOverUI())
        {
            ConfirmPlacement();
        }
    }

    // Ye build placement input bridge: cancel placement via E_Build_PlacementCancel.
    private void HandleCancelInput()
    {
        if (CanHandlePlacementEvent() && !IsPointerOverUI())
        {
            CancelPlacement();
        }
    }

    // Ye build placement input bridge: rotate placement via E_Build_PlacementRotate.
    private void HandleRotateInput()
    {
        if (CanHandlePlacementEvent())
        {
            RotatePreview();
        }
    }

    // Ye build placement input bridge: delete edited placement via E_Build_PlacementDelete.
    private void HandleDeleteInput()
    {
        if (CanHandlePlacementEvent() && editingBuilding != null)
        {
            DeleteEditingBuilding();
        }
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void SetPreviewPositionFromWorld(Vector3 worldPosition)
    {
        currentPivotCell = gridPlacementSystem.WorldToPivotCell(worldPosition, currentItem.Size, currentRotationSteps);
        currentPlacementPosition = gridPlacementSystem.PivotCellToWorld(currentPivotCell, currentItem.Size, currentRotationSteps);
        hasPlacementPosition = true;
        previewController.SetTransform(currentPlacementPosition, currentRotation);
    }

    private void ValidateCurrentPlacement(bool force)
    {
        if (!force && Time.time < nextPlacementCheckTime)
        {
            return;
        }

        nextPlacementCheckTime = Time.time + placementCheckInterval;
        previewController.SetValid(CanCommitCurrentPlacement());
    }

    private bool CanCommitCurrentPlacement()
    {
        return currentItem != null
            && hasPlacementPosition
            && gridPlacementSystem != null
            && gridPlacementSystem.CanPlace(currentPivotCell, currentItem.Size, currentRotationSteps);
    }

    private void RestoreEditedBuilding()
    {
        previewController.RestoreVisual();
        previewController.RestoreEditingColliders();
        placementCommitter.Restore(editingBuilding, currentItem, originalPivotCell, originalRotationSteps, originalPosition, originalRotation);
        ClearPlacementState();
    }

    private void ClearPlacementState()
    {
        currentItem = null;
        editingBuilding = null;
        hasPlacementPosition = false;
        currentRotation = Quaternion.identity;
        currentRotationSteps = 0;
        previewController.ClearReferences();
    }
}
