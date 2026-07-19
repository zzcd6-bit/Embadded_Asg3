using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MinimapController : MonoBehaviour, IPointerClickHandler
{
    public enum NorthAxis
    {
        PositiveZ,
        NegativeZ,
        PositiveX,
        NegativeX
    }

    private enum DisplayState
    {
        Minimap,
        Expanding,
        Expanded
    }

    [Serializable]
    public sealed class MapItem
    {
        public Transform target;
        public Sprite sprite;

        [NonSerialized] public RectTransform iconRect;
        [NonSerialized] public Image iconImage;
    }

    [Header("World")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject mapPlane;
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private NorthAxis planeNorthAxis = NorthAxis.PositiveZ;

    [Header("UI")]
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform mapImageRect;
    [SerializeField] private RawImage mapImage;
    [SerializeField] private RectTransform playerIndicator;
    [SerializeField] private RectTransform itemIconRoot;

    [Header("Map")]
    [SerializeField, Min(1f)] private float visibleWorldDiameter = 60f;
    [Tooltip("Minimap zoom scale. 1 is the default view; larger values zoom the map in.")]
    [SerializeField, Min(0.01f)] private float minimapScale = 1f;
    [SerializeField] private bool autoResolvePlayer = true;
    [SerializeField] private bool clampPlayerToMap = true;
    [SerializeField] private int renderTextureSize = 1024;
    [SerializeField] private float cameraHeight = 120f;

    [Header("Expand")]
    [SerializeField] private bool clickToExpand = true;
    [SerializeField] private KeyCode closeExpandedMapKey = KeyCode.Escape;
    [SerializeField, Min(0.1f)] private float maskExpandDuration = 2f;
    [SerializeField, Min(0.1f)] private float contentMoveStartTime = 1f;
    [SerializeField, Min(0.1f)] private float contentMoveEndTime = 3f;
    [SerializeField, Min(1f)] private float expandedMaskScreenMultiplier = 1.25f;
    [SerializeField] private AnimationCurve expandEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Expanded Controls")]
    [SerializeField, Min(0.01f)] private float expandedInitialZoom = 1f;
    [SerializeField, Min(0.01f)] private float expandedMinZoom = 0.5f;
    [SerializeField, Min(0.01f)] private float expandedMaxZoom = 4f;
    [SerializeField, Min(0.01f)] private float expandedZoomStep = 0.15f;
    [SerializeField] private bool zoomAroundMouse = true;
    [SerializeField] private bool clampExpandedMapToScreen = true;

    [Header("Items")]
    [SerializeField] private List<MapItem> mapItems = new();
    [SerializeField] private Vector2 itemIconSize = new(24f, 24f);

    private RenderTexture runtimeTexture;
    private Bounds mapBounds;
    private bool hasMapBounds;
    private MapFrame mapFrame;
    private Vector3 externallyProvidedPlayerPosition;
    private bool useExternalPlayerPosition;

    private DisplayState displayState = DisplayState.Minimap;
    private RectTransform rootRect;
    private RectTransform rootParent;
    private Graphic clickGraphic;
    private GameModeState modeBeforeExpandedMap = GameModeState.Exploration;

    private RectTransformSnapshot minimapSnapshot;
    private Vector2 minimapSize;
    private Vector2 expandedTargetSize;
    private Vector2 expandStartPosition;
    private Vector2 expandStartSize;
    private float expandElapsed;
    private float expandedZoom = 1f;
    private bool isMiddleDragging;
    private Vector2 lastMousePosition;

    private struct MapFrame
    {
        public Vector3 center;
        public Vector3 north;
        public Vector3 east;
        public float minEast;
        public float maxEast;
        public float minNorth;
        public float maxNorth;

        public float Width => Mathf.Max(0.001f, maxEast - minEast);
        public float Height => Mathf.Max(0.001f, maxNorth - minNorth);
    }

    private struct RectTransformSnapshot
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;

        public RectTransformSnapshot(RectTransform rectTransform)
        {
            anchorMin = rectTransform.anchorMin;
            anchorMax = rectTransform.anchorMax;
            pivot = rectTransform.pivot;
            anchoredPosition = rectTransform.anchoredPosition;
            sizeDelta = rectTransform.sizeDelta;
        }

        public void Apply(RectTransform rectTransform)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }
    }

    public Transform Player
    {
        get => player;
        set
        {
            player = value;
            useExternalPlayerPosition = false;
            UpdateMinimap();
        }
    }

    public GameObject MapPlane
    {
        get => mapPlane;
        set
        {
            mapPlane = value;
            RefreshMapBounds();
            UpdateMinimap();
        }
    }

    public float MinimapScale
    {
        get => minimapScale;
        set
        {
            minimapScale = Mathf.Max(0.01f, value);
            ResizeMapImageToBounds();
            UpdateMinimap();
        }
    }

    public NorthAxis PlaneNorthAxis
    {
        get => planeNorthAxis;
        set
        {
            planeNorthAxis = value;
            RefreshMapBounds();
            UpdateMinimap();
        }
    }

    private bool IsExpandedDisplay => displayState != DisplayState.Minimap;

    private void OnEnable()
    {
        ResolveReferences(true);
        EnsureRenderTexture();
        RefreshMapBounds();
        UpdateMinimap();
    }

    private void OnDisable()
    {
        if (displayState != DisplayState.Minimap)
        {
            ExitExpandedMap(false);
        }

        ReleaseRuntimeTexture();
    }

    private void OnValidate()
    {
        renderTextureSize = Mathf.Max(64, renderTextureSize);
        visibleWorldDiameter = Mathf.Max(1f, visibleWorldDiameter);
        minimapScale = Mathf.Max(0.01f, minimapScale);
        expandedInitialZoom = Mathf.Max(0.01f, expandedInitialZoom);
        expandedMinZoom = Mathf.Max(0.01f, expandedMinZoom);
        expandedMaxZoom = Mathf.Max(expandedMinZoom, expandedMaxZoom);
        expandedZoomStep = Mathf.Max(0.01f, expandedZoomStep);
        contentMoveEndTime = Mathf.Max(contentMoveStartTime + 0.01f, contentMoveEndTime);
        itemIconSize.x = Mathf.Max(1f, itemIconSize.x);
        itemIconSize.y = Mathf.Max(1f, itemIconSize.y);

        ResolveReferences(false);
        RefreshMapBounds();
    }

    private void LateUpdate()
    {
        if (displayState == DisplayState.Expanding)
        {
            UpdateExpandAnimation();
        }
        else if (displayState == DisplayState.Expanded)
        {
            UpdateExpandedInput();
        }

        UpdateMinimap();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!clickToExpand ||
            displayState != DisplayState.Minimap ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        EnterExpandedMap();
    }

    public void SetPlayerWorldPosition(Vector3 worldPosition)
    {
        externallyProvidedPlayerPosition = worldPosition;
        useExternalPlayerPosition = true;
        UpdateMinimap();
    }

    public void ClearExternalPlayerWorldPosition()
    {
        useExternalPlayerPosition = false;
        UpdateMinimap();
    }

    public void RefreshMapBounds()
    {
        hasMapBounds = TryGetMapBounds(mapPlane, out mapBounds);
        if (!hasMapBounds)
        {
            return;
        }

        mapFrame = BuildMapFrame(mapBounds);
        ConfigureCameraForWholeMap();
        ResizeMapImageToBounds();
    }

    public void EnterExpandedMap()
    {
        ResolveReferences(true);
        if (rootRect == null || rootParent == null || !hasMapBounds)
        {
            return;
        }

        modeBeforeExpandedMap = GameModeManager.Instance.CurrentMode;
        if (!GameModeManager.Instance.RequestMode(GameModeState.Menu, this))
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        displayState = DisplayState.Expanding;
        expandElapsed = 0f;
        isMiddleDragging = false;
        expandedZoom = Mathf.Clamp(expandedInitialZoom, expandedMinZoom, expandedMaxZoom);

        minimapSnapshot = new RectTransformSnapshot(rootRect);
        minimapSize = rootRect.rect.size;

        Vector3[] corners = new Vector3[4];
        rootRect.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;

        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.position = worldCenter;

        expandStartPosition = rootRect.anchoredPosition;
        expandStartSize = rootRect.rect.size;
        expandedTargetSize = GetExpandedMaskSize();

        ResizeMapImageToBounds();
        mapImageRect.anchoredPosition = GetMapOffset(WorldToNormalizedMapPosition(GetPlayerPosition()));
        ClampExpandedMapPosition();
        UpdateItemIcons();
        UpdatePlayerIndicator();
    }

    public void ExitExpandedMap(bool exitMenuMode = true)
    {
        displayState = DisplayState.Minimap;
        expandElapsed = 0f;
        isMiddleDragging = false;
        expandedZoom = 1f;

        if (rootRect != null)
        {
            minimapSnapshot.Apply(rootRect);
        }

        ResizeMapImageToBounds();
        UpdateMinimap();

        if (exitMenuMode && GameModeManager.Instance.IsCurrentMode(GameModeState.Menu))
        {
            GameModeManager.Instance.ExitMode(GameModeState.Menu, modeBeforeExpandedMap);
        }
    }

    private void ResolveReferences(bool canCreateRuntimeObjects)
    {
        rootRect = transform as RectTransform;
        rootParent = rootRect != null ? rootRect.parent as RectTransform : null;

        if (viewport == null)
        {
            viewport = rootRect;
        }

        if (mapImage == null)
        {
            mapImage = GetComponentInChildren<RawImage>(true);
        }

        if (mapImageRect == null && mapImage != null)
        {
            mapImageRect = mapImage.rectTransform;
        }

        if (itemIconRoot == null && mapImageRect != null)
        {
            Transform existingRoot = mapImageRect.Find("Item Icons");
            itemIconRoot = existingRoot as RectTransform;
            if (itemIconRoot == null && canCreateRuntimeObjects)
            {
                itemIconRoot = CreateItemIconRoot(mapImageRect);
            }
        }

        if (playerIndicator == null)
        {
            Transform indicator = transform.Find("Player Indicator");
            if (indicator != null)
            {
                playerIndicator = indicator as RectTransform;
            }
        }

        if (minimapCamera == null)
        {
            Transform cameraTransform = transform.Find("Minimap Camera");
            if (cameraTransform != null)
            {
                minimapCamera = cameraTransform.GetComponent<Camera>();
            }
        }

        if (autoResolvePlayer && player == null)
        {
            ResolvePlayer();
        }

        clickGraphic = GetComponent<Graphic>();
        if (clickGraphic != null)
        {
            clickGraphic.raycastTarget = clickToExpand;
        }
    }

    private void ResolvePlayer()
    {
        if (PlayerController.instance != null)
        {
            player = PlayerController.instance.transform;
            return;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            player = taggedPlayer.transform;
            return;
        }

        ActionPlayerController actionPlayer = FindAnyObjectByType<ActionPlayerController>(FindObjectsInactive.Include);
        if (actionPlayer != null)
        {
            player = actionPlayer.transform;
        }
    }

    private void EnsureRenderTexture()
    {
        if (minimapCamera == null || mapImage == null)
        {
            return;
        }

        if (runtimeTexture != null &&
            runtimeTexture.width == renderTextureSize &&
            runtimeTexture.height == renderTextureSize)
        {
            minimapCamera.targetTexture = runtimeTexture;
            mapImage.texture = runtimeTexture;
            return;
        }

        ReleaseRuntimeTexture();

        runtimeTexture = new RenderTexture(renderTextureSize, renderTextureSize, 16, RenderTextureFormat.ARGB32)
        {
            name = "Minimap Runtime Texture",
            useMipMap = false,
            autoGenerateMips = false
        };
        runtimeTexture.Create();

        minimapCamera.targetTexture = runtimeTexture;
        mapImage.texture = runtimeTexture;
    }

    private void ReleaseRuntimeTexture()
    {
        if (minimapCamera != null && minimapCamera.targetTexture == runtimeTexture)
        {
            minimapCamera.targetTexture = null;
        }

        if (mapImage != null && mapImage.texture == runtimeTexture)
        {
            mapImage.texture = null;
        }

        if (runtimeTexture == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(runtimeTexture);
        }
        else
        {
            DestroyImmediate(runtimeTexture);
        }

        runtimeTexture = null;
    }

    private void UpdateExpandAnimation()
    {
        expandElapsed += Time.unscaledDeltaTime;

        float maskProgress = Ease01(expandElapsed / maskExpandDuration);
        rootRect.sizeDelta = Vector2.LerpUnclamped(expandStartSize, expandedTargetSize, maskProgress);

        float moveProgress = Mathf.InverseLerp(contentMoveStartTime, contentMoveEndTime, expandElapsed);
        moveProgress = Ease01(moveProgress);
        rootRect.anchoredPosition = Vector2.LerpUnclamped(expandStartPosition, Vector2.zero, moveProgress);

        ResizeMapImageToBounds();
        ClampExpandedMapPosition();

        if (expandElapsed >= contentMoveEndTime)
        {
            displayState = DisplayState.Expanded;
            rootRect.sizeDelta = expandedTargetSize;
            rootRect.anchoredPosition = Vector2.zero;
            ResizeMapImageToBounds();
            ClampExpandedMapPosition();
        }
    }

    private void UpdateExpandedInput()
    {
        if (Input.GetKeyDown(closeExpandedMapKey))
        {
            ExitExpandedMap();
            return;
        }

        if (Input.GetMouseButtonDown(2))
        {
            isMiddleDragging = true;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(2))
        {
            isMiddleDragging = false;
        }

        if (isMiddleDragging)
        {
            Vector2 mousePosition = Input.mousePosition;
            Vector2 delta = mousePosition - lastMousePosition;
            lastMousePosition = mousePosition;
            mapImageRect.anchoredPosition += ScreenDeltaToLocalDelta(delta);
            ClampExpandedMapPosition();
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            ApplyExpandedZoom(scroll);
        }
    }

    private void UpdateMinimap()
    {
        ResolveReferences(true);
        EnsureRenderTexture();

        if (!hasMapBounds)
        {
            RefreshMapBounds();
        }

        if (!hasMapBounds || mapImageRect == null || viewport == null)
        {
            return;
        }

        if (displayState == DisplayState.Minimap)
        {
            Vector3 playerPosition = GetPlayerPosition();
            Vector2 normalizedPosition = WorldToNormalizedMapPosition(playerPosition);
            mapImageRect.anchoredPosition = GetMapOffset(normalizedPosition);
        }

        UpdateItemIcons();
        UpdatePlayerIndicator();
    }

    private Vector3 GetPlayerPosition()
    {
        if (useExternalPlayerPosition)
        {
            return externallyProvidedPlayerPosition;
        }

        if (player == null && autoResolvePlayer)
        {
            ResolvePlayer();
        }

        return player != null ? player.position : mapFrame.center;
    }

    private Vector2 WorldToNormalizedMapPosition(Vector3 worldPosition)
    {
        Vector3 offset = worldPosition - mapFrame.center;
        float east = Vector3.Dot(offset, mapFrame.east);
        float north = Vector3.Dot(offset, mapFrame.north);

        float x = Mathf.InverseLerp(mapFrame.minEast, mapFrame.maxEast, east);
        float y = Mathf.InverseLerp(mapFrame.minNorth, mapFrame.maxNorth, north);

        if (clampPlayerToMap)
        {
            x = Mathf.Clamp01(x);
            y = Mathf.Clamp01(y);
        }

        return new Vector2(x, y);
    }

    private Vector2 GetMapOffset(Vector2 normalizedPosition)
    {
        Vector2 mapSize = mapImageRect.rect.size;
        return new Vector2(
            (0.5f - normalizedPosition.x) * mapSize.x,
            (0.5f - normalizedPosition.y) * mapSize.y
        );
    }

    private void ConfigureCameraForWholeMap()
    {
        if (minimapCamera == null)
        {
            return;
        }

        minimapCamera.orthographic = true;
        minimapCamera.transform.SetPositionAndRotation(
            new Vector3(mapFrame.center.x, mapBounds.max.y + cameraHeight, mapFrame.center.z),
            Quaternion.LookRotation(Vector3.down, mapFrame.north)
        );
        minimapCamera.orthographicSize = Mathf.Max(mapFrame.Width, mapFrame.Height) * 0.5f;
    }

    private void ResizeMapImageToBounds()
    {
        if (viewport == null || mapImageRect == null)
        {
            return;
        }

        Vector2 activeViewportSize = GetActiveViewportSize();
        float viewportDiameter = Mathf.Min(activeViewportSize.x, activeViewportSize.y);
        viewportDiameter = Mathf.Max(1f, viewportDiameter);

        float displayScale = minimapScale * (IsExpandedDisplay ? expandedZoom : 1f);
        float pixelsPerWorldUnit = viewportDiameter / visibleWorldDiameter * displayScale;
        Vector2 size = new(
            Mathf.Max(1f, mapFrame.Width * pixelsPerWorldUnit),
            Mathf.Max(1f, mapFrame.Height * pixelsPerWorldUnit)
        );

        mapImageRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapImageRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapImageRect.pivot = new Vector2(0.5f, 0.5f);
        mapImageRect.sizeDelta = size;

        if (itemIconRoot != null)
        {
            itemIconRoot.sizeDelta = size;
        }
    }

    private void UpdateItemIcons()
    {
        if (itemIconRoot == null || mapImageRect == null)
        {
            return;
        }

        Vector2 mapSize = mapImageRect.rect.size;
        for (int i = 0; i < mapItems.Count; i++)
        {
            MapItem item = mapItems[i];
            EnsureItemIcon(item, i);

            if (item.iconRect == null || item.iconImage == null)
            {
                continue;
            }

            bool visible = item.target != null && item.sprite != null;
            item.iconRect.gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            Vector2 normalizedPosition = WorldToNormalizedMapPosition(item.target.position);
            item.iconRect.anchoredPosition = NormalizedPositionToMapLocal(normalizedPosition, mapSize);
            item.iconRect.sizeDelta = itemIconSize;
            item.iconRect.localRotation = Quaternion.identity;
            item.iconImage.sprite = item.sprite;
        }

        HideUnusedItemIcons(mapItems.Count);
    }

    private void UpdatePlayerIndicator()
    {
        if (playerIndicator == null)
        {
            return;
        }

        playerIndicator.localRotation = Quaternion.Euler(0f, 0f, -GetPlayerSignedAngleFromNorth());

        if (!IsExpandedDisplay)
        {
            playerIndicator.anchoredPosition = Vector2.zero;
            return;
        }

        Vector2 normalizedPosition = WorldToNormalizedMapPosition(GetPlayerPosition());
        Vector2 mapLocalPosition = NormalizedPositionToMapLocal(normalizedPosition, mapImageRect.rect.size);
        playerIndicator.anchoredPosition = mapImageRect.anchoredPosition + mapLocalPosition;
    }

    private void EnsureItemIcon(MapItem item, int index)
    {
        if (itemIconRoot == null || item.iconRect != null && item.iconImage != null)
        {
            return;
        }

        string iconName = $"Map Item {index + 1}";
        Transform existingIcon = itemIconRoot.Find(iconName);
        GameObject iconObject = existingIcon != null ? existingIcon.gameObject : new GameObject(iconName);
        iconObject.layer = gameObject.layer;
        iconObject.transform.SetParent(itemIconRoot, false);

        RectTransform rectTransform = iconObject.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = iconObject.AddComponent<RectTransform>();
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = itemIconSize;

        Image image = iconObject.GetComponent<Image>();
        if (image == null)
        {
            image = iconObject.AddComponent<Image>();
        }

        image.raycastTarget = false;
        image.sprite = item.sprite;

        item.iconRect = rectTransform;
        item.iconImage = image;
    }

    private void HideUnusedItemIcons(int usedCount)
    {
        if (itemIconRoot == null)
        {
            return;
        }

        for (int i = usedCount; i < itemIconRoot.childCount; i++)
        {
            itemIconRoot.GetChild(i).gameObject.SetActive(false);
        }
    }

    private void ApplyExpandedZoom(float scroll)
    {
        Vector2 cursorLocalPoint = Vector2.zero;
        Vector2 contentCoordinate = Vector2.zero;
        Vector2 oldMapSize = mapImageRect.rect.size;

        if (zoomAroundMouse &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootRect,
                Input.mousePosition,
                null,
                out cursorLocalPoint))
        {
            contentCoordinate = new Vector2(
                oldMapSize.x > 0f ? (cursorLocalPoint.x - mapImageRect.anchoredPosition.x) / oldMapSize.x : 0f,
                oldMapSize.y > 0f ? (cursorLocalPoint.y - mapImageRect.anchoredPosition.y) / oldMapSize.y : 0f
            );
        }

        expandedZoom = Mathf.Clamp(
            expandedZoom * (1f + scroll * expandedZoomStep),
            expandedMinZoom,
            expandedMaxZoom
        );

        ResizeMapImageToBounds();

        if (zoomAroundMouse)
        {
            Vector2 newMapSize = mapImageRect.rect.size;
            mapImageRect.anchoredPosition = cursorLocalPoint - new Vector2(
                contentCoordinate.x * newMapSize.x,
                contentCoordinate.y * newMapSize.y
            );
        }

        ClampExpandedMapPosition();
    }

    private void ClampExpandedMapPosition()
    {
        if (!clampExpandedMapToScreen || mapImageRect == null)
        {
            return;
        }

        Vector2 viewportSize = GetActiveViewportSize();
        Vector2 mapSize = mapImageRect.rect.size;
        Vector2 maxOffset = new(
            Mathf.Max(0f, (mapSize.x - viewportSize.x) * 0.5f),
            Mathf.Max(0f, (mapSize.y - viewportSize.y) * 0.5f)
        );

        Vector2 position = mapImageRect.anchoredPosition;
        position.x = Mathf.Clamp(position.x, -maxOffset.x, maxOffset.x);
        position.y = Mathf.Clamp(position.y, -maxOffset.y, maxOffset.y);
        mapImageRect.anchoredPosition = position;
    }

    private Vector2 GetActiveViewportSize()
    {
        if (IsExpandedDisplay && rootParent != null)
        {
            return rootParent.rect.size;
        }

        if (viewport != null)
        {
            Vector2 size = viewport.rect.size;
            if (size.x > 0f && size.y > 0f)
            {
                return size;
            }

            return viewport.sizeDelta;
        }

        return Vector2.one;
    }

    private Vector2 GetExpandedMaskSize()
    {
        Vector2 parentSize = rootParent != null ? rootParent.rect.size : new Vector2(Screen.width, Screen.height);
        float diameter = Mathf.Sqrt(parentSize.x * parentSize.x + parentSize.y * parentSize.y) *
                         expandedMaskScreenMultiplier;
        diameter = Mathf.Max(diameter, Mathf.Max(parentSize.x, parentSize.y));
        return new Vector2(diameter, diameter);
    }

    private Vector2 ScreenDeltaToLocalDelta(Vector2 screenDelta)
    {
        if (rootRect == null)
        {
            return screenDelta;
        }

        Canvas canvas = rootRect.GetComponentInParent<Canvas>();
        float scaleFactor = canvas != null ? Mathf.Max(0.0001f, canvas.scaleFactor) : 1f;
        return screenDelta / scaleFactor;
    }

    private Vector2 NormalizedPositionToMapLocal(Vector2 normalizedPosition, Vector2 mapSize)
    {
        return new Vector2(
            (normalizedPosition.x - 0.5f) * mapSize.x,
            (normalizedPosition.y - 0.5f) * mapSize.y
        );
    }

    private float GetPlayerSignedAngleFromNorth()
    {
        if (player == null)
        {
            return 0f;
        }

        Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up);
        if (forward.sqrMagnitude <= 0.0001f)
        {
            return 0f;
        }

        return Vector3.SignedAngle(mapFrame.north, forward.normalized, Vector3.up);
    }

    private float Ease01(float value)
    {
        return expandEase != null
            ? expandEase.Evaluate(Mathf.Clamp01(value))
            : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));
    }

    private MapFrame BuildMapFrame(Bounds bounds)
    {
        Vector3 north = GetPlaneNorthDirection();
        Vector3 east = Vector3.Cross(Vector3.up, north).normalized;
        Vector3 center = bounds.center;

        float minEast = float.PositiveInfinity;
        float maxEast = float.NegativeInfinity;
        float minNorth = float.PositiveInfinity;
        float maxNorth = float.NegativeInfinity;

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        for (int x = 0; x <= 1; x++)
        {
            for (int y = 0; y <= 1; y++)
            {
                for (int z = 0; z <= 1; z++)
                {
                    Vector3 corner = new(
                        x == 0 ? min.x : max.x,
                        y == 0 ? min.y : max.y,
                        z == 0 ? min.z : max.z
                    );
                    Vector3 offset = corner - center;
                    float eastDistance = Vector3.Dot(offset, east);
                    float northDistance = Vector3.Dot(offset, north);
                    minEast = Mathf.Min(minEast, eastDistance);
                    maxEast = Mathf.Max(maxEast, eastDistance);
                    minNorth = Mathf.Min(minNorth, northDistance);
                    maxNorth = Mathf.Max(maxNorth, northDistance);
                }
            }
        }

        return new MapFrame
        {
            center = center,
            north = north,
            east = east,
            minEast = minEast,
            maxEast = maxEast,
            minNorth = minNorth,
            maxNorth = maxNorth
        };
    }

    private Vector3 GetPlaneNorthDirection()
    {
        Transform source = mapPlane != null ? mapPlane.transform : transform;
        Vector3 localAxis = planeNorthAxis switch
        {
            NorthAxis.NegativeZ => Vector3.back,
            NorthAxis.PositiveX => Vector3.right,
            NorthAxis.NegativeX => Vector3.left,
            _ => Vector3.forward
        };

        Vector3 north = Vector3.ProjectOnPlane(source.TransformDirection(localAxis), Vector3.up);
        if (north.sqrMagnitude <= 0.0001f)
        {
            north = Vector3.forward;
        }

        return north.normalized;
    }

    private RectTransform CreateItemIconRoot(RectTransform parent)
    {
        GameObject rootObject = new("Item Icons", typeof(RectTransform));
        rootObject.layer = gameObject.layer;
        rootObject.transform.SetParent(parent, false);

        RectTransform rectTransform = rootObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = parent.rect.size;
        return rectTransform;
    }

    private static bool TryGetMapBounds(GameObject source, out Bounds bounds)
    {
        if (source == null)
        {
            bounds = default;
            return false;
        }

        Renderer renderer = source.GetComponent<Renderer>();
        if (renderer != null)
        {
            bounds = renderer.bounds;
            return true;
        }

        Collider collider = source.GetComponent<Collider>();
        if (collider != null)
        {
            bounds = collider.bounds;
            return true;
        }

        bounds = new Bounds(source.transform.position, new Vector3(
            Mathf.Abs(source.transform.lossyScale.x) * 10f,
            1f,
            Mathf.Abs(source.transform.lossyScale.z) * 10f
        ));
        return true;
    }
}
