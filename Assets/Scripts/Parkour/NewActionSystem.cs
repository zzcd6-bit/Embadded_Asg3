using UnityEngine;

[CreateAssetMenu(menuName = "Parkour Menu/Create New Parkour Action")]
public class NewActionSystem : ScriptableObject
{
    public string animationName;

    [Header("Height Check")]
    [SerializeField] float minHeight;
    [SerializeField] float maxHeight;

    [Header("Rotation")]
    public bool lookAtBarrier;

    [HideInInspector]
    public Quaternion RequiredRotation { get; private set; }

    [Header("TargetMatching")]
    public bool allowTargetMatching = true;
    public AvatarTarget compareBodyPart;
    public float compareStartTime;
    public float compareEndTime;
    public Vector3 ComparePosition {get; set;}
    public Vector3 WeightMask;

    public bool CheckBarrierHeight(BarrierChecker.BarrierInfo hitData, Transform player)
    {
        if (!hitData.hitFound)
            return false;

        if (!hitData.heightHitFound)
        {
            Debug.Log("Height ray did not hit anything.");
            return false;
        }

        float checkHeight = hitData.heightInfo.point.y - player.position.y;

        Debug.Log("Check the height = " + checkHeight);

        bool heightMatched = checkHeight >= minHeight && checkHeight <= maxHeight;

        if (!heightMatched)
            return false;

        if (lookAtBarrier)
        {
            Vector3 faceDirection = -hitData.hitInfo.normal;
            faceDirection.y = 0f;

            if (faceDirection.sqrMagnitude > 0.001f)
                RequiredRotation = Quaternion.LookRotation(faceDirection);

            if (allowTargetMatching)
                ComparePosition = hitData.heightInfo.point;
        }

        return true;
    }
}