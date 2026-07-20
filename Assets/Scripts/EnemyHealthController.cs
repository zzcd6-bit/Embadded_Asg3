using UnityEngine;

public class EnemyHealthController : MonoBehaviour
{
    [Header("Health")]
    public int currentHealth = 5;
    public bool isDead;

    [Header("Hitbox (only this collider can take damage)")]
    [SerializeField] private Collider[] hitColliders; // 头/身都拖进来

    [Header("References")]
    [SerializeField] private EnemyAI enemyAI;         // 可拖拽；不拖也会自动找

    void Awake()
    {
        if (enemyAI == null) enemyAI = GetComponentInParent<EnemyAI>();
    }


    
    public bool AcceptHit(Collider hit)
    {
        if (hitColliders == null || hitColliders.Length == 0) return true; // 不填就全都接受
        for (int i = 0; i < hitColliders.Length; i++)
            if (hit == hitColliders[i]) return true;
        return false;
    }

    public void DamageEnemy(int damage, Vector3 hitfrom)
    {
        if (isDead) return;
        currentHealth -= damage;
        // ✅ 关键：没死也要触发受击（Reaction=HitStagger）
        if (enemyAI != null && currentHealth > 0)
        {
            Vector3 dir = (enemyAI.transform.position - hitfrom);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = enemyAI.transform.forward;
            enemyAI.ReactToAttack(dir.normalized);
        }
        
        if (currentHealth <= 0)
        {
            isDead = true; 
            if (enemyAI != null) enemyAI.Die();
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // 方便你：如果没拖 enemyAI，就自动补
        if (enemyAI == null) enemyAI = GetComponentInParent<EnemyAI>();
    }
#endif
}
