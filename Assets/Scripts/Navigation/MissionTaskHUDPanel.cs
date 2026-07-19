using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum MissionTaskHUDCategory
{
    SecondaryTask,
    ImportantPerson
}

[DisallowMultipleComponent]
public sealed class MissionTaskHUDPanel : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private string missionName = "Bamboo Blight";
    [SerializeField] private string missionDescription = "Strange rustling ahead";
    [SerializeField] private string nextStepText = "Speak to the Keeper";

    [Header("Category")]
    [SerializeField] private MissionTaskHUDCategory category = MissionTaskHUDCategory.SecondaryTask;
    [SerializeField] private Color secondaryTaskColor = HUDNavigationCuePalette.Secondary;
    [SerializeField] private Color importantPersonColor = HUDNavigationCuePalette.Important;

    [Header("Bindings")]
    [SerializeField] private Image titleBackgroundImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text nextStepTextMain;
    [SerializeField] private TMP_Text nextStepTextGlow;

    private void Reset()
    {
        FindBindings();
        Apply();
    }

    private void Awake()
    {
        FindBindings();
        Apply();
    }

    private void OnValidate()
    {
        FindBindings();
        Apply();
    }

    public void SetMission(string newMissionName, string newDescription, string newNextStep)
    {
        SetMission(newMissionName, newDescription, newNextStep, category);
    }

    public void SetMission(
        string newMissionName,
        string newDescription,
        string newNextStep,
        MissionTaskHUDCategory newCategory)
    {
        missionName = newMissionName;
        missionDescription = newDescription;
        nextStepText = newNextStep;
        category = newCategory;
        Apply();
    }

    public void Apply()
    {
        if (titleText != null)
        {
            titleText.text = missionName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = missionDescription;
        }

        if (nextStepTextMain != null)
        {
            nextStepTextMain.text = nextStepText;
        }

        if (nextStepTextGlow != null)
        {
            nextStepTextGlow.text = nextStepText;
        }

        if (titleBackgroundImage != null)
        {
            titleBackgroundImage.color = GetTitleColor(category);
        }
    }

    public Color GetTitleColor(MissionTaskHUDCategory targetCategory)
    {
        return targetCategory == MissionTaskHUDCategory.ImportantPerson
            ? importantPersonColor
            : secondaryTaskColor;
    }

    private void FindBindings()
    {
        if (titleBackgroundImage == null)
        {
            titleBackgroundImage = FindChildImage("Title Background");
        }

        if (titleText == null)
        {
            titleText = FindChildText("Mission Name");
        }

        if (descriptionText == null)
        {
            descriptionText = FindChildText("Mission Description");
        }

        if (nextStepTextMain == null)
        {
            nextStepTextMain = FindChildText("Next Step");
        }

        if (nextStepTextGlow == null)
        {
            nextStepTextGlow = FindChildText("Next Step Glow");
        }
    }

    private TMP_Text FindChildText(string childName)
    {
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == childName)
            {
                return text;
            }
        }

        return null;
    }

    private Image FindChildImage(string childName)
    {
        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            if (image.name == childName)
            {
                return image;
            }
        }

        return null;
    }
}
