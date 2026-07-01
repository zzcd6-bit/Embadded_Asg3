using DynamicFogAndMist2;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class BambooFogWindController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DynamicFog fog;
    [SerializeField] private bool autoFindFog = true;

    [Header("Base Fog Wind")]
    [SerializeField] private Vector3 direction = new Vector3(1f, 0f, 0.25f);
    [SerializeField, Min(0f)] private float directionMagnitude = 0.035f;
    [SerializeField, Min(0f)] private float speed = 0.75f;
    [SerializeField, Min(0f)] private float turbulence = 12f;
    [SerializeField, Min(0.001f)] private float noiseScale = 20f;
    [SerializeField, Range(-1f, 1f)] private float noiseShift;

    [Header("Gusts")]
    [SerializeField] private bool enableGusts = true;
    [SerializeField] private bool animateInEditMode;
    [SerializeField] private Vector2 gustIntervalRange = new Vector2(8f, 18f);
    [SerializeField] private Vector2 gustDurationRange = new Vector2(2f, 4f);
    [SerializeField, Min(0f)] private float gustSpeedBoost = 0.45f;
    [SerializeField, Min(0f)] private float gustTurbulenceBoost = 8f;
    [SerializeField] private AnimationCurve gustCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.35f, 1f),
        new Keyframe(1f, 0f)
    );

    private DynamicFogProfile appliedProfile;
    private double nextGustTime;
    private double gustStartTime;
    private float gustDuration;
    private float gustAmount;
    private float appliedTurbulence;
    private float appliedNoiseScale;
    private float appliedNoiseShift;
    private bool gustActive;

    private void OnEnable()
    {
        ResolveReferences();
        ScheduleNextGust(GetTime());
        ApplyWind();
    }

    private void Update()
    {
        ResolveReferences();
        UpdateGust();
        ApplyWind();
    }

    private void OnValidate()
    {
        gustIntervalRange = SanitizeRange(gustIntervalRange, 0.1f);
        gustDurationRange = SanitizeRange(gustDurationRange, 0.1f);
        ResolveReferences();
        ApplyWind();
    }

    [ContextMenu("Apply Fog Wind Now")]
    public void ApplyWind()
    {
        if (fog == null || fog.profile == null)
        {
            return;
        }

        DynamicFogProfile profile = fog.profile;
        Vector3 windDirection = GetNormalizedDirection() * directionMagnitude;

        profile.direction = windDirection;
        profile.speed = speed + gustAmount * gustSpeedBoost;
        profile.turbulence = turbulence + gustAmount * gustTurbulenceBoost;
        profile.scale = noiseScale;
        profile.shift = noiseShift;
        profile.ValidateSettings();

        if (profile != appliedProfile ||
            !Mathf.Approximately(profile.turbulence, appliedTurbulence) ||
            !Mathf.Approximately(profile.scale, appliedNoiseScale) ||
            !Mathf.Approximately(profile.shift, appliedNoiseShift))
        {
            appliedProfile = profile;
            appliedTurbulence = profile.turbulence;
            appliedNoiseScale = profile.scale;
            appliedNoiseShift = profile.shift;
            fog.UpdateMaterialProperties();
        }
    }

    private void ResolveReferences()
    {
        if (!autoFindFog || fog != null)
        {
            return;
        }

        fog = FindAnyObjectByType<DynamicFog>();
    }

    private void UpdateGust()
    {
        if (!enableGusts || (!Application.isPlaying && !animateInEditMode))
        {
            gustActive = false;
            gustAmount = 0f;
            return;
        }

        double time = GetTime();
        if (!gustActive && time >= nextGustTime)
        {
            gustActive = true;
            gustStartTime = time;
            gustDuration = Random.Range(gustDurationRange.x, gustDurationRange.y);
        }

        if (!gustActive)
        {
            gustAmount = 0f;
            return;
        }

        float t = Mathf.Clamp01((float)((time - gustStartTime) / Mathf.Max(0.01f, gustDuration)));
        gustAmount = gustCurve != null ? Mathf.Clamp01(gustCurve.Evaluate(t)) : Mathf.Sin(t * Mathf.PI);

        if (t >= 1f)
        {
            gustActive = false;
            gustAmount = 0f;
            ScheduleNextGust(time);
        }
    }

    private Vector3 GetNormalizedDirection()
    {
        Vector3 windDirection = direction;
        windDirection.y = 0f;
        if (windDirection.sqrMagnitude <= 0.0001f)
        {
            windDirection = Vector3.right;
        }

        return windDirection.normalized;
    }

    private void ScheduleNextGust(double time)
    {
        nextGustTime = time + Random.Range(gustIntervalRange.x, gustIntervalRange.y);
    }

    private static Vector2 SanitizeRange(Vector2 range, float minimum)
    {
        range.x = Mathf.Max(minimum, range.x);
        range.y = Mathf.Max(minimum, range.y);
        if (range.y < range.x)
        {
            range.y = range.x;
        }

        return range;
    }

    private static double GetTime()
    {
        return Application.isPlaying ? Time.timeAsDouble : Time.realtimeSinceStartupAsDouble;
    }
}
