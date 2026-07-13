using UnityEngine;

[DisallowMultipleComponent]
public class PlayerBarsHud : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerResourceController resourceController;

    [Header("Masks")]
    [SerializeField] private RectTransform manaFillMask;
    [SerializeField] private RectTransform healthFillMask;

    [Header("Behaviour")]
    [SerializeField] private bool autoFindResourceController = true;
    [SerializeField] private float smoothSpeed = 12f;

    private float displayedMana = 1f;
    private float displayedHealth = 1f;
    private float targetMana = 1f;
    private float targetHealth = 1f;

    private void Awake()
    {
        ResolveResourceController();
        RefreshTargets();
        displayedMana = targetMana;
        displayedHealth = targetHealth;
        ApplyMasks();
    }

    private void OnEnable()
    {
        ResolveResourceController();
        Subscribe();
        RefreshTargets();
        ApplyMasks();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (resourceController == null && autoFindResourceController)
        {
            ResolveResourceController();
            Subscribe();
            RefreshTargets();
        }

        if (smoothSpeed <= 0f)
        {
            displayedMana = targetMana;
            displayedHealth = targetHealth;
        }
        else
        {
            float t = 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime);
            displayedMana = Mathf.Lerp(displayedMana, targetMana, t);
            displayedHealth = Mathf.Lerp(displayedHealth, targetHealth, t);
        }

        ApplyMasks();
    }

    public void SetResourceController(PlayerResourceController controller)
    {
        if (resourceController == controller)
            return;

        Unsubscribe();
        resourceController = controller;
        Subscribe();
        RefreshTargets();
        ApplyMasks();
    }

    public void SetMasks(RectTransform manaMask, RectTransform healthMask)
    {
        manaFillMask = manaMask;
        healthFillMask = healthMask;
        ApplyMasks();
    }

    private void ResolveResourceController()
    {
        if (resourceController != null || !autoFindResourceController)
            return;

        resourceController = PlayerResourceController.Instance;

        if (resourceController == null)
        {
            resourceController = FindAnyObjectByType<PlayerResourceController>();
        }
    }

    private void Subscribe()
    {
        if (resourceController == null)
            return;

        resourceController.HpChanged -= OnHpChanged;
        resourceController.InkChanged -= OnInkChanged;
        resourceController.HpChanged += OnHpChanged;
        resourceController.InkChanged += OnInkChanged;
    }

    private void Unsubscribe()
    {
        if (resourceController == null)
            return;

        resourceController.HpChanged -= OnHpChanged;
        resourceController.InkChanged -= OnInkChanged;
    }

    private void RefreshTargets()
    {
        if (resourceController == null)
        {
            targetMana = 1f;
            targetHealth = 1f;
            return;
        }

        OnHpChanged(resourceController.CurrentHp, resourceController.MaxHp);
        OnInkChanged(resourceController.CurrentInk, resourceController.MaxInk);
    }

    private void OnHpChanged(int current, int max)
    {
        targetHealth = GetRatio(current, max);
    }

    private void OnInkChanged(int current, int max)
    {
        targetMana = GetRatio(current, max);
    }

    private void ApplyMasks()
    {
        ApplyMask(manaFillMask, displayedMana);
        ApplyMask(healthFillMask, displayedHealth);
    }

    private static void ApplyMask(RectTransform mask, float value)
    {
        if (mask == null)
            return;

        Vector2 anchorMax = mask.anchorMax;
        anchorMax.x = Mathf.Clamp01(value);
        mask.anchorMax = anchorMax;
    }

    private static float GetRatio(int current, int max)
    {
        if (max <= 0)
            return 0f;

        return Mathf.Clamp01((float)current / max);
    }
}
