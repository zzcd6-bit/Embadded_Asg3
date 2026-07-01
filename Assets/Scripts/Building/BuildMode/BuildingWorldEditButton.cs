using UnityEngine;
using UnityEngine.UI;

public class BuildingWorldEditButton : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector3 localOffset = new(0f, 2.2f, 0f);
    [SerializeField] private Vector2 buttonSize = new(120f, 42f);
    [SerializeField] private float canvasScale = 0.01f;

    [Header("References")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Button editButton;
    [SerializeField] private Text label;
    [SerializeField] private Transform player;
    [SerializeField] private PlacementController placementController;
    [SerializeField] private bool treatExistingBuildingAsCompleted = true;

    private BuildingInstance buildingInstance;

    //Builds the world-space button and binds it to the building instance.
    //创建世界空间按钮，并将其绑定到建筑实例。
    private void Awake()
    {
        ResolveReferences();
        if (buildingInstance != null
            && treatExistingBuildingAsCompleted
            && buildingInstance.Item == null
            && !buildingInstance.IsPlacementCompleted)
        {
            buildingInstance.MarkExistingAsCompleted();
        }

        EnsureCanvas();
        EnsureButton();

        if (editButton != null)
        {
            editButton.onClick.AddListener(StartEditing);
        }

        SetVisible(false);
    }

    //Updates visibility and rotates the button toward the player after movement.
    //在移动更新之后刷新显示状态，并让按钮朝向玩家。
    private void LateUpdate()
    {
        UpdateVisibility();
        UpdateTransform();
    }

    //Shows the edit button only while build mode can edit this completed building.
    //仅在建造模式可编辑该已完成建筑时显示编辑按钮。
    private void UpdateVisibility()
    {
        ResolveReferences();

        bool canShow = BuildModeController.Instance != null
            && BuildModeController.Instance.IsBuildMode
            && placementController != null
            && !placementController.IsPlacing
            && buildingInstance != null
            && buildingInstance.IsPlacementCompleted;

        SetVisible(canShow);
    }

    //Refreshes cached references after runtime components are added.
    //在运行时组件被添加后刷新缓存引用。
    public void RefreshReferences()
    {
        ResolveReferences();
    }

    //Finds the owning building instance and placement controller when missing.
    //在引用缺失时查找所属建筑实例和摆放控制器。
    private void ResolveReferences()
    {
        if (buildingInstance == null)
        {
            buildingInstance = GetComponentInParent<BuildingInstance>();
        }

        if (placementController == null)
        {
            placementController = FindAnyObjectByType<PlacementController>();
        }
    }

    //Positions the canvas at the local offset and rotates it around Y to face the player.
    //将画布放在本地偏移位置，并绕 Y 轴旋转面向玩家。
    private void UpdateTransform()
    {
        if (worldCanvas == null)
        {
            return;
        }

        Transform playerTransform = GetPlayerTransform();
        worldCanvas.transform.position = transform.TransformPoint(localOffset);

        if (playerTransform == null)
        {
            return;
        }

        Vector3 direction = worldCanvas.transform.position - playerTransform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            worldCanvas.transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    //Requests placement editing for this building instance.
    //请求对当前建筑实例进入摆放编辑状态。
    private void StartEditing()
    {
        if (buildingInstance == null || placementController == null)
        {
            return;
        }

        placementController.StartEditing(buildingInstance);
        SetVisible(false);
    }

    //Creates the world-space canvas if the prefab does not provide one.
    //如果 prefab 未提供画布，则创建世界空间画布。
    private void EnsureCanvas()
    {
        if (worldCanvas != null)
        {
            return;
        }

        GameObject canvasObject = new("WorldEditButtonCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = localOffset;
        canvasObject.transform.localScale = Vector3.one * canvasScale;

        worldCanvas = canvasObject.GetComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
    }

    //Creates the edit button and label if the prefab does not provide them.
    //如果 prefab 未提供按钮和文字，则创建编辑按钮与标签。
    private void EnsureButton()
    {
        if (editButton != null)
        {
            return;
        }

        GameObject buttonObject = new("EditButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(worldCanvas.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = buttonSize;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.16f, 0.30f, 0.42f, 0.95f);

        editButton = buttonObject.GetComponent<Button>();

        GameObject labelObject = new("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        label = labelObject.GetComponent<Text>();
        label.text = "Edit";
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 18;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
    }

    //Applies the visible state to the world-space canvas.
    //将显示状态应用到世界空间画布。
    private void SetVisible(bool visible)
    {
        if (worldCanvas != null && worldCanvas.gameObject.activeSelf != visible)
        {
            worldCanvas.gameObject.SetActive(visible);
        }
    }

    //Finds the player transform used for billboard rotation.
    //查找用于公告板旋转的玩家 Transform。
    private Transform GetPlayerTransform()
    {
        if (player != null)
        {
            return player;
        }

        if (PlayerController.instance != null)
        {
            player = PlayerController.instance.transform;
            return player;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        player = taggedPlayer != null ? taggedPlayer.transform : null;
        return player;
    }
}
