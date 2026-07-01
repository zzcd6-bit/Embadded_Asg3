using UnityEngine;

namespace LegacyPlayer
{
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 12f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;

    private CharacterController characterController;
    private Vector3 moveInput;
    private Vector3 velocity;
    private bool canControl = true;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener<float>(E_EventType.E_Input_Horizontal, OnHorizontalInput);
        EventCenter.Instance.AddEventListener<float>(E_EventType.E_Input_Vertical, OnVerticalInput);
        EventCenter.Instance.AddEventListener(E_EventType.E_Player_Jump, OnJumpInput);
        EventCenter.Instance.AddEventListener<bool>(E_EventType.E_Player_ControlEnable,OnControlEnable);
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<float>(E_EventType.E_Input_Horizontal, OnHorizontalInput);
        EventCenter.Instance.RemoveEventListener<float>(E_EventType.E_Input_Vertical, OnVerticalInput);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Player_Jump, OnJumpInput);
        EventCenter.Instance.RemoveEventListener<bool>(E_EventType.E_Player_ControlEnable,OnControlEnable);
    }

    private void Update()
    {
        Move();
        ApplyGravity();
    }

    private void OnHorizontalInput(float value)
    {
        moveInput.x = value;
    }

    private void OnVerticalInput(float value)
    {
        moveInput.z = value;
    }

    private void OnControlEnable(bool state)
    {
        SetControlEnabled(state);
    }

    private void OnJumpInput()
    {
        if (!canControl)
            return;

        if (IsGrounded())
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void Move()
    {
        if (!canControl)
            return;

        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.z);

        if (inputDir.magnitude > 1f)
            inputDir.Normalize();

        if (inputDir.sqrMagnitude < 0.01f)
            return;

        Transform cam = Camera.main.transform;

        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x;

        if (moveDir.magnitude > 1f)
            moveDir.Normalize();

        characterController.Move(moveDir * moveSpeed * Time.deltaTime);

        Quaternion targetRot = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );
    }

    private void ApplyGravity()
    {
        if (IsGrounded() && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
            return characterController.isGrounded;

        return Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    public void SetControlEnabled(bool state)
    {
        canControl = state;

        if (!state)
        {
            moveInput = Vector3.zero;
        }
    }
}
}
