using System.Collections.Generic;
using UnityEngine;

public class SlashBrushSkill : BrushSkillBase
{
    protected override BrushSkillType SkillType
    {
        get { return BrushSkillType.Slash; }
    }

    [Header("Slash Detection")]
    public LayerMask targetLayer;
    public float rayDistance = 200f;
    public float sphereRadius = 1.5f;
    public int sampleCount = 12;

    [Header("Damage")]
    public int damage = 1;

    [Header("Debug")]
    public bool drawDebugRay = true;

    protected override void Execute(BrushGestureResult result, BrushCastContext context)
    {
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

        for (int i = 0; i < sampleCount; i++)
        {
            float t = sampleCount <= 1 ? 0.5f : i / (float)(sampleCount - 1);
            int index = Mathf.RoundToInt(t * (screenPoints.Count - 1));

            Vector2 screenPoint = screenPoints[index];

            Ray ray = castContextBuilder.GetWorldRayFromStrokePoint(screenPoint);

            if (drawDebugRay)
                UnityEngine.Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red, 1.5f);

            RaycastHit[] hits = Physics.SphereCastAll(
                ray,
                sphereRadius,
                rayDistance,
                targetLayer,
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
                    damage = damage,
                    knockback = 0f,
                    hitPoint = hit.point,
                    hitDirection = ray.direction,
                    sourceAction = null,

                    element = ElementType.Physical,
                    canApplyElementStatus = false,

                    skillMultiplier = 0.8f,
                    damageBonus = 0f,
                    reactionMultiplier = 1f,
                    canCrit = true
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
}