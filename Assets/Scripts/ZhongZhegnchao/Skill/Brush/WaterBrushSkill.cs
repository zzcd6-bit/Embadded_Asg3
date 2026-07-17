using UnityEngine;

public class WaterBrushSkill : BrushSkillBase
{
    [Header("Config")]
    public BrushSkillConfig config;

    [Header("Water Aura Shooter")]
    public PlayerWaterAuraShooter waterAuraShooter;

    protected override BrushSkillType SkillType
    {
        get { return BrushSkillType.Water; }
    }

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
            Debug.LogWarning("[WaterBrushSkill] Config is missing.", this);
            return;
        }

        if (waterAuraShooter == null)
        {
            ResolveWaterAuraShooter();
        }

        if (waterAuraShooter == null)
        {
            Debug.LogWarning("[WaterBrushSkill] PlayerWaterAuraShooter not found.", this);
            return;
        }

        float damageMultiplier =
            Mathf.Max(
                0f,
                GetSkillTreeStatValue(
                    SkillTreeStatType
                        .DamageMultiplier,
                    1f
                )
            );

        float projectileSpeedMultiplier =
            Mathf.Max(
                0.01f,
                GetSkillTreeStatValue(
                    SkillTreeStatType
                        .ProjectileSpeedMultiplier,
                    1f
                )
            );

        waterAuraShooter.ActivateWaterAura(
            config,
            damageMultiplier,
            projectileSpeedMultiplier
        );

        Debug.Log("[WaterBrushSkill] Water aura activated.", this);
    }

    private void ResolveWaterAuraShooter()
    {
        GameObject searchObject = caster != null
            ? caster
            : gameObject;

        if (searchObject == null)
            return;

        waterAuraShooter = searchObject.GetComponent<PlayerWaterAuraShooter>();

        if (waterAuraShooter == null)
        {
            waterAuraShooter = searchObject.GetComponentInChildren<PlayerWaterAuraShooter>();
        }

        if (waterAuraShooter == null)
        {
            waterAuraShooter = searchObject.GetComponentInParent<PlayerWaterAuraShooter>();
        }
    }
}