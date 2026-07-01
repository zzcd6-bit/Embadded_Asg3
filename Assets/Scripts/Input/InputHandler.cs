using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance { get; private set; }

    [Header("Movement")]
    [SerializeField] private string horizontalAxis = "Horizontal";
    [SerializeField] private string verticalAxis = "Vertical";
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;
    [SerializeField] private string jumpButton = "Jump";

    [Header("Cursor")]
    [SerializeField] private KeyCode releaseCursorKey = KeyCode.LeftAlt;
    [SerializeField] private KeyCode alternateReleaseCursorKey = KeyCode.RightAlt;

    [Header("Placement")]
    [SerializeField] private int placementConfirmMouseButton = 0;
    [SerializeField] private int placementCancelMouseButton = 1;
    [SerializeField] private KeyCode placementRotateKey = KeyCode.R;
    [SerializeField] private KeyCode placementDeleteKey = KeyCode.E;

    [Header("Build Mode")]
    [SerializeField] private KeyCode buildModeToggleKey = KeyCode.Tab;

    public Vector2 MoveInput { get; private set; }
    public Vector3 MousePosition { get; private set; }
    public bool RunHeld { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool ReleaseCursorHeld { get; private set; }
    public bool PlacementConfirmPressed { get; private set; }
    public bool PlacementCancelPressed { get; private set; }
    public bool PlacementRotatePressed { get; private set; }
    public bool PlacementDeletePressed { get; private set; }
    public bool BuildModeTogglePressed { get; private set; }

    // Finds the active input handler or creates one when the scene has none.
    public static InputHandler GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        InputHandler existing = FindAnyObjectByType<InputHandler>();
        if (existing != null)
        {
            return existing;
        }

        GameObject inputObject = new("InputHandler");
        return inputObject.AddComponent<InputHandler>();
    }

    // Registers the singleton instance and removes duplicate handlers.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Samples legacy Unity input once per frame and exposes stable input state.
    private void Update()
    {
        MoveInput = new Vector2(Input.GetAxis(horizontalAxis), Input.GetAxis(verticalAxis));
        MousePosition = Input.mousePosition;
        RunHeld = Input.GetKey(runKey);
        JumpPressed = Input.GetButtonDown(jumpButton);
        ReleaseCursorHeld = Input.GetKey(releaseCursorKey) || Input.GetKey(alternateReleaseCursorKey);
        PlacementConfirmPressed = Input.GetMouseButtonDown(placementConfirmMouseButton);
        PlacementCancelPressed = Input.GetMouseButtonDown(placementCancelMouseButton);
        PlacementRotatePressed = Input.GetKeyDown(placementRotateKey);
        PlacementDeletePressed = Input.GetKeyDown(placementDeleteKey);
        BuildModeTogglePressed = Input.GetKeyDown(buildModeToggleKey);
    }

    // Clears the singleton reference when this handler is destroyed.
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
