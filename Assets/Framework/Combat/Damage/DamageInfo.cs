using UnityEngine;

public struct DamageInfo
{
    public GameObject attacker;
    public GameObject target;

    public int damage;
    public float knockback;

    public Vector3 hitPoint;
    public Vector3 hitDirection;

    public ActionConfig sourceAction;
}

public interface IDamageable
{
    void TakeDamage(DamageInfo damageInfo);
}