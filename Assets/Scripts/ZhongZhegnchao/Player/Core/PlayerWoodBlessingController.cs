using System.Collections;
using UnityEngine;

public class PlayerWoodBlessingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Transform vfxRoot;
    [SerializeField] private Transform healNumberAnchor;
    [SerializeField] private PlayerShieldController shieldController;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private Coroutine woodRoutine;
    private Coroutine healVfxRecycleRoutine;

    private GameObject activeHealVfx;
    private bool activeHealVfxFromPool;

    private GameObject activeShieldVfx;
    private bool activeShieldVfxFromPool;

    private bool shieldVfxFollowPlayerPosition;
    private Vector3 shieldVfxWorldOffset;
    private Quaternion shieldVfxFixedWorldRotation = Quaternion.identity;

    private IHealable healReceiver;
    private PlayerShieldController boundShieldController;

    private void Awake()
    {
        if (playerRoot == null)
            playerRoot = transform;

        if (vfxRoot == null)
            vfxRoot = transform;

        if (healNumberAnchor == null)
            healNumberAnchor = transform;

        if (shieldController == null)
            shieldController = GetComponent<PlayerShieldController>();

        ResolveHealReceiver();
        BindShieldEvents();
    }

    private void OnEnable()
    {
        BindShieldEvents();
    }

    private void LateUpdate()
    {
        UpdateShieldVfxFollowPositionOnly();
    }
    private void OnDisable()
    {
        if (woodRoutine != null)
        {
            StopCoroutine(woodRoutine);
            woodRoutine = null;
        }

        StopHealVfxRecycleRoutine();

        RecycleHealVfx();
        RecycleShieldVfx();

        UnbindShieldEvents();
    }

    public void ApplyWoodBlessing(
        BrushSkillConfig config
    )
    {
        if (config == null)
            return;

        float duration =
            Mathf.Max(
                0f,
                config.woodEffectDuration
            );

        float tickInterval =
            Mathf.Max(
                0.05f,
                config.woodHealTickInterval
            );

        int tickCount =
            Mathf.Max(
                1,
                Mathf.FloorToInt(
                    duration /
                    tickInterval
                )
            );

        int totalHealAmount =
            Mathf.Max(
                0,
                config.woodHealAmountPerTick
            ) * tickCount;

        int shieldAmount =
            config.woodGrantShield
                ? Mathf.Max(
                    0,
                    config.woodShieldAmount
                )
                : 0;

        ApplyWoodBlessing(
            config,
            totalHealAmount,
            shieldAmount
        );
    }

    public void ApplyWoodBlessing(
        BrushSkillConfig config,
        int totalHealAmount,
        int shieldAmount
    )
    {
        if (config == null)
            return;

        totalHealAmount =
            Mathf.Max(
                0,
                totalHealAmount
            );

        shieldAmount =
            Mathf.Max(
                0,
                shieldAmount
            );

        if (healReceiver == null)
            ResolveHealReceiver();

        PlayHealVfxOnce(config);

        ApplyShield(
            config,
            shieldAmount
        );

        if (woodRoutine != null)
        {
            StopCoroutine(woodRoutine);
            woodRoutine = null;
        }

        woodRoutine =
            StartCoroutine(
                WoodHealRoutine(
                    config,
                    totalHealAmount
                )
            );

        if (debugLog)
        {
            Debug.Log(
                "[PlayerWoodBlessingController] " +
                $"Wood blessing applied. " +
                $"Duration={config.woodEffectDuration}, " +
                $"TotalHeal={totalHealAmount}, " +
                $"Shield={shieldAmount}",
                this
            );
        }
    }

    private IEnumerator WoodHealRoutine(
        BrushSkillConfig config,
        int totalHealAmount
    )
    {
        float duration =
            Mathf.Max(
                0f,
                config.woodEffectDuration
            );

        float tickInterval =
            Mathf.Max(
                0.05f,
                config.woodHealTickInterval
            );

        if (totalHealAmount <= 0)
        {
            woodRoutine = null;
            yield break;
        }

        if (duration <= 0f)
        {
            DoHealTick(totalHealAmount);

            woodRoutine = null;
            yield break;
        }

        int totalTicks =
            Mathf.Max(
                1,
                Mathf.FloorToInt(
                    duration /
                    tickInterval
                )
            );

        int baseHealPerTick =
            totalHealAmount /
            totalTicks;

        int extraHealTicks =
            totalHealAmount %
            totalTicks;

        float timer = 0f;
        float tickTimer = 0f;
        int tickIndex = 0;

        while (timer < duration &&
               tickIndex < totalTicks)
        {
            float deltaTime =
                Time.deltaTime;

            timer += deltaTime;
            tickTimer += deltaTime;

            if (tickTimer < tickInterval)
            {
                yield return null;
                continue;
            }

            tickTimer -= tickInterval;

            int healAmount =
                baseHealPerTick;

            if (tickIndex <
                extraHealTicks)
            {
                healAmount++;
            }

            DoHealTick(healAmount);

            tickIndex++;

            yield return null;
        }

        /*
         * Very low frame rate may skip the final ticks.
         * Apply the remaining planned heal here.
         */
        while (tickIndex < totalTicks)
        {
            int healAmount =
                baseHealPerTick;

            if (tickIndex <
                extraHealTicks)
            {
                healAmount++;
            }

            DoHealTick(healAmount);

            tickIndex++;
        }

        woodRoutine = null;
    }

    private void UpdateShieldVfxFollowPositionOnly()
    {
        if (activeShieldVfx == null)
            return;

        if (!shieldVfxFollowPlayerPosition)
            return;

        Transform followRoot = vfxRoot != null
            ? vfxRoot
            : playerRoot;

        if (followRoot == null)
            return;

        activeShieldVfx.transform.position =
            followRoot.position + shieldVfxWorldOffset;

        activeShieldVfx.transform.rotation = shieldVfxFixedWorldRotation;
    }

    private void DoHealTick(int healAmount)
    {
        if (healAmount <= 0)
            return;

        int actualHeal = healAmount;

        if (healReceiver != null)
        {
            actualHeal = healReceiver.Heal(healAmount);
        }
        else
        {
            Debug.LogWarning(
                "[PlayerWoodBlessingController] No IHealable found on Player. Heal number will show, but HP will not change.",
                this
            );
        }

        if (actualHeal <= 0)
            return;

        if (DamageNumberSpawner.Instance != null)
        {
            DamageNumberSpawner.Instance.ShowHealNumber(
                actualHeal,
                healNumberAnchor.position
            );
        }
    }

    private void ApplyShield(
        BrushSkillConfig config,
        int shieldAmount
    )
    {
        if (!config.woodGrantShield)
            return;

        if (shieldAmount <= 0)
            return;

        if (shieldController == null)
        {
            shieldController =
                GetComponent<
                    PlayerShieldController>();
        }

        if (shieldController == null)
        {
            shieldController =
                gameObject.AddComponent<
                    PlayerShieldController>();
        }

        BindShieldEvents();

        shieldController.ApplyShield(
            shieldAmount,
            config.woodShieldDuration,
            config.woodRefreshShieldWhenReapply
        );

        PlayShieldVfx(
            config,
            shieldAmount
        );
    }

    private void PlayHealVfxOnce(BrushSkillConfig config)
    {
        StopHealVfxRecycleRoutine();
        RecycleHealVfx();

        GameObject vfxObject = SpawnVfxObject(
            config.woodHealVfxUsePool,
            config.woodHealVfxPoolName,
            config.woodHealVfxPrefab,
            "Heal VFX"
        );

        if (vfxObject == null)
            return;

        SetupVfxTransform(
            vfxObject,
            config.woodHealVfxParentToPlayer,
            config.woodHealVfxLocalOffset
        );

        RestartParticleSystems(
            vfxObject,
            forceLoop: false
        );

        activeHealVfx = vfxObject;
        activeHealVfxFromPool = config.woodHealVfxUsePool;

        float recycleDelay = Mathf.Max(
            0.05f,
            config.woodHealVfxRecycleDelay
        );

        healVfxRecycleRoutine = StartCoroutine(
            RecycleHealVfxAfterDelay(recycleDelay)
        );
    }

    private IEnumerator RecycleHealVfxAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        healVfxRecycleRoutine = null;
        RecycleHealVfx();
    }

    private void PlayShieldVfx(
        BrushSkillConfig config,
        int shieldAmount
    )
    {
        if (!config.woodGrantShield)
            return;

        if (shieldAmount <= 0)
            return;

        RecycleShieldVfx();

        GameObject vfxObject = SpawnVfxObject(
            config.woodShieldVfxUsePool,
            config.woodShieldVfxPoolName,
            config.woodShieldVfxPrefab,
            "Shield VFX"
        );

        if (vfxObject == null)
            return;

        SetupShieldVfxTransform(
            vfxObject,
            config.woodShieldVfxParentToPlayer,
            config.woodShieldVfxLocalOffset
        );

        RestartParticleSystems(
            vfxObject,
            forceLoop: config.woodShieldVfxForceLoop
        );

        activeShieldVfx = vfxObject;
        activeShieldVfxFromPool = config.woodShieldVfxUsePool;
    }

    private GameObject SpawnVfxObject(
        bool usePool,
        string poolName,
        GameObject prefab,
        string label
    )
    {
        GameObject vfxObject = null;

        if (usePool)
        {
            if (string.IsNullOrEmpty(poolName))
            {
                Debug.LogWarning(
                    $"[PlayerWoodBlessingController] {label} pool name is empty.",
                    this
                );

                return null;
            }

            vfxObject = PoolMgr.Instance.GetObj(poolName);
        }
        else
        {
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"[PlayerWoodBlessingController] {label} prefab is missing.",
                    this
                );

                return null;
            }

            vfxObject = Instantiate(prefab);
        }

        if (vfxObject == null)
            return null;

        if (!vfxObject.activeSelf)
            vfxObject.SetActive(true);

        return vfxObject;
    }

    private void SetupVfxTransform(
        GameObject vfxObject,
        bool parentToPlayer,
        Vector3 localOffset
    )
    {
        if (vfxObject == null)
            return;

        Transform parent = parentToPlayer
            ? vfxRoot
            : null;

        if (parent != null)
        {
            vfxObject.transform.SetParent(parent, false);
            vfxObject.transform.localPosition = localOffset;
            vfxObject.transform.localRotation = Quaternion.identity;
        }
        else
        {
            vfxObject.transform.SetParent(null);
            vfxObject.transform.position =
                playerRoot.position + localOffset;
            vfxObject.transform.rotation = Quaternion.identity;
        }

        vfxObject.transform.localScale = Vector3.one;
    }

    private void SetupShieldVfxTransform(
    GameObject vfxObject,
    bool followPlayerPosition,
    Vector3 worldOffset
)
    {
        if (vfxObject == null)
            return;

        Transform followRoot = vfxRoot != null
            ? vfxRoot
            : playerRoot;

        vfxObject.transform.SetParent(null, true);

        shieldVfxFollowPlayerPosition = followPlayerPosition;
        shieldVfxWorldOffset = worldOffset;
        shieldVfxFixedWorldRotation = Quaternion.identity;

        if (followRoot != null)
        {
            vfxObject.transform.position =
                followRoot.position + shieldVfxWorldOffset;
        }
        else
        {
            vfxObject.transform.position =
                transform.position + shieldVfxWorldOffset;
        }

        vfxObject.transform.rotation = shieldVfxFixedWorldRotation;
        vfxObject.transform.localScale = Vector3.one;
    }

    private void RestartParticleSystems(
        GameObject vfxObject,
        bool forceLoop
    )
    {
        if (vfxObject == null)
            return;

        ParticleSystem[] particleSystems =
            vfxObject.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];

            if (ps == null)
                continue;

            ParticleSystem.MainModule main = ps.main;
            main.loop = forceLoop;

            ps.Clear(true);
            ps.Play(true);
        }
    }

    private void RecycleHealVfx()
    {
        GameObject vfxObject = activeHealVfx;
        bool fromPool = activeHealVfxFromPool;

        activeHealVfx = null;
        activeHealVfxFromPool = false;

        if (!IsAlive(vfxObject))
            return;

        PushOrDestroyVfx(
            vfxObject,
            fromPool
        );
    }

    private void RecycleShieldVfx()
    {
        GameObject vfxObject = activeShieldVfx;
        bool fromPool = activeShieldVfxFromPool;

        activeShieldVfx = null;
        activeShieldVfxFromPool = false;

        shieldVfxFollowPlayerPosition = false;
        shieldVfxWorldOffset = Vector3.zero;
        shieldVfxFixedWorldRotation = Quaternion.identity;

        if (!IsAlive(vfxObject))
            return;

        PushOrDestroyVfx(
            vfxObject,
            fromPool
        );
    }

    private void OnShieldCleared()
    {
        RecycleShieldVfx();
    }

    private void BindShieldEvents()
    {
        if (shieldController == null)
            return;

        if (boundShieldController == shieldController)
            return;

        UnbindShieldEvents();

        boundShieldController = shieldController;
        boundShieldController.ShieldCleared += OnShieldCleared;
    }

    private void UnbindShieldEvents()
    {
        if (boundShieldController != null)
        {
            boundShieldController.ShieldCleared -= OnShieldCleared;
            boundShieldController = null;
        }
    }

    private void ResolveHealReceiver()
    {
        healReceiver = null;

        MonoBehaviour[] parentBehaviours =
            GetComponentsInParent<MonoBehaviour>(true);

        for (int i = 0; i < parentBehaviours.Length; i++)
        {
            if (parentBehaviours[i] is IHealable receiver)
            {
                healReceiver = receiver;
                return;
            }
        }

        MonoBehaviour[] childBehaviours =
            GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < childBehaviours.Length; i++)
        {
            if (childBehaviours[i] is IHealable receiver)
            {
                healReceiver = receiver;
                return;
            }
        }
    }

    private bool IsAlive(GameObject obj)
    {
        return !ReferenceEquals(obj, null) && obj != null;
    }

    private void StopHealVfxRecycleRoutine()
    {
        if (healVfxRecycleRoutine != null)
        {
            StopCoroutine(healVfxRecycleRoutine);
            healVfxRecycleRoutine = null;
        }
    }

    private void PushOrDestroyVfx(
        GameObject vfxObject,
        bool fromPool
    )
    {
        if (!IsAlive(vfxObject))
            return;

        vfxObject.transform.SetParent(null, true);

        if (fromPool)
        {
            PoolMgr.Instance.PushObj(vfxObject);
        }
        else
        {
            Destroy(vfxObject);
        }
    }
}