using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BuildItemButton : MonoBehaviour
{
    [SerializeField] private Text nameLabel;
    [SerializeField] private Text metaLabel;
    [SerializeField] private Image icon;
    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = new(0.12f, 0.14f, 0.16f, 0.96f);
    [SerializeField] private Color highlightedColor = new(0.24f, 0.42f, 0.34f, 0.98f);

    private Button button;
    private BuildableItemData item;
    private BuildInventoryUI inventory;

    public BuildableItemData Item => item;

    //Caches the button and binds item selection.
    //缓存按钮组件，并绑定物品选择事件。
    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    //Assigns item data and updates the square slot display.
    //分配物品数据，并更新正方形格子的显示。
    public void Initialize(BuildableItemData itemData, BuildInventoryUI owner)
    {
        item = itemData;
        inventory = owner;

        if (nameLabel != null)
        {
            nameLabel.text = itemData != null ? itemData.DisplayName : "Empty";
        }

        if (metaLabel != null)
        {
            Vector2Int size = itemData != null ? itemData.Size : Vector2Int.one;
            string costText = itemData != null && itemData.Cost > 0 ? $"  |  {itemData.Cost}" : string.Empty;
            metaLabel.text = $"{size.x}x{size.y}{costText}";
            metaLabel.gameObject.SetActive(!string.IsNullOrEmpty(metaLabel.text));
        }

        if (icon != null)
        {
            icon.sprite = itemData != null ? itemData.Icon : null;
            icon.enabled = icon.sprite != null;
        }
    }

    //Applies visual state for selected or normal item.
    //应用物品选中或普通状态的视觉表现。
    public void SetSelected(bool selected)
    {
        if (background != null)
        {
            background.color = selected ? highlightedColor : normalColor;
        }
    }

    //Requests the inventory UI to select this item.
    //请求物品栏 UI 选中当前物品。
    private void HandleClick()
    {
        if (item != null && inventory != null)
        {
            inventory.SelectItem(item);
        }
    }
}
