using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class XianxiaLookPass
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string BackupPath = "Assets/ArtStyle/XianxiaLookBackup.asset";
    private const string ProfilePath = "Assets/ArtStyle/XianxiaLookProfile.asset";
    private const string ReportPath = "Assets/ArtStyle/XianxiaLookReport.md";
    private const string VolumeName = "Xianxia Global Volume";
    private const string LightName = "Xianxia Main Light";

    [MenuItem("Tools/Art Style/Apply Xianxia Look Pass")]
    public static void ApplyXianxiaLook()
    {
        Directory.CreateDirectory("Assets/ArtStyle");

        Scene scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        XianxiaLookBackup backup = LoadOrCreateBackup(scene);

        ApplyRenderSettings();
        ApplySun();
        ApplyVolume();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        WriteReport("Applied", backup.createdAt);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Xianxia look pass applied. Restore with Tools/Art Style/Restore Look Before Xianxia Pass. Report: {ReportPath}");
    }

    [MenuItem("Tools/Art Style/Restore Look Before Xianxia Pass")]
    public static void RestoreBeforeXianxiaLook()
    {
        XianxiaLookBackup backup = AssetDatabase.LoadAssetAtPath<XianxiaLookBackup>(BackupPath);
        if (backup == null)
        {
            Debug.LogWarning($"No Xianxia look backup found at {BackupPath}.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        RestoreRenderSettings(backup);
        RestoreSun(backup);
        RestoreVolumes(backup);
        RemoveXianxiaVolumes();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        WriteReport("Restored", backup.createdAt);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Restored scene look from the Xianxia backup.");
    }

    private static XianxiaLookBackup LoadOrCreateBackup(Scene scene)
    {
        XianxiaLookBackup existing = AssetDatabase.LoadAssetAtPath<XianxiaLookBackup>(BackupPath);
        if (existing != null)
        {
            return existing;
        }

        XianxiaLookBackup backup = ScriptableObject.CreateInstance<XianxiaLookBackup>();
        backup.createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        backup.scenePath = scene.path;
        backup.renderSettings = CaptureRenderSettings();
        backup.sun = CaptureSun(RenderSettings.sun);
        backup.volumes = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include)
            .Where(volume => !IsBambooOrDynamicFogVolume(volume))
            .Select(CaptureVolume)
            .ToList();

        AssetDatabase.CreateAsset(backup, BackupPath);
        EditorUtility.SetDirty(backup);
        return backup;
    }

    private static void ApplyRenderSettings()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.58f, 0.70f, 0.72f, 1f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 34f;
        RenderSettings.fogEndDistance = 310f;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.66f, 0.77f, 0.82f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.31f, 0.42f, 0.43f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.13f, 0.18f, 0.19f, 1f);
        RenderSettings.ambientIntensity = 0.62f;
        RenderSettings.subtractiveShadowColor = new Color(0.20f, 0.30f, 0.36f, 1f);

        Material skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/ToonScapes/Spring Isles/Skybox/Materials/TSI_Skybox_02A.mat")
            ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/ToonScapes/Spring Isles/Skybox/Materials/TSI_Skybox_01A.mat");
        if (skybox != null)
        {
            RenderSettings.skybox = skybox;
        }
    }

    private static void ApplySun()
    {
        Light sun = RenderSettings.sun;
        if (sun == null)
        {
            sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude)
                .FirstOrDefault(light => light.type == LightType.Directional);
        }

        if (sun == null)
        {
            GameObject go = new GameObject(LightName);
            sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.name = LightName;
        sun.color = new Color(0.72f, 0.88f, 1f, 1f);
        sun.intensity = 0.92f;
        sun.shadowStrength = 0.82f;
        sun.transform.rotation = Quaternion.Euler(37f, -22f, 0f);
        RenderSettings.sun = sun;
    }

    private static void ApplyVolume()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        ConfigureProfile(profile);

        Volume volume = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include)
            .FirstOrDefault(v => v.name == VolumeName);
        if (volume == null)
        {
            GameObject go = new GameObject(VolumeName);
            volume = go.AddComponent<Volume>();
        }

        volume.enabled = true;
        volume.isGlobal = true;
        volume.priority = 60f;
        volume.weight = 1f;
        volume.sharedProfile = profile;
        EditorUtility.SetDirty(profile);
    }

    private static void ConfigureProfile(VolumeProfile profile)
    {
        ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
        color.active = true;
        color.postExposure.Override(-0.18f);
        color.contrast.Override(18f);
        color.colorFilter.Override(new Color(0.78f, 0.93f, 0.96f, 1f));
        color.hueShift.Override(-3f);
        color.saturation.Override(-16f);

        WhiteBalance whiteBalance = GetOrAdd<WhiteBalance>(profile);
        whiteBalance.active = true;
        whiteBalance.temperature.Override(-18f);
        whiteBalance.tint.Override(7f);

        Bloom bloom = GetOrAdd<Bloom>(profile);
        bloom.active = true;
        bloom.threshold.Override(0.78f);
        bloom.intensity.Override(0.38f);
        bloom.scatter.Override(0.58f);
        bloom.tint.Override(new Color(0.72f, 0.92f, 1f, 1f));

        Vignette vignette = GetOrAdd<Vignette>(profile);
        vignette.active = true;
        vignette.color.Override(new Color(0.04f, 0.09f, 0.10f, 1f));
        vignette.intensity.Override(0.20f);
        vignette.smoothness.Override(0.58f);

        Tonemapping tonemapping = GetOrAdd<Tonemapping>(profile);
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES);

        ShadowsMidtonesHighlights smh = GetOrAdd<ShadowsMidtonesHighlights>(profile);
        smh.active = true;
        smh.shadows.Override(new Vector4(0.70f, 0.88f, 0.95f, 0f));
        smh.midtones.Override(new Vector4(0.92f, 0.98f, 1f, 0f));
        smh.highlights.Override(new Vector4(1.00f, 0.96f, 0.84f, 0f));
        smh.shadowsStart.Override(0f);
        smh.shadowsEnd.Override(0.28f);
        smh.highlightsStart.Override(0.58f);
        smh.highlightsEnd.Override(1f);

        FilmGrain grain = GetOrAdd<FilmGrain>(profile);
        grain.active = true;
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(0.08f);
        grain.response.Override(0.72f);
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T component))
        {
            component = profile.Add<T>(true);
        }

        return component;
    }

    private static XianxiaRenderSettingsBackup CaptureRenderSettings()
    {
        return new XianxiaRenderSettingsBackup
        {
            fog = RenderSettings.fog,
            fogColor = RenderSettings.fogColor,
            fogMode = RenderSettings.fogMode,
            fogDensity = RenderSettings.fogDensity,
            fogStartDistance = RenderSettings.fogStartDistance,
            fogEndDistance = RenderSettings.fogEndDistance,
            ambientMode = RenderSettings.ambientMode,
            ambientSkyColor = RenderSettings.ambientSkyColor,
            ambientEquatorColor = RenderSettings.ambientEquatorColor,
            ambientGroundColor = RenderSettings.ambientGroundColor,
            ambientLight = RenderSettings.ambientLight,
            ambientIntensity = RenderSettings.ambientIntensity,
            subtractiveShadowColor = RenderSettings.subtractiveShadowColor,
            skyboxPath = AssetDatabase.GetAssetPath(RenderSettings.skybox)
        };
    }

    private static XianxiaLightBackup CaptureSun(Light sun)
    {
        if (sun == null)
        {
            return new XianxiaLightBackup { hadSun = false };
        }

        return new XianxiaLightBackup
        {
            hadSun = true,
            hierarchyPath = GetHierarchyPath(sun.transform),
            name = sun.name,
            color = sun.color,
            intensity = sun.intensity,
            shadowStrength = sun.shadowStrength,
            rotation = sun.transform.rotation.eulerAngles
        };
    }

    private static XianxiaVolumeBackup CaptureVolume(Volume volume)
    {
        return new XianxiaVolumeBackup
        {
            hierarchyPath = GetHierarchyPath(volume.transform),
            name = volume.name,
            enabled = volume.enabled,
            isGlobal = volume.isGlobal,
            priority = volume.priority,
            weight = volume.weight,
            profilePath = AssetDatabase.GetAssetPath(volume.sharedProfile)
        };
    }

    private static void RestoreRenderSettings(XianxiaLookBackup backup)
    {
        XianxiaRenderSettingsBackup settings = backup.renderSettings;
        RenderSettings.fog = settings.fog;
        RenderSettings.fogColor = settings.fogColor;
        RenderSettings.fogMode = settings.fogMode;
        RenderSettings.fogDensity = settings.fogDensity;
        RenderSettings.fogStartDistance = settings.fogStartDistance;
        RenderSettings.fogEndDistance = settings.fogEndDistance;
        RenderSettings.ambientMode = settings.ambientMode;
        RenderSettings.ambientSkyColor = settings.ambientSkyColor;
        RenderSettings.ambientEquatorColor = settings.ambientEquatorColor;
        RenderSettings.ambientGroundColor = settings.ambientGroundColor;
        RenderSettings.ambientLight = settings.ambientLight;
        RenderSettings.ambientIntensity = settings.ambientIntensity;
        RenderSettings.subtractiveShadowColor = settings.subtractiveShadowColor;
        RenderSettings.skybox = string.IsNullOrEmpty(settings.skyboxPath)
            ? null
            : AssetDatabase.LoadAssetAtPath<Material>(settings.skyboxPath);
    }

    private static void RestoreSun(XianxiaLookBackup backup)
    {
        RemoveExtraXianxiaLights(backup.sun.name);

        if (!backup.sun.hadSun)
        {
            RenderSettings.sun = null;
            RemoveAllXianxiaLights();
            return;
        }

        Light sun = null;

        if (RenderSettings.sun != null && RenderSettings.sun.name == LightName)
        {
            sun = RenderSettings.sun;
        }

        if (sun == null)
        {
            sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include)
                .FirstOrDefault(light => light.name == LightName && light.type == LightType.Directional);
        }

        Transform transform = FindTransform(backup.sun.hierarchyPath);
        if (sun == null)
        {
            sun = transform != null ? transform.GetComponent<Light>() : null;
        }

        if (sun == null)
        {
            sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include)
                .FirstOrDefault(light => light.name == backup.sun.name && light.type == LightType.Directional);
        }

        if (sun == null)
        {
            GameObject go = new GameObject(backup.sun.name);
            sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        RestoreSunValues(sun, backup.sun);
        RenderSettings.sun = sun;
        RemoveExtraXianxiaLights(backup.sun.name);
    }

    private static void RestoreSunValues(Light sun, XianxiaLightBackup backup)
    {
        sun.name = backup.name;
        sun.type = LightType.Directional;
        sun.color = backup.color;
        sun.intensity = backup.intensity;
        sun.shadowStrength = backup.shadowStrength;
        sun.transform.rotation = Quaternion.Euler(backup.rotation);
    }

    private static void RemoveExtraXianxiaLights(string restoredSunName)
    {
        Light renderSun = RenderSettings.sun;
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include)
                     .Where(light => light.name == LightName && light != renderSun)
                     .ToArray())
        {
            UnityEngine.Object.DestroyImmediate(light.gameObject);
        }

        if (!string.IsNullOrEmpty(restoredSunName))
        {
            Light[] duplicateRestoredLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include)
                .Where(light => light.name == restoredSunName && light.type == LightType.Directional)
                .ToArray();

            Light keep = RenderSettings.sun != null && RenderSettings.sun.name == restoredSunName
                ? RenderSettings.sun
                : duplicateRestoredLights.FirstOrDefault();

            foreach (Light duplicate in duplicateRestoredLights)
            {
                if (duplicate != keep)
                {
                    UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
                }
            }
        }
    }

    private static void RemoveAllXianxiaLights()
    {
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include)
                     .Where(light => light.name == LightName)
                     .ToArray())
        {
            UnityEngine.Object.DestroyImmediate(light.gameObject);
        }
    }

    private static void RestoreVolumes(XianxiaLookBackup backup)
    {
        foreach (XianxiaVolumeBackup volumeBackup in backup.volumes)
        {
            Transform transform = FindTransform(volumeBackup.hierarchyPath);
            Volume volume = transform != null ? transform.GetComponent<Volume>() : null;
            if (volume == null)
            {
                volume = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include)
                    .FirstOrDefault(v => v.name == volumeBackup.name);
            }

            if (volume == null || IsBambooOrDynamicFogVolume(volume))
            {
                continue;
            }

            volume.enabled = volumeBackup.enabled;
            volume.isGlobal = volumeBackup.isGlobal;
            volume.priority = volumeBackup.priority;
            volume.weight = volumeBackup.weight;
            volume.sharedProfile = string.IsNullOrEmpty(volumeBackup.profilePath)
                ? null
                : AssetDatabase.LoadAssetAtPath<VolumeProfile>(volumeBackup.profilePath);
        }
    }

    private static void RemoveXianxiaVolumes()
    {
        VolumeProfile xianxiaProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        foreach (Volume volume in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include)
                     .Where(v => v.name == VolumeName || (xianxiaProfile != null && v.sharedProfile == xianxiaProfile))
                     .ToArray())
        {
            UnityEngine.Object.DestroyImmediate(volume.gameObject);
        }
    }

    private static bool IsBambooOrDynamicFogVolume(Volume volume)
    {
        string name = volume.name.ToLowerInvariant();
        return name.Contains("dynamic fog") || name.Contains("bamboo fog");
    }

    private static Transform FindTransform(string hierarchyPath)
    {
        if (string.IsNullOrEmpty(hierarchyPath))
        {
            return null;
        }

        string[] parts = hierarchyPath.Split('/');
        GameObject root = SceneManager.GetActiveScene().GetRootGameObjects()
            .FirstOrDefault(go => go.name == parts[0]);
        Transform current = root != null ? root.transform : null;

        for (int i = 1; current != null && i < parts.Length; i++)
        {
            current = current.Find(parts[i]);
        }

        return current;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        Stack<string> parts = new Stack<string>();
        Transform current = transform;
        while (current != null)
        {
            parts.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", parts);
    }

    private static void WriteReport(string action, string backupCreatedAt)
    {
        File.WriteAllLines(ReportPath, new[]
        {
            "# Xianxia Look Pass",
            "",
            $"Action: {action}",
            $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"Backup created: {backupCreatedAt}",
            "",
            "## Restore",
            "- Use `Tools/Art Style/Restore Look Before Xianxia Pass` to revert scene lighting, fog, skybox, sun, and global volume settings.",
            "- This pass does not modify Bamboo fog scripts, Bamboo fog zones, or Dynamic Fog volumes."
        });
        AssetDatabase.ImportAsset(ReportPath);
    }
}

public class XianxiaLookBackup : ScriptableObject
{
    public string createdAt;
    public string scenePath;
    public XianxiaRenderSettingsBackup renderSettings;
    public XianxiaLightBackup sun;
    public List<XianxiaVolumeBackup> volumes = new List<XianxiaVolumeBackup>();
}

[Serializable]
public class XianxiaRenderSettingsBackup
{
    public bool fog;
    public Color fogColor;
    public FogMode fogMode;
    public float fogDensity;
    public float fogStartDistance;
    public float fogEndDistance;
    public AmbientMode ambientMode;
    public Color ambientSkyColor;
    public Color ambientEquatorColor;
    public Color ambientGroundColor;
    public Color ambientLight;
    public float ambientIntensity;
    public Color subtractiveShadowColor;
    public string skyboxPath;
}

[Serializable]
public class XianxiaLightBackup
{
    public bool hadSun;
    public string hierarchyPath;
    public string name;
    public Color color;
    public float intensity;
    public float shadowStrength;
    public Vector3 rotation;
}

[Serializable]
public class XianxiaVolumeBackup
{
    public string hierarchyPath;
    public string name;
    public bool enabled;
    public bool isGlobal;
    public float priority;
    public float weight;
    public string profilePath;
}
