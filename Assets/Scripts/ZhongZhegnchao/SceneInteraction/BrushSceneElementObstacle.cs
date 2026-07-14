using System.Collections;
using DG.Tweening;
using UnityEngine;

public enum BrushSceneElementType
{
    None,
    Wood,
    Fire
}

[DisallowMultipleComponent]
public class BrushSceneElementObstacle :
    MonoBehaviour,
    IBrushSceneElementReactable
{
    [Header("Element")]
    [SerializeField]
    private BrushSceneElementType elementType =
        BrushSceneElementType.None;

    [Header("Target")]
    [Tooltip("水弹索敌点。建议放在障碍物 Collider 内部。")]
    [SerializeField]
    private Transform reactionTarget;

    [Header("Disappear")]
    [Tooltip("真正缩小消失的模型节点。建议拖 VisualRoot。")]
    [SerializeField]
    private Transform disappearTarget;

    [SerializeField]
    private float disappearDuration = 2f;

    [SerializeField]
    private Ease disappearEase = Ease.InQuad;

    [Header("Reaction VFX")]
    [Tooltip("反应时激活的 VFX。Wood 可放 Fire VFX。")]
    [SerializeField]
    private GameObject reactionVfxObject;

    [Header("Collision")]
    [SerializeField]
    private bool disableCollidersOnReact = true;

    [Header("Finish")]
    [SerializeField]
    private bool deactivateRootOnComplete = true;

    [Header("Debug")]
    [SerializeField]
    private bool debugLog = true;

    private bool hasReacted;
    private Tween disappearTween;

    public Transform ReactionTarget
    {
        get
        {
            return reactionTarget != null
                ? reactionTarget
                : transform;
        }
    }

    private void Awake()
    {
        if (disappearTarget == null)
        {
            disappearTarget = transform;
        }

        if (reactionVfxObject != null)
        {
            reactionVfxObject.SetActive(false);
        }
    }

    public bool CanReactTo(BrushSkillType skillType)
    {
        if (hasReacted)
            return false;

        switch (elementType)
        {
            case BrushSceneElementType.Wood:
                return skillType == BrushSkillType.Fire;

            case BrushSceneElementType.Fire:
                return skillType == BrushSkillType.Water;
        }

        return false;
    }

    public bool TryReact(
        BrushSkillType skillType,
        GameObject source
    )
    {
        if (!CanReactTo(skillType))
            return false;

        hasReacted = true;

        // Water 水弹可能还在处理自己的 Collision。
        // 延迟一帧再关闭 Collider，避免干扰水弹命中流程。
        if (skillType == BrushSkillType.Water)
        {
            StartCoroutine(
                BeginReactionNextFrame()
            );
        }
        else
        {
            BeginReaction();
        }

        if (debugLog)
        {
            Debug.Log(
                $"[BrushSceneElementObstacle] " +
                $"{elementType} reacted to {skillType}.",
                this
            );
        }

        return true;
    }

    private IEnumerator BeginReactionNextFrame()
    {
        yield return null;

        BeginReaction();
    }

    private void BeginReaction()
    {
        if (disableCollidersOnReact)
        {
            DisableAllColliders();
        }

        if (reactionVfxObject != null)
        {
            reactionVfxObject.SetActive(true);
        }

        StartDisappear();
    }

    private void DisableAllColliders()
    {
        Collider[] colliders =
            GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];

            if (col != null)
            {
                col.enabled = false;
            }
        }
    }

    private void StartDisappear()
    {
        if (disappearTarget == null)
            return;

        if (disappearTween != null &&
            disappearTween.IsActive())
        {
            disappearTween.Kill();
        }

        disappearTween = disappearTarget
            .DOScale(
                Vector3.zero,
                Mathf.Max(0.01f, disappearDuration)
            )
            .SetEase(disappearEase)
            .OnComplete(OnDisappearComplete);
    }

    private void OnDisappearComplete()
    {
        disappearTween = null;

        if (deactivateRootOnComplete)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (disappearTween != null &&
            disappearTween.IsActive())
        {
            disappearTween.Kill();
        }

        disappearTween = null;
    }
}