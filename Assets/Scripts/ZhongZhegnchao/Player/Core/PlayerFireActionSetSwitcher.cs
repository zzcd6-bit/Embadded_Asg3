using UnityEngine;

public class PlayerFireActionSetSwitcher : MonoBehaviour
{
    [Header("组件")]
    public PlayerElementInfusion elementInfusion;
    public ActionPlayer actionPlayer;
    public CombatActionManager combatActionManager;
    public PlayerCombatInputHandler combatInputHandler;

    [Header("动作配置")]
    public PlayerActionConfigSet normalActionSet;
    public PlayerActionConfigSet fireActionSet;

    [Header("设置")]
    public bool restoreNormalActionSetWhenFireEnd = true;

    [Header("Debug")]
    public bool debugLog = true;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (elementInfusion != null)
        {
            elementInfusion.FireInfusionStateChanged += OnFireInfusionStateChanged;
        }
    }

    private void OnDisable()
    {
        if (elementInfusion != null)
        {
            elementInfusion.FireInfusionStateChanged -= OnFireInfusionStateChanged;
        }
    }

    private void ResolveReferences()
    {
        if (elementInfusion == null)
        {
            elementInfusion = GetComponent<PlayerElementInfusion>();
        }

        if (elementInfusion == null)
        {
            elementInfusion = GetComponentInChildren<PlayerElementInfusion>();
        }

        if (actionPlayer == null)
        {
            actionPlayer = GetComponent<ActionPlayer>();
        }

        if (actionPlayer == null)
        {
            actionPlayer = GetComponentInChildren<ActionPlayer>();
        }

        if (combatActionManager == null)
        {
            combatActionManager = GetComponent<CombatActionManager>();
        }

        if (combatActionManager == null)
        {
            combatActionManager = GetComponentInChildren<CombatActionManager>();
        }

        if (combatInputHandler == null)
        {
            combatInputHandler = GetComponent<PlayerCombatInputHandler>();
        }

        if (combatInputHandler == null)
        {
            combatInputHandler = GetComponentInChildren<PlayerCombatInputHandler>();
        }
    }

    private void OnFireInfusionStateChanged(bool isFireInfused)
    {
        if (isFireInfused)
        {
            SwitchToFireActionSet();
        }
        else
        {
            if (restoreNormalActionSetWhenFireEnd)
            {
                SwitchToNormalActionSet();
            }
        }
    }

    private void SwitchToFireActionSet()
    {
        ApplyActionSet(fireActionSet, "Fire");
    }

    private void SwitchToNormalActionSet()
    {
        ApplyActionSet(normalActionSet, "Normal");
    }

    private void ApplyActionSet(PlayerActionConfigSet targetActionSet, string label)
    {
        if (targetActionSet == null)
        {
            Debug.LogWarning(
                $"[PlayerFireActionSetSwitcher] {label} Action Set is missing.",
                this
            );

            return;
        }

        if (actionPlayer != null)
        {
            actionPlayer.SetActionConfigSet(targetActionSet);
        }
        else
        {
            Debug.LogWarning("[PlayerFireActionSetSwitcher] ActionPlayer is missing.", this);
        }

        if (combatActionManager != null)
        {
            combatActionManager.SetActionConfigSet(targetActionSet);
        }
        else
        {
            Debug.LogWarning("[PlayerFireActionSetSwitcher] CombatActionManager is missing.", this);
        }

        if (combatInputHandler != null)
        {
            combatInputHandler.SetNormalAttackActionId(
                targetActionSet.normalAttackActionId
            );
        }
        else
        {
            Debug.LogWarning("[PlayerFireActionSetSwitcher] PlayerCombatInputHandler is missing.", this);
        }

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerFireActionSetSwitcher] Switched to {label} Action Set. " +
                $"Set={targetActionSet.name}, NormalAttack={targetActionSet.normalAttackActionId}",
                this
            );
        }
    }
}