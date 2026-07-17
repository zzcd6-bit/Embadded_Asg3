using System;
using UnityEngine;

public class PlayerInputReceiver : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }

    public event Action AttackPressed;
    public event Action LockOnPressed;

    private bool jumpPressed;

    private EventCenter cachedEventCenter;
    private InputMgr cachedInputMgr;

    private void OnEnable()
    {
        cachedInputMgr = InputMgr.Instance;
        cachedEventCenter = EventCenter.Instance;

        if (cachedInputMgr == null)
        {
            Debug.LogError("[PlayerInputReceiver] InputMgr.Instance is null.");
            return;
        }

        if (cachedEventCenter == null)
        {
            Debug.LogError("[PlayerInputReceiver] EventCenter.Instance is null.");
            return;
        }

        cachedInputMgr.ChangeKeyboardInfo(
            E_EventType.E_Input_Jump,
            KeyCode.Space,
            InputInfo.E_InputType.Down
        );

        cachedInputMgr.ChangeKeyboardInfo(
            E_EventType.E_Input_Attack,
            KeyCode.Mouse0,
            InputInfo.E_InputType.Down
        );

        cachedInputMgr.ChangeKeyboardInfo(
            E_EventType.E_Input_LockOn,
            KeyCode.Mouse2,
            InputInfo.E_InputType.Down
        );

        cachedInputMgr.StartOrCloseInputMgr(true);

        cachedEventCenter.AddEventListener<float>(
            E_EventType.E_Input_Horizontal,
            OnHorizontalInput
        );

        cachedEventCenter.AddEventListener<float>(
            E_EventType.E_Input_Vertical,
            OnVerticalInput
        );

        cachedEventCenter.AddEventListener(
            E_EventType.E_Input_Jump,
            OnJumpInput
        );

        cachedEventCenter.AddEventListener(
            E_EventType.E_Input_Attack,
            OnAttackInput
        );

        cachedEventCenter.AddEventListener(
            E_EventType.E_Input_LockOn,
            OnLockOnInput
        );
    }

    private void OnDisable()
    {
        if (cachedEventCenter != null)
        {
            cachedEventCenter.RemoveEventListener<float>(
                E_EventType.E_Input_Horizontal,
                OnHorizontalInput
            );

            cachedEventCenter.RemoveEventListener<float>(
                E_EventType.E_Input_Vertical,
                OnVerticalInput
            );

            cachedEventCenter.RemoveEventListener(
                E_EventType.E_Input_Jump,
                OnJumpInput
            );

            cachedEventCenter.RemoveEventListener(
                E_EventType.E_Input_Attack,
                OnAttackInput
            );

            cachedEventCenter.RemoveEventListener(
                E_EventType.E_Input_LockOn,
                OnLockOnInput
            );
        }

        AttackPressed = null;
        LockOnPressed = null;

        cachedEventCenter = null;
        cachedInputMgr = null;
    }

    private void OnHorizontalInput(float value)
    {
        if (!GameModeManager.Instance.CurrentCapabilities.canMove)
        {
            value = 0f;
        }

        MoveInput = new Vector2(value, MoveInput.y);
    }

    private void OnVerticalInput(float value)
    {
        if (!GameModeManager.Instance.CurrentCapabilities.canMove)
        {
            value = 0f;
        }

        MoveInput = new Vector2(MoveInput.x, value);
    }

    private void OnJumpInput()
    {
        if (!GameModeManager.Instance.CurrentCapabilities.canMove)
        {
            return;
        }

        jumpPressed = true;
    }

    private void OnAttackInput()
    {
        if (!GameModeManager.Instance.CurrentCapabilities.canAttack)
        {
            return;
        }

        AttackPressed?.Invoke();
    }

    private void OnLockOnInput()
    {
        if (!GameModeManager.Instance.CurrentCapabilities.canLockOn)
        {
            return;
        }

        LockOnPressed?.Invoke();
    }

    public bool ConsumeJumpPressed()
    {
        if (!jumpPressed)
        {
            return false;
        }

        jumpPressed = false;
        return true;
    }
}
