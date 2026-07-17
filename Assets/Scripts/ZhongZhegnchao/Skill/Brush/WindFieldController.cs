using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WindFieldController : MonoBehaviour
{
    private BrushSkillConfig config;
    private GameObject caster;

    private float pullRadiusMultiplier = 1f;
    private float pullForceMultiplier = 1f;
    private float infusionDamageMultiplier = 1f;

    private float remainingDuration;

    private bool isRunning;
    private bool isRecycling;

    private AudioSource windLoopAudioSource;
    private int windAudioRequestVersion;

    private ElementType absorbedElement =
        ElementType.None;

    private ParticleSystem[] particleSystems;

    private ParticleSystem.MinMaxGradient[]
        originalStartColors;

    private readonly HashSet<EnemyWhitebox>
        controlledEnemies =
            new HashSet<EnemyWhitebox>();

    private readonly HashSet<EnemyWhitebox>
        currentFrameEnemies =
            new HashSet<EnemyWhitebox>();

    private readonly Dictionary<
        EnemyWhitebox,
        float
    > nextCenterTickTimes =
        new Dictionary<EnemyWhitebox, float>();

    private readonly List<EnemyWhitebox>
        releaseBuffer =
            new List<EnemyWhitebox>();

    private void Awake()
    {
        CacheParticleSystems();
    }

    private void OnEnable()
    {
        RestoreParticleColors();
        RestartParticleSystems();
    }

    public void Initialize(
        BrushSkillConfig newConfig,
        GameObject newCaster
    )
    {
        Initialize(
            newConfig,
            newCaster,
            1f,
            1f,
            1f
        );
    }

    public void Initialize(
        BrushSkillConfig newConfig,
        GameObject newCaster,
        float newPullRadiusMultiplier,
        float newPullForceMultiplier,
        float newInfusionDamageMultiplier
    )
    {
        ReleaseAllControlledEnemies();

        config = newConfig;
        caster = newCaster;

        pullRadiusMultiplier =
            Mathf.Max(
                0.01f,
                newPullRadiusMultiplier
            );

        pullForceMultiplier =
            Mathf.Max(
                0.01f,
                newPullForceMultiplier
            );

        infusionDamageMultiplier =
            Mathf.Max(
                0f,
                newInfusionDamageMultiplier
            );

        absorbedElement =
            ElementType.None;

        nextCenterTickTimes.Clear();

        remainingDuration =
            config != null
                ? Mathf.Max(
                    0.01f,
                    config.windFieldDuration
                )
                : 0f;

        isRunning = config != null;
        isRecycling = false;

        RestoreParticleColors();
        RestartParticleSystems();
        StartWindLoopSound();

        if (config == null)
        {
            Debug.LogWarning(
                "[WindFieldController] " +
                "Config is null.",
                this
            );

            RecycleSelf();
        }
    }

    private void Update()
    {
        if (!isRunning)
            return;

        if (config == null)
        {
            RecycleSelf();
            return;
        }

        remainingDuration -= Time.deltaTime;

        if (remainingDuration <= 0f)
        {
            RecycleSelf();
            return;
        }

        UpdateWindPull();
    }

    private void UpdateWindPull()
    {
        currentFrameEnemies.Clear();

        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                GetResolvedPullRadius(),
                config.targetLayer,
                QueryTriggerInteraction.Collide
            );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];

            if (col == null)
                continue;

            EnemyWhitebox enemy =
                FindEnemy(col);

            if (enemy == null)
                continue;

            if (enemy.IsDead)
                continue;

            if (!currentFrameEnemies.Add(enemy))
                continue;

            if (controlledEnemies.Add(enemy))
            {
                enemy.SetWindPullActive(true);
            }

            PullEnemy(enemy);
        }

        ReleaseEnemiesOutsideRange();
    }

    private float GetResolvedPullRadius()
    {
        if (config == null)
            return 0f;

        return Mathf.Max(
            0.01f,
            config.windPullRadius *
            pullRadiusMultiplier
        );
    }

    private float GetResolvedPullSpeed()
    {
        if (config == null)
            return 0f;

        return Mathf.Max(
            0f,
            config.windPullSpeed *
            pullForceMultiplier
        );
    }

    private float GetResolvedInfusionDamageMultiplier()
    {
        if (absorbedElement ==
            ElementType.None)
        {
            return 1f;
        }

        return infusionDamageMultiplier;
    }

    private EnemyWhitebox FindEnemy(
        Component component
    )
    {
        if (component == null)
            return null;

        EnemyWhitebox enemy =
            component.GetComponent<EnemyWhitebox>();

        if (enemy == null)
        {
            enemy = component
                .GetComponentInParent<
                    EnemyWhitebox>();
        }

        if (enemy == null)
        {
            enemy = component
                .GetComponentInChildren<
                    EnemyWhitebox>();
        }

        return enemy;
    }

    private void PullEnemy(
    EnemyWhitebox enemy
)
    {
        if (enemy == null)
            return;

        Vector3 toCenter =
            transform.position -
            enemy.transform.position;

        toCenter.y = 0f;

        float distance =
            toCenter.magnitude;

        if (distance <= config.windCenterRadius)
        {
            ProcessCenterEnemy(enemy);
            return;
        }
        nextCenterTickTimes.Remove(enemy);

        if (enemy.IsBeingKnockedBack)
        {
            return;
        }

        if (toCenter.sqrMagnitude <= 0.0001f)
            return;

        Vector3 direction =
            toCenter.normalized;

        float moveDistance =
            GetResolvedPullSpeed() *
            Time.deltaTime;

        moveDistance = Mathf.Min(
            moveDistance,
            distance
        );

        Vector3 move =
            direction * moveDistance;

        enemy.ApplyWindPullMove(move);

        Vector3 afterMoveToCenter =
            transform.position -
            enemy.transform.position;

        afterMoveToCenter.y = 0f;

        if (afterMoveToCenter.magnitude <=
            config.windCenterRadius)
        {
            ProcessCenterEnemy(enemy);
        }
    }

    private void ProcessCenterEnemy(
    EnemyWhitebox enemy
)
    {
        if (enemy == null)
            return;

        if (enemy.IsDead)
            return;

        ElementStatusController status =
            FindStatusController(enemy);

        TryAbsorbElement(status);

        float currentTime =
            Time.time;

        if (nextCenterTickTimes.TryGetValue(
                enemy,
                out float nextTickTime
            ))
        {
            if (currentTime < nextTickTime)
            {
                return;
            }
        }

        float tickInterval =
            Mathf.Max(
                0.05f,
                config.windCenterTickInterval
            );

        nextCenterTickTimes[enemy] =
            currentTime + tickInterval;

        bool canApplySpreadStatus = true;

        if (status != null &&
            absorbedElement != ElementType.None)
        {
            bool hasVaporizePair =
                status.HasVaporizePair(
                    absorbedElement
                );

            if (hasVaporizePair)
            {
                canApplySpreadStatus =
                    status.CanTriggerVaporizeNow(
                        absorbedElement
                    );
            }
        }

        ApplyCenterTickDamage(enemy);

        if (status != null &&
            absorbedElement != ElementType.None &&
            canApplySpreadStatus)
        {
            ApplySpreadStatus(status);
        }
    }

    private void TryAbsorbElement(
    ElementStatusController status
)
    {
        if (absorbedElement != ElementType.None)
            return;

        if (status == null)
            return;

        ElementType element =
            GetAbsorbableElement(status);

        if (element == ElementType.None)
            return;

        AbsorbElement(element);
    }

    private ElementType GetCurrentDamageElement()
    {
        if (absorbedElement == ElementType.Fire)
        {
            return ElementType.Fire;
        }

        if (absorbedElement == ElementType.Water)
        {
            return ElementType.Water;
        }

        return ElementType.Wind;
    }

    private void ApplyCenterTickDamage(
    EnemyWhitebox enemy
)
    {
        if (enemy == null)
            return;

        GameObject attackerObject =
            caster != null
                ? caster
                : gameObject;

        Vector3 hitDirection =
            enemy.transform.position -
            transform.position;

        hitDirection.y = 0f;

        if (hitDirection.sqrMagnitude > 0.0001f)
        {
            hitDirection.Normalize();
        }

        float resolvedDamageMultiplier =
            GetResolvedInfusionDamageMultiplier();

        int resolvedBaseDamage =
            Mathf.Max(
                0,
                Mathf.RoundToInt(
                    config.baseDamage *
                    resolvedDamageMultiplier
                )
            );

        float resolvedSkillMultiplier =
            Mathf.Max(
                0f,
                config.skillMultiplier *
                resolvedDamageMultiplier
            );

        DamageInfo damageInfo =
            new DamageInfo
            {
                attacker = attackerObject,
                target = enemy.gameObject,

                damage = resolvedBaseDamage,
                knockback = 0f,

                hitPoint =
                    enemy.transform.position,

                hitDirection =
                    hitDirection,

                sourceAction = null,

                element =
                    GetCurrentDamageElement(),

                canApplyElementStatus = false,

                skillMultiplier =
                    resolvedSkillMultiplier,

                damageBonus =
                    config.damageBonus,

                reactionMultiplier =
                    config.reactionMultiplier,

                reactionType =
                    ElementReactionType.None,

                canCrit =
                    config.canCrit
            };

        enemy.TakeDamage(damageInfo);
    }

    private ElementStatusController
        FindStatusController(
            EnemyWhitebox enemy
        )
    {
        if (enemy == null)
            return null;

        ElementStatusController status =
            enemy.GetComponent<
                ElementStatusController>();

        if (status == null)
        {
            status = enemy
                .GetComponentInParent<
                    ElementStatusController>();
        }

        if (status == null)
        {
            status = enemy
                .GetComponentInChildren<
                    ElementStatusController>();
        }

        return status;
    }

    private ElementType GetAbsorbableElement(
        ElementStatusController status
    )
    {
        if (status == null)
            return ElementType.None;

        if (status.HasFireStatus)
        {
            return ElementType.Fire;
        }

        if (status.IsWet)
        {
            return ElementType.Water;
        }

        return ElementType.None;
    }

    private void AbsorbElement(
        ElementType element
    )
    {
        if (element != ElementType.Fire &&
            element != ElementType.Water)
        {
            return;
        }

        absorbedElement = element;

        Color targetColor =
            absorbedElement == ElementType.Fire
                ? config.windFireColor
                : config.windWaterColor;

        ApplyParticleColor(targetColor);

        Debug.Log(
            $"[WindFieldController] " +
            $"Absorbed element: {absorbedElement}",
            this
        );
    }

    private void ApplySpreadStatus(
        ElementStatusController status
    )
    {
        if (status == null)
            return;

        GameObject owner =
            caster != null
                ? caster
                : gameObject;

        if (absorbedElement == ElementType.Fire)
        {
            int resolvedSpreadTickDamage =
                Mathf.Max(
                    0,
                    Mathf.RoundToInt(
                        config.windSpreadFireTickDamage *
                        infusionDamageMultiplier
                    )
                );

            status.ApplyBurning(
                owner,
                config.windSpreadFireDuration,
                config.windSpreadFireTickInterval,
                resolvedSpreadTickDamage
            );

            return;
        }

        if (absorbedElement == ElementType.Water)
        {
            status.ApplyWet(
                owner,
                config.windSpreadWetDuration
            );
        }
    }

    private void ApplyParticleColor(
        Color color
    )
    {
        if (particleSystems == null)
            return;

        for (int i = 0;
             i < particleSystems.Length;
             i++)
        {
            ParticleSystem particleSystem =
                particleSystems[i];

            if (particleSystem == null)
                continue;

            ParticleSystem.MainModule main =
                particleSystem.main;

            main.startColor = color;
        }
    }

    private void CacheParticleSystems()
    {
        particleSystems =
            GetComponentsInChildren<
                ParticleSystem>(true);

        originalStartColors =
            new ParticleSystem.MinMaxGradient[
                particleSystems.Length
            ];

        for (int i = 0;
             i < particleSystems.Length;
             i++)
        {
            ParticleSystem particleSystem =
                particleSystems[i];

            if (particleSystem == null)
                continue;

            originalStartColors[i] =
                particleSystem.main.startColor;
        }
    }

    private void RestoreParticleColors()
    {
        if (particleSystems == null ||
            originalStartColors == null)
        {
            return;
        }

        int count = Mathf.Min(
            particleSystems.Length,
            originalStartColors.Length
        );

        for (int i = 0; i < count; i++)
        {
            ParticleSystem particleSystem =
                particleSystems[i];

            if (particleSystem == null)
                continue;

            ParticleSystem.MainModule main =
                particleSystem.main;

            main.startColor =
                originalStartColors[i];
        }
    }

    private void RestartParticleSystems()
    {
        if (particleSystems == null)
            return;

        for (int i = 0;
             i < particleSystems.Length;
             i++)
        {
            ParticleSystem particleSystem =
                particleSystems[i];

            if (particleSystem == null)
                continue;

            particleSystem.Clear(true);
            particleSystem.Play(true);
        }
    }

    private void ReleaseEnemiesOutsideRange()
    {
        releaseBuffer.Clear();

        foreach (
            EnemyWhitebox enemy
            in controlledEnemies
        )
        {
            if (enemy == null ||
                !currentFrameEnemies.Contains(enemy))
            {
                releaseBuffer.Add(enemy);
            }
        }

        for (int i = 0;
             i < releaseBuffer.Count;
             i++)
        {
            EnemyWhitebox enemy =
                releaseBuffer[i];

            if (enemy != null)
            {
                enemy.SetWindPullActive(false);
            }

            controlledEnemies.Remove(enemy);
            nextCenterTickTimes.Remove(enemy);
        }
    }

    private void ReleaseAllControlledEnemies()
    {
        foreach (
            EnemyWhitebox enemy
            in controlledEnemies
        )
        {
            if (enemy != null)
            {
                enemy.SetWindPullActive(false);
            }
        }

        controlledEnemies.Clear();
        currentFrameEnemies.Clear();
        releaseBuffer.Clear();
    }

    private void StartWindLoopSound()
    {
        StopWindLoopSound();

        if (config == null ||
            string.IsNullOrEmpty(config.windLoopSoundName))
        {
            return;
        }

        int requestVersion = ++windAudioRequestVersion;

        MusicMgr.Instance.PlaySound(
            config.windLoopSoundName,
            true,
            config.windLoopSoundSync,
            source =>
            {
                if (source == null)
                    return;

                bool requestIsStillValid =
                    requestVersion == windAudioRequestVersion &&
                    isRunning &&
                    gameObject.activeInHierarchy;

                if (!requestIsStillValid)
                {
                    MusicMgr.Instance.StopSound(source);
                    return;
                }

                windLoopAudioSource = source;
            }
        );
    }

    private void StopWindLoopSound()
    {
        windAudioRequestVersion++;

        if (windLoopAudioSource == null)
            return;

        MusicMgr.Instance.StopSound(windLoopAudioSource);
        windLoopAudioSource = null;
    }

    private void RecycleSelf()
    {
        if (isRecycling)
            return;

        isRecycling = true;
        isRunning = false;

        StopWindLoopSound();
        ReleaseAllControlledEnemies();

        PoolMgr.Instance.PushObj(gameObject);
    }

    private void OnDisable()
    {
        isRunning = false;

        StopWindLoopSound();
        ReleaseAllControlledEnemies();

        nextCenterTickTimes.Clear();

        config = null;
        caster = null;

        absorbedElement =
            ElementType.None;

        pullRadiusMultiplier = 1f;
        pullForceMultiplier = 1f;
        infusionDamageMultiplier = 1f;

        isRecycling = false;
    }
}