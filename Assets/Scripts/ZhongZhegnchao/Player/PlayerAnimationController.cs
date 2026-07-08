using System.Collections;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private enum PlayerAnimState
    {
        None,
        Idle,
        RunStart,
        RunLoop,
        RunStop,
        Jump,
        Dodge,
        Action
    }

    [Header("Timing")]
    [SerializeField] private float runStartToLoopNormalizedTime = 0.75f;
    [SerializeField] private float runStopToIdleNormalizedTime = 0.85f;
    private Coroutine actionReturnCoroutine;

    private PlayerAnimancerDriver animancerDriver;

    private PlayerAnimState currentState = PlayerAnimState.None;

    private Coroutine locomotionCoroutine;

    private bool lastHasMoveInput;
    private bool isActionPlaying;

    public void Init(Animator targetAnimator)
    {
        animancerDriver = GetComponentInChildren<PlayerAnimancerDriver>();

        if (animancerDriver == null)
        {
            Debug.LogError("[PlayerAnimationController] PlayerAnimancerDriver not found.");
            return;
        }

        animancerDriver.Init(targetAnimator);

        currentState = PlayerAnimState.Idle;

        Debug.Log("[PlayerAnimationController] Init complete.");
    }

    public void SetLocomotion(float speed, bool hasMoveInput)
    {
        lastHasMoveInput = hasMoveInput;

        if (animancerDriver == null)
        {
            return;
        }

        if (isActionPlaying)
        {
            return;
        }

        if (hasMoveInput)
        {
            HandleMoveStartOrLoop();
        }
        else
        {
            HandleMoveStop();
        }
    }

    private void HandleMoveStartOrLoop()
    {
        if (currentState == PlayerAnimState.RunStart)
        {
            return;
        }

        if (currentState == PlayerAnimState.RunLoop)
        {
            return;
        }

        PlayRunStart();
    }

    private void HandleMoveStop()
    {
        if (currentState == PlayerAnimState.Idle)
        {
            return;
        }

        if (currentState == PlayerAnimState.RunStop)
        {
            return;
        }

        if (currentState == PlayerAnimState.RunStart ||
            currentState == PlayerAnimState.RunLoop)
        {
            PlayRunStop();
        }
    }

    private void PlayIdle()
    {
        StopLocomotionCoroutine();

        animancerDriver.PlayIdle();
        currentState = PlayerAnimState.Idle;
    }

    private void PlayRunStart()
    {
        StopLocomotionCoroutine();

        animancerDriver.PlayRunStart();
        currentState = PlayerAnimState.RunStart;

        float delay =
            animancerDriver.GetRunStartLength() *
            runStartToLoopNormalizedTime;

        if (delay <= 0f)
        {
            PlayRunLoop();
            return;
        }

        locomotionCoroutine = StartCoroutine(RunStartToLoopCoroutine(delay));
    }

    private IEnumerator RunStartToLoopCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        locomotionCoroutine = null;

        if (isActionPlaying)
        {
            yield break;
        }

        if (!lastHasMoveInput)
        {
            PlayRunStop();
            yield break;
        }

        if (currentState == PlayerAnimState.RunStart)
        {
            PlayRunLoop();
        }
    }

    private void PlayRunLoop()
    {
        StopLocomotionCoroutine();

        animancerDriver.PlayRunLoop();
        currentState = PlayerAnimState.RunLoop;
    }

    private void PlayRunStop()
    {
        StopLocomotionCoroutine();

        animancerDriver.PlayRunStop();
        currentState = PlayerAnimState.RunStop;

        float delay =
            animancerDriver.GetRunStopLength() *
            runStopToIdleNormalizedTime;

        if (delay <= 0f)
        {
            PlayIdle();
            return;
        }

        locomotionCoroutine = StartCoroutine(RunStopToIdleCoroutine(delay));
    }

    private IEnumerator RunStopToIdleCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        locomotionCoroutine = null;

        if (isActionPlaying)
        {
            yield break;
        }

        if (lastHasMoveInput)
        {
            PlayRunStart();
            yield break;
        }

        if (currentState == PlayerAnimState.RunStop)
        {
            PlayIdle();
        }
    }

    public void PlayJump()
    {
        if (animancerDriver == null)
        {
            return;
        }

        StopLocomotionCoroutine();

        isActionPlaying = true;
        currentState = PlayerAnimState.Jump;

        animancerDriver.PlayJump();
    }

    public void PlayDodge()
    {
        if (animancerDriver == null)
        {
            return;
        }

        StopLocomotionCoroutine();

        isActionPlaying = true;
        currentState = PlayerAnimState.Dodge;

        animancerDriver.PlayDodge();
    }

    public void ReturnToLocomotion(bool hasMoveInput)
    {
        if (animancerDriver == null)
        {
            return;
        }

        isActionPlaying = false;
        lastHasMoveInput = hasMoveInput;

        if (hasMoveInput)
        {
            PlayRunLoop();
        }
        else
        {
            PlayIdle();
        }
    }

    public void SetGrounded(bool isGrounded)
    {
        // 纯 Animancer 方案暂时不用 Animator Bool。
        // 保留接口，避免 PlayerLocomotion 调用时报错。
    }

    public bool CanStartCombatAction()
    {
        if (currentState == PlayerAnimState.Jump)
        {
            return false;
        }

        if (currentState == PlayerAnimState.Dodge)
        {
            return false;
        }

        return true;
    }

    private void StopLocomotionCoroutine()
    {
        if (locomotionCoroutine == null)
        {
            return;
        }

        StopCoroutine(locomotionCoroutine);
        locomotionCoroutine = null;
    }

    public void BeginAction()
    {
        if (animancerDriver == null)
        {
            return;
        }

        StopLocomotionCoroutine();
        StopActionReturnCoroutine();

        isActionPlaying = true;
        currentState = PlayerAnimState.Action;
    }

    public void EndAction(bool hasMoveInput)
    {
        ReturnToLocomotion(hasMoveInput);
    }

    public void EndAction(
    bool hasMoveInput,
    float delay,
    bool useRunStartWhenMoving
)
    {
        if (animancerDriver == null)
        {
            return;
        }

        StopActionReturnCoroutine();

        if (delay <= 0f)
        {
            DoReturnFromAction(hasMoveInput, useRunStartWhenMoving);
            return;
        }

        actionReturnCoroutine = StartCoroutine(
            ReturnFromActionCoroutine(hasMoveInput, delay, useRunStartWhenMoving)
        );
    }

    private IEnumerator ReturnFromActionCoroutine(
        bool hasMoveInput,
        float delay,
        bool useRunStartWhenMoving
    )
    {
        yield return new WaitForSeconds(delay);

        actionReturnCoroutine = null;

        DoReturnFromAction(hasMoveInput, useRunStartWhenMoving);
    }

    private void DoReturnFromAction(bool hasMoveInput, bool useRunStartWhenMoving)
    {
        isActionPlaying = false;
        lastHasMoveInput = hasMoveInput;

        if (hasMoveInput)
        {
            if (useRunStartWhenMoving)
            {
                PlayRunStart();
            }
            else
            {
                PlayRunLoop();
            }
        }
        else
        {
            PlayIdle();
        }
    }

    private void StopActionReturnCoroutine()
    {
        if (actionReturnCoroutine == null)
        {
            return;
        }

        StopCoroutine(actionReturnCoroutine);
        actionReturnCoroutine = null;
    }
}