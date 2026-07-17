using UnityEngine;

public class WoodBrushSkill : BrushSkillBase
{
    [Header("Config")]
    public BrushSkillConfig config;

    [Header("Wood Controller")]
    public PlayerWoodBlessingController woodBlessingController;

    protected override BrushSkillType SkillType => BrushSkillType.Wood;

    protected override int GetInkCost()
    {
        if (config == null)
            return 0;

        return config.inkCost;
    }

    protected override float GetCooldown()
    {
        if (config == null)
            return 0f;

        if (!config.useSkillCooldown)
            return 0f;

        return config.skillCooldown;
    }

    protected override void Execute(
        BrushGestureResult result,
        BrushCastContext context
    )
    {
        if (config == null)
        {
            Debug.LogWarning("[WoodBrushSkill] Config is missing.", this);
            return;
        }

        if (woodBlessingController == null)
        {
            ResolveWoodBlessingController();
        }

        if (woodBlessingController == null)
        {
            Debug.LogWarning("[WoodBrushSkill] PlayerWoodBlessingController not found.", this);
            return;
        }

        int maxHp =
            ResolvePlayerMaxHp();

        int totalHealAmount =
            ResolveTotalHealAmount(
                maxHp
            );

        int shieldAmount =
            ResolveShieldAmount(
                maxHp
            );

        woodBlessingController.ApplyWoodBlessing(
            config,
            totalHealAmount,
            shieldAmount
        );

        Debug.Log("[WoodBrushSkill] Wood blessing activated.", this);
    }

    private int ResolvePlayerMaxHp()
    {
        GameObject searchObject =
            caster != null
                ? caster
                : gameObject;

        if (searchObject == null)
            return 1;

        PlayerDamageReceiver damageReceiver =
            searchObject.GetComponent<
                PlayerDamageReceiver>();

        if (damageReceiver == null)
        {
            damageReceiver =
                searchObject.GetComponentInParent<
                    PlayerDamageReceiver>();
        }

        if (damageReceiver == null)
        {
            damageReceiver =
                searchObject.GetComponentInChildren<
                    PlayerDamageReceiver>(true);
        }

        if (damageReceiver != null)
        {
            return Mathf.Max(
                1,
                damageReceiver.MaxHp
            );
        }

        CharacterCombatStats combatStats =
            searchObject.GetComponent<
                CharacterCombatStats>();

        if (combatStats == null)
        {
            combatStats =
                searchObject.GetComponentInParent<
                    CharacterCombatStats>();
        }

        if (combatStats == null)
        {
            combatStats =
                searchObject.GetComponentInChildren<
                    CharacterCombatStats>(true);
        }

        return combatStats != null
            ? Mathf.Max(
                1,
                combatStats.MaxHp
            )
            : 1;
    }

    private int ResolveTotalHealAmount(
        int maxHp
    )
    {
        float healPercent =
            GetSkillTreeStatValue(
                SkillTreeStatType
                    .HealPercent,
                -1f
            );

        if (healPercent >= 0f)
        {
            return Mathf.Max(
                0,
                Mathf.RoundToInt(
                    maxHp *
                    healPercent
                )
            );
        }

        float duration =
            Mathf.Max(
                0f,
                config.woodEffectDuration
            );

        float tickInterval =
            Mathf.Max(
                0.05f,
                config.woodHealTickInterval
            );

        int tickCount =
            Mathf.Max(
                1,
                Mathf.FloorToInt(
                    duration /
                    tickInterval
                )
            );

        return Mathf.Max(
            0,
            config.woodHealAmountPerTick
        ) * tickCount;
    }

    private int ResolveShieldAmount(
        int maxHp
    )
    {
        if (!config.woodGrantShield)
            return 0;

        float shieldPercent =
            GetSkillTreeStatValue(
                SkillTreeStatType
                    .ShieldPercent,
                -1f
            );

        if (shieldPercent >= 0f)
        {
            return Mathf.Max(
                0,
                Mathf.RoundToInt(
                    maxHp *
                    shieldPercent
                )
            );
        }

        return Mathf.Max(
            0,
            config.woodShieldAmount
        );
    }

    private void ResolveWoodBlessingController()
    {
        GameObject searchObject = caster != null
            ? caster
            : gameObject;

        if (searchObject == null)
            return;

        woodBlessingController =
            searchObject.GetComponent<PlayerWoodBlessingController>();

        if (woodBlessingController == null)
        {
            woodBlessingController =
                searchObject.GetComponentInChildren<PlayerWoodBlessingController>();
        }

        if (woodBlessingController == null)
        {
            woodBlessingController =
                searchObject.GetComponentInParent<PlayerWoodBlessingController>();
        }
    }
}