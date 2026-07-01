using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BambooFogZone))]
public class BambooFogZoneEditor : Editor
{
    private bool showAdvanced;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty fogDensity = serializedObject.FindProperty("fogDensity");
        SerializedProperty autoRadiusFromDensity = serializedObject.FindProperty("autoRadiusFromDensity");
        SerializedProperty triggerRadius = serializedObject.FindProperty("triggerRadius");
        SerializedProperty visibilityRadius = serializedObject.FindProperty("visibilityRadius");
        SerializedProperty clearRadius = serializedObject.FindProperty("clearRadius");
        SerializedProperty transitionDuration = serializedObject.FindProperty("transitionDuration");
        SerializedProperty priority = serializedObject.FindProperty("priority");
        SerializedProperty requirePlayerTag = serializedObject.FindProperty("requirePlayerTag");

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(fogDensity, new GUIContent("Fog Density"));

        using (new EditorGUI.DisabledScope(true))
        {
            BambooFogZone previewZone = (BambooFogZone)target;
            BambooFogSettings previewSettings = previewZone.GetSettings();
            EditorGUILayout.FloatField("Visibility Radius", previewSettings.visibilityDistance);
            EditorGUILayout.FloatField("Clear Radius", previewSettings.clearRadius);
        }

        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true);
        if (showAdvanced)
        {
            EditorGUILayout.PropertyField(autoRadiusFromDensity, new GUIContent("Auto Radius From Density"));
            EditorGUILayout.PropertyField(triggerRadius, new GUIContent("Trigger Radius"));

            if (!autoRadiusFromDensity.boolValue)
            {
                EditorGUILayout.PropertyField(visibilityRadius, new GUIContent("Visibility Radius"));
                EditorGUILayout.PropertyField(clearRadius, new GUIContent("Clear Radius"));
            }

            EditorGUILayout.PropertyField(transitionDuration, new GUIContent("Transition Duration"));
            EditorGUILayout.PropertyField(priority);
            EditorGUILayout.PropertyField(requirePlayerTag, new GUIContent("Require Player Tag"));
        }

        bool changed = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();

        BambooFogZone zone = (BambooFogZone)target;
        BambooFogController controller = FindFogController();

        if (changed && controller != null)
        {
            controller.ForcePreviewZoneSettings(zone);
            EditorUtility.SetDirty(controller);
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);

        if (GUILayout.Button("Teleport Player Here And Preview Fog"))
        {
            if (controller != null)
            {
                controller.PreviewZone(zone, true);
                EditorUtility.SetDirty(controller);
                SceneView.RepaintAll();
            }
            else
            {
                Debug.LogWarning("No BambooFogController found in the scene.");
            }
        }

        if (GUILayout.Button("Preview Fog Without Teleport"))
        {
            if (controller != null)
            {
                controller.PreviewZone(zone, false);
                EditorUtility.SetDirty(controller);
                SceneView.RepaintAll();
            }
            else
            {
                Debug.LogWarning("No BambooFogController found in the scene.");
            }
        }

        EditorGUILayout.HelpBox("This preview works in Edit Mode and Play Mode. While this zone is being previewed, changing its fog values updates the effect immediately.", MessageType.Info);
    }

    private static BambooFogController FindFogController()
    {
        if (BambooFogController.Instance != null)
        {
            return BambooFogController.Instance;
        }

        return FindAnyObjectByType<BambooFogController>();
    }
}
