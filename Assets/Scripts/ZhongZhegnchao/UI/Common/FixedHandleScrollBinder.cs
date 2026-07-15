using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FixedHandleScrollBinder : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ScrollRect scrollRect;

    [SerializeField]
    private Scrollbar verticalScrollbar;

    [Header("Handle")]
    [SerializeField]
    [Range(0.01f, 1f)]
    private float fixedHandleSize = 0.12f;

    private bool isUpdating;

    private void Awake()
    {
        ApplyHandleSize();
    }

    private void OnEnable()
    {
        if (verticalScrollbar != null)
        {
            verticalScrollbar.onValueChanged.AddListener(
                OnScrollbarValueChanged
            );
        }

        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(
                OnScrollRectValueChanged
            );
        }

        ApplyHandleSize();
        SyncScrollbarFromScrollRect();
    }

    private void OnDisable()
    {
        if (verticalScrollbar != null)
        {
            verticalScrollbar.onValueChanged.RemoveListener(
                OnScrollbarValueChanged
            );
        }

        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(
                OnScrollRectValueChanged
            );
        }
    }

    private void ApplyHandleSize()
    {
        if (verticalScrollbar == null)
            return;

        verticalScrollbar.size =
            Mathf.Clamp01(fixedHandleSize);
    }

    private void OnScrollbarValueChanged(
        float value
    )
    {
        if (isUpdating)
            return;

        if (scrollRect == null ||
            verticalScrollbar == null)
        {
            return;
        }

        isUpdating = true;

        float normalizedPosition =
            GetScrollRectValue(value);

        scrollRect.verticalNormalizedPosition =
            normalizedPosition;

        ApplyHandleSize();

        isUpdating = false;
    }

    private void OnScrollRectValueChanged(
        Vector2 position
    )
    {
        if (isUpdating)
            return;

        SyncScrollbarFromScrollRect();
    }

    private void SyncScrollbarFromScrollRect()
    {
        if (scrollRect == null ||
            verticalScrollbar == null)
        {
            return;
        }

        isUpdating = true;

        float scrollbarValue =
            GetScrollbarValue(
                scrollRect.verticalNormalizedPosition
            );

        verticalScrollbar.SetValueWithoutNotify(
            scrollbarValue
        );

        ApplyHandleSize();

        isUpdating = false;
    }

    private float GetScrollRectValue(
        float scrollbarValue
    )
    {
        switch (verticalScrollbar.direction)
        {
            case Scrollbar.Direction.TopToBottom:
                return 1f - scrollbarValue;

            case Scrollbar.Direction.BottomToTop:
                return scrollbarValue;
        }

        return scrollbarValue;
    }

    private float GetScrollbarValue(
        float verticalNormalizedPosition
    )
    {
        switch (verticalScrollbar.direction)
        {
            case Scrollbar.Direction.TopToBottom:
                return 1f - verticalNormalizedPosition;

            case Scrollbar.Direction.BottomToTop:
                return verticalNormalizedPosition;
        }

        return verticalNormalizedPosition;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyHandleSize();
    }
#endif
}