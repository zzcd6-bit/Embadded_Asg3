using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossController : MonoBehaviour
{
    Animator animator;

    [Header("Target")]
    public Transform player;
    public string playerTag = "Player";
    public float turnSpeed = 6f;
    public bool facePlayer = true;
    public bool snapFaceOnWarning = true; // 预警开始瞬间对齐玩家一次

    [Header("Health")]
    public int maxHP = 1000;
    [SerializeField] private int currentHP;

    [Header("Damage Rules")]
    [Tooltip("如果不填，则默认：任何非Trigger的Collider都可受伤；如果填写，则只有列表内Collider可受伤")]
    public Collider[] validHitColliders;
    public bool invulnerable = false;

    [Header("Debug")]
    public bool logDamage = true;

    public event Action<int, int> OnHealthChanged;
    public event Action<int, Vector3> OnDamaged;
    public event Action OnDied;

    public int CurrentHP => currentHP;
    public bool IsDead => currentHP <= 0;

    private bool _attackRequested;
    private bool _isAttacking;
    private float _nextAttackTime;
    private Coroutine _attackRoutine;
    private int attackCount = 0;

    // =========================================================
    // Attack Select (keep minimal)
    // =========================================================
    public enum BossAttackType { SlamB, SweepC, AoeA, SummonD}
    [SerializeField] public BossAttackType _currentAttack = BossAttackType.AoeA;
    
    public float attackCoolDown = 2.5f;     // 冷却
    public float attackCoolDownD = 10f;     // 冷却

    // =========================================================
    // Attack A (AOE Poison Drops)
    // =========================================================
    [Header("Attack A (AOE Poison Drops)")]
    public bool enableAttackA = true;

    [Header("A Timing (clean)")]
    public float warningTimeA = 1.0f;       // 预警先出现 1 秒
    public float animateDelayA = 1.0f;      // 动画触发后 1 秒落下特效
    public float windowTimeA = 0.15f;       // 伤害窗口持续时间（越大越容易判中）
    public float hitTickA = 0.05f;          // 窗口内检测间隔（越小越密越耗）
    public float lifeAfterWindowA = 0.4f;   // 窗口结束后残留多久再销毁

    [Header("A Spawn")]
    public int spawnCount = 6;
    public float perPointRandomDelayMax = 1.0f;

    [Header("A Spawn Area (Rectangle, centered on Boss)")]
    public float areaWidth = 14f;
    public float areaLength = 18f;
    public float minSpacing = 1.2f;
    public int triesPerPoint = 12;

    [Header("A Prefabs")]
    public GameObject poisonPointPrefab; // 必须带 PoisonPoint
    public string animAttackATrigger = "AttackA";

    [Header("A Damage")]
    public float damageA = 18f;
    public float hitRadiusA = 2.0f;

    public float attackWindowTime = 0.15f;   // 落下后多久进行命中判定（命中一次）
    public float pointLifeAfterHit = 0.4f;   // 命中后再留多久销毁

    // runtime
    private readonly List<Vector3> _lastSpawnPoints = new List<Vector3>(64);
    private readonly List<PoisonPoint> _activePoisonPoints = new List<PoisonPoint>(64);

    private int _pointsRemainingToHit = 0;
    public float delayBeforeHitA = 0.0f; 
    
    // ===============================
    // Attack B loop (SMB-driven)
    // ===============================
    [Header("B Slam (Loop Only)")]
    public bool enableAttackB = true;
    public float reactionTime = 0.9f;       // 预警持续/反应时间
    public float slamOffset = 2.5f;         // 预警中心在前方距离
    public float groundY = 0.12f;           // 预警贴地高度（建议>=0.08）

    [Header("Attack B Damage")]
    public float damageB = 20f;
    public LayerMask playerMask;           // 只勾 Player layer
    public bool damageOncePerAttack = true;// 一次攻击只扣一次
    public float windowHitInterval = 0.08f;// 若允许多次判定，用这个间隔（秒）

    bool _attackWindowOpen;
    bool _hasDealtDamageThisAttack;
    float _nextWindowHitTime;


    [Header("Warning Prefab")]
    public GameObject warningPrefab;
    public Transform warningOrigin;
    private WarningArea _warningArea;      // ✅ 从预警实例上拿
    private PlayerHealth _playerHealth;    // ✅ 从 player 上拿
    private Transform _playerRoot;         // ✅ 用于对比 root（可选）


    [Header("Animator Params")]
    public string animAttackBTrigger = "AttackB";
    public string animIsAttackingBool = ""; // 你Animator里没有就留空

    private GameObject _warningInstance;

    // ===============================
    // [ADD] Attack C (Sweep / Sector)
    // ===============================
    [Header("Attack C Sweep (Sector)")]
    public bool enableSweepC = true;            // 是否允许C参与循环
    [Range(0f, 1f)] public float sweepCWeight = 0.5f; // B/C随机权重：越大越偏向C

    public float damageC = 15f;

    [Tooltip("Forward offset of the sector center (meters).")]
    public float sweepOffset = 1.2f;

    [Tooltip("Force the sector to stick to this Y (like your groundY for B).")]
    public float sweepGroundY = 0.4f;

    [Header("Sweep Warning Prefab")]
    public GameObject warningPrefabC;           // 扇形预警prefab
    public Transform warningOriginC;            // 可为空，默认transform

    [Header("Animator Params (C)")]
    public string animAttackCTrigger = "AttackC";  // Animator里新增一个Trigger

    // runtime cache
    private WarningSector _warningSector;       // 当前预警实例上的扇形数据（C用）

    [Header("Attack D (Summon Minions)")]
    public bool enableAttackD = true;

    public string animAttackDTrigger = "AttackD";

    [Tooltip("播放AttackD动画后，等多久再生成FX/进入下一步")]
    public float attackD_AnimationDelay = 0.6f;

    [Tooltip("生成FX后，再等多久生成小兵")]
    public float attackD_FXDelay = 0.4f;

    [Tooltip("生成的小兵Prefab（直接拖你的小兵Prefab）")]
    public GameObject minionPrefab;

    [Tooltip("生成时的特效（可选，不要就留空）")]
    public GameObject summonFxPrefab;
    public float summonFXOfset = 0.4f;

    [Tooltip("一次生成几个小兵，按需求默认3")]
    public int minionCount = 3;

    [Tooltip("小兵围绕Boss生成的半径")]
    public float minionSpawnRadius = 2.0f;

    [Tooltip("小兵生成的高度（贴地可用 groundY / 或 0）")]
    public float minionSpawnY = 0.0f;

    [Tooltip("每个小兵死亡时，对Boss造成的伤害（高额）")]
    public int bossDamagePerMinionDeath = 80;

    //20260117
    [Header("Fight Gate")]
    public bool bossFightActive = false;

    [Header("Animator Params (In)")]
    public string animInTrigger = "BossIn";   // 你Animator里 AnyState -> In 的Trigger

    [Header("UI (Boss HP)")]
    public Image hpFillImage;                 // 拖：fantasy_linear_fill 的 Image
    public bool invertFill = false;           // 有些素材方向相反再勾
    public bool hideUIOnDeath = true;         // 可选：死亡自动隐藏

    [Header("Death")]
    public string animDeathTrigger = "TriggerDeath"; // 你Animator里的死亡Trigger名
    public BossManager bossManager;           // 拖 BossManager（或运行时注入）

    [Header("Gun Damage Multipliers")]
    [Tooltip("玩家枪伤基础倍率（1=不变，0.8=减伤，1.2=增伤）")]
    public float gunBaseMultiplier = 1.0f;

    [Range(0f, 1f)]
    [Tooltip("暴击概率（0~1）")]
    public float critChance = 0.15f;

    [Tooltip("暴击倍率（2=双倍伤害）")]
    public float critMultiplier = 2.0f;
    
    // 如果你的枪目前是直接调用 DamageBoss()（而不是 DamageBossByGun），就勾上这个
    [Header("Compatibility")]
    public bool applyGunRulesInsideDamageBoss = true;

    //（可选）最后一次是否暴击，给你Debug或飘字用
    public bool lastHitWasCrit { get; private set; }

    private bool _deathHandled = false;



    [Header("Low HP Drop")]
    [Header("Drop By Damage Step")]
    [Range(0f, 1f)]
    public float dropStep01 = 0.2f;          // x：每掉 x% 触发一次（0~1）

    public bool enableStepDrops = true;

    [Header("Drop Spawn")]
    public GameObject[] dropPrefabs;
    public int dropCountPerTrigger = 2;      // 每次触发掉几个
    public float dropForwardDistance = 4.0f; // 正前方距离
    public float dropDistanceRandom = 1.0f;  // ±1m
    public float dropHeight = 0.6f;          // 生成时抬高
    public bool snapToGround = true;
    public LayerMask groundMask = ~0;

    // runtime
    private float _nextDropHp01 = -1f;        // 下一条分段线（归一化0~1），例如0.8、0.6...



    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();

        // ✅ 这里你原本的 currentHP 初始化有点怪，我建议明确：默认满血
        if (currentHP <= 0) currentHP = maxHP;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        OnHealthChanged?.Invoke(currentHP, maxHP);
        SyncHPUI();
    }
    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }

        // 如果还没有玩家，直接退出（避免 player.root 空引用）
        if (player == null)
        {
            Debug.LogError("[BOSS] Player Transform not found!", this);
            enabled = false;
            return;
        }

        // 只保留这一套“全覆盖”获取
        _playerHealth =
            player.GetComponent<PlayerHealth>() ??
            player.GetComponentInParent<PlayerHealth>() ??
            player.GetComponentInChildren<PlayerHealth>();

        if (_playerHealth == null)
            Debug.LogError("[BOSS] PlayerHealth NOT found on player hierarchy!", this);

        _playerRoot = player.root;

        // 第一轮：冷却结束才出预警
        _nextAttackTime = Time.time + attackCoolDown;
    }


    private void Update()
    {
        if (!bossFightActive) return;
        if (player == null) return;

        // 只有：冷却结束 → 才申请攻击（然后才会出现预警）
        if (!_attackRequested && !_isAttacking && Time.time >= _nextAttackTime)
        {
            BossAttackType next = PickNextAttack();
            RequestAttack(next);
        }

        if (_attackWindowOpen)
        {   
            //Debug.Log("[BOSS] _attackWindowOpen = TRUE (Update is calling DoDamageCheckB)", this);
            DoDamageCheckActive(force: false);
        }

        if (facePlayer) FaceTarget(player.position);
    }

    //20260117
    public void StartBossFight()
    {
        // 开启战斗逻辑
        bossFightActive = true;

        // 清理旧状态（防止重复触发）
        StopAllCoroutines();
        ClearWarning();
        _attackRequested = false;
        _isAttacking = false;
        _attackWindowOpen = false;
        _hasDealtDamageThisAttack = false;
        attackCount = 0;

        facePlayer = true;

        // 冷却从“现在”开始算
        // 因为你已经保证 attackCoolDown >= In 动画长度
        _nextAttackTime = Time.time + attackCoolDown;

        // 播 In（只是视觉/表现，不参与逻辑）
        if (animator != null && !string.IsNullOrEmpty(animInTrigger))
        {
            animator.ResetTrigger(animInTrigger);
            animator.SetTrigger(animInTrigger);
        }

        GetComponent<BossSound>()?.PlayRoar();

        SyncHPUI();
        ResetStepDropState();
    }

    // ===============================
    // Health -> UI
    // ===============================
    private void SyncHPUI()
    {
        if (hpFillImage == null) return;

        float t = (maxHP <= 0) ? 0f : (currentHP / (float)maxHP);
        if (invertFill) t = 1f - t;

        hpFillImage.fillAmount = t;
    }

    private BossAttackType PickNextAttack()
    {
        int step = attackCount % 7;

        switch (step)
        {
            case 0: return BossAttackType.SlamB;   // B
            case 1: return BossAttackType.AoeA;    // A
            case 2: return BossAttackType.SweepC;  // C
            case 3: return BossAttackType.SummonD; // D
            case 4: return BossAttackType.SlamB;   // B
            case 5: return BossAttackType.AoeA;    // A
            default: return BossAttackType.SweepC; // C
        }
    }

    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    // ===============================
    // Damage / Health
    // ===============================
    private bool IsMinionDeathDamage(int dmg)
    {
        return dmg > 0 && dmg == bossDamagePerMinionDeath;
    }

    public bool AcceptHit(Collider hitCol)
    {
        if (hitCol == null) return false;
        if (hitCol.isTrigger) return false;

        if (validHitColliders == null || validHitColliders.Length == 0)
            return true;

        for (int i = 0; i < validHitColliders.Length; i++)
            if (validHitColliders[i] == hitCol) return true;

        return false;
    }

    public void DamageBoss(int damage, Vector3 hitFrom)
    {
        if (invulnerable || IsDead) return;
        if (damage <= 0) return;

        // ① 判定伤害来源
        bool fromMinionDeath = IsMinionDeathDamage(damage);

        int finalDamage = damage;

        // ② 枪伤：在不改其它脚本前提下，兼容“枪直接调用 DamageBoss()”
        if (!fromMinionDeath && applyGunRulesInsideDamageBoss)
        {
            finalDamage = ComputeGunDamage(damage);
        }
        else
        {
            // 小怪死亡伤害：明确不触发暴击
            lastHitWasCrit = false;
        }

        int before = currentHP;
        currentHP = Mathf.Max(0, currentHP - finalDamage);

        if (logDamage)
            Debug.Log($"[Boss] -{finalDamage} HP ({before} -> {currentHP}) " +
                    $"src={(fromMinionDeath ? "MinionDeath" : "Gun/Other")} crit={lastHitWasCrit}", this);

        OnHealthChanged?.Invoke(currentHP, maxHP);
        OnDamaged?.Invoke(finalDamage, hitFrom);

        CheckStepDrops(before, currentHP);
        // 同步UI
        SyncHPUI();

        if (currentHP <= 0)
        {
            HandleDeathOnce();
        }
    }

    private int ComputeGunDamage(int rawDamage)
    {
        float dmg = rawDamage * gunBaseMultiplier;

        lastHitWasCrit = (UnityEngine.Random.value < critChance);
        if (lastHitWasCrit) dmg *= critMultiplier;

        return Mathf.Max(1, Mathf.RoundToInt(dmg));
    }

    private void HandleDeathOnce()
    {
        if (_deathHandled) return;
        _deathHandled = true;

        if (logDamage) Debug.Log("[Boss] DIED", this);

        // Animator 死亡触发
        if (animator != null && !string.IsNullOrEmpty(animDeathTrigger))
        {
            animator.ResetTrigger(animDeathTrigger);
            animator.SetTrigger(animDeathTrigger);
        }

        //★
        GetComponent<BossSound>()?.PlayRoar();

        // 停止战斗逻辑（如果你用了 gate）
        bossFightActive = false;

        // 通知外部
        OnDied?.Invoke();

        // 调 BossManager.Death()
        if (bossManager != null)
            bossManager.Death();

        // 可选：隐藏血条
        if (hideUIOnDeath && hpFillImage != null)
            hpFillImage.transform.root.gameObject.SetActive(false);
    }

    private void ResetStepDropState()
    {
        if (!enableStepDrops || dropStep01 <= 0f)
        {
            _nextDropHp01 = -1f;
            return;
        }

        // 第一条线：1 - step（例如 0.8）
        _nextDropHp01 = 1f - dropStep01;

        // 防止奇怪数值
        if (_nextDropHp01 < 0f) _nextDropHp01 = 0f;
    }

    private void CheckStepDrops(int hpBefore, int hpAfter)
    {
        if (!enableStepDrops) return;
        if (dropStep01 <= 0f) return;
        if (maxHP <= 0) return;

        // 没初始化就初始化（防呆）
        if (_nextDropHp01 < 0f)
            ResetStepDropState();

        float before01 = hpBefore / (float)maxHP;
        float after01  = hpAfter  / (float)maxHP;

        // 如果这次扣血没有让HP下降，直接返回
        if (after01 >= before01) return;

        // 只要 after01 <= 下一条线，就触发，并把线往下挪 step，可能一次跨多条所以用 while
        // 加一个小 epsilon 避免浮点边界抖动
        const float eps = 0.0001f;

        while (_nextDropHp01 >= 0f && after01 <= _nextDropHp01 + eps)
        {
            SpawnDropsOnce();

            _nextDropHp01 -= dropStep01;

            // 最后一条线允许到 0
            if (_nextDropHp01 < 0f) _nextDropHp01 = -1f; // 结束
        }
    }

    private void SpawnDropsOnce()
    {
        if (dropPrefabs == null || dropPrefabs.Length == 0) return;
        if (dropCountPerTrigger <= 0) return;

        for (int i = 0; i < dropCountPerTrigger; i++)
        {
            float d = dropForwardDistance + UnityEngine.Random.Range(-dropDistanceRandom, dropDistanceRandom);

            Vector3 pos = transform.position + transform.forward * d;
            pos.y += dropHeight;

            if (snapToGround)
            {
                Vector3 rayOrigin = pos + Vector3.up * 3f;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
                    pos = hit.point;
            }

            GameObject prefab = dropPrefabs[UnityEngine.Random.Range(0, dropPrefabs.Length)];
            Instantiate(prefab, pos, Quaternion.identity);
        }
    }

    public void ResetHP()
    {
        _deathHandled = false;
        currentHP = maxHP;
        OnHealthChanged?.Invoke(currentHP, maxHP);
        SyncHPUI();
        ResetStepDropState();
    }


    // =========================================================
    // Attack A entry
    // =========================================================
    public void RequestAttackA()
    {
        if (_attackRequested) return;

        _attackRequested = true;
        _currentAttack = BossAttackType.AoeA;

        if (_attackRoutine != null) StopCoroutine(_attackRoutine);
        _attackRoutine = StartCoroutine(AttackA_Flow());
    }

    private IEnumerator AttackA_Flow()
    {
        // ② 立即锁向一次
        if (snapFaceOnWarning && player != null)
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        facePlayer = false;
        _isAttacking = true;

        // ④ 先生成 N 个随机点预警（只生成一次）
        SpawnPoisonPoints();
        if (_pointsRemainingToHit <= 0)
            goto FINISH; // 没生成点就别等了

        // ① 预警先出现 1 秒
        yield return new WaitForSeconds(warningTimeA);

        // ③ 播放 AttackA 动画
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(animIsAttackingBool))
                animator.SetBool(animIsAttackingBool, true);

            animator.ResetTrigger(animAttackATrigger);
            animator.SetTrigger(animAttackATrigger);

            GetComponent<BossSound>()?.PlayAOE();       //声音
        }

        // 动画开始的同一刻：通知所有点开始计时（AnimateDelay + 随机延迟）
        for (int i = 0; i < _activePoisonPoints.Count; i++)
        {
            if (_activePoisonPoints[i] != null)
                _activePoisonPoints[i].BeginStrike(animateDelayA);
        }

        // 等待最后一个点结束 -> 才进入冷却
        while (_pointsRemainingToHit > 0)
            yield return null;

    FINISH:
        facePlayer = true;

        if (animator != null && !string.IsNullOrEmpty(animIsAttackingBool))
            animator.SetBool(animIsAttackingBool, false);

        _isAttacking = false;

        // 冷却从最后一个点结束后开始
        _nextAttackTime = Time.time + attackCoolDown;
        _attackRequested = false;

        attackCount++;
    }


    private void SpawnPoisonPoints()
    {
        _lastSpawnPoints.Clear();
        _activePoisonPoints.Clear();

        if (poisonPointPrefab == null)
        {
            Debug.LogError("[Boss] poisonPointPrefab is NULL.", this);
            _pointsRemainingToHit = 0;
            return;
        }

        int created = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            if (!TryPickPoint(out Vector3 worldPos)) continue;

            _lastSpawnPoints.Add(worldPos);

            GameObject go = Instantiate(poisonPointPrefab, worldPos, Quaternion.identity);

            PoisonPoint pp = go.GetComponent<PoisonPoint>();
            if (pp == null)
            {
                Debug.LogError("[Boss] poisonPointPrefab must have PoisonPoint script.", go);
                Destroy(go);
                continue;
            }

            float extraDelay = (perPointRandomDelayMax > 0f)
                ? UnityEngine.Random.Range(0f, perPointRandomDelayMax)
                : 0f;

            pp.Setup(
                owner: this,
                forcedGroundY: groundY,
                extraRandomDelay: extraDelay,
                hitRadius: hitRadiusA,
                damage: damageA,
                mask: playerMask,
                windowTime: windowTimeA,
                hitInterval: hitTickA,
                lifeAfterWindow: lifeAfterWindowA
            );

            _activePoisonPoints.Add(pp);
            created++;
        }

        _pointsRemainingToHit = created;

        if (created == 0)
            Debug.LogWarning("[Boss] SpawnPoisonPoints created 0 points (area too small / spacing too big?).", this);
    }


    private bool TryPickPoint(out Vector3 worldPos)
    {
        // 本地矩形：X=宽，Z=长
        float halfW = Mathf.Max(0.1f, areaWidth * 0.5f);
        float halfL = Mathf.Max(0.1f, areaLength * 0.5f);

        for (int t = 0; t < triesPerPoint; t++)
        {
            float lx = UnityEngine.Random.Range(-halfW, halfW);
            float lz = UnityEngine.Random.Range(-halfL, halfL);

            Vector3 local = new Vector3(lx, 0f, lz);
            Vector3 w = transform.TransformPoint(local);
            w.y = groundY;

            // 最小间距检查（平方距离）
            bool ok = true;
            float minSqr = minSpacing * minSpacing;
            for (int i = 0; i < _lastSpawnPoints.Count; i++)
            {
                Vector3 p = _lastSpawnPoints[i];
                Vector2 a = new Vector2(p.x, p.z);
                Vector2 b = new Vector2(w.x, w.z);
                if ((a - b).sqrMagnitude < minSqr)
                {
                    ok = false;
                    break;
                }
            }

            if (!ok) continue;

            worldPos = w;
            return true;
        }

        worldPos = default;
        return false;
    }

    // PoisonPoint 在“命中完成”时调用
    public void NotifyPoisonPointFinished(PoisonPoint point)
    {
        if (_pointsRemainingToHit > 0)
            _pointsRemainingToHit--;
    }

    // ===============================
    // Attack Flow: Request -> Warning -> Wait Reaction -> Trigger Anim
    // ===============================
    // [ADD] 通用申请攻击
    public void RequestAttack(BossAttackType type)
    {
        if (_attackRequested) return;

        // --- 关键：A 走独立流程 ---
        if (type == BossAttackType.AoeA)
        {
            _attackRequested = true;
            _currentAttack = BossAttackType.AoeA;

            if (_attackRoutine != null) StopCoroutine(_attackRoutine);
            _attackRoutine = StartCoroutine(AttackA_Flow());
            return;
        }

        if (type == BossAttackType.SummonD)
        {
            if (_isAttacking) return;         // 防止外部误调用叠加
            _attackRequested = true;
            _currentAttack = type;

            // 播放 AttackD 动画
            if (animator != null)
            {
                animator.ResetTrigger(animAttackDTrigger);
                animator.SetTrigger(animAttackDTrigger);
            }

            GetComponent<BossSound>()?.PlaySummon();

            if (_attackRoutine != null) StopCoroutine(_attackRoutine);
            _attackRoutine = StartCoroutine(AttackD_Flow());
            return;
        }

        _attackRequested = true;
        _currentAttack = type;

        if (_attackRoutine != null) StopCoroutine(_attackRoutine);
        _attackRoutine = StartCoroutine(Attack_Flow(type));
    }

    // [CHANGE] 原来的 RequestAttackB() 变成调用通用
    public void RequestAttackB()
    {
        RequestAttack(BossAttackType.SlamB);
    }

    // [ADD] 通用Flow
    private IEnumerator Attack_Flow(BossAttackType type)
    {
        // 申请攻击 → 立刻锁定朝向一次（你原本的逻辑 그대로）
        if (snapFaceOnWarning && player != null)
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        // 锁定后：暂停追踪（预警期间不再转）
        facePlayer = false;

        // 出预警
        SpawnWarning(type);

        // 反应时间
        yield return new WaitForSeconds(reactionTime);

        // 发起攻击（触发动画）
        _isAttacking = true;

        if (animator != null)
        {
            if (!string.IsNullOrEmpty(animIsAttackingBool))
                animator.SetBool(animIsAttackingBool, true);

            if (type == BossAttackType.SlamB)
            {
                animator.ResetTrigger(animAttackBTrigger);
                animator.SetTrigger(animAttackBTrigger);
            }
            else // SweepC
            {
                animator.ResetTrigger(animAttackCTrigger);
                animator.SetTrigger(animAttackCTrigger);
            }
        }

        // 注意：清预警、恢复、冷却——全部交给 AttackSMB 的回调（你原本就是这么做的 :contentReference[oaicite:5]{index=5}）
    }

    private IEnumerator AttackD_Flow()
    {
        // ② 立即锁向一次（沿用你已有逻辑）
        if (snapFaceOnWarning && player != null)
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        // 施法期间不追踪
        facePlayer = false;

        // 标记正在攻击
        _isAttacking = true;

        // 播放动画（Attack_Flow 已经 set trigger 了也没关系；你也可以只在这里 set trigger）
        // 这里不重复 set trigger 也可以，保持最小改动：不写也行

        // ① AnimationDelay
        yield return new WaitForSeconds(attackD_AnimationDelay);

        // ② 生成特效
        if (summonFxPrefab != null)
        {   
            Vector3 FXposition = transform.position + new Vector3(0f, summonFXOfset, 0f);
            Instantiate(summonFxPrefab, FXposition , Quaternion.identity);
        }

        // ③ FXDelay
        yield return new WaitForSeconds(attackD_FXDelay);

        // ④ 生成小兵
        SpawnMinionsD();

        // 攻击结束，进入冷却（不等小兵死）
        _isAttacking = false;
        facePlayer = true;

        _nextAttackTime = Time.time + attackCoolDownD;
        _attackRequested = false;

        attackCount++;
    }

    private void SpawnMinionsD()
    {
        if (minionPrefab == null) return;

        for (int i = 0; i < minionCount; i++)
        {
            // 均匀分布在圆周上（比完全随机更稳定、不卡在一起）
            float t = (minionCount <= 1) ? 0f : (i / (float)minionCount) * 360f;
            Vector3 dir = Quaternion.Euler(0f, t, 0f) * Vector3.forward;

            Vector3 pos = transform.position + dir * minionSpawnRadius;
            pos.y = minionSpawnY;

            GameObject m = Instantiate(minionPrefab, pos, Quaternion.identity);

            // 给小兵挂“死亡回调Boss扣血”脚本
            var relay = m.GetComponent<BossMinionDeathRelay>();
            if (relay == null) relay = m.AddComponent<BossMinionDeathRelay>();
            relay.Bind(this, bossDamagePerMinionDeath);
        }
    }

    // [ADD] 根据招式出预警
    private void SpawnWarning(BossAttackType type)
    {
        if (_warningInstance != null) return;

        if (type == BossAttackType.SlamB)
        {
            if (warningPrefab == null) return;

            Transform origin = (warningOrigin != null) ? warningOrigin : transform;
            Vector3 pos = origin.position + transform.forward * slamOffset;
            pos.y = groundY;

            Quaternion rot = Quaternion.LookRotation(transform.forward, Vector3.up);
            _warningInstance = Instantiate(warningPrefab, pos, rot);

            // B：从“新实例”上抓 WarningArea（你现在就是这么修好的 :contentReference[oaicite:6]{index=6}）
            _warningArea = _warningInstance.GetComponentInChildren<WarningArea>();
            _warningSector = null;

            if (_warningArea == null)
                Debug.LogError("[BOSS] WarningPrefab(B) missing WarningArea component.", _warningInstance);
        }
        else
        {
            if (warningPrefabC == null) return;

            Transform origin = (warningOriginC != null) ? warningOriginC : transform;
            Vector3 pos = origin.position + transform.forward * sweepOffset;
            pos.y = sweepGroundY;

            Quaternion rot = Quaternion.LookRotation(transform.forward, Vector3.up);
            _warningInstance = Instantiate(warningPrefabC, pos, rot);

            // C：抓 WarningSector
            _warningSector = _warningInstance.GetComponentInChildren<WarningSector>();
            _warningArea = null;

            if (_warningSector == null)
                Debug.LogError("[BOSS] WarningPrefab(C) missing WarningSector component.", _warningInstance);
        }
    }

    private void ClearWarning()
    {
        if (_warningInstance != null)
        {
            Destroy(_warningInstance);
            _warningInstance = null;
        }
        _warningArea = null;
        _warningSector = null;
    }


    // ===============================
    // Called by AttackSMB (挂在AttackB动画状态上)
    // ===============================
    public void Anim_AttackWindowOpen()
    {
        _attackWindowOpen = true;
        _hasDealtDamageThisAttack = false;
        _nextWindowHitTime = 0f;
    }

    public void Anim_AttackHit()
    {
        // 如果你希望“锤子落地那一刻必定判定一次”，可以在这里强制判一次：
        DoDamageCheckActive(force: true);
    }

    public void Anim_AttackWindowClose()
    {
        _attackWindowOpen = false;

        // 你之前的需求：窗口关闭时清预警
        ClearWarning();
    }

    public void Anim_AttackEnd()
    {
        _isAttacking = false;

        if (animator != null && !string.IsNullOrEmpty(animIsAttackingBool))
            animator.SetBool(animIsAttackingBool, false);

        facePlayer = true;

        // cooldown 从攻击结束开始计时（你现有逻辑 :contentReference[oaicite:11]{index=11}）
        _nextAttackTime = Time.time + attackCoolDown;

        _attackRequested = false;

        attackCount++;
    }

    private void DoDamageCheckActive(bool force)
    {
        if (_currentAttack == BossAttackType.SlamB) DoDamageCheckB(force);
        else DoDamageCheckSweep(force);
    }

    void DoDamageCheckB(bool force)
    {
        if (!force)
        {
            if (Time.time < _nextWindowHitTime) return;
            _nextWindowHitTime = Time.time + windowHitInterval;
        }

        if (damageOncePerAttack && _hasDealtDamageThisAttack) return;

        // 必须有预警实例，才能做到“范围=预警范围”
        if (_warningInstance == null) return;

        Vector3 center;
        Vector3 halfExtents;
        Quaternion rot;

        if (_warningArea != null)
        {
            _warningArea.GetWorldBox(out center, out halfExtents, out rot);
        }
        else
        {
            // 兜底（不推荐长期用）
            center = _warningInstance.transform.position;
            rot = _warningInstance.transform.rotation;
            halfExtents = new Vector3(1f, 1f, 2f);
        }

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rot, playerMask, QueryTriggerInteraction.Ignore);
        Debug.Log($"[BOSS] OverlapBox hits = {hits.Length}, mask={playerMask.value}", this);
        if (hits == null || hits.Length == 0) return;

        for (int i = 0; i < hits.Length; i++)
        {
            // 命中到任何属于玩家层级的 collider 都算
            Transform hitRoot = hits[i].transform.root;
            if (_playerRoot != null && hitRoot != _playerRoot) continue;

            if (_playerHealth == null)
            {
                Debug.LogError("[BOSS] _playerHealth is NULL, cannot deal damage.", this);
                return;
            }

            _playerHealth.TakeDamage(damageB, transform.position);
            _hasDealtDamageThisAttack = true;

            // Debug：确认真的调用到了
            Debug.Log($"[BOSS] Damage applied: {damageB}, PlayerHP now={_playerHealth.hp}", this);
            break;

        }
    }

    private void DoDamageCheckSweep(bool force)
    {
        if (!force)
        {
            if (Time.time < _nextWindowHitTime) return;
            _nextWindowHitTime = Time.time + windowHitInterval;
        }

        if (damageOncePerAttack && _hasDealtDamageThisAttack) return;

        if (_warningInstance == null || _warningSector == null) return;

        _warningSector.GetWorldSector(out Vector3 center, out Vector3 forward, out float radius, out float angle, out float height);

        // 1) 先用球找候选
        Collider[] hits = Physics.OverlapSphere(center, radius, playerMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return;

        float halfAngle = angle * 0.5f;
        float halfH = height * 0.5f;

        for (int i = 0; i < hits.Length; i++)
        {
            // 2) 用 bounds.center 做判断（更稳）
            Vector3 p = hits[i].bounds.center;

            // 高度过滤（可选，但建议有，避免楼上楼下穿模）
            float dy = Mathf.Abs(p.y - center.y);
            if (dy > halfH) continue;

            Vector3 toP = p - center;
            toP.y = 0f;

            if (toP.sqrMagnitude < 0.0001f) continue;

            float a = Vector3.Angle(forward, toP.normalized);
            if (a > halfAngle) continue;

            // 3) 命中：扣血
            var hp = hits[i].GetComponentInParent<PlayerHealth>();
            if (hp == null) continue;

            hp.TakeDamage(damageC, transform.position);
            _hasDealtDamageThisAttack = true;

            if (logDamage)
                Debug.Log($"[BOSS] SweepC damage applied: {damageC}, PlayerHP now={hp.hp}", this);

            break;
        }
    }


    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
         // ===== A: 画矩形范围（跟随Boss朝向） =====
        float halfW = Mathf.Max(0.1f, areaWidth * 0.5f);
        float halfL = Mathf.Max(0.1f, areaLength * 0.5f);

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.9f);
        Gizmos.DrawWireCube(new Vector3(0f, groundY - transform.position.y, 0f), new Vector3(halfW * 2f, 0.05f, halfL * 2f));

        // 画最近一次生成的落点
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 1f);
        for (int i = 0; i < _lastSpawnPoints.Count; i++)
        Gizmos.DrawWireSphere(_lastSpawnPoints[i], 0.25f);
 
        
        if (_warningInstance == null) return;

        // --- C: Sector gizmo ---
        WarningSector sector = _warningInstance.GetComponentInChildren<WarningSector>();
        if (sector != null)
        {
            sector.GetWorldSector(out Vector3 c, out Vector3 fwd, out float r, out float ang, out float h);

            Gizmos.color = new Color(1f, 0.4f, 0.1f, 1f);
            Gizmos.DrawWireSphere(c, 0.08f);

            // 画两条边界射线 + 一个扇形弧（用分段线近似）
            float half = ang * 0.5f;
            Vector3 leftDir = Quaternion.AngleAxis(-half, Vector3.up) * fwd;
            Vector3 rightDir = Quaternion.AngleAxis(half, Vector3.up) * fwd;

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
            Gizmos.DrawLine(c, c + leftDir * r);
            Gizmos.DrawLine(c, c + rightDir * r);

            int seg = 24;
            Vector3 prev = c + leftDir * r;
            for (int i = 1; i <= seg; i++)
            {
                float t = (float)i / seg;
                float a = Mathf.Lerp(-half, half, t);
                Vector3 dir = Quaternion.AngleAxis(a, Vector3.up) * fwd;
                Vector3 p = c + dir * r;
                Gizmos.DrawLine(prev, p);
                prev = p;
            }

            // 高度范围（简单画两条上下线提示）
            Gizmos.color = new Color(1f, 1f, 0.2f, 1f);
            Gizmos.DrawLine(c + Vector3.up * (h * 0.5f), c - Vector3.up * (h * 0.5f));

            return;
        }

        // --- B: Box gizmo ---
        WarningArea area = _warningInstance.GetComponentInChildren<WarningArea>();
        if (area == null) return;

        area.GetWorldBox(out Vector3 center, out Vector3 halfExtents, out Quaternion rot);

        Gizmos.matrix = Matrix4x4.TRS(center, rot, Vector3.one);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);

        Gizmos.color = new Color(1f, 1f, 0.2f, 1f);
        Gizmos.DrawSphere(Vector3.zero, 0.08f);

        Gizmos.color = new Color(0.2f, 1f, 0.2f, 1f);
        Gizmos.DrawLine(Vector3.zero, Vector3.forward * Mathf.Max(0.2f, halfExtents.z));
    }
    #endif


}
