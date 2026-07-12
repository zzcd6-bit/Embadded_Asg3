using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class YeInteractionOptionUIAutoRunner
{
    private const string MarkerRelativePath = "Temp/YeRebuildInteractionOptionUI.once";

    static YeInteractionOptionUIAutoRunner()
    {
        EditorApplication.delayCall += RunOnceIfRequested;
    }

    private static void RunOnceIfRequested()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string markerPath = Path.Combine(projectRoot, MarkerRelativePath);

        if (!File.Exists(markerPath))
        {
            return;
        }

        File.Delete(markerPath);
        YeInteractionOptionUIBuilder.RebuildFromCommandLine();
    }
}
