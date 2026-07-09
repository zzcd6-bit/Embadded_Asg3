using UnityEngine;

public class EnemyWhitebox : MonoBehaviour, IDamageable
{
    [Header("HP")]
    public int maxHp = 100;
    public int currentHp = 100;

    [Header("Debug")]
    public bool debugLog = true;

    private void Awake()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        DamageInfo calculatedDamage = DamageCalculator.Calculate(damageInfo);

        int damage = Mathf.Max(0, calculatedDamage.finalDamage);

        currentHp -= damage;
        currentHp = Mathf.Max(0, currentHp);

        if (debugLog)
        {
            Debug.Log(
                $"[EnemyDamageReceiver] Damage={damage}, " +
                $"Element={calculatedDamage.element}, " +
                $"Crit={calculatedDamage.isCritical}, " +
                $"HP={currentHp}/{maxHp}",
                this
            );
        }

        if (currentHp <= 0)
        {
            Debug.Log("[EnemyDamageReceiver] Enemy Dead.", this);
        }
    }
}