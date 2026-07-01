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
    }
}