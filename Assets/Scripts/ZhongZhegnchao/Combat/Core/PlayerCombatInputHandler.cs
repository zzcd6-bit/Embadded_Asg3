using UnityEngine;

public class PlayerCombatInputHandler : MonoBehaviour
{
    private PlayerInputReceiver inputReceiver;
    private CombatActionManager combatActionManager;
    private PlayerAnimationController animationController;
    private PlayerLockOnController lockOnController;

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

        initialized = true;

        BindInput();
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

    public void SetNormalAttackActionId(string newNormalAttackActionId)
    {
        if (string.IsNullOrEmpty(newNormalAttackActionId))
        {
            Debug.LogWarning("[PlayerCombatInputHandler] New normalAttackActionId is empty.", this);
            return;
        }

        normalAttackActionId = newNormalAttackActionId;

        Debug.Log(
            $"[PlayerCombatInputHandler] Normal attack action id changed to: {normalAttackActionId}",
            this
        );
    }

    private void OnAttackPressed()
    {
        if (!GameModeManager.Instance.CurrentCapabilities.canAttack)
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
