using UnityEngine;

public class BrushCursorController : MonoBehaviour
{
    [Header("References")]
    public Camera brushLineCamera;
    public Transform drawPlane;
    public Transform cursorRoot;
    public Animator brushAnimator;

    [Header("Animator Parameters")]
    public string isDrawingParam = "IsDrawing";
    public string moveXParam = "MoveX";
    public string moveYParam = "MoveY";

    [Header("Cursor Settings")]
    public bool alignToCamera = true;

    [Header("Brush Rotation Offset")]
    public Vector3 rotationOffset = new Vector3(25f, 0f, -35f);

    private bool isBrushMode;
    private bool isDrawing;

    private Vector2 lastMousePosition;
    private Vector2 currentAnimMove;

    private void Awake()
    {
        if (cursorRoot != null)
            cursorRoot.gameObject.SetActive(false);

        if (brushAnimator != null)
            brushAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener<bool>(
            E_EventType.E_Brush_ModeChanged,
            OnBrushModeChanged
        );

        EventCenter.Instance.AddEventListener(
            E_EventType.E_Brush_DrawStart,
            OnDrawStart
        );

        EventCenter.Instance.AddEventListener(
            E_EventType.E_Brush_DrawEnd,
            OnDrawEnd
        );
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<bool>(
            E_EventType.E_Brush_ModeChanged,
            OnBrushModeChanged
        );

        EventCenter.Instance.RemoveEventListener(
            E_EventType.E_Brush_DrawStart,
            OnDrawStart
        );

        EventCenter.Instance.RemoveEventListener(
            E_EventType.E_Brush_DrawEnd,
            OnDrawEnd
        );
    }

    private void Update()
    {
        if (!isBrushMode)
            return;

        UpdateCursorPosition();
        UpdateAnimatorByMouseMovement();
    }

    private void OnBrushModeChanged(bool state)
    {
        isBrushMode = state;
        isDrawing = false;

        if (cursorRoot != null)
            cursorRoot.gameObject.SetActive(state);

        lastMousePosition = Input.mousePosition;
        currentAnimMove = Vector2.zero;

        SetAnimatorDrawing(false);
        SetAnimatorMove(Vector2.zero);
    }

    private void OnDrawStart()
    {
        if (!isBrushMode)
            return;

        isDrawing = true;

        if (brushAnimator != null)
            brushAnimator.SetBool(isDrawingParam, true);
    }

    private void OnDrawEnd()
    {
        if (!isBrushMode)
            return;

        isDrawing = false;

        if (brushAnimator != null)
        {
            brushAnimator.SetBool(isDrawingParam, false);
            brushAnimator.SetFloat(moveXParam, 0f);
            brushAnimator.SetFloat(moveYParam, 0f);
        }
    }

    private void UpdateCursorPosition()
    {
        if (cursorRoot == null)
            return;

        if (TryGetMouseWorldPoint(out Vector3 worldPoint))
        {
            cursorRoot.position = worldPoint;

            if (alignToCamera && brushLineCamera != null)
            {
                cursorRoot.rotation =
                    brushLineCamera.transform.rotation *
                    Quaternion.Euler(rotationOffset);
            }
        }
    }

    private void UpdateAnimatorByMouseMovement()
    {
        if (brushAnimator == null)
            return;

        brushAnimator.SetBool(isDrawingParam, isDrawing);

        float targetX = 0f;
        float targetY = 0f;

        if (isDrawing)
        {
            targetX = Mathf.Clamp(Input.GetAxis("Mouse X") * 3f, -1f, 1f);
            targetY = Mathf.Clamp(Input.GetAxis("Mouse Y") * 3f, -1f, 1f);
        }

        brushAnimator.SetFloat(
            moveXParam,
            Mathf.Lerp(brushAnimator.GetFloat(moveXParam), targetX, 0.07f)
        );

        brushAnimator.SetFloat(
            moveYParam,
            Mathf.Lerp(brushAnimator.GetFloat(moveYParam), targetY, 0.07f)
        );
    }

    private void SetAnimatorDrawing(bool state)
    {
        if (brushAnimator == null)
            return;

        brushAnimator.SetBool(isDrawingParam, state);
    }

    private void SetAnimatorMove(Vector2 move)
    {
        if (brushAnimator == null)
            return;

        brushAnimator.SetFloat(moveXParam, move.x);
        brushAnimator.SetFloat(moveYParam, move.y);
    }

    private bool TryGetMouseWorldPoint(out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;

        if (brushLineCamera == null || drawPlane == null)
            return false;

        Ray ray = brushLineCamera.ScreenPointToRay(Input.mousePosition);

        Plane plane = new Plane(
            -brushLineCamera.transform.forward,
            drawPlane.position
        );

        if (plane.Raycast(ray, out float enter))
        {
            worldPoint = ray.GetPoint(enter);
            return true;
        }

        return false;
    }
}