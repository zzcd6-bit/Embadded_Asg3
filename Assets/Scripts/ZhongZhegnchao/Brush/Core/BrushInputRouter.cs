using UnityEngine;

public class BrushInputRouter : MonoBehaviour
{
    private bool isBrushMode;

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener<bool>(
            E_EventType.E_Brush_ModeChanged,
            OnBrushModeChanged
        );

        EventCenter.Instance.AddEventListener(
            E_EventType.E_Player_NormalAttack,
            OnLeftMouseDown
        );
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<bool>(
            E_EventType.E_Brush_ModeChanged,
            OnBrushModeChanged
        );

        EventCenter.Instance.RemoveEventListener(
            E_EventType.E_Player_NormalAttack,
            OnLeftMouseDown
        );
    }

    private void OnBrushModeChanged(bool state)
    {
        isBrushMode = state;
    }

    private void OnLeftMouseDown()
    {
        if (!isBrushMode ||
            !GameModeManager.Instance.CurrentCapabilities.canDrawBrush)
            return;

        EventCenter.Instance.EventTrigger(E_EventType.E_Brush_DrawStart);
    }
}
