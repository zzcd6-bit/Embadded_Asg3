using System;
using UnityEngine;

public interface ICurrencyWallet
{
    int CurrentCoins { get; }

    int MaximumCoins { get; }

    /// <summary>
    /// 只要金币总数发生改变就会触发。
    /// 参数为改变后的金币数量。
    /// </summary>
    event Action<int> CoinsChanged;

    /// <summary>
    /// 获得金币时触发。
    /// 第一个参数：实际获得数量。
    /// 第二个参数：获得后的金币总数。
    /// </summary>
    event Action<int, int> CoinsGained;

    /// <summary>
    /// 花费金币时触发。
    /// 第一个参数：实际花费数量。
    /// 第二个参数：花费后的金币总数。
    /// </summary>
    event Action<int, int> CoinsSpent;

    /// <summary>
    /// 包含金币变化原因和来源的完整事件。
    /// </summary>
    event Action<CurrencyChangeInfo>
        CurrencyChanged;

    bool CanAfford(int amount);

    int AddCoins(
        int amount,
        CurrencyChangeReason reason,
        UnityEngine.Object source
    );

    bool TrySpendCoins(
        int amount,
        CurrencyChangeReason reason,
        UnityEngine.Object source
    );
}