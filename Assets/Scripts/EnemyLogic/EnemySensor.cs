using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemySensor : MonoBehaviour
{
    [Tooltip("只要玩家进入该 Trigger，就认为近距离感知成立")]
    public bool PlayerInside { get; private set; }

    void Reset()
    {
        // 确保这是 Trigger
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInside = true;
            Debug.Log("See player nearby");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInside = false;
            Debug.Log("Lost Vision nearby");
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 仅用于调试可视化
        Gizmos.color = PlayerInside ? Color.red : Color.yellow;
        var col = GetComponent<Collider>();
        if (col is SphereCollider sc)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(sc.center, sc.radius);
        }
    }
#endif
}
