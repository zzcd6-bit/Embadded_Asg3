using System.Collections.Generic;
using UnityEngine;

public class SlashBrushSkill : BrushSkillBase
{
    [Header("Camera Mapping")]
    public Camera worldCamera;

    [Tooltip("拖 BrushLineCamera 或 DisplayCamera。它负责把屏幕点转成 Viewport。")]
    public Camera strokeViewCamera;

    [Header("Slash Detection")]
    public LayerMask targetLayer;
    public float rayDistance = 200f;
    public float sphereRadius = 1.5f;
    public int sampleCount = 12;

    [Header("Damage")]
    public int damage = 1;

    [Header("Debug")]
    public bool drawDebugRay = true;

    protected override void Execute(BrushGestureResult result)
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null)
        {
            Debug.LogWarning("SlashBrushSkill: WorldCamera is missing.");
            return;
        }

        List<Vector2> screenPoints = result.strokeData.screenPoints;

        if (screenPoints == null || screenPoints.Count < 2)
            return;

        HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

        for (int i = 0; i < sampleCount; i++)
        {
            float t = sampleCount <= 1 ? 0.5f : i / (float)(sampleCount - 1);
            int index = Mathf.RoundToInt(t * (screenPoints.Count - 1));

            Vector2 screenPoint = screenPoints[index];

            Ray ray = GetWorldRayFromStrokePoint(screenPoint);

            if (drawDebugRay)
                Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red, 1.5f);

            RaycastHit[] hits = Physics.SphereCastAll(
                ray,
                sphereRadius,
                rayDistance,
                targetLayer,
                QueryTriggerInteraction.Collide
            );

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                    continue;

                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

                if (damageable != null)
                    hitTargets.Add(damageable);
            }
        }

        foreach (IDamageable target in hitTargets)
        {
            target.TakeDamage(damage);
        }

        Debug.Log($"Slash executed. Damage target count: {hitTargets.Count}");
    }

    private Ray GetWorldRayFromStrokePoint(Vector2 screenPoint)
    {
        Vector3 viewportPoint;

        if (strokeViewCamera != null)
        {
            viewportPoint = strokeViewCamera.ScreenToViewportPoint(
                new Vector3(screenPoint.x, screenPoint.y, 0f)
            );
        }
        else
        {
            viewportPoint = new Vector3(
                screenPoint.x / Screen.width,
                screenPoint.y / Screen.height,
                0f
            );
        }

        viewportPoint.x = Mathf.Clamp01(viewportPoint.x);
        viewportPoint.y = Mathf.Clamp01(viewportPoint.y);
        viewportPoint.z = 0f;

        return worldCamera.ViewportPointToRay(viewportPoint);
    }
}