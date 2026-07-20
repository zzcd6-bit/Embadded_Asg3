using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class NavigationHUDIndicator : MonoBehaviour
{
    public static NavigationHUDIndicator Instance { get; private set; }

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetWorldOffset = new(0f, 2f, 0f);
    [SerializeField] private bool hideWhenNoTarget = true;
    [SerializeField] private bool hideWhenBehindCamera;

    [Header("Canvas")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform ellipseSpace;
    [SerializeField] private Camera worldCamera;

    [Header("Ellipse Bounds")]
    [SerializeField] private bool clampToEllipse = true;
    [SerializeField, Range(0.05f, 0.5f)] private float ellipseRadiusX = 0.42f;
    [SerializeField, Range(0.05f, 0.5f)] private float ellipseRadiusY = 0.34f;
    [SerializeField] private Vector2 ellipsePadding = new(48f, 48f);
    [SerializeField] private bool rotateTowardTargetWhenClamped;
    [SerializeField] private bool drawEllipseGizmo = true;
    [SerializeField] private Color ellipseGizmoColor = new(1f, 0.86f, 0.12f, 0.75f);

    [Header("Visual")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image glowImage;
    [SerializeField] private NavigationHUDArrow arrow;
    [SerializeField] private Material glowMaterial;
    [SerializeField] private Sprite indicatorSprite;
    [SerializeField] private Vector2 iconSize = new(64f, 64f);
    [SerializeField] private Color baseColor = new(1f, 0.83f, 0f, 1f);
    [SerializeField] private Color peakColor = new(1f, 1f, 0.82f, 1f);

    [Header("Pulse")]
    [SerializeField, Min(0.01f)] private float pulseDuration = 1.2f;
    [SerializeField, Min(0f)] private float scaleAmplitude = 0.18f;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.88f;

    [Header("Glow")]
    [SerializeField] private Color glowColor = new(1f, 0.86f, 0.12f, 1f);
    [SerializeField] private float glowScale = 1.35f;
    [SerializeField, Range(0f, 1f)] private float glowMinAlpha = 0.18f;
    [SerializeField, Range(0f, 1f)] private float glowMaxAlpha = 0.72f;
    [SerializeField, Min(0f)] private float shaderGlowIntensity = 2.6f;
    [SerializeField, Range(0f, 0.08f)] private float shaderGlowRadius = 0.018f;
    [SerializeField, Range(0f, 2f)] private float shaderGlowSoftness = 1.2f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 currentBasePosition;
    private bool currentIsClamped;
    private Vector2 currentCenterToPivotDirection;
    private Material iconMaterialInstance;
    private Material glowMaterialInstance;

    public bool IsOnEllipseBoundary => currentIsClamped;
    public Vector2 CenterToPivotDirection => currentCenterToPivotDirection;

    private void Awake()
    {
        if (Instance == null || Instance == this)
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Reset()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        UpdateIndicator();
    }

    private void OnDisable()
    {
        DestroyMaterialInstance(ref iconMaterialInstance);
        DestroyMaterialInstance(ref glowMaterialInstance);
    }

    private void LateUpdate()
    {
        UpdateIndicator();
    }

    private void OnValidate()
    {
        ResolveReferences();
        ApplyVisualDefaults();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        UpdateIndicator();
    }

    public void StartFollowing(GameObject targetObject)
    {
        StartFollowing(targetObject, HUDNavigationCueKind.NonTask);
    }

    public void StartFollowing(GameObject targetObject, HUDNavigationCueKind cueKind)
    {
        target = targetObject != null ? targetObject.transform : null;
        SetCueColor(HUDNavigationCuePalette.GetColor(cueKind));
        UpdateIndicator();
    }

    public void StopFollowing(GameObject targetObject)
    {
        if (targetObject == null || target != targetObject.transform)
        {
            return;
        }

        target = null;
        SetVisible(false);
        UpdateArrow(false, Vector2.right);
    }

    public void SetCueColor(Color color)
    {
        baseColor = color;
        peakColor = Color.Lerp(color, Color.white, 0.42f);
        glowColor = color;
    }

    private void ResolveReferences()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (ellipseSpace == null && canvas != null)
        {
            ellipseSpace = canvas.transform as RectTransform;
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        EnsureImages();
        ApplyVisualDefaults();
    }

    private void EnsureImages()
    {
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
            if (iconImage == null)
            {
                iconImage = gameObject.AddComponent<Image>();
            }
        }

        if (indicatorSprite == null && iconImage.sprite != null)
        {
            indicatorSprite = iconImage.sprite;
        }

        glowImage = EnsureEffectImage(glowImage, "Glow", 0);
        RemoveEffectObject("Outer Pulse");

        if (arrow == null)
        {
            arrow = GetComponentInChildren<NavigationHUDArrow>(true);
        }

        if (arrow == null)
        {
            Transform arrowTransform = transform.Find("Arrow");
            if (arrowTransform != null)
            {
                arrow = arrowTransform.GetComponent<NavigationHUDArrow>();
                if (arrow == null)
                {
                    arrow = arrowTransform.gameObject.AddComponent<NavigationHUDArrow>();
                }
            }
        }
    }

    private Image EnsureEffectImage(Image image, string objectName, int siblingIndex)
    {
        if (image != null)
        {
            return image;
        }

        Transform existing = transform.Find(objectName);
        GameObject effectObject = existing != null ? existing.gameObject : new GameObject(objectName);
        effectObject.transform.SetParent(transform, false);
        effectObject.transform.SetSiblingIndex(siblingIndex);

        RectTransform effectRect = effectObject.GetComponent<RectTransform>();
        if (effectRect == null)
        {
            effectRect = effectObject.AddComponent<RectTransform>();
        }

        effectRect.anchorMin = new Vector2(0.5f, 0.5f);
        effectRect.anchorMax = new Vector2(0.5f, 0.5f);
        effectRect.pivot = new Vector2(0.5f, 0.5f);
        effectRect.anchoredPosition = Vector2.zero;

        image = effectObject.GetComponent<Image>();
        if (image == null)
        {
            image = effectObject.AddComponent<Image>();
        }

        image.raycastTarget = false;
        return image;
    }

    private void ApplyVisualDefaults()
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.raycastTarget = false;
        iconImage.enabled = false;
        iconImage.material = GetMaterialInstance(ref iconMaterialInstance);
        rectTransform.sizeDelta = iconSize;

        if (indicatorSprite != null)
        {
            iconImage.sprite = indicatorSprite;
        }

        ConfigureEffectImage(glowImage, glowScale);
    }

    private void ConfigureEffectImage(Image image, float scale)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = indicatorSprite != null ? indicatorSprite : iconImage.sprite;
        if (image == glowImage)
        {
            image.material = GetMaterialInstance(ref glowMaterialInstance);
        }

        image.raycastTarget = false;

        RectTransform effectRect = image.rectTransform;
        effectRect.sizeDelta = iconSize;
        effectRect.localScale = Vector3.one * scale;
    }

    private void UpdateIndicator()
    {
        if (rectTransform == null || canvas == null || ellipseSpace == null || worldCamera == null)
        {
            ResolveReferences();
        }

        if (target == null || canvas == null || ellipseSpace == null || worldCamera == null)
        {
            SetVisible(!hideWhenNoTarget);
            UpdateArrow(false, Vector2.right);
            return;
        }

        Vector3 worldPosition = target.TransformPoint(targetWorldOffset);
        Vector3 viewport = worldCamera.WorldToViewportPoint(worldPosition);
        if (hideWhenBehindCamera && viewport.z < 0f)
        {
            SetVisible(false);
            UpdateArrow(false, Vector2.right);
            return;
        }

        SetVisible(true);

        Vector2 unclampedPosition;
        Vector2 clampedPosition;
        bool isClamped = false;
        bool isBehindCamera = viewport.z < 0f;

        if (isBehindCamera)
        {
            Vector2 behindDirection = GetBehindCameraDirection(worldPosition);
            clampedPosition = GetPointOnEllipse(behindDirection, GetEllipseRadii());
            unclampedPosition = clampedPosition + behindDirection.normalized;
            isClamped = true;
        }
        else
        {
            Vector2 normalizedFromCenter = new(viewport.x - 0.5f, viewport.y - 0.5f);
            unclampedPosition = NormalizedToEllipseSpace(normalizedFromCenter);
            clampedPosition = unclampedPosition;

            if (clampToEllipse)
            {
                Vector2 radii = GetEllipseRadii();
                float ellipseValue = GetEllipseValue(unclampedPosition, radii);

                if (ellipseValue > 1f)
                {
                    clampedPosition = unclampedPosition / Mathf.Sqrt(ellipseValue);
                    isClamped = true;
                }
            }
        }

        currentBasePosition = clampedPosition;
        currentIsClamped = isClamped;
        currentCenterToPivotDirection = clampedPosition.sqrMagnitude > 0.001f ? clampedPosition.normalized : Vector2.right;

        float time = GetPreviewTime();
        ApplyPulse(time, isClamped, unclampedPosition - clampedPosition);
        UpdateArrow(isClamped, currentCenterToPivotDirection);
    }

    private static float GetPreviewTime()
    {
#if UNITY_EDITOR
        return Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;
#else
        return Time.time;
#endif
    }

    private Vector2 NormalizedToEllipseSpace(Vector2 normalizedFromCenter)
    {
        Rect rect = ellipseSpace.rect;
        return new Vector2(normalizedFromCenter.x * rect.width, normalizedFromCenter.y * rect.height);
    }

    private Vector2 GetBehindCameraDirection(Vector3 worldPosition)
    {
        Vector3 cameraLocalPosition = worldCamera.transform.InverseTransformPoint(worldPosition);
        Vector2 direction = new(cameraLocalPosition.x, cameraLocalPosition.y);
        if (direction.sqrMagnitude < 0.0001f)
        {
            return Vector2.down;
        }

        return direction;
    }

    private Vector2 GetEllipseRadii()
    {
        Rect rect = ellipseSpace.rect;
        return new Vector2(
            Mathf.Max(1f, rect.width * ellipseRadiusX - ellipsePadding.x),
            Mathf.Max(1f, rect.height * ellipseRadiusY - ellipsePadding.y));
    }

    private static float GetEllipseValue(Vector2 position, Vector2 radii)
    {
        return
            (position.x * position.x) / (radii.x * radii.x) +
            (position.y * position.y) / (radii.y * radii.y);
    }

    private static Vector2 GetPointOnEllipse(Vector2 direction, Vector2 radii)
    {
        float ellipseValue = GetEllipseValue(direction, radii);
        if (ellipseValue <= 0.0001f)
        {
            return new Vector2(0f, -radii.y);
        }

        return direction / Mathf.Sqrt(ellipseValue);
    }

    private void ApplyPulse(float time, bool isClamped, Vector2 clampedDirection)
    {
        float pulse = Mathf.Sin((time / pulseDuration) * Mathf.PI * 2f) * 0.5f + 0.5f;
        float easedPulse = Mathf.SmoothStep(0f, 1f, pulse);
        float scale = 1f + scaleAmplitude * easedPulse;
        float alpha = Mathf.Lerp(minAlpha, 1f, easedPulse);

        rectTransform.anchoredPosition = currentBasePosition;
        rectTransform.localScale = Vector3.one;

        if (rotateTowardTargetWhenClamped && isClamped && clampedDirection.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(clampedDirection.y, clampedDirection.x) * Mathf.Rad2Deg - 90f;
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            rectTransform.localRotation = Quaternion.identity;
        }

        if (glowImage != null)
        {
            Color pulseColor = Color.Lerp(baseColor, peakColor, easedPulse);
            glowImage.color = WithAlpha(Color.Lerp(glowColor, pulseColor, 0.35f), Mathf.Lerp(glowMinAlpha, glowMaxAlpha, easedPulse) * alpha);
            glowImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, glowScale, easedPulse);
            ApplyGlowMaterial(glowImage, easedPulse, 1f);
        }
    }

    private void UpdateArrow(bool isClamped, Vector2 centerToPivotDirection)
    {
        if (arrow == null)
        {
            return;
        }

        arrow.ApplyNavigationState(
            isClamped,
            centerToPivotDirection,
            glowColor,
            glowColor,
            glowMaterial,
            shaderGlowIntensity,
            shaderGlowRadius,
            shaderGlowSoftness);
    }

    private void RemoveEffectObject(string objectName)
    {
        Transform existing = transform.Find(objectName);
        if (existing != null)
        {
            DestroyEffectObject(existing.gameObject);
        }
    }

    private static void DestroyEffectObject(GameObject effectObject)
    {
        if (effectObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            effectObject.SetActive(false);
        }
        else
        {
            DestroyImmediate(effectObject);
        }
    }

    private void ApplyGlowMaterial(Image image, float pulse, float multiplier)
    {
        if (image == null || image.material == null)
        {
            return;
        }

        image.material.SetColor("_GlowColor", glowColor);
        image.material.SetFloat("_GlowIntensity", shaderGlowIntensity * multiplier * Mathf.Lerp(0.55f, 1.25f, pulse));
        image.material.SetFloat("_GlowRadius", shaderGlowRadius);
        image.material.SetFloat("_GlowSoftness", shaderGlowSoftness);
    }

    private Material GetMaterialInstance(ref Material materialInstance)
    {
        if (materialInstance != null)
        {
            return materialInstance;
        }

        Material source = glowMaterial;
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

    private static void DestroyMaterialInstance(ref Material materialInstance)
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
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a *= alpha;
        return color;
    }

    private void OnDrawGizmos()
    {
        if (!drawEllipseGizmo)
        {
            return;
        }

        if (rectTransform == null || canvas == null || ellipseSpace == null)
        {
            ResolveReferences();
        }

        if (ellipseSpace == null)
        {
            return;
        }

        DrawEllipseGizmo();
    }

    private void DrawEllipseGizmo()
    {
        const int segmentCount = 96;
        Vector2 radii = GetEllipseRadii();
        Vector3 previous = EllipseLocalPointToWorld(new Vector2(radii.x, 0f));

        Color previousColor = Gizmos.color;
        Gizmos.color = ellipseGizmoColor;

        for (int i = 1; i <= segmentCount; i++)
        {
            float angle = i / (float)segmentCount * Mathf.PI * 2f;
            Vector2 localPoint = new(Mathf.Cos(angle) * radii.x, Mathf.Sin(angle) * radii.y);
            Vector3 current = EllipseLocalPointToWorld(localPoint);
            Gizmos.DrawLine(previous, current);
            previous = current;
        }

        Gizmos.color = previousColor;
    }

    private Vector3 EllipseLocalPointToWorld(Vector2 ellipseLocalPoint)
    {
        Vector3 local = new(ellipseLocalPoint.x, ellipseLocalPoint.y, 0f);
        return ellipseSpace.TransformPoint(local);
    }
}
