using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCharacterStatsController :
    MonoBehaviour
{
    [Header("Config")]
    [SerializeField]
    private PlayerCharacterStatsConfig config;

    [Header("Runtime Level")]
    [SerializeField]
    [Min(1)]
    private int currentLevel = 1;

    [Header("References")]
    [SerializeField]
    private CharacterCombatStats combatStats;

    [SerializeField]
    private PlayerDamageReceiver damageReceiver;

    [SerializeField]
    private PlayerInkPouchController inkPouchController;

    [Header("Debug")]
    [SerializeField]
    private bool debugLog = true;

    public event Action StatsChanged;

    public int CurrentLevel
    {
        get { return currentLevel; }
    }

    public int MaxLevel
    {
        get
        {
            return config != null
                ? Mathf.Max(1, config.maxLevel)
                : currentLevel;
        }
    }

    public CharacterCombatStats CombatStats
    {
        get { return combatStats; }
    }

    public PlayerDamageReceiver DamageReceiver
    {
        get { return damageReceiver; }
    }

    public PlayerInkPouchController InkPouchController
    {
        get { return inkPouchController; }
    }

    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        if (!IsInitialized)
        {
            Init();
        }
    }

    public bool Init(
        PlayerDamageReceiver newDamageReceiver = null
    )
    {
        if (IsInitialized)
            return true;

        if (newDamageReceiver != null)
        {
            damageReceiver =
                newDamageReceiver;
        }

        ResolveReferences();

        if (combatStats == null)
        {
            combatStats =
                gameObject.AddComponent<
                    CharacterCombatStats>();
        }

        if (config == null)
        {
            Debug.LogError(
                "[PlayerCharacterStatsController] " +
                "Config is missing.",
                this
            );

            return false;
        }

        if (damageReceiver == null)
        {
            Debug.LogWarning(
                "[PlayerCharacterStatsController] " +
                "PlayerDamageReceiver not found.",
                this
            );

            return false;
        }

        currentLevel =
            Mathf.Clamp(
                currentLevel,
                1,
                MaxLevel
            );

        ApplyLevelStats(false);

        IsInitialized = true;

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerCharacterStatsController] " +
                $"Initialized. Level={currentLevel}",
                this
            );
        }

        return true;
    }

    private void ResolveReferences()
    {
        if (combatStats == null)
        {
            combatStats =
                GetComponent<
                    CharacterCombatStats>();
        }

        if (combatStats == null)
        {
            combatStats =
                GetComponentInChildren<
                    CharacterCombatStats>(true);
        }

        if (damageReceiver == null)
        {
            damageReceiver =
                GetComponent<
                    PlayerDamageReceiver>();
        }

        if (damageReceiver == null)
        {
            damageReceiver =
                GetComponentInChildren<
                    PlayerDamageReceiver>(true);
        }

        if (inkPouchController == null)
        {
            inkPouchController =
                GetComponent<
                    PlayerInkPouchController>();
        }

        if (inkPouchController == null)
        {
            inkPouchController =
                GetComponentInChildren<
                    PlayerInkPouchController>(true);
        }
    }

    public bool LevelUp()
    {
        if (currentLevel >= MaxLevel)
            return false;

        SetLevel(
            currentLevel + 1,
            true
        );

        return true;
    }

    public void SetLevel(
        int newLevel,
        bool addMaxHpGrowthToCurrentHp = true
    )
    {
        if (config == null)
            return;

        currentLevel =
            Mathf.Clamp(
                newLevel,
                1,
                MaxLevel
            );

        if (!IsInitialized)
        {
            Init();
            return;
        }

        ApplyLevelStats(
            addMaxHpGrowthToCurrentHp
        );
    }

    public void RefreshStats()
    {
        if (!IsInitialized)
        {
            Init();
            return;
        }

        ApplyLevelStats(false);
    }

    public void SetEquipmentModifiers(
        CharacterStatModifiers modifiers
    )
    {
        if (combatStats == null)
            return;

        combatStats.SetEquipmentModifiers(
            modifiers
        );

        SyncMaxHp(false);

        StatsChanged?.Invoke();
    }

    private void ApplyLevelStats(
        bool addMaxHpGrowthToCurrentHp
    )
    {
        if (config == null ||
            combatStats == null)
        {
            return;
        }

        int oldMaxHp =
            damageReceiver != null
                ? damageReceiver.MaxHp
                : combatStats.MaxHp;

        int oldCurrentHp =
            damageReceiver != null
                ? damageReceiver.CurrentHp
                : oldMaxHp;

        CharacterBaseStats levelStats =
            config.GetStatsAtLevel(
                currentLevel
            );

        combatStats.SetBaseStats(
            levelStats
        );

        int newMaxHp =
            combatStats.MaxHp;

        if (damageReceiver != null)
        {
            int targetCurrentHp =
                oldCurrentHp;

            if (addMaxHpGrowthToCurrentHp &&
                newMaxHp > oldMaxHp)
            {
                targetCurrentHp +=
                    newMaxHp - oldMaxHp;
            }

            damageReceiver.SetHp(
                targetCurrentHp,
                newMaxHp
            );
        }

        StatsChanged?.Invoke();

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerCharacterStatsController] " +
                $"Stats applied. " +
                $"Level={currentLevel}, " +
                $"HP={newMaxHp}, " +
                $"ATK={combatStats.AttackPower}, " +
                $"DEF={combatStats.Defense}",
                this
            );
        }
    }

    private void SyncMaxHp(
        bool addMaxHpGrowthToCurrentHp
    )
    {
        if (combatStats == null ||
            damageReceiver == null)
        {
            return;
        }

        int oldMaxHp =
            damageReceiver.MaxHp;

        int currentHp =
            damageReceiver.CurrentHp;

        int newMaxHp =
            combatStats.MaxHp;

        if (addMaxHpGrowthToCurrentHp &&
            newMaxHp > oldMaxHp)
        {
            currentHp +=
                newMaxHp - oldMaxHp;
        }

        damageReceiver.SetHp(
            currentHp,
            newMaxHp
        );
    }
}