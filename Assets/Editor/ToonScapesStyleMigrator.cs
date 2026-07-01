using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ToonScapesStyleMigrator
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ReportPath = "Assets/ArtStyle/ToonScapesAlignmentReport.md";

    private static readonly string[] TargetMaterialFolders =
    {
        "Assets/Model/水寨/Material",
        "Assets/Rope_Bridge_Set/Models/Materials",
        "Assets/BK/PureNature_AsianMountains"
    };

    private static readonly string[] CharacterSkipTokens =
    {
        "marc", "stacy", "steve", "skin", "hair", "pants", "shirt", "shoes", "wrist", "glasses",
        "kachujin", "player"
    };

    private static readonly string[] BkAllowedTokens =
    {
        "/asiancliff/", "/asianpeak/", "/asianpeaksmall/", "/boulders/", "/rubble/", "/terrains/",
        "/textures/surfaces/"
    };

    [MenuItem("Tools/Art Style/Apply ToonScapes Alignment")]
    public static void ApplyFromMenu()
    {
        ApplyToonScapesAlignment();
    }

    public static void ApplyToonScapesAlignment()
    {
        Directory.CreateDirectory("Assets/ArtStyle");

        var report = new List<string>
        {
            "# ToonScapes Alignment Report",
            "",
            $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            "",
            "## Scene",
        };

        var materialStats = ConvertTargetMaterials(report);
        ApplySceneLook(report);

        report.Add("");
        report.Add("## Summary");
        report.Add($"- Converted materials: {materialStats.converted}");
        report.Add($"- Skipped materials: {materialStats.skipped}");
        report.Add($"- Errors: {materialStats.errors}");
        report.Add("");
        report.Add("## Manual Follow-Up Checklist");
        report.Add("- Open `Assets/Scenes/SampleScene.unity` and inspect the main playable camera angle.");
        report.Add("- For any BK cliffs that look too noisy, lower their material texture tiling or replace the cliff texture with a flatter hand-painted variant.");
        report.Add("- For water-village texture atlases with baked photo lighting, paint/blur the albedo maps or replace them with flatter color atlases.");
        report.Add("- Re-bake lighting or clear stale baked GI if shadows look inconsistent after changing the skybox and fog.");

        File.WriteAllLines(ReportPath, report);
        AssetDatabase.ImportAsset(ReportPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"ToonScapes alignment complete. Report: {ReportPath}");
    }

    private static (int converted, int skipped, int errors) ConvertTargetMaterials(List<string> report)
    {
        var surfaceShader = Shader.Find("ToonScapes/URP/Surface");
        var surfaceTopShader = Shader.Find("ToonScapes/URP/SurfaceTop");
        var stoneShader = Shader.Find("ToonScapes/URP/Stone");
        var vegetationShader = Shader.Find("ToonScapes/URP/Vegetation");

        if (surfaceShader == null || surfaceTopShader == null || stoneShader == null || vegetationShader == null)
        {
            throw new InvalidOperationException("Missing one or more ToonScapes shaders.");
        }

        var ramp = LoadAssetByGuid<Texture2D>("62ed797383387454f86cb4b985032c17");
        var paths = AssetDatabase.FindAssets("t:Material", TargetMaterialFolders)
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct()
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var converted = 0;
        var skipped = 0;
        var errors = 0;

        report.Add("");
        report.Add("## Materials");

        foreach (var path in paths)
        {
            var lowerPath = Normalize(path);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                skipped++;
                continue;
            }

            if (!ShouldProcess(path, material.name, out var skipReason))
            {
                skipped++;
                report.Add($"- Skipped `{path}`: {skipReason}");
                continue;
            }

            try
            {
                var originalTexture = FirstTexture(material, "_BaseMap", "_MainTex", "_MainTexture", "_Albedo", "_BaseColorMap", "_TextureSample");
                var originalNormal = FirstTexture(material, "_BumpMap", "_NormalMap", "_Normal", "_MainNormalMap", "_SurfaceNormalMap");
                var originalColor = FirstColor(material, "_BaseColor", "_Color", "_BaseTint");
                var kind = Classify(path, material.name);
                var shader = kind switch
                {
                    MaterialKind.Stone => stoneShader,
                    MaterialKind.Vegetation => vegetationShader,
                    MaterialKind.SurfaceTop => surfaceTopShader,
                    _ => surfaceShader
                };

                Undo.RecordObject(material, "Apply ToonScapes Alignment");
                material.shader = shader;

                ApplyCommonToonSettings(material, ramp, originalColor, originalTexture);

                switch (kind)
                {
                    case MaterialKind.Stone:
                        ApplyStoneSettings(material, originalTexture, originalNormal);
                        break;
                    case MaterialKind.Vegetation:
                        ApplyVegetationSettings(material, originalTexture, originalNormal);
                        break;
                    case MaterialKind.SurfaceTop:
                        ApplySurfaceSettings(material, originalTexture, originalNormal, 3f);
                        break;
                    default:
                        ApplySurfaceSettings(material, originalTexture, originalNormal, 1f);
                        break;
                }

                EditorUtility.SetDirty(material);
                converted++;
                report.Add($"- Converted `{path}` -> `{shader.name}`");
            }
            catch (Exception ex)
            {
                errors++;
                report.Add($"- Error `{path}`: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        return (converted, skipped, errors);
    }

    private static void ApplySceneLook(List<string> report)
    {
        if (!File.Exists(SampleScenePath))
        {
            report.Add("- SampleScene not found; scene settings were not changed.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);

        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.2117647f, 0.6664727f, 1f, 1f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 80f;
        RenderSettings.fogEndDistance = 480f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.white;
        RenderSettings.ambientEquatorColor = new Color(0.7276166f, 0.76220477f, 0.7830189f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.5283019f, 0.5283019f, 0.5283019f, 1f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.subtractiveShadowColor = new Color(0.42f, 0.478f, 0.627f, 1f);
        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/ToonScapes/Spring Isles/Skybox/Materials/TSI_Skybox_01A.mat")
            ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/ToonScapes/Spring Isles/Skybox/Materials/TSI_Skybox_02A.mat");

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/ToonScapes/Shared Assets/Volume Profiles/TS_Profile_Sunny_01.asset");
        if (profile != null)
        {
            var volume = FindOrCreateGlobalVolume();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;
        }

        var sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude).FirstOrDefault(l => l.type == LightType.Directional);
        if (sun == null)
        {
            var go = new GameObject("ToonScapes Main Light");
            sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.name = "ToonScapes Main Light";
        sun.color = new Color(1f, 0.95686275f, 0.8392157f, 1f);
        sun.intensity = 1.35f;
        sun.shadowStrength = 0.65f;
        sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        RenderSettings.sun = sun;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        report.Add("- Applied ToonScapes-style fog, trilight ambient color, skybox, global sunny volume, and main directional light to SampleScene.");
    }

    private static Volume FindOrCreateGlobalVolume()
    {
        var existing = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Exclude)
            .FirstOrDefault(v => v.name == "ToonScapes Global Volume");
        if (existing != null)
        {
            return existing;
        }

        var go = new GameObject("ToonScapes Global Volume");
        return go.AddComponent<Volume>();
    }

    private static bool ShouldProcess(string path, string materialName, out string reason)
    {
        var lowerPath = Normalize(path);
        var lowerName = materialName.ToLowerInvariant();

        if (CharacterSkipTokens.Any(token => lowerName.Contains(token) || lowerPath.Contains(token)))
        {
            reason = "character/player-like material";
            return false;
        }

        if (lowerPath.Contains("/water/") || lowerPath.Contains("/waterfalls/") || lowerName.Contains("water"))
        {
            reason = "water material intentionally excluded";
            return false;
        }

        if (lowerPath.StartsWith("assets/bk/"))
        {
            if (!BkAllowedTokens.Any(lowerPath.Contains))
            {
                reason = "BK asset outside cliff/peak/rock/terrain scope";
                return false;
            }

            if (lowerPath.Contains("/trees/") || lowerPath.Contains("/plants/") || lowerPath.Contains("/fx/") || lowerPath.Contains("/sky/"))
            {
                reason = "BK foliage/fx/sky intentionally excluded";
                return false;
            }
        }

        reason = "";
        return true;
    }

    private static MaterialKind Classify(string path, string materialName)
    {
        var text = Normalize(path + "/" + materialName);

        if (text.Contains("asiancliff") || text.Contains("asianpeak") || text.Contains("boulder") ||
            text.Contains("rubble") || text.Contains("rock") || text.Contains("stone") ||
            text.Contains("granite") || text.Contains("slate") || text.Contains("concrete") ||
            text.Contains("砖") || text.Contains("石") || text.Contains("墙") || text.Contains("瓦"))
        {
            return MaterialKind.Stone;
        }

        if (text.Contains("terrain") || text.Contains("floor") || text.Contains("地面") || text.Contains("sand") || text.Contains("terrazzo"))
        {
            return MaterialKind.SurfaceTop;
        }

        if (text.Contains("bamboo") || text.Contains("竹") || text.Contains("leaf") || text.Contains("leaves") ||
            text.Contains("grass") || text.Contains("shrub") || text.Contains("plant") || text.Contains("旗"))
        {
            return MaterialKind.Vegetation;
        }

        return MaterialKind.Surface;
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

    private static void ApplyStoneSettings(Material material, Texture texture, Texture normal)
    {
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
    }

    private static void ApplyVegetationSettings(Material material, Texture texture, Texture normal)
    {
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
    }

    private static Texture FirstTexture(Material material, params string[] names)
    {
        foreach (var name in names)
        {
            if (material.HasProperty(name))
            {
                var texture = material.GetTexture(name);
                if (texture != null)
                {
                    return texture;
                }
            }
        }

        return null;
    }

    private static Color FirstColor(Material material, params string[] names)
    {
        foreach (var name in names)
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
        var path = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static bool IsNearlyWhite(Color color)
    {
        return Mathf.Abs(color.r - 1f) < 0.02f &&
               Mathf.Abs(color.g - 1f) < 0.02f &&
               Mathf.Abs(color.b - 1f) < 0.02f &&
               color.a > 0.98f;
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
        Vegetation
    }
}
