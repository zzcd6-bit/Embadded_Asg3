using UnityEngine;

[ExecuteAlways]
public sealed class PaddleHandAttachment : MonoBehaviour
{
    [SerializeField] private string targetAnimatorName = "NPC9";
    [SerializeField] private string paddleObjectName = "\u8239\u68680";
    [SerializeField] private Transform paddleRoot;
    [SerializeField] private bool startWithLeftHandAsPivot = true;
    [SerializeField, Tooltip("False/0 = left hand, true/1 = right hand.")]
    private bool currentHand;
    [SerializeField] private HumanBodyBones pivotHand = HumanBodyBones.LeftHand;
    [SerializeField] private HumanBodyBones guideHand = HumanBodyBones.RightHand;
    [SerializeField] private Vector3 localPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 guideHandLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 localEulerOffset = Vector3.zero;
    [SerializeField] private Vector3 paddleLocalAxis = Vector3.forward;
    [SerializeField, Tooltip("When enabled, the paddle local axis points from the lower hand to the higher hand.")]
    private bool axisFromLowHandToHighHand = true;
    [SerializeField, Tooltip("Flip the final paddle axis direction when the imported model points the opposite way.")]
    private bool invertPaddleAxis;
    [SerializeField] private Vector3 localScale = Vector3.one;
    [SerializeField, Min(1)] private int smoothSwapFrames = 12;
    [SerializeField] private bool attachInEditMode = true;

    private bool smoothingSwap;
    private int smoothingFrame;
    private Vector3 smoothStartPosition;
    private Quaternion smoothStartRotation;
    private Vector3 smoothStartScale;

    public string TargetAnimatorName
    {
        get => targetAnimatorName;
        set => targetAnimatorName = value;
    }

    public string PaddleObjectName
    {
        get => paddleObjectName;
        set => paddleObjectName = value;
    }

    public Transform PaddleRoot
    {
        get => paddleRoot;
        set => paddleRoot = value;
    }

    public bool StartWithLeftHandAsPivot
    {
        get => startWithLeftHandAsPivot;
        set => startWithLeftHandAsPivot = value;
    }

    public bool CurrentHand
    {
        get => currentHand;
        set => SetCurrentHand(value);
    }

    public int CurrentHandValue => currentHand ? 1 : 0;

    public HumanBodyBones PivotHand
    {
        get => pivotHand;
        set
        {
            pivotHand = value;
            UpdateCurrentHandFromPivot();
        }
    }

    public HumanBodyBones GuideHand
    {
        get => guideHand;
        set => guideHand = value;
    }

    public Vector3 LocalPositionOffset
    {
        get => localPositionOffset;
        set => localPositionOffset = value;
    }

    public Vector3 GuideHandLocalOffset
    {
        get => guideHandLocalOffset;
        set => guideHandLocalOffset = value;
    }

    public Vector3 LocalEulerOffset
    {
        get => localEulerOffset;
        set => localEulerOffset = value;
    }

    private void OnEnable()
    {
        ApplyStartingHandIfNeeded();
        Attach();
    }

    private void Start()
    {
        ApplyStartingHandIfNeeded();
        Attach();
    }

    private void LateUpdate()
    {
        Attach();
    }

    private void OnValidate()
    {
        UpdateCurrentHandFromPivot();

        if (!Application.isPlaying && attachInEditMode)
        {
            Attach();
        }
        else
        {
            ApplyLocalTransform(ResolvePaddleRoot());
        }
    }

    [ContextMenu("Attach To Hands")]
    public void Attach()
    {
        if (!Application.isPlaying && !attachInEditMode)
        {
            return;
        }

        Transform paddle = ResolvePaddleRoot();
        if (paddle == null)
        {
            return;
        }

        Animator animator = FindTargetAnimator();
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
        {
            return;
        }

        Transform pivot = animator.GetBoneTransform(pivotHand);
        Transform guide = animator.GetBoneTransform(guideHand);
        if (pivot == null || guide == null)
        {
            return;
        }

        if (paddle.parent != pivot)
        {
            paddle.SetParent(pivot, smoothingSwap);
        }

        ApplyLocalTransform(paddle);
        AlignPaddleAxisToHands(paddle, guide);

        if (smoothingSwap)
        {
            ApplySmoothedSwapPose(paddle);
        }
    }

    [ContextMenu("Use Paddle0")]
    public void UsePaddle0()
    {
        paddleObjectName = "\u8239\u68680";
        paddleRoot = null;
        Attach();
    }

    [ContextMenu("Swap Hands")]
    public void SwapHands()
    {
        BeginSmoothSwap();
        SetHands(guideHand, pivotHand);
        Attach();
    }

    public void SwapHandsFromAnimationEvent()
    {
        SwapHands();
    }

    [ContextMenu("Set Left Hand As Pivot")]
    public void SetLeftHandAsPivot()
    {
        SetHands(HumanBodyBones.LeftHand, HumanBodyBones.RightHand);
        Attach();
    }

    [ContextMenu("Set Right Hand As Pivot")]
    public void SetRightHandAsPivot()
    {
        SetHands(HumanBodyBones.RightHand, HumanBodyBones.LeftHand);
        Attach();
    }

    public void SetCurrentHand(bool useRightHand)
    {
        if (useRightHand)
        {
            SetRightHandAsPivot();
        }
        else
        {
            SetLeftHandAsPivot();
        }
    }

    private void ApplyStartingHandIfNeeded()
    {
        if (Application.isPlaying && startWithLeftHandAsPivot)
        {
            SetHands(HumanBodyBones.LeftHand, HumanBodyBones.RightHand);
        }
    }

    private void SetHands(HumanBodyBones newPivotHand, HumanBodyBones newGuideHand)
    {
        pivotHand = newPivotHand;
        guideHand = newGuideHand;
        UpdateCurrentHandFromPivot();
    }

    private void BeginSmoothSwap()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Transform paddle = ResolvePaddleRoot();
        if (paddle == null)
        {
            return;
        }

        smoothStartPosition = paddle.position;
        smoothStartRotation = paddle.rotation;
        smoothStartScale = paddle.localScale;
        smoothingFrame = 0;
        smoothingSwap = true;
    }

    private void ApplySmoothedSwapPose(Transform paddle)
    {
        Vector3 targetPosition = paddle.position;
        Quaternion targetRotation = paddle.rotation;
        Vector3 targetScale = paddle.localScale;

        smoothingFrame++;
        float t = Mathf.Clamp01(smoothingFrame / (float)Mathf.Max(1, smoothSwapFrames));
        t = Mathf.SmoothStep(0f, 1f, t);

        paddle.position = Vector3.Lerp(smoothStartPosition, targetPosition, t);
        paddle.rotation = Quaternion.Slerp(smoothStartRotation, targetRotation, t);
        paddle.localScale = Vector3.Lerp(smoothStartScale, targetScale, t);

        if (smoothingFrame >= smoothSwapFrames)
        {
            smoothingSwap = false;
        }
    }

    private void UpdateCurrentHandFromPivot()
    {
        currentHand = pivotHand == HumanBodyBones.RightHand;
    }

    private Transform ResolvePaddleRoot()
    {
        if (paddleRoot != null)
        {
            return paddleRoot;
        }

        if (gameObject.name == paddleObjectName)
        {
            return transform;
        }

        GameObject paddleObject = GameObject.Find(paddleObjectName);
        return paddleObject != null ? paddleObject.transform : transform;
    }

    private Animator FindTargetAnimator()
    {
        if (string.IsNullOrWhiteSpace(targetAnimatorName))
        {
            return null;
        }

        GameObject target = GameObject.Find(targetAnimatorName);
        if (target == null)
        {
            return null;
        }

        return target.GetComponent<Animator>() ?? target.GetComponentInChildren<Animator>();
    }

    private void ApplyLocalTransform(Transform paddle)
    {
        if (paddle == null)
        {
            return;
        }

        paddle.localPosition = localPositionOffset;
        paddle.localRotation = Quaternion.Euler(localEulerOffset);
        paddle.localScale = localScale;
    }

    private void AlignPaddleAxisToHands(Transform paddle, Transform guide)
    {
        Vector3 pivotPosition = paddle.position;
        Vector3 guidePosition = guide.TransformPoint(guideHandLocalOffset);
        Vector3 targetDirection = axisFromLowHandToHighHand
            ? GetLowHandToHighHandDirection(pivotPosition, guidePosition)
            : pivotPosition - guidePosition;

        if (invertPaddleAxis)
        {
            targetDirection = -targetDirection;
        }

        if (targetDirection.sqrMagnitude < 0.000001f)
        {
            return;
        }

        Vector3 axis = paddleLocalAxis.sqrMagnitude < 0.000001f ? Vector3.forward : paddleLocalAxis.normalized;
        Vector3 currentAxisDirection = paddle.TransformDirection(axis);
        if (currentAxisDirection.sqrMagnitude < 0.000001f)
        {
            return;
        }

        Quaternion alignment = Quaternion.FromToRotation(currentAxisDirection, targetDirection.normalized);
        paddle.rotation = alignment * paddle.rotation;
    }

    private static Vector3 GetLowHandToHighHandDirection(Vector3 pivotPosition, Vector3 guidePosition)
    {
        return pivotPosition.y <= guidePosition.y
            ? guidePosition - pivotPosition
            : pivotPosition - guidePosition;
    }
}
