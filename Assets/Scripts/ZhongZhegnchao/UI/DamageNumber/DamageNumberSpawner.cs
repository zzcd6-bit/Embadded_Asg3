using UnityEngine;

public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner Instance { get; private set; }

    [Header("组件")]
    public Canvas canvas;
    public RectTransform canvasRect;
    public Camera worldCamera;
    public DamageNumberPopup popupPrefab;

    [Header("位置")]
    public Vector2 randomStartOffset = Vector2.zero;

    [Header("屏幕边缘限制")]
    public bool clampToCanvas = true;
    public float canvasEdgePadding = 40f;

    [Header("Debug")]
    public bool debugLog = true;

    private void Awake()
    {
        Instance = this;

        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }

        if (canvasRect == null && canvas != null)
        {
            canvasRect = canvas.rootCanvas.GetComponent<RectTransform>();
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }
    }

    public void ShowDamageNumber(
        int damage,
        Vector3 worldPosition,
        ElementType element,
        bool isCritical,
        ElementReactionType reactionType
    )
    {
        if (damage <= 0)
            return;

        if (popupPrefab == null)
        {
            Debug.LogWarning("[DamageNumberSpawner] Popup prefab is missing.", this);
            return;
        }

        if (canvas == null || canvasRect == null)
        {
            Debug.LogWarning("[DamageNumberSpawner] Canvas or CanvasRect is missing.", this);
            return;
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (worldCamera == null)
        {
            Debug.LogWarning("[DamageNumberSpawner] WorldCamera is missing.", this);
            return;
        }

        Vector3 viewportPosition = worldCamera.WorldToViewportPoint(worldPosition);

        // z < 0 代表目标在相机背后，不能显示
        if (viewportPosition.z < 0f)
        {
            if (debugLog)
            {
                Debug.Log("[DamageNumberSpawner] Target is behind camera.");
            }

            return;
        }

        Vector2 canvasPosition = ViewportToCanvasPosition(viewportPosition);

        canvasPosition.x += Random.Range(-randomStartOffset.x, randomStartOffset.x);
        canvasPosition.y += Random.Range(-randomStartOffset.y, randomStartOffset.y);

        if (clampToCanvas)
        {
            canvasPosition = ClampToCanvas(canvasPosition);
        }

        DamageNumberPopup popup = Instantiate(
            popupPrefab,
            canvasRect
        );

        RectTransform popupRect = popup.GetComponent<RectTransform>();

        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);

        popupRect.anchoredPosition = canvasPosition;
        popupRect.localScale = Vector3.one;
        popupRect.localRotation = Quaternion.identity;

        popup.transform.SetAsLastSibling();

        popup.Init(
            damage,
            element,
            isCritical,
            reactionType
        );

        if (debugLog)
        {
            Debug.Log(
                $"[DamageNumberSpawner] Damage number spawned. " +
                $"Damage={damage}, Viewport={viewportPosition}, CanvasPos={canvasPosition}",
                this
            );
        }
    }

    public void ShowDamageNumber(DamageInfo damageInfo, Vector3 worldPosition)
    {
        int damage = damageInfo.finalDamage;

        if (damage <= 0)
        {
            damage = damageInfo.damage;
        }

        ShowDamageNumber(
            damage,
            worldPosition,
            damageInfo.element,
            damageInfo.isCritical,
            damageInfo.reactionType
        );
    }

    private Vector2 ViewportToCanvasPosition(Vector3 viewportPosition)
    {
        Rect rect = canvasRect.rect;

        float x = (viewportPosition.x - 0.5f) * rect.width;
        float y = (viewportPosition.y - 0.5f) * rect.height;

        return new Vector2(x, y);
    }

    private Vector2 ClampToCanvas(Vector2 position)
    {
        Rect rect = canvasRect.rect;

        float halfWidth = rect.width * 0.5f;
        float halfHeight = rect.height * 0.5f;

        position.x = Mathf.Clamp(
            position.x,
            -halfWidth + canvasEdgePadding,
            halfWidth - canvasEdgePadding
        );

        position.y = Mathf.Clamp(
            position.y,
            -halfHeight + canvasEdgePadding,
            halfHeight - canvasEdgePadding
        );

        return position;
    }
}