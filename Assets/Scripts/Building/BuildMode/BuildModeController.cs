using UnityEngine;

public class BuildModeController : MonoBehaviour
{
    public static BuildModeController Instance { get; private set; }

    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private BuildInventoryPanelAnimator inventoryPanel;
    [SerializeField] private PlacementController placementController;

    public bool IsBuildMode { get; private set; }

    //Initializes references and exposes the active build mode controller.
    //初始化引用，并暴露当前建造模式控制器。
    private void Awake()
    {
        Instance = this;

        if (inputHandler == null)
        {
            inputHandler = InputHandler.GetOrCreate();
        }

        if (placementController == null)
        {
            placementController = FindAnyObjectByType<PlacementController>();
        }
    }

    //Starts gameplay in normal mode with the build inventory hidden.
    //游戏开始时进入普通模式，并隐藏建造物品栏。
    private void Start()
    {
        SetBuildMode(false);
    }

    //Toggles build mode when the configured input is pressed.
    //按下配置的输入键时切换建造模式。
    private void Update()
    {
        if (inputHandler != null && inputHandler.BuildModeTogglePressed)
        {
            SetBuildMode(!IsBuildMode);
        }
    }

    //Applies build mode state and cancels active placement when leaving it.
    //应用建造模式状态，并在退出时取消当前摆放。
    public void SetBuildMode(bool enabled)
    {
        IsBuildMode = enabled;

        if (inventoryPanel != null)
        {
            inventoryPanel.SetVisible(enabled);
        }

        if (!enabled && placementController != null)
        {
            placementController.CancelPlacement();
        }
    }
}
