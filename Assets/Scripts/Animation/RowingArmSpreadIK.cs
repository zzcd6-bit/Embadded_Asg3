using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class RowingArmSpreadIK : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)]
    private float weight = 1f;

    [SerializeField, Min(0f)]
    private float spreadDistance = 0.12f;

    [SerializeField]
    private Vector3 leftHandLocalOffset;

    [SerializeField]
    private Vector3 rightHandLocalOffset;

    [SerializeField]
    private bool keepCurrentHandRotation = true;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman || weight <= 0f)
        {
            return;
        }

        ApplyHandOffset(AvatarIKGoal.LeftHand, HumanBodyBones.LeftHand, -1f, leftHandLocalOffset);
        ApplyHandOffset(AvatarIKGoal.RightHand, HumanBodyBones.RightHand, 1f, rightHandLocalOffset);
    }

    private void ApplyHandOffset(AvatarIKGoal goal, HumanBodyBones handBone, float side, Vector3 localOffset)
    {
        Transform hand = animator.GetBoneTransform(handBone);
        if (hand == null)
        {
            return;
        }

        Vector3 targetPosition = hand.position
            + transform.right * spreadDistance * side
            + transform.TransformVector(localOffset);

        animator.SetIKPositionWeight(goal, weight);
        animator.SetIKPosition(goal, targetPosition);

        if (keepCurrentHandRotation)
        {
            animator.SetIKRotationWeight(goal, weight);
            animator.SetIKRotation(goal, hand.rotation);
        }
        else
        {
            animator.SetIKRotationWeight(goal, 0f);
        }
    }
}
