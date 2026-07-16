using System.Collections.Generic;
using UnityEngine;

public class PlayerSaveController :
    MonoBehaviour,
    IPlayerSaveable
{
    [Header("组件")]
    public PlayerDamageReceiver damageReceiver;

    public PlayerBrushSkillInventory skillInventory;

    public PlayerInkPouchController inkPouchController;

    public PlayerCharacterStatsController
        characterStatsController;

    [Header("位移组件")]
    public Transform playerRoot;

    public CharacterController characterController;

    public Rigidbody playerRigidbody;

    [Header("背包与装备")]
    public PlayerInventoryController
        inventoryController;

    public PlayerEquipmentController
        equipmentController;

    [Header("金币")]
    [SerializeField]
    private PlayerCurrencyController
        currencyController;

    [Header("Debug")]
    public bool debugLog = true;

    private void Awake()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (playerRoot == null)
        {
            playerRoot = transform;
        }

        ResolveDamageReceiver();
        ResolveSkillInventory();
        ResolveInkPouchController();
        ResolveCharacterStatsController();
        ResolveMovementComponents();
        ResolveInventoryController();
        ResolveEquipmentController();
        ResolveCurrencyController();
    }

    private void ResolveDamageReceiver()
    {
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
    }

    private void ResolveSkillInventory()
    {
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

    private void ResolveInkPouchController()
    {
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

    private void ResolveCharacterStatsController()
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
    }

    private void ResolveMovementComponents()
    {
        if (characterController == null &&
            playerRoot != null)
        {
            characterController =
                playerRoot.GetComponent<
                    CharacterController>();
        }

        if (characterController == null &&
            playerRoot != null)
        {
            characterController =
                playerRoot.GetComponentInChildren<
                    CharacterController>(true);
        }

        if (playerRigidbody == null &&
            playerRoot != null)
        {
            playerRigidbody =
                playerRoot.GetComponent<
                    Rigidbody>();
        }

        if (playerRigidbody == null &&
            playerRoot != null)
        {
            playerRigidbody =
                playerRoot.GetComponentInChildren<
                    Rigidbody>(true);
        }
    }

    private void ResolveInventoryController()
    {
        if (inventoryController == null)
        {
            inventoryController =
                GetComponent<
                    PlayerInventoryController>();
        }

        if (inventoryController == null)
        {
            inventoryController =
                GetComponentInChildren<
                    PlayerInventoryController>(true);
        }
    }

    private void ResolveEquipmentController()
    {
        if (equipmentController == null)
        {
            equipmentController =
                GetComponent<
                    PlayerEquipmentController>();
        }

        if (equipmentController == null)
        {
            equipmentController =
                GetComponentInChildren<
                    PlayerEquipmentController>(true);
        }
    }

    private void ResolveCurrencyController()
    {
        if (currencyController == null)
        {
            currencyController =
                GetComponent<
                    PlayerCurrencyController>();
        }

        if (currencyController == null)
        {
            currencyController =
                GetComponentInChildren<
                    PlayerCurrencyController>(true);
        }
    }

    public PlayerSaveData CapturePlayerSaveData()
    {
        ResolveReferences();

        PlayerSaveData saveData =
            new PlayerSaveData();

        Transform targetTransform =
            playerRoot != null
                ? playerRoot
                : transform;

        saveData.position =
            targetTransform.position;

        saveData.eulerAngles =
            targetTransform.eulerAngles;

        CaptureCharacterStats(saveData);
        CaptureHealth(saveData);
        CaptureSkills(saveData);
        CaptureInk(saveData);
        CaptureInventory(saveData);
        CaptureEquipment(saveData);
        CaptureCurrency(saveData);
        CaptureCollectedPickups(saveData);

        if (debugLog)
        {
            Debug.Log(
                "[PlayerSaveController] " +
                "Player data captured. " +
                $"Level={saveData.characterLevel}, " +
                $"EXP={saveData.currentExperience}, " +
                $"HP={saveData.currentHp}/" +
                $"{saveData.maxHp}, " +
                $"Ink={saveData.currentInk}/" +
                $"{saveData.maxInk}, " +
                $"Coins={saveData.currentCoins}, " +
                $"ItemCount=" +
                $"{saveData.inventoryItems.Count}, " +
                $"SkillCount=" +
                $"{saveData.unlockedBrushSkills.Count}, " +
                $"CollectedPickups=" +
                $"{saveData.collectedPickupIds.Count}",
                this
            );
        }

        return saveData;
    }

    private void CaptureCharacterStats(
        PlayerSaveData saveData
    )
    {
        if (saveData == null ||
            characterStatsController == null)
        {
            return;
        }

        saveData.characterLevel =
            characterStatsController.CurrentLevel;

        saveData.currentExperience =
            characterStatsController
                .CurrentExperience;
    }

    private void CaptureHealth(
        PlayerSaveData saveData
    )
    {
        if (saveData == null ||
            damageReceiver == null)
        {
            return;
        }

        saveData.currentHp =
            damageReceiver.CurrentHp;

        saveData.maxHp =
            damageReceiver.MaxHp;
    }

    private void CaptureSkills(
        PlayerSaveData saveData
    )
    {
        if (saveData == null ||
            skillInventory == null)
        {
            return;
        }

        List<BrushSkillType> unlockedSkills =
            skillInventory.GetUnlockedSkills();

        saveData.unlockedBrushSkills.Clear();

        for (int i = 0;
             i < unlockedSkills.Count;
             i++)
        {
            BrushSkillType skillType =
                unlockedSkills[i];

            if (skillType ==
                BrushSkillType.None)
            {
                continue;
            }

            saveData.unlockedBrushSkills.Add(
                skillType.ToString()
            );
        }
    }

    private void CaptureInk(
        PlayerSaveData saveData
    )
    {
        if (saveData == null ||
            inkPouchController == null)
        {
            return;
        }

        saveData.currentInk =
            inkPouchController.CurrentInk;

        saveData.maxInk =
            inkPouchController.MaxInk;
    }

    private void CaptureInventory(
        PlayerSaveData saveData
    )
    {
        if (saveData == null)
            return;

        if (inventoryController == null)
        {
            saveData.hasInventoryData =
                false;

            return;
        }

        saveData.hasInventoryData =
            true;

        inventoryController.CaptureSaveData(
            saveData.inventoryItems
        );
    }

    private void CaptureEquipment(
        PlayerSaveData saveData
    )
    {
        if (saveData == null ||
            equipmentController == null)
        {
            return;
        }

        equipmentController.CaptureSaveData(
            saveData
        );
    }

    private void CaptureCurrency(
        PlayerSaveData saveData
    )
    {
        if (saveData == null)
            return;

        if (currencyController == null)
        {
            saveData.hasCurrencyData =
                false;

            saveData.currentCoins = 0;

            return;
        }

        saveData.hasCurrencyData =
            true;

        saveData.currentCoins =
            currencyController.CurrentCoins;
    }

    private void CaptureCollectedPickups(
        PlayerSaveData saveData
    )
    {
        if (saveData == null)
            return;

        if (saveData.collectedPickupIds == null)
        {
            saveData.collectedPickupIds =
                new List<string>();
        }

        saveData.collectedPickupIds.Clear();

        saveData.collectedPickupIds.AddRange(
            InventoryPickupPersistence
                .CaptureCollectedIds()
        );
    }

    public void RestorePlayerSaveData(
        PlayerSaveData saveData
    )
    {
        if (saveData == null)
        {
            Debug.LogWarning(
                "[PlayerSaveController] " +
                "Save data is null.",
                this
            );

            return;
        }

        ResolveReferences();

        TeleportPlayer(
            saveData.position,
            saveData.eulerAngles
        );

        RestoreCharacterStats(saveData);

        /*
         * 必须先恢复背包和装备。
         * 因为头盔等装备可能增加最大生命值。
         */
        RestoreInventoryAndEquipment(
            saveData
        );

        /*
         * 装备属性计算完成以后，
         * 再恢复存档中的准确当前生命值。
         */
        RestoreHealth(saveData);

        RestoreSkills(saveData);
        RestoreInk(saveData);
        RestoreCurrency(saveData);
        RestoreCollectedPickups(saveData);

        if (debugLog)
        {
            Debug.Log(
                "[PlayerSaveController] " +
                "Player data restored. " +
                $"Level=" +
                $"{characterStatsController?.CurrentLevel ?? 0}, " +
                $"HP=" +
                $"{damageReceiver?.CurrentHp ?? 0}/" +
                $"{damageReceiver?.MaxHp ?? 0}, " +
                $"Coins=" +
                $"{currencyController?.CurrentCoins ?? 0}",
                this
            );
        }
    }

    private void RestoreCharacterStats(
        PlayerSaveData saveData
    )
    {
        if (saveData == null ||
            characterStatsController == null)
        {
            return;
        }

        characterStatsController.SetLevel(
            Mathf.Max(
                1,
                saveData.characterLevel
            ),
            false
        );

        characterStatsController.SetExperience(
            Mathf.Max(
                0,
                saveData.currentExperience
            )
        );
    }

    private void RestoreInventoryAndEquipment(
        PlayerSaveData saveData
    )
    {
        if (saveData == null)
            return;

        /*
         * 旧存档没有背包字段时，
         * 保留 PlayerInventoryController
         * 当前配置的 Starting Items。
         */
        if (!saveData.hasInventoryData)
            return;

        if (inventoryController != null)
        {
            inventoryController.RestoreFromSaveData(
                saveData.inventoryItems
            );
        }

        /*
         * 必须在背包恢复后再恢复装备。
         * 装备通过 Instance ID 查找背包中的装备实例。
         */
        if (equipmentController != null)
        {
            equipmentController.RestoreFromSaveData(
                saveData
            );
        }
    }

    private void RestoreHealth(
        PlayerSaveData saveData
    )
    {
        if (saveData == null ||
            damageReceiver == null)
        {
            return;
        }

        int resolvedMaxHp =
            saveData.maxHp;

        /*
         * 优先使用人物基础属性与装备属性
         * 重新计算出来的最终最大生命值。
         */
        if (characterStatsController != null &&
            characterStatsController.CombatStats !=
            null)
        {
            resolvedMaxHp =
                characterStatsController
                    .CombatStats
                    .MaxHp;
        }

        resolvedMaxHp =
            Mathf.Max(
                1,
                resolvedMaxHp
            );

        int resolvedCurrentHp =
            Mathf.Clamp(
                saveData.currentHp,
                0,
                resolvedMaxHp
            );

        damageReceiver.SetHp(
            resolvedCurrentHp,
            resolvedMaxHp
        );
    }

    private void RestoreSkills(
        PlayerSaveData saveData
    )
    {
        if (saveData == null ||
            skillInventory == null)
        {
            return;
        }

        List<BrushSkillType> loadedSkills =
            new List<BrushSkillType>();

        if (saveData.unlockedBrushSkills != null)
        {
            for (int i = 0;
                 i <
                 saveData.unlockedBrushSkills.Count;
                 i++)
            {
                string skillName =
                    saveData.unlockedBrushSkills[i];

                bool parsed =
                    System.Enum.TryParse(
                        skillName,
                        out BrushSkillType skillType
                    );

                if (!parsed)
                    continue;

                if (skillType ==
                    BrushSkillType.None)
                {
                    continue;
                }

                if (loadedSkills.Contains(
                        skillType))
                {
                    continue;
                }

                loadedSkills.Add(
                    skillType
                );
            }
        }

        skillInventory.SetUnlockedSkills(
            loadedSkills
        );
    }

    private void RestoreInk(
        PlayerSaveData saveData
    )
    {
        if (saveData == null ||
            inkPouchController == null)
        {
            return;
        }

        inkPouchController.SetInk(
            saveData.currentInk,
            saveData.maxInk
        );
    }

    private void RestoreCurrency(
        PlayerSaveData saveData
    )
    {
        if (saveData == null ||
            currencyController == null)
        {
            return;
        }

        if (saveData.hasCurrencyData)
        {
            /*
             * RestoreCoins 不会触发
             * CoinsGained 提示，
             * 避免加载存档时弹出“获得金币”。
             */
            currencyController.RestoreCoins(
                Mathf.Max(
                    0,
                    saveData.currentCoins
                )
            );
        }
        else
        {
            /*
             * 兼容旧存档：
             * 旧存档没有金币字段时，
             * 使用 PlayerCurrencyController
             * 配置的 Starting Coins。
             */
            currencyController
                .ResetToStartingCoins();
        }
    }

    private void RestoreCollectedPickups(
        PlayerSaveData saveData
    )
    {
        if (saveData == null)
            return;

        InventoryPickupPersistence.LoadCollectedIds(
            saveData.collectedPickupIds
        );
    }

    private void TeleportPlayer(
        Vector3 position,
        Vector3 eulerAngles
    )
    {
        Transform targetTransform =
            playerRoot != null
                ? playerRoot
                : transform;

        bool hadCharacterController =
            characterController != null;

        bool characterControllerWasEnabled =
            false;

        if (hadCharacterController)
        {
            characterControllerWasEnabled =
                characterController.enabled;

            characterController.enabled =
                false;
        }

        if (playerRigidbody != null)
        {
#if UNITY_6000_0_OR_NEWER
            playerRigidbody.linearVelocity =
                Vector3.zero;
#else
            playerRigidbody.velocity =
                Vector3.zero;
#endif

            playerRigidbody.angularVelocity =
                Vector3.zero;

            playerRigidbody.position =
                position;

            playerRigidbody.rotation =
                Quaternion.Euler(
                    eulerAngles
                );
        }

        targetTransform.SetPositionAndRotation(
            position,
            Quaternion.Euler(
                eulerAngles
            )
        );

        Physics.SyncTransforms();

        if (hadCharacterController)
        {
            characterController.enabled =
                characterControllerWasEnabled;
        }

        if (debugLog)
        {
            Debug.Log(
                "[PlayerSaveController] " +
                $"Teleported player to {position}, " +
                $"rotation {eulerAngles}",
                this
            );
        }
    }
}