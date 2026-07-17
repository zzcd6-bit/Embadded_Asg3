using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StorePanel : BasePanel
{
    [Header("Store Information")]
    [SerializeField]
    private TMP_Text shopTitleText;

    [Header("Product Grid")]
    [SerializeField]
    private Transform contentRoot;

    [SerializeField]
    private StoreProductGridView productGridPrefab;

    [Header("Equipment Detail")]
    [SerializeField]
    private GameObject equipmentDetailPanel;

    [SerializeField]
    private EquipmentDetailView equipmentDetailView;

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

    [Header("Buy Button Display")]
    [SerializeField]
    private string buyButtonPrefix = "Buy";

    [SerializeField]
    private string noProductButtonText = "Buy";

    [SerializeField]
    private string insufficientCoinsText = "Not Enough";

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

    private Action closeCallback;

    private bool currencySubscribed;

    private bool isBound;

    public ShopData CurrentShop
    {
        get { return currentShop; }
    }

    public bool IsBound
    {
        get { return isBound; }
    }

    protected override void Awake()
    {
        base.Awake();

        ResolveUIReferences();
    }

    private void Update()
    {
        if (!isBound ||
            !closeWithEscape)
        {
            return;
        }

        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            RequestClose();
        }
    }

    public override void ShowMe()
    {
        gameObject.SetActive(true);

        HideDetailPanels();
    }

    public override void HideMe()
    {
        Unbind();

        gameObject.SetActive(false);
    }

    /// <summary>
    /// UIMgr 创建完 StorePanel 后调用。
    /// </summary>
    public void Bind(
        ShopData shopData,
        GameObject interactor,
        Action onClose
    )
    {
        Unbind();

        currentShop =
            shopData;

        closeCallback =
            onClose;

        ResolvePlayerReferences(
            interactor
        );

        if (currentShop == null)
        {
            Debug.LogError(
                "[StorePanel] ShopData is missing.",
                this
            );

            RequestClose();
            return;
        }

        if (inventoryController == null)
        {
            Debug.LogError(
                "[StorePanel] " +
                "PlayerInventoryController was not found.",
                this
            );

            RequestClose();
            return;
        }

        if (currencyController == null)
        {
            Debug.LogError(
                "[StorePanel] " +
                "PlayerCurrencyController was not found.",
                this
            );

            RequestClose();
            return;
        }

        currentCategory =
            InventoryCategory.All;

        selectedProduct =
            null;

        isBound = true;

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
                "[StorePanel] " +
                $"Opened shop: {currentShop.DisplayName}",
                this
            );
        }
    }

    private void Unbind()
    {
        UnsubscribeCurrency();

        ClearProductGrid();

        currentShop = null;
        selectedProduct = null;

        inventoryController = null;
        equipmentController = null;
        currencyController = null;

        closeCallback = null;

        isBound = false;

        HideDetailPanels();
    }

    private void ResolveUIReferences()
    {
        if (equipmentDetailView == null &&
            equipmentDetailPanel != null)
        {
            equipmentDetailView =
                equipmentDetailPanel
                    .GetComponent<EquipmentDetailView>();
        }

        if (itemDetailView == null &&
            itemDetailPanel != null)
        {
            itemDetailView =
                itemDetailPanel
                    .GetComponent<ItemDetailView>();
        }

        if (equipmentBuyButtonText == null &&
            equipmentBuyButton != null)
        {
            equipmentBuyButtonText =
                equipmentBuyButton
                    .GetComponentInChildren<TMP_Text>(true);
        }

        if (itemBuyButtonText == null &&
            itemBuyButton != null)
        {
            itemBuyButtonText =
                itemBuyButton
                    .GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void ResolvePlayerReferences(
        GameObject interactor
    )
    {
        if (interactor != null)
        {
            inventoryController =
                interactor.GetComponent<
                    PlayerInventoryController>();

            if (inventoryController == null)
            {
                inventoryController =
                    interactor.GetComponentInParent<
                        PlayerInventoryController>();
            }

            if (inventoryController == null)
            {
                inventoryController =
                    interactor.GetComponentInChildren<
                        PlayerInventoryController>(true);
            }

            equipmentController =
                interactor.GetComponent<
                    PlayerEquipmentController>();

            if (equipmentController == null)
            {
                equipmentController =
                    interactor.GetComponentInParent<
                        PlayerEquipmentController>();
            }

            if (equipmentController == null)
            {
                equipmentController =
                    interactor.GetComponentInChildren<
                        PlayerEquipmentController>(true);
            }

            currencyController =
                interactor.GetComponent<
                    PlayerCurrencyController>();

            if (currencyController == null)
            {
                currencyController =
                    interactor.GetComponentInParent<
                        PlayerCurrencyController>();
            }

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
                FindFirstObjectByType<
                    PlayerInventoryController>();
        }

        if (equipmentController == null)
        {
            equipmentController =
                FindFirstObjectByType<
                    PlayerEquipmentController>();
        }

        if (currencyController == null)
        {
            currencyController =
                FindFirstObjectByType<
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

    protected override void ClickBtn(
        string btnName
    )
    {
        switch (btnName)
        {
            case "CloseButton":
            case "XButton":

                RequestClose();
                break;

            case "AllButton":

                SetCategory(
                    InventoryCategory.All
                );
                break;

            case "EquipmentButton":

                SetCategory(
                    InventoryCategory.Equipment
                );
                break;

            case "ConsumableButton":

                SetCategory(
                    InventoryCategory.Consumable
                );
                break;

            case "QuestButton":

                SetCategory(
                    InventoryCategory.Quest
                );
                break;

            case "EquipmentBuyButton":
            case "ItemBuyButton":

                TryBuySelectedProduct();
                break;
        }
    }

    private void SetCategory(
        InventoryCategory category
    )
    {
        if (currentCategory == category)
            return;

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
            RefreshBuyButtons();
            return;
        }

        ShopProductConfig firstVisibleProduct =
            null;

        IReadOnlyList<ShopProductConfig> products =
            currentShop.Products;

        for (int i = 0;
             i < products.Count;
             i++)
        {
            ShopProductConfig product =
                products[i];

            if (!IsProductVisible(product))
                continue;

            if (firstVisibleProduct == null)
            {
                firstVisibleProduct =
                    product;
            }

            StoreProductGridView productView =
                Instantiate(
                    productGridPrefab,
                    contentRoot,
                    false
                );

            productView.gameObject.SetActive(
                true
            );

            productView.transform.SetAsLastSibling();

            productView.Bind(
                product,
                SelectProduct
            );

            spawnedProductViews.Add(
                productView
            );
        }

        selectedProduct =
            firstVisibleProduct;

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

        EquipmentData equipmentData =
            selectedProduct.ItemData
                as EquipmentData;

        if (equipmentData != null)
        {
            ShowEquipmentDetail(
                previewEntry
            );
        }
        else
        {
            ShowItemDetail(
                previewEntry
            );
        }

        RefreshBuyButtons();
    }

    private void ShowEquipmentDetail(
        InventoryItemEntry previewEntry
    )
    {
        if (itemDetailPanel != null)
        {
            itemDetailPanel.SetActive(false);
        }

        if (equipmentDetailPanel != null)
        {
            equipmentDetailPanel.SetActive(true);

            equipmentDetailPanel.transform
                .SetAsLastSibling();
        }

        if (equipmentDetailView != null)
        {
            equipmentDetailView.ShowEquipment(
                previewEntry,
                equipmentController
            );
        }
    }

    private void ShowItemDetail(
        InventoryItemEntry previewEntry
    )
    {
        if (equipmentDetailPanel != null)
        {
            equipmentDetailPanel.SetActive(false);
        }

        if (itemDetailPanel != null)
        {
            itemDetailPanel.SetActive(true);

            itemDetailPanel.transform
                .SetAsLastSibling();
        }

        if (itemDetailView != null)
        {
            itemDetailView.ShowItem(
                previewEntry
            );
        }
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

        SetBuyButton(
            equipmentBuyButton,
            equipmentBuyButtonText,
            false,
            noProductButtonText
        );

        SetBuyButton(
            itemBuyButton,
            itemBuyButtonText,
            false,
            noProductButtonText
        );
    }

    private void RefreshBuyButtons()
    {
        bool hasProduct =
            selectedProduct != null &&
            selectedProduct.IsValid &&
            selectedProduct.ItemData != null;

        int price =
            hasProduct
                ? selectedProduct.Price
                : 0;

        bool hasEnoughCoins =
            hasProduct &&
            currencyController != null &&
            currencyController.CanAfford(
                price
            );

        string buttonText;

        if (!hasProduct)
        {
            buttonText =
                noProductButtonText;
        }
        else if (!hasEnoughCoins)
        {
            buttonText =
                $"{insufficientCoinsText} ({price})";
        }
        else
        {
            buttonText =
                $"{buyButtonPrefix} {price}";
        }

        SetBuyButton(
            equipmentBuyButton,
            equipmentBuyButtonText,
            hasEnoughCoins,
            buttonText
        );

        SetBuyButton(
            itemBuyButton,
            itemBuyButtonText,
            hasEnoughCoins,
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

    private void TryBuySelectedProduct()
    {
        if (selectedProduct == null ||
            !selectedProduct.IsValid ||
            selectedProduct.ItemData == null ||
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
             * 加入背包失败时退回金币。
             * RestoreCoins 不会触发“获得金币”提示。
             */

            currencyController.RestoreCoins(
                currencyController.CurrentCoins +
                price
            );

            Debug.LogError(
                "[StorePanel] " +
                "Purchase failed because the item " +
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
                "[StorePanel] " +
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

    private void RequestClose()
    {
        if (!isBound &&
            !gameObject.activeInHierarchy)
        {
            return;
        }

        Action callback =
            closeCallback;

        /*
         * 先解除绑定，
         * 避免 UIMgr 销毁时再次触发状态。
         */

        Unbind();

        callback?.Invoke();

        UIMgr.Instance.HidePanel<
            StorePanel>();
    }

    private void OnDestroy()
    {
        Unbind();
    }
}