using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLockOnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReceiver inputReceiver;
    [SerializeField] private PlayerLocomotion locomotion;
    [SerializeField] private Transform playerRoot;

    [Header("Target Search")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float lockRange = 12f;
    [SerializeField] private float unlockDistance = 16f;
    [SerializeField] private float maxLockAngle = 70f;

    [Header("Attack Facing")]
    [SerializeField] private float attackFaceLockTime = 0.18f;

    [Header("死亡目标处理")]
    public PlayerLockOnCameraTargetGroup lockOnCameraTargetGroup;

    private EnemyTargetable currentTarget;
    private Camera mainCamera;

    private bool initialized;
    private bool bound;

    public bool IsLocked
    {
        get { return currentTarget != null; }
    }

    public EnemyTargetable CurrentTarget
    {
        get { return currentTarget; }
    }

    public event Action<EnemyTargetable> TargetChanged;

    public void Init(
        PlayerInputReceiver input,
        PlayerLocomotion playerLocomotion,
        Transform root,
        LayerMask targetLayer
    )
    {
        UnbindInput();

        inputReceiver = input;
        locomotion = playerLocomotion;
        playerRoot = root;
        enemyLayer = targetLayer;

        mainCamera = Camera.main;

        ResolveLockOnCameraTargetGroup();

        initialized = true;

        BindInput();
    }

    private void Awake()
    {
        if (playerRoot == null)
        {
            playerRoot = transform;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        ResolveLockOnCameraTargetGroup();
    }

    private void OnEnable()
    {
        EnemyWhitebox.OnAnyEnemyDead += OnEnemyDead;

        if (initialized)
        {
            BindInput();
        }
    }

    private void OnDisable()
    {
        EnemyWhitebox.OnAnyEnemyDead -= OnEnemyDead;

        UnbindInput();

        ClearTargetSilently();
    }

    private void ResolveLockOnCameraTargetGroup()
    {
        if (lockOnCameraTargetGroup != null)
            return;

        lockOnCameraTargetGroup = GetComponent<PlayerLockOnCameraTargetGroup>();

        if (lockOnCameraTargetGroup == null)
        {
            lockOnCameraTargetGroup = GetComponentInChildren<PlayerLockOnCameraTargetGroup>();
        }

        if (lockOnCameraTargetGroup == null)
        {
            lockOnCameraTargetGroup = GetComponentInParent<PlayerLockOnCameraTargetGroup>();
        }
    }

    private void OnEnemyDead(EnemyWhitebox enemy)
    {
        if (enemy == null)
            return;

        EnemyTargetable deadTarget = enemy.GetComponent<EnemyTargetable>();

        if (deadTarget == null)
        {
            deadTarget = enemy.GetComponentInChildren<EnemyTargetable>();
        }

        if (lockOnCameraTargetGroup != null)
        {
            lockOnCameraTargetGroup.RemoveTargetFromGroup(enemy.transform);

            if (deadTarget != null && deadTarget.TargetPoint != null)
            {
                lockOnCameraTargetGroup.RemoveTargetFromGroup(deadTarget.TargetPoint);
            }
        }

        if (!IsLocked)
            return;

        if (currentTarget == null)
            return;

        EnemyWhitebox currentEnemy = currentTarget.GetComponent<EnemyWhitebox>();

        if (currentEnemy == null)
        {
            currentEnemy = currentTarget.GetComponentInParent<EnemyWhitebox>();
        }

        if (currentEnemy == enemy)
        {
            UnlockTarget();
        }
    }

    private void BindInput()
    {
        if (bound)
            return;

        if (inputReceiver == null)
            return;

        inputReceiver.LockOnPressed += ToggleLockOn;
        bound = true;
    }

    private void UnbindInput()
    {
        if (!bound)
            return;

        if (inputReceiver != null)
        {
            inputReceiver.LockOnPressed -= ToggleLockOn;
        }

        bound = false;
    }

    private void Update()
    {
        if (!IsLocked)
            return;

        if (!IsCurrentTargetValid())
        {
            UnlockTarget();
        }
    }

    private void ToggleLockOn()
    {
        if (IsLocked)
        {
            UnlockTarget();
            return;
        }

        EnemyTargetable bestTarget = FindBestTarget();

        if (bestTarget == null)
        {
            Debug.Log("[LockOn] No target found.");
            return;
        }

        LockTarget(bestTarget);
    }

    private void LockTarget(EnemyTargetable target)
    {
        if (target == null)
            return;

        currentTarget = target;

        TargetChanged?.Invoke(currentTarget);

        Debug.Log("[LockOn] Locked: " + target.name);
    }

    public void UnlockTarget()
    {
        if (currentTarget != null)
        {
            Debug.Log("[LockOn] Unlocked: " + currentTarget.name);
        }

        currentTarget = null;

        TargetChanged?.Invoke(null);
    }

    private void ClearTargetSilently()
    {
        currentTarget = null;
    }

    public bool FaceCurrentTargetForAttack()
    {
        if (!IsLocked)
            return false;

        if (!IsCurrentTargetValid())
        {
            UnlockTarget();
            return false;
        }

        if (!TryGetCurrentTargetDirection(out Vector3 direction))
            return false;

        if (locomotion != null)
        {
            locomotion.FaceDirectionInstant(direction, attackFaceLockTime);
        }
        else if (playerRoot != null)
        {
            playerRoot.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        return true;
    }

    public Vector3 GetAttackDirection()
    {
        if (TryGetCurrentTargetDirection(out Vector3 direction))
        {
            return direction;
        }

        if (playerRoot != null)
        {
            return playerRoot.forward;
        }

        return transform.forward;
    }

    public bool TryGetCurrentTargetDirection(out Vector3 direction)
    {
        direction = Vector3.zero;

        if (currentTarget == null)
            return false;

        Transform targetPoint = currentTarget.TargetPoint;

        if (targetPoint == null)
            return false;

        Transform root = playerRoot != null ? playerRoot : transform;

        direction = targetPoint.position - root.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector3.zero;
            return false;
        }

        direction.Normalize();
        return true;
    }

    private bool IsCurrentTargetValid()
    {
        if (currentTarget == null)
            return false;

        if (!currentTarget.CanBeLocked)
            return false;

        EnemyWhitebox enemy = currentTarget.GetComponent<EnemyWhitebox>();

        if (enemy == null)
        {
            enemy = currentTarget.GetComponentInParent<EnemyWhitebox>();
        }

        if (enemy != null && enemy.IsDead)
            return false;

        Transform targetPoint = currentTarget.TargetPoint;

        if (targetPoint == null)
            return false;

        Transform root = playerRoot != null ? playerRoot : transform;

        float distance = Vector3.Distance(
            root.position,
            targetPoint.position
        );

        if (distance > unlockDistance)
            return false;

        return true;
    }

    private EnemyTargetable FindBestTarget()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        Transform root = playerRoot != null ? playerRoot : transform;

        Collider[] hits = Physics.OverlapSphere(
            root.position,
            lockRange,
            enemyLayer,
            QueryTriggerInteraction.Collide
        );

        EnemyTargetable bestTarget = null;
        float bestScore = float.MaxValue;

        HashSet<EnemyTargetable> checkedTargets = new HashSet<EnemyTargetable>();

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            EnemyTargetable target = hit.GetComponentInParent<EnemyTargetable>();

            if (target == null)
                continue;

            if (checkedTargets.Contains(target))
                continue;

            checkedTargets.Add(target);

            if (!target.CanBeLocked)
                continue;

            EnemyWhitebox enemy = target.GetComponent<EnemyWhitebox>();

            if (enemy == null)
            {
                enemy = target.GetComponentInParent<EnemyWhitebox>();
            }

            if (enemy != null && enemy.IsDead)
                continue;

            Transform targetPoint = target.TargetPoint;

            if (targetPoint == null)
                continue;

            Vector3 targetPosition = targetPoint.position;

            if (mainCamera != null)
            {
                Vector3 viewportPosition = mainCamera.WorldToViewportPoint(targetPosition);

                if (viewportPosition.z <= 0f)
                    continue;

                Vector3 cameraToTarget = targetPosition - mainCamera.transform.position;
                float angle = Vector3.Angle(mainCamera.transform.forward, cameraToTarget);

                if (angle > maxLockAngle)
                    continue;

                Vector2 screenCenter = new Vector2(0.5f, 0.5f);
                Vector2 targetScreenPosition = new Vector2(
                    viewportPosition.x,
                    viewportPosition.y
                );

                float screenDistance = Vector2.Distance(
                    screenCenter,
                    targetScreenPosition
                );

                float worldDistance = Vector3.Distance(
                    root.position,
                    targetPosition
                );

                float score = screenDistance * 10f + worldDistance * 0.1f;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = target;
                }
            }
            else
            {
                float worldDistance = Vector3.Distance(
                    root.position,
                    targetPosition
                );

                if (worldDistance < bestScore)
                {
                    bestScore = worldDistance;
                    bestTarget = target;
                }
            }
        }

        return bestTarget;
    }
}