#if UNITY_EDITOR
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BurnBambooBarrierStage4To10Deployer
{
    private const string ScenePath = "Assets/Scene/Ye Scene2.unity";
    private const string DatabasePath = "Assets/Scripts/Ye's Script/Dialogue/BurnBambooBarrier/BurnBambooBarrierDialogueDatabase.asset";
    private const string MazeGatePrefabPath = "Assets/My Prefab/Special/Maze Gate.prefab";

    private const string RuntimeRootName = "Quest Dialogue Runtime";
    private const string ForestKeeperName = "Forest Keeper Dialogue Capsule";
    private const string BarrierGateName = "Bamboo Bug Barrier Gate";

    private const string IntroductionConversation = "P_ForestKeeper_FireIntroduction";
    private const string FireReminderConversation = "P_ForestKeeper_FireReminder";
    private const string CompletedConversation = "P_ForestKeeper_AfterBarrierOpened";

    [MenuItem("Tools/Ye/Deploy Burn Bamboo Barrier Stage 4-10")]
    public static void DeployMenu()
    {
        Deploy();
    }

    public static void Deploy()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        DialogueDatabase database = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(DatabasePath);
        if (database == null)
        {
            Debug.LogError("[BurnBambooBarrierStage4To10Deployer] Dialogue database not found: " + DatabasePath);
            return;
        }

        AddOrReplaceFollowupConversations(database);

        GameObject runtimeRoot = GetOrCreateRoot(RuntimeRootName);
        BurnBambooBarrierQuestService questService = GetOrAddComponent<BurnBambooBarrierQuestService>(runtimeRoot);

        GameObject barrierGate = CreateOrUpdateBarrierGate(runtimeRoot.transform, questService);
        ConfigureForestKeeper(questService);

        EditorUtility.SetDirty(questService);
        EditorUtility.SetDirty(barrierGate);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BurnBambooBarrierStage4To10Deployer] Stage 4-10 deployed to Ye Scene2.");
    }

    private static GameObject CreateOrUpdateBarrierGate(Transform parent, BurnBambooBarrierQuestService questService)
    {
        GameObject gateObject = GameObject.Find(BarrierGateName);
        if (gateObject == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MazeGatePrefabPath);
            gateObject = prefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
                : new GameObject(BarrierGateName);

            gateObject.name = BarrierGateName;
            gateObject.transform.SetParent(parent);
            gateObject.transform.position = new Vector3(0f, 0f, 13f);
            gateObject.transform.rotation = Quaternion.identity;
            gateObject.transform.localScale = Vector3.one;
        }

        MazeGate mazeGate = GetOrAddComponent<MazeGate>(gateObject);
        mazeGate.edgeType = MazeEdgeType.BidirectionalDoorClosed;
        mazeGate.state = GateState.Closed;

        DisableBuiltInWalkOpenTrigger(gateObject);
        ConfigureWallColliderForAttackDetection(gateObject);

        BambooBugBarrierDamageReceiver receiver = GetOrAddComponent<BambooBugBarrierDamageReceiver>(gateObject);
        SerializedObject serialized = new SerializedObject(receiver);
        serialized.FindProperty("gate").objectReferenceValue = mazeGate;
        serialized.FindProperty("questService").objectReferenceValue = questService;
        serialized.FindProperty("requireQuestActive").boolValue = true;
        serialized.FindProperty("saveImmediatelyOnOpen").boolValue = true;
        serialized.FindProperty("requiredElement").enumValueIndex = (int)ElementType.Fire;
        serialized.FindProperty("acceptFireInfusedAttacker").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(mazeGate);
        EditorUtility.SetDirty(receiver);
        return gateObject;
    }

    private static void DisableBuiltInWalkOpenTrigger(GameObject gateObject)
    {
        MazeGateTriggerOpener[] openers = gateObject.GetComponentsInChildren<MazeGateTriggerOpener>(true);
        for (int i = 0; i < openers.Length; i++)
        {
            openers[i].enabled = false;
            EditorUtility.SetDirty(openers[i]);
        }

        Transform trigger = gateObject.transform.Find("Trigger");
        if (trigger == null)
        {
            return;
        }

        Collider[] triggerColliders = trigger.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < triggerColliders.Length; i++)
        {
            triggerColliders[i].enabled = false;
            EditorUtility.SetDirty(triggerColliders[i]);
        }
    }

    private static void ConfigureWallColliderForAttackDetection(GameObject gateObject)
    {
        Transform wallColliderRoot = gateObject.transform.Find("Wall Collider");
        if (wallColliderRoot == null)
        {
            Debug.LogWarning("[BurnBambooBarrierStage4To10Deployer] Wall Collider child was not found on Maze Gate.");
            return;
        }

        Collider[] colliders = wallColliderRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = true;
            colliders[i].isTrigger = true;
            EditorUtility.SetDirty(colliders[i]);
        }
    }

    private static void ConfigureForestKeeper(BurnBambooBarrierQuestService questService)
    {
        GameObject keeper = GameObject.Find(ForestKeeperName);
        if (keeper == null)
        {
            Debug.LogWarning("[BurnBambooBarrierStage4To10Deployer] Forest Keeper was not found.");
            return;
        }

        DialogueSystemReactable reactable = GetOrAddComponent<DialogueSystemReactable>(keeper);
        reactable.ConfigureConversation(IntroductionConversation);

        BurnBambooBarrierForestKeeperDialogueSelector selector =
            GetOrAddComponent<BurnBambooBarrierForestKeeperDialogueSelector>(keeper);

        SerializedObject serialized = new SerializedObject(selector);
        serialized.FindProperty("dialogueReactable").objectReferenceValue = reactable;
        serialized.FindProperty("questService").objectReferenceValue = questService;
        serialized.FindProperty("introductionConversation").stringValue = IntroductionConversation;
        serialized.FindProperty("fireReminderConversation").stringValue = FireReminderConversation;
        serialized.FindProperty("completedConversation").stringValue = CompletedConversation;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(reactable);
        EditorUtility.SetDirty(selector);
    }

    private static void AddOrReplaceFollowupConversations(DialogueDatabase database)
    {
        database.conversations.RemoveAll(c =>
            c.Title == FireReminderConversation ||
            c.Title == CompletedConversation);

        database.conversations.Add(CreateFireReminderConversation());
        database.conversations.Add(CreateCompletedConversation());
        database.ResetCache();
        EditorUtility.SetDirty(database);
    }

    private static Conversation CreateFireReminderConversation()
    {
        Conversation conversation = CreateConversation(102, FireReminderConversation, 1, 2);
        DialogueEntry start = CreateEntry(conversation, 0, "START", string.Empty, 1, 2, true);
        DialogueEntry line1 = CreateEntry(conversation, 1, "Keeper_Reminder_01", "火元素已经交给你了。去攻击那面异常的竹墙。", 2, 1);
        DialogueEntry line2 = CreateEntry(conversation, 2, "Keeper_Reminder_02", "普通攻击只会被弹开，带着火焰的攻击才能解除竹虫伪装。", 2, 1);
        line2.userScript = "ActivateObjective(\"Quest_BurnBambooBarrier\", \"HitBambooBugWithFire\")";

        Link(start, line1);
        Link(line1, line2);
        conversation.dialogueEntries.AddRange(new[] { start, line1, line2 });
        return conversation;
    }

    private static Conversation CreateCompletedConversation()
    {
        Conversation conversation = CreateConversation(103, CompletedConversation, 1, 2);
        DialogueEntry start = CreateEntry(conversation, 0, "START", string.Empty, 1, 2, true);
        DialogueEntry line1 = CreateEntry(conversation, 1, "Keeper_Completed_01", "竹虫的伪装已经解除，前方道路开放了。", 2, 1);
        DialogueEntry line2 = CreateEntry(conversation, 2, "Keeper_Completed_02", "继续深入竹林吧，但别把整片林子都点着。", 2, 1);

        Link(start, line1);
        Link(line1, line2);
        conversation.dialogueEntries.AddRange(new[] { start, line1, line2 });
        return conversation;
    }

    private static Conversation CreateConversation(int id, string title, int actorId, int conversantId)
    {
        Conversation conversation = new Conversation
        {
            id = id,
            fields = new List<Field>(),
            dialogueEntries = new List<DialogueEntry>()
        };
        conversation.Title = title;
        conversation.ActorID = actorId;
        conversation.ConversantID = conversantId;
        return conversation;
    }

    private static DialogueEntry CreateEntry(
        Conversation conversation,
        int id,
        string title,
        string dialogueText,
        int actorId,
        int conversantId,
        bool isRoot = false)
    {
        DialogueEntry entry = new DialogueEntry
        {
            id = id,
            conversationID = conversation.id,
            isRoot = isRoot,
            fields = new List<Field>(),
            outgoingLinks = new List<Link>()
        };
        entry.Title = title;
        entry.ActorID = actorId;
        entry.ConversantID = conversantId;
        entry.DialogueText = dialogueText;
        return entry;
    }

    private static void Link(DialogueEntry origin, DialogueEntry destination)
    {
        origin.outgoingLinks.Add(new Link(
            origin.conversationID,
            origin.id,
            destination.conversationID,
            destination.id));
    }

    private static GameObject GetOrCreateRoot(string name)
    {
        GameObject root = GameObject.Find(name);
        if (root != null)
        {
            return root;
        }

        root = new GameObject(name);
        root.transform.position = Vector3.zero;
        return root;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        return gameObject.AddComponent<T>();
    }
}
#endif
