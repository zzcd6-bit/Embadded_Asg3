using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BambooFogZone : MonoBehaviour
{
    [Header("Fog")]
    [SerializeField, Range(0f, 1f)] private float fogDensity = 0.75f;
    [SerializeField] private bool autoRadiusFromDensity = true;

    [Header("Advanced")]
    [SerializeField, Min(0.1f)] private float triggerRadius = 6f;
    [SerializeField, Min(0f)] private float visibilityRadius = 45f;
    [SerializeField, Min(0f)] private float clearRadius = 6f;
    [SerializeField, Min(0f)] private float transitionDuration = 2f;
    [SerializeField] private int priority;
    [SerializeField] private bool requirePlayerTag = true;

    private const float VisibilityRadiusAtClearFog = 120f;
    private const float VisibilityRadiusAtDenseFog = 24f;
    private const float ClearRadiusAtClearFog = 14f;
    private const float ClearRadiusAtDenseFog = 3f;
    private const float AutoRadiusCurvePower = 0.65f;

    public BambooFogSettings Settings => GetSettings();
    public int Priority => priority;
    public float FogDensity => fogDensity;
    public bool AutoRadiusFromDensity => autoRadiusFromDensity;
    public float TriggerRadius => triggerRadius;

    public void Configure(BambooFogSettings newSettings, int newPriority, bool newRequirePlayerTag = true)
    {
        fogDensity = Mathf.Clamp01(newSettings.fogDensity);
        visibilityRadius = Mathf.Max(0f, newSettings.visibilityDistance);
        clearRadius = Mathf.Max(0f, newSettings.clearRadius);
        transitionDuration = Mathf.Max(0f, newSettings.transitionDuration);
        priority = newPriority;
        requirePlayerTag = newRequirePlayerTag;
        ApplyColliderSettings();
    }

    public void SetTriggerRadius(float radius)
    {
        triggerRadius = Mathf.Max(0.1f, radius);
        ApplyColliderSettings();
    }

    public BambooFogSettings GetSettings()
    {
        float density = Mathf.Clamp01(fogDensity);
        float radiusT = Mathf.Pow(density, AutoRadiusCurvePower);
        float targetVisibilityRadius = autoRadiusFromDensity
            ? Mathf.Lerp(VisibilityRadiusAtClearFog, VisibilityRadiusAtDenseFog, radiusT)
            : visibilityRadius;
        float targetClearRadius = autoRadiusFromDensity
            ? Mathf.Lerp(ClearRadiusAtClearFog, ClearRadiusAtDenseFog, radiusT)
            : clearRadius;

        return new BambooFogSettings(density, targetVisibilityRadius, targetClearRadius, transitionDuration);
    }

    private void Reset()
    {
        autoRadiusFromDensity = true;
        ApplyColliderSettings();
    }

    private void OnValidate()
    {
        fogDensity = Mathf.Clamp01(fogDensity);
        triggerRadius = Mathf.Max(0.1f, triggerRadius);
        visibilityRadius = Mathf.Max(0f, visibilityRadius);
        clearRadius = Mathf.Max(0f, clearRadius);
        transitionDuration = Mathf.Max(0f, transitionDuration);
        ApplyColliderSettings();

        if (BambooFogController.Instance != null)
        {
            BambooFogController.Instance.RefreshZoneSettings(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        BambooFogController controller = BambooFogController.Instance;
        if (controller != null)
        {
            controller.RegisterZone(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        BambooFogController controller = BambooFogController.Instance;
        if (controller != null)
        {
            controller.UnregisterZone(this);
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        PlayerController playerController = other.GetComponentInParent<PlayerController>();
        if (playerController != null && playerController == PlayerController.instance)
        {
            return true;
        }

        if (!requirePlayerTag)
        {
            return true;
        }

        return other.CompareTag("Player");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.65f, 0.85f, 1f, 0.3f);
        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            if (zoneCollider is SphereCollider sphereCollider)
            {
                Gizmos.DrawSphere(sphereCollider.center, sphereCollider.radius);
            }
            else
            {
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
            }
        }
    }

    private void ApplyColliderSettings()
    {
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            sphereCollider.isTrigger = true;
            sphereCollider.radius = triggerRadius;
            return;
        }

        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }
    }
}
