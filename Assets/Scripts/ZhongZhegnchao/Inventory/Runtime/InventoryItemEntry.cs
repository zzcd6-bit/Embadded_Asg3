using System;
using UnityEngine;

[Serializable]
public class InventoryItemEntry
{
    [SerializeField]
    private string instanceId;

    [SerializeField]
    private ItemData itemData;

    [SerializeField]
    [Min(1)]
    private int quantity = 1;

    [SerializeField]
    [Min(1)]
    private int weaponLevel = 1;

    [SerializeField]
    [Min(0)]
    private int weaponExperience;

    public string InstanceId
    {
        get { return instanceId; }
    }

    public ItemData ItemData
    {
        get { return itemData; }
    }

    public int Quantity
    {
        get { return quantity; }
    }

    public int WeaponLevel
    {
        get { return weaponLevel; }
    }

    public int WeaponExperience
    {
        get { return weaponExperience; }
    }

    public InventoryItemEntry(
        ItemData data,
        int initialQuantity
    )
        : this(
            Guid.NewGuid().ToString("N"),
            data,
            initialQuantity,
            1,
            0
        )
    {
    }

    public InventoryItemEntry(
        string savedInstanceId,
        ItemData data,
        int savedQuantity,
        int savedWeaponLevel,
        int savedWeaponExperience
    )
    {
        instanceId =
            string.IsNullOrWhiteSpace(
                savedInstanceId)
                ? Guid.NewGuid().ToString("N")
                : savedInstanceId;

        itemData = data;

        quantity =
            Mathf.Max(
                1,
                savedQuantity
            );

        weaponLevel =
            Mathf.Max(
                1,
                savedWeaponLevel
            );

        weaponExperience =
            Mathf.Max(
                0,
                savedWeaponExperience
            );
    }

    public void AddQuantity(int amount)
    {
        if (amount <= 0)
            return;

        quantity += amount;
    }

    public bool RemoveQuantity(int amount)
    {
        if (amount <= 0 ||
            quantity < amount)
        {
            return false;
        }

        quantity -= amount;

        return true;
    }

    public void SetWeaponProgress(
        int level,
        int experience
    )
    {
        weaponLevel =
            Mathf.Max(1, level);

        weaponExperience =
            Mathf.Max(0, experience);
    }
}