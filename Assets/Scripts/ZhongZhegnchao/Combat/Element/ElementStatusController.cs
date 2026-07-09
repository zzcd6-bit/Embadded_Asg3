using System.Collections;
using UnityEngine;

public class ElementStatusController : MonoBehaviour
{
    [Header("Current Element Status")]
    [SerializeField] private bool hasFireStatus;
    [SerializeField] private float fireStatusRemainingTime;

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

    public DamageInfo ProcessIncomingElement(DamageInfo damageInfo)
    {
        if (damageInfo.element == ElementType.None ||
            damageInfo.element == ElementType.Physical)
        {
            return damageInfo;
        }

        if (hasFireStatus)
        {
            ElementReactionType reactionType;
            float reactionMultiplier;
            bool shouldClearFire;

            bool reacted = ElementReactionCalculator.TryReactWithFire(
                damageInfo.element,
                out reactionType,
                out reactionMultiplier,
                out shouldClearFire
            );

            if (reacted)
            {
                damageInfo.reactionType = reactionType;
                damageInfo.reactionMultiplier *= reactionMultiplier;

                if (debugLog)
                {
                    Debug.Log(
                        $"[ElementStatusController] Reaction triggered: {reactionType}, " +
                        $"multiplier={reactionMultiplier}",
                        this
                    );
                }

                if (shouldClearFire)
                {
                    StopBurning();
                    ClearFireStatus();
                }
            }
        }

        return damageInfo;
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