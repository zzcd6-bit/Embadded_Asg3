using UnityEngine;

public class BarrierChecker : MonoBehaviour
{
    public Vector3 rayOffset = new Vector3(0f, 0.2f, 0f);
    public float rayLength = 0.9f;
    public LayerMask BarrierLayer;
    public float heightRayLength = 6f;

    public struct BarrierInfo
    {
        public bool hitFound;
        public RaycastHit hitInfo;

        public bool heightHitFound;
        public RaycastHit heightInfo;
    }

    public BarrierInfo CheckBarrier()
    {
        BarrierInfo hitData = new BarrierInfo();

        Vector3 rayOrigin = transform.position + rayOffset;

        hitData.hitFound = Physics.Raycast(
            rayOrigin,
            transform.forward,
            out hitData.hitInfo,
            rayLength,
            BarrierLayer
        );

        Debug.DrawRay(
            rayOrigin,
            transform.forward * rayLength,
            hitData.hitFound ? Color.red : Color.green
        );

        if (hitData.hitFound)
        {
            Vector3 heightOrigin = hitData.hitInfo.point + Vector3.up * heightRayLength;

            hitData.heightHitFound = Physics.Raycast(
                heightOrigin,
                Vector3.down,
                out hitData.heightInfo,
                heightRayLength,
                BarrierLayer
            );

            Debug.DrawRay(
                heightOrigin,
                Vector3.down * heightRayLength,
                hitData.heightHitFound ? Color.red : Color.green
            );
        }

        return hitData;
    }
}