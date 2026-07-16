using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventoryItemSaveData
{
    public string instanceId;
    public string itemId;

    public int quantity = 1;

    public int weaponLevel = 1;
    public int weaponExperience;
}

[Serializable]
public class PlayerSaveData
{
    public Vector3 position;
    public Vector3 eulerAngles;

    public int characterLevel = 1;
    public int currentExperience = 0;

    public int currentHp;
    public int maxHp;

    public int currentInk;
    public int maxInk;

    public List<string> unlockedBrushSkills = new List<string>();

    [Header("Inventory")]
    public bool hasInventoryData;

    public List<InventoryItemSaveData> inventoryItems =
        new List<InventoryItemSaveData>();

    [Header("Equipped Items")]
    public string equippedHelmetInstanceId;
    public string equippedChestInstanceId;
    public string equippedShoesInstanceId;
    public string equippedWeaponInstanceId;

    [Header("Collected Pickups")]
    public List<string> collectedPickupIds =
        new List<string>();

    [Header("Currency")]
    public bool hasCurrencyData;

    public int currentCoins;
}