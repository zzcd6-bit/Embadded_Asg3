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
    private E_UILayer panelLayer =
        E_UILayer.Top;

    [SerializeField]
    private bool useSynchronousLoading =
        true;

    [Header("Interaction Display")]
    [SerializeField]
    private bool automaticallySetOptionName =
        true;

    [SerializeField]
    private string optionPrefix =
        "Open Shop";

    [Header("Gameplay")]
    [SerializeField]
    private bool disablePlayerControl =
        true;

    [SerializeField]
    private bool pauseGame =
        true;

    [Header("Cursor")]
    [SerializeField]
    private bool unlockCursor =
        true;

    [Header("Debug")]
    [SerializeField]
    private bool debugLog;

    private ReactableObject reactableObject;

    private ActionPlayerController
        playerController;

    private bool shopIsOpen;

    private float previousTimeScale = 1f;

    private CursorLockMode
        previousCursorLockMode;

    private bool previousCursorVisible;

    private void Awake()
    {
        reactableObject =
            GetComponent<ReactableObject>();

        if (reactableObject != null)
        {
            reactableObject.OnInteractEvent
                .AddListener(
                    HandleInteract
                );
        }

        RefreshOptionName();
    }

    private void OnDestroy()
    {
        if (reactableObject != null)
        {
            reactableObject.OnInteractEvent
                .RemoveListener(
                    HandleInteract
                );
        }

        if (shopIsOpen)
        {
            RestoreGameplayState();
        }
    }

    private void HandleInteract(
        GameObject interactor
    )
    {
        if (shopIsOpen)
            return;

        if (shopData == null)
        {
            Debug.LogWarning(
                "[ShopVendor] ShopData is missing.",
                this
            );

            return;
        }

        ResolvePlayerController(
            interactor
        );

        UIMgr.Instance.ShowPanel<
            StorePanel>(
            panelLayer,
            panel =>
            {
                if (panel == null)
                {
                    Debug.LogError(
                        "[ShopVendor] " +
                        "StorePanel failed to load.",
                        this
                    );

                    return;
                }

                ApplyGameplayState();

                panel.Bind(
                    shopData,
                    interactor,
                    HandleStorePanelClosed
                );

                if (debugLog)
                {
                    Debug.Log(
                        "[ShopVendor] " +
                        $"Opened shop: " +
                        $"{shopData.DisplayName}",
                        this
                    );
                }
            },
            useSynchronousLoading
        );
    }

    private void ResolvePlayerController(
        GameObject interactor
    )
    {
        playerController = null;

        if (interactor == null)
            return;

        playerController =
            interactor.GetComponent<
                ActionPlayerController>();

        if (playerController == null)
        {
            playerController =
                interactor.GetComponentInParent<
                    ActionPlayerController>();
        }

        if (playerController == null)
        {
            playerController =
                interactor.GetComponentInChildren<
                    ActionPlayerController>(true);
        }
    }

    private void ApplyGameplayState()
    {
        if (shopIsOpen)
            return;

        shopIsOpen = true;

        previousTimeScale =
            Time.timeScale;

        previousCursorLockMode =
            Cursor.lockState;

        previousCursorVisible =
            Cursor.visible;

        if (disablePlayerControl &&
            playerController != null)
        {
            playerController
                .SetGameplayControlEnabled(
                    false
                );
        }

        if (pauseGame)
        {
            Time.timeScale = 0f;
        }

        if (unlockCursor)
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;
        }

        if (reactableObject != null)
        {
            reactableObject.SetInteractable(
                false
            );
        }
    }

    private void HandleStorePanelClosed()
    {
        RestoreGameplayState();

        if (debugLog)
        {
            Debug.Log(
                "[ShopVendor] Store panel closed.",
                this
            );
        }
    }

    private void RestoreGameplayState()
    {
        if (!shopIsOpen)
            return;

        shopIsOpen = false;

        if (pauseGame)
        {
            Time.timeScale =
                previousTimeScale;
        }

        if (disablePlayerControl &&
            playerController != null)
        {
            playerController
                .SetGameplayControlEnabled(
                    true
                );
        }

        if (unlockCursor)
        {
            Cursor.lockState =
                previousCursorLockMode;

            Cursor.visible =
                previousCursorVisible;
        }

        if (reactableObject != null)
        {
            reactableObject.SetInteractable(
                true
            );
        }

        playerController = null;
    }

    private void RefreshOptionName()
    {
        if (!automaticallySetOptionName ||
            reactableObject == null ||
            shopData == null)
        {
            return;
        }

        reactableObject.SetOptionName(
            $"{optionPrefix} " +
            $"{shopData.DisplayName}"
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            reactableObject =
                GetComponent<ReactableObject>();

            RefreshOptionName();
        }
    }
#endif
}