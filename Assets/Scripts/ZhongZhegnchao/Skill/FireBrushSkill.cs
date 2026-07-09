using System.Collections.Generic;
using UnityEngine;

public class FireBrushSkill : BrushSkillBase
{
    protected override BrushSkillType SkillType
    {
        get { return BrushSkillType.Fire; }
    }

    [Header("Fire Cast")]
    public LayerMask targetLayer;
    public float rayDistance = 200f;
    public float fallbackDistance = 12f;
    public float damageRadius = 3f;

    [Header("Damage")]
    public int damage = 3;
    public float knockback = 0f;

    [Header("Visual Effect")]
    public GameObject fireVfxPrefab;
    public float vfxLifetime = 2f;
    public Vector3 vfxOffset = Vector3.zero;

    [Header("Burning Debuff")]
    public bool applyBurning = true;
    public float burningDuration = 5f;
    public float burningTickInterval = 1f;
    public int burningTickDamage = 1;

    [Header("Player Fire Infusion")]
    public bool applyFireInfusionToPlayer = true;
    public float fireInfusionDuration = 8f;
    public float fireAttackDamageMultiplier = 1.5f;
    public bool fireAttackApplyBurning = true;
    public float fireAttackBurningDuration = 5f;
    public float fireAttackBurningTickInterval = 1f;
    public int fireAttackBurningTickDamage = 1;

    [Header("Debug")]
    public bool drawDebug = true;

    protected override void Execute(BrushGestureResult result, BrushCastContext context)
    {
        if (context == null)
        {
            Debug.LogWarning("[FireBrushSkill] BrushCastContext is null.");
            return;
        }

        Vector3 castPoint = GetCastPoint(context);

        SpawnFireVfx(castPoint);

        if (drawDebug)
        {
            Debug.DrawRay(
                context.centerRay.origin,
                context.centerRay.direction * rayDistance,
                Color.red,
                1.5f
            );

            Debug.Log($"[FireBrushSkill] Cast fire at: {castPoint}");
        }

        ApplyFireDamage(castPoint, context);
        ApplyFireInfusionToPlayer(context);
    }

    private void SpawnFireVfx(Vector3 castPoint)
    {
        if (fireVfxPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = castPoint + vfxOffset;

        GameObject vfx = Instantiate(
            fireVfxPrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (vfxLifetime > 0f)
        {
            Destroy(vfx, vfxLifetime);
        }
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

        return context.centerRay.origin + context.centerRay.direction * fallbackDistance;
    }

    private void ApplyFireDamage(Vector3 castPoint, BrushCastContext context)
    {
        Collider[] colliders = Physics.OverlapSphere(
            castPoint,
            damageRadius,
            targetLayer,
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
                damage = damage,
                knockback = knockback,
                hitPoint = targetObject.transform.position,
                hitDirection = hitDirection,
                sourceAction = null,

                element = ElementType.Fire,
                canApplyElementStatus = true,

                skillMultiplier = 1.2f,
                damageBonus = 0f,
                reactionMultiplier = 1f,
                canCrit = false
            };

            damageable.TakeDamage(damageInfo);

            ApplyBurningToTarget(targetObject);

            damagedTargets.Add(damageable);
        }

        Debug.Log($"[FireBrushSkill] Fire damage target count: {damagedTargets.Count}");
    }

    private void ApplyBurningToTarget(GameObject targetObject)
    {
        if (!applyBurning)
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
            burningDuration,
            burningTickInterval,
            burningTickDamage
        );
    }

    private void ApplyFireInfusionToPlayer(BrushCastContext context)
    {
        if (!applyFireInfusionToPlayer)
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
            fireInfusionDuration,
            fireAttackDamageMultiplier,
            fireAttackApplyBurning,
            fireAttackBurningDuration,
            fireAttackBurningTickInterval,
            fireAttackBurningTickDamage
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
    }
}