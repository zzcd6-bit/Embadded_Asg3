using System.Collections;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

public class ActionPlayer : MonoBehaviour, IHitStopReceiver
{
    [Header("Animancer")]
    [SerializeField] private AnimancerComponent animancer;

    [Header("调试")]
    [SerializeField] private bool logHitWithoutDamageable = false;

    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private PlayerInputReceiver inputReceiver;

    private ActionConfig currentAction;
    private AnimancerState currentState;
    private CharacterController characterController;

    [SerializeField] private PlayerActionConfigSet actionConfigSet;

    [SerializeField] private PlayerLocomotion locomotion;
    [SerializeField] private PlayerElementInfusion elementInfusion;

    private bool lockedMovementByAction;

    private float currentTime;
    private float previousTime;

    private float currentBaseSpeed = 1f;
    private float hitStopSpeedMultiplier = 1f;

    private bool isPlaying;

    private readonly HashSet<int> triggeredEventIndexes = new HashSet<int>();
    private readonly Dictionary<int, HashSet<Collider>> hitTargets = new Dictionary<int, HashSet<Collider>>();
    private readonly List<AudioSource> activeLoopAudioSources = new List<AudioSource>();

    private Coroutine hitStopCoroutine;
    private int actionPlaybackVersion;

    public bool IsPlaying
    {
        get { return isPlaying; }
    }

    public float CurrentTime
    {
        get { return currentTime; }
    }

    public ActionConfig CurrentAction
    {
        get { return currentAction; }
    }

    public void Init(
        PlayerAnimationController animation,
        PlayerInputReceiver input,
        PlayerLocomotion playerLocomotion
    )
    {
        animationController = animation;
        inputReceiver = input;
        locomotion = playerLocomotion;

        if (animancer == null)
        {
            animancer = GetComponentInChildren<AnimancerComponent>();
        }

        if (animancer == null)
        {
            Debug.LogError("[ActionPlayer] Init 失败：没有找到 AnimancerComponent。");
        }
    }

    private void Awake()
    {
        if (animancer == null)
        {
            animancer = GetComponentInChildren<AnimancerComponent>();
        }

        if (animationController == null)
        {
            animationController = GetComponent<PlayerAnimationController>();
        }

        if (inputReceiver == null)
        {
            inputReceiver = GetComponent<PlayerInputReceiver>();
        }

        if (locomotion == null)
        {
            locomotion = GetComponent<PlayerLocomotion>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (elementInfusion == null)
        {
            elementInfusion = GetComponent<PlayerElementInfusion>();
        }
    }

    private void Update()
    {
        if (!isPlaying || currentAction == null)
        {
            return;
        }

        float actionLength = currentAction.GetLength();

        previousTime = currentTime;

        if (currentState != null)
        {
            currentTime = Mathf.Min((float)currentState.Time, actionLength);
        }
        else
        {
            currentTime += Time.deltaTime;
        }

        ApplyActiveSpeedEvent(currentTime);
        UpdateActiveMovementEvents(previousTime, currentTime);
        TriggerPointEvents(previousTime, currentTime);
        UpdateActiveHitBoxes(currentTime);

        if (currentTime >= actionLength)
        {
            StopAction();
        }
    }

    private void UpdateActiveMovementEvents(float fromTime, float toTime)
    {
        if (currentAction == null || currentAction.events == null)
        {
            return;
        }

        for (int i = 0; i < currentAction.events.Count; i++)
        {
            ActionEventData actionEvent = currentAction.events[i];

            if (actionEvent == null || !actionEvent.enabled)
            {
                continue;
            }

            if (actionEvent.type != ActionEventType.Movement)
            {
                continue;
            }

            bool isActive =
                toTime >= actionEvent.startTime &&
                fromTime <= actionEvent.EndTime;

            if (!isActive)
            {
                continue;
            }

            ApplyMovementEvent(
                actionEvent.movement,
                actionEvent.startTime,
                actionEvent.duration,
                fromTime,
                toTime
            );
        }
    }

    private void ApplyMovementEvent(
        MovementEventData data,
        float startTime,
        float duration,
        float fromTime,
        float toTime
    )
    {
        if (data == null || duration <= 0f)
        {
            return;
        }

        float eventEndTime = startTime + duration;

        float clampedFrom = Mathf.Clamp(fromTime, startTime, eventEndTime);
        float clampedTo = Mathf.Clamp(toTime, startTime, eventEndTime);

        float normalizedFrom = Mathf.Clamp01((clampedFrom - startTime) / duration);
        float normalizedTo = Mathf.Clamp01((clampedTo - startTime) / duration);

        float curveFrom = data.moveCurve != null
            ? data.moveCurve.Evaluate(normalizedFrom)
            : normalizedFrom;

        float curveTo = data.moveCurve != null
            ? data.moveCurve.Evaluate(normalizedTo)
            : normalizedTo;

        float deltaRate = curveTo - curveFrom;

        if (Mathf.Abs(deltaRate) <= 0.0001f)
        {
            return;
        }

        Vector3 localDirection = data.localDirection;

        if (localDirection.sqrMagnitude <= 0.0001f)
        {
            localDirection = Vector3.forward;
        }

        if (data.horizontalOnly)
        {
            localDirection.y = 0f;
        }

        Vector3 worldDirection = transform.TransformDirection(localDirection.normalized);

        if (data.horizontalOnly)
        {
            worldDirection.y = 0f;
            worldDirection.Normalize();
        }

        Vector3 move = worldDirection * data.distance * deltaRate;

        if (characterController != null)
        {
            characterController.Move(move);
        }
        else
        {
            transform.position += move;
        }
    }

    public void PlayAction(ActionConfig config)
    {
        TryPlayAction(config, false);
    }

    public void PlayAction(ActionConfig config, bool forceInterrupt)
    {
        TryPlayAction(config, forceInterrupt);
    }

    public bool TryPlayAction(ActionConfig config)
    {
        return TryPlayAction(config, false);
    }

    public bool TryPlayAction(ActionConfig config, bool forceInterrupt)
    {
        if (config == null)
        {
            Debug.LogWarning("[ActionPlayer] TryPlayAction 失败：ActionConfig 为空。");
            return false;
        }

        if (config.animationClip == null)
        {
            Debug.LogWarning($"[ActionPlayer] TryPlayAction 失败：{config.name} 没有配置 AnimationClip。");
            return false;
        }

        if (animancer == null)
        {
            animancer = GetComponentInChildren<AnimancerComponent>();
        }

        if (animancer == null)
        {
            Debug.LogError("[ActionPlayer] TryPlayAction 失败：没有找到 AnimancerComponent。");
            return false;
        }

        if (isPlaying)
        {
            if (!forceInterrupt)
            {
                return false;
            }

            StopCurrentActionForInterrupt();
        }

        actionPlaybackVersion++;
        StopActionLoopAudio();

        animationController?.BeginAction();

        lockedMovementByAction = config.lockMovement;

        if (lockedMovementByAction && locomotion != null)
        {
            locomotion.CanMove = false;
        }

        currentAction = config;
        currentTime = 0f;
        previousTime = 0f;

        currentBaseSpeed = 1f;
        hitStopSpeedMultiplier = 1f;

        triggeredEventIndexes.Clear();
        hitTargets.Clear();

        isPlaying = true;

        currentState = animancer.Play(config.animationClip, config.fadeDuration);
        currentState.Time = 0f;
        currentState.Speed = 1f;

        return true;
    }

    public void InterruptAction()
    {
        if (!isPlaying && currentAction == null)
        {
            return;
        }

        StopCurrentActionForInterrupt();
    }

    private void StopCurrentActionForInterrupt()
    {
        actionPlaybackVersion++;
        StopActionLoopAudio();

        if (currentState != null)
        {
            currentState.Speed = 1f;
        }

        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
            hitStopCoroutine = null;
        }

        if (lockedMovementByAction && locomotion != null)
        {
            locomotion.CanMove = true;
        }

        lockedMovementByAction = false;

        isPlaying = false;
        currentAction = null;
        currentState = null;

        triggeredEventIndexes.Clear();
        hitTargets.Clear();

        currentTime = 0f;
        previousTime = 0f;

        currentBaseSpeed = 1f;
        hitStopSpeedMultiplier = 1f;
    }

    public void StopAction()
    {
        actionPlaybackVersion++;
        StopActionLoopAudio();

        ActionConfig finishedAction = currentAction;

        if (currentState != null)
        {
            currentState.Speed = 1f;
        }

        bool shouldReturnToLocomotion =
            finishedAction == null || finishedAction.returnToLocomotionOnEnd;

        float exitDelay =
            finishedAction != null
                ? finishedAction.exitToLocomotionDelay
                : 0f;

        bool useRunStart =
            finishedAction == null ||
            finishedAction.returnToMoveWithRunStart;

        if (lockedMovementByAction && locomotion != null)
        {
            locomotion.CanMove = true;
        }

        lockedMovementByAction = false;

        isPlaying = false;
        currentAction = null;
        currentState = null;

        triggeredEventIndexes.Clear();
        hitTargets.Clear();

        currentTime = 0f;
        previousTime = 0f;

        currentBaseSpeed = 1f;
        hitStopSpeedMultiplier = 1f;

        if (shouldReturnToLocomotion)
        {
            bool hasMoveInput =
                inputReceiver != null &&
                inputReceiver.MoveInput.magnitude > 0.1f;

            animationController?.EndAction(
                hasMoveInput,
                exitDelay,
                useRunStart
            );
        }
    }

    private void TriggerPointEvents(float fromTime, float toTime)
    {
        if (currentAction == null || currentAction.events == null)
        {
            return;
        }

        for (int i = 0; i < currentAction.events.Count; i++)
        {
            ActionEventData actionEvent = currentAction.events[i];

            if (actionEvent == null || !actionEvent.enabled)
            {
                continue;
            }

            if (triggeredEventIndexes.Contains(i))
            {
                continue;
            }

            bool crossed = fromTime <= actionEvent.startTime && toTime >= actionEvent.startTime;

            if (!crossed)
            {
                continue;
            }

            switch (actionEvent.type)
            {
                case ActionEventType.VFX:
                    TriggerVFX(actionEvent.vfx);
                    triggeredEventIndexes.Add(i);
                    break;

                case ActionEventType.HitStop:
                    TriggerHitStop(actionEvent.hitStop);
                    triggeredEventIndexes.Add(i);
                    break;

                case ActionEventType.Audio:
                    TriggerAudio(actionEvent.audio);
                    triggeredEventIndexes.Add(i);
                    break;
            }
        }
    }

    private void TriggerAudio(AudioEventData data)
    {
        if (data == null || string.IsNullOrEmpty(data.soundName))
        {
            return;
        }

        int playbackVersion = actionPlaybackVersion;

        MusicMgr.Instance.PlaySound(
            data.soundName,
            data.loop,
            data.isSync,
            source =>
            {
                if (source == null)
                {
                    return;
                }

                bool actionIsStillValid =
                    isPlaying &&
                    currentAction != null &&
                    playbackVersion == actionPlaybackVersion;

                if (!actionIsStillValid)
                {
                    MusicMgr.Instance.StopSound(source);
                    return;
                }

                if (data.loop && data.stopWhenActionEnds &&
                    !activeLoopAudioSources.Contains(source))
                {
                    activeLoopAudioSources.Add(source);
                }
            }
        );
    }

    private void StopActionLoopAudio()
    {
        for (int i = activeLoopAudioSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeLoopAudioSources[i];

            if (source != null)
            {
                MusicMgr.Instance.StopSound(source);
            }
        }

        activeLoopAudioSources.Clear();
    }

    private void ApplyActiveSpeedEvent(float time)
    {
        if (currentAction == null || currentAction.events == null || currentState == null)
        {
            return;
        }

        float speed = 1f;
        float latestStartTime = -1f;

        for (int i = 0; i < currentAction.events.Count; i++)
        {
            ActionEventData actionEvent = currentAction.events[i];

            if (actionEvent == null || !actionEvent.enabled)
            {
                continue;
            }

            if (actionEvent.type != ActionEventType.Speed)
            {
                continue;
            }

            bool active = time >= actionEvent.startTime && time <= actionEvent.EndTime;

            if (!active)
            {
                continue;
            }

            if (actionEvent.startTime >= latestStartTime)
            {
                latestStartTime = actionEvent.startTime;
                speed = Mathf.Max(0f, actionEvent.speed.animationSpeed);
            }
        }

        currentBaseSpeed = speed;
        RefreshAnimancerSpeed();
    }

    private void RefreshAnimancerSpeed()
    {
        if (currentState == null)
        {
            return;
        }

        currentState.Speed = currentBaseSpeed * hitStopSpeedMultiplier;
    }

    private void TriggerVFX(VFXEventData data)
    {
        if (data == null || data.prefab == null)
        {
            return;
        }

        GameObject obj = Instantiate(data.prefab);
        SetupVFXObject(obj, data);
    }

    private void SetupVFXObject(GameObject vfxObject, VFXEventData data)
    {
        if (vfxObject == null || data == null)
        {
            return;
        }

        Transform bindPoint = FindBindPoint(data.bindPointName);

        Vector3 worldPosition;
        Quaternion worldRotation;

        if (bindPoint != null)
        {
            worldPosition = bindPoint.TransformPoint(data.localPosition);
            worldRotation = bindPoint.rotation * Quaternion.Euler(data.localEulerAngles);
        }
        else
        {
            worldPosition = transform.TransformPoint(data.localPosition);
            worldRotation = transform.rotation * Quaternion.Euler(data.localEulerAngles);
        }

        vfxObject.transform.SetParent(null);
        vfxObject.transform.position = worldPosition;
        vfxObject.transform.rotation = worldRotation;
        vfxObject.transform.localScale = data.localScale;

        if (data.attachToBindPoint && bindPoint != null)
        {
            vfxObject.transform.SetParent(bindPoint, true);
        }

        RestartParticles(vfxObject);

        if (data.destroyDelay > 0f)
        {
            Destroy(vfxObject, data.destroyDelay);
        }
    }

    private void RestartParticles(GameObject vfxObject)
    {
        if (vfxObject == null)
        {
            return;
        }

        ParticleSystem[] particleSystems = vfxObject.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];

            ps.Clear(true);
            ps.Play(true);
        }
    }

    private Transform FindBindPoint(string bindPointName)
    {
        if (string.IsNullOrEmpty(bindPointName))
        {
            return null;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == bindPointName)
            {
                return children[i];
            }
        }

        return null;
    }

    private void UpdateActiveHitBoxes(float time)
    {
        if (currentAction == null || currentAction.events == null)
        {
            return;
        }

        for (int i = 0; i < currentAction.events.Count; i++)
        {
            ActionEventData actionEvent = currentAction.events[i];

            if (actionEvent == null || !actionEvent.enabled)
            {
                continue;
            }

            if (actionEvent.type != ActionEventType.HitBox)
            {
                continue;
            }

            bool active = time >= actionEvent.startTime && time <= actionEvent.EndTime;

            if (!active)
            {
                continue;
            }

            DetectHitBox(i, actionEvent.hitBox);
        }
    }

    private void DetectHitBox(int eventIndex, HitBoxEventData data)
    {
        if (data == null)
        {
            return;
        }

        if (!hitTargets.TryGetValue(eventIndex, out HashSet<Collider> alreadyHitSet))
        {
            alreadyHitSet = new HashSet<Collider>();
            hitTargets.Add(eventIndex, alreadyHitSet);
        }

        Collider[] colliders = QueryHitBox(data);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider hitCollider = colliders[i];

            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (data.hitOncePerTarget && alreadyHitSet.Contains(hitCollider))
            {
                continue;
            }

            alreadyHitSet.Add(hitCollider);

            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();

            Vector3 hitPoint = GetSafeClosestPoint(hitCollider, transform.position);

            Vector3 hitDirection = hitCollider.transform.position - transform.position;

            if (hitDirection.sqrMagnitude > 0.0001f)
            {
                hitDirection.Normalize();
            }
            else
            {
                hitDirection = transform.forward;
            }

            DamageInfo damageInfo = new DamageInfo
            {
                attacker = gameObject,
                target = hitCollider.gameObject,
                damage = data.damage,
                knockback = data.knockback,
                hitPoint = hitPoint,
                hitDirection = hitDirection,
                sourceAction = currentAction,

                element = ElementType.Physical,
                canApplyElementStatus = false,

                skillMultiplier = 1f,
                damageBonus = 0f,
                reactionMultiplier = 1f,
                canCrit = true
            };

            if (elementInfusion != null)
            {
                elementInfusion.ApplyToDamageInfo(ref damageInfo);
            }

            if (damageable != null)
            {
                damageable.TakeDamage(damageInfo);

                if (elementInfusion != null && damageInfo.canApplyElementStatus)
                {
                    Component damageComponent = damageable as Component;

                    GameObject targetObject = damageComponent != null
                        ? damageComponent.gameObject
                        : hitCollider.gameObject;

                    elementInfusion.ApplyElementStatusToTarget(
                        targetObject,
                        gameObject
                    );
                }

                TriggerHitFeedback(data, damageInfo);
            }
            else if (logHitWithoutDamageable)
            {
                Debug.Log($"[ActionPlayer] 命中 {hitCollider.name}，但是目标没有实现 IDamageable。");
            }
        }
    }

    public void SetActionConfigSet(PlayerActionConfigSet newActionConfigSet)
    {
        if (newActionConfigSet == null)
        {
            Debug.LogWarning("[ActionPlayer] SetActionConfigSet failed: newActionConfigSet is null.", this);
            return;
        }

        actionConfigSet = newActionConfigSet;

        Debug.Log(
            $"[ActionPlayer] ActionConfigSet changed to: {newActionConfigSet.name}",
            this
        );
    }
    private void TriggerHitFeedback(HitBoxEventData hitBoxData, DamageInfo damageInfo)
    {
        if (hitBoxData == null)
        {
            return;
        }

        if (hitBoxData.triggerHitStopOnHit)
        {
            TriggerHitStop(hitBoxData.hitStopOnHit);
        }

        if (hitBoxData.spawnHitVFXOnHit)
        {
            Vector3 hitPoint = damageInfo.hitPoint;

            if (hitPoint == Vector3.zero && damageInfo.target != null)
            {
                hitPoint = damageInfo.target.transform.position;
            }

            Vector3 effectForward = -damageInfo.hitDirection;

            if (effectForward.sqrMagnitude <= 0.0001f)
            {
                effectForward = transform.forward;
            }

            Quaternion hitRotation = Quaternion.LookRotation(effectForward.normalized);

            TriggerVFXAtWorldPosition(hitBoxData.hitVFX, hitPoint, hitRotation);
        }
    }

    private void TriggerVFXAtWorldPosition(VFXEventData data, Vector3 position, Quaternion rotation)
    {
        if (data == null || data.prefab == null)
        {
            return;
        }

        GameObject obj = Instantiate(data.prefab);
        SetupVFXObjectAtWorldPosition(obj, data, position, rotation);
    }

    private void SetupVFXObjectAtWorldPosition(
    GameObject vfxObject,
    VFXEventData data,
    Vector3 position,
    Quaternion rotation
)
    {
        if (vfxObject == null || data == null)
        {
            return;
        }

        vfxObject.transform.SetParent(null);
        vfxObject.transform.position = position + rotation * data.localPosition;
        vfxObject.transform.rotation = rotation * Quaternion.Euler(data.localEulerAngles);
        vfxObject.transform.localScale = data.localScale;

        RestartParticles(vfxObject);

        if (data.destroyDelay > 0f)
        {
            Destroy(vfxObject, data.destroyDelay);
        }
    }

    private Collider[] QueryHitBox(HitBoxEventData data)
    {
        Vector3 center = transform.TransformPoint(data.localOffset);
        Quaternion rotation = transform.rotation * Quaternion.Euler(data.localEulerAngles);

        Vector3 lossyScale = AbsVector3(transform.lossyScale);
        Vector3 scaledSize = Vector3.Scale(data.size, lossyScale);

        float maxScale = Mathf.Max(lossyScale.x, lossyScale.y, lossyScale.z);
        float scaledRadius = data.radius * maxScale;
        float scaledCapsuleHeight = data.capsuleHeight * maxScale;

        switch (data.shape)
        {
            case HitShapeType.Box:
                return Physics.OverlapBox(
                    center,
                    scaledSize * 0.5f,
                    rotation,
                    data.targetLayer,
                    data.queryTriggerInteraction
                );

            case HitShapeType.Sphere:
                return Physics.OverlapSphere(
                    center,
                    scaledRadius,
                    data.targetLayer,
                    data.queryTriggerInteraction
                );

            case HitShapeType.Capsule:
                return QueryCapsule(data, center, rotation, scaledRadius, scaledCapsuleHeight);

            case HitShapeType.Sector:
                return QuerySector(data, center, rotation, scaledRadius);

            default:
                return new Collider[0];
        }
    }

    private Collider[] QueryCapsule(
        HitBoxEventData data,
        Vector3 center,
        Quaternion rotation,
        float radius,
        float height
    )
    {
        float halfLineHeight = Mathf.Max(0f, height * 0.5f - radius);

        Vector3 pointA = center + rotation * Vector3.up * halfLineHeight;
        Vector3 pointB = center - rotation * Vector3.up * halfLineHeight;

        return Physics.OverlapCapsule(
            pointA,
            pointB,
            radius,
            data.targetLayer,
            data.queryTriggerInteraction
        );
    }

    private Collider[] QuerySector(
        HitBoxEventData data,
        Vector3 center,
        Quaternion rotation,
        float radius
    )
    {
        Collider[] sphereResults = Physics.OverlapSphere(
            center,
            radius,
            data.targetLayer,
            data.queryTriggerInteraction
        );

        List<Collider> sectorResults = new List<Collider>();

        Vector3 forward = rotation * Vector3.forward;
        float halfAngle = data.sectorAngle * 0.5f;

        for (int i = 0; i < sphereResults.Length; i++)
        {
            Collider target = sphereResults[i];

            if (target == null)
            {
                continue;
            }

            Vector3 direction = target.transform.position - center;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                continue;
            }

            float angle = Vector3.Angle(forward, direction.normalized);

            if (angle <= halfAngle)
            {
                sectorResults.Add(target);
            }
        }

        return sectorResults.ToArray();
    }

    private void TriggerHitStop(HitStopEventData data)
    {
        if (data == null)
        {
            return;
        }

        if (data.affectSelf)
        {
            ApplyHitStop(data.stopDuration, data.animationSpeedMultiplier);
        }

        if (!data.affectObjectsInRange)
        {
            return;
        }

        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            data.affectedRadius,
            data.affectedLayer,
            QueryTriggerInteraction.Collide
        );

        HashSet<IHitStopReceiver> receivers = new HashSet<IHitStopReceiver>();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider target = colliders[i];

            if (target == null)
            {
                continue;
            }

            if (target.transform.IsChildOf(transform))
            {
                continue;
            }

            IHitStopReceiver receiver = target.GetComponentInParent<IHitStopReceiver>();

            if (receiver != null)
            {
                receivers.Add(receiver);
            }
        }

        foreach (IHitStopReceiver receiver in receivers)
        {
            receiver.ApplyHitStop(data.stopDuration, data.animationSpeedMultiplier);
        }
    }

    public void ApplyHitStop(float duration, float speedMultiplier)
    {
        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
        }

        hitStopCoroutine = StartCoroutine(HitStopRoutine(duration, speedMultiplier));
    }

    private IEnumerator HitStopRoutine(float duration, float speedMultiplier)
    {
        hitStopSpeedMultiplier = Mathf.Clamp01(speedMultiplier);
        RefreshAnimancerSpeed();

        yield return new WaitForSecondsRealtime(duration);

        hitStopSpeedMultiplier = 1f;
        RefreshAnimancerSpeed();

        hitStopCoroutine = null;
    }

    private Vector3 AbsVector3(Vector3 value)
    {
        return new Vector3(
            Mathf.Abs(value.x),
            Mathf.Abs(value.y),
            Mathf.Abs(value.z)
        );
    }

    public bool IsActionPlaying
    {
        get { return currentAction != null; }
    }

    private Vector3 GetSafeClosestPoint(Collider targetCollider, Vector3 fromPosition)
    {
        if (targetCollider == null)
        {
            return fromPosition;
        }

        if (targetCollider is BoxCollider ||
            targetCollider is SphereCollider ||
            targetCollider is CapsuleCollider)
        {
            return targetCollider.ClosestPoint(fromPosition);
        }

        MeshCollider meshCollider = targetCollider as MeshCollider;

        if (meshCollider != null && meshCollider.convex)
        {
            return targetCollider.ClosestPoint(fromPosition);
        }

        return targetCollider.bounds.ClosestPoint(fromPosition);
    }
}