using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSaveManager : MonoBehaviour
{
    public static PlayerSaveManager Instance { get; private set; }

    [Header("Save Targets")]
    public PlayerSaveController playerSaveController;
    public MonoBehaviour[] saveModules;

    [Header("File Settings")]
    public string saveFolderName = "Saves";
    public string saveFileName = "player_save.json";

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

        if (playerSaveController == null)
        {
            playerSaveController = FindObjectOfType<PlayerSaveController>();
        }

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

    public void SavePlayer()
    {
        GameSaveData saveData = CaptureGameSaveData();
        string json = JsonUtility.ToJson(saveData, true);

        if (!Directory.Exists(SaveFolderPath))
        {
            Directory.CreateDirectory(SaveFolderPath);
        }

        File.WriteAllText(SaveFilePath, json);

        if (debugLog)
        {
            Debug.Log($"[PlayerSaveManager] Game saved to: {SaveFilePath}", this);
        }
    }

    public void LoadPlayer()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.LogWarning($"[PlayerSaveManager] Save file not found: {SaveFilePath}", this);
            return;
        }

        string json = File.ReadAllText(SaveFilePath);
        GameSaveData saveData = ReadSaveJson(json);
        RestoreGameSaveData(saveData);

        if (debugLog)
        {
            Debug.Log($"[PlayerSaveManager] Game loaded from: {SaveFilePath}", this);
        }
    }

    public string GetSaveFilePath()
    {
        return SaveFilePath;
    }

    private static GameSaveData ReadSaveJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new GameSaveData();
        }

        if (json.Contains("\"version\""))
        {
            return JsonUtility.FromJson<GameSaveData>(json);
        }

        return new GameSaveData
        {
            player = JsonUtility.FromJson<PlayerSaveData>(json)
        };
    }

    public GameSaveData CaptureGameSaveData()
    {
        RefreshSaveModules();

        GameSaveData saveData = new GameSaveData
        {
            sceneName = SceneManager.GetActiveScene().name
        };

        if (playerSaveController != null)
        {
            saveData.player = playerSaveController.CapturePlayerSaveData();
        }

        for (int i = 0; i < saveModules.Length; i++)
        {
            if (saveModules[i] is IGameSaveModule module)
            {
                module.CaptureGameSaveData(saveData);
            }
        }

        return saveData;
    }

    public void RestoreGameSaveData(GameSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogWarning("[PlayerSaveManager] Game save data is null.", this);
            return;
        }

        RefreshSaveModules();

        if (playerSaveController != null && saveData.player != null)
        {
            playerSaveController.RestorePlayerSaveData(saveData.player);
        }

        for (int i = 0; i < saveModules.Length; i++)
        {
            if (saveModules[i] is IGameSaveModule module)
            {
                module.RestoreGameSaveData(saveData);
            }
        }
    }

    public void ClearSaveCache()
    {
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
        }

        if (debugLog)
        {
            Debug.Log($"[PlayerSaveManager] Save cache cleared. Path={SaveFilePath}", this);
        }
    }

    private void RefreshSaveModules()
    {
        saveModules = FindObjectsOfType<MonoBehaviour>(true);
    }

    private void EnsureDefaultSaveModules()
    {
        WorldStateSaveController.EnsureInstance();

        if (FindAnyObjectByType<PlacementCommitter>() != null
            && FindAnyObjectByType<BuildingSaveController>() == null)
        {
            new GameObject("BuildingSaveController").AddComponent<BuildingSaveController>();
        }

        if (FindAnyObjectByType<TimedPickupSpawner>(FindObjectsInactive.Include) != null
            && FindAnyObjectByType<GameTimeRefreshManager>() == null)
        {
            new GameObject("GameTimeRefreshManager").AddComponent<GameTimeRefreshManager>();
        }
    }
}
