using UnityEngine;

public class BrushCastContext
{
    public BrushGestureResult gestureResult;
    public BrushSkillType skillType;

    public GameObject caster;

    public Vector2 startScreenPoint;
    public Vector2 centerScreenPoint;
    public Vector2 endScreenPoint;

    public Ray startRay;
    public Ray centerRay;
    public Ray endRay;

    public bool hasGroundPoint;
    public Vector3 groundPoint;
    public Vector3 groundNormal;

    public bool hasTarget;
    public GameObject targetObject;
    public Vector3 targetPoint;

    public Vector3 castDirection;
}