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
    [SerializeField] private InputHandler inputHandler;

    private Animator anim;
    private CharacterController CC;
    private bool cursorReleased;

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

        if (inputHandler == null)
        {
            inputHandler = InputHandler.GetOrCreate();
        }
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
        HandleCursorReleaseInput();

        if (!hasControl)
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

        Vector2 moveInput = inputHandler != null ? inputHandler.MoveInput : Vector2.zero;
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

        bool isRunning = inputHandler != null && inputHandler.RunHeld;
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
        bool shouldReleaseCursor = inputHandler != null && inputHandler.ReleaseCursorHeld;
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
            SetCursorReleased(inputHandler != null && inputHandler.ReleaseCursorHeld);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.TransformPoint(surfaceCheckOffset), surfaceCheckRadius);
    }
}
