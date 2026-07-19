using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InteractionOptionUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private TMP_Text optionText;
    [SerializeField] private GameObject selectedBackground;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button button;
    [SerializeField] private Image buttonImage;

    [Header("Button Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;

    [Header("Display")]
    [SerializeField, Range(0f, 1f)] private float normalAlpha = 0.65f;
    [SerializeField, Range(0f, 1f)] private float selectedAlpha = 1f;

    private int index;
    private Action<int> clickCallback;
    private Action<int> hoverCallback;
    private Action<int> exitCallback;

    public RectTransform RectTransform => transform as RectTransform;

    public void Bind(int optionIndex, string displayName, Action<int> onClick, Action<int> onHover, Action<int> onExit)
    {
        index = optionIndex;
        clickCallback = onClick;
        hoverCallback = onHover;
        exitCallback = onExit;

        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
            button.onClick.AddListener(HandleButtonClicked);
        }

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

        if (buttonImage != null)
        {
            Sprite sprite = selected ? selectedSprite : normalSprite;
            if (sprite != null)
            {
                buttonImage.sprite = sprite;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hoverCallback?.Invoke(index);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        exitCallback?.Invoke(index);
    }

    private void HandleButtonClicked()
    {
        clickCallback?.Invoke(index);
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }
    }
}
