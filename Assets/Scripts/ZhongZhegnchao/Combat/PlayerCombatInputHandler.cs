using UnityEngine;

public class PlayerCombatInputHandler : MonoBehaviour
{
    private PlayerInputReceiver inputReceiver;
    private CombatActionManager combatActionManager;
    private PlayerAnimationController animationController;
    private PlayerLockOnController lockOnController;
    private PlayerLocomotion playerLocomotion;

    private string normalAttackActionId;

    private bool initialized;
    private bool bound;

    public void Init(
        PlayerInputReceiver input,
        CombatActionManager manager,
        string normalAttackId,
        PlayerAnimationController animController,
        PlayerLockOnController playerLockOnController
    )
    {
        UnbindInput();

        inputReceiver = input;
        combatActionManager = manager;
        normalAttackActionId = normalAttackId;
        animationController = animController;
        lockOnController = playerLockOnController;

        CacheReferences();

        initialized = true;

        BindInput();
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        if (initialized)
        {
            BindInput();
        }
    }

    private void OnDisable()
    {
        UnbindInput();
    }

    private void CacheReferences()
    {
        if (playerLocomotion == null)
        {
            playerLocomotion = GetComponent<PlayerLocomotion>();
        }

        if (playerLocomotion == null)
        {
            playerLocomotion = GetComponentInChildren<PlayerLocomotion>();
        }
    }

    private void BindInput()
    {
        if (bound)
        {
            return;
        }

        if (inputReceiver == null)
        {
            return;
        }

        inputReceiver.AttackPressed += OnAttackPressed;
        bound = true;
    }

    private void UnbindInput()
    {
        if (!bound)
        {
            return;
        }

        if (inputReceiver != null)
        {
            inputReceiver.AttackPressed -= OnAttackPressed;
        }

        bound = false;
    }

    private void OnAttackPressed()
    {
        CacheReferences();

        // Dodge ÆÚ¼ä²»ÔÊÐí¹¥»÷£¬±ÜÃâ Dodge ºÍ Attack ÇÀ¶¯»­ / ÇÀ×´Ì¬¡£
        if (playerLocomotion != null && playerLocomotion.IsDodging)
        {
            return;
        }

        if (animationController != null && !animationController.CanStartCombatAction())
        {
            return;
        }

        if (combatActionManager == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(normalAttackActionId))
        {
            Debug.LogWarning("[PlayerCombatInputHandler] normalAttackActionId is empty.", this);
            return;
        }

        if (lockOnController != null)
        {
            lockOnController.FaceCurrentTargetForAttack();
        }

        combatActionManager.TryPlayAction(normalAttackActionId);
    }
}