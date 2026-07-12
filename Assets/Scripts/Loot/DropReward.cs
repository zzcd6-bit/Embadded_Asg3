using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class DropReward : MonoBehaviour
{
    [Serializable]
    public class DropEntry
    {
        [SerializeField] private GameObject prefab;
        [SerializeField, Range(0f, 1f)] private float chance = 1f;
        [SerializeField, Min(0)] private int minCount = 1;
        [SerializeField, Min(0)] private int maxCount = 1;

        public GameObject Prefab => prefab;
        public float Chance => chance;
        public int MinCount => Mathf.Max(0, minCount);
        public int MaxCount => Mathf.Max(MinCount, maxCount);
    }

    [Header("Drops")]
    [SerializeField] private DropEntry[] entries = Array.Empty<DropEntry>();
    [SerializeField] private Transform dropOrigin;
    [SerializeField] private bool parentDropsToScene = true;

    [Header("Scatter")]
    [SerializeField, Min(0f)] private float spawnRadius = 0.35f;
    [SerializeField, Min(0f)] private float spawnHeight = 0.3f;
    [SerializeField] private bool randomYaw = true;

    [Header("Projectile")]
    [SerializeField] private bool applyImpulse = true;
    [SerializeField] private bool addRigidbodyIfMissing;
    [SerializeField, Min(0f)] private float minHorizontalImpulse = 1.5f;
    [SerializeField, Min(0f)] private float maxHorizontalImpulse = 3f;
    [SerializeField, Min(0f)] private float minUpwardImpulse = 2f;
    [SerializeField, Min(0f)] private float maxUpwardImpulse = 4f;
    [SerializeField, Min(0f)] private float maxTorqueImpulse = 4f;

    [Header("Events")]
    [SerializeField] private UnityEvent onDropped = new();

    private readonly List<GameObject> lastDropped = new();

    public IReadOnlyList<GameObject> LastDropped => lastDropped;
    public UnityEvent OnDropped => onDropped;

    public IReadOnlyList<GameObject> Drop()
    {
        Vector3 origin = dropOrigin != null ? dropOrigin.position : transform.position;
        return DropAt(origin);
    }

    public IReadOnlyList<GameObject> DropAt(Vector3 origin)
    {
        lastDropped.Clear();

        for (int i = 0; i < entries.Length; i++)
        {
            DropEntry entry = entries[i];
            if (entry == null || entry.Prefab == null)
            {
                continue;
            }

            if (UnityEngine.Random.value > entry.Chance)
            {
                continue;
            }

            int count = UnityEngine.Random.Range(entry.MinCount, entry.MaxCount + 1);
            for (int j = 0; j < count; j++)
            {
                GameObject item = CreateDrop(entry.Prefab, origin);
                if (item != null)
                {
                    lastDropped.Add(item);
                }
            }
        }

        if (lastDropped.Count > 0)
        {
            onDropped?.Invoke();
        }

        return lastDropped;
    }

    private GameObject CreateDrop(GameObject prefab, Vector3 origin)
    {
        Vector2 offset2D = UnityEngine.Random.insideUnitCircle * spawnRadius;
        Vector3 position = origin + new Vector3(offset2D.x, spawnHeight, offset2D.y);
        Quaternion rotation = randomYaw
            ? Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f)
            : prefab.transform.rotation;

        Transform parent = parentDropsToScene ? null : transform;
        GameObject item = Instantiate(prefab, position, rotation, parent);

        if (applyImpulse)
        {
            ApplyProjectileImpulse(item, origin);
        }

        return item;
    }

    private void ApplyProjectileImpulse(GameObject item, Vector3 origin)
    {
        Rigidbody body = item.GetComponent<Rigidbody>();
        if (body == null && addRigidbodyIfMissing)
        {
            body = item.AddComponent<Rigidbody>();
        }

        if (body == null)
        {
            return;
        }

        Vector3 direction = item.transform.position - origin;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            Vector2 random = UnityEngine.Random.insideUnitCircle.normalized;
            direction = new Vector3(random.x, 0f, random.y);
        }

        direction.Normalize();

        float horizontalImpulse = UnityEngine.Random.Range(minHorizontalImpulse, Mathf.Max(minHorizontalImpulse, maxHorizontalImpulse));
        float upwardImpulse = UnityEngine.Random.Range(minUpwardImpulse, Mathf.Max(minUpwardImpulse, maxUpwardImpulse));
        Vector3 impulse = direction * horizontalImpulse + Vector3.up * upwardImpulse;
        body.AddForce(impulse, ForceMode.Impulse);

        if (maxTorqueImpulse > 0f)
        {
            body.AddTorque(UnityEngine.Random.insideUnitSphere * maxTorqueImpulse, ForceMode.Impulse);
        }
    }

    private void OnValidate()
    {
        if (maxHorizontalImpulse < minHorizontalImpulse)
        {
            maxHorizontalImpulse = minHorizontalImpulse;
        }

        if (maxUpwardImpulse < minUpwardImpulse)
        {
            maxUpwardImpulse = minUpwardImpulse;
        }
    }
}
