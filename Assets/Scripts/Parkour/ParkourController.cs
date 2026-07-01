using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkourController : MonoBehaviour
{
    public BarrierChecker barrierChecker;
    public Animator anim;
    public List<NewActionSystem> newActions;

    [Header("Action Settings")]
    public float actionDuration = 1.0f;
    public float crossFadeTime = 0.2f;

    private bool playerInAction;
    private InputHandler inputHandler;

    private void Awake()
    {
        if (barrierChecker == null)
            barrierChecker = GetComponent<BarrierChecker>();

        if (anim == null)
            anim = GetComponent<Animator>();

        inputHandler = InputHandler.GetOrCreate();
    }

    void Update()
    {
        if (inputHandler != null && inputHandler.JumpPressed && !playerInAction)
        {
            if (barrierChecker == null)
            {
                Debug.LogWarning("BarrierChecker is missing.");
                return;
            }

            BarrierChecker.BarrierInfo hitData = barrierChecker.CheckBarrier();

            if (!hitData.hitFound)
                return;

            foreach (NewActionSystem action in newActions)
            {
                if (action == null)
                    continue;

                if (action.CheckBarrierHeight(hitData, transform))
                {
                    StartCoroutine(PerformTheAction(action));
                    break;
                }
            }
        }
    }

    IEnumerator PerformTheAction(NewActionSystem action)
    {
        playerInAction = true;

        if (PlayerController.instance != null)
            PlayerController.instance.SetControl(false);

        if (anim != null)
            anim.CrossFade(action.animationName, crossFadeTime);

        float timer = 0f;

        while (timer < actionDuration)
        {
            timer += Time.deltaTime;

            if (action.lookAtBarrier)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    action.RequiredRotation,
                    PlayerController.instance.rotSpeed * Time.deltaTime
                );
            }

            yield return null;
        }

        if (PlayerController.instance != null)
            PlayerController.instance.SetControl(true);

        playerInAction = false;
    }

    void CompareTarget(NewActionSystem action)
    {
        anim.MatchTarget(action.ComparePosition, transform.rotation, action.compareBodyPart, 
            new MatchTargetWeightMask(action.WeightMask,0), action.compareStartTime, action.compareEndTime);
    }
}
