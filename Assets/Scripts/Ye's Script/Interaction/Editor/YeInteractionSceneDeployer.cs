using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class YeInteractionSceneDeployer
{
    private const string YeScenePath = "Assets/Scene/Ye Scene.unity";

    [MenuItem("Tools/Ye/Deploy Interaction System")]
    public static void DeployYeSceneMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        DeployYeScene();
        EditorUtility.DisplayDialog(
            "Ye Interaction",
            "Interaction system deployed to Assets/Scene/Ye Scene.unity.",
            "OK");
    }

    public static void DeployYeSceneFromCommandLine()
    {
        DeployYeScene();
    }

    private static void DeployYeScene()
    {
        Scene scene = EditorSceneManager.OpenScene(YeScenePath, OpenSceneMode.Single);

        GameObject player = FindPlayer();
        if (player == null)
        {
            Debug.LogError("[YeInteractionSceneDeployer] Player not found in Ye Scene.");
            return;
        }

        InteractionRollBoxUI rollBoxUI = EnsureInteractionUI();
        InteractionSensor sensor = EnsureSensor(player);
        InteractionManager manager = EnsureManager(player, sensor, rollBoxUI);
        EnsureInputBridge(player, manager);
        EnsureEventSystem();
        EnsureTestReactables(player.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[YeInteractionSceneDeployer] Ye interaction system deployed.");
    }

    private static GameObject FindPlayer()
    {
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            return taggedPlayer;
        }

        PlayerController playerController = Object.FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            return playerController.gameObject;
        }

        ActionPlayerController actionPlayerController = Object.FindObjectOfType<ActionPlayerController>();
        return actionPlayerController != null ? actionPlayerController.gameObject : null;
    }

    private static InteractionSensor EnsureSensor(GameObject player)
    {
        Transform sensorTransform = player.transform.Find("InteractionSensor");
        GameObject sensorObject = sensorTransform != null
            ? sensorTransform.gameObject
            : new GameObject("InteractionSensor");

        if (sensorTransform == null)
        {
            sensorObject.transform.SetParent(player.transform, false);
        }

        sensorObject.transform.localPosition = new Vector3(0f, 1f, 0f);
        sensorObject.transform.localRotation = Quaternion.identity;
        sensorObject.transform.localScale = Vector3.one;

        SphereCollider sphere = sensorObject.GetComponent<SphereCollider>();
        if (sphere == null)
        {
            sphere = sensorObject.AddComponent<SphereCollider>();
        }

        sphere.isTrigger = true;
        sphere.radius = 3f;
        sphere.center = Vector3.zero;

        if (player.GetComponent<Rigidbody>() == null && player.GetComponent<CharacterController>() != null)
        {
            Rigidbody rigidbody = sensorObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = sensorObject.AddComponent<Rigidbody>();
            }

            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
        }

        InteractionSensor sensor = sensorObject.GetComponent<InteractionSensor>();
        if (sensor == null)
        {
            sensor = sensorObject.AddComponent<InteractionSensor>();
        }

        SetLayerMaskField(sensor, "interactableLayers", ~0);
        SetBoolField(sensor, "searchInParents", true);
        EditorUtility.SetDirty(sensor);

        return sensor;
    }

    private static InteractionManager EnsureManager(GameObject player, InteractionSensor sensor, InteractionRollBoxUI rollBoxUI)
    {
        InteractionManager manager = player.GetComponent<InteractionManager>();
        if (manager == null)
        {
            manager = player.AddComponent<InteractionManager>();
        }

        SetObjectField(manager, "sensor", sensor);
        SetObjectField(manager, "rollBoxUI", rollBoxUI);
        SetBoolField(manager, "pollKeyboardInput", false);
        SetBoolField(manager, "pollMouseWheelInput", true);
        SetBoolField(manager, "selectNeighbourAfterRemoval", true);
        SetFloatField(manager, "cleanupInterval", 0.5f);
        EditorUtility.SetDirty(manager);

        return manager;
    }

    private static void EnsureInputBridge(GameObject player, InteractionManager manager)
    {
        YeInteractionInputBridge bridge = player.GetComponent<YeInteractionInputBridge>();
        if (bridge == null)
        {
            bridge = player.AddComponent<YeInteractionInputBridge>();
        }

        SetObjectField(bridge, "interactionManager", manager);
        EditorUtility.SetDirty(bridge);
    }

    private static InteractionRollBoxUI EnsureInteractionUI()
    {
        Canvas canvas = EnsureCanvas();
        GameObject rollBox = EnsureChild(canvas.transform, "InteractionRollBox", typeof(RectTransform));
        RectTransform rollRect = rollBox.GetComponent<RectTransform>();
        rollRect.anchorMin = new Vector2(1f, 0.5f);
        rollRect.anchorMax = new Vector2(1f, 0.5f);
        rollRect.pivot = new Vector2(1f, 0.5f);
        rollRect.anchoredPosition = new Vector2(-80f, 0f);
        rollRect.sizeDelta = new Vector2(320f, 230f);

        Image rollImage = rollBox.GetComponent<Image>();
        if (rollImage == null)
        {
            rollImage = rollBox.AddComponent<Image>();
        }

        rollImage.color = new Color(0f, 0f, 0f, 0.15f);
        rollImage.raycastTarget = false;

        GameObject background = EnsureChild(rollBox.transform, "Background", typeof(RectTransform));
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        Stretch(backgroundRect);
        Image backgroundImage = background.GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = background.AddComponent<Image>();
        }

        backgroundImage.color = new Color(0f, 0f, 0f, 0.35f);
        backgroundImage.raycastTarget = false;

        GameObject scrollView = EnsureChild(rollBox.transform, "ScrollView", typeof(RectTransform));
        RectTransform scrollRectTransform = scrollView.GetComponent<RectTransform>();
        Stretch(scrollRectTransform);
        ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = scrollView.AddComponent<ScrollRect>();
        }

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = false;
        scrollRect.scrollSensitivity = 0f;

        GameObject viewport = EnsureChild(scrollView.transform, "Viewport", typeof(RectTransform));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect);
        Image viewportImage = viewport.GetComponent<Image>();
        if (viewportImage == null)
        {
            viewportImage = viewport.AddComponent<Image>();
        }

        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewportImage.raycastTarget = true;

        RectMask2D mask = viewport.GetComponent<RectMask2D>();
        if (mask == null)
        {
            viewport.AddComponent<RectMask2D>();
        }

        GameObject content = EnsureChild(viewport.transform, "Content", typeof(RectTransform));
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = content.AddComponent<VerticalLayoutGroup>();
        }

        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = content.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        InteractionOptionUI optionTemplate = EnsureOptionTemplate(content.transform);

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        InteractionRollBoxUI rollBoxUI = rollBox.GetComponent<InteractionRollBoxUI>();
        if (rollBoxUI == null)
        {
            rollBoxUI = rollBox.AddComponent<InteractionRollBoxUI>();
        }

        SetObjectField(rollBoxUI, "root", rollBox);
        SetObjectField(rollBoxUI, "scrollRect", scrollRect);
        SetObjectField(rollBoxUI, "viewport", viewportRect);
        SetObjectField(rollBoxUI, "content", contentRect);
        SetObjectField(rollBoxUI, "optionPrefab", optionTemplate);
        EditorUtility.SetDirty(rollBoxUI);

        rollBox.SetActive(false);
        return rollBoxUI;
    }

    private static Canvas EnsureCanvas()
    {
        GameObject canvasObject = GameObject.Find("Ye Interaction Canvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("Ye Interaction Canvas", typeof(RectTransform));
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = canvasObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        return canvas;
    }

    private static InteractionOptionUI EnsureOptionTemplate(Transform content)
    {
        GameObject option = EnsureChild(content, "InteractionOptionTemplate", typeof(RectTransform));
        RectTransform optionRect = option.GetComponent<RectTransform>();
        optionRect.sizeDelta = new Vector2(300f, 46f);

        Image optionImage = option.GetComponent<Image>();
        if (optionImage == null)
        {
            optionImage = option.AddComponent<Image>();
        }

        optionImage.color = new Color(1f, 1f, 1f, 0f);
        optionImage.raycastTarget = true;

        CanvasGroup canvasGroup = option.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = option.AddComponent<CanvasGroup>();
        }

        LayoutElement layoutElement = option.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = option.AddComponent<LayoutElement>();
        }

        layoutElement.minHeight = 46f;
        layoutElement.preferredHeight = 46f;

        GameObject selectedBackground = EnsureChild(option.transform, "SelectedBackground", typeof(RectTransform));
        RectTransform selectedRect = selectedBackground.GetComponent<RectTransform>();
        Stretch(selectedRect);
        Image selectedImage = selectedBackground.GetComponent<Image>();
        if (selectedImage == null)
        {
            selectedImage = selectedBackground.AddComponent<Image>();
        }

        selectedImage.color = new Color(0.05f, 0.09f, 0.12f, 0.82f);
        selectedImage.raycastTarget = false;

        GameObject optionText = EnsureChild(option.transform, "OptionText", typeof(RectTransform));
        RectTransform textRect = optionText.GetComponent<RectTransform>();
        Stretch(textRect);
        textRect.offsetMin = new Vector2(20f, 0f);
        textRect.offsetMax = new Vector2(-20f, 0f);

        TextMeshProUGUI text = optionText.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = optionText.AddComponent<TextMeshProUGUI>();
        }

        text.text = "Interact";
        text.fontSize = 26f;
        text.alignment = TextAlignmentOptions.MidlineRight;
        text.raycastTarget = false;
        text.color = Color.white;

        InteractionOptionUI optionUI = option.GetComponent<InteractionOptionUI>();
        if (optionUI == null)
        {
            optionUI = option.AddComponent<InteractionOptionUI>();
        }

        SetObjectField(optionUI, "optionText", text);
        SetObjectField(optionUI, "selectedBackground", selectedBackground);
        SetObjectField(optionUI, "canvasGroup", canvasGroup);
        SetFloatField(optionUI, "normalAlpha", 0.65f);
        SetFloatField(optionUI, "selectedAlpha", 1f);
        selectedBackground.SetActive(false);
        option.SetActive(false);
        EditorUtility.SetDirty(optionUI);

        return optionUI;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private static void EnsureTestReactables(Transform player)
    {
        CreateTestReactable(
            "Ye Interaction Test Critical",
            player.position + player.forward * 1.8f + player.right * -1.1f,
            "Start Quest",
            InteractionCategory.Critical,
            Color.cyan);

        CreateTestReactable(
            "Ye Interaction Test Normal",
            player.position + player.forward * 2.1f,
            "Talk",
            InteractionCategory.Normal,
            new Color(0.9f, 0.8f, 0.25f, 1f));

        CreateTestReactable(
            "Ye Interaction Test Pickup",
            player.position + player.forward * 1.8f + player.right * 1.1f,
            "Pick Herb",
            InteractionCategory.Pickup,
            Color.green);
    }

    private static void CreateTestReactable(
        string name,
        Vector3 position,
        string optionName,
        InteractionCategory category,
        Color color)
    {
        GameObject reactableObject = GameObject.Find(name);
        if (reactableObject == null)
        {
            reactableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            reactableObject.name = name;
        }

        reactableObject.transform.position = position;
        reactableObject.transform.localScale = Vector3.one * 0.6f;

        Renderer renderer = reactableObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateDebugMaterial(name, color);
        }

        ReactableObject reactable = reactableObject.GetComponent<ReactableObject>();
        if (reactable == null)
        {
            reactable = reactableObject.AddComponent<ReactableObject>();
        }

        SetStringField(reactable, "optionName", optionName);
        SetEnumField(reactable, "category", (int)category);
        SetBoolField(reactable, "interactable", true);
        SetBoolField(reactable, "disableAfterInteraction", false);

        YeInteractionDebugAction action = reactableObject.GetComponent<YeInteractionDebugAction>();
        if (action == null)
        {
            action = reactableObject.AddComponent<YeInteractionDebugAction>();
        }

        SetObjectField(action, "reactableObject", reactable);
        SetBoolField(action, "disableReactableAfterUse", true);
        SetBoolField(action, "destroyAfterUse", false);

        if (reactable.OnInteractEvent.GetPersistentEventCount() == 0)
        {
            UnityEventTools.AddPersistentListener(reactable.OnInteractEvent, action.Run);
        }

        EditorUtility.SetDirty(reactableObject);
        EditorUtility.SetDirty(reactable);
        EditorUtility.SetDirty(action);
    }

    private static Material CreateDebugMaterial(string name, Color color)
    {
        string folder = "Assets/Scripts/Ye's Script/Interaction/Generated";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/Scripts/Ye's Script/Interaction", "Generated");
        }

        string path = $"{folder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject EnsureChild(Transform parent, string name, params System.Type[] components)
    {
        Transform child = parent.Find(name);
        GameObject gameObject = child != null ? child.gameObject : new GameObject(name, components);
        if (child == null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return gameObject;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void SetObjectField(Object target, string fieldName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property != null)
        {
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private static void SetStringField(Object target, string fieldName, string value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property != null)
        {
            property.stringValue = value;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private static void SetBoolField(Object target, string fieldName, bool value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property != null)
        {
            property.boolValue = value;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private static void SetFloatField(Object target, string fieldName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property != null)
        {
            property.floatValue = value;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private static void SetEnumField(Object target, string fieldName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property != null)
        {
            property.enumValueIndex = value;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private static void SetLayerMaskField(Object target, string fieldName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property != null)
        {
            property.intValue = value;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
