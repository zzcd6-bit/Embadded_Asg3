using UnityEngine;

[CreateAssetMenu(
    fileName = "QuestItem_",
    menuName = "Game/Inventory/Quest Item"
)]
public class QuestItemData : ItemData
{
    public override InventoryCategory Category
    {
        get { return InventoryCategory.Quest; }
    }
}