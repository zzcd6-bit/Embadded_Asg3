using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ItemDatabase",
    menuName = "游戏/背包系统/道具数据库"
)]
public class ItemDatabase : ScriptableObject
{
    [SerializeField]
    private List<ItemData> allItems =
        new List<ItemData>();

    private Dictionary<string, ItemData> itemLookup;

    public IReadOnlyList<ItemData> AllItems
    {
        get { return allItems; }
    }

    public ItemData GetItemById(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        BuildLookupIfNeeded();

        itemLookup.TryGetValue(
            itemId,
            out ItemData result
        );

        return result;
    }

    public bool TryGetItem(
        string itemId,
        out ItemData itemData
    )
    {
        BuildLookupIfNeeded();

        return itemLookup.TryGetValue(
            itemId,
            out itemData
        );
    }

    private void BuildLookupIfNeeded()
    {
        if (itemLookup != null)
            return;

        itemLookup =
            new Dictionary<string, ItemData>();

        for (int i = 0;
             i < allItems.Count;
             i++)
        {
            ItemData itemData = allItems[i];

            if (itemData == null)
                continue;

            if (string.IsNullOrWhiteSpace(
                    itemData.ItemId))
            {
                Debug.LogWarning(
                    $"[ItemDatabase] " +
                    $"Item ID is empty: {itemData.name}",
                    itemData
                );

                continue;
            }

            if (itemLookup.ContainsKey(
                    itemData.ItemId))
            {
                Debug.LogError(
                    $"[ItemDatabase] Duplicate Item ID: " +
                    $"{itemData.ItemId}",
                    itemData
                );

                continue;
            }

            itemLookup.Add(
                itemData.ItemId,
                itemData
            );
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        itemLookup = null;
    }
#endif
}