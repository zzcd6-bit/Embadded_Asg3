using UnityEngine;

[DisallowMultipleComponent]
public class WaterProjectileSceneInteractionRelay :
    MonoBehaviour
{
    private GameObject source;
    private Transform target;
    private float reachDistance;

    private bool hasTriggered;

    public void Init(
        GameObject newSource,
        Transform newTarget,
        float newReachDistance
    )
    {
        source = newSource;
        target = newTarget;

        reachDistance = Mathf.Max(
            0.01f,
            newReachDistance
        );

        hasTriggered = false;
    }

    public void Clear()
    {
        source = null;
        target = null;
        reachDistance = 0f;
        hasTriggered = false;
    }

    private void Update()
    {
        if (hasTriggered)
            return;

        if (target == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            target.position
        );

        if (distance > reachDistance)
            return;

        TryReact(target.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasTriggered)
            return;

        if (collision == null ||
            collision.gameObject == null)
        {
            return;
        }

        TryReact(collision.gameObject);
    }

    private void TryReact(GameObject hitObject)
    {
        if (hitObject == null)
            return;

        IBrushSceneElementReactable reactable =
            FindReactable(hitObject);

        if (reactable == null)
            return;

        if (!reactable.CanReactTo(
                BrushSkillType.Water
            ))
        {
            return;
        }

        GameObject reactionSource =
            source != null
                ? source
                : gameObject;

        if (reactable.TryReact(
                BrushSkillType.Water,
                reactionSource
            ))
        {
            hasTriggered = true;
        }
    }

    private IBrushSceneElementReactable FindReactable(
        GameObject targetObject
    )
    {
        IBrushSceneElementReactable reactable =
            targetObject
                .GetComponent<
                    IBrushSceneElementReactable>();

        if (reactable == null)
        {
            reactable = targetObject
                .GetComponentInParent<
                    IBrushSceneElementReactable>();
        }

        if (reactable == null)
        {
            reactable = targetObject
                .GetComponentInChildren<
                    IBrushSceneElementReactable>();
        }

        return reactable;
    }

    private void OnDisable()
    {
        Clear();
    }
}