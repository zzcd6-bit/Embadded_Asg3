using System;

[Serializable]
public class CharacterElementStatSet
{
    public float physical;
    public float fire;
    public float water;
    public float ice;
    public float thunder;
    public float earth;
    public float wind;

    public float Get(ElementType element)
    {
        switch (element)
        {
            case ElementType.Physical:
                return physical;

            case ElementType.Fire:
                return fire;

            case ElementType.Water:
                return water;

            case ElementType.Ice:
                return ice;

            case ElementType.Thunder:
                return thunder;

            case ElementType.Earth:
                return earth;

            case ElementType.Wind:
                return wind;

            default:
                return 0f;
        }
    }

    public void AddScaled(
        CharacterElementStatSet other,
        float multiplier
    )
    {
        if (other == null)
            return;

        physical += other.physical * multiplier;
        fire += other.fire * multiplier;
        water += other.water * multiplier;
        ice += other.ice * multiplier;
        thunder += other.thunder * multiplier;
        earth += other.earth * multiplier;
        wind += other.wind * multiplier;
    }

    public CharacterElementStatSet Clone()
    {
        return new CharacterElementStatSet
        {
            physical = physical,
            fire = fire,
            water = water,
            ice = ice,
            thunder = thunder,
            earth = earth,
            wind = wind
        };
    }
}

[Serializable]
public class CharacterBaseStats
{
    public int maxHp = 100;
    public int attackPower = 10;
    public int defense;

    public float damageBonus;

    public float critRate = 0.1f;
    public float critDamage = 1.5f;

    public CharacterElementStatSet elementDamageBonus =
        new CharacterElementStatSet();

    public CharacterElementStatSet elementResistance =
        new CharacterElementStatSet();

    public CharacterBaseStats Clone()
    {
        return new CharacterBaseStats
        {
            maxHp = maxHp,
            attackPower = attackPower,
            defense = defense,

            damageBonus = damageBonus,

            critRate = critRate,
            critDamage = critDamage,

            elementDamageBonus =
                elementDamageBonus != null
                    ? elementDamageBonus.Clone()
                    : new CharacterElementStatSet(),

            elementResistance =
                elementResistance != null
                    ? elementResistance.Clone()
                    : new CharacterElementStatSet()
        };
    }
}

[Serializable]
public class CharacterLevelGrowthStats
{
    public int maxHpPerLevel = 10;
    public int attackPowerPerLevel = 2;
    public int defensePerLevel = 1;

    public float damageBonusPerLevel;

    public CharacterElementStatSet
        elementDamageBonusPerLevel =
            new CharacterElementStatSet();

    public CharacterElementStatSet
        elementResistancePerLevel =
            new CharacterElementStatSet();
}

[Serializable]
public class CharacterStatModifiers
{
    public int maxHp;
    public int attackPower;
    public int defense;

    public float damageBonus;

    public float critRate;
    public float critDamage;

    public CharacterElementStatSet elementDamageBonus =
        new CharacterElementStatSet();

    public CharacterElementStatSet elementResistance =
        new CharacterElementStatSet();

    public CharacterStatModifiers Clone()
    {
        return new CharacterStatModifiers
        {
            maxHp = maxHp,
            attackPower = attackPower,
            defense = defense,

            damageBonus = damageBonus,

            critRate = critRate,
            critDamage = critDamage,

            elementDamageBonus =
                elementDamageBonus != null
                    ? elementDamageBonus.Clone()
                    : new CharacterElementStatSet(),

            elementResistance =
                elementResistance != null
                    ? elementResistance.Clone()
                    : new CharacterElementStatSet()
        };
    }
}