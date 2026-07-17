using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BrushSkillConfig))]
public class BrushSkillConfigEditor : Editor
{
    private SerializedProperty skillType;

    private SerializedProperty targetLayer;
    private SerializedProperty rayDistance;
    private SerializedProperty fallbackDistance;

    private SerializedProperty enableSceneElementInteraction;
    private SerializedProperty sceneElementLayer;

    private SerializedProperty inkCost;

    private SerializedProperty useSkillCooldown;
    private SerializedProperty skillCooldown;

    private SerializedProperty baseDamage;
    private SerializedProperty knockback;
    private SerializedProperty element;
    private SerializedProperty canApplyElementStatus;
    private SerializedProperty skillMultiplier;
    private SerializedProperty damageBonus;
    private SerializedProperty reactionMultiplier;
    private SerializedProperty canCrit;

    private SerializedProperty slashSphereRadius;
    private SerializedProperty slashSampleCount;

    private SerializedProperty fireDamageRadius;
    private SerializedProperty applyBurning;
    private SerializedProperty burningDuration;
    private SerializedProperty burningTickInterval;
    private SerializedProperty burningTickDamage;
    private SerializedProperty applyInfusionToPlayer;
    private SerializedProperty infusionDuration;
    private SerializedProperty infusionDamageMultiplier;
    private SerializedProperty infusionApplyBurningOnHit;
    private SerializedProperty infusionBurningDuration;
    private SerializedProperty infusionBurningTickInterval;
    private SerializedProperty infusionBurningTickDamage;

    private SerializedProperty waterAuraDuration;
    private SerializedProperty waterUsePool;
    private SerializedProperty waterProjectilePoolName;
    private SerializedProperty waterProjectilePrefab;
    private SerializedProperty waterShootInterval;
    private SerializedProperty waterSearchRange;
    private SerializedProperty waterProjectileDamage;
    private SerializedProperty waterProjectileKnockback;
    private SerializedProperty wetDuration;
    private SerializedProperty waterUseArcHoming;
    private SerializedProperty waterProjectileSpeed;
    private SerializedProperty waterProjectileLifeTime;
    private SerializedProperty waterArcHeight;
    private SerializedProperty waterRotateSpeed;
    private SerializedProperty waterReachDistance;
    private SerializedProperty waterTargetOffset;

    private SerializedProperty waterExplosionSoundName;
    private SerializedProperty waterExplosionSoundSync;

    private SerializedProperty waterProjectileHitLayer;
    private SerializedProperty waterIgnoreAttackerCollision;
    private SerializedProperty waterDestroyOnNonHitLayerCollision;

    private SerializedProperty waterUseBarrage;
    private SerializedProperty waterProjectileCountPerBurst;
    private SerializedProperty waterBarrageAngle;
    private SerializedProperty waterBarrageSpawnRadius;
    private SerializedProperty waterBarrageVerticalStep;
    private SerializedProperty waterDistributeTargets;

    private SerializedProperty waterUseRandomBarrage;
    private SerializedProperty waterBurstDelayMin;
    private SerializedProperty waterBurstDelayMax;
    private SerializedProperty waterRandomSpawnRadius;
    private SerializedProperty waterRandomTargetOffsetRadius;
    private SerializedProperty waterArcSideOffsetMin;
    private SerializedProperty waterArcSideOffsetMax;
    private SerializedProperty waterArcHeightRandomMin;
    private SerializedProperty waterArcHeightRandomMax;
    private SerializedProperty waterSpeedRandomMin;
    private SerializedProperty waterSpeedRandomMax;

    private SerializedProperty woodEffectDuration;

    private SerializedProperty woodHealAmountPerTick;
    private SerializedProperty woodHealTickInterval;

    private SerializedProperty woodHealVfxUsePool;
    private SerializedProperty woodHealVfxPoolName;
    private SerializedProperty woodHealVfxPrefab;
    private SerializedProperty woodHealVfxLocalOffset;
    private SerializedProperty woodHealVfxParentToPlayer;
    private SerializedProperty woodHealVfxRecycleDelay;

    private SerializedProperty woodShieldVfxUsePool;
    private SerializedProperty woodShieldVfxPoolName;
    private SerializedProperty woodShieldVfxPrefab;
    private SerializedProperty woodShieldVfxLocalOffset;
    private SerializedProperty woodShieldVfxParentToPlayer;
    private SerializedProperty woodShieldVfxForceLoop;

    private SerializedProperty woodGrantShield;
    private SerializedProperty woodShieldAmount;
    private SerializedProperty woodShieldDuration;
    private SerializedProperty woodRefreshShieldWhenReapply;

    private SerializedProperty bridgeTriggerLayer;
    private SerializedProperty bridgeCastRadius;
    private SerializedProperty bridgeActiveDuration;

    private SerializedProperty windFieldPoolName;
    private SerializedProperty windSpawnDistance;
    private SerializedProperty windSpawnYOffset;
    private SerializedProperty windFieldDuration;

    private SerializedProperty windLoopSoundName;
    private SerializedProperty windLoopSoundSync;

    private SerializedProperty windCenterTickInterval;

    private SerializedProperty windPullRadius;
    private SerializedProperty windPullSpeed;
    private SerializedProperty windCenterRadius;

    private SerializedProperty windFireColor;
    private SerializedProperty windWaterColor;

    private SerializedProperty windSpreadFireDuration;
    private SerializedProperty windSpreadFireTickInterval;
    private SerializedProperty windSpreadFireTickDamage;

    private SerializedProperty windSpreadWetDuration;

    private void OnEnable()
    {
        skillType = serializedObject.FindProperty("skillType");

        targetLayer = serializedObject.FindProperty("targetLayer");
        rayDistance = serializedObject.FindProperty("rayDistance");
        fallbackDistance = serializedObject.FindProperty("fallbackDistance");

        enableSceneElementInteraction =
    serializedObject.FindProperty(
        "enableSceneElementInteraction"
    );

        sceneElementLayer =
            serializedObject.FindProperty(
                "sceneElementLayer"
            );

        inkCost = serializedObject.FindProperty("inkCost");

        useSkillCooldown = serializedObject.FindProperty("useSkillCooldown");
        skillCooldown = serializedObject.FindProperty("skillCooldown");

        baseDamage = serializedObject.FindProperty("baseDamage");
        knockback = serializedObject.FindProperty("knockback");
        element = serializedObject.FindProperty("element");
        canApplyElementStatus = serializedObject.FindProperty("canApplyElementStatus");
        skillMultiplier = serializedObject.FindProperty("skillMultiplier");
        damageBonus = serializedObject.FindProperty("damageBonus");
        reactionMultiplier = serializedObject.FindProperty("reactionMultiplier");
        canCrit = serializedObject.FindProperty("canCrit");

        slashSphereRadius = serializedObject.FindProperty("slashSphereRadius");
        slashSampleCount = serializedObject.FindProperty("slashSampleCount");

        fireDamageRadius = serializedObject.FindProperty("fireDamageRadius");
        applyBurning = serializedObject.FindProperty("applyBurning");
        burningDuration = serializedObject.FindProperty("burningDuration");
        burningTickInterval = serializedObject.FindProperty("burningTickInterval");
        burningTickDamage = serializedObject.FindProperty("burningTickDamage");
        applyInfusionToPlayer = serializedObject.FindProperty("applyInfusionToPlayer");
        infusionDuration = serializedObject.FindProperty("infusionDuration");
        infusionDamageMultiplier = serializedObject.FindProperty("infusionDamageMultiplier");
        infusionApplyBurningOnHit = serializedObject.FindProperty("infusionApplyBurningOnHit");
        infusionBurningDuration = serializedObject.FindProperty("infusionBurningDuration");
        infusionBurningTickInterval = serializedObject.FindProperty("infusionBurningTickInterval");
        infusionBurningTickDamage = serializedObject.FindProperty("infusionBurningTickDamage");

        waterAuraDuration = serializedObject.FindProperty("waterAuraDuration");
        waterUsePool = serializedObject.FindProperty("waterUsePool");
        waterProjectilePoolName = serializedObject.FindProperty("waterProjectilePoolName");
        waterProjectilePrefab = serializedObject.FindProperty("waterProjectilePrefab");
        waterShootInterval = serializedObject.FindProperty("waterShootInterval");
        waterSearchRange = serializedObject.FindProperty("waterSearchRange");
        waterProjectileDamage = serializedObject.FindProperty("waterProjectileDamage");
        waterProjectileKnockback = serializedObject.FindProperty("waterProjectileKnockback");
        wetDuration = serializedObject.FindProperty("wetDuration");
        waterUseArcHoming = serializedObject.FindProperty("waterUseArcHoming");
        waterProjectileSpeed = serializedObject.FindProperty("waterProjectileSpeed");
        waterProjectileLifeTime = serializedObject.FindProperty("waterProjectileLifeTime");
        waterArcHeight = serializedObject.FindProperty("waterArcHeight");
        waterRotateSpeed = serializedObject.FindProperty("waterRotateSpeed");
        waterReachDistance = serializedObject.FindProperty("waterReachDistance");
        waterTargetOffset = serializedObject.FindProperty("waterTargetOffset");

        waterExplosionSoundName =
            serializedObject.FindProperty("waterExplosionSoundName");

        waterExplosionSoundSync =
            serializedObject.FindProperty("waterExplosionSoundSync");

        waterProjectileHitLayer = serializedObject.FindProperty("waterProjectileHitLayer");
        waterIgnoreAttackerCollision = serializedObject.FindProperty("waterIgnoreAttackerCollision");
        waterDestroyOnNonHitLayerCollision = serializedObject.FindProperty("waterDestroyOnNonHitLayerCollision");

        waterUseBarrage = serializedObject.FindProperty("waterUseBarrage");
        waterProjectileCountPerBurst = serializedObject.FindProperty("waterProjectileCountPerBurst");
        waterBarrageAngle = serializedObject.FindProperty("waterBarrageAngle");
        waterBarrageSpawnRadius = serializedObject.FindProperty("waterBarrageSpawnRadius");
        waterBarrageVerticalStep = serializedObject.FindProperty("waterBarrageVerticalStep");
        waterDistributeTargets = serializedObject.FindProperty("waterDistributeTargets");

        waterUseRandomBarrage = serializedObject.FindProperty("waterUseRandomBarrage");
        waterBurstDelayMin = serializedObject.FindProperty("waterBurstDelayMin");
        waterBurstDelayMax = serializedObject.FindProperty("waterBurstDelayMax");
        waterRandomSpawnRadius = serializedObject.FindProperty("waterRandomSpawnRadius");
        waterRandomTargetOffsetRadius = serializedObject.FindProperty("waterRandomTargetOffsetRadius");
        waterArcSideOffsetMin = serializedObject.FindProperty("waterArcSideOffsetMin");
        waterArcSideOffsetMax = serializedObject.FindProperty("waterArcSideOffsetMax");
        waterArcHeightRandomMin = serializedObject.FindProperty("waterArcHeightRandomMin");
        waterArcHeightRandomMax = serializedObject.FindProperty("waterArcHeightRandomMax");
        waterSpeedRandomMin = serializedObject.FindProperty("waterSpeedRandomMin");
        waterSpeedRandomMax = serializedObject.FindProperty("waterSpeedRandomMax");

        woodEffectDuration = serializedObject.FindProperty("woodEffectDuration");

        woodHealAmountPerTick = serializedObject.FindProperty("woodHealAmountPerTick");
        woodHealTickInterval = serializedObject.FindProperty("woodHealTickInterval");

        woodHealVfxUsePool = serializedObject.FindProperty("woodHealVfxUsePool");
        woodHealVfxPoolName = serializedObject.FindProperty("woodHealVfxPoolName");
        woodHealVfxPrefab = serializedObject.FindProperty("woodHealVfxPrefab");
        woodHealVfxLocalOffset = serializedObject.FindProperty("woodHealVfxLocalOffset");
        woodHealVfxParentToPlayer = serializedObject.FindProperty("woodHealVfxParentToPlayer");
        woodHealVfxRecycleDelay = serializedObject.FindProperty("woodHealVfxRecycleDelay");

        woodShieldVfxUsePool = serializedObject.FindProperty("woodShieldVfxUsePool");
        woodShieldVfxPoolName = serializedObject.FindProperty("woodShieldVfxPoolName");
        woodShieldVfxPrefab = serializedObject.FindProperty("woodShieldVfxPrefab");
        woodShieldVfxLocalOffset = serializedObject.FindProperty("woodShieldVfxLocalOffset");
        woodShieldVfxParentToPlayer = serializedObject.FindProperty("woodShieldVfxParentToPlayer");
        woodShieldVfxForceLoop = serializedObject.FindProperty("woodShieldVfxForceLoop");

        woodGrantShield = serializedObject.FindProperty("woodGrantShield");
        woodShieldAmount = serializedObject.FindProperty("woodShieldAmount");
        woodShieldDuration = serializedObject.FindProperty("woodShieldDuration");
        woodRefreshShieldWhenReapply = serializedObject.FindProperty("woodRefreshShieldWhenReapply");

        bridgeTriggerLayer = serializedObject.FindProperty("bridgeTriggerLayer");
        bridgeCastRadius = serializedObject.FindProperty("bridgeCastRadius");
        bridgeActiveDuration = serializedObject.FindProperty("bridgeActiveDuration");

        windFieldPoolName =
    serializedObject.FindProperty(
        "windFieldPoolName"
    );

        windSpawnDistance =
            serializedObject.FindProperty(
                "windSpawnDistance"
            );

        windSpawnYOffset =
            serializedObject.FindProperty(
                "windSpawnYOffset"
            );

        windFieldDuration =
            serializedObject.FindProperty(
                "windFieldDuration"
            );

        windLoopSoundName =
            serializedObject.FindProperty(
                "windLoopSoundName"
            );

        windLoopSoundSync =
            serializedObject.FindProperty(
                "windLoopSoundSync"
            );

        windPullRadius =
            serializedObject.FindProperty(
                "windPullRadius"
            );

        windPullSpeed =
            serializedObject.FindProperty(
                "windPullSpeed"
            );

        windCenterRadius =
            serializedObject.FindProperty(
                "windCenterRadius"
            );

        windFireColor =
            serializedObject.FindProperty(
                "windFireColor"
            );

        windWaterColor =
            serializedObject.FindProperty(
                "windWaterColor"
            );

        windSpreadFireDuration =
            serializedObject.FindProperty(
                "windSpreadFireDuration"
            );

        windSpreadFireTickInterval =
            serializedObject.FindProperty(
                "windSpreadFireTickInterval"
            );

        windSpreadFireTickDamage =
            serializedObject.FindProperty(
                "windSpreadFireTickDamage"
            );

        windSpreadWetDuration =
            serializedObject.FindProperty(
                "windSpreadWetDuration"
            );

        windCenterTickInterval =
    serializedObject.FindProperty(
        "windCenterTickInterval"
    );
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawTitle("��������");
        EditorGUILayout.PropertyField(skillType, new GUIContent("��������"));

        DrawCommonSettings();

        BrushSkillType currentType =
            (BrushSkillType)skillType.enumValueIndex;

        switch (currentType)
        {
            case BrushSkillType.Slash:
                DrawSlashSettings();
                break;

            case BrushSkillType.Fire:
                DrawFireSettings();
                break;

            case BrushSkillType.Water:
                DrawWaterSettings();
                break;

            case BrushSkillType.Wood:
                DrawWoodSettings();
                break;

            case BrushSkillType.Bridge:
                DrawBridgeSettings();
                break;

            case BrushSkillType.Wind:
                DrawWindSettings();
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCommonSettings()
    {
        DrawTitle("ͨ�ü������");
        EditorGUILayout.PropertyField(
    targetLayer,
    new GUIContent("Ŀ�� Layer")
);

        EditorGUILayout.PropertyField(
            rayDistance,
            new GUIContent("���� / ��������")
        );

        EditorGUILayout.PropertyField(
            fallbackDistance,
            new GUIContent("Ĭ���ͷž���")
        );

        DrawTitle("����Ԫ�ؽ���");

        EditorGUILayout.PropertyField(
            enableSceneElementInteraction,
            new GUIContent("���ó���Ԫ�ؽ���")
        );

        if (enableSceneElementInteraction.boolValue)
        {
            EditorGUILayout.PropertyField(
                sceneElementLayer,
                new GUIContent("����Ԫ�� Layer")
            );
        }

        DrawTitle("ī������");
        EditorGUILayout.PropertyField(inkCost, new GUIContent("ī������"));

        DrawTitle("ͨ���˺�����");
        EditorGUILayout.PropertyField(baseDamage, new GUIContent("�����˺�"));
        EditorGUILayout.PropertyField(knockback, new GUIContent("��������"));
        EditorGUILayout.PropertyField(element, new GUIContent("Ԫ������"));
        EditorGUILayout.PropertyField(canApplyElementStatus, new GUIContent("�ɸ���Ԫ��״̬"));
        EditorGUILayout.PropertyField(skillMultiplier, new GUIContent("���ܱ���"));
        EditorGUILayout.PropertyField(damageBonus, new GUIContent("�˺��ӳ�"));
        EditorGUILayout.PropertyField(reactionMultiplier, new GUIContent("��Ӧ����"));
        EditorGUILayout.PropertyField(canCrit, new GUIContent("�Ƿ�ɱ���"));
    }

    private void DrawCooldownSettings()
    {
        DrawTitle("������ȴ");

        EditorGUILayout.PropertyField(
            useSkillCooldown,
            new GUIContent("���ü��� CD")
        );

        if (useSkillCooldown.boolValue)
        {
            EditorGUILayout.PropertyField(
                skillCooldown,
                new GUIContent("CD ʱ��")
            );
        }
    }

    private void DrawSlashSettings()
    {
        DrawTitle("Slash ר������");
        EditorGUILayout.PropertyField(slashSphereRadius, new GUIContent("���뾶"));
        EditorGUILayout.PropertyField(slashSampleCount, new GUIContent("��������"));
    }

    private void DrawFireSettings()
    {
        DrawCooldownSettings();

        DrawTitle("Fire ��Χ�˺�");
        EditorGUILayout.PropertyField(fireDamageRadius, new GUIContent("���淶Χ�뾶"));

        DrawTitle("Burning ȼ��״̬");
        EditorGUILayout.PropertyField(applyBurning, new GUIContent("�Ƿ񸽼�ȼ��"));
        EditorGUILayout.PropertyField(burningDuration, new GUIContent("ȼ�ճ���ʱ��"));
        EditorGUILayout.PropertyField(burningTickInterval, new GUIContent("ȼ�ռ��"));
        EditorGUILayout.PropertyField(burningTickDamage, new GUIContent("ȼ��ÿ���˺�"));

        DrawTitle("��һ�ħ");
        EditorGUILayout.PropertyField(applyInfusionToPlayer, new GUIContent("�Ƿ����һ�ħ"));
        EditorGUILayout.PropertyField(infusionDuration, new GUIContent("��ħ����ʱ��"));
        EditorGUILayout.PropertyField(infusionDamageMultiplier, new GUIContent("��ħ�˺�����"));
        EditorGUILayout.PropertyField(infusionApplyBurningOnHit, new GUIContent("��ħ�����Ƿ񸽼�ȼ��"));
        EditorGUILayout.PropertyField(infusionBurningDuration, new GUIContent("��ħȼ�ճ���ʱ��"));
        EditorGUILayout.PropertyField(infusionBurningTickInterval, new GUIContent("��ħȼ�ռ��"));
        EditorGUILayout.PropertyField(infusionBurningTickDamage, new GUIContent("��ħȼ���˺�"));
    }

    private void DrawWaterSettings()
    {
        DrawCooldownSettings();

        DrawTitle("Water �⻷����");
        EditorGUILayout.PropertyField(waterAuraDuration, new GUIContent("ˮ�⻷����ʱ��"));

        DrawTitle("Water ˮ������");
        EditorGUILayout.PropertyField(waterUsePool, new GUIContent("ʹ�ö����"));

        if (waterUsePool.boolValue)
        {
            EditorGUILayout.PropertyField(waterProjectilePoolName, new GUIContent("ˮ�������� / Resources ·��"));
        }
        else
        {
            EditorGUILayout.PropertyField(waterProjectilePrefab, new GUIContent("ˮ�� Prefab"));
        }

        EditorGUILayout.PropertyField(waterShootInterval, new GUIContent("������"));
        EditorGUILayout.PropertyField(waterSearchRange, new GUIContent("���з�Χ"));

        DrawTitle("Water ��Ļ����");
        EditorGUILayout.PropertyField(waterUseBarrage, new GUIContent("ʹ�õ�Ļ����"));

        if (waterUseBarrage.boolValue)
        {
            EditorGUILayout.PropertyField(waterProjectileCountPerBurst, new GUIContent("ÿ��ˮ������"));
            EditorGUILayout.PropertyField(waterBarrageAngle, new GUIContent("��Ļչ���Ƕ�"));
            EditorGUILayout.PropertyField(waterBarrageSpawnRadius, new GUIContent("������ɢ�뾶"));
            EditorGUILayout.PropertyField(waterBarrageVerticalStep, new GUIContent("���´�λ����"));
            EditorGUILayout.PropertyField(waterDistributeTargets, new GUIContent("�Ƿ��ɢ�������Ŀ��"));
        }

        DrawTitle("Water ˮ����ײ");
        EditorGUILayout.PropertyField(waterProjectileHitLayer, new GUIContent("ˮ�������� Layer"));
        EditorGUILayout.PropertyField(waterIgnoreAttackerCollision, new GUIContent("�����ͷ�����ײ"));
        EditorGUILayout.PropertyField(waterDestroyOnNonHitLayerCollision, new GUIContent("������Ŀ�� Layer �Ƿ���ʧ"));

        DrawTitle("Water ˮ���˺�");
        EditorGUILayout.PropertyField(waterProjectileDamage, new GUIContent("ˮ���˺�"));
        EditorGUILayout.PropertyField(waterProjectileKnockback, new GUIContent("ˮ������"));
        EditorGUILayout.PropertyField(wetDuration, new GUIContent("Wet ʪ�����ʱ��"));

        DrawTitle("Water ˮ������");
        EditorGUILayout.PropertyField(waterUseArcHoming, new GUIContent("ʹ�û���׷��"));
        EditorGUILayout.PropertyField(waterProjectileSpeed, new GUIContent("ˮ���ٶ�"));
        EditorGUILayout.PropertyField(waterProjectileLifeTime, new GUIContent("ˮ����������"));
        EditorGUILayout.PropertyField(waterArcHeight, new GUIContent("���߸߶�"));
        EditorGUILayout.PropertyField(waterRotateSpeed, new GUIContent("��ת�ٶ�"));
        EditorGUILayout.PropertyField(waterReachDistance, new GUIContent("���о���"));
        EditorGUILayout.PropertyField(waterTargetOffset, new GUIContent("Ŀ��ƫ��"));

        DrawTitle("Water ��ը��Ч");
        EditorGUILayout.PropertyField(
            waterExplosionSoundName,
            new GUIContent("��ը��Ч����")
        );
        EditorGUILayout.PropertyField(
            waterExplosionSoundSync,
            new GUIContent("ͬ��������Ч")
        );
        EditorGUILayout.HelpBox(
            "��д Resources/Audio �µ����·��������Ҫ��չ�������磺Skills/WaterExplosion",
            MessageType.Info
        );

        DrawTitle("Water ��Ļ�����");
        EditorGUILayout.PropertyField(waterUseRandomBarrage, new GUIContent("ʹ�������Ļ"));

        if (waterUseRandomBarrage.boolValue)
        {
            EditorGUILayout.PropertyField(waterBurstDelayMin, new GUIContent("��С�����ӳ�"));
            EditorGUILayout.PropertyField(waterBurstDelayMax, new GUIContent("������ӳ�"));
            EditorGUILayout.PropertyField(waterRandomSpawnRadius, new GUIContent("������ɰ뾶"));
            EditorGUILayout.PropertyField(waterRandomTargetOffsetRadius, new GUIContent("���Ŀ��ƫ�ư뾶"));
            EditorGUILayout.PropertyField(waterArcSideOffsetMin, new GUIContent("��С������ƫ��"));
            EditorGUILayout.PropertyField(waterArcSideOffsetMax, new GUIContent("��������ƫ��"));
            EditorGUILayout.PropertyField(waterArcHeightRandomMin, new GUIContent("��С���߸߶����"));
            EditorGUILayout.PropertyField(waterArcHeightRandomMax, new GUIContent("����߸߶����"));
            EditorGUILayout.PropertyField(waterSpeedRandomMin, new GUIContent("��С�ٶ����"));
            EditorGUILayout.PropertyField(waterSpeedRandomMax, new GUIContent("����ٶ����"));
        }
    }

    private void DrawWoodSettings()
    {
        DrawCooldownSettings();

        DrawTitle("Wood ��������");
        EditorGUILayout.PropertyField(woodEffectDuration, new GUIContent("���ܳ���ʱ��"));

        DrawTitle("Wood ��Ѫ����");
        EditorGUILayout.PropertyField(woodHealAmountPerTick, new GUIContent("ÿ����Ѫ��"));
        EditorGUILayout.PropertyField(woodHealTickInterval, new GUIContent("��Ѫ���"));

        DrawTitle("Wood ���� VFX ����");
        EditorGUILayout.PropertyField(woodHealVfxUsePool, new GUIContent("ʹ�ö����"));

        if (woodHealVfxUsePool.boolValue)
        {
            EditorGUILayout.PropertyField(woodHealVfxPoolName, new GUIContent("���� VFX ������ / Resources ·��"));
        }
        else
        {
            EditorGUILayout.PropertyField(woodHealVfxPrefab, new GUIContent("���� VFX Prefab"));
        }

        EditorGUILayout.PropertyField(woodHealVfxLocalOffset, new GUIContent("���� VFX ����ƫ��"));
        EditorGUILayout.PropertyField(woodHealVfxParentToPlayer, new GUIContent("���� VFX �Ƿ���� Player"));
        EditorGUILayout.PropertyField(woodHealVfxRecycleDelay, new GUIContent("���� VFX �����ӳ�"));

        DrawTitle("Wood ���� VFX ����");
        EditorGUILayout.PropertyField(woodShieldVfxUsePool, new GUIContent("ʹ�ö����"));

        if (woodShieldVfxUsePool.boolValue)
        {
            EditorGUILayout.PropertyField(woodShieldVfxPoolName, new GUIContent("���� VFX ������ / Resources ·��"));
        }
        else
        {
            EditorGUILayout.PropertyField(woodShieldVfxPrefab, new GUIContent("���� VFX Prefab"));
        }

        EditorGUILayout.PropertyField(woodShieldVfxLocalOffset, new GUIContent("���� VFX ����ƫ��"));
        EditorGUILayout.PropertyField(woodShieldVfxParentToPlayer, new GUIContent("���� VFX �Ƿ���� Player λ��"));
        EditorGUILayout.PropertyField(woodShieldVfxForceLoop, new GUIContent("���� VFX ǿ��ѭ��"));

        DrawTitle("Wood ��������");
        EditorGUILayout.PropertyField(woodGrantShield, new GUIContent("�Ƿ���軤��"));

        if (woodGrantShield.boolValue)
        {
            EditorGUILayout.PropertyField(woodShieldAmount, new GUIContent("����ֵ"));
            EditorGUILayout.PropertyField(woodShieldDuration, new GUIContent("���ܳ���ʱ��"));
            EditorGUILayout.PropertyField(woodRefreshShieldWhenReapply, new GUIContent("�ظ��ͷ��Ƿ�ˢ�»���"));
        }
    }
    private void DrawBridgeSettings()
    {
        DrawTitle("Bridge ר������");
        EditorGUILayout.PropertyField(bridgeTriggerLayer, new GUIContent("�Ŵ����� Layer"));
        EditorGUILayout.PropertyField(bridgeCastRadius, new GUIContent("���뾶"));
        EditorGUILayout.PropertyField(bridgeActiveDuration, new GUIContent("�ų���ʱ��"));
    }

    private void DrawWindSettings()
    {
        DrawCooldownSettings();

        DrawTitle("Wind �糡����");

        EditorGUILayout.PropertyField(
            windFieldPoolName,
            new GUIContent("�糡�����·��")
        );

        EditorGUILayout.PropertyField(
            windSpawnDistance,
            new GUIContent("��ͷǰ���ɾ���")
        );

        EditorGUILayout.PropertyField(
            windSpawnYOffset,
            new GUIContent("���� Y ƫ��")
        );

        EditorGUILayout.PropertyField(
            windFieldDuration,
            new GUIContent("�糡����ʱ��")
        );

        DrawTitle("Wind ������Ч");

        EditorGUILayout.PropertyField(
            windLoopSoundName,
            new GUIContent("ѭ����Ч����")
        );

        EditorGUILayout.PropertyField(
            windLoopSoundSync,
            new GUIContent("ͬ��������Ч")
        );

        EditorGUILayout.HelpBox(
            "��д Resources/Audio �µ����·��������Ҫ��չ�������磺Skills/WindLoop������Ч���ڷ糡�����ڼ�ѭ�����š�",
            MessageType.Info
        );

        DrawTitle("Wind ��ק");

        EditorGUILayout.PropertyField(
            windPullRadius,
            new GUIContent("��ק��Χ")
        );

        EditorGUILayout.PropertyField(
            windPullSpeed,
            new GUIContent("��ק�ٶ�")
        );

        EditorGUILayout.PropertyField(
            windCenterRadius,
            new GUIContent("�糡�����ж��뾶")
        );

        DrawTitle("Wind ���ĳ����˺�");

        EditorGUILayout.PropertyField(
            windCenterTickInterval,
            new GUIContent("�����˺����")
        );

        DrawTitle("Wind ��ɢ��ɫ");

        EditorGUILayout.PropertyField(
            windFireColor,
            new GUIContent("Fire Ⱦɫ")
        );

        EditorGUILayout.PropertyField(
            windWaterColor,
            new GUIContent("Water Ⱦɫ")
        );

        DrawTitle("Wind Fire ��ɢ");

        EditorGUILayout.PropertyField(
            windSpreadFireDuration,
            new GUIContent("Fire ״̬����ʱ��")
        );

        EditorGUILayout.PropertyField(
            windSpreadFireTickInterval,
            new GUIContent("ȼ�ռ��")
        );

        EditorGUILayout.PropertyField(
            windSpreadFireTickDamage,
            new GUIContent("ȼ��ÿ���˺�")
        );

        DrawTitle("Wind Water ��ɢ");

        EditorGUILayout.PropertyField(
            windSpreadWetDuration,
            new GUIContent("Wet ״̬����ʱ��")
        );
    }

    private void DrawTitle(string title)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }


}