#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

internal static class InventoryEditorGUI
{
    public static void DrawSection(
        string title,
        Action drawAction
    )
    {
        EditorGUILayout.Space(6);

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox
        );

        EditorGUILayout.LabelField(
            title,
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(2);

        drawAction?.Invoke();

        EditorGUILayout.EndVertical();
    }

    public static void DrawChineseEnum(
        SerializedProperty property,
        string label
    )
    {
        if (property == null)
            return;

        string[] sourceNames =
            property.enumDisplayNames;

        string[] displayNames =
            new string[sourceNames.Length];

        for (int i = 0;
             i < sourceNames.Length;
             i++)
        {
            displayNames[i] =
                TranslateEnumName(
                    sourceNames[i]
                );
        }

        int currentIndex =
            Mathf.Clamp(
                property.enumValueIndex,
                0,
                Mathf.Max(
                    0,
                    displayNames.Length - 1
                )
            );

        int newIndex =
            EditorGUILayout.Popup(
                label,
                currentIndex,
                displayNames
            );

        if (newIndex != currentIndex)
        {
            property.enumValueIndex =
                newIndex;
        }
    }

    public static void DrawPercent(
        SerializedProperty property,
        string label
    )
    {
        EditorGUILayout.PropertyField(
            property,
            new GUIContent(
                label,
                "百分比使用小数填写，例如 0.15 表示 15%。"
            )
        );
    }

    private static string TranslateEnumName(
        string enumName
    )
    {
        switch (enumName)
        {
            case "Common":
                return "普通";

            case "Rare":
                return "稀有";

            case "Legendary":
                return "传说";

            case "Helmet":
                return "头盔";

            case "Chest":
                return "胸甲";

            case "Shoes":
                return "鞋子";

            case "Weapon":
                return "武器";

            case "Physical":
                return "物理";

            case "Fire":
                return "火";

            case "Water":
                return "水";

            case "Ice":
                return "冰";

            case "Thunder":
                return "雷";

            case "Earth":
                return "土";

            case "Wind":
                return "风";

            case "CharacterExperience":
                return "人物经验书";

            case "WeaponExperience":
                return "武器经验书";

            case "HealHp":
                return "恢复生命值";

            case "RestoreInk":
                return "恢复墨量";

            case "ElementDamageBuff":
                return "元素伤害药水";

            case "ElementResistanceBuff":
                return "元素抗性药水";

            default:
                return enumName;
        }
    }
}

public abstract class ItemDataChineseEditorBase :
    Editor
{
    protected SerializedProperty itemId;
    protected SerializedProperty displayName;
    protected SerializedProperty description;
    protected SerializedProperty icon;
    protected SerializedProperty quality;
    protected SerializedProperty canDiscard;
    protected SerializedProperty maxStack;

    protected virtual void OnEnable()
    {
        itemId =
            serializedObject.FindProperty(
                "itemId"
            );

        displayName =
            serializedObject.FindProperty(
                "displayName"
            );

        description =
            serializedObject.FindProperty(
                "description"
            );

        icon =
            serializedObject.FindProperty(
                "icon"
            );

        quality =
            serializedObject.FindProperty(
                "quality"
            );

        canDiscard =
            serializedObject.FindProperty(
                "canDiscard"
            );

        maxStack =
            serializedObject.FindProperty(
                "maxStack"
            );
    }

    protected void DrawBaseInformation(
        bool showQuality,
        bool showMaxStack
    )
    {
        EditorGUILayout.PropertyField(
            itemId,
            new GUIContent(
                "道具 ID",
                "用于存档和查找。创建后尽量不要修改。"
            )
        );

        EditorGUILayout.PropertyField(
            displayName,
            new GUIContent("显示名称")
        );

        EditorGUILayout.PropertyField(
            icon,
            new GUIContent(
                "道具图标",
                "背包格子和详情面板显示的图片。"
            )
        );

        EditorGUILayout.PropertyField(
            description,
            new GUIContent("道具说明")
        );

        if (showQuality)
        {
            InventoryEditorGUI.DrawChineseEnum(
                quality,
                "装备稀有度"
            );
        }

        EditorGUILayout.PropertyField(
            canDiscard,
            new GUIContent("允许丢弃")
        );

        if (showMaxStack)
        {
            EditorGUILayout.PropertyField(
                maxStack,
                new GUIContent("最大堆叠数量")
            );
        }
    }
}

[CustomEditor(typeof(EquipmentData))]
[CanEditMultipleObjects]
public class EquipmentDataChineseEditor :
    ItemDataChineseEditorBase
{
    private SerializedProperty slotType;
    private SerializedProperty equipmentSet;

    private SerializedProperty maxHpBonus;

    private SerializedProperty defenseBonus;
    private SerializedProperty resistanceElement;
    private SerializedProperty elementResistanceBonus;

    private SerializedProperty damageBonus;
    private SerializedProperty damageElement;
    private SerializedProperty elementDamageBonus;

    private SerializedProperty baseAttackPower;
    private SerializedProperty attackPowerPerLevel;

    private SerializedProperty criticalRateBonus;
    private SerializedProperty criticalDamageBonus;

    private SerializedProperty weaponMaxLevel;
    private SerializedProperty
        levelOneExperienceRequirement;

    private SerializedProperty
        experienceIncreasePerLevel;

    protected override void OnEnable()
    {
        base.OnEnable();

        slotType =
            serializedObject.FindProperty(
                "slotType"
            );

        equipmentSet =
            serializedObject.FindProperty(
                "equipmentSet"
            );

        maxHpBonus =
            serializedObject.FindProperty(
                "maxHpBonus"
            );

        defenseBonus =
            serializedObject.FindProperty(
                "defenseBonus"
            );

        resistanceElement =
            serializedObject.FindProperty(
                "resistanceElement"
            );

        elementResistanceBonus =
            serializedObject.FindProperty(
                "elementResistanceBonus"
            );

        damageBonus =
            serializedObject.FindProperty(
                "damageBonus"
            );

        damageElement =
            serializedObject.FindProperty(
                "damageElement"
            );

        elementDamageBonus =
            serializedObject.FindProperty(
                "elementDamageBonus"
            );

        baseAttackPower =
            serializedObject.FindProperty(
                "baseAttackPower"
            );

        attackPowerPerLevel =
            serializedObject.FindProperty(
                "attackPowerPerLevel"
            );

        criticalRateBonus =
            serializedObject.FindProperty(
                "criticalRateBonus"
            );

        criticalDamageBonus =
            serializedObject.FindProperty(
                "criticalDamageBonus"
            );

        weaponMaxLevel =
            serializedObject.FindProperty(
                "weaponMaxLevel"
            );

        levelOneExperienceRequirement =
            serializedObject.FindProperty(
                "levelOneExperienceRequirement"
            );

        experienceIncreasePerLevel =
            serializedObject.FindProperty(
                "experienceIncreasePerLevel"
            );
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        InventoryEditorGUI.DrawSection(
            "基础信息",
            () =>
            {
                DrawBaseInformation(
                    true,
                    false
                );
            }
        );

        InventoryEditorGUI.DrawSection(
            "装备分类",
            () =>
            {
                InventoryEditorGUI
                    .DrawChineseEnum(
                        slotType,
                        "装备部位"
                    );

                EditorGUILayout.PropertyField(
                    equipmentSet,
                    new GUIContent(
                        "所属套装",
                        "四件相同套装装备后触发套装效果。"
                    )
                );
            }
        );

        EquipmentSlotType currentSlot =
            (EquipmentSlotType)
            slotType.enumValueIndex;

        switch (currentSlot)
        {
            case EquipmentSlotType.Helmet:

                DrawHelmetStats();
                break;

            case EquipmentSlotType.Chest:

                DrawChestStats();
                break;

            case EquipmentSlotType.Shoes:

                DrawShoesStats();
                break;

            case EquipmentSlotType.Weapon:

                DrawWeaponStats();
                break;
        }

        InventoryEditorGUI.DrawSection(
            "配置工具",
            () =>
            {
                EditorGUILayout.HelpBox(
                    "所有百分比填写小数。" +
                    "\n例如：0.10 = 10%，0.25 = 25%。",
                    MessageType.Info
                );

                if (GUILayout.Button(
                        "清空当前装备无关词条"
                    ))
                {
                    ClearIrrelevantStats(
                        currentSlot
                    );
                }
            }
        );

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHelmetStats()
    {
        InventoryEditorGUI.DrawSection(
            "头盔固定词条",
            () =>
            {
                EditorGUILayout.HelpBox(
                    "主词条：生命值\n" +
                    "副词条：暴击率、暴击伤害",
                    MessageType.None
                );

                EditorGUILayout.PropertyField(
                    maxHpBonus,
                    new GUIContent(
                        "生命值加成"
                    )
                );

                InventoryEditorGUI.DrawPercent(
                    criticalRateBonus,
                    "暴击率加成"
                );

                InventoryEditorGUI.DrawPercent(
                    criticalDamageBonus,
                    "暴击伤害加成"
                );
            }
        );
    }

    private void DrawChestStats()
    {
        InventoryEditorGUI.DrawSection(
            "胸甲固定词条",
            () =>
            {
                EditorGUILayout.HelpBox(
                    "主词条：防御力\n" +
                    "副词条：单一属性抗性",
                    MessageType.None
                );

                EditorGUILayout.PropertyField(
                    defenseBonus,
                    new GUIContent(
                        "防御力加成"
                    )
                );

                InventoryEditorGUI
                    .DrawChineseEnum(
                        resistanceElement,
                        "抗性属性"
                    );

                InventoryEditorGUI.DrawPercent(
                    elementResistanceBonus,
                    "属性抗性加成"
                );
            }
        );
    }

    private void DrawShoesStats()
    {
        InventoryEditorGUI.DrawSection(
            "鞋子固定词条",
            () =>
            {
                EditorGUILayout.HelpBox(
                    "主词条：整体伤害加成\n" +
                    "副词条：单一属性伤害加成",
                    MessageType.None
                );

                InventoryEditorGUI.DrawPercent(
                    damageBonus,
                    "整体伤害加成"
                );

                InventoryEditorGUI
                    .DrawChineseEnum(
                        damageElement,
                        "伤害属性"
                    );

                InventoryEditorGUI.DrawPercent(
                    elementDamageBonus,
                    "属性伤害加成"
                );
            }
        );
    }

    private void DrawWeaponStats()
    {
        InventoryEditorGUI.DrawSection(
            "武器固定词条",
            () =>
            {
                EditorGUILayout.HelpBox(
                    "主词条：攻击力\n" +
                    "副词条：暴击率、暴击伤害",
                    MessageType.None
                );

                EditorGUILayout.PropertyField(
                    baseAttackPower,
                    new GUIContent(
                        "初始攻击力"
                    )
                );

                EditorGUILayout.PropertyField(
                    attackPowerPerLevel,
                    new GUIContent(
                        "每级攻击力成长"
                    )
                );

                InventoryEditorGUI.DrawPercent(
                    criticalRateBonus,
                    "暴击率加成"
                );

                InventoryEditorGUI.DrawPercent(
                    criticalDamageBonus,
                    "暴击伤害加成"
                );
            }
        );

        InventoryEditorGUI.DrawSection(
            "武器升级配置",
            () =>
            {
                EditorGUILayout.PropertyField(
                    weaponMaxLevel,
                    new GUIContent(
                        "武器最大等级"
                    )
                );

                EditorGUILayout.PropertyField(
                    levelOneExperienceRequirement,
                    new GUIContent(
                        "一级升级所需经验"
                    )
                );

                EditorGUILayout.PropertyField(
                    experienceIncreasePerLevel,
                    new GUIContent(
                        "每级经验需求增加量"
                    )
                );
            }
        );
    }

    private void ClearIrrelevantStats(
        EquipmentSlotType currentSlot
    )
    {
        switch (currentSlot)
        {
            case EquipmentSlotType.Helmet:

                defenseBonus.intValue = 0;
                elementResistanceBonus.floatValue = 0f;

                damageBonus.floatValue = 0f;
                elementDamageBonus.floatValue = 0f;

                baseAttackPower.intValue = 0;
                attackPowerPerLevel.intValue = 0;
                break;

            case EquipmentSlotType.Chest:

                maxHpBonus.intValue = 0;

                damageBonus.floatValue = 0f;
                elementDamageBonus.floatValue = 0f;

                baseAttackPower.intValue = 0;
                attackPowerPerLevel.intValue = 0;

                criticalRateBonus.floatValue = 0f;
                criticalDamageBonus.floatValue = 0f;
                break;

            case EquipmentSlotType.Shoes:

                maxHpBonus.intValue = 0;

                defenseBonus.intValue = 0;
                elementResistanceBonus.floatValue = 0f;

                baseAttackPower.intValue = 0;
                attackPowerPerLevel.intValue = 0;

                criticalRateBonus.floatValue = 0f;
                criticalDamageBonus.floatValue = 0f;
                break;

            case EquipmentSlotType.Weapon:

                maxHpBonus.intValue = 0;

                defenseBonus.intValue = 0;
                elementResistanceBonus.floatValue = 0f;

                damageBonus.floatValue = 0f;
                elementDamageBonus.floatValue = 0f;
                break;
        }

        EditorUtility.SetDirty(target);
    }
}

[CustomEditor(typeof(EquipmentSetData))]
[CanEditMultipleObjects]
public class EquipmentSetDataChineseEditor :
    Editor
{
    private SerializedProperty setId;
    private SerializedProperty displayName;
    private SerializedProperty description;
    private SerializedProperty element;
    private SerializedProperty elementDamageBonus;
    private SerializedProperty
        elementResistanceBonus;

    private void OnEnable()
    {
        setId =
            serializedObject.FindProperty(
                "setId"
            );

        displayName =
            serializedObject.FindProperty(
                "displayName"
            );

        description =
            serializedObject.FindProperty(
                "description"
            );

        element =
            serializedObject.FindProperty(
                "element"
            );

        elementDamageBonus =
            serializedObject.FindProperty(
                "elementDamageBonus"
            );

        elementResistanceBonus =
            serializedObject.FindProperty(
                "elementResistanceBonus"
            );
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        InventoryEditorGUI.DrawSection(
            "套装基础信息",
            () =>
            {
                EditorGUILayout.PropertyField(
                    setId,
                    new GUIContent("套装 ID")
                );

                EditorGUILayout.PropertyField(
                    displayName,
                    new GUIContent("套装名称")
                );

                EditorGUILayout.PropertyField(
                    description,
                    new GUIContent("套装说明")
                );
            }
        );

        InventoryEditorGUI.DrawSection(
            "四件套效果",
            () =>
            {
                InventoryEditorGUI
                    .DrawChineseEnum(
                        element,
                        "套装属性"
                    );

                InventoryEditorGUI.DrawPercent(
                    elementDamageBonus,
                    "属性伤害加成"
                );

                InventoryEditorGUI.DrawPercent(
                    elementResistanceBonus,
                    "属性抗性加成"
                );
            }
        );

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(ConsumableItemData))]
[CanEditMultipleObjects]
public class ConsumableItemDataChineseEditor :
    ItemDataChineseEditorBase
{
    private SerializedProperty effectType;
    private SerializedProperty amount;
    private SerializedProperty element;
    private SerializedProperty percentageValue;
    private SerializedProperty duration;

    protected override void OnEnable()
    {
        base.OnEnable();

        effectType =
            serializedObject.FindProperty(
                "effectType"
            );

        amount =
            serializedObject.FindProperty(
                "amount"
            );

        element =
            serializedObject.FindProperty(
                "element"
            );

        percentageValue =
            serializedObject.FindProperty(
                "percentageValue"
            );

        duration =
            serializedObject.FindProperty(
                "duration"
            );
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        InventoryEditorGUI.DrawSection(
            "道具基础信息",
            () =>
            {
                DrawBaseInformation(
                    false,
                    true
                );
            }
        );

        InventoryEditorGUI.DrawSection(
            "使用效果",
            () =>
            {
                InventoryEditorGUI
                    .DrawChineseEnum(
                        effectType,
                        "效果类型"
                    );

                ConsumableEffectType type =
                    (ConsumableEffectType)
                    effectType.enumValueIndex;

                switch (type)
                {
                    case ConsumableEffectType
                        .CharacterExperience:

                    case ConsumableEffectType
                        .WeaponExperience:

                    case ConsumableEffectType
                        .HealHp:

                    case ConsumableEffectType
                        .RestoreInk:

                        EditorGUILayout.PropertyField(
                            amount,
                            new GUIContent(
                                "增加数值"
                            )
                        );

                        break;

                    case ConsumableEffectType
                        .ElementDamageBuff:

                    case ConsumableEffectType
                        .ElementResistanceBuff:

                        InventoryEditorGUI
                            .DrawChineseEnum(
                                element,
                                "作用属性"
                            );

                        InventoryEditorGUI.DrawPercent(
                            percentageValue,
                            "加成比例"
                        );

                        EditorGUILayout.PropertyField(
                            duration,
                            new GUIContent(
                                "持续时间（秒）"
                            )
                        );

                        break;
                }
            }
        );

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(QuestItemData))]
[CanEditMultipleObjects]
public class QuestItemDataChineseEditor :
    ItemDataChineseEditorBase
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        InventoryEditorGUI.DrawSection(
            "任务道具配置",
            () =>
            {
                DrawBaseInformation(
                    false,
                    true
                );

                EditorGUILayout.HelpBox(
                    "任务道具无法使用。" +
                    "\n重要任务道具建议关闭“允许丢弃”。",
                    MessageType.Info
                );
            }
        );

        serializedObject.ApplyModifiedProperties();
    }
}

#endif