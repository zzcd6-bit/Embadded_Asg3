using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSkillTreeController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private SkillTreeConfig config;

    [Header("References")]
    [SerializeField]
    private PlayerCharacterStatsController
        characterStatsController;

    [SerializeField]
    private PlayerBrushSkillInventory
        skillInventory;

    [Header("Runtime")]
    [SerializeField]
    private List<SkillTreeLevelSaveData>
        runtimeLevels =
            new List<SkillTreeLevelSaveData>();

    [SerializeField]
    private bool canResetSkills;

    [Header("Debug")]
    [SerializeField]
    private bool debugLog;

    public event Action SkillTreeChanged;

    public event Action<
        BrushSkillType,
        int> SkillLevelChanged;

    public event Action<
        int,
        int,
        int> SkillPointsChanged;

    public event Action<bool>
        ResetAvailabilityChanged;

    public SkillTreeConfig Config => config;

    public int TotalSkillPoints
    {
        get
        {
            int level =
                characterStatsController != null
                    ? characterStatsController.CurrentLevel
                    : 1;

            return Mathf.Max(0, level - 1);
        }
    }

    public int UsedSkillPoints
    {
        get
        {
            EnsureRuntimeEntries();

            int used = 0;

            for (int i = 0;
                 i < runtimeLevels.Count;
                 i++)
            {
                SkillTreeLevelSaveData record =
                    runtimeLevels[i];

                if (record == null)
                    continue;

                used += Mathf.Max(
                    0,
                    record.level - 1
                );
            }

            return used;
        }
    }

    public int AvailableSkillPoints =>
        Mathf.Max(
            0,
            TotalSkillPoints -
            UsedSkillPoints
        );

    public bool CanResetSkills =>
        canResetSkills;

    private void Awake()
    {
        ResolveReferences();
        EnsureRuntimeEntries();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (characterStatsController != null)
        {
            characterStatsController.LevelChanged +=
                HandleCharacterLevelChanged;
        }
    }

    private void Start()
    {
        SyncUnlockedSkills();
    }

    private void OnDisable()
    {
        if (characterStatsController != null)
        {
            characterStatsController.LevelChanged -=
                HandleCharacterLevelChanged;
        }
    }

    private void ResolveReferences()
    {
        if (characterStatsController == null)
        {
            characterStatsController =
                GetComponent<
                    PlayerCharacterStatsController>();
        }

        if (characterStatsController == null)
        {
            characterStatsController =
                GetComponentInChildren<
                    PlayerCharacterStatsController>(true);
        }

        if (skillInventory == null)
        {
            skillInventory =
                GetComponent<
                    PlayerBrushSkillInventory>();
        }

        if (skillInventory == null)
        {
            skillInventory =
                GetComponentInChildren<
                    PlayerBrushSkillInventory>(true);
        }
    }

    private void HandleCharacterLevelChanged(
        int newLevel
    )
    {
        ClampLevelsToPointBudget();
        NotifyAll();
    }

    public bool IsSkillUnlocked(
        BrushSkillType skillType
    )
    {
        if (skillInventory == null)
            return false;

        List<BrushSkillType> unlocked =
            skillInventory.GetUnlockedSkills();

        return unlocked != null &&
               unlocked.Contains(skillType);
    }

    public void SyncUnlockedSkills()
    {
        SyncUnlockedSkills(true);
    }

    public void SyncUnlockedSkills(bool notify)
    {
        ResolveReferences();
        EnsureRuntimeEntries();

        if (skillInventory == null ||
            config == null)
        {
            return;
        }

        List<BrushSkillType> unlocked =
            skillInventory.GetUnlockedSkills();

        if (unlocked == null)
            unlocked = new List<BrushSkillType>();

        bool changed = false;

        for (int i = 0;
             i < config.Skills.Count;
             i++)
        {
            SkillTreeSkillConfig skill =
                config.Skills[i];

            if (skill == null)
                continue;

            SkillTreeLevelSaveData record =
                FindRecord(skill.SkillType);

            if (record == null)
                continue;

            bool isUnlocked =
                unlocked.Contains(
                    skill.SkillType
                );

            int targetLevel =
                isUnlocked
                    ? Mathf.Max(1, record.level)
                    : 0;

            if (record.level == targetLevel)
                continue;

            record.level = targetLevel;
            changed = true;
        }

        if (!changed)
            return;

        ClampLevelsToPointBudget();

        if (notify)
            NotifyAll();
    }

    public int GetSkillLevel(
        BrushSkillType skillType
    )
    {
        SyncUnlockedSkills(false);

        SkillTreeLevelSaveData record =
            FindRecord(skillType);

        return record != null
            ? Mathf.Max(0, record.level)
            : 0;
    }

    public bool CanUpgradeSkill(
        BrushSkillType skillType
    )
    {
        SyncUnlockedSkills(false);

        return IsSkillUnlocked(skillType) &&
               AvailableSkillPoints > 0;
    }

    public bool UpgradeSkill(
        BrushSkillType skillType
    )
    {
        SyncUnlockedSkills(false);

        if (!CanUpgradeSkill(skillType))
            return false;

        SkillTreeLevelSaveData record =
            FindRecord(skillType);

        if (record == null)
            return false;

        record.level =
            Mathf.Max(1, record.level) + 1;

        SkillLevelChanged?.Invoke(
            skillType,
            record.level
        );

        NotifyAll();

        if (debugLog)
        {
            Debug.Log(
                "[PlayerSkillTreeController] " +
                $"Upgraded {skillType} " +
                $"to Lv.{record.level}. " +
                $"Available SP=" +
                $"{AvailableSkillPoints}",
                this
            );
        }

        return true;
    }

    public float GetStatValue(
        BrushSkillType skillType,
        SkillTreeStatType statType,
        float fallbackValue = 0f
    )
    {
        if (config == null)
            return fallbackValue;

        int level =
            GetSkillLevel(skillType);

        return config.GetStatValue(
            skillType,
            statType,
            level,
            fallbackValue
        );
    }

    public void GrantResetOpportunity()
    {
        if (canResetSkills)
            return;

        canResetSkills = true;

        ResetAvailabilityChanged?.Invoke(true);
        SkillTreeChanged?.Invoke();
    }

    public bool ResetAllocatedSkillPoints()
    {
        SyncUnlockedSkills(false);

        if (!canResetSkills)
            return false;

        int refundedPoints =
            UsedSkillPoints;

        for (int i = 0;
             i < runtimeLevels.Count;
             i++)
        {
            SkillTreeLevelSaveData record =
                runtimeLevels[i];

            if (record == null)
                continue;

            bool unlocked =
                TryParseSkillType(
                    record.skillType,
                    out BrushSkillType skillType
                ) &&
                IsSkillUnlocked(skillType);

            record.level =
                unlocked
                    ? 1
                    : 0;
        }

        canResetSkills = false;

        ResetAvailabilityChanged?.Invoke(false);
        NotifyAll();

        if (debugLog)
        {
            Debug.Log(
                "[PlayerSkillTreeController] " +
                $"Skills reset. " +
                $"Refunded SP={refundedPoints}.",
                this
            );
        }

        return true;
    }

    public PlayerSkillTreeSaveData CaptureSaveData()
    {
        SyncUnlockedSkills(false);

        PlayerSkillTreeSaveData saveData =
            new PlayerSkillTreeSaveData
            {
                hasData = true,
                canResetSkills =
                    canResetSkills
            };

        for (int i = 0;
             i < runtimeLevels.Count;
             i++)
        {
            SkillTreeLevelSaveData record =
                runtimeLevels[i];

            if (record == null)
                continue;

            if (!TryParseSkillType(
                    record.skillType,
                    out BrushSkillType skillType))
            {
                continue;
            }

            saveData.levels.Add(
                new SkillTreeLevelSaveData(
                    skillType,
                    Mathf.Max(0, record.level)
                )
            );
        }

        return saveData;
    }

    public void RestoreFromSaveData(
        PlayerSkillTreeSaveData saveData
    )
    {
        EnsureRuntimeEntries();

        for (int i = 0;
             i < runtimeLevels.Count;
             i++)
        {
            if (runtimeLevels[i] != null)
                runtimeLevels[i].level = 0;
        }

        if (saveData == null ||
            !saveData.hasData)
        {
            canResetSkills = false;

            SyncUnlockedSkills(false);
            ClampLevelsToPointBudget();
            NotifyAll();

            return;
        }

        canResetSkills =
            saveData.canResetSkills;

        if (saveData.levels != null)
        {
            for (int i = 0;
                 i < saveData.levels.Count;
                 i++)
            {
                SkillTreeLevelSaveData savedRecord =
                    saveData.levels[i];

                if (savedRecord == null ||
                    !TryParseSkillType(
                        savedRecord.skillType,
                        out BrushSkillType skillType))
                {
                    continue;
                }

                SkillTreeLevelSaveData runtimeRecord =
                    FindRecord(skillType);

                if (runtimeRecord == null)
                    continue;

                runtimeRecord.level =
                    Mathf.Max(0, savedRecord.level);
            }
        }

        /*
         * PlayerSaveController must restore
         * unlocked skills first, then call this.
         */
        SyncUnlockedSkills(false);
        ClampLevelsToPointBudget();

        NotifyAll();
    }

    private void EnsureRuntimeEntries()
    {
        if (config == null)
            return;

        for (int i = 0;
             i < config.Skills.Count;
             i++)
        {
            SkillTreeSkillConfig skill =
                config.Skills[i];

            if (skill == null)
                continue;

            if (FindRecord(skill.SkillType) != null)
                continue;

            runtimeLevels.Add(
                new SkillTreeLevelSaveData(
                    skill.SkillType,
                    0
                )
            );
        }
    }

    private SkillTreeLevelSaveData FindRecord(
        BrushSkillType skillType
    )
    {
        for (int i = 0;
             i < runtimeLevels.Count;
             i++)
        {
            SkillTreeLevelSaveData record =
                runtimeLevels[i];

            if (record == null)
                continue;

            if (!TryParseSkillType(
                    record.skillType,
                    out BrushSkillType parsedType))
            {
                continue;
            }

            if (parsedType == skillType)
                return record;
        }

        return null;
    }

    private void ClampLevelsToPointBudget()
    {
        EnsureRuntimeEntries();

        int excess =
            UsedSkillPoints -
            TotalSkillPoints;

        if (excess <= 0)
            return;

        for (int i =
                 runtimeLevels.Count - 1;
             i >= 0 && excess > 0;
             i--)
        {
            SkillTreeLevelSaveData record =
                runtimeLevels[i];

            if (record == null)
                continue;

            int removable =
                Mathf.Max(
                    0,
                    record.level - 1
                );

            if (removable <= 0)
                continue;

            int removeAmount =
                Mathf.Min(
                    removable,
                    excess
                );

            record.level -= removeAmount;
            excess -= removeAmount;
        }
    }

    private bool TryParseSkillType(
        string value,
        out BrushSkillType skillType
    )
    {
        return Enum.TryParse(
            value,
            true,
            out skillType
        );
    }

    private void NotifyAll()
    {
        SkillPointsChanged?.Invoke(
            TotalSkillPoints,
            UsedSkillPoints,
            AvailableSkillPoints
        );

        SkillTreeChanged?.Invoke();
    }
}