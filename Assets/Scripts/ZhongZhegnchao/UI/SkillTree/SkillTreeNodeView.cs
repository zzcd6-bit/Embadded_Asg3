using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SkillTreeNodeView : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private BrushSkillType skillType;

    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GameObject selectedMark;
    [SerializeField] private GameObject lockedMark;
    [SerializeField] private GameObject upgradeReadyMark;

    [Header("Display")]
    [SerializeField]
    private Color unlockedColor =
        Color.white;

    [SerializeField]
    private Color lockedColor =
        new Color(0.35f, 0.35f, 0.35f, 0.8f);

    private SkillTreeSkillConfig config;
    private PlayerSkillTreeController controller;
    private Action<BrushSkillType> clickCallback;

    public BrushSkillType SkillType => skillType;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    public void Bind(
        SkillTreeSkillConfig newConfig,
        PlayerSkillTreeController newController,
        Action<BrushSkillType> onClick
    )
    {
        config = newConfig;
        controller = newController;
        clickCallback = onClick;

        Refresh(false);
    }

    public void Unbind()
    {
        config = null;
        controller = null;
        clickCallback = null;
    }

    public void Refresh(bool selected)
    {
        bool hasConfig = config != null;

        bool unlocked =
            hasConfig &&
            controller != null &&
            controller.IsSkillUnlocked(skillType);

        int level =
            unlocked
                ? controller.GetSkillLevel(skillType)
                : 0;

        if (button != null)
        {
            /*
             * Locked skills remain clickable so
             * the detail panel can explain them.
             */
            button.interactable = hasConfig;
        }

        if (iconImage != null)
        {
            iconImage.sprite =
                hasConfig ? config.Icon : null;

            iconImage.enabled =
                iconImage.sprite != null;

            iconImage.color =
                unlocked
                    ? unlockedColor
                    : lockedColor;
        }

        if (titleText != null)
        {
            titleText.text =
                hasConfig
                    ? config.DisplayName
                    : skillType.ToString();
        }

        if (levelText != null)
        {
            levelText.text =
                unlocked
                    ? $"Lv.{level}"
                    : "Locked";
        }

        if (selectedMark != null)
            selectedMark.SetActive(selected);

        if (lockedMark != null)
            lockedMark.SetActive(!unlocked);

        if (upgradeReadyMark != null)
        {
            upgradeReadyMark.SetActive(
                unlocked &&
                controller.CanUpgradeSkill(skillType)
            );
        }
    }

    private void HandleClick()
    {
        clickCallback?.Invoke(skillType);
    }
}