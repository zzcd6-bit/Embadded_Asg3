using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class PlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera placementCamera;
    [SerializeField] private GridPlacementSystem gridPlacementSystem;
    [SerializeField] private GridSurfaceClassifier surfaceClassifier;
    [SerializeField] private PlacementPreviewController previewController;
    [SerializeField] private PlacementCommitter placementCommitter;

    [Header("Raycast")]
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float raycastDistance = 500f;
    [SerializeField] private bool ignorePointerOverUI = true;
    [SerializeField] private bool useScreenAnchorRaycast = true;
    [SerializeField] private Vector2 screenAnchorViewport = new(0.5f, 0.58f);
    [SerializeField] private bool alignRotationToCamera = true;

    [Header("Bridge Anchor Focus")]
    [SerializeField] private bool focusAnchorWhenBridgeHasGap = true;
    [SerializeField, Min(0f)] private float bridgeHeightLayerTolerance = 0.75f;

    [Header("Validation")]
    [SerializeField] private float placementCheckInterval = 0.1f;

    [Header("Ground Alignment")]
    [SerializeField] private bool alignToHighestGroundPoint = true;
    [SerializeField] private float surfaceGap = 0.02f;
    [SerializeField] private float fallbackGroundSampleHeight = 40f;

    [Header("NavMesh Placement")]
    [SerializeField] private bool requireNavMeshSurface = true;
    [SerializeField] private float navMeshSampleDistance = 0.45f;
    [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;

    private BuildableItemData currentItem;
    private Quaternion currentRotation = Quaternion.identity;
    private Vector3 currentPlacementPosition;
    private Vector2Int currentPivotCell;
    private int currentRotationSteps;
    private int manualRotationOffsetSteps;
    private bool hasPlacementPosition;
    private float nextPlacementCheckTime;
    private int placementStartedFrame = -1;
    private BuildingInstance editingBuilding;
    private Vector2Int originalPivotCell;
    private int originalRotationSteps;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private readonly List<Vector2Int> validationCells = new();
    private readonly List<Vector2Int> anchorStartCells = new();
    private readonly List<Vector2Int> anchorEndCells = new();
    private readonly List<Vector2Int> bridgeFootprintCells = new();

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

        if (surfaceClassifier == null)
        {
            surfaceClassifier = FindAnyObjectByType<GridSurfaceClassifier>();
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

        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
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
        manualRotationOffsetSteps = 0;
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
        manualRotationOffsetSteps = NormalizeRotationSteps(currentRotationSteps - GetCameraRotationSteps());
        currentRotation = Quaternion.Euler(0f, building.transform.eulerAngles.y, 0f);
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
        manualRotationOffsetSteps = NormalizeRotationSteps(manualRotationOffsetSteps + 1);
        UpdatePlacementRotationFromCamera();

        if (hasPlacementPosition && gridPlacementSystem != null && currentItem != null)
        {
            currentPivotCell = gridPlacementSystem.WorldToPivotCell(currentPlacementPosition, currentItem.Size, currentRotationSteps);
            currentPlacementPosition = gridPlacementSystem.PivotCellToWorld(currentPivotCell, currentItem.Size, currentRotationSteps);
            AlignCurrentPlacementToGround();
            previewController.SetTransform(currentPlacementPosition, currentRotation);
        }

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

    public bool TryGetCurrentPlacementCells(List<Vector2Int> cells)
    {
        if (cells == null || currentItem == null || gridPlacementSystem == null || !hasPlacementPosition)
        {
            return false;
        }

        cells.Clear();
        cells.AddRange(gridPlacementSystem.GetOccupiedCells(currentPivotCell, currentItem.Size, currentRotationSteps));
        return cells.Count > 0;
    }

    public bool TryGetCurrentPreviewCells(List<Vector2Int> cells, out bool isValid)
    {
        isValid = false;
        if (cells == null || currentItem == null || gridPlacementSystem == null || !hasPlacementPosition)
        {
            return false;
        }

        cells.Clear();
        isValid = CanCommitCurrentPlacement();

        if (!isValid || !UsesAnchorSurfaceRule())
        {
            cells.AddRange(gridPlacementSystem.GetOccupiedCells(currentPivotCell, currentItem.Size, currentRotationSteps));
            return cells.Count > 0;
        }

        GetAnchorSideCells(anchorStartCells, anchorEndCells);
        cells.AddRange(anchorStartCells);
        cells.AddRange(anchorEndCells);
        return cells.Count > 0;
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

        if (!useScreenAnchorRaycast
            && ignorePointerOverUI
            && EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        UpdatePlacementRotationFromCamera();

        Ray ray = useScreenAnchorRaycast
            ? ActiveCamera.ViewportPointToRay(new Vector3(screenAnchorViewport.x, screenAnchorViewport.y, 0f))
            : ActiveCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            if (!TrySetPreviewPositionFromWorld(hit.point))
            {
                hasPlacementPosition = false;
            }

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

    private void UpdatePlacementRotationFromCamera()
    {
        if (!alignRotationToCamera || ActiveCamera == null)
        {
            currentRotation = Quaternion.Euler(0f, currentRotation.eulerAngles.y, 0f);
            return;
        }

        currentRotationSteps = NormalizeRotationSteps(GetCameraRotationSteps() + manualRotationOffsetSteps);
        currentRotation = Quaternion.Euler(0f, currentRotationSteps * 90f, 0f);
    }

    private int GetCameraRotationSteps()
    {
        if (ActiveCamera == null)
        {
            return 0;
        }

        return NormalizeRotationSteps(Mathf.RoundToInt(ActiveCamera.transform.eulerAngles.y / 90f));
    }

    private static int NormalizeRotationSteps(int steps)
    {
        return (steps % 4 + 4) % 4;
    }

    private bool TrySetPreviewPositionFromWorld(Vector3 worldPosition)
    {
        if (requireNavMeshSurface
            && !UsesAnchorSurfaceRule()
            && !TrySampleNavMesh(worldPosition, out worldPosition))
        {
            return false;
        }

        currentPivotCell = gridPlacementSystem.WorldToPivotCell(worldPosition, currentItem.Size, currentRotationSteps);
        ApplyBridgeAnchorFocusIfNeeded(worldPosition);
        currentPlacementPosition = gridPlacementSystem.PivotCellToWorld(currentPivotCell, currentItem.Size, currentRotationSteps);
        currentRotation = Quaternion.Euler(0f, currentRotation.eulerAngles.y, 0f);

        if (requireNavMeshSurface && TrySampleNavMesh(currentPlacementPosition, out Vector3 snappedPosition))
        {
            currentPlacementPosition.y = snappedPosition.y;
        }

        AlignCurrentPlacementToGround();
        hasPlacementPosition = true;
        previewController.SetTransform(currentPlacementPosition, currentRotation);
        return true;
    }

    private void ApplyBridgeAnchorFocusIfNeeded(Vector3 focusWorldPosition)
    {
        if (!focusAnchorWhenBridgeHasGap || !UsesAnchorSurfaceRule())
        {
            return;
        }

        GetAnchorSideCells(currentPivotCell, anchorStartCells, anchorEndCells);
        bool startAnchorValid = IsAnchorSideValid(anchorStartCells);
        bool endAnchorValid = IsAnchorSideValid(anchorEndCells);
        if (!BridgeFootprintHasGapOrInvalidSurface(startAnchorValid, endAnchorValid))
        {
            return;
        }

        if (!TryChooseBridgeFocusAnchor(startAnchorValid, endAnchorValid, out bool useStartAnchor))
        {
            return;
        }

        List<Vector2Int> focusAnchorCells = useStartAnchor ? anchorStartCells : anchorEndCells;
        if (!TryGetCellCenter(focusAnchorCells, out Vector2 currentAnchorCenter))
        {
            return;
        }

        Vector2Int desiredAnchorCell = gridPlacementSystem.WorldToCell(focusWorldPosition);
        Vector2Int currentAnchorCell = new(
            Mathf.RoundToInt(currentAnchorCenter.x),
            Mathf.RoundToInt(currentAnchorCenter.y)
        );

        currentPivotCell += desiredAnchorCell - currentAnchorCell;
    }

    private bool BridgeFootprintHasGapOrInvalidSurface(bool startAnchorValid, bool endAnchorValid)
    {
        if (surfaceClassifier == null)
        {
            return false;
        }

        if (!TryGetBridgeReferenceHeights(startAnchorValid, endAnchorValid, out float startHeight, out float endHeight))
        {
            return true;
        }

        bridgeFootprintCells.Clear();
        bridgeFootprintCells.AddRange(gridPlacementSystem.GetOccupiedCells(currentPivotCell, currentItem.Size, currentRotationSteps));
        if (bridgeFootprintCells.Count == 0)
        {
            return true;
        }

        Vector2Int rotatedSize = GridPlacementSystem.GetRotatedSize(currentItem.Size, currentRotationSteps);
        bool lengthUsesX = rotatedSize.x >= rotatedSize.y;
        GetFootprintAxisRange(bridgeFootprintCells, lengthUsesX, out int minAxis, out int maxAxis);
        float axisSpan = Mathf.Max(1, maxAxis - minAxis);

        for (int i = 0; i < bridgeFootprintCells.Count; i++)
        {
            Vector2Int cell = bridgeFootprintCells[i];
            int axisValue = lengthUsesX ? cell.x : cell.y;
            float t = Mathf.Clamp01((axisValue - minAxis) / axisSpan);
            float expectedHeight = Mathf.Lerp(startHeight, endHeight, t);

            if (!surfaceClassifier.TryGetCellSurface(cell, out CellSurfaceInfo info)
                || !info.HasSurface
                || !info.IsFullyOnSurface
                || info.HasSplit)
            {
                return true;
            }

            if (Mathf.Abs(info.RepresentativeY - expectedHeight) > bridgeHeightLayerTolerance)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetBridgeReferenceHeights(bool startAnchorValid, bool endAnchorValid, out float startHeight, out float endHeight)
    {
        startHeight = 0f;
        endHeight = 0f;

        bool hasStartHeight = startAnchorValid && TryGetAverageGroundY(anchorStartCells, out startHeight);
        bool hasEndHeight = endAnchorValid && TryGetAverageGroundY(anchorEndCells, out endHeight);

        if (hasStartHeight && hasEndHeight)
        {
            return true;
        }

        if (hasStartHeight)
        {
            endHeight = startHeight;
            return true;
        }

        if (hasEndHeight)
        {
            startHeight = endHeight;
            return true;
        }

        return false;
    }

    private bool TryChooseBridgeFocusAnchor(bool startAnchorValid, bool endAnchorValid, out bool useStartAnchor)
    {
        useStartAnchor = true;

        if (startAnchorValid && !endAnchorValid)
        {
            return true;
        }

        if (!startAnchorValid && endAnchorValid)
        {
            useStartAnchor = false;
            return true;
        }

        if (!startAnchorValid && !endAnchorValid)
        {
            return false;
        }

        Vector3 cameraPosition = ActiveCamera != null ? ActiveCamera.transform.position : currentPlacementPosition;
        float startDistance = TryGetWorldCenter(anchorStartCells, out Vector3 startCenter)
            ? Vector3.SqrMagnitude(startCenter - cameraPosition)
            : float.MaxValue;
        float endDistance = TryGetWorldCenter(anchorEndCells, out Vector3 endCenter)
            ? Vector3.SqrMagnitude(endCenter - cameraPosition)
            : float.MaxValue;

        useStartAnchor = startDistance <= endDistance;
        return true;
    }

    private bool TryGetAverageGroundY(List<Vector2Int> cells, out float averageY)
    {
        averageY = 0f;
        if (cells == null || cells.Count == 0)
        {
            return false;
        }

        float sum = 0f;
        int count = 0;
        for (int i = 0; i < cells.Count; i++)
        {
            if (!TryGetCellHighestGroundY(cells[i], out float groundY))
            {
                continue;
            }

            sum += groundY;
            count++;
        }

        if (count == 0)
        {
            return false;
        }

        averageY = sum / count;
        return true;
    }

    private bool TryGetWorldCenter(List<Vector2Int> cells, out Vector3 center)
    {
        center = Vector3.zero;
        if (cells == null || cells.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            center += gridPlacementSystem.CellToWorld(cells[i]);
        }

        center /= cells.Count;
        return true;
    }

    private static bool TryGetCellCenter(List<Vector2Int> cells, out Vector2 center)
    {
        center = Vector2.zero;
        if (cells == null || cells.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            center += cells[i];
        }

        center /= cells.Count;
        return true;
    }

    private static void GetFootprintAxisRange(List<Vector2Int> cells, bool useXAxis, out int minAxis, out int maxAxis)
    {
        minAxis = int.MaxValue;
        maxAxis = int.MinValue;

        for (int i = 0; i < cells.Count; i++)
        {
            int axisValue = useXAxis ? cells[i].x : cells[i].y;
            minAxis = Mathf.Min(minAxis, axisValue);
            maxAxis = Mathf.Max(maxAxis, axisValue);
        }
    }

    private bool UsesAnchorSurfaceRule()
    {
        return currentItem != null && currentItem.SurfaceRule == PlacementSurfaceRule.AnchorsOnly;
    }

    private void AlignCurrentPlacementToGround()
    {
        if (!alignToHighestGroundPoint || previewController == null || !previewController.HasPreview)
        {
            return;
        }

        if (!TryGetHighestFootprintGroundY(out float highestGroundY))
        {
            return;
        }

        previewController.SetTransform(currentPlacementPosition, currentRotation);
        if (!previewController.TryGetWorldBounds(out Bounds previewBounds))
        {
            currentPlacementPosition.y = highestGroundY + surfaceGap + currentItem.PlacementYOffset;
            previewController.SetTransform(currentPlacementPosition, currentRotation);
            return;
        }

        float bottomOffset = currentPlacementPosition.y - previewBounds.min.y;
        currentPlacementPosition.y = highestGroundY + bottomOffset + surfaceGap + currentItem.PlacementYOffset;
        previewController.SetTransform(currentPlacementPosition, currentRotation);
    }

    private bool TryGetHighestFootprintGroundY(out float highestGroundY)
    {
        highestGroundY = float.MinValue;
        bool foundGround = false;

        GetGroundAlignmentCells(validationCells);
        foreach (Vector2Int cell in validationCells)
        {
            if (!TryGetCellHighestGroundY(cell, out float cellGroundY))
            {
                continue;
            }

            highestGroundY = Mathf.Max(highestGroundY, cellGroundY);
            foundGround = true;
        }

        return foundGround;
    }

    private bool TryGetCellHighestGroundY(Vector2Int cell, out float highestGroundY)
    {
        if (surfaceClassifier != null
            && surfaceClassifier.TryGetCellSurface(cell, out CellSurfaceInfo info)
            && info.HasSurface)
        {
            highestGroundY = info.MaxY;
            return true;
        }

        Vector3 cellWorld = gridPlacementSystem.CellToWorld(cell);
        Vector3 rayOrigin = cellWorld + Vector3.up * fallbackGroundSampleHeight;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            highestGroundY = hit.point.y;
            return true;
        }

        highestGroundY = cellWorld.y;
        return false;
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
            && gridPlacementSystem.CanPlace(currentPivotCell, currentItem.Size, currentRotationSteps)
            && IsCurrentPlacementSurfaceValid();
    }

    private bool IsCurrentPlacementSurfaceValid()
    {
        if (UsesAnchorSurfaceRule())
        {
            GetAnchorSideCells(anchorStartCells, anchorEndCells);
            return IsAnchorSideValid(anchorStartCells) || IsAnchorSideValid(anchorEndCells);
        }

        return IsCurrentFootprintOnNavMesh() && IsCurrentFootprintBuildableSurface();
    }

    private bool IsCurrentFootprintOnNavMesh()
    {
        if (!requireNavMeshSurface)
        {
            return true;
        }

        GetSurfaceValidationCells(validationCells);
        foreach (Vector2Int cell in validationCells)
        {
            if (!TrySampleNavMesh(gridPlacementSystem.CellToWorld(cell), out _))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsAnchorSideValid(List<Vector2Int> cells)
    {
        return cells.Count > 0
            && AreCellsOnNavMesh(cells)
            && AreCellsBuildableSurface(cells)
            && AreAnchorHeightsAllowed(cells);
    }

    private bool AreCellsOnNavMesh(List<Vector2Int> cells)
    {
        if (!requireNavMeshSurface)
        {
            return true;
        }

        foreach (Vector2Int cell in cells)
        {
            if (!TrySampleNavMesh(gridPlacementSystem.CellToWorld(cell), out _))
            {
                return false;
            }
        }

        return true;
    }

    private bool AreCellsBuildableSurface(List<Vector2Int> cells)
    {
        if (surfaceClassifier == null)
        {
            return true;
        }

        foreach (Vector2Int cell in cells)
        {
            if (!surfaceClassifier.IsCellBuildableSurface(cell))
            {
                return false;
            }
        }

        return true;
    }

    private bool AreAnchorHeightsAllowed(List<Vector2Int> cells)
    {
        if (currentItem == null
            || currentItem.SurfaceRule != PlacementSurfaceRule.AnchorsOnly
            || currentItem.MaxAnchorHeightDelta <= 0f)
        {
            return true;
        }

        bool foundGround = false;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (Vector2Int cell in cells)
        {
            if (!TryGetCellHighestGroundY(cell, out float groundY))
            {
                return false;
            }

            minY = Mathf.Min(minY, groundY);
            maxY = Mathf.Max(maxY, groundY);
            foundGround = true;
        }

        return foundGround && maxY - minY <= currentItem.MaxAnchorHeightDelta;
    }

    private bool IsCurrentFootprintBuildableSurface()
    {
        if (surfaceClassifier == null)
        {
            return true;
        }

        GetSurfaceValidationCells(validationCells);
        foreach (Vector2Int cell in validationCells)
        {
            if (!surfaceClassifier.IsCellBuildableSurface(cell))
            {
                return false;
            }
        }

        return true;
    }

    private void GetGroundAlignmentCells(List<Vector2Int> cells)
    {
        if (!UsesAnchorSurfaceRule())
        {
            GetSurfaceValidationCells(cells);
            return;
        }

        GetAnchorSideCells(anchorStartCells, anchorEndCells);
        cells.Clear();

        if (IsAnchorSideValid(anchorStartCells))
        {
            cells.AddRange(anchorStartCells);
            return;
        }

        if (IsAnchorSideValid(anchorEndCells))
        {
            cells.AddRange(anchorEndCells);
            return;
        }

        cells.AddRange(anchorStartCells);
        cells.AddRange(anchorEndCells);
    }

    private void GetSurfaceValidationCells(List<Vector2Int> cells)
    {
        cells.Clear();
        if (currentItem == null || gridPlacementSystem == null)
        {
            return;
        }

        IReadOnlyList<Vector2Int> occupiedCells = gridPlacementSystem.GetOccupiedCells(
            currentPivotCell,
            currentItem.Size,
            currentRotationSteps
        );

        if (currentItem.SurfaceRule != PlacementSurfaceRule.AnchorsOnly)
        {
            cells.AddRange(occupiedCells);
            return;
        }

        GetAnchorSideCells(anchorStartCells, anchorEndCells);
        cells.AddRange(anchorStartCells);
        cells.AddRange(anchorEndCells);
    }

    private void GetAnchorSideCells(List<Vector2Int> startCells, List<Vector2Int> endCells)
    {
        GetAnchorSideCells(currentPivotCell, startCells, endCells);
    }

    private void GetAnchorSideCells(Vector2Int pivotCell, List<Vector2Int> startCells, List<Vector2Int> endCells)
    {
        startCells.Clear();
        endCells.Clear();
        if (currentItem == null || gridPlacementSystem == null)
        {
            return;
        }

        IReadOnlyList<Vector2Int> occupiedCells = gridPlacementSystem.GetOccupiedCells(
            pivotCell,
            currentItem.Size,
            currentRotationSteps
        );

        AddAnchorSideCells(occupiedCells, startCells, endCells);
    }

    private void AddAnchorSideCells(IReadOnlyList<Vector2Int> occupiedCells, List<Vector2Int> startCells, List<Vector2Int> endCells)
    {
        if (occupiedCells == null || occupiedCells.Count == 0)
        {
            return;
        }

        Vector2Int rotatedSize = GridPlacementSystem.GetRotatedSize(currentItem.Size, currentRotationSteps);
        bool useXAxis = rotatedSize.x >= rotatedSize.y;
        int anchorDepth = Mathf.Min(currentItem.AnchorDepth, useXAxis ? rotatedSize.x : rotatedSize.y);

        int minAxis = int.MaxValue;
        int maxAxis = int.MinValue;
        for (int i = 0; i < occupiedCells.Count; i++)
        {
            int axisValue = useXAxis ? occupiedCells[i].x : occupiedCells[i].y;
            minAxis = Mathf.Min(minAxis, axisValue);
            maxAxis = Mathf.Max(maxAxis, axisValue);
        }

        int minAnchorLimit = minAxis + anchorDepth - 1;
        int maxAnchorLimit = maxAxis - anchorDepth + 1;
        for (int i = 0; i < occupiedCells.Count; i++)
        {
            Vector2Int cell = occupiedCells[i];
            int axisValue = useXAxis ? cell.x : cell.y;
            if (axisValue <= minAnchorLimit)
            {
                startCells.Add(cell);
            }

            if (axisValue >= maxAnchorLimit)
            {
                endCells.Add(cell);
            }
        }
    }

    private bool TrySampleNavMesh(Vector3 position, out Vector3 navMeshPosition)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, navMeshSampleDistance, navMeshAreaMask))
        {
            navMeshPosition = hit.position;
            return true;
        }

        navMeshPosition = position;
        return false;
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
        manualRotationOffsetSteps = 0;
        previewController.ClearReferences();
    }
}
