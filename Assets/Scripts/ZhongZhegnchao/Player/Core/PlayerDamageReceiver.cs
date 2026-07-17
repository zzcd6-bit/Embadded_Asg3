using UnityEngine;

[DisallowMultipleComponent]
public class PlayerDamageReceiver : MonoBehaviour, IDamageable, IHealable
{
    [Header("HP")]
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp = 100;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool isDead;
    private PlayerAnimationController animationController;
    private ActionPlayer actionPlayer;

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
        bool wasDead = isDead;

        maxHp = Mathf.Max(1, newMaxHp);

        currentHp = Mathf.Clamp(
            newCurrentHp,
            0,
            maxHp
        );

        isDead = currentHp <= 0;

        if (!wasDead && isDead)
        {
            PlayDeathPresentation();
            GameModeManager.Instance.RequestMode(GameModeState.Dead, this, true);
        }
        else if (wasDead && !isDead)
        {
            ResetDeathPresentation();
            GameModeManager.Instance.ExitMode(GameModeState.Dead);
        }
    }

    private void Awake()
    {
        ResolveReferences();
        ResetHp();
    }

    private void ResolveReferences()
    {
        if (animationController == null)
        {
            animationController =
                GetComponent<PlayerAnimationController>();
        }

        if (animationController == null)
        {
            animationController =
                GetComponentInChildren<
                    PlayerAnimationController>(true);
        }

        if (actionPlayer == null)
        {
            actionPlayer =
                GetComponent<ActionPlayer>();
        }

        if (actionPlayer == null)
        {
            actionPlayer =
                GetComponentInChildren<ActionPlayer>(true);
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (!GameModeManager.Instance.CurrentCapabilities.canTakeDamage)
        {
            return;
        }

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

        PlayDeathPresentation();
        GameModeManager.Instance.RequestMode(GameModeState.Dead, this, true);

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

    private void PlayDeathPresentation()
    {
        ResolveReferences();

        if (actionPlayer != null)
        {
            actionPlayer.InterruptAction();
        }

        if (animationController != null)
        {
            animationController.PlayDeath();
        }
    }

    private void ResetDeathPresentation()
    {
        ResolveReferences();

        if (animationController != null)
        {
            animationController.ResetAfterDeath();
        }
    }

    public int Heal(int amount)
    {
        amount = Mathf.Max(0, amount);

        if (amount <= 0)
            return 0;

        int oldHp = currentHp;

        currentHp = Mathf.Min(
            currentHp + amount,
            maxHp
        );

        int actualHeal = currentHp - oldHp;

        if (actualHeal > 0)
        {
            // 如果你有 HP UI，在这里刷新
            // UpdateHpUI();
        }

        return actualHeal;
    }

    public void ResetHp()
    {
        bool wasDead = isDead;

        currentHp = maxHp;
        isDead = false;

        if (wasDead)
        {
            ResetDeathPresentation();
            GameModeManager.Instance.ExitMode(GameModeState.Dead);
        }
    }
}
