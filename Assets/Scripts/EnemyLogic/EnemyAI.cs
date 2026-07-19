using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
[System.Flags]  public enum WeightDebugMask
{   None = 0, Base = 1 << 0, Separation = 1 << 1, Backtrack  = 1 << 2, 
    Individual = 1 << 3, Global = 1 << 4, Player = 1 << 5, All = ~0 }

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum AIState { Patrol, Catch, Search }
    enum ReactionState  { None, HitStagger, Stunned, Dead}
    public AIState state = AIState.Patrol;
    private ReactionState reaction = ReactionState.None;
    public int ReactionValue => (int)reaction;
    [SerializeField] private EnemyBehavior behavior;

    public bool isBlind;

    [Header("Locked Mode (Boss Minion)")]
    public bool locked = false;   // 外部可直接控制（或用方法封装）
    public float lockedViewDistance = 999f;     // locked时强制远距离可见/追击
    public bool lockedIgnoreLineOfSight = true; // true=无视遮挡，false=仍用CanSeePlayer射线检测

    private Transform player;
    private Vector3 lastKnownPos;
    private float lostTimer;

    private AIState _lastLoggedState = (AIState)(-1);

    public bool IsDead => reaction == ReactionState.Dead;

    [Header("Speed")]
    public float catchSpeed = 6f;      // 追击速度
    public float defaultSpeed = 4f;        // 记住Inspector原始速度

    [Header("Patrol Graph")]
    public PatrolNode startNode;
    [Header("Zone Filter")]
    public ZoneTag readableZones = ZoneTag.None; 
   
    [Header("Routing Penalty System")]
    public float selfPenValue = 0.5f;           //每次访问做出的惩罚，不变
    public float selfPenDecay = 0.1f;           //下一次访问别的节点，已造成惩罚的恢复速度，不变
    [Range(0.01f, 1f)]
    private Dictionary<PatrolNode, float> individualHeat = new Dictionary<PatrolNode, float>();
    [Range(0.01f, 1f)]
    public float backtrackPenalty = 0.3f;       //每次访问，为防止回头做出的一次性惩罚，内部控制，需要传入玩家状态；
    [Range(0f, 2f)]
    public float playerHeatSensitivity = 1.0f;  //敌人对玩家的敏感度，需要接口；

    [Header("Arrival Check")]
    public float arriveEpsilon = 0.1f;
    public float minSpeedToConsiderStopped = 0.05f;
    private NavMeshAgent agent;
    private PatrolNode currentTarget;   // 当前目标点
    private PatrolNode previousNode;    // 上一个点（用于回头惩罚）

    [Header("Patrol - Inspect")]
    public float inspectAngleMin = 30f;
    public float inspectAngleMax = 110f;
    public float inspectTurnSpeed = 120f; // degrees per second
    public float inspectPause = 0.1f;
    private bool isInspecting = false;
    public LayerMask patrolNodeMask;   // Inspector 勾选 PatrolNode layer
    public float nearestNodeRadius = 20f;


    [Header("Catch - Vision")]
    public Transform eyePoint;
    public float viewDistance = 15f;
    public float baseViewDistance  = 15f;
    public float crouchViewDistance = 6f;

    [Range(1f, 179f)] public float viewAngle = 120f;
    public LayerMask visionBlockMask;

    [Header("Catch - Lose Sight")]
    public float loseSightGrace = 2f;    // 丢失视野缓冲（秒）
    public float trackAfterLost = 1.0f;    // 丢失后继续追“真实玩家位置”的记忆窗口
    private float trackTimer;             // 运行时计时
    public float reacquireConfirm = 0.15f; // 连续看见多久才算重新发现
    private float seeTimer;
    public bool canSee = false;   //重要

    [Header("Catch - Attack")]
    public float attackRange = 3f;
    public float attackCooldown = 3f;
    private float attackTimer;

    [Header("Catch - Close Slowdown")]
    [SerializeField] private float slowDownDistance = 2.8f;  // 开始慢走贴脸的距离
    [SerializeField] private float closeSpeed = 1.5f;         // 贴脸慢走速度（应 < catchSpeed）

    [Header("Catch - Movement")]
    public float catchRepathInterval = 0.2f;
    private float repathTimer;

    [Header("MultiRay Offsets (player size ~ 1,2,1)")]
    public float sideOffset = 0.32f;   // 左右肩
    public float upOffset   = 0.60f;   // 胸/头
    public float downOffset = 0.20f;   // 轻微下偏（不要大）

    public EnemySensor EnemySensor;

    [Header("Search")]
    [SerializeField] private float searchConeAngle = 70f;
    [SerializeField] private float searchMinRadius = 1.5f;
    [SerializeField] private float searchMaxRadius = 10f;
    [SerializeField] private int searchRings = 1;
    [SerializeField] private int searchPointsPerRing = 5;
    [SerializeField] private float navmeshSampleRadius = 2.0f;
    [SerializeField] private float searchDuration = 15f;
    [SerializeField] private float searchMaxStep = 8f; // 例：8米内算“踱步”步长

    private float searchTimer;
    private Vector3 searchDirHint = Vector3.forward;

    private readonly List<Vector3> searchPoints = new();

    private Vector3 searchCurrentPoint;
    private bool searchInspecting = false;    // Search 状态下是否在转头检查
    private Coroutine patrolInspectCR;
    private Coroutine searchInspectCR;

    [System.Serializable]
    public struct WeightBreakdown
    {
        public float finalW,prob;
        public float backPen,selfPen,playerTrack;
        public float closePen,globalPen;
    }
    private readonly Dictionary<PatrolNode, WeightBreakdown> lastWeights
    = new Dictionary<PatrolNode, WeightBreakdown>();

    public bool TryGetLastWeight(PatrolNode node, out WeightBreakdown info)
    => lastWeights.TryGetValue(node, out info);

    //======================== HIT STAGGER (Tuning) ========================
    [Header("Hit Reaction - Stagger")]
    [SerializeField] public float hitStaggerDuration = 0.2f;
    [SerializeField] public float hitStaggerSpeed = 3.0f;
    private Vector3 staggerDir;
    private float staggerTimer;
    private float staggerSpeed;
    private float stunTimer;

    [Header("Forced Catch (On Hit/Stun)")]
    [SerializeField] private float forcedCatchDuration = 10f;
    private bool pendingForcedCatch = false;
    private Vector3 pendingForcedPos;
    private float forcedCatchTimer = 0f;
    private Vector3 forcedCatchPos;

    [Header("Hearing / Gunshot Investigation")]
    [SerializeField] float hearingRadius = 45f;

    [SerializeField] float gunshotNearDist = 10f;      // <=10m 偏差小
    [SerializeField] float gunshotFarDist  = 40f;      // >=40m 偏差大
    [SerializeField] float gunshotMaxRadiusMul = 2.2f; // 最远时半径倍率

    // ===== Debug  =====
    [Header("Debug Vision")]
    public bool debugDrawVision = true;
    public bool debugDrawFOV = true;
    public bool debugDrawRays = true;

    [Header("Debug Weight Pipeline")]
    public WeightDebugMask debugWeightMask = WeightDebugMask.All;

    // 最近一次检测缓存（用于Gizmos显示）
    private bool _dbgLastCanSee;
    private Vector3 _dbgLastOrigin;
    private Vector3 _dbgLastPlayerCenter;
    private Vector3[] _dbgLastTargets = new Vector3[5];
    private int _dbgLastPassRayIndex = -1; // 哪一条射线“通过”（无遮挡）
    private float _dbgLastAngle;
    private bool _dbgLastInFOV;
    
    [Header("Debug Weights")]
    public bool debugShowWeightedOnNodes = false; // 只对这个敌人启用
    private readonly List<PatrolNode> debugTouchedNodes = new List<PatrolNode>();

    public static EnemyAI DebugSource; // 当前用于调试显示的敌人

    void OnEnable()
    {
        DebugSource = this;
        simpleGun.OnGunshot += HearGunshot;
        FirstPersonController.OnPlayerStatusChange += playerStatus;
    }

    void OnDisable()
    {
        simpleGun.OnGunshot += HearGunshot;
        FirstPersonController.OnPlayerStatusChange -= playerStatus;
    }


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        behavior = GetComponent<EnemyBehavior>();

        if (!locked)
        {
            if (startNode == null)
            {
                Debug.LogError($"missing startNode!", this);
                enabled = false;
                return;
            }
            currentTarget = startNode;
            previousNode = null;
            SetDestination(currentTarget.Position);
        }
        else
        {
            // locked：不需要巡逻起点
            state = AIState.Catch;
            //hasSeenPlayer = true;
            isBlind = false;
            viewDistance = lockedViewDistance;
        }


        player = FirstPersonController.instance != null ? FirstPersonController.instance.transform : null;
        if (player == null)
        {
            Debug.LogError($"{name}: PlayerController.instance missing.");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (reaction == ReactionState.Dead) return;

        if (reaction != ReactionState.None) 
        {
            UpdateReaction(); 
            return;
        }

        Vector3 playerPos = default;

        // ===== LOCKED MODE =====
        if (locked)
        {
            // locked：永远可见（超远距离），永远保持Catch，不跑Patrol/Search，不需要startNode
            isBlind = false;
            viewDistance = lockedViewDistance;

            Vector3 ppos = (player != null) ? player.position : transform.position;

            bool lockedCanSee = true;
            if (!lockedIgnoreLineOfSight)
                lockedCanSee = CanSeePlayer(out ppos);

            canSee = lockedCanSee;

            if (state != AIState.Catch) state = AIState.Catch;

            UpdateCatch(lockedCanSee, ppos);
            return;
        }

        canSee = CanSeePlayer(out playerPos);

        if (behavior != null && behavior.IsShoutPausing) return;

        if (!behavior) behavior = GetComponent<EnemyBehavior>();

        // 任何状态下：看到玩家（连续达到门槛） → 进入 Catch
        if (reaction == ReactionState.None && state != AIState.Catch)
        {
            if (canSee) seeTimer += Time.deltaTime;
            else        seeTimer = 0f;

            if (seeTimer >= reacquireConfirm)
            {
                EnterCatch(playerPos);
                seeTimer = 0f; // 避免重复触发
            }
        }
        else
        {
            // 进入 Catch 或有反应状态时，不累积“重获视野”计时
            seeTimer = 0f;
        }

        if (state != _lastLoggedState)
        {
            Debug.Log($"{name} STATE -> {state}");
            _lastLoggedState = state;
        }

        switch (state)
        {
        case AIState.Patrol:    UpdatePatrol(); break;
        case AIState.Catch:     UpdateCatch(canSee, playerPos); break;
        case AIState.Search:    UpdateSearch(canSee, playerPos); break;
        }
    }

    //-----------------------Patrol Logic---------------------
    void UpdatePatrol()
    {
        if (currentTarget == null) // 如果当前目标丢了
        {
            currentTarget = (previousNode == null) ? startNode : previousNode;  //回到上个节点，或者回到起点
            SetDestination(currentTarget.Position);
            previousNode = null;
            return;
        }

        // 有目标点：向它移动（NavMeshAgent 已经在移动）
        // 到达目标点后：计算权重惩罚，选择下一个目标
        if (!isInspecting && ReachedDestination() && patrolInspectCR == null)
        {
            patrolInspectCR = StartCoroutine(InspectAndContinue());
        }
    }
    PatrolNode PickNextNode(PatrolNode from, PatrolNode prev)
    {
        if (debugShowWeightedOnNodes)
        {
            for (int j = 0; j < debugTouchedNodes.Count; j++)
                if (debugTouchedNodes[j] != null)
                    debugTouchedNodes[j].debugWeightedW = -1f;
            debugTouchedNodes.Clear();
        }

        // 每次抉择前清空上次记录（只保留“本次决策”的结果，避免旧数据误导）
        lastWeights.Clear();

        var neighbors = from.neighbors;
        if (neighbors == null || neighbors.Count == 0)
            return prev != null ? prev : from;

        float total = 0f;
        float[] weights = new float[neighbors.Count];

        // -------- Pass 1: compute weights + store breakdown (without prob yet) --------
        for (int i = 0; i < neighbors.Count; i++)
        {
            PatrolNode n = neighbors[i];
            if (n == null) { weights[i] = 0f; continue; }

            if (!IsNodeReadable(n)) { weights[i] = 0f; continue; }

            float baseW = Mathf.Max(0.0001f, n.baseWeight);

            float closePen = 1f;      // Separation
            float backPen = 1f;       // Backtrack
            float selfPen = 1f;       // Individual heat
            float globalPen = 1f;     // Global heat
            float playerTrack = 1f;   // Player heat attract

            // 3) Backtrack
            if ((debugWeightMask & WeightDebugMask.Backtrack) != 0)
            {
                if (prev != null && n == prev)
                    backPen = backtrackPenalty;
            }

            // 4) Individual heat
            if ((debugWeightMask & WeightDebugMask.Individual) != 0)
            {
                if (individualHeat.TryGetValue(n, out float sp))
                    selfPen = sp;   // sp ∈ [0.5, 1.0]
            }

            // 5) Global heat (traffic)
            if ((debugWeightMask & WeightDebugMask.Global) != 0)
            {
                if (GlobalHeatManager.Instance != null)
                    globalPen = GlobalHeatManager.Instance.GetHeat(n); // ∈ [0,1]
            }

            // 6) Player heat attract
            if ((debugWeightMask & WeightDebugMask.Player) != 0)
            {
                if (GlobalHeatManager.Instance != null)
                {
                    float ph = GlobalHeatManager.Instance.GetPlayerHeat(n);
                    playerTrack = 1f + ph * playerHeatSensitivity;
                }
            }

            float w = baseW * closePen * backPen * selfPen * globalPen * playerTrack;
            w = Mathf.Max(0.0001f, w);

            weights[i] = w;
            total += w;

            // store breakdown for external usage
            lastWeights[n] = new WeightBreakdown
            {
                finalW = w,
                prob = 0f, // pass2 fill
                backPen = backPen,
                selfPen = selfPen,
                playerTrack = playerTrack,
                closePen = closePen,
                globalPen = globalPen
            };

            if (debugShowWeightedOnNodes)
            {
                n.debugWeightedW = w;
                debugTouchedNodes.Add(n);
            }
        }

        // 如果全是 0（理论上不会），退化成选第一个非空
        if (total <= 0.0001f)
        {
            for (int i = 0; i < neighbors.Count; i++)
                if (neighbors[i] != null) return neighbors[i];
            return prev != null ? prev : from;
        }

        // -------- Pass 2: compute normalized probabilities --------
        for (int i = 0; i < neighbors.Count; i++)
        {
            PatrolNode n = neighbors[i];
            if (n == null) continue;

            if (lastWeights.TryGetValue(n, out var info))
            {
                info.prob = info.finalW / total;
                lastWeights[n] = info;
            }
        }

        // -------- Sampling --------
        float roll = Random.value * total;
        float acc = 0f;
        for (int i = 0; i < neighbors.Count; i++)
        {
            acc += weights[i];
            if (roll <= acc)
                return neighbors[i];
        }

        return neighbors[neighbors.Count - 1];
    }

    bool AgentReady()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    void SetDestination(Vector3 pos)
    {
        if (reaction != ReactionState.None) return;   // Reaction 期间任何导航都禁止
        if (!AgentReady()) return;

        agent.isStopped = false;
        agent.SetDestination(pos);
    }

    bool ReachedDestination()
    {
        if (agent.pathPending) return false;

        // remainingDistance 有时会跳到 0，但还在滑行，所以要结合速度
        if (agent.remainingDistance > agent.stoppingDistance + arriveEpsilon)
            return false;

        return agent.velocity.sqrMagnitude <= (minSpeedToConsiderStopped * minSpeedToConsiderStopped);
    }

    IEnumerator InspectAndContinue()
    {
        if (reaction != ReactionState.None) { patrolInspectCR = null; yield break; }
        isInspecting = true;

        // 1. 停止 NavMesh 移动
        agent.isStopped = true;
        // 记录初始朝向
        Quaternion startRot = transform.rotation;

        // 2. 左转,暂停，回正,右转 180°,回到初始朝向
        yield return RotateByAngle(Random.Range(inspectAngleMin, inspectAngleMax));
        yield return new WaitForSeconds(inspectPause);
        yield return RotateToRotation(startRot);
        yield return RotateByAngle(-Random.Range(inspectAngleMin, inspectAngleMax));
        yield return new WaitForSeconds(inspectPause);

        // ===== 巡逻逻辑继续 =====
        CoolDownIndividualHeat();
        RegisterVisit(currentTarget);
        PatrolNode next = PickNextNode(currentTarget, previousNode);
        previousNode = currentTarget;
        currentTarget = next;
        patrolInspectCR = null;
        SetDestination(currentTarget.Position);
        isInspecting = false;
    }

    IEnumerator RotateByAngle(float angle)
    {
        float rotated = 0f;
        float dir = Mathf.Sign(angle);

        while (Mathf.Abs(rotated) < Mathf.Abs(angle))
        {
            float delta = inspectTurnSpeed * Time.deltaTime * dir;
            transform.Rotate(0f, delta, 0f);
            rotated += delta;
            yield return null;
        }
    }

    IEnumerator RotateToRotation(Quaternion target)
    {
        while (Quaternion.Angle(transform.rotation, target) > 0.5f)
        {
            transform.rotation = Quaternion.RotateTowards(
            transform.rotation, target, inspectTurnSpeed * Time.deltaTime );
            yield return null;
        }
        transform.rotation = target;
    }

    //----------------------- Node Logic----------------------------
    private bool IsNodeReadable(PatrolNode node)
    {
        if (node == null) return false;
        if (readableZones == ZoneTag.None) return true; // 不限制

        // 只要节点 zones 与 readableZones 有交集，就可读
        return (node.zones & readableZones) != 0;
    }

    void CoolDownIndividualHeat() //加了deltatime会让他随时间变化
    {
        if (individualHeat.Count == 0) return;

        var keys = new List<PatrolNode>(individualHeat.Keys);
        foreach (var k in keys)
        {
            float v = individualHeat[k] + selfPenDecay;   // 每次调用恢复 0.1
            if (v >= 1f)
                individualHeat.Remove(k);         // 恢复完成，移除记录
            else
                individualHeat[k] = v;            // 更新 selfPen
        }
    }

    void RegisterVisit(PatrolNode node)
    {
        if (node == null) return;

        // 个体热度
        if (!individualHeat.ContainsKey(node))
            individualHeat[node] = 0f;
        individualHeat[node] = selfPenValue;
        // 全局热度
        GlobalHeatManager.Instance?.AddTrafficHeat(node, 1f);
    }

    public PatrolNode DebugPickNextNode(PatrolNode current, PatrolNode previous)
    {
        return PickNextNode(current, previous);
    }

    PatrolNode FindNearestNodeByOverlap()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, nearestNodeRadius, patrolNodeMask, QueryTriggerInteraction.Collide);
        PatrolNode best = null;
        float bestSqr = float.PositiveInfinity;

        Vector3 pos = transform.position;
        for (int i = 0; i < cols.Length; i++)
        {
            var n = cols[i].GetComponentInParent<PatrolNode>();
            if (n == null) continue;
            if (!IsNodeReadable(n)) continue;

            float d = (n.Position - pos).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                best = n;
            }
        }
        return best;
    }

    //-------------------------Catch Mode-----------------------
    void EnterCatch(Vector3 seenPlayerPos)
    {
        // 切状态前先清旧状态残留（避免 SearchInspect/PatrolInspect 继续影响 agent / rotation）
        StopAllAIcoroutines();  // already exists at bottom

        Debug.Log("Start Caching. Moving towards Player");

        state = AIState.Catch;
        agent.speed = catchSpeed;
        
        lastKnownPos = seenPlayerPos;
        
        trackTimer = trackAfterLost;
        lostTimer = 0f;
        seeTimer = 0f;          // ✅ 防止“重获视野计时”残留造成反复触发
        searchTimer = 0f;       // ✅ 退出 Search 后别再留着计时
        attackTimer = 0f;
        repathTimer = 0f;

        agent.isStopped = false;
        agent.ResetPath();
    }

    void UpdateCatch(bool canSee, Vector3 playerPos)
    {
        // 🔒 External hard lock: always chase player, never Search/Patrol
        if (locked)
        {
            state = AIState.Catch;

            // 实时锁定玩家位置（不是 forcedCatchPos）
            lastKnownPos = playerPos;

            // 清掉所有“会把你踢出 Catch”的计时
            lostTimer = 0f;
            trackTimer = 0f;
            seeTimer = 0f;

            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
            {
                repathTimer = catchRepathInterval;
                agent.isStopped = false;

                Vector3 dest = playerPos;

                // ① 先尝试常规 NavMesh 吸附
                bool navOk = TryGetNavDestination(playerPos, out dest);

                // ② 如果吸附点不可达（典型：玩家站桌子上）
                if (!navOk || agent.pathStatus != NavMeshPathStatus.PathComplete)
                {
                    // 朝怪物方向找“边界可达点”
                    if (TryGetReachablePointTowardEnemy(playerPos, out var boundary))
                    {
                        dest = boundary;
                    }
                }

                agent.SetDestination(dest);
            }


            // 攻击逻辑仍然照常（你原来的）
            attackTimer -= Time.deltaTime;
            float dist1 = Vector3.Distance(transform.position, playerPos);
            if (canSee && dist1 <= attackRange)
            {
                if (attackTimer <= 0f)
                {
                    attackTimer = attackCooldown;
                    behavior?.RequestAttack(playerPos);
                }
            }

            return; // ❗locked 时不再跑下面任何逻辑
        }
        
        
        // ✅ Forced Catch: 10s lock position, ignore lose-sight -> Search
        if (forcedCatchTimer > 0f)
        {
            forcedCatchTimer -= Time.deltaTime;

            // 保险：确保仍在 Catch
            state = AIState.Catch;

            // 用锁定点作为“最后目击点”
            lastKnownPos = forcedCatchPos;

            // 不让丢失逻辑把你踢去 Search
            lostTimer = 0f;
            trackTimer = 0f;

            // 追锁定点（按你的 repath 节奏）
            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
            {
                repathTimer = catchRepathInterval;
                agent.isStopped = false;
                agent.SetDestination(forcedCatchPos);
            }

            // 攻击仍建议需要 canSee（避免隔墙打）
            // 这里用当前 canSee + 当前 playerPos（外层算出来的）
            attackTimer -= Time.deltaTime;
            float dist0 = Vector3.Distance(transform.position, forcedCatchPos);
            if (canSee && dist0 <= attackRange)
            {
                if (attackTimer <= 0f)
                {
                    attackTimer = attackCooldown;
                    behavior?.RequestAttack(playerPos);
                }
            }

            return; // ✅ 强制期内不跑正常 Catch（尤其是“进入 Search”那段）
        }

        // 1) 更新 lastKnownPos + 丢失计时
        if (canSee)
        {
            lastKnownPos = playerPos;
            lostTimer = 0f;
        }
        else
        {
            lostTimer += Time.deltaTime;
        }

         // 先算距离（后面多处要用）
        Vector3 distRef = canSee ? playerPos : lastKnownPos;
        // ✅ 水平距离（忽略Y）
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = distRef;           b.y = 0f;
        float dist = Vector3.Distance(a, b);

        //float dist = Vector3.Distance(transform.position, distRef);
        
        // 贴脸慢走：进入 slowDownDistance 后，把 speed 降为 closeSpeed
        agent.speed = (dist <= slowDownDistance) ? closeSpeed : catchSpeed;

        // 导航：看见就追玩家；看不见就追最后目击点（缓冲期内）
        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
        {
            repathTimer = catchRepathInterval;

            Vector3 dest;
            if (canSee)
            {
                trackTimer = trackAfterLost;
                lastKnownPos = playerPos;
                lostTimer = 0f;

                // 追“外圈点”，避免贴脸推挤导致距离振荡/穿模
                Vector3 toEnemy = transform.position - playerPos;
                toEnemy.y = 0f;
                Vector3 dir = (toEnemy.sqrMagnitude > 0.0001f) ? toEnemy.normalized : -transform.forward;
                dest = playerPos + dir * agent.stoppingDistance;
            }
            else
            {
                // 丢失视野：先“记忆追踪”玩家真实位置 trackAfterLost 秒，再回追 lastKnownPos
                trackTimer -= Time.deltaTime;

                if (player != null && trackTimer > 0f)
                    dest = player.position;     // ✅ 1秒窗口内追真实玩家位置（折中记忆）
                else
                    dest = lastKnownPos;        // ✅ 之后回归追最后目击点
            }

            agent.isStopped = false;
            agent.SetDestination(dest);
        }

        // 3) 攻击（建议：只有 canSee 才攻击，避免隔墙）
        attackTimer -= Time.deltaTime;

        if (canSee && dist <= attackRange)
        {
            // 面向玩家（水平）
            Vector3 look = playerPos - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
            {
                Quaternion rot = Quaternion.LookRotation(look.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, 12f * Time.deltaTime);
            }

            if (attackTimer <= 0f)
            {
                attackTimer = attackCooldown;
                if (behavior != null)
                    behavior.RequestAttack(playerPos);
            }
        }

        // 4) 丢失视野超过缓冲 → Search
        if (!canSee && lostTimer >= loseSightGrace)
    {
        // ✅ 如果玩家点不可达（典型：站桌子上），不要因为视线抖动就进 Search
        //    继续维持 Catch，追边界点/lastKnownPos，让它“贴桌边盯人”
        if (TryGetReachablePointTowardEnemy(playerPos, out _))
        {
            // 有可达边界点：继续 Catch，不进 Search（避免三态抖）
            lostTimer = 0f; // 或者不清也行，但清了更稳
            return;
        }

        agent.speed = defaultSpeed;

        Vector3 hint = lastKnownPos - transform.position;
        hint.y = 0f;
        if (hint.sqrMagnitude > 0.001f) searchDirHint = hint.normalized;

        EnterSearch(lastKnownPos);
        return;
    }

    }

    bool TryGetNavDestination(Vector3 desiredWorldPos, out Vector3 navPos)
    {
        // 用较大的采样半径，允许桌子边缘也能找到最近地面点
        float sample = 3.0f;
        if (NavMesh.SamplePosition(desiredWorldPos, out NavMeshHit hit, sample, NavMesh.AllAreas))
        {
            navPos = hit.position;
            return true;
        }

        navPos = desiredWorldPos;
        return false;
    }
    bool TryGetReachablePointTowardEnemy(Vector3 playerPos, out Vector3 reachablePos)
    {
        reachablePos = playerPos;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return false;

        // 参数：尽量复用你已有范围，不额外加一堆 inspector 变量
        float sampleRadius = Mathf.Max(attackRange, agent.radius * 2f, 1.5f);
        float maxBackDistance = Mathf.Min(viewDistance, 10f);      // 不要无限推
        float step = 0.5f;                                         // 够用了

        // 朝怪物方向回推（player -> enemy）
        Vector3 dirToEnemy = (transform.position - playerPos);
        dirToEnemy.y = 0f;
        if (dirToEnemy.sqrMagnitude < 0.0001f) return false;
        dirToEnemy.Normalize();

        var path = new NavMeshPath();

        // 从玩家点开始往回推，找到第一个“可达”的 NavMesh 点（边界附近）
        for (float d = 0f; d <= maxBackDistance; d += step)
        {
            Vector3 probe = playerPos + dirToEnemy * d;

            // 吸到 NavMesh 上（注意：不是垂直投影）
            if (!NavMesh.SamplePosition(probe, out var hit, sampleRadius, NavMesh.AllAreas))
                continue;

            // 验证这个点从敌人当前位置是否真的可走到
            NavMesh.CalculatePath(agent.transform.position, hit.position, NavMesh.AllAreas, path);
            if (path.status != NavMeshPathStatus.PathComplete)
                continue;

            reachablePos = hit.position;
            return true;
        }

        return false;
    }

    
    //-------------------------Search Mode-----------------------
    void EnterSearch(Vector3 anchorPos)
    {
        if (locked) return;

        // ✅ 防止在 Search 状态下被重复 EnterSearch（你日志里“Search里又生成点”最直接的保险）
        if (state == AIState.Search) return;

        // ✅ 进入 Search 前先清理旧状态残留
        StopAllAIcoroutines();

        Debug.Log($"{name} -> SEARCH (lost sight). Anchor={anchorPos}");
        agent.speed = defaultSpeed;
        state = AIState.Search;

        // ✅ 关键：清掉 Catch / Reacquire 残留计时器，避免 Search done/Patrol 立刻被拉回 Catch
        lostTimer = 0f;
        seeTimer = 0f;
        trackTimer = 0f;
        attackTimer = attackCooldown; // 可选：避免刚出视野边缘立即触发攻击

        agent.isStopped = false;
        agent.ResetPath();

        lastKnownPos = anchorPos;
        searchInspecting = false;

        // 10秒搜索
        searchTimer = searchDuration;

        // 生成扇形搜索点（不依赖节点）
        searchPoints.Clear();

        // 把 anchorPos 修正到最近的 NavMesh 点上（避免 generated=0）
        if (NavMesh.SamplePosition(anchorPos, out var navHit, 3.0f, NavMesh.AllAreas))
        anchorPos = navHit.position;

        BuildSearchPointsCone(anchorPos, searchDirHint);
        Debug.Log($"{name} SEARCH points generated = {searchPoints.Count}, anchor={anchorPos}, dir={searchDirHint}");

        // 用 searchPointQueue 判断
        if (searchPoints.Count == 0)
        {
            // 退化：只去 lastKnownPos 看一眼
            searchCurrentPoint = anchorPos;
            SetDestination(searchCurrentPoint);
            return;
        }

        // 取第一个点前往
        searchCurrentPoint = PopFarthestWithinStep();
        SetDestination(searchCurrentPoint);
    }

    void UpdateSearch(bool canSee, Vector3 playerPos)
    {
        if (canSee) return; // Update() 顶部会切 Catch

        // 10秒超时回 Patrol
        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0f)
        {
            ExitSearchToPatrol();
            return;
        }

        // 正在扫描就不要推进
        if (searchInspecting) return;

        // 到点就扫描
        if (ReachedDestination() && searchInspectCR == null)
        {
            searchInspectCR = StartCoroutine(SearchInspect());
            return;
        }

        // 如果当前点走不了/路径无效，直接换下一个点
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            GoNextSearchPointOrFinish();
            return;
        }
    }

    public bool CanSeePlayer(out Vector3 playerPos)
    {
        playerPos = default;
        if (player == null) return false;

        Vector3 origin = (eyePoint != null) ? eyePoint.position : (transform.position + Vector3.up * 1.6f);
        Vector3 center = player.position + Vector3.up * 2.0f;

        // ✅ 触发器近距离：也要给 playerPos
        if (EnemySensor != null && EnemySensor.PlayerInside)
        {
            playerPos = center;
            CacheDebug(true, origin, center, -2, 0f, true);
            return true;
        }

        // ✅ Close guarantee：也要给 playerPos
        float closeDist = attackRange * 1.2f;
        if ((center - origin).sqrMagnitude <= closeDist * closeDist)
        {
            playerPos = center;
            CacheDebug(true, origin, center, -1, 0f, true);
            return true;
        }
        
        if(EnemySensor.PlayerInside) return true;

        if (isBlind) return false;

        // 0) 触发器粗筛
        if (player == null)
        {
            CacheDebug(false, Vector3.zero, Vector3.zero, -1, 0f, false);
            return false;
        }

        // 3) 距离粗筛（可选但推荐）
        Vector3 toCenter = center - origin;
        float distCenter = toCenter.magnitude;
        if (distCenter > viewDistance)
        {
            CacheDebug(false, origin, center, -1, 0f, false);
            return false;
        }

        // 4) 视野角（水平扇面：只看XZ平面）
        Vector3 flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z);
        Vector3 flatTo = new Vector3(toCenter.x, 0f, toCenter.z);

        bool inFOV = true;
        float angle = 0f;

        if (flatTo.sqrMagnitude > 0.0001f && flatForward.sqrMagnitude > 0.0001f)
        {
            angle = Vector3.Angle(flatForward, flatTo);
            inFOV = angle <= viewAngle * 0.5f;
        }

        if (!inFOV)
        {
            CacheDebug(false, origin, center, -1, angle, false);
            return false;
        }

        // 5) 多射线：中心 + 左右 + 上下（任意1条无遮挡即看见）
        Vector3 right = transform.right;
        Vector3 up = Vector3.up;

        Vector3[] targets =
        {
            center,
            center + right * sideOffset,
            center - right * sideOffset,
            center + up    * upOffset,
            center - up    * downOffset,
        };

        int passIndex = -1;

        for (int i = 0; i < targets.Length; i++)
        {
            Vector3 to = targets[i] - origin;
            float dist = to.magnitude;
            if (dist < 0.001f) { passIndex = i; break; }

            Vector3 dir = to / dist;

            // 只检测遮挡物层；没撞到遮挡 => 这条线通过
            bool blocked = Physics.Raycast(origin, dir, dist, visionBlockMask, QueryTriggerInteraction.Ignore);
            if (!blocked)
            {
                passIndex = i;
                break;
            }
        }

        bool canSee = passIndex != -1;
        CacheDebug(canSee, origin, center, passIndex, angle, inFOV);

        if (canSee)
        {
            playerPos = center; // 作为 lastKnownPos 更稳定
            return true;
        }

        return false;
    }

    void BuildSearchPointsCone(Vector3 anchorPos, Vector3 dirHint)
    {
        Vector3 dir = dirHint;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
        {
            // 兜底：朝向敌人当前前方
            dir = transform.forward;
            dir.y = 0f;
        }
        dir = dir.normalized;

        // 近->远的圈
        for (int ring = 0; ring < searchRings; ring++)
        {
            float t = (searchRings <= 1) ? 1f : ring / (float)(searchRings - 1);
            float radius = Mathf.Lerp(searchMinRadius, searchMaxRadius, t);

            for (int i = 0; i < searchPointsPerRing; i++)
            {
                float u = (searchPointsPerRing <= 1) ? 0.5f : i / (float)(searchPointsPerRing - 1);
                float angle = Mathf.Lerp(-searchConeAngle, searchConeAngle, u);

                Vector3 sampleDir = Quaternion.AngleAxis(angle, Vector3.up) * dir;
                Vector3 raw = anchorPos + sampleDir * radius;

                if (NavMesh.SamplePosition(raw, out NavMeshHit hit, navmeshSampleRadius, NavMesh.AllAreas))
                {
                    // 简单去重（可选）
                    if (searchPoints.Count == 0 || (hit.position - anchorPos).sqrMagnitude > 0.25f)
                        searchPoints.Add(hit.position);
                }
            }
        }
    }

    //函数重载
    void BuildSearchPointsCone(Vector3 anchorPos, Vector3 dirHint, float minRadius, float maxRadius)
    {
        Vector3 dir = dirHint;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
        {
            dir = transform.forward;
            dir.y = 0f;
        }
        dir = dir.normalized;

        for (int ring = 0; ring < searchRings; ring++)
        {
            float t = (searchRings <= 1) ? 1f : ring / (float)(searchRings - 1);
            float radius = Mathf.Lerp(minRadius, maxRadius, t);

            for (int i = 0; i < searchPointsPerRing; i++)
            {
                float u = (searchPointsPerRing <= 1) ? 0.5f : i / (float)(searchPointsPerRing - 1);
                float angle = Mathf.Lerp(-searchConeAngle, searchConeAngle, u);

                Vector3 sampleDir = Quaternion.AngleAxis(angle, Vector3.up) * dir;
                Vector3 raw = anchorPos + sampleDir * radius;

                if (NavMesh.SamplePosition(raw, out NavMeshHit hit, navmeshSampleRadius, NavMesh.AllAreas))
                {
                    if (searchPoints.Count == 0 || (hit.position - anchorPos).sqrMagnitude > 0.25f)
                        searchPoints.Add(hit.position);
                }
            }
        }
    }

    void GoNextSearchPointOrFinish()
    {
        if (reaction != ReactionState.None) return;

        if (searchPoints.Count > 0)
        {
            searchCurrentPoint = PopFarthestWithinStep();
            SetDestination(searchCurrentPoint);
            searchInspecting = false;
            return;
        }

        // 没点了就回 Patrol
        Debug.Log($"{name} SEARCH queue empty. searchTimer={searchTimer:F2}");
        ExitSearchToPatrol();
    }

    void ExitSearchToPatrol()
    {
        Debug.Log("Searching is done (timeout). Back to Patrol mode");
        StopAllAIcoroutines();
        searchPoints.Clear();
        searchInspecting = false;
        if (searchInspectCR != null) { StopCoroutine(searchInspectCR); searchInspectCR = null; }

        // ✅ 清理“重获视野计时”，避免刚回 Patrol 就秒进 Catch
        seeTimer = 0f;
        lostTimer = 0f;
        trackTimer = 0f;

        state = AIState.Patrol;

        agent.speed = defaultSpeed;
        agent.isStopped = false;
        agent.ResetPath(); // ✅ 让 Search 的路径彻底作废

        var near = FindNearestNodeByOverlap();
        currentTarget = near != null ? near : startNode;
        SetDestination(currentTarget.Position);
    }

    Vector3 PopFarthestWithinStep()
    {
        if (searchPoints.Count == 0)
            return lastKnownPos;

        Vector3 origin = transform.position;
        float maxStepSqr = searchMaxStep * searchMaxStep;

        int bestIndex = -1;
        float bestDistSqr = -1f;

        int fallbackIndex = 0;
        float fallbackDistSqr = (searchPoints[0] - origin).sqrMagnitude;

        for (int i = 0; i < searchPoints.Count; i++)
        {
            float d = (searchPoints[i] - origin).sqrMagnitude;

            // fallback：全体最远（用于“没有任何点在 maxStep 内”的情况）
            if (d > fallbackDistSqr)
            {
                fallbackDistSqr = d;
                fallbackIndex = i;
            }

            // best：<= maxStep 的最远
            if (d <= maxStepSqr && d > bestDistSqr)
            {
                bestDistSqr = d;
                bestIndex = i;
            }
        }

        int pick = (bestIndex != -1) ? bestIndex : fallbackIndex;
        Vector3 chosen = searchPoints[pick];
        searchPoints.RemoveAt(pick);
        return chosen;
    }

    IEnumerator SearchInspect()
    {
        if (reaction != ReactionState.None) { searchInspectCR = null; yield break; }
        searchInspecting = true;
        if (AgentReady()) agent.isStopped = true;

        // 转头检查（复用巡逻参数）
        Quaternion startRot = transform.rotation;
        yield return RotateByAngle(Random.Range(inspectAngleMin, inspectAngleMax));
        yield return new WaitForSeconds(inspectPause);
        yield return RotateToRotation(startRot);
        yield return RotateByAngle(-Random.Range(inspectAngleMin, inspectAngleMax));
        yield return new WaitForSeconds(inspectPause);

        if (AgentReady()) agent.isStopped = false;

        searchInspectCR = null;
        searchInspecting = false;

        // 扫描结束后去下一个搜索点
        GoNextSearchPointOrFinish();
        yield break;
    }

    //========================REACTION STATE========================
    public void Die()
    {
        if (reaction == ReactionState.Dead) return;  // 防止重复死亡
        EnterDeath();
    }

    void EnterDeath()
    {
        Debug.Log("An Enemy is Dead");
        reaction = ReactionState.Dead;
        StopAllAIcoroutines();

        if (agent != null && agent.enabled )   // 2️⃣ 停止移动
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }
    }

    void UpdateReaction()
    {
        if (reaction == ReactionState.HitStagger)
        {
            staggerTimer -= Time.deltaTime;
            transform.position += staggerDir * staggerSpeed * Time.deltaTime;

            if (staggerTimer <= 0f)
            {
                Debug.Log("Exit Hit State");
                reaction = ReactionState.None;
                if (agent != null)
                {
                    agent.enabled = true;

                    if (NavMesh.SamplePosition(transform.position, out var hit, 1.0f, NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                        agent.isStopped = false;
                        ActivatePendingForcedCatch();
                    }
                    else agent.enabled = true; // 找不到 NavMesh 就先别恢复移动，避免再次报错
                }
            }
        }

        else if (reaction == ReactionState.Stunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
                {
                    Debug.Log("Exit Stunning State");
                    reaction = ReactionState.None;
                    if (agent != null && agent.enabled)
                        agent.isStopped = false;
                    ActivatePendingForcedCatch();
                }
        }
    }

    public void ForceCatchLock(Vector3 playerPosAtHit)
    {
        if (reaction == ReactionState.Dead) return;

        forcedCatchTimer = forcedCatchDuration;
        forcedCatchPos = playerPosAtHit;

        // 立刻切到 Catch（复用你现有 EnterCatch，会清协程/计时器）
        EnterCatch(playerPosAtHit);
    }

    public void ReactToAttack(Vector3 hitDirection) //attack状态的外部调用接口
    {
        if (reaction == ReactionState.Dead) return;
        else if (reaction == ReactionState.HitStagger) return;

        EnterHitStagger(hitDirection);
    }

    void EnterHitStagger(Vector3 hitDirection)  //进入攻击击退状态
    {
        Debug.Log("Enter Hit State");
        reaction = ReactionState.HitStagger;
        StopAllAIcoroutines();

        // --- Pending forced catch: record lock point now, start timer after reaction ends ---
        if (player != null)
        {
            pendingForcedCatch = true;
            pendingForcedPos = player.position + Vector3.up * 1.0f; // 建议用中心点，和 CanSee 返回一致
        }

        // 意图立刻变追击（但本次硬直期间 agent disabled，不会移动，符合预期）
        state = AIState.Catch;
        lastKnownPos = pendingForcedPos;

        staggerDir = hitDirection.normalized;
        staggerTimer = hitStaggerDuration;   // 例如 0.5f
        staggerSpeed = hitStaggerSpeed;       // 例如 3.0f

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false; // ⭐ 关键：手动位移期间彻底关掉 agent
        }
    }

    public void ReactToStun(float duration)
    {
        if (reaction == ReactionState.Dead)
            return;
        // Stun 的优先级 > HitStagger
        EnterStun(duration);
    }

    void EnterStun(float duration)
    {
        Debug.Log("Enter Stunning State");
        StopAllAIcoroutines();
        reaction = ReactionState.Stunned;
        stunTimer = duration;

        if (player != null)
        {
            pendingForcedCatch = true;
            pendingForcedPos = player.position + Vector3.up * 1.0f;
        }

        state = AIState.Catch;
        lastKnownPos = pendingForcedPos;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    void StopAllAIcoroutines()
    {
        if (patrolInspectCR != null) { StopCoroutine(patrolInspectCR); patrolInspectCR = null; }
        if (searchInspectCR != null) { StopCoroutine(searchInspectCR); searchInspectCR = null; }

        isInspecting = false;
        searchInspecting = false;
    }

    void ActivatePendingForcedCatch()
    {
        if (!pendingForcedCatch) return;
        if (reaction == ReactionState.Dead) return;

        pendingForcedCatch = false;

        // 真正开始 10s 锁点保护
        forcedCatchTimer = forcedCatchDuration;
        forcedCatchPos = pendingForcedPos;

        // 保险：保持在 Catch，并清掉丢失计时，避免立刻被 Search 逻辑影响
        state = AIState.Catch;
        lastKnownPos = forcedCatchPos;
        lostTimer = 0f;
        trackTimer = 0f;
        seeTimer = 0f;

        // 立刻下发一次追击目的地（agent 已恢复 enabled 时更可靠）
        if (AgentReady())
        {
            repathTimer = 0f;
            agent.isStopped = false;
            agent.ResetPath();
            agent.SetDestination(forcedCatchPos);
        }
    }

    public void HearGunshot(Vector3 soundPos)
    {
        if (reaction != ReactionState.None) return;
        if (locked) return;
        if (forcedCatchTimer > 0f) return;
        if (state == AIState.Catch) return;
        
        float dist = Vector3.Distance(transform.position, soundPos);
        if (dist > hearingRadius) return;

        ForceSearchFromGunshot(soundPos);
    }

    void ForceSearchFromGunshot(Vector3 anchorPos)
    {
        if (!AgentReady()) return;

        // 直接切状态
        state = AIState.Search;

        // 清旧搜索数据
        searchPoints.Clear();
        searchInspecting = false;

        // 重置 Search 计时（新线索）
        searchTimer = searchDuration;

        // 停掉当前路径，准备走新目标
        agent.isStopped = false;
        agent.ResetPath();

        // anchor 修正到 NavMesh
        if (NavMesh.SamplePosition(anchorPos, out var navHit, 3.0f, NavMesh.AllAreas))
            anchorPos = navHit.position;

        lastKnownPos = anchorPos;

        // 方向提示：敌人 -> 枪声
        Vector3 dirHint = anchorPos - transform.position;
        dirHint.y = 0f;

        // ✅ 距离越远偏差越大：半径倍率随距离增大
        float dist = Vector3.Distance(transform.position, anchorPos);
        float k = Mathf.InverseLerp(gunshotNearDist, gunshotFarDist, dist);
        float mul = Mathf.Lerp(1f, gunshotMaxRadiusMul, k);

        float minR = searchMinRadius * mul;
        float maxR = searchMaxRadius * mul;

        // 生成 5 点（建议 rings=1, perRing=5）
        BuildSearchPointsCone(anchorPos, dirHint, minR, maxR);

        // 立刻选“最远但不超过最大步长”的点作为下一个
        if (searchPoints.Count > 0)
        {
            searchCurrentPoint = PopFarthestWithinStep();
            SetDestination(searchCurrentPoint);
        }
        else
        {
            // 兜底：没生成点就直接走 anchor
            searchCurrentPoint = anchorPos;
            SetDestination(searchCurrentPoint);
        }

        // 可选：Debug
        Debug.Log($"{name} -> SEARCH (gunshot simple). points={searchPoints.Count}, anchor={anchorPos}, dist={dist:F1}, mul={mul:F2}");
    }

    public void playerStatus(int status)
    {
        if (locked)
        {
            isBlind = false;
            viewDistance = lockedViewDistance;
            return;
        }
        
        // 恢复默认
        isBlind = false;
        viewDistance = baseViewDistance;

        switch (status)
        {
            case 1: // crouch
                viewDistance = crouchViewDistance;
                break;

            case 2: // hiding
                isBlind = true;
                break;
        }
    }

    //------------------------Debug----------------------------
    private void CacheDebug(bool canSee, Vector3 origin, Vector3 center, int passIndex, float angle, bool inFOV)
    {
        _dbgLastCanSee = canSee;
        _dbgLastOrigin = origin;
        _dbgLastPlayerCenter = center;
        _dbgLastPassRayIndex = passIndex;
        _dbgLastAngle = angle;
        _dbgLastInFOV = inFOV;

        // 存 targets 供 gizmos
        if (_dbgLastTargets == null || _dbgLastTargets.Length != 5) _dbgLastTargets = new Vector3[5];
        _dbgLastTargets[0] = center;
        _dbgLastTargets[1] = center + transform.right * sideOffset;
        _dbgLastTargets[2] = center - transform.right * sideOffset;
        _dbgLastTargets[3] = center + Vector3.up * upOffset;
        _dbgLastTargets[4] = center - Vector3.up * downOffset;
    }

    private void OnDrawGizmos()
    {
        if (!debugDrawVision) return;

        // 眼睛点
        Vector3 origin = (eyePoint != null) ? eyePoint.position : (transform.position + Vector3.up * 1.6f);

        // 1) 画FOV扇面（只画两条边 + 外圈弧的近似）
        if (debugDrawFOV)
        {
            // FOV颜色：可见=绿，不可见=红（不指定颜色也行，但调试时强烈建议区分）
            Gizmos.color = _dbgLastCanSee ? Color.green : Color.red;

            Vector3 fwd = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            if (fwd.sqrMagnitude > 0.0001f)
            {
                float half = viewAngle * 0.5f;
                Quaternion leftRot = Quaternion.Euler(0f, -half, 0f);
                Quaternion rightRot = Quaternion.Euler(0f, half, 0f);

                Vector3 leftDir = (leftRot * fwd) * viewDistance;
                Vector3 rightDir = (rightRot * fwd) * viewDistance;

                Gizmos.DrawLine(origin, origin + leftDir);
                Gizmos.DrawLine(origin, origin + rightDir);

                // 简单画个弧线（分段）
                int seg = 16;
                Vector3 prev = origin + (Quaternion.Euler(0f, -half, 0f) * fwd) * viewDistance;
                for (int i = 1; i <= seg; i++)
                {
                    float t = (float)i / seg;
                    float yaw = Mathf.Lerp(-half, half, t);
                    Vector3 p = origin + (Quaternion.Euler(0f, yaw, 0f) * fwd) * viewDistance;
                    Gizmos.DrawLine(prev, p);
                    prev = p;
                }
            }
        }

        // 2) 画多射线
        if (debugDrawRays && _dbgLastTargets != null && _dbgLastTargets.Length == 5)
        {
            for (int i = 0; i < 5; i++)
            {
                // 通过的那条加亮
                Gizmos.color = (i == _dbgLastPassRayIndex) ? Color.cyan : new Color(1f, 1f, 1f, 0.35f);
                Gizmos.DrawLine(origin, _dbgLastTargets[i]);

                // 目标点画个小球
                Gizmos.DrawSphere(_dbgLastTargets[i], 0.05f);
            }
        }

#if UNITY_EDITOR
        // 3) 显示文字：是否在FOV，最终是否看见，夹角是多少
        if (debugDrawVision)
        {
            UnityEditor.Handles.Label(
                origin + Vector3.up * 0.25f,
                $"InFOV: {_dbgLastInFOV}\nAngle: {_dbgLastAngle:0.#}\nCanSee: {_dbgLastCanSee}\nPassRay: {_dbgLastPassRayIndex}"
            );
        }
#endif
    }
}
