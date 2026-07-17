using UnityEngine;
using Unity.Cinemachine;

public class PlayerLockOnCameraTargetGroup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerLockOnController lockOnController;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private CinemachineTargetGroup targetGroup;
    [SerializeField] private Transform playerCameraTarget;

    [Header("Group Weight")]
    [SerializeField] private float playerWeight = 8f;
    [SerializeField] private float playerRadius = 0.6f;
    [SerializeField] private float enemyWeight = 2f;
    [SerializeField] private float enemyRadius = 0.8f;

    private Transform currentEnemyTargetPoint;

    private bool initialized;
    private bool bound;

    public CinemachineTargetGroup TargetGroup
    {
        get { return targetGroup; }
    }

    public void Init(
        PlayerLockOnController controller,
        Transform cameraTarget
    )
    {
        Unbind();

        lockOnController = controller;
        playerCameraTarget = cameraTarget;

        EnsureCinemachineCamera();
        EnsureTargetGroup();

        SetupCinemachineCameraTarget();
        ResetGroupToPlayerOnly();

        initialized = true;

        Bind();
    }

    private void Awake()
    {
        if (lockOnController == null)
        {
            lockOnController = GetComponent<PlayerLockOnController>();
        }

        EnsureCinemachineCamera();
    }

    private void OnEnable()
    {
        if (initialized)
        {
            Bind();
        }
    }

    private void OnDisable()
    {
        Unbind();

        // 不要在 Disable 阶段操作 TargetGroup。
        // 这个阶段 Unity 可能已经开始销毁 targetGroup 了。
        currentEnemyTargetPoint = null;
    }

    private void OnDestroy()
    {
        Unbind();

        currentEnemyTargetPoint = null;
        targetGroup = null;
        cinemachineCamera = null;
    }

    private void Bind()
    {
        if (bound)
        {
            return;
        }

        if (lockOnController == null)
        {
            return;
        }

        lockOnController.TargetChanged += OnTargetChanged;
        bound = true;
    }

    private void Unbind()
    {
        if (!bound)
        {
            return;
        }

        if (lockOnController != null)
        {
            lockOnController.TargetChanged -= OnTargetChanged;
        }

        bound = false;
    }

    public void RemoveTargetFromGroup(Transform target)
    {
        if (target == null)
            return;

        if (targetGroup == null)
            return;

        targetGroup.RemoveMember(target);

        Debug.Log($"[LockOnCameraTargetGroup] Removed target from group: {target.name}", this);
    }

    private void EnsureCinemachineCamera()
    {
        if (cinemachineCamera != null)
        {
            return;
        }

        cinemachineCamera = GetComponentInChildren<CinemachineCamera>(true);

        if (cinemachineCamera != null)
        {
            return;
        }

        cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();

        if (cinemachineCamera == null)
        {
            Debug.LogWarning("[LockOnCameraTargetGroup] Scene 中找不到 CinemachineCamera。");
        }
    }

    private void EnsureTargetGroup()
    {
        if (targetGroup != null)
        {
            return;
        }

        GameObject groupObj = new GameObject("PlayerLockOnTargetGroup");
        groupObj.transform.position = transform.position;
        groupObj.transform.rotation = Quaternion.identity;

        targetGroup = groupObj.AddComponent<CinemachineTargetGroup>();

        targetGroup.PositionMode = CinemachineTargetGroup.PositionModes.GroupAverage;
        targetGroup.RotationMode = CinemachineTargetGroup.RotationModes.Manual;
        targetGroup.UpdateMethod = CinemachineTargetGroup.UpdateMethods.LateUpdate;
    }

    private void SetupCinemachineCameraTarget()
    {
        if (cinemachineCamera == null)
        {
            return;
        }

        if (targetGroup == null)
        {
            return;
        }

        if (playerCameraTarget == null)
        {
            Debug.LogWarning("[LockOnCameraTargetGroup] playerCameraTarget 为空，无法设置 Cinemachine Target。");
            return;
        }

        CameraTarget cameraTargetData = cinemachineCamera.Target;

        // 重点：
        // Tracking Target 还是 CameraTarget。
        // 这样你的鼠标旋转镜头不会失效。
        cameraTargetData.TrackingTarget = playerCameraTarget;

        // LookAt Target 才使用 TargetGroup。
        // 这样锁定敌人时，镜头视线会兼顾 Player + Enemy。
        cameraTargetData.CustomLookAtTarget = true;
        cameraTargetData.LookAtTarget = targetGroup.transform;

        cinemachineCamera.Target = cameraTargetData;

        Debug.Log("[LockOnCameraTargetGroup] Tracking Target = CameraTarget, LookAt Target = TargetGroup.");
    }

    private void ResetGroupToPlayerOnly()
    {
        if (targetGroup == null)
        {
            return;
        }

        targetGroup.Targets.Clear();

        if (playerCameraTarget != null)
        {
            targetGroup.AddMember(
                playerCameraTarget,
                playerWeight,
                playerRadius
            );
        }

        currentEnemyTargetPoint = null;

        SafeDoUpdate();
    }

    private void OnTargetChanged(EnemyTargetable target)
    {
        if (targetGroup == null)
        {
            currentEnemyTargetPoint = null;
            return;
        }

        RemoveCurrentEnemyMember();

        if (target == null)
        {
            SafeDoUpdate();
            return;
        }

        Transform targetPoint = target.TargetPoint;

        if (targetPoint == null)
        {
            SafeDoUpdate();
            return;
        }

        currentEnemyTargetPoint = targetPoint;

        AddOrReplaceMember(
            currentEnemyTargetPoint,
            enemyWeight,
            enemyRadius
        );

        SafeDoUpdate();
    }

    private void RemoveCurrentEnemyMember()
    {
        if (targetGroup == null || currentEnemyTargetPoint == null)
        {
            currentEnemyTargetPoint = null;
            return;
        }

        int index = targetGroup.FindMember(currentEnemyTargetPoint);

        if (index >= 0)
        {
            targetGroup.RemoveMember(currentEnemyTargetPoint);
        }

        currentEnemyTargetPoint = null;
    }

    private void AddOrReplaceMember(
        Transform target,
        float weight,
        float radius
    )
    {
        if (targetGroup == null || target == null)
        {
            return;
        }

        int index = targetGroup.FindMember(target);

        if (index >= 0)
        {
            targetGroup.RemoveMember(target);
        }

        targetGroup.AddMember(target, weight, radius);
    }

    private void SafeDoUpdate()
    {
        if (targetGroup == null)
        {
            return;
        }

        targetGroup.DoUpdate();
    }
}