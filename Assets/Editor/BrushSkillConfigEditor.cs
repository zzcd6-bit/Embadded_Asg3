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

        DrawTitle("画笔技能配置");
        EditorGUILayout.PropertyField(skillType, new GUIContent("技能类型"));

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
        DrawTitle("通用技能设置");
        EditorGUILayout.PropertyField(
    targetLayer,
    new GUIContent("目标 Layer")
);

        EditorGUILayout.PropertyField(
            rayDistance,
            new GUIContent("射线 / 检测距离")
        );

        EditorGUILayout.PropertyField(
            fallbackDistance,
            new GUIContent("默认释放距离")
        );

        DrawTitle("场景元素交互");

        EditorGUILayout.PropertyField(
            enableSceneElementInteraction,
            new GUIContent("启用场景元素交互")
        );

        if (enableSceneElementInteraction.boolValue)
        {
            EditorGUILayout.PropertyField(
                sceneElementLayer,
                new GUIContent("场景元素 Layer")
            );
        }

        DrawTitle("墨量消耗");
        EditorGUILayout.PropertyField(inkCost, new GUIContent("墨量消耗"));

        DrawTitle("通用伤害设置");
        EditorGUILayout.PropertyField(baseDamage, new GUIContent("基础伤害"));
        EditorGUILayout.PropertyField(knockback, new GUIContent("击退力度"));
        EditorGUILayout.PropertyField(element, new GUIContent("元素类型"));
        EditorGUILayout.PropertyField(canApplyElementStatus, new GUIContent("可附加元素状态"));
        EditorGUILayout.PropertyField(skillMultiplier, new GUIContent("技能倍率"));
        EditorGUILayout.PropertyField(damageBonus, new GUIContent("伤害加成"));
        EditorGUILayout.PropertyField(reactionMultiplier, new GUIContent("反应倍率"));
        EditorGUILayout.PropertyField(canCrit, new GUIContent("是否可暴击"));
    }

    private void DrawCooldownSettings()
    {
        DrawTitle("技能冷却");

        EditorGUILayout.PropertyField(
            useSkillCooldown,
            new GUIContent("启用技能 CD")
        );

        if (useSkillCooldown.boolValue)
        {
            EditorGUILayout.PropertyField(
                skillCooldown,
                new GUIContent("CD 时间")
            );
        }
    }

    private void DrawSlashSettings()
    {
        DrawTitle("Slash 专属设置");
        EditorGUILayout.PropertyField(slashSphereRadius, new GUIContent("检测半径"));
        EditorGUILayout.PropertyField(slashSampleCount, new GUIContent("采样数量"));
    }

    private void DrawFireSettings()
    {
        DrawCooldownSettings();

        DrawTitle("Fire 范围伤害");
        EditorGUILayout.PropertyField(fireDamageRadius, new GUIContent("火焰范围半径"));

        DrawTitle("Burning 燃烧状态");
        EditorGUILayout.PropertyField(applyBurning, new GUIContent("是否附加燃烧"));
        EditorGUILayout.PropertyField(burningDuration, new GUIContent("燃烧持续时间"));
        EditorGUILayout.PropertyField(burningTickInterval, new GUIContent("燃烧间隔"));
        EditorGUILayout.PropertyField(burningTickDamage, new GUIContent("燃烧每跳伤害"));

        DrawTitle("玩家火元素附魔");
        EditorGUILayout.PropertyField(applyInfusionToPlayer, new GUIContent("是否给予火元素附魔"));
        EditorGUILayout.PropertyField(infusionDuration, new GUIContent("附魔持续时间"));
        EditorGUILayout.PropertyField(infusionDamageMultiplier, new GUIContent("附魔伤害倍率"));
        EditorGUILayout.PropertyField(infusionApplyBurningOnHit, new GUIContent("附魔攻击是否附加燃烧"));
        EditorGUILayout.PropertyField(infusionBurningDuration, new GUIContent("附魔燃烧持续时间"));
        EditorGUILayout.PropertyField(infusionBurningTickInterval, new GUIContent("附魔燃烧间隔"));
        EditorGUILayout.PropertyField(infusionBurningTickDamage, new GUIContent("附魔燃烧每跳伤害"));
    }

    private void DrawWaterSettings()
    {
        DrawCooldownSettings();

        DrawTitle("Water 光环设置");
        EditorGUILayout.PropertyField(waterAuraDuration, new GUIContent("水光环持续时间"));

        DrawTitle("Water 水弹设置");
        EditorGUILayout.PropertyField(waterUsePool, new GUIContent("使用对象池"));

        if (waterUsePool.boolValue)
        {
            EditorGUILayout.PropertyField(waterProjectilePoolName, new GUIContent("水弹对象池名称 / Resources 路径"));
        }
        else
        {
            EditorGUILayout.PropertyField(waterProjectilePrefab, new GUIContent("水弹 Prefab"));
        }

        EditorGUILayout.PropertyField(waterShootInterval, new GUIContent("发射间隔"));
        EditorGUILayout.PropertyField(waterSearchRange, new GUIContent("索敌范围"));

        DrawTitle("Water 弹幕设置");
        EditorGUILayout.PropertyField(waterUseBarrage, new GUIContent("使用弹幕发射"));

        if (waterUseBarrage.boolValue)
        {
            EditorGUILayout.PropertyField(waterProjectileCountPerBurst, new GUIContent("每波水弹数量"));
            EditorGUILayout.PropertyField(waterBarrageAngle, new GUIContent("弹幕展开角度"));
            EditorGUILayout.PropertyField(waterBarrageSpawnRadius, new GUIContent("生成扩散半径"));
            EditorGUILayout.PropertyField(waterBarrageVerticalStep, new GUIContent("上下错位间隔"));
            EditorGUILayout.PropertyField(waterDistributeTargets, new GUIContent("是否分散攻击多个目标"));
        }

        DrawTitle("Water 水弹碰撞");
        EditorGUILayout.PropertyField(waterProjectileHitLayer, new GUIContent("水弹命中 Layer"));
        EditorGUILayout.PropertyField(waterIgnoreAttackerCollision, new GUIContent("忽略释放者碰撞"));
        EditorGUILayout.PropertyField(waterDestroyOnNonHitLayerCollision, new GUIContent("碰到非目标 Layer 是否消失"));

        DrawTitle("Water 水弹伤害");
        EditorGUILayout.PropertyField(waterProjectileDamage, new GUIContent("水弹伤害"));
        EditorGUILayout.PropertyField(waterProjectileKnockback, new GUIContent("水弹击退"));
        EditorGUILayout.PropertyField(wetDuration, new GUIContent("Wet 潮湿持续时间"));

        DrawTitle("Water 水弹飞行");
        EditorGUILayout.PropertyField(waterUseArcHoming, new GUIContent("使用弧线追踪"));
        EditorGUILayout.PropertyField(waterProjectileSpeed, new GUIContent("水弹速度"));
        EditorGUILayout.PropertyField(waterProjectileLifeTime, new GUIContent("水弹生命周期"));
        EditorGUILayout.PropertyField(waterArcHeight, new GUIContent("弧线高度"));
        EditorGUILayout.PropertyField(waterRotateSpeed, new GUIContent("旋转速度"));
        EditorGUILayout.PropertyField(waterReachDistance, new GUIContent("命中距离"));
        EditorGUILayout.PropertyField(waterTargetOffset, new GUIContent("目标偏移"));

        DrawTitle("Water 爆炸音效");
        EditorGUILayout.PropertyField(
            waterExplosionSoundName,
            new GUIContent("爆炸音效名称")
        );
        EditorGUILayout.PropertyField(
            waterExplosionSoundSync,
            new GUIContent("同步播放音效")
        );
        EditorGUILayout.HelpBox(
            "填写 Resources/Audio 下的相对路径，不需要扩展名。例如：Skills/WaterExplosion",
            MessageType.Info
        );

        DrawTitle("Water 随机弹幕");
        EditorGUILayout.PropertyField(waterUseRandomBarrage, new GUIContent("使用随机弹幕"));

        if (waterUseRandomBarrage.boolValue)
        {
            EditorGUILayout.PropertyField(waterBurstDelayMin, new GUIContent("最小发射延迟"));
            EditorGUILayout.PropertyField(waterBurstDelayMax, new GUIContent("最大发射延迟"));
            EditorGUILayout.PropertyField(waterRandomSpawnRadius, new GUIContent("随机生成半径"));
            EditorGUILayout.PropertyField(waterRandomTargetOffsetRadius, new GUIContent("随机目标偏移半径"));
            EditorGUILayout.PropertyField(waterArcSideOffsetMin, new GUIContent("最小弧线侧向偏移"));
            EditorGUILayout.PropertyField(waterArcSideOffsetMax, new GUIContent("最大弧线侧向偏移"));
            EditorGUILayout.PropertyField(waterArcHeightRandomMin, new GUIContent("最小随机弧线高度"));
            EditorGUILayout.PropertyField(waterArcHeightRandomMax, new GUIContent("最大随机弧线高度"));
            EditorGUILayout.PropertyField(waterSpeedRandomMin, new GUIContent("最小随机速度"));
            EditorGUILayout.PropertyField(waterSpeedRandomMax, new GUIContent("最大随机速度"));
        }
    }

    private void DrawWoodSettings()
    {
        DrawCooldownSettings();

        DrawTitle("Wood 效果设置");
        EditorGUILayout.PropertyField(woodEffectDuration, new GUIContent("技能持续时间"));

        DrawTitle("Wood 回血设置");
        EditorGUILayout.PropertyField(woodHealAmountPerTick, new GUIContent("每跳回血量"));
        EditorGUILayout.PropertyField(woodHealTickInterval, new GUIContent("回血间隔"));

        DrawTitle("Wood 治疗 VFX 设置");
        EditorGUILayout.PropertyField(woodHealVfxUsePool, new GUIContent("使用对象池"));

        if (woodHealVfxUsePool.boolValue)
        {
            EditorGUILayout.PropertyField(woodHealVfxPoolName, new GUIContent("治疗 VFX 对象池名称 / Resources 路径"));
        }
        else
        {
            EditorGUILayout.PropertyField(woodHealVfxPrefab, new GUIContent("治疗 VFX Prefab"));
        }

        EditorGUILayout.PropertyField(woodHealVfxLocalOffset, new GUIContent("治疗 VFX 本地偏移"));
        EditorGUILayout.PropertyField(woodHealVfxParentToPlayer, new GUIContent("治疗 VFX 是否跟随 Player"));
        EditorGUILayout.PropertyField(woodHealVfxRecycleDelay, new GUIContent("治疗 VFX 回收延迟"));

        DrawTitle("Wood 护盾 VFX 设置");
        EditorGUILayout.PropertyField(woodShieldVfxUsePool, new GUIContent("使用对象池"));

        if (woodShieldVfxUsePool.boolValue)
        {
            EditorGUILayout.PropertyField(woodShieldVfxPoolName, new GUIContent("护盾 VFX 对象池名称 / Resources 路径"));
        }
        else
        {
            EditorGUILayout.PropertyField(woodShieldVfxPrefab, new GUIContent("护盾 VFX Prefab"));
        }

        EditorGUILayout.PropertyField(woodShieldVfxLocalOffset, new GUIContent("护盾 VFX 本地偏移"));
        EditorGUILayout.PropertyField(woodShieldVfxParentToPlayer, new GUIContent("护盾 VFX 是否跟随 Player"));
        EditorGUILayout.PropertyField(woodShieldVfxForceLoop, new GUIContent("护盾 VFX 强制循环"));

        DrawTitle("Wood 护盾设置");
        EditorGUILayout.PropertyField(woodGrantShield, new GUIContent("是否给予护盾"));

        if (woodGrantShield.boolValue)
        {
            EditorGUILayout.PropertyField(woodShieldAmount, new GUIContent("护盾值"));
            EditorGUILayout.PropertyField(woodShieldDuration, new GUIContent("护盾持续时间"));
            EditorGUILayout.PropertyField(woodRefreshShieldWhenReapply, new GUIContent("重复释放是否刷新护盾"));
        }
    }
    private void DrawBridgeSettings()
    {
        DrawTitle("Bridge 专属设置");
        EditorGUILayout.PropertyField(bridgeTriggerLayer, new GUIContent("桥触发 Layer"));
        EditorGUILayout.PropertyField(bridgeCastRadius, new GUIContent("检测半径"));
        EditorGUILayout.PropertyField(bridgeActiveDuration, new GUIContent("桥持续时间"));
    }

    private void DrawWindSettings()
    {
        DrawCooldownSettings();

        DrawTitle("Wind 风场设置");

        EditorGUILayout.PropertyField(
            windFieldPoolName,
            new GUIContent("风场对象池路径")
        );

        EditorGUILayout.PropertyField(
            windSpawnDistance,
            new GUIContent("镜头前生成距离")
        );

        EditorGUILayout.PropertyField(
            windSpawnYOffset,
            new GUIContent("生成 Y 偏移")
        );

        EditorGUILayout.PropertyField(
            windFieldDuration,
            new GUIContent("风场持续时间")
        );

        DrawTitle("Wind 循环音效");

        EditorGUILayout.PropertyField(
            windLoopSoundName,
            new GUIContent("循环音效名称")
        );

        EditorGUILayout.PropertyField(
            windLoopSoundSync,
            new GUIContent("同步播放音效")
        );

        EditorGUILayout.HelpBox(
            "填写 Resources/Audio 下的相对路径，不需要扩展名。例如：Skills/WindLoop。音效会在风场存在期间循环播放。",
            MessageType.Info
        );

        DrawTitle("Wind 拉拽");

        EditorGUILayout.PropertyField(
            windPullRadius,
            new GUIContent("拉拽范围")
        );

        EditorGUILayout.PropertyField(
            windPullSpeed,
            new GUIContent("拉拽速度")
        );

        EditorGUILayout.PropertyField(
            windCenterRadius,
            new GUIContent("风场中心判定半径")
        );

        DrawTitle("Wind 中心持续伤害");

        EditorGUILayout.PropertyField(
            windCenterTickInterval,
            new GUIContent("持续伤害间隔")
        );

        DrawTitle("Wind 扩散染色");

        EditorGUILayout.PropertyField(
            windFireColor,
            new GUIContent("Fire 染色")
        );

        EditorGUILayout.PropertyField(
            windWaterColor,
            new GUIContent("Water 染色")
        );

        DrawTitle("Wind Fire 扩散");

        EditorGUILayout.PropertyField(
            windSpreadFireDuration,
            new GUIContent("Fire 状态持续时间")
        );

        EditorGUILayout.PropertyField(
            windSpreadFireTickInterval,
            new GUIContent("燃烧间隔")
        );

        EditorGUILayout.PropertyField(
            windSpreadFireTickDamage,
            new GUIContent("燃烧每跳伤害")
        );

        DrawTitle("Wind Water 扩散");

        EditorGUILayout.PropertyField(
            windSpreadWetDuration,
            new GUIContent("Wet 状态持续时间")
        );
    }

    private void DrawTitle(string title)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }


}