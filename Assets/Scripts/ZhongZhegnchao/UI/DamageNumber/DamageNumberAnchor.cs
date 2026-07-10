using UnityEngine;

public class DamageNumberAnchor : MonoBehaviour
{
    [Header("伤害数字生成点")]
    public Transform anchorPoint;

    [Header("没有 Anchor Point 时使用的偏移")]
    public Vector3 fallbackOffset = new Vector3(0f, 1.6f, 0f);

    public Vector3 GetWorldPosition()
    {
        if (anchorPoint != null)
        {
            return anchorPoint.position;
        }

        return transform.position + fallbackOffset;
    }
}