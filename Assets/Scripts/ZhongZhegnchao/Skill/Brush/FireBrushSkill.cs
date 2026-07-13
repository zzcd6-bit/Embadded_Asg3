using System.Collections.Generic;
using UnityEngine;

public class FireBrushSkill : BrushSkillBase
{
    private class VisibleFireTarget
    {
        public IDamageable damageable;
        public GameObject targetObject;
        public Vector3 hitPoint;
    }

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

        List<VisibleFireTarget> visibleTargets = FindVisibleEnemyTargets();

        if (visibleTargets.Count <= 0)
        {
            Debug.Log("[FireBrushSkill] No enemy visible on screen.");
            return;
        }

        ApplyFireToVisibleTargets(visibleTargets, context);
        ApplyFireInfusionToPlayer(context);

        if (drawDebug)
        {
            Debug.Log(
                $"[FireBrushSkill] Fire applied to visible enemies. Count = {visibleTargets.Count}"
            );
        }
    }

    private List<VisibleFireTarget> FindVisibleEnemyTargets()
    {
        List<VisibleFireTarget> results = new List<VisibleFireTarget>();
        HashSet<IDamageable> addedTargets = new HashSet<IDamageable>();

        Camera cameraToUse = GetWorldCamera();

        if (cameraToUse == null)
        {
            Debug.LogWarning("[FireBrushSkill] World camera not found.");
            return results;
        }

        Collider[] colliders = Physics.OverlapSphere(
            cameraToUse.transform.position,
            config.rayDistance,
            config.targetLayer,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];

            if (col == null)
                continue;

            if (!IsColliderOnScreen(col, cameraToUse))
                continue;

            IDamageable damageable = col.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            if (addedTargets.Contains(damageable))
                continue;

            Component damageComponent = damageable as Component;

            GameObject targetObject = damageComponent != null
                ? damageComponent.gameObject
                : col.gameObject;

            EnemyWhitebox enemy = targetObject.GetComponent<EnemyWhitebox>();

            if (enemy == null)
            {
                enemy = targetObject.GetComponentInParent<EnemyWhitebox>();
            }

            if (enemy != null && enemy.IsDead)
                continue;

            VisibleFireTarget target = new VisibleFireTarget
            {
                damageable = damageable,
                targetObject = targetObject,
                hitPoint = col.bounds.center
            };

            results.Add(target);
            addedTargets.Add(damageable);
        }

        return results;
    }

    private bool IsColliderOnScreen(Collider col, Camera cameraToUse)
    {
        if (col == null || cameraToUse == null)
            return false;

        Bounds bounds = col.bounds;

        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        Vector3[] checkPoints =
        {
            center,
            center + new Vector3(0f, extents.y, 0f),
            center - new Vector3(0f, extents.y, 0f),
            center + new Vector3(extents.x, 0f, 0f),
            center - new Vector3(extents.x, 0f, 0f),
            center + new Vector3(0f, 0f, extents.z),
            center - new Vector3(0f, 0f, extents.z)
        };

        for (int i = 0; i < checkPoints.Length; i++)
        {
            Vector3 viewportPoint = cameraToUse.WorldToViewportPoint(checkPoints[i]);

            if (viewportPoint.z <= 0f)
                continue;

            if (viewportPoint.x >= 0f &&
                viewportPoint.x <= 1f &&
                viewportPoint.y >= 0f &&
                viewportPoint.y <= 1f)
            {
                return true;
            }
        }

        return false;
    }

    private Camera GetWorldCamera()
    {
        if (castContextBuilder != null && castContextBuilder.worldCamera != null)
        {
            return castContextBuilder.worldCamera;
        }

        return Camera.main;
    }

    private void ApplyFireToVisibleTargets(
        List<VisibleFireTarget> targets,
        BrushCastContext context
    )
    {
        if (targets == null || targets.Count <= 0)
            return;

        GameObject attackerObject = context.caster != null
            ? context.caster
            : gameObject;

        for (int i = 0; i < targets.Count; i++)
        {
            VisibleFireTarget target = targets[i];

            if (target == null)
                continue;

            if (target.damageable == null || target.targetObject == null)
                continue;

            Vector3 hitDirection =
                target.targetObject.transform.position - attackerObject.transform.position;

            if (hitDirection.sqrMagnitude > 0.001f)
            {
                hitDirection.Normalize();
            }
            else
            {
                hitDirection = Vector3.forward;
            }

            DamageInfo damageInfo = new DamageInfo
            {
                attacker = attackerObject,
                target = target.targetObject,

                damage = config.baseDamage,
                knockback = config.knockback,

                hitPoint = target.hitPoint,
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

            target.damageable.TakeDamage(damageInfo);

            ApplyBurningToTarget(target.targetObject, attackerObject);
            ActivateFireVfxOnTarget(target.targetObject);
        }
    }

    private void ApplyBurningToTarget(GameObject targetObject, GameObject attacker)
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
            attacker,
            config.burningDuration,
            config.burningTickInterval,
            config.burningTickDamage
        );
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
            return;

        vfxController.ActivateFireVfx(config.burningDuration);
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

    protected override float GetCooldown()
    {
        if (config == null)
            return 0f;

        if (!config.useSkillCooldown)
            return 0f;

        return config.skillCooldown;
    }

    protected override int GetInkCost()
    {
        if (config == null)
            return 0;

        return config.inkCost;
    }
}