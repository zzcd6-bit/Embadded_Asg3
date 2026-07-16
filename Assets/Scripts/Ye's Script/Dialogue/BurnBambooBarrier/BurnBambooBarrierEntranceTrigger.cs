using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BurnBambooBarrierEntranceTrigger : MonoBehaviour
{
    [SerializeField] private BurnBambooBarrierQuestService questService;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool logTrigger = true;

    private bool hasTriggered;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        if (questService == null)
        {
            questService = BurnBambooBarrierQuestService.GetOrCreate();
        }

        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (!other.CompareTag(playerTag))
        {
            return;
        }

        BurnBambooBarrierQuestService service =
            questService != null ? questService : BurnBambooBarrierQuestService.GetOrCreate();

        if (service.State != BurnBambooBarrierQuestState.Locked)
        {
            return;
        }

        hasTriggered = true;
        service.AcceptQuest(BurnBambooBarrierQuestService.QuestId);
        service.TrackQuest(BurnBambooBarrierQuestService.QuestId);
        service.ActivateObjective(
            BurnBambooBarrierQuestService.QuestId,
            BurnBambooBarrierQuestService.TalkToForestKeeperObjectiveId);

        if (logTrigger)
        {
            Debug.Log("已接取任务：燃尽竹障", this);
        }
    }
}
