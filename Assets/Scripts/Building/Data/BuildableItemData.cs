using UnityEngine;

[CreateAssetMenu(menuName = "Building/Buildable Item", fileName = "BuildableItem")]
public class BuildableItemData : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string displayName = "New Building";
    [SerializeField] private Sprite icon;
    [SerializeField, TextArea] private string description;

    [Header("Placement")]
    [SerializeField] private GameObject buildingPrefab;
    [SerializeField] private Vector2Int size = Vector2Int.one;

    [Header("Economy")]
    [SerializeField] private int cost;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Icon => icon;
    public string Description => description;
    public GameObject BuildingPrefab => buildingPrefab;
    public Vector2Int Size => new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
    public int Cost => Mathf.Max(0, cost);
}
