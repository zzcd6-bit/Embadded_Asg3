using System;
using System.Collections.Generic;

[Serializable]
public class SkillTreeLevelSaveData
{
    public string skillType;
    public int level;

    public SkillTreeLevelSaveData()
    {
    }

    public SkillTreeLevelSaveData(
        BrushSkillType newSkillType,
        int newLevel
    )
    {
        skillType = newSkillType.ToString();
        level = newLevel;
    }
}

[Serializable]
public class PlayerSkillTreeSaveData
{
    public bool hasData;
    public bool canResetSkills;

    public List<SkillTreeLevelSaveData> levels =
        new List<SkillTreeLevelSaveData>();
}