using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class TimelineCameraControlPoint : MonoBehaviour
{
    [SerializeField, Min(1f)] private float lensFieldOfView = 40f;
    [SerializeField, Min(0)] private int stopDurationFrames = 0;
    [SerializeField, Min(1)] private int travelFramesFromPrevious = 60;
    [SerializeField, Range(-1f, 1f)] private float bendX;
    [SerializeField, Range(-1f, 1f)] private float bendY;
    [SerializeField, Range(-1f, 1f)] private float bendZ;

    public float LensFieldOfView => lensFieldOfView;
    public int StopDurationFrames => stopDurationFrames;
    public int TravelFramesFromPrevious => travelFramesFromPrevious;
    public Vector3 Bend => new Vector3(bendX, bendY, bendZ);

    private void OnValidate()
    {
        lensFieldOfView = Mathf.Max(1f, lensFieldOfView);
        stopDurationFrames = Mathf.Max(0, stopDurationFrames);
        travelFramesFromPrevious = Mathf.Max(1, travelFramesFromPrevious);
        bendX = Mathf.Clamp(bendX, -1f, 1f);
        bendY = Mathf.Clamp(bendY, -1f, 1f);
        bendZ = Mathf.Clamp(bendZ, -1f, 1f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.1f, 0.75f, 1f, 1f);
        Gizmos.DrawSphere(transform.position, 0.16f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.7f);
    }
}
