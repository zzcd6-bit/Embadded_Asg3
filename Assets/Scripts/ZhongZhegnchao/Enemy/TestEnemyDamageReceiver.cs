using UnityEngine;

public class TestEnemyDamageReceiver : MonoBehaviour, IDamageable
{
    public void TakeDamage(DamageInfo damageInfo)
    {
        Debug.Log("Enemy Hit! Damage = " + damageInfo.damage);
    }
}