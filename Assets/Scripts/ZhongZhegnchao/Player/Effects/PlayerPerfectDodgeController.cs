using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerPerfectDodgeController : MonoBehaviour
{
    [Header("Dodge Window")]
    [SerializeField] private float perfectDodgeWindow = 0.18f;
    [SerializeField] private float invincibleWindow = 0.35f;

    [Header("Enemy Attack Threat")]
    [SerializeField] private float defaultThreatDuration = 0.35f;
    [SerializeField] private bool allowThreatToTriggerWhenAlreadyDodging = true;

    [Header("Perfect Dodge Result")]
    [SerializeField] private float perfectDodgeInvincibleExtension = 0.35f;

    [Header("Feedback")]
    [SerializeField] private PlayerAfterImageSpawner afterImageSpawner;
    [SerializeField] private bool spawnAfterImageOnPerfectDodge = true;

    [Header("Post Process")]
    [SerializeField] private PlayerPerfectDodgePostProcess postProcessFeedback;
    [SerializeField] private bool usePostProcessFeedback = true;

    [Header("Hit Pause")]
    [SerializeField] private bool useHitPause = true;
    [SerializeField] private float hitPauseTimeScale = 0.08f;
    [SerializeField] private float hitPauseDuration = 0.08f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private float dodgeStartTime = -999f;
    private float perfectDodgeEndTime = -999f;
    private float invincibleEndTime = -999f;

    private GameObject currentThreatAttacker;
    private float threatEndTime = -999f;
    private bool threatConsumed;

    private bool hasPerfectDodgedThisDodge;
    private Coroutine hitPauseCoroutine;

    public bool IsInPerfectDodgeWindow
    {
        get
        {
            return Time.time <= perfectDodgeEndTime &&
                   !hasPerfectDodgedThisDodge;
        }
    }

    public bool IsInvincible
    {
        get { return Time.time <= invincibleEndTime; }
    }

    public bool HasActiveAttackThreat
    {
        get
        {
            return Time.time <= threatEndTime &&
                   !threatConsumed;
        }
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void CacheReferences()
    {
        if (afterImageSpawner == null)
        {
            afterImageSpawner = GetComponent<PlayerAfterImageSpawner>();
        }

        if (afterImageSpawner == null)
        {
            afterImageSpawner = GetComponentInChildren<PlayerAfterImageSpawner>();
        }

        if (postProcessFeedback == null)
        {
            postProcessFeedback = GetComponent<PlayerPerfectDodgePostProcess>();
        }

        if (postProcessFeedback == null)
        {
            postProcessFeedback = GetComponentInChildren<PlayerPerfectDodgePostProcess>();
        }
    }

    /// <summary>
    /// 玩家闪避开始时调用。
    /// </summary>
    public void BeginDodgeWindow()
    {
        dodgeStartTime = Time.time;
        perfectDodgeEndTime = dodgeStartTime + Mathf.Max(0f, perfectDodgeWindow);
        invincibleEndTime = dodgeStartTime + Mathf.Max(0f, invincibleWindow);
        hasPerfectDodgedThisDodge = false;

        if (debugLog)
        {
            Debug.Log(
                $"<color=yellow>[PerfectDodge]</color> BeginDodgeWindow. " +
                $"now={Time.time:F3}, perfectEnd={perfectDodgeEndTime:F3}, " +
                $"invincibleEnd={invincibleEndTime:F3}",
                this
            );
        }

        // 如果 Enemy 已经进入攻击威胁窗口，
        // 那么玩家此时按闪避，直接触发完美闪避。
        TryTriggerPerfectDodgeByThreat();
    }

    /// <summary>
    /// Enemy 进入攻击动作 / 攻击威胁时调用。
    /// </summary>
    public void NotifyIncomingEnemyAttack(GameObject attacker, float duration)
    {
        currentThreatAttacker = attacker;
        threatEndTime = Time.time + Mathf.Max(
            0.01f,
            duration > 0f ? duration : defaultThreatDuration
        );
        threatConsumed = false;

        if (debugLog)
        {
            string attackerName = attacker != null ? attacker.name : "null";

            Debug.Log(
                $"<color=orange>[PerfectDodge]</color> Incoming attack threat. " +
                $"attacker={attackerName}, now={Time.time:F3}, threatEnd={threatEndTime:F3}",
                this
            );
        }

        // 如果玩家已经在闪避窗口里，
        // Enemy 这时进入攻击，也允许触发完美闪避。
        if (allowThreatToTriggerWhenAlreadyDodging && IsInPerfectDodgeWindow)
        {
            TriggerPerfectDodge(attacker);
        }
    }

    /// <summary>
    /// HitBox 命中兜底逻辑。
    /// 如果 Enemy HitBox 打到 Player 时，Player 正好在闪避窗口，也触发完美闪避。
    /// </summary>
    public bool TryPerfectDodge(DamageInfo damageInfo)
    {
        if (debugLog)
        {
            Debug.Log(
                $"<color=orange>[PerfectDodge]</color> TryPerfectDodge by hit. " +
                $"now={Time.time:F3}, perfectEnd={perfectDodgeEndTime:F3}, " +
                $"inWindow={IsInPerfectDodgeWindow}, hasThreat={HasActiveAttackThreat}",
                this
            );
        }

        if (!IsInPerfectDodgeWindow)
        {
            return false;
        }

        TriggerPerfectDodge(damageInfo.attacker);
        return true;
    }

    public bool CanIgnoreDamageByInvincibleWindow()
    {
        return IsInvincible;
    }

    private bool TryTriggerPerfectDodgeByThreat()
    {
        if (!HasActiveAttackThreat)
        {
            return false;
        }

        TriggerPerfectDodge(currentThreatAttacker);
        return true;
    }

    private void TriggerPerfectDodge(GameObject attacker)
    {
        if (hasPerfectDodgedThisDodge)
        {
            return;
        }

        hasPerfectDodgedThisDodge = true;
        threatConsumed = true;

        // 完美闪避成功后，补一小段无敌时间，防止后续 HitBox 继续扣血。
        invincibleEndTime = Mathf.Max(
            invincibleEndTime,
            Time.time + perfectDodgeInvincibleExtension
        );

        TriggerPerfectDodgeFeedback(attacker);
        TriggerPerfectDodgeEvent(attacker);

        if (debugLog)
        {
            string attackerName = attacker != null ? attacker.name : "null";

            Debug.Log(
                $"<color=cyan><b>[PERFECT DODGE]</b></color> Success! " +
                $"attacker={attackerName}, time={Time.time:F3}",
                this
            );
        }
    }

    private void TriggerPerfectDodgeFeedback(GameObject attacker)
    {
        if (spawnAfterImageOnPerfectDodge && afterImageSpawner != null)
        {
            afterImageSpawner.SpawnAfterImage();
        }

        if (usePostProcessFeedback && postProcessFeedback != null)
        {
            postProcessFeedback.PlayPerfectDodgeEffect();
        }

        if (useHitPause)
        {
            StartHitPause();
        }

        // 后续可以继续加：
        // Camera Shake
        // Screen Flash
        // Counter Window
        // SFX
        //
        // 但建议这些后续都通过 E_Player_PerfectDodge 的监听器来扩展。
    }

    private void TriggerPerfectDodgeEvent(GameObject attacker)
    {
        EventCenter.Instance.EventTrigger(
            E_EventType.E_Player_PerfectDodge,
            new PerfectDodgeEventInfo
            {
                player = gameObject,
                attacker = attacker,
                playerPosition = transform.position,
                attackerPosition = attacker != null
                    ? attacker.transform.position
                    : Vector3.zero,
                triggerTime = Time.time
            }
        );
    }

    private void StartHitPause()
    {
        if (hitPauseCoroutine != null)
        {
            StopCoroutine(hitPauseCoroutine);
        }

        hitPauseCoroutine = StartCoroutine(HitPauseRoutine());
    }

    private IEnumerator HitPauseRoutine()
    {
        float originalTimeScale = Time.timeScale;

        Time.timeScale = Mathf.Clamp(hitPauseTimeScale, 0.01f, 1f);

        yield return new WaitForSecondsRealtime(hitPauseDuration);

        Time.timeScale = originalTimeScale;
        hitPauseCoroutine = null;
    }

    private void OnDisable()
    {
        if (hitPauseCoroutine != null)
        {
            StopCoroutine(hitPauseCoroutine);
            hitPauseCoroutine = null;

            Time.timeScale = 1f;
        }
    }
}