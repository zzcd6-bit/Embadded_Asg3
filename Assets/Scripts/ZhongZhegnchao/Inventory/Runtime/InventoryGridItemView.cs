using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InventoryGridItemView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private Button button;

    [SerializeField]
    private Image itemImage;

    [SerializeField]
    private TMP_Text quantityText;

    [Header("Debug")]
    [SerializeField]
    private bool debugLog = false;

    private InventoryItemEntry entry;

    private Action<InventoryItemEntry>
        clickCallback;

    public InventoryItemEntry Entry
    {
        get { return entry; }
    }

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(
                HandleClick
            );
        }
        else
        {
            Debug.LogError(
                "[InventoryGridItemView] " +
                "Button component is missing.",
                this
            );
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                HandleClick
            );
        }
    }

    public void Bind(
        InventoryItemEntry newEntry,
        Action<InventoryItemEntry> onClick
    )
    {
        entry = newEntry;
        clickCallback = onClick;

        ItemData itemData =
            entry != null
                ? entry.ItemData
                : null;

        if (itemImage != null)
        {
            itemImage.sprite =
                itemData != null
                    ? itemData.Icon
                    : null;

            itemImage.enabled =
                itemImage.sprite != null;
        }

        if (quantityText != null)
        {
            bool showQuantity =
                itemData != null &&
                itemData.IsStackable;

            quantityText.gameObject.SetActive(
                showQuantity
            );

            quantityText.text =
                showQuantity
                    ? $"x{entry.Quantity}"
                    : string.Empty;
        }

        if (debugLog)
        {
            Debug.Log(
                $"[InventoryGridItemView] Bind: " +
                $"{itemData?.DisplayName ?? "NULL"}",
                this
            );
        }
    }

    private void HandleClick()
    {
        if (entry == null)
        {
            Debug.LogWarning(
                "[InventoryGridItemView] " +
                "This grid has not been bound to an item. " +
                "Do not manually place visible grid items.",
                this
            );

            return;
        }

        if (clickCallback == null)
        {
            Debug.LogWarning(
                "[InventoryGridItemView] " +
                "Click callback is missing.",
                this
            );

            return;
        }

        if (debugLog)
        {
            Debug.Log(
                $"[InventoryGridItemView] Click: " +
                $"{entry.ItemData.DisplayName}",
                this
            );
        }

        clickCallback.Invoke(entry);
    }
}