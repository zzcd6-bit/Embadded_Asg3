using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera placementCamera;
    [SerializeField] private InputHandler inputHandler;
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

    //Resolves collaborators used by the placement state machine.
    //解析摆放状态机所需的协作组件。
    private void Awake()
    {
        if (inputHandler == null)
        {
            inputHandler = InputHandler.GetOrCreate();
        }

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

    //Updates active placement or edit interactions.
    //更新当前激活的摆放或编辑交互。
    private void Update()
    {
        if (!IsPlacing)
        {
            return;
        }

        UpdatePreviewPosition();
        HandlePlacementInput();
    }

    //Starts placing a new buildable item from inventory selection.
    //从物品栏选择开始摆放一个新的可建造物品。
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

    //Starts editing an already completed building instance.
    //开始编辑一个已经完成摆放的建筑实例。
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

    //Confirms the current placement or edit if the grid rules allow it.
    //当网格规则允许时确认当前摆放或编辑。
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

    //Cancels new placement or restores an edited building to its old state.
    //取消新建摆放，或将编辑中的建筑恢复到旧状态。
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

    //Rotates the preview or edited building by one quarter turn.
    //将预览体或编辑中的建筑旋转 90 度。
    public void RotatePreview()
    {
        currentRotation *= Quaternion.Euler(0f, 90f, 0f);
        currentRotationSteps = (currentRotationSteps + 1) % 4;
        previewController.SetTransform(currentPlacementPosition, currentRotation);
        ValidateCurrentPlacement(true);
    }

    //Deletes the building currently being edited.
    //删除当前正在编辑的建筑。
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

    //Initializes timing and position state for a new placement session.
    //为新的摆放会话初始化时间和位置状态。
    private void BeginPlacementSession()
    {
        hasPlacementPosition = false;
        nextPlacementCheckTime = 0f;
        placementStartedFrame = Time.frameCount;
    }

    //Snaps the active preview to the grid position under the mouse.
    //将当前预览体吸附到鼠标下方的网格位置。
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

        Vector3 mousePosition = inputHandler != null ? inputHandler.MousePosition : Input.mousePosition;
        Ray ray = ActiveCamera.ScreenPointToRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            SetPreviewPositionFromWorld(hit.point);
            ValidateCurrentPlacement(false);
        }
    }

    //Handles confirm, cancel, rotate, and delete input while placing.
    //处理摆放期间的确认、取消、旋转和删除输入。
    private void HandlePlacementInput()
    {
        if (Time.frameCount == placementStartedFrame)
        {
            return;
        }

        if (inputHandler != null && inputHandler.PlacementRotatePressed)
        {
            RotatePreview();
            return;
        }

        if (editingBuilding != null && inputHandler != null && inputHandler.PlacementDeletePressed)
        {
            DeleteEditingBuilding();
            return;
        }

        if (IsPointerOverUI())
        {
            return;
        }

        if (inputHandler != null && inputHandler.PlacementConfirmPressed)
        {
            ConfirmPlacement();
            return;
        }

        if (inputHandler != null && inputHandler.PlacementCancelPressed)
        {
            CancelPlacement();
        }
    }

    //Returns whether the pointer is currently over Unity UI.
    //返回鼠标指针当前是否位于 Unity UI 上。
    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    //Converts a world hit point to a grid-snapped preview transform.
    //将世界命中点转换为网格吸附后的预览体位置。
    private void SetPreviewPositionFromWorld(Vector3 worldPosition)
    {
        currentPivotCell = gridPlacementSystem.WorldToPivotCell(worldPosition, currentItem.Size, currentRotationSteps);
        currentPlacementPosition = gridPlacementSystem.PivotCellToWorld(currentPivotCell, currentItem.Size, currentRotationSteps);
        hasPlacementPosition = true;
        previewController.SetTransform(currentPlacementPosition, currentRotation);
    }

    //Refreshes placement validity and updates the preview visual.
    //刷新摆放有效性，并更新预览显示。
    private void ValidateCurrentPlacement(bool force)
    {
        if (!force && Time.time < nextPlacementCheckTime)
        {
            return;
        }

        nextPlacementCheckTime = Time.time + placementCheckInterval;
        previewController.SetValid(CanCommitCurrentPlacement());
    }

    //Returns whether the current placement state can be committed.
    //返回当前摆放状态是否可以提交。
    private bool CanCommitCurrentPlacement()
    {
        return currentItem != null
            && hasPlacementPosition
            && gridPlacementSystem != null
            && gridPlacementSystem.CanPlace(currentPivotCell, currentItem.Size, currentRotationSteps);
    }

    //Restores an edited building to its original transform and occupied cells.
    //将编辑中的建筑恢复到原始位置、朝向和占用格。
    private void RestoreEditedBuilding()
    {
        previewController.RestoreVisual();
        previewController.RestoreEditingColliders();
        placementCommitter.Restore(editingBuilding, currentItem, originalPivotCell, originalRotationSteps, originalPosition, originalRotation);
        ClearPlacementState();
    }

    //Clears transient placement state after completion, cancel, or delete.
    //在完成、取消或删除后清理临时摆放状态。
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
