using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;

public class EnemyWhitebox : MonoBehaviour, IDamageable
{
    public static event Action<EnemyWhitebox> OnAnyEnemyDead;

    [Header("HP")]
    public int maxHp = 100;
    public int currentHp = 100;
    [SerializeField] private bool resetHealthOnAwake = true;

    [Header("Out Of Combat Regeneration")]
    [SerializeField] private float outOfCombatHoldTime = 3f;
    [SerializeField] private float fullHealDuration = 3f;

    [Header("Death")]
    [SerializeField] private bool destroyOnDeath = false;
    [SerializeField] private float destroyDelay = 2f;

    [Header("SimpleEnemy Bridge")]
    [SerializeField] private SimpleEnemy simpleEnemy;
    [SerializeField] private bool notifySimpleEnemyOnDamaged = true;

    [Tooltip("如果开启，SimpleEnemy 自己也会做一次受击击退。一般建议关闭，避免和 DamageInfo.knockback 双重击退。")]
    [SerializeField] private bool letSimpleEnemyApplyOwnKnockback = false;

    [Header("击退设置")]
    public bool enableKnockback = true;
    public float knockbackDuration = 0.18f;
    public float knockbackVerticalForce = 0f;
    public bool knockbackOnlyHorizontal = true;

    [Header("击退碰撞")]
    [SerializeField]
    private LayerMask knockbackObstacleLayer = ~0;

    [SerializeField]
    private float knockbackCollisionSkin = 0.05f;

    [SerializeField]
    private bool stopKnockbackOnObstacle = true;

    [Header("移动控制")]
    [SerializeField]
    private bool canMove = true;

    [SerializeField]
    private bool debugMovementControl = false;

    [Header("Debug")]
    public bool debugLog = true;

    private bool isDead;
    private bool inCombat;

    private Coroutine regenRoutine;
    private Coroutine knockbackCoroutine;

    private ElementStatusController elementStatusController;
    private ElementVfxController elementVfxController;
    private DamageNumberAnchor damageNumberAnchor;

    private CharacterController characterController;
    private Rigidbody enemyRigidbody;
    private NavMeshAgent navMeshAgent;

    private int windPullControlCount;
    private bool canMoveBeforeWind;

    public bool IsDead
    {
        get { return isDead; }
    }

    public bool CanMove
    {
        get
        {
            return canMove && !isDead;
        }
    }

    public bool IsWindPullActive
    {
        get
        {
            return windPullControlCount > 0;
        }
    }

    public bool IsBeingKnockedBack
    {
        get
        {
            return knockbackCoroutine != null;
        }
    }

    public int CurrentHealth
    {
        get { return currentHp; }
    }

    public int MaxHealth
    {
        get { return maxHp; }
    }

    public int CurrentHp
    {
        get { return currentHp; }
    }

    public int MaxHp
    {
        get { return maxHp; }
    }

    public void SetCanMove(bool value)
    {
        canMove = value;

        bool actualCanMove =
            canMove && !isDead;

        if (simpleEnemy != null)
        {
            simpleEnemy.enabled =
                actualCanMove;
        }

        if (navMeshAgent != null &&
            navMeshAgent.enabled &&
            navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped =
                !actualCanMove;
        }

        if (debugMovementControl)
        {
            Debug.Log(
                $"[EnemyWhitebox] " +
                $"CanMove={CanMove}",
                this
            );
        }
    }

    private void Awake()
    {
        maxHp = Mathf.Max(1, maxHp);

        if (resetHealthOnAwake)
        {
            currentHp = maxHp;
        }
        else
        {
            currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        }

        isDead = currentHp <= 0;

        ResolveReferences();
    }

    private void LateUpdate()
    {
        if (CanMove)
            return;

        if (isDead)
            return;

        if (navMeshAgent == null)
            return;

        if (!navMeshAgent.enabled ||
            !navMeshAgent.isOnNavMesh)
        {
            return;
        }

        navMeshAgent.isStopped = true;
    }

    private void ResolveReferences()
    {
        if (simpleEnemy == null)
        {
            simpleEnemy = GetComponent<SimpleEnemy>();
        }

        if (simpleEnemy == null)
        {
            simpleEnemy = GetComponentInChildren<SimpleEnemy>();
        }

        if (simpleEnemy == null)
        {
            simpleEnemy = GetComponentInParent<SimpleEnemy>();
        }

        elementStatusController = GetComponent<ElementStatusController>();

        if (elementStatusController == null)
        {
            elementStatusController = GetComponentInParent<ElementStatusController>();
        }

        if (elementStatusController == null)
        {
            elementStatusController = gameObject.AddComponent<ElementStatusController>();
        }

        elementVfxController = GetComponent<ElementVfxController>();

        if (elementVfxController == null)
        {
            elementVfxController = GetComponentInChildren<ElementVfxController>();
        }

        damageNumberAnchor = GetComponent<DamageNumberAnchor>();

        if (damageNumberAnchor == null)
        {
            damageNumberAnchor = GetComponentInChildren<DamageNumberAnchor>();
        }

        characterController = GetComponent<CharacterController>();

        if (characterController == null)
        {
            characterController = GetComponentInChildren<CharacterController>();
        }

        enemyRigidbody = GetComponent<Rigidbody>();

        if (enemyRigidbody == null)
        {
            enemyRigidbody = GetComponentInChildren<Rigidbody>();
        }

        navMeshAgent = GetComponent<NavMeshAgent>();

        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponentInChildren<NavMeshAgent>();
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isDead)
        {
            return;
        }

        StopRegenRoutine();

        if (elementStatusController != null)
        {
            damageInfo = elementStatusController.ProcessIncomingElement(damageInfo);
        }

        DamageInfo calculatedDamage = DamageCalculator.Calculate(damageInfo);

        int damage = Mathf.Max(0, calculatedDamage.finalDamage);

        if (damage <= 0)
        {
            return;
        }

        currentHp -= damage;
        currentHp = Mathf.Max(0, currentHp);

        ShowDamageNumber(calculatedDamage, damage);

        ApplyKnockback(calculatedDamage);

        if (debugLog)
        {
            Debug.Log(
                $"[EnemyWhitebox] Damage={damage}, " +
                $"Element={calculatedDamage.element}, " +
                $"Reaction={calculatedDamage.reactionType}, " +
                $"Crit={calculatedDamage.isCritical}, " +
                $"Knockback={calculatedDamage.knockback}, " +
                $"HP={currentHp}/{maxHp}",
                this
            );
        }

        if (currentHp <= 0)
        {
            Die();
            return;
        }

        NotifySimpleEnemyDamaged(calculatedDamage);
    }

    private void ShowDamageNumber(DamageInfo calculatedDamage, int damage)
    {
        if (DamageNumberSpawner.Instance == null)
            return;

        if (damage <= 0)
            return;

        Vector3 numberPosition = transform.position;

        if (damageNumberAnchor != null)
        {
            numberPosition = damageNumberAnchor.GetWorldPosition();
        }

        DamageNumberSpawner.Instance.ShowDamageNumber(
            calculatedDamage,
            numberPosition
        );
    }

    private void NotifySimpleEnemyDamaged(DamageInfo calculatedDamage)
    {
        if (!notifySimpleEnemyOnDamaged)
            return;

        if (!CanMove)
            return;

        if (simpleEnemy == null)
            return;

        Transform attacker = calculatedDamage.attacker != null
            ? calculatedDamage.attacker.transform
            : null;

        simpleEnemy.OnDamaged(
            attacker,
            letSimpleEnemyApplyOwnKnockback
        );
    }

    private void ApplyKnockback(DamageInfo damageInfo)
    {
        if (!enableKnockback)
            return;

        if (damageInfo.knockback <= 0f)
            return;

        Vector3 direction = damageInfo.hitDirection;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            if (damageInfo.attacker != null)
            {
                direction = transform.position - damageInfo.attacker.transform.position;
            }
            else
            {
                direction = -transform.forward;
            }
        }

        if (knockbackOnlyHorizontal)
        {
            direction.y = 0f;
        }

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        direction.Normalize();

        if (!knockbackOnlyHorizontal && knockbackVerticalForce > 0f)
        {
            direction.y += knockbackVerticalForce;
            direction.Normalize();
        }

        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
        }

        knockbackCoroutine = StartCoroutine(
            KnockbackRoutine(direction, damageInfo.knockback)
        );
    }

    private IEnumerator KnockbackRoutine(
    Vector3 direction,
    float force
)
    {
        float timer = 0f;

        while (timer < knockbackDuration &&
               !isDead)
        {
            float deltaTime = Time.deltaTime;

            timer += deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    timer / knockbackDuration
                );

            float strength = Mathf.Lerp(
                force,
                0f,
                normalizedTime
            );

            Vector3 desiredMove =
                direction *
                strength *
                deltaTime;

            bool hitObstacle;

            Vector3 safeMove =
                GetSafeKnockbackMove(
                    desiredMove,
                    out hitObstacle
                );

            if (safeMove.sqrMagnitude > 0.000001f)
            {
                ApplyKnockbackMove(safeMove);
            }

            if (hitObstacle &&
                stopKnockbackOnObstacle)
            {
                if (debugLog)
                {
                    Debug.Log(
                        "[EnemyWhitebox] " +
                        "Knockback stopped by obstacle.",
                        this
                    );
                }

                break;
            }

            yield return null;
        }

        knockbackCoroutine = null;
    }

    private void ApplyKnockbackMove(
    Vector3 move
)
    {
        if (navMeshAgent != null &&
            navMeshAgent.enabled &&
            navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.Move(move);
            return;
        }

        if (characterController != null &&
            characterController.enabled)
        {
            characterController.Move(move);
            return;
        }

        if (enemyRigidbody != null &&
            !enemyRigidbody.isKinematic)
        {
            enemyRigidbody.MovePosition(
                enemyRigidbody.position + move
            );

            return;
        }

        transform.position += move;
    }

    private Vector3 GetSafeKnockbackMove(
    Vector3 desiredMove,
    out bool hitObstacle
)
    {
        hitObstacle = false;

        float moveDistance =
            desiredMove.magnitude;

        if (moveDistance <= 0.0001f)
        {
            return Vector3.zero;
        }

        Vector3 moveDirection =
            desiredMove / moveDistance;

        GetKnockbackCapsule(
            out Vector3 point1,
            out Vector3 point2,
            out float radius
        );

        float castDistance =
            moveDistance +
            Mathf.Max(
                0f,
                knockbackCollisionSkin
            );

        RaycastHit[] hits =
            Physics.CapsuleCastAll(
                point1,
                point2,
                radius,
                moveDirection,
                castDistance,
                knockbackObstacleLayer,
                QueryTriggerInteraction.Ignore
            );

        float nearestDistance =
            float.MaxValue;

        bool foundObstacle = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.collider == null)
                continue;

            if (IsOwnCollider(hit.collider))
                continue;

            if (hit.distance >= nearestDistance)
                continue;

            nearestDistance = hit.distance;
            foundObstacle = true;
        }

        if (!foundObstacle)
        {
            return desiredMove;
        }

        hitObstacle = true;

        float safeDistance = Mathf.Max(
            0f,
            nearestDistance -
            knockbackCollisionSkin
        );

        safeDistance = Mathf.Min(
            safeDistance,
            moveDistance
        );

        return moveDirection * safeDistance;
    }

    private void GetKnockbackCapsule(
    out Vector3 point1,
    out Vector3 point2,
    out float radius
)
    {
        if (characterController != null)
        {
            Transform controllerTransform =
                characterController.transform;

            Vector3 center =
                controllerTransform.TransformPoint(
                    characterController.center
                );

            float scaleX =
                Mathf.Abs(
                    controllerTransform.lossyScale.x
                );

            float scaleY =
                Mathf.Abs(
                    controllerTransform.lossyScale.y
                );

            float scaleZ =
                Mathf.Abs(
                    controllerTransform.lossyScale.z
                );

            radius =
                characterController.radius *
                Mathf.Max(scaleX, scaleZ);

            float height =
                characterController.height *
                scaleY;

            float halfSegment =
                Mathf.Max(
                    0f,
                    height * 0.5f - radius
                );

            point1 =
                center +
                Vector3.up * halfSegment;

            point2 =
                center -
                Vector3.up * halfSegment;

            return;
        }

        if (navMeshAgent != null)
        {
            radius = Mathf.Max(
                0.05f,
                navMeshAgent.radius
            );

            float height = Mathf.Max(
                radius * 2f,
                navMeshAgent.height
            );

            Vector3 center =
                navMeshAgent.transform.position +
                Vector3.up *
                (
                    navMeshAgent.baseOffset +
                    height * 0.5f
                );

            float halfSegment =
                Mathf.Max(
                    0f,
                    height * 0.5f - radius
                );

            point1 =
                center +
                Vector3.up * halfSegment;

            point2 =
                center -
                Vector3.up * halfSegment;

            return;
        }

        radius = 0.4f;

        Vector3 fallbackCenter =
            transform.position +
            Vector3.up;

        point1 =
            fallbackCenter +
            Vector3.up * 0.6f;

        point2 =
            fallbackCenter -
            Vector3.up * 0.6f;
    }

    private bool IsOwnCollider(
    Collider targetCollider
)
    {
        if (targetCollider == null)
            return false;

        Transform enemyRoot =
            simpleEnemy != null
                ? simpleEnemy.transform
                : transform;

        Transform targetTransform =
            targetCollider.transform;

        if (targetTransform == enemyRoot)
        {
            return true;
        }

        return targetTransform.IsChildOf(
            enemyRoot
        );
    }

    public void SetCombatActive(bool active)
    {
        if (isDead)
            return;

        if (inCombat == active)
            return;

        inCombat = active;

        if (active)
        {
            StopRegenRoutine();
            return;
        }

        if (currentHp < maxHp)
        {
            regenRoutine = StartCoroutine(OutOfCombatRegenRoutine());
        }
    }

    private IEnumerator OutOfCombatRegenRoutine()
    {
        float hold = Mathf.Max(0f, outOfCombatHoldTime);

        if (hold > 0f)
        {
            yield return new WaitForSeconds(hold);
        }

        if (isDead || inCombat)
        {
            regenRoutine = null;
            yield break;
        }

        int startHealth = currentHp;
        float duration = Mathf.Max(0.01f, fullHealDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (isDead || inCombat)
            {
                regenRoutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            currentHp = Mathf.RoundToInt(
                Mathf.Lerp(startHealth, maxHp, t)
            );

            yield return null;
        }

        currentHp = maxHp;
        regenRoutine = null;

        if (debugLog)
        {
            Debug.Log(
                $"[EnemyWhitebox] Out of combat regen completed. HP={currentHp}/{maxHp}",
                this
            );
        }
    }

    private void StopRegenRoutine()
    {
        if (regenRoutine == null)
            return;

        StopCoroutine(regenRoutine);
        regenRoutine = null;
    }

    public void ResetHealth()
    {
        StopRegenRoutine();

        isDead = false;
        inCombat = false;
        currentHp = Mathf.Max(1, maxHp);

        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }

        if (elementStatusController != null)
        {
            elementStatusController.StopAllElementStatus();
        }

        if (elementVfxController != null)
        {
            elementVfxController.StopAllElementVfx();
        }

        if (debugLog)
        {
            Debug.Log(
                $"[EnemyWhitebox] Health reset. HP={currentHp}/{maxHp}",
                this
            );
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        inCombat = false;
        currentHp = 0;

        StopRegenRoutine();

        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }

        if (elementStatusController != null)
        {
            elementStatusController.StopAllElementStatus();
        }

        if (elementVfxController != null)
        {
            elementVfxController.StopAllElementVfx();
        }

        if (simpleEnemy != null)
        {
            simpleEnemy.Die();
        }

        OnAnyEnemyDead?.Invoke(this);

        if (debugLog)
        {
            Debug.Log("[EnemyWhitebox] Enemy Dead.", this);
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    public void SetWindPullActive(bool active)
    {
        if (active)
        {
            if (windPullControlCount == 0)
            {
                canMoveBeforeWind = canMove;

                SetCanMove(false);
            }

            windPullControlCount++;

            return;
        }

        if (windPullControlCount <= 0)
            return;

        windPullControlCount--;

        if (windPullControlCount > 0)
            return;

        SetCanMove(canMoveBeforeWind);
    }

    public void ApplyWindPullMove(
    Vector3 desiredMove
)
    {
        if (isDead)
            return;

        if (!IsWindPullActive)
            return;

        bool hitObstacle;

        Vector3 safeMove =
            GetSafeKnockbackMove(
                desiredMove,
                out hitObstacle
            );

        if (safeMove.sqrMagnitude <= 0.000001f)
            return;

        ApplyKnockbackMove(safeMove);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxHp < 1)
            maxHp = 1;

        if (currentHp < 0)
            currentHp = 0;

        if (outOfCombatHoldTime < 0f)
            outOfCombatHoldTime = 0f;

        if (fullHealDuration < 0.01f)
            fullHealDuration = 0.01f;

        if (destroyDelay < 0f)
            destroyDelay = 0f;
    }
#endif
}