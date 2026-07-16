using System.Collections;
using UnityEngine;

public class ElementStatusController : MonoBehaviour
{
    [Header("Current Element Status")]
    [SerializeField] private bool hasFireStatus;
    [SerializeField] private float fireStatusRemainingTime;

    [Header("Fire Burning Settings")]
    [SerializeField] private bool addBurningDurationWhenReapply = true;
    [SerializeField] private float maxFireStatusDuration = 15f;

    [SerializeField] private float currentBurningTickInterval = 1f;
    [SerializeField] private int currentBurningTickDamage = 1;
    [SerializeField] private GameObject burningOwner;
    [SerializeField] private float burningTickTimer;

    [Header("Water Status")]
    [SerializeField] private bool isWet;
    [SerializeField] private float wetRemainingTime;
    [SerializeField] private GameObject wetOwner;

    [Header("Reaction Settings")]
    [SerializeField] private float vaporizeMultiplier = 1.5f;

    [Header("元素反应内置 CD")]
    public bool useReactionInternalCooldown = true;

    [Tooltip("蒸发反应内置 CD")]
    public float vaporizeInternalCooldown = 0.8f;

    [SerializeField] private float nextVaporizeAllowedTime;

    [Header("Debug")]
    public bool debugLog = true;

    private Coroutine burningCoroutine;
    private IDamageable damageable;

    public bool HasFireStatus
    {
        get { return hasFireStatus; }
    }

    public float FireStatusRemainingTime
    {
        get { return fireStatusRemainingTime; }
    }

    public bool IsWet
    {
        get { return isWet; }
    }

    public float WetRemainingTime
    {
        get { return wetRemainingTime; }
    }

    private void Awake()
    {
        damageable = GetComponent<IDamageable>();

        if (damageable == null)
        {
            damageable = GetComponentInParent<IDamageable>();
        }
    }

    private void Update()
    {
        UpdateWetStatus();
    }

    private void UpdateWetStatus()
    {
        if (!isWet)
            return;

        wetRemainingTime -= Time.deltaTime;

        if (wetRemainingTime <= 0f)
        {
            ClearWetStatus();
        }
    }

    public void ApplyBurning(
        GameObject attacker,
        float duration,
        float tickInterval,
        int tickDamage
    )
    {
        if (duration <= 0f)
            return;

        if (tickInterval <= 0f)
            tickInterval = 1f;

        tickDamage = Mathf.Max(0, tickDamage);

        burningOwner = attacker;
        currentBurningTickInterval = tickInterval;
        currentBurningTickDamage = tickDamage;

        if (hasFireStatus)
        {
            if (addBurningDurationWhenReapply)
            {
                fireStatusRemainingTime += duration;
            }
            else
            {
                fireStatusRemainingTime = Mathf.Max(
                    fireStatusRemainingTime,
                    duration
                );
            }

            fireStatusRemainingTime = Mathf.Min(
                fireStatusRemainingTime,
                maxFireStatusDuration
            );

            if (burningCoroutine == null)
            {
                burningCoroutine = StartCoroutine(BurningRoutine());
            }

            if (debugLog)
            {
                Debug.Log(
                    $"[ElementStatusController] Burning reapplied to {gameObject.name}. " +
                    $"Remaining={fireStatusRemainingTime:F2}s, " +
                    $"Tick={currentBurningTickInterval}, Damage={currentBurningTickDamage}",
                    this
                );
            }

            return;
        }

        hasFireStatus = true;
        fireStatusRemainingTime = Mathf.Min(duration, maxFireStatusDuration);
        burningTickTimer = 0f;

        if (burningCoroutine != null)
        {
            StopCoroutine(burningCoroutine);
            burningCoroutine = null;
        }

        burningCoroutine = StartCoroutine(BurningRoutine());

        if (debugLog)
        {
            Debug.Log(
                $"[ElementStatusController] Burning applied to {gameObject.name}. " +
                $"duration={duration}, tick={tickInterval}, damage={tickDamage}",
                this
            );
        }
    }

    private IEnumerator BurningRoutine()
    {
        while (hasFireStatus && fireStatusRemainingTime > 0f)
        {
            float deltaTime = Time.deltaTime;

            fireStatusRemainingTime -= deltaTime;
            fireStatusRemainingTime = Mathf.Max(0f, fireStatusRemainingTime);

            burningTickTimer += deltaTime;

            if (burningTickTimer >= currentBurningTickInterval)
            {
                burningTickTimer = 0f;

                if (damageable != null && currentBurningTickDamage > 0)
                {
                    DamageInfo damageInfo = new DamageInfo
                    {
                        attacker = burningOwner,
                        target = gameObject,
                        damage = currentBurningTickDamage,
                        knockback = 0f,
                        hitPoint = transform.position,
                        hitDirection = Vector3.zero,
                        sourceAction = null,

                        element = ElementType.Fire,
                        canApplyElementStatus = false,

                        skillMultiplier = 0f,
                        damageBonus = 0f,
                        reactionMultiplier = 1f,
                        canCrit = false
                    };

                    damageable.TakeDamage(damageInfo);

                    if (debugLog)
                    {
                        Debug.Log(
                            $"[ElementStatusController] Burning tick damage = {currentBurningTickDamage}, " +
                            $"remaining={fireStatusRemainingTime:F2}",
                            this
                        );
                    }
                }
            }

            yield return null;
        }

        ClearFireStatus(false);
    }

    public void ApplyWet(GameObject attacker, float duration)
    {
        if (duration <= 0f)
            return;

        isWet = true;
        wetOwner = attacker;

        wetRemainingTime = Mathf.Max(
            wetRemainingTime,
            duration
        );

        if (debugLog)
        {
            Debug.Log(
                $"[ElementStatusController] Wet applied to {gameObject.name}. duration={duration}",
                this
            );
        }
    }

    public void ClearWetStatus()
    {
        if (!isWet && wetRemainingTime <= 0f)
            return;

        isWet = false;
        wetOwner = null;
        wetRemainingTime = 0f;

        if (debugLog)
        {
            Debug.Log(
                $"[ElementStatusController] Wet cleared from {gameObject.name}.",
                this
            );
        }
    }

    public bool HasVaporizePair(
    ElementType incomingElement
)
    {
        if (incomingElement == ElementType.Water &&
            HasFireStatus)
        {
            return true;
        }

        if (incomingElement == ElementType.Fire &&
            IsWet)
        {
            return true;
        }

        return false;
    }

    public bool CanTriggerVaporizeNow(
        ElementType incomingElement
    )
    {
        if (!HasVaporizePair(incomingElement))
            return false;

        return CanTriggerVaporize();
    }

    public DamageInfo ProcessIncomingElement(DamageInfo damageInfo)
    {
        if (damageInfo.element == ElementType.Water && IsBurning())
        {
            if (!CanTriggerVaporize())
            {
                if (debugLog)
                {
                    Debug.Log(
                        $"[ElementStatusController] Vaporize is cooling down. Remaining={GetVaporizeCooldownRemaining():F2}s",
                        this
                    );
                }

                return damageInfo;
            }

            damageInfo.reactionType = ElementReactionType.Vaporize;
            damageInfo.reactionMultiplier *= vaporizeMultiplier;

            ClearFireStatus();
            StartVaporizeCooldown();

            if (debugLog)
            {
                Debug.Log(
                    "[ElementStatusController] Vaporize triggered: Water hit Fire. Water damage increased.",
                    this
                );
            }

            return damageInfo;
        }

        if (damageInfo.element == ElementType.Fire && isWet)
        {
            if (!CanTriggerVaporize())
            {
                if (debugLog)
                {
                    Debug.Log(
                        $"[ElementStatusController] Vaporize is cooling down. Remaining={GetVaporizeCooldownRemaining():F2}s",
                        this
                    );
                }

                return damageInfo;
            }

            damageInfo.reactionType = ElementReactionType.Vaporize;
            damageInfo.reactionMultiplier *= vaporizeMultiplier;

            ClearWetStatus();
            StartVaporizeCooldown();

            if (debugLog)
            {
                Debug.Log(
                    "[ElementStatusController] Vaporize triggered: Fire hit Wet. Fire damage increased.",
                    this
                );
            }

            return damageInfo;
        }

        return damageInfo;
    }

    private bool CanTriggerVaporize()
    {
        if (!useReactionInternalCooldown)
            return true;

        return Time.time >= nextVaporizeAllowedTime;
    }

    private void StartVaporizeCooldown()
    {
        if (!useReactionInternalCooldown)
            return;

        nextVaporizeAllowedTime = Time.time + vaporizeInternalCooldown;
    }

    private float GetVaporizeCooldownRemaining()
    {
        if (!useReactionInternalCooldown)
            return 0f;

        return Mathf.Max(0f, nextVaporizeAllowedTime - Time.time);
    }

    private bool IsBurning()
    {
        return hasFireStatus;
    }

    private void StopBurning()
    {
        if (burningCoroutine != null)
        {
            StopCoroutine(burningCoroutine);
            burningCoroutine = null;
        }
    }

    public void ClearFireStatus()
    {
        ClearFireStatus(true);
    }

    private void ClearFireStatus(bool stopCoroutine)
    {
        if (stopCoroutine)
        {
            StopBurning();
        }

        hasFireStatus = false;
        fireStatusRemainingTime = 0f;
        burningTickTimer = 0f;
        burningOwner = null;

        if (!stopCoroutine)
        {
            burningCoroutine = null;
        }

        if (debugLog)
        {
            Debug.Log(
                $"[ElementStatusController] Fire status cleared on {gameObject.name}.",
                this
            );
        }
    }

    public void StopAllElementStatus()
    {
        ClearFireStatus();
        ClearWetStatus();
    }
}