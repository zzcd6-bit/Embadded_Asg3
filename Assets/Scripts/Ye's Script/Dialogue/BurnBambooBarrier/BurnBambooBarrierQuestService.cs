using System;
using System.Collections.Generic;
using UnityEngine;

public enum BurnBambooBarrierQuestState
{
    Locked,
    Active,
    Completed
}

public class BurnBambooBarrierQuestService : MonoBehaviour, IGameSaveModule
{
    public const string QuestId = "Quest_BurnBambooBarrier";
    public const string TalkToForestKeeperObjectiveId = "TalkToForestKeeper";
    public const string ObtainBasicFireSkillObjectiveId = "ObtainBasicFireSkill";
    public const string HitBambooBugWithFireObjectiveId = "HitBambooBugWithFire";
    public const string FireBasicSkillId = "FireBasic";
    public const string BambooBarrierOpenedSaveToken = "Quest_BurnBambooBarrier.BambooBugBarrier.Opened";
    public const string RewardGrantedSaveToken = "Quest_BurnBambooBarrier.Reward.Granted";

    public static BurnBambooBarrierQuestService Instance { get; private set; }

    [Header("Quest State")]
    [SerializeField] private BurnBambooBarrierQuestState questState = BurnBambooBarrierQuestState.Locked;
    [SerializeField] private string trackedQuestId;
    [SerializeField] private string activeObjectiveId;

    [Header("Objectives")]
    [SerializeField] private bool talkToForestKeeperCompleted;
    [SerializeField] private bool obtainBasicFireSkillCompleted;
    [SerializeField] private bool hitBambooBugWithFireCompleted;

    [Header("Temporary Skill State")]
    [SerializeField] private bool fireBasicUnlocked;

    [Header("World State")]
    [SerializeField] private bool bambooBarrierOpened;
    [SerializeField] private bool rewardGranted;

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = true;

    public BurnBambooBarrierQuestState State => questState;
    public string TrackedQuestId => trackedQuestId;
    public string ActiveObjectiveId => activeObjectiveId;
    public bool IsActive => questState == BurnBambooBarrierQuestState.Active;
    public bool IsCompleted => questState == BurnBambooBarrierQuestState.Completed;
    public bool IsFireBasicUnlocked => fireBasicUnlocked;
    public bool IsBambooBarrierOpened => bambooBarrierOpened;
    public bool IsRewardGranted => rewardGranted;

    public event Action StateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[BurnBambooBarrierQuestService] Duplicate service found. Destroying the new instance.",
                this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static BurnBambooBarrierQuestService GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        BurnBambooBarrierQuestService existing =
            FindAnyObjectByType<BurnBambooBarrierQuestService>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        Debug.LogError(
            "[BurnBambooBarrierQuestService] Service is missing from the scene. " +
            "Add it to Quest Dialogue Runtime instead of creating it at runtime.");
        return null;
    }

    public string GetQuestState(string questId)
    {
        if (!IsSupportedQuest(questId))
        {
            return "Unknown";
        }

        return questState.ToString();
    }

    public bool IsQuestState(string questId, string state)
    {
        return IsSupportedQuest(questId) &&
               string.Equals(questState.ToString(), state, StringComparison.OrdinalIgnoreCase);
    }

    public void AcceptQuest(string questId)
    {
        if (!IsSupportedQuest(questId) ||
            questState != BurnBambooBarrierQuestState.Locked ||
            bambooBarrierOpened)
        {
            return;
        }

        questState = BurnBambooBarrierQuestState.Active;
        trackedQuestId = QuestId;
        activeObjectiveId = TalkToForestKeeperObjectiveId;
        Log("Accepted quest. Active objective: " + activeObjectiveId);
        NotifyChanged();
    }

    public void TrackQuest(string questId)
    {
        if (!IsSupportedQuest(questId))
        {
            return;
        }

        trackedQuestId = questId;
        Log("Tracking quest: " + trackedQuestId);
        NotifyChanged();
    }

    public void ActivateObjective(string questId, string objectiveId)
    {
        if (!IsSupportedQuest(questId) || questState != BurnBambooBarrierQuestState.Active)
        {
            return;
        }

        activeObjectiveId = objectiveId;
        Log("Active objective: " + activeObjectiveId);
        NotifyChanged();
    }

    public void CompleteObjective(string questId, string objectiveId)
    {
        if (!IsSupportedQuest(questId) || questState != BurnBambooBarrierQuestState.Active)
        {
            return;
        }

        if (SetObjectiveCompleted(objectiveId))
        {
            Log("Completed objective: " + objectiveId);
            NotifyChanged();
        }
    }

    public bool IsObjectiveCompleted(string objectiveId)
    {
        return objectiveId switch
        {
            TalkToForestKeeperObjectiveId => talkToForestKeeperCompleted,
            ObtainBasicFireSkillObjectiveId => obtainBasicFireSkillCompleted,
            HitBambooBugWithFireObjectiveId => hitBambooBugWithFireCompleted,
            _ => false
        };
    }

    public void UnlockSkill(string skillId)
    {
        if (!string.Equals(skillId, FireBasicSkillId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (fireBasicUnlocked)
        {
            return;
        }

        fireBasicUnlocked = true;
        Log("Marked temporary skill as available: " + FireBasicSkillId);
        NotifyChanged();
    }

    public void MarkFireSkillPickedUp()
    {
        if (questState == BurnBambooBarrierQuestState.Locked)
        {
            AcceptQuest(QuestId);
        }

        fireBasicUnlocked = true;
        CompleteObjective(QuestId, ObtainBasicFireSkillObjectiveId);

        if (questState == BurnBambooBarrierQuestState.Active &&
            !hitBambooBugWithFireCompleted)
        {
            ActivateObjective(QuestId, HitBambooBugWithFireObjectiveId);
        }

        Log("Fire skill pickup collected.");
        NotifyChanged();
    }

    public bool HasSkill(string skillId)
    {
        if (!string.Equals(skillId, FireBasicSkillId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return fireBasicUnlocked || PlayerHasFireSkill();
    }

    public void CompleteQuest(string questId)
    {
        if (!IsSupportedQuest(questId) || questState != BurnBambooBarrierQuestState.Active)
        {
            return;
        }

        questState = BurnBambooBarrierQuestState.Completed;
        activeObjectiveId = string.Empty;
        Log("Completed quest: " + QuestId);
        NotifyChanged();
    }

    public void MarkBambooBarrierOpened()
    {
        if (bambooBarrierOpened)
        {
            return;
        }

        bambooBarrierOpened = true;
        Log("Bamboo bug barrier opened.");
        NotifyChanged();
    }

    public void CompleteBambooBarrierObjectiveAndQuest()
    {
        MarkBambooBarrierOpened();

        if (questState == BurnBambooBarrierQuestState.Locked)
        {
            AcceptQuest(QuestId);
        }

        if (questState == BurnBambooBarrierQuestState.Active)
        {
            CompleteObjective(QuestId, HitBambooBugWithFireObjectiveId);
            CompleteQuest(QuestId);
        }

        GrantRewardOnce();
    }

    public void GrantRewardOnce()
    {
        if (rewardGranted)
        {
            return;
        }

        rewardGranted = true;
        Log("Reward granted: Reward_BurnBambooBarrier (skill point x1, gold x100 placeholder).");
        NotifyChanged();
    }

    public void CaptureGameSaveData(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        if (saveData.worldState == null)
        {
            saveData.worldState = new WorldStateSaveData();
        }

        AddToken(saveData.worldState.openedChestIds, QuestId + ".State." + questState);

        if (!string.IsNullOrWhiteSpace(trackedQuestId))
        {
            AddToken(saveData.worldState.openedChestIds, QuestId + ".Tracked");
        }

        if (!string.IsNullOrWhiteSpace(activeObjectiveId))
        {
            AddToken(saveData.worldState.openedChestIds, QuestId + ".ActiveObjective." + activeObjectiveId);
        }

        if (talkToForestKeeperCompleted)
        {
            AddToken(saveData.worldState.openedChestIds, QuestId + ".Objective." + TalkToForestKeeperObjectiveId);
        }

        if (obtainBasicFireSkillCompleted)
        {
            AddToken(saveData.worldState.openedChestIds, QuestId + ".Objective." + ObtainBasicFireSkillObjectiveId);
        }

        if (hitBambooBugWithFireCompleted)
        {
            AddToken(saveData.worldState.openedChestIds, QuestId + ".Objective." + HitBambooBugWithFireObjectiveId);
        }

        if (fireBasicUnlocked || PlayerHasFireSkill())
        {
            AddToken(saveData.worldState.openedChestIds, QuestId + ".Skill." + FireBasicSkillId);
        }

        if (bambooBarrierOpened)
        {
            AddToken(saveData.worldState.openedChestIds, BambooBarrierOpenedSaveToken);
        }

        if (rewardGranted)
        {
            AddToken(saveData.worldState.openedChestIds, RewardGrantedSaveToken);
        }
    }

    public void RestoreGameSaveData(GameSaveData saveData)
    {
        ResetQuestForRestore();

        if (saveData == null || saveData.worldState == null || saveData.worldState.openedChestIds == null)
        {
            NotifyChanged();
            return;
        }

        List<string> tokens = saveData.worldState.openedChestIds;

        if (tokens.Contains(QuestId + ".State." + BurnBambooBarrierQuestState.Completed))
        {
            questState = BurnBambooBarrierQuestState.Completed;
        }
        else if (tokens.Contains(QuestId + ".State." + BurnBambooBarrierQuestState.Active))
        {
            questState = BurnBambooBarrierQuestState.Active;
        }

        if (tokens.Contains(QuestId + ".Tracked"))
        {
            trackedQuestId = QuestId;
        }

        RestoreActiveObjective(tokens);

        talkToForestKeeperCompleted = tokens.Contains(QuestId + ".Objective." + TalkToForestKeeperObjectiveId);
        obtainBasicFireSkillCompleted = tokens.Contains(QuestId + ".Objective." + ObtainBasicFireSkillObjectiveId);
        hitBambooBugWithFireCompleted = tokens.Contains(QuestId + ".Objective." + HitBambooBugWithFireObjectiveId);
        fireBasicUnlocked = tokens.Contains(QuestId + ".Skill." + FireBasicSkillId) || PlayerHasFireSkill();
        bambooBarrierOpened = tokens.Contains(BambooBarrierOpenedSaveToken) ||
                              questState == BurnBambooBarrierQuestState.Completed;
        rewardGranted = tokens.Contains(RewardGrantedSaveToken) ||
                        questState == BurnBambooBarrierQuestState.Completed;

        NotifyChanged();
    }

    [ContextMenu("Debug/Reset Quest")]
    public void ResetQuestForTesting()
    {
        questState = BurnBambooBarrierQuestState.Locked;
        trackedQuestId = string.Empty;
        activeObjectiveId = string.Empty;
        talkToForestKeeperCompleted = false;
        obtainBasicFireSkillCompleted = false;
        hitBambooBugWithFireCompleted = false;
        fireBasicUnlocked = false;
        bambooBarrierOpened = false;
        rewardGranted = false;
        Log("Reset quest for testing.");
        NotifyChanged();
    }

    private void ResetQuestForRestore()
    {
        questState = BurnBambooBarrierQuestState.Locked;
        trackedQuestId = string.Empty;
        activeObjectiveId = string.Empty;
        talkToForestKeeperCompleted = false;
        obtainBasicFireSkillCompleted = false;
        hitBambooBugWithFireCompleted = false;
        fireBasicUnlocked = false;
        bambooBarrierOpened = false;
        rewardGranted = false;
    }

    private bool SetObjectiveCompleted(string objectiveId)
    {
        switch (objectiveId)
        {
            case TalkToForestKeeperObjectiveId:
                if (talkToForestKeeperCompleted)
                {
                    return false;
                }

                talkToForestKeeperCompleted = true;
                return true;

            case ObtainBasicFireSkillObjectiveId:
                if (obtainBasicFireSkillCompleted)
                {
                    return false;
                }

                obtainBasicFireSkillCompleted = true;
                return true;

            case HitBambooBugWithFireObjectiveId:
                if (hitBambooBugWithFireCompleted)
                {
                    return false;
                }

                hitBambooBugWithFireCompleted = true;
                return true;

            default:
                Debug.LogWarning(
                    "[BurnBambooBarrierQuestService] Unknown objective: " + objectiveId,
                    this);
                return false;
        }
    }

    private static bool IsSupportedQuest(string questId)
    {
        return string.Equals(questId, QuestId, StringComparison.OrdinalIgnoreCase);
    }

    private void RestoreActiveObjective(List<string> tokens)
    {
        activeObjectiveId = string.Empty;

        if (questState != BurnBambooBarrierQuestState.Active)
        {
            return;
        }

        if (tokens.Contains(QuestId + ".ActiveObjective." + HitBambooBugWithFireObjectiveId))
        {
            activeObjectiveId = HitBambooBugWithFireObjectiveId;
            return;
        }

        if (tokens.Contains(QuestId + ".ActiveObjective." + ObtainBasicFireSkillObjectiveId))
        {
            activeObjectiveId = ObtainBasicFireSkillObjectiveId;
            return;
        }

        activeObjectiveId = TalkToForestKeeperObjectiveId;
    }

    private static void AddToken(List<string> tokens, string token)
    {
        if (tokens == null || string.IsNullOrWhiteSpace(token) || tokens.Contains(token))
        {
            return;
        }

        tokens.Add(token);
    }

    private bool PlayerHasFireSkill()
    {
        PlayerBrushSkillInventory inventory = FindAnyObjectByType<PlayerBrushSkillInventory>();
        return inventory != null && inventory.HasBrushSkill(BrushSkillType.Fire);
    }

    private void NotifyChanged()
    {
        StateChanged?.Invoke();
    }

    private void Log(string message)
    {
        if (!logStateChanges)
        {
            return;
        }

        Debug.Log("[BurnBambooBarrierQuestService] " + message, this);
    }
}
