using UnityEngine;

[DisallowMultipleComponent]
public class PlayerDamageReceiver : MonoBehaviour, IDamageable
{
    [Header("HP")]
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp = 100;

    [Header("Perfect Dodge")]
    [SerializeField] private PlayerPerfectDodgeController perfectDodgeController;

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

    private void Awake()
    {
        CacheReferences();
        ResetHp();
    }

    private void CacheReferences()
    {
        if (perfectDodgeController == null)
        {
            perfectDodgeController = GetComponent<PlayerPerfectDodgeController>();
        }

        if (perfectDodgeController == null)
        {
            perfectDodgeController = GetComponentInChildren<PlayerPerfectDodgeController>();
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isDead)
        {
            return;
        }

        if (perfectDodgeController != null)
        {
            if (perfectDodgeController.TryPerfectDodge(damageInfo))
            {
                // 完美闪避成功：不扣血，不发受伤事件。
                return;
            }

            if (perfectDodgeController.CanIgnoreDamageByInvincibleWindow())
            {
                if (debugLog)
                {
                    Debug.Log("[PlayerDamageReceiver] Damage ignored by invincible window.", this);
                }

                // 普通闪避无敌：不扣血，不发受伤事件。
                return;
            }
        }

        ApplyDamage(damageInfo);
    }

    private void ApplyDamage(DamageInfo damageInfo)
    {
        int damage = Mathf.Max(0, damageInfo.damage);

        currentHp -= damage;
        currentHp = Mathf.Max(0, currentHp);

        bool willDie = currentHp <= 0;

        TriggerPlayerDamagedEvent(damageInfo, damage, willDie);

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
            OnDead(damageInfo);
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