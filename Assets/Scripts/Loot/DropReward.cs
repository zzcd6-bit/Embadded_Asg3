using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class DropReward : MonoBehaviour
{
    [Serializable]
    public class DropEntry
    {
        [SerializeField]
        private GameObject prefab;

        [SerializeField]
        [Range(0f, 1f)]
        private float chance = 1f;

        [SerializeField]
        [Min(0)]
        private int minCount = 1;

        [SerializeField]
        [Min(0)]
        private int maxCount = 1;

        public GameObject Prefab => prefab;
        public float Chance => chance;
        public int MinCount => Mathf.Max(0, minCount);
        public int MaxCount => Mathf.Max(MinCount, maxCount);
    }

    [Header("Drops")]
    [SerializeField]
    private DropEntry[] entries = Array.Empty<DropEntry>();

    [SerializeField]
    private Transform dropOrigin;

    [SerializeField]
    private bool parentDropsToScene = true;

    [Header("Instant Rewards")]
    [SerializeField]
    private bool grantInstantRewards = true;

    [SerializeField]
    private bool grantInstantRewardsOnlyOnce = true;

    [Header("Coin Reward")]
    [SerializeField]
    private bool grantCoins;

    [SerializeField]
    [Range(0f, 1f)]
    private float coinChance = 1f;

    [SerializeField]
    [Min(0)]
    private int minCoins;

    [SerializeField]
    [Min(0)]
    private int maxCoins;

    [SerializeField]
    private CurrencyChangeReason coinRewardReason =
        CurrencyChangeReason.EnemyReward;

    [Header("Experience Reward")]
    [SerializeField]
    private bool grantExperience;

    [SerializeField]
    [Range(0f, 1f)]
    private float experienceChance = 1f;

    [SerializeField]
    [Min(0)]
    private int minExperience;

    [SerializeField]
    [Min(0)]
    private int maxExperience;

    [Header("Reward Receiver")]
    [SerializeField]
    private PlayerCurrencyController currencyController;

    [SerializeField]
    private PlayerCharacterStatsController experienceReceiver;

    [Header("Scatter")]
    [SerializeField]
    [Min(0f)]
    private float spawnRadius = 0.35f;

    [SerializeField]
    [Min(0f)]
    private float spawnHeight = 0.3f;

    [SerializeField]
    private bool randomYaw = true;

    [Header("Initial Velocity")]
    [FormerlySerializedAs("applyImpulse")]
    [SerializeField]
    private bool applyInitialVelocity = true;

    [SerializeField]
    private bool addRigidbodyIfMissing;

    [FormerlySerializedAs("minHorizontalImpulse")]
    [SerializeField]
    [Min(0f)]
    private float minHorizontalSpeed = 0f;

    [FormerlySerializedAs("maxHorizontalImpulse")]
    [SerializeField]
    [Min(0f)]
    private float maxHorizontalSpeed = 0.15f;

    [FormerlySerializedAs("minUpwardImpulse")]
    [SerializeField]
    [Min(0f)]
    private float minUpwardSpeed = 1.2f;

    [FormerlySerializedAs("maxUpwardImpulse")]
    [SerializeField]
    [Min(0f)]
    private float maxUpwardSpeed = 1.8f;

    [Header("Events")]
    [SerializeField]
    private UnityEvent onDropped = new();

    [SerializeField]
    private UnityEvent<int> onCoinsRewarded = new();

    [SerializeField]
    private UnityEvent<int> onExperienceRewarded = new();

    private readonly List<GameObject> lastDropped = new();

    private bool hasGrantedInstantRewards;

    private int lastRewardedCoins;
    private int lastRewardedExperience;

    public IReadOnlyList<GameObject> LastDropped => lastDropped;
    public UnityEvent OnDropped => onDropped;

    public int LastRewardedCoins => lastRewardedCoins;
    public int LastRewardedExperience => lastRewardedExperience;

    public UnityEvent<int> OnCoinsRewarded => onCoinsRewarded;
    public UnityEvent<int> OnExperienceRewarded => onExperienceRewarded;

    private void Reset()
    {
        ResolveRewardReceivers();
    }

    private void Awake()
    {
        ResolveRewardReceivers();
    }

    public IReadOnlyList<GameObject> Drop()
    {
        Vector3 origin =
            dropOrigin != null
                ? dropOrigin.position
                : transform.position;

        return DropAt(origin);
    }

    public IReadOnlyList<GameObject> DropAt(Vector3 origin)
    {
        lastDropped.Clear();

        lastRewardedCoins = 0;
        lastRewardedExperience = 0;

        DropPhysicalItems(origin);
        GrantInstantRewards();

        if (lastDropped.Count > 0)
        {
            onDropped?.Invoke();
        }

        return lastDropped;
    }

    private void DropPhysicalItems(Vector3 origin)
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Length; i++)
        {
            DropEntry entry = entries[i];

            if (entry == null ||
                entry.Prefab == null)
            {
                continue;
            }

            if (UnityEngine.Random.value >
                entry.Chance)
            {
                continue;
            }

            int count =
                UnityEngine.Random.Range(
                    entry.MinCount,
                    entry.MaxCount + 1
                );

            for (int j = 0; j < count; j++)
            {
                GameObject item =
                    CreateDrop(
                        entry.Prefab,
                        origin
                    );

                if (item != null)
                {
                    lastDropped.Add(item);
                }
            }
        }
    }

    private void GrantInstantRewards()
    {
        if (!grantInstantRewards)
            return;

        if (grantInstantRewardsOnlyOnce &&
            hasGrantedInstantRewards)
        {
            return;
        }

        hasGrantedInstantRewards = true;

        GrantCoins();
        GrantExperience();
    }

    private void GrantCoins()
    {
        if (!grantCoins)
            return;

        int amount =
            GetRandomRewardAmount(
                coinChance,
                minCoins,
                maxCoins
            );

        if (amount <= 0)
            return;

        ResolveRewardReceivers();

        if (currencyController == null)
        {
            Debug.LogWarning(
                "[DropReward] Coin reward failed. " +
                "PlayerCurrencyController not found.",
                this
            );

            return;
        }

        lastRewardedCoins =
            currencyController.AddCoins(
                amount,
                coinRewardReason,
                this
            );

        if (lastRewardedCoins > 0)
        {
            onCoinsRewarded?.Invoke(
                lastRewardedCoins
            );
        }
    }

    private void GrantExperience()
    {
        if (!grantExperience)
            return;

        int amount =
            GetRandomRewardAmount(
                experienceChance,
                minExperience,
                maxExperience
            );

        if (amount <= 0)
            return;

        ResolveRewardReceivers();

        if (experienceReceiver == null)
        {
            Debug.LogWarning(
                "[DropReward] Experience reward failed. " +
                "PlayerCharacterStatsController not found.",
                this
            );

            return;
        }

        experienceReceiver.AddExperience(
            amount
        );

        lastRewardedExperience = amount;

        onExperienceRewarded?.Invoke(
            lastRewardedExperience
        );
    }

    private int GetRandomRewardAmount(
        float chance,
        int minAmount,
        int maxAmount
    )
    {
        if (UnityEngine.Random.value > chance)
            return 0;

        int safeMin =
            Mathf.Max(
                0,
                minAmount
            );

        int safeMax =
            Mathf.Max(
                safeMin,
                maxAmount
            );

        if (safeMax <= 0)
            return 0;

        return UnityEngine.Random.Range(
            safeMin,
            safeMax + 1
        );
    }

    private GameObject CreateDrop(
        GameObject prefab,
        Vector3 origin
    )
    {
        Vector2 offset2D =
            UnityEngine.Random.insideUnitCircle *
            spawnRadius;

        Vector3 position =
            origin +
            new Vector3(
                offset2D.x,
                spawnHeight,
                offset2D.y
            );

        Quaternion rotation =
            randomYaw
                ? Quaternion.Euler(
                    0f,
                    UnityEngine.Random.Range(0f, 360f),
                    0f
                )
                : prefab.transform.rotation;

        Transform parent =
            parentDropsToScene
                ? null
                : transform;

        GameObject item =
            Instantiate(
                prefab,
                position,
                rotation,
                parent
            );

        if (applyInitialVelocity)
        {
            ApplyInitialVelocity(
                item,
                origin
            );
        }

        return item;
    }

    private void ApplyInitialVelocity(
        GameObject item,
        Vector3 origin
    )
    {
        Rigidbody body =
            item.GetComponent<Rigidbody>();

        if (body == null &&
            addRigidbodyIfMissing)
        {
            body =
                item.AddComponent<Rigidbody>();
        }

        if (body == null)
        {
            return;
        }

        Vector3 direction =
            item.transform.position -
            origin;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            Vector2 random =
                UnityEngine.Random
                    .insideUnitCircle
                    .normalized;

            direction =
                new Vector3(
                    random.x,
                    0f,
                    random.y
                );
        }

        direction.Normalize();

        float horizontalSpeed =
            UnityEngine.Random.Range(
                minHorizontalSpeed,
                Mathf.Max(
                    minHorizontalSpeed,
                    maxHorizontalSpeed
                )
            );

        float upwardSpeed =
            UnityEngine.Random.Range(
                minUpwardSpeed,
                Mathf.Max(
                    minUpwardSpeed,
                    maxUpwardSpeed
                )
            );

        body.isKinematic = false;
        body.useGravity = true;

        body.linearVelocity =
            direction * horizontalSpeed +
            Vector3.up * upwardSpeed;

        body.angularVelocity =
            Vector3.zero;
    }

    private void ResolveRewardReceivers()
    {
        if (currencyController == null)
        {
            currencyController =
                FindAnyObjectByType<PlayerCurrencyController>();
        }

        if (experienceReceiver == null)
        {
            experienceReceiver =
                FindAnyObjectByType<PlayerCharacterStatsController>();
        }
    }

    private void OnValidate()
    {
        if (maxHorizontalSpeed <
            minHorizontalSpeed)
        {
            maxHorizontalSpeed =
                minHorizontalSpeed;
        }

        if (maxUpwardSpeed <
            minUpwardSpeed)
        {
            maxUpwardSpeed =
                minUpwardSpeed;
        }

        if (maxCoins < minCoins)
        {
            maxCoins = minCoins;
        }

        if (maxExperience < minExperience)
        {
            maxExperience =
                minExperience;
        }
    }
}