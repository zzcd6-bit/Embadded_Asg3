using UnityEngine;

public class AttackSMB : StateMachineBehaviour
{
    [Range(0f, 1f)] public float windowOpenTime = 0.25f;
    [Range(0f, 1f)] public float hitTime = 0.35f;
    [Range(0f, 1f)] public float windowCloseTime = 0.45f;

    bool opened, hitDone, closed;
    EnemyBehavior beh;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        opened = hitDone = closed = false;

        beh = animator.GetComponent<EnemyBehavior>();
        if (beh == null) beh = animator.GetComponentInParent<EnemyBehavior>();
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (beh == null) return;

        float t = stateInfo.normalizedTime % 1f;

        if (!opened && t >= windowOpenTime) { opened = true; beh.Anim_AttackWindowOpen(); }
        if (!hitDone && t >= hitTime)       { hitDone = true; beh.Anim_AttackHit(); }
        if (!closed && t >= windowCloseTime){ closed = true; beh.Anim_AttackWindowClose(); }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (beh != null) beh.Anim_AttackWindowClose();
    }
}
