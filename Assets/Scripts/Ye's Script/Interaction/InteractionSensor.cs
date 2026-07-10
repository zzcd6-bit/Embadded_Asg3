using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class InteractionSensor : MonoBehaviour
{
    [Header("Filter")]
    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private bool searchInParents = true;

    private readonly Dictionary<IReactable, HashSet<Collider>> overlapColliders = new();
    private readonly Dictionary<Collider, IReactable> colliderOwners = new();

    private SphereCollider sensorCollider;

    public event Action<IReactable> ReactableEntered;
    public event Action<IReactable> ReactableExited;

    public float Radius
    {
        get => sensorCollider != null ? sensorCollider.radius : 0f;
        set
        {
            if (sensorCollider != null)
            {
                sensorCollider.radius = Mathf.Max(0f, value);
            }
        }
    }

    private void Awake()
    {
        sensorCollider = GetComponent<SphereCollider>();
        sensorCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsLayerAllowed(other.gameObject.layer) || colliderOwners.ContainsKey(other))
        {
            return;
        }

        IReactable reactable = FindReactable(other);
        if (!IsValidReactable(reactable))
        {
            return;
        }

        colliderOwners.Add(other, reactable);

        if (!overlapColliders.TryGetValue(reactable, out HashSet<Collider> colliders))
        {
            colliders = new HashSet<Collider>();
            overlapColliders.Add(reactable, colliders);
        }

        bool wasEmpty = colliders.Count == 0;
        colliders.Add(other);

        if (wasEmpty)
        {
            ReactableEntered?.Invoke(reactable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!colliderOwners.TryGetValue(other, out IReactable reactable))
        {
            return;
        }

        colliderOwners.Remove(other);

        if (!overlapColliders.TryGetValue(reactable, out HashSet<Collider> colliders))
        {
            return;
        }

        colliders.Remove(other);
        if (colliders.Count > 0)
        {
            return;
        }

        overlapColliders.Remove(reactable);
        ReactableExited?.Invoke(reactable);
    }

    public void CleanupInvalidEntries()
    {
        List<IReactable> invalidReactables = null;
        List<Collider> invalidColliders = null;

        foreach (KeyValuePair<Collider, IReactable> pair in colliderOwners)
        {
            if (pair.Key != null && IsValidReactable(pair.Value))
            {
                continue;
            }

            invalidColliders ??= new List<Collider>();
            invalidColliders.Add(pair.Key);
        }

        if (invalidColliders != null)
        {
            foreach (Collider invalidCollider in invalidColliders)
            {
                colliderOwners.Remove(invalidCollider);
            }
        }

        foreach (KeyValuePair<IReactable, HashSet<Collider>> pair in overlapColliders)
        {
            pair.Value.RemoveWhere(collider => collider == null);

            if (!IsValidReactable(pair.Key) || pair.Value.Count == 0)
            {
                invalidReactables ??= new List<IReactable>();
                invalidReactables.Add(pair.Key);
            }
        }

        if (invalidReactables == null)
        {
            return;
        }

        foreach (IReactable reactable in invalidReactables)
        {
            overlapColliders.Remove(reactable);
            ReactableExited?.Invoke(reactable);
        }
    }

    public void ForceRemove(IReactable reactable)
    {
        if (reactable == null)
        {
            return;
        }

        overlapColliders.Remove(reactable);

        List<Collider> collidersToRemove = null;
        foreach (KeyValuePair<Collider, IReactable> pair in colliderOwners)
        {
            if (!ReferenceEquals(pair.Value, reactable))
            {
                continue;
            }

            collidersToRemove ??= new List<Collider>();
            collidersToRemove.Add(pair.Key);
        }

        if (collidersToRemove != null)
        {
            foreach (Collider collider in collidersToRemove)
            {
                colliderOwners.Remove(collider);
            }
        }

        ReactableExited?.Invoke(reactable);
    }

    private bool IsLayerAllowed(int layer)
    {
        return (interactableLayers.value & (1 << layer)) != 0;
    }

    private IReactable FindReactable(Collider targetCollider)
    {
        if (!searchInParents)
        {
            return FindReactableOnObject(targetCollider.gameObject);
        }

        Transform current = targetCollider.transform;
        while (current != null)
        {
            IReactable reactable = FindReactableOnObject(current.gameObject);
            if (reactable != null)
            {
                return reactable;
            }

            current = current.parent;
        }

        return null;
    }

    private static IReactable FindReactableOnObject(GameObject target)
    {
        MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IReactable reactable)
            {
                return reactable;
            }
        }

        return null;
    }

    private static bool IsValidReactable(IReactable reactable)
    {
        if (reactable == null)
        {
            return false;
        }

        return reactable is not UnityEngine.Object unityObject || unityObject != null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere == null)
        {
            return;
        }

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireSphere(sphere.center, sphere.radius);
    }
#endif
}
