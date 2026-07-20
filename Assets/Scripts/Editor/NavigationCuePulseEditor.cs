using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NavigationCuePulse))]
public sealed class NavigationCuePulseEditor : Editor
{
    private void OnEnable()
    {
        EditorApplication.update += UpdatePreview;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdatePreview;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NavigationCuePulse cuePulse = (NavigationCuePulse)target;
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            string label = cuePulse.EditorPreviewActive ? "Stop Preview" : "Start Preview";
            if (GUILayout.Button(label))
            {
                if (cuePulse.EditorPreviewActive)
                {
                    cuePulse.StopEditorPreview();
                }
                else
                {
                    cuePulse.StartEditorPreview();
                }

                SceneView.RepaintAll();
            }
        }

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Preview is only available outside Play Mode.", MessageType.Info);
        }
    }

    private void UpdatePreview()
    {
        foreach (Object selectedObject in targets)
        {
            if (selectedObject is NavigationCuePulse cuePulse && cuePulse.EditorPreviewActive)
            {
                cuePulse.EditorPreviewTick((float)EditorApplication.timeSinceStartup);
            }
        }

        SceneView.RepaintAll();
    }
}
