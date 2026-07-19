using UnityEngine;

public class BossMinionDeathRelay : MonoBehaviour
{
    [Header("Boss Ref (set by Boss when spawning)")]
    public BossController boss;

    [Header("On Minion Death -> Damage Boss")]
    public int damageToBoss = 200;
    public bool log = true;

    [Header("Force Minion Lock Mode")]
    public bool forceLocked = true;

    private EnemyHealthController _hp;
    private EnemyAI _ai;
    private bool _notified;

    // ✅ 兼容你 Boss 里的 relay.Bind(this, damage)
    public void Bind(BossController bossRef, int dmg)
    {
        boss = bossRef;
        damageToBoss = dmg;

        // 绑定后立刻锁住，防止刚生成那一帧乱跑
        CacheIfNeeded();
        ApplyLock();
    }

    void Awake()
    {
        CacheIfNeeded();
    }

    void OnEnable()
    {
        _notified = false;
        CacheIfNeeded();
        ApplyLock();
    }

    private void OnDisable()
    {
        TryNotify();
    }

    void Update()
    {
        if (_notified) return;

        if (_hp == null)
        {
            if (log) Debug.LogError("[MinionRelay] EnemyHealthController not found on this minion.", this);
            _notified = true;
            return;
        }

        // ✅ 只认“真正死亡”
        if (_hp.isDead)
        {
            _notified = true;

            if (boss != null)
            {
                boss.DamageBoss(damageToBoss, transform.position);
                if (log) Debug.Log($"[MinionRelay] Minion died -> Boss takes {damageToBoss} dmg", this);
            }
            else
            {
                if (log) Debug.LogError("[MinionRelay] boss reference is NULL, cannot damage boss.", this);
            }

            // 只触发一次即可
            Destroy(this);
            return;
        }

        // 轻量维持 locked
        ApplyLock();
    }

    private void CacheIfNeeded()
    {
        if (_hp == null)
        {
            _hp = GetComponent<EnemyHealthController>()
               ?? GetComponentInChildren<EnemyHealthController>()
               ?? GetComponentInParent<EnemyHealthController>();
        }

        if (_ai == null)
        {
            _ai = GetComponent<EnemyAI>()
               ?? GetComponentInChildren<EnemyAI>()
               ?? GetComponentInParent<EnemyAI>();
        }
    }

    private void ApplyLock()
    {
        if (!forceLocked || _ai == null) return;
        _ai.locked = true;
    }

        private void TryNotify()
    {
        if (_notified) return;
        if (_hp == null || boss == null) return;
        if (!_hp.isDead) return;

        _notified = true;
        boss.DamageBoss(damageToBoss, transform.position);
    }

}
