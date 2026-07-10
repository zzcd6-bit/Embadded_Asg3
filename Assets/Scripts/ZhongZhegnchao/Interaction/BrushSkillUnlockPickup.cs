using UnityEngine;

public class BrushSkillUnlockPickup : MonoBehaviour
{
    [Header("要解锁的技能")]
    public BrushSkillType skillType = BrushSkillType.Fire;

    [Header("触发设置")]
    public bool destroyAfterUnlock = true;
    public bool requirePlayerTag = false;
    public string playerTag = "Player";

    [Header("Debug")]
    public bool debugLog = true;

    private bool used;

    private void OnTriggerEnter(Collider other)
    {
        if (used)
            return;

        if (requirePlayerTag && !other.CompareTag(playerTag))
            return;

        IBrushSkillUnlockReceiver receiver = FindReceiver(other.gameObject);

        if (receiver == null)
            return;

        bool unlocked = receiver.UnlockBrushSkill(skillType);

        if (!unlocked)
            return;

        used = true;

        if (debugLog)
        {
            Debug.Log(
                $"[BrushSkillUnlockPickup] Player unlocked skill: {skillType}",
                this
            );
        }

        if (destroyAfterUnlock)
        {
            Destroy(gameObject);
        }
    }

    private IBrushSkillUnlockReceiver FindReceiver(GameObject target)
    {
        if (target == null)
            return null;

        MonoBehaviour[] parentBehaviours =
            target.GetComponentsInParent<MonoBehaviour>(true);

        for (int i = 0; i < parentBehaviours.Length; i++)
        {
            if (parentBehaviours[i] is IBrushSkillUnlockReceiver receiver)
            {
                return receiver;
            }
        }

        MonoBehaviour[] childBehaviours =
            target.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < childBehaviours.Length; i++)
        {
            if (childBehaviours[i] is IBrushSkillUnlockReceiver receiver)
            {
                return receiver;
            }
        }

        return null;
    }
}