using UnityEngine;

public class GameEntry : MonoBehaviour
{
    private void Awake()
    {
        // 开启 Framework 输入检测
        InputMgr.Instance.StartOrCloseInputMgr(true);

        // 玩家输入
        InputMgr.Instance.ChangeKeyboardInfo(
            E_EventType.E_Player_Jump,
            KeyCode.Space,
            InputInfo.E_InputType.Down
        );

        InputMgr.Instance.ChangeMouseInfo(
            E_EventType.E_Player_NormalAttack,
            0,
            InputInfo.E_InputType.Down
        );

        // Brush Mode 输入
        InputMgr.Instance.ChangeMouseInfo(
            E_EventType.E_Brush_Enter,
            1,
            InputInfo.E_InputType.Down
        );

        InputMgr.Instance.ChangeMouseInfo(
            E_EventType.E_Brush_Exit,
            1,
            InputInfo.E_InputType.Up
        );

        InputMgr.Instance.ChangeMouseInfo(
            E_EventType.E_Brush_DrawEnd,
            0,
            InputInfo.E_InputType.Up
        );
        // Ye build placement input bridge: register rune placement test controls in the existing InputMgr.
        InputMgr.Instance.ChangeKeyboardInfo(
            E_EventType.E_Build_StartItem1,
            KeyCode.Alpha1,
            InputInfo.E_InputType.Down
        );

        InputMgr.Instance.ChangeMouseInfo(
            E_EventType.E_Build_PlacementConfirm,
            0,
            InputInfo.E_InputType.Down
        );

        InputMgr.Instance.ChangeMouseInfo(
            E_EventType.E_Build_PlacementCancel,
            1,
            InputInfo.E_InputType.Down
        );

        InputMgr.Instance.ChangeKeyboardInfo(
            E_EventType.E_Build_PlacementRotate,
            KeyCode.R,
            InputInfo.E_InputType.Down
        );

        InputMgr.Instance.ChangeKeyboardInfo(
            E_EventType.E_Build_PlacementDelete,
            KeyCode.E,
            InputInfo.E_InputType.Down
        );
    }
}