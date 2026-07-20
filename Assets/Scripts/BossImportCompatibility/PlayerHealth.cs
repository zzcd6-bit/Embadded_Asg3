using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    public float maxHP = 100f;
    public float hp = 100f;
    public ElementType damageElement = ElementType.Fire;

    [SerializeField] private MonoBehaviour damageReceiverOverride;

    public event Action<float, Vector3> OnDamaged;
    public event Action OnDead;

    private IDamageable damageReceiver;

    public float HP01 => maxHP <= 0.01f ? 0f : hp / maxHP;

    private void Awake()
    {
        hp = Mathf.Clamp(hp, 0f, maxHP);
        damageReceiver = ResolveDamageReceiver();
    }

    public void TakeDamage(float damage, Vector3 hitFromWorldPos)
    {
        if (damage <= 0f) return;

        Vector3 hitDirection = transform.position - hitFromWorldPos;
        hitDirection.y = 0f;
        if (hitDirection.sqrMagnitude < 0.0001f)
            hitDirection = -transform.forward;
        hitDirection.Normalize();

        damageReceiver ??= ResolveDamageReceiver();
        if (damageReceiver != null)
        {
            damageReceiver.TakeDamage(new DamageInfo
            {
                attacker = null,
                target = gameObject,
                damage = Mathf.Max(1, Mathf.RoundToInt(damage)),
                hitPoint = transform.position,
                hitDirection = hitDirection,
                element = damageElement,
                canApplyElementStatus = true,
                skillMultiplier = 0f,
                reactionMultiplier = 1f,
                canCrit = false
            });
        }

        hp = Mathf.Clamp(hp - damage, 0f, maxHP);
        OnDamaged?.Invoke(damage, hitDirection);

        if (hp <= 0f)
            OnDead?.Invoke();
    }

    private IDamageable ResolveDamageReceiver()
    {
        if (damageReceiverOverride is IDamageable explicitReceiver)
            return explicitReceiver;

        Component[] components = GetComponentsInParent<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] is IDamageable damageable)
                return damageable;
        }

        components = GetComponentsInChildren<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] is IDamageable damageable)
                return damageable;
        }

        return null;
    }
}
