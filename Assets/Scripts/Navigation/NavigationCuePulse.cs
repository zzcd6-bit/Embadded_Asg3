using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class NavigationCuePulse : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetLocalOffset = new(0f, 3.79f, 0f);

    [Header("Billboard")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool faceCamera = true;

    [Header("Pulse")]
    [SerializeField, Min(0.01f)] private float pulseDuration = 1.2f;
    [SerializeField, Min(0f)] private float scaleAmplitude = 0.24f;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.86f;
    [SerializeField, Min(0f)] private float bobAmplitude = 0.12f;
    [SerializeField, Min(0f)] private float bobSpeed = 1.6f;
    [SerializeField] private Color baseColor = new(1f, 0.83f, 0f, 1f);
    [SerializeField] private Color peakColor = new(1f, 1f, 0.82f, 1f);

    [Header("Glow")]
    [SerializeField] private bool createGlowSprite = true;
    [SerializeField, Min(1f)] private float glowScale = 1.9f;
    [SerializeField, Range(0f, 1f)] private float glowMinAlpha = 0.18f;
    [SerializeField, Range(0f, 1f)] private float glowMaxAlpha = 0.7f;
    [SerializeField] private Color glowColor = new(1f, 0.86f, 0.12f, 1f);
    [SerializeField] private bool createOuterPulse = true;
    [SerializeField, Min(1f)] private float outerPulseScale = 2.75f;
    [SerializeField, Range(0f, 1f)] private float outerPulseMaxAlpha = 0.28f;
    [SerializeField] private Color outerPulseColor = new(1f, 0.55f, 0.02f, 1f);

    [Header("Emission")]
    [SerializeField] private bool useEmission = true;
    [SerializeField, ColorUsage(true, true)] private Color emissionColor = new(1f, 0.72f, 0.05f, 1f);
    [SerializeField, Min(0f)] private float emissionMinIntensity = 1.2f;
    [SerializeField, Min(0f)] private float emissionMaxIntensity = 3.8f;

    private SpriteRenderer mainRenderer;
    private SpriteRenderer glowRenderer;
    private SpriteRenderer outerPulseRenderer;
    private Vector3 startScale;
    private Vector3 startLocalPosition;
    private MaterialPropertyBlock mainBlock;
    private MaterialPropertyBlock glowBlock;
    private MaterialPropertyBlock outerPulseBlock;
    private Color startColor;

#if UNITY_EDITOR
    [SerializeField, HideInInspector] private bool editorPreviewActive;
    private Vector3 editorPreviewPosition;
    private Quaternion editorPreviewRotation;
    private Vector3 editorPreviewScale;
    public bool EditorPreviewActive => editorPreviewActive;
#endif

    private void Awake()
    {
        Initialize();
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && editorPreviewActive)
        {
            StopEditorPreview();
        }
#endif
    }

    private void Initialize()
    {
        mainRenderer = GetComponent<SpriteRenderer>();
        startScale = transform.localScale;
        startLocalPosition = transform.localPosition;
        startColor = mainRenderer.color;
        mainBlock = new MaterialPropertyBlock();
        glowBlock = new MaterialPropertyBlock();
        outerPulseBlock = new MaterialPropertyBlock();

        if (createGlowSprite)
        {
            EnsureGlowSprite();
        }

        if (createOuterPulse)
        {
            EnsureOuterPulseSprite();
        }
    }

    private void LateUpdate()
    {
        ApplyPulse(Time.time, true);
    }

    private void ApplyPulse(float time, bool followTarget)
    {
        if (mainRenderer == null)
        {
            Initialize();
        }

        if (createGlowSprite && glowRenderer == null)
        {
            EnsureGlowSprite();
        }

        if (createOuterPulse && outerPulseRenderer == null)
        {
            EnsureOuterPulseSprite();
        }

        if (followTarget && target != null)
        {
            transform.position = target.TransformPoint(targetLocalOffset);
        }

        if (faceCamera)
        {
            FaceCamera();
        }

        float pulse = Mathf.Sin((time / pulseDuration) * Mathf.PI * 2f) * 0.5f + 0.5f;
        float easedPulse = Mathf.SmoothStep(0f, 1f, pulse);
        float scale = 1f + scaleAmplitude * easedPulse;
        float alpha = Mathf.Lerp(minAlpha, 1f, easedPulse);
        float bob = Mathf.Sin(time * bobSpeed * Mathf.PI * 2f) * bobAmplitude;

        if (!followTarget || target == null)
        {
            transform.localPosition = startLocalPosition + Vector3.up * bob;
        }

        transform.localScale = startScale * scale;
        SetRendererColor(
            mainRenderer,
            mainBlock,
            Color.Lerp(baseColor, peakColor, easedPulse),
            alpha,
            GetEmissionColor(easedPulse, 1f));

        if (glowRenderer != null)
        {
            glowRenderer.sprite = mainRenderer.sprite;
            glowRenderer.sortingLayerID = mainRenderer.sortingLayerID;
            glowRenderer.sortingOrder = mainRenderer.sortingOrder - 1;

            float glowPulse = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, easedPulse);
            glowRenderer.transform.localScale = Vector3.one * Mathf.Lerp(1f, glowScale, easedPulse);
            SetRendererColor(glowRenderer, glowBlock, glowColor, glowPulse, GetEmissionColor(easedPulse, 0.7f));
        }

        if (outerPulseRenderer != null)
        {
            outerPulseRenderer.sprite = mainRenderer.sprite;
            outerPulseRenderer.sortingLayerID = mainRenderer.sortingLayerID;
            outerPulseRenderer.sortingOrder = mainRenderer.sortingOrder - 2;

            float outwardPulse = Mathf.Repeat(time / pulseDuration, 1f);
            float outerScale = Mathf.Lerp(glowScale, outerPulseScale, outwardPulse);
            float outerAlpha = outerPulseMaxAlpha * (1f - outwardPulse);
            outerPulseRenderer.transform.localScale = Vector3.one * outerScale;
            SetRendererColor(
                outerPulseRenderer,
                outerPulseBlock,
                outerPulseColor,
                outerAlpha,
                GetEmissionColor(1f - outwardPulse, 0.45f));
        }
    }

    private void FaceCamera()
    {
        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(targetCamera.transform.forward, targetCamera.transform.up);
    }

    private void EnsureGlowSprite()
    {
        Transform existingGlow = transform.Find("Glow");
        GameObject glowObject = existingGlow != null ? existingGlow.gameObject : new GameObject("Glow");
        glowObject.transform.SetParent(transform, false);
        if (!Application.isPlaying)
        {
            glowObject.hideFlags = HideFlags.HideAndDontSave;
        }

        glowObject.transform.localPosition = Vector3.forward * 0.01f;
        glowObject.transform.localRotation = Quaternion.identity;
        glowObject.transform.localScale = Vector3.one * glowScale;

        glowRenderer = glowObject.GetComponent<SpriteRenderer>();
        if (glowRenderer == null)
        {
            glowRenderer = glowObject.AddComponent<SpriteRenderer>();
        }

        glowRenderer.sprite = mainRenderer.sprite;
        glowRenderer.sharedMaterial = mainRenderer.sharedMaterial;
        glowRenderer.flipX = mainRenderer.flipX;
        glowRenderer.flipY = mainRenderer.flipY;
        glowRenderer.maskInteraction = mainRenderer.maskInteraction;
    }

    private void EnsureOuterPulseSprite()
    {
        outerPulseRenderer = EnsureEffectSprite("Outer Pulse", outerPulseScale, -0.01f);
    }

    private SpriteRenderer EnsureEffectSprite(string objectName, float initialScale, float localZ)
    {
        Transform existing = transform.Find(objectName);
        GameObject effectObject = existing != null ? existing.gameObject : new GameObject(objectName);
        effectObject.transform.SetParent(transform, false);
        if (!Application.isPlaying)
        {
            effectObject.hideFlags = HideFlags.HideAndDontSave;
        }

        effectObject.transform.localPosition = Vector3.forward * localZ;
        effectObject.transform.localRotation = Quaternion.identity;
        effectObject.transform.localScale = Vector3.one * initialScale;

        SpriteRenderer effectRenderer = effectObject.GetComponent<SpriteRenderer>();
        if (effectRenderer == null)
        {
            effectRenderer = effectObject.AddComponent<SpriteRenderer>();
        }

        effectRenderer.sprite = mainRenderer.sprite;
        effectRenderer.sharedMaterial = mainRenderer.sharedMaterial;
        effectRenderer.flipX = mainRenderer.flipX;
        effectRenderer.flipY = mainRenderer.flipY;
        effectRenderer.maskInteraction = mainRenderer.maskInteraction;
        return effectRenderer;
    }

    private Color GetEmissionColor(float pulse, float multiplier)
    {
        if (!useEmission)
        {
            return Color.black;
        }

        float intensity = Mathf.Lerp(emissionMinIntensity, emissionMaxIntensity, pulse) * multiplier;
        return emissionColor * intensity;
    }

    private static void SetRendererColor(
        SpriteRenderer spriteRenderer,
        MaterialPropertyBlock block,
        Color color,
        float alpha,
        Color emission)
    {
        color.a *= alpha;
        spriteRenderer.color = color;
        Color hdrColor = new(
            color.r + emission.r,
            color.g + emission.g,
            color.b + emission.b,
            color.a);

        spriteRenderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", hdrColor);
        block.SetColor("_Color", color);
        block.SetColor("_EmissionColor", emission);
        block.SetColor("_Emission", emission);
        block.SetFloat("_Use_Emission", emission.maxColorComponent > 0f ? 1f : 0f);
        block.SetFloat("_EmissionEnabled", emission.maxColorComponent > 0f ? 1f : 0f);
        spriteRenderer.SetPropertyBlock(block);
    }

#if UNITY_EDITOR
    public void StartEditorPreview()
    {
        if (Application.isPlaying || editorPreviewActive)
        {
            return;
        }

        Initialize();
        editorPreviewActive = true;
        editorPreviewPosition = transform.position;
        editorPreviewRotation = transform.rotation;
        editorPreviewScale = transform.localScale;
        startScale = editorPreviewScale;
        startColor = mainRenderer.color;
    }

    public void StopEditorPreview()
    {
        if (Application.isPlaying)
        {
            return;
        }

        editorPreviewActive = false;

        if (mainRenderer == null)
        {
            mainRenderer = GetComponent<SpriteRenderer>();
        }

        transform.position = editorPreviewPosition;
        transform.rotation = editorPreviewRotation;
        transform.localScale = editorPreviewScale;

        if (mainRenderer != null)
        {
            mainRenderer.color = startColor;
            mainRenderer.SetPropertyBlock(null);
        }

        if (glowRenderer != null)
        {
            if (glowRenderer.gameObject != null)
            {
                DestroyImmediate(glowRenderer.gameObject);
            }

            glowRenderer = null;
        }

        if (outerPulseRenderer != null)
        {
            if (outerPulseRenderer.gameObject != null)
            {
                DestroyImmediate(outerPulseRenderer.gameObject);
            }

            outerPulseRenderer = null;
        }
    }

    public void EditorPreviewTick(float time)
    {
        if (Application.isPlaying || !editorPreviewActive)
        {
            return;
        }

        ApplyPulse(time, true);
    }
#endif
}
