using UnityEngine;
using System;

public class EnemyWhitebox : MonoBehaviour, IDamageable
{
    public static event Action<EnemyWhitebox> OnAnyEnemyDead;

    [Header("HP")]
    public int maxHp = 100;
    public int currentHp = 100;

    private ElementVfxController elementVfxController;
    private DamageNumberAnchor damageNumberAnchor;

    [Header("Debug")]
    public bool debugLog = true;

    private bool isDead;
    private ElementStatusController elementStatusController;

    public bool IsDead
    {
        get { return isDead; }
    }

    private void Awake()
    {
        currentHp = maxHp;
        isDead = false;

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
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isDead)
        {
            return;
        }

        if (elementStatusController != null)
        {
            damageInfo = elementStatusController.ProcessIncomingElement(damageInfo);
        }

        DamageInfo calculatedDamage = DamageCalculator.Calculate(damageInfo);

        int damage = Mathf.Max(0, calculatedDamage.finalDamage);

        currentHp -= damage;
        currentHp = Mathf.Max(0, currentHp);

        if (DamageNumberSpawner.Instance != null && damage > 0)
        {
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

        if (debugLog)
        {
            Debug.Log(
                $"[EnemyDamageReceiver] Damage={damage}, " +
                $"Element={calculatedDamage.element}, " +
                $"Reaction={calculatedDamage.reactionType}, " +
                $"Crit={calculatedDamage.isCritical}, " +
                $"HP={currentHp}/{maxHp}",
                this
            );
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        currentHp = 0;

        if (elementStatusController != null)
        {
            elementStatusController.StopAllElementStatus();
        }

        if (elementVfxController != null)
        {
            elementVfxController.StopAllElementVfx();
        }

        OnAnyEnemyDead?.Invoke(this);

        Debug.Log("[EnemyDamageReceiver] Enemy Dead.", this);
    }
}