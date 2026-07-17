using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;

    [Header("Movement")]
    public float movementSpeed = 3f;
    public float rotSpeed = 40f;
    public Camera cam;

    public bool hasControl = true;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnStart = true;
    [SerializeField] private KeyCode releaseCursorKey = KeyCode.LeftAlt;
    [SerializeField] private KeyCode alternateReleaseCursorKey = KeyCode.RightAlt;
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;

    private Animator anim;
    private CharacterController CC;
    private bool cursorReleased;
    private Vector2 moveInput;

    [Header("Surface Check")]
    public float surfaceCheckRadius = 0.1f;
    public Vector3 surfaceCheckOffset;
    public LayerMask surfaceLayer;
    public bool onSurface;

    [Header("Gravity")]
    [SerializeField] private float fallingSpeed;

    private void Awake()
    {
        instance = this;

        anim = GetComponent<Animator>();
        CC = GetComponent<CharacterController>();

        if (cam == null)
        {
            cam = Camera.main;
        }

        InputMgr.Instance.StartOrCloseInputMgr(true);
    }

    private void OnEnable()
    {
        // Ye input cleanup: receive movement from ZhongZhengchao's InputMgr/EventCenter.
        EventCenter.Instance.AddEventListener<float>(E_EventType.E_Input_Horizontal, OnHorizontalInput);
        EventCenter.Instance.AddEventListener<float>(E_EventType.E_Input_Vertical, OnVerticalInput);
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<float>(E_EventType.E_Input_Horizontal, OnHorizontalInput);
        EventCenter.Instance.RemoveEventListener<float>(E_EventType.E_Input_Vertical, OnVerticalInput);
    }

    private void Start()
    {
        if (lockCursorOnStart)
        {
            SetCursorReleased(false);
        }
    }

    private void Update()
    {
        if (GameModeManager.Instance.CurrentCapabilities.canLook)
        {
            HandleCursorReleaseInput();
        }

        if (!hasControl || !GameModeManager.Instance.CurrentCapabilities.canMove)
        {
            if (anim != null)
            {
                anim.SetFloat("Speed", 0f, 0.15f, Time.deltaTime);
            }

            return;
        }

        SurfaceCheck();
        PlayerMovement();
    }

    void PlayerMovement()
    {
        if (cam == null || CC == null) return;

        float horizontal = moveInput.x;
        float vertical = moveInput.y;

        Vector3 camForward = cam.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cam.transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 movementDirection = camForward * vertical + camRight * horizontal;
        float movementAmount = Mathf.Clamp01(movementDirection.magnitude);

        if (onSurface)
        {
            fallingSpeed = -1f;
        }
        else
        {
            fallingSpeed += Physics.gravity.y * Time.deltaTime;
        }

        bool isRunning = Input.GetKey(runKey);
        float currentSpeed = isRunning ? movementSpeed * 2f : movementSpeed;
        float targetBlend = movementAmount * (isRunning ? 1f : 0.5f);

        Vector3 finalMove = movementDirection.normalized * currentSpeed;
        finalMove.y = fallingSpeed;

        CC.Move(finalMove * Time.deltaTime);

        if (movementAmount > 0.01f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                lookRotation,
                rotSpeed * Time.deltaTime
            );
        }

        if (anim != null)
        {
            anim.SetFloat("Speed", targetBlend, 0.2f, Time.deltaTime);
        }
    }

    void SurfaceCheck()
    {
        onSurface = Physics.CheckSphere(
            transform.TransformPoint(surfaceCheckOffset),
            surfaceCheckRadius,
            surfaceLayer
        );
    }

    public void SetControl(bool hasControl)
    {
        this.hasControl = hasControl;
    }

    private void HandleCursorReleaseInput()
    {
        bool shouldReleaseCursor = Input.GetKey(releaseCursorKey) || Input.GetKey(alternateReleaseCursorKey);
        if (cursorReleased != shouldReleaseCursor)
        {
            SetCursorReleased(shouldReleaseCursor);
        }
    }

    private void SetCursorReleased(bool released)
    {
        cursorReleased = released;
        Cursor.lockState = released ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = released;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && lockCursorOnStart)
        {
            SetCursorReleased(Input.GetKey(releaseCursorKey) || Input.GetKey(alternateReleaseCursorKey));
        }
    }

    private void OnHorizontalInput(float value)
    {
        moveInput = new Vector2(value, moveInput.y);
    }

    private void OnVerticalInput(float value)
    {
        moveInput = new Vector2(moveInput.x, value);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.TransformPoint(surfaceCheckOffset), surfaceCheckRadius);
    }
}
