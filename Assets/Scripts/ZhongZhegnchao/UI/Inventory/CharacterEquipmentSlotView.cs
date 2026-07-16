using System;
using UnityEngine;
using UnityEngine.UI;

public class CharacterEquipmentSlotView :
    MonoBehaviour
{
    [SerializeField]
    private EquipmentSlotType slotType;

    [SerializeField]
    private Button button;

    [SerializeField]
    private Image itemImage;

    [SerializeField]
    private Sprite emptySlotSprite;

    [SerializeField]
    private GameObject selectedFrame;

    private InventoryItemEntry currentItem;

    private Action<
        EquipmentSlotType,
        InventoryItemEntry
    > clickCallback;

    public EquipmentSlotType SlotType
    {
        get { return slotType; }
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
        InventoryItemEntry item,
        Action<
            EquipmentSlotType,
            InventoryItemEntry
        > onClick
    )
    {
        currentItem = item;
        clickCallback = onClick;

        if (itemImage != null)
        {
            Sprite displaySprite =
                currentItem?.ItemData?.Icon;

            if (displaySprite == null)
            {
                displaySprite =
                    emptySlotSprite;
            }

            itemImage.sprite =
                displaySprite;

            itemImage.enabled =
                displaySprite != null;
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrame != null)
        {
            selectedFrame.SetActive(
                selected
            );
        }
    }

    private void HandleClick()
    {
        clickCallback?.Invoke(
            slotType,
            currentItem
        );
    }
}