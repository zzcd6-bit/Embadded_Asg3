public static class ElementReactionCalculator
{
    public static bool TryReactWithFire(
        ElementType incomingElement,
        out ElementReactionType reactionType,
        out float reactionMultiplier,
        out bool shouldClearFire
    )
    {
        reactionType = ElementReactionType.None;
        reactionMultiplier = 1f;
        shouldClearFire = false;

        switch (incomingElement)
        {
            case ElementType.Water:
                reactionType = ElementReactionType.Vaporize;
                reactionMultiplier = 1.5f;
                shouldClearFire = true;
                return true;

            case ElementType.Ice:
                reactionType = ElementReactionType.Melt;
                reactionMultiplier = 1.5f;
                shouldClearFire = true;
                return true;

            case ElementType.Thunder:
                reactionType = ElementReactionType.Overload;
                reactionMultiplier = 1.3f;
                shouldClearFire = true;
                return true;

            default:
                return false;
        }
    }
}