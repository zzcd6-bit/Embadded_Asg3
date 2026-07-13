#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class YeStandardTeleportPointDeployer
{
    private const string RootName = "Ye_StandardTeleportPoints";
    private const string RegistryName = "Ye_TeleportPointRegistry";

    private struct PointTemplate
    {
        public string IdSuffix;
        public string DisplayName;
        public Vector3 Offset;
        public bool IsDefault;

        public PointTemplate(string idSuffix, string displayName, Vector3 offset, bool isDefault)
        {
            IdSuffix = idSuffix;
            DisplayName = displayName;
            Offset = offset;
            IsDefault = isDefault;
        }
    }

    [MenuItem("Ye Tools/Teleport/Deploy Standard Teleport Points")]
    public static void DeployStandardTeleportPoints()
    {
        Transform player = FindPlayerTransform();
        Vector3 origin = player != null ? player.position : Vector3.zero;
        string scenePrefix = SanitizeId(SceneManager.GetActiveScene().name);

        TeleportPointRegistry registry = EnsureRegistry();
        EnsurePlayerRuntimeComponents(player);

        Transform root = EnsureRoot();
        List<TeleportPoint> points = new List<TeleportPoint>();

        PointTemplate[] templates =
        {
            new PointTemplate("default", "Default Respawn", Vector3.zero, true),
            new PointTemplate("north", "North Teleport", new Vector3(0f, 0f, 12f), false),
            new PointTemplate("east", "East Teleport", new Vector3(12f, 0f, 0f), false),
            new PointTemplate("west", "West Teleport", new Vector3(-12f, 0f, 0f), false),
        };

        for (int i = 0; i < templates.Length; i++)
        {
            PointTemplate template = templates[i];
            string pointId = $"ye_{scenePrefix}_{template.IdSuffix}";
            TeleportPoint point = EnsureTeleportPoint(root, pointId, template.DisplayName, origin + template.Offset, template.IsDefault);
            points.Add(point);
        }

        ConfigureRegistry(registry, points);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[YeStandardTeleportPointDeployer] Deployed {points.Count} standard teleport point(s).");
    }

    private static TeleportPointRegistry EnsureRegistry()
    {
        TeleportPointRegistry registry = Object.FindAnyObjectByType<TeleportPointRegistry>(FindObjectsInactive.Include);
        if (registry != null)
        {
            Undo.RecordObject(registry, "Configure Teleport Registry");
            return registry;
        }

        GameObject registryObject = new GameObject(RegistryName);
        Undo.RegisterCreatedObjectUndo(registryObject, "Create Teleport Registry");
        return registryObject.AddComponent<TeleportPointRegistry>();
    }

    private static Transform EnsureRoot()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
            return existing.transform;

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Teleport Points Root");
        return root.transform;
    }

    private static TeleportPoint EnsureTeleportPoint(Transform root, string pointId, string displayName, Vector3 position, bool isDefault)
    {
        Transform existing = root.Find("TP_" + pointId);
        GameObject pointObject = existing != null ? existing.gameObject : new GameObject("TP_" + pointId);

        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(pointObject, "Create Teleport Point");
            pointObject.transform.SetParent(root);
        }

        Undo.RecordObject(pointObject.transform, "Move Teleport Point");
        pointObject.transform.position = position;
        pointObject.transform.rotation = Quaternion.identity;

        SphereCollider triggerCollider = pointObject.GetComponent<SphereCollider>();
        if (triggerCollider == null)
        {
            triggerCollider = Undo.AddComponent<SphereCollider>(pointObject);
        }

        Undo.RecordObject(triggerCollider, "Configure Teleport Point Collider");
        triggerCollider.isTrigger = false;
        triggerCollider.radius = 1.35f;

        Transform spawn = pointObject.transform.Find("Spawn");
        if (spawn == null)
        {
            GameObject spawnObject = new GameObject("Spawn");
            Undo.RegisterCreatedObjectUndo(spawnObject, "Create Teleport Spawn");
            spawnObject.transform.SetParent(pointObject.transform);
            spawn = spawnObject.transform;
        }

        Undo.RecordObject(spawn, "Configure Teleport Spawn");
        spawn.localPosition = Vector3.up * 0.15f;
        spawn.localRotation = Quaternion.identity;

        EnsureVisual(pointObject.transform);

        TeleportPoint point = pointObject.GetComponent<TeleportPoint>();
        if (point == null)
        {
            point = Undo.AddComponent<TeleportPoint>(pointObject);
        }

        SerializedObject serializedPoint = new SerializedObject(point);
        serializedPoint.FindProperty("pointId").stringValue = pointId;
        serializedPoint.FindProperty("displayName").stringValue = displayName;
        serializedPoint.FindProperty("spawnTransform").objectReferenceValue = spawn;
        serializedPoint.FindProperty("isDefaultRespawnPoint").boolValue = isDefault;
        serializedPoint.FindProperty("setRespawnOnRegister").boolValue = true;
        serializedPoint.FindProperty("setRespawnOnTeleportArrival").boolValue = true;
        serializedPoint.FindProperty("healWhenRegistered").boolValue = true;
        serializedPoint.FindProperty("fullHeal").boolValue = true;
        serializedPoint.FindProperty("restoreInkWhenRegistered").boolValue = true;
        serializedPoint.FindProperty("fullRestoreInk").boolValue = true;
        serializedPoint.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(point);
        return point;
    }

    private static void EnsureVisual(Transform pointRoot)
    {
        Transform visual = pointRoot.Find("Visual");
        if (visual == null)
        {
            GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visualObject.name = "Visual";
            Undo.RegisterCreatedObjectUndo(visualObject, "Create Teleport Visual");
            visualObject.transform.SetParent(pointRoot);
            visual = visualObject.transform;

            Collider visualCollider = visualObject.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Object.DestroyImmediate(visualCollider);
            }
        }

        Undo.RecordObject(visual, "Configure Teleport Visual");
        visual.localPosition = new Vector3(0f, 0.05f, 0f);
        visual.localRotation = Quaternion.identity;
        visual.localScale = new Vector3(1.8f, 0.1f, 1.8f);
    }

    private static void ConfigureRegistry(TeleportPointRegistry registry, List<TeleportPoint> points)
    {
        if (registry == null)
            return;

        SerializedObject serializedRegistry = new SerializedObject(registry);
        serializedRegistry.FindProperty("autoDiscoverScenePoints").boolValue = true;

        SerializedProperty scenePoints = serializedRegistry.FindProperty("scenePoints");
        for (int i = 0; i < points.Count; i++)
        {
            AddObjectReferenceIfMissing(scenePoints, points[i]);

            if (points[i] != null && points[i].IsDefaultRespawnPoint)
            {
                serializedRegistry.FindProperty("defaultRespawnPoint").objectReferenceValue = points[i];
            }
        }

        serializedRegistry.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(registry);
    }

    private static void AddObjectReferenceIfMissing(SerializedProperty arrayProperty, Object target)
    {
        if (target == null)
            return;

        for (int i = 0; i < arrayProperty.arraySize; i++)
        {
            if (arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue == target)
                return;
        }

        int index = arrayProperty.arraySize;
        arrayProperty.InsertArrayElementAtIndex(index);
        arrayProperty.GetArrayElementAtIndex(index).objectReferenceValue = target;
    }

    private static void EnsurePlayerRuntimeComponents(Transform player)
    {
        if (player == null)
        {
            Debug.LogWarning("[YeStandardTeleportPointDeployer] Player not found. Runtime player components were not added.");
            return;
        }

        EnsureComponent<PlayerTeleportService>(player.gameObject);
        EnsureComponent<PlayerResourceController>(player.gameObject);
        EnsureComponent<PlayerDeathRespawnController>(player.gameObject);
    }

    private static void EnsureComponent<T>(GameObject target) where T : Component
    {
        if (target.GetComponent<T>() != null)
            return;

        Undo.AddComponent<T>(target);
    }

    private static Transform FindPlayerTransform()
    {
        PlayerResourceController resourceController = Object.FindAnyObjectByType<PlayerResourceController>(FindObjectsInactive.Include);
        if (resourceController != null)
            return resourceController.transform;

        PlayerDamageReceiver damageReceiver = Object.FindAnyObjectByType<PlayerDamageReceiver>(FindObjectsInactive.Include);
        if (damageReceiver != null)
            return damageReceiver.transform;

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
            return taggedPlayer.transform;

        ActionPlayerController actionController = Object.FindAnyObjectByType<ActionPlayerController>(FindObjectsInactive.Include);
        if (actionController != null)
            return actionController.transform;

        PlayerController playerController = Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        return playerController != null ? playerController.transform : null;
    }

    private static string SanitizeId(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return "scene";

        char[] chars = source.ToLowerInvariant().ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            bool valid = c >= 'a' && c <= 'z' || c >= '0' && c <= '9';
            chars[i] = valid ? c : '_';
        }

        return new string(chars).Trim('_');
    }
}
#endif
