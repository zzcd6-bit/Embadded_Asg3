using System.Reflection;
using PixelCrushers.DialogueSystem;
using UnityEngine;

public class BurnBambooBarrierDialogueLuaBridge : MonoBehaviour
{
    [SerializeField] private BurnBambooBarrierQuestService questService;
    [SerializeField] private bool unregisterOnDisable;
    [SerializeField] private bool logBridgeCalls = true;

    private void Awake()
    {
        if (questService == null)
        {
            questService = BurnBambooBarrierQuestService.GetOrCreate();
        }
    }

    private void OnEnable()
    {
        RegisterLuaFunction(nameof(AcceptQuest));
        RegisterLuaFunction(nameof(TrackQuest));
        RegisterLuaFunction(nameof(ActivateObjective));
        RegisterLuaFunction(nameof(CompleteObjective));
        RegisterLuaFunction(nameof(UnlockSkill));
        RegisterLuaFunction(nameof(HasSkill));
        RegisterLuaFunction(nameof(GetQuestState), "QuestState");
        RegisterLuaFunction(nameof(IsQuestState));
        RegisterLuaFunction(nameof(JumpParagraph));
        RegisterLuaFunction(nameof(EndConversation));
        RegisterLuaFunction(nameof(ShowQuestNotification));
        RegisterLuaFunction(nameof(ShowTutorial));
        RegisterLuaFunction(nameof(ShowSkillUnlockUI));
    }

    private void OnDisable()
    {
        if (!unregisterOnDisable)
        {
            return;
        }

        Lua.UnregisterFunction(nameof(AcceptQuest));
        Lua.UnregisterFunction(nameof(TrackQuest));
        Lua.UnregisterFunction(nameof(ActivateObjective));
        Lua.UnregisterFunction(nameof(CompleteObjective));
        Lua.UnregisterFunction(nameof(UnlockSkill));
        Lua.UnregisterFunction(nameof(HasSkill));
        Lua.UnregisterFunction("QuestState");
        Lua.UnregisterFunction(nameof(IsQuestState));
        Lua.UnregisterFunction(nameof(JumpParagraph));
        Lua.UnregisterFunction(nameof(EndConversation));
        Lua.UnregisterFunction(nameof(ShowQuestNotification));
        Lua.UnregisterFunction(nameof(ShowTutorial));
        Lua.UnregisterFunction(nameof(ShowSkillUnlockUI));
    }

    public void AcceptQuest(string questId)
    {
        if (Service == null)
        {
            return;
        }

        Service.AcceptQuest(questId);
        Log("AcceptQuest(" + questId + ")");
    }

    public void TrackQuest(string questId)
    {
        if (Service == null)
        {
            return;
        }

        Service.TrackQuest(questId);
        Log("TrackQuest(" + questId + ")");
    }

    public void ActivateObjective(string questId, string objectiveId)
    {
        if (Service == null)
        {
            return;
        }

        Service.ActivateObjective(questId, objectiveId);
        Log("ActivateObjective(" + questId + ", " + objectiveId + ")");
    }

    public void CompleteObjective(string questId, string objectiveId)
    {
        if (Service == null)
        {
            return;
        }

        Service.CompleteObjective(questId, objectiveId);
        Log("CompleteObjective(" + questId + ", " + objectiveId + ")");
    }

    public void UnlockSkill(string skillId)
    {
        if (Service == null)
        {
            return;
        }

        Service.UnlockSkill(skillId);
        Log("UnlockSkill(" + skillId + ")");
    }

    public bool HasSkill(string skillId)
    {
        if (Service == null)
        {
            return false;
        }

        bool result = Service.HasSkill(skillId);
        Log("HasSkill(" + skillId + ") = " + result);
        return result;
    }

    public string GetQuestState(string questId)
    {
        if (Service == null)
        {
            return "Unknown";
        }

        string state = Service.GetQuestState(questId);
        Log("QuestState(" + questId + ") = " + state);
        return state;
    }

    public bool IsQuestState(string questId, string state)
    {
        if (Service == null)
        {
            return false;
        }

        bool result = Service.IsQuestState(questId, state);
        Log("IsQuestState(" + questId + ", " + state + ") = " + result);
        return result;
    }

    public void JumpParagraph(string paragraphId)
    {
        if (string.IsNullOrWhiteSpace(paragraphId))
        {
            return;
        }

        Log("JumpParagraph(" + paragraphId + ")");
        StartCoroutine(JumpParagraphAfterCurrentConversationStops(paragraphId));
    }

    public void EndConversation()
    {
        Log("EndConversation()");
        DialogueManager.StopConversation();
    }

    public void ShowQuestNotification(string message)
    {
        if (DialogueManager.instance != null)
        {
            DialogueManager.ShowAlert(message);
        }

        Debug.Log("[QuestNotification] " + message, this);
    }

    public void ShowTutorial(string tutorialId)
    {
        Debug.Log("[Tutorial] " + tutorialId, this);
    }

    public void ShowSkillUnlockUI(string skillId)
    {
        Debug.Log("[SkillUnlock] " + skillId, this);
    }

    private BurnBambooBarrierQuestService Service
    {
        get
        {
            if (questService == null)
            {
                questService = BurnBambooBarrierQuestService.GetOrCreate();
            }

            return questService;
        }
    }

    private void RegisterLuaFunction(string methodName, string luaName = null)
    {
        MethodInfo method = GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public);

        if (method == null)
        {
            Debug.LogWarning(
                "[BurnBambooBarrierDialogueLuaBridge] Missing Lua bridge method: " + methodName,
                this);
            return;
        }

        Lua.RegisterFunction(string.IsNullOrEmpty(luaName) ? methodName : luaName, this, method);
    }

    private System.Collections.IEnumerator JumpParagraphAfterCurrentConversationStops(string paragraphId)
    {
        if (DialogueManager.isConversationActive)
        {
            DialogueManager.StopConversation();
            yield return null;
        }

        DialogueManager.StartConversation(paragraphId);
    }

    private void Log(string message)
    {
        if (!logBridgeCalls)
        {
            return;
        }

        Debug.Log("[BurnBambooBarrierDialogueLuaBridge] " + message, this);
    }
}
