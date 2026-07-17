using UnityEngine;

public abstract class BrushSkillBase : MonoBehaviour
{
    [Header("Skill Match")]
    public float minScore = 0.65f;

    [Header("Cast Context")]
    public BrushCastContextBuilder castContextBuilder;

    [Header("Caster")]
    public GameObject caster;

    public bool requireUnlockedSkill = true;

    public bool requireInkCost = true;

    [Header("Debug")]
    public bool debugLog = true;

    private IBrushSkillCostReceiver skillCostReceiver;
    private IBrushSkillUnlockReceiver skillUnlockReceiver;
    private PlayerSkillTreeController skillTreeController;

    private float nextUseAllowedTime;

    protected abstract BrushSkillType SkillType { get; }

    public float CooldownRemaining
    {
        get
        {
            return Mathf.Max(0f, nextUseAllowedTime - Time.unscaledTime);
        }
    }

    public bool IsCoolingDown
    {
        get
        {
            return CooldownRemaining > 0f;
        }
    }

    protected virtual void Awake()
    {
        if (castContextBuilder == null)
        {
            castContextBuilder = GetComponent<BrushCastContextBuilder>();
        }

        if (castContextBuilder == null)
        {
            castContextBuilder = GetComponentInChildren<BrushCastContextBuilder>();
        }

        if (caster == null)
        {
            caster = gameObject;
        }

        ResolveSkillUnlockReceiver();
        ResolveSkillCostReceiver();
        ResolveSkillTreeController();
    }

    protected virtual void OnEnable()
    {
        EventCenter.Instance.AddEventListener<BrushGestureResult>(
            E_EventType.E_Brush_GestureRecognized,
            OnGestureRecognized
        );
    }

    protected virtual void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<BrushGestureResult>(
            E_EventType.E_Brush_GestureRecognized,
            OnGestureRecognized
        );
    }

    private void OnGestureRecognized(BrushGestureResult result)
    {
        if (result == null)
            return;

        if (result.score < minScore)
            return;

        if (result.skillType != SkillType)
            return;

        if (!CanUseUnlockedSkill())
            return;

        if (!CanUseCooldown())
            return;

        if (!TryPayInkCost())
            return;

        BrushCastContext context = null;

        if (castContextBuilder != null)
        {
            context = castContextBuilder.Build(result, caster);
        }

        Execute(result, context);

        StartCooldown();
    }

    private bool CanUseCooldown()
    {
        float cooldown = GetResolvedCooldown();

        if (cooldown <= 0f)
            return true;

        if (Time.unscaledTime >= nextUseAllowedTime)
            return true;

        if (debugLog)
        {
            Debug.Log(
                $"[BrushSkillBase] Skill is cooling down: {SkillType}, Remaining={CooldownRemaining:F2}s",
                this
            );
        }

        return false;
    }

    private void StartCooldown()
    {
        float cooldown = GetResolvedCooldown();

        if (cooldown <= 0f)
            return;

        nextUseAllowedTime = Time.unscaledTime + cooldown;

        if (debugLog)
        {
            Debug.Log(
                $"[BrushSkillBase] Cooldown started: {SkillType}, CD={cooldown:F2}s",
                this
            );
        }
    }

    protected virtual float GetCooldown()
    {
        return 0f;
    }

    private float GetResolvedCooldown()
    {
        float baseCooldown =
            Mathf.Max(
                0f,
                GetCooldown()
            );

        if (baseCooldown <= 0f)
            return 0f;

        float cooldownMultiplier =
            Mathf.Max(
                0f,
                GetSkillTreeStatValue(
                    SkillTreeStatType
                        .CooldownMultiplier,
                    1f
                )
            );

        return baseCooldown *
               cooldownMultiplier;
    }

    protected float GetSkillTreeStatValue(
        SkillTreeStatType statType,
        float fallbackValue
    )
    {
        if (skillTreeController == null)
        {
            ResolveSkillTreeController();
        }

        if (skillTreeController == null)
            return fallbackValue;

        return skillTreeController.GetStatValue(
            SkillType,
            statType,
            fallbackValue
        );
    }

    protected int GetSkillTreeLevel()
    {
        if (skillTreeController == null)
        {
            ResolveSkillTreeController();
        }

        if (skillTreeController == null)
            return 0;

        return skillTreeController.GetSkillLevel(
            SkillType
        );
    }

    private void ResolveSkillTreeController()
    {
        skillTreeController = null;

        GameObject searchObject =
            caster != null
                ? caster
                : gameObject;

        if (searchObject == null)
            return;

        skillTreeController =
            searchObject.GetComponent<
                PlayerSkillTreeController>();

        if (skillTreeController == null)
        {
            skillTreeController =
                searchObject.GetComponentInParent<
                    PlayerSkillTreeController>();
        }

        if (skillTreeController == null)
        {
            skillTreeController =
                searchObject.GetComponentInChildren<
                    PlayerSkillTreeController>(true);
        }
    }

    private void ResolveSkillUnlockReceiver()
    {
        skillUnlockReceiver = null;

        GameObject searchObject = caster != null
            ? caster
            : gameObject;

        if (searchObject == null)
            return;

        MonoBehaviour[] parentBehaviours =
            searchObject.GetComponentsInParent<MonoBehaviour>(true);

        for (int i = 0; i < parentBehaviours.Length; i++)
        {
            if (parentBehaviours[i] is IBrushSkillUnlockReceiver receiver)
            {
                skillUnlockReceiver = receiver;
                return;
            }
        }

        MonoBehaviour[] childBehaviours =
            searchObject.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < childBehaviours.Length; i++)
        {
            if (childBehaviours[i] is IBrushSkillUnlockReceiver receiver)
            {
                skillUnlockReceiver = receiver;
                return;
            }
        }
    }

    private bool CanUseUnlockedSkill()
    {
        if (!requireUnlockedSkill)
            return true;

        if (skillUnlockReceiver == null)
        {
            ResolveSkillUnlockReceiver();
        }

        if (skillUnlockReceiver == null)
        {
            Debug.LogWarning(
                $"[BrushSkillBase] Skill inventory not found. Skill blocked: {SkillType}",
                this
            );

            return false;
        }

        if (!skillUnlockReceiver.HasBrushSkill(SkillType))
        {
            Debug.Log(
                $"[BrushSkillBase] Player has not unlocked skill: {SkillType}",
                this
            );

            return false;
        }

        return true;
    }

    private void ResolveSkillCostReceiver()
    {
        skillCostReceiver = null;

        GameObject searchObject = caster != null
            ? caster
            : gameObject;

        if (searchObject == null)
            return;

        MonoBehaviour[] parentBehaviours =
            searchObject.GetComponentsInParent<MonoBehaviour>(true);

        for (int i = 0; i < parentBehaviours.Length; i++)
        {
            if (parentBehaviours[i] is IBrushSkillCostReceiver receiver)
            {
                skillCostReceiver = receiver;
                return;
            }
        }

        MonoBehaviour[] childBehaviours =
            searchObject.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < childBehaviours.Length; i++)
        {
            if (childBehaviours[i] is IBrushSkillCostReceiver receiver)
            {
                skillCostReceiver = receiver;
                return;
            }
        }
    }

    protected virtual int GetInkCost()
    {
        return 0;
    }

    private bool TryPayInkCost()
    {
        if (!requireInkCost)
            return true;

        int inkCost = Mathf.Max(0, GetInkCost());

        if (inkCost <= 0)
            return true;

        if (skillCostReceiver == null)
        {
            ResolveSkillCostReceiver();
        }

        if (skillCostReceiver == null)
        {
            Debug.LogWarning(
                $"[BrushSkillBase] Ink receiver not found. Skill blocked: {SkillType}",
                this
            );

            return false;
        }

        if (!skillCostReceiver.TryConsumeInk(inkCost))
        {
            Debug.Log(
                $"[BrushSkillBase] Not enough ink for skill: {SkillType}, Cost={inkCost}",
                this
            );

            return false;
        }

        return true;
    }

    protected abstract void Execute(
        BrushGestureResult result,
        BrushCastContext context
    );
}