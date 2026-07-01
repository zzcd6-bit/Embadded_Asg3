using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BuildCategoryButton : MonoBehaviour
{
    [SerializeField] private Text label;
    [SerializeField] private Image icon;
    [SerializeField] private Image background;
    [SerializeField] private Color selectedColor = new(0.20f, 0.39f, 0.34f, 0.96f);
    [SerializeField] private Color normalColor = new(0.10f, 0.12f, 0.14f, 0.82f);
    [SerializeField] private Color selectedTextColor = new(0.95f, 1f, 0.92f, 1f);
    [SerializeField] private Color normalTextColor = new(0.68f, 0.73f, 0.73f, 1f);

    private Button button;
    private BuildCategoryData category;
    private BuildInventoryUI inventory;

    public BuildCategoryData Category => category;

    //Caches the button and binds category selection.
    //缓存按钮组件，并绑定分类选择事件。
    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    //Assigns category data and updates display text and icon.
    //分配分类数据，并更新显示文字和图标。
    public void Initialize(BuildCategoryData categoryData, BuildInventoryUI owner)
    {
        category = categoryData;
        inventory = owner;

        if (label != null)
        {
            label.text = categoryData != null ? categoryData.DisplayName : "Empty";
        }

        if (icon != null)
        {
            icon.sprite = categoryData != null ? categoryData.Icon : null;
            icon.enabled = icon.sprite != null;
        }
    }

    //Applies visual state for selected or normal category.
    //应用分类选中或普通状态的视觉表现。
    public void SetSelected(bool selected)
    {
        if (background != null)
        {
            background.color = selected ? selectedColor : normalColor;
        }

        if (label != null)
        {
            label.color = selected ? selectedTextColor : normalTextColor;
            label.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
        }
    }

    //Requests the inventory UI to show this category.
    //请求物品栏 UI 显示当前分类。
    private void HandleClick()
    {
        if (category != null && inventory != null)
        {
            inventory.ShowCategory(category);
        }
    }
}
