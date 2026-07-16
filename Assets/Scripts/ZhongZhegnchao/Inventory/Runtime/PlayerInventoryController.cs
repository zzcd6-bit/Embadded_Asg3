using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInventoryController : MonoBehaviour
{
    [Serializable]
    private class StartingItem
    {
        public ItemData itemData;

        [Min(1)]
        public int quantity = 1;
    }

    [Header("Database")]
    [SerializeField]
    private ItemDatabase itemDatabase;

    [Header("Starting Items")]
    [SerializeField]
    private List<StartingItem> startingItems =
        new List<StartingItem>();

    [Header("Debug")]
    [SerializeField]
    private bool debugLog;

    private readonly List<InventoryItemEntry> items =
        new List<InventoryItemEntry>();

    private bool initialized;

    public event Action InventoryChanged;

    public IReadOnlyList<InventoryItemEntry> Items
    {
        get { return items; }
    }

    public ItemDatabase ItemDatabase
    {
        get { return itemDatabase; }
    }

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (initialized)
            return;

        initialized = true;

        for (int i = 0;
             i < startingItems.Count;
             i++)
        {
            StartingItem startingItem =
                startingItems[i];

            if (startingItem == null ||
                startingItem.itemData == null)
            {
                continue;
            }

            AddItemInternal(
                startingItem.itemData,
                startingItem.quantity
            );
        }

        InventoryChanged?.Invoke();
    }

    public InventoryItemEntry FindByInstanceId(
    string instanceId
)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return null;

        for (int i = 0;
             i < items.Count;
             i++)
        {
            InventoryItemEntry entry = items[i];

            if (entry == null)
                continue;

            if (entry.InstanceId == instanceId)
            {
                return entry;
            }
        }

        return null;
    }

    public void CaptureSaveData(
    List<InventoryItemSaveData> output
)
    {
        if (output == null)
            return;

        output.Clear();

        for (int i = 0;
             i < items.Count;
             i++)
        {
            InventoryItemEntry entry = items[i];

            if (entry == null ||
                entry.ItemData == null)
            {
                continue;
            }

            output.Add(
                new InventoryItemSaveData
                {
                    instanceId =
                        entry.InstanceId,

                    itemId =
                        entry.ItemData.ItemId,

                    quantity =
                        entry.Quantity,

                    weaponLevel =
                        entry.WeaponLevel,

                    weaponExperience =
                        entry.WeaponExperience
                }
            );
        }
    }

    public void RestoreFromSaveData(
    List<InventoryItemSaveData> savedItems
)
    {
        items.Clear();

        initialized = true;

        if (savedItems == null)
        {
            InventoryChanged?.Invoke();
            return;
        }

        if (itemDatabase == null)
        {
            Debug.LogError(
                "[PlayerInventoryController] " +
                "ItemDatabase is missing.",
                this
            );

            InventoryChanged?.Invoke();
            return;
        }

        for (int i = 0;
             i < savedItems.Count;
             i++)
        {
            InventoryItemSaveData savedItem =
                savedItems[i];

            if (savedItem == null ||
                string.IsNullOrWhiteSpace(
                    savedItem.itemId))
            {
                continue;
            }

            ItemData itemData =
                itemDatabase.GetItemById(
                    savedItem.itemId
                );

            if (itemData == null)
            {
                Debug.LogWarning(
                    "[PlayerInventoryController] " +
                    $"Cannot find ItemData: " +
                    $"{savedItem.itemId}",
                    this
                );

                continue;
            }

            InventoryItemEntry entry =
                new InventoryItemEntry(
                    savedItem.instanceId,
                    itemData,
                    savedItem.quantity,
                    savedItem.weaponLevel,
                    savedItem.weaponExperience
                );

            items.Add(entry);
        }

        InventoryChanged?.Invoke();

        if (debugLog)
        {
            Debug.Log(
                "[PlayerInventoryController] " +
                $"Inventory restored. Count={items.Count}",
                this
            );
        }
    }

    public InventoryItemEntry AddItem(
        ItemData itemData,
        int quantity = 1
    )
    {
        if (itemData == null ||
            quantity <= 0)
        {
            return null;
        }

        InventoryItemEntry result =
            AddItemInternal(
                itemData,
                quantity
            );

        InventoryChanged?.Invoke();

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerInventoryController] " +
                $"Added {itemData.DisplayName} x{quantity}",
                this
            );
        }

        return result;
    }

    private InventoryItemEntry AddItemInternal(
        ItemData itemData,
        int quantity
    )
    {
        if (itemData.IsStackable)
        {
            InventoryItemEntry existing =
                FindStackableItem(itemData);

            if (existing != null)
            {
                existing.AddQuantity(quantity);
                return existing;
            }

            InventoryItemEntry newStack =
                new InventoryItemEntry(
                    itemData,
                    quantity
                );

            items.Add(newStack);

            return newStack;
        }

        InventoryItemEntry firstCreated = null;

        for (int i = 0; i < quantity; i++)
        {
            InventoryItemEntry entry =
                new InventoryItemEntry(
                    itemData,
                    1
                );

            items.Add(entry);

            if (firstCreated == null)
            {
                firstCreated = entry;
            }
        }

        return firstCreated;
    }

    private InventoryItemEntry FindStackableItem(
        ItemData itemData
    )
    {
        for (int i = 0;
             i < items.Count;
             i++)
        {
            InventoryItemEntry entry =
                items[i];

            if (entry == null)
                continue;

            if (entry.ItemData == itemData)
            {
                return entry;
            }
        }

        return null;
    }

    public bool RemoveItem(
        InventoryItemEntry entry,
        int quantity = 1
    )
    {
        if (entry == null ||
            quantity <= 0 ||
            !items.Contains(entry))
        {
            return false;
        }

        if (entry.ItemData != null &&
            entry.ItemData.IsStackable &&
            entry.Quantity > quantity)
        {
            entry.RemoveQuantity(quantity);
        }
        else
        {
            items.Remove(entry);
        }

        InventoryChanged?.Invoke();

        return true;
    }

    public bool Contains(
        InventoryItemEntry entry
    )
    {
        return entry != null &&
               items.Contains(entry);
    }

    public void NotifyInventoryChanged()
    {
        InventoryChanged?.Invoke();
    }
}