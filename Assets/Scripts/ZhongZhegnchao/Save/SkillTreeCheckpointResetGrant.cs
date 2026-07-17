using UnityEngine;

[DisallowMultipleComponent]
public class SkillTreeCheckpointResetGrant : MonoBehaviour
{
    [SerializeField]
    private PlayerSkillTreeController
        skillTreeController;

    public void GrantResetOpportunity()
    {
        ResolveReference();

        if (skillTreeController == null)
        {
            Debug.LogWarning(
                "[SkillTreeCheckpointResetGrant] " +
                "PlayerSkillTreeController was not found.",
                this
            );

            return;
        }

        skillTreeController.GrantResetOpportunity();
    }

    private void ResolveReference()
    {
        if (skillTreeController != null)
            return;

        skillTreeController =
            FindAnyObjectByType<
                PlayerSkillTreeController>();
    }
}