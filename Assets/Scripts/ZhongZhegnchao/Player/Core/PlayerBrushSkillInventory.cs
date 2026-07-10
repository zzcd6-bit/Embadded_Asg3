using System.Collections.Generic;
using UnityEngine;

public class PlayerBrushSkillInventory : MonoBehaviour, IBrushSkillUnlockReceiver
{
    [Header("初始拥有的画符技能")]
    public List<BrushSkillType> defaultUnlockedSkills =
        new List<BrushSkillType>();

    [Header("当前已获得的画符技能")]
    [SerializeField]
    private List<BrushSkillType> unlockedSkills =
        new List<BrushSkillType>();

    [Header("Debug")]
    public bool debugLog = true;

    private void Awake()
    {
        ResetToDefaultSkills();
    }

    public bool HasBrushSkill(BrushSkillType skillType)
    {
        if (skillType == BrushSkillType.None)
            return false;

        return unlockedSkills.Contains(skillType);
    }

    public bool UnlockBrushSkill(BrushSkillType skillType)
    {
        if (skillType == BrushSkillType.None)
            return false;

        if (unlockedSkills.Contains(skillType))
        {
            if (debugLog)
            {
                Debug.Log(
                    $"[PlayerBrushSkillInventory] Skill already unlocked: {skillType}",
                    this
                );
            }

            return false;
        }

        unlockedSkills.Add(skillType);

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerBrushSkillInventory] Skill unlocked: {skillType}",
                this
            );
        }

        return true;
    }

    public void ResetToDefaultSkills()
    {
        unlockedSkills.Clear();

        for (int i = 0; i < defaultUnlockedSkills.Count; i++)
        {
            BrushSkillType skillType = defaultUnlockedSkills[i];

            if (skillType == BrushSkillType.None)
                continue;

            if (!unlockedSkills.Contains(skillType))
            {
                unlockedSkills.Add(skillType);
            }
        }
    }

    public List<BrushSkillType> GetUnlockedSkills()
    {
        return new List<BrushSkillType>(unlockedSkills);
    }

    public void SetUnlockedSkills(List<BrushSkillType> skills)
    {
        unlockedSkills.Clear();

        if (skills == null)
            return;

        for (int i = 0; i < skills.Count; i++)
        {
            BrushSkillType skillType = skills[i];

            if (skillType == BrushSkillType.None)
                continue;

            if (!unlockedSkills.Contains(skillType))
            {
                unlockedSkills.Add(skillType);
            }
        }
    }
}