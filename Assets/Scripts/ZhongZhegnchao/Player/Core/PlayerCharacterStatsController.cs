using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCharacterStatsController :
    MonoBehaviour,
    IExperienceReceiver
{
    [Header("Config")]
    [SerializeField]
    private PlayerCharacterStatsConfig config;

    [Header("Runtime Level")]
    [SerializeField]
    [Min(1)]
    private int currentLevel = 1;

    [Header("Runtime Experience")]
    [SerializeField]
    [Min(0)]
    private int currentExperience = 0;

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

    public event Action<int, int> ExperienceChanged;

    public event Action<int> LevelChanged;

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

    public int CurrentExperience
    {
        get { return currentExperience; }
    }

    public int ExperienceToNextLevel
    {
        get
        {
            if (IsMaxLevel)
                return 0;

            if (config == null)
                return 0;

            return config
                .GetExperienceRequiredForLevel(
                    currentLevel
                );
        }
    }

    public bool IsMaxLevel
    {
        get
        {
            return currentLevel >= MaxLevel;
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

    private void OnEnable()
    {
        EnemyWhitebox.OnAnyEnemyDead +=
            OnAnyEnemyDead;
    }

    private void Start()
    {
        if (!IsInitialized)
        {
            Init();
        }
    }

    private void OnDisable()
    {
        EnemyWhitebox.OnAnyEnemyDead -=
            OnAnyEnemyDead;
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

        ClampExperience();

        IsInitialized = true;

        ApplyLevelStats(false);

        NotifyExperienceChanged();

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerCharacterStatsController] " +
                $"Initialized. " +
                $"Level={currentLevel}, " +
                $"EXP={currentExperience}/" +
                $"{ExperienceToNextLevel}",
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

    private void OnAnyEnemyDead(
        EnemyWhitebox enemy
    )
    {
        if (enemy == null)
            return;

        int experienceReward =
            enemy.ExperienceReward;

        if (experienceReward <= 0)
            return;

        AddExperience(experienceReward);
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0)
            return;

        if (!IsInitialized &&
            !Init())
        {
            return;
        }

        if (IsMaxLevel)
        {
            currentExperience = 0;

            StatsChanged?.Invoke();
            NotifyExperienceChanged();

            return;
        }

        currentExperience += amount;

        int previousLevel =
            currentLevel;

        while (!IsMaxLevel)
        {
            int requiredExperience =
                config
                    .GetExperienceRequiredForLevel(
                        currentLevel
                    );

            if (requiredExperience <= 0)
                break;

            if (currentExperience <
                requiredExperience)
            {
                break;
            }

            currentExperience -=
                requiredExperience;

            currentLevel++;
        }

        if (IsMaxLevel)
        {
            currentExperience = 0;
        }

        bool leveledUp =
            currentLevel != previousLevel;

        if (leveledUp)
        {
            ApplyLevelStats(true);

            LevelChanged?.Invoke(
                currentLevel
            );
        }
        else
        {
            StatsChanged?.Invoke();
        }

        NotifyExperienceChanged();

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerCharacterStatsController] " +
                $"Gain EXP={amount}. " +
                $"Level={currentLevel}, " +
                $"EXP={currentExperience}/" +
                $"{ExperienceToNextLevel}",
                this
            );
        }
    }

    public void SetExperience(
        int newExperience
    )
    {
        if (!IsInitialized &&
            !Init())
        {
            return;
        }

        currentExperience =
            Mathf.Max(
                0,
                newExperience
            );

        int previousLevel =
            currentLevel;

        while (!IsMaxLevel)
        {
            int requiredExperience =
                config
                    .GetExperienceRequiredForLevel(
                        currentLevel
                    );

            if (requiredExperience <= 0)
                break;

            if (currentExperience <
                requiredExperience)
            {
                break;
            }

            currentExperience -=
                requiredExperience;

            currentLevel++;
        }

        if (IsMaxLevel)
        {
            currentExperience = 0;
        }

        if (currentLevel != previousLevel)
        {
            ApplyLevelStats(false);

            LevelChanged?.Invoke(
                currentLevel
            );
        }
        else
        {
            StatsChanged?.Invoke();
        }

        NotifyExperienceChanged();
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

        ClampExperience();

        if (!IsInitialized)
        {
            Init();
            return;
        }

        ApplyLevelStats(
            addMaxHpGrowthToCurrentHp
        );

        LevelChanged?.Invoke(
            currentLevel
        );

        NotifyExperienceChanged();
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
                newMaxHp > oldMaxHp &&
                oldCurrentHp > 0)
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
            newMaxHp > oldMaxHp &&
            currentHp > 0)
        {
            currentHp +=
                newMaxHp - oldMaxHp;
        }

        damageReceiver.SetHp(
            currentHp,
            newMaxHp
        );
    }

    private void ClampExperience()
    {
        currentExperience =
            Mathf.Max(
                0,
                currentExperience
            );

        if (IsMaxLevel)
        {
            currentExperience = 0;
            return;
        }

        int requiredExperience =
            ExperienceToNextLevel;

        if (requiredExperience <= 0)
        {
            currentExperience = 0;
            return;
        }

        currentExperience =
            Mathf.Clamp(
                currentExperience,
                0,
                requiredExperience - 1
            );
    }

    private void NotifyExperienceChanged()
    {
        ExperienceChanged?.Invoke(
            currentExperience,
            ExperienceToNextLevel
        );
    }
}