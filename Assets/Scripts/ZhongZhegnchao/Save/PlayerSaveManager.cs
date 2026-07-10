using System.IO;
using UnityEngine;

public class PlayerSaveManager : MonoBehaviour
{
    public static PlayerSaveManager Instance { get; private set; }

    [Header("存档对象")]
    public PlayerSaveController playerSaveController;

    [Header("文件设置")]
    public string saveFolderName = "Saves";
    public string saveFileName = "player_save.json";

    [Header("测试按键")]
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
        if (playerSaveController == null)
        {
            Debug.LogWarning("[PlayerSaveManager] PlayerSaveController is missing.", this);
            return;
        }

        PlayerSaveData saveData = playerSaveController.CapturePlayerSaveData();

        string json = JsonUtility.ToJson(saveData, true);

        if (!Directory.Exists(SaveFolderPath))
        {
            Directory.CreateDirectory(SaveFolderPath);
        }

        File.WriteAllText(SaveFilePath, json);

        if (debugLog)
        {
            Debug.Log($"[PlayerSaveManager] Player saved to: {SaveFilePath}", this);
        }
    }

    public void LoadPlayer()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.LogWarning($"[PlayerSaveManager] Save file not found: {SaveFilePath}", this);
            return;
        }

        if (playerSaveController == null)
        {
            Debug.LogWarning("[PlayerSaveManager] PlayerSaveController is missing.", this);
            return;
        }

        string json = File.ReadAllText(SaveFilePath);

        PlayerSaveData saveData = JsonUtility.FromJson<PlayerSaveData>(json);

        playerSaveController.RestorePlayerSaveData(saveData);

        if (debugLog)
        {
            Debug.Log($"[PlayerSaveManager] Player loaded from: {SaveFilePath}", this);
        }
    }

    public string GetSaveFilePath()
    {
        return SaveFilePath;
    }
}