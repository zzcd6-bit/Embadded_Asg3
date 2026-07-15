using UnityEngine;

[CreateAssetMenu(
    fileName = "Player_CharacterStatsConfig",
    menuName = "Game/Player/Character Stats Config"
)]
public class PlayerCharacterStatsConfig :
    ScriptableObject
{
    [Header("Level")]
    [Min(1)]
    public int maxLevel = 100;

    [Header("Level 1 Base Stats")]
    public CharacterBaseStats levelOneStats =
        new CharacterBaseStats();

    [Header("Growth Per Level")]
    [Tooltip(
        "暴击率和暴击伤害不会随等级成长。"
    )]
    public CharacterLevelGrowthStats growthPerLevel =
        new CharacterLevelGrowthStats();

    public CharacterBaseStats GetStatsAtLevel(
        int level
    )
    {
        int safeMaxLevel =
            Mathf.Max(1, maxLevel);

        int finalLevel =
            Mathf.Clamp(
                level,
                1,
                safeMaxLevel
            );

        int gainedLevels =
            finalLevel - 1;

        CharacterBaseStats result =
            levelOneStats != null
                ? levelOneStats.Clone()
                : new CharacterBaseStats();

        if (growthPerLevel != null)
        {
            result.maxHp +=
                growthPerLevel.maxHpPerLevel *
                gainedLevels;

            result.attackPower +=
                growthPerLevel.attackPowerPerLevel *
                gainedLevels;

            result.defense +=
                growthPerLevel.defensePerLevel *
                gainedLevels;

            result.damageBonus +=
                growthPerLevel.damageBonusPerLevel *
                gainedLevels;

            result.elementDamageBonus.AddScaled(
                growthPerLevel
                    .elementDamageBonusPerLevel,
                gainedLevels
            );

            result.elementResistance.AddScaled(
                growthPerLevel
                    .elementResistancePerLevel,
                gainedLevels
            );
        }

        result.maxHp =
            Mathf.Max(1, result.maxHp);

        result.attackPower =
            Mathf.Max(0, result.attackPower);

        result.defense =
            Mathf.Max(0, result.defense);

        result.critRate =
            Mathf.Clamp01(result.critRate);

        result.critDamage =
            Mathf.Max(1f, result.critDamage);

        return result;
    }
}