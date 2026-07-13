using UnityEngine;

public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner Instance { get; private set; }

    [Header("Canvas")]
    public Canvas canvas;
    public RectTransform canvasRect;
    public Camera worldCamera;

    [Header("Pool")]
    public bool usePool = true;

    [Tooltip("Resources 路径，例如 Prefabs/DamageNumberPopup 或 UI/DamageNumberPopup")]
    public string popupPoolName = "Prefabs/DamageNumberPopup";

    [Header("Fallback")]
    public DamageNumberPopup popupPrefab;

    [Header("Position")]
    public Vector2 randomStartOffset = new Vector2(30f, 20f);
    public bool clampToCanvas = true;
    public float canvasEdgePadding = 40f;

    [Header("Debug")]
    public bool debugLog = false;

    private void Awake()
    {
        Instance = this;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvasRect == null && canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    public void ShowDamageNumber(
        DamageInfo damageInfo,
        Vector3 worldPosition
    )
    {
        DamageNumberPopup popup = GetPopup();

        if (popup == null)
            return;

        bool positionValid = SetupPopupPosition(
            popup,
            worldPosition
        );

        if (!positionValid)
        {
            RecyclePopup(popup.gameObject);
            return;
        }

        EnsurePopupActive(popup);

        popup.Init(damageInfo);
    }

    public void ShowDamageNumber(
        int damage,
        Vector3 worldPosition
    )
    {
        DamageNumberPopup popup = GetPopup();

        if (popup == null)
            return;

        bool positionValid = SetupPopupPosition(
            popup,
            worldPosition
        );

        if (!positionValid)
        {
            RecyclePopup(popup.gameObject);
            return;
        }

        EnsurePopupActive(popup);

        popup.Init(
            damage,
            ElementType.Physical,
            false,
            ElementReactionType.None
        );
    }

    private DamageNumberPopup GetPopup()
    {
        GameObject obj = null;

        if (usePool)
        {
            if (string.IsNullOrEmpty(popupPoolName))
            {
                Debug.LogWarning(
                    "[DamageNumberSpawner] Popup Pool Name is empty.",
                    this
                );

                return null;
            }

            obj = PoolMgr.Instance.GetObj(popupPoolName);
        }
        else
        {
            if (popupPrefab == null)
            {
                Debug.LogWarning(
                    "[DamageNumberSpawner] Popup Prefab is missing.",
                    this
                );

                return null;
            }

            obj = Instantiate(popupPrefab.gameObject);
        }

        if (obj == null)
            return null;

        // 重点：从对象池拿出来后，先确保激活
        if (!obj.activeSelf)
            obj.SetActive(true);

        obj.transform.SetParent(canvasRect, false);
        obj.transform.localScale = Vector3.one;
        obj.transform.SetAsLastSibling();

        DamageNumberPopup popup =
            obj.GetComponent<DamageNumberPopup>();

        if (popup == null)
        {
            popup = obj.GetComponentInChildren<DamageNumberPopup>(true);
        }

        if (popup == null)
        {
            Debug.LogWarning(
                "[DamageNumberSpawner] DamageNumberPopup component not found.",
                obj
            );

            obj.SetActive(false);
            return null;
        }

        // 如果 DamageNumberPopup 挂在子物体上，也确保它是 active
        if (!popup.gameObject.activeSelf)
            popup.gameObject.SetActive(true);

        popup.OnRecycleRequested = RecyclePopup;

        return popup;
    }

    private void EnsurePopupActive(DamageNumberPopup popup)
    {
        if (popup == null)
            return;

        Transform root = popup.transform;

        if (!root.gameObject.activeSelf)
            root.gameObject.SetActive(true);

        if (!popup.gameObject.activeSelf)
            popup.gameObject.SetActive(true);
    }

    private bool SetupPopupPosition(
        DamageNumberPopup popup,
        Vector3 worldPosition
    )
    {
        if (popup == null)
            return false;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null)
            return false;

        if (canvasRect == null)
            return false;

        RectTransform popupRect =
            popup.GetComponent<RectTransform>();

        if (popupRect == null)
            return false;

        Vector3 viewportPosition =
            worldCamera.WorldToViewportPoint(worldPosition);

        // 重点：不要在这里 SetActive(false) 后又继续 Init
        if (viewportPosition.z < 0f)
        {
            if (debugLog)
            {
                Debug.Log(
                    "[DamageNumberSpawner] Damage number is behind camera.",
                    this
                );
            }

            return false;
        }

        Rect rect = canvasRect.rect;

        float x = (viewportPosition.x - 0.5f) * rect.width;
        float y = (viewportPosition.y - 0.5f) * rect.height;

        if (randomStartOffset != Vector2.zero)
        {
            x += Random.Range(
                -randomStartOffset.x,
                randomStartOffset.x
            );

            y += Random.Range(
                -randomStartOffset.y,
                randomStartOffset.y
            );
        }

        if (clampToCanvas)
        {
            float halfWidth = rect.width * 0.5f;
            float halfHeight = rect.height * 0.5f;

            x = Mathf.Clamp(
                x,
                -halfWidth + canvasEdgePadding,
                halfWidth - canvasEdgePadding
            );

            y = Mathf.Clamp(
                y,
                -halfHeight + canvasEdgePadding,
                halfHeight - canvasEdgePadding
            );
        }

        popupRect.anchoredPosition = new Vector2(x, y);

        return true;
    }

    private void RecyclePopup(GameObject obj)
    {
        if (obj == null)
            return;

        if (usePool)
        {
            PoolMgr.Instance.PushObj(obj);
        }
        else
        {
            obj.SetActive(false);
        }
    }
}