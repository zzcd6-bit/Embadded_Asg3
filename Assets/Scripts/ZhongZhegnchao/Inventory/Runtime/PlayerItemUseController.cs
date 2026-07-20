using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerItemUseController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerInventoryController inventoryController;

    [SerializeField]
    private PlayerEquipmentController equipmentController;

    [SerializeField]
    private PlayerCharacterStatsController statsController;

    [SerializeField]
    private CharacterCombatStats combatStats;

    [SerializeField]
    private PlayerDamageReceiver damageReceiver;

    [SerializeField]
    private PlayerInkPouchController inkController;

    [Header("Debug")]
    [SerializeField]
    private bool debugLog = true;

    public event Action<ConsumableItemData>
        TemporaryBuffRequested;

    private void Awake()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (inventoryController == null)
        {
            inventoryController =
                GetComponent<
                    PlayerInventoryController>();
        }

        if (equipmentController == null)
        {
            equipmentController =
                GetComponent<
                    PlayerEquipmentController>();
        }

        if (statsController == null)
        {
            statsController =
                GetComponent<
                    PlayerCharacterStatsController>();
        }

        if (damageReceiver == null)
        {
            damageReceiver =
                GetComponent<
                    PlayerDamageReceiver>();
        }

        if (inkController == null)
        {
            inkController =
                GetComponent<
                    PlayerInkPouchController>();
        }

        if (combatStats == null &&
            statsController != null)
        {
            combatStats =
                statsController.CombatStats;
        }

        if (combatStats == null)
        {
            combatStats =
                GetComponent<
                    CharacterCombatStats>();
        }

        if (combatStats == null)
        {
            combatStats =
                GetComponentInChildren<
                    CharacterCombatStats>(true);
        }
    }

    public bool TryUse(
        InventoryItemEntry entry
    )
    {
        if (entry == null ||
            inventoryController == null ||
            !inventoryController.Contains(entry) ||
            entry.ItemData is not ConsumableItemData data)
        {
            return false;
        }

        bool used = false;

        switch (data.EffectType)
        {
            case ConsumableEffectType
                .CharacterExperience:

                if (statsController != null)
                {
                    statsController.AddExperience(
                        data.Amount
                    );

                    used = true;
                }

                break;

            case ConsumableEffectType
                .WeaponExperience:

                if (equipmentController != null)
                {
                    used =
                        equipmentController
                            .AddExperienceToEquippedWeapon(
                                data.Amount
                            );
                }

                break;

            case ConsumableEffectType.HealHp:

                if (damageReceiver != null &&
                    damageReceiver.CurrentHp <
                    damageReceiver.MaxHp)
                {
                    damageReceiver.SetHp(
                        damageReceiver.CurrentHp +
                        data.Amount,
                        damageReceiver.MaxHp
                    );

                    used = true;
                }

                break;

            case ConsumableEffectType.RestoreInk:

                if (inkController != null &&
                    inkController.CurrentInk <
                    inkController.MaxInk)
                {
                    inkController.SetInk(
                        inkController.CurrentInk +
                        data.Amount,
                        inkController.MaxInk
                    );

                    used = true;
                }

                break;

            case ConsumableEffectType
                .ElementDamageBuff:

            case ConsumableEffectType
                .ElementResistanceBuff:

                used =
                    TryApplyTemporaryBuff(data);

                if (used)
                {
                    TemporaryBuffRequested?.Invoke(data);
                }

                break;
        }

        if (!used)
            return false;

        inventoryController.RemoveItem(
            entry,
            1
        );

        return true;
    }

    private bool TryApplyTemporaryBuff(
        ConsumableItemData data
    )
    {
        ResolveReferences();

        if (combatStats == null &&
            statsController != null)
        {
            statsController.Init();
            combatStats =
                statsController.CombatStats;
        }

        if (combatStats == null)
        {
            Debug.LogWarning(
                "[PlayerItemUseController] " +
                "CharacterCombatStats not found. " +
                "Temporary buff was not applied.",
                this
            );

            return false;
        }

        bool applied =
            combatStats
                .TryApplyTemporaryConsumableBuff(data);

        if (!applied)
            return false;

        if (statsController != null)
        {
            statsController.RefreshStats();
        }

        if (debugLog)
        {
            Debug.Log(
                "[PlayerItemUseController] Temporary buff used. " +
                $"Item={data.DisplayName}, " +
                $"Effect={data.EffectType}, " +
                $"Element={data.Element}, " +
                $"Value={data.PercentageValue:P0}, " +
                $"Duration={data.Duration:F1}s",
                this
            );
        }

        return true;
    }
}
