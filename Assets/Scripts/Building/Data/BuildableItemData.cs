using UnityEngine;

public enum PlacementSurfaceRule
{
    FullFootprintGrounded = 0,
    AnchorsOnly = 1
}

[CreateAssetMenu(menuName = "Building/Buildable Item", fileName = "BuildableItem")]
public class BuildableItemData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string itemId;

    [Header("Display")]
    [SerializeField] private string displayName = "New Building";
    [SerializeField] private Sprite icon;
    [SerializeField, TextArea] private string description;

    [Header("Placement")]
    [SerializeField] private GameObject buildingPrefab;
    [SerializeField] private Vector2Int size = Vector2Int.one;
    [SerializeField] private PlacementSurfaceRule surfaceRule = PlacementSurfaceRule.FullFootprintGrounded;
    [SerializeField, Min(1)] private int anchorDepth = 1;
    [SerializeField, Min(0f)] private float maxAnchorHeightDelta;
    [SerializeField] private float placementYOffset;
    [SerializeField] private bool contributesWalkableNavMesh;

    [Header("Economy")]
    [SerializeField] private int cost;

    public string ItemId => string.IsNullOrWhiteSpace(itemId) ? name : itemId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Icon => icon;
    public string Description => description;
    public GameObject BuildingPrefab => buildingPrefab;
    public Vector2Int Size => new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
    public PlacementSurfaceRule SurfaceRule => surfaceRule;
    public int AnchorDepth => Mathf.Max(1, anchorDepth);
    public float MaxAnchorHeightDelta => Mathf.Max(0f, maxAnchorHeightDelta);
    public float PlacementYOffset => placementYOffset;
    public bool ContributesWalkableNavMesh => contributesWalkableNavMesh;
    public int Cost => Mathf.Max(0, cost);

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = name;
        }
    }
#endif
}
