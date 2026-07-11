using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BrushSkillConfig))]
public class BrushSkillConfigEditor : Editor
{
    private SerializedProperty skillType;

    private SerializedProperty targetLayer;
    private SerializedProperty rayDistance;
    private SerializedProperty fallbackDistance;

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

    private SerializedProperty bridgeTriggerLayer;
    private SerializedProperty bridgeCastRadius;
    private SerializedProperty bridgeActiveDuration;

    private SerializedProperty inkCost;

    private void OnEnable()
    {
        skillType = serializedObject.FindProperty("skillType");

        targetLayer = serializedObject.FindProperty("targetLayer");
        rayDistance = serializedObject.FindProperty("rayDistance");
        fallbackDistance = serializedObject.FindProperty("fallbackDistance");

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

        bridgeTriggerLayer = serializedObject.FindProperty("bridgeTriggerLayer");
        bridgeCastRadius = serializedObject.FindProperty("bridgeCastRadius");
        bridgeActiveDuration = serializedObject.FindProperty("bridgeActiveDuration");

        inkCost = serializedObject.FindProperty("inkCost");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawTitle("技能身份");

        DrawSkillTypePopup();

        BrushSkillType selectedSkillType =
            (BrushSkillType)skillType.enumValueIndex;

        EditorGUILayout.Space(8);

        if (selectedSkillType == BrushSkillType.None)
        {
            EditorGUILayout.HelpBox(
                "请先选择一个技能类型。选择 Slash / Fire / Bridge 后，才会显示对应的参数。",
                MessageType.Info
            );

            serializedObject.ApplyModifiedProperties();
            return;
        }

        DrawCommonSettings(selectedSkillType);

        EditorGUILayout.Space(8);

        switch (selectedSkillType)
        {
            case BrushSkillType.Slash:
                DrawSlashSettings();
                break;

            case BrushSkillType.Fire:
                DrawFireSettings();
                break;

            case BrushSkillType.Bridge:
                DrawBridgeSettings();
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSkillTypePopup()
    {
        string[] displayNames =
        {
            "无",
            "斩击 Slash",
            "火符 Fire",
            "桥符 Bridge"
        };

        int[] values =
        {
            (int)BrushSkillType.None,
            (int)BrushSkillType.Slash,
            (int)BrushSkillType.Fire,
            (int)BrushSkillType.Bridge
        };

        skillType.enumValueIndex = EditorGUILayout.IntPopup(
            "技能类型",
            skillType.enumValueIndex,
            displayNames,
            values
        );
    }

    private void DrawCommonSettings(BrushSkillType selectedSkillType)
    {
        DrawTitle("通用检测设置");

        EditorGUILayout.PropertyField(targetLayer, new GUIContent("目标 Layer"));

        EditorGUILayout.PropertyField(rayDistance, new GUIContent("射线距离"));

        if (selectedSkillType == BrushSkillType.Fire ||
            selectedSkillType == BrushSkillType.Bridge)
        {
            EditorGUILayout.PropertyField(fallbackDistance, new GUIContent("默认释放距离"));
        }

        EditorGUILayout.Space(8);

        DrawTitle("墨囊消耗设置");
        EditorGUILayout.PropertyField(inkCost, new GUIContent("墨囊消耗"));

        EditorGUILayout.Space(8);

        DrawTitle("通用伤害设置");

        EditorGUILayout.PropertyField(baseDamage, new GUIContent("基础伤害"));
        EditorGUILayout.PropertyField(knockback, new GUIContent("击退力度"));
        EditorGUILayout.PropertyField(element, new GUIContent("元素类型"));
        EditorGUILayout.PropertyField(canApplyElementStatus, new GUIContent("可附加元素状态"));

        EditorGUILayout.PropertyField(skillMultiplier, new GUIContent("技能倍率"));
        EditorGUILayout.PropertyField(damageBonus, new GUIContent("伤害加成"));
        EditorGUILayout.PropertyField(reactionMultiplier, new GUIContent("反应倍率"));
        EditorGUILayout.PropertyField(canCrit, new GUIContent("是否可以暴击"));
    }

    private void DrawSlashSettings()
    {
        DrawTitle("斩击 Slash 设置");

        EditorGUILayout.HelpBox(
            "Slash 会沿着画符轨迹采样多条射线，检测轨迹路径上的敌人。",
            MessageType.None
        );

        EditorGUILayout.PropertyField(slashSphereRadius, new GUIContent("检测半径"));
        EditorGUILayout.PropertyField(slashSampleCount, new GUIContent("轨迹采样数量"));
    }

    private void DrawFireSettings()
    {
        DrawTitle("火符 Fire 设置");

        EditorGUILayout.HelpBox(
            "Fire 会在画符中心射线命中的敌人或地面位置释放范围火焰。",
            MessageType.None
        );

        EditorGUILayout.PropertyField(fireDamageRadius, new GUIContent("火焰伤害范围"));

        EditorGUILayout.Space(8);

        DrawTitle("燃烧 Debuff 设置");

        EditorGUILayout.PropertyField(applyBurning, new GUIContent("施加燃烧"));

        if (applyBurning.boolValue)
        {
            EditorGUILayout.PropertyField(burningDuration, new GUIContent("燃烧持续时间"));
            EditorGUILayout.PropertyField(burningTickInterval, new GUIContent("燃烧间隔"));
            EditorGUILayout.PropertyField(burningTickDamage, new GUIContent("每次燃烧伤害"));
        }

        EditorGUILayout.Space(8);

        DrawTitle("玩家附火设置");

        EditorGUILayout.PropertyField(applyInfusionToPlayer, new GUIContent("给玩家附火"));

        if (applyInfusionToPlayer.boolValue)
        {
            EditorGUILayout.PropertyField(infusionDuration, new GUIContent("附火持续时间"));
            EditorGUILayout.PropertyField(infusionDamageMultiplier, new GUIContent("附火伤害倍率"));

            EditorGUILayout.Space(4);

            EditorGUILayout.PropertyField(
                infusionApplyBurningOnHit,
                new GUIContent("附火攻击施加燃烧")
            );

            if (infusionApplyBurningOnHit.boolValue)
            {
                EditorGUILayout.PropertyField(infusionBurningDuration, new GUIContent("攻击燃烧持续时间"));
                EditorGUILayout.PropertyField(infusionBurningTickInterval, new GUIContent("攻击燃烧间隔"));
                EditorGUILayout.PropertyField(infusionBurningTickDamage, new GUIContent("攻击燃烧伤害"));
            }
        }
    }

    private void DrawBridgeSettings()
    {
        DrawTitle("桥符 Bridge 设置");

        EditorGUILayout.HelpBox(
            "Bridge 会根据画符位置在地面生成桥。后续 BridgeBrushSkill 会读取这些参数。",
            MessageType.None
        );

        EditorGUILayout.PropertyField(bridgeTriggerLayer, new GUIContent("桥触发器 Layer"));
        EditorGUILayout.PropertyField(bridgeCastRadius, new GUIContent("射线检测半径"));
        EditorGUILayout.PropertyField(bridgeActiveDuration, new GUIContent("桥激活时间"));
    }

    private void DrawTitle(string title)
    {
        EditorGUILayout.Space(6);

        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.fontSize = 13;

        EditorGUILayout.LabelField(title, style);
    }
}