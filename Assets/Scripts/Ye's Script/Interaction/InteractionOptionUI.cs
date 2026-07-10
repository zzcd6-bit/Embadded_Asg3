using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionOptionUI : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private TMP_Text optionText;
    [SerializeField] private GameObject selectedBackground;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Display")]
    [SerializeField, Range(0f, 1f)] private float normalAlpha = 0.65f;
    [SerializeField, Range(0f, 1f)] private float selectedAlpha = 1f;

    private int index;
    private Action<int> clickCallback;
    private Action<int> hoverCallback;

    public RectTransform RectTransform => transform as RectTransform;

    public void Bind(int optionIndex, string displayName, Action<int> onClick, Action<int> onHover)
    {
        index = optionIndex;
        clickCallback = onClick;
        hoverCallback = onHover;

        if (optionText != null)
        {
            optionText.text = displayName;
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedBackground != null)
        {
            selectedBackground.SetActive(selected);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = selected ? selectedAlpha : normalAlpha;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hoverCallback?.Invoke(index);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        clickCallback?.Invoke(index);
    }
}
