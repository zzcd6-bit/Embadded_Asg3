using System;
using TMPro;
using UnityEngine;

public class CharacterProfilePanel : BasePanel
{
    [Header("Character")]
    [SerializeField]
    private TMP_Text levelValueText;

    [Header("Main Stats")]
    [SerializeField]
    private TMP_Text healthValueText;

    [SerializeField]
    private TMP_Text inkValueText;

    [Header("Offensive Stats")]
    [SerializeField]
    private TMP_Text attackValueText;

    [SerializeField]
    private TMP_Text damageBonusValueText;

    [SerializeField]
    private TMP_Text criticalChanceValueText;

    [SerializeField]
    private TMP_Text criticalDamageValueText;

    [SerializeField]
    private TMP_Text physicalDamageValueText;

    [SerializeField]
    private TMP_Text fireDamageValueText;

    [SerializeField]
    private TMP_Text waterDamageValueText;

    [SerializeField]
    private TMP_Text windDamageValueText;

    [Header("Defensive Stats")]
    [SerializeField]
    private TMP_Text defenseValueText;

    [SerializeField]
    private TMP_Text physicalResistanceValueText;

    [SerializeField]
    private TMP_Text fireResistanceValueText;

    [SerializeField]
    private TMP_Text waterResistanceValueText;

    [SerializeField]
    private TMP_Text windResistanceValueText;

    private PlayerCharacterStatsController statsController;

    private Action closeCallback;

    public override void ShowMe()
    {
        gameObject.SetActive(true);

        Refresh();
    }

    public override void HideMe()
    {
        Unbind();

        gameObject.SetActive(false);
    }

    public void Bind(
        PlayerCharacterStatsController controller,
        Action onClose
    )
    {
        Unbind();

        statsController = controller;
        closeCallback = onClose;

        if (statsController != null)
        {
            statsController.StatsChanged += Refresh;
        }

        Refresh();
    }

    private void Unbind()
    {
        if (statsController != null)
        {
            statsController.StatsChanged -= Refresh;
        }

        statsController = null;
        closeCallback = null;
    }

    private void Refresh()
    {
        if (statsController == null)
            return;

        CharacterCombatStats combatStats =
            statsController.CombatStats;

        PlayerDamageReceiver damageReceiver =
            statsController.DamageReceiver;

        PlayerInkPouchController inkPouch =
            statsController.InkPouchController;

        if (combatStats == null)
            return;

        RefreshCharacterInfo();

        RefreshMainStats(
            combatStats,
            damageReceiver,
            inkPouch
        );

        RefreshOffensiveStats(combatStats);

        RefreshDefensiveStats(combatStats);
    }

    private void RefreshCharacterInfo()
    {
        SetText(
            levelValueText,
            statsController.CurrentLevel.ToString()
        );
    }

    private void RefreshMainStats(
        CharacterCombatStats combatStats,
        PlayerDamageReceiver damageReceiver,
        PlayerInkPouchController inkPouch
    )
    {
        if (damageReceiver != null)
        {
            SetText(
                healthValueText,
                $"{damageReceiver.CurrentHp}/" +
                $"{combatStats.MaxHp}"
            );
        }
        else
        {
            SetText(
                healthValueText,
                combatStats.MaxHp.ToString()
            );
        }

        if (inkPouch != null)
        {
            SetText(
                inkValueText,
                $"{inkPouch.CurrentInk}/" +
                $"{inkPouch.MaxInk}"
            );
        }
        else
        {
            SetText(
                inkValueText,
                "-"
            );
        }
    }

    private void RefreshOffensiveStats(
        CharacterCombatStats combatStats
    )
    {
        SetText(
            attackValueText,
            combatStats.AttackPower.ToString()
        );

        SetText(
            damageBonusValueText,
            FormatPercent(
                combatStats.DamageBonus
            )
        );

        SetText(
            criticalChanceValueText,
            FormatPercent(
                combatStats.CritRate
            )
        );

        SetText(
            criticalDamageValueText,
            FormatPercent(
                combatStats.CritDamage
            )
        );

        SetText(
            physicalDamageValueText,
            FormatPercent(
                combatStats.GetElementDamageBonus(
                    ElementType.Physical
                )
            )
        );

        SetText(
            fireDamageValueText,
            FormatPercent(
                combatStats.GetElementDamageBonus(
                    ElementType.Fire
                )
            )
        );

        SetText(
            waterDamageValueText,
            FormatPercent(
                combatStats.GetElementDamageBonus(
                    ElementType.Water
                )
            )
        );

        SetText(
            windDamageValueText,
            FormatPercent(
                combatStats.GetElementDamageBonus(
                    ElementType.Wind
                )
            )
        );
    }

    private void RefreshDefensiveStats(
        CharacterCombatStats combatStats
    )
    {
        SetText(
            defenseValueText,
            combatStats.Defense.ToString()
        );

        SetText(
            physicalResistanceValueText,
            FormatPercent(
                combatStats.GetElementResistance(
                    ElementType.Physical
                )
            )
        );

        SetText(
            fireResistanceValueText,
            FormatPercent(
                combatStats.GetElementResistance(
                    ElementType.Fire
                )
            )
        );

        SetText(
            waterResistanceValueText,
            FormatPercent(
                combatStats.GetElementResistance(
                    ElementType.Water
                )
            )
        );

        SetText(
            windResistanceValueText,
            FormatPercent(
                combatStats.GetElementResistance(
                    ElementType.Wind
                )
            )
        );
    }

    private void SetText(
        TMP_Text targetText,
        string value
    )
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }

    private string FormatPercent(
        float value
    )
    {
        return $"{value * 100f:0.#}%";
    }

    protected override void ClickBtn(
        string btnName
    )
    {
        if (btnName == "CloseButton")
        {
            closeCallback?.Invoke();
        }
    }

    private void OnDestroy()
    {
        Unbind();
    }
}