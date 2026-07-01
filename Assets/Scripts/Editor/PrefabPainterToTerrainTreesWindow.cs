using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PrefabPainterToTerrainTreesWindow : EditorWindow
{
    private enum OriginalHandling
    {
        Keep,
        Disable,
        Delete
    }

    private GameObject sourceRoot;
    private Terrain targetTerrain;
    private OriginalHandling originalHandling = OriginalHandling.Disable;
    private bool includeInactive = true;
    private bool useOriginalY = true;
    private float bendFactor = 1f;

    [MenuItem("Tools/Vegetation/Prefab Painter To Terrain Trees")]
    private static void Open()
    {
        GetWindow<PrefabPainterToTerrainTreesWindow>("Prefab To Terrain Trees");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Convert Prefab Painter Objects", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Drag the parent object that contains painted prefab instances, choose the Terrain, then convert them into Terrain TreeInstances.",
            MessageType.Info);

        sourceRoot = (GameObject)EditorGUILayout.ObjectField("Painted Parent", sourceRoot, typeof(GameObject), true);
        targetTerrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", targetTerrain, typeof(Terrain), true);
        includeInactive = EditorGUILayout.Toggle("Include Inactive Children", includeInactive);
        useOriginalY = EditorGUILayout.Toggle("Preserve Original Y", useOriginalY);
        bendFactor = EditorGUILayout.FloatField("New Prototype Bend Factor", bendFactor);
        originalHandling = (OriginalHandling)EditorGUILayout.EnumPopup("Original Objects", originalHandling);

        using (new EditorGUI.DisabledScope(sourceRoot == null || targetTerrain == null))
        {
            if (GUILayout.Button("Convert Children To Terrain Trees", GUILayout.Height(32f)))
            {
                Convert();
            }
        }
    }

    private void Convert()
    {
        TerrainData terrainData = targetTerrain.terrainData;
        if (terrainData == null)
        {
            EditorUtility.DisplayDialog("Conversion failed", "The target Terrain has no TerrainData.", "OK");
            return;
        }

        List<GameObject> instances = CollectPrefabInstanceRoots(sourceRoot, includeInactive);
        if (instances.Count == 0)
        {
            EditorUtility.DisplayDialog("Nothing to convert", "No prefab instance roots were found under the painted parent.", "OK");
            return;
        }

        Undo.RegisterCompleteObjectUndo(terrainData, "Convert Prefab Painter Objects To Terrain Trees");
        Undo.RegisterFullObjectHierarchyUndo(sourceRoot, "Convert Prefab Painter Objects To Terrain Trees");

        TreePrototype[] prototypes = terrainData.treePrototypes;
        List<TreePrototype> prototypeList = new List<TreePrototype>(prototypes);
        Dictionary<GameObject, int> prototypeIndices = BuildPrototypeIndexMap(prototypeList);

        List<TreeInstance> treeInstances = new List<TreeInstance>(terrainData.treeInstances);
        List<GameObject> convertedObjects = new List<GameObject>();
        int convertedCount = 0;
        int skippedCount = 0;

        foreach (GameObject instance in instances)
        {
            GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            if (prefab == null)
            {
                skippedCount++;
                continue;
            }

            if (!TryGetOrCreatePrototypeIndex(prefab, prototypeList, prototypeIndices, out int prototypeIndex))
            {
                skippedCount++;
                continue;
            }

            if (!TryCreateTreeInstance(instance.transform, targetTerrain, prototypeIndex, out TreeInstance treeInstance))
            {
                skippedCount++;
                continue;
            }

            treeInstances.Add(treeInstance);
            convertedObjects.Add(instance);
            convertedCount++;
        }

        if (convertedCount == 0)
        {
            EditorUtility.DisplayDialog("Conversion failed", "No objects could be converted. Make sure the children are connected prefab instances inside the target Terrain bounds.", "OK");
            return;
        }

        terrainData.treePrototypes = prototypeList.ToArray();
        terrainData.treeInstances = treeInstances.ToArray();
        terrainData.RefreshPrototypes();
        EditorUtility.SetDirty(terrainData);

        HandleOriginalObjects(convertedObjects);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog(
            "Conversion complete",
            $"Converted {convertedCount} objects into Terrain trees.\nSkipped {skippedCount} objects.",
            "OK");
    }

    private static List<GameObject> CollectPrefabInstanceRoots(GameObject root, bool includeInactiveChildren)
    {
        List<GameObject> results = new List<GameObject>();
        HashSet<GameObject> seen = new HashSet<GameObject>();
        Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactiveChildren);

        foreach (Transform child in transforms)
        {
            if (child.gameObject == root)
            {
                continue;
            }

            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(child.gameObject);
            if (prefabRoot == null || prefabRoot != child.gameObject)
            {
                continue;
            }

            if (!prefabRoot.transform.IsChildOf(root.transform))
            {
                continue;
            }

            if (seen.Add(prefabRoot))
            {
                results.Add(prefabRoot);
            }
        }

        return results;
    }

    private Dictionary<GameObject, int> BuildPrototypeIndexMap(List<TreePrototype> prototypes)
    {
        Dictionary<GameObject, int> map = new Dictionary<GameObject, int>();
        for (int i = 0; i < prototypes.Count; i++)
        {
            GameObject prefab = prototypes[i].prefab;
            if (prefab != null && !map.ContainsKey(prefab))
            {
                map.Add(prefab, i);
            }
        }

        return map;
    }

    private bool TryGetOrCreatePrototypeIndex(
        GameObject prefab,
        List<TreePrototype> prototypes,
        Dictionary<GameObject, int> prototypeIndices,
        out int prototypeIndex)
    {
        if (prototypeIndices.TryGetValue(prefab, out prototypeIndex))
        {
            return true;
        }

        TreePrototype prototype = new TreePrototype
        {
            prefab = prefab,
            bendFactor = Mathf.Max(0f, bendFactor)
        };

        prototypeIndex = prototypes.Count;
        prototypes.Add(prototype);
        prototypeIndices.Add(prefab, prototypeIndex);
        return true;
    }

    private bool TryCreateTreeInstance(
        Transform instanceTransform,
        Terrain terrain,
        int prototypeIndex,
        out TreeInstance treeInstance)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainLocalPosition = instanceTransform.position - terrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        treeInstance = default;

        if (terrainSize.x <= 0f || terrainSize.y <= 0f || terrainSize.z <= 0f)
        {
            return false;
        }

        float normalizedX = terrainLocalPosition.x / terrainSize.x;
        float normalizedZ = terrainLocalPosition.z / terrainSize.z;

        if (normalizedX < 0f || normalizedX > 1f || normalizedZ < 0f || normalizedZ > 1f)
        {
            return false;
        }

        float normalizedY = useOriginalY
            ? Mathf.Clamp01(terrainLocalPosition.y / terrainSize.y)
            : terrainData.GetInterpolatedHeight(normalizedX, normalizedZ) / terrainSize.y;

        Vector3 lossyScale = instanceTransform.lossyScale;
        float widthScale = Mathf.Max(0.0001f, (Mathf.Abs(lossyScale.x) + Mathf.Abs(lossyScale.z)) * 0.5f);
        float heightScale = Mathf.Max(0.0001f, Mathf.Abs(lossyScale.y));

        treeInstance = new TreeInstance
        {
            position = new Vector3(normalizedX, normalizedY, normalizedZ),
            prototypeIndex = prototypeIndex,
            widthScale = widthScale,
            heightScale = heightScale,
            rotation = instanceTransform.eulerAngles.y * Mathf.Deg2Rad,
            color = Color.white,
            lightmapColor = Color.white
        };

        return true;
    }

    private void HandleOriginalObjects(List<GameObject> instances)
    {
        switch (originalHandling)
        {
            case OriginalHandling.Keep:
                return;
            case OriginalHandling.Disable:
                foreach (GameObject instance in instances)
                {
                    Undo.RecordObject(instance, "Disable Converted Prefab Painter Objects");
                    instance.SetActive(false);
                    EditorUtility.SetDirty(instance);
                }
                return;
            case OriginalHandling.Delete:
                foreach (GameObject instance in instances)
                {
                    Undo.DestroyObjectImmediate(instance);
                }
                return;
        }
    }
}
