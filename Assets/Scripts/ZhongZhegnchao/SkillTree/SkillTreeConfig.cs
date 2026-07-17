using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkillTreeGrowthMode
{
    Linear,
    SoftCap
}

public enum SkillTreeValueFormat
{
    Number,
    Integer,
    Percent,
    Multiplier,
    Seconds
}

public enum SkillTreeStatType
{
    DamageMultiplier,
    RangeMultiplier,
    CooldownMultiplier,
    BurnDamageMultiplier,
    FormDamageMultiplier,
    PullRadiusMultiplier,
    PullForceMultiplier,
    InfusionDamageMultiplier,
    ProjectileSpeedMultiplier,
    HealPercent,
    ShieldPercent,
    ShieldDurationSeconds
}

[Serializable]
public class SkillTreeStatConfig
{
    [SerializeField] private SkillTreeStatType statType;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField, Min(1)] private int unlockLevel = 1;
    [SerializeField]
    private SkillTreeGrowthMode growthMode =
        SkillTreeGrowthMode.Linear;
    [SerializeField] private float baseValue = 1f;
    [SerializeField] private float valuePerLevel = 0.1f;
    [SerializeField] private float maximumBonus = 0.5f;
    [SerializeField, Min(0.01f)] private float softCapCurve = 8f;
    [SerializeField]
    private SkillTreeValueFormat valueFormat =
        SkillTreeValueFormat.Percent;
    [SerializeField, Range(0, 3)] private int decimalPlaces = 1;
    [SerializeField] private bool showInUI = true;

    public SkillTreeStatType StatType => statType;
    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName)
            ? statType.ToString()
            : displayName;
    public Sprite Icon => icon;
    public int UnlockLevel => Mathf.Max(1, unlockLevel);
    public bool ShowInUI => showInUI;

    public SkillTreeStatConfig()
    {
    }

    public SkillTreeStatConfig(
        SkillTreeStatType newStatType,
        string newDisplayName,
        SkillTreeGrowthMode newGrowthMode,
        float newBaseValue,
        float newValuePerLevel,
        float newMaximumBonus,
        float newSoftCapCurve,
        SkillTreeValueFormat newValueFormat,
        int newUnlockLevel = 1
    )
    {
        statType = newStatType;
        displayName = newDisplayName;
        growthMode = newGrowthMode;
        baseValue = newBaseValue;
        valuePerLevel = newValuePerLevel;
        maximumBonus = newMaximumBonus;
        softCapCurve = Mathf.Max(0.01f, newSoftCapCurve);
        valueFormat = newValueFormat;
        unlockLevel = Mathf.Max(1, newUnlockLevel);
    }

    public float Evaluate(int skillLevel)
    {
        if (skillLevel < UnlockLevel)
            return 0f;

        int growthLevels =
            Mathf.Max(0, skillLevel - UnlockLevel);

        if (growthMode == SkillTreeGrowthMode.SoftCap)
        {
            if (growthLevels <= 0)
                return baseValue;

            float ratio =
                growthLevels /
                (growthLevels + Mathf.Max(0.01f, softCapCurve));

            return baseValue + maximumBonus * ratio;
        }

        return baseValue + valuePerLevel * growthLevels;
    }

    public string FormatValue(int skillLevel)
    {
        if (skillLevel < UnlockLevel)
            return $"Lv.{UnlockLevel} Unlock";

        return FormatRawValue(Evaluate(skillLevel));
    }

    public string FormatUpgradeValue(int currentLevel)
    {
        int nextLevel = Mathf.Max(1, currentLevel + 1);

        if (currentLevel < UnlockLevel &&
            nextLevel >= UnlockLevel)
        {
            return "Unlocked";
        }

        string nextValue = FormatValue(nextLevel);

        if (currentLevel < UnlockLevel)
            return nextValue;

        float difference =
            Evaluate(nextLevel) - Evaluate(currentLevel);

        string differenceText =
            FormatSignedValue(difference);

        return string.IsNullOrEmpty(differenceText)
            ? nextValue
            : $"{nextValue} ({differenceText})";
    }

    private string FormatRawValue(float value)
    {
        switch (valueFormat)
        {
            case SkillTreeValueFormat.Integer:
                return Mathf.RoundToInt(value).ToString();

            case SkillTreeValueFormat.Percent:
                return $"{value * 100f:F1}%";

            case SkillTreeValueFormat.Multiplier:
                return $"x{value:F2}";

            case SkillTreeValueFormat.Seconds:
                return $"{value:F1}s";

            default:
                return value.ToString($"F{decimalPlaces}");
        }
    }

    private string FormatSignedValue(float value)
    {
        if (Mathf.Approximately(value, 0f))
            return string.Empty;

        string prefix = value > 0f ? "+" : string.Empty;

        switch (valueFormat)
        {
            case SkillTreeValueFormat.Integer:
                return $"{prefix}{Mathf.RoundToInt(value)}";

            case SkillTreeValueFormat.Percent:
                return $"{prefix}{value * 100f:F1}%";

            case SkillTreeValueFormat.Multiplier:
                return $"{prefix}{value:F2}";

            case SkillTreeValueFormat.Seconds:
                return $"{prefix}{value:F1}s";

            default:
                return $"{prefix}{value.ToString($"F{decimalPlaces}")}";
        }
    }
}

[Serializable]
public class SkillTreeSkillConfig
{
    [SerializeField] private BrushSkillType skillType;
    [SerializeField] private string displayName;
    [SerializeField, TextArea(2, 5)] private string introduction;
    [SerializeField] private Sprite icon;
    [SerializeField]
    private List<SkillTreeStatConfig> stats =
        new List<SkillTreeStatConfig>();

    public BrushSkillType SkillType => skillType;
    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName)
            ? skillType.ToString()
            : displayName;
    public string Introduction => introduction;
    public Sprite Icon => icon;
    public IReadOnlyList<SkillTreeStatConfig> Stats => stats;

    public SkillTreeSkillConfig()
    {
    }

    public SkillTreeSkillConfig(
        BrushSkillType newSkillType,
        string newDisplayName,
        string newIntroduction,
        params SkillTreeStatConfig[] newStats
    )
    {
        skillType = newSkillType;
        displayName = newDisplayName;
        introduction = newIntroduction;
        stats = new List<SkillTreeStatConfig>(newStats);
    }

    public SkillTreeStatConfig GetStat(
        SkillTreeStatType statType
    )
    {
        for (int i = 0; i < stats.Count; i++)
        {
            SkillTreeStatConfig stat = stats[i];

            if (stat != null &&
                stat.StatType == statType)
            {
                return stat;
            }
        }

        return null;
    }
}

[CreateAssetMenu(
    fileName = "SkillTreeConfig",
    menuName = "Game/Skill Tree/Skill Tree Config"
)]
public class SkillTreeConfig : ScriptableObject
{
    [SerializeField]
    private List<SkillTreeSkillConfig> skills =
        new List<SkillTreeSkillConfig>();

    public IReadOnlyList<SkillTreeSkillConfig> Skills => skills;

    public SkillTreeSkillConfig GetSkillConfig(
        BrushSkillType skillType
    )
    {
        for (int i = 0; i < skills.Count; i++)
        {
            SkillTreeSkillConfig skill = skills[i];

            if (skill != null &&
                skill.SkillType == skillType)
            {
                return skill;
            }
        }

        return null;
    }

    public float GetStatValue(
        BrushSkillType skillType,
        SkillTreeStatType statType,
        int skillLevel,
        float fallbackValue = 0f
    )
    {
        SkillTreeSkillConfig skill =
            GetSkillConfig(skillType);

        if (skill == null)
            return fallbackValue;

        SkillTreeStatConfig stat =
            skill.GetStat(statType);

        return stat != null
            ? stat.Evaluate(skillLevel)
            : fallbackValue;
    }

    [ContextMenu("Generate Default Five Skill Data")]
    private void GenerateDefaultFiveSkillData()
    {
        skills = new List<SkillTreeSkillConfig>
        {
            Make(
                BrushSkillType.Slash,
                "Slash",
                "A reliable brush slash that deals direct damage.",
                Stat(
                    SkillTreeStatType.DamageMultiplier,
                    "Damage",
                    SkillTreeGrowthMode.Linear,
                    1f, 0.10f, 0f, 1f
                ),
                Stat(
                    SkillTreeStatType.RangeMultiplier,
                    "Range",
                    SkillTreeGrowthMode.SoftCap,
                    1f, 0f, 0.35f, 6f
                ),
                Stat(
                    SkillTreeStatType.CooldownMultiplier,
                    "Cooldown",
                    SkillTreeGrowthMode.SoftCap,
                    1f, 0f, -0.35f, 8f
                )
            ),

            Make(
                BrushSkillType.Fire,
                "Fire",
                "Burn enemies and strengthen the alternate fire form.",
                Stat(
                    SkillTreeStatType.BurnDamageMultiplier,
                    "Burn Damage",
                    SkillTreeGrowthMode.Linear,
                    1f, 0.08f, 0f, 1f
                ),
                Stat(
                    SkillTreeStatType.FormDamageMultiplier,
                    "Form Damage",
                    SkillTreeGrowthMode.Linear,
                    1f, 0.06f, 0f, 1f
                ),
                Stat(
                    SkillTreeStatType.CooldownMultiplier,
                    "Cooldown",
                    SkillTreeGrowthMode.SoftCap,
                    1f, 0f, -0.40f, 8f
                )
            ),

            Make(
                BrushSkillType.Wind,
                "Wind",
                "Pull enemies together and absorb another element.",
                Stat(
                    SkillTreeStatType.PullRadiusMultiplier,
                    "Pull Radius",
                    SkillTreeGrowthMode.SoftCap,
                    1f, 0f, 0.50f, 7f
                ),
                Stat(
                    SkillTreeStatType.PullForceMultiplier,
                    "Pull Force",
                    SkillTreeGrowthMode.SoftCap,
                    1f, 0f, 1f, 8f
                ),
                Stat(
                    SkillTreeStatType.InfusionDamageMultiplier,
                    "Infusion Damage",
                    SkillTreeGrowthMode.Linear,
                    1f, 0.08f, 0f, 1f
                )
            ),

            Make(
                BrushSkillType.Water,
                "Water",
                "Launch a water projectile for safe ranged damage.",
                Stat(
                    SkillTreeStatType.DamageMultiplier,
                    "Damage",
                    SkillTreeGrowthMode.Linear,
                    1f, 0.10f, 0f, 1f
                ),
                Stat(
                    SkillTreeStatType.ProjectileSpeedMultiplier,
                    "Projectile Speed",
                    SkillTreeGrowthMode.SoftCap,
                    1f, 0f, 0.60f, 7f
                ),
                Stat(
                    SkillTreeStatType.CooldownMultiplier,
                    "Cooldown",
                    SkillTreeGrowthMode.SoftCap,
                    1f, 0f, -0.40f, 8f
                )
            ),

            new SkillTreeSkillConfig(
                BrushSkillType.Wood,
                "Wood",
                "Restore health. A protective shield is unlocked at level 5.",
                Stat(
                    SkillTreeStatType.HealPercent,
                    "Heal",
                    SkillTreeGrowthMode.SoftCap,
                    0.20f, 0f, 0.55f, 12f
                ),
                new SkillTreeStatConfig(
                    SkillTreeStatType.ShieldPercent,
                    "Shield",
                    SkillTreeGrowthMode.SoftCap,
                    0.15f,
                    0f,
                    0.45f,
                    10f,
                    SkillTreeValueFormat.Percent,
                    5
                ),
                Stat(
                    SkillTreeStatType.CooldownMultiplier,
                    "Cooldown",
                    SkillTreeGrowthMode.SoftCap,
                    1f, 0f, -0.35f, 8f
                )
            )
        };

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private SkillTreeSkillConfig Make(
        BrushSkillType type,
        string title,
        string intro,
        params SkillTreeStatConfig[] stats
    )
    {
        return new SkillTreeSkillConfig(
            type,
            title,
            intro,
            stats
        );
    }

    private SkillTreeStatConfig Stat(
        SkillTreeStatType type,
        string title,
        SkillTreeGrowthMode mode,
        float baseValue,
        float perLevel,
        float maxBonus,
        float curve
    )
    {
        return new SkillTreeStatConfig(
            type,
            title,
            mode,
            baseValue,
            perLevel,
            maxBonus,
            curve,
            SkillTreeValueFormat.Percent
        );
    }
}