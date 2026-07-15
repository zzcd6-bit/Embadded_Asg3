using UnityEngine;

public class CharacterCombatStats : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField]
    private CharacterBaseStats baseStats =
        new CharacterBaseStats();

    [Header("Equipment Modifiers")]
    [SerializeField]
    private CharacterStatModifiers equipmentModifiers =
        new CharacterStatModifiers();

    public int MaxHp
    {
        get
        {
            return Mathf.Max(
                1,
                baseStats.maxHp +
                equipmentModifiers.maxHp
            );
        }
    }

    public int AttackPower
    {
        get
        {
            return Mathf.Max(
                0,
                baseStats.attackPower +
                equipmentModifiers.attackPower
            );
        }
    }

    public int Defense
    {
        get
        {
            return Mathf.Max(
                0,
                baseStats.defense +
                equipmentModifiers.defense
            );
        }
    }

    public float DamageBonus
    {
        get
        {
            return
                baseStats.damageBonus +
                equipmentModifiers.damageBonus;
        }
    }

    public float CritRate
    {
        get
        {
            return Mathf.Clamp01(
                baseStats.critRate +
                equipmentModifiers.critRate
            );
        }
    }

    public float CritDamage
    {
        get
        {
            return Mathf.Max(
                1f,
                baseStats.critDamage +
                equipmentModifiers.critDamage
            );
        }
    }

    public void SetBaseStats(
        CharacterBaseStats newStats
    )
    {
        baseStats =
            newStats != null
                ? newStats.Clone()
                : new CharacterBaseStats();
    }

    public void SetEquipmentModifiers(
        CharacterStatModifiers modifiers
    )
    {
        equipmentModifiers =
            modifiers != null
                ? modifiers.Clone()
                : new CharacterStatModifiers();
    }

    public void ClearEquipmentModifiers()
    {
        equipmentModifiers =
            new CharacterStatModifiers();
    }

    public float GetElementDamageBonus(
        ElementType element
    )
    {
        float baseValue =
            baseStats.elementDamageBonus != null
                ? baseStats
                    .elementDamageBonus
                    .Get(element)
                : 0f;

        float modifierValue =
            equipmentModifiers
                .elementDamageBonus != null
                ? equipmentModifiers
                    .elementDamageBonus
                    .Get(element)
                : 0f;

        return baseValue + modifierValue;
    }

    public float GetElementResistance(
        ElementType element
    )
    {
        float baseValue =
            baseStats.elementResistance != null
                ? baseStats
                    .elementResistance
                    .Get(element)
                : 0f;

        float modifierValue =
            equipmentModifiers
                .elementResistance != null
                ? equipmentModifiers
                    .elementResistance
                    .Get(element)
                : 0f;

        return baseValue + modifierValue;
    }

    public CharacterBaseStats
        GetBaseStatsSnapshot()
    {
        return baseStats.Clone();
    }

    public CharacterStatModifiers
        GetEquipmentModifiersSnapshot()
    {
        return equipmentModifiers.Clone();
    }
}