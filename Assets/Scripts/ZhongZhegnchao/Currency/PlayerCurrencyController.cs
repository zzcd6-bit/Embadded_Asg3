using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCurrencyController :
    MonoBehaviour,
    ICurrencyWallet
{
    [Header("Currency Settings")]
    [SerializeField]
    [Min(0)]
    private int startingCoins;

    [SerializeField]
    [Min(1)]
    private int maximumCoins = 999999;

    [Header("Runtime")]
    [SerializeField]
    [Min(0)]
    private int currentCoins;

    [Header("Debug")]
    [SerializeField]
    private bool debugLog = true;

    [SerializeField]
    [Min(1)]
    private int debugAmount = 100;

    /// <summary>
    /// 参数：改变后的金币总数。
    /// HUD 金币数量可以监听这个事件。
    /// </summary>
    public event Action<int> CoinsChanged;

    /// <summary>
    /// 参数：
    /// 1. 实际获得的金币数量
    /// 2. 当前金币总数
    ///
    /// 后续“获得物品提示 UI”监听这个事件。
    /// </summary>
    public event Action<int, int> CoinsGained;

    /// <summary>
    /// 参数：
    /// 1. 实际花费的金币数量
    /// 2. 当前金币总数
    /// </summary>
    public event Action<int, int> CoinsSpent;

    /// <summary>
    /// 完整金币变化信息。
    /// 可用于任务、统计、日志等系统。
    /// </summary>
    public event Action<CurrencyChangeInfo>
        CurrencyChanged;

    public int CurrentCoins
    {
        get { return currentCoins; }
    }

    public int MaximumCoins
    {
        get { return maximumCoins; }
    }

    public int StartingCoins
    {
        get { return startingCoins; }
    }

    private void Awake()
    {
        maximumCoins =
            Mathf.Max(
                1,
                maximumCoins
            );

        currentCoins =
            Mathf.Clamp(
                startingCoins,
                0,
                maximumCoins
            );
    }

    /// <summary>
    /// 判断当前金币是否足够。
    /// 不会实际扣除金币。
    /// </summary>
    public bool CanAfford(int amount)
    {
        if (amount < 0)
            return false;

        return currentCoins >= amount;
    }

    /// <summary>
    /// 简单增加金币。
    /// 默认原因是 Unknown。
    /// </summary>
    public int AddCoins(int amount)
    {
        return AddCoins(
            amount,
            CurrencyChangeReason.Unknown,
            null
        );
    }

    /// <summary>
    /// 增加金币。
    ///
    /// 返回实际增加的金币数量。
    /// 达到金币上限时，实际增加量可能小于传入值。
    /// </summary>
    public int AddCoins(
        int amount,
        CurrencyChangeReason reason,
        UnityEngine.Object source
    )
    {
        if (amount <= 0)
        {
            return 0;
        }

        int previousCoins =
            currentCoins;

        long requestedCoins =
            (long)currentCoins +
            amount;

        long clampedCoins =
            Math.Min(
                requestedCoins,
                maximumCoins
            );

        currentCoins =
            (int)clampedCoins;

        int actualAdded =
            currentCoins -
            previousCoins;

        if (actualAdded <= 0)
        {
            return 0;
        }

        NotifyCurrencyChanged(
            CurrencyChangeType.Gain,
            reason,
            previousCoins,
            currentCoins,
            actualAdded,
            source
        );

        CoinsGained?.Invoke(
            actualAdded,
            currentCoins
        );

        if (debugLog)
        {
            Debug.Log(
                "[PlayerCurrencyController] " +
                $"Gained {actualAdded} coins. " +
                $"Reason={reason}, " +
                $"Balance={currentCoins}",
                this
            );
        }

        return actualAdded;
    }

    /// <summary>
    /// 简单花费金币。
    /// 默认原因是 Unknown。
    /// </summary>
    public bool TrySpendCoins(int amount)
    {
        return TrySpendCoins(
            amount,
            CurrencyChangeReason.Unknown,
            null
        );
    }

    /// <summary>
    /// 尝试花费金币。
    ///
    /// 金币不足时返回 false，
    /// 并且不会改变金币数量。
    /// </summary>
    public bool TrySpendCoins(
        int amount,
        CurrencyChangeReason reason,
        UnityEngine.Object source
    )
    {
        if (amount <= 0)
        {
            return false;
        }

        if (!CanAfford(amount))
        {
            if (debugLog)
            {
                Debug.LogWarning(
                    "[PlayerCurrencyController] " +
                    $"Not enough coins. " +
                    $"Required={amount}, " +
                    $"Current={currentCoins}",
                    this
                );
            }

            return false;
        }

        int previousCoins =
            currentCoins;

        currentCoins -=
            amount;

        NotifyCurrencyChanged(
            CurrencyChangeType.Spend,
            reason,
            previousCoins,
            currentCoins,
            -amount,
            source
        );

        CoinsSpent?.Invoke(
            amount,
            currentCoins
        );

        if (debugLog)
        {
            Debug.Log(
                "[PlayerCurrencyController] " +
                $"Spent {amount} coins. " +
                $"Reason={reason}, " +
                $"Balance={currentCoins}",
                this
            );
        }

        return true;
    }

    /// <summary>
    /// 读取存档时使用。
    ///
    /// 不触发 CoinsGained 或 CoinsSpent，
    /// 避免加载存档时出现“获得金币”提示。
    /// </summary>
    public void RestoreCoins(int savedCoins)
    {
        SetCoinsInternal(
            savedCoins,
            CurrencyChangeReason.SaveRestore,
            null,
            false
        );
    }

    /// <summary>
    /// 开始新游戏时使用。
    /// </summary>
    public void ResetToStartingCoins()
    {
        SetCoinsInternal(
            startingCoins,
            CurrencyChangeReason.NewGame,
            null,
            false
        );
    }

    /// <summary>
    /// 强制设置金币。
    ///
    /// 主要提供给特殊剧情、作弊工具或管理系统。
    /// 正常奖励使用 AddCoins，
    /// 正常购买使用 TrySpendCoins。
    /// </summary>
    public void SetCoins(
        int newCoins,
        CurrencyChangeReason reason =
            CurrencyChangeReason.Unknown,
        UnityEngine.Object source = null
    )
    {
        SetCoinsInternal(
            newCoins,
            reason,
            source,
            true
        );
    }

    /// <summary>
    /// 强制通知当前金币数量。
    /// UI 初始化时可以调用。
    /// </summary>
    public void NotifyCurrentCoins()
    {
        CoinsChanged?.Invoke(
            currentCoins
        );
    }

    private void SetCoinsInternal(
        int newCoins,
        CurrencyChangeReason reason,
        UnityEngine.Object source,
        bool notifyGainOrSpend
    )
    {
        int previousCoins =
            currentCoins;

        currentCoins =
            Mathf.Clamp(
                newCoins,
                0,
                maximumCoins
            );

        int difference =
            currentCoins -
            previousCoins;

        if (difference == 0)
        {
            CoinsChanged?.Invoke(
                currentCoins
            );

            return;
        }

        CurrencyChangeType changeType;

        if (difference > 0)
        {
            changeType =
                CurrencyChangeType.Gain;
        }
        else
        {
            changeType =
                CurrencyChangeType.Spend;
        }

        NotifyCurrencyChanged(
            CurrencyChangeType.Set,
            reason,
            previousCoins,
            currentCoins,
            difference,
            source
        );

        if (!notifyGainOrSpend)
            return;

        if (changeType ==
            CurrencyChangeType.Gain)
        {
            CoinsGained?.Invoke(
                difference,
                currentCoins
            );
        }
        else
        {
            CoinsSpent?.Invoke(
                Mathf.Abs(difference),
                currentCoins
            );
        }
    }

    private void NotifyCurrencyChanged(
        CurrencyChangeType changeType,
        CurrencyChangeReason reason,
        int previousCoins,
        int newCoins,
        int delta,
        UnityEngine.Object source
    )
    {
        CoinsChanged?.Invoke(
            newCoins
        );

        CurrencyChangeInfo changeInfo =
            new CurrencyChangeInfo(
                changeType,
                reason,
                previousCoins,
                newCoins,
                delta,
                source
            );

        CurrencyChanged?.Invoke(
            changeInfo
        );
    }

    [ContextMenu("Debug/Add Coins")]
    private void DebugAddCoins()
    {
        AddCoins(
            debugAmount,
            CurrencyChangeReason.Debug,
            this
        );
    }

    [ContextMenu("Debug/Spend Coins")]
    private void DebugSpendCoins()
    {
        TrySpendCoins(
            debugAmount,
            CurrencyChangeReason.Debug,
            this
        );
    }

    [ContextMenu("Debug/Reset Coins")]
    private void DebugResetCoins()
    {
        ResetToStartingCoins();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maximumCoins =
            Mathf.Max(
                1,
                maximumCoins
            );

        startingCoins =
            Mathf.Clamp(
                startingCoins,
                0,
                maximumCoins
            );

        currentCoins =
            Mathf.Clamp(
                currentCoins,
                0,
                maximumCoins
            );

        debugAmount =
            Mathf.Max(
                1,
                debugAmount
            );
    }
#endif
}