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

    #region Player Input
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
}