using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ReactableObject))]
public class ShopVendor : MonoBehaviour
{
    [Header("Shop")]
    [SerializeField]
    private ShopData shopData;

    [Header("UI")]
    [SerializeField]
    private E_UILayer panelLayer = E_UILayer.Top;

    [SerializeField]
    private bool useSynchronousLoading = true;

    [Header("Interaction Display")]
    [SerializeField]
    private bool automaticallySetOptionName = true;

    [SerializeField]
    private string optionPrefix = "Open Shop";

    [Header("Debug")]
    [SerializeField]
    private bool debugLog;

    private ReactableObject reactableObject;
    private bool shopIsOpen;

    private void Awake()
    {
        reactableObject = GetComponent<ReactableObject>();

        if (reactableObject != null)
        {
            reactableObject.OnInteractEvent.AddListener(HandleInteract);
        }

        RefreshOptionName();
    }

    private void OnDestroy()
    {
        if (reactableObject != null)
        {
            reactableObject.OnInteractEvent.RemoveListener(HandleInteract);
        }

        if (shopIsOpen)
        {
            RestoreGameplayState();
        }
    }

    private void HandleInteract(GameObject interactor)
    {
        if (shopIsOpen)
            return;

        if (shopData == null)
        {
            Debug.LogWarning("[ShopVendor] ShopData is missing.", this);
            return;
        }

        if (!GameModeManager.Instance.CurrentCapabilities.canOpenMenu)
        {
            return;
        }

        if (!GameModeManager.Instance.RequestMode(GameModeState.Menu, this))
        {
            return;
        }

        shopIsOpen = true;

        if (reactableObject != null)
        {
            reactableObject.SetInteractable(false);
        }

        UIMgr.Instance.ShowPanel<StorePanel>(
            panelLayer,
            panel =>
            {
                if (panel == null)
                {
                    Debug.LogError("[ShopVendor] StorePanel failed to load.", this);
                    RestoreGameplayState();
                    return;
                }

                panel.Bind(shopData, interactor, HandleStorePanelClosed);

                if (debugLog)
                {
                    Debug.Log($"[ShopVendor] Opened shop: {shopData.DisplayName}", this);
                }
            },
            useSynchronousLoading
        );
    }

    private void HandleStorePanelClosed()
    {
        RestoreGameplayState();

        if (debugLog)
        {
            Debug.Log("[ShopVendor] Store panel closed.", this);
        }
    }

    private void RestoreGameplayState()
    {
        if (!shopIsOpen)
            return;

        shopIsOpen = false;

        GameModeManager.Instance.ExitMode(GameModeState.Menu);

        if (reactableObject != null)
        {
            reactableObject.SetInteractable(true);
        }
    }

    private void RefreshOptionName()
    {
        if (!automaticallySetOptionName ||
            reactableObject == null ||
            shopData == null)
        {
            return;
        }

        reactableObject.SetOptionName($"{optionPrefix} {shopData.DisplayName}");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            reactableObject = GetComponent<ReactableObject>();
            RefreshOptionName();
        }
    }
#endif
}
