using UnityEngine;
using System;
using System.Collections;

public class EnemyWhitebox : MonoBehaviour, IDamageable
{
    public static event Action<EnemyWhitebox> OnAnyEnemyDead;

    [Header("HP")]
    public int maxHp = 100;
    public int currentHp = 100;

    [Header("ª˜ÕÀ…Ë÷√")]
    public bool enableKnockback = true;
    public float knockbackDuration = 0.18f;
    public float knockbackVerticalForce = 0f;
    public bool knockbackOnlyHorizontal = true;

    private ElementVfxController elementVfxController;
    private DamageNumberAnchor damageNumberAnchor;

    [Header("Debug")]
    public bool debugLog = true;

    private bool isDead;
    private ElementStatusController elementStatusController;

    private CharacterController characterController;
    private Rigidbody enemyRigidbody;
    private Coroutine knockbackCoroutine;

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

        characterController = GetComponent<CharacterController>();

        if (characterController == null)
        {
            characterController = GetComponentInChildren<CharacterController>();
        }

        enemyRigidbody = GetComponent<Rigidbody>();

        if (enemyRigidbody == null)
        {
            enemyRigidbody = GetComponentInChildren<Rigidbody>();
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

        if (damage > 0)
        {
            ApplyKnockback(calculatedDamage);
        }

        if (debugLog)
        {
            Debug.Log(
                $"[EnemyDamageReceiver] Damage={damage}, " +
                $"Element={calculatedDamage.element}, " +
                $"Reaction={calculatedDamage.reactionType}, " +
                $"Crit={calculatedDamage.isCritical}, " +
                $"Knockback={calculatedDamage.knockback}, " +
                $"HP={currentHp}/{maxHp}",
                this
            );
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void ApplyKnockback(DamageInfo damageInfo)
    {
        if (!enableKnockback)
            return;

        if (damageInfo.knockback <= 0f)
            return;

        Vector3 direction = damageInfo.hitDirection;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            if (damageInfo.attacker != null)
            {
                direction = transform.position - damageInfo.attacker.transform.position;
            }
            else
            {
                direction = -transform.forward;
            }
        }

        if (knockbackOnlyHorizontal)
        {
            direction.y = 0f;
        }

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        direction.Normalize();

        if (!knockbackOnlyHorizontal && knockbackVerticalForce > 0f)
        {
            direction.y += knockbackVerticalForce;
            direction.Normalize();
        }

        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
        }

        knockbackCoroutine = StartCoroutine(
            KnockbackRoutine(direction, damageInfo.knockback)
        );
    }

    private IEnumerator KnockbackRoutine(Vector3 direction, float force)
    {
        float timer = 0f;

        while (timer < knockbackDuration)
        {
            float deltaTime = Time.deltaTime;
            timer += deltaTime;

            float normalizedTime = Mathf.Clamp01(timer / knockbackDuration);

            float strength = Mathf.Lerp(
                force,
                0f,
                normalizedTime
            );

            Vector3 move = direction * strength * deltaTime;

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(move);
            }
            else if (enemyRigidbody != null && !enemyRigidbody.isKinematic)
            {
                enemyRigidbody.MovePosition(enemyRigidbody.position + move);
            }
            else
            {
                transform.position += move;
            }

            yield return null;
        }

        knockbackCoroutine = null;
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        currentHp = 0;

        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }

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