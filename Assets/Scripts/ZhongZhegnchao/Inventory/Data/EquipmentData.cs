using UnityEngine;

[CreateAssetMenu(
    fileName = "Equipment_",
    menuName = "Game/Inventory/Equipment"
)]
public class EquipmentData : ItemData
{
    [Header("Equipment")]
    [SerializeField]
    private EquipmentSlotType slotType;

    [SerializeField]
    private EquipmentSetData equipmentSet;

    [Header("Helmet Stats")]
    [SerializeField]
    private int maxHpBonus;

    [Header("Chest Stats")]
    [SerializeField]
    private int defenseBonus;

    [SerializeField]
    private ElementType resistanceElement =
        ElementType.Physical;

    [SerializeField]
    private float elementResistanceBonus;

    [Header("Shoes Stats")]
    [SerializeField]
    private float damageBonus;

    [SerializeField]
    private ElementType damageElement =
        ElementType.Physical;

    [SerializeField]
    private float elementDamageBonus;

    [Header("Weapon Stats")]
    [SerializeField]
    private int baseAttackPower;

    [SerializeField]
    private int attackPowerPerLevel;

    [Header("Critical Stats")]
    [SerializeField]
    private float criticalRateBonus;

    [SerializeField]
    private float criticalDamageBonus;

    [Header("Weapon Level")]
    [SerializeField]
    [Min(1)]
    private int weaponMaxLevel = 20;

    [SerializeField]
    [Min(1)]
    private int levelOneExperienceRequirement = 100;

    [SerializeField]
    [Min(0)]
    private int experienceIncreasePerLevel = 25;

    public override InventoryCategory Category
    {
        get { return InventoryCategory.Equipment; }
    }

    public override bool IsStackable
    {
        get { return false; }
    }

    public EquipmentSlotType SlotType
    {
        get { return slotType; }
    }

    public EquipmentSetData EquipmentSet
    {
        get { return equipmentSet; }
    }

    public int MaxHpBonus
    {
        get { return Mathf.Max(0, maxHpBonus); }
    }

    public int DefenseBonus
    {
        get { return Mathf.Max(0, defenseBonus); }
    }

    public ElementType ResistanceElement
    {
        get { return resistanceElement; }
    }

    public float ElementResistanceBonus
    {
        get
        {
            return Mathf.Max(
                0f,
                elementResistanceBonus
            );
        }
    }

    public float DamageBonus
    {
        get { return Mathf.Max(0f, damageBonus); }
    }

    public ElementType DamageElement
    {
        get { return damageElement; }
    }

    public float ElementDamageBonus
    {
        get
        {
            return Mathf.Max(
                0f,
                elementDamageBonus
            );
        }
    }

    public int BaseAttackPower
    {
        get { return Mathf.Max(0, baseAttackPower); }
    }

    public float CriticalRateBonus
    {
        get
        {
            return Mathf.Max(
                0f,
                criticalRateBonus
            );
        }
    }

    public float CriticalDamageBonus
    {
        get
        {
            return Mathf.Max(
                0f,
                criticalDamageBonus
            );
        }
    }

    public int WeaponMaxLevel
    {
        get
        {
            return Mathf.Max(
                1,
                weaponMaxLevel
            );
        }
    }

    public int GetAttackPowerAtLevel(int level)
    {
        if (slotType != EquipmentSlotType.Weapon)
        {
            return 0;
        }

        int finalLevel =
            Mathf.Clamp(
                level,
                1,
                WeaponMaxLevel
            );

        return BaseAttackPower +
               Mathf.Max(0, attackPowerPerLevel) *
               (finalLevel - 1);
    }

    public int GetWeaponExperienceRequirement(
        int weaponLevel
    )
    {
        int finalLevel =
            Mathf.Clamp(
                weaponLevel,
                1,
                WeaponMaxLevel
            );

        if (finalLevel >= WeaponMaxLevel)
        {
            return 0;
        }

        return Mathf.Max(
            1,
            levelOneExperienceRequirement +
            experienceIncreasePerLevel *
            (finalLevel - 1)
        );
    }
}