using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_EventType
{
    #region Scene Events
    E_SceneLoadChange,
    #endregion

    #region Input Axis
    E_Input_Horizontal,
    E_Input_Vertical,
    #endregion

    #region New Player Input
    E_Input_Jump,
    E_Input_Attack,
    E_Input_LockOn,
    #endregion

    #region Old Player / Brush Compatibility
    E_Player_Jump,
    E_Player_NormalAttack,
    E_Player_ControlEnable,
    E_Player_CombatEnable,
    #endregion

    #region Camera
    E_Camera_InputEnable,
    #endregion

    #region Brush Input
    E_Brush_Enter,
    E_Brush_Exit,
    E_Brush_ModeChanged,
    E_Brush_RequestExit,
    #endregion

    #region Brush Result
    E_Brush_DrawStart,
    E_Brush_DrawEnd,
    E_Brush_StrokeFinished,
    E_Brush_GestureRecognized,
    E_Brush_Slash,
    E_Brush_FireTransfer,
    #endregion

    #region New Player Combat Events
    E_Player_Damaged,
    E_Player_Dead,
    #endregion

    #region Enemy
    E_Enemy_Spawned,
    E_Enemy_Dead,
    E_Enemy_Damaged,
    #endregion

    #region Quest
    E_Quest_Accepted,
    E_Quest_Updated,
    E_Quest_Completed,
    E_Quest_Rewarded,
    #endregion

    #region Dialogue
    E_Dialogue_Started,
    E_Dialogue_Ended,
    #endregion

    #region UI
    E_UI_ShowDamageNumber,
    #endregion

    #region Audio
    E_Audio_PlaySFX,
    #endregion
}