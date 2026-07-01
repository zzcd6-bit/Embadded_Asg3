using System.Collections.Generic;
using DynamicFogAndMist2;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public interface IBambooFogDensityController
{
    float CurrentFogDensity { get; }
    float TargetFogDensity { get; }

    void SetFogDensity(float density);
    void SetFogDensity(float density, float duration);
    void SetFogDensityImmediate(float density);
    void AddFogDensity(float delta, float duration);
}

public class BambooFogController : MonoBehaviour, IBambooFogDensityController
{
    public static BambooFogController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private DynamicFog fog;
    [SerializeField] private Transform player;
    [SerializeField] private DynamicFogProfile sourceProfile;

    [Header("Default Bamboo Fog")]
    [SerializeField] private BambooFogSettings defaultSettings = new BambooFogSettings(0.45f, 90f, 10f, 2f);

    [Header("Cold Gray Look")]
    [SerializeField] private bool applyColdGrayLook = true;
    [SerializeField, Range(0f, 1f)] private float coldGrayBlend = 0.8f;
    [SerializeField] private Color coldGrayTintColor = new Color(0.58f, 0.68f, 0.68f, 1f);
    [SerializeField] private Color coldGrayNoiseColor = new Color(0.46f, 0.54f, 0.55f, 1f);

    [Header("Visual Density Mapping")]
    [SerializeField] private AnimationCurve visualDensityCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 0.65f),
        new Keyframe(1f, 1f)
    );
    [SerializeField, Min(0.1f)] private float densityReferenceDistance = 45f;
    [SerializeField, Range(0f, 1f)] private float maxVisualFogAmount = 0.95f;
    [SerializeField, Min(0.0001f)] private float clearFogDecay = 0.08f;
    [SerializeField, Min(0.0001f)] private float denseFogDecay = 0.002f;

    [Header("Player Vision")]
    [SerializeField] private bool revealAroundPlayer = true;
    [SerializeField] private float revealAlpha = 0f;
    [SerializeField] private float revealInterval = 0.1f;

    [Header("Debug Input")]
    [SerializeField] private bool enableDebugKeys = true;
    [SerializeField] private KeyCode increaseFogKey = KeyCode.Equals;
    [SerializeField] private KeyCode decreaseFogKey = KeyCode.Minus;
    [SerializeField] private float debugDensityStep = 0.1f;
    [SerializeField] private float debugTransitionDuration = 1.5f;

    [Header("Editor Preview Teleport")]
    [SerializeField] private float navMeshSampleRadius = 8f;

    private readonly List<BambooFogZone> activeZones = new List<BambooFogZone>();
    private BambooFogZone currentZone;
    private Texture2D runtimeFogOfWarTexture;
    private Color32[] runtimeFogOfWarPixels;
    private BambooFogSettings currentSettings;
    private BambooFogSettings transitionStartSettings;
    private BambooFogSettings targetSettings;
    private Color baseTintColor;
    private Color baseNoiseColor;
    private float baseDistanceMax;
    private float transitionStartTime;
    private float transitionDuration;
    private float nextRevealTime;
    private bool initialized;
    private bool hasBaseProfileValues;

    public BambooFogSettings CurrentSettings => currentSettings;
    public BambooFogSettings TargetSettings => targetSettings;
    public float CurrentFogDensity => currentSettings.fogDensity;
    public float TargetFogDensity => targetSettings.fogDensity;
    public DynamicFog Fog => fog;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple BambooFogController instances found. The newest one will replace the previous instance.");
        }

        Instance = this;
        ResolveReferences();
        InitializeSettings();
    }

    private void OnEnable()
    {
        Instance = this;
        InitializeSettings();
    }

    private void InitializeSettings()
    {
        if (initialized)
        {
            return;
        }

        currentSettings = defaultSettings;
        targetSettings = defaultSettings;
        transitionStartSettings = defaultSettings;
        initialized = true;
    }

    private void Start()
    {
        ResolveReferences();
        PrepareFogProfile();
        ApplySettings(currentSettings);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        DestroyRuntimeFogOfWarTexture();
    }

    private void Update()
    {
        ResolveReferences();
        HandleDebugInput();
        UpdateTransition();
        UpdatePlayerReveal();
    }

    public static bool TrySetFogDensity(float density, float duration)
    {
        if (Instance == null)
        {
            return false;
        }

        Instance.SetFogDensity(density, duration);
        return true;
    }

    public static bool TrySetFogDensityImmediate(float density)
    {
        if (Instance == null)
        {
            return false;
        }

        Instance.SetFogDensityImmediate(density);
        return true;
    }

    public void SetFogDensity(float density)
    {
        SetFogDensity(density, targetSettings.transitionDuration);
    }

    public void SetFogDensity(float density, float duration)
    {
        BambooFogSettings settings = targetSettings;
        settings.fogDensity = Mathf.Clamp01(density);
        settings.transitionDuration = duration;
        SetTargetSettings(settings, duration);
    }

    public void SetFogDensityImmediate(float density)
    {
        SetFogDensity(density, 0f);
    }

    public void AddFogDensity(float delta, float duration)
    {
        SetFogDensity(targetSettings.fogDensity + delta, duration);
    }

    public void SetTargetSettings(BambooFogSettings settings)
    {
        SetTargetSettings(settings, settings.transitionDuration);
    }

    public void SetTargetSettings(BambooFogSettings settings, float duration)
    {
        settings.fogDensity = Mathf.Clamp01(settings.fogDensity);
        settings.visibilityDistance = Mathf.Max(0f, settings.visibilityDistance);
        settings.clearRadius = Mathf.Max(0f, settings.clearRadius);

        transitionStartSettings = currentSettings;
        targetSettings = settings;
        transitionDuration = Mathf.Max(0f, duration);
        transitionStartTime = Time.time;

        if (transitionDuration <= 0f)
        {
            currentSettings = targetSettings;
            ApplySettings(currentSettings);
        }
    }

    public void RegisterZone(BambooFogZone zone)
    {
        if (zone == null || activeZones.Contains(zone))
        {
            return;
        }

        activeZones.Add(zone);

        if (currentZone == null)
        {
            currentZone = zone;
            SetTargetSettings(zone.Settings);
        }
    }

    public void UnregisterZone(BambooFogZone zone)
    {
        if (zone == null)
        {
            return;
        }

        activeZones.Remove(zone);

        if (zone != currentZone)
        {
            return;
        }

        currentZone = GetBestActiveZone();
        if (currentZone != null)
        {
            SetTargetSettings(currentZone.Settings);
        }
    }

    public void PreviewZone(BambooFogZone zone, bool teleportPlayer)
    {
        if (zone == null)
        {
            return;
        }

        ResolveReferences();
        PrepareFogProfile();

        if (teleportPlayer)
        {
            TeleportPlayer(zone.transform.position);
        }

        if (!activeZones.Contains(zone))
        {
            activeZones.Add(zone);
        }

        currentZone = zone;
        SetTargetSettings(zone.Settings, 0f);
        RevealAtPlayer(true);
    }

    public void RefreshZoneSettings(BambooFogZone zone)
    {
        if (zone == null || zone != currentZone)
        {
            return;
        }

        SetTargetSettings(zone.Settings, 0f);
        RevealAtPlayer(true);
    }

    public void ForcePreviewZoneSettings(BambooFogZone zone)
    {
        if (zone == null)
        {
            return;
        }

        ResolveReferences();
        PrepareFogProfile();

        if (!activeZones.Contains(zone))
        {
            activeZones.Add(zone);
        }

        currentZone = zone;
        SetTargetSettings(zone.Settings, 0f);
        RevealAtPlayer(true);
    }

    private void ResolveReferences()
    {
        if (fog == null)
        {
            fog = FindAnyObjectByType<DynamicFog>();
        }

        if (player == null)
        {
            if (PlayerController.instance != null)
            {
                player = PlayerController.instance.transform;
            }
            else
            {
                GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
                player = taggedPlayer != null ? taggedPlayer.transform : null;
            }
        }
    }

    private void PrepareFogProfile()
    {
        if (fog == null)
        {
            return;
        }

        sourceProfile = ResolveSourceProfile(sourceProfile != null ? sourceProfile : fog.profile);
        if (fog.profile == null && sourceProfile != null)
        {
            fog.profile = sourceProfile;
            fog.UpdateMaterialProperties();
        }

        if (fog.profile == null)
        {
            return;
        }

        if (!hasBaseProfileValues)
        {
            baseTintColor = fog.profile.tintColor;
            baseNoiseColor = fog.profile.noiseColor;
            baseDistanceMax = fog.profile.distanceMax;
            hasBaseProfileValues = true;
        }

        EnsureFogOfWarTexture();
        fog.enableFogOfWar = revealAroundPlayer;
        fog.fogOfWarRestoreDelay = 0f;
        fog.fogOfWarRestoreDuration = 0f;
        fog.UpdateMaterialProperties();
    }

    private DynamicFogProfile ResolveSourceProfile(DynamicFogProfile candidate)
    {
        if (candidate != null && !candidate.name.Contains(" Runtime"))
        {
            return candidate;
        }

#if UNITY_EDITOR
        string sourceName = candidate != null ? candidate.name : "FogMedium";
        while (sourceName.Contains(" Runtime"))
        {
            sourceName = sourceName.Replace(" Runtime", string.Empty);
        }

        string[] guids = AssetDatabase.FindAssets(sourceName);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            DynamicFogProfile profile = AssetDatabase.LoadAssetAtPath<DynamicFogProfile>(path);
            if (profile != null && profile.name == sourceName)
            {
                return profile;
            }
        }
#endif

        return candidate;
    }

    private void DestroyRuntimeFogOfWarTexture()
    {
        if (runtimeFogOfWarTexture != null)
        {
            Texture2D textureToDestroy = runtimeFogOfWarTexture;
            runtimeFogOfWarTexture = null;
            runtimeFogOfWarPixels = null;
            if (Application.isPlaying)
            {
                Destroy(textureToDestroy);
            }
            else
            {
                DestroyImmediate(textureToDestroy);
            }
        }
    }

    private void HandleDebugInput()
    {
        if (!enableDebugKeys)
        {
            return;
        }

        if (Input.GetKeyDown(increaseFogKey) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            SetFogDensity(targetSettings.fogDensity + debugDensityStep, debugTransitionDuration);
        }
        else if (Input.GetKeyDown(decreaseFogKey) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            SetFogDensity(targetSettings.fogDensity - debugDensityStep, debugTransitionDuration);
        }
    }

    private void UpdateTransition()
    {
        if (transitionDuration <= 0f)
        {
            return;
        }

        float t = Mathf.Clamp01((Time.time - transitionStartTime) / transitionDuration);
        t = t * t * (3f - 2f * t);
        currentSettings = BambooFogSettings.Lerp(transitionStartSettings, targetSettings, t);
        ApplySettings(currentSettings);

        if (t >= 1f)
        {
            transitionDuration = 0f;
            currentSettings = targetSettings;
            ApplySettings(currentSettings);
        }
    }

    private void ApplySettings(BambooFogSettings settings)
    {
        if (fog == null)
        {
            return;
        }

        if (fog.profile == null)
        {
            PrepareFogProfile();
        }

        DynamicFogProfile profile = fog.profile;
        if (profile == null)
        {
            return;
        }

        ApplyVisualDensity(profile, settings.fogDensity);

        profile.distanceMax = baseDistanceMax;

        profile.tintColor = BlendProfileColor(baseTintColor, coldGrayTintColor);
        profile.noiseColor = BlendProfileColor(baseNoiseColor, coldGrayNoiseColor);

        profile.ValidateSettings();
        EnsureFogOfWarTexture();
        fog.enableFogOfWar = revealAroundPlayer;
        fog.fogOfWarRestoreDelay = 0f;
        fog.fogOfWarRestoreDuration = 0f;
        fog.UpdateMaterialProperties();
    }

    private Color BlendProfileColor(Color source, Color target)
    {
        if (!applyColdGrayLook)
        {
            return source;
        }

        Color blended = Color.Lerp(source, target, coldGrayBlend);
        blended.a = source.a;
        return blended;
    }

    private void ApplyVisualDensity(DynamicFogProfile profile, float density)
    {
        float inputDensity = Mathf.Clamp01(density);
        float visualDensity = visualDensityCurve != null
            ? Mathf.Clamp01(visualDensityCurve.Evaluate(inputDensity))
            : inputDensity;
        float decayAtClearFog = Mathf.Max(0.0001f, clearFogDecay);
        float decayAtDenseFog = Mathf.Max(0.0001f, denseFogDecay);

        if (visualDensity <= 0f)
        {
            profile.densityLinear = 0f;
            profile.densityExponential = 1.001f - decayAtClearFog;
            return;
        }

        float decay = Mathf.Lerp(decayAtClearFog, decayAtDenseFog, visualDensity);
        float referenceDistance = Mathf.Max(0.1f, densityReferenceDistance);
        float targetFogAmount = visualDensity * maxVisualFogAmount;
        float distanceFactor = 1f - Mathf.Exp(-referenceDistance * decay);

        profile.densityLinear = targetFogAmount * decay / Mathf.Max(0.0001f, distanceFactor);
        profile.densityExponential = 1.001f - decay;
    }

    private void EnsureFogOfWarTexture()
    {
        if (!revealAroundPlayer || fog == null || !fog.isActiveAndEnabled || fog.fogOfWarTexture != null)
        {
            return;
        }

        int size = Mathf.Clamp(fog.fogOfWarTextureSize, 32, 2048);
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        {
            name = "Bamboo Fog Of War Runtime",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[size * size];
        Color32 fogged = new Color32(255, 255, 255, 255);
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = fogged;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        runtimeFogOfWarPixels = pixels;
        runtimeFogOfWarTexture = texture;
        fog.fogOfWarTexture = texture;
    }

    private void UpdatePlayerReveal()
    {
        if (!revealAroundPlayer || fog == null || player == null || currentSettings.clearRadius <= 0f)
        {
            return;
        }

        if (Time.time < nextRevealTime)
        {
            return;
        }

        nextRevealTime = Time.time + revealInterval;
        RevealAtPlayer(false);
    }

    private void RevealAtPlayer(bool force)
    {
        if (!revealAroundPlayer || fog == null || player == null || currentSettings.clearRadius <= 0f)
        {
            return;
        }

        ResetRuntimeFogOfWarTexture();
        fog.SetFogOfWarAlpha(player.position, currentSettings.clearRadius, revealAlpha, false, 0f, fog.fogOfWarSmoothness, 0f, 0f);
        fog.UpdateFogOfWar(true);
    }

    private void ResetRuntimeFogOfWarTexture()
    {
        if (fog == null || fog.fogOfWarTexture == null)
        {
            return;
        }

        int pixelCount = fog.fogOfWarTexture.width * fog.fogOfWarTexture.height;
        if (runtimeFogOfWarPixels == null || runtimeFogOfWarPixels.Length != pixelCount)
        {
            runtimeFogOfWarPixels = new Color32[pixelCount];
        }

        Color32 fogged = new Color32(255, 255, 255, 255);
        for (int i = 0; i < runtimeFogOfWarPixels.Length; i++)
        {
            runtimeFogOfWarPixels[i] = fogged;
        }

        fog.fogOfWarTextureData = runtimeFogOfWarPixels;
    }

    private void TeleportPlayer(Vector3 position)
    {
        if (player == null)
        {
            return;
        }

        CharacterController characterController = player.GetComponent<CharacterController>();
        bool controllerWasEnabled = characterController != null && characterController.enabled;
        if (controllerWasEnabled)
        {
            characterController.enabled = false;
        }

        player.position = GetWalkableTeleportPosition(position, characterController);

        if (controllerWasEnabled)
        {
            characterController.enabled = true;
        }
    }

    private Vector3 GetWalkableTeleportPosition(Vector3 position, CharacterController characterController)
    {
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            return position;
        }

        float footOffset = 0f;
        if (characterController != null)
        {
            footOffset = characterController.height * 0.5f + characterController.center.y;
        }

        return hit.position + Vector3.up * footOffset;
    }

    private BambooFogZone GetBestActiveZone()
    {
        BambooFogZone bestZone = null;

        for (int i = activeZones.Count - 1; i >= 0; i--)
        {
            BambooFogZone zone = activeZones[i];
            if (zone == null || !zone.isActiveAndEnabled)
            {
                activeZones.RemoveAt(i);
                continue;
            }

            if (bestZone == null || zone.Priority >= bestZone.Priority)
            {
                bestZone = zone;
            }
        }

        return bestZone;
    }

}
