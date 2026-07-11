using System.Collections.Generic;
using UnityEngine;

public class CombatActionManager : MonoBehaviour
{
    [Header("动作列表")]
    [SerializeField] private List<ActionConfig> initialActions = new List<ActionConfig>();

    [Header("调试")]
    [SerializeField] private bool logCombo = false;

    private readonly Dictionary<string, ActionConfig> actionMap = new Dictionary<string, ActionConfig>();

    private ActionPlayer actionPlayer;

    private ActionConfig bufferedComboAction;
    private bool hasBufferedCombo;

    public bool IsPlaying
    {
        get
        {
            return actionPlayer != null && actionPlayer.IsPlaying;
        }
    }

    public void Init(ActionPlayer player, params ActionConfig[] extraConfigs)
    {
        actionPlayer = player;

        actionMap.Clear();

        for (int i = 0; i < initialActions.Count; i++)
        {
            RegisterAction(initialActions[i]);
        }

        if (extraConfigs != null)
        {
            for (int i = 0; i < extraConfigs.Length; i++)
            {
                RegisterAction(extraConfigs[i]);
            }
        }

        Debug.Log($"[CombatActionManager] Init complete. Action Count = {actionMap.Count}");
    }

    private void Update()
    {
        UpdateBufferedCombo();
    }

    public void SetActionConfigSet(PlayerActionConfigSet newActionSet)
    {
        if (newActionSet == null)
        {
            Debug.LogWarning("[CombatActionManager] SetActionConfigSet failed: newActionSet is null.", this);
            return;
        }

        actionMap.Clear();

        for (int i = 0; i < initialActions.Count; i++)
        {
            RegisterAction(initialActions[i]);
        }

        ActionConfig[] actions = newActionSet.GetAllActions();

        if (actions != null)
        {
            for (int i = 0; i < actions.Length; i++)
            {
                RegisterAction(actions[i]);
            }
        }

        ClearBufferedCombo();

        Debug.Log(
            $"[CombatActionManager] Action set switched. Count={actionMap.Count}, NormalAttack={newActionSet.normalAttackActionId}",
            this
        );
    }

    public void RegisterAction(ActionConfig config)
    {
        if (config == null)
        {
            return;
        }

        string id = GetActionId(config);

        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning($"[CombatActionManager] 注册动作失败：{config.name} 没有 actionId。");
            return;
        }

        actionMap[id] = config;

        if (config.nextComboAction != null)
        {
            RegisterAction(config.nextComboAction);
        }
    }

    public bool TryPlayAction(string actionId)
    {
        if (!actionMap.TryGetValue(actionId, out ActionConfig config))
        {
            Debug.LogWarning($"[CombatActionManager] 找不到动作：{actionId}");
            return false;
        }

        return TryPlayAction(config);
    }

    public bool TryPlayAction(ActionConfig config)
    {
        if (config == null)
        {
            Debug.LogWarning("[CombatActionManager] TryPlayAction 失败：ActionConfig 为空。");
            return false;
        }

        if (actionPlayer == null)
        {
            Debug.LogWarning("[CombatActionManager] TryPlayAction 失败：ActionPlayer 为空。");
            return false;
        }

        RegisterAction(config);

        if (!actionPlayer.IsPlaying)
        {
            ClearBufferedCombo();
            actionPlayer.PlayAction(config);
            return true;
        }

        return TryBufferComboInput();
    }

    private bool TryBufferComboInput()
    {
        ActionConfig currentAction = actionPlayer.CurrentAction;

        if (currentAction == null)
        {
            return false;
        }

        if (!currentAction.enableCombo)
        {
            if (logCombo)
            {
                Debug.Log("[CombatActionManager] 当前动作不允许连招。");
            }

            return false;
        }

        if (currentAction.nextComboAction == null)
        {
            if (logCombo)
            {
                Debug.Log("[CombatActionManager] 当前动作没有配置下一段连招。");
            }

            return false;
        }

        float time = actionPlayer.CurrentTime;

        bool inInputWindow =
            time >= currentAction.comboInputStartTime &&
            time <= currentAction.comboInputEndTime;

        if (!inInputWindow)
        {
            if (logCombo)
            {
                Debug.Log(
                    $"[CombatActionManager] 不在连招输入窗口内。time={time:F3}, window={currentAction.comboInputStartTime:F3}-{currentAction.comboInputEndTime:F3}"
                );
            }

            return false;
        }

        bufferedComboAction = currentAction.nextComboAction;
        hasBufferedCombo = true;

        if (logCombo)
        {
            Debug.Log($"[CombatActionManager] 已缓存连招：{bufferedComboAction.name}");
        }

        return true;
    }

    private void UpdateBufferedCombo()
    {
        if (!hasBufferedCombo)
        {
            return;
        }

        if (actionPlayer == null || !actionPlayer.IsPlaying)
        {
            ClearBufferedCombo();
            return;
        }

        ActionConfig currentAction = actionPlayer.CurrentAction;

        if (currentAction == null)
        {
            ClearBufferedCombo();
            return;
        }

        if (actionPlayer.CurrentTime < currentAction.comboCancelTime)
        {
            return;
        }

        ActionConfig nextAction = bufferedComboAction;

        ClearBufferedCombo();

        if (nextAction == null)
        {
            return;
        }

        if (logCombo)
        {
            Debug.Log($"[CombatActionManager] 执行连招切换：{nextAction.name}");
        }

        actionPlayer.PlayAction(nextAction, true);
    }

    private void ClearBufferedCombo()
    {
        bufferedComboAction = null;
        hasBufferedCombo = false;
    }

    public ActionConfig GetAction(string actionId)
    {
        if (actionMap.TryGetValue(actionId, out ActionConfig config))
        {
            return config;
        }

        return null;
    }

    private string GetActionId(ActionConfig config)
    {
        if (config == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(config.actionId))
        {
            return config.actionId;
        }

        return config.name;
    }
}