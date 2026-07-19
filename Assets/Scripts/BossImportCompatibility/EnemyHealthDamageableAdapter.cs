using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealthController))]
public class EnemyHealthDamageableAdapter : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyHealthController enemyHealth;

    private void Awake()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealthController>();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealthController>();

        if (enemyHealth == null || enemyHealth.isDead)
            return;

        damageInfo.target = gameObject;
        DamageInfo calculatedDamage = DamageCalculator.Calculate(damageInfo);
        int damage = Mathf.Max(0, calculatedDamage.finalDamage);
        if (damage <= 0)
            return;

        Vector3 hitFrom = calculatedDamage.attacker != null
            ? calculatedDamage.attacker.transform.position
            : calculatedDamage.hitPoint;

        enemyHealth.DamageEnemy(damage, hitFrom);
    }
}
