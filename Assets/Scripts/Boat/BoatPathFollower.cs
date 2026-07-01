using UnityEngine;
using UnityEngine.Serialization;

public sealed class BoatPathFollower : MonoBehaviour
{
    private enum TurnSpeedState
    {
        Straight,
        Turning
    }

    [SerializeField] private BoatCurvePath path;
    [SerializeField, FormerlySerializedAs("speed"), Min(0f)] private float maxSpeed = 2f;
    [SerializeField, Min(0f)] private float cornerSpeed = 0.8f;
    [SerializeField, Min(0.01f)] private float acceleration = 1.5f;
    [SerializeField, Min(0f)] private float targetTurnAngle = 12f;
    [SerializeField, Min(1), Tooltip("Offset in cached sampled points used to build the local turn angle.")]
    private int anglePointOffset = 1;
    [SerializeField, Min(0), Tooltip("How many cached sampled points ahead to test before entering a turn. This is not a path node count.")]
    private int enterLookAheadPoints = 3;
    [SerializeField, Min(0), Tooltip("How many cached sampled points ahead to test before exiting a turn. This is not a path node count.")]
    private int exitLookAheadPoints = 1;
    [SerializeField, Min(0f)] private float endSlowdownDistance = 4f;
    [SerializeField] private bool playOnStart;
    [SerializeField] private bool loop;
    [SerializeField] private bool startAtNearestPoint = true;
    [SerializeField] private bool alignToVelocity = true;
    [SerializeField] private Vector3 boatForwardAxis = Vector3.forward;
    [SerializeField] private Vector3 worldUp = Vector3.up;
    [SerializeField, Min(0f)] private float rotationLerpSpeed = 8f;
    [Header("Water Surface")]
    [SerializeField] private LayerMask waterLayerMask = ~0;
    [SerializeField, Min(0f)] private float waterRaycastHeight = 3f;
    [SerializeField, Min(0.01f)] private float waterRaycastDistance = 8f;
    [SerializeField] private float draftYOffset = 0f;
    [SerializeField, Min(0f)] private float waveAmplitude = 0.08f;
    [SerializeField, Min(0f)] private float waveFrequency = 1f;
    [SerializeField, Range(0f, 45f)] private float maxWaterRollAngle = 20f;
    [SerializeField, Tooltip("X axis rotation amplitude in degrees. Formula: A * sin(K * t).")]
    private float xAxisRotationA = 0f;
    [SerializeField, Tooltip("X axis rotation frequency factor. Formula: A * sin(K * t).")]
    private float xAxisRotationK = 1f;
    [SerializeField, Min(0f)] private float rollSmoothSpeed = 8f;
    [SerializeField, Min(0f)] private float waterHeightSmoothSpeed = 8f;

    private float distance;
    private float currentSpeed;
    private float currentTargetSpeed;
    private float endSlowdownStartDistance;
    private float endSlowdownStartSpeed;
    private bool moving;
    private bool slowingForEnd;
    private bool hasSmoothedWaterY;
    private float smoothedWaterY;
    private float currentWaterRollAngle;
    private TurnSpeedState turnSpeedState = TurnSpeedState.Straight;

    public bool IsMoving => moving;
    public float Distance => distance;
    public float Speed
    {
        get => currentSpeed;
        set => currentSpeed = Mathf.Max(0f, value);
    }

    public float MaxSpeed
    {
        get => maxSpeed;
        set => maxSpeed = Mathf.Max(0f, value);
    }

    public float CornerSpeed
    {
        get => cornerSpeed;
        set => cornerSpeed = Mathf.Max(0f, value);
    }

    public float Acceleration
    {
        get => acceleration;
        set => acceleration = Mathf.Max(0.01f, value);
    }

    public BoatCurvePath Path => path;

    private void OnValidate()
    {
        maxSpeed = Mathf.Max(0f, maxSpeed);
        cornerSpeed = Mathf.Max(0f, cornerSpeed);
        acceleration = Mathf.Max(0.01f, acceleration);
        targetTurnAngle = Mathf.Max(0f, targetTurnAngle);
        anglePointOffset = Mathf.Max(1, anglePointOffset);
        enterLookAheadPoints = Mathf.Max(0, enterLookAheadPoints);
        exitLookAheadPoints = Mathf.Max(0, exitLookAheadPoints);
        endSlowdownDistance = Mathf.Max(0f, endSlowdownDistance);
        waterRaycastHeight = Mathf.Max(0f, waterRaycastHeight);
        waterRaycastDistance = Mathf.Max(0.01f, waterRaycastDistance);
        waveAmplitude = Mathf.Max(0f, waveAmplitude);
        waveFrequency = Mathf.Max(0f, waveFrequency);
        maxWaterRollAngle = Mathf.Clamp(maxWaterRollAngle, 0f, 45f);
        xAxisRotationK = Mathf.Max(0f, xAxisRotationK);
        rollSmoothSpeed = Mathf.Max(0f, rollSmoothSpeed);
        waterHeightSmoothSpeed = Mathf.Max(0f, waterHeightSmoothSpeed);
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartMove();
        }
    }

    private void Update()
    {
        if (!moving || path == null)
        {
            return;
        }

        float totalLength = path.TotalLength;
        if (totalLength <= Mathf.Epsilon)
        {
            path.RebuildCurve();
            totalLength = path.TotalLength;
        }

        if (totalLength <= Mathf.Epsilon)
        {
            return;
        }

        UpdateSpeedTargetForTurn();
        currentSpeed = GetNextSpeed(totalLength, Time.deltaTime);
        distance += currentSpeed * Time.deltaTime;

        if (loop)
        {
            distance %= totalLength;
        }
        else if (distance >= totalLength)
        {
            distance = totalLength;
            moving = false;
            currentSpeed = 0f;
        }

        ApplyPathPose();
    }

    [ContextMenu("Start Move")]
    public void StartMove()
    {
        if (path == null)
        {
            return;
        }

        path.RebuildCurve();

        if (startAtNearestPoint)
        {
            distance = path.GetNearestDistance(transform.position);
        }

        moving = true;
        currentSpeed = 0f;
        currentTargetSpeed = maxSpeed;
        slowingForEnd = false;
        hasSmoothedWaterY = false;
        turnSpeedState = TurnSpeedState.Straight;
        ApplyPathPose();
    }

    [ContextMenu("Timeline Play Boat Move")]
    public void TimelinePlayBoatMove()
    {
        StartMove();
        NotifyTimelineMoveStarted();
    }

    [ContextMenu("Restart Move")]
    public void RestartMove()
    {
        distance = 0f;
        moving = true;
        currentSpeed = 0f;
        currentTargetSpeed = maxSpeed;
        slowingForEnd = false;
        hasSmoothedWaterY = false;
        turnSpeedState = TurnSpeedState.Straight;
        ApplyPathPose();
    }

    [ContextMenu("Timeline Restart Boat Move")]
    public void TimelineRestartBoatMove()
    {
        RestartMove();
    }

    [ContextMenu("Stop Move")]
    public void StopMove()
    {
        moving = false;
        currentSpeed = 0f;
        slowingForEnd = false;
    }

    [ContextMenu("Timeline Stop Boat Move")]
    public void TimelineStopBoatMove()
    {
        StopMove();
    }

    public void SetPath(BoatCurvePath newPath)
    {
        path = newPath;
        distance = 0f;
    }

    public void SetDistance(float newDistance)
    {
        distance = Mathf.Max(0f, newDistance);
        ApplyPathPose();
    }

    private float GetNextSpeed(float totalLength, float deltaTime)
    {
        if (TryGetEndSlowdownSpeed(totalLength, out float endSpeed))
        {
            return endSpeed;
        }

        return Mathf.MoveTowards(currentSpeed, currentTargetSpeed, acceleration * deltaTime);
    }

    private void UpdateSpeedTargetForTurn()
    {
        if (path == null)
        {
            return;
        }

        if (turnSpeedState == TurnSpeedState.Straight)
        {
            float angle = Mathf.Abs(path.GetSignedSampleAngleAtPointOffset(
                distance,
                enterLookAheadPoints,
                anglePointOffset,
                Vector3.up));

            if (angle > targetTurnAngle)
            {
                turnSpeedState = TurnSpeedState.Turning;
                currentTargetSpeed = Mathf.Min(cornerSpeed, maxSpeed);
            }

            return;
        }

        float exitAngle = Mathf.Abs(path.GetSignedSampleAngleAtPointOffset(
            distance,
            exitLookAheadPoints,
            anglePointOffset,
            Vector3.up));

        if (exitAngle < targetTurnAngle)
        {
            turnSpeedState = TurnSpeedState.Straight;
            currentTargetSpeed = maxSpeed;
        }
    }

    private bool TryGetEndSlowdownSpeed(float totalLength, out float endSpeed)
    {
        endSpeed = currentSpeed;

        if (loop || endSlowdownDistance <= Mathf.Epsilon)
        {
            slowingForEnd = false;
            return false;
        }

        float remainingDistance = Mathf.Max(0f, totalLength - distance);
        if (remainingDistance > endSlowdownDistance)
        {
            slowingForEnd = false;
            return false;
        }

        if (!slowingForEnd)
        {
            slowingForEnd = true;
            endSlowdownStartDistance = Mathf.Max(0.001f, remainingDistance);
            endSlowdownStartSpeed = currentSpeed;
        }

        float t = Mathf.Clamp01(remainingDistance / endSlowdownStartDistance);
        endSpeed = Mathf.Lerp(0f, endSlowdownStartSpeed, t);
        return true;
    }

    private void ApplyPathPose()
    {
        if (path == null)
        {
            return;
        }

        Vector3 targetPosition = path.GetPositionAtDistance(distance);
        Quaternion targetRotation = transform.rotation;

        if (alignToVelocity)
        {
            Vector3 direction = path.GetVelocityDirectionAtDistance(distance);
            if (direction.sqrMagnitude > 0.000001f)
            {
                Vector3 up = worldUp.sqrMagnitude <= 0.000001f ? Vector3.up : worldUp.normalized;
                Quaternion pathRotation = Quaternion.LookRotation(direction.normalized, up) *
                    Quaternion.FromToRotation(NormalizeAxis(boatForwardAxis), Vector3.forward);

                targetRotation = rotationLerpSpeed <= 0f
                    ? pathRotation
                    : Quaternion.Slerp(transform.rotation, pathRotation, 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime));
            }
        }

        ApplyWaterPose(ref targetPosition, ref targetRotation);
        transform.SetPositionAndRotation(targetPosition, targetRotation);
    }

    private void ApplyWaterPose(ref Vector3 targetPosition, ref Quaternion targetRotation)
    {
        float deltaTime = Application.isPlaying ? Time.deltaTime : 1f;
        float bobOffset = waveAmplitude <= 0f || waveFrequency <= 0f
            ? 0f
            : Mathf.Sin(Time.time * Mathf.PI * 2f * waveFrequency) * waveAmplitude;

        bool hasWaterHit = TryGetWaterSurface(targetPosition, out RaycastHit waterHit);
        float targetWaterY = hasWaterHit ? waterHit.point.y : targetPosition.y;
        if (!hasSmoothedWaterY)
        {
            smoothedWaterY = targetWaterY;
            hasSmoothedWaterY = true;
        }
        else
        {
            float heightT = waterHeightSmoothSpeed <= 0f
                ? 1f
                : 1f - Mathf.Exp(-waterHeightSmoothSpeed * deltaTime);
            smoothedWaterY = Mathf.Lerp(smoothedWaterY, targetWaterY, heightT);
        }

        targetPosition.y = smoothedWaterY + draftYOffset + bobOffset;

        float targetRollAngle = hasWaterHit ? GetZRollAngleForWaterNormal(targetRotation, waterHit.normal) : 0f;
        float rollT = rollSmoothSpeed <= 0f
            ? 1f
            : 1f - Mathf.Exp(-rollSmoothSpeed * deltaTime);
        currentWaterRollAngle = Mathf.Lerp(currentWaterRollAngle, targetRollAngle, rollT);
        float xAxisAngle = xAxisRotationA * Mathf.Sin(xAxisRotationK * Time.time);
        targetRotation *= Quaternion.Euler(xAxisAngle, 0f, currentWaterRollAngle);
    }

    private bool TryGetWaterSurface(Vector3 basePosition, out RaycastHit hit)
    {
        Vector3 rayOrigin = basePosition + Vector3.up * waterRaycastHeight;
        float rayDistance = waterRaycastHeight + waterRaycastDistance;
        return Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out hit,
            rayDistance,
            waterLayerMask,
            QueryTriggerInteraction.Ignore);
    }

    private float GetZRollAngleForWaterNormal(Quaternion baseRotation, Vector3 waterNormal)
    {
        Vector3 forward = baseRotation * Vector3.forward;
        Vector3 baseUp = ProjectDirection(baseRotation * Vector3.up, forward);
        Vector3 desiredUp = ProjectDirection(waterNormal, forward);
        if (baseUp.sqrMagnitude <= 0.000001f || desiredUp.sqrMagnitude <= 0.000001f)
        {
            return 0f;
        }

        float roll = Vector3.SignedAngle(baseUp, desiredUp, forward);
        return Mathf.Clamp(roll, -maxWaterRollAngle, maxWaterRollAngle);
    }

    private static Vector3 NormalizeAxis(Vector3 axis)
    {
        return axis.sqrMagnitude <= 0.000001f ? Vector3.forward : axis.normalized;
    }

    private static Vector3 ProjectDirection(Vector3 direction, Vector3 normal)
    {
        Vector3 projected = Vector3.ProjectOnPlane(direction, normal);
        return projected.sqrMagnitude <= 0.000001f ? Vector3.zero : projected.normalized;
    }

    private void NotifyTimelineMoveStarted()
    {
        BoatRowingAnimationDriver[] animationDrivers = GetComponentsInChildren<BoatRowingAnimationDriver>(true);
        for (int i = 0; i < animationDrivers.Length; i++)
        {
            if (animationDrivers[i] != null)
            {
                animationDrivers[i].TimelineSyncBoatMoveStarted();
            }
        }
    }
}
