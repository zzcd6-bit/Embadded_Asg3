using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInventoryUIController :
    MonoBehaviour
{
    [Header("Top Tabs")]
    [SerializeField]
    private Button characterButton;

    [SerializeField]
    private Button itemsButton;

    [SerializeField]
    private GameObject characterPage;

    [SerializeField]
    private GameObject itemsPage;

    [Header("Character Equipment")]
    [SerializeField]
    private CharacterEquipmentSlotView helmetSlot;

    [SerializeField]
    private CharacterEquipmentSlotView chestSlot;

    [SerializeField]
    private CharacterEquipmentSlotView shoesSlot;

    [SerializeField]
    private CharacterEquipmentSlotView weaponSlot;

    [SerializeField]
    private EquipmentDetailView
        characterEquipmentDetail;

    [Header("Item Categories")]
    [SerializeField]
    private Button allButton;

    [SerializeField]
    private Button equipmentButton;

    [SerializeField]
    private Button consumablesButton;

    [SerializeField]
    private Button questButton;

    [Header("Item Grid")]
    [SerializeField]
    private Transform itemGridContent;

    [SerializeField]
    private InventoryGridItemView gridPrefab;

    [SerializeField]
    private bool debugLog = true;

    [Header("Item Detail")]
    [SerializeField]
    private EquipmentDetailView
        itemEquipmentDetail;

    [SerializeField]
    private ItemDetailView itemDetail;

    [Header("Item Detail Buttons")]
    [SerializeField]
    private Button equipButton;

    [SerializeField]
    private Button equipmentDiscardButton;

    [SerializeField]
    private Button useButton;

    [SerializeField]
    private Button itemDiscardButton;

    private PlayerCharacterStatsController
        statsController;

    private PlayerInventoryController
        inventoryController;

    private PlayerEquipmentController
        equipmentController;

    private PlayerItemUseController
        itemUseController;

    private InventoryCategory currentCategory =
        InventoryCategory.All;

    private InventoryItemEntry selectedItem;

    private EquipmentSlotType selectedSlot =
        EquipmentSlotType.Weapon;

    private bool suppressInventoryRefresh;

    private readonly List<InventoryGridItemView>
        spawnedGridItems =
            new List<InventoryGridItemView>();

    private void Awake()
    {
        RegisterButtons();

        ValidateUIReferences();
    }

    private void ValidateUIReferences()
    {
        if (characterEquipmentDetail == null)
        {
            Debug.LogError(
                "[CharacterInventoryUIController] " +
                "Character Equipment Detail is missing.",
                this
            );
        }

        if (itemEquipmentDetail == null)
        {
            Debug.LogError(
                "[CharacterInventoryUIController] " +
                "Item Equipment Detail is missing.\n" +
                "Assign ItemsArea/DetailArea/" +
                "EquipmentDetailPanel.",
                this
            );
        }

        if (itemDetail == null)
        {
            Debug.LogError(
                "[CharacterInventoryUIController] " +
                "Item Detail is missing.\n" +
                "Assign ItemsArea/DetailArea/" +
                "ItemDetailPanel.",
                this
            );
        }

        if (characterEquipmentDetail != null &&
            itemEquipmentDetail != null &&
            characterEquipmentDetail ==
            itemEquipmentDetail)
        {
            Debug.LogError(
                "[CharacterInventoryUIController] " +
                "Character Equipment Detail and " +
                "Item Equipment Detail point to the " +
                "same object. They must be different.",
                this
            );
        }

        if (itemEquipmentDetail != null &&
            itemDetail != null &&
            itemEquipmentDetail.transform.parent !=
            itemDetail.transform.parent)
        {
            Debug.LogWarning(
                "[CharacterInventoryUIController] " +
                "Item Equipment Detail and Item Detail " +
                "are not under the same DetailArea.",
                this
            );
        }
    }

    private void OnDestroy()
    {
        UnregisterButtons();
        Unbind();
    }

    public void Bind(
        PlayerCharacterStatsController controller
    )
    {
        Unbind();

        statsController = controller;

        if (statsController == null)
            return;

        GameObject playerObject =
            statsController.gameObject;

        inventoryController =
            playerObject.GetComponentInChildren<
                PlayerInventoryController>(true);

        equipmentController =
            playerObject.GetComponentInChildren<
                PlayerEquipmentController>(true);

        itemUseController =
            playerObject.GetComponentInChildren<
                PlayerItemUseController>(true);

        if (inventoryController != null)
        {
            inventoryController.InventoryChanged +=
                HandleInventoryChanged;
        }

        if (equipmentController != null)
        {
            equipmentController.EquipmentChanged +=
                HandleEquipmentChanged;
        }

        ShowCharacterPage();
    }

    public void Unbind()
    {
        if (inventoryController != null)
        {
            inventoryController.InventoryChanged -=
                HandleInventoryChanged;
        }

        if (equipmentController != null)
        {
            equipmentController.EquipmentChanged -=
                HandleEquipmentChanged;
        }

        statsController = null;
        inventoryController = null;
        equipmentController = null;
        itemUseController = null;

        selectedItem = null;

        ClearGrid();
    }

    private void RegisterButtons()
    {
        characterButton?.onClick.AddListener(
            ShowCharacterPage
        );

        itemsButton?.onClick.AddListener(
            ShowItemsPage
        );

        allButton?.onClick.AddListener(
            () => SetCategory(
                InventoryCategory.All
            )
        );

        equipmentButton?.onClick.AddListener(
            () => SetCategory(
                InventoryCategory.Equipment
            )
        );

        consumablesButton?.onClick.AddListener(
            () => SetCategory(
                InventoryCategory.Consumable
            )
        );

        questButton?.onClick.AddListener(
            () => SetCategory(
                InventoryCategory.Quest
            )
        );

        equipButton?.onClick.AddListener(
            EquipSelectedItem
        );

        equipmentDiscardButton
            ?.onClick.AddListener(
                DiscardSelectedItem
            );

        useButton?.onClick.AddListener(
            UseSelectedItem
        );

        itemDiscardButton
            ?.onClick.AddListener(
                DiscardSelectedItem
            );
    }

    private void UnregisterButtons()
    {
        characterButton?.onClick.RemoveListener(
            ShowCharacterPage
        );

        itemsButton?.onClick.RemoveListener(
            ShowItemsPage
        );

        equipButton?.onClick.RemoveListener(
            EquipSelectedItem
        );

        equipmentDiscardButton
            ?.onClick.RemoveListener(
                DiscardSelectedItem
            );

        useButton?.onClick.RemoveListener(
            UseSelectedItem
        );

        itemDiscardButton
            ?.onClick.RemoveListener(
                DiscardSelectedItem
            );
    }

    private void ShowCharacterPage()
    {
        if (characterPage != null)
        {
            characterPage.SetActive(true);
        }

        if (itemsPage != null)
        {
            itemsPage.SetActive(false);
        }

        RefreshCharacterEquipment();

        SelectCharacterSlot(
            selectedSlot,
            equipmentController != null
                ? equipmentController.GetEquippedItem(
                    selectedSlot
                )
                : null
        );
    }

    private void ShowItemsPage()
    {
        if (characterPage != null)
        {
            characterPage.SetActive(false);
        }

        if (itemsPage != null)
        {
            itemsPage.SetActive(true);
        }

        RefreshItemGrid();
    }

    private void SetCategory(
        InventoryCategory category
    )
    {
        currentCategory = category;
        selectedItem = null;

        RefreshItemGrid();
    }

    private void RefreshCharacterEquipment()
    {
        BindCharacterSlot(
            helmetSlot,
            EquipmentSlotType.Helmet
        );

        BindCharacterSlot(
            chestSlot,
            EquipmentSlotType.Chest
        );

        BindCharacterSlot(
            shoesSlot,
            EquipmentSlotType.Shoes
        );

        BindCharacterSlot(
            weaponSlot,
            EquipmentSlotType.Weapon
        );
    }

    private void BindCharacterSlot(
        CharacterEquipmentSlotView slotView,
        EquipmentSlotType slotType
    )
    {
        if (slotView == null)
            return;

        InventoryItemEntry equippedItem =
            equipmentController != null
                ? equipmentController
                    .GetEquippedItem(slotType)
                : null;

        slotView.Bind(
            equippedItem,
            SelectCharacterSlot
        );
    }

    private void SelectCharacterSlot(
        EquipmentSlotType slotType,
        InventoryItemEntry entry
    )
    {
        selectedSlot = slotType;

        helmetSlot?.SetSelected(
            slotType ==
            EquipmentSlotType.Helmet
        );

        chestSlot?.SetSelected(
            slotType ==
            EquipmentSlotType.Chest
        );

        shoesSlot?.SetSelected(
            slotType ==
            EquipmentSlotType.Shoes
        );

        weaponSlot?.SetSelected(
            slotType ==
            EquipmentSlotType.Weapon
        );

        if (characterEquipmentDetail == null)
            return;

        if (entry == null)
        {
            characterEquipmentDetail.ShowEmpty();
        }
        else
        {
            characterEquipmentDetail.ShowEquipment(
                entry,
                equipmentController
            );
        }
    }

    private void RefreshItemGrid()
    {
        ClearGrid();

        if (inventoryController == null)
        {
            Debug.LogError(
                "[CharacterInventoryUIController] " +
                "PlayerInventoryController not found.",
                this
            );

            RefreshSelectedItemDetail();
            return;
        }

        if (gridPrefab == null)
        {
            Debug.LogError(
                "[CharacterInventoryUIController] " +
                "Grid Prefab is missing.",
                this
            );

            return;
        }

        if (itemGridContent == null)
        {
            Debug.LogError(
                "[CharacterInventoryUIController] " +
                "Item Grid Content is missing.",
                this
            );

            return;
        }

        IReadOnlyList<InventoryItemEntry> items =
            inventoryController.Items;

        InventoryItemEntry firstVisibleItem = null;

        for (int i = 0; i < items.Count; i++)
        {
            InventoryItemEntry entry = items[i];

            if (!MatchesCategory(entry))
                continue;

            if (firstVisibleItem == null)
            {
                firstVisibleItem = entry;
            }

            InventoryGridItemView view =
                Instantiate(
                    gridPrefab,
                    itemGridContent,
                    false
                );

            view.gameObject.SetActive(true);

            view.Bind(
                entry,
                SelectInventoryItem
            );

            spawnedGridItems.Add(view);
        }

        if (selectedItem != null &&
            !inventoryController.Contains(selectedItem))
        {
            selectedItem = null;
        }

        if (selectedItem != null &&
            !MatchesCategory(selectedItem))
        {
            selectedItem = null;
        }

        // 切换分类时默认显示第一个道具
        if (selectedItem == null)
        {
            selectedItem = firstVisibleItem;
        }

        RefreshSelectedItemDetail();

        if (debugLog)
        {
            Debug.Log(
                $"[CharacterInventoryUIController] " +
                $"Grid refreshed. " +
                $"Visible Count={spawnedGridItems.Count}",
                this
            );
        }
    }

    private bool MatchesCategory(
        InventoryItemEntry entry
    )
    {
        if (entry?.ItemData == null)
            return false;

        if (currentCategory ==
            InventoryCategory.All)
        {
            return true;
        }

        return entry.ItemData.Category ==
               currentCategory;
    }

    private void SelectInventoryItem(
    InventoryItemEntry entry
)
    {
        if (entry == null ||
            entry.ItemData == null)
        {
            return;
        }

        selectedItem = entry;

        if (debugLog)
        {
            Debug.Log(
                $"[CharacterInventoryUIController] " +
                $"Selected item: " +
                $"{entry.ItemData.DisplayName}, " +
                $"Category={entry.ItemData.Category}",
                this
            );
        }

        RefreshSelectedItemDetail();
    }

    private void RefreshSelectedItemDetail()
    {
        if (selectedItem == null ||
            selectedItem.ItemData == null)
        {
            if (itemEquipmentDetail != null)
            {
                itemEquipmentDetail.gameObject
                    .SetActive(false);
            }

            if (itemDetail != null)
            {
                itemDetail.gameObject
                    .SetActive(true);

                itemDetail.transform.SetAsLastSibling();

                itemDetail.ShowEmpty();
            }

            RefreshActionButtons();

            if (debugLog)
            {
                Debug.Log(
                    "[CharacterInventoryUIController] " +
                    "No item selected. Show empty detail.",
                    this
                );
            }

            return;
        }

        bool isEquipment =
            selectedItem.ItemData is EquipmentData;

        if (isEquipment)
        {
            if (itemEquipmentDetail == null)
            {
                Debug.LogError(
                    "[CharacterInventoryUIController] " +
                    "Item Equipment Detail is NULL.\n" +
                    "Please assign: " +
                    "ItemsArea/DetailArea/" +
                    "EquipmentDetailPanel",
                    this
                );

                return;
            }

            if (itemDetail != null)
            {
                itemDetail.gameObject.SetActive(false);
            }

            itemEquipmentDetail.gameObject
                .SetActive(true);

            itemEquipmentDetail.transform
                .SetAsLastSibling();

            itemEquipmentDetail.ShowEquipment(
                selectedItem,
                equipmentController
            );

            if (debugLog)
            {
                Debug.Log(
                    "[CharacterInventoryUIController] " +
                    $"Equipment detail refreshed: " +
                    $"{selectedItem.ItemData.DisplayName}, " +
                    $"Panel={itemEquipmentDetail.name}",
                    itemEquipmentDetail
                );
            }
        }
        else
        {
            if (itemDetail == null)
            {
                Debug.LogError(
                    "[CharacterInventoryUIController] " +
                    "Item Detail is NULL.\n" +
                    "Please assign: " +
                    "ItemsArea/DetailArea/ItemDetailPanel",
                    this
                );

                return;
            }

            if (itemEquipmentDetail != null)
            {
                itemEquipmentDetail.gameObject
                    .SetActive(false);
            }

            itemDetail.gameObject.SetActive(true);

            itemDetail.transform.SetAsLastSibling();

            itemDetail.ShowItem(
                selectedItem
            );

            if (debugLog)
            {
                Debug.Log(
                    "[CharacterInventoryUIController] " +
                    $"Normal item detail refreshed: " +
                    $"{selectedItem.ItemData.DisplayName}, " +
                    $"Panel={itemDetail.name}",
                    itemDetail
                );
            }
        }

        Canvas.ForceUpdateCanvases();

        RefreshActionButtons();
    }

    private void RefreshActionButtons()
    {
        bool hasSelection =
            selectedItem?.ItemData != null;

        bool isEquipment =
            hasSelection &&
            selectedItem.ItemData
            is EquipmentData;

        bool isConsumable =
            hasSelection &&
            selectedItem.ItemData
            is ConsumableItemData;

        bool isEquipped =
            hasSelection &&
            equipmentController != null &&
            equipmentController
                .IsEquipped(selectedItem);

        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(
                isEquipment
            );

            equipButton.interactable =
                isEquipment &&
                !isEquipped;
        }

        if (equipmentDiscardButton != null)
        {
            equipmentDiscardButton
                .gameObject
                .SetActive(isEquipment);

            equipmentDiscardButton.interactable =
                isEquipment &&
                !isEquipped &&
                selectedItem.ItemData.CanDiscard;
        }

        if (useButton != null)
        {
            useButton.gameObject.SetActive(
                isConsumable
            );

            useButton.interactable =
                isConsumable;
        }

        if (itemDiscardButton != null)
        {
            bool canDiscard =
                hasSelection &&
                !isEquipment &&
                selectedItem.ItemData.CanDiscard;

            itemDiscardButton.gameObject
                .SetActive(canDiscard);

            itemDiscardButton.interactable =
                canDiscard;
        }
    }

    private void EquipSelectedItem()
    {
        if (selectedItem == null ||
            equipmentController == null)
        {
            return;
        }

        if (equipmentController.Equip(
                selectedItem))
        {
            RefreshCharacterEquipment();
            RefreshItemGrid();
        }
    }

    private void UseSelectedItem()
    {
        if (selectedItem == null ||
            itemUseController == null ||
            inventoryController == null)
        {
            return;
        }

        InventoryItemEntry usedEntry =
            selectedItem;

        int previousVisibleIndex =
            GetVisibleItemIndex(usedEntry);

        suppressInventoryRefresh = true;

        bool usedSuccessfully;

        try
        {
            usedSuccessfully =
                itemUseController.TryUse(
                    usedEntry
                );
        }
        finally
        {
            suppressInventoryRefresh = false;
        }

        if (!usedSuccessfully)
        {
            return;
        }

        // 道具仍有剩余数量，继续选中原道具
        if (inventoryController.Contains(usedEntry) &&
            MatchesCategory(usedEntry))
        {
            selectedItem = usedEntry;
        }
        else
        {
            // 道具已经被完全消耗，选择原位置的下一个道具。
            // 如果原位置已经超过范围，则自动选择最后一个。
            selectedItem =
                GetVisibleItemAtOrNearIndex(
                    previousVisibleIndex
                );
        }

        RefreshItemGrid();
    }

    private int GetVisibleItemIndex(
    InventoryItemEntry targetEntry
)
    {
        if (inventoryController == null ||
            targetEntry == null)
        {
            return -1;
        }

        IReadOnlyList<InventoryItemEntry> items =
            inventoryController.Items;

        int visibleIndex = 0;

        for (int i = 0; i < items.Count; i++)
        {
            InventoryItemEntry entry =
                items[i];

            if (!MatchesCategory(entry))
                continue;

            if (entry == targetEntry)
            {
                return visibleIndex;
            }

            visibleIndex++;
        }

        return -1;
    }

    private InventoryItemEntry GetVisibleItemAtOrNearIndex(
        int requestedIndex
    )
    {
        if (inventoryController == null)
            return null;

        IReadOnlyList<InventoryItemEntry> items =
            inventoryController.Items;

        InventoryItemEntry lastVisibleItem =
            null;

        int visibleIndex = 0;

        for (int i = 0; i < items.Count; i++)
        {
            InventoryItemEntry entry =
                items[i];

            if (!MatchesCategory(entry))
                continue;

            lastVisibleItem = entry;

            if (visibleIndex ==
                Mathf.Max(0, requestedIndex))
            {
                return entry;
            }

            visibleIndex++;
        }

        // 原道具是列表最后一个时，返回新的最后一个
        return lastVisibleItem;
    }

    private void DiscardSelectedItem()
    {
        if (selectedItem == null ||
            inventoryController == null)
        {
            return;
        }

        if (!selectedItem.ItemData.CanDiscard)
            return;

        if (equipmentController != null &&
            equipmentController.IsEquipped(
                selectedItem))
        {
            return;
        }

        inventoryController.RemoveItem(
            selectedItem,
            1
        );

        selectedItem = null;

        RefreshItemGrid();
    }

    private void HandleInventoryChanged()
    {
        if (suppressInventoryRefresh)
            return;

        if (itemsPage != null &&
            itemsPage.activeSelf)
        {
            RefreshItemGrid();
        }
    }

    private void HandleEquipmentChanged()
    {
        RefreshCharacterEquipment();

        if (characterEquipmentDetail != null)
        {
            InventoryItemEntry selectedEntry =
                equipmentController
                    .GetEquippedItem(
                        selectedSlot
                    );

            SelectCharacterSlot(
                selectedSlot,
                selectedEntry
            );
        }

        if (itemsPage != null &&
            itemsPage.activeSelf)
        {
            RefreshItemGrid();
        }
    }

    private void ClearGrid()
    {
        for (int i = 0;
             i < spawnedGridItems.Count;
             i++)
        {
            InventoryGridItemView view =
                spawnedGridItems[i];

            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }

        spawnedGridItems.Clear();
    }
}