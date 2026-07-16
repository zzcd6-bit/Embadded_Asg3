using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailView : MonoBehaviour
{
    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text typeText;

    [SerializeField]
    private TMP_Text qualityText;

    [SerializeField]
    private TMP_Text quantityText;

    [SerializeField]
    private TMP_Text introductionText;

    [SerializeField]
    private GameObject useButtonRoot;

    [SerializeField]
    private GameObject discardButtonRoot;

    public void ShowItem(
        InventoryItemEntry entry
    )
    {
        gameObject.SetActive(true);

        ItemData data =
            entry != null
                ? entry.ItemData
                : null;

        if (data == null)
        {
            ShowEmpty();
            return;
        }

        SetImage(
            iconImage,
            data.Icon
        );

        SetText(
            nameText,
            data.DisplayName
        );

        SetText(
            typeText,
            GetTypeName(data)
        );

        if (qualityText != null)
        {
            qualityText.gameObject.SetActive(false);
        }

        SetText(
            quantityText,
            $"x{entry.Quantity}"
        );

        SetText(
            introductionText,
            GetDescription(data)
        );

        if (useButtonRoot != null)
        {
            useButtonRoot.SetActive(
                data is ConsumableItemData
            );
        }

        if (discardButtonRoot != null)
        {
            discardButtonRoot.SetActive(
                data.CanDiscard
            );
        }
    }

    public void ShowEmpty()
    {
        SetImage(iconImage, null);

        SetText(nameText, "SELECT AN ITEM");
        SetText(typeText, "-");
        SetText(qualityText, "-");
        SetText(quantityText, string.Empty);
        SetText(introductionText, string.Empty);

        if (useButtonRoot != null)
        {
            useButtonRoot.SetActive(false);
        }

        if (discardButtonRoot != null)
        {
            discardButtonRoot.SetActive(false);
        }

        if (qualityText != null)
        {
            qualityText.gameObject.SetActive(false);
        }
    }

    private string GetTypeName(ItemData data)
    {
        if (data is ConsumableItemData consumable)
        {
            return consumable
                .EffectType
                .ToString();
        }

        if (data is QuestItemData)
        {
            return "Quest Item";
        }

        return data.Category.ToString();
    }

    private string GetDescription(ItemData data)
    {
        if (data is not ConsumableItemData consumable)
        {
            return data.Description;
        }

        string effectDescription =
            GetEffectDescription(
                consumable
            );

        if (string.IsNullOrWhiteSpace(
                data.Description))
        {
            return effectDescription;
        }

        return
            $"{data.Description}\n\n" +
            $"{effectDescription}";
    }

    private string GetEffectDescription(
        ConsumableItemData data
    )
    {
        switch (data.EffectType)
        {
            case ConsumableEffectType
                .CharacterExperience:

                return
                    $"Character EXP +{data.Amount}";

            case ConsumableEffectType
                .WeaponExperience:

                return
                    $"Weapon EXP +{data.Amount}";

            case ConsumableEffectType.HealHp:

                return
                    $"Restore {data.Amount} Health";

            case ConsumableEffectType.RestoreInk:

                return
                    $"Restore {data.Amount} Ink";

            case ConsumableEffectType
                .ElementDamageBuff:

                return
                    $"{data.Element} Damage " +
                    $"+{data.PercentageValue * 100f:0.#}% " +
                    $"for {data.Duration:0.#} seconds";

            case ConsumableEffectType
                .ElementResistanceBuff:

                return
                    $"{data.Element} Resistance " +
                    $"+{data.PercentageValue * 100f:0.#}% " +
                    $"for {data.Duration:0.#} seconds";

            default:
                return string.Empty;
        }
    }

    private void SetText(
        TMP_Text target,
        string value
    )
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private void SetImage(
        Image target,
        Sprite sprite
    )
    {
        if (target == null)
            return;

        target.sprite = sprite;
        target.enabled = sprite != null;
    }
}