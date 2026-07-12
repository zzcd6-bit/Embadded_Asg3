using System;
using System.Collections;
using UnityEngine;

public class PlayerElementInfusion : MonoBehaviour
{
    public event Action<bool> FireInfusionStateChanged;

    [Header("µ±Ç°¸½Ä§×´Ì¬")]
    [SerializeField] private bool isFireInfused;

    [Header("»ðÑæ¸½Ä§²ÎÊý")]
    [SerializeField] private float fireDamageMultiplier = 1.5f;
    [SerializeField] private bool fireApplyBurningOnHit = true;
    [SerializeField] private float fireBurningDuration = 5f;
    [SerializeField] private float fireBurningTickInterval = 1f;
    [SerializeField] private int fireBurningTickDamage = 1;

    [Header("Debug")]
    public bool debugLog = true;

    private Coroutine infusionCoroutine;

    public bool IsFireInfused
    {
        get { return isFireInfused; }
    }

    public void ApplyFireInfusion(
        float duration,
        float damageMultiplier,
        bool applyBurningOnHit,
        float burningDuration,
        float burningTickInterval,
        int burningTickDamage
    )
    {
        fireDamageMultiplier = damageMultiplier;
        fireApplyBurningOnHit = applyBurningOnHit;
        fireBurningDuration = burningDuration;
        fireBurningTickInterval = burningTickInterval;
        fireBurningTickDamage = burningTickDamage;

        if (!isFireInfused)
        {
            isFireInfused = true;
            FireInfusionStateChanged?.Invoke(true);
        }

        if (infusionCoroutine != null)
        {
            StopCoroutine(infusionCoroutine);
        }

        infusionCoroutine = StartCoroutine(FireInfusionTimer(duration));

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerElementInfusion] Fire infusion applied. Duration={duration}",
                this
            );
        }
    }

    private IEnumerator FireInfusionTimer(float duration)
    {
        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }

        ClearInfusion();
    }

    public void ClearInfusion()
    {
        if (infusionCoroutine != null)
        {
            StopCoroutine(infusionCoroutine);
            infusionCoroutine = null;
        }

        if (!isFireInfused)
            return;

        isFireInfused = false;

        FireInfusionStateChanged?.Invoke(false);

        if (debugLog)
        {
            Debug.Log("[PlayerElementInfusion] Fire infusion cleared.", this);
        }
    }

    public void ApplyToDamageInfo(ref DamageInfo damageInfo)
    {
        if (!isFireInfused)
            return;

        damageInfo.element = ElementType.Fire;
        damageInfo.canApplyElementStatus = true;

        damageInfo.damage = Mathf.RoundToInt(
            damageInfo.damage * fireDamageMultiplier
        );
    }

    public void ApplyElementStatusToTarget(GameObject targetObject, GameObject attacker)
    {
        if (!isFireInfused)
            return;

        if (!fireApplyBurningOnHit)
            return;

        if (targetObject == null)
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
            fireBurningDuration,
            fireBurningTickInterval,
            fireBurningTickDamage
        );
    }
}