using UnityEngine;

[DisallowMultipleComponent]
public class DeathDropBridge : MonoBehaviour
{
    [SerializeField] private EnemyWhitebox enemyHealth;
    [SerializeField] private DropReward dropReward;
    [SerializeField] private bool dropOnlyOnce = true;

    private bool hasDropped;

    private void Reset()
    {
        enemyHealth = GetComponentInParent<EnemyWhitebox>();
        dropReward = GetComponent<DropReward>();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void LateUpdate()
    {
        if (enemyHealth == null || dropReward == null)
        {
            ResolveReferences();
            return;
        }

        if (!enemyHealth.IsDead)
        {
            return;
        }

        Drop();
    }

    public void Drop()
    {
        if (dropOnlyOnce && hasDropped)
        {
            return;
        }

        hasDropped = true;
        dropReward.Drop();
    }

    private void ResolveReferences()
    {
        if (enemyHealth == null)
        {
            enemyHealth = GetComponentInParent<EnemyWhitebox>();
        }

        if (dropReward == null)
        {
            dropReward = GetComponent<DropReward>();
        }
    }
}