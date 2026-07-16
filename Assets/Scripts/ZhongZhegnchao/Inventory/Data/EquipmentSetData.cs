using UnityEngine;

[CreateAssetMenu(
    fileName = "EquipmentSet_",
    menuName = "Game/Inventory/Equipment Set"
)]
public class EquipmentSetData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string setId;

    [SerializeField]
    private string displayName;

    [TextArea(2, 6)]
    [SerializeField]
    private string description;

    [Header("4 Piece Bonus")]
    [SerializeField]
    private ElementType element =
        ElementType.Physical;

    [SerializeField]
    [Range(0f, 5f)]
    private float elementDamageBonus = 0.15f;

    [SerializeField]
    [Range(0f, 1f)]
    private float elementResistanceBonus = 0.15f;

    public string SetId
    {
        get { return setId; }
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

    public ElementType Element
    {
        get { return element; }
    }

    public float ElementDamageBonus
    {
        get { return elementDamageBonus; }
    }

    public float ElementResistanceBonus
    {
        get { return elementResistanceBonus; }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(setId))
        {
            setId = name;
        }

        elementDamageBonus =
            Mathf.Max(
                0f,
                elementDamageBonus
            );

        elementResistanceBonus =
            Mathf.Max(
                0f,
                elementResistanceBonus
            );
    }
#endif
}