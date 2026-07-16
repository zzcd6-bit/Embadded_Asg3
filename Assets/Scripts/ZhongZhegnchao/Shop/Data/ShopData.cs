using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShopProductConfig
{
    [SerializeField]
    private ItemData itemData;

    [SerializeField]
    [Min(1)]
    private int price = 100;

    [SerializeField]
    [Min(1)]
    private int quantityPerPurchase = 1;

    public ItemData ItemData
    {
        get { return itemData; }
    }

    public int Price
    {
        get
        {
            return Mathf.Max(
                1,
                price
            );
        }
    }

    public int QuantityPerPurchase
    {
        get
        {
            return Mathf.Max(
                1,
                quantityPerPurchase
            );
        }
    }

    public bool IsValid
    {
        get
        {
            return itemData != null &&
                   price > 0 &&
                   quantityPerPurchase > 0;
        }
    }
}

[CreateAssetMenu(
    fileName = "Shop_New",
    menuName = "游戏/商店系统/商店配置"
)]
public class ShopData : ScriptableObject
{
    [Header("商店信息")]
    [SerializeField]
    private string shopId;

    [SerializeField]
    private string displayName = "商店";

    [Header("售卖商品")]
    [SerializeField]
    private List<ShopProductConfig> products =
        new List<ShopProductConfig>();

    public string ShopId
    {
        get { return shopId; }
    }

    public string DisplayName
    {
        get
        {
            return string.IsNullOrWhiteSpace(
                displayName
            )
                ? name
                : displayName;
        }
    }

    public IReadOnlyList<ShopProductConfig> Products
    {
        get { return products; }
    }
}