using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public readonly struct InteractionDisplayData
{
    public string OptionName { get; }
    public InteractionCategory Category { get; }

    public InteractionDisplayData(string optionName, InteractionCategory category)
    {
        OptionName = optionName;
        Category = category;
    }
}

public class InteractionOptionSliderHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
{
    private InteractionRollBoxUI owner;

    public void Init(InteractionRollBoxUI rollBoxUI)
    {
        owner = rollBoxUI;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        owner?.SelectBySliderPointer(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        owner?.SelectBySliderPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        owner?.SelectBySliderPointer(eventData);
    }
}

public class InteractionRollBoxUI : MonoBehaviour
{
    private static int visibleRollBoxCount;

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Scroll View")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;

    [Header("Option Slider")]
    [SerializeField] private Scrollbar optionScrollbar;
    [SerializeField] private RectTransform sliderHandle;
    [SerializeField] private bool useHalfScreenHeightAsSliderBaseY = true;
    [SerializeField] private float sliderBaseYOffset;
    [SerializeField, Min(1f)] private float sliderLength = 260f;

    [Header("Option Template")]
    [SerializeField] private InteractionOptionUI optionPrefab;

    private readonly List<InteractionOptionUI> optionViews = new();
    private int activeOptionCount;
    private int currentSelectedIndex = -1;
    private bool isVisible;

    public event Action<int> OptionClicked;
    public event Action<int> OptionHovered;
    public event Action<int> OptionExited;
    public event Action<int> OptionSliderSelected;

    public static bool BlocksCameraZoom => visibleRollBoxCount > 0;

    public void SetOptions(IReadOnlyList<InteractionDisplayData> options, int selectedIndex)
    {
        EnsureSliderReferences();
        activeOptionCount = options.Count;
        currentSelectedIndex = selectedIndex;
        EnsureViewCount(options.Count);

        for (int i = 0; i < optionViews.Count; i++)
        {
            bool active = i < options.Count;
            optionViews[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            InteractionDisplayData data = options[i];
            optionViews[i].Bind(i, data.OptionName, HandleOptionClicked, HandleOptionHovered, HandleOptionExited);
            optionViews[i].SetSelected(i == selectedIndex);
        }

        SetVisible(options.Count > 0);
        RefreshSliderVisibility();
        RefreshSliderPosition();

        if (content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }
    }

    public void SetSelected(int selectedIndex, bool scrollIntoView)
    {
        currentSelectedIndex = selectedIndex;

        for (int i = 0; i < optionViews.Count; i++)
        {
            InteractionOptionUI view = optionViews[i];
            if (!view.gameObject.activeSelf)
            {
                continue;
            }

            view.SetSelected(i == selectedIndex);
        }

        if (scrollIntoView && selectedIndex >= 0 && selectedIndex < optionViews.Count)
        {
            EnsureVisible(optionViews[selectedIndex].RectTransform);
        }

        RefreshSliderPosition();
    }

    public void SetVisible(bool visible)
    {
        GameObject targetRoot = root != null ? root : gameObject;
        if (isVisible != visible)
        {
            visibleRollBoxCount += visible ? 1 : -1;
            visibleRollBoxCount = Mathf.Max(0, visibleRollBoxCount);
            isVisible = visible;
        }

        if (targetRoot.activeSelf != visible)
        {
            targetRoot.SetActive(visible);
        }
    }

    public void SelectBySliderPointer(PointerEventData eventData)
    {
        if (eventData == null || activeOptionCount <= 1 || sliderHandle == null)
        {
            return;
        }

        RectTransform coordinateRect = sliderHandle.parent as RectTransform;
        if (coordinateRect == null)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                coordinateRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        SelectBySliderLocalY(localPoint.y);
    }

    private void OnDisable()
    {
        if (!isVisible)
        {
            return;
        }

        visibleRollBoxCount = Mathf.Max(0, visibleRollBoxCount - 1);
        isVisible = false;
    }

    private void EnsureViewCount(int requiredCount)
    {
        if (optionPrefab == null || content == null)
        {
            return;
        }

        while (optionViews.Count < requiredCount)
        {
            InteractionOptionUI instance = Instantiate(optionPrefab, content);
            instance.gameObject.SetActive(false);
            optionViews.Add(instance);
        }
    }

    private void HandleOptionClicked(int index)
    {
        OptionClicked?.Invoke(index);
    }

    private void HandleOptionHovered(int index)
    {
        OptionHovered?.Invoke(index);
    }

    private void HandleOptionExited(int index)
    {
        OptionExited?.Invoke(index);
    }

    private void SelectBySliderLocalY(float localY)
    {
        float baseY = GetSliderBaseY();
        float percent = 1f - Mathf.InverseLerp(baseY, baseY + sliderLength, localY);
        int index = Mathf.RoundToInt(percent * activeOptionCount);
        index = Mathf.Clamp(index, 0, activeOptionCount - 1);

        if (index == currentSelectedIndex)
        {
            RefreshSliderPosition();
            return;
        }

        currentSelectedIndex = index;
        OptionSliderSelected?.Invoke(index);
        RefreshSliderPosition();
    }

    private void RefreshSliderVisibility()
    {
        if (optionScrollbar == null)
        {
            return;
        }

        optionScrollbar.gameObject.SetActive(activeOptionCount > 1);
    }

    private void RefreshSliderPosition()
    {
        if (sliderHandle == null || activeOptionCount <= 1 || currentSelectedIndex < 0)
        {
            return;
        }

        float baseY = GetSliderBaseY();
        float percent = 1f - (float)currentSelectedIndex / activeOptionCount;
        Vector2 position = sliderHandle.anchoredPosition;
        position.y = baseY + sliderLength * percent;
        sliderHandle.anchoredPosition = position;
    }

    private float GetSliderBaseY()
    {
        if (!useHalfScreenHeightAsSliderBaseY)
        {
            return sliderBaseYOffset;
        }

        return Screen.height * 0.5f + sliderBaseYOffset;
    }

    private void EnsureSliderReferences()
    {
        if (optionScrollbar == null)
        {
            optionScrollbar = GetComponentInChildren<Scrollbar>(true);
        }

        if (optionScrollbar != null)
        {
            optionScrollbar.enabled = false;

            if (sliderHandle == null)
            {
                sliderHandle = optionScrollbar.handleRect;
            }
        }

        if (sliderHandle == null)
        {
            return;
        }

        InteractionOptionSliderHandle dragHandle = sliderHandle.GetComponent<InteractionOptionSliderHandle>();
        if (dragHandle == null)
        {
            dragHandle = sliderHandle.gameObject.AddComponent<InteractionOptionSliderHandle>();
        }

        dragHandle.Init(this);
    }

    private void EnsureVisible(RectTransform item)
    {
        if (scrollRect == null || viewport == null || content == null || item == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        Bounds itemBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, item);
        Bounds viewportBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, viewport);
        Vector2 anchoredPosition = content.anchoredPosition;

        if (itemBounds.max.y > viewportBounds.max.y)
        {
            anchoredPosition.y -= itemBounds.max.y - viewportBounds.max.y;
        }
        else if (itemBounds.min.y < viewportBounds.min.y)
        {
            anchoredPosition.y += viewportBounds.min.y - itemBounds.min.y;
        }
        else
        {
            return;
        }

        content.anchoredPosition = anchoredPosition;
        scrollRect.StopMovement();
    }
}
