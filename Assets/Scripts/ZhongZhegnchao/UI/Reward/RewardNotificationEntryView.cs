using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RewardNotificationEntryView :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text quantityText;

    [Header("Display")]
    [SerializeField]
    private string quantityPrefix = "¡Á ";

    public void Bind(
        Sprite icon,
        string displayName,
        int quantity
    )
    {
        SetIcon(icon);

        if (nameText != null)
        {
            nameText.text =
                string.IsNullOrWhiteSpace(
                    displayName
                )
                    ? "Unknown"
                    : displayName;
        }

        SetQuantity(quantity);
    }

    public void SetQuantity(int quantity)
    {
        quantity =
            Mathf.Max(
                1,
                quantity
            );

        if (quantityText != null)
        {
            quantityText.text =
                $"{quantityPrefix}{quantity}";
        }
    }

    private void SetIcon(Sprite icon)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = icon;

        iconImage.enabled =
            icon != null;
    }
}