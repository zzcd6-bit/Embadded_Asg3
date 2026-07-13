using UnityEngine;

[DisallowMultipleComponent]
public class PlayerDamageReceiver : MonoBehaviour, IDamageable
{
    [Header("HP")]
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp = 100;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool isDead;

    public int CurrentHp
    {
        get { return currentHp; }
    }

    public int MaxHp
    {
        get { return maxHp; }
    }

    public bool IsDead
    {
        get { return isDead; }
    }

    public void SetHp(int newCurrentHp, int newMaxHp)
    {
        maxHp = Mathf.Max(1, newMaxHp);
        currentHp = Mathf.Clamp(newCurrentHp, 0, maxHp);
        isDead = currentHp <= 0;
    }

    private void Awake()
    {
        ResetHp();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isDead)
        {
            return;
        }

        ApplyDamage(damageInfo);
    }

    private void ApplyDamage(DamageInfo damageInfo)
    {
        DamageInfo calculatedDamage = DamageCalculator.Calculate(damageInfo);
        int damage = Mathf.Max(0, calculatedDamage.finalDamage);

        currentHp -= damage;
        currentHp = Mathf.Max(0, currentHp);

        if (DamageNumberSpawner.Instance != null && damage > 0)
        {
            DamageNumberSpawner.Instance.ShowDamageNumber(
                calculatedDamage,
                transform.position
            );
        }

        bool willDie = currentHp <= 0;

        TriggerPlayerDamagedEvent(calculatedDamage, damage, willDie);

        if (debugLog)
        {
            string attackerName = damageInfo.attacker != null
                ? damageInfo.attacker.name
                : "null";

            Debug.Log(
                $"[PlayerDamageReceiver] Player took damage. " +
                $"attacker={attackerName}, damage={damage}, hp={currentHp}/{maxHp}, willDie={willDie}",
                this
            );
        }

        if (willDie)
        {
            OnDead(calculatedDamage);
        }
    }

    private void TriggerPlayerDamagedEvent(DamageInfo damageInfo, int finalDamage, bool willDie)
    {
        EventCenter.Instance.EventTrigger(
            E_EventType.E_Player_Damaged,
            new CharacterDamagedEventInfo
            {
                attacker = damageInfo.attacker,
                target = gameObject,
                damage = finalDamage,
                hitPoint = damageInfo.hitPoint,
                hitDirection = damageInfo.hitDirection,
                isDead = willDie,
                sourceDamageInfo = damageInfo
            }
        );
    }

    private void OnDead(DamageInfo damageInfo)
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        EventCenter.Instance.EventTrigger(
            E_EventType.E_Player_Dead,
            new CharacterDeadEventInfo
            {
                killer = damageInfo.attacker,
                deadTarget = gameObject,
                deathPosition = transform.position,
                sourceDamageInfo = damageInfo
            }
        );

        if (debugLog)
        {
            string killerName = damageInfo.attacker != null
                ? damageInfo.attacker.name
                : "null";

            Debug.Log(
                $"[PlayerDamageReceiver] Player Dead. killer={killerName}",
                this
            );
        }

        // TODO:
        // 1. 禁用输入
        // 2. 播放死亡动画
        // 3. 打开 GameOver UI
        // 4. 后续这些都可以通过 E_Player_Dead 的监听器处理
    }

    public void ResetHp()
    {
        currentHp = maxHp;
        isDead = false;
    }
}