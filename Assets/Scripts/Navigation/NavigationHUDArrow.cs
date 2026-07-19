using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class NavigationHUDArrow : MonoBehaviour
{
    [SerializeField] private Image arrowImage;
    [SerializeField] private float radius = -1f;
    [SerializeField, Min(0f)] private float glowIntensityMultiplier = 1f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Material materialInstance;

    private void Reset()
    {
        ResolveReferences();
        CaptureInitialRadius();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CaptureInitialRadius();
        SetVisible(false);
    }

    private void OnDisable()
    {
        DestroyMaterialInstance();
    }

    private void OnValidate()
    {
        ResolveReferences();
        CaptureInitialRadius();
    }

    public void ApplyNavigationState(
        bool indicatorIsOnBoundary,
        Vector2 centerToPivotDirection,
        Color arrowColor,
        Color glowColor,
        Material sourceMaterial,
        float shaderGlowIntensity,
        float shaderGlowRadius,
        float shaderGlowSoftness)
    {
        ResolveReferences();

        SetVisible(indicatorIsOnBoundary);
        if (!indicatorIsOnBoundary || centerToPivotDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector2 direction = centerToPivotDirection.normalized;
        float lockedRadius = GetRadius();
        rectTransform.anchoredPosition = direction * lockedRadius;

        // Arrow art points right at zero rotation.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        rectTransform.localScale = Vector3.one;

        if (arrowImage != null)
        {
            arrowImage.color = arrowColor;
            arrowImage.material = GetMaterialInstance(sourceMaterial);
            if (arrowImage.material != null)
            {
                arrowImage.material.SetColor("_GlowColor", glowColor);
                arrowImage.material.SetFloat("_GlowIntensity", shaderGlowIntensity * glowIntensityMultiplier);
                arrowImage.material.SetFloat("_GlowRadius", shaderGlowRadius);
                arrowImage.material.SetFloat("_GlowSoftness", shaderGlowSoftness);
            }
        }
    }

    private void ResolveReferences()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (arrowImage == null)
        {
            arrowImage = GetComponent<Image>();
        }

        if (arrowImage != null)
        {
            arrowImage.raycastTarget = false;
        }
    }

    private void CaptureInitialRadius()
    {
        if (rectTransform == null)
        {
            return;
        }

        if (radius < 0f)
        {
            radius = rectTransform.anchoredPosition.magnitude;
        }
    }

    private float GetRadius()
    {
        if (radius < 0f)
        {
            CaptureInitialRadius();
        }

        return Mathf.Max(0f, radius);
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (arrowImage != null)
        {
            arrowImage.enabled = visible;
        }
    }

    private Material GetMaterialInstance(Material sourceMaterial)
    {
        if (materialInstance != null)
        {
            return materialInstance;
        }

        Material source = sourceMaterial;
        if (source == null)
        {
            Shader shader = Shader.Find("UI/Navigation HUD Glow");
            if (shader == null)
            {
                return null;
            }

            source = new Material(shader);
        }

        materialInstance = new Material(source)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        return materialInstance;
    }

    private void DestroyMaterialInstance()
    {
        if (materialInstance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(materialInstance);
        }
        else
        {
            DestroyImmediate(materialInstance);
        }

        materialInstance = null;
    }
}
