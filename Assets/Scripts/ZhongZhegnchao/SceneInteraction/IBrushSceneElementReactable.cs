using UnityEngine;

public interface IBrushSceneElementReactable
{
    Transform ReactionTarget { get; }

    bool CanReactTo(BrushSkillType skillType);

    bool TryReact(
        BrushSkillType skillType,
        GameObject source
    );
}