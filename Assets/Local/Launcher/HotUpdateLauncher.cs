using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HotUpdateLauncher : MonoBehaviour
{
    [Header("Loading UI")]
    public Slider progressSlider;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI statusText;

    [Header("Addressables")]
    public string downloadLabel = "all";

    [Header("HybridCLR")]
    public string hotUpdateDllKey = "HotUpdate.dll.bytes";

    public string[] aotMetadataDllKeys =
    {
        "mscorlib.dll.bytes",
        "System.dll.bytes",
        "System.Core.dll.bytes"
    };

    [Header("Scene")]
    public string mainSceneKey = "Main";

    private void Start()
    {
        SetProgress(0f, "Start hot update...");
        StartHotUpdate();
    }

    private void StartHotUpdate()
    {
        ABMgr.Instance.SetDefaultDownloadKey(downloadLabel);
        ABMgr.Instance.SetRetryConfig(3, 1f);

        ABMgr.Instance.Initialize((success, msg) =>
        {
            if (!success)
            {
                SetStatus("Initialize failed: " + msg);
                return;
            }

            SetStatus("Checking catalog...");
            CheckCatalog();
        });
    }

    private void CheckCatalog()
    {
        ABMgr.Instance.CheckAndUpdateCatalogs((success, hasUpdate, msg) =>
        {
            if (!success)
            {
                SetStatus("Catalog update failed: " + msg);
                return;
            }

            SetStatus(hasUpdate ? "Catalog updated." : "Catalog no update.");

            CheckDownloadSize();
        });
    }

    private void CheckDownloadSize()
    {
        ABMgr.Instance.GetDownloadSize(downloadLabel, (success, size, msg) =>
        {
            if (!success)
            {
                SetStatus("Get download size failed: " + msg);
                return;
            }

            if (size <= 0)
            {
                SetProgress(1f, "No resource update.");
                LoadHybridCLR();
            }
            else
            {
                float mb = size / 1048576f;
                SetStatus($"Need download: {mb:F2} MB");
                DownloadResources();
            }
        });
    }

    private void DownloadResources()
    {
        ABMgr.Instance.DownloadDependencies(
            downloadLabel,
            (progress, downloadedBytes, totalBytes, info) =>
            {
                SetProgress(progress, info);
            },
            (success, msg) =>
            {
                if (!success)
                {
                    SetStatus("Download failed: " + msg);
                    return;
                }

                SetProgress(1f, "Download complete.");
                LoadHybridCLR();
            }
        );
    }

    private void LoadHybridCLR()
    {
        // 如果没有配置热更 DLL，就直接进主场景
        if (string.IsNullOrEmpty(hotUpdateDllKey))
        {
            SetStatus("No HotUpdate.dll. Loading main scene...");
            LoadMainScene();
            return;
        }

        // 如果没有配置 AOT 元数据，就直接加载 HotUpdate.dll
        if (aotMetadataDllKeys == null || aotMetadataDllKeys.Length == 0)
        {
            SetStatus("No AOT metadata. Loading HotUpdate.dll...");
            LoadHotUpdateDll();
            return;
        }

        SetStatus("Loading AOT metadata...");

        ABMgr.Instance.LoadAOTMetadataAsync(aotMetadataDllKeys, (success, msg) =>
        {
            if (!success)
            {
                SetStatus("Load AOT metadata failed: " + msg);
                return;
            }

            LoadHotUpdateDll();
        });
    }

    private void LoadHotUpdateDll()
    {
        ABMgr.Instance.LoadHotUpdateAssemblyAsync(
            hotUpdateDllKey,
            OnHotUpdateAssemblyLoaded,
            (error) =>
            {
                SetStatus("Load hot update dll failed: " + error);
            }
        );
    }

    private void OnHotUpdateAssemblyLoaded(Assembly assembly)
    {
        SetStatus("HotUpdate loaded.");

        // 可选：调用热更入口
        TryCallHotUpdateEntry(assembly);

        LoadMainScene();
    }

    private void TryCallHotUpdateEntry(Assembly assembly)
    {
        try
        {
            Type entryType = assembly.GetType("HotUpdateEntry");

            if (entryType == null)
            {
                Debug.LogWarning("HotUpdateEntry not found. Skip entry call.");
                return;
            }

            MethodInfo method = entryType.GetMethod(
                "Start",
                BindingFlags.Public | BindingFlags.Static
            );

            if (method == null)
            {
                Debug.LogWarning("HotUpdateEntry.Start not found. Skip entry call.");
                return;
            }

            method.Invoke(null, null);
        }
        catch (Exception e)
        {
            Debug.LogError("Call HotUpdateEntry failed: " + e);
        }
    }

    private void LoadMainScene()
    {
        SetStatus("Loading main scene...");

        ABMgr.Instance.LoadSceneAsync(
            mainSceneKey,
            (SceneInstance scene) =>
            {
                SetProgress(1f, "Main scene loaded.");
            },
            (error) =>
            {
                SetStatus("Load main scene failed: " + error);
            },
            LoadSceneMode.Single
        );
    }

    private void SetProgress(float progress, string status)
    {
        progress = Mathf.Clamp01(progress);

        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }

        if (progressText != null)
        {
            progressText.text = Mathf.RoundToInt(progress * 100f) + "%";
        }

        SetStatus(status);
    }

    private void SetStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }

        Debug.Log("[HotUpdateLauncher] " + status);
    }
}