using UnityEngine;

public class WarningSector : MonoBehaviour
{
    [Header("Local Sector (relative to this transform)")]
    public Vector3 localCenter = Vector3.zero;

    [Tooltip("Radius in local space. Will be scaled by max(X,Z) lossyScale.")]
    public float localRadius = 6f;

    [Tooltip("Full angle in degrees (e.g. 120 means 60 left + 60 right).")]
    [Range(1f, 360f)] public float angleDeg = 120f;

    [Tooltip("Vertical thickness (world). Will be scaled by lossyScale.y.")]
    public float localHeight = 2.0f;

    public void GetWorldSector(out Vector3 center, out Vector3 forward, out float radius, out float angle, out float height)
    {
        center = transform.TransformPoint(localCenter);
        forward = transform.forward;

        float s = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        radius = localRadius * s;

        angle = angleDeg;

        height = localHeight * Mathf.Abs(transform.lossyScale.y);
    }
}
