using DG.Tweening;
using UnityEngine;

public class BrushModeController : MonoBehaviour
{
    [Header("Brush Mode")]
    public float brushTimeScale = 0.15f;
    public float transitionDuration = 0.35f;

    [Header("New Player")]
    [SerializeField] private ActionPlayerController newPlayerController;

    [Header("»­»­Ä£Ê½ÀäÈ´")]
    public BrushModeCooldownController cooldownController;

    public bool IsBrushMode { get; private set; }

    private const float DefaultFixedDeltaTime = 0.02f;
    private Tween timeTween;

    private void Awake()
    {
        if (newPlayerController == null)
        {
            newPlayerController = FindObjectOfType<ActionPlayerController>();
        }

        if (cooldownController == null)
        {
            cooldownController = GetComponent<BrushModeCooldownController>();
        }

        if (cooldownController == null)
        {
            cooldownController = GetComponentInChildren<BrushModeCooldownController>();
        }

        if (cooldownController == null)
        {
            cooldownController = GetComponentInParent<BrushModeCooldownController>();
        }
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener(E_EventType.E_Brush_Enter, EnterBrushMode);
        EventCenter.Instance.AddEventListener(E_EventType.E_Brush_Exit, ExitBrushMode);
        EventCenter.Instance.AddEventListener(
            E_EventType.E_Brush_RequestExit,
            ExitBrushMode
        );
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Brush_Enter, EnterBrushMode);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Brush_Exit, ExitBrushMode);
        EventCenter.Instance.RemoveEventListener(
            E_EventType.E_Brush_RequestExit,
            ExitBrushMode
        );
    }

    private void EnterBrushMode()
    {
        if (cooldownController != null && !cooldownController.CanEnterBrushMode())
        {
            Debug.Log(
                $"[BrushModeController] Brush mode is cooling down. Remaining: {cooldownController.RemainingCooldown:F1}s"
            );

            return;
        }

        if (IsBrushMode)
            return;

        IsBrushMode = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;

        if (newPlayerController != null)
        {
            newPlayerController.SetGameplayControlEnabled(false);
        }

        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_Player_ControlEnable, false);
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_Player_CombatEnable, false);
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_Camera_InputEnable, false);
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_Brush_ModeChanged, true);

        ChangeTimeScale(brushTimeScale);
    }

    private void ExitBrushMode()
    {
        if (!IsBrushMode)
            return;

        if (cooldownController != null)
        {
            cooldownController.StartCooldown();
        }

        IsBrushMode = false;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_Brush_ModeChanged, false);

        ChangeTimeScale(1f, () =>
        {
            if (newPlayerController != null)
            {
                newPlayerController.SetGameplayControlEnabled(true);
            }

            EventCenter.Instance.EventTrigger<bool>(E_EventType.E_Player_ControlEnable, true);
            EventCenter.Instance.EventTrigger<bool>(E_EventType.E_Player_CombatEnable, true);
            EventCenter.Instance.EventTrigger<bool>(E_EventType.E_Camera_InputEnable, true);
        });
    }

    private void ChangeTimeScale(float targetScale, TweenCallback onComplete = null)
    {
        if (timeTween != null && timeTween.IsActive())
            timeTween.Kill();

        timeTween = DOVirtual.Float(
            Time.timeScale,
            targetScale,
            transitionDuration,
            value =>
            {
                Time.timeScale = value;
                Time.fixedDeltaTime = DefaultFixedDeltaTime * Mathf.Max(value, 0.01f);
            }
        )
        .SetUpdate(true)
        .OnComplete(onComplete);
    }
}