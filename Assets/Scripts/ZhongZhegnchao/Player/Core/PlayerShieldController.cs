using System;
using UnityEngine;

public class PlayerShieldController : MonoBehaviour
{
    [Header("Shield State")]
    [SerializeField] private bool hasShield;
    [SerializeField] private int currentShieldValue;
    [SerializeField] private float shieldRemainingTime;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public event Action ShieldApplied;
    public event Action ShieldCleared;

    public bool HasShield
    {
        get
        {
            return hasShield &&
                   currentShieldValue > 0 &&
                   shieldRemainingTime > 0f;
        }
    }

    public int CurrentShieldValue
    {
        get { return currentShieldValue; }
    }

    public float ShieldRemainingTime
    {
        get { return shieldRemainingTime; }
    }

    private void Update()
    {
        if (!hasShield)
            return;

        shieldRemainingTime -= Time.deltaTime;

        if (shieldRemainingTime <= 0f)
        {
            ClearShield();
        }
    }

    public void ApplyShield(
        int shieldAmount,
        float duration,
        bool refreshShieldWhenReapply
    )
    {
        shieldAmount = Mathf.Max(0, shieldAmount);
        duration = Mathf.Max(0f, duration);

        if (shieldAmount <= 0 || duration <= 0f)
            return;

        hasShield = true;

        if (refreshShieldWhenReapply)
        {
            currentShieldValue = shieldAmount;
            shieldRemainingTime = duration;
        }
        else
        {
            currentShieldValue = Mathf.Max(
                currentShieldValue,
                shieldAmount
            );

            shieldRemainingTime = Mathf.Max(
                shieldRemainingTime,
                duration
            );
        }

        ShieldApplied?.Invoke();

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerShieldController] Shield applied. Value={currentShieldValue}, Duration={shieldRemainingTime:F2}",
                this
            );
        }
    }

    public int AbsorbDamage(int incomingDamage)
    {
        if (!HasShield)
            return incomingDamage;

        incomingDamage = Mathf.Max(0, incomingDamage);

        if (incomingDamage <= 0)
            return 0;

        int absorbedDamage = Mathf.Min(
            currentShieldValue,
            incomingDamage
        );

        currentShieldValue -= absorbedDamage;
        incomingDamage -= absorbedDamage;

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerShieldController] Shield absorbed {absorbedDamage}. Remaining Shield={currentShieldValue}",
                this
            );
        }

        if (currentShieldValue <= 0)
        {
            ClearShield();
        }

        return incomingDamage;
    }

    public void ClearShield()
    {
        bool hadShieldBeforeClear =
            hasShield ||
            currentShieldValue > 0 ||
            shieldRemainingTime > 0f;

        hasShield = false;
        currentShieldValue = 0;
        shieldRemainingTime = 0f;

        if (hadShieldBeforeClear)
        {
            ShieldCleared?.Invoke();
        }

        if (debugLog)
        {
            Debug.Log("[PlayerShieldController] Shield cleared.", this);
        }
    }
}