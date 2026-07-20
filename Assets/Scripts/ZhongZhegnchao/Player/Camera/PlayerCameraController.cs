using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Camera Target")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private bool detachTargetFromPlayer = true;

    [Header("Target Height Follow")]
    [Tooltip("CameraTarget height above the player.")]
    [SerializeField] private float targetHeightOffset = 1.6f;

    [Tooltip("Follow player Y height. Keep this on for slopes, stairs, and jumping.")]
    [SerializeField] private bool followPlayerY = true;

    [SerializeField] private float yFollowSpeed = 12f;

    [Header("Look Settings")]
    [FormerlySerializedAs("mouseSensitivity")]
    [SerializeField] private float horizontalMouseSensitivity = 3f;
    [SerializeField] private float verticalMouseSensitivity = 2f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 65f;
    [SerializeField] private bool lockCursor = true;
    [SerializeField] private KeyCode releaseCursorKey = KeyCode.LeftAlt;
    [SerializeField] private KeyCode alternateReleaseCursorKey = KeyCode.RightAlt;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 1.5f;
    [SerializeField] private float minCameraDistance = 1.5f;
    [SerializeField] private float maxCameraDistance = 6f;
    [SerializeField] private float zoomLerpSpeed = 12f;

    private bool cameraInputEnabled = true;
    private float yaw;
    private float pitch;
    private float currentTargetY;
    private float targetCameraDistance;

    private bool initialized;
    private bool ownsDetachedTarget;
    private bool cursorReleasedLastFrame;
    private bool skipLookThisFrame;
    private CinemachineThirdPersonFollow thirdPersonFollow;

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
        pitch = cameraTarget.eulerAngles.x;
        if (pitch > 180f)
        {
            pitch -= 360f;
        }

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);

        CacheThirdPersonFollow();

        initialized = true;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            cursorReleasedLastFrame = false;
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
        UpdateCursorState();
        UpdateTargetRotation();
        UpdateZoom();
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
        lockCursor = locked;
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
        if (!CanRotateCamera())
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * horizontalMouseSensitivity;
        pitch -= mouseY * verticalMouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void UpdateCursorState()
    {
        skipLookThisFrame = false;

        if (!cameraInputEnabled ||
            !GameModeManager.Instance.CurrentCapabilities.canLook ||
            !lockCursor)
        {
            return;
        }

        bool cursorReleased = IsReleaseCursorHeld();
        if (cursorReleased != cursorReleasedLastFrame)
        {
            skipLookThisFrame = true;
        }

        Cursor.lockState = cursorReleased ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = cursorReleased;
        cursorReleasedLastFrame = cursorReleased;
    }

    private bool CanRotateCamera()
    {
        return cameraInputEnabled &&
               GameModeManager.Instance.CurrentCapabilities.canLook &&
               !skipLookThisFrame &&
               (!lockCursor || !IsReleaseCursorHeld()) &&
               Cursor.lockState == CursorLockMode.Locked;
    }

    private bool IsReleaseCursorHeld()
    {
        return Input.GetKey(releaseCursorKey) ||
               Input.GetKey(alternateReleaseCursorKey);
    }

    private void UpdateZoom()
    {
        if (thirdPersonFollow == null)
        {
            CacheThirdPersonFollow();
        }

        if (thirdPersonFollow == null)
        {
            return;
        }

        if (cameraInputEnabled &&
            GameModeManager.Instance.CurrentCapabilities.canLook &&
            Cursor.lockState == CursorLockMode.Locked &&
            !InteractionRollBoxUI.BlocksCameraZoom)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                targetCameraDistance -= scroll * zoomSpeed;
                targetCameraDistance = Mathf.Clamp(targetCameraDistance, minCameraDistance, maxCameraDistance);
            }
        }

        thirdPersonFollow.CameraDistance = Mathf.Lerp(
            thirdPersonFollow.CameraDistance,
            targetCameraDistance,
            zoomLerpSpeed * Time.deltaTime
        );
    }

    private void CacheThirdPersonFollow()
    {
        thirdPersonFollow = GetComponentInChildren<CinemachineThirdPersonFollow>(true);

        if (thirdPersonFollow == null)
        {
            Debug.LogWarning("[PlayerCameraController] CinemachineThirdPersonFollow not found in player children.");
            return;
        }

        targetCameraDistance = Mathf.Clamp(
            thirdPersonFollow.CameraDistance,
            minCameraDistance,
            maxCameraDistance
        );
    }
}
