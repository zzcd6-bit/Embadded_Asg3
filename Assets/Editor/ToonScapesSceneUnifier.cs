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

public static class ToonScapesSceneUnifier
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ReportPath = "Assets/ArtStyle/ToonScapesFullSceneReport.md";
    private const string ToonVolumeName = "ToonScapes Global Volume";
    private const string ToonMainLightName = "ToonScapes Main Light";
    private const string XianxiaVolumeName = "Xianxia Global Volume";
    private const string XianxiaMainLightName = "Xianxia Main Light";
    private const string XianxiaProfilePath = "Assets/ArtStyle/XianxiaLookProfile.asset";
    private const string ToonVolumeProfilePath = "Assets/ToonScapes/Shared Assets/Volume Profiles/TS_Profile_Sunny_01.asset";

    private static readonly string[] ToonAssetPrefixes =
    {
        "assets/toonscapes/"
    };

    private static readonly string[] ProtectedAssetPrefixes =
    {
        "assets/dynamicfogurp/",
        "assets/tutorialinfo/"
    };

    [MenuItem("Tools/Art Style/Unify Entire Scene To ToonScapes")]
    public static void UnifyFromMenu()
    {
        UnifyScene();
    }

    public static void UnifyScene()
    {
        Directory.CreateDirectory("Assets/ArtStyle");

        string backupPath = BackupSceneFile();
        var report = new List<string>
        {
            "# ToonScapes Full Scene Report",
            "",
            $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"Scene backup: `{backupPath}`",
            "",
            "## Scene Look"
        };

        Scene scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        RemoveXianxiaLookObjects(report);
        ApplyRenderSettings();
        ApplyToonScapesVolume(report);
        ApplyMainLight(report);
        EnableCameraPostProcessing(report);

        var materials = CollectSceneMaterials(report);
        var stats = ConvertMaterials(materials, report);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        report.Add("");
        report.Add("## Summary");
        report.Add($"- Scene materials converted: {stats.converted}");
        report.Add($"- Already ToonScapes materials: {stats.alreadyToon}");
        report.Add($"- Skipped/protected materials: {stats.skipped}");
        report.Add($"- Conversion errors: {stats.errors}");
        report.Add("");
        report.Add("## Notes");
        report.Add("- Bamboo fog scripts, Bamboo fog zones, and Dynamic Fog volumes were not modified.");
        report.Add("- If a texture atlas still looks photographic, repainting the albedo is the next step; shader conversion alone cannot remove baked photo lighting.");

        File.WriteAllLines(ReportPath, report);
        AssetDatabase.ImportAsset(ReportPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Full ToonScapes scene unification complete. Report: {ReportPath}");
    }

    private static string BackupSceneFile()
    {
        string backupFolder = $"Assets/ArtStyle/Backups/BeforeFullToonScapes_{DateTime.Now:yyyyMMdd_HHmmss}";
        Directory.CreateDirectory(backupFolder);
        string backupPath = $"{backupFolder}/SampleScene.unity";
        File.Copy(SampleScenePath, backupPath, true);
        AssetDatabase.ImportAsset(backupPath);
        return backupPath;
    }

    private static void RemoveXianxiaLookObjects(List<string> report)
    {
        var xianxiaProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(XianxiaProfilePath);
        int removedVolumes = 0;
        foreach (Volume volume in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include)
                     .Where(v => v.name == XianxiaVolumeName || (xianxiaProfile != null && v.sharedProfile == xianxiaProfile))
                     .ToArray())
        {
            UnityEngine.Object.DestroyImmediate(volume.gameObject);
            removedVolumes++;
        }

        int removedLights = 0;
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include)
                     .Where(l => l.name == XianxiaMainLightName)
                     .ToArray())
        {
            if (RenderSettings.sun == light)
            {
                RenderSettings.sun = null;
            }

            UnityEngine.Object.DestroyImmediate(light.gameObject);
            removedLights++;
        }

        report.Add($"- Removed Xianxia look residues: {removedLights} light(s), {removedVolumes} volume(s).");
    }

    private static void ApplyRenderSettings()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.2117647f, 0.6664727f, 1f, 1f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 80f;
        RenderSettings.fogEndDistance = 480f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.white;
        RenderSettings.ambientEquatorColor = new Color(0.7276166f, 0.76220477f, 0.7830189f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.5283019f, 0.5283019f, 0.5283019f, 1f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.subtractiveShadowColor = new Color(0.42f, 0.478f, 0.627f, 1f);

        Material skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/ToonScapes/Spring Isles/Skybox/Materials/TSI_Skybox_01A.mat")
            ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/ToonScapes/Spring Isles/Skybox/Materials/TSI_Skybox_02A.mat");
        if (skybox != null)
        {
            RenderSettings.skybox = skybox;
        }
    }

    private static void ApplyToonScapesVolume(List<string> report)
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ToonVolumeProfilePath);
        if (profile == null)
        {
            report.Add($"- ToonScapes volume profile missing: `{ToonVolumeProfilePath}`.");
            return;
        }

        Volume volume = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include)
            .FirstOrDefault(v => v.name == ToonVolumeName);
        if (volume == null)
        {
            var go = new GameObject(ToonVolumeName);
            volume = go.AddComponent<Volume>();
        }

        volume.enabled = true;
        volume.isGlobal = true;
        volume.priority = 20f;
        volume.weight = 1f;
        volume.sharedProfile = profile;
        report.Add("- Applied ToonScapes global post-processing profile.");
    }

    private static void ApplyMainLight(List<string> report)
    {
        Light sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include)
            .FirstOrDefault(l => l.name == ToonMainLightName && l.type == LightType.Directional);

        if (sun == null)
        {
            sun = RenderSettings.sun != null && RenderSettings.sun.type == LightType.Directional
                ? RenderSettings.sun
                : UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include)
                    .FirstOrDefault(l => l.type == LightType.Directional);
        }

        if (sun == null)
        {
            var go = new GameObject(ToonMainLightName);
            sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.name = ToonMainLightName;
        sun.enabled = true;
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.95686275f, 0.8392157f, 1f);
        sun.intensity = 1.35f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.65f;
        sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        RenderSettings.sun = sun;

        int disabledExtraDirectionalLights = 0;
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include)
                     .Where(l => l != sun && l.type == LightType.Directional))
        {
            light.enabled = false;
            disabledExtraDirectionalLights++;
        }

        report.Add($"- Applied ToonScapes main light. Disabled {disabledExtraDirectionalLights} extra directional light(s).");
    }

    private static void EnableCameraPostProcessing(List<string> report)
    {
        int enabled = 0;
        foreach (Camera camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            UniversalAdditionalCameraData data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (data == null)
            {
                continue;
            }

            data.renderPostProcessing = true;
            enabled++;
        }

        report.Add($"- Enabled URP post-processing on {enabled} camera(s).");
    }

    private static List<Material> CollectSceneMaterials(List<string> report)
    {
        var materials = new HashSet<Material>();

        foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
        {
            if (renderer == null || IsDynamicFogObject(renderer.gameObject))
            {
                continue;
            }

            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null)
                {
                    materials.Add(material);
                }
            }
        }

        foreach (Terrain terrain in UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include))
        {
            if (terrain.materialTemplate != null)
            {
                materials.Add(terrain.materialTemplate);
            }
        }

        report.Add($"- Collected {materials.Count} unique scene material(s).");
        return materials.OrderBy(m => AssetDatabase.GetAssetPath(m), StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static (int converted, int alreadyToon, int skipped, int errors) ConvertMaterials(IEnumerable<Material> materials, List<string> report)
    {
        Shader surfaceShader = Shader.Find("ToonScapes/URP/Surface");
        Shader surfaceTopShader = Shader.Find("ToonScapes/URP/SurfaceTop");
        Shader stoneShader = Shader.Find("ToonScapes/URP/Stone");
        Shader vegetationShader = Shader.Find("ToonScapes/URP/Vegetation");
        Shader waterShader = Shader.Find("ToonScapes/URP/Water");
        Shader waterfallShader = Shader.Find("ToonScapes/URP/Waterfall");
        Shader backgroundShader = Shader.Find("ToonScapes/URP/Background");

        if (surfaceShader == null || surfaceTopShader == null || stoneShader == null || vegetationShader == null)
        {
            throw new InvalidOperationException("Missing one or more required ToonScapes shaders.");
        }

        Texture2D ramp = LoadAssetByGuid<Texture2D>("62ed797383387454f86cb4b985032c17");
        int converted = 0;
        int alreadyToon = 0;
        int skipped = 0;
        int errors = 0;

        report.Add("");
        report.Add("## Materials");

        foreach (Material material in materials)
        {
            string path = AssetDatabase.GetAssetPath(material);
            string lowerPath = Normalize(path);
            string shaderName = material.shader != null ? material.shader.name : "";

            if (string.IsNullOrEmpty(path) || path.StartsWith("Resources/unity_builtin_extra", StringComparison.OrdinalIgnoreCase))
            {
                skipped++;
                report.Add($"- Skipped `{material.name}`: built-in or scene-only material.");
                continue;
            }

            if (ToonAssetPrefixes.Any(lowerPath.StartsWith) || shaderName.StartsWith("ToonScapes/", StringComparison.OrdinalIgnoreCase))
            {
                alreadyToon++;
                continue;
            }

            if (ProtectedAssetPrefixes.Any(lowerPath.StartsWith) || IsProtectedShader(shaderName))
            {
                skipped++;
                report.Add($"- Skipped `{path}`: protected system material or shader `{shaderName}`.");
                continue;
            }

            try
            {
                Texture mainTexture = FirstTexture(material, "_BaseMap", "_MainTex", "_MainTexture", "_Albedo", "_BaseColorMap", "_TextureSample", "_Diffuse");
                Texture normalTexture = FirstTexture(material, "_BumpMap", "_NormalMap", "_Normal", "_MainNormalMap", "_SurfaceNormalMap");
                Color color = FirstColor(material, "_BaseColor", "_Color", "_BaseTint", "_Tint");
                MaterialKind kind = Classify(path, material.name, shaderName);
                Shader shader = kind switch
                {
                    MaterialKind.Waterfall => waterfallShader ?? waterShader ?? surfaceShader,
                    MaterialKind.Water => waterShader ?? surfaceShader,
                    MaterialKind.Background => backgroundShader ?? surfaceShader,
                    MaterialKind.Stone => stoneShader,
                    MaterialKind.Vegetation => vegetationShader,
                    MaterialKind.SurfaceTop => surfaceTopShader,
                    _ => surfaceShader
                };

                Undo.RecordObject(material, "Unify Scene To ToonScapes");
                material.shader = shader;
                ApplyCommonToonSettings(material, ramp, color, mainTexture);
                ApplyKindSettings(material, kind, mainTexture, normalTexture);
                EditorUtility.SetDirty(material);

                converted++;
                report.Add($"- Converted `{path}` -> `{shader.name}` ({kind}).");
            }
            catch (Exception ex)
            {
                errors++;
                report.Add($"- Error `{path}`: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        return (converted, alreadyToon, skipped, errors);
    }

    private static bool IsDynamicFogObject(GameObject gameObject)
    {
        Transform current = gameObject.transform;
        while (current != null)
        {
            string name = current.name.ToLowerInvariant();
            if (name.Contains("dynamic fog") || name.Contains("bamboo fog"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool IsProtectedShader(string shaderName)
    {
        string name = shaderName.ToLowerInvariant();
        return name.Contains("dynamicfog")
            || name.Contains("particle")
            || name.Contains("sprite")
            || name.Contains("textmeshpro")
            || name.Contains("ui/");
    }

    private static MaterialKind Classify(string path, string materialName, string shaderName)
    {
        string text = Normalize($"{path}/{materialName}/{shaderName}");

        if (text.Contains("waterfall"))
        {
            return MaterialKind.Waterfall;
        }

        if (text.Contains("water") || text.Contains("river") || text.Contains("lake"))
        {
            return MaterialKind.Water;
        }

        if (text.Contains("background") || text.Contains("/bg_") || text.Contains("skyline"))
        {
            return MaterialKind.Background;
        }

        if (text.Contains("asiancliff") || text.Contains("asianpeak") || text.Contains("mountain") ||
            text.Contains("boulder") || text.Contains("rubble") || text.Contains("rock") ||
            text.Contains("stone") || text.Contains("granite") || text.Contains("slate") ||
            text.Contains("cliff") || text.Contains("wall"))
        {
            return MaterialKind.Stone;
        }

        if (text.Contains("terrain") || text.Contains("floor") || text.Contains("ground") ||
            text.Contains("sand") || text.Contains("path") || text.Contains("road"))
        {
            return MaterialKind.SurfaceTop;
        }

        if (text.Contains("bamboo") || text.Contains("leaf") || text.Contains("leaves") ||
            text.Contains("grass") || text.Contains("shrub") || text.Contains("plant") ||
            text.Contains("tree") || text.Contains("branch") || text.Contains("flower") ||
            text.Contains("blossom"))
        {
            return MaterialKind.Vegetation;
        }

        return MaterialKind.Surface;
    }

    private static void ApplyKindSettings(Material material, MaterialKind kind, Texture texture, Texture normal)
    {
        switch (kind)
        {
            case MaterialKind.Stone:
                SetTextureIf(material, "_SurfaceTexture", texture);
                SetTextureIf(material, "_OverlayTexture", texture);
                SetTextureIf(material, "_MainTexture", texture);
                SetTextureIf(material, "_MainTex", texture);
                SetTextureIf(material, "_SurfaceNormalMap", normal);
                SetTextureIf(material, "_OverlayNormalMap", normal);
                SetFloatIf(material, "_SurfaceTextureTiling", 3.5f);
                SetFloatIf(material, "_OverlayTextureTiling", 4f);
                SetFloatIf(material, "_EnableMacroNormals", 0f);
                SetFloatIf(material, "_EnableCoverage", 0f);
                SetFloatIf(material, "_AmbientOcclusion", 0.35f);
                SetFloatIf(material, "_Occlusion1", 1f);
                SetFloatIf(material, "_EdgeWear1", 0.35f);
                break;
            case MaterialKind.Vegetation:
                SetTextureIf(material, "_MainTexture", texture);
                SetTextureIf(material, "_MainTex", texture);
                SetTextureIf(material, "_NormalMap", normal);
                SetFloatIf(material, "_AlphaClipThreshold", 0.35f);
                SetFloatIf(material, "_Cutoff", 0.35f);
                SetFloatIf(material, "_EnableWind", 1f);
                SetFloatIf(material, "_EnableSubsurfaceDistortion", 1f);
                SetFloatIf(material, "_DistortionScale", 0.15f);
                SetFloatIf(material, "_DistortionAmount", 0.08f);
                SetFloatIf(material, "_SpecularIntensity", 0.12f);
                break;
            case MaterialKind.Water:
            case MaterialKind.Waterfall:
                SetTextureIf(material, "_MainTexture", texture);
                SetTextureIf(material, "_MainTex", texture);
                SetTextureIf(material, "_NormalMap", normal);
                SetColorIf(material, "_BaseTint", new Color(0.25f, 0.72f, 0.95f, 0.72f));
                SetColorIf(material, "_Color", new Color(0.25f, 0.72f, 0.95f, 0.72f));
                SetFloatIf(material, "_EnableFoam", 1f);
                SetFloatIf(material, "_FoamIntensity", 0.45f);
                break;
            case MaterialKind.SurfaceTop:
                ApplySurfaceSettings(material, texture, normal, 3f);
                break;
            default:
                ApplySurfaceSettings(material, texture, normal, 1f);
                break;
        }
    }

    private static void ApplyCommonToonSettings(Material material, Texture2D ramp, Color tint, Texture texture)
    {
        SetTextureIf(material, "_TextureRamp", ramp);
        SetFloatIf(material, "_RampScale", 0.5f);
        SetFloatIf(material, "_RampOffset", 0.5f);
        SetFloatIf(material, "_ReceiveShadows", 1f);
        SetFloatIf(material, "_EnableColorTint", texture == null || !IsNearlyWhite(tint) ? 1f : 0f);
        SetColorIf(material, "_BaseTint", tint);
        SetColorIf(material, "_Color", tint);
        SetFloatIf(material, "_EnableEmission", 0f);
        SetFloatIf(material, "_EnableRimLighting", 0f);
        SetFloatIf(material, "_EnableRimLighting1", 0f);
        SetFloatIf(material, "_EnableSpecularHighlights", 1f);
        SetFloatIf(material, "_SpecularIntensity", 0.18f);
        SetFloatIf(material, "_SpecularSize", 0.18f);
        SetFloatIf(material, "_SpecularSize1", 0.18f);
        SetFloatIf(material, "_SpecularSmoothness", 0.08f);
        SetFloatIf(material, "_SpecularSmoothness1", 0.08f);
        SetFloatIf(material, "_NormalMapInfluence", 0.35f);
    }

    private static void ApplySurfaceSettings(Material material, Texture texture, Texture normal, float tiling)
    {
        SetTextureIf(material, "_MainTexture", texture);
        SetTextureIf(material, "_MainTex", texture);
        SetTextureIf(material, "_NormalMap", normal);
        SetFloatIf(material, "_MainTextureTiling", tiling);
        SetFloatIf(material, "_Occlusion", 1f);
        SetFloatIf(material, "_RimIntensity1", 0.2f);
        SetFloatIf(material, "_RimSpread", 0.25f);
    }

    private static Texture FirstTexture(Material material, params string[] names)
    {
        foreach (string name in names)
        {
            if (!material.HasProperty(name))
            {
                continue;
            }

            Texture texture = material.GetTexture(name);
            if (texture != null)
            {
                return texture;
            }
        }

        return null;
    }

    private static Color FirstColor(Material material, params string[] names)
    {
        foreach (string name in names)
        {
            if (material.HasProperty(name))
            {
                return material.GetColor(name);
            }
        }

        return Color.white;
    }

    private static void SetTextureIf(Material material, string property, Texture texture)
    {
        if (texture != null && material.HasProperty(property))
        {
            material.SetTexture(property, texture);
        }
    }

    private static void SetFloatIf(Material material, string property, float value)
    {
        if (material.HasProperty(property))
        {
            material.SetFloat(property, value);
        }
    }

    private static void SetColorIf(Material material, string property, Color value)
    {
        if (material.HasProperty(property))
        {
            material.SetColor(property, value);
        }
    }

    private static T LoadAssetByGuid<T>(string guid) where T : UnityEngine.Object
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static bool IsNearlyWhite(Color color)
    {
        return Mathf.Abs(color.r - 1f) < 0.02f
            && Mathf.Abs(color.g - 1f) < 0.02f
            && Mathf.Abs(color.b - 1f) < 0.02f
            && color.a > 0.98f;
    }

    private static string Normalize(string value)
    {
        return value.Replace('\\', '/').ToLowerInvariant();
    }

    private enum MaterialKind
    {
        Surface,
        SurfaceTop,
        Stone,
        Vegetation,
        Water,
        Waterfall,
        Background
    }
}
