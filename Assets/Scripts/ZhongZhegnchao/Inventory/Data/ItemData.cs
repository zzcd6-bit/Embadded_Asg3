using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string itemId;

    [SerializeField]
    private string displayName;

    [TextArea(3, 8)]
    [SerializeField]
    private string description;

    [SerializeField]
    private Sprite icon;

    [SerializeField]
    private ItemQuality quality =
        ItemQuality.Common;

    [Header("Inventory")]
    [SerializeField]
    private bool canDiscard = true;

    [SerializeField]
    [Min(1)]
    private int maxStack = 99;

    public string ItemId
    {
        get { return itemId; }
    }

    public string DisplayName
    {
        get
        {
            return string.IsNullOrWhiteSpace(displayName)
                ? name
                : displayName;
        }
    }

    public string Description
    {
        get { return description; }
    }

    public Sprite Icon
    {
        get { return icon; }
    }

    public ItemQuality Quality
    {
        get { return quality; }
    }

    public bool CanDiscard
    {
        get { return canDiscard; }
    }

    public virtual bool IsStackable
    {
        get { return true; }
    }

    public int MaxStack
    {
        get
        {
            return Mathf.Max(
                1,
                maxStack
            );
        }
    }

    public abstract InventoryCategory Category
    {
        get;
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = name;
        }

        maxStack =
            Mathf.Max(1, maxStack);
    }
#endif
}