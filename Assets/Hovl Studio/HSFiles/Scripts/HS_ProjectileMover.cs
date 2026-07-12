using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace Hovl
{
    public class HS_ProjectileMover : MonoBehaviour
    {
        [SerializeField] protected float speed = 15f;
        [SerializeField] protected float hitOffset = 0f;
        [SerializeField] protected bool UseFirePointRotation;
        [SerializeField] protected Vector3 rotationOffset = Vector3.zero;

        [Header("Pool")]
        [SerializeField] protected bool usePool = true;
        [SerializeField] protected string poolName = "";
        public Action<GameObject> OnRecycleRequested;

        [Header("Arc Homing")]
        [SerializeField] protected bool useArcHoming = true;
        [SerializeField] protected float arcHeight = 2f;
        [SerializeField] protected float rotateSpeed = 12f;
        [SerializeField] protected float reachDistance = 0.45f;
        [SerializeField] protected Vector3 targetOffset = new Vector3(0f, 1f, 0f);

        [Header("Damage")]
        [SerializeField] protected bool applyDamage = true;
        [SerializeField] protected int damage = 8;
        [SerializeField] protected float knockback = 1f;
        [SerializeField] protected float skillMultiplier = 1f;
        [SerializeField] protected ElementType element = ElementType.Water;
        [SerializeField] protected bool canApplyElementStatus = true;
        [SerializeField] protected float wetDuration = 5f;

        [Header("Effects")]
        [SerializeField] protected GameObject hit;
        [SerializeField] protected ParticleSystem hitPS;
        [SerializeField] protected GameObject flash;
        [SerializeField] protected ParticleSystem projectilePS;
        [SerializeField] protected GameObject[] Detached;

        [Header("Components")]
        [SerializeField] protected Rigidbody rb;
        [SerializeField] protected Collider col;
        [SerializeField] protected Light lightSourse;

        [Header("Lifetime")]
        [SerializeField] protected bool notDestroy = true;
        [SerializeField] protected float lifeTime = 5f;
        [SerializeField] protected float detachedLifeTime = 1f;

        [Header("Collision Filter")]
        [SerializeField] protected LayerMask hitLayer = ~0;
        [SerializeField] protected bool ignoreAttackerCollision = true;
        [SerializeField] protected bool destroyOnNonHitLayerCollision = false;

        [Header("Arc Variation")]
        [SerializeField] protected float runtimeArcSideOffset = 0f;
        [SerializeField] protected float runtimeArcHeightOffset = 0f;
        [SerializeField] protected float runtimeSpeedOffset = 0f;
        [SerializeField] protected Vector3 runtimeTargetOffset = Vector3.zero;

        protected Vector3 arcSideDirection;

        protected readonly List<Collider> ignoredAttackerColliders = new List<Collider>();

        protected bool initialized;
        protected bool collided;
        protected Coroutine lifeRoutine;
        protected Coroutine disableAfterHitRoutine;

        protected GameObject attacker;
        protected Transform target;
        protected Vector3 startPosition;
        protected float travelTimer;
        protected float travelDuration = 1f;

        [System.Serializable]
        protected class DetachedState
        {
            public GameObject obj;
            public Transform originalParent;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
        }

        protected DetachedState[] detachedStates;

        protected virtual void Awake()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody>();

            if (col == null)
                col = GetComponent<Collider>();

            SetupDetachedCache();
        }

        protected virtual void Start()
        {
            initialized = true;

            if (flash != null)
                flash.transform.SetParent(null, true);

            StartLifeTimer();
        }

        protected virtual void OnEnable()
        {
            ResetProjectileState();

            if (!initialized)
                return;

            StartLifeTimer();
        }

        protected virtual void OnDisable()
        {
            RestoreIgnoredAttackerCollisions();
            StopRunningCoroutines();
        }

        public virtual void Init(
    GameObject newAttacker,
    Transform newTarget,
    int newDamage,
    float newKnockback,
    float newWetDuration
)
        {
            RestoreIgnoredAttackerCollisions();

            attacker = newAttacker;
            target = newTarget;
            damage = newDamage;
            knockback = newKnockback;
            wetDuration = newWetDuration;

            ResetProjectileState();
            SetupArcData();
            IgnoreAttackerCollisions();
            StartLifeTimer();
        }

        public virtual void Init(
            GameObject newAttacker,
            Transform newTarget
        )
        {
            RestoreIgnoredAttackerCollisions();

            attacker = newAttacker;
            target = newTarget;

            ResetProjectileState();
            SetupArcData();
            IgnoreAttackerCollisions();
            StartLifeTimer();
        }

        protected virtual void ResetProjectileState()
        {
            collided = false;
            travelTimer = 0f;
            startPosition = transform.position;

            StopRunningCoroutines();

            if (flash != null)
                flash.transform.SetParent(null, true);

            if (lightSourse != null)
                lightSourse.enabled = true;

            if (col != null)
                col.enabled = true;

            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.None;

#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
            }

            if (projectilePS != null)
            {
                projectilePS.Clear(true);
                projectilePS.Play(true);
            }

            if (notDestroy)
                RestoreDetachedObjects();

            runtimeArcSideOffset = 0f;
            runtimeArcHeightOffset = 0f;
            runtimeSpeedOffset = 0f;
            runtimeTargetOffset = Vector3.zero;
            arcSideDirection = transform.right;
        }

        protected virtual void SetupArcData()
        {
            startPosition = transform.position;
            travelTimer = 0f;

            Vector3 endPosition = startPosition + transform.forward * 8f;

            if (target != null)
            {
                endPosition = target.position + targetOffset + runtimeTargetOffset;
            }

            Vector3 forward = endPosition - startPosition;
            forward.y = 0f;

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = transform.forward;
            }

            forward.Normalize();

            arcSideDirection = Vector3.Cross(Vector3.up, forward).normalized;

            float distance = Vector3.Distance(startPosition, endPosition);

            float finalSpeed = Mathf.Max(0.1f, speed + runtimeSpeedOffset);

            travelDuration = Mathf.Max(
                0.15f,
                distance / finalSpeed
            );
        }

        public virtual void SetRuntimeArcVariation(
    float sideOffset,
    float heightOffset,
    float speedOffset,
    Vector3 extraTargetOffset
)
        {
            runtimeArcSideOffset = sideOffset;
            runtimeArcHeightOffset = heightOffset;
            runtimeSpeedOffset = speedOffset;
            runtimeTargetOffset = extraTargetOffset;
        }

        protected virtual void StopRunningCoroutines()
        {
            if (lifeRoutine != null)
            {
                StopCoroutine(lifeRoutine);
                lifeRoutine = null;
            }

            if (disableAfterHitRoutine != null)
            {
                StopCoroutine(disableAfterHitRoutine);
                disableAfterHitRoutine = null;
            }
        }

        protected virtual void SetupDetachedCache()
        {
            if (Detached == null || Detached.Length == 0)
                return;

            detachedStates = new DetachedState[Detached.Length];

            for (int i = 0; i < Detached.Length; i++)
            {
                GameObject obj = Detached[i];

                if (obj == null)
                    continue;

                detachedStates[i] = new DetachedState
                {
                    obj = obj,
                    originalParent = obj.transform.parent,
                    localPosition = obj.transform.localPosition,
                    localRotation = obj.transform.localRotation,
                    localScale = obj.transform.localScale
                };
            }
        }

        protected virtual void RestoreDetachedObjects()
        {
            if (detachedStates == null || detachedStates.Length == 0)
                return;

            for (int i = 0; i < detachedStates.Length; i++)
            {
                DetachedState state = detachedStates[i];

                if (state == null || state.obj == null)
                    continue;

                Transform t = state.obj.transform;

                t.SetParent(state.originalParent, false);
                t.localPosition = state.localPosition;
                t.localRotation = state.localRotation;
                t.localScale = state.localScale;

                ParticleSystem[] systems =
                    state.obj.GetComponentsInChildren<ParticleSystem>(true);

                for (int j = 0; j < systems.Length; j++)
                {
                    ParticleSystem ps = systems[j];

                    if (ps == null)
                        continue;

                    ps.Clear(true);
                    ps.Play(true);
                }
            }
        }

        protected virtual void StartLifeTimer()
        {
            if (lifeRoutine != null)
                StopCoroutine(lifeRoutine);

            lifeRoutine = StartCoroutine(LifeTimerRoutine(lifeTime));
        }

        protected virtual IEnumerator LifeTimerRoutine(float time)
        {
            yield return new WaitForSeconds(time);

            RecycleOrDestroy();
        }

        protected virtual void FixedUpdate()
        {
            if (collided)
                return;

            if (useArcHoming && target != null)
            {
                UpdateArcHoming();
                return;
            }

            UpdateStraightMove();
        }

        protected virtual void UpdateStraightMove()
        {
            if (rb == null || speed == 0f)
                return;

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = transform.forward * speed;
#else
            rb.velocity = transform.forward * speed;
#endif
        }

        protected virtual void UpdateArcHoming()
        {
            if (target == null)
                return;

            travelTimer += Time.fixedDeltaTime;

            float t = Mathf.Clamp01(travelTimer / travelDuration);

            Vector3 endPosition =
                target.position +
                targetOffset +
                runtimeTargetOffset;

            Vector3 flatPosition = Vector3.Lerp(
                startPosition,
                endPosition,
                t
            );

            float finalArcHeight = Mathf.Max(
                0f,
                arcHeight + runtimeArcHeightOffset
            );

            float verticalArc = Mathf.Sin(t * Mathf.PI) * finalArcHeight;

            float sideArc = Mathf.Sin(t * Mathf.PI) * runtimeArcSideOffset;

            Vector3 nextPosition =
                flatPosition +
                Vector3.up * verticalArc +
                arcSideDirection * sideArc;

            Vector3 moveDirection = nextPosition - transform.position;

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(moveDirection.normalized);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotateSpeed * Time.fixedDeltaTime
                );
            }

            if (rb != null)
            {
                rb.MovePosition(nextPosition);
            }
            else
            {
                transform.position = nextPosition;
            }

            float distanceToTarget = Vector3.Distance(
                transform.position,
                endPosition
            );

            if (t >= 1f || distanceToTarget <= reachDistance)
            {
                HitTargetObject(
                    target.gameObject,
                    transform.position,
                    -transform.forward
                );
            }
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (collided)
                return;

            if (collision == null || collision.collider == null)
                return;

            if (ShouldIgnoreCollider(collision.collider))
                return;

            ContactPoint contact = default;

            if (collision.contactCount > 0)
            {
                contact = collision.contacts[0];
            }

            Vector3 hitPoint = collision.contactCount > 0
                ? contact.point
                : transform.position;

            Vector3 hitNormal = collision.contactCount > 0
                ? contact.normal
                : -transform.forward;

            HitTargetObject(collision.gameObject, hitPoint, hitNormal);

            if (collision.contactCount > 0)
            {
                SpawnHit(contact);
            }
        }

        protected virtual void HitTargetObject(
            GameObject hitObject,
            Vector3 hitPoint,
            Vector3 hitNormal
        )
        {
            if (collided)
                return;

            collided = true;
            StopRunningCoroutines();

            ApplyDamageToTarget(hitObject, hitPoint);

            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }

            if (lightSourse != null)
                lightSourse.enabled = false;

            if (col != null)
                col.enabled = false;

            ReleaseDetachedObjects();

            if (projectilePS != null)
                projectilePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            PlayHitEffect(hitPoint, hitNormal);

            float endDelay = 1f;

            if (hitPS != null)
                endDelay = Mathf.Max(hitPS.main.duration, 0.05f);

            disableAfterHitRoutine = StartCoroutine(DisableAfterHit(endDelay));
        }

        protected virtual bool ShouldIgnoreCollider(Collider targetCollider)
        {
            if (targetCollider == null)
                return true;

            if (attacker != null)
            {
                if (targetCollider.transform == attacker.transform ||
                    targetCollider.transform.IsChildOf(attacker.transform))
                {
                    return true;
                }
            }

            int targetLayerValue = 1 << targetCollider.gameObject.layer;
            bool isHitLayer = (hitLayer.value & targetLayerValue) != 0;

            if (!isHitLayer)
            {
                if (destroyOnNonHitLayerCollision)
                {
                    collided = true;
                    RecycleOrDestroy();
                }

                return true;
            }

            return false;
        }

        protected virtual void IgnoreAttackerCollisions()
        {
            if (!ignoreAttackerCollision)
                return;

            if (attacker == null)
                return;

            if (col == null)
                return;

            Collider[] attackerColliders =
                attacker.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < attackerColliders.Length; i++)
            {
                Collider attackerCollider = attackerColliders[i];

                if (attackerCollider == null)
                    continue;

                if (attackerCollider == col)
                    continue;

                Physics.IgnoreCollision(col, attackerCollider, true);
                ignoredAttackerColliders.Add(attackerCollider);
            }
        }

        protected virtual void RestoreIgnoredAttackerCollisions()
        {
            if (col == null)
            {
                ignoredAttackerColliders.Clear();
                return;
            }

            for (int i = 0; i < ignoredAttackerColliders.Count; i++)
            {
                Collider ignoredCollider = ignoredAttackerColliders[i];

                if (ignoredCollider == null)
                    continue;

                Physics.IgnoreCollision(col, ignoredCollider, false);
            }

            ignoredAttackerColliders.Clear();
        }

        protected virtual Vector3 GetSafeClosestPoint(
            Collider targetCollider,
            Vector3 fromPosition
        )
        {
            if (targetCollider == null)
                return fromPosition;

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
        protected virtual void ApplyDamageToTarget(GameObject hitObject, Vector3 hitPoint)
        {
            if (!applyDamage || hitObject == null)
                return;

            IDamageable damageable = hitObject.GetComponent<IDamageable>();

            if (damageable == null)
                damageable = hitObject.GetComponentInParent<IDamageable>();

            if (damageable == null)
                damageable = hitObject.GetComponentInChildren<IDamageable>();

            if (damageable == null)
                return;

            Vector3 direction = hitObject.transform.position - transform.position;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = transform.forward;
            }

            direction.Normalize();

            DamageInfo damageInfo = new DamageInfo
            {
                attacker = attacker != null ? attacker : gameObject,
                target = hitObject,
                damage = damage,
                knockback = knockback,
                hitPoint = hitPoint,
                hitDirection = direction,
                sourceAction = null,

                element = element,
                canApplyElementStatus = canApplyElementStatus,

                skillMultiplier = skillMultiplier,
                damageBonus = 0f,
                reactionMultiplier = 1f,
                canCrit = true
            };

            damageable.TakeDamage(damageInfo);

            if (canApplyElementStatus && element == ElementType.Water)
            {
                ApplyWetToTarget(hitObject);
            }
        }

        protected virtual void ApplyWetToTarget(GameObject hitObject)
        {
            ElementStatusController status =
                hitObject.GetComponent<ElementStatusController>();

            if (status == null)
                status = hitObject.GetComponentInParent<ElementStatusController>();

            if (status == null)
                status = hitObject.GetComponentInChildren<ElementStatusController>();

            if (status == null)
                status = hitObject.AddComponent<ElementStatusController>();

            status.ApplyWet(
                attacker != null ? attacker : gameObject,
                wetDuration
            );
        }

        protected virtual IEnumerator DisableAfterHit(float delay)
        {
            yield return new WaitForSeconds(delay);

            RecycleOrDestroy();
        }

        protected virtual void PlayHitEffect(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (hit == null)
                return;

            Vector3 pos = hitPoint + hitNormal * hitOffset;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, hitNormal);

            hit.transform.position = pos;
            hit.transform.rotation = rot;

            if (UseFirePointRotation)
            {
                hit.transform.rotation =
                    transform.rotation * Quaternion.Euler(0f, 180f, 0f);
            }
            else if (rotationOffset != Vector3.zero)
            {
                hit.transform.rotation = Quaternion.Euler(rotationOffset);
            }
            else
            {
                hit.transform.LookAt(hitPoint + hitNormal);
            }

            if (hitPS != null)
            {
                hitPS.Clear(true);
                hitPS.Play(true);
            }
        }

        protected virtual void SpawnHit(ContactPoint contact)
        {
            PlayHitEffect(
                contact.point,
                contact.normal
            );
        }

        protected virtual void ReleaseDetachedObjects()
        {
            if (detachedStates == null || detachedStates.Length == 0)
                return;

            for (int i = 0; i < detachedStates.Length; i++)
            {
                DetachedState state = detachedStates[i];

                if (state == null || state.obj == null)
                    continue;

                Transform t = state.obj.transform;
                t.SetParent(null, true);

                ParticleSystem[] systems =
                    state.obj.GetComponentsInChildren<ParticleSystem>(true);

                for (int j = 0; j < systems.Length; j++)
                {
                    ParticleSystem ps = systems[j];

                    if (ps == null)
                        continue;

                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }

                if (!notDestroy && !usePool)
                    Destroy(state.obj, detachedLifeTime);
            }
        }

        public virtual void ApplyWaterConfig(BrushSkillConfig config)
        {
            if (config == null)
                return;

            usePool = config.waterUsePool;

            speed = config.waterProjectileSpeed;
            lifeTime = config.waterProjectileLifeTime;

            useArcHoming = config.waterUseArcHoming;
            arcHeight = config.waterArcHeight;
            rotateSpeed = config.waterRotateSpeed;
            reachDistance = config.waterReachDistance;
            targetOffset = config.waterTargetOffset;

            damage = config.waterProjectileDamage;
            knockback = config.waterProjectileKnockback;
            wetDuration = config.wetDuration;

            hitLayer = config.waterProjectileHitLayer;
            ignoreAttackerCollision = config.waterIgnoreAttackerCollision;
            destroyOnNonHitLayerCollision = config.waterDestroyOnNonHitLayerCollision;

            element = ElementType.Water;
            canApplyElementStatus = true;
        }

        public virtual void RefreshArcData()
        {
            SetupArcData();
        }

        protected virtual void RecycleOrDestroy()
        {
            StopRunningCoroutines();

            if (usePool)
            {
                if (OnRecycleRequested != null)
                {
                    OnRecycleRequested.Invoke(gameObject);
                }
                else
                {
                    gameObject.SetActive(false);
                }

                return;
            }

            if (notDestroy)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}