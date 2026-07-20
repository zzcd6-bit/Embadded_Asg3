using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StorePanelController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField]
    private GameObject panelRoot;

    [SerializeField]
    private TMP_Text shopTitleText;

    [SerializeField]
    private Button closeButton;

    [Header("Category Buttons")]
    [SerializeField]
    private Button allButton;

    [SerializeField]
    private Button equipmentButton;

    [SerializeField]
    private Button consumableButton;

    [SerializeField]
    private Button questButton;

    [Header("Product Grid")]
    [SerializeField]
    private Transform contentRoot;

    [SerializeField]
    private StoreProductGridView
        productGridPrefab;

    [Header("Equipment Detail")]
    [SerializeField]
    private GameObject equipmentDetailPanel;

    [SerializeField]
    private EquipmentDetailView
        equipmentDetailView;

    [SerializeField]
    private Button equipmentBuyButton;

    [SerializeField]
    private TMP_Text equipmentBuyButtonText;

    [Header("Normal Item Detail")]
    [SerializeField]
    private GameObject itemDetailPanel;

    [SerializeField]
    private ItemDetailView itemDetailView;

    [SerializeField]
    private Button itemBuyButton;

    [SerializeField]
    private TMP_Text itemBuyButtonText;

    [Header("Currency")]
    [SerializeField]
    private CurrencyAmountView currencyAmountView;

    [Header("Display")]
    [SerializeField]
    private string buyButtonPrefix = "购买";

    [SerializeField]
    private string emptyBuyButtonText = "购买";

    [Header("Input")]
    [SerializeField]
    private bool closeWithEscape = true;

    [Header("Debug")]
    [SerializeField]
    private bool debugLog = true;

    private readonly List<StoreProductGridView>
        spawnedProductViews =
            new List<StoreProductGridView>();

    private ShopData currentShop;

    private ShopProductConfig selectedProduct;

    private InventoryCategory currentCategory =
        InventoryCategory.All;

    private PlayerInventoryController
        inventoryController;

    private PlayerEquipmentController
        equipmentController;

    private PlayerCurrencyController
        currencyController;

    private bool initialized;

    private bool currencySubscribed;

    public bool IsOpen
    {
        get
        {
            return panelRoot != null &&
                   panelRoot.activeSelf;
        }
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Update()
    {
        if (!IsOpen ||
            !closeWithEscape)
        {
            return;
        }

        Keyboard keyboard =
            Keyboard.current;

        if (keyboard != null &&
            keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    private void OnDisable()
    {
        UnsubscribeCurrency();
    }

    private void OnDestroy()
    {
        UnregisterButtons();
        UnsubscribeCurrency();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        if (panelRoot == null)
        {
            panelRoot =
                gameObject;
        }

        RegisterButtons();

        initialized = true;
    }

    private void RegisterButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(
                Close
            );
        }

        if (allButton != null)
        {
            allButton.onClick.AddListener(
                HandleAllClicked
            );
        }

        if (equipmentButton != null)
        {
            equipmentButton.onClick.AddListener(
                HandleEquipmentClicked
            );
        }

        if (consumableButton != null)
        {
            consumableButton.onClick.AddListener(
                HandleConsumableClicked
            );
        }

        if (questButton != null)
        {
            questButton.onClick.AddListener(
                HandleQuestClicked
            );
        }

        if (equipmentBuyButton != null)
        {
            equipmentBuyButton.onClick.AddListener(
                HandleBuyClicked
            );
        }

        if (itemBuyButton != null)
        {
            itemBuyButton.onClick.AddListener(
                HandleBuyClicked
            );
        }
    }

    private void UnregisterButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                Close
            );
        }

        if (allButton != null)
        {
            allButton.onClick.RemoveListener(
                HandleAllClicked
            );
        }

        if (equipmentButton != null)
        {
            equipmentButton.onClick.RemoveListener(
                HandleEquipmentClicked
            );
        }

        if (consumableButton != null)
        {
            consumableButton.onClick.RemoveListener(
                HandleConsumableClicked
            );
        }

        if (questButton != null)
        {
            questButton.onClick.RemoveListener(
                HandleQuestClicked
            );
        }

        if (equipmentBuyButton != null)
        {
            equipmentBuyButton.onClick.RemoveListener(
                HandleBuyClicked
            );
        }

        if (itemBuyButton != null)
        {
            itemBuyButton.onClick.RemoveListener(
                HandleBuyClicked
            );
        }
    }

    public void Open(
        ShopData shopData,
        GameObject interactor
    )
    {
        EnsureInitialized();

        if (shopData == null)
        {
            Debug.LogWarning(
                "[StorePanelController] " +
                "ShopData is null.",
                this
            );

            return;
        }

        ResolvePlayerReferences(
            interactor
        );

        if (inventoryController == null ||
            currencyController == null)
        {
            Debug.LogError(
                "[StorePanelController] " +
                "Player inventory or currency " +
                "controller was not found.",
                this
            );

            return;
        }

        currentShop =
            shopData;

        currentCategory =
            InventoryCategory.All;

        selectedProduct =
            null;

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        SubscribeCurrency();

        if (currencyAmountView != null)
        {
            currencyAmountView.Bind(
                currencyController
            );
        }

        if (shopTitleText != null)
        {
            shopTitleText.text =
                currentShop.DisplayName;
        }

        RefreshProductGrid();

        if (debugLog)
        {
            Debug.Log(
                "[StorePanelController] " +
                $"Opened shop: " +
                $"{currentShop.DisplayName}",
                this
            );
        }
    }

    public void Close()
    {
        UnsubscribeCurrency();

        ClearProductGrid();

        currentShop =
            null;

        selectedProduct =
            null;

        HideDetailPanels();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void ResolvePlayerReferences(
        GameObject interactor
    )
    {
        if (interactor != null)
        {
            inventoryController =
                interactor.GetComponentInParent<
                    PlayerInventoryController>();

            if (inventoryController == null)
            {
                inventoryController =
                    interactor.GetComponentInChildren<
                        PlayerInventoryController>(true);
            }

            equipmentController =
                interactor.GetComponentInParent<
                    PlayerEquipmentController>();

            if (equipmentController == null)
            {
                equipmentController =
                    interactor.GetComponentInChildren<
                        PlayerEquipmentController>(true);
            }

            currencyController =
                interactor.GetComponentInParent<
                    PlayerCurrencyController>();

            if (currencyController == null)
            {
                currencyController =
                    interactor.GetComponentInChildren<
                        PlayerCurrencyController>(true);
            }
        }

        if (inventoryController == null)
        {
            inventoryController =
                FindAnyObjectByType<
                    PlayerInventoryController>();
        }

        if (equipmentController == null)
        {
            equipmentController =
                FindAnyObjectByType<
                    PlayerEquipmentController>();
        }

        if (currencyController == null)
        {
            currencyController =
                FindAnyObjectByType<
                    PlayerCurrencyController>();
        }
    }

    private void SubscribeCurrency()
    {
        if (currencyController == null ||
            currencySubscribed)
        {
            return;
        }

        currencyController.CoinsChanged +=
            HandleCoinsChanged;

        currencySubscribed = true;
    }

    private void UnsubscribeCurrency()
    {
        if (currencyController != null &&
            currencySubscribed)
        {
            currencyController.CoinsChanged -=
                HandleCoinsChanged;
        }

        currencySubscribed = false;
    }

    private void HandleCoinsChanged(
        int currentCoins
    )
    {
        RefreshBuyButtons();
    }

    private void HandleAllClicked()
    {
        SetCategory(
            InventoryCategory.All
        );
    }

    private void HandleEquipmentClicked()
    {
        SetCategory(
            InventoryCategory.Equipment
        );
    }

    private void HandleConsumableClicked()
    {
        SetCategory(
            InventoryCategory.Consumable
        );
    }

    private void HandleQuestClicked()
    {
        SetCategory(
            InventoryCategory.Quest
        );
    }

    private void SetCategory(
        InventoryCategory category
    )
    {
        currentCategory =
            category;

        selectedProduct =
            null;

        RefreshProductGrid();
    }

    private void RefreshProductGrid()
    {
        ClearProductGrid();

        if (currentShop == null ||
            productGridPrefab == null ||
            contentRoot == null)
        {
            HideDetailPanels();
            return;
        }

        ShopProductConfig firstProduct =
            null;

        IReadOnlyList<ShopProductConfig>
            products =
                currentShop.Products;

        for (int i = 0;
             i < products.Count;
             i++)
        {
            ShopProductConfig product =
                products[i];

            if (!IsProductVisible(product))
                continue;

            if (firstProduct == null)
            {
                firstProduct =
                    product;
            }

            StoreProductGridView view =
                Instantiate(
                    productGridPrefab,
                    contentRoot,
                    false
                );

            view.gameObject.SetActive(true);

            view.Bind(
                product,
                SelectProduct
            );

            spawnedProductViews.Add(
                view
            );
        }

        selectedProduct =
            firstProduct;

        RefreshSelectedProductDetail();
    }

    private bool IsProductVisible(
        ShopProductConfig product
    )
    {
        if (product == null ||
            !product.IsValid ||
            product.ItemData == null)
        {
            return false;
        }

        if (currentCategory ==
            InventoryCategory.All)
        {
            return true;
        }

        return product.ItemData.Category ==
               currentCategory;
    }

    private void SelectProduct(
        ShopProductConfig product
    )
    {
        if (product == null ||
            !product.IsValid)
        {
            return;
        }

        selectedProduct =
            product;

        RefreshSelectedProductDetail();
    }

    private void RefreshSelectedProductDetail()
    {
        if (selectedProduct == null ||
            selectedProduct.ItemData == null)
        {
            HideDetailPanels();
            RefreshBuyButtons();
            return;
        }

        InventoryItemEntry previewEntry =
            new InventoryItemEntry(
                selectedProduct.ItemData,
                selectedProduct
                    .QuantityPerPurchase
            );

        bool isEquipment =
            selectedProduct.ItemData
                is EquipmentData;

        if (isEquipment)
        {
            if (itemDetailPanel != null)
            {
                itemDetailPanel.SetActive(false);
            }

            if (equipmentDetailPanel != null)
            {
                equipmentDetailPanel.SetActive(true);
            }

            if (equipmentDetailView != null)
            {
                equipmentDetailView.ShowEquipment(
                    previewEntry,
                    equipmentController
                );
            }
        }
        else
        {
            if (equipmentDetailPanel != null)
            {
                equipmentDetailPanel.SetActive(false);
            }

            if (itemDetailPanel != null)
            {
                itemDetailPanel.SetActive(true);
            }

            if (itemDetailView != null)
            {
                itemDetailView.ShowItem(
                    previewEntry
                );
            }
        }

        RefreshBuyButtons();
    }

    private void HideDetailPanels()
    {
        if (equipmentDetailPanel != null)
        {
            equipmentDetailPanel.SetActive(false);
        }

        if (itemDetailPanel != null)
        {
            itemDetailPanel.SetActive(false);
        }
    }

    private void RefreshBuyButtons()
    {
        bool hasProduct =
            selectedProduct != null &&
            selectedProduct.IsValid;

        int price =
            hasProduct
                ? selectedProduct.Price
                : 0;

        bool canAfford =
            hasProduct &&
            inventoryController != null &&
            currencyController != null &&
            currencyController.CanAfford(
                price
            );

        string buttonText =
            hasProduct
                ? $"{buyButtonPrefix} {price}"
                : emptyBuyButtonText;

        SetBuyButton(
            equipmentBuyButton,
            equipmentBuyButtonText,
            canAfford,
            buttonText
        );

        SetBuyButton(
            itemBuyButton,
            itemBuyButtonText,
            canAfford,
            buttonText
        );
    }

    private void SetBuyButton(
        Button button,
        TMP_Text buttonText,
        bool interactable,
        string displayText
    )
    {
        if (button != null)
        {
            button.interactable =
                interactable;
        }

        if (buttonText != null)
        {
            buttonText.text =
                displayText;
        }
    }

    private void HandleBuyClicked()
    {
        if (selectedProduct == null ||
            !selectedProduct.IsValid ||
            inventoryController == null ||
            currencyController == null)
        {
            return;
        }

        int price =
            selectedProduct.Price;

        if (!currencyController.CanAfford(
                price))
        {
            RefreshBuyButtons();
            return;
        }

        bool paid =
            currencyController.TrySpendCoins(
                price,
                CurrencyChangeReason.ShopPurchase,
                this
            );

        if (!paid)
        {
            RefreshBuyButtons();
            return;
        }

        InventoryItemEntry addedEntry =
            inventoryController.AddItem(
                selectedProduct.ItemData,
                selectedProduct
                    .QuantityPerPurchase
            );

        if (addedEntry == null)
        {
            /*
             * 背包加入失败时静默退还金币，
             * 不触发“获得金币”提示。
             */
            currencyController.RestoreCoins(
                currencyController.CurrentCoins +
                price
            );

            Debug.LogError(
                "[StorePanelController] " +
                "Purchase failed because item " +
                "could not be added to inventory.",
                this
            );

            RefreshBuyButtons();
            return;
        }

        RefreshBuyButtons();

        if (debugLog)
        {
            Debug.Log(
                "[StorePanelController] " +
                $"Purchased " +
                $"{selectedProduct.ItemData.DisplayName} " +
                $"x{selectedProduct.QuantityPerPurchase}, " +
                $"Price={price}",
                this
            );
        }
    }

    private void ClearProductGrid()
    {
        for (int i = 0;
             i < spawnedProductViews.Count;
             i++)
        {
            StoreProductGridView view =
                spawnedProductViews[i];

            if (view != null)
            {
                Destroy(
                    view.gameObject
                );
            }
        }

        spawnedProductViews.Clear();
    }
}