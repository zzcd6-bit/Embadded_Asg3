using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class YeGridPlacementBootstrap : MonoBehaviour
{
    [Serializable]
    private class RuntimeBuildItem
    {
        public string displayName = "Block";
        public GameObject prefab = null;
        public Vector2Int size = Vector2Int.one;
        public int cost = 0;
    }

    [Header("Scene References")]
    [SerializeField] private Camera placementCamera;
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private GridPlacementSystem gridPlacementSystem;
    [SerializeField] private PlacementController placementController;
    [SerializeField] private BuildModeController buildModeController;

    [Header("Grid")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector3 gridOrigin;
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Items")]
    [SerializeField] private BuildableItemData[] itemAssets;
    [SerializeField] private bool createFallbackItems = true;
    [SerializeField] private RuntimeBuildItem[] runtimeItems =
    {
        new() { displayName = "Block 1x1", size = new Vector2Int(1, 1) },
        new() { displayName = "Block 2x2", size = new Vector2Int(2, 2) }
    };

    [Header("UI")]
    [SerializeField] private bool createRuntimeInventory = true;
    [SerializeField] private Vector2 panelSize = new(520f, 104f);

    private readonly List<BuildableItemData> availableItems = new();
    private GameObject inventoryPanel;

    private void Awake()
    {
        ResolveSceneReferences();
        BuildItemList();

        if (createRuntimeInventory)
        {
            CreateInventoryUI();
        }
    }

    private void Start()
    {
        if (buildModeController != null)
        {
            buildModeController.SetBuildMode(false);
        }
    }

    private void LateUpdate()
    {
        if (inventoryPanel != null && buildModeController != null)
        {
            inventoryPanel.SetActive(buildModeController.IsBuildMode);
        }
    }

    private void ResolveSceneReferences()
    {
        if (placementCamera == null)
        {
            placementCamera = Camera.main;
        }

        if (inputHandler == null)
        {
            inputHandler = InputHandler.GetOrCreate();
        }

        if (gridPlacementSystem == null)
        {
            gridPlacementSystem = FindAnyObjectByType<GridPlacementSystem>();
        }

        if (gridPlacementSystem == null)
        {
            GameObject gridObject = new("GridPlacementSystem");
            gridPlacementSystem = gridObject.AddComponent<GridPlacementSystem>();
        }

        SetPrivateField(gridPlacementSystem, "cellSize", Mathf.Max(0.01f, cellSize));
        SetPrivateField(gridPlacementSystem, "origin", gridOrigin);

        if (placementController == null)
        {
            placementController = FindAnyObjectByType<PlacementController>();
        }

        if (placementController == null)
        {
            GameObject placementObject = new("PlacementController");
            placementController = placementObject.AddComponent<PlacementController>();
        }

        SetPrivateField(placementController, "placementCamera", placementCamera);
        SetPrivateField(placementController, "inputHandler", inputHandler);
        SetPrivateField(placementController, "gridPlacementSystem", gridPlacementSystem);
        SetPrivateField(placementController, "groundLayer", groundLayer);
        EnsurePlacementHelpers();

        if (buildModeController == null)
        {
            buildModeController = FindAnyObjectByType<BuildModeController>();
        }

        if (buildModeController == null)
        {
            GameObject buildModeObject = new("BuildModeController");
            buildModeController = buildModeObject.AddComponent<BuildModeController>();
        }

        SetPrivateField(buildModeController, "inputHandler", inputHandler);
        SetPrivateField(buildModeController, "placementController", placementController);
    }

    private void EnsurePlacementHelpers()
    {
        PlacementPreviewController previewController = placementController.GetComponent<PlacementPreviewController>();
        if (previewController == null)
        {
            previewController = placementController.gameObject.AddComponent<PlacementPreviewController>();
        }

        PlacementCommitter placementCommitter = placementController.GetComponent<PlacementCommitter>();
        if (placementCommitter == null)
        {
            placementCommitter = placementController.gameObject.AddComponent<PlacementCommitter>();
        }

        placementCommitter.Initialize(gridPlacementSystem);
        SetPrivateField(placementController, "previewController", previewController);
        SetPrivateField(placementController, "placementCommitter", placementCommitter);
    }

    private void BuildItemList()
    {
        availableItems.Clear();

        if (itemAssets != null)
        {
            foreach (BuildableItemData item in itemAssets)
            {
                if (item != null && item.BuildingPrefab != null)
                {
                    availableItems.Add(item);
                }
            }
        }

        if (runtimeItems != null)
        {
            foreach (RuntimeBuildItem item in runtimeItems)
            {
                BuildableItemData data = CreateItemData(item);
                if (data != null)
                {
                    availableItems.Add(data);
                }
            }
        }

        if (availableItems.Count == 0 && createFallbackItems)
        {
            availableItems.Add(CreateItemData(new RuntimeBuildItem
            {
                displayName = "Block 1x1",
                size = Vector2Int.one
            }));
        }
    }

    private BuildableItemData CreateItemData(RuntimeBuildItem item)
    {
        if (item == null)
        {
            return null;
        }

        GameObject prefab = item.prefab != null ? item.prefab : CreatePrimitivePrefab(item.displayName, item.size);
        if (prefab == null)
        {
            return null;
        }

        BuildableItemData data = ScriptableObject.CreateInstance<BuildableItemData>();
        data.name = item.displayName;
        SetPrivateField(data, "displayName", item.displayName);
        SetPrivateField(data, "buildingPrefab", prefab);
        SetPrivateField(data, "size", new Vector2Int(Mathf.Max(1, item.size.x), Mathf.Max(1, item.size.y)));
        SetPrivateField(data, "cost", Mathf.Max(0, item.cost));
        return data;
    }

    private static GameObject CreatePrimitivePrefab(string displayName, Vector2Int size)
    {
        Vector2Int safeSize = new(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefab.name = $"YeRuntime_{displayName}_Prefab";
        prefab.transform.position = new Vector3(0f, -10000f, 0f);
        prefab.transform.localScale = new Vector3(safeSize.x, 1f, safeSize.y);

        Renderer renderer = prefab.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateMaterial(new Color(0.35f, 0.62f, 0.78f, 1f));
        }

        prefab.hideFlags = HideFlags.HideAndDontSave;
        return prefab;
    }

    private void CreateInventoryUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new("Ye Build Inventory Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        inventoryPanel = new GameObject("Ye Build Inventory Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        inventoryPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = inventoryPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 26f);
        panelRect.sizeDelta = panelSize;

        Image panelImage = inventoryPanel.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.10f, 0.11f, 0.86f);

        AddLabel(inventoryPanel.transform, "Press Tab for build mode", new Vector2(0f, 34f), new Vector2(panelSize.x - 24f, 24f), 14, TextAnchor.MiddleCenter);

        float startX = -((availableItems.Count - 1) * 94f) * 0.5f;
        for (int i = 0; i < availableItems.Count; i++)
        {
            BuildableItemData item = availableItems[i];
            Button button = AddButton(inventoryPanel.transform, item.DisplayName, new Vector2(startX + i * 94f, -14f));
            button.onClick.AddListener(() => placementController.StartPlacement(item));
        }

        inventoryPanel.SetActive(false);
    }

    private static Text AddLabel(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
    {
        GameObject labelObject = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text label = labelObject.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        return label;
    }

    private static Button AddButton(Transform parent, string text, Vector2 position)
    {
        GameObject buttonObject = new(text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(84f, 42f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.25f, 0.29f, 0.95f);

        AddLabel(buttonObject.transform, text, Vector2.zero, new Vector2(76f, 36f), 12, TextAnchor.MiddleCenter);
        return buttonObject.GetComponent<Button>();
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        shader ??= Shader.Find("Standard");

        Material material = new(shader)
        {
            color = color
        };

        return material;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null)
        {
            return;
        }

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(target, value);
    }
}
