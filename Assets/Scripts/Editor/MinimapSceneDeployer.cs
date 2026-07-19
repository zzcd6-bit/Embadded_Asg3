using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MinimapSceneDeployer
{
    private const string ScenePath = "Assets/Scene/Merge Scene 2.unity";
    private const string RootName = "Minimap HUD";
    private const string CameraName = "Minimap Camera";

    private const string FrameSpritePath = "Assets/UI/HUD/Minimap/Minimap Frame.png";
    private const string ShapeSpritePath = "Assets/UI/HUD/Minimap/Minimap Shape.png";
    private const string PlayerSpritePath = "Assets/UI/HUD/Minimap/Minimap Player.png";

    [MenuItem("Tools/HUD/Deploy Minimap To Merge Scene 2")]
    public static void DeployToMergeScene2()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Deploy(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MinimapSceneDeployer] Minimap deployed to Merge Scene 2.");
    }

    public static void DeployFromCommandLine()
    {
        DeployToMergeScene2();
    }

    private static void Deploy(Scene scene)
    {
        Canvas canvas = FindOrCreateCanvas(scene);
        GameObject mapPlane = FindSceneObject(scene, "Plane");
        Transform player = FindPlayer(scene);

        GameObject existing = FindSceneObject(scene, RootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject minimapRoot = CreateUIObject(RootName, canvas.transform);
        RectTransform rootRect = minimapRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(24f, -24f);
        rootRect.sizeDelta = new Vector2(220f, 220f);

        Image shapeImage = minimapRoot.AddComponent<Image>();
        shapeImage.sprite = LoadSprite(ShapeSpritePath);
        shapeImage.color = Color.white;
        shapeImage.raycastTarget = false;

        Mask mask = minimapRoot.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject mapObject = CreateUIObject("Map Image", minimapRoot.transform);
        RawImage mapImage = mapObject.AddComponent<RawImage>();
        mapImage.color = Color.white;
        mapImage.raycastTarget = false;
        RectTransform mapRect = mapObject.GetComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapRect.pivot = new Vector2(0.5f, 0.5f);
        mapRect.anchoredPosition = Vector2.zero;
        mapRect.sizeDelta = new Vector2(640f, 640f);

        GameObject playerObject = CreateUIObject("Player Indicator", minimapRoot.transform);
        Image playerImage = playerObject.AddComponent<Image>();
        playerImage.sprite = LoadSprite(PlayerSpritePath);
        playerImage.raycastTarget = false;
        RectTransform playerRect = playerObject.GetComponent<RectTransform>();
        playerRect.anchorMin = new Vector2(0.5f, 0.5f);
        playerRect.anchorMax = new Vector2(0.5f, 0.5f);
        playerRect.pivot = new Vector2(0.5f, 0.5f);
        playerRect.anchoredPosition = Vector2.zero;
        playerRect.sizeDelta = new Vector2(34f, 34f);

        GameObject frameObject = CreateUIObject("Frame", minimapRoot.transform);
        Image frameImage = frameObject.AddComponent<Image>();
        frameImage.sprite = LoadSprite(FrameSpritePath);
        frameImage.raycastTarget = false;
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        StretchToParent(frameRect);

        GameObject cameraObject = new GameObject(CameraName);
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        Camera minimapCamera = cameraObject.AddComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        minimapCamera.nearClipPlane = 0.3f;
        minimapCamera.farClipPlane = 500f;
        minimapCamera.enabled = true;

        MinimapController controller = minimapRoot.AddComponent<MinimapController>();
        SerializedObject serializedController = new(controller);
        SetProperty(serializedController, "player", player);
        SetProperty(serializedController, "mapPlane", mapPlane);
        SetProperty(serializedController, "minimapCamera", minimapCamera);
        SetProperty(serializedController, "viewport", rootRect);
        SetProperty(serializedController, "mapImageRect", mapRect);
        SetProperty(serializedController, "mapImage", mapImage);
        SetProperty(serializedController, "playerIndicator", playerRect);
        serializedController.FindProperty("visibleWorldDiameter").floatValue = 60f;
        serializedController.FindProperty("minimapScale").floatValue = 1f;
        serializedController.FindProperty("renderTextureSize").intValue = 1024;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        controller.RefreshMapBounds();
        EditorUtility.SetDirty(minimapRoot);
        EditorUtility.SetDirty(cameraObject);
    }

    private static Canvas FindOrCreateCanvas(Scene scene)
    {
        Canvas fallbackCanvas = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return canvas;
                }

                fallbackCanvas ??= canvas;
            }
        }

        if (fallbackCanvas != null)
        {
            return fallbackCanvas;
        }

        GameObject canvasObject = new("Canvas");
        SceneManager.MoveGameObjectToScene(canvasObject, scene);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        gameObject.transform.SetParent(parent, false);
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        StretchToParent(rectTransform);
        return gameObject;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            throw new InvalidOperationException($"Sprite not found: {path}");
        }

        return sprite;
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = FindChildRecursive(root.transform, objectName);
            if (match != null)
            {
                return match.gameObject;
            }
        }

        return null;
    }

    private static Transform FindPlayer(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform taggedPlayer = FindTaggedChildRecursive(root.transform, "Player");
            if (taggedPlayer != null)
            {
                return taggedPlayer;
            }
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            PlayerController playerController = root.GetComponentInChildren<PlayerController>(true);
            if (playerController != null)
            {
                return playerController.transform;
            }

            ActionPlayerController actionPlayer = root.GetComponentInChildren<ActionPlayerController>(true);
            if (actionPlayer != null)
            {
                return actionPlayer.transform;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
        {
            return parent;
        }

        foreach (Transform child in parent)
        {
            Transform match = FindChildRecursive(child, objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static Transform FindTaggedChildRecursive(Transform parent, string tag)
    {
        if (parent.CompareTag(tag))
        {
            return parent;
        }

        foreach (Transform child in parent)
        {
            Transform match = FindTaggedChildRecursive(child, tag);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static void SetProperty(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
    }
}
