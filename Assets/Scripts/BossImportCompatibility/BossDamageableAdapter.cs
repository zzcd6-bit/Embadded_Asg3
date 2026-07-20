using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BossController))]
public class BossDamageableAdapter : MonoBehaviour, IDamageable
{
    [SerializeField] private BossController boss;

    private void Awake()
    {
        if (boss == null)
            boss = GetComponent<BossController>();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (boss == null)
            boss = GetComponent<BossController>();

        if (boss == null || boss.IsDead)
            return;

        damageInfo.target = gameObject;
        DamageInfo calculatedDamage = DamageCalculator.Calculate(damageInfo);
        int damage = Mathf.Max(0, calculatedDamage.finalDamage);
        if (damage <= 0)
            return;

        Vector3 hitFrom = calculatedDamage.attacker != null
            ? calculatedDamage.attacker.transform.position
            : calculatedDamage.hitPoint;

        boss.DamageBoss(damage, hitFrom);
    }
}
