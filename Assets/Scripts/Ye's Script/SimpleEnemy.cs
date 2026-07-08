using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SimpleEnemy : MonoBehaviour
{
    public enum StartMode
    {
        Idle,
        Patrol
    }

    private enum EnemyState
    {
        Idle,
        Patrol,
        Attack,
        ReturnHome,
        Dead
    }

    [Header("Start")]
    [SerializeField] private StartMode startMode = StartMode.Patrol;
    [SerializeField] private Transform player;

    [Header("Patrol")]
    [SerializeField] private EnemyPatrolNode startNode;
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float arriveEpsilon = 0.15f;
    [SerializeField] private float stoppedSpeed = 0.05f;

    [Header("Look Around Routine")]
    [SerializeField] private float lookAngleMin = 35f;
    [SerializeField] private float lookAngleMax = 95f;
    [SerializeField] private float lookTurnSpeed = 140f;
    [SerializeField] private float lookPause = 0.15f;

    [Header("Detection Sectors")]
    [SerializeField] private float frontViewAngle = 150f;
    [SerializeField] private float frontViewRadius = 12f;
    [SerializeField] private float rearViewRadius = 4f;
    [SerializeField] private float targetHeightOffset = 1f;
    [SerializeField] private bool drawDetectionGizmos = true;

    [Header("Attack")]
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float repathInterval = 0.2f;
    [SerializeField] private float loseSightGrace = 1f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackHitRadius = 1.2f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Hit Reaction")]
    [SerializeField] private float knockbackDistance = 1.2f;
    [SerializeField] private float knockbackDuration = 0.18f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string attackTrigger = "TrigAttack";
    [SerializeField] private string deadTrigger = "TrigDead";

    [Header("Debug")]
    [SerializeField] private bool enableLogs;

    private NavMeshAgent agent;
    private EnemyHealth health;
    private EnemyState state;
    private EnemyPatrolNode currentNode;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private Coroutine lookRoutine;
    private float attackTimer;
    private float repathTimer;
    private float lostTimer;
    private Coroutine knockbackRoutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        health = GetComponent<EnemyHealth>();
        if (health == null)
            health = GetComponentInChildren<EnemyHealth>();

        homePosition = transform.position;
        homeRotation = transform.rotation;
    }

    private void Start()
    {
        ResolvePlayer();
        ApplyInitialState();
    }

    private void Update()
    {
        if (state == EnemyState.Dead)
            return;

        ResolvePlayer();
        UpdateAnimatorSpeed();

        if (state != EnemyState.Attack && CanDetectPlayer())
        {
            EnterAttack();
            return;
        }

        switch (state)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.ReturnHome:
                UpdateReturnHome();
                break;
        }
    }

    public void OnDamaged(Transform attacker)
    {
        if (state == EnemyState.Dead)
            return;

        if (attacker != null)
            player = attacker;
        else
            ResolvePlayer();

        ApplyKnockback(attacker);
        EnterAttack();
    }

    public void Die()
    {
        if (state == EnemyState.Dead)
            return;

        state = EnemyState.Dead;
        StopLookRoutine();
        StopKnockbackRoutine();
        NotifyCombatActive(false);

        if (AgentReady())
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animator != null && !string.IsNullOrEmpty(deadTrigger))
            animator.SetTrigger(deadTrigger);

        Log("Died.");
    }

    private void ApplyInitialState()
    {
        if (startMode == StartMode.Patrol && startNode != null)
        {
            currentNode = startNode;
            state = EnemyState.Patrol;
            agent.speed = patrolSpeed;
            SetDestination(currentNode.Position);
        }
        else
        {
            state = EnemyState.Idle;
            StopAgent();
        }
    }

    private void UpdateIdle()
    {
        StopAgent();
    }

    private void UpdatePatrol()
    {
        if (lookRoutine != null)
            return;

        if (currentNode == null)
        {
            StopAgent();
            return;
        }

        if (ReachedDestination())
            lookRoutine = StartCoroutine(LookAroundThenPickNextNode());
    }

    private void EnterAttack()
    {
        bool wasAttack = state == EnemyState.Attack;

        StopLookRoutine();
        state = EnemyState.Attack;
        NotifyCombatActive(true);
        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackRange * 0.85f;
        attackTimer = 0f;
        repathTimer = 0f;
        lostTimer = 0f;
        if (AgentReady())
            agent.isStopped = false;

        if (!wasAttack)
            Log("Activated.");
    }

    private void UpdateAttack()
    {
        if (player == null)
        {
            LosePlayerAndReturnHome();
            return;
        }

        bool detected = CanDetectPlayer();
        if (detected)
            lostTimer = 0f;
        else
            lostTimer += Time.deltaTime;

        if (lostTimer >= loseSightGrace)
        {
            LosePlayerAndReturnHome();
            return;
        }

        Vector3 target = player.position;
        float distance = HorizontalDistance(transform.position, target);

        FaceTarget(target);

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;

            if (distance > attackRange)
            {
                SetDestination(target);
            }
            else
            {
                StopAgent();
            }
        }

        attackTimer -= Time.deltaTime;
        if (distance <= attackRange && attackTimer <= 0f)
        {
            attackTimer = attackCooldown;
            DoAttack();
        }
    }

    private void LosePlayerAndReturnHome()
    {
        StopLookRoutine();
        NotifyCombatActive(false);
        lookRoutine = StartCoroutine(LookAroundThenReturnHome());
    }

    private void UpdateReturnHome()
    {
        if (lookRoutine != null)
            return;

        if (!ReachedDestination())
            return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            homeRotation,
            lookTurnSpeed * Time.deltaTime
        );

        if (Quaternion.Angle(transform.rotation, homeRotation) <= 1f)
        {
            agent.stoppingDistance = 0f;
            ApplyInitialState();
        }
    }

    private IEnumerator LookAroundThenPickNextNode()
    {
        yield return LookAround();

        if (state != EnemyState.Patrol)
        {
            lookRoutine = null;
            yield break;
        }

        EnemyPatrolNode next = currentNode != null ? currentNode.PickRandomNext() : null;
        if (next != null)
        {
            currentNode = next;
            SetDestination(currentNode.Position);
        }
        else
        {
            StopAgent();
        }

        lookRoutine = null;
    }

    private IEnumerator LookAroundThenReturnHome()
    {
        state = EnemyState.ReturnHome;
        yield return LookAround();

        if (state != EnemyState.ReturnHome)
        {
            lookRoutine = null;
            yield break;
        }

        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0f;
        SetDestination(homePosition);
        lookRoutine = null;
    }

    private IEnumerator LookAround()
    {
        StopAgent();

        Quaternion startRotation = transform.rotation;
        float leftAngle = Random.Range(lookAngleMin, lookAngleMax);
        float rightAngle = Random.Range(lookAngleMin, lookAngleMax);

        yield return RotateByAngle(-leftAngle);
        yield return new WaitForSeconds(lookPause);
        yield return RotateToRotation(startRotation);
        yield return RotateByAngle(rightAngle);
        yield return new WaitForSeconds(lookPause);
        yield return RotateToRotation(startRotation);
    }

    private IEnumerator RotateByAngle(float angle)
    {
        Quaternion target = Quaternion.AngleAxis(angle, Vector3.up) * transform.rotation;
        yield return RotateToRotation(target);
    }

    private IEnumerator RotateToRotation(Quaternion target)
    {
        while (state != EnemyState.Dead && Quaternion.Angle(transform.rotation, target) > 0.5f)
        {
            if (state != EnemyState.Attack && CanDetectPlayer())
            {
                EnterAttack();
                yield break;
            }

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                target,
                lookTurnSpeed * Time.deltaTime
            );
            yield return null;
        }

        if (state != EnemyState.Dead)
            transform.rotation = target;
    }

    private bool CanDetectPlayer()
    {
        if (player == null)
            return false;

        Vector3 toPlayer = player.position + Vector3.up * targetHeightOffset - transform.position;
        toPlayer.y = 0f;

        float sqrDistance = toPlayer.sqrMagnitude;
        if (sqrDistance < 0.001f)
            return true;

        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Vector3.forward;

        float angle = Vector3.Angle(flatForward.normalized, toPlayer.normalized);
        float frontHalfAngle = frontViewAngle * 0.5f;

        if (angle <= frontHalfAngle)
            return sqrDistance <= frontViewRadius * frontViewRadius;

        return sqrDistance <= rearViewRadius * rearViewRadius;
    }

    private void DoAttack()
    {
        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);

        Vector3 center = attackPoint != null
            ? attackPoint.position
            : transform.position + transform.forward * Mathf.Max(1f, attackRange * 0.6f) + Vector3.up;

        Collider[] hits = playerLayer.value == 0
            ? Physics.OverlapSphere(center, attackHitRadius, ~0, QueryTriggerInteraction.Ignore)
            : Physics.OverlapSphere(center, attackHitRadius, playerLayer, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            if (playerLayer.value == 0 && !hits[i].CompareTag("Player"))
                continue;

            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(new DamageInfo
                {
                    attacker = gameObject,
                    target = hits[i].gameObject,
                    damage = attackDamage,
                    knockback = 0f,
                    hitPoint = hits[i].ClosestPoint(center),
                    hitDirection = (hits[i].transform.position - transform.position).normalized,
                    sourceAction = null
                });
            }

            break;
        }
    }

    private void SetDestination(Vector3 destination)
    {
        if (!AgentReady())
            return;

        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    private void StopAgent()
    {
        if (!AgentReady())
            return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    private bool ReachedDestination()
    {
        if (!AgentReady())
            return false;

        if (agent.pathPending)
            return false;

        if (agent.remainingDistance > agent.stoppingDistance + arriveEpsilon)
            return false;

        return agent.velocity.sqrMagnitude <= stoppedSpeed * stoppedSpeed;
    }

    private bool AgentReady()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 look = target - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude < 0.001f)
            return;

        Quaternion rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            rotation,
            lookTurnSpeed * Time.deltaTime
        );
    }

    private void ResolvePlayer()
    {
        if (player != null)
            return;

        if (PlayerController.instance != null)
        {
            player = PlayerController.instance.transform;
            return;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
            player = taggedPlayer.transform;
    }

    private void StopLookRoutine()
    {
        if (lookRoutine == null)
            return;

        StopCoroutine(lookRoutine);
        lookRoutine = null;
    }

    private void ApplyKnockback(Transform attacker)
    {
        if (knockbackDistance <= 0f || knockbackDuration <= 0f)
            return;

        Vector3 direction;
        if (attacker != null)
        {
            direction = transform.position - attacker.position;
            direction.y = 0f;
        }
        else
        {
            direction = -transform.forward;
        }

        if (direction.sqrMagnitude < 0.001f)
            direction = -transform.forward;

        direction.Normalize();

        StopKnockbackRoutine();
        knockbackRoutine = StartCoroutine(KnockbackRoutine(direction));
    }

    private IEnumerator KnockbackRoutine(Vector3 direction)
    {
        StopAgent();

        float elapsed = 0f;
        while (elapsed < knockbackDuration && state != EnemyState.Dead)
        {
            float delta = Time.deltaTime;
            float move = knockbackDistance / knockbackDuration * delta;

            if (AgentReady())
            {
                agent.isStopped = true;
                agent.Move(direction * move);
            }
            else
            {
                transform.position += direction * move;
            }

            elapsed += delta;
            yield return null;
        }

        knockbackRoutine = null;

        if (state == EnemyState.Attack && AgentReady())
            agent.isStopped = false;
    }

    private void StopKnockbackRoutine()
    {
        if (knockbackRoutine == null)
            return;

        StopCoroutine(knockbackRoutine);
        knockbackRoutine = null;
    }

    private void NotifyCombatActive(bool isActive)
    {
        if (health != null)
            health.SetCombatActive(isActive);
    }

    private void Log(string message)
    {
        if (!enableLogs)
            return;

        Debug.Log($"[SimpleEnemy] {name}: {message}", this);
    }

    private void UpdateAnimatorSpeed()
    {
        if (animator == null || string.IsNullOrEmpty(speedParam))
            return;

        float speed = agent != null && agent.enabled ? agent.velocity.magnitude : 0f;
        animator.SetFloat(speedParam, speed);
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDetectionGizmos)
            return;

        DrawSector(frontViewRadius, frontViewAngle, new Color(0f, 0.8f, 1f, 0.7f));
        DrawRearSector(new Color(1f, 0.65f, 0f, 0.7f));

        Gizmos.color = Color.red;
        Vector3 center = attackPoint != null
            ? attackPoint.position
            : transform.position + transform.forward * Mathf.Max(1f, attackRange * 0.6f) + Vector3.up;
        Gizmos.DrawWireSphere(center, attackHitRadius);
    }

    private void DrawSector(float radius, float angle, Color color)
    {
        Gizmos.color = color;
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        float half = angle * 0.5f;
        Vector3 previous = origin + Quaternion.Euler(0f, -half, 0f) * forward.normalized * radius;
        Gizmos.DrawLine(origin, previous);

        const int segments = 24;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float yaw = Mathf.Lerp(-half, half, t);
            Vector3 next = origin + Quaternion.Euler(0f, yaw, 0f) * forward.normalized * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }

        Gizmos.DrawLine(origin, previous);
    }

    private void DrawRearSector(Color color)
    {
        Gizmos.color = color;
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        float frontHalf = frontViewAngle * 0.5f;
        Vector3 previous = origin + Quaternion.Euler(0f, frontHalf, 0f) * forward.normalized * rearViewRadius;
        Gizmos.DrawLine(origin, previous);

        const int segments = 32;
        for (int i = 1; i <= segments; i++)
        {
            float yaw = Mathf.Lerp(frontHalf, 360f - frontHalf, i / (float)segments);
            Vector3 next = origin + Quaternion.Euler(0f, yaw, 0f) * forward.normalized * rearViewRadius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }

        Gizmos.DrawLine(origin, previous);
    }
}
