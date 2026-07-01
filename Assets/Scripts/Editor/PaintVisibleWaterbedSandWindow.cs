using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PaintVisibleWaterbedSandWindow : EditorWindow
{
    private struct RandomLayerSettings
    {
        public readonly float StoneCoverage;
        public readonly float StoneMaxWeight;
        public readonly float RiverRocksCoverage;
        public readonly float RiverRocksMaxWeight;
        public readonly float MaxCombinedRockWeight;
        public readonly float NoiseScale;
        public readonly float DetailStrength;
        public readonly int Seed;

        public RandomLayerSettings(
            float stoneCoverage,
            float stoneMaxWeight,
            float riverRocksCoverage,
            float riverRocksMaxWeight,
            float maxCombinedRockWeight,
            float noiseScale,
            float detailStrength,
            int seed)
        {
            StoneCoverage = Mathf.Clamp01(stoneCoverage);
            StoneMaxWeight = Mathf.Clamp01(stoneMaxWeight);
            RiverRocksCoverage = Mathf.Clamp01(riverRocksCoverage);
            RiverRocksMaxWeight = Mathf.Clamp01(riverRocksMaxWeight);
            MaxCombinedRockWeight = Mathf.Clamp01(maxCombinedRockWeight);
            NoiseScale = Mathf.Max(0.1f, noiseScale);
            DetailStrength = Mathf.Clamp01(detailStrength);
            Seed = seed;
        }
    }

    private const string DefaultSandLayerPath = "Assets/ToonScapes/Spring Isles/Terrain/Terrain Data/Layer_TSI_Terrain_Sand_01A.terrainlayer";
    private const string DefaultStoneLayerPath = "Assets/ToonScapes/Spring Isles/Terrain/Terrain Data/Layer_TSI_Terrain_Stone_01A.terrainlayer";
    private const string DefaultRiverRocksLayerPath = "Assets/ToonScapes/Spring Isles/Terrain/Terrain Data/Layer_TSI_Terrain_River_Rocks_01A.terrainlayer";
    private const float DefaultRayHeight = 500f;
    private const float DefaultSandWeight = 1f;
    private const int DefaultEdgeBlendPixels = 3;
    private const float DefaultEdgeWeight = 0.55f;

    private GameObject waterRoot;
    private Terrain targetTerrain;
    private TerrainLayer sandLayer;
    private TerrainLayer stoneLayer;
    private TerrainLayer riverRocksLayer;
    private LayerMask waterLayerMask;
    private LayerMask blockerLayerMask = ~0;
    private bool processAllSceneTerrains = true;
    private bool includeInactive;
    private bool createTemporaryWaterColliders = true;
    private bool requireTerrainBelowWater = true;
    private float rayHeight = DefaultRayHeight;
    private float sandWeight = DefaultSandWeight;
    private int edgeBlendPixels = DefaultEdgeBlendPixels;
    private float edgeWeight = DefaultEdgeWeight;
    private float stoneCoverage = 0.28f;
    private float stoneMaxWeight = 0.22f;
    private float riverRocksCoverage = 0.45f;
    private float riverRocksMaxWeight = 0.35f;
    private float maxCombinedRockWeight = 0.55f;
    private float randomNoiseScale = 12f;
    private float randomDetailStrength = 0.35f;
    private int randomSeed = 12345;
    private Vector2 scroll;
    private string resultText = "No waterbed painted yet.";

    [MenuItem("Tools/Terrain/Paint Visible Waterbed")]
    private static void Open()
    {
        GetWindow<PaintVisibleWaterbedSandWindow>("Visible Waterbed");
    }

    private void OnEnable()
    {
        if (sandLayer == null)
        {
            sandLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(DefaultSandLayerPath);
        }

        if (stoneLayer == null)
        {
            stoneLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(DefaultStoneLayerPath);
        }

        if (riverRocksLayer == null)
        {
            riverRocksLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(DefaultRiverRocksLayerPath);
        }

        int waterLayer = LayerMask.NameToLayer("Water");
        if (waterLayer >= 0)
        {
            waterLayerMask = 1 << waterLayer;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Paint Visible Waterbed", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Drag a parent object containing Water-layer children. The tool casts vertical rays from above; visible water projections are painted as sand with randomized stone and river-rock overlays.",
            MessageType.Info);

        waterRoot = (GameObject)EditorGUILayout.ObjectField("Water Parent", waterRoot, typeof(GameObject), true);
        includeInactive = EditorGUILayout.Toggle("Include Inactive Children", includeInactive);
        waterLayerMask = LayerMaskField("Water Layer Mask", waterLayerMask);
        blockerLayerMask = LayerMaskField("Raycast Layers", blockerLayerMask);

        processAllSceneTerrains = EditorGUILayout.Toggle("Process All Scene Terrains", processAllSceneTerrains);
        using (new EditorGUI.DisabledScope(processAllSceneTerrains))
        {
            targetTerrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", targetTerrain, typeof(Terrain), true);
        }

        sandLayer = (TerrainLayer)EditorGUILayout.ObjectField("Sand Layer", sandLayer, typeof(TerrainLayer), false);
        stoneLayer = (TerrainLayer)EditorGUILayout.ObjectField("Random Stone Layer", stoneLayer, typeof(TerrainLayer), false);
        riverRocksLayer = (TerrainLayer)EditorGUILayout.ObjectField("Random River Rocks Layer", riverRocksLayer, typeof(TerrainLayer), false);
        createTemporaryWaterColliders = EditorGUILayout.Toggle("Temporary Water Colliders", createTemporaryWaterColliders);
        requireTerrainBelowWater = EditorGUILayout.Toggle("Require Terrain Below Water", requireTerrainBelowWater);
        rayHeight = EditorGUILayout.FloatField("Ray Height", Mathf.Max(1f, rayHeight));
        sandWeight = EditorGUILayout.Slider("Total Waterbed Weight", sandWeight, 0f, 1f);
        edgeBlendPixels = EditorGUILayout.IntSlider("Edge Blend Pixels", edgeBlendPixels, 0, 24);
        edgeWeight = EditorGUILayout.Slider("Edge Blend Weight", edgeWeight, 0f, 1f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Random Stone Mix", EditorStyles.boldLabel);
        stoneCoverage = EditorGUILayout.Slider("Stone Coverage", stoneCoverage, 0f, 1f);
        stoneMaxWeight = EditorGUILayout.Slider("Stone Max Weight", stoneMaxWeight, 0f, 1f);
        riverRocksCoverage = EditorGUILayout.Slider("River Rocks Coverage", riverRocksCoverage, 0f, 1f);
        riverRocksMaxWeight = EditorGUILayout.Slider("River Rocks Max Weight", riverRocksMaxWeight, 0f, 1f);
        maxCombinedRockWeight = EditorGUILayout.Slider("Max Combined Rock Weight", maxCombinedRockWeight, 0f, 1f);
        randomNoiseScale = EditorGUILayout.FloatField("Noise Scale (World Units)", Mathf.Max(0.1f, randomNoiseScale));
        randomDetailStrength = EditorGUILayout.Slider("Detail Strength", randomDetailStrength, 0f, 1f);
        randomSeed = EditorGUILayout.IntField("Random Seed", randomSeed);

        using (new EditorGUI.DisabledScope(waterRoot == null || sandLayer == null || stoneLayer == null || riverRocksLayer == null || (!processAllSceneTerrains && targetTerrain == null)))
        {
            if (GUILayout.Button("Paint Visible Waterbed", GUILayout.Height(32f)))
            {
                Paint();
            }
        }

        EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(resultText, GUILayout.MinHeight(130f));
        EditorGUILayout.EndScrollView();
    }

    private void Paint()
    {
        List<Transform> waterTransforms = CollectWaterTransforms(waterRoot, waterLayerMask, includeInactive);
        if (waterTransforms.Count == 0)
        {
            EditorUtility.DisplayDialog("No Water Found", "No children on the selected Water layer were found.", "OK");
            return;
        }

        Bounds waterBounds = CalculateWaterBounds(waterTransforms);
        if (waterBounds.size == Vector3.zero)
        {
            EditorUtility.DisplayDialog("No Water Bounds", "The Water-layer children have no Renderer or Collider bounds.", "OK");
            return;
        }

        Terrain[] terrains = processAllSceneTerrains ? FindSceneTerrains() : new[] { targetTerrain };
        if (terrains.Length == 0)
        {
            EditorUtility.DisplayDialog("No Terrain", "No Terrain components were found in open scenes.", "OK");
            return;
        }

        List<GameObject> temporaryObjects = new List<GameObject>();
        HashSet<Collider> waterColliders = CollectWaterColliders(waterTransforms);
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

        StringBuilder report = new StringBuilder();
        HashSet<TerrainData> processedTerrainData = new HashSet<TerrainData>();
        int totalChangedPixels = 0;
        int paintedTerrains = 0;

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
                    "Painting visible waterbed",
                    terrain.name,
                    terrains.Length <= 1 ? 0f : (float)i / (terrains.Length - 1));

                Undo.RegisterCompleteObjectUndo(terrainData, "Paint Visible Waterbed");

                int sandLayerIndex = EnsureTerrainLayer(terrainData, sandLayer);
                int stoneLayerIndex = EnsureTerrainLayer(terrainData, stoneLayer);
                int riverRocksLayerIndex = EnsureTerrainLayer(terrainData, riverRocksLayer);
                LayerMask raycastLayers = blockerLayerMask;
                raycastLayers.value |= waterLayerMask.value;
                int changedPixels = PaintTerrainWaterbed(
                    terrain,
                    sandLayerIndex,
                    stoneLayerIndex,
                    riverRocksLayerIndex,
                    waterBounds,
                    waterColliders,
                    raycastLayers,
                    rayHeight,
                    sandWeight,
                    edgeBlendPixels,
                    edgeWeight,
                    requireTerrainBelowWater,
                    new RandomLayerSettings(
                        stoneCoverage,
                        stoneMaxWeight,
                        riverRocksCoverage,
                        riverRocksMaxWeight,
                        maxCombinedRockWeight,
                        randomNoiseScale,
                        randomDetailStrength,
                        randomSeed));

                if (changedPixels > 0)
                {
                    totalChangedPixels += changedPixels;
                    paintedTerrains++;
                    EditorUtility.SetDirty(terrainData);
                    MarkTerrainSceneDirty(terrain);
                }

                report.AppendLine($"{terrain.name}: painted {changedPixels} alphamap pixels.");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            CleanupTemporaryObjects(temporaryObjects);
        }

        resultText =
            $"Finished. Painted {totalChangedPixels} alphamap pixels across {paintedTerrains} TerrainData assets.\n" +
            $"Water objects: {waterTransforms.Count}. Water colliders used: {waterColliders.Count}.\n\n" +
            report;

        EditorUtility.DisplayDialog(
            "Paint complete",
            $"Painted {totalChangedPixels} alphamap pixels across {paintedTerrains} TerrainData assets.",
            "OK");
    }

    private static int PaintTerrainWaterbed(
        Terrain terrain,
        int sandLayerIndex,
        int stoneLayerIndex,
        int riverRocksLayerIndex,
        Bounds waterBounds,
        HashSet<Collider> waterColliders,
        LayerMask raycastLayers,
        float rayHeight,
        float targetSandWeight,
        int edgeRadius,
        float edgeBlendWeight,
        bool requireTerrainBelowWater,
        RandomLayerSettings randomSettings)
    {
        TerrainData terrainData = terrain.terrainData;
        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        int layerCount = terrainData.alphamapLayers;
        float[,,] alphamaps = terrainData.GetAlphamaps(0, 0, width, height);
        float[,] targetWeights = BuildVisibleWaterMask(
            terrain,
            waterBounds,
            waterColliders,
            raycastLayers,
            rayHeight,
            Mathf.Clamp01(targetSandWeight),
            edgeRadius,
            Mathf.Clamp01(edgeBlendWeight),
            requireTerrainBelowWater);

        int changedPixels = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float targetWeight = targetWeights[y, x];
                if (targetWeight <= 0f)
                {
                    continue;
                }

                CalculateRandomOverlayWeights(
                    terrain,
                    x,
                    y,
                    width,
                    height,
                    targetWeight,
                    randomSettings,
                    out float stoneWeight,
                    out float riverRocksWeight);

                float sandWeight = Mathf.Max(0f, targetWeight - stoneWeight - riverRocksWeight);
                BlendWaterbedWeights(
                    alphamaps,
                    x,
                    y,
                    layerCount,
                    sandLayerIndex,
                    sandWeight,
                    stoneLayerIndex,
                    stoneWeight,
                    riverRocksLayerIndex,
                    riverRocksWeight);
                changedPixels++;
            }
        }

        if (changedPixels > 0)
        {
            terrainData.SetAlphamaps(0, 0, alphamaps);
        }

        return changedPixels;
    }

    private static void CalculateRandomOverlayWeights(
        Terrain terrain,
        int x,
        int y,
        int width,
        int height,
        float targetWeight,
        RandomLayerSettings settings,
        out float stoneWeight,
        out float riverRocksWeight)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;
        float normalizedX = width <= 1 ? 0f : (float)x / (width - 1);
        float normalizedZ = height <= 1 ? 0f : (float)y / (height - 1);
        float worldX = terrainPosition.x + normalizedX * terrainSize.x;
        float worldZ = terrainPosition.z + normalizedZ * terrainSize.z;
        float scale = Mathf.Max(0.1f, settings.NoiseScale);
        float detailStrength = Mathf.Clamp01(settings.DetailStrength);

        float stoneNoise = LayeredNoise(worldX, worldZ, scale, settings.Seed * 0.013f + 17.37f, settings.Seed * 0.021f - 31.11f, detailStrength);
        float riverRocksNoise = LayeredNoise(worldX, worldZ, scale * 0.73f, settings.Seed * -0.019f + 83.41f, settings.Seed * 0.017f + 9.53f, detailStrength);

        stoneWeight = targetWeight * settings.StoneMaxWeight * CoverageMask(stoneNoise, settings.StoneCoverage);
        riverRocksWeight = targetWeight * settings.RiverRocksMaxWeight * CoverageMask(riverRocksNoise, settings.RiverRocksCoverage);

        float maxRockWeight = targetWeight * settings.MaxCombinedRockWeight;
        float totalRockWeight = stoneWeight + riverRocksWeight;
        if (totalRockWeight > maxRockWeight && totalRockWeight > 0.0001f)
        {
            float scaleDown = maxRockWeight / totalRockWeight;
            stoneWeight *= scaleDown;
            riverRocksWeight *= scaleDown;
        }
    }

    private static float LayeredNoise(float worldX, float worldZ, float scale, float offsetX, float offsetZ, float detailStrength)
    {
        float coarse = Mathf.PerlinNoise(worldX / scale + offsetX, worldZ / scale + offsetZ);
        float detail = Mathf.PerlinNoise(worldX / (scale * 0.31f) - offsetZ, worldZ / (scale * 0.31f) + offsetX);
        return Mathf.Clamp01(Mathf.Lerp(coarse, (coarse + detail) * 0.5f, detailStrength));
    }

    private static float CoverageMask(float noise, float coverage)
    {
        coverage = Mathf.Clamp01(coverage);
        if (coverage <= 0f)
        {
            return 0f;
        }

        if (coverage >= 1f)
        {
            return noise;
        }

        float threshold = 1f - coverage;
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(threshold, 1f, noise));
    }

    private static float[,] BuildVisibleWaterMask(
        Terrain terrain,
        Bounds waterBounds,
        HashSet<Collider> waterColliders,
        LayerMask raycastLayers,
        float rayHeight,
        float sandWeight,
        int edgeRadius,
        float edgeWeight,
        bool requireTerrainBelowWater)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;
        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        float[,] weights = new float[height, width];
        bool[,] visibleMask = new bool[height, width];

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

                if (!TryGetVisibleWaterHit(
                    new Vector3(worldX, rayOriginY, worldZ),
                    rayDistance,
                    raycastLayers,
                    waterColliders,
                    out RaycastHit hit))
                {
                    continue;
                }

                if (requireTerrainBelowWater && terrainWorldY >= hit.point.y)
                {
                    continue;
                }

                visibleMask[y, x] = true;
                weights[y, x] = sandWeight;
            }
        }

        ApplyEdgeBlend(weights, visibleMask, width, height, edgeRadius, edgeWeight);
        return weights;
    }

    private static bool TryGetVisibleWaterHit(
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
            if (hit.collider == null)
            {
                continue;
            }

            if (!hit.collider.enabled || !hit.collider.gameObject.activeInHierarchy)
            {
                continue;
            }

            waterHit = hit;
            return waterColliders.Contains(hit.collider);
        }

        waterHit = default;
        return false;
    }

    private static void ApplyEdgeBlend(float[,] weights, bool[,] visibleMask, int width, int height, int radius, float maxEdgeWeight)
    {
        if (radius <= 0 || maxEdgeWeight <= 0f)
        {
            return;
        }

        float[,] blendedWeights = (float[,])weights.Clone();
        float radiusSquared = radius * radius;

        for (int sourceY = 0; sourceY < height; sourceY++)
        {
            for (int sourceX = 0; sourceX < width; sourceX++)
            {
                if (!visibleMask[sourceY, sourceX])
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
                        float distanceSquared = offsetX * offsetX + offsetY * offsetY;

                        if (distanceSquared <= 0f || distanceSquared > radiusSquared)
                        {
                            continue;
                        }

                        float distance = Mathf.Sqrt(distanceSquared);
                        float falloff = 1f - Mathf.Clamp01(distance / radius);
                        float weight = Mathf.SmoothStep(0f, maxEdgeWeight, falloff);

                        if (weight > blendedWeights[y, x])
                        {
                            blendedWeights[y, x] = weight;
                        }
                    }
                }
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                weights[y, x] = blendedWeights[y, x];
            }
        }
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

    private static void BlendWaterbedWeights(
        float[,,] alphamaps,
        int x,
        int y,
        int layerCount,
        int sandLayerIndex,
        float sandWeight,
        int stoneLayerIndex,
        float stoneWeight,
        int riverRocksLayerIndex,
        float riverRocksWeight)
    {
        sandWeight = Mathf.Clamp01(sandWeight);
        stoneWeight = Mathf.Clamp01(stoneWeight);
        riverRocksWeight = Mathf.Clamp01(riverRocksWeight);
        float targetWeightSum = Mathf.Clamp01(sandWeight + stoneWeight + riverRocksWeight);
        if (targetWeightSum <= 0f)
        {
            return;
        }

        float otherWeightSum = 0f;
        for (int layer = 0; layer < layerCount; layer++)
        {
            if (layer != sandLayerIndex && layer != stoneLayerIndex && layer != riverRocksLayerIndex)
            {
                otherWeightSum += alphamaps[y, x, layer];
            }
        }

        alphamaps[y, x, sandLayerIndex] = sandWeight;
        alphamaps[y, x, stoneLayerIndex] = stoneWeight;
        alphamaps[y, x, riverRocksLayerIndex] = riverRocksWeight;

        float remainingWeight = Mathf.Max(0f, 1f - targetWeightSum);
        if (otherWeightSum <= 0.0001f)
        {
            int otherLayerCount = 0;
            for (int layer = 0; layer < layerCount; layer++)
            {
                if (layer != sandLayerIndex && layer != stoneLayerIndex && layer != riverRocksLayerIndex)
                {
                    otherLayerCount++;
                }
            }

            if (otherLayerCount <= 0)
            {
                alphamaps[y, x, sandLayerIndex] += remainingWeight;
                return;
            }

            float fallbackWeight = otherLayerCount > 0 ? remainingWeight / otherLayerCount : 0f;
            for (int layer = 0; layer < layerCount; layer++)
            {
                if (layer != sandLayerIndex && layer != stoneLayerIndex && layer != riverRocksLayerIndex)
                {
                    alphamaps[y, x, layer] = fallbackWeight;
                }
            }

            return;
        }

        float scale = remainingWeight / otherWeightSum;
        for (int layer = 0; layer < layerCount; layer++)
        {
            if (layer != sandLayerIndex && layer != stoneLayerIndex && layer != riverRocksLayerIndex)
            {
                alphamaps[y, x, layer] *= scale;
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
