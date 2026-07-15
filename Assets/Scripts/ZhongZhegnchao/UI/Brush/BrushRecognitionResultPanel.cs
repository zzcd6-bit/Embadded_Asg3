using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class BrushRecognitionResultPanel : BasePanel
{
    [Header("UI")]
    [SerializeField]
    private RectTransform panelRoot;

    [SerializeField]
    private RectTransform background;

    [SerializeField]
    private TMP_Text resultText;

    [SerializeField]
    private TMP_Text skillText;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [Header("Success VFX")]
    [SerializeField]
    private Image[] inkSplashes;

    [SerializeField]
    private Image glowRing;

    [SerializeField]
    private Image sweepLight;

    [SerializeField]
    private float inkBurstMinDistance = 60f;

    [SerializeField]
    private float inkBurstMaxDistance = 150f;

    [SerializeField]
    private float sweepDistance = 400f;

    [Header("Display")]
    [SerializeField]
    private float displayDuration = 1.5f;

    private Sequence animationSequence;

    private Vector2 panelRootOriginalPosition;
    private Vector2 skillTextOriginalPosition;

    private Vector2 sweepLightOriginalPosition;

    protected override void Awake()
    {
        base.Awake();

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        CacheOriginalState();
    }

    private void CacheOriginalState()
    {
        if (panelRoot != null)
        {
            panelRootOriginalPosition =
                panelRoot.anchoredPosition;
        }

        if (skillText != null)
        {
            skillTextOriginalPosition =
                skillText.rectTransform.anchoredPosition;
        }

        if (sweepLight != null)
        {
            sweepLightOriginalPosition =
                sweepLight.rectTransform.anchoredPosition;
        }
    }

    public override void ShowMe()
    {
        gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    public override void HideMe()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        gameObject.SetActive(false);
    }

    public void ShowSuccess(
        BrushSkillType skillType
    )
    {
        ShowMe();

        if (resultText != null)
        {
            resultText.text =
                "RECOGNITION SUCCESS";
        }

        if (skillText != null)
        {
            skillText.text =
                $"SKILL RELEASED: " +
                $"{GetSkillDisplayName(skillType)}";
        }

        PlaySuccessAnimation();
    }

    public void ShowFailure()
    {
        ShowMe();

        if (resultText != null)
        {
            resultText.text =
                "RECOGNITION FAILED";
        }

        if (skillText != null)
        {
            skillText.text =
                "DRAW THE SYMBOL AGAIN";
        }

        PlayFailureAnimation();
    }

    private void PlaySuccessAnimation()
    {
        KillCurrentAnimation();
        ResetAnimationState();

        animationSequence = DOTween.Sequence();

        animationSequence.SetUpdate(true);

        // 先让整个 Panel 出现
        animationSequence
            .Append(
                canvasGroup
                    .DOFade(1f, 0.06f)
            );

        // 背景同时展开
        animationSequence
            .Join(
                background
                    .DOScaleX(1f, 0.18f)
                    .SetEase(Ease.OutExpo)
            );

        // VFX 使用绝对时间 Insert
        // 现在 Canvas 已经正在出现
        AddSuccessVFX(animationSequence);

        // Recognition Success 砸入
        animationSequence
            .Append(
                resultText.rectTransform
                    .DOScale(1f, 0.16f)
                    .SetEase(Ease.OutBack)
            );

        animationSequence
            .Join(
                resultText
                    .DOFade(1f, 0.10f)
            );

        // 技能名称滑入
        animationSequence
            .Append(
                skillText.rectTransform
                    .DOAnchorPosY(
                        skillTextOriginalPosition.y,
                        0.16f
                    )
                    .SetEase(Ease.OutCubic)
            );

        animationSequence
            .Join(
                skillText
                    .DOFade(1f, 0.12f)
            );

        // 整体冲击
        animationSequence
            .Append(
                panelRoot
                    .DOPunchScale(
                        Vector3.one * 0.08f,
                        0.18f,
                        5,
                        0.5f
                    )
            );

        animationSequence
            .AppendInterval(displayDuration);

        // 向上离场
        animationSequence
            .Append(
                panelRoot
                    .DOAnchorPosY(
                        panelRootOriginalPosition.y + 25f,
                        0.25f
                    )
                    .SetEase(Ease.InCubic)
            );

        animationSequence
            .Join(
                canvasGroup
                    .DOFade(0f, 0.25f)
            );

        animationSequence
            .OnComplete(OnAnimationComplete);
    }

    private void AddSuccessVFX(
    Sequence sequence
)
    {
        if (sequence == null)
            return;

        PlayInkBurst(sequence);
        PlayGlowRing(sequence);
        PlaySweepLight(sequence);
    }

    private void PlayInkBurst(
    Sequence sequence
)
    {
        if (inkSplashes == null)
            return;

        for (int i = 0; i < inkSplashes.Length; i++)
        {
            Image ink = inkSplashes[i];

            if (ink == null)
                continue;

            RectTransform inkRect =
                ink.rectTransform;

            Vector2 direction =
                Random.insideUnitCircle;

            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector2.up;
            }

            direction.Normalize();

            float distance = Random.Range(
                inkBurstMinDistance,
                inkBurstMaxDistance
            );

            Vector2 targetPosition =
                direction * distance;

            float delay =
                Random.Range(0f, 0.07f);

            float targetScale =
                Random.Range(0.6f, 1.3f);

            float alpha =
                Random.Range(0.5f, 1f);

            inkRect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    Random.Range(0f, 360f)
                );

            sequence.Insert(
                delay,
                ink.DOFade(
                    alpha,
                    0.05f
                )
            );

            sequence.Insert(
                delay,
                inkRect
                    .DOAnchorPos(
                        targetPosition,
                        0.32f
                    )
                    .SetEase(Ease.OutCubic)
            );

            sequence.Insert(
                delay,
                inkRect
                    .DOScale(
                        targetScale,
                        0.18f
                    )
                    .SetEase(Ease.OutBack)
            );

            sequence.Insert(
                delay + 0.13f,
                ink
                    .DOFade(
                        0f,
                        0.25f
                    )
            );
        }
    }

    private void PlayGlowRing(
    Sequence sequence
)
    {
        if (glowRing == null)
            return;

        sequence.Insert(
            0.02f,
            glowRing
                .DOFade(
                    0.8f,
                    0.05f
                )
        );

        sequence.Insert(
            0.02f,
            glowRing.rectTransform
                .DOScale(
                    1.8f,
                    0.35f
                )
                .SetEase(Ease.OutCubic)
        );

        sequence.Insert(
            0.12f,
            glowRing
                .DOFade(
                    0f,
                    0.25f
                )
        );
    }

    private void PlaySweepLight(
    Sequence sequence
)
    {
        if (sweepLight == null)
            return;

        float endX =
            sweepLightOriginalPosition.x +
            sweepDistance;

        sequence.Insert(
            0.12f,
            sweepLight
                .DOFade(
                    0.85f,
                    0.04f
                )
        );

        sequence.Insert(
            0.12f,
            sweepLight.rectTransform
                .DOAnchorPosX(
                    endX,
                    0.3f
                )
                .SetEase(Ease.OutQuad)
        );

        sequence.Insert(
            0.28f,
            sweepLight
                .DOFade(
                    0f,
                    0.12f
                )
        );
    }

    private void PlayFailureAnimation()
    {
        KillCurrentAnimation();
        ResetAnimationState();

        animationSequence = DOTween.Sequence();

        // 不受 Brush Mode TimeScale 影响
        animationSequence.SetUpdate(true);

        // 快速淡入
        animationSequence
            .Append(
                canvasGroup
                    .DOFade(1f, 0.08f)
            );

        // 背景快速展开
        animationSequence
            .Join(
                background
                    .DOScaleX(1f, 0.12f)
                    .SetEase(Ease.OutExpo)
            );

        // Failed 文字砸入
        animationSequence
            .Append(
                resultText.rectTransform
                    .DOScale(1f, 0.14f)
                    .SetEase(Ease.OutBack)
            );

        animationSequence
            .Join(
                resultText
                    .DOFade(1f, 0.10f)
            );

        // 整体左右震动
        animationSequence
            .Append(
                panelRoot
                    .DOShakeAnchorPos(
                        0.28f,
                        new Vector2(18f, 0f),
                        12,
                        90f,
                        false,
                        true
                    )
            );

        // 提示文字从下方滑入
        animationSequence
            .Append(
                skillText.rectTransform
                    .DOAnchorPosY(
                        skillTextOriginalPosition.y,
                        0.16f
                    )
                    .SetEase(Ease.OutCubic)
            );

        animationSequence
            .Join(
                skillText
                    .DOFade(1f, 0.10f)
            );

        // 停留
        animationSequence
            .AppendInterval(displayDuration);

        // 淡出
        animationSequence
            .Append(
                canvasGroup
                    .DOFade(0f, 0.25f)
            );

        animationSequence
            .OnComplete(OnAnimationComplete);
    }

    private void ResetAnimationState()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (panelRoot != null)
        {
            panelRoot.anchoredPosition =
                panelRootOriginalPosition;

            panelRoot.localScale =
                Vector3.one;
        }

        if (background != null)
        {
            background.localScale =
                new Vector3(
                    0f,
                    1f,
                    1f
                );
        }

        if (resultText != null)
        {
            resultText.alpha = 0f;

            resultText.rectTransform.localScale =
                Vector3.one * 1.5f;
        }

        if (skillText != null)
        {
            skillText.alpha = 0f;

            skillText.rectTransform.anchoredPosition =
                skillTextOriginalPosition +
                Vector2.down * 20f;
        }

        // Glow Ring 初始状态
        if (glowRing != null)
        {
            Color glowColor =
                glowRing.color;

            glowColor.a = 0f;

            glowRing.color =
                glowColor;

            glowRing.rectTransform.localScale =
                Vector3.one * 0.2f;
        }

        // Sweep Light 初始状态
        if (sweepLight != null)
        {
            Color sweepColor =
                sweepLight.color;

            sweepColor.a = 0f;

            sweepLight.color =
                sweepColor;

            sweepLight.rectTransform.anchoredPosition =
                new Vector2(
                    sweepLightOriginalPosition.x -
                    sweepDistance,
                    sweepLightOriginalPosition.y
                );
        }

        // Ink 初始状态
        if (inkSplashes != null)
        {
            for (int i = 0;
                 i < inkSplashes.Length;
                 i++)
            {
                Image ink =
                    inkSplashes[i];

                if (ink == null)
                    continue;

                Color inkColor =
                    ink.color;

                inkColor.a = 0f;

                ink.color =
                    inkColor;

                ink.rectTransform.anchoredPosition =
                    Vector2.zero;

                ink.rectTransform.localScale =
                    Vector3.one * 0.2f;
            }
        }
    }

    private void OnAnimationComplete()
    {
        animationSequence = null;

        HideMe();
    }

    private void KillCurrentAnimation()
    {
        if (animationSequence != null &&
            animationSequence.IsActive())
        {
            animationSequence.Kill();
        }

        animationSequence = null;
    }

    private string GetSkillDisplayName(
        BrushSkillType skillType
    )
    {
        switch (skillType)
        {
            case BrushSkillType.Slash:
                return "Slash";

            case BrushSkillType.Fire:
                return "Fire";

            case BrushSkillType.Water:
                return "Water";

            case BrushSkillType.Wood:
                return "Wood";

            case BrushSkillType.Bridge:
                return "Bridge";

            default:
                return "Unknown";
        }
    }

    private void OnDisable()
    {
        KillCurrentAnimation();
    }
}