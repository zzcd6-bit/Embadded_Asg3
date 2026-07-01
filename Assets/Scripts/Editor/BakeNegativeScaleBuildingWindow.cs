using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BakeNegativeScaleBuildingWindow : EditorWindow
{
    private const string DefaultMeshFolder = "Assets/Generated/MirroredBakeMeshes";

    private GameObject sourceRoot;
    private string meshFolder = DefaultMeshFolder;
    private bool disableOriginal;
    private bool selectBakedObject = true;

    [MenuItem("Tools/Building/Bake Negative Scale Building")]
    private static void Open()
    {
        GetWindow<BakeNegativeScaleBuildingWindow>("Bake Negative Scale");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Bake Negative Scale Building", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Creates a baked copy, removes negative Transform scale from the copy, and bakes the old mirrored world shape into new mesh/collider data.",
            MessageType.Info);

        sourceRoot = (GameObject)EditorGUILayout.ObjectField("Source Root", sourceRoot, typeof(GameObject), true);
        meshFolder = EditorGUILayout.TextField("Generated Mesh Folder", meshFolder);
        disableOriginal = EditorGUILayout.Toggle("Disable Original After Bake", disableOriginal);
        selectBakedObject = EditorGUILayout.Toggle("Select Baked Object", selectBakedObject);

        using (new EditorGUI.DisabledScope(sourceRoot == null))
        {
            if (GUILayout.Button("Bake Copy With Positive Scale", GUILayout.Height(32f)))
            {
                Bake();
            }
        }
    }

    private void Bake()
    {
        if (sourceRoot == null)
        {
            return;
        }

        if (!meshFolder.StartsWith("Assets/"))
        {
            EditorUtility.DisplayDialog("Invalid mesh folder", "Generated Mesh Folder must be inside Assets/.", "OK");
            return;
        }

        EnsureFolder(meshFolder);

        GameObject bakedRoot = Instantiate(sourceRoot, sourceRoot.transform.parent);
        bakedRoot.name = sourceRoot.name + "_BakedPositiveScale";
        bakedRoot.transform.SetSiblingIndex(sourceRoot.transform.GetSiblingIndex() + 1);
        Undo.RegisterCreatedObjectUndo(bakedRoot, "Bake Negative Scale Building");

        MeshRecord[] meshRecords = CaptureMeshRecords(bakedRoot);
        BoxColliderRecord[] boxRecords = CaptureBoxColliderRecords(bakedRoot);

        MakeScalesPositive(bakedRoot.transform);

        int bakedMeshCount = BakeMeshes(meshRecords);
        int bakedBoxCount = BakeBoxColliders(boxRecords);

        if (disableOriginal)
        {
            Undo.RecordObject(sourceRoot, "Disable Original Mirrored Building");
            sourceRoot.SetActive(false);
            EditorUtility.SetDirty(sourceRoot);
        }

        EditorUtility.SetDirty(bakedRoot);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (selectBakedObject)
        {
            Selection.activeGameObject = bakedRoot;
        }

        EditorUtility.DisplayDialog(
            "Bake complete",
            $"Created: {bakedRoot.name}\nBaked meshes: {bakedMeshCount}\nAdjusted BoxColliders: {bakedBoxCount}",
            "OK");
    }

    private static MeshRecord[] CaptureMeshRecords(GameObject root)
    {
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        List<MeshRecord> records = new(filters.Length);

        foreach (MeshFilter filter in filters)
        {
            if (filter.sharedMesh == null)
            {
                continue;
            }

            records.Add(new MeshRecord
            {
                filter = filter,
                sourceMesh = filter.sharedMesh,
                oldLocalToWorld = filter.transform.localToWorldMatrix
            });
        }

        return records.ToArray();
    }

    private static BoxColliderRecord[] CaptureBoxColliderRecords(GameObject root)
    {
        BoxCollider[] colliders = root.GetComponentsInChildren<BoxCollider>(true);
        List<BoxColliderRecord> records = new(colliders.Length);

        foreach (BoxCollider box in colliders)
        {
            records.Add(new BoxColliderRecord
            {
                collider = box,
                oldLocalToWorld = box.transform.localToWorldMatrix,
                center = box.center,
                size = box.size
            });
        }

        return records.ToArray();
    }

    private static void MakeScalesPositive(Transform root)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform transform in transforms)
        {
            Vector3 scale = transform.localScale;
            transform.localScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }
    }

    private int BakeMeshes(MeshRecord[] records)
    {
        int bakedCount = 0;

        foreach (MeshRecord record in records)
        {
            Mesh sourceMesh = record.sourceMesh;
            Mesh bakedMesh;

            try
            {
                bakedMesh = Instantiate(sourceMesh);
            }
            catch
            {
                Debug.LogWarning($"Could not duplicate mesh for {record.filter.name}. Is the mesh readable?");
                continue;
            }

            Matrix4x4 delta = record.filter.transform.worldToLocalMatrix * record.oldLocalToWorld;
            Vector3[] vertices = bakedMesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = delta.MultiplyPoint3x4(vertices[i]);
            }

            bakedMesh.vertices = vertices;

            if (bakedMesh.normals != null && bakedMesh.normals.Length == vertices.Length)
            {
                Matrix4x4 normalMatrix = delta.inverse.transpose;
                Vector3[] normals = bakedMesh.normals;
                for (int i = 0; i < normals.Length; i++)
                {
                    normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
                }

                bakedMesh.normals = normals;
            }

            if (bakedMesh.tangents != null && bakedMesh.tangents.Length == vertices.Length)
            {
                Matrix4x4 tangentMatrix = delta.inverse.transpose;
                Vector4[] tangents = bakedMesh.tangents;
                float handedness = delta.determinant < 0f ? -1f : 1f;

                for (int i = 0; i < tangents.Length; i++)
                {
                    Vector3 tangent = tangentMatrix.MultiplyVector(new Vector3(tangents[i].x, tangents[i].y, tangents[i].z)).normalized;
                    tangents[i] = new Vector4(tangent.x, tangent.y, tangent.z, tangents[i].w * handedness);
                }

                bakedMesh.tangents = tangents;
            }

            if (delta.determinant < 0f)
            {
                FlipTriangleWinding(bakedMesh);
            }

            bakedMesh.RecalculateBounds();
            bakedMesh.name = sourceMesh.name + "_BakedPositiveScale";

            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{meshFolder}/{SanitizeFileName(record.filter.name)}_{SanitizeFileName(sourceMesh.name)}.asset");

            AssetDatabase.CreateAsset(bakedMesh, path);
            record.filter.sharedMesh = bakedMesh;
            bakedCount++;
        }

        return bakedCount;
    }

    private static int BakeBoxColliders(BoxColliderRecord[] records)
    {
        int bakedCount = 0;

        foreach (BoxColliderRecord record in records)
        {
            BoxCollider box = record.collider;
            if (box == null)
            {
                continue;
            }

            Matrix4x4 delta = box.transform.worldToLocalMatrix * record.oldLocalToWorld;
            Bounds bounds = BuildTransformedLocalBounds(record.center, record.size, delta);
            box.center = bounds.center;
            box.size = new Vector3(Mathf.Abs(bounds.size.x), Mathf.Abs(bounds.size.y), Mathf.Abs(bounds.size.z));
            bakedCount++;
        }

        return bakedCount;
    }

    private static Bounds BuildTransformedLocalBounds(Vector3 center, Vector3 size, Matrix4x4 delta)
    {
        Vector3 extents = size * 0.5f;
        Vector3 first = delta.MultiplyPoint3x4(center + new Vector3(-extents.x, -extents.y, -extents.z));
        Bounds bounds = new(first, Vector3.zero);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    bounds.Encapsulate(delta.MultiplyPoint3x4(corner));
                }
            }
        }

        return bounds;
    }

    private static void FlipTriangleWinding(Mesh mesh)
    {
        for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
        {
            int[] triangles = mesh.GetTriangles(subMesh);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                (triangles[i], triangles[i + 1]) = (triangles[i + 1], triangles[i]);
            }

            mesh.SetTriangles(triangles, subMesh);
        }
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

    private struct MeshRecord
    {
        public MeshFilter filter;
        public Mesh sourceMesh;
        public Matrix4x4 oldLocalToWorld;
    }

    private struct BoxColliderRecord
    {
        public BoxCollider collider;
        public Matrix4x4 oldLocalToWorld;
        public Vector3 center;
        public Vector3 size;
    }
}
