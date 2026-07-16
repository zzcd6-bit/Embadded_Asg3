using UnityEngine;

[CreateAssetMenu(
    fileName = "Consumable_",
    menuName = "Game/Inventory/Consumable"
)]
public class ConsumableItemData : ItemData
{
    [Header("Effect")]
    [SerializeField]
    private ConsumableEffectType effectType;

    [SerializeField]
    [Min(0)]
    private int amount;

    [SerializeField]
    private ElementType element =
        ElementType.Physical;

    [SerializeField]
    [Range(0f, 5f)]
    private float percentageValue;

    [SerializeField]
    [Min(0f)]
    private float duration = 30f;

    public override InventoryCategory Category
    {
        get { return InventoryCategory.Consumable; }
    }

    public ConsumableEffectType EffectType
    {
        get { return effectType; }
    }

    public int Amount
    {
        get { return Mathf.Max(0, amount); }
    }

    public ElementType Element
    {
        get { return element; }
    }

    public float PercentageValue
    {
        get { return Mathf.Max(0f, percentageValue); }
    }

    public float Duration
    {
        get { return Mathf.Max(0f, duration); }
    }
}