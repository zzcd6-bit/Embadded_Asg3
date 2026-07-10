using UnityEngine;

public abstract class BrushSkillBase : MonoBehaviour
{
    [Header("Skill Match")]
    public float minScore = 0.65f;

    [Header("Cast Context")]
    public BrushCastContextBuilder castContextBuilder;

    [Header("Caster")]
    public GameObject caster;

    [Header("技能获得限制")]
    public bool requireUnlockedSkill = true;

    private IBrushSkillUnlockReceiver skillUnlockReceiver;

    protected abstract BrushSkillType SkillType { get; }

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

        BrushCastContext context = null;

        if (castContextBuilder != null)
        {
            context = castContextBuilder.Build(result, caster);
        }

        Execute(result, context);
    }

    protected abstract void Execute(
        BrushGestureResult result,
        BrushCastContext context
    );
}