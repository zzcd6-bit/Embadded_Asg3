using System.Collections.Generic;
using UnityEngine;

public class PlayerSaveController : MonoBehaviour, IPlayerSaveable
{
    [Header("组件")]
    public PlayerDamageReceiver damageReceiver;
    public PlayerBrushSkillInventory skillInventory;
    public PlayerInkPouchController inkPouchController;
    public PlayerCharacterStatsController characterStatsController;

    [Header("位移组件")]
    public Transform playerRoot;
    public CharacterController characterController;
    public Rigidbody playerRigidbody;

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

        if (damageReceiver == null)
        {
            damageReceiver = GetComponent<PlayerDamageReceiver>();
        }

        if (damageReceiver == null)
        {
            damageReceiver = GetComponentInChildren<PlayerDamageReceiver>();
        }

        if (skillInventory == null)
        {
            skillInventory = GetComponent<PlayerBrushSkillInventory>();
        }

        if (skillInventory == null)
        {
            skillInventory = GetComponentInChildren<PlayerBrushSkillInventory>();
        }

        if (inkPouchController == null)
        {
            inkPouchController = GetComponent<PlayerInkPouchController>();
        }

        if (inkPouchController == null)
        {
            inkPouchController = GetComponentInChildren<PlayerInkPouchController>();
        }

        if (characterController == null)
        {
            characterController = playerRoot.GetComponent<CharacterController>();
        }

        if (characterController == null)
        {
            characterController = playerRoot.GetComponentInChildren<CharacterController>();
        }

        if (playerRigidbody == null)
        {
            playerRigidbody = playerRoot.GetComponent<Rigidbody>();
        }

        if (playerRigidbody == null)
        {
            playerRigidbody = playerRoot.GetComponentInChildren<Rigidbody>();
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
                    PlayerCharacterStatsController>();
        }
    }

    public PlayerSaveData CapturePlayerSaveData()
    {
        PlayerSaveData saveData = new PlayerSaveData();

        Transform targetTransform = playerRoot != null ? playerRoot : transform;

        saveData.position = targetTransform.position;
        saveData.eulerAngles = targetTransform.eulerAngles;

        if (damageReceiver != null)
        {
            saveData.currentHp = damageReceiver.CurrentHp;
            saveData.maxHp = damageReceiver.MaxHp;
        }

        if (skillInventory != null)
        {
            List<BrushSkillType> unlockedSkills = skillInventory.GetUnlockedSkills();

            saveData.unlockedBrushSkills.Clear();

            for (int i = 0; i < unlockedSkills.Count; i++)
            {
                BrushSkillType skillType = unlockedSkills[i];

                if (skillType == BrushSkillType.None)
                    continue;

                saveData.unlockedBrushSkills.Add(skillType.ToString());
            }
        }

        if (inkPouchController != null)
        {
            saveData.currentInk = inkPouchController.CurrentInk;
            saveData.maxInk = inkPouchController.MaxInk;
        }

        if (characterStatsController != null)
        {
            saveData.characterLevel =
                characterStatsController.CurrentLevel;
        }

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerSaveController] Player data captured. " +
                $"HP={saveData.currentHp}/{saveData.maxHp}, " +
                $"Ink={saveData.currentInk}/{saveData.maxInk}, " +
                $"SkillCount={saveData.unlockedBrushSkills.Count}",
                this
            );
        }

        return saveData;
    }

    public void RestorePlayerSaveData(PlayerSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogWarning("[PlayerSaveController] Save data is null.", this);
            return;
        }

        TeleportPlayer(saveData.position, saveData.eulerAngles);

        if (characterStatsController != null)
        {
            characterStatsController.SetLevel(
                Mathf.Max(
                    1,
                    saveData.characterLevel
                ),
                false
            );
        }

        if (damageReceiver != null)
        {
            int resolvedMaxHp =
                characterStatsController != null &&
                characterStatsController.CombatStats != null
                    ? characterStatsController
                        .CombatStats
                        .MaxHp
                    : saveData.maxHp;

            damageReceiver.SetHp(
                saveData.currentHp,
                resolvedMaxHp
            );
        }

        if (skillInventory != null)
        {
            List<BrushSkillType> loadedSkills = new List<BrushSkillType>();

            if (saveData.unlockedBrushSkills != null)
            {
                for (int i = 0; i < saveData.unlockedBrushSkills.Count; i++)
                {
                    string skillName = saveData.unlockedBrushSkills[i];

                    if (System.Enum.TryParse(skillName, out BrushSkillType skillType))
                    {
                        if (skillType != BrushSkillType.None &&
                            !loadedSkills.Contains(skillType))
                        {
                            loadedSkills.Add(skillType);
                        }
                    }
                }
            }

            skillInventory.SetUnlockedSkills(loadedSkills);
        }

        if (inkPouchController != null)
        {
            inkPouchController.SetInk(
                saveData.currentInk,
                saveData.maxInk
            );
        }

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerSaveController] Player data restored. " +
                $"HP={saveData.currentHp}/{saveData.maxHp}, " +
                $"Ink={saveData.currentInk}/{saveData.maxInk}",
                this
            );
        }
    }

    private void TeleportPlayer(Vector3 position, Vector3 eulerAngles)
    {
        Transform targetTransform = playerRoot != null ? playerRoot : transform;

        bool hadCharacterController = characterController != null;
        bool characterControllerWasEnabled = false;

        if (hadCharacterController)
        {
            characterControllerWasEnabled = characterController.enabled;
            characterController.enabled = false;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.position = position;
            playerRigidbody.rotation = Quaternion.Euler(eulerAngles);
        }

        targetTransform.SetPositionAndRotation(
            position,
            Quaternion.Euler(eulerAngles)
        );

        Physics.SyncTransforms();

        if (hadCharacterController)
        {
            characterController.enabled = characterControllerWasEnabled;
        }

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerSaveController] Teleported player to {position}, rotation {eulerAngles}",
                this
            );
        }
    }
}