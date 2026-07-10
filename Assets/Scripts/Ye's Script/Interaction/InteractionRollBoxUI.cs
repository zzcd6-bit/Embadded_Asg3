using System;
using System.Collections.Generic;
using UnityEngine;
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

public class InteractionRollBoxUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Scroll View")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;

    [Header("Option Template")]
    [SerializeField] private InteractionOptionUI optionPrefab;

    private readonly List<InteractionOptionUI> optionViews = new();

    public event Action<int> OptionClicked;
    public event Action<int> OptionHovered;

    public void SetOptions(IReadOnlyList<InteractionDisplayData> options, int selectedIndex)
    {
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
            optionViews[i].Bind(i, data.OptionName, HandleOptionClicked, HandleOptionHovered);
            optionViews[i].SetSelected(i == selectedIndex);
        }

        SetVisible(options.Count > 0);

        if (content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }
    }

    public void SetSelected(int selectedIndex, bool scrollIntoView)
    {
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
    }

    public void SetVisible(bool visible)
    {
        GameObject targetRoot = root != null ? root : gameObject;
        if (targetRoot.activeSelf != visible)
        {
            targetRoot.SetActive(visible);
        }
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
