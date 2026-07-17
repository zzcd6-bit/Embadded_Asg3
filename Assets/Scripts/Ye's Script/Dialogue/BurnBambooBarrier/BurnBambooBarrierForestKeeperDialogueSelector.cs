using UnityEngine;

[DisallowMultipleComponent]
public class BurnBambooBarrierForestKeeperDialogueSelector : MonoBehaviour
{
    [SerializeField] private DialogueSystemReactable dialogueReactable;
    [SerializeField] private BurnBambooBarrierQuestService questService;

    [Header("Conversations")]
    [SerializeField] private string introductionConversation = "P_ForestKeeper_FireIntroduction";
    [SerializeField] private string fireReminderConversation = "P_ForestKeeper_FireReminder";
    [SerializeField] private string completedConversation = "P_ForestKeeper_AfterBarrierOpened";

    private void Awake()
    {
        if (dialogueReactable == null)
        {
            dialogueReactable = GetComponent<DialogueSystemReactable>();
        }

        if (questService == null)
        {
            questService = BurnBambooBarrierQuestService.GetOrCreate();
        }
    }

    private void OnEnable()
    {
        if (questService == null)
        {
            questService = BurnBambooBarrierQuestService.GetOrCreate();
        }

        if (questService != null)
        {
            questService.StateChanged += RefreshConversation;
        }

        RefreshConversation();
    }

    private void OnDisable()
    {
        if (questService != null)
        {
            questService.StateChanged -= RefreshConversation;
        }
    }

    private void RefreshConversation()
    {
        if (dialogueReactable == null)
        {
            return;
        }

        string conversation = introductionConversation;

        if (questService != null)
        {
            if (questService.IsCompleted)
            {
                conversation = completedConversation;
            }
            else if (questService.HasSkill(BurnBambooBarrierQuestService.FireBasicSkillId) ||
                     questService.IsObjectiveCompleted(BurnBambooBarrierQuestService.ObtainBasicFireSkillObjectiveId))
            {
                conversation = fireReminderConversation;
            }
        }

        dialogueReactable.ConfigureConversation(conversation);
    }
}
