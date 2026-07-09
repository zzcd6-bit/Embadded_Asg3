using UnityEngine;

public class PlayerElementInfusion : MonoBehaviour
{
    [Header("Current Infusion")]
    [SerializeField] private ElementType currentElement = ElementType.Physical;
    [SerializeField] private float remainingTime;
    [SerializeField] private float damageMultiplier = 1f;

    [Header("Burning On Hit")]
    [SerializeField] private bool applyBurningOnHit;
    [SerializeField] private float burningDuration = 5f;
    [SerializeField] private float burningTickInterval = 1f;
    [SerializeField] private int burningTickDamage = 1;

    [Header("Debug")]
    public bool debugLog = true;

    public ElementType CurrentElement
    {
        get { return currentElement; }
    }

    public bool HasElementInfusion
    {
        get
        {
            return currentElement != ElementType.None &&
                   currentElement != ElementType.Physical &&
                   remainingTime > 0f;
        }
    }

    private void Update()
    {
        if (remainingTime <= 0f)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            ClearInfusion();
        }
    }

    public void ApplyFireInfusion(
        float duration,
        float newDamageMultiplier,
        bool newApplyBurningOnHit,
        float newBurningDuration,
        float newBurningTickInterval,
        int newBurningTickDamage
    )
    {
        currentElement = ElementType.Fire;
        remainingTime = duration;
        damageMultiplier = Mathf.Max(1f, newDamageMultiplier);

        applyBurningOnHit = newApplyBurningOnHit;
        burningDuration = newBurningDuration;
        burningTickInterval = newBurningTickInterval;
        burningTickDamage = newBurningTickDamage;

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerElementInfusion] Fire infusion applied. " +
                $"duration={duration}, damageMultiplier={damageMultiplier}",
                this
            );
        }
    }

    public void ApplyToDamageInfo(ref DamageInfo damageInfo)
    {
        if (!HasElementInfusion)
            return;

        damageInfo.element = currentElement;
        damageInfo.canApplyElementStatus = true;

        damageInfo.damage = Mathf.Max(
            1,
            Mathf.RoundToInt(damageInfo.damage * damageMultiplier)
        );
    }

    public void ApplyElementStatusToTarget(GameObject targetObject, GameObject attacker)
    {
        if (!HasElementInfusion)
            return;

        if (targetObject == null)
            return;

        if (currentElement != ElementType.Fire)
            return;

        if (!applyBurningOnHit)
            return;

        ElementStatusController statusController =
            targetObject.GetComponent<ElementStatusController>();

        if (statusController == null)
        {
            statusController = targetObject.GetComponentInParent<ElementStatusController>();
        }

        if (statusController == null)
        {
            statusController = targetObject.AddComponent<ElementStatusController>();
        }

        statusController.ApplyBurning(
            attacker,
            burningDuration,
            burningTickInterval,
            burningTickDamage
        );
    }

    public void ClearInfusion()
    {
        currentElement = ElementType.Physical;
        remainingTime = 0f;
        damageMultiplier = 1f;

        applyBurningOnHit = false;

        if (debugLog)
        {
            Debug.Log("[PlayerElementInfusion] Infusion cleared.", this);
        }
    }
}