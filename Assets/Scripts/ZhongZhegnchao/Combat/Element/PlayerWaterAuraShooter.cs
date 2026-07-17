using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hovl;

public class PlayerWaterAuraShooter : MonoBehaviour
{
    [Header("Scene References")]
    public GameObject waterAuraObject;
    public Transform spawnPoint;

    [Header("Debug")]
    public bool debugLog = true;

    private Coroutine auraRoutine;
    private BrushSkillConfig activeConfig;

    private float activeDamageMultiplier = 1f;
    private float activeProjectileSpeedMultiplier = 1f;

    private readonly HashSet<GameObject> pooledProjectiles = new HashSet<GameObject>();

    private class TargetRecord
    {
        public Transform target;
        public float distance;
    }

    private void Awake()
    {
        if (waterAuraObject != null)
        {
            waterAuraObject.SetActive(false);
        }

        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }
    }

    public void ActivateWaterAura(
        BrushSkillConfig config
    )
    {
        ActivateWaterAura(
            config,
            1f,
            1f
        );
    }

    public void ActivateWaterAura(
        BrushSkillConfig config,
        float damageMultiplier,
        float projectileSpeedMultiplier
    )
    {
        if (config == null)
        {
            Debug.LogWarning(
                "[PlayerWaterAuraShooter] " +
                "Water config is null.",
                this
            );

            return;
        }

        activeConfig = config;

        activeDamageMultiplier =
            Mathf.Max(
                0f,
                damageMultiplier
            );

        activeProjectileSpeedMultiplier =
            Mathf.Max(
                0.01f,
                projectileSpeedMultiplier
            );

        if (auraRoutine != null)
        {
            StopCoroutine(auraRoutine);
            auraRoutine = null;
        }

        auraRoutine = StartCoroutine(
            WaterAuraRoutine(
                activeConfig.waterAuraDuration
            )
        );

        if (debugLog)
        {
            Debug.Log(
                "[PlayerWaterAuraShooter] " +
                $"Water aura activated. " +
                $"Duration=" +
                $"{activeConfig.waterAuraDuration}, " +
                $"DamageMultiplier=" +
                $"{activeDamageMultiplier:F2}, " +
                $"SpeedMultiplier=" +
                $"{activeProjectileSpeedMultiplier:F2}",
                this
            );
        }
    }

    public void StopWaterAura()
    {
        if (auraRoutine != null)
        {
            StopCoroutine(auraRoutine);
            auraRoutine = null;
        }

        if (waterAuraObject != null)
        {
            waterAuraObject.SetActive(false);
        }

        activeConfig = null;
        activeDamageMultiplier = 1f;
        activeProjectileSpeedMultiplier = 1f;

        if (debugLog)
        {
            Debug.Log("[PlayerWaterAuraShooter] Water aura stopped.", this);
        }
    }

    private IEnumerator WaterAuraRoutine(float duration)
    {
        if (waterAuraObject != null)
        {
            waterAuraObject.SetActive(true);
        }

        float timer = 0f;
        float shootTimer = 0f;

        while (timer < duration)
        {
            if (activeConfig == null)
                break;

            timer += Time.deltaTime;
            shootTimer += Time.deltaTime;

            if (shootTimer >= activeConfig.waterShootInterval)
            {
                shootTimer = 0f;
                ShootWaterBarrage();
            }

            yield return null;
        }

        if (waterAuraObject != null)
        {
            waterAuraObject.SetActive(false);
        }

        auraRoutine = null;
        activeConfig = null;
        activeDamageMultiplier = 1f;
        activeProjectileSpeedMultiplier = 1f;

        if (debugLog)
        {
            Debug.Log("[PlayerWaterAuraShooter] Water aura ended.", this);
        }
    }

    private void ShootWaterBarrage()
    {
        if (activeConfig == null)
            return;

        int projectileCount = 1;

        if (activeConfig.waterUseBarrage)
        {
            projectileCount = Mathf.Max(
                1,
                activeConfig.waterProjectileCountPerBurst
            );
        }

        int targetSearchCount = activeConfig.waterDistributeTargets
            ? projectileCount
            : 1;

        List<Transform> targets =
    FindWaterTargets(targetSearchCount);

        if (targets.Count == 0)
            return;

        StartCoroutine(
            ShootWaterBarrageRoutine(
                targets,
                projectileCount
            )
        );
    }

    private IEnumerator ShootWaterBarrageRoutine(
    List<Transform> targets,
    int projectileCount
)
    {
        if (targets == null || targets.Count == 0)
            yield break;

        for (int i = 0; i < projectileCount; i++)
        {
            if (activeConfig == null)
                yield break;

            Transform target = activeConfig.waterDistributeTargets
                ? targets[i % targets.Count]
                : targets[Random.Range(0, targets.Count)];

            if (target != null)
            {
                Vector3 spawnPosition = GetRandomBarrageSpawnPosition(
                    i,
                    projectileCount,
                    target
                );

                ShootWaterProjectile(
                    target,
                    spawnPosition,
                    i,
                    projectileCount
                );
            }

            if (activeConfig.waterUseRandomBarrage)
            {
                float delay = Random.Range(
                    activeConfig.waterBurstDelayMin,
                    activeConfig.waterBurstDelayMax
                );

                if (delay > 0f)
                {
                    yield return new WaitForSeconds(delay);
                }
            }
            else
            {
                yield return null;
            }
        }

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerWaterAuraShooter] Random water barrage fired. Count={projectileCount}",
                this
            );
        }
    }

    private List<Transform> FindWaterTargets(
    int maxCount
)
    {
        List<Transform> result =
            new List<Transform>();

        List<TargetRecord> records =
            new List<TargetRecord>();

        if (activeConfig == null)
            return result;

        int searchLayer =
            activeConfig.targetLayer.value;

        if (activeConfig.enableSceneElementInteraction)
        {
            searchLayer |=
                activeConfig.sceneElementLayer.value;
        }

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            activeConfig.waterSearchRange,
            searchLayer,
            QueryTriggerInteraction.Collide
        );

        HashSet<Transform> addedTargets =
            new HashSet<Transform>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];

            if (col == null)
                continue;

            Transform targetTransform = null;

            IBrushSceneElementReactable sceneReactable =
                FindSceneElementReactable(col);

            bool isWaterSceneTarget =
                sceneReactable != null &&
                sceneReactable.CanReactTo(
                    BrushSkillType.Water
                );

            if (isWaterSceneTarget)
            {
                targetTransform =
                    sceneReactable.ReactionTarget;
            }
            else
            {
                EnemyWhitebox enemy =
                    col.GetComponent<EnemyWhitebox>();

                if (enemy == null)
                {
                    enemy = col
                        .GetComponentInParent<
                            EnemyWhitebox>();
                }

                if (enemy == null)
                {
                    enemy = col
                        .GetComponentInChildren<
                            EnemyWhitebox>();
                }

                if (enemy != null && enemy.IsDead)
                    continue;

                IDamageable damageable =
                    col.GetComponent<IDamageable>();

                if (damageable == null)
                {
                    damageable = col
                        .GetComponentInParent<
                            IDamageable>();
                }

                if (damageable == null)
                {
                    damageable = col
                        .GetComponentInChildren<
                            IDamageable>();
                }

                if (damageable == null)
                    continue;

                targetTransform = col.transform;

                if (enemy != null)
                {
                    targetTransform = enemy.transform;
                }
            }

            if (targetTransform == null)
                continue;

            if (addedTargets.Contains(targetTransform))
                continue;

            addedTargets.Add(targetTransform);

            float distance = Vector3.Distance(
                transform.position,
                targetTransform.position
            );

            records.Add(
                new TargetRecord
                {
                    target = targetTransform,
                    distance = distance
                }
            );
        }

        records.Sort(
            (a, b) =>
                a.distance.CompareTo(b.distance)
        );

        int count = Mathf.Min(
            maxCount,
            records.Count
        );

        for (int i = 0; i < count; i++)
        {
            result.Add(records[i].target);
        }

        return result;
    }

    private IBrushSceneElementReactable
    FindSceneElementReactable(
        Component component
    )
    {
        if (component == null)
            return null;

        IBrushSceneElementReactable reactable =
            component.GetComponent<
                IBrushSceneElementReactable>();

        if (reactable == null)
        {
            reactable = component
                .GetComponentInParent<
                    IBrushSceneElementReactable>();
        }

        if (reactable == null)
        {
            reactable = component
                .GetComponentInChildren<
                    IBrushSceneElementReactable>();
        }

        return reactable;
    }

    private Vector3 GetRandomBarrageSpawnPosition(
    int index,
    int totalCount,
    Transform target
)
    {
        Vector3 basePosition = spawnPoint.position;

        if (activeConfig == null)
            return basePosition;

        if (!activeConfig.waterUseRandomBarrage)
        {
            return GetBarrageSpawnPosition(
                index,
                totalCount,
                target
            );
        }

        Vector3 randomCircle = Random.insideUnitSphere;
        randomCircle.y = Mathf.Abs(randomCircle.y) * 0.5f;

        Vector3 randomOffset =
            randomCircle.normalized *
            Random.Range(0f, activeConfig.waterRandomSpawnRadius);

        return basePosition + randomOffset;
    }

    private void ShootWaterProjectile(
        Transform target,
        Vector3 spawnPosition,
        int index,
        int totalCount
    )
    {
        if (activeConfig == null)
            return;

        if (target == null)
            return;

        GameObject projectileObj = GetWaterProjectile();

        if (projectileObj == null)
            return;

        projectileObj.transform.position = spawnPosition;

        Vector3 targetPoint = target.position + activeConfig.waterTargetOffset;
        Vector3 direction = targetPoint - spawnPosition;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = transform.forward;
        }

        projectileObj.transform.rotation = Quaternion.LookRotation(direction.normalized);

        HS_ProjectileMover mover = projectileObj.GetComponent<HS_ProjectileMover>();

        if (mover != null)
        {
            IBrushSceneElementReactable sceneReactable =
                FindSceneElementReactable(target);

            bool isSceneElementTarget =
                activeConfig.enableSceneElementInteraction &&
                sceneReactable != null &&
                sceneReactable.CanReactTo(
                    BrushSkillType.Water
                );

            mover.OnRecycleRequested =
                RecycleWaterProjectile;

            string explosionSoundName =
                activeConfig.waterExplosionSoundName;

            bool explosionSoundSync =
                activeConfig.waterExplosionSoundSync;

            mover.OnProjectileHit =
                (hitObject, hitPoint) =>
                {
                    PlayWaterExplosionSound(
                        explosionSoundName,
                        explosionSoundSync
                    );
                };

            mover.ApplyWaterConfig(activeConfig);

            mover.Init(
                gameObject,
                target,
                GetResolvedProjectileDamage(),
                activeConfig.waterProjectileKnockback,
                activeConfig.wetDuration
            );

            float sideOffset = 0f;
            float heightOffset = 0f;

            float speedOffset =
                GetSkillTreeSpeedOffset();

            Vector3 extraTargetOffset =
                isSceneElementTarget
                    ? -activeConfig.waterTargetOffset
                    : Vector3.zero;

            if (activeConfig.waterUseRandomBarrage)
            {
                sideOffset = Random.Range(
                    activeConfig.waterArcSideOffsetMin,
                    activeConfig.waterArcSideOffsetMax
                );

                heightOffset = Random.Range(
                    activeConfig.waterArcHeightRandomMin,
                    activeConfig.waterArcHeightRandomMax
                );

                speedOffset += Random.Range(
                    activeConfig.waterSpeedRandomMin,
                    activeConfig.waterSpeedRandomMax
                );

                if (!isSceneElementTarget)
                {
                    extraTargetOffset =
                        Random.insideUnitSphere *
                        activeConfig
                            .waterRandomTargetOffsetRadius;

                    extraTargetOffset.y =
                        Mathf.Abs(extraTargetOffset.y) *
                        0.6f;
                }
            }

            mover.SetRuntimeArcVariation(
                sideOffset,
                heightOffset,
                speedOffset,
                extraTargetOffset
            );

            mover.RefreshArcData();

            WaterProjectileSceneInteractionRelay relay =
                projectileObj.GetComponent<
                    WaterProjectileSceneInteractionRelay>();

            if (isSceneElementTarget)
            {
                if (relay == null)
                {
                    relay = projectileObj.AddComponent<
                        WaterProjectileSceneInteractionRelay>();
                }

                relay.Init(
                    gameObject,
                    target,
                    activeConfig.waterReachDistance
                );
            }
            else if (relay != null)
            {
                relay.Clear();
            }
        }
        else
        {
            Debug.LogWarning(
                "[PlayerWaterAuraShooter] " +
                "HS_ProjectileMover not found on water projectile.",
                projectileObj
            );
        }
    }

    private void PlayWaterExplosionSound(
        string soundName,
        bool isSync
    )
    {
        if (string.IsNullOrEmpty(soundName))
            return;

        MusicMgr.Instance.PlaySound(
            soundName,
            false,
            isSync
        );
    }

    private int GetResolvedProjectileDamage()
    {
        if (activeConfig == null)
            return 0;

        return Mathf.Max(
            0,
            Mathf.RoundToInt(
                activeConfig.waterProjectileDamage *
                activeDamageMultiplier
            )
        );
    }

    private float GetSkillTreeSpeedOffset()
    {
        if (activeConfig == null)
            return 0f;

        float baseSpeed =
            Mathf.Max(
                0f,
                activeConfig.waterProjectileSpeed
            );

        float resolvedSpeed =
            baseSpeed *
            activeProjectileSpeedMultiplier;

        return resolvedSpeed -
               baseSpeed;
    }

    private GameObject GetWaterProjectile()
    {
        if (activeConfig == null)
            return null;

        if (activeConfig.waterUsePool)
        {
            if (string.IsNullOrEmpty(activeConfig.waterProjectilePoolName))
            {
                Debug.LogWarning(
                    "[PlayerWaterAuraShooter] Water Projectile Pool Name is empty.",
                    this
                );

                return null;
            }

            GameObject obj = PoolMgr.Instance.GetObj(
                activeConfig.waterProjectilePoolName
            );

            if (obj == null)
            {
                Debug.LogWarning(
                    $"[PlayerWaterAuraShooter] PoolMgr failed to get object: {activeConfig.waterProjectilePoolName}",
                    this
                );

                return null;
            }

            pooledProjectiles.Add(obj);
            return obj;
        }

        if (activeConfig.waterProjectilePrefab == null)
        {
            Debug.LogWarning(
                "[PlayerWaterAuraShooter] Water projectile prefab is missing.",
                this
            );

            return null;
        }

        GameObject newObj = Instantiate(activeConfig.waterProjectilePrefab);
        newObj.SetActive(true);

        pooledProjectiles.Remove(newObj);
        return newObj;
    }

    private void RecycleWaterProjectile(GameObject obj)
    {
        if (obj == null)
            return;

        if (pooledProjectiles.Contains(obj))
        {
            PoolMgr.Instance.PushObj(obj);
        }
        else
        {
            obj.SetActive(false);
        }
    }

    private Vector3 GetBarrageSpawnPosition(
    int index,
    int totalCount,
    Transform target
)
    {
        Vector3 basePosition = spawnPoint.position;

        if (activeConfig == null)
            return basePosition;

        if (!activeConfig.waterUseBarrage || totalCount <= 1)
            return basePosition;

        Vector3 toTarget = target.position - basePosition;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            toTarget = transform.forward;
        }

        toTarget.Normalize();

        float normalized = totalCount <= 1
            ? 0f
            : index / (float)(totalCount - 1) - 0.5f;

        float angle = normalized * activeConfig.waterBarrageAngle;

        Vector3 spreadDirection =
            Quaternion.AngleAxis(angle, Vector3.up) * toTarget;

        Vector3 right = Vector3.Cross(Vector3.up, spreadDirection).normalized;

        float sideOffset =
            normalized *
            activeConfig.waterBarrageSpawnRadius *
            2f;

        int verticalIndex = index % 3 - 1;

        float verticalOffset =
            verticalIndex *
            activeConfig.waterBarrageVerticalStep;

        return basePosition +
               right * sideOffset +
               Vector3.up * verticalOffset;
    }
}