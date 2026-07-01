using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GlobalWindController))]
public class GlobalWindControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);

        if (GUILayout.Button("Apply Wind Now"))
        {
            ((GlobalWindController)target).ApplyWind();
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Check Terrain Tree Wind Materials"))
        {
            CheckTerrainTreeWindMaterials();
        }
    }

    private static void CheckTerrainTreeWindMaterials()
    {
        Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsInactive.Exclude);
        StringBuilder report = new StringBuilder();

        for (int terrainIndex = 0; terrainIndex < terrains.Length; terrainIndex++)
        {
            Terrain terrain = terrains[terrainIndex];
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            TreePrototype[] prototypes = terrain.terrainData.treePrototypes;
            report.AppendLine($"{terrain.name}: {prototypes.Length} tree prototypes");

            for (int prototypeIndex = 0; prototypeIndex < prototypes.Length; prototypeIndex++)
            {
                GameObject prefab = prototypes[prototypeIndex].prefab;
                if (prefab == null)
                {
                    report.AppendLine($"  [{prototypeIndex}] Missing prefab");
                    continue;
                }

                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    report.AppendLine($"  [{prototypeIndex}] {prefab.name}: no renderers");
                    continue;
                }

                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    Material[] materials = renderer.sharedMaterials;
                    for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        Material material = materials[materialIndex];
                        if (material == null)
                        {
                            continue;
                        }

                        bool hasEnableWind = material.HasProperty("_EnableWind");
                        string windState = hasEnableWind ? material.GetFloat("_EnableWind").ToString("0.##") : "no _EnableWind";
                        string shaderName = material.shader != null ? material.shader.name : "Missing Shader";
                        report.AppendLine($"  [{prototypeIndex}] {prefab.name}/{material.name}: {shaderName}, wind={windState}");
                    }
                }
            }
        }

        if (report.Length == 0)
        {
            report.AppendLine("No active terrains with tree prototypes found.");
        }

        Debug.Log(report.ToString());
    }
}
