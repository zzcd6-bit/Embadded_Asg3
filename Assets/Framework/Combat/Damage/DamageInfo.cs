using UnityEngine;

public struct DamageInfo
{
    public GameObject attacker;
    public GameObject target;

    // Old field, keep it as base damage.
    public int damage;
    public float knockback;

    public Vector3 hitPoint;
    public Vector3 hitDirection;

    public ActionConfig sourceAction;

    // Element
    public ElementType element;
    public bool canApplyElementStatus;

    // Damage formula
    public float skillMultiplier;
    public float damageBonus;
    public float reactionMultiplier;

    // Critical
    public bool canCrit;
    public bool isCritical;

    // Result
    public int finalDamage;
}

public interface IDamageable
{
    void TakeDamage(DamageInfo damageInfo);
}