using Animancer;
using UnityEngine;

public class PlayerAnimancerDriver : MonoBehaviour
{
    [Header("Animancer")]
    [SerializeField] private AnimancerComponent animancer;

    [Header("Locomotion")]
    [SerializeField] private ClipTransition idle;
    [SerializeField] private ClipTransition runStart;
    [SerializeField] private ClipTransition runLoop;
    [SerializeField] private ClipTransition runStop;

    [Header("Actions")]
    [SerializeField] private ClipTransition jump;

    public void Init(Animator targetAnimator)
    {
        if (targetAnimator != null)
        {
            targetAnimator.applyRootMotion = false;
        }

        if (animancer == null)
        {
            animancer = GetComponentInChildren<AnimancerComponent>();
        }

        if (animancer == null)
        {
            Debug.LogError("[PlayerAnimancerDriver] AnimancerComponent not found.");
            return;
        }

        if (animancer.Animator != null)
        {
            animancer.Animator.applyRootMotion = false;
        }

        PlayIdle();

        Debug.Log("[PlayerAnimancerDriver] Init complete.");
    }

    public void PlayIdle()
    {
        PlayTransition(idle, "Idle");
    }

    public void PlayRunStart()
    {
        PlayTransition(runStart, "RunStart");
    }

    public void PlayRunLoop()
    {
        PlayTransition(runLoop, "RunLoop");
    }

    public void PlayRunStop()
    {
        PlayTransition(runStop, "RunStop");
    }

    public void PlayJump()
    {
        PlayTransition(jump, "Jump");
    }

    public float GetRunStartLength()
    {
        return GetTransitionLength(runStart);
    }

    public float GetRunStopLength()
    {
        return GetTransitionLength(runStop);
    }

    public float GetJumpLength()
    {
        return GetTransitionLength(jump);
    }

    private void PlayTransition(ClipTransition transition, string stateName)
    {
        if (animancer == null)
        {
            Debug.LogError("[PlayerAnimancerDriver] Animancer is null. Cannot play: " + stateName);
            return;
        }

        if (transition == null)
        {
            Debug.LogWarning("[PlayerAnimancerDriver] Transition is null: " + stateName);
            return;
        }

        if (transition.Clip == null)
        {
            Debug.LogWarning("[PlayerAnimancerDriver] Transition clip is null: " + stateName);
            return;
        }

        animancer.Play(transition);
    }

    private float GetTransitionLength(ClipTransition transition)
    {
        if (transition == null || transition.Clip == null)
        {
            return 0f;
        }

        float speed = transition.Speed;

        if (Mathf.Approximately(speed, 0f))
        {
            speed = 1f;
        }

        return transition.Clip.length / Mathf.Abs(speed);
    }
}