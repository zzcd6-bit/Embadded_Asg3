using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerActionConfigSet",
    menuName = "Combat/Player Action Config Set"
)]
public class PlayerActionConfigSet : ScriptableObject
{
    [Header("默认攻击动作 ID")]
    public string normalAttackActionId = "Player_Attack_01";

    [Header("玩家动作配置列表")]
    public List<ActionConfig> actions = new List<ActionConfig>();

    public ActionConfig GetNormalAttack()
    {
        return GetAction(normalAttackActionId);
    }

    public ActionConfig GetAction(string actionId)
    {
        if (string.IsNullOrEmpty(actionId) || actions == null)
        {
            return null;
        }

        for (int i = 0; i < actions.Count; i++)
        {
            ActionConfig config = actions[i];

            if (config == null)
            {
                continue;
            }

            if (config.actionId == actionId || config.name == actionId)
            {
                return config;
            }
        }

        return null;
    }

    public ActionConfig[] GetAllActions()
    {
        if (actions == null)
        {
            return new ActionConfig[0];
        }

        return actions.ToArray();
    }
}