#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BuildingFootprintGizmo))]
public class BuildingFootprintGizmoEditor : Editor
{
    //Draws the custom inspector controls for footprint preview setup.
    //绘制占地预览设置的自定义 Inspector 控件。
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BuildingFootprintGizmo footprint = (BuildingFootprintGizmo)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Attach this to a building prefab root. The transform origin is treated as the placement pivot, and the grid is drawn under the model around that pivot.",
            MessageType.Info
        );

        using (new EditorGUI.DisabledScope(footprint.SourceItem == null))
        {
            if (GUILayout.Button("Apply Size From Source Item"))
            {
                Undo.RecordObject(footprint, "Apply Footprint Size");
                footprint.SyncFromSourceItem();
                EditorUtility.SetDirty(footprint);
            }
        }
    }

    [MenuItem("Tools/Building/Add Footprint Gizmo To Selection")]
    //Adds a footprint gizmo component to selected building roots.
    //为选中的建筑根物体添加占地 Gizmo 组件。
    public static void AddFootprintGizmoToSelection()
    {
        if (Selection.gameObjects.Length == 0)
        {
            return;
        }

        foreach (GameObject selected in Selection.gameObjects)
        {
            if (selected.GetComponent<BuildingFootprintGizmo>() != null)
            {
                continue;
            }

            Undo.AddComponent<BuildingFootprintGizmo>(selected);
            EditorUtility.SetDirty(selected);
        }
    }
}
#endif
