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

    [Header("Weapon Progress")]
    [Tooltip("拖入整个武器等级区域根节点。头盔、胸甲和鞋子会隐藏此对象。")]
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
        PlayerEquipmentController equipmentController
    )
    {
        gameObject.SetActive(true);

        if (entry == null ||
            !(entry.ItemData is EquipmentData data))
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

    public void ShowEmpty()
    {
        SetImage(
            iconImage,
            null
        );

        SetText(
            nameText,
            "EMPTY"
        );

        SetText(
            typeText,
            "-"
        );

        if (qualityText != null)
        {
            qualityText.gameObject.SetActive(
                !showRarityInsideType
            );

            qualityText.text = "-";
        }

        SetText(
            mainStatNameText,
            "-"
        );

        SetText(
            mainStatValueText,
            "-"
        );

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

        ClearWeaponProgress();

        SetText(
            introductionText,
            "Select an equipment slot."
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

    private void RefreshStats(
        EquipmentData data,
        InventoryItemEntry entry
    )
    {
        switch (data.SlotType)
        {
            case EquipmentSlotType.Helmet:
                ShowHelmetStats(data);
                break;

            case EquipmentSlotType.Chest:
                ShowChestStats(data);
                break;

            case EquipmentSlotType.Shoes:
                ShowShoesStats(data);
                break;

            case EquipmentSlotType.Weapon:
                ShowWeaponStats(
                    data,
                    entry
                );
                break;
        }
    }

    private void ShowHelmetStats(
        EquipmentData data
    )
    {
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
    }

    private void ShowChestStats(
        EquipmentData data
    )
    {
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
    }

    private void ShowShoesStats(
        EquipmentData data
    )
    {
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
    }

    private void ShowWeaponStats(
        EquipmentData data,
        InventoryItemEntry entry
    )
    {
        int currentLevel =
            entry != null
                ? Mathf.Max(
                    1,
                    entry.WeaponLevel
                )
                : 1;

        SetText(
            mainStatNameText,
            "Attack"
        );

        SetText(
            mainStatValueText,
            $"+{data.GetAttackPowerAtLevel(currentLevel)}"
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
        PlayerEquipmentController equipmentController
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
            data != null &&
            data.SlotType ==
            EquipmentSlotType.Weapon;

        SetWeaponProgressVisible(
            isWeapon
        );

        if (!isWeapon ||
            entry == null)
        {
            ClearWeaponProgressContent();
            return;
        }

        int currentLevel =
            Mathf.Max(
                1,
                entry.WeaponLevel
            );

        int maximumLevel =
            Mathf.Max(
                1,
                data.WeaponMaxLevel
            );

        bool isMaximumLevel =
            currentLevel >= maximumLevel;

        SetText(
            weaponLevelText,
            $"Lv. {currentLevel}/{maximumLevel}"
        );

        if (isMaximumLevel)
        {
            if (weaponExperienceFill != null)
            {
                weaponExperienceFill.fillAmount =
                    1f;
            }

            SetText(
                weaponExperienceText,
                "MAX"
            );

            return;
        }

        int requiredExperience =
            data.GetWeaponExperienceRequirement(
                currentLevel
            );

        int currentExperience =
            Mathf.Max(
                0,
                entry.WeaponExperience
            );

        if (weaponExperienceFill != null)
        {
            weaponExperienceFill.fillAmount =
                requiredExperience <= 0
                    ? 0f
                    : Mathf.Clamp01(
                        currentExperience /
                        (float)requiredExperience
                    );
        }

        SetText(
            weaponExperienceText,
            requiredExperience <= 0
                ? $"{currentExperience}/-"
                : $"{currentExperience}/{requiredExperience}"
        );
    }

    private void SetWeaponProgressVisible(
        bool visible
    )
    {
        if (weaponProgressRoot != null)
        {
            weaponProgressRoot.SetActive(
                visible
            );
        }

        /*
         * 即使没有配置 Weapon Progress Root，
         * 也单独控制里面的 UI，避免头盔、胸甲和鞋子
         * 残留武器等级文字。
         */

        if (weaponLevelText != null)
        {
            weaponLevelText.gameObject.SetActive(
                visible
            );
        }

        if (weaponExperienceFill != null)
        {
            weaponExperienceFill.gameObject.SetActive(
                visible
            );
        }

        if (weaponExperienceText != null)
        {
            weaponExperienceText.gameObject.SetActive(
                visible
            );
        }
    }

    private void ClearWeaponProgress()
    {
        SetWeaponProgressVisible(false);
        ClearWeaponProgressContent();
    }

    private void ClearWeaponProgressContent()
    {
        SetText(
            weaponLevelText,
            string.Empty
        );

        SetText(
            weaponExperienceText,
            string.Empty
        );

        if (weaponExperienceFill != null)
        {
            weaponExperienceFill.fillAmount =
                0f;
        }
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
            root.SetActive(
                visible
            );
        }

        if (!visible)
        {
            SetText(
                nameTarget,
                string.Empty
            );

            SetText(
                valueTarget,
                string.Empty
            );

            return;
        }

        SetText(
            nameTarget,
            statName
        );

        SetText(
            valueTarget,
            statValue
        );
    }

    private string FormatPercent(
        float value
    )
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
            target.text =
                value;
        }
    }

    private void SetImage(
        Image target,
        Sprite sprite
    )
    {
        if (target == null)
            return;

        target.sprite =
            sprite;

        target.enabled =
            sprite != null;
    }
}