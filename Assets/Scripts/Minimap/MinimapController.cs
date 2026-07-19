using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class MinimapController : MonoBehaviour
{
    [Header("World")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject mapPlane;
    [SerializeField] private Camera minimapCamera;

    [Header("UI")]
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform mapImageRect;
    [SerializeField] private RawImage mapImage;
    [SerializeField] private RectTransform playerIndicator;

    [Header("Map")]
    [SerializeField, Min(1f)] private float visibleWorldDiameter = 60f;
    [Tooltip("小地图缩放比例。1 为默认视野，数值越大地图越放大，玩家移动时底图位移也越明显。")]
    [SerializeField, Min(0.01f)] private float minimapScale = 1f;
    [SerializeField] private bool autoResolvePlayer = true;
    [SerializeField] private bool clampPlayerToMap = true;
    [SerializeField] private int renderTextureSize = 1024;
    [SerializeField] private float cameraHeight = 120f;

    private RenderTexture runtimeTexture;
    private Bounds mapBounds;
    private bool hasMapBounds;
    private Vector3 externallyProvidedPlayerPosition;
    private bool useExternalPlayerPosition;

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

    private void OnEnable()
    {
        ResolveReferences();
        EnsureRenderTexture();
        RefreshMapBounds();
        UpdateMinimap();
    }

    private void OnDisable()
    {
        ReleaseRuntimeTexture();
    }

    private void OnValidate()
    {
        renderTextureSize = Mathf.Max(64, renderTextureSize);
        visibleWorldDiameter = Mathf.Max(1f, visibleWorldDiameter);
        minimapScale = Mathf.Max(0.01f, minimapScale);
        ResolveReferences();
        RefreshMapBounds();
        UpdateMinimap();
    }

    private void LateUpdate()
    {
        UpdateMinimap();
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
        if (hasMapBounds)
        {
            ConfigureCameraForWholeMap();
            ResizeMapImageToBounds();
        }
    }

    private void ResolveReferences()
    {
        if (viewport == null)
        {
            viewport = transform as RectTransform;
        }

        if (mapImage == null)
        {
            mapImage = GetComponentInChildren<RawImage>(true);
        }

        if (mapImageRect == null && mapImage != null)
        {
            mapImageRect = mapImage.rectTransform;
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

    private void UpdateMinimap()
    {
        ResolveReferences();
        EnsureRenderTexture();

        if (!hasMapBounds)
        {
            RefreshMapBounds();
        }

        if (!hasMapBounds || mapImageRect == null || viewport == null)
        {
            return;
        }

        Vector3 playerPosition = GetPlayerPosition();
        Vector2 normalizedPosition = WorldToNormalizedMapPosition(playerPosition);
        mapImageRect.anchoredPosition = GetMapOffset(normalizedPosition);

        if (playerIndicator != null)
        {
            playerIndicator.anchoredPosition = Vector2.zero;
            playerIndicator.localRotation = Quaternion.identity;
        }
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

        return player != null ? player.position : mapBounds.center;
    }

    private Vector2 WorldToNormalizedMapPosition(Vector3 worldPosition)
    {
        float minX = mapBounds.min.x;
        float maxX = mapBounds.max.x;
        float minZ = mapBounds.min.z;
        float maxZ = mapBounds.max.z;

        float x = Mathf.InverseLerp(minX, maxX, worldPosition.x);
        float z = Mathf.InverseLerp(minZ, maxZ, worldPosition.z);

        if (clampPlayerToMap)
        {
            x = Mathf.Clamp01(x);
            z = Mathf.Clamp01(z);
        }

        return new Vector2(x, z);
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

        Vector3 center = mapBounds.center;
        minimapCamera.orthographic = true;
        minimapCamera.transform.SetPositionAndRotation(
            new Vector3(center.x, mapBounds.max.y + cameraHeight, center.z),
            Quaternion.Euler(90f, 0f, 0f)
        );
        minimapCamera.orthographicSize = Mathf.Max(mapBounds.size.x, mapBounds.size.z) * 0.5f;
    }

    private void ResizeMapImageToBounds()
    {
        if (viewport == null || mapImageRect == null)
        {
            return;
        }

        float viewportDiameter = Mathf.Min(viewport.rect.width, viewport.rect.height);
        if (viewportDiameter <= 0f)
        {
            viewportDiameter = Mathf.Min(viewport.sizeDelta.x, viewport.sizeDelta.y);
        }

        viewportDiameter = Mathf.Max(1f, viewportDiameter);
        float pixelsPerWorldUnit = viewportDiameter / visibleWorldDiameter * minimapScale;
        Vector2 size = new(
            Mathf.Max(1f, mapBounds.size.x * pixelsPerWorldUnit),
            Mathf.Max(1f, mapBounds.size.z * pixelsPerWorldUnit)
        );

        mapImageRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapImageRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapImageRect.pivot = new Vector2(0.5f, 0.5f);
        mapImageRect.sizeDelta = size;
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
