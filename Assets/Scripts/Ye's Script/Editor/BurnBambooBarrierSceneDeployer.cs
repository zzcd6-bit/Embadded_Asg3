#if UNITY_EDITOR
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DialogueSystemControllerWrapper = PixelCrushers.DialogueSystem.Wrappers.DialogueSystemController;

public static class BurnBambooBarrierSceneDeployer
{
    private const string ScenePath = "Assets/Scene/Ye Scene2.unity";
    private const string DatabasePath = "Assets/Scripts/Ye's Script/Dialogue/BurnBambooBarrier/BurnBambooBarrierDialogueDatabase.asset";
    private const string PlayerStatsConfigPath = "Assets/Scripts/ZhongZhegnchao/Config/Player/Player_CharacterStatsConfig.asset";
    private const string DialogueManagerPrefabPath = "Assets/Plugins/Pixel Crushers/Dialogue System/Prefabs/Dialogue Manager.prefab";
    private const string StandardDialogueUiPrefabPath = "Assets/Plugins/Pixel Crushers/Dialogue System/Prefabs/Standard UI Prefabs/Templates/JRPG/JRPG Template Standard Dialogue UI.prefab";

    private const string DialogueManagerName = "Dialogue Manager";
    private const string DialogueUiName = "Dialogue UI Canvas";
    private const string RuntimeRootName = "Quest Dialogue Runtime";
    private const string EntranceTriggerName = "BurnBambooBarrier_EntranceTrigger";
    private const string ForestKeeperName = "Forest Keeper Dialogue Capsule";

    [MenuItem("Tools/Ye/Deploy Burn Bamboo Barrier Demo")]
    public static void DeployMenu()
    {
        Deploy();
    }

    public static void Deploy()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        DialogueDatabase database = CreateOrUpdateDatabase();
        GameObject runtimeRoot = GetOrCreateRoot(RuntimeRootName);

        BurnBambooBarrierQuestService questService =
            GetOrAddComponent<BurnBambooBarrierQuestService>(runtimeRoot);
        BurnBambooBarrierDialogueLuaBridge luaBridge =
            GetOrAddComponent<BurnBambooBarrierDialogueLuaBridge>(runtimeRoot);
        DialogueSceneStateController dialogueStateController =
            GetOrAddComponent<DialogueSceneStateController>(runtimeRoot);

        SerializedObject bridgeSerialized = new SerializedObject(luaBridge);
        bridgeSerialized.FindProperty("questService").objectReferenceValue = questService;
        bridgeSerialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject dialogueManager = GetOrCreateDialogueManager();
        GameObject dialogueUi = GetOrCreateDialogueUi();
        ConfigureDialogueStateController(dialogueStateController, dialogueUi);
        ConfigureDialogueManager(dialogueManager, database, dialogueUi);

        CreateOrUpdateEntranceTrigger(runtimeRoot.transform, questService);
        CreateOrUpdateForestKeeper(runtimeRoot.transform);
        EnsurePlayerStatsConfig();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BurnBambooBarrierSceneDeployer] Deployed demo objects to Ye Scene2.");
    }

    private static void EnsurePlayerStatsConfig()
    {
        PlayerCharacterStatsConfig statsConfig =
            AssetDatabase.LoadAssetAtPath<PlayerCharacterStatsConfig>(PlayerStatsConfigPath);
        if (statsConfig == null)
        {
            Debug.LogWarning("[BurnBambooBarrierSceneDeployer] Player stats config asset was not found.");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Player");
        }

        if (player == null)
        {
            Debug.LogWarning("[BurnBambooBarrierSceneDeployer] Player object was not found in Ye Scene2.");
            return;
        }

        PlayerCharacterStatsController statsController =
            GetOrAddComponent<PlayerCharacterStatsController>(player);

        SerializedObject serializedStats = new SerializedObject(statsController);
        serializedStats.FindProperty("config").objectReferenceValue = statsConfig;
        serializedStats.ApplyModifiedPropertiesWithoutUndo();
    }

    private static DialogueDatabase CreateOrUpdateDatabase()
    {
        DialogueDatabase database = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<DialogueDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        database.version = "Burn Bamboo Barrier Demo";
        database.author = "Codex";
        database.description = "Minimal Dialogue System database for Quest_BurnBambooBarrier.";
        database.actors = new List<Actor>
        {
            CreateActor(1, "Player", true),
            CreateActor(2, "守林人", false),
            CreateActor(3, "系统", false)
        };
        database.items = new List<Item>();
        database.locations = new List<Location>();
        database.variables = new List<Variable>();
        database.conversations = new List<Conversation>
        {
            CreateForestKeeperIntroduction(),
            CreateFireGranted()
        };
        database.ResetEmphasisSettings();
        database.ResetCache();
        EditorUtility.SetDirty(database);
        return database;
    }

    private static Actor CreateActor(int id, string name, bool isPlayer)
    {
        Actor actor = new Actor
        {
            id = id,
            fields = new List<Field>()
        };
        actor.Name = name;
        actor.IsPlayer = isPlayer;
        return actor;
    }

    private static Conversation CreateForestKeeperIntroduction()
    {
        Conversation conversation = CreateConversation(100, "P_ForestKeeper_FireIntroduction", 1, 2);

        DialogueEntry start = CreateEntry(
            conversation,
            0,
            "START",
            string.Empty,
            1,
            2,
            true);

        DialogueEntry line1 = CreateEntry(conversation, 1, "Keeper_01", "前面的路走不通了。", 2, 1);
        DialogueEntry line2 = CreateEntry(conversation, 2, "Keeper_02", "那不是普通竹墙，而是一只藏进竹林里的竹虫。", 2, 1);
        DialogueEntry line3 = CreateEntry(conversation, 3, "Player_01", "竹虫能变成一整面墙？", 1, 2);
        DialogueEntry line4 = CreateEntry(conversation, 4, "Keeper_03", "它会藏在竹节和根须之间，看起来和周围竹子没有区别。", 2, 1);
        DialogueEntry line5 = CreateEntry(conversation, 5, "Keeper_04", "普通攻击惊不动它，只有火元素能破坏伪装。", 2, 1);
        line5.userScript = "CompleteObjective(\"Quest_BurnBambooBarrier\", \"TalkToForestKeeper\")";

        DialogueEntry teach = CreateEntry(conversation, 6, "TeachFire", "把使用火元素的方法教给我。", 1, 2);
        teach.MenuText = "把使用火元素的方法教给我。";
        teach.userScript =
            "UnlockSkill(\"FireBasic\"); " +
            "CompleteObjective(\"Quest_BurnBambooBarrier\", \"ObtainBasicFireSkill\"); " +
            "ShowSkillUnlockUI(\"FireBasic\"); " +
            "ShowTutorial(\"Tutorial_FireBasic\")";
        teach.currentSequence = "Continue()";

        DialogueEntry later = CreateEntry(conversation, 7, "ComeBackLater", "我准备好后再来。", 1, 2);
        later.MenuText = "我准备好后再来。";
        later.userScript = "EndConversation()";

        DialogueEntry jump = CreateEntry(conversation, 8, "Jump_FireGranted", string.Empty, 2, 1);
        jump.userScript = "JumpParagraph(\"P_ForestKeeper_FireGranted\")";
        jump.currentSequence = "Continue()";

        Link(start, line1);
        Link(line1, line2);
        Link(line2, line3);
        Link(line3, line4);
        Link(line4, line5);
        Link(line5, teach);
        Link(line5, later);
        Link(teach, jump);

        conversation.dialogueEntries.AddRange(new[]
        {
            start, line1, line2, line3, line4, line5, teach, later, jump
        });
        return conversation;
    }

    private static Conversation CreateFireGranted()
    {
        Conversation conversation = CreateConversation(101, "P_ForestKeeper_FireGranted", 1, 2);

        DialogueEntry start = CreateEntry(
            conversation,
            0,
            "START",
            string.Empty,
            1,
            2,
            true);

        DialogueEntry line1 = CreateEntry(conversation, 1, "System_01", "获得火系基础技能：引火。", 3, 1);
        DialogueEntry line2 = CreateEntry(conversation, 2, "System_02", "绘制火元素符号，可以使攻击附带火元素。", 3, 1);
        DialogueEntry line3 = CreateEntry(conversation, 3, "Keeper_01", "你不需要烧毁整片竹林。", 2, 1);
        DialogueEntry line4 = CreateEntry(conversation, 4, "Keeper_02", "只要让火元素碰到那面异常竹墙，竹虫就无法维持伪装。", 2, 1);
        DialogueEntry line5 = CreateEntry(conversation, 5, "Keeper_03", "前方颜色稍暗、竹叶没有随风摆动的地方，就是它。", 2, 1);
        line5.userScript = "ActivateObjective(\"Quest_BurnBambooBarrier\", \"HitBambooBugWithFire\")";

        Link(start, line1);
        Link(line1, line2);
        Link(line2, line3);
        Link(line3, line4);
        Link(line4, line5);

        conversation.dialogueEntries.AddRange(new[]
        {
            start, line1, line2, line3, line4, line5
        });
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

    private static GameObject GetOrCreateDialogueManager()
    {
        GameObject existing = GameObject.Find(DialogueManagerName);
        if (existing != null)
        {
            return existing;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueManagerPrefabPath);
        GameObject instance = prefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
            : new GameObject(DialogueManagerName);
        instance.name = DialogueManagerName;
        return instance;
    }

    private static GameObject GetOrCreateDialogueUi()
    {
        GameObject existing = GameObject.Find(DialogueUiName);
        if (existing != null)
        {
            EnsureDialogueUiCanvasComponents(existing);
            return existing;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StandardDialogueUiPrefabPath);
        GameObject instance = prefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
            : new GameObject(DialogueUiName);
        instance.name = DialogueUiName;
        EnsureDialogueUiCanvasComponents(instance);
        return instance;
    }

    private static void EnsureDialogueUiCanvasComponents(GameObject dialogueUi)
    {
        Canvas canvas = GetOrAddComponent<Canvas>(dialogueUi);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(dialogueUi);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GetOrAddComponent<GraphicRaycaster>(dialogueUi);
    }

    private static void ConfigureDialogueStateController(
        DialogueSceneStateController dialogueStateController,
        GameObject dialogueUi)
    {
        SerializedObject serialized = new SerializedObject(dialogueStateController);
        serialized.FindProperty("dialogueUiObject").objectReferenceValue = dialogueUi;
        serialized.FindProperty("dialogueUiObjectName").stringValue = DialogueUiName;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureDialogueManager(
        GameObject dialogueManager,
        DialogueDatabase database,
        GameObject dialogueUiObject)
    {
        DialogueSystemControllerWrapper controller = dialogueManager.GetComponent<DialogueSystemControllerWrapper>();
        if (controller == null)
        {
            controller = dialogueManager.AddComponent<DialogueSystemControllerWrapper>();
        }

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("initialDatabase").objectReferenceValue = database;

        SerializedProperty displaySettings = serialized.FindProperty("displaySettings");
        SerializedProperty dialogueUiProperty = displaySettings.FindPropertyRelative("dialogueUI");
        if (dialogueUiProperty != null)
        {
            dialogueUiProperty.objectReferenceValue = dialogueUiObject;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateOrUpdateEntranceTrigger(Transform parent, BurnBambooBarrierQuestService questService)
    {
        GameObject trigger = GameObject.Find(EntranceTriggerName);
        if (trigger == null)
        {
            trigger = new GameObject(EntranceTriggerName);
            trigger.transform.SetParent(parent);
            trigger.transform.position = new Vector3(0f, 1f, 4f);
            BoxCollider collider = trigger.AddComponent<BoxCollider>();
            collider.size = new Vector3(6f, 2f, 3f);
            collider.isTrigger = true;
        }

        BurnBambooBarrierEntranceTrigger entrance =
            GetOrAddComponent<BurnBambooBarrierEntranceTrigger>(trigger);
        SerializedObject serialized = new SerializedObject(entrance);
        serialized.FindProperty("questService").objectReferenceValue = questService;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateOrUpdateForestKeeper(Transform parent)
    {
        GameObject npc = GameObject.Find(ForestKeeperName);
        if (npc == null)
        {
            npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = ForestKeeperName;
            npc.transform.SetParent(parent);
            npc.transform.position = new Vector3(2f, 1f, 7f);
        }

        Collider collider = npc.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = false;
        }

        DialogueSystemReactable reactable = GetOrAddComponent<DialogueSystemReactable>(npc);
        SerializedObject serialized = new SerializedObject(reactable);
        serialized.FindProperty("conversationTitle").stringValue = "P_ForestKeeper_FireIntroduction";
        serialized.ApplyModifiedPropertiesWithoutUndo();
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
