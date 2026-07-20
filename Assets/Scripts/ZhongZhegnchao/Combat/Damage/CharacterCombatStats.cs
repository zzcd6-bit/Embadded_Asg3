using System.Collections.Generic;
using UnityEngine;

public class CharacterCombatStats : MonoBehaviour
{
    private enum TemporaryBuffStatType
    {
        ElementDamageBonus,
        ElementResistance
    }

    private class TemporaryElementBuff
    {
        public TemporaryBuffStatType statType;
        public ElementType element;
        public float value;
        public float remainingDuration;
        public float duration;
    }

    [Header("Base Stats")]
    [SerializeField]
    private CharacterBaseStats baseStats =
        new CharacterBaseStats();

    [Header("Equipment Modifiers")]
    [SerializeField]
    private CharacterStatModifiers equipmentModifiers =
        new CharacterStatModifiers();

    [Header("Runtime Temporary Modifiers")]
    [SerializeField]
    private CharacterStatModifiers temporaryModifiers =
        new CharacterStatModifiers();

    [Header("Debug")]
    [SerializeField]
    private bool debugTemporaryBuffs = true;

    private readonly List<TemporaryElementBuff>
        activeTemporaryBuffs =
            new List<TemporaryElementBuff>();

    public int MaxHp
    {
        get
        {
            return Mathf.Max(
                1,
                GetBaseStats().maxHp +
                GetEquipmentModifiers().maxHp +
                GetTemporaryModifiers().maxHp
            );
        }
    }

    public int AttackPower
    {
        get
        {
            return Mathf.Max(
                0,
                GetBaseStats().attackPower +
                GetEquipmentModifiers().attackPower +
                GetTemporaryModifiers().attackPower
            );
        }
    }

    public int Defense
    {
        get
        {
            return Mathf.Max(
                0,
                GetBaseStats().defense +
                GetEquipmentModifiers().defense +
                GetTemporaryModifiers().defense
            );
        }
    }

    public float DamageBonus
    {
        get
        {
            return
                GetBaseStats().damageBonus +
                GetEquipmentModifiers().damageBonus +
                GetTemporaryModifiers().damageBonus;
        }
    }

    public float CritRate
    {
        get
        {
            return Mathf.Clamp01(
                GetBaseStats().critRate +
                GetEquipmentModifiers().critRate +
                GetTemporaryModifiers().critRate
            );
        }
    }

    public float CritDamage
    {
        get
        {
            return Mathf.Max(
                1f,
                GetBaseStats().critDamage +
                GetEquipmentModifiers().critDamage +
                GetTemporaryModifiers().critDamage
            );
        }
    }

    private void Awake()
    {
        EnsureStatContainers();
    }

    private void OnValidate()
    {
        EnsureStatContainers();
    }

    private void Update()
    {
        TickTemporaryBuffs();
    }

    public void SetBaseStats(
        CharacterBaseStats newStats
    )
    {
        baseStats =
            newStats != null
                ? newStats.Clone()
                : new CharacterBaseStats();

        EnsureStatContainers();
    }

    public void SetEquipmentModifiers(
        CharacterStatModifiers modifiers
    )
    {
        equipmentModifiers =
            modifiers != null
                ? modifiers.Clone()
                : new CharacterStatModifiers();

        EnsureStatContainers();
    }

    public void ClearEquipmentModifiers()
    {
        equipmentModifiers =
            new CharacterStatModifiers();

        EnsureStatContainers();
    }

    public bool TryApplyTemporaryConsumableBuff(
        ConsumableItemData data
    )
    {
        if (data == null)
            return false;

        switch (data.EffectType)
        {
            case ConsumableEffectType.ElementDamageBuff:
                return AddTemporaryElementDamageBonus(
                    data.Element,
                    data.PercentageValue,
                    data.Duration
                );

            case ConsumableEffectType.ElementResistanceBuff:
                return AddTemporaryElementResistance(
                    data.Element,
                    data.PercentageValue,
                    data.Duration
                );

            default:
                return false;
        }
    }

    public bool AddTemporaryElementDamageBonus(
        ElementType element,
        float value,
        float duration
    )
    {
        return AddTemporaryElementBuff(
            TemporaryBuffStatType.ElementDamageBonus,
            element,
            value,
            duration
        );
    }

    public bool AddTemporaryElementResistance(
        ElementType element,
        float value,
        float duration
    )
    {
        return AddTemporaryElementBuff(
            TemporaryBuffStatType.ElementResistance,
            element,
            value,
            duration
        );
    }

    public void ClearTemporaryBuffs()
    {
        if (activeTemporaryBuffs.Count <= 0)
            return;

        activeTemporaryBuffs.Clear();
        RebuildTemporaryModifiers();
    }

    public float GetElementDamageBonus(
        ElementType element
    )
    {
        float baseValue =
            GetBaseStats().elementDamageBonus != null
                ? GetBaseStats()
                    .elementDamageBonus
                    .Get(element)
                : 0f;

        float equipmentValue =
            GetEquipmentModifiers()
                .elementDamageBonus != null
                ? GetEquipmentModifiers()
                    .elementDamageBonus
                    .Get(element)
                : 0f;

        float temporaryValue =
            GetTemporaryModifiers()
                .elementDamageBonus != null
                ? GetTemporaryModifiers()
                    .elementDamageBonus
                    .Get(element)
                : 0f;

        return baseValue +
               equipmentValue +
               temporaryValue;
    }

    public float GetElementResistance(
        ElementType element
    )
    {
        float baseValue =
            GetBaseStats().elementResistance != null
                ? GetBaseStats()
                    .elementResistance
                    .Get(element)
                : 0f;

        float equipmentValue =
            GetEquipmentModifiers()
                .elementResistance != null
                ? GetEquipmentModifiers()
                    .elementResistance
                    .Get(element)
                : 0f;

        float temporaryValue =
            GetTemporaryModifiers()
                .elementResistance != null
                ? GetTemporaryModifiers()
                    .elementResistance
                    .Get(element)
                : 0f;

        return baseValue +
               equipmentValue +
               temporaryValue;
    }

    public CharacterBaseStats
        GetBaseStatsSnapshot()
    {
        return GetBaseStats().Clone();
    }

    public CharacterStatModifiers
        GetEquipmentModifiersSnapshot()
    {
        return GetEquipmentModifiers().Clone();
    }

    public CharacterStatModifiers
        GetTemporaryModifiersSnapshot()
    {
        return GetTemporaryModifiers().Clone();
    }

    private bool AddTemporaryElementBuff(
        TemporaryBuffStatType statType,
        ElementType element,
        float value,
        float duration
    )
    {
        if (element == ElementType.None)
            return false;

        if (value <= 0f)
            return false;

        if (duration <= 0f)
            return false;

        TemporaryElementBuff existingBuff =
            FindTemporaryElementBuff(
                statType,
                element
            );

        if (existingBuff != null)
        {
            existingBuff.value = value;
            existingBuff.duration = duration;
            existingBuff.remainingDuration = duration;
        }
        else
        {
            activeTemporaryBuffs.Add(
                new TemporaryElementBuff
                {
                    statType = statType,
                    element = element,
                    value = value,
                    duration = duration,
                    remainingDuration = duration
                }
            );
        }

        RebuildTemporaryModifiers();

        if (debugTemporaryBuffs)
        {
            Debug.Log(
                "[CharacterCombatStats] Temporary buff applied. " +
                $"Type={statType}, " +
                $"Element={element}, " +
                $"Value={value:P0}, " +
                $"Duration={duration:F1}s",
                this
            );
        }

        return true;
    }

    private TemporaryElementBuff FindTemporaryElementBuff(
        TemporaryBuffStatType statType,
        ElementType element
    )
    {
        for (int i = 0; i < activeTemporaryBuffs.Count; i++)
        {
            TemporaryElementBuff buff =
                activeTemporaryBuffs[i];

            if (buff.statType == statType &&
                buff.element == element)
            {
                return buff;
            }
        }

        return null;
    }

    private void TickTemporaryBuffs()
    {
        if (activeTemporaryBuffs.Count <= 0)
            return;

        bool hasExpiredBuff = false;

        for (int i = activeTemporaryBuffs.Count - 1;
             i >= 0;
             i--)
        {
            TemporaryElementBuff buff =
                activeTemporaryBuffs[i];

            buff.remainingDuration -= Time.deltaTime;

            if (buff.remainingDuration > 0f)
                continue;

            if (debugTemporaryBuffs)
            {
                Debug.Log(
                    "[CharacterCombatStats] Temporary buff expired. " +
                    $"Type={buff.statType}, " +
                    $"Element={buff.element}",
                    this
                );
            }

            activeTemporaryBuffs.RemoveAt(i);
            hasExpiredBuff = true;
        }

        if (hasExpiredBuff)
        {
            RebuildTemporaryModifiers();
        }
    }

    private void RebuildTemporaryModifiers()
    {
        temporaryModifiers =
            new CharacterStatModifiers();

        for (int i = 0; i < activeTemporaryBuffs.Count; i++)
        {
            TemporaryElementBuff buff =
                activeTemporaryBuffs[i];

            switch (buff.statType)
            {
                case TemporaryBuffStatType.ElementDamageBonus:
                    temporaryModifiers
                        .elementDamageBonus
                        .Add(
                            buff.element,
                            buff.value
                        );
                    break;

                case TemporaryBuffStatType.ElementResistance:
                    temporaryModifiers
                        .elementResistance
                        .Add(
                            buff.element,
                            buff.value
                        );
                    break;
            }
        }

        EnsureStatContainers();
    }

    private CharacterBaseStats GetBaseStats()
    {
        if (baseStats == null)
        {
            baseStats =
                new CharacterBaseStats();
        }

        return baseStats;
    }

    private CharacterStatModifiers GetEquipmentModifiers()
    {
        if (equipmentModifiers == null)
        {
            equipmentModifiers =
                new CharacterStatModifiers();
        }

        return equipmentModifiers;
    }

    private CharacterStatModifiers GetTemporaryModifiers()
    {
        if (temporaryModifiers == null)
        {
            temporaryModifiers =
                new CharacterStatModifiers();
        }

        return temporaryModifiers;
    }

    private void EnsureStatContainers()
    {
        if (baseStats == null)
        {
            baseStats =
                new CharacterBaseStats();
        }

        if (baseStats.elementDamageBonus == null)
        {
            baseStats.elementDamageBonus =
                new CharacterElementStatSet();
        }

        if (baseStats.elementResistance == null)
        {
            baseStats.elementResistance =
                new CharacterElementStatSet();
        }

        if (equipmentModifiers == null)
        {
            equipmentModifiers =
                new CharacterStatModifiers();
        }

        if (equipmentModifiers.elementDamageBonus == null)
        {
            equipmentModifiers.elementDamageBonus =
                new CharacterElementStatSet();
        }

        if (equipmentModifiers.elementResistance == null)
        {
            equipmentModifiers.elementResistance =
                new CharacterElementStatSet();
        }

        if (temporaryModifiers == null)
        {
            temporaryModifiers =
                new CharacterStatModifiers();
        }

        if (temporaryModifiers.elementDamageBonus == null)
        {
            temporaryModifiers.elementDamageBonus =
                new CharacterElementStatSet();
        }

        if (temporaryModifiers.elementResistance == null)
        {
            temporaryModifiers.elementResistance =
                new CharacterElementStatSet();
        }
    }
}
