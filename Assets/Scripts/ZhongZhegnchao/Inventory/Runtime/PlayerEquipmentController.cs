using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerEquipmentController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerInventoryController inventoryController;

    [SerializeField]
    private PlayerCharacterStatsController statsController;

    private InventoryItemEntry helmet;
    private InventoryItemEntry chest;
    private InventoryItemEntry shoes;
    private InventoryItemEntry weapon;

    public event Action EquipmentChanged;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (inventoryController != null)
        {
            inventoryController.InventoryChanged +=
                ValidateEquippedItems;
        }
    }

    private void Start()
    {
        RebuildEquipmentModifiers();
    }

    private void OnDisable()
    {
        if (inventoryController != null)
        {
            inventoryController.InventoryChanged -=
                ValidateEquippedItems;
        }
    }

    private void ResolveReferences()
    {
        if (inventoryController == null)
        {
            inventoryController =
                GetComponent<
                    PlayerInventoryController>();
        }

        if (statsController == null)
        {
            statsController =
                GetComponent<
                    PlayerCharacterStatsController>();
        }

        if (statsController == null)
        {
            statsController =
                GetComponentInChildren<
                    PlayerCharacterStatsController>(true);
        }
    }

    public InventoryItemEntry GetEquippedItem(
        EquipmentSlotType slotType
    )
    {
        switch (slotType)
        {
            case EquipmentSlotType.Helmet:
                return helmet;

            case EquipmentSlotType.Chest:
                return chest;

            case EquipmentSlotType.Shoes:
                return shoes;

            case EquipmentSlotType.Weapon:
                return weapon;

            default:
                return null;
        }
    }

    public bool IsEquipped(
        InventoryItemEntry entry
    )
    {
        if (entry == null)
            return false;

        return helmet == entry ||
               chest == entry ||
               shoes == entry ||
               weapon == entry;
    }

    public bool Equip(
        InventoryItemEntry entry
    )
    {
        if (entry == null ||
            entry.ItemData is not EquipmentData data)
        {
            return false;
        }

        if (inventoryController != null &&
            !inventoryController.Contains(entry))
        {
            return false;
        }

        switch (data.SlotType)
        {
            case EquipmentSlotType.Helmet:
                helmet = entry;
                break;

            case EquipmentSlotType.Chest:
                chest = entry;
                break;

            case EquipmentSlotType.Shoes:
                shoes = entry;
                break;

            case EquipmentSlotType.Weapon:
                weapon = entry;
                break;

            default:
                return false;
        }

        RebuildEquipmentModifiers();

        EquipmentChanged?.Invoke();

        inventoryController?.NotifyInventoryChanged();

        return true;
    }

    public void Unequip(
        EquipmentSlotType slotType
    )
    {
        switch (slotType)
        {
            case EquipmentSlotType.Helmet:
                helmet = null;
                break;

            case EquipmentSlotType.Chest:
                chest = null;
                break;

            case EquipmentSlotType.Shoes:
                shoes = null;
                break;

            case EquipmentSlotType.Weapon:
                weapon = null;
                break;
        }

        RebuildEquipmentModifiers();

        EquipmentChanged?.Invoke();

        inventoryController?.NotifyInventoryChanged();
    }

    public int GetSetCount(
        EquipmentSetData setData
    )
    {
        if (setData == null)
            return 0;

        int count = 0;

        if (IsFromSet(helmet, setData))
            count++;

        if (IsFromSet(chest, setData))
            count++;

        if (IsFromSet(shoes, setData))
            count++;

        if (IsFromSet(weapon, setData))
            count++;

        return count;
    }

    private bool IsFromSet(
        InventoryItemEntry entry,
        EquipmentSetData setData
    )
    {
        if (entry?.ItemData is not EquipmentData data)
        {
            return false;
        }

        return data.EquipmentSet == setData;
    }

    public bool AddExperienceToEquippedWeapon(
        int amount
    )
    {
        if (amount <= 0 ||
            weapon == null ||
            weapon.ItemData is not EquipmentData weaponData)
        {
            return false;
        }

        int level =
            weapon.WeaponLevel;

        int experience =
            weapon.WeaponExperience +
            amount;

        while (level < weaponData.WeaponMaxLevel)
        {
            int required =
                weaponData
                    .GetWeaponExperienceRequirement(
                        level
                    );

            if (required <= 0 ||
                experience < required)
            {
                break;
            }

            experience -= required;
            level++;
        }

        if (level >= weaponData.WeaponMaxLevel)
        {
            level =
                weaponData.WeaponMaxLevel;

            experience = 0;
        }

        weapon.SetWeaponProgress(
            level,
            experience
        );

        RebuildEquipmentModifiers();

        EquipmentChanged?.Invoke();

        inventoryController?.NotifyInventoryChanged();

        return true;
    }

    private void ValidateEquippedItems()
    {
        bool changed = false;

        if (!IsValidInventoryItem(helmet))
        {
            helmet = null;
            changed = true;
        }

        if (!IsValidInventoryItem(chest))
        {
            chest = null;
            changed = true;
        }

        if (!IsValidInventoryItem(shoes))
        {
            shoes = null;
            changed = true;
        }

        if (!IsValidInventoryItem(weapon))
        {
            weapon = null;
            changed = true;
        }

        if (!changed)
            return;

        RebuildEquipmentModifiers();
        EquipmentChanged?.Invoke();
    }

    private bool IsValidInventoryItem(
        InventoryItemEntry entry
    )
    {
        return entry == null ||
               inventoryController == null ||
               inventoryController.Contains(entry);
    }

    private void RebuildEquipmentModifiers()
    {
        if (statsController == null)
            return;

        CharacterStatModifiers modifiers =
            new CharacterStatModifiers();

        AddEquipmentModifiers(
            modifiers,
            helmet
        );

        AddEquipmentModifiers(
            modifiers,
            chest
        );

        AddEquipmentModifiers(
            modifiers,
            shoes
        );

        AddEquipmentModifiers(
            modifiers,
            weapon
        );

        ApplyFourPieceSetBonus(
            modifiers
        );

        statsController.SetEquipmentModifiers(
            modifiers
        );
    }

    private void AddEquipmentModifiers(
        CharacterStatModifiers modifiers,
        InventoryItemEntry entry
    )
    {
        if (modifiers == null ||
            entry?.ItemData is not EquipmentData data)
        {
            return;
        }

        modifiers.maxHp +=
            data.MaxHpBonus;

        modifiers.defense +=
            data.DefenseBonus;

        modifiers.damageBonus +=
            data.DamageBonus;

        modifiers.critRate +=
            data.CriticalRateBonus;

        modifiers.critDamage +=
            data.CriticalDamageBonus;

        if (data.SlotType ==
            EquipmentSlotType.Weapon)
        {
            modifiers.attackPower +=
                data.GetAttackPowerAtLevel(
                    entry.WeaponLevel
                );
        }

        AddElementValue(
            modifiers.elementDamageBonus,
            data.DamageElement,
            data.ElementDamageBonus
        );

        AddElementValue(
            modifiers.elementResistance,
            data.ResistanceElement,
            data.ElementResistanceBonus
        );
    }

    private void ApplyFourPieceSetBonus(
        CharacterStatModifiers modifiers
    )
    {
        EquipmentSetData setData =
            GetFirstEquippedSet();

        if (setData == null)
            return;

        if (GetSetCount(setData) < 4)
            return;

        AddElementValue(
            modifiers.elementDamageBonus,
            setData.Element,
            setData.ElementDamageBonus
        );

        AddElementValue(
            modifiers.elementResistance,
            setData.Element,
            setData.ElementResistanceBonus
        );
    }

    private EquipmentSetData GetFirstEquippedSet()
    {
        InventoryItemEntry[] equippedItems =
        {
            helmet,
            chest,
            shoes,
            weapon
        };

        for (int i = 0;
             i < equippedItems.Length;
             i++)
        {
            if (equippedItems[i]?.ItemData
                is EquipmentData data &&
                data.EquipmentSet != null)
            {
                return data.EquipmentSet;
            }
        }

        return null;
    }

    private void AddElementValue(
        CharacterElementStatSet target,
        ElementType element,
        float value
    )
    {
        if (target == null ||
            Mathf.Approximately(value, 0f))
        {
            return;
        }

        switch (element)
        {
            case ElementType.Physical:
                target.physical += value;
                break;

            case ElementType.Fire:
                target.fire += value;
                break;

            case ElementType.Water:
                target.water += value;
                break;

            case ElementType.Ice:
                target.ice += value;
                break;

            case ElementType.Thunder:
                target.thunder += value;
                break;

            case ElementType.Earth:
                target.earth += value;
                break;

            case ElementType.Wind:
                target.wind += value;
                break;
        }
    }

    public void CaptureSaveData(
    PlayerSaveData saveData
)
    {
        if (saveData == null)
            return;

        saveData.equippedHelmetInstanceId =
            helmet != null
                ? helmet.InstanceId
                : string.Empty;

        saveData.equippedChestInstanceId =
            chest != null
                ? chest.InstanceId
                : string.Empty;

        saveData.equippedShoesInstanceId =
            shoes != null
                ? shoes.InstanceId
                : string.Empty;

        saveData.equippedWeaponInstanceId =
            weapon != null
                ? weapon.InstanceId
                : string.Empty;
    }

    public void RestoreFromSaveData(
    PlayerSaveData saveData
)
    {
        if (saveData == null ||
            inventoryController == null)
        {
            return;
        }

        helmet = ResolveSavedEquipment(
            saveData.equippedHelmetInstanceId,
            EquipmentSlotType.Helmet
        );

        chest = ResolveSavedEquipment(
            saveData.equippedChestInstanceId,
            EquipmentSlotType.Chest
        );

        shoes = ResolveSavedEquipment(
            saveData.equippedShoesInstanceId,
            EquipmentSlotType.Shoes
        );

        weapon = ResolveSavedEquipment(
            saveData.equippedWeaponInstanceId,
            EquipmentSlotType.Weapon
        );

        RebuildEquipmentModifiers();

        EquipmentChanged?.Invoke();

        inventoryController
            .NotifyInventoryChanged();
    }

    private InventoryItemEntry ResolveSavedEquipment(
    string instanceId,
    EquipmentSlotType expectedSlot
)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return null;

        InventoryItemEntry entry =
            inventoryController.FindByInstanceId(
                instanceId
            );

        if (entry == null)
        {
            Debug.LogWarning(
                "[PlayerEquipmentController] " +
                $"Saved equipment instance not found: " +
                $"{instanceId}",
                this
            );

            return null;
        }

        if (entry.ItemData is not EquipmentData data)
        {
            return null;
        }

        if (data.SlotType != expectedSlot)
        {
            Debug.LogWarning(
                "[PlayerEquipmentController] " +
                $"Equipment slot mismatch. " +
                $"Expected={expectedSlot}, " +
                $"Actual={data.SlotType}",
                this
            );

            return null;
        }

        return entry;
    }
}