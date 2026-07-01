using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildInventoryUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private List<BuildCategoryData> categories = new();

    [Header("Prefabs")]
    [SerializeField] private BuildCategoryButton categoryButtonPrefab;
    [SerializeField] private BuildItemButton itemButtonPrefab;

    [Header("Containers")]
    [SerializeField] private Transform categoryRoot;
    [SerializeField] private Transform itemRoot;
    [SerializeField] private Text titleLabel;
    [SerializeField] private Text hintLabel;

    [Header("Events")]
    [SerializeField] private BuildItemSelectedEvent onItemSelected = new();

    private readonly List<BuildCategoryButton> categoryButtons = new();
    private readonly List<BuildItemButton> itemButtons = new();
    private BuildCategoryData currentCategory;
    private BuildableItemData currentItem;

    public BuildItemSelectedEvent OnItemSelected => onItemSelected;
    public BuildableItemData CurrentItem => currentItem;

    //Builds the inventory UI when the scene starts.
    //场景启动时构建物品栏 UI。
    private void Start()
    {
        Rebuild();
    }

    //Recreates category buttons and opens the first available category.
    //重新创建分类按钮，并打开第一个可用分类。
    public void Rebuild()
    {
        ClearButtons();

        foreach (BuildCategoryData category in categories)
        {
            if (category == null)
            {
                continue;
            }

            BuildCategoryButton button = Instantiate(categoryButtonPrefab, categoryRoot);
            button.gameObject.SetActive(true);
            button.Initialize(category, this);
            categoryButtons.Add(button);
        }

        BuildCategoryData firstCategory = categories.Find(category => category != null);
        if (firstCategory != null)
        {
            ShowCategory(firstCategory);
        }
        else
        {
            SetHeader("Build", "No categories assigned.");
        }
    }

    //Shows items belonging to the selected category.
    //显示所选分类下的所有物品。
    public void ShowCategory(BuildCategoryData category)
    {
        if (category == null)
        {
            return;
        }

        currentCategory = category;
        currentItem = null;
        ClearItemButtons();

        foreach (BuildCategoryButton button in categoryButtons)
        {
            button.SetSelected(button.Category == category);
        }

        foreach (BuildableItemData item in category.Items)
        {
            if (item == null)
            {
                continue;
            }

            BuildItemButton button = Instantiate(itemButtonPrefab, itemRoot);
            button.gameObject.SetActive(true);
            button.Initialize(item, this);
            itemButtons.Add(button);
        }

        SetHeader(category.DisplayName, "Select an item square.");
    }

    //Selects an item and notifies placement listeners.
    //选中物品，并通知摆放监听者。
    public void SelectItem(BuildableItemData item)
    {
        currentItem = item;

        foreach (BuildItemButton button in itemButtons)
        {
            button.SetSelected(button.Item == item);
        }

        SetHeader(currentCategory != null ? currentCategory.DisplayName : "Build", item.DisplayName);
        onItemSelected.Invoke(item);
    }

    //Updates optional title and hint labels.
    //更新可选的标题和提示文本。
    private void SetHeader(string title, string hint)
    {
        if (titleLabel != null)
        {
            titleLabel.text = title;
        }

        if (hintLabel != null)
        {
            hintLabel.text = hint;
        }
    }

    //Destroys category and item buttons created at runtime.
    //销毁运行时创建的分类按钮和物品按钮。
    private void ClearButtons()
    {
        foreach (BuildCategoryButton button in categoryButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }

        categoryButtons.Clear();
        ClearItemButtons();
    }

    //Destroys item buttons created for the current category.
    //销毁当前分类下运行时创建的物品按钮。
    private void ClearItemButtons()
    {
        foreach (BuildItemButton button in itemButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }

        itemButtons.Clear();
    }
}
