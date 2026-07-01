using UnityEngine;

public abstract class BrushSkillBase : MonoBehaviour
{
    [Header("Gesture Match")]
    public string[] gestureNames;
    public float minScore = 0.65f;

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

        if (!IsMatchedGesture(result.gestureName))
            return;

        Execute(result);
    }

    protected bool IsMatchedGesture(string gestureName)
    {
        if (gestureNames == null || gestureNames.Length == 0)
            return false;

        for (int i = 0; i < gestureNames.Length; i++)
        {
            if (gestureNames[i] == gestureName)
                return true;
        }

        return false;
    }

    protected abstract void Execute(BrushGestureResult result);
}