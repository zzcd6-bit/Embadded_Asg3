using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RewardNotificationController :
    MonoBehaviour
{
    private sealed class ActiveRewardLine
    {
        public RewardNotificationEntryView view;
        public int quantity;
    }

    [Header("UI References")]
    [SerializeField]
    private GameObject panelRoot;

    [SerializeField]
    private CanvasGroup panelCanvasGroup;

    [SerializeField]
    private Transform contentRoot;

    [SerializeField]
    private RewardNotificationEntryView
        entryPrefab;

    [Header("Currency Display")]
    [SerializeField]
    private Sprite coinIcon;

    [SerializeField]
    private string coinDisplayName = "金币";

    [Header("Timing")]
    [SerializeField]
    [Min(0.1f)]
    private float displayDuration = 2.5f;

    [SerializeField]
    [Min(0f)]
    private float fadeOutDuration = 0.25f;

    [Header("Sources")]
    [SerializeField]
    private PlayerInventoryController
        inventoryController;

    [SerializeField]
    private PlayerCurrencyController
        currencyController;

    [SerializeField]
    private bool autoFindSources = true;

    [Header("Debug")]
    [SerializeField]
    private bool debugLog;

    /*
     * 记录当前背包状态。
     * 用于判断 InventoryChanged 后，
     * 哪些物品的数量真正增加了。
     */
    private readonly Dictionary<string, int>
        inventorySnapshot =
            new Dictionary<string, int>();

    /*
     * 当前通知面板中已经显示的奖励。
     * 相同奖励会合并数量。
     */
    private readonly Dictionary<
        string,
        ActiveRewardLine>
        activeRewardLines =
            new Dictionary<
                string,
                ActiveRewardLine>();

    private Coroutine displayCoroutine;

    private bool inventorySubscribed;
    private bool currencySubscribed;

    private bool inventorySnapshotReady;
    private bool notificationSessionActive;

    private void Awake()
    {
        ResolveUIReferences();

        HidePanelImmediately();
    }

    private void OnEnable()
    {
        ResolveSources();
        SubscribeSources();
    }

    private void Start()
    {
        /*
         * 某些情况下 Player 可能比 UI 晚生成。
         */
        if (inventoryController == null ||
            currencyController == null)
        {
            StartCoroutine(
                ResolveSourcesRoutine()
            );
        }
    }

    private void OnDisable()
    {
        UnsubscribeSources();

        if (displayCoroutine != null)
        {
            StopCoroutine(
                displayCoroutine
            );

            displayCoroutine = null;
        }
    }

    private void ResolveUIReferences()
    {
        if (panelCanvasGroup == null &&
            panelRoot != null)
        {
            panelCanvasGroup =
                panelRoot.GetComponent<
                    CanvasGroup>();
        }
    }

    private void ResolveSources()
    {
        if (!autoFindSources)
            return;

        if (inventoryController == null)
        {
            inventoryController =
                FindFirstObjectByType<
                    PlayerInventoryController>();
        }

        if (currencyController == null)
        {
            currencyController =
                FindFirstObjectByType<
                    PlayerCurrencyController>();
        }
    }

    private IEnumerator ResolveSourcesRoutine()
    {
        while (isActiveAndEnabled)
        {
            ResolveSources();
            SubscribeSources();

            if (inventoryController != null &&
                currencyController != null)
            {
                yield break;
            }

            yield return
                new WaitForSecondsRealtime(
                    0.5f
                );
        }
    }

    private void SubscribeSources()
    {
        if (inventoryController != null &&
            !inventorySubscribed)
        {
            /*
             * 先记录当前已有物品，
             * 避免游戏开局时把 Starting Items
             * 当成刚刚获得的奖励。
             */
            CaptureInventorySnapshot();

            inventoryController.InventoryChanged +=
                HandleInventoryChanged;

            inventorySubscribed = true;
        }

        if (currencyController != null &&
            !currencySubscribed)
        {
            currencyController.CoinsGained +=
                HandleCoinsGained;

            currencySubscribed = true;
        }
    }

    private void UnsubscribeSources()
    {
        if (inventoryController != null &&
            inventorySubscribed)
        {
            inventoryController.InventoryChanged -=
                HandleInventoryChanged;
        }

        if (currencyController != null &&
            currencySubscribed)
        {
            currencyController.CoinsGained -=
                HandleCoinsGained;
        }

        inventorySubscribed = false;
        currencySubscribed = false;
    }

    public void Bind(
        PlayerInventoryController newInventory,
        PlayerCurrencyController newCurrency
    )
    {
        UnsubscribeSources();

        inventoryController =
            newInventory;

        currencyController =
            newCurrency;

        SubscribeSources();
    }

    private void HandleInventoryChanged()
    {
        if (inventoryController == null)
            return;

        if (!inventorySnapshotReady)
        {
            CaptureInventorySnapshot();
            return;
        }

        IReadOnlyList<InventoryItemEntry>
            currentItems =
                inventoryController.Items;

        Dictionary<ItemData, int>
            gainedItems =
                new Dictionary<ItemData, int>();

        Dictionary<string, int>
            newSnapshot =
                new Dictionary<string, int>();

        for (int i = 0;
             i < currentItems.Count;
             i++)
        {
            InventoryItemEntry entry =
                currentItems[i];

            if (entry == null ||
                entry.ItemData == null)
            {
                continue;
            }

            string entryKey =
                GetInventoryEntryKey(
                    entry,
                    i
                );

            int currentQuantity =
                Mathf.Max(
                    0,
                    entry.Quantity
                );

            newSnapshot[entryKey] =
                currentQuantity;

            int previousQuantity = 0;

            inventorySnapshot.TryGetValue(
                entryKey,
                out previousQuantity
            );

            int gainedQuantity =
                currentQuantity -
                previousQuantity;

            if (gainedQuantity <= 0)
                continue;

            if (!gainedItems.ContainsKey(
                    entry.ItemData))
            {
                gainedItems.Add(
                    entry.ItemData,
                    0
                );
            }

            gainedItems[entry.ItemData] +=
                gainedQuantity;
        }

        inventorySnapshot.Clear();

        foreach (
            KeyValuePair<string, int> pair
            in newSnapshot)
        {
            inventorySnapshot.Add(
                pair.Key,
                pair.Value
            );
        }

        foreach (
            KeyValuePair<ItemData, int> pair
            in gainedItems)
        {
            ShowItemReward(
                pair.Key,
                pair.Value
            );
        }
    }

    private void HandleCoinsGained(
        int gainedAmount,
        int currentBalance
    )
    {
        if (gainedAmount <= 0)
            return;

        ShowReward(
            "currency_coins",
            coinIcon,
            coinDisplayName,
            gainedAmount
        );
    }

    public void ShowItemReward(
        ItemData itemData,
        int quantity
    )
    {
        if (itemData == null ||
            quantity <= 0)
        {
            return;
        }

        string itemKey =
            !string.IsNullOrWhiteSpace(
                itemData.ItemId
            )
                ? itemData.ItemId
                : itemData.GetInstanceID()
                    .ToString();

        ShowReward(
            $"item_{itemKey}",
            itemData.Icon,
            itemData.DisplayName,
            quantity
        );
    }

    public void ShowCoinReward(int quantity)
    {
        if (quantity <= 0)
            return;

        ShowReward(
            "currency_coins",
            coinIcon,
            coinDisplayName,
            quantity
        );
    }

    public void ShowReward(
        string rewardKey,
        Sprite icon,
        string displayName,
        int quantity
    )
    {
        if (quantity <= 0 ||
            entryPrefab == null ||
            contentRoot == null ||
            panelRoot == null)
        {
            return;
        }

        if (!notificationSessionActive)
        {
            BeginNotificationSession();
        }

        if (activeRewardLines.TryGetValue(
                rewardKey,
                out ActiveRewardLine line))
        {
            line.quantity +=
                quantity;

            if (line.view != null)
            {
                line.view.SetQuantity(
                    line.quantity
                );
            }
        }
        else
        {
            RewardNotificationEntryView
                newView =
                    Instantiate(
                        entryPrefab,
                        contentRoot,
                        false
                    );

            newView.gameObject.SetActive(true);

            newView.Bind(
                icon,
                displayName,
                quantity
            );

            ActiveRewardLine newLine =
                new ActiveRewardLine
                {
                    view = newView,
                    quantity = quantity
                };

            activeRewardLines.Add(
                rewardKey,
                newLine
            );
        }

        RestartDisplayTimer();

        if (debugLog)
        {
            Debug.Log(
                "[RewardNotificationController] " +
                $"Show reward: " +
                $"{displayName} x{quantity}",
                this
            );
        }
    }

    private void BeginNotificationSession()
    {
        ClearRewardLines();

        notificationSessionActive =
            true;

        panelRoot.SetActive(true);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }
    }

    private void RestartDisplayTimer()
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(
                displayCoroutine
            );
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
        }

        displayCoroutine =
            StartCoroutine(
                DisplayRoutine()
            );
    }

    private IEnumerator DisplayRoutine()
    {
        yield return
            new WaitForSecondsRealtime(
                displayDuration
            );

        if (panelCanvasGroup != null &&
            fadeOutDuration > 0f)
        {
            float elapsed = 0f;

            while (elapsed <
                   fadeOutDuration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsed /
                        fadeOutDuration
                    );

                panelCanvasGroup.alpha =
                    1f - progress;

                yield return null;
            }
        }

        EndNotificationSession();

        displayCoroutine = null;
    }

    private void EndNotificationSession()
    {
        notificationSessionActive =
            false;

        HidePanelImmediately();
        ClearRewardLines();
    }

    private void HidePanelImmediately()
    {
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void ClearRewardLines()
    {
        foreach (
            KeyValuePair<
                string,
                ActiveRewardLine> pair
            in activeRewardLines)
        {
            if (pair.Value != null &&
                pair.Value.view != null)
            {
                Destroy(
                    pair.Value.view.gameObject
                );
            }
        }

        activeRewardLines.Clear();
    }

    public void ResetInventorySnapshot()
    {
        CaptureInventorySnapshot();
    }

    private void CaptureInventorySnapshot()
    {
        inventorySnapshot.Clear();

        if (inventoryController == null)
        {
            inventorySnapshotReady =
                false;

            return;
        }

        IReadOnlyList<InventoryItemEntry>
            items =
                inventoryController.Items;

        for (int i = 0;
             i < items.Count;
             i++)
        {
            InventoryItemEntry entry =
                items[i];

            if (entry == null ||
                entry.ItemData == null)
            {
                continue;
            }

            string entryKey =
                GetInventoryEntryKey(
                    entry,
                    i
                );

            inventorySnapshot[entryKey] =
                Mathf.Max(
                    0,
                    entry.Quantity
                );
        }

        inventorySnapshotReady = true;
    }

    private string GetInventoryEntryKey(
        InventoryItemEntry entry,
        int index
    )
    {
        if (entry == null)
        {
            return $"null_{index}";
        }

        if (!string.IsNullOrWhiteSpace(
                entry.InstanceId))
        {
            return entry.InstanceId;
        }

        string itemId =
            entry.ItemData != null
                ? entry.ItemData.ItemId
                : "NoItem";

        return $"{itemId}_{index}";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        displayDuration =
            Mathf.Max(
                0.1f,
                displayDuration
            );

        fadeOutDuration =
            Mathf.Max(
                0f,
                fadeOutDuration
            );
    }
#endif
}