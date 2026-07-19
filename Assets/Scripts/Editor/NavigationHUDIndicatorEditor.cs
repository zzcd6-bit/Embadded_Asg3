using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NavigationHUDIndicator))]
public sealed class NavigationHUDIndicatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Auto Bind Scene Objects"))
        {
            NavigationHUDIndicatorAutoBinder.TryAutoBindSceneObjects();
        }
    }
}

[InitializeOnLoad]
public static class NavigationHUDIndicatorAutoBinder
{
    static NavigationHUDIndicatorAutoBinder()
    {
        EditorApplication.delayCall += TryAutoBindSceneObjects;
    }

    public static void TryAutoBindSceneObjects()
    {
        GameObject navigationObject = GameObject.Find("HUD_Canvas/Navigation UI");
        GameObject targetObject = GameObject.Find("Sample Mission Target");

        if (navigationObject == null || targetObject == null)
        {
            return;
        }

        NavigationHUDIndicator indicator = navigationObject.GetComponent<NavigationHUDIndicator>();
        if (indicator == null)
        {
            Undo.AddComponent<NavigationHUDIndicator>(navigationObject);
            indicator = navigationObject.GetComponent<NavigationHUDIndicator>();
        }

        Canvas canvas = navigationObject.GetComponentInParent<Canvas>();
        RectTransform ellipseSpace = canvas != null ? canvas.transform as RectTransform : null;
        Camera worldCamera = Camera.main;

        Undo.RecordObject(indicator, "Auto Bind Navigation HUD Indicator");
        indicator.EditorConfigureSceneReferences(
            targetObject.transform,
            canvas,
            ellipseSpace,
            worldCamera);
    }
}
