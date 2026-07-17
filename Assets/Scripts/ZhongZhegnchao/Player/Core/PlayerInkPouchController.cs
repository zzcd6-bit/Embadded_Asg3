using UnityEngine;

public class PlayerInkPouchController : MonoBehaviour, IBrushSkillCostReceiver
{
    [Header("配置")]
    public PlayerInkPouchConfig config;

    [Header("当前墨囊")]
    [SerializeField] private int currentInk;
    [SerializeField] private int maxInk;

    [Header("Debug")]
    public bool debugLog = true;

    public int CurrentInk
    {
        get { return currentInk; }
    }

    public int MaxInk
    {
        get { return maxInk; }
    }

    private void Awake()
    {
        InitFromConfig();
    }

    private void OnEnable()
    {
        EnemyWhitebox.OnAnyEnemyDead += OnEnemyDead;
    }

    private void OnDisable()
    {
        EnemyWhitebox.OnAnyEnemyDead -= OnEnemyDead;
    }

    public void InitFromConfig()
    {
        if (config == null)
        {
            maxInk = 10;
            currentInk = maxInk;
            return;
        }

        maxInk = Mathf.Max(1, config.maxInk);
        currentInk = Mathf.Clamp(config.startInk, 0, maxInk);
    }

    public bool HasEnoughInk(int cost)
    {
        cost = Mathf.Max(0, cost);
        return currentInk >= cost;
    }

    public bool TryConsumeInk(int cost)
    {
        cost = Mathf.Max(0, cost);

        if (cost <= 0)
            return true;

        if (currentInk < cost)
        {
            if (debugLog)
            {
                Debug.Log(
                    $"[PlayerInkPouch] Not enough ink. Current={currentInk}, Cost={cost}",
                    this
                );
            }

            return false;
        }

        currentInk -= cost;

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerInkPouch] Ink consumed. Cost={cost}, Current={currentInk}/{maxInk}",
                this
            );
        }

        return true;
    }

    public void RestoreInk(int amount)
    {
        if (!GameModeManager.Instance.CurrentCapabilities.canRecoverInk)
        {
            return;
        }

        amount = Mathf.Max(0, amount);

        if (amount <= 0)
            return;

        int oldInk = currentInk;

        currentInk = Mathf.Clamp(
            currentInk + amount,
            0,
            maxInk
        );

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerInkPouch] Ink restored. +{amount}, {oldInk} -> {currentInk}/{maxInk}",
                this
            );
        }
    }

    public void SetInk(int newCurrentInk, int newMaxInk)
    {
        maxInk = Mathf.Max(1, newMaxInk);
        currentInk = Mathf.Clamp(newCurrentInk, 0, maxInk);
    }

    private void OnEnemyDead(EnemyWhitebox enemy)
    {
        if (enemy == null)
            return;

        int restoreAmount = config != null
            ? config.restoreInkOnEnemyKill
            : 2;

        RestoreInk(restoreAmount);
    }
}