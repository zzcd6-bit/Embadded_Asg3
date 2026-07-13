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

        woodBlessingController.ApplyWoodBlessing(config);

        Debug.Log("[WoodBrushSkill] Wood blessing activated.", this);
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