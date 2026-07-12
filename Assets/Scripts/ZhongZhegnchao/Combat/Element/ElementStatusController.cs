using System.Collections;
using UnityEngine;

public class ElementStatusController : MonoBehaviour
{
    [Header("Current Element Status")]
    [SerializeField] private bool hasFireStatus;
    [SerializeField] private float fireStatusRemainingTime;

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

        hasFireStatus = true;
        fireStatusRemainingTime = duration;

        if (burningCoroutine != null)
        {
            StopCoroutine(burningCoroutine);
        }

        burningCoroutine = StartCoroutine(
            BurningRoutine(attacker, duration, tickInterval, tickDamage)
        );

        if (debugLog)
        {
            Debug.Log(
                $"[ElementStatusController] Burning applied to {gameObject.name}. " +
                $"duration={duration}, tick={tickInterval}, damage={tickDamage}",
                this
            );
        }
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
    public void ApplyWet(GameObject attacker, float duration)
    {
        isWet = true;
        wetOwner = attacker;
        wetRemainingTime = Mathf.Max(wetRemainingTime, duration);

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

    public DamageInfo ProcessIncomingElement(DamageInfo damageInfo)
    {
        // Water 打到 Fire / Burning，触发蒸发，增强这一次 Water 伤害
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

        // Fire 打到 Wet，触发蒸发，增强这一次 Fire 伤害
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

    private IEnumerator BurningRoutine(
        GameObject attacker,
        float duration,
        float tickInterval,
        int tickDamage
    )
    {
        float timer = duration;

        while (timer > 0f)
        {
            yield return new WaitForSeconds(tickInterval);

            timer -= tickInterval;
            fireStatusRemainingTime = Mathf.Max(0f, timer);

            if (damageable != null && tickDamage > 0)
            {
                DamageInfo damageInfo = new DamageInfo
                {
                    attacker = attacker,
                    target = gameObject,
                    damage = tickDamage,
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
                        $"[ElementStatusController] Burning tick damage = {tickDamage}, " +
                        $"remaining={fireStatusRemainingTime}",
                        this
                    );
                }
            }
        }

        ClearFireStatus();
    }

    public void ClearFireStatus()
    {
        hasFireStatus = false;
        fireStatusRemainingTime = 0f;
        burningCoroutine = null;

        if (debugLog)
        {
            Debug.Log($"[ElementStatusController] Fire status cleared on {gameObject.name}.", this);
        }
    }

    public void StopAllElementStatus()
    {
        StopBurning();
        ClearFireStatus();
    }
}