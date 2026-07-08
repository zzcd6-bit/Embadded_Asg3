using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class PlayerPerfectDodgePostProcess : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Volume targetVolume;
    [SerializeField] private bool createMissingOverrides = true;

    [Header("Effect Duration")]
    [SerializeField] private float effectDuration = 0.35f;

    [Header("Vignette")]
    [SerializeField] private bool useVignette = true;
    [SerializeField] private Color vignetteColor = new Color(0.25f, 0.75f, 1f, 1f);
    [SerializeField] private float vignettePeakIntensity = 0.45f;
    [SerializeField] private float vignetteSmoothness = 0.55f;

    [Header("Chromatic Aberration")]
    [SerializeField] private bool useChromaticAberration = true;
    [SerializeField] private float chromaticPeakIntensity = 0.55f;

    [Header("Bloom")]
    [SerializeField] private bool useBloom = true;
    [SerializeField] private float bloomPeakIntensity = 2.2f;

    [SerializeField] private string targetVolumeObjectName = "PerfectDodge_GlobalVolume";

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private Bloom bloom;

    private float defaultVignetteIntensity;
    private float defaultVignetteSmoothness;
    private Color defaultVignetteColor;

    private float defaultChromaticIntensity;
    private float defaultBloomIntensity;

    private Coroutine effectCoroutine;
    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        if (targetVolume == null && !string.IsNullOrEmpty(targetVolumeObjectName))
        {
            GameObject volumeObject = GameObject.Find(targetVolumeObjectName);

            if (volumeObject != null)
            {
                targetVolume = volumeObject.GetComponent<Volume>();
            }
        }

        if (targetVolume == null)
        {
            targetVolume = FindObjectOfType<Volume>();
        }

        if (targetVolume == null)
        {
            Debug.LogWarning("[PlayerPerfectDodgePostProcess] No Volume found.", this);
            return;
        }

        VolumeProfile profile = targetVolume.profile;

        if (profile == null)
        {
            Debug.LogWarning("[PlayerPerfectDodgePostProcess] Target Volume has no profile.", this);
            return;
        }

        SetupVignette(profile);
        SetupChromaticAberration(profile);
        SetupBloom(profile);

        SaveDefaultValues();

        initialized = true;
    }

    private void SetupVignette(VolumeProfile profile)
    {
        if (!profile.TryGet(out vignette) && createMissingOverrides)
        {
            vignette = profile.Add<Vignette>(true);
        }

        if (vignette == null)
        {
            return;
        }

        vignette.active = true;
        vignette.intensity.overrideState = true;
        vignette.color.overrideState = true;
        vignette.smoothness.overrideState = true;
    }

    private void SetupChromaticAberration(VolumeProfile profile)
    {
        if (!profile.TryGet(out chromaticAberration) && createMissingOverrides)
        {
            chromaticAberration = profile.Add<ChromaticAberration>(true);
        }

        if (chromaticAberration == null)
        {
            return;
        }

        chromaticAberration.active = true;
        chromaticAberration.intensity.overrideState = true;
    }

    private void SetupBloom(VolumeProfile profile)
    {
        if (!profile.TryGet(out bloom) && createMissingOverrides)
        {
            bloom = profile.Add<Bloom>(true);
        }

        if (bloom == null)
        {
            return;
        }

        bloom.active = true;
        bloom.intensity.overrideState = true;
    }

    private void SaveDefaultValues()
    {
        if (vignette != null)
        {
            defaultVignetteIntensity = vignette.intensity.value;
            defaultVignetteSmoothness = vignette.smoothness.value;
            defaultVignetteColor = vignette.color.value;
        }

        if (chromaticAberration != null)
        {
            defaultChromaticIntensity = chromaticAberration.intensity.value;
        }

        if (bloom != null)
        {
            defaultBloomIntensity = bloom.intensity.value;
        }
    }

    public void PlayPerfectDodgeEffect()
    {
        Initialize();

        if (!initialized)
        {
            return;
        }

        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }

        effectCoroutine = StartCoroutine(PerfectDodgeEffectRoutine());

        if (debugLog)
        {
            Debug.Log("[PlayerPerfectDodgePostProcess] Play perfect dodge post process.", this);
        }
    }

    private IEnumerator PerfectDodgeEffectRoutine()
    {
        float timer = 0f;
        float duration = Mathf.Max(0.01f, effectDuration);

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float normalized = Mathf.Clamp01(timer / duration);

            // ÏÈÇ¿£¬ºóÂýÂý»Ö¸´
            float strength = 1f - normalized;
            strength = strength * strength;

            ApplyEffect(strength);

            yield return null;
        }

        RestoreDefaultValues();
        effectCoroutine = null;
    }

    private void ApplyEffect(float strength)
    {
        if (useVignette && vignette != null)
        {
            vignette.color.value = vignetteColor;
            vignette.intensity.value = Mathf.Lerp(
                defaultVignetteIntensity,
                vignettePeakIntensity,
                strength
            );

            vignette.smoothness.value = Mathf.Lerp(
                defaultVignetteSmoothness,
                vignetteSmoothness,
                strength
            );
        }

        if (useChromaticAberration && chromaticAberration != null)
        {
            chromaticAberration.intensity.value = Mathf.Lerp(
                defaultChromaticIntensity,
                chromaticPeakIntensity,
                strength
            );
        }

        if (useBloom && bloom != null)
        {
            bloom.intensity.value = Mathf.Lerp(
                defaultBloomIntensity,
                bloomPeakIntensity,
                strength
            );
        }
    }

    private void RestoreDefaultValues()
    {
        if (vignette != null)
        {
            vignette.intensity.value = defaultVignetteIntensity;
            vignette.smoothness.value = defaultVignetteSmoothness;
            vignette.color.value = defaultVignetteColor;
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = defaultChromaticIntensity;
        }

        if (bloom != null)
        {
            bloom.intensity.value = defaultBloomIntensity;
        }
    }

    private void OnDisable()
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
            effectCoroutine = null;
        }

        if (initialized)
        {
            RestoreDefaultValues();
        }
    }
}