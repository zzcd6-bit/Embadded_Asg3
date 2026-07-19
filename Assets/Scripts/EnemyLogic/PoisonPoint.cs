using System.Collections;
using UnityEngine;

public class PoisonPoint : MonoBehaviour
{
    [Header("VFX Prefabs (drag here)")]
    public GameObject warningVfxPrefab;   // 预警圈（生成后一直存在到结束）
    public GameObject impactVfxPrefab;    // 落下/爆炸特效（会自动销毁）

    [Header("Auto Destroy (fallback)")]
    public float impactVfxFallbackLife = 4f; // prefab 没粒子时的兜底销毁时间

    [Header("Debug Gizmos")]
    public bool drawGizmos = true;

    // runtime
    private BossController _owner;
    private Transform _expectedPlayerRoot;

    private float _groundY;
    private float _extraRandomDelay;
    private float _hitRadius;
    private float _damage;
    private LayerMask _mask;

    private float _windowTime;
    private float _hitInterval;
    private float _lifeAfterWindow;

    private bool _started;
    private bool _finishedNotified;

    private GameObject _warningInstance;

    // NonAlloc cache
    private static readonly Collider[] _hits = new Collider[32];

    [Header("Damage Rules")]
    public bool damageOncePerPoint = true;
    private bool _didDamage;

    /// <summary>
    /// 生成点后立刻调用：只负责“出现预警”和参数初始化。
    /// 真正落下由 BeginStrike() 触发（这样就能严格按 WarningTime -> 动画 -> AnimateDelay）。
    /// </summary>
    public void Setup(
        BossController owner,
        float forcedGroundY,
        float extraRandomDelay,
        float hitRadius,
        float damage,
        LayerMask mask,
        float windowTime,
        float hitInterval,
        float lifeAfterWindow
    )
    {
        _owner = owner;
        _expectedPlayerRoot = (owner != null && owner.player != null) ? owner.player.root : null;

        _groundY = forcedGroundY;
        _extraRandomDelay = Mathf.Max(0f, extraRandomDelay);

        _hitRadius = Mathf.Max(0.05f, hitRadius);
        _damage = damage;
        _mask = mask;

        _windowTime = Mathf.Max(0f, windowTime);
        _hitInterval = Mathf.Max(0.01f, hitInterval);
        _lifeAfterWindow = Mathf.Max(0f, lifeAfterWindow);

        // 贴地
        Vector3 p = transform.position;
        p.y = _groundY;
        transform.position = p;

        // 生成预警（只一次）
        if (warningVfxPrefab != null)
            _warningInstance = Instantiate(warningVfxPrefab, transform.position, Quaternion.identity);
    }

    /// <summary>
    /// Boss 在 “WarningTime 结束 + 动画开始” 时调用。
    /// animateDelay = 从动画触发到落下特效的延迟（你要的 AnimateDelay）。
    /// </summary>
    public void BeginStrike(float animateDelay)
    {
        if (_started) return;
        _started = true;
        _didDamage = false;
        StartCoroutine(StrikeRoutine(Mathf.Max(0f, animateDelay)));
    }

    private IEnumerator StrikeRoutine(float animateDelay)
    {
        // 动画开始后计时：AnimateDelay + 每点随机 0~1s
        float wait = animateDelay + _extraRandomDelay;
        if (wait > 0f) yield return new WaitForSeconds(wait);

        // 落下特效（自动销毁）
        if (impactVfxPrefab != null)
        {
            GameObject impact = Instantiate(impactVfxPrefab, transform.position, Quaternion.identity);
            AutoDestroyVfx(impact, impactVfxFallbackLife);
        }

        // 窗口持续检测
        float end = Time.time + _windowTime;
        while (Time.time < end)
        {
            DoHitOnce();
            yield return new WaitForSeconds(_hitInterval);
        }

        // 窗口结束后保留一小段时间（让玩家看到残留/毒液）
        if (_lifeAfterWindow > 0f)
            yield return new WaitForSeconds(_lifeAfterWindow);

        if (_warningInstance != null) Destroy(_warningInstance);

        NotifyFinishedAndDestroy();
    }

    private void DoHitOnce()
    {
        if (damageOncePerPoint && _didDamage) return;
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            _hitRadius,
            _hits,
            _mask,
            QueryTriggerInteraction.Ignore
        );

        if (count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            Collider col = _hits[i];
            if (col == null) continue;

            if (_expectedPlayerRoot != null && col.transform.root != _expectedPlayerRoot) continue;

            PlayerHealth hp =
                col.GetComponentInParent<PlayerHealth>() ??
                col.GetComponentInChildren<PlayerHealth>();

            if (hp == null) continue;

            hp.TakeDamage(_damage, (_owner != null) ? _owner.transform.position : transform.position);
            _didDamage = true;
            break; // 一次点只需要命中一次即可（窗口内反复检测是为了更稳中）
        }
    }

    private void NotifyFinishedAndDestroy()
    {
        if (!_finishedNotified)
        {
            _finishedNotified = true;
            if (_owner != null)
                _owner.NotifyPoisonPointFinished(this);
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // 防止外部提前 Destroy 导致 Boss 永远等不到回调
        if (!_finishedNotified && _owner != null)
            _owner.NotifyPoisonPointFinished(this);
    }

    private static void AutoDestroyVfx(GameObject go, float fallbackLife)
    {
        if (go == null) return;

        // ParticleSystem：用 duration + startLifetime 估算
        ParticleSystem ps = go.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            float life = main.duration;

            // startLifetime 可能是常量/范围
            var sl = main.startLifetime;
            if (sl.mode == ParticleSystemCurveMode.Constant)
                life += sl.constant;
            else if (sl.mode == ParticleSystemCurveMode.TwoConstants)
                life += sl.constantMax;
            else
                life += 1.5f; // 兜底

            Destroy(go, Mathf.Clamp(life + 0.2f, 0.5f, 20f));
            return;
        }

        // 没粒子：兜底
        Destroy(go, Mathf.Clamp(fallbackLife, 0.5f, 20f));
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 1f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, _hitRadius));
    }
#endif
}
