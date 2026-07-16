public static class InventoryDisplayNameUtility
{
    public static string GetEquipmentSlotName(
        EquipmentSlotType slotType,
        bool useChinese
    )
    {
        if (!useChinese)
        {
            return slotType.ToString();
        }

        switch (slotType)
        {
            case EquipmentSlotType.Helmet:
                return "Helmet";

            case EquipmentSlotType.Chest:
                return "Chest";

            case EquipmentSlotType.Shoes:
                return "Shoes";

            case EquipmentSlotType.Weapon:
                return "Weapon";

            default:
                return slotType.ToString();
        }
    }

    public static string GetQualityName(
        ItemQuality quality,
        bool useChinese
    )
    {
        if (!useChinese)
        {
            return quality.ToString();
        }

        switch (quality)
        {
            case ItemQuality.Common:
                return "Common";

            case ItemQuality.Rare:
                return "Rare";

            case ItemQuality.Legendary:
                return "Legendary";

            default:
                return quality.ToString();
        }
    }

    public static string GetElementName(
        ElementType element,
        bool useChinese
    )
    {
        if (!useChinese)
        {
            return element.ToString();
        }

        switch (element)
        {
            case ElementType.Physical:
                return "Physica";

            case ElementType.Fire:
                return "Fire";

            case ElementType.Water:
                return "Water";

            case ElementType.Ice:
                return "Ice";

            case ElementType.Thunder:
                return "Thunder";

            case ElementType.Earth:
                return "Earth";

            case ElementType.Wind:
                return "Wind";

            default:
                return element.ToString();
        }
    }

    public static string GetConsumableEffectName(
        ConsumableEffectType effectType,
        bool useChinese
    )
    {
        if (!useChinese)
        {
            return effectType.ToString();
        }

        switch (effectType)
        {
            case ConsumableEffectType.CharacterExperience:
                return "人物经验";

            case ConsumableEffectType.WeaponExperience:
                return "武器经验";

            case ConsumableEffectType.HealHp:
                return "恢复生命值";

            case ConsumableEffectType.RestoreInk:
                return "恢复墨量";

            case ConsumableEffectType.ElementDamageBuff:
                return "元素伤害加成";

            case ConsumableEffectType.ElementResistanceBuff:
                return "元素抗性加成";

            default:
                return effectType.ToString();
        }
    }
}