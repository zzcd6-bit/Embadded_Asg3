using UnityEngine;
using UnityEngine.AI;

public class EnemyBehavior : MonoBehaviour
{
    EnemyAI ai;
    NavMeshAgent agent;
    public Animator anim;

    EnemyAI.AIState lastState;

    [Header("Combo")]
    [SerializeField] int maxCombo = 4;          // 4段
    [SerializeField] float comboWindow = 2.5f;  // 命中后允许接招的时间窗口（秒）

    int comboIndex = 0;     // 1~4
    float comboTimer = 0f;  // >0表示连击窗口还开着


    [Header("Shout Pause")]
    [SerializeField] float shoutPauseDuration = 1.0f;
    public float shoutPauseTimer = 0f;
    public bool IsShoutPausing => shoutPauseTimer > 0f;

    bool deathDisabled = false;

    // ===================== Combat (Melee) =====================
    [Header("Melee Damage")]
    [SerializeField] float attackDamage = 10f;

    // 命中窗口（动画事件控制）
    bool attackWindowOpen = false;
    bool hasAppliedHit = false;

    // 命中范围（OverlapSphere）
    [SerializeField] float hitRadius = 1.0f;
    [SerializeField] Vector3 hitOffsetLocal = new Vector3(0f, 1.0f, 1.0f); // 本地坐标：前方1m，高度1m
    [SerializeField] LayerMask playerMask;

    // 夹角限制（面向玩家才算命中）
    [SerializeField, Range(0f, 180f)] float hitAngleLimit = 70f;

    // 遮挡检测（用 EnemyAI.visionBlockMask）
    [SerializeField] float lineOfSightEyeHeight = 1.6f;   // 敌人“眼睛高度”
    [SerializeField] float lineOfSightTargetHeight = 1.0f; // 玩家“胸口高度”

    // 可选：调试输出
    [SerializeField] bool debugAttackHit = true;

    void Awake()
    {
        ai = GetComponent<EnemyAI>();
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        if (ai != null) lastState = ai.state;
    }

    void Update()
    {
        if (!agent || !anim || !ai) return;

        if (ai.ReactionValue == 3) // Dead
        {
            // 确保进入Death
            anim.SetInteger("Reaction", 3);
            anim.SetFloat("Speed", 0f);
            

            if (!deathDisabled)
            {
                var st = anim.GetCurrentAnimatorStateInfo(0);
                if (st.IsName("Death") && st.normalizedTime >= 0.99f)
                {
                    anim.enabled = false;   // ✅ 停在最后一帧
                    deathDisabled = true;
                }
            }
            return;
        }
        
        bool agentReady = agent.enabled && agent.isOnNavMesh;

        // --- Combo timer ---
        if (comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f) comboIndex = 0;
        }

        // --- Animator params ---
        anim.SetFloat("Speed", agent.velocity.magnitude);
        anim.SetInteger("AIState", (int)ai.state);
        anim.SetInteger("Reaction", ai.ReactionValue);

        // --- Hit/Stun/Dead can interrupt shout pause immediately ---
        if (shoutPauseTimer > 0f && (ai.ReactionValue == 1 || ai.ReactionValue == 2 || ai.ReactionValue == 3))
        {
            CancelShoutPause(); // 你自己已在里面做了 shoutPauseTimer=0 和安全恢复
        }

        // --- Shout pause: stop moving + freeze state-change detection ---
        if (shoutPauseTimer > 0f)
        {
            shoutPauseTimer -= Time.deltaTime;

            if (agentReady) agent.isStopped = true;

            if (shoutPauseTimer <= 0f)
            {
                if (agentReady && ai.ReactionValue != 3)
                    agent.isStopped = false;
            }

            return; // freeze while shouting
        }

        // --- State change -> trigger shout once, then pause movement ---
        if (ai.state != lastState)
        {
            lastState = ai.state;

            if (ai.ReactionValue == 0) // only shout when normal
            {
                anim.ResetTrigger("TrigShout");
                anim.SetTrigger("TrigShout");

                shoutPauseTimer = shoutPauseDuration;
                if (agentReady) agent.isStopped = true;
            }
        }
    }


    void CancelShoutPause()
    {
        shoutPauseTimer = 0f;

        // If dead, let death logic decide; otherwise unstop immediately
        if (agent != null && agent.enabled && agent.isOnNavMesh && ai != null && ai.ReactionValue != 3)
            agent.isStopped = false;

        if (anim != null)
            anim.ResetTrigger("TrigShout"); // clears pending trigger if not yet consumed
    }

    public void RequestAttack(Vector3 playerPos)
    {
        // 如果连击窗口已经超时，这里也可以强制归零（双保险）
        if (comboTimer <= 0f) comboIndex = 0;

        // 把当前段数喂给 Animator，让它从 Locomotion 分支到 Attack_0~3
        anim.SetInteger("Combo", comboIndex);
        anim.ResetTrigger("TrigAttack");
        anim.SetTrigger("TrigAttack");

        // 每次开始攻击，重置本次是否已结算伤害（你之前的命中门控）
        hasAppliedHit = false;
    }

    // ---- Animation Events：把这些函数挂到 Attack 动画剪辑上 ----
    public void Anim_AttackWindowOpen()  { attackWindowOpen = true; }
    public void Anim_AttackWindowClose() { attackWindowOpen = false; }

    // 命中帧：在挥到位那一帧调用
    public void Anim_AttackHit()
    {
        if (!attackWindowOpen) return;  // 只在窗口内判定
        if (hasAppliedHit) return;      // 一次攻击最多结算一次

        bool hit = TryDealMeleeDamage();
        hasAppliedHit = true;

        if (hit)
        {
            // ✅ 命中才推进连击，并刷新窗口
            comboIndex++;
            if (comboIndex >= maxCombo) comboIndex = 0; // 4段后回到第1段（你也可以改成停在最后一段）
            comboTimer = comboWindow;
        }
        else
        {
            // ❌ 空挥/被挡/角度不够：断连
            comboIndex = 0;
            comboTimer = 0f;
        }
    }

    bool TryDealMeleeDamage()
    {
        // 1) OverlapSphere 找玩家
        Vector3 center = transform.TransformPoint(hitOffsetLocal);
        Collider[] hits = Physics.OverlapSphere(
            center, hitRadius, playerMask, QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0) return false;

        // 取最近的命中 collider
        Collider best = null;
        float bestSqr = float.PositiveInfinity;
        for (int i = 0; i < hits.Length; i++)
        {
            float d = (hits[i].bounds.center - center).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = hits[i]; }
        }
        if (best == null) return false;

        Vector3 playerPos = best.bounds.center;

        // 2) 夹角限制
        Vector3 toPlayer = playerPos - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return false;

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > hitAngleLimit) return false;

        // 3) 遮挡检测（敌人眼睛 -> 玩家胸口）
        LayerMask blockMask = ai != null ? ai.visionBlockMask : 0;
        if (blockMask.value != 0)
        {
            Vector3 eye = transform.position + Vector3.up * lineOfSightEyeHeight;
            Vector3 target = playerPos + Vector3.up * lineOfSightTargetHeight;

            if (Physics.Linecast(eye, target, blockMask, QueryTriggerInteraction.Ignore))
                return false;
        }

        // 4) 命中成立：扣血 + 受击反馈
        Vector3 sourcePos = transform.position;

        var hp = best.GetComponentInParent<PlayerHealth>();
        if (hp != null)
            hp.TakeDamage(attackDamage, sourcePos);

        var fb = best.GetComponentInParent<DamageFeedbackController>();
        if (fb != null)
        {
            Vector3 hitDir = playerPos - sourcePos;
            hitDir.y = 0f;
            if (hitDir.sqrMagnitude > 0.0001f) hitDir.Normalize();
            fb.OnDamaged(attackDamage, hitDir);
        }

        return true;
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireSphere(hitOffsetLocal, hitRadius);
    }


}
