using UnityEngine;

public class EnemyTargetable : MonoBehaviour
{
    [SerializeField] private Transform lockPoint;
    [SerializeField] private bool canBeLocked = true;

    public bool CanBeLocked
    {
        get
        {
            return canBeLocked && gameObject.activeInHierarchy;
        }
    }

    public Transform TargetPoint
    {
        get
        {
            if (lockPoint != null)
            {
                return lockPoint;
            }

            return transform;
        }
    }
}