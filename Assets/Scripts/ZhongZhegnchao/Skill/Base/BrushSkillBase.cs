using UnityEngine;

public abstract class BrushSkillBase : MonoBehaviour
{
    [Header("Skill Match")]
    public float minScore = 0.65f;

    [Header("Cast Context")]
    public BrushCastContextBuilder castContextBuilder;

    [Header("Caster")]
    public GameObject caster;

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