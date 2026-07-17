using UnityEngine;

public class WindBrushSkill : BrushSkillBase
{
    [Header("Config")]
    public BrushSkillConfig config;

    protected override BrushSkillType SkillType
    {
        get
        {
            return BrushSkillType.Wind;
        }
    }

    protected override int GetInkCost()
    {
        if (config == null)
            return 0;

        return config.inkCost;
    }

    protected override float GetCooldown()
    {
        if (config == null)
            return 0f;

        if (!config.useSkillCooldown)
            return 0f;

        return config.skillCooldown;
    }

    protected override void Execute(
        BrushGestureResult result,
        BrushCastContext context
    )
    {
        if (config == null)
        {
            Debug.LogWarning(
                "[WindBrushSkill] Config is missing.",
                this
            );

            return;
        }

        if (config.skillType != BrushSkillType.Wind)
        {
            Debug.LogWarning(
                "[WindBrushSkill] Wrong config skill type.",
                this
            );

            return;
        }

        if (string.IsNullOrEmpty(
                config.windFieldPoolName
            ))
        {
            Debug.LogWarning(
                "[WindBrushSkill] " +
                "Wind field pool name is empty.",
                this
            );

            return;
        }

        Camera cameraToUse =
            GetWorldCamera();

        if (cameraToUse == null)
        {
            Debug.LogWarning(
                "[WindBrushSkill] World camera not found.",
                this
            );

            return;
        }

        GameObject resource =
            Resources.Load<GameObject>(
                config.windFieldPoolName
            );

        if (resource == null)
        {
            Debug.LogWarning(
                $"[WindBrushSkill] " +
                $"Wind field prefab not found: " +
                $"{config.windFieldPoolName}",
                this
            );

            return;
        }

        GameObject windFieldObject =
            PoolMgr.Instance.GetObj(
                config.windFieldPoolName
            );

        if (windFieldObject == null)
            return;

        GameObject casterObject =
            context != null &&
            context.caster != null
                ? context.caster
                : caster != null
                    ? caster
                    : gameObject;

        Vector3 cameraForward =
            cameraToUse.transform.forward;

        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude <= 0.0001f)
        {
            cameraForward =
                casterObject.transform.forward;

            cameraForward.y = 0f;
        }

        cameraForward.Normalize();

        Vector3 spawnPosition =
            cameraToUse.transform.position +
            cameraForward *
            config.windSpawnDistance;

        spawnPosition.y =
            casterObject.transform.position.y +
            config.windSpawnYOffset;

        windFieldObject.transform.position =
            spawnPosition;

        windFieldObject.transform.rotation =
            Quaternion.LookRotation(
                cameraForward,
                Vector3.up
            );

        WindFieldController controller =
            windFieldObject.GetComponent<
                WindFieldController>();

        if (controller == null)
        {
            controller =
                windFieldObject
                    .GetComponentInChildren<
                        WindFieldController>(true);
        }

        if (controller == null)
        {
            Debug.LogWarning(
                "[WindBrushSkill] " +
                "WindFieldController not found.",
                windFieldObject
            );

            PoolMgr.Instance.PushObj(
                windFieldObject
            );

            return;
        }

        float pullRadiusMultiplier =
            Mathf.Max(
                0.01f,
                GetSkillTreeStatValue(
                    SkillTreeStatType
                        .PullRadiusMultiplier,
                    1f
                )
            );

        float pullForceMultiplier =
            Mathf.Max(
                0.01f,
                GetSkillTreeStatValue(
                    SkillTreeStatType
                        .PullForceMultiplier,
                    1f
                )
            );

        float infusionDamageMultiplier =
            Mathf.Max(
                0f,
                GetSkillTreeStatValue(
                    SkillTreeStatType
                        .InfusionDamageMultiplier,
                    1f
                )
            );

        controller.Initialize(
            config,
            casterObject,
            pullRadiusMultiplier,
            pullForceMultiplier,
            infusionDamageMultiplier
        );

        Debug.Log(
            $"[WindBrushSkill] " +
            $"Wind field spawned at " +
            $"{spawnPosition}.",
            this
        );
    }

    private Camera GetWorldCamera()
    {
        if (castContextBuilder != null &&
            castContextBuilder.worldCamera != null)
        {
            return castContextBuilder.worldCamera;
        }

        return Camera.main;
    }
}