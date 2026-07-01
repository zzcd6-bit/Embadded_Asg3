using UnityEngine;

public class BuildInventoryPanelAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private Vector2 visibleAnchoredPosition;
    [SerializeField] private Vector2 hiddenAnchoredPosition = new(0f, -220f);
    [SerializeField] private float moveSpeed = 14f;

    private bool visible;

    //Caches the panel rect and starts it in the hidden position.
    //缓存面板 RectTransform，并让它从隐藏位置开始。
    private void Awake()
    {
        if (panel == null)
        {
            panel = GetComponent<RectTransform>();
        }

        if (panel != null)
        {
            visibleAnchoredPosition = panel.anchoredPosition;
            panel.anchoredPosition = hiddenAnchoredPosition;
        }
    }

    //Smoothly moves the panel toward its visible or hidden target.
    //将面板平滑移动到显示或隐藏目标位置。
    private void Update()
    {
        if (panel == null)
        {
            return;
        }

        Vector2 target = visible ? visibleAnchoredPosition : hiddenAnchoredPosition;
        panel.anchoredPosition = Vector2.Lerp(panel.anchoredPosition, target, Time.unscaledDeltaTime * moveSpeed);
    }

    //Sets whether the panel should slide into view.
    //设置面板是否应该滑入显示。
    public void SetVisible(bool isVisible)
    {
        visible = isVisible;
    }
}
