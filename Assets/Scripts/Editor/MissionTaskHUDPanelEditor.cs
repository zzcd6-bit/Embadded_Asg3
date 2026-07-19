using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MissionTaskHUDPanel))]
public sealed class MissionTaskHUDPanelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MissionTaskHUDPanel panel = (MissionTaskHUDPanel)target;
        if (GUILayout.Button("Refresh Mission HUD"))
        {
            panel.BuildOrRefresh();
            EditorUtility.SetDirty(panel);
        }

        if (GUILayout.Button("Create/Find In HUD Canvas"))
        {
            MissionTaskHUDPanelBuilder.CreateOrFindPanel();
        }
    }
}

public static class MissionTaskHUDPanelBuilder
{
    [MenuItem("Tools/Navigation/Create Mission Task HUD Panel")]
    public static void CreateOrFindPanel()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("MissionTaskHUDPanelBuilder could not find a Canvas in the open scene.");
            return;
        }

        Transform existing = canvas.transform.Find("Mission Task Panel");
        GameObject panelObject = existing != null ? existing.gameObject : new GameObject("Mission Task Panel");
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(panelObject, "Create Mission Task HUD Panel");
            panelObject.transform.SetParent(canvas.transform, false);
        }

        MissionTaskHUDPanel panel = panelObject.GetComponent<MissionTaskHUDPanel>();
        if (panel == null)
        {
            panel = Undo.AddComponent<MissionTaskHUDPanel>(panelObject);
        }

        panel.BuildOrRefresh();
        Selection.activeGameObject = panelObject;
        EditorUtility.SetDirty(panelObject);
    }

    private static Canvas FindCanvas()
    {
        GameObject hudCanvas = GameObject.Find("HUD_Canvas");
        if (hudCanvas != null && hudCanvas.TryGetComponent(out Canvas exactCanvas))
        {
            return exactCanvas;
        }

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.name.Contains("HUD") || canvas.name.Contains("Canvas"))
            {
                return canvas;
            }
        }

        return canvases.Length > 0 ? canvases[0] : null;
    }
}
