using UnityEngine;

public class BossAttackSMB : StateMachineBehaviour
{
    [Range(0f, 1f)] public float windowOpenTime = 0.25f;
    [Range(0f, 1f)] public float hitTime = 0.35f;
    [Range(0f, 1f)] public float windowCloseTime = 0.45f;

    bool opened, hitDone, closed;
    BossController boss;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        opened = hitDone = closed = false;

        boss = animator.GetComponent<BossController>();
        if (boss == null) boss = animator.GetComponentInParent<BossController>();

        boss?.GetComponent<BossSound>()?.PlayAttack();
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (boss == null) return;

        float t = stateInfo.normalizedTime % 1f;

        if (!opened && t >= windowOpenTime) { opened = true; boss.Anim_AttackWindowOpen(); }
        if (!hitDone && t >= hitTime)       { hitDone = true; boss.Anim_AttackHit(); }
        if (!closed && t >= windowCloseTime){ closed = true; boss.Anim_AttackWindowClose(); }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (boss != null) boss.Anim_AttackWindowClose();

        // ✅ 关键：攻击结束复位 + 进入冷却（否则只能攻击一次）
        boss.Anim_AttackEnd();
    }
}
