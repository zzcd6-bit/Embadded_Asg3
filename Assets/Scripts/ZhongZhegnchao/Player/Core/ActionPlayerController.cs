using UnityEngine;

public class ActionPlayerController : MonoBehaviour
{
    [Header("动作配置 Addressables Key")]
    [SerializeField] private string playerActionSetKey = "Player_ActionSet";

    [Header("Lock On")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Action Config")]
    [SerializeField] private PlayerActionConfigSet playerActionSet;

#if UNITY_EDITOR
    [Header("Editor 本地测试配置")]
    [SerializeField] private bool useEditorLocalActionSet = true;

    [SerializeField] private PlayerActionConfigSet editorLocalActionSet;

    [SerializeField]
    private string editorLocalActionSetPath =
        "Assets/HotUpdate/GameAssets/Config/Combat/Player_ActionSet.asset";
#endif

    private CharacterController characterController;
    private PlayerInputReceiver inputReceiver;
    private PlayerLocomotion locomotion;
    private PlayerAnimationController animationController;
    private PlayerCameraController cameraController;

    private PlayerLockOnController lockOnController;

    private ActionPlayer actionPlayer;
    private CombatActionManager combatActionManager;
    private PlayerCombatInputHandler combatInputHandler;
    private PlayerDamageReceiver playerDamageReceiver;

    private Animator animator;
    private Transform cameraTarget;

    private PlayerLockOnCameraTargetGroup lockOnCameraTargetGroup;

    private bool initialized;
    private bool combatInitialized;

    private bool gameplayControlEnabled = true;

    public void InitPlayer()
    {
        Init();
    }

#if UNITY_EDITOR
    private void Start()
    {
        Init();
    }
#endif

    public void Init()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        Debug.Log("[PlayerController] Init start.");

        InitReferences();
        InitBaseComponents();

        LoadActionConfigSet();

        Debug.Log("[PlayerController] Init base complete.");
    }

    private void InitReferences()
    {
        characterController = GetComponent<CharacterController>();

        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }

        characterController.center = new Vector3(0f, 1f, 0f);
        characterController.radius = 0.36f;
        characterController.height = 2f;

        animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
        else
        {
            Debug.LogWarning("[PlayerController] Animator not found.");
        }

        cameraTarget = transform.Find("CameraTarget");

        if (cameraTarget == null)
        {
            GameObject targetObj = new GameObject("CameraTarget");
            targetObj.transform.SetParent(transform);
            targetObj.transform.localPosition = new Vector3(0f, 1.679f, 0f);
            targetObj.transform.localRotation = Quaternion.identity;
            cameraTarget = targetObj.transform;
        }
    }

    private void InitBaseComponents()
    {
        inputReceiver = GetOrAddComponent<PlayerInputReceiver>();
        animationController = GetOrAddComponent<PlayerAnimationController>();
        locomotion = GetOrAddComponent<PlayerLocomotion>();
        cameraController = GetOrAddComponent<PlayerCameraController>();

        lockOnController = GetOrAddComponent<PlayerLockOnController>();
        lockOnCameraTargetGroup = GetOrAddComponent<PlayerLockOnCameraTargetGroup>();

        actionPlayer = GetOrAddComponent<ActionPlayer>();
        combatActionManager = GetOrAddComponent<CombatActionManager>();
        combatInputHandler = GetOrAddComponent<PlayerCombatInputHandler>();
        playerDamageReceiver = GetOrAddComponent<PlayerDamageReceiver>();

        animationController.Init(animator);
        locomotion.Init(characterController, inputReceiver, animationController);
        cameraController.Init(cameraTarget);

        LayerMask finalEnemyLayer = GetFinalEnemyLayer();

        lockOnController.Init(
            inputReceiver,
            locomotion,
            transform,
            finalEnemyLayer
        );

        lockOnCameraTargetGroup.Init(
            lockOnController,
            cameraTarget
        );

        actionPlayer.Init(animationController, inputReceiver, locomotion);
    }

    public void SetGameplayControlEnabled(bool enabled)
    {
        gameplayControlEnabled = enabled;

        if (inputReceiver != null)
        {
            inputReceiver.enabled = enabled;
        }

        if (locomotion != null)
        {
            locomotion.enabled = enabled;
        }

        if (combatInputHandler != null)
        {
            combatInputHandler.enabled = enabled;
        }

        if (lockOnController != null)
        {
            lockOnController.enabled = enabled;
        }

        if (cameraController != null)
        {
            cameraController.SetCameraInputEnabled(enabled);
        }

        Debug.Log("[PlayerController] Gameplay control enabled: " + enabled);
    }

    private LayerMask GetFinalEnemyLayer()
    {
        if (enemyLayer.value != 0)
        {
            return enemyLayer;
        }

        int enemyLayerMask = LayerMask.GetMask("Enemy");

        if (enemyLayerMask == 0)
        {
            Debug.LogWarning(
                "[PlayerController] 没有设置 enemyLayer，并且项目中找不到名为 Enemy 的 Layer。LockOn 可能找不到敌人。"
            );
        }

        return enemyLayerMask;
    }

    private void LoadActionConfigSet()
    {
        if (playerActionSet == null)
        {
            Debug.LogError("[PlayerController] playerActionSet is null. Please assign Player_ActionSet in Inspector.", this);
            return;
        }

        InitCombat(playerActionSet);
    }

#if UNITY_EDITOR
    private PlayerActionConfigSet GetEditorLocalActionSet()
    {
        if (editorLocalActionSet != null)
        {
            return editorLocalActionSet;
        }

        if (string.IsNullOrEmpty(editorLocalActionSetPath))
        {
            return null;
        }

        PlayerActionConfigSet asset =
            UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerActionConfigSet>(
                editorLocalActionSetPath
            );

        if (asset == null)
        {
            Debug.LogWarning(
                "[PlayerController] 找不到 Editor 本地 ActionSet，路径: " +
                editorLocalActionSetPath
            );
        }

        return asset;
    }
#endif

    private void OnActionConfigSetLoaded(PlayerActionConfigSet actionSet)
    {
        if (actionSet == null)
        {
            Debug.LogError("[PlayerController] PlayerActionConfigSet 加载成功但结果为空。");
            return;
        }

        InitCombat(actionSet);
    }

    private void OnActionConfigSetLoadFailed(string error)
    {
        Debug.LogError("[PlayerController] PlayerActionConfigSet 加载失败：" + error);
    }

    private void InitCombat(PlayerActionConfigSet actionSet)
    {
        if (combatInitialized)
        {
            return;
        }

        combatInitialized = true;

        ActionConfig[] allActions = actionSet.GetAllActions();

        combatActionManager.Init(actionPlayer, allActions);

        combatInputHandler.Init(
            inputReceiver,
            combatActionManager,
            actionSet.normalAttackActionId,
            animationController,
            lockOnController
        );

        Debug.Log(
            $"[PlayerController] Combat Init complete. Action Count = {allActions.Length}, NormalAttack = {actionSet.normalAttackActionId}"
        );
    }

    private T GetOrAddComponent<T>() where T : Component
    {
        T component = GetComponent<T>();

        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    private void OnDestroy()
    {
        
    }
}