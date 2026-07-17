using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CharacterSkillTreeUIController : MonoBehaviour
{
    [Header("Page Roots")]
    [SerializeField] private GameObject characterArea;
    [SerializeField] private GameObject itemsArea;
    [SerializeField] private GameObject skillArea;

    [Header("Top Buttons")]
    [SerializeField] private Button characterButton;
    [SerializeField] private Button itemButton;
    [SerializeField] private Button skillButton;

    [Header("Skill Summary")]
    [SerializeField] private TMP_Text skillPointText;
    [SerializeField] private Button resetSkillButton;
    [SerializeField] private TMP_Text resetStatusText;

    [Header("Skill Nodes")]
    [SerializeField] private SkillTreeNodeView[] skillNodes;

    [Header("Detail Main")]
    [SerializeField] private Image detailIcon;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailLevelText;
    [SerializeField] private TMP_Text detailIntroText;

    [Header("Detail Stats")]
    [SerializeField] private SkillTreeStatAreaView currentStatArea;
    [SerializeField] private SkillTreeStatAreaView upgradeStatArea;

    [Header("Upgrade")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text upgradeButtonText;

    [Header("Display Text")]
    [SerializeField]
    private string upgradeText =
        "Upgrade  1 SP";

    [SerializeField]
    private string lockedText =
        "Locked";

    [SerializeField]
    private string noPointText =
        "No Skill Point";

    [SerializeField]
    private string resetAvailableText =
        "Reset Available";

    [SerializeField]
    private string resetUnavailableText =
        "Save at checkpoint to reset";

    private PlayerCharacterStatsController statsController;
    private PlayerSkillTreeController skillTreeController;

    private BrushSkillType selectedSkill =
        BrushSkillType.Slash;

    private bool buttonsRegistered;

    private void Awake()
    {
        RegisterButtons();
    }

    private void OnDestroy()
    {
        UnregisterButtons();
        Unbind();
    }

    public void Bind(
        PlayerCharacterStatsController controller
    )
    {
        Unbind();

        statsController = controller;
        ResolvePlayerReferences();

        if (skillTreeController == null)
        {
            Debug.LogError(
                "[CharacterSkillTreeUIController] " +
                "PlayerSkillTreeController was not found.",
                this
            );
            return;
        }

        skillTreeController.SyncUnlockedSkills();

        skillTreeController.SkillTreeChanged +=
            RefreshAll;

        BindNodes();
        SelectInitialSkill();
        RefreshAll();
    }

    public void Unbind()
    {
        if (skillTreeController != null)
        {
            skillTreeController.SkillTreeChanged -=
                RefreshAll;
        }

        if (skillNodes != null)
        {
            for (int i = 0;
                 i < skillNodes.Length;
                 i++)
            {
                if (skillNodes[i] != null)
                    skillNodes[i].Unbind();
            }
        }

        statsController = null;
        skillTreeController = null;
    }

    private void ResolvePlayerReferences()
    {
        if (statsController == null)
            return;

        skillTreeController =
            statsController.GetComponent<
                PlayerSkillTreeController>();

        if (skillTreeController == null)
        {
            skillTreeController =
                statsController.GetComponentInParent<
                    PlayerSkillTreeController>();
        }

        if (skillTreeController == null)
        {
            skillTreeController =
                statsController.GetComponentInChildren<
                    PlayerSkillTreeController>(true);
        }
    }

    private void RegisterButtons()
    {
        if (buttonsRegistered)
            return;

        if (characterButton != null)
            characterButton.onClick.AddListener(
                ShowCharacterPage
            );

        if (itemButton != null)
            itemButton.onClick.AddListener(
                ShowItemsPage
            );

        if (skillButton != null)
            skillButton.onClick.AddListener(
                ShowSkillPage
            );

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(
                UpgradeSelectedSkill
            );

        if (resetSkillButton != null)
            resetSkillButton.onClick.AddListener(
                ResetSkills
            );

        buttonsRegistered = true;
    }

    private void UnregisterButtons()
    {
        if (!buttonsRegistered)
            return;

        if (characterButton != null)
            characterButton.onClick.RemoveListener(
                ShowCharacterPage
            );

        if (itemButton != null)
            itemButton.onClick.RemoveListener(
                ShowItemsPage
            );

        if (skillButton != null)
            skillButton.onClick.RemoveListener(
                ShowSkillPage
            );

        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(
                UpgradeSelectedSkill
            );

        if (resetSkillButton != null)
            resetSkillButton.onClick.RemoveListener(
                ResetSkills
            );

        buttonsRegistered = false;
    }

    private void BindNodes()
    {
        if (skillNodes == null ||
            skillTreeController == null ||
            skillTreeController.Config == null)
        {
            return;
        }

        for (int i = 0;
             i < skillNodes.Length;
             i++)
        {
            SkillTreeNodeView node =
                skillNodes[i];

            if (node == null)
                continue;

            SkillTreeSkillConfig config =
                skillTreeController.Config
                    .GetSkillConfig(
                        node.SkillType
                    );

            node.Bind(
                config,
                skillTreeController,
                SelectSkill
            );
        }
    }

    private void SelectInitialSkill()
    {
        if (skillNodes == null)
            return;

        for (int i = 0;
             i < skillNodes.Length;
             i++)
        {
            SkillTreeNodeView node =
                skillNodes[i];

            if (node == null)
                continue;

            if (skillTreeController.IsSkillUnlocked(
                    node.SkillType))
            {
                selectedSkill =
                    node.SkillType;
                return;
            }
        }

        if (skillNodes.Length > 0 &&
            skillNodes[0] != null)
        {
            selectedSkill =
                skillNodes[0].SkillType;
        }
    }

    private void SelectSkill(
        BrushSkillType skillType
    )
    {
        selectedSkill = skillType;
        RefreshAll();
    }

    public void ShowCharacterPage()
    {
        SetPageActive(true, false, false);
    }

    public void ShowItemsPage()
    {
        SetPageActive(false, true, false);
    }

    public void ShowSkillPage()
    {
        SetPageActive(false, false, true);

        if (skillTreeController != null)
            skillTreeController.SyncUnlockedSkills();

        RefreshAll();
    }

    private void SetPageActive(
        bool showCharacter,
        bool showItems,
        bool showSkill
    )
    {
        if (characterArea != null)
            characterArea.SetActive(showCharacter);

        if (itemsArea != null)
            itemsArea.SetActive(showItems);

        if (skillArea != null)
            skillArea.SetActive(showSkill);
    }

    private void UpgradeSelectedSkill()
    {
        if (skillTreeController == null)
            return;

        skillTreeController.UpgradeSkill(
            selectedSkill
        );
    }

    private void ResetSkills()
    {
        if (skillTreeController == null)
            return;

        skillTreeController
            .ResetAllocatedSkillPoints();
    }

    private void RefreshAll()
    {
        if (skillTreeController == null)
            return;

        RefreshSummary();
        RefreshNodes();
        RefreshDetail();
    }

    private void RefreshSummary()
    {
        if (skillPointText != null)
        {
            skillPointText.text =
                skillTreeController
                    .AvailableSkillPoints
                    .ToString();
        }

        bool canReset =
            skillTreeController.CanResetSkills &&
            skillTreeController.UsedSkillPoints > 0;

        if (resetSkillButton != null)
            resetSkillButton.interactable = canReset;

        if (resetStatusText != null)
        {
            resetStatusText.text =
                skillTreeController.CanResetSkills
                    ? resetAvailableText
                    : resetUnavailableText;
        }
    }

    private void RefreshNodes()
    {
        if (skillNodes == null)
            return;

        for (int i = 0;
             i < skillNodes.Length;
             i++)
        {
            SkillTreeNodeView node =
                skillNodes[i];

            if (node == null)
                continue;

            node.Refresh(
                node.SkillType == selectedSkill
            );
        }
    }

    private void RefreshDetail()
    {
        SkillTreeConfig treeConfig =
            skillTreeController.Config;

        if (treeConfig == null)
        {
            ClearDetail();
            return;
        }

        SkillTreeSkillConfig skillConfig =
            treeConfig.GetSkillConfig(
                selectedSkill
            );

        if (skillConfig == null)
        {
            ClearDetail();
            return;
        }

        bool unlocked =
            skillTreeController.IsSkillUnlocked(
                selectedSkill
            );

        int currentLevel =
            unlocked
                ? skillTreeController.GetSkillLevel(
                    selectedSkill
                )
                : 0;

        if (detailIcon != null)
        {
            detailIcon.sprite = skillConfig.Icon;
            detailIcon.enabled =
                skillConfig.Icon != null;
        }

        if (detailNameText != null)
            detailNameText.text =
                skillConfig.DisplayName;

        if (detailLevelText != null)
        {
            detailLevelText.text =
                unlocked
                    ? $"Lv.{currentLevel}"
                    : lockedText;
        }

        if (detailIntroText != null)
            detailIntroText.text =
                skillConfig.Introduction;

        currentStatArea?.Show(
            skillConfig,
            currentLevel,
            false
        );

        upgradeStatArea?.Show(
            skillConfig,
            currentLevel,
            true
        );

        bool canUpgrade =
            skillTreeController.CanUpgradeSkill(
                selectedSkill
            );

        if (upgradeButton != null)
            upgradeButton.interactable =
                canUpgrade;

        if (upgradeButtonText != null)
        {
            if (!unlocked)
                upgradeButtonText.text = lockedText;
            else if (!canUpgrade)
                upgradeButtonText.text = noPointText;
            else
                upgradeButtonText.text = upgradeText;
        }
    }

    private void ClearDetail()
    {
        if (detailIcon != null)
        {
            detailIcon.sprite = null;
            detailIcon.enabled = false;
        }

        if (detailNameText != null)
            detailNameText.text = string.Empty;

        if (detailLevelText != null)
            detailLevelText.text = string.Empty;

        if (detailIntroText != null)
            detailIntroText.text = string.Empty;

        currentStatArea?.HideAll();
        upgradeStatArea?.HideAll();

        if (upgradeButton != null)
            upgradeButton.interactable = false;
    }
}