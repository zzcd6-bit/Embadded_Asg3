using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Camera Target")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private bool detachTargetFromPlayer = true;

    [Header("Target Y Lock")]
    [SerializeField] private float fixedWorldY = 1.6f;
    [SerializeField] private bool followJumpY = true;
    [SerializeField] private float jumpYFollowSpeed = 8f;
    [SerializeField] private float jumpYDeadZone = 0.05f;
    [SerializeField] private float maxJumpYFollow = 2.5f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 180f;
    [SerializeField] private bool lockCursor = true;

    private bool cameraInputEnabled = true;
    private float yaw;
    private float basePlayerY;
    private float currentTargetY;

    private bool initialized;
    private bool ownsDetachedTarget;

    public void Init(Transform target)
    {
        cameraTarget = target;

        if (cameraTarget == null)
        {
            Debug.LogError("[PlayerCameraController] CameraTarget is null.");
            return;
        }

        basePlayerY = transform.position.y;
        currentTargetY = fixedWorldY;

        if (detachTargetFromPlayer)
        {
            if (cameraTarget.parent == transform)
            {
                ownsDetachedTarget = true;
            }

            cameraTarget.SetParent(null);
        }

        Vector3 playerPos = transform.position;

        cameraTarget.position = new Vector3(
            playerPos.x,
            currentTargetY,
            playerPos.z
        );

        yaw = transform.eulerAngles.y;

        initialized = true;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Debug.Log("[PlayerCameraController] Init complete.");
    }

    private void LateUpdate()
    {
        if (!initialized || cameraTarget == null)
        {
            return;
        }

        UpdateTargetPosition();
        UpdateTargetRotation();
    }

    private void OnDestroy()
    {
        if (ownsDetachedTarget && cameraTarget != null)
        {
            Destroy(cameraTarget.gameObject);
            cameraTarget = null;
        }
    }

    public void SetCameraInputEnabled(bool enabled)
    {
        cameraInputEnabled = enabled;
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
    private void UpdateTargetPosition()
    {
        Vector3 playerPos = transform.position;

        float targetY = fixedWorldY;

        if (followJumpY)
        {
            float jumpOffset = playerPos.y - basePlayerY;

            if (jumpOffset < jumpYDeadZone)
            {
                jumpOffset = 0f;
            }

            jumpOffset = Mathf.Clamp(jumpOffset, 0f, maxJumpYFollow);

            targetY = fixedWorldY + jumpOffset;
        }

        currentTargetY = Mathf.Lerp(
            currentTargetY,
            targetY,
            jumpYFollowSpeed * Time.deltaTime
        );

        cameraTarget.position = new Vector3(
            playerPos.x,
            currentTargetY,
            playerPos.z
        );
    }

    private void UpdateTargetRotation()
    {
        if (!cameraInputEnabled)
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X");

        yaw += mouseX * mouseSensitivity * Time.deltaTime;

        cameraTarget.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}