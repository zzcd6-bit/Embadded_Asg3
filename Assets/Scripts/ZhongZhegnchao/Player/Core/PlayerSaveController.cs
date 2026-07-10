using System.Collections.Generic;
using UnityEngine;

public class PlayerSaveController : MonoBehaviour, IPlayerSaveable
{
    [Header("组件")]
    public PlayerDamageReceiver damageReceiver;
    public PlayerBrushSkillInventory skillInventory;

    [Header("位移组件")]
    public Transform playerRoot;
    public CharacterController characterController;
    public Rigidbody playerRigidbody;

    [Header("Debug")]
    public bool debugLog = true;

    private void Awake()
    {
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

        if (playerRoot == null)
        {
            playerRoot = transform;
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

            for (int i = 0; i < unlockedSkills.Count; i++)
            {
                saveData.unlockedBrushSkills.Add(unlockedSkills[i].ToString());
            }
        }

        if (debugLog)
        {
            Debug.Log("[PlayerSaveController] Player data captured.", this);
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

        if (damageReceiver != null)
        {
            damageReceiver.SetHp(saveData.currentHp, saveData.maxHp);
        }

        if (skillInventory != null)
        {
            List<BrushSkillType> loadedSkills = new List<BrushSkillType>();

            for (int i = 0; i < saveData.unlockedBrushSkills.Count; i++)
            {
                string skillName = saveData.unlockedBrushSkills[i];

                if (System.Enum.TryParse(skillName, out BrushSkillType skillType))
                {
                    loadedSkills.Add(skillType);
                }
            }

            skillInventory.SetUnlockedSkills(loadedSkills);
        }

        if (debugLog)
        {
            Debug.Log("[PlayerSaveController] Player data restored.", this);
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