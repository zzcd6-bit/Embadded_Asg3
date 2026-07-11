using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SlashBrushSkill : BrushSkillBase
{
    protected override BrushSkillType SkillType
    {
        get { return BrushSkillType.Slash; }
    }

    [Header("Config")]
    public BrushSkillConfig config;

    [Header("Debug")]
    public bool drawDebugRay = true;

    protected override void Execute(BrushGestureResult result, BrushCastContext context)
    {
        if (config == null)
        {
            Debug.LogWarning("[SlashBrushSkill] Config is missing.");
            return;
        }

        if (config.skillType != BrushSkillType.Slash)
        {
            Debug.LogWarning("[SlashBrushSkill] Wrong config skill type.");
            return;
        }

        if (context != null)
        {
            Debug.Log(
                $"[SlashBrushSkill] Context created. " +
                $"HasTarget: {context.hasTarget}, " +
                $"HasGround: {context.hasGroundPoint}, " +
                $"CenterPoint: {context.centerScreenPoint}"
            );
        }
        else
        {
            Debug.LogWarning("[SlashBrushSkill] Cast context is null.");
        }

        if (castContextBuilder == null)
        {
            Debug.LogWarning("[SlashBrushSkill] CastContextBuilder is missing.");
            return;
        }

        if (context == null)
        {
            Debug.LogWarning("[SlashBrushSkill] BrushCastContext is null.");
            return;
        }

        List<Vector2> screenPoints = result.strokeData.screenPoints;

        if (screenPoints == null || screenPoints.Count < 2)
            return;

        Dictionary<IDamageable, DamageInfo> hitTargets = new Dictionary<IDamageable, DamageInfo>();

        for (int i = 0; i < config.slashSampleCount; i++)
        {
            float t = config.slashSampleCount <= 1
                ? 0.5f
                : i / (float)(config.slashSampleCount - 1);
            int index = Mathf.RoundToInt(t * (screenPoints.Count - 1));

            Vector2 screenPoint = screenPoints[index];

            Ray ray = castContextBuilder.GetWorldRayFromStrokePoint(screenPoint);

            if (drawDebugRay)
                UnityEngine.Debug.DrawRay(ray.origin, ray.direction * config.rayDistance, Color.red, 1.5f);

            RaycastHit[] hits = Physics.SphereCastAll(
                ray,
                config.slashSphereRadius,
                config.rayDistance,
                config.targetLayer,
                QueryTriggerInteraction.Collide
            );

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                    continue;

                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

                if (damageable == null)
                    continue;

                if (hitTargets.ContainsKey(damageable))
                    continue;

                UnityEngine.Component damageComponent = damageable as UnityEngine.Component;

                GameObject targetObject = damageComponent != null
                    ? damageComponent.gameObject
                    : hit.collider.gameObject;

                DamageInfo damageInfo = new DamageInfo
                {
                    attacker = context.caster != null ? context.caster : gameObject,
                    target = targetObject,

                    damage = config.baseDamage,
                    knockback = config.knockback,

                    hitPoint = hit.point,
                    hitDirection = ray.direction,
                    sourceAction = null,

                    element = config.element,
                    canApplyElementStatus = config.canApplyElementStatus,

                    skillMultiplier = config.skillMultiplier,
                    damageBonus = config.damageBonus,
                    reactionMultiplier = config.reactionMultiplier,
                    reactionType = ElementReactionType.None,
                    canCrit = config.canCrit
                };

                hitTargets.Add(damageable, damageInfo);
            }
        }

        foreach (KeyValuePair<IDamageable, DamageInfo> pair in hitTargets)
        {
            pair.Key.TakeDamage(pair.Value);
        }

        UnityEngine.Debug.Log($"Slash executed. Damage target count: {hitTargets.Count}");
    }

    protected override int GetInkCost()
    {
        if (config == null)
            return 0;

        return config.inkCost;
    }
}