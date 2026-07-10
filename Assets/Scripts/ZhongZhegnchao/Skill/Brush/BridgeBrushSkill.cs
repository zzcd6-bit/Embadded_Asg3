using UnityEngine;

public class BridgeBrushSkill : BrushSkillBase
{
    protected override BrushSkillType SkillType
    {
        get { return BrushSkillType.Bridge; }
    }

    [Header("Config")]
    public BrushSkillConfig config;

    [Header("Debug")]
    public bool drawDebug = true;

    protected override void Execute(BrushGestureResult result, BrushCastContext context)
    {
        if (config == null)
        {
            Debug.LogWarning("[BridgeBrushSkill] Config is missing.");
            return;
        }

        if (config.skillType != BrushSkillType.Bridge)
        {
            Debug.LogWarning("[BridgeBrushSkill] Wrong config skill type.");
            return;
        }

        if (context == null)
        {
            Debug.LogWarning("[BridgeBrushSkill] BrushCastContext is null.");
            return;
        }

        BridgePreset bridgePreset = FindBridgePreset(context);

        if (bridgePreset == null)
        {
            Debug.Log("[BridgeBrushSkill] No BridgePreset detected.");
            return;
        }

        bridgePreset.ActivateBridge(config.bridgeActiveDuration);

        if (drawDebug)
        {
            Debug.Log($"[BridgeBrushSkill] Activated bridge: {bridgePreset.name}", bridgePreset);
        }
    }

    private BridgePreset FindBridgePreset(BrushCastContext context)
    {
        Ray ray = context.centerRay;

        if (drawDebug)
        {
            Debug.DrawRay(
                ray.origin,
                ray.direction * config.rayDistance,
                Color.cyan,
                1.5f
            );
        }

        RaycastHit hit;

        bool hasHit = Physics.SphereCast(
            ray,
            config.bridgeCastRadius,
            out hit,
            config.rayDistance,
            config.bridgeTriggerLayer,
            QueryTriggerInteraction.Collide
        );

        if (!hasHit || hit.collider == null)
        {
            return null;
        }

        BridgePreset preset = hit.collider.GetComponent<BridgePreset>();

        if (preset == null)
        {
            preset = hit.collider.GetComponentInParent<BridgePreset>();
        }

        if (preset == null)
        {
            preset = hit.collider.GetComponentInChildren<BridgePreset>();
        }

        return preset;
    }
}