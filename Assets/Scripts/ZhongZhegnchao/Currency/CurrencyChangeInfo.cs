using System;
using UnityEngine;

public enum CurrencyChangeType
{
    Gain,
    Spend,
    Set
}

public enum CurrencyChangeReason
{
    Unknown,

    Pickup,
    EnemyReward,
    QuestReward,

    ShopPurchase,
    Refund,

    NewGame,
    SaveRestore,
    Debug
}

[Serializable]
public struct CurrencyChangeInfo
{
    public CurrencyChangeType changeType;

    public CurrencyChangeReason reason;

    public int previousCoins;

    public int currentCoins;

    /// <summary>
    /// 正数代表获得，负数代表花费。
    /// </summary>
    public int delta;

    /// <summary>
    /// 触发金币变化的对象。
    /// 例如敌人、任务、商店。
    /// </summary>
    public UnityEngine.Object source;

    public CurrencyChangeInfo(
        CurrencyChangeType newChangeType,
        CurrencyChangeReason newReason,
        int newPreviousCoins,
        int newCurrentCoins,
        int newDelta,
        UnityEngine.Object newSource
    )
    {
        changeType = newChangeType;
        reason = newReason;

        previousCoins =
            newPreviousCoins;

        currentCoins =
            newCurrentCoins;

        delta =
            newDelta;

        source =
            newSource;
    }
}