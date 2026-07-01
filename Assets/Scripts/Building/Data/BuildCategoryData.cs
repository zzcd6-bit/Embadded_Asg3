using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Building/Build Category", fileName = "BuildCategory")]
public class BuildCategoryData : ScriptableObject
{
    [SerializeField] private string displayName = "Category";
    [SerializeField] private Sprite icon;
    [SerializeField] private List<BuildableItemData> items = new();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Icon => icon;
    public IReadOnlyList<BuildableItemData> Items => items;
}
