using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

public class BrushTransitionController : MonoBehaviour
{
    [Header("Canvas Material")]
    public Renderer brushCanvasRenderer;
    public string brushAmountProperty = "_BrushAmount";

    [Header("Post Processing")]
    public Volume brushVolume;

    [Header("Transition")]
    public float transitionDuration = 0.35f;

    private Material brushCanvasMaterial;
    private Tween transitionTween;
    private float currentAmount;

    private void Awake()
    {
        if (brushCanvasRenderer != null)
        {
            brushCanvasMaterial = brushCanvasRenderer.material;
            brushCanvasMaterial.SetFloat(brushAmountProperty, 0f);
        }

        if (brushVolume != null)
        {
            brushVolume.weight = 0f;
        }
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener<bool>(
            E_EventType.E_Brush_ModeChanged,
            OnBrushModeChanged
        );
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<bool>(
            E_EventType.E_Brush_ModeChanged,
            OnBrushModeChanged
        );
    }

    private void OnBrushModeChanged(bool isBrushMode)
    {
        float target = isBrushMode ? 1f : 0f;

        if (transitionTween != null && transitionTween.IsActive())
            transitionTween.Kill();

        transitionTween = DOVirtual.Float(
            currentAmount,
            target,
            transitionDuration,
            value =>
            {
                currentAmount = value;

                if (brushCanvasMaterial != null)
                    brushCanvasMaterial.SetFloat(brushAmountProperty, value);

                if (brushVolume != null)
                    brushVolume.weight = value;
            }
        ).SetUpdate(true);
    }
}