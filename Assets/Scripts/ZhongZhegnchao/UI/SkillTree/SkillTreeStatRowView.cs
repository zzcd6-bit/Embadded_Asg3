using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SkillTreeStatRowView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text numberText;

    public void Show(
        SkillTreeStatConfig stat,
        int currentLevel,
        bool showNextLevel
    )
    {
        if (stat == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = stat.Icon;
            iconImage.enabled = stat.Icon != null;
        }

        if (nameText != null)
            nameText.text = stat.DisplayName;

        if (numberText != null)
        {
            numberText.text =
                showNextLevel
                    ? stat.FormatUpgradeValue(currentLevel)
                    : stat.FormatValue(currentLevel);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}