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

    [Header("Debug")]
    public bool debugLog = true;

    public PlayerInventoryController
    inventoryController;

    public PlayerEquipmentController
        equipmentController;

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

        if (damageReceiver == null)
        {
            damageReceiver =
                GetComponent<PlayerDamageReceiver>();
        }

        if (damageReceiver == null)
        {
            damageReceiver =
                GetComponentInChildren<
                    PlayerDamageReceiver>(true);
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
                playerRoot.GetComponent<Rigidbody>();
        }

        if (playerRigidbody == null &&
            playerRoot != null)
        {
            playerRigidbody =
                playerRoot.GetComponentInChildren<
                    Rigidbody>(true);
        }

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

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerSaveController] " +
                $"Player data captured. " +
                $"Level={saveData.characterLevel}, " +
                $"EXP={saveData.currentExperience}, " +
                $"HP={saveData.currentHp}/{saveData.maxHp}, " +
                $"Ink={saveData.currentInk}/{saveData.maxInk}, " +
                $"SkillCount=" +
                $"{saveData.unlockedBrushSkills.Count}",
                this
            );
        }

        if (inventoryController != null)
        {
            saveData.hasInventoryData = true;

            inventoryController.CaptureSaveData(
                saveData.inventoryItems
            );
        }
        else
        {
            saveData.hasInventoryData = false;
        }

        if (equipmentController != null)
        {
            equipmentController.CaptureSaveData(
                saveData
            );
        }

        saveData.collectedPickupIds.Clear();

        saveData.collectedPickupIds.AddRange(
            InventoryPickupPersistence
                .CaptureCollectedIds()
        );

        return saveData;
    }

    private void RestoreInventoryAndEquipment(
    PlayerSaveData saveData
)
    {
        if (saveData == null)
            return;

        // 旧存档没有背包字段时，
        // 保留 PlayerInventoryController 的 Starting Items。
        if (!saveData.hasInventoryData)
            return;

        if (inventoryController != null)
        {
            inventoryController.RestoreFromSaveData(
                saveData.inventoryItems
            );
        }

        if (equipmentController != null)
        {
            equipmentController.RestoreFromSaveData(
                saveData
            );
        }
    }

    private void CaptureCharacterStats(
        PlayerSaveData saveData
    )
    {
        if (saveData == null)
            return;

        if (characterStatsController == null)
            return;

        saveData.characterLevel =
            characterStatsController.CurrentLevel;

        saveData.currentExperience =
            characterStatsController.CurrentExperience;
    }

    private void CaptureHealth(
        PlayerSaveData saveData
    )
    {
        if (saveData == null)
            return;

        if (damageReceiver == null)
            return;

        saveData.currentHp =
            damageReceiver.CurrentHp;

        saveData.maxHp =
            damageReceiver.MaxHp;
    }

    private void CaptureSkills(
        PlayerSaveData saveData
    )
    {
        if (saveData == null)
            return;

        if (skillInventory == null)
            return;

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
        if (saveData == null)
            return;

        if (inkPouchController == null)
            return;

        saveData.currentInk =
            inkPouchController.CurrentInk;

        saveData.maxInk =
            inkPouchController.MaxInk;
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

        TeleportPlayer(
    saveData.position,
    saveData.eulerAngles
);

        RestoreCharacterStats(saveData);

        // 必须先恢复背包和装备。
        // 因为头盔可能增加 Max HP。
        RestoreInventoryAndEquipment(saveData);

        // 装备属性计算完成后再恢复当前生命值。
        RestoreHealth(saveData);

        RestoreSkills(saveData);
        RestoreInk(saveData);

        InventoryPickupPersistence.LoadCollectedIds(
            saveData.collectedPickupIds
        );
    }

    private void RestoreCharacterStats(
        PlayerSaveData saveData
    )
    {
        if (saveData == null)
            return;

        if (characterStatsController == null)
            return;

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

    private void RestoreHealth(
        PlayerSaveData saveData
    )
    {
        if (saveData == null)
            return;

        if (damageReceiver == null)
            return;

        int resolvedMaxHp =
            saveData.maxHp;

        if (characterStatsController != null &&
            characterStatsController.CombatStats != null)
        {
            resolvedMaxHp =
                characterStatsController
                    .CombatStats
                    .MaxHp;
        }

        damageReceiver.SetHp(
            saveData.currentHp,
            resolvedMaxHp
        );
    }

    private void RestoreSkills(
        PlayerSaveData saveData
    )
    {
        if (saveData == null)
            return;

        if (skillInventory == null)
            return;

        List<BrushSkillType> loadedSkills =
            new List<BrushSkillType>();

        if (saveData.unlockedBrushSkills != null)
        {
            for (int i = 0;
                 i < saveData.unlockedBrushSkills.Count;
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

                if (loadedSkills.Contains(skillType))
                    continue;

                loadedSkills.Add(skillType);
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
        if (saveData == null)
            return;

        if (inkPouchController == null)
            return;

        inkPouchController.SetInk(
            saveData.currentInk,
            saveData.maxInk
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
                Quaternion.Euler(eulerAngles);
        }

        targetTransform.SetPositionAndRotation(
            position,
            Quaternion.Euler(eulerAngles)
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
                $"[PlayerSaveController] " +
                $"Teleported player to {position}, " +
                $"rotation {eulerAngles}",
                this
            );
        }
    }
}