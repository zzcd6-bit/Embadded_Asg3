using System.Collections.Generic;
using UnityEngine;

public class BrushCastContextBuilder : MonoBehaviour
{
    [Header("Camera Mapping")]
    public Camera worldCamera;
    public Camera strokeViewCamera;

    [Header("Raycast")]
    public float rayDistance = 200f;
    public LayerMask groundLayer;
    public LayerMask targetLayer;
    public float targetSphereRadius = 1.2f;

    [Header("Debug")]
    public bool drawDebugRay = true;

    public BrushCastContext Build(BrushGestureResult result, GameObject caster)
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (result == null || result.strokeData == null)
            return null;

        List<Vector2> points = result.strokeData.screenPoints;

        if (points == null || points.Count == 0)
            return null;

        BrushCastContext context = new BrushCastContext();

        context.gestureResult = result;
        context.skillType = result.skillType;
        context.caster = caster;

        context.startScreenPoint = points[0];
        context.centerScreenPoint = GetAverageScreenPoint(points);
        context.endScreenPoint = points[points.Count - 1];

        context.startRay = GetWorldRayFromStrokePoint(context.startScreenPoint);
        context.centerRay = GetWorldRayFromStrokePoint(context.centerScreenPoint);
        context.endRay = GetWorldRayFromStrokePoint(context.endScreenPoint);

        context.castDirection = context.centerRay.direction;

        TryFindGroundPoint(context);
        TryFindTarget(context);

        return context;
    }

    private Vector2 GetAverageScreenPoint(List<Vector2> points)
    {
        Vector2 total = Vector2.zero;

        for (int i = 0; i < points.Count; i++)
        {
            total += points[i];
        }

        return total / points.Count;
    }

    public Ray GetWorldRayFromStrokePoint(Vector2 screenPoint)
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null)
            return new Ray(Vector3.zero, Vector3.forward);

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

    private void TryFindGroundPoint(BrushCastContext context)
    {
        if (context == null)
            return;

        if (drawDebugRay)
        {
            Debug.DrawRay(
                context.centerRay.origin,
                context.centerRay.direction * rayDistance,
                Color.green,
                1.5f
            );
        }

        if (Physics.Raycast(
            context.centerRay,
            out RaycastHit hit,
            rayDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        ))
        {
            context.hasGroundPoint = true;
            context.groundPoint = hit.point;
            context.groundNormal = hit.normal;
        }
    }

    private void TryFindTarget(BrushCastContext context)
    {
        if (context == null)
            return;

        if (drawDebugRay)
        {
            Debug.DrawRay(
                context.centerRay.origin,
                context.centerRay.direction * rayDistance,
                Color.yellow,
                1.5f
            );
        }

        RaycastHit[] hits = Physics.SphereCastAll(
            context.centerRay,
            targetSphereRadius,
            rayDistance,
            targetLayer,
            QueryTriggerInteraction.Collide
        );

        float closestDistance = float.MaxValue;
        GameObject closestTarget = null;
        Vector3 closestPoint = Vector3.zero;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.collider == null)
                continue;

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            Component damageComponent = damageable as Component;

            GameObject targetObject = damageComponent != null
                ? damageComponent.gameObject
                : hit.collider.gameObject;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestTarget = targetObject;
                closestPoint = hit.point;
            }
        }

        if (closestTarget != null)
        {
            context.hasTarget = true;
            context.targetObject = closestTarget;
            context.targetPoint = closestPoint;
        }
    }
}