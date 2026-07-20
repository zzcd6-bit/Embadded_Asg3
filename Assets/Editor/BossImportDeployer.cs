using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

[InitializeOnLoad]
public static class BossImportDeployer
{
    private const string AutoRunMarkerPath = "Assets/Editor/BossImportDeployer.autorun";

    private const string ScenePath = "Assets/Scene/Merge Scene 2.unity";
    private const string OutputFolder = "Assets/Prefabs/BossImported";
    private const string BossSourcePrefabPath = "Assets/Boss_FlameBoss/FlameBoss.prefab";
    private const string MinionSourcePrefabPath = "Assets/Prefabw/BeastForBoss.prefab";
    private const string BossCombatPrefabPath = OutputFolder + "/FlameBossCombat.prefab";
    private const string MinionCombatPrefabPath = OutputFolder + "/BeastForBossCombat.prefab";

    private const string PoisonPointPrefabPath = "Assets/Piloto Studio/Indicators/Indicator_Circle_Simple_Red.prefab";
    private const string SlamWarningPrefabPath = "Assets/Piloto Studio/Indicators/Indicator_Square_Simple_Red 1.prefab";
    private const string SweepWarningPrefabPath = "Assets/Piloto Studio/Indicators/Indicator_Cone_Simple_Red 2.prefab";
    private const string SummonFxPrefabPath = "Assets/Piloto Studio/SpellsPack5/BloodMagic_GroundAOE.prefab";

    private const string BossRoarClipPath = "Assets/YQCsound/Boss_Roar.mp3";
    private const string BossAttackClipPath = "Assets/YQCsound/Boss_Attack.mp3";
    private const string BossAoeClipPath = "Assets/YQCsound/Boss_AOE.mp3";
    private const string BossSummonClipPath = "Assets/YQCsound/Boss_Summon.mp3";

    static BossImportDeployer()
    {
        EditorApplication.delayCall += TryAutoRun;
    }

    [MenuItem("Tools/Boss Import/Deploy Flame Boss To Merge Scene 2")]
    public static void Run()
    {
        AssetDatabase.Refresh();
        EnsureFolder(OutputFolder);

        GameObject minionCombatPrefab = BuildMinionCombatPrefab();
        GameObject bossCombatPrefab = BuildBossCombatPrefab(minionCombatPrefab);

        DeployToScene(bossCombatPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BossImportDeployer] Flame Boss deployed to Merge Scene 2.");
    }

    private static void TryAutoRun()
    {
        if (!File.Exists(AutoRunMarkerPath))
            return;

        File.Delete(AutoRunMarkerPath);

        try
        {
            Run();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            AssetDatabase.Refresh();
        }
    }

    private static GameObject BuildBossCombatPrefab(GameObject minionCombatPrefab)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BossSourcePrefabPath);
        try
        {
            root.name = "FlameBossCombat";
            SetTagIfAvailable(root, "Enemy");
            SetLayerRecursively(root, LayerMask.NameToLayer("Enemy"));

            BossController boss = EnsureComponent<BossController>(root);
            BossDamageableAdapter adapter = EnsureComponent<BossDamageableAdapter>(root);
            _ = adapter;

            boss.maxHP = 500;
            boss.playerTag = "Player";
            boss.turnSpeed = 6f;
            boss.facePlayer = true;
            boss.snapFaceOnWarning = true;
            boss.attackCoolDown = 4f;
            boss.attackCoolDownD = 10f;

            boss.enableAttackA = true;
            boss.warningTimeA = 1.5f;
            boss.animateDelayA = 1f;
            boss.windowTimeA = 0.5f;
            boss.hitTickA = 0.05f;
            boss.lifeAfterWindowA = 0.4f;
            boss.spawnCount = 12;
            boss.perPointRandomDelayMax = 1f;
            boss.areaWidth = 50f;
            boss.areaLength = 50f;
            boss.minSpacing = 15f;
            boss.triesPerPoint = 12;
            boss.poisonPointPrefab = LoadRequired<GameObject>(PoisonPointPrefabPath);
            boss.animAttackATrigger = "AttackA";
            boss.damageA = 18f;
            boss.hitRadiusA = 7f;
            boss.attackWindowTime = 1f;
            boss.pointLifeAfterHit = 0.4f;
            boss.delayBeforeHitA = 0f;

            boss.enableAttackB = true;
            boss.reactionTime = 0.7f;
            boss.slamOffset = 2.5f;
            boss.groundY = 0.4f;
            boss.damageB = 20f;
            boss.playerMask = LayerMask.GetMask("Player");
            boss.damageOncePerAttack = true;
            boss.windowHitInterval = 0.08f;
            boss.warningPrefab = LoadRequired<GameObject>(SlamWarningPrefabPath);
            boss.warningOrigin = root.transform;
            boss.animAttackBTrigger = "AttackB";
            boss.animIsAttackingBool = "IsAttacking";

            boss.enableSweepC = true;
            boss.sweepCWeight = 0.5f;
            boss.damageC = 15f;
            boss.sweepOffset = 1.2f;
            boss.sweepGroundY = 0.4f;
            boss.warningPrefabC = LoadRequired<GameObject>(SweepWarningPrefabPath);
            boss.warningOriginC = root.transform;
            boss.animAttackCTrigger = "AttackC";

            boss.enableAttackD = true;
            boss.animAttackDTrigger = "AttackD";
            boss.attackD_AnimationDelay = 2f;
            boss.attackD_FXDelay = 2f;
            boss.minionPrefab = minionCombatPrefab;
            boss.summonFxPrefab = LoadRequired<GameObject>(SummonFxPrefabPath);
            boss.summonFXOfset = 0.4f;
            boss.minionCount = 1;
            boss.minionSpawnRadius = 5f;
            boss.minionSpawnY = 0f;
            boss.bossDamagePerMinionDeath = 50;

            boss.bossFightActive = false;
            boss.animInTrigger = "BossIn";
            boss.hideUIOnDeath = true;
            boss.animDeathTrigger = "TriggerDeath";
            boss.gunBaseMultiplier = 3f;
            boss.critChance = 0.152f;
            boss.critMultiplier = 10f;
            boss.applyGunRulesInsideDamageBoss = false;

            boss.dropStep01 = 0.2f;
            boss.enableStepDrops = false;
            boss.dropPrefabs = new GameObject[0];
            boss.dropCountPerTrigger = 2;
            boss.dropForwardDistance = 4f;
            boss.dropDistanceRandom = 1f;
            boss.dropHeight = 0.6f;
            boss.snapToGround = true;
            boss.groundMask = LayerMask.GetMask("Ground");

            BossSound sound = EnsureComponent<BossSound>(root);
            sound.Boss_Roar = LoadRequired<AudioClip>(BossRoarClipPath);
            sound.Boss_Attack = LoadRequired<AudioClip>(BossAttackClipPath);
            sound.Boss_AOE = LoadRequired<AudioClip>(BossAoeClipPath);
            sound.Boss_Summon = LoadRequired<AudioClip>(BossSummonClipPath);
            sound.attackDelay = 0f;

            EnsureDamageCollider(root);
            EnableRenderers(root);

            PrefabUtility.SaveAsPrefabAsset(root, BossCombatPrefabPath, out bool success);
            if (!success)
                throw new IOException("Failed to save " + BossCombatPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return LoadRequired<GameObject>(BossCombatPrefabPath);
    }

    private static GameObject BuildMinionCombatPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(MinionSourcePrefabPath);
        try
        {
            root.name = "BeastForBossCombat";
            SetTagIfAvailable(root, "Enemy");
            SetLayerRecursively(root, LayerMask.NameToLayer("Enemy"));

            EnemyAI[] aiComponents = root.GetComponentsInChildren<EnemyAI>(true);
            for (int i = 0; i < aiComponents.Length; i++)
            {
                aiComponents[i].locked = true;
                aiComponents[i].lockedViewDistance = 999f;
                aiComponents[i].lockedIgnoreLineOfSight = true;
            }

            EnemyBehavior[] behaviors = root.GetComponentsInChildren<EnemyBehavior>(true);
            for (int i = 0; i < behaviors.Length; i++)
                SetSerializedLayerMask(behaviors[i], "playerMask", LayerMask.GetMask("Player"));

            EnemyHealthController[] healthControllers = root.GetComponentsInChildren<EnemyHealthController>(true);
            for (int i = 0; i < healthControllers.Length; i++)
                EnsureComponent<EnemyHealthDamageableAdapter>(healthControllers[i].gameObject);

            PrefabUtility.SaveAsPrefabAsset(root, MinionCombatPrefabPath, out bool success);
            if (!success)
                throw new IOException("Failed to save " + MinionCombatPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return LoadRequired<GameObject>(MinionCombatPrefabPath);
    }

    private static void DeployToScene(GameObject bossCombatPrefab)
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject player = FindPlayer();
        if (player != null)
            ConfigurePlayerCompatibility(player);
        else
            Debug.LogWarning("[BossImportDeployer] Player was not found in Merge Scene 2.");

        DestroyRootIfExists("Imported Flame Boss");
        DestroyRootIfExists("Imported BossFightTrigger");

        Vector3 playerPosition = player != null ? player.transform.position : new Vector3(0f, 0f, -3.54f);
        Vector3 bossPosition = new Vector3(playerPosition.x, 0f, playerPosition.z + 21.5f);

        GameObject bossObject = (GameObject)PrefabUtility.InstantiatePrefab(bossCombatPrefab);
        bossObject.name = "Imported Flame Boss";
        bossObject.transform.SetPositionAndRotation(bossPosition, Quaternion.identity);
        bossObject.SetActive(true);

        BossController boss = bossObject.GetComponent<BossController>();
        if (boss != null)
        {
            boss.player = player != null ? player.transform : null;
            boss.warningOrigin = bossObject.transform;
            boss.warningOriginC = bossObject.transform;
            boss.bossFightActive = false;
        }

        GameObject triggerObject = new GameObject("Imported BossFightTrigger");
        triggerObject.transform.position = Vector3.Lerp(playerPosition, bossPosition, 0.45f) + Vector3.up * 1.5f;

        BoxCollider box = triggerObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(14f, 4f, 10f);

        BossManager manager = triggerObject.AddComponent<BossManager>();
        manager.Boss = bossObject;
        manager.canTriggerMultipleTimes = false;
        manager.startDelay = 0.5f;
        manager.bossSpawnEffect = LoadRequired<GameObject>(SummonFxPrefabPath);

        GameObject spawnPoint = new GameObject("BossSpawnPoint");
        spawnPoint.transform.SetParent(triggerObject.transform, false);
        spawnPoint.transform.position = bossPosition;
        spawnPoint.transform.rotation = Quaternion.identity;
        manager.bossSpawnPoint = spawnPoint.transform;

        if (boss != null)
            boss.bossManager = manager;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static void ConfigurePlayerCompatibility(GameObject player)
    {
        SetTagIfAvailable(player, "Player");
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
            player.layer = playerLayer;

        FirstPersonController fpc = EnsureComponent<FirstPersonController>(player);
        if (fpc.animator == null)
            fpc.animator = player.GetComponentInChildren<Animator>(true);

        PlayerHealth health = EnsureComponent<PlayerHealth>(player);
        health.damageElement = ElementType.Fire;

        EnsureComponent<DamageFeedbackController>(player);
    }

    private static GameObject FindPlayer()
    {
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
            return taggedPlayer;

        PlayerDamageReceiver[] receivers = Object.FindObjectsByType<PlayerDamageReceiver>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        return receivers.Length > 0 ? receivers[0].gameObject : null;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
            component = target.AddComponent<T>();
        return component;
    }

    private static void EnsureFolder(string folder)
    {
        folder = folder.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
        if (string.IsNullOrEmpty(parent))
            parent = "Assets";

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }

    private static T LoadRequired<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            throw new FileNotFoundException("Required asset not found: " + path);
        return asset;
    }

    private static void SetTagIfAvailable(GameObject target, string tag)
    {
        try
        {
            target.tag = tag;
        }
        catch
        {
            Debug.LogWarning("[BossImportDeployer] Tag not available: " + tag);
        }
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (layer < 0)
            return;

        target.layer = layer;
        for (int i = 0; i < target.transform.childCount; i++)
            SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
    }

    private static void SetSerializedLayerMask(Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void EnsureDamageCollider(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].isTrigger)
                return;
        }

        CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
        capsule.isTrigger = false;
        capsule.center = new Vector3(0f, 2f, 0f);
        capsule.radius = 1.2f;
        capsule.height = 4f;
    }

    private static void EnableRenderers(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = true;
    }

    private static void DestroyRootIfExists(string objectName)
    {
        GameObject[] roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = roots.Length - 1; i >= 0; i--)
        {
            if (roots[i].name == objectName)
                Object.DestroyImmediate(roots[i]);
        }
    }
}
