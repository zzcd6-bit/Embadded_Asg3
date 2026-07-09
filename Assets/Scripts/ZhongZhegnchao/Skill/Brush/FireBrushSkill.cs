using System.Collections.Generic;
using UnityEngine;

public class FireBrushSkill : BrushSkillBase
{
    protected override BrushSkillType SkillType
    {
        get { return BrushSkillType.Fire; }
    }
    [Header("Config")]
    public BrushSkillConfig config;

    [Header("Debug")]
    public bool drawDebug = true;

    protected override void Execute(BrushGestureResult result, BrushCastContext context)
    {
        if (config == null)
        {
            Debug.LogWarning("[FireBrushSkill] Config is missing.");
            return;
        }

        if (config.skillType != BrushSkillType.Fire)
        {
            Debug.LogWarning("[FireBrushSkill] Wrong config skill type.");
            return;
        }

        if (context == null)
        {
            Debug.LogWarning("[FireBrushSkill] BrushCastContext is null.");
            return;
        }

        Vector3 castPoint = GetCastPoint(context);

        if (drawDebug)
        {
            Debug.DrawRay(
                context.centerRay.origin,
                context.centerRay.direction * config.rayDistance,
                Color.red,
                1.5f
            );

            Debug.Log($"[FireBrushSkill] Cast fire at: {castPoint}");
        }

        ApplyFireDamage(castPoint, context);
        ApplyFireInfusionToPlayer(context);
    }

    private Vector3 GetCastPoint(BrushCastContext context)
    {
        if (context.hasTarget)
        {
            return context.targetPoint;
        }

        if (context.hasGroundPoint)
        {
            return context.groundPoint;
        }

        return context.centerRay.origin + context.centerRay.direction * config.fallbackDistance;
    }

    private void ApplyFireDamage(Vector3 castPoint, BrushCastContext context)
    {
        Collider[] colliders = Physics.OverlapSphere(
            castPoint,
            config.fireDamageRadius,
            config.targetLayer,
            QueryTriggerInteraction.Collide
        );

        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];

            if (col == null)
                continue;

            IDamageable damageable = col.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            if (damagedTargets.Contains(damageable))
                continue;

            Component damageComponent = damageable as Component;

            GameObject targetObject = damageComponent != null
                ? damageComponent.gameObject
                : col.gameObject;

            Vector3 hitDirection = targetObject.transform.position - transform.position;

            if (hitDirection.sqrMagnitude > 0.001f)
            {
                hitDirection.Normalize();
            }
            else
            {
                hitDirection = context.centerRay.direction;
            }

            DamageInfo damageInfo = new DamageInfo
            {
                attacker = context.caster != null ? context.caster : gameObject,
                target = targetObject,

                damage = config.baseDamage,
                knockback = config.knockback,

                hitPoint = targetObject.transform.position,
                hitDirection = hitDirection,
                sourceAction = null,

                element = config.element,
                canApplyElementStatus = config.canApplyElementStatus,

                skillMultiplier = config.skillMultiplier,
                damageBonus = config.damageBonus,
                reactionMultiplier = config.reactionMultiplier,
                reactionType = ElementReactionType.None,
                canCrit = config.canCrit
            };

            damageable.TakeDamage(damageInfo);

            ApplyBurningToTarget(targetObject);
            ActivateFireVfxOnTarget(targetObject);

            damagedTargets.Add(damageable);
        }

        Debug.Log($"[FireBrushSkill] Fire damage target count: {damagedTargets.Count}");
    }

    private void ActivateFireVfxOnTarget(GameObject targetObject)
    {
        if (targetObject == null)
            return;

        ElementVfxController vfxController =
            targetObject.GetComponent<ElementVfxController>();

        if (vfxController == null)
        {
            vfxController = targetObject.GetComponentInParent<ElementVfxController>();
        }

        if (vfxController == null)
        {
            vfxController = targetObject.GetComponentInChildren<ElementVfxController>();
        }

        if (vfxController == null)
        {
            return;
        }

        vfxController.ActivateFireVfx(config.burningDuration);
    }

    private void ApplyBurningToTarget(GameObject targetObject)
    {
        if (config == null || !config.applyBurning)
            return;

        if (targetObject == null)
            return;

        ElementStatusController statusController =
            targetObject.GetComponent<ElementStatusController>();

        if (statusController == null)
        {
            statusController = targetObject.GetComponentInParent<ElementStatusController>();
        }

        if (statusController == null)
        {
            statusController = targetObject.AddComponent<ElementStatusController>();
        }

        statusController.ApplyBurning(
            gameObject,
            config.burningDuration,
            config.burningTickInterval,
            config.burningTickDamage
        );
    }

    private void ApplyFireInfusionToPlayer(BrushCastContext context)
    {
        if (config == null || !config.applyInfusionToPlayer)
            return;

        PlayerElementInfusion infusion = null;

        if (context != null && context.caster != null)
        {
            infusion = context.caster.GetComponent<PlayerElementInfusion>();

            if (infusion == null)
            {
                infusion = context.caster.GetComponentInParent<PlayerElementInfusion>();
            }

            if (infusion == null)
            {
                infusion = context.caster.GetComponentInChildren<PlayerElementInfusion>();
            }
        }

        if (infusion == null)
        {
            infusion = GetComponentInParent<PlayerElementInfusion>();
        }

        if (infusion == null)
        {
            infusion = FindObjectOfType<PlayerElementInfusion>();
        }

        if (infusion == null)
        {
            Debug.LogWarning("[FireBrushSkill] PlayerElementInfusion not found.");
            return;
        }

        infusion.ApplyFireInfusion(
            config.infusionDuration,
            config.infusionDamageMultiplier,
            config.infusionApplyBurningOnHit,
            config.infusionBurningDuration,
            config.infusionBurningTickInterval,
            config.infusionBurningTickDamage
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
    }
}