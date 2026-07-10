public interface IBrushSkillUnlockReceiver
{
    bool HasBrushSkill(BrushSkillType skillType);

    bool UnlockBrushSkill(BrushSkillType skillType);
}