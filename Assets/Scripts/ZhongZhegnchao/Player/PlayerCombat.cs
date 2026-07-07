using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Normal Attack")]
    public Transform attackPoint;
    public Vector3 attackBoxSize = new Vector3(1.5f, 1.2f, 1.5f);
    public int damage = 1;
    public LayerMask enemyLayer;
    public float attackCooldown = 0.4f;

    private float nextAttackTime;
    private bool canAttack = true;

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener(E_EventType.E_Player_NormalAttack, OnNormalAttackInput);
        EventCenter.Instance.AddEventListener<bool>(E_EventType.E_Player_CombatEnable,OnCombatEnable);
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Player_NormalAttack, OnNormalAttackInput);
        EventCenter.Instance.RemoveEventListener<bool>(E_EventType.E_Player_CombatEnable,OnCombatEnable);
    }

    private void OnCombatEnable(bool state)
    {
        canAttack = state;
    }

    private void OnNormalAttackInput()
    {
        if (!canAttack)
            return;

        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;
        NormalAttack();
    }

    private void NormalAttack()
    {
        if (attackPoint == null)
        {
            Debug.LogWarning("AttackPoint is missing.");
            return;
        }

        Collider[] hits = Physics.OverlapBox(
            attackPoint.position,
            attackBoxSize * 0.5f,
            transform.rotation,
            enemyLayer
        );

        foreach (Collider hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
        }

        Debug.Log("Normal Attack");
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(
            attackPoint.position,
            transform.rotation,
            Vector3.one
        );

        Gizmos.DrawWireCube(Vector3.zero, attackBoxSize);
        Gizmos.matrix = oldMatrix;
    }
}