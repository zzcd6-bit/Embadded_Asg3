using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_EventType
{
    #region Game Mode
    E_GameMode_Changed,
    #endregion

    #region Scene Events
    E_SceneLoadChange,
    #endregion

    #region Input Axis
    E_Input_Horizontal,
    E_Input_Vertical,
    #endregion

    #region New Player Input
    E_Input_Jump,
    E_Input_Dodge,
    E_Input_Attack,
    E_Input_LockOn,
    E_Input_CharacterPanel,
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

    #region Ye Build Placement Input Bridge
    // Ye build placement input bridge: events routed through InputMgr so build mode can coexist with ZhongZhengchao input.
    E_Build_TestStartItem1,
    E_Build_PlacementConfirm,
    E_Build_PlacementCancel,
    E_Build_PlacementRotate,
    E_Build_PlacementDelete,
    #endregion

    #region Ye Interaction Input Bridge
    // Ye interaction input bridge: additive events for Ye's stable roll-box interaction system.
    E_Interaction_ExecutePrimary,
    E_Interaction_ExecuteAlternative,
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
    E_Player_Teleported,
    #endregion

    #region Enemy
    E_Enemy_Spawned,
    E_Enemy_Dead,
    E_Enemy_Damaged,
    E_Enemy_CombatChanged,
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
