using UnityEngine;

public class WarningArea : MonoBehaviour
{
    [Header("Local Box (relative to prefab root)")]
    public Vector3 localCenter = Vector3.zero;
    public Vector3 localSize = new Vector3(2f, 2f, 6f); // X宽, Y厚, Z长（厚给大一点不影响观感）

    public void GetWorldBox(out Vector3 center, out Vector3 halfExtents, out Quaternion rotation)
    {
        rotation = transform.rotation;
        center = transform.TransformPoint(localCenter);

        Vector3 worldSize = Vector3.Scale(localSize, transform.lossyScale);
        halfExtents = worldSize * 0.5f;
    }
}
