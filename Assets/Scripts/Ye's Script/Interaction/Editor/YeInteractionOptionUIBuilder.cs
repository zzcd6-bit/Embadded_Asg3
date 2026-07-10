using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class YeInteractionOptionUIBuilder
{
    private const string YeScenePath = "Assets/Scene/Ye Scene.unity";
    private const string NormalButtonPath = "Assets/UI/Main Buttons/Button 2 Normal.png";
    private const string ClickedButtonPath = "Assets/UI/Main Buttons/Button 2 Clicked Color.png";
    private const string SliderBarPath = "Assets/UI/Sliders/Slider Bar.png";
    private const string SliderThumbPath = "Assets/UI/Sliders/Slider Thumbtrack 2.png";

    [MenuItem("Tools/Ye/Rebuild Interaction Option UI")]
    public static void RebuildMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        RebuildInteractionOptionUI();
        EditorUtility.DisplayDialog("Ye Interaction UI", "Interaction option UI rebuilt with Assets/UI sprites.", "OK");
    }

    public static void RebuildFromCommandLine()
    {
        RebuildInteractionOptionUI();
    }

    private static void RebuildInteractionOptionUI()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != YeScenePath)
        {
            scene = EditorSceneManager.OpenScene(YeScenePath, OpenSceneMode.Single);
        }
        else
        {
            EditorSceneManager.SaveScene(scene);
        }

        Sprite normalButton = LoadSprite(NormalButtonPath);
        Sprite clickedButton = LoadSprite(ClickedButtonPath);
        Sprite sliderBar = LoadSprite(SliderBarPath);
        Sprite sliderThumb = LoadSprite(SliderThumbPath);

        Canvas canvas = EnsureCanvas();
        GameObject rollBox = EnsureChild(canvas.transform, "InteractionRollBox", typeof(RectTransform));
        ClearChildren(rollBox.transform);

        RectTransform rollRect = rollBox.GetComponent<RectTransform>();
        rollRect.anchorMin = new Vector2(0.66f, 0.55f);
        rollRect.anchorMax = new Vector2(0.66f, 0.55f);
        rollRect.pivot = new Vector2(0.5f, 1f);
        rollRect.anchoredPosition = Vector2.zero;
        rollRect.sizeDelta = new Vector2(410f, 380f);

        RemoveIfExists<Image>(rollBox);

        GameObject scrollView = EnsureChild(rollBox.transform, "OptionButtonScrollView", typeof(RectTransform));
        RectTransform scrollViewRect = scrollView.GetComponent<RectTransform>();
        scrollViewRect.anchorMin = new Vector2(0f, 0f);
        scrollViewRect.anchorMax = new Vector2(0f, 1f);
        scrollViewRect.pivot = new Vector2(0f, 1f);
        scrollViewRect.anchoredPosition = new Vector2(0f, 0f);
        scrollViewRect.sizeDelta = new Vector2(304f, 0f);

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

        if (viewport.GetComponent<RectMask2D>() == null)
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

        VerticalLayoutGroup layoutGroup = content.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = content.AddComponent<VerticalLayoutGroup>();
        }

        layoutGroup.padding = new RectOffset(0, 0, 0, 0);
        layoutGroup.spacing = 14f;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        ContentSizeFitter sizeFitter = content.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = content.AddComponent<ContentSizeFitter>();
        }

        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        InteractionOptionUI optionTemplate = CreateOptionTemplate(
            content.transform,
            normalButton,
            clickedButton);

        GameObject scrollbarRoot = CreateVerticalScrollbar(
            rollBox.transform,
            sliderBar,
            sliderThumb);

        Scrollbar scrollbar = scrollbarRoot.GetComponent<Scrollbar>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scrollRect.verticalScrollbarSpacing = 18f;

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

        rollBox.SetActive(false);
        EditorUtility.SetDirty(rollBox);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[YeInteractionOptionUIBuilder] Rebuilt interaction option UI with Assets/UI button and slider sprites.");
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

    private static InteractionOptionUI CreateOptionTemplate(
        Transform content,
        Sprite normalButton,
        Sprite clickedButton)
    {
        GameObject option = new GameObject("InteractionOptionTemplate", typeof(RectTransform));
        option.transform.SetParent(content, false);

        RectTransform optionRect = option.GetComponent<RectTransform>();
        optionRect.sizeDelta = new Vector2(292f, 78f);

        Image buttonImage = option.AddComponent<Image>();
        buttonImage.sprite = normalButton;
        buttonImage.type = Image.Type.Sliced;
        buttonImage.raycastTarget = true;

        Button button = option.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = clickedButton,
            pressedSprite = clickedButton,
            selectedSprite = clickedButton,
            disabledSprite = normalButton
        };

        CanvasGroup canvasGroup = option.AddComponent<CanvasGroup>();

        LayoutElement layoutElement = option.AddComponent<LayoutElement>();
        layoutElement.minHeight = 78f;
        layoutElement.preferredHeight = 78f;
        layoutElement.preferredWidth = 292f;

        GameObject labelObject = new GameObject("OptionText", typeof(RectTransform));
        labelObject.transform.SetParent(option.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        Stretch(labelRect);
        labelRect.offsetMin = new Vector2(30f, 6f);
        labelRect.offsetMax = new Vector2(-30f, -6f);

        TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
        text.text = "Interact";
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.15f, 0.1f, 0.08f, 1f);
        text.raycastTarget = false;

        InteractionOptionUI optionUI = option.AddComponent<InteractionOptionUI>();
        SetObjectField(optionUI, "optionText", text);
        SetObjectField(optionUI, "canvasGroup", canvasGroup);
        SetObjectField(optionUI, "button", button);
        SetObjectField(optionUI, "buttonImage", buttonImage);
        SetObjectField(optionUI, "normalSprite", normalButton);
        SetObjectField(optionUI, "selectedSprite", clickedButton);
        SetFloatField(optionUI, "normalAlpha", 1f);
        SetFloatField(optionUI, "selectedAlpha", 1f);

        option.SetActive(false);
        return optionUI;
    }

    private static GameObject CreateVerticalScrollbar(Transform parent, Sprite sliderBar, Sprite sliderThumb)
    {
        GameObject scrollbarObject = new GameObject("OptionScrollbar", typeof(RectTransform));
        scrollbarObject.transform.SetParent(parent, false);
        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 1f);
        scrollbarRect.anchoredPosition = new Vector2(0f, 0f);
        scrollbarRect.sizeDelta = new Vector2(78f, 0f);

        Image transparentHitArea = scrollbarObject.AddComponent<Image>();
        transparentHitArea.color = new Color(1f, 1f, 1f, 0f);
        transparentHitArea.raycastTarget = true;

        GameObject trackVisual = new GameObject("SliderBarVertical", typeof(RectTransform));
        trackVisual.transform.SetParent(scrollbarObject.transform, false);
        RectTransform trackRect = trackVisual.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0.5f, 0.5f);
        trackRect.anchorMax = new Vector2(0.5f, 0.5f);
        trackRect.pivot = new Vector2(0.5f, 0.5f);
        trackRect.anchoredPosition = Vector2.zero;
        trackRect.sizeDelta = new Vector2(330f, 34f);
        trackRect.localEulerAngles = new Vector3(0f, 0f, 90f);

        Image trackImage = trackVisual.AddComponent<Image>();
        trackImage.sprite = sliderBar;
        trackImage.type = Image.Type.Sliced;
        trackImage.raycastTarget = false;

        GameObject slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbarObject.transform, false);
        RectTransform slidingAreaRect = slidingArea.GetComponent<RectTransform>();
        slidingAreaRect.anchorMin = new Vector2(0f, 0f);
        slidingAreaRect.anchorMax = new Vector2(1f, 1f);
        slidingAreaRect.offsetMin = new Vector2(0f, 34f);
        slidingAreaRect.offsetMax = new Vector2(0f, -34f);

        GameObject handle = new GameObject("SliderThumbButton", typeof(RectTransform));
        handle.transform.SetParent(slidingArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0.5f);
        handleRect.anchorMax = new Vector2(1f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(0f, 72f);
        handleRect.localEulerAngles = new Vector3(0f, 0f, 90f);

        Image handleImage = handle.AddComponent<Image>();
        handleImage.sprite = sliderThumb;
        handleImage.type = Image.Type.Sliced;
        handleImage.raycastTarget = true;

        Button thumbButton = handle.AddComponent<Button>();
        thumbButton.targetGraphic = handleImage;
        thumbButton.transition = Selectable.Transition.ColorTint;

        Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.size = 0.35f;

        return scrollbarObject;
    }

    private static Sprite LoadSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            throw new System.InvalidOperationException($"Sprite not found or could not be imported: {path}");
        }

        return sprite;
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

    private static void ClearChildren(Transform transform)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(transform.GetChild(i).gameObject);
        }
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

    private static void RemoveIfExists<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component != null)
        {
            Object.DestroyImmediate(component);
        }
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
}
