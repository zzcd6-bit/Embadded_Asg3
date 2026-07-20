using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSaveManager : MonoBehaviour
{
    public static PlayerSaveManager Instance
    {
        get;
        private set;
    }

    [Header("Save Targets")]
    public PlayerSaveController
        playerSaveController;

    public MonoBehaviour[] saveModules;

    [Header("Skill Tree")]
    [SerializeField]
    private PlayerSkillTreeController
        skillTreeController;

    [SerializeField]
    private bool grantSkillResetOpportunityOnSave =
        true;

    [Header("File Settings")]
    public string saveFolderName = "Saves";

    public string saveFileName =
        "player_save.json";

    [Header("Test Keys")]
    public KeyCode saveKey = KeyCode.F8;

    public KeyCode loadKey = KeyCode.F9;

    [Header("Debug")]
    public bool debugLog = true;

    private string SaveFolderPath
    {
        get
        {
            return Path.Combine(
                Application.persistentDataPath,
                saveFolderName
            );
        }
    }

    private string SaveFilePath
    {
        get
        {
            return Path.Combine(
                SaveFolderPath,
                saveFileName
            );
        }
    }

    private void Awake()
    {
        Instance = this;

        ResolveReferences();

        EnsureDefaultSaveModules();
        RefreshSaveModules();
    }

    private void Update()
    {
        if (Input.GetKeyDown(saveKey))
        {
            SavePlayer();
        }

        if (Input.GetKeyDown(loadKey))
        {
            LoadPlayer();
        }
    }

    private void ResolveReferences()
    {
        ResolvePlayerSaveController();
        ResolveSkillTreeController();
    }

    private void ResolvePlayerSaveController()
    {
        if (playerSaveController == null)
        {
            playerSaveController =
                FindAnyObjectByType<
                    PlayerSaveController>();
        }
    }

    private void ResolveSkillTreeController()
    {
        if (skillTreeController != null)
            return;

        if (playerSaveController != null)
        {
            skillTreeController =
                playerSaveController
                    .GetComponent<
                        PlayerSkillTreeController>();

            if (skillTreeController == null)
            {
                skillTreeController =
                    playerSaveController
                        .GetComponentInParent<
                            PlayerSkillTreeController>();
            }

            if (skillTreeController == null)
            {
                skillTreeController =
                    playerSaveController
                        .GetComponentInChildren<
                            PlayerSkillTreeController>(
                                true
                            );
            }
        }

        if (skillTreeController == null)
        {
            skillTreeController =
                FindAnyObjectByType<
                    PlayerSkillTreeController>();
        }
    }

    public void SavePlayer()
    {
        ResolveReferences();

        /*
         * 必须在捕获存档数据之前，
         * 给予技能重新分配资格。
         *
         * 这样 CapturePlayerSaveData()
         * 保存到的 canResetSkills 才会是 true。
         */
        GrantSkillResetOpportunity();

        GameSaveData saveData =
            CaptureGameSaveData();

        string json =
            JsonUtility.ToJson(
                saveData,
                true
            );

        if (!Directory.Exists(
                SaveFolderPath))
        {
            Directory.CreateDirectory(
                SaveFolderPath
            );
        }

        File.WriteAllText(
            SaveFilePath,
            json
        );

        if (debugLog)
        {
            Debug.Log(
                "[PlayerSaveManager] " +
                $"Game saved to: " +
                $"{SaveFilePath}",
                this
            );

            if (skillTreeController != null)
            {
                Debug.Log(
                    "[PlayerSaveManager] " +
                    "Skill reset opportunity saved. " +
                    $"CanResetSkills=" +
                    $"{skillTreeController.CanResetSkills}, " +
                    $"AvailableSP=" +
                    $"{skillTreeController.AvailableSkillPoints}",
                    this
                );
            }
        }
    }

    private void GrantSkillResetOpportunity()
    {
        if (!grantSkillResetOpportunityOnSave)
            return;

        ResolveSkillTreeController();

        if (skillTreeController == null)
        {
            Debug.LogWarning(
                "[PlayerSaveManager] " +
                "PlayerSkillTreeController " +
                "was not found. " +
                "Skill reset opportunity " +
                "could not be granted.",
                this
            );

            return;
        }

        /*
         * GrantResetOpportunity 内部是 bool，
         * 所以重复保存不会累计多次重置。
         */
        skillTreeController
            .GrantResetOpportunity();
    }

    public void LoadPlayer()
    {
        if (!File.Exists(
                SaveFilePath))
        {
            Debug.LogWarning(
                "[PlayerSaveManager] " +
                $"Save file not found: " +
                $"{SaveFilePath}",
                this
            );

            return;
        }

        ResolveReferences();

        string json =
            File.ReadAllText(
                SaveFilePath
            );

        GameSaveData saveData =
            ReadSaveJson(
                json
            );

        RestoreGameSaveData(
            saveData
        );

        if (debugLog)
        {
            Debug.Log(
                "[PlayerSaveManager] " +
                $"Game loaded from: " +
                $"{SaveFilePath}",
                this
            );
        }
    }

    public string GetSaveFilePath()
    {
        return SaveFilePath;
    }

    private static GameSaveData ReadSaveJson(
        string json
    )
    {
        if (string.IsNullOrWhiteSpace(
                json))
        {
            return new GameSaveData();
        }

        if (json.Contains("\"version\""))
        {
            return JsonUtility
                .FromJson<GameSaveData>(
                    json
                );
        }

        return new GameSaveData
        {
            player =
                JsonUtility
                    .FromJson<PlayerSaveData>(
                        json
                    )
        };
    }

    public GameSaveData CaptureGameSaveData()
    {
        ResolveReferences();
        RefreshSaveModules();

        GameSaveData saveData =
            new GameSaveData
            {
                sceneName =
                    SceneManager
                        .GetActiveScene()
                        .name
            };

        if (playerSaveController != null)
        {
            saveData.player =
                playerSaveController
                    .CapturePlayerSaveData();
        }
        else
        {
            Debug.LogWarning(
                "[PlayerSaveManager] " +
                "PlayerSaveController " +
                "was not found.",
                this
            );
        }

        for (int i = 0;
             i < saveModules.Length;
             i++)
        {
            MonoBehaviour targetModule =
                saveModules[i];

            if (targetModule is
                IGameSaveModule module)
            {
                module.CaptureGameSaveData(
                    saveData
                );
            }
        }

        return saveData;
    }

    public void RestoreGameSaveData(
        GameSaveData saveData
    )
    {
        if (saveData == null)
        {
            Debug.LogWarning(
                "[PlayerSaveManager] " +
                "Game save data is null.",
                this
            );

            return;
        }

        ResolveReferences();
        RefreshSaveModules();

        if (playerSaveController != null &&
            saveData.player != null)
        {
            playerSaveController
                .RestorePlayerSaveData(
                    saveData.player
                );
        }

        for (int i = 0;
             i < saveModules.Length;
             i++)
        {
            MonoBehaviour targetModule =
                saveModules[i];

            if (targetModule is
                IGameSaveModule module)
            {
                module.RestoreGameSaveData(
                    saveData
                );
            }
        }
    }

    public void ClearSaveCache()
    {
        if (File.Exists(
                SaveFilePath))
        {
            File.Delete(
                SaveFilePath
            );
        }

        if (debugLog)
        {
            Debug.Log(
                "[PlayerSaveManager] " +
                "Save cache cleared. " +
                $"Path={SaveFilePath}",
                this
            );
        }
    }

    private void RefreshSaveModules()
    {
        saveModules =
            FindObjectsByType<
                MonoBehaviour>(
                    FindObjectsInactive.Include);
    }

    private void EnsureDefaultSaveModules()
    {
        WorldStateSaveController
            .EnsureInstance();

        if (FindAnyObjectByType<
                PlacementCommitter>() != null &&
            FindAnyObjectByType<
                BuildingSaveController>() == null)
        {
            new GameObject(
                "BuildingSaveController"
            ).AddComponent<
                BuildingSaveController>();
        }

        if (FindAnyObjectByType<
                TimedPickupSpawner>(
                    FindObjectsInactive.Include
                ) != null &&
            FindAnyObjectByType<
                GameTimeRefreshManager>() == null)
        {
            new GameObject(
                "GameTimeRefreshManager"
            ).AddComponent<
                GameTimeRefreshManager>();
        }
    }
}
