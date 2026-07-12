public interface IBrushSkillCostReceiver
{
    bool HasEnoughInk(int cost);

    bool TryConsumeInk(int cost);
}