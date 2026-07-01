using UnityEngine;

public sealed class BoatRowingAnimationDriver : MonoBehaviour
{
    private enum TurnState
    {
        Straight,
        Left,
        Right
    }

    [Header("References")]
    [SerializeField] private BoatPathFollower boat;
    [SerializeField] private BoatCurvePath path;
    [SerializeField] private Animator rowerAnimator;

    [Header("Animator Parameters")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string leftTurnTrigger = "Left Turn Trigger";
    [SerializeField] private string rightTurnTrigger = "Right Turn Trigger";
    [SerializeField] private string turnEndTrigger = "Turn End Trigger";
    [SerializeField] private string startTrigger = "Start Trigger";
    [SerializeField] private string stopTrigger = "Stop Trigger";

    [Header("Speed")]
    [SerializeField, Min(0f)] private float animatorSpeedMultiplier = 1f;
    [SerializeField, Min(0f)] private float speedDampTime = 0.15f;

    [Header("Turn Detection")]
    [SerializeField, Min(1), Tooltip("Offset in cached sampled points used to build the local turn angle.")]
    private int anglePointOffset = 1;
    [SerializeField, Min(0), Tooltip("How many cached sampled points ahead to test before entering a turn. This is not a path node count.")]
    private int enterLookAheadPoints = 3;
    [SerializeField, Min(0), Tooltip("How many cached sampled points ahead to test before exiting a turn. This is not a path node count.")]
    private int exitLookAheadPoints = 1;
    [SerializeField, Min(0f)] private float targetTurnAngle = 12f;
    [SerializeField] private Vector3 turnAxis = Vector3.up;

    [Header("Gizmos")]
    [SerializeField, Min(0.25f)] private float gizmoSampleStep = 1f;
    [SerializeField] private float gizmoHeightOffset = 0.45f;
    [SerializeField] private Color straightPathColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
    [SerializeField] private Color leftTurnPathColor = new Color(0.25f, 0.55f, 1f, 1f);
    [SerializeField] private Color rightTurnPathColor = new Color(1f, 0.55f, 0.15f, 1f);

    private int speedHash;
    private int leftTurnHash;
    private int rightTurnHash;
    private int turnEndHash;
    private int startHash;
    private int stopHash;
    private bool wasMoving;
    private bool hasObservedMovingState;
    private TurnState turnState = TurnState.Straight;

    private void Awake()
    {
        ResolveReferences();
        CacheAnimatorHashes();
    }

    private void OnValidate()
    {
        animatorSpeedMultiplier = Mathf.Max(0f, animatorSpeedMultiplier);
        anglePointOffset = Mathf.Max(1, anglePointOffset);
        enterLookAheadPoints = Mathf.Max(0, enterLookAheadPoints);
        exitLookAheadPoints = Mathf.Max(0, exitLookAheadPoints);
        targetTurnAngle = Mathf.Max(0f, targetTurnAngle);
        gizmoSampleStep = Mathf.Max(0.25f, gizmoSampleStep);
    }

    private void Update()
    {
        ResolveReferences();

        if (rowerAnimator == null)
        {
            return;
        }

        bool isMoving = boat != null && boat.IsMoving;
        UpdateMovingTriggers(isMoving);
        UpdateSpeed(isMoving);

        if (!isMoving)
        {
            EndTurnIfNeeded();
            return;
        }

        UpdateTurn();
    }

    [ContextMenu("Start Rowing")]
    public void StartRowing()
    {
        ResolveReferences();
        CacheAnimatorHashes();

        if (boat != null)
        {
            boat.StartMove();
        }

        hasObservedMovingState = true;
        wasMoving = true;
        turnState = TurnState.Straight;
        ResetTrigger(stopHash);
        ResetTrigger(leftTurnHash);
        ResetTrigger(rightTurnHash);
        ResetTrigger(turnEndHash);
        SendTrigger(startHash, startTrigger);
    }

    [ContextMenu("Restart Rowing")]
    public void RestartRowing()
    {
        ResolveReferences();
        CacheAnimatorHashes();

        if (boat != null)
        {
            boat.RestartMove();
        }

        hasObservedMovingState = true;
        wasMoving = true;
        turnState = TurnState.Straight;
        ResetTrigger(stopHash);
        ResetTrigger(leftTurnHash);
        ResetTrigger(rightTurnHash);
        ResetTrigger(turnEndHash);
        SendTrigger(startHash, startTrigger);
    }

    [ContextMenu("Stop Rowing")]
    public void StopRowing()
    {
        ResolveReferences();
        CacheAnimatorHashes();

        if (boat != null)
        {
            boat.StopMove();
        }

        hasObservedMovingState = true;
        wasMoving = false;
        SendTrigger(stopHash, stopTrigger);
        EndTurnIfNeeded();
    }

    [ContextMenu("Timeline Play Boat Animation")]
    public void TimelinePlayBoatAnimation()
    {
        StartRowing();
    }

    public void TimelineSyncBoatMoveStarted()
    {
        ResolveReferences();
        CacheAnimatorHashes();

        hasObservedMovingState = true;
        wasMoving = true;
        turnState = TurnState.Straight;
        ResetTrigger(stopHash);
        ResetTrigger(leftTurnHash);
        ResetTrigger(rightTurnHash);
        ResetTrigger(turnEndHash);
        SendTrigger(startHash, startTrigger);
    }

    [ContextMenu("Timeline Restart Boat Animation")]
    public void TimelineRestartBoatAnimation()
    {
        RestartRowing();
    }

    [ContextMenu("Timeline Stop Boat Animation")]
    public void TimelineStopBoatAnimation()
    {
        StopRowing();
    }

    private void ResolveReferences()
    {
        if (rowerAnimator == null)
        {
            rowerAnimator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        }

        if (boat == null)
        {
            boat = GetComponentInParent<BoatPathFollower>();
        }

        if (path == null && boat != null)
        {
            path = boat.Path;
        }
    }

    private void CacheAnimatorHashes()
    {
        speedHash = Hash(speedParameter);
        leftTurnHash = Hash(leftTurnTrigger);
        rightTurnHash = Hash(rightTurnTrigger);
        turnEndHash = Hash(turnEndTrigger);
        startHash = Hash(startTrigger);
        stopHash = Hash(stopTrigger);
    }

    private void UpdateMovingTriggers(bool isMoving)
    {
        if (!hasObservedMovingState)
        {
            hasObservedMovingState = true;
            wasMoving = isMoving;

            if (isMoving)
            {
                SendTrigger(startHash, startTrigger);
            }

            return;
        }

        if (isMoving == wasMoving)
        {
            return;
        }

        wasMoving = isMoving;
        SendTrigger(isMoving ? startHash : stopHash, isMoving ? startTrigger : stopTrigger);
    }

    private void UpdateSpeed(bool isMoving)
    {
        if (speedHash == 0)
        {
            return;
        }

        float normalizedSpeed = 0f;
        if (isMoving && boat != null && boat.MaxSpeed > 0.0001f)
        {
            normalizedSpeed = Mathf.Clamp01(boat.Speed / boat.MaxSpeed);
        }

        float targetSpeed = Mathf.Clamp01(normalizedSpeed * animatorSpeedMultiplier);
        rowerAnimator.SetFloat(speedHash, targetSpeed, speedDampTime, Time.deltaTime);
    }

    private void UpdateTurn()
    {
        BoatCurvePath activePath = GetActivePath();
        if (activePath == null || boat == null)
        {
            return;
        }

        if (turnState == TurnState.Straight)
        {
            float signedAngle = activePath.GetSignedSampleAngleAtPointOffset(
                boat.Distance,
                enterLookAheadPoints,
                anglePointOffset,
                turnAxis);

            float absoluteAngle = Mathf.Abs(signedAngle);
            if (absoluteAngle > targetTurnAngle)
            {
                BeginTurn(signedAngle < 0f ? TurnState.Left : TurnState.Right);
            }

            return;
        }

        float exitAngle = activePath.GetSignedSampleAngleAtPointOffset(
            boat.Distance,
            exitLookAheadPoints,
            anglePointOffset,
            turnAxis);

        float absoluteExitAngle = Mathf.Abs(exitAngle);
        if (absoluteExitAngle < targetTurnAngle)
        {
            EndTurnIfNeeded();
        }
    }

    private void BeginTurn(TurnState newTurnState)
    {
        turnState = newTurnState;

        if (newTurnState == TurnState.Left)
        {
            ResetTrigger(rightTurnHash);
            ResetTrigger(turnEndHash);
            SendTrigger(leftTurnHash, leftTurnTrigger);
        }
        else if (newTurnState == TurnState.Right)
        {
            ResetTrigger(leftTurnHash);
            ResetTrigger(turnEndHash);
            SendTrigger(rightTurnHash, rightTurnTrigger);
        }
    }

    private void EndTurnIfNeeded()
    {
        if (turnState == TurnState.Straight)
        {
            return;
        }

        turnState = TurnState.Straight;
        ResetTrigger(leftTurnHash);
        ResetTrigger(rightTurnHash);
        SendTrigger(turnEndHash, turnEndTrigger);
    }

    private BoatCurvePath GetActivePath()
    {
        if (path != null)
        {
            return path;
        }

        return boat != null ? boat.Path : null;
    }

    private void SendTrigger(int triggerHash, string triggerName)
    {
        if (triggerHash == 0 || rowerAnimator == null || string.IsNullOrEmpty(triggerName))
        {
            return;
        }

        rowerAnimator.SetTrigger(triggerHash);
    }

    private void ResetTrigger(int triggerHash)
    {
        if (triggerHash != 0 && rowerAnimator != null)
        {
            rowerAnimator.ResetTrigger(triggerHash);
        }
    }

    private static int Hash(string parameterName)
    {
        return string.IsNullOrEmpty(parameterName) ? 0 : Animator.StringToHash(parameterName);
    }

    private void OnDrawGizmos()
    {
        BoatCurvePath activePath = GetActivePath();
        if (activePath == null)
        {
            ResolveReferences();
            activePath = GetActivePath();
        }

        if (activePath == null)
        {
            return;
        }

        if (!Application.isPlaying)
        {
            activePath.RebuildCurve();
        }

        float totalLength = activePath.TotalLength;
        if (totalLength <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 up = Vector3.up * gizmoHeightOffset;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(totalLength / gizmoSampleStep));
        Vector3 previousPoint = activePath.GetPositionAtDistance(0f) + up;
        Color previousColor = GetGizmoColor(activePath, 0f);

        for (int i = 1; i <= sampleCount; i++)
        {
            float distance = totalLength * i / sampleCount;
            Vector3 point = activePath.GetPositionAtDistance(distance) + up;
            Color color = GetGizmoColor(activePath, distance);

            Gizmos.color = color == previousColor ? color : Color.Lerp(previousColor, color, 0.5f);
            Gizmos.DrawLine(previousPoint, point);

            previousPoint = point;
            previousColor = color;
        }
    }

    private Color GetGizmoColor(BoatCurvePath activePath, float distance)
    {
        float signedAngle = activePath.GetSignedSampleAngleAtPointOffset(
            distance,
            enterLookAheadPoints,
            anglePointOffset,
            turnAxis);

        if (Mathf.Abs(signedAngle) <= targetTurnAngle)
        {
            return straightPathColor;
        }

        return signedAngle < 0f ? leftTurnPathColor : rightTurnPathColor;
    }
}
