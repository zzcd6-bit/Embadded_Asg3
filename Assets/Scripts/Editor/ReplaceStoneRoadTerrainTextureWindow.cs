using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ReplaceStoneRoadTerrainTextureWindow : EditorWindow
{
    private const string DefaultStoneLayerPath = "Assets/ToonScapes/Spring Isles/Terrain/Terrain Data/Layer_TSI_Terrain_Stone_01A.terrainlayer";
    private const float DefaultRayHeight = 500f;
    private const float DefaultSlopeThreshold = 70f;

    private GameObject waterRoot;
    private Terrain targetTerrain;
    private TerrainLayer sourceStoneLayer;
    private TerrainLayer replacementLayer;
    private LayerMask waterLayerMask;
    private LayerMask blockerLayerMask = ~0;
    private bool processAllSceneTerrains = true;
    private bool includeInactiveWater;
    private bool createTemporaryWaterColliders = true;
    private bool excludeWaterCovered = true;
    private bool requireTerrainBelowWater = true;
    private bool excludeSteepSlopes = true;
    private float rayHeight = DefaultRayHeight;
    private float slopeThreshold = DefaultSlopeThreshold;
    private int waterExclusionPaddingPixels = 1;
    private float minSourceWeight = 0.05f;
    private float replacementStrength = 1f;
    private Vector2 scroll;
    private string resultText = "No stone road texture replaced yet.";

    [MenuItem("Tools/Terrain/Replace Stone Road Texture")]
    private static void Open()
    {
        GetWindow<ReplaceStoneRoadTerrainTextureWindow>("Replace Stone Road");
    }

    private void OnEnable()
    {
        if (sourceStoneLayer == null)
        {
            sourceStoneLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(DefaultStoneLayerPath);
        }

        int waterLayer = LayerMask.NameToLayer("Water");
        if (waterLayer >= 0)
        {
            waterLayerMask = 1 << waterLayer;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Replace Stone Road Texture", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Transfers Stone texture weights to a replacement TerrainLayer, excluding water-covered riverbeds and steep rocky slopes.",
            MessageType.Info);

        processAllSceneTerrains = EditorGUILayout.Toggle("Process All Scene Terrains", processAllSceneTerrains);
        using (new EditorGUI.DisabledScope(processAllSceneTerrains))
        {
            targetTerrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", targetTerrain, typeof(Terrain), true);
        }

        sourceStoneLayer = (TerrainLayer)EditorGUILayout.ObjectField("Source Stone Layer", sourceStoneLayer, typeof(TerrainLayer), false);
        replacementLayer = (TerrainLayer)EditorGUILayout.ObjectField("Replacement Layer", replacementLayer, typeof(TerrainLayer), false);
        minSourceWeight = EditorGUILayout.Slider("Min Source Weight", minSourceWeight, 0f, 1f);
        replacementStrength = EditorGUILayout.Slider("Replacement Strength", replacementStrength, 0f, 1f);

        EditorGUILayout.Space();
        excludeSteepSlopes = EditorGUILayout.Toggle("Exclude Steep Slopes", excludeSteepSlopes);
        using (new EditorGUI.DisabledScope(!excludeSteepSlopes))
        {
            slopeThreshold = EditorGUILayout.Slider("Slope Threshold", slopeThreshold, 0f, 90f);
        }

        EditorGUILayout.Space();
        excludeWaterCovered = EditorGUILayout.Toggle("Exclude Water Covered", excludeWaterCovered);
        using (new EditorGUI.DisabledScope(!excludeWaterCovered))
        {
            waterRoot = (GameObject)EditorGUILayout.ObjectField("Water Parent", waterRoot, typeof(GameObject), true);
            includeInactiveWater = EditorGUILayout.Toggle("Include Inactive Water", includeInactiveWater);
            waterLayerMask = LayerMaskField("Water Layer Mask", waterLayerMask);
            blockerLayerMask = LayerMaskField("Raycast Layers", blockerLayerMask);
            createTemporaryWaterColliders = EditorGUILayout.Toggle("Temporary Water Colliders", createTemporaryWaterColliders);
            requireTerrainBelowWater = EditorGUILayout.Toggle("Require Terrain Below Water", requireTerrainBelowWater);
            rayHeight = EditorGUILayout.FloatField("Ray Height", Mathf.Max(1f, rayHeight));
            waterExclusionPaddingPixels = EditorGUILayout.IntSlider("Water Exclusion Padding", waterExclusionPaddingPixels, 0, 16);
        }

        bool missingWaterInput = excludeWaterCovered && waterRoot == null;
        using (new EditorGUI.DisabledScope(
            sourceStoneLayer == null ||
            replacementLayer == null ||
            sourceStoneLayer == replacementLayer ||
            missingWaterInput ||
            (!processAllSceneTerrains && targetTerrain == null)))
        {
            if (GUILayout.Button("Replace Stone Road Texture", GUILayout.Height(32f)))
            {
                Replace();
            }
        }

        EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(resultText, GUILayout.MinHeight(130f));
        EditorGUILayout.EndScrollView();
    }

    private void Replace()
    {
        Terrain[] terrains = processAllSceneTerrains ? FindSceneTerrains() : new[] { targetTerrain };
        if (terrains.Length == 0)
        {
            EditorUtility.DisplayDialog("No Terrain", "No Terrain components were found in open scenes.", "OK");
            return;
        }

        List<Transform> waterTransforms = new List<Transform>();
        Bounds waterBounds = new Bounds();
        HashSet<Collider> waterColliders = new HashSet<Collider>();
        List<GameObject> temporaryObjects = new List<GameObject>();

        if (excludeWaterCovered)
        {
            waterTransforms = CollectWaterTransforms(waterRoot, waterLayerMask, includeInactiveWater);
            if (waterTransforms.Count == 0)
            {
                EditorUtility.DisplayDialog("No Water Found", "No children on the selected Water layer were found.", "OK");
                return;
            }

            waterBounds = CalculateWaterBounds(waterTransforms);
            if (waterBounds.size == Vector3.zero)
            {
                EditorUtility.DisplayDialog("No Water Bounds", "The Water-layer children have no Renderer or Collider bounds.", "OK");
                return;
            }

            waterColliders = CollectWaterColliders(waterTransforms);
            if (createTemporaryWaterColliders)
            {
                CreateTemporaryWaterColliders(waterTransforms, waterColliders, temporaryObjects);
            }

            if (waterColliders.Count == 0)
            {
                CleanupTemporaryObjects(temporaryObjects);
                EditorUtility.DisplayDialog("No Water Colliders", "No water colliders could be found or generated. Keep Temporary Water Colliders enabled for mesh water.", "OK");
                return;
            }
        }

        StringBuilder report = new StringBuilder();
        HashSet<TerrainData> processedTerrainData = new HashSet<TerrainData>();
        int totalChangedPixels = 0;
        int changedTerrains = 0;

        try
        {
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (terrain == null || terrain.terrainData == null)
                {
                    continue;
                }

                TerrainData terrainData = terrain.terrainData;
                if (!processedTerrainData.Add(terrainData))
                {
                    report.AppendLine($"{terrain.name}: skipped, TerrainData already processed.");
                    continue;
                }

                EditorUtility.DisplayProgressBar(
                    "Replacing stone road texture",
                    terrain.name,
                    terrains.Length <= 1 ? 0f : (float)i / (terrains.Length - 1));

                int sourceLayerIndex = FindTerrainLayerIndex(terrainData, sourceStoneLayer);
                if (sourceLayerIndex < 0)
                {
                    report.AppendLine($"{terrain.name}: skipped, source Stone layer is not on this TerrainData.");
                    continue;
                }

                Undo.RegisterCompleteObjectUndo(terrainData, "Replace Stone Road Texture");
                int replacementLayerIndex = EnsureTerrainLayer(terrainData, replacementLayer);
                LayerMask raycastLayers = blockerLayerMask;
                raycastLayers.value |= waterLayerMask.value;

                int changedPixels = ReplaceTerrainStoneRoad(
                    terrain,
                    sourceLayerIndex,
                    replacementLayerIndex,
                    excludeWaterCovered,
                    waterBounds,
                    waterColliders,
                    raycastLayers,
                    rayHeight,
                    waterExclusionPaddingPixels,
                    requireTerrainBelowWater,
                    excludeSteepSlopes,
                    slopeThreshold,
                    minSourceWeight,
                    replacementStrength);

                if (changedPixels > 0)
                {
                    totalChangedPixels += changedPixels;
                    changedTerrains++;
                    EditorUtility.SetDirty(terrainData);
                    MarkTerrainSceneDirty(terrain);
                }

                report.AppendLine($"{terrain.name}: replaced {changedPixels} alphamap pixels.");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            CleanupTemporaryObjects(temporaryObjects);
        }

        resultText =
            $"Finished. Replaced {totalChangedPixels} alphamap pixels across {changedTerrains} TerrainData assets.\n" +
            $"Water exclusion: {(excludeWaterCovered ? $"{waterTransforms.Count} water objects, {waterColliders.Count} colliders." : "off")}.\n\n" +
            report;

        EditorUtility.DisplayDialog(
            "Replace complete",
            $"Replaced {totalChangedPixels} alphamap pixels across {changedTerrains} TerrainData assets.",
            "OK");
    }

    private static int ReplaceTerrainStoneRoad(
        Terrain terrain,
        int sourceLayerIndex,
        int replacementLayerIndex,
        bool excludeWater,
        Bounds waterBounds,
        HashSet<Collider> waterColliders,
        LayerMask raycastLayers,
        float rayHeight,
        int waterPaddingPixels,
        bool requireTerrainBelowWater,
        bool excludeSteep,
        float slopeThreshold,
        float minSourceWeight,
        float replacementStrength)
    {
        TerrainData terrainData = terrain.terrainData;
        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        int layerCount = terrainData.alphamapLayers;
        float[,,] alphamaps = terrainData.GetAlphamaps(0, 0, width, height);
        bool[,] waterMask = excludeWater
            ? BuildWaterCoveredMask(terrain, waterBounds, waterColliders, raycastLayers, rayHeight, waterPaddingPixels, requireTerrainBelowWater)
            : new bool[height, width];
        int changedPixels = 0;

        for (int y = 0; y < height; y++)
        {
            float normalizedZ = height <= 1 ? 0f : (float)y / (height - 1);

            for (int x = 0; x < width; x++)
            {
                if (waterMask[y, x])
                {
                    continue;
                }

                float normalizedX = width <= 1 ? 0f : (float)x / (width - 1);
                if (excludeSteep && terrainData.GetSteepness(normalizedX, normalizedZ) >= slopeThreshold)
                {
                    continue;
                }

                float sourceWeight = alphamaps[y, x, sourceLayerIndex];
                if (sourceWeight < minSourceWeight)
                {
                    continue;
                }

                float transferWeight = sourceWeight * Mathf.Clamp01(replacementStrength);
                alphamaps[y, x, sourceLayerIndex] = sourceWeight - transferWeight;
                alphamaps[y, x, replacementLayerIndex] += transferWeight;
                NormalizeWeights(alphamaps, x, y, layerCount);
                changedPixels++;
            }
        }

        if (changedPixels > 0)
        {
            terrainData.SetAlphamaps(0, 0, alphamaps);
        }

        return changedPixels;
    }

    private static bool[,] BuildWaterCoveredMask(
        Terrain terrain,
        Bounds waterBounds,
        HashSet<Collider> waterColliders,
        LayerMask raycastLayers,
        float rayHeight,
        int paddingPixels,
        bool requireTerrainBelowWater)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;
        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        bool[,] mask = new bool[height, width];

        int minX = Mathf.Clamp(Mathf.FloorToInt((waterBounds.min.x - terrainPosition.x) / terrainSize.x * (width - 1)), 0, width - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt((waterBounds.max.x - terrainPosition.x) / terrainSize.x * (width - 1)), 0, width - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt((waterBounds.min.z - terrainPosition.z) / terrainSize.z * (height - 1)), 0, height - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt((waterBounds.max.z - terrainPosition.z) / terrainSize.z * (height - 1)), 0, height - 1);
        float rayOriginY = waterBounds.max.y + Mathf.Max(1f, rayHeight);
        float rayDistance = rayHeight + Mathf.Max(terrainSize.y, waterBounds.size.y) + 20f;

        for (int y = minY; y <= maxY; y++)
        {
            float normalizedZ = height <= 1 ? 0f : (float)y / (height - 1);
            float worldZ = terrainPosition.z + normalizedZ * terrainSize.z;

            for (int x = minX; x <= maxX; x++)
            {
                float normalizedX = width <= 1 ? 0f : (float)x / (width - 1);
                float worldX = terrainPosition.x + normalizedX * terrainSize.x;
                float terrainWorldY = terrainPosition.y + terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);

                if (!TryGetWaterHit(new Vector3(worldX, rayOriginY, worldZ), rayDistance, raycastLayers, waterColliders, out RaycastHit hit))
                {
                    continue;
                }

                if (requireTerrainBelowWater && terrainWorldY >= hit.point.y)
                {
                    continue;
                }

                mask[y, x] = true;
            }
        }

        if (paddingPixels > 0)
        {
            ApplyMaskPadding(mask, width, height, paddingPixels);
        }

        return mask;
    }

    private static bool TryGetWaterHit(
        Vector3 origin,
        float distance,
        LayerMask raycastLayers,
        HashSet<Collider> waterColliders,
        out RaycastHit waterHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance, raycastLayers, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || !hit.collider.enabled || !hit.collider.gameObject.activeInHierarchy)
            {
                continue;
            }

            waterHit = hit;
            return waterColliders.Contains(hit.collider);
        }

        waterHit = default;
        return false;
    }

    private static void ApplyMaskPadding(bool[,] mask, int width, int height, int radius)
    {
        bool[,] paddedMask = (bool[,])mask.Clone();
        int radiusSquared = radius * radius;

        for (int sourceY = 0; sourceY < height; sourceY++)
        {
            for (int sourceX = 0; sourceX < width; sourceX++)
            {
                if (!mask[sourceY, sourceX])
                {
                    continue;
                }

                int minY = Mathf.Max(0, sourceY - radius);
                int maxY = Mathf.Min(height - 1, sourceY + radius);
                int minX = Mathf.Max(0, sourceX - radius);
                int maxX = Mathf.Min(width - 1, sourceX + radius);

                for (int y = minY; y <= maxY; y++)
                {
                    int offsetY = y - sourceY;

                    for (int x = minX; x <= maxX; x++)
                    {
                        int offsetX = x - sourceX;
                        if (offsetX * offsetX + offsetY * offsetY <= radiusSquared)
                        {
                            paddedMask[y, x] = true;
                        }
                    }
                }
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                mask[y, x] = paddedMask[y, x];
            }
        }
    }

    private static void NormalizeWeights(float[,,] alphamaps, int x, int y, int layerCount)
    {
        float sum = 0f;
        for (int layer = 0; layer < layerCount; layer++)
        {
            sum += alphamaps[y, x, layer];
        }

        if (sum <= 0.0001f)
        {
            return;
        }

        for (int layer = 0; layer < layerCount; layer++)
        {
            alphamaps[y, x, layer] /= sum;
        }
    }

    private static int FindTerrainLayerIndex(TerrainData terrainData, TerrainLayer terrainLayer)
    {
        TerrainLayer[] layers = terrainData.terrainLayers;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == terrainLayer)
            {
                return i;
            }
        }

        return -1;
    }

    private static int EnsureTerrainLayer(TerrainData terrainData, TerrainLayer terrainLayer)
    {
        TerrainLayer[] layers = terrainData.terrainLayers;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == terrainLayer)
            {
                return i;
            }
        }

        TerrainLayer[] expandedLayers = new TerrainLayer[layers.Length + 1];
        for (int i = 0; i < layers.Length; i++)
        {
            expandedLayers[i] = layers[i];
        }

        expandedLayers[expandedLayers.Length - 1] = terrainLayer;
        terrainData.terrainLayers = expandedLayers;
        return expandedLayers.Length - 1;
    }

    private static List<Transform> CollectWaterTransforms(GameObject root, LayerMask waterMask, bool includeInactiveChildren)
    {
        List<Transform> results = new List<Transform>();
        if (root == null)
        {
            return results;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactiveChildren);
        foreach (Transform candidate in transforms)
        {
            if (((1 << candidate.gameObject.layer) & waterMask.value) == 0)
            {
                continue;
            }

            if (candidate.GetComponent<Renderer>() == null &&
                candidate.GetComponent<Collider>() == null &&
                candidate.GetComponent<MeshFilter>() == null)
            {
                continue;
            }

            results.Add(candidate);
        }

        return results;
    }

    private static Bounds CalculateWaterBounds(List<Transform> waterTransforms)
    {
        Bounds bounds = new Bounds();
        bool hasBounds = false;

        foreach (Transform waterTransform in waterTransforms)
        {
            Renderer renderer = waterTransform.GetComponent<Renderer>();
            if (renderer != null)
            {
                EncapsulateBounds(ref bounds, ref hasBounds, renderer.bounds);
            }

            Collider collider = waterTransform.GetComponent<Collider>();
            if (collider != null)
            {
                EncapsulateBounds(ref bounds, ref hasBounds, collider.bounds);
            }

            MeshFilter meshFilter = waterTransform.GetComponent<MeshFilter>();
            if (renderer == null && collider == null && meshFilter != null && meshFilter.sharedMesh != null)
            {
                EncapsulateBounds(ref bounds, ref hasBounds, TransformBounds(waterTransform, meshFilter.sharedMesh.bounds));
            }
        }

        return hasBounds ? bounds : new Bounds();
    }

    private static Bounds TransformBounds(Transform transform, Bounds localBounds)
    {
        Vector3 center = transform.TransformPoint(localBounds.center);
        Vector3 extents = localBounds.extents;

        Vector3 axisX = transform.TransformVector(new Vector3(extents.x, 0f, 0f));
        Vector3 axisY = transform.TransformVector(new Vector3(0f, extents.y, 0f));
        Vector3 axisZ = transform.TransformVector(new Vector3(0f, 0f, extents.z));

        extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
        extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
        extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

        return new Bounds(center, extents * 2f);
    }

    private static void EncapsulateBounds(ref Bounds bounds, ref bool hasBounds, Bounds value)
    {
        if (!hasBounds)
        {
            bounds = value;
            hasBounds = true;
            return;
        }

        bounds.Encapsulate(value);
    }

    private static HashSet<Collider> CollectWaterColliders(List<Transform> waterTransforms)
    {
        HashSet<Collider> colliders = new HashSet<Collider>();
        foreach (Transform waterTransform in waterTransforms)
        {
            Collider[] childColliders = waterTransform.GetComponents<Collider>();
            foreach (Collider collider in childColliders)
            {
                if (collider != null && collider.enabled)
                {
                    colliders.Add(collider);
                }
            }
        }

        return colliders;
    }

    private static void CreateTemporaryWaterColliders(
        List<Transform> waterTransforms,
        HashSet<Collider> waterColliders,
        List<GameObject> temporaryObjects)
    {
        foreach (Transform waterTransform in waterTransforms)
        {
            MeshFilter meshFilter = waterTransform.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                continue;
            }

            GameObject temporaryObject = new GameObject($"__TempWaterCollider_{waterTransform.name}");
            temporaryObject.hideFlags = HideFlags.HideAndDontSave;
            temporaryObject.layer = waterTransform.gameObject.layer;
            temporaryObject.transform.SetPositionAndRotation(waterTransform.position, waterTransform.rotation);
            temporaryObject.transform.localScale = waterTransform.lossyScale;

            MeshCollider meshCollider = temporaryObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;

            waterColliders.Add(meshCollider);
            temporaryObjects.Add(temporaryObject);
        }
    }

    private static void CleanupTemporaryObjects(List<GameObject> temporaryObjects)
    {
        foreach (GameObject temporaryObject in temporaryObjects)
        {
            if (temporaryObject != null)
            {
                DestroyImmediate(temporaryObject);
            }
        }
    }

    private static Terrain[] FindSceneTerrains()
    {
        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include);
        List<Terrain> sceneTerrains = new List<Terrain>(terrains.Length);

        foreach (Terrain terrain in terrains)
        {
            if (terrain == null || !terrain.gameObject.scene.IsValid())
            {
                continue;
            }

            sceneTerrains.Add(terrain);
        }

        return sceneTerrains.ToArray();
    }

    private static LayerMask LayerMaskField(string label, LayerMask selected)
    {
        List<string> layerNames = new List<string>();
        List<int> layerNumbers = new List<int>();

        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (string.IsNullOrEmpty(layerName))
            {
                continue;
            }

            layerNames.Add(layerName);
            layerNumbers.Add(i);
        }

        int maskWithoutEmpty = 0;
        for (int i = 0; i < layerNumbers.Count; i++)
        {
            if (((1 << layerNumbers[i]) & selected.value) != 0)
            {
                maskWithoutEmpty |= 1 << i;
            }
        }

        maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layerNames.ToArray());

        int mask = 0;
        for (int i = 0; i < layerNumbers.Count; i++)
        {
            if ((maskWithoutEmpty & (1 << i)) != 0)
            {
                mask |= 1 << layerNumbers[i];
            }
        }

        selected.value = mask;
        return selected;
    }

    private static void MarkTerrainSceneDirty(Terrain terrain)
    {
        Scene scene = terrain.gameObject.scene;
        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
