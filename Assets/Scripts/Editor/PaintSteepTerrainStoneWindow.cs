using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PaintSteepTerrainStoneWindow : EditorWindow
{
    private const string DefaultStoneLayerPath = "Assets/ToonScapes/Spring Isles/Terrain/Terrain Data/Layer_TSI_Terrain_Stone_01A.terrainlayer";
    private const float DefaultSlopeThreshold = 70f;
    private const float DefaultBlendSlopeRange = 12f;
    private const int DefaultEdgeExpansionPixels = 4;
    private const float DefaultEdgeMaxStoneWeight = 0.6f;
    private const float DefaultUphillTolerance = 0.25f;

    private Terrain targetTerrain;
    private TerrainLayer stoneLayer;
    private float slopeThreshold = DefaultSlopeThreshold;
    private float blendSlopeRange = DefaultBlendSlopeRange;
    private int edgeExpansionPixels = DefaultEdgeExpansionPixels;
    private float edgeMaxStoneWeight = DefaultEdgeMaxStoneWeight;
    private bool onlyExpandDownhill = true;
    private float uphillTolerance = DefaultUphillTolerance;
    private bool processAllSceneTerrains = true;
    private Vector2 scroll;
    private string resultText = "No terrain painted yet.";

    [MenuItem("Tools/Terrain/Paint Steep Slopes Stone")]
    private static void Open()
    {
        GetWindow<PaintSteepTerrainStoneWindow>("Steep Slope Stone");
    }

    private void OnEnable()
    {
        if (stoneLayer == null)
        {
            stoneLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(DefaultStoneLayerPath);
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Paint Steep Terrain Slopes", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Paints steep slopes to Layer_TSI_Terrain_Stone_01A, with a softer material transition below the cliff edge.",
            MessageType.Info);

        processAllSceneTerrains = EditorGUILayout.Toggle("Process All Scene Terrains", processAllSceneTerrains);

        using (new EditorGUI.DisabledScope(processAllSceneTerrains))
        {
            targetTerrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", targetTerrain, typeof(Terrain), true);
        }

        stoneLayer = (TerrainLayer)EditorGUILayout.ObjectField("Stone Layer", stoneLayer, typeof(TerrainLayer), false);
        slopeThreshold = EditorGUILayout.Slider("Slope Threshold", slopeThreshold, 0f, 90f);
        blendSlopeRange = EditorGUILayout.Slider("Slope Blend Range", blendSlopeRange, 0f, 45f);
        edgeExpansionPixels = EditorGUILayout.IntSlider("Edge Expansion Pixels", edgeExpansionPixels, 0, 32);
        edgeMaxStoneWeight = EditorGUILayout.Slider("Edge Max Stone Weight", edgeMaxStoneWeight, 0f, 1f);
        onlyExpandDownhill = EditorGUILayout.Toggle("Only Expand Downhill", onlyExpandDownhill);

        using (new EditorGUI.DisabledScope(!onlyExpandDownhill))
        {
            uphillTolerance = EditorGUILayout.FloatField("Uphill Tolerance", Mathf.Max(0f, uphillTolerance));
        }

        using (new EditorGUI.DisabledScope(stoneLayer == null || (!processAllSceneTerrains && targetTerrain == null)))
        {
            if (GUILayout.Button("Paint Slopes > Threshold", GUILayout.Height(32f)))
            {
                Paint();
            }
        }

        EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(resultText, GUILayout.MinHeight(120f));
        EditorGUILayout.EndScrollView();
    }

    private void Paint()
    {
        if (stoneLayer == null)
        {
            EditorUtility.DisplayDialog("Missing Terrain Layer", "Assign Layer_TSI_Terrain_Stone_01A first.", "OK");
            return;
        }

        Terrain[] terrains = processAllSceneTerrains ? FindSceneTerrains() : new[] { targetTerrain };
        if (terrains.Length == 0)
        {
            EditorUtility.DisplayDialog("No Terrain", "No Terrain components were found in open scenes.", "OK");
            return;
        }

        StringBuilder report = new StringBuilder();
        HashSet<TerrainData> processedTerrainData = new HashSet<TerrainData>();
        int paintedTerrains = 0;
        int totalChangedPixels = 0;

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
                    "Painting steep terrain slopes",
                    terrain.name,
                    terrains.Length <= 1 ? 0f : (float)i / (terrains.Length - 1));

                Undo.RegisterCompleteObjectUndo(terrainData, "Paint Steep Terrain Slopes Stone");

                int stoneLayerIndex = EnsureTerrainLayer(terrainData, stoneLayer);
                int changedPixels = PaintTerrainData(
                    terrainData,
                    stoneLayerIndex,
                    slopeThreshold,
                    blendSlopeRange,
                    edgeExpansionPixels,
                    edgeMaxStoneWeight,
                    onlyExpandDownhill,
                    uphillTolerance);

                if (changedPixels > 0)
                {
                    paintedTerrains++;
                    totalChangedPixels += changedPixels;
                    EditorUtility.SetDirty(terrainData);
                    MarkTerrainSceneDirty(terrain);
                }

                report.AppendLine($"{terrain.name}: painted {changedPixels} alphamap pixels.");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        resultText =
            $"Finished. Painted {totalChangedPixels} alphamap pixels across {paintedTerrains} TerrainData assets.\n\n" +
            report;

        EditorUtility.DisplayDialog(
            "Paint complete",
            $"Painted {totalChangedPixels} alphamap pixels across {paintedTerrains} TerrainData assets.",
            "OK");
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

    private static int PaintTerrainData(
        TerrainData terrainData,
        int stoneLayerIndex,
        float threshold,
        float blendRange,
        int expansionPixels,
        float maxExpansionWeight,
        bool downhillOnly,
        float uphillTolerance)
    {
        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        int layerCount = terrainData.alphamapLayers;
        float[,,] alphamaps = terrainData.GetAlphamaps(0, 0, width, height);
        float[,] targetStoneWeights = BuildStoneWeightMap(
            terrainData,
            width,
            height,
            threshold,
            blendRange,
            expansionPixels,
            maxExpansionWeight,
            downhillOnly,
            uphillTolerance);
        int changedPixels = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float targetStoneWeight = targetStoneWeights[y, x];

                if (targetStoneWeight <= 0f || alphamaps[y, x, stoneLayerIndex] >= targetStoneWeight - 0.001f)
                {
                    continue;
                }

                BlendStoneWeight(alphamaps, x, y, layerCount, stoneLayerIndex, targetStoneWeight);
                changedPixels++;
            }
        }

        if (changedPixels > 0)
        {
            terrainData.SetAlphamaps(0, 0, alphamaps);
        }

        return changedPixels;
    }

    private static float[,] BuildStoneWeightMap(
        TerrainData terrainData,
        int width,
        int height,
        float threshold,
        float blendRange,
        int expansionPixels,
        float maxExpansionWeight,
        bool downhillOnly,
        float uphillTolerance)
    {
        float[,] stoneWeights = new float[height, width];
        bool[,] fullStoneMask = new bool[height, width];
        float[,] heights = new float[height, width];
        float blendStart = Mathf.Max(0f, threshold - Mathf.Max(0f, blendRange));

        for (int y = 0; y < height; y++)
        {
            float normalizedZ = height <= 1 ? 0f : (float)y / (height - 1);

            for (int x = 0; x < width; x++)
            {
                float normalizedX = width <= 1 ? 0f : (float)x / (width - 1);
                float steepness = terrainData.GetSteepness(normalizedX, normalizedZ);
                float heightValue = terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
                float stoneWeight = 0f;

                if (steepness > threshold)
                {
                    stoneWeight = 1f;
                    fullStoneMask[y, x] = true;
                }
                else if (blendRange > 0f && steepness > blendStart)
                {
                    stoneWeight = Mathf.InverseLerp(blendStart, threshold, steepness);
                    stoneWeight = Mathf.SmoothStep(0f, 1f, stoneWeight);
                }

                heights[y, x] = heightValue;
                stoneWeights[y, x] = stoneWeight;
            }
        }

        ApplyEdgeExpansion(
            stoneWeights,
            fullStoneMask,
            heights,
            width,
            height,
            expansionPixels,
            Mathf.Clamp01(maxExpansionWeight),
            downhillOnly,
            Mathf.Max(0f, uphillTolerance));

        return stoneWeights;
    }

    private static void ApplyEdgeExpansion(
        float[,] stoneWeights,
        bool[,] fullStoneMask,
        float[,] heights,
        int width,
        int height,
        int radius,
        float maxWeight,
        bool downhillOnly,
        float uphillTolerance)
    {
        if (radius <= 0 || maxWeight <= 0f)
        {
            return;
        }

        float[,] expandedWeights = (float[,])stoneWeights.Clone();
        float radiusSquared = radius * radius;

        for (int sourceY = 0; sourceY < height; sourceY++)
        {
            for (int sourceX = 0; sourceX < width; sourceX++)
            {
                if (!fullStoneMask[sourceY, sourceX])
                {
                    continue;
                }

                int minY = Mathf.Max(0, sourceY - radius);
                int maxY = Mathf.Min(height - 1, sourceY + radius);
                int minX = Mathf.Max(0, sourceX - radius);
                int maxX = Mathf.Min(width - 1, sourceX + radius);
                float sourceHeight = heights[sourceY, sourceX];

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

                        if (downhillOnly && heights[y, x] > sourceHeight + uphillTolerance)
                        {
                            continue;
                        }

                        float distance = Mathf.Sqrt(distanceSquared);
                        float falloff = 1f - Mathf.Clamp01(distance / radius);
                        float weight = Mathf.SmoothStep(0f, maxWeight, falloff);

                        if (weight > expandedWeights[y, x])
                        {
                            expandedWeights[y, x] = weight;
                        }
                    }
                }
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                stoneWeights[y, x] = expandedWeights[y, x];
            }
        }
    }

    private static void BlendStoneWeight(float[,,] alphamaps, int x, int y, int layerCount, int stoneLayerIndex, float targetStoneWeight)
    {
        targetStoneWeight = Mathf.Clamp01(targetStoneWeight);
        float otherWeightSum = 0f;

        for (int layer = 0; layer < layerCount; layer++)
        {
            if (layer != stoneLayerIndex)
            {
                otherWeightSum += alphamaps[y, x, layer];
            }
        }

        alphamaps[y, x, stoneLayerIndex] = targetStoneWeight;
        float remainingWeight = 1f - targetStoneWeight;

        if (otherWeightSum <= 0.0001f)
        {
            float fallbackWeight = layerCount > 1 ? remainingWeight / (layerCount - 1) : 0f;

            for (int layer = 0; layer < layerCount; layer++)
            {
                if (layer != stoneLayerIndex)
                {
                    alphamaps[y, x, layer] = fallbackWeight;
                }
            }

            return;
        }

        float scale = remainingWeight / otherWeightSum;
        for (int layer = 0; layer < layerCount; layer++)
        {
            if (layer != stoneLayerIndex)
            {
                alphamaps[y, x, layer] *= scale;
            }
        }
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
