using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentDetailView : MonoBehaviour
{
    [Header("Main")]
    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text typeText;

    [SerializeField]
    private TMP_Text qualityText;

    [Header("Main Stat")]
    [SerializeField]
    private TMP_Text mainStatNameText;

    [SerializeField]
    private TMP_Text mainStatValueText;

    [Header("Secondary Stat 1")]
    [SerializeField]
    private GameObject secondaryStat1Root;

    [SerializeField]
    private TMP_Text secondaryStat1NameText;

    [SerializeField]
    private TMP_Text secondaryStat1ValueText;

    [Header("Secondary Stat 2")]
    [SerializeField]
    private GameObject secondaryStat2Root;

    [SerializeField]
    private TMP_Text secondaryStat2NameText;

    [SerializeField]
    private TMP_Text secondaryStat2ValueText;

    [Header("Set Bonus")]
    [SerializeField]
    private GameObject setBonusArea;

    [SerializeField]
    private TMP_Text setTitleText;

    [SerializeField]
    private GameObject twoPiecesArea;

    [SerializeField]
    private TMP_Text fourPiecesTitleText;

    [SerializeField]
    private TMP_Text fourPiecesDescriptionText;

    [Header("Introduction")]
    [SerializeField]
    private TMP_Text introductionText;

    [Header("Optional Weapon Progress")]
    [SerializeField]
    private GameObject weaponProgressRoot;

    [SerializeField]
    private TMP_Text weaponLevelText;

    [SerializeField]
    private Image weaponExperienceFill;

    [SerializeField]
    private TMP_Text weaponExperienceText;

    [Header("Display Settings")]
    [SerializeField]
    private bool useChineseValueText = true;

    [SerializeField]
    [Tooltip("勾选后会在 Type 中显示：武器 · 传说")]
    private bool showRarityInsideType = true;

    public void ShowEquipment(
        InventoryItemEntry entry,
        PlayerEquipmentController
            equipmentController
    )
    {
        gameObject.SetActive(true);

        if (entry?.ItemData is not EquipmentData data)
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

        RefreshTypeAndRarity(data);

        RefreshStats(
            data,
            entry
        );

        RefreshSetBonus(
            data,
            equipmentController
        );

        RefreshWeaponProgress(
            data,
            entry
        );

        SetText(
            introductionText,
            data.Description
        );
    }

    private void RefreshTypeAndRarity(
    EquipmentData data
)
    {
        if (data == null)
            return;

        string typeName =
            InventoryDisplayNameUtility
                .GetEquipmentSlotName(
                    data.SlotType,
                    useChineseValueText
                );

        string rarityName =
            InventoryDisplayNameUtility
                .GetQualityName(
                    data.Quality,
                    useChineseValueText
                );

        if (showRarityInsideType)
        {
            SetText(
                typeText,
                $"{typeName} · {rarityName}"
            );

            if (qualityText != null)
            {
                qualityText.gameObject.SetActive(
                    false
                );
            }
        }
        else
        {
            SetText(
                typeText,
                typeName
            );

            if (qualityText != null)
            {
                qualityText.gameObject.SetActive(
                    true
                );

                qualityText.text =
                    rarityName;
            }
        }
    }

    public void ShowEmpty()
    {
        SetImage(iconImage, null);

        SetText(nameText, "EMPTY");
        SetText(typeText, "-");

        if (qualityText != null)
        {
            qualityText.gameObject.SetActive(
                !showRarityInsideType
            );

            qualityText.text = "-";
        }

        SetText(mainStatNameText, "-");
        SetText(mainStatValueText, "-");

        SetSecondaryStat(
            secondaryStat1Root,
            secondaryStat1NameText,
            secondaryStat1ValueText,
            false,
            string.Empty,
            string.Empty
        );

        SetSecondaryStat(
            secondaryStat2Root,
            secondaryStat2NameText,
            secondaryStat2ValueText,
            false,
            string.Empty,
            string.Empty
        );

        if (setBonusArea != null)
        {
            setBonusArea.SetActive(false);
        }

        if (weaponProgressRoot != null)
        {
            weaponProgressRoot.SetActive(false);
        }

        SetText(
            introductionText,
            "Select an equipment slot."
        );
    }

    private void RefreshStats(
        EquipmentData data,
        InventoryItemEntry entry
    )
    {
        switch (data.SlotType)
        {
            case EquipmentSlotType.Helmet:

                SetText(
                    mainStatNameText,
                    "Health"
                );

                SetText(
                    mainStatValueText,
                    $"+{data.MaxHpBonus}"
                );

                SetSecondaryStat(
                    secondaryStat1Root,
                    secondaryStat1NameText,
                    secondaryStat1ValueText,
                    true,
                    "Critical Chance",
                    FormatPercent(
                        data.CriticalRateBonus
                    )
                );

                SetSecondaryStat(
                    secondaryStat2Root,
                    secondaryStat2NameText,
                    secondaryStat2ValueText,
                    true,
                    "Critical Damage",
                    FormatPercent(
                        data.CriticalDamageBonus
                    )
                );

                break;

            case EquipmentSlotType.Chest:

                SetText(
                    mainStatNameText,
                    "Defense"
                );

                SetText(
                    mainStatValueText,
                    $"+{data.DefenseBonus}"
                );

                SetSecondaryStat(
                    secondaryStat1Root,
                    secondaryStat1NameText,
                    secondaryStat1ValueText,
                    true,
                    $"{GetElementName(data.ResistanceElement)} Resistance",
                    FormatPercent(
                        data.ElementResistanceBonus
                    )
                );

                HideSecondaryStat2();

                break;

            case EquipmentSlotType.Shoes:

                SetText(
                    mainStatNameText,
                    "Damage Bonus"
                );

                SetText(
                    mainStatValueText,
                    FormatPercent(
                        data.DamageBonus
                    )
                );

                SetSecondaryStat(
                    secondaryStat1Root,
                    secondaryStat1NameText,
                    secondaryStat1ValueText,
                    true,
                    $"{GetElementName(data.DamageElement)} Damage",
                    FormatPercent(
                        data.ElementDamageBonus
                    )
                );

                HideSecondaryStat2();

                break;

            case EquipmentSlotType.Weapon:

                SetText(
                    mainStatNameText,
                    "Attack"
                );

                SetText(
                    mainStatValueText,
                    $"+{data.GetAttackPowerAtLevel(entry.WeaponLevel)}"
                );

                SetSecondaryStat(
                    secondaryStat1Root,
                    secondaryStat1NameText,
                    secondaryStat1ValueText,
                    true,
                    "Critical Chance",
                    FormatPercent(
                        data.CriticalRateBonus
                    )
                );

                SetSecondaryStat(
                    secondaryStat2Root,
                    secondaryStat2NameText,
                    secondaryStat2ValueText,
                    true,
                    "Critical Damage",
                    FormatPercent(
                        data.CriticalDamageBonus
                    )
                );

                break;
        }
    }

    private void HideSecondaryStat2()
    {
        SetSecondaryStat(
            secondaryStat2Root,
            secondaryStat2NameText,
            secondaryStat2ValueText,
            false,
            string.Empty,
            string.Empty
        );
    }

    private void RefreshSetBonus(
        EquipmentData data,
        PlayerEquipmentController
            equipmentController
    )
    {
        EquipmentSetData setData =
            data.EquipmentSet;

        if (setData == null)
        {
            if (setBonusArea != null)
            {
                setBonusArea.SetActive(false);
            }

            return;
        }

        if (setBonusArea != null)
        {
            setBonusArea.SetActive(true);
        }

        int setCount =
            equipmentController != null
                ? equipmentController.GetSetCount(
                    setData
                )
                : 0;

        SetText(
            setTitleText,
            $"{setData.DisplayName} ({setCount}/4)"
        );

        if (twoPiecesArea != null)
        {
            twoPiecesArea.SetActive(false);
        }

        SetText(
            fourPiecesTitleText,
            "4 Pieces"
        );

        string effectText =
            $"+{FormatPercent(setData.ElementDamageBonus)} " +
            $"{GetElementName(setData.Element)} Damage\n" +
            $"+{FormatPercent(setData.ElementResistanceBonus)} " +
            $"{GetElementName(setData.Element)} Resistance";

        SetText(
            fourPiecesDescriptionText,
            effectText
        );
    }

    private void RefreshWeaponProgress(
        EquipmentData data,
        InventoryItemEntry entry
    )
    {
        bool isWeapon =
            data.SlotType ==
            EquipmentSlotType.Weapon;

        if (weaponProgressRoot != null)
        {
            weaponProgressRoot.SetActive(
                isWeapon
            );
        }

        if (!isWeapon)
            return;

        SetText(
            weaponLevelText,
            $"Lv. {entry.WeaponLevel}"
        );

        int required =
            data.GetWeaponExperienceRequirement(
                entry.WeaponLevel
            );

        bool isMax =
            entry.WeaponLevel >=
            data.WeaponMaxLevel;

        if (weaponExperienceFill != null)
        {
            weaponExperienceFill.fillAmount =
                isMax || required <= 0
                    ? 1f
                    : Mathf.Clamp01(
                        entry.WeaponExperience /
                        (float)required
                    );
        }

        SetText(
            weaponExperienceText,
            isMax
                ? "MAX"
                : $"{entry.WeaponExperience}/{required}"
        );
    }

    private void SetSecondaryStat(
        GameObject root,
        TMP_Text nameTarget,
        TMP_Text valueTarget,
        bool visible,
        string statName,
        string statValue
    )
    {
        if (root != null)
        {
            root.SetActive(visible);
        }

        if (!visible)
            return;

        SetText(nameTarget, statName);
        SetText(valueTarget, statValue);
    }

    private string FormatPercent(float value)
    {
        return $"{value * 100f:0.#}%";
    }

    private string GetElementName(
    ElementType element
)
    {
        return InventoryDisplayNameUtility
            .GetElementName(
                element,
                useChineseValueText
            );
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