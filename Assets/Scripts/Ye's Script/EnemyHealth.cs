using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private bool destroyOnDeath;
    [SerializeField] private float destroyDelay = 2f;

    [Header("References")]
    [SerializeField] private SimpleEnemy enemy;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);

        if (enemy == null)
            enemy = GetComponentInParent<SimpleEnemy>();
    }

    public void TakeDamage(int damage)
    {
        Transform attacker = PlayerController.instance != null ? PlayerController.instance.transform : null;
        TakeDamage(damage, attacker);
    }

    public void TakeDamage(int damage, Transform attacker)
    {
        if (IsDead)
            return;

        int amount = Mathf.Max(0, damage);
        if (amount == 0)
            return;

        CurrentHealth -= amount;

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            IsDead = true;
            if (enemy != null)
                enemy.Die();

            if (destroyOnDeath)
                Destroy(enemy != null ? enemy.gameObject : gameObject, destroyDelay);

            return;
        }

        if (enemy != null)
            enemy.OnDamaged(attacker);
    }

    public void ResetHealth()
    {
        IsDead = false;
        CurrentHealth = Mathf.Max(1, maxHealth);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxHealth < 1)
            maxHealth = 1;

        if (enemy == null)
            enemy = GetComponentInParent<SimpleEnemy>();
    }
#endif
}
