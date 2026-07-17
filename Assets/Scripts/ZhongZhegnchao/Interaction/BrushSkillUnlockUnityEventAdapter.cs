using UnityEngine;

public class BrushSkillUnlockUnityEventAdapter : MonoBehaviour
{
    [Header("Skill")]
    [SerializeField] private BrushSkillType skillType = BrushSkillType.Fire;

    [Header("Receiver")]
    [SerializeField] private PlayerBrushSkillInventory skillInventory;

    [Header("After Unlock")]
    [SerializeField] private bool disableObjectAfterUnlock = true;
    [SerializeField] private bool destroyObjectAfterUnlock = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private void Awake()
    {
        if (skillInventory == null)
        {
            skillInventory = FindAnyObjectByType<PlayerBrushSkillInventory>();
        }
    }

    public void UnlockSkill()
    {
        if (skillInventory == null)
        {
            Debug.LogWarning(
                "[BrushSkillUnlockUnityEventAdapter] PlayerBrushSkillInventory not found.",
                this
            );

            return;
        }

        bool success = skillInventory.UnlockBrushSkill(skillType);

        if (debugLog)
        {
            Debug.Log(
                $"[BrushSkillUnlockUnityEventAdapter] UnlockSkill called. Skill={skillType}, Success={success}",
                this
            );
        }

        if (!success)
            return;

        if (destroyObjectAfterUnlock)
        {
            Destroy(gameObject);
            return;
        }

        if (disableObjectAfterUnlock)
        {
            gameObject.SetActive(false);
        }
    }
}