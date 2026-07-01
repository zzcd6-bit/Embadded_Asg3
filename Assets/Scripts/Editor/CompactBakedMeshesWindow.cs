using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class CompactBakedMeshesWindow : EditorWindow
{
    private const string DefaultSourceMeshFolder = "Assets/Generated/MirroredBakeMeshes";
    private const string DefaultOutputMeshFolder = "Assets/Generated/MirroredBakeMeshes_Compact";
    private const float HashPrecision = 100000f;

    private GameObject sourceRoot;
    private string sourceMeshFolder = DefaultSourceMeshFolder;
    private string outputMeshFolder = DefaultOutputMeshFolder;
    private bool skipTransformsWithChildren = true;
    private bool disableSourceAfterCompact;
    private Vector2 scroll;
    private string analysisText = "No analysis yet.";
    private AnalysisStats lastAnalysis;

    [MenuItem("Tools/Building/Compact Baked Meshes")]
    private static void Open()
    {
        GetWindow<CompactBakedMeshesWindow>("Compact Baked Meshes");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Compact Baked Meshes", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Creates a compact copy of a baked building. Repeated meshes are normalized around their bounds center, moved back through Transform offsets, and shared when their shape data matches.",
            MessageType.Info);

        sourceRoot = (GameObject)EditorGUILayout.ObjectField("Source Root", sourceRoot, typeof(GameObject), true);
        sourceMeshFolder = EditorGUILayout.TextField("Source Mesh Folder", sourceMeshFolder);
        outputMeshFolder = EditorGUILayout.TextField("Output Mesh Folder", outputMeshFolder);
        skipTransformsWithChildren = EditorGUILayout.Toggle("Skip Mesh Transforms With Children", skipTransformsWithChildren);
        disableSourceAfterCompact = EditorGUILayout.Toggle("Disable Source After Compact", disableSourceAfterCompact);

        using (new EditorGUI.DisabledScope(sourceRoot == null))
        {
            if (GUILayout.Button("Analyze Selected Root", GUILayout.Height(28f)))
            {
                Analyze();
            }

            if (GUILayout.Button("Create Compacted Copy", GUILayout.Height(32f)))
            {
                CompactCopy();
            }
        }

        EditorGUILayout.LabelField("Analysis", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(analysisText, GUILayout.MinHeight(90f));
        EditorGUILayout.EndScrollView();
    }

    private void Analyze()
    {
        if (!ValidateInputs())
        {
            return;
        }

        try
        {
            lastAnalysis = GatherAnalysisStats();
            analysisText = FormatAnalysis(lastAnalysis);
        }
        catch (System.Exception exception)
        {
            analysisText = "Analysis failed:\n" + exception.Message;
        }
    }

    private void CompactCopy()
    {
        if (!ValidateInputs())
        {
            return;
        }

        AnalysisStats stats;
        try
        {
            stats = GatherAnalysisStats();
            lastAnalysis = stats;
            analysisText = FormatAnalysis(stats);
        }
        catch (System.Exception exception)
        {
            EditorUtility.DisplayDialog("Analysis failed", exception.Message, "OK");
            return;
        }

        if (stats.EligibleFilters == 0)
        {
            EditorUtility.DisplayDialog("Nothing to compact", "No eligible generated mesh filters were found under the selected root.", "OK");
            return;
        }

        float sharingRatio = stats.SharedCandidates / Mathf.Max(1f, stats.EligibleFilters);
        if (stats.SharedCandidates == 0 || sharingRatio < 0.1f)
        {
            bool shouldContinue = EditorUtility.DisplayDialog(
                "Low compaction potential",
                $"Only {stats.SharedCandidates} of {stats.EligibleFilters} eligible mesh references look shareable.\n\n" +
                "This probably will not reduce the project size much. Continue anyway?",
                "Continue",
                "Cancel");

            if (!shouldContinue)
            {
                return;
            }
        }

        if (stats.GeneratedMeshFilters > 2000)
        {
            bool shouldContinue = EditorUtility.DisplayDialog(
                "Large building root",
                $"This root contains {stats.GeneratedMeshFilters} generated mesh references.\n\n" +
                "The tool has a cancelable progress bar, but the operation can still take a while. Continue?",
                "Continue",
                "Cancel");

            if (!shouldContinue)
            {
                return;
            }
        }

        EnsureFolder(outputMeshFolder);

        GameObject compactRoot = Instantiate(sourceRoot, sourceRoot.transform.parent);
        compactRoot.name = sourceRoot.name + "_CompactSharedMeshes";
        compactRoot.transform.SetSiblingIndex(sourceRoot.transform.GetSiblingIndex() + 1);
        Undo.RegisterCreatedObjectUndo(compactRoot, "Create Compacted Baked Mesh Copy");

        MeshFilter[] filters = compactRoot.GetComponentsInChildren<MeshFilter>(true);
        Dictionary<string, Mesh> meshByHash = new();
        Dictionary<string, string> pathByHash = new();

        int generatedMeshFilters = 0;
        int compactedFilters = 0;
        int skippedFilters = 0;
        int createdMeshAssets = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < filters.Length; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Compacting baked meshes",
                        $"{i + 1}/{filters.Length}: {filters[i].name}",
                        filters.Length == 0 ? 1f : (float)(i + 1) / filters.Length))
                {
                    throw new System.OperationCanceledException("Compaction cancelled by user.");
                }

                MeshFilter filter = filters[i];
                if (!TryGetGeneratedMesh(filter, out Mesh sourceMesh, out _))
                {
                    continue;
                }

                generatedMeshFilters++;
                if (ShouldSkipFilter(filter))
                {
                    skippedFilters++;
                    continue;
                }

                Vector3 offset = sourceMesh.bounds.center;
                MoveMeshOffsetIntoTransform(filter.transform, offset);
                AdjustCollidersOnTransform(filter.transform, offset, sourceMesh);

                string hash = BuildNormalizedMeshHash(sourceMesh);
                if (!meshByHash.TryGetValue(hash, out Mesh compactMesh))
                {
                    compactMesh = CreateNormalizedMesh(sourceMesh, offset);
                    string path = AssetDatabase.GenerateUniqueAssetPath(
                        $"{outputMeshFolder}/{SanitizeFileName(sourceMesh.name)}_Compact.asset");

                    AssetDatabase.CreateAsset(compactMesh, path);
                    meshByHash.Add(hash, compactMesh);
                    pathByHash.Add(hash, path);
                    createdMeshAssets++;
                }

                filter.sharedMesh = compactMesh;

                foreach (MeshCollider meshCollider in filter.GetComponents<MeshCollider>())
                {
                    if (meshCollider.sharedMesh == sourceMesh)
                    {
                        meshCollider.sharedMesh = compactMesh;
                    }
                }

                compactedFilters++;
            }
        }
        catch (System.OperationCanceledException exception)
        {
            Debug.LogWarning(exception.Message);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        if (disableSourceAfterCompact)
        {
            Undo.RecordObject(sourceRoot, "Disable Source Baked Building");
            sourceRoot.SetActive(false);
            EditorUtility.SetDirty(sourceRoot);
        }

        EditorUtility.SetDirty(compactRoot);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = compactRoot;

        analysisText =
            $"Created compact root: {compactRoot.name}\n" +
            $"Generated mesh filters found: {generatedMeshFilters}\n" +
            $"Compacted filters: {compactedFilters}\n" +
            $"Skipped filters: {skippedFilters}\n" +
            $"Created shared mesh assets: {createdMeshAssets}\n" +
            $"Output folder: {outputMeshFolder}\n\n" +
            "Inspect the compact copy before deleting the old baked root or old mesh folder.";

        EditorUtility.DisplayDialog("Compaction complete", analysisText, "OK");
    }

    private AnalysisStats GatherAnalysisStats()
    {
        MeshFilter[] filters = sourceRoot.GetComponentsInChildren<MeshFilter>(true);
        Dictionary<string, int> hashCounts = new();
        AnalysisStats stats = new()
        {
            SourceRoot = sourceRoot
        };

        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            if (!TryGetGeneratedMesh(filter, out Mesh mesh, out string assetPath))
            {
                continue;
            }

            stats.GeneratedMeshFilters++;
            stats.CurrentBytesCountedPerFilter += GetAssetFileSize(assetPath);

            if (ShouldSkipFilter(filter))
            {
                stats.SkippedFilters++;
                continue;
            }

            stats.EligibleFilters++;
            string hash = BuildNormalizedMeshHash(mesh);
            hashCounts.TryAdd(hash, 0);
            hashCounts[hash]++;
        }

        stats.UniqueNormalizedMeshes = hashCounts.Count;
        stats.SharedCandidates = Mathf.Max(0, stats.EligibleFilters - stats.UniqueNormalizedMeshes);
        return stats;
    }

    private static string FormatAnalysis(AnalysisStats stats)
    {
        return
            $"Source root: {(stats.SourceRoot != null ? stats.SourceRoot.name : "<missing>")}\n" +
            $"Generated mesh filters under root: {stats.GeneratedMeshFilters}\n" +
            $"Eligible filters: {stats.EligibleFilters}\n" +
            $"Skipped filters: {stats.SkippedFilters}\n" +
            $"Unique normalized meshes: {stats.UniqueNormalizedMeshes}\n" +
            $"Potential shared references: {stats.SharedCandidates}\n" +
            $"Current referenced generated mesh bytes, counted per filter: {FormatBytes(stats.CurrentBytesCountedPerFilter)}\n\n" +
            "This is an estimate for sharing inside the selected root. The compact operation creates a copy and does not delete the old mesh folder.";
    }

    private bool ValidateInputs()
    {
        if (sourceRoot == null)
        {
            EditorUtility.DisplayDialog("Missing source root", "Assign a Source Root first.", "OK");
            return false;
        }

        if (!sourceMeshFolder.StartsWith("Assets/") || !outputMeshFolder.StartsWith("Assets/"))
        {
            EditorUtility.DisplayDialog("Invalid folder", "Source and output folders must be inside Assets/.", "OK");
            return false;
        }

        if (!AssetDatabase.IsValidFolder(sourceMeshFolder))
        {
            EditorUtility.DisplayDialog("Missing source folder", $"Could not find {sourceMeshFolder}.", "OK");
            return false;
        }

        return true;
    }

    private bool TryGetGeneratedMesh(MeshFilter filter, out Mesh mesh, out string assetPath)
    {
        mesh = filter.sharedMesh;
        assetPath = mesh != null ? AssetDatabase.GetAssetPath(mesh) : string.Empty;
        return mesh != null && assetPath.StartsWith(sourceMeshFolder + "/");
    }

    private bool ShouldSkipFilter(MeshFilter filter)
    {
        return skipTransformsWithChildren && filter.transform.childCount > 0;
    }

    private static void MoveMeshOffsetIntoTransform(Transform transform, Vector3 offset)
    {
        Vector3 localDelta = transform.localRotation * Vector3.Scale(transform.localScale, offset);
        transform.localPosition += localDelta;
    }

    private static void AdjustCollidersOnTransform(Transform transform, Vector3 offset, Mesh sourceMesh)
    {
        foreach (BoxCollider box in transform.GetComponents<BoxCollider>())
        {
            box.center -= offset;
        }

        foreach (SphereCollider sphere in transform.GetComponents<SphereCollider>())
        {
            sphere.center -= offset;
        }

        foreach (CapsuleCollider capsule in transform.GetComponents<CapsuleCollider>())
        {
            capsule.center -= offset;
        }
    }

    private static Mesh CreateNormalizedMesh(Mesh sourceMesh, Vector3 offset)
    {
        Mesh mesh = Instantiate(sourceMesh);
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= offset;
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.name = sourceMesh.name + "_Compact";
        return mesh;
    }

    private static string BuildNormalizedMeshHash(Mesh mesh)
    {
        Vector3 offset = mesh.bounds.center;

        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        writer.Write(mesh.subMeshCount);
        writer.Write(mesh.vertexCount);

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector4[] tangents = mesh.tangents;
        Vector2[] uv = mesh.uv;
        Color[] colors = mesh.colors;

        for (int i = 0; i < vertices.Length; i++)
        {
            WriteRounded(writer, vertices[i] - offset);

            if (normals != null && normals.Length == vertices.Length)
            {
                WriteRounded(writer, normals[i]);
            }

            if (tangents != null && tangents.Length == vertices.Length)
            {
                WriteRounded(writer, tangents[i]);
            }

            if (uv != null && uv.Length == vertices.Length)
            {
                WriteRounded(writer, uv[i]);
            }

            if (colors != null && colors.Length == vertices.Length)
            {
                WriteRounded(writer, colors[i]);
            }
        }

        for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
        {
            int[] triangles = mesh.GetTriangles(subMesh);
            writer.Write(triangles.Length);
            for (int i = 0; i < triangles.Length; i++)
            {
                writer.Write(triangles[i]);
            }
        }

        using SHA256 sha = SHA256.Create();
        return System.BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", string.Empty);
    }

    private static void WriteRounded(BinaryWriter writer, Vector2 value)
    {
        writer.Write(Mathf.RoundToInt(value.x * HashPrecision));
        writer.Write(Mathf.RoundToInt(value.y * HashPrecision));
    }

    private static void WriteRounded(BinaryWriter writer, Vector3 value)
    {
        writer.Write(Mathf.RoundToInt(value.x * HashPrecision));
        writer.Write(Mathf.RoundToInt(value.y * HashPrecision));
        writer.Write(Mathf.RoundToInt(value.z * HashPrecision));
    }

    private static void WriteRounded(BinaryWriter writer, Vector4 value)
    {
        writer.Write(Mathf.RoundToInt(value.x * HashPrecision));
        writer.Write(Mathf.RoundToInt(value.y * HashPrecision));
        writer.Write(Mathf.RoundToInt(value.z * HashPrecision));
        writer.Write(Mathf.RoundToInt(value.w * HashPrecision));
    }

    private static void WriteRounded(BinaryWriter writer, Color value)
    {
        writer.Write(Mathf.RoundToInt(value.r * HashPrecision));
        writer.Write(Mathf.RoundToInt(value.g * HashPrecision));
        writer.Write(Mathf.RoundToInt(value.b * HashPrecision));
        writer.Write(Mathf.RoundToInt(value.a * HashPrecision));
    }

    private static long GetAssetFileSize(string assetPath)
    {
        string fullPath = Path.GetFullPath(assetPath);
        return File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0L;
    }

    private static string FormatBytes(long bytes)
    {
        return bytes >= 1024L * 1024L
            ? $"{bytes / 1024f / 1024f:F1} MB"
            : $"{bytes / 1024f:F1} KB";
    }

    private static void EnsureFolder(string assetFolder)
    {
        string[] parts = assetFolder.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(value) ? "Mesh" : value;
    }

    private struct AnalysisStats
    {
        public GameObject SourceRoot;
        public int GeneratedMeshFilters;
        public int EligibleFilters;
        public int SkippedFilters;
        public int UniqueNormalizedMeshes;
        public int SharedCandidates;
        public long CurrentBytesCountedPerFilter;
    }
}
