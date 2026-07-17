using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StoreProductGridView : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Button button;

    [SerializeField]
    private Image itemImage;

    [SerializeField]
    private TMP_Text quantityText;

    private ShopProductConfig product;

    private Action<ShopProductConfig>
        clickCallback;

    public ShopProductConfig Product
    {
        get { return product; }
    }

    private void Awake()
    {
        if (button == null)
        {
            button =
                GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(
                HandleClick
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
        ShopProductConfig newProduct,
        Action<ShopProductConfig> onClick
    )
    {
        product =
            newProduct;

        clickCallback =
            onClick;

        ItemData itemData =
            product != null
                ? product.ItemData
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
                product != null &&
                product.QuantityPerPurchase > 1;

            quantityText.gameObject.SetActive(
                showQuantity
            );

            quantityText.text =
                showQuantity
                    ? $"¡Á{product.QuantityPerPurchase}"
                    : string.Empty;
        }
    }

    private void HandleClick()
    {
        if (product == null)
            return;

        clickCallback?.Invoke(
            product
        );
    }
}