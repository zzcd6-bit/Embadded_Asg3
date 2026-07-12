using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Camera Target")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private bool detachTargetFromPlayer = true;

    [Header("Target Height Follow")]
    [Tooltip("CameraTarget 相对 Player 的高度，例如 1.6 表示在角色头部附近")]
    [SerializeField] private float targetHeightOffset = 1.6f;

    [Tooltip("是否跟随 Player 的 Y 高度。上桥、上坡、下坡建议开启")]
    [SerializeField] private bool followPlayerY = true;

    [SerializeField] private float yFollowSpeed = 12f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 180f;
    [SerializeField] private bool lockCursor = true;

    private bool cameraInputEnabled = true;
    private float yaw;
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

        if (detachTargetFromPlayer)
        {
            if (cameraTarget.parent == transform)
            {
                ownsDetachedTarget = true;
            }

            cameraTarget.SetParent(null);
        }

        Vector3 playerPos = transform.position;

        currentTargetY = playerPos.y + targetHeightOffset;

        cameraTarget.position = new Vector3(
            playerPos.x,
            currentTargetY,
            playerPos.z
        );

        yaw = transform.eulerAngles.y;
        cameraTarget.rotation = Quaternion.Euler(0f, yaw, 0f);

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

        float targetY;

        if (followPlayerY)
        {
            targetY = playerPos.y + targetHeightOffset;
        }
        else
        {
            targetY = currentTargetY;
        }

        currentTargetY = Mathf.Lerp(
            currentTargetY,
            targetY,
            yFollowSpeed * Time.deltaTime
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