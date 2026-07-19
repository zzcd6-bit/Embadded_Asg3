using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class MissionTaskHUDPanel : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private string missionName = "战争回响";
    [SerializeField] private string missionDescription = "前往枢纽区，与希金斯交谈";
    [SerializeField] private string nextStepText = "前往枢纽区完成";

    [Header("Layout")]
    [SerializeField] private Vector2 anchoredPosition = new(92f, -285f);
    [SerializeField] private Vector2 titlePanelSize = new(360f, 44f);
    [SerializeField] private Vector2 bodyPanelSize = new(430f, 92f);
    [SerializeField] private float titleToBodyGap = 8f;
    [SerializeField] private Vector2 titleTextPadding = new(74f, 0f);
    [SerializeField] private Vector2 bodyTextPadding = new(72f, -18f);
    [SerializeField] private Vector2 nextStepOffset = new(72f, -52f);

    [Header("Background")]
    [SerializeField] private Sprite titleBackgroundSprite;
    [SerializeField] private Sprite bodyBackgroundSprite;
    [SerializeField] private Color titleBackgroundColor = new(0.06f, 0.06f, 0.055f, 0.72f);
    [SerializeField] private Color bodyBackgroundColor = new(0.04f, 0.04f, 0.035f, 0.48f);
    [SerializeField] private bool useImageSlicing = true;

    [Header("Style")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private float titleFontSize = 25f;
    [SerializeField] private float descriptionFontSize = 21f;
    [SerializeField] private float nextStepFontSize = 21f;
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color descriptionColor = Color.white;
    [SerializeField] private Color nextStepColor = new(1f, 0.86f, 0.08f, 1f);
    [SerializeField] private Color descriptionShadowColor = new(0f, 0f, 0f, 0.82f);
    [SerializeField] private Color nextStepGlowColor = new(1f, 0.76f, 0.02f, 0.62f);

    [Header("Decoration")]
    [SerializeField] private bool showBullet = true;
    [SerializeField] private Color bulletColor = new(0.58f, 1f, 0.05f, 1f);
    [SerializeField] private Vector2 bulletPosition = new(44f, 0f);
    [SerializeField] private Vector2 bulletSize = new(22f, 22f);

    private RectTransform rectTransform;
    private Image titleBackground;
    private Image bodyBackground;
    private TMP_Text titleText;
    private TMP_Text descriptionText;
    private TMP_Text nextStepGlowText;
    private TMP_Text nextStepMainText;
    private Image bulletImage;

    private void Reset()
    {
        BuildOrRefresh();
    }

    private void OnEnable()
    {
        BuildOrRefresh();
    }

    private void OnValidate()
    {
        BuildOrRefresh();
    }

    public void SetMission(string newMissionName, string newDescription, string newNextStep)
    {
        missionName = newMissionName;
        missionDescription = newDescription;
        nextStepText = newNextStep;
        BuildOrRefresh();
    }

    public void BuildOrRefresh()
    {
        rectTransform = GetComponent<RectTransform>();
        ConfigureRoot();

        titleBackground = EnsurePanelImage("Title Background", titleBackground, titlePanelSize, Vector2.zero, titleBackgroundColor, titleBackgroundSprite);
        bodyBackground = EnsurePanelImage("Body Background", bodyBackground, bodyPanelSize, new Vector2(0f, -(titlePanelSize.y + titleToBodyGap)), bodyBackgroundColor, bodyBackgroundSprite);

        titleText = EnsureText("Mission Name", titleText, titleBackground.rectTransform);
        ConfigureText(titleText, missionName, titleColor, titleFontSize, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        ConfigureRect(titleText.rectTransform, titlePanelSize, titleTextPadding, new Vector2(0f, 0.5f));

        descriptionText = EnsureText("Mission Description", descriptionText, bodyBackground.rectTransform);
        ConfigureText(descriptionText, missionDescription, descriptionColor, descriptionFontSize, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        ConfigureRect(descriptionText.rectTransform, bodyPanelSize, bodyTextPadding, new Vector2(0f, 1f));
        EnsureShadow(descriptionText.gameObject, descriptionShadowColor, new Vector2(2f, -2f));

        nextStepGlowText = EnsureText("Next Step Glow", nextStepGlowText, bodyBackground.rectTransform);
        ConfigureText(nextStepGlowText, nextStepText, nextStepGlowColor, nextStepFontSize, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        ConfigureRect(nextStepGlowText.rectTransform, bodyPanelSize, nextStepOffset + new Vector2(0f, -1f), new Vector2(0f, 1f));
        nextStepGlowText.rectTransform.localScale = Vector3.one * 1.035f;

        nextStepMainText = EnsureText("Next Step", nextStepMainText, bodyBackground.rectTransform);
        ConfigureText(nextStepMainText, nextStepText, nextStepColor, nextStepFontSize, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        ConfigureRect(nextStepMainText.rectTransform, bodyPanelSize, nextStepOffset, new Vector2(0f, 1f));
        EnsureShadow(nextStepMainText.gameObject, new Color(0f, 0f, 0f, 0.68f), new Vector2(1.5f, -1.5f));

        ConfigureBullet();
    }

    private void ConfigureRoot()
    {
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(Mathf.Max(titlePanelSize.x, bodyPanelSize.x), titlePanelSize.y + titleToBodyGap + bodyPanelSize.y);
    }

    private Image EnsurePanelImage(
        string objectName,
        Image cachedImage,
        Vector2 size,
        Vector2 position,
        Color color,
        Sprite sprite)
    {
        if (cachedImage == null)
        {
            Transform existing = transform.Find(objectName);
            GameObject panelObject = existing != null ? existing.gameObject : new GameObject(objectName);
            panelObject.transform.SetParent(transform, false);
            cachedImage = panelObject.GetComponent<Image>();
            if (cachedImage == null)
            {
                cachedImage = panelObject.AddComponent<Image>();
            }
        }

        RectTransform panelRect = cachedImage.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = position;
        panelRect.sizeDelta = size;

        cachedImage.color = color;
        cachedImage.sprite = sprite;
        cachedImage.type = sprite != null && useImageSlicing ? Image.Type.Sliced : Image.Type.Simple;
        cachedImage.raycastTarget = false;
        return cachedImage;
    }

    private TMP_Text EnsureText(string objectName, TMP_Text cachedText, Transform parent)
    {
        if (cachedText == null)
        {
            Transform existing = parent.Find(objectName);
            GameObject textObject = existing != null ? existing.gameObject : new GameObject(objectName);
            textObject.transform.SetParent(parent, false);
            cachedText = textObject.GetComponent<TextMeshProUGUI>();
            if (cachedText == null)
            {
                cachedText = textObject.AddComponent<TextMeshProUGUI>();
            }
        }

        cachedText.raycastTarget = false;
        return cachedText;
    }

    private void ConfigureText(
        TMP_Text text,
        string value,
        Color color,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment)
    {
        text.text = value;
        text.color = color;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        if (font != null)
        {
            text.font = font;
        }
    }

    private static void ConfigureRect(RectTransform target, Vector2 parentSize, Vector2 offset, Vector2 anchor)
    {
        target.anchorMin = anchor;
        target.anchorMax = anchor;
        target.pivot = new Vector2(0f, 0.5f);
        target.anchoredPosition = offset;
        target.sizeDelta = parentSize;
    }

    private static void EnsureShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = target.AddComponent<Shadow>();
        }

        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private void ConfigureBullet()
    {
        if (!showBullet)
        {
            if (bulletImage != null)
            {
                bulletImage.enabled = false;
            }
            return;
        }

        if (bulletImage == null)
        {
            Transform existing = titleBackground.transform.Find("Mission Bullet");
            GameObject bulletObject = existing != null ? existing.gameObject : new GameObject("Mission Bullet");
            bulletObject.transform.SetParent(titleBackground.transform, false);
            bulletImage = bulletObject.GetComponent<Image>();
            if (bulletImage == null)
            {
                bulletImage = bulletObject.AddComponent<Image>();
            }
        }

        bulletImage.enabled = true;
        bulletImage.color = bulletColor;
        bulletImage.raycastTarget = false;

        RectTransform bulletRect = bulletImage.rectTransform;
        bulletRect.anchorMin = new Vector2(0f, 0.5f);
        bulletRect.anchorMax = new Vector2(0f, 0.5f);
        bulletRect.pivot = new Vector2(0.5f, 0.5f);
        bulletRect.anchoredPosition = bulletPosition;
        bulletRect.sizeDelta = bulletSize;
    }
}
