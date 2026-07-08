using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerLocomotion : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float gravity = -20f;

    [Header("Smooth")]
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 16f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float minJumpAirTime = 0.12f;

    [Header("Dodge")]
    [SerializeField] private float dodgeDistance = 3f;
    [SerializeField] private float dodgeDuration = 0.36f;
    [SerializeField] private float dodgeCooldown = 1f;
    [SerializeField] private float dodgeAnimationReturnDelay = 0.36f;

    [Header("Dodge / Combat Rule")]
    [SerializeField] private bool dodgeCanInterruptCombatAction = true;
    [SerializeField] private bool allowDodgeWhenMoveLockedByCombatAction = true;
    [SerializeField] private bool skipReturnToLocomotionWhenActionPlaying = true;

    [Header("External Facing")]
    [SerializeField] private float defaultExternalFaceLockTime = 0.15f;

    public bool CanMove { get; set; } = true;

    public bool IsDodging
    {
        get { return isDodging; }
    }

    public bool IsJumping
    {
        get { return isJumping; }
    }

    private CharacterController characterController;
    private PlayerInputReceiver inputReceiver;
    private PlayerAnimationController animationController;

    [Header("References")]
    [SerializeField] private PlayerPerfectDodgeController perfectDodgeController;
    [SerializeField] private ActionPlayer actionPlayer;

    private Transform mainCameraTransform;

    private float verticalVelocity;
    private float currentMoveAmount;

    private Vector3 lastMoveDirection;

    private bool initialized;

    private bool isJumping;
    private bool hasLeftGround;
    private float jumpTimer;

    private bool isDodging;
    private float dodgeTimer;
    private float dodgeAnimationTimer;
    private float lastDodgeTime = -999f;
    private Vector3 dodgeDirection;

    private float externalFaceLockTimer;

    public void Init(
        CharacterController controller,
        PlayerInputReceiver input,
        PlayerAnimationController animation
    )
    {
        characterController = controller;
        inputReceiver = input;
        animationController = animation;

        CacheReferences();

        lastMoveDirection = transform.forward;
        initialized = true;
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void CacheReferences()
    {
        if (perfectDodgeController == null)
        {
            perfectDodgeController = GetComponent<PlayerPerfectDodgeController>();
        }

        if (perfectDodgeController == null)
        {
            perfectDodgeController = GetComponentInChildren<PlayerPerfectDodgeController>();
        }

        if (actionPlayer == null)
        {
            actionPlayer = GetComponent<ActionPlayer>();
        }

        if (actionPlayer == null)
        {
            actionPlayer = GetComponentInChildren<ActionPlayer>();
        }
    }

    private void Update()
    {
        LocomotionUpdate();
    }

    private void LocomotionUpdate()
    {
        if (!initialized || characterController == null || inputReceiver == null)
        {
            return;
        }

        UpdateExternalFaceLockTimer();
        UpdateCameraReference();

        Vector2 input = inputReceiver.MoveInput;
        input = Vector2.ClampMagnitude(input, 1f);

        bool isGroundedBeforeMove = characterController.isGrounded;

        // 注意：
        // 不再完全依赖 CanMove 才处理输入。
        // 因为攻击时可能 CanMove=false，但我们仍然希望 Dodge 能取消攻击。
        HandleActionInput(input, isGroundedBeforeMove);

        ApplyGravity(isGroundedBeforeMove);

        if (isDodging)
        {
            UpdateDodge();
        }
        else
        {
            UpdateNormalMove(input);
        }

        UpdateJumpGroundState();
    }

    private void UpdateExternalFaceLockTimer()
    {
        if (externalFaceLockTimer <= 0f)
        {
            return;
        }

        externalFaceLockTimer -= Time.deltaTime;

        if (externalFaceLockTimer < 0f)
        {
            externalFaceLockTimer = 0f;
        }
    }

    private void HandleActionInput(Vector2 input, bool isGrounded)
    {
        if (inputReceiver.ConsumeJumpPressed())
        {
            // Jump 仍然需要 CanMove，避免攻击锁移动时跳跃。
            if (CanMove)
            {
                TryJump(isGrounded);
            }
        }

        if (inputReceiver.ConsumeDodgePressed())
        {
            TryDodge(input, isGrounded);
        }
    }

    private void TryJump(bool isGrounded)
    {
        if (!isGrounded)
        {
            return;
        }

        if (isDodging)
        {
            return;
        }

        if (isJumping)
        {
            return;
        }

        isJumping = true;
        hasLeftGround = false;
        jumpTimer = 0f;

        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        animationController?.SetGrounded(false);
        animationController?.PlayJump();
    }

    private void UpdateJumpGroundState()
    {
        bool isGroundedAfterMove = characterController.isGrounded;

        animationController?.SetGrounded(isGroundedAfterMove);

        if (!isJumping)
        {
            return;
        }

        jumpTimer += Time.deltaTime;

        if (!isGroundedAfterMove)
        {
            hasLeftGround = true;
        }

        bool canLand =
            hasLeftGround &&
            isGroundedAfterMove &&
            jumpTimer >= minJumpAirTime &&
            verticalVelocity <= 0f;

        if (!canLand)
        {
            return;
        }

        isJumping = false;
        hasLeftGround = false;
        jumpTimer = 0f;

        if (skipReturnToLocomotionWhenActionPlaying && IsCombatActionPlaying())
        {
            return;
        }

        bool hasMoveInput = inputReceiver.MoveInput.magnitude > 0.1f;
        animationController?.ReturnToLocomotion(hasMoveInput);
    }

    private void TryDodge(Vector2 input, bool isGrounded)
    {
        if (!isGrounded)
        {
            return;
        }

        if (isDodging || isJumping)
        {
            return;
        }

        if (Time.time - lastDodgeTime < dodgeCooldown)
        {
            return;
        }

        bool combatActionPlaying = IsCombatActionPlaying();

        // 如果 CanMove=false，但是当前是攻击动作，可以允许 Dodge 取消攻击。
        if (!CanMove)
        {
            bool canDodgeByCancelAction =
                allowDodgeWhenMoveLockedByCombatAction &&
                combatActionPlaying;

            if (!canDodgeByCancelAction)
            {
                return;
            }
        }

        Vector3 moveDirection = GetCameraRelativeMoveDirection(input);

        if (moveDirection.sqrMagnitude <= 0.0001f)
        {
            moveDirection = transform.forward;
        }

        dodgeDirection = moveDirection.normalized;
        lastMoveDirection = dodgeDirection;

        externalFaceLockTimer = 0f;
        RotateToMoveDirection(dodgeDirection);

        // 关键：
        // 如果攻击中允许 Dodge 取消攻击，这里要先清理 ActionPlayer 状态。
        if (dodgeCanInterruptCombatAction && combatActionPlaying)
        {
            InterruptCombatAction();
        }

        isDodging = true;
        dodgeTimer = 0f;
        dodgeAnimationTimer = 0f;
        lastDodgeTime = Time.time;

        currentMoveAmount = 0f;

        perfectDodgeController?.BeginDodgeWindow();

        animationController?.PlayDodge();
    }

    private void UpdateDodge()
    {
        dodgeTimer += Time.deltaTime;
        dodgeAnimationTimer += Time.deltaTime;

        if (dodgeTimer <= dodgeDuration)
        {
            float dodgeSpeed = dodgeDistance / dodgeDuration;

            Vector3 dodgeMove =
                dodgeDirection * dodgeSpeed +
                Vector3.up * verticalVelocity;

            characterController.Move(dodgeMove * Time.deltaTime);
        }
        else
        {
            Vector3 fallMove = Vector3.up * verticalVelocity;
            characterController.Move(fallMove * Time.deltaTime);
        }

        if (dodgeAnimationTimer >= dodgeAnimationReturnDelay)
        {
            FinishDodge();
        }
    }

    private void FinishDodge()
    {
        if (!isDodging)
        {
            return;
        }

        isDodging = false;

        // 关键：
        // 如果 Dodge 结束这一刻，攻击动作已经开始播放，
        // 就不要强制 ReturnToLocomotion，否则会覆盖攻击动作。
        if (skipReturnToLocomotionWhenActionPlaying && IsCombatActionPlaying())
        {
            return;
        }

        bool hasMoveInput = inputReceiver.MoveInput.magnitude > 0.1f;
        animationController?.ReturnToLocomotion(hasMoveInput);
    }

    private void UpdateNormalMove(Vector2 input)
    {
        if (!CanMove)
        {
            currentMoveAmount = 0f;

            Vector3 verticalMove = Vector3.up * verticalVelocity;
            characterController.Move(verticalMove * Time.deltaTime);

            return;
        }

        bool hasMoveInput = input.magnitude > 0.1f;

        float targetMoveAmount = hasMoveInput ? 1f : 0f;

        float smoothSpeed = targetMoveAmount > currentMoveAmount
            ? acceleration
            : deceleration;

        currentMoveAmount = Mathf.MoveTowards(
            currentMoveAmount,
            targetMoveAmount,
            smoothSpeed * Time.deltaTime
        );

        Vector3 moveDirection = GetCameraRelativeMoveDirection(input);

        if (hasMoveInput && moveDirection.sqrMagnitude > 0.0001f)
        {
            lastMoveDirection = moveDirection;
        }

        Vector3 horizontalDirection = hasMoveInput
            ? moveDirection
            : lastMoveDirection;

        Vector3 finalMove =
            horizontalDirection * moveSpeed * currentMoveAmount +
            Vector3.up * verticalVelocity;

        characterController.Move(finalMove * Time.deltaTime);

        if (hasMoveInput && externalFaceLockTimer <= 0f)
        {
            RotateToMoveDirection(moveDirection);
        }

        if (!isJumping && !isDodging)
        {
            animationController?.SetLocomotion(currentMoveAmount, hasMoveInput);
        }
    }

    private bool IsCombatActionPlaying()
    {
        return actionPlayer != null && actionPlayer.IsActionPlaying;
    }

    private void InterruptCombatAction()
    {
        if (actionPlayer == null)
        {
            return;
        }

        actionPlayer.InterruptAction();
    }

    private void UpdateCameraReference()
    {
        if (mainCameraTransform != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            mainCameraTransform = mainCamera.transform;
        }
    }

    private Vector3 GetCameraRelativeMoveDirection(Vector2 input)
    {
        if (input.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        if (mainCameraTransform == null)
        {
            return new Vector3(input.x, 0f, input.y);
        }

        Vector3 cameraForward = mainCameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = mainCameraTransform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Vector3 moveDirection =
            cameraForward * input.y +
            cameraRight * input.x;

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        return moveDirection;
    }

    private void ApplyGravity(bool isGrounded)
    {
        if (isGrounded && verticalVelocity < 0f && !isJumping)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void RotateToMoveDirection(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    public void FaceDirectionInstant(Vector3 direction)
    {
        FaceDirectionInstant(direction, defaultExternalFaceLockTime);
    }

    public void FaceDirectionInstant(Vector3 direction, float lockMoveRotationTime)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        if (lockMoveRotationTime > 0f)
        {
            externalFaceLockTimer = Mathf.Max(externalFaceLockTimer, lockMoveRotationTime);
        }
    }

    public void FaceTargetInstant(Transform target, float lockMoveRotationTime)
    {
        if (target == null)
        {
            return;
        }

        Vector3 direction = target.position - transform.position;
        FaceDirectionInstant(direction, lockMoveRotationTime);
    }
}