using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private bool destroyOnDeath;
    [SerializeField] private float destroyDelay = 2f;

    [Header("Out Of Combat Regeneration")]
    [SerializeField] private float outOfCombatHoldTime = 3f;
    [SerializeField] private float fullHealDuration = 3f;

    [Header("References")]
    [SerializeField] private SimpleEnemy enemy;

    [Header("Debug")]
    [SerializeField] private bool enableLogs;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    private Coroutine regenRoutine;
    private bool inCombat;

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

    public void TakeDamage(DamageInfo damageInfo)
    {
        Transform attacker = damageInfo.attacker != null ? damageInfo.attacker.transform : null;
        TakeDamage(damageInfo.damage, attacker);
    }

    public void TakeDamage(int damage, Transform attacker)
    {
        if (IsDead)
            return;

        int amount = Mathf.Max(0, damage);
        if (amount == 0)
            return;

        CurrentHealth -= amount;
        StopRegenRoutine();

        Log($"Hit for {amount}. HP = {CurrentHealth}/{maxHealth}.");

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            IsDead = true;
            StopRegenRoutine();
            Log("Died.");

            if (enemy != null)
                enemy.Die();

            if (destroyOnDeath)
                Destroy(enemy != null ? enemy.gameObject : gameObject, destroyDelay);

            return;
        }

        if (enemy != null)
            enemy.OnDamaged(attacker);
    }

    public void SetCombatActive(bool active)
    {
        if (IsDead)
            return;

        if (inCombat == active)
            return;

        inCombat = active;

        if (active)
        {
            StopRegenRoutine();
            return;
        }

        if (CurrentHealth < maxHealth)
            regenRoutine = StartCoroutine(OutOfCombatRegenRoutine());
    }

    public void ResetHealth()
    {
        StopRegenRoutine();
        IsDead = false;
        CurrentHealth = Mathf.Max(1, maxHealth);
    }

    private System.Collections.IEnumerator OutOfCombatRegenRoutine()
    {
        float hold = Mathf.Max(0f, outOfCombatHoldTime);
        if (hold > 0f)
            yield return new WaitForSeconds(hold);

        if (IsDead || inCombat)
        {
            regenRoutine = null;
            yield break;
        }

        int startHealth = CurrentHealth;
        float duration = Mathf.Max(0.01f, fullHealDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (IsDead || inCombat)
            {
                regenRoutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            CurrentHealth = Mathf.RoundToInt(Mathf.Lerp(startHealth, maxHealth, t));
            yield return null;
        }

        CurrentHealth = maxHealth;
        regenRoutine = null;
    }

    private void StopRegenRoutine()
    {
        if (regenRoutine == null)
            return;

        StopCoroutine(regenRoutine);
        regenRoutine = null;
    }

    private void Log(string message)
    {
        if (!enableLogs)
            return;

        Debug.Log($"[EnemyHealth] {name}: {message}", this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxHealth < 1)
            maxHealth = 1;

        if (outOfCombatHoldTime < 0f)
            outOfCombatHoldTime = 0f;

        if (fullHealDuration < 0.01f)
            fullHealDuration = 0.01f;

        if (enemy == null)
            enemy = GetComponentInParent<SimpleEnemy>();
    }
#endif
}
