using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HybridCLR;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

/// <summary>
/// Addressables 专用 ABMgr。
/// 不使用传统 AssetBundle.LoadFromFile。
///
/// 功能：
/// 1. Addressables 初始化
/// 2. Catalog 版本检查 / 更新
/// 3. 远程资源下载
/// 4. 下载进度
/// 5. 失败重试
/// 6. Addressable 资源加载
/// 7. 引用计数
/// 8. GameObject 统一实例化 / 释放
/// 9. Addressable 场景加载 / 卸载
/// 10. Addressables 缓存清理
/// 11. HybridCLR AOT 元数据加载
/// 12. HybridCLR 热更新 DLL 加载
/// </summary>
public class ABMgr : MonoBehaviour
{
    private static ABMgr instance;

    public static ABMgr Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("[ABMgr]");
                instance = obj.AddComponent<ABMgr>();
                DontDestroyOnLoad(obj);
            }

            return instance;
        }
    }

    [Header("Download Config")]
    [SerializeField] private string defaultDownloadKey = "all";
    [SerializeField] private int maxRetryCount = 3;
    [SerializeField] private float retryDelay = 1f;

    private bool isInitialized = false;

    /// <summary>
    /// 已加载资源的 Addressables Handle。
    /// key = 类型名 + Addressables key。
    /// </summary>
    private readonly Dictionary<string, AsyncOperationHandle> assetHandleDic =
        new Dictionary<string, AsyncOperationHandle>();

    /// <summary>
    /// 框架层自己的引用计数。
    /// Addressables 内部也有引用计数，但这里再包一层，方便你管理。
    /// </summary>
    private readonly Dictionary<string, int> assetRefCountDic =
        new Dictionary<string, int>();

    /// <summary>
    /// Addressables.InstantiateAsync 创建出来的实例。
    /// 这种实例必须用 Addressables.ReleaseInstance 释放。
    /// </summary>
    private readonly Dictionary<GameObject, AsyncOperationHandle<GameObject>> instanceHandleDic =
        new Dictionary<GameObject, AsyncOperationHandle<GameObject>>();

    /// <summary>
    /// 已加载的 Addressable 场景。
    /// </summary>
    private readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> sceneHandleDic =
        new Dictionary<string, AsyncOperationHandle<SceneInstance>>();

    private string MakeAssetKey<T>(object key) where T : UnityEngine.Object
    {
        return typeof(T).FullName + "|" + key;
    }

    private string MakeSceneKey(object key)
    {
        return key.ToString();
    }

    public void SetDefaultDownloadKey(string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            defaultDownloadKey = key;
        }
    }

    public void SetRetryConfig(int retryCount, float delay)
    {
        maxRetryCount = Mathf.Max(1, retryCount);
        retryDelay = Mathf.Max(0f, delay);
    }

    //============================================================
    // 1. 初始化 Addressables
    //============================================================

    public void Initialize(UnityAction<bool, string> callBack = null)
    {
        StartCoroutine(InitializeCoroutine(callBack));
    }

    private IEnumerator InitializeCoroutine(UnityAction<bool, string> callBack)
    {
        if (isInitialized)
        {
            callBack?.Invoke(true, "Addressables already initialized.");
            yield break;
        }

        // 重点：这里必须传 false，避免完成后自动释放 handle
        AsyncOperationHandle initHandle = Addressables.InitializeAsync(false);

        yield return initHandle;

        // 先判断 handle 是否有效，再读取 Status
        if (!initHandle.IsValid())
        {
            callBack?.Invoke(false, "Addressables initialize failed: handle is invalid.");
            yield break;
        }

        if (initHandle.Status == AsyncOperationStatus.Succeeded)
        {
            isInitialized = true;

            callBack?.Invoke(true, "Addressables initialize success.");
        }
        else
        {
            string error = initHandle.OperationException != null
                ? initHandle.OperationException.Message
                : "Addressables initialize failed.";

            callBack?.Invoke(false, error);
        }

        // 所有信息读完后，最后再 Release
        if (initHandle.IsValid())
        {
            Addressables.Release(initHandle);
        }
    }

    private IEnumerator EnsureInitializedCoroutine()
    {
        if (isInitialized)
        {
            yield break;
        }

        bool done = false;
        bool success = false;

        Initialize((result, msg) =>
        {
            success = result;
            done = true;

            if (!result)
            {
                Debug.LogError("ABMgr Initialize Failed: " + msg);
            }
        });

        while (!done)
        {
            yield return null;
        }

        if (!success)
        {
            yield break;
        }
    }

    //============================================================
    // 2. Catalog 版本检查 / 更新
    //============================================================

    /// <summary>
    /// 检查并更新 Catalog。
    /// 建议游戏启动时先调用这个，再下载资源。
    /// </summary>
    public void CheckAndUpdateCatalogs(UnityAction<bool, bool, string> callBack)
    {
        StartCoroutine(CheckAndUpdateCatalogsCoroutine(callBack));
    }

    private IEnumerator CheckAndUpdateCatalogsCoroutine(UnityAction<bool, bool, string> callBack)
    {
        yield return EnsureInitializedCoroutine();

        if (!isInitialized)
        {
            callBack?.Invoke(false, false, "Addressables not initialized.");
            yield break;
        }

        AsyncOperationHandle<List<string>> checkHandle =
            Addressables.CheckForCatalogUpdates(false);

        yield return checkHandle;

        if (checkHandle.Status != AsyncOperationStatus.Succeeded)
        {
            string error = checkHandle.OperationException != null
                ? checkHandle.OperationException.Message
                : "Check catalog update failed.";

            if (checkHandle.IsValid())
            {
                Addressables.Release(checkHandle);
            }

            callBack?.Invoke(false, false, error);
            yield break;
        }

        List<string> catalogs = checkHandle.Result;

        if (catalogs == null || catalogs.Count == 0)
        {
            if (checkHandle.IsValid())
            {
                Addressables.Release(checkHandle);
            }

            callBack?.Invoke(true, false, "No catalog update.");
            yield break;
        }

        var updateHandle = Addressables.UpdateCatalogs(catalogs, false);

        yield return updateHandle;

        if (updateHandle.Status == AsyncOperationStatus.Succeeded)
        {
            if (updateHandle.IsValid())
            {
                Addressables.Release(updateHandle);
            }

            if (checkHandle.IsValid())
            {
                Addressables.Release(checkHandle);
            }

            callBack?.Invoke(true, true, "Catalog update success.");
        }
        else
        {
            string error = updateHandle.OperationException != null
                ? updateHandle.OperationException.Message
                : "Update catalogs failed.";

            if (updateHandle.IsValid())
            {
                Addressables.Release(updateHandle);
            }

            if (checkHandle.IsValid())
            {
                Addressables.Release(checkHandle);
            }

            callBack?.Invoke(false, true, error);
        }
    }

    //============================================================
    // 3. 获取下载大小
    //============================================================

    public void GetDownloadSize(object key, UnityAction<bool, long, string> callBack)
    {
        StartCoroutine(GetDownloadSizeCoroutine(key, callBack));
    }

    private IEnumerator GetDownloadSizeCoroutine(object key, UnityAction<bool, long, string> callBack)
    {
        yield return EnsureInitializedCoroutine();

        if (!isInitialized)
        {
            callBack?.Invoke(false, 0, "Addressables not initialized.");
            yield break;
        }

        AsyncOperationHandle<long> handle =
            Addressables.GetDownloadSizeAsync(key);

        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            long size = handle.Result;

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            callBack?.Invoke(true, size, "Get download size success.");
        }
        else
        {
            string error = handle.OperationException != null
                ? handle.OperationException.Message
                : "Get download size failed.";

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            callBack?.Invoke(false, 0, error);
        }
    }

    //============================================================
    // 4. 下载远程依赖资源
    //============================================================

    public void DownloadDefault(
        UnityAction<float, long, long, string> progressCallBack,
        UnityAction<bool, string> finishCallBack
    )
    {
        DownloadDependencies(defaultDownloadKey, progressCallBack, finishCallBack);
    }

    /// <summary>
    /// 下载某个 Address / Label 对应的依赖。
    /// 常用 key：all、ui、scene、hotupdate。
    /// </summary>
    public void DownloadDependencies(
        object key,
        UnityAction<float, long, long, string> progressCallBack,
        UnityAction<bool, string> finishCallBack
    )
    {
        StartCoroutine(DownloadDependenciesCoroutine(key, progressCallBack, finishCallBack));
    }

    private IEnumerator DownloadDependenciesCoroutine(
        object key,
        UnityAction<float, long, long, string> progressCallBack,
        UnityAction<bool, string> finishCallBack
    )
    {
        yield return EnsureInitializedCoroutine();

        if (!isInitialized)
        {
            finishCallBack?.Invoke(false, "Addressables not initialized.");
            yield break;
        }

        for (int attempt = 1; attempt <= maxRetryCount; attempt++)
        {
            AsyncOperationHandle handle =
                Addressables.DownloadDependenciesAsync(key, false);

            while (!handle.IsDone)
            {
                var status = handle.GetDownloadStatus();

                float progress = status.Percent;
                long downloadedBytes = status.DownloadedBytes;
                long totalBytes = status.TotalBytes;

                string info =
                    $"Downloading {downloadedBytes / 1048576f:F2} MB / {totalBytes / 1048576f:F2} MB";

                progressCallBack?.Invoke(progress, downloadedBytes, totalBytes, info);

                yield return null;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                progressCallBack?.Invoke(1f, 1, 1, "Download complete.");

                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                finishCallBack?.Invoke(true, "Download success.");
                yield break;
            }

            string error = handle.OperationException != null
                ? handle.OperationException.Message
                : "Download failed.";

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            if (attempt < maxRetryCount)
            {
                progressCallBack?.Invoke(
                    0f,
                    0,
                    0,
                    $"Download failed. Retry {attempt}/{maxRetryCount}. Error: {error}"
                );

                yield return new WaitForSecondsRealtime(retryDelay);
            }
            else
            {
                finishCallBack?.Invoke(false, error);
            }
        }
    }

    //============================================================
    // 5. 加载普通资源
    //============================================================

    /// <summary>
    /// 加载 Addressable 资源。
    /// 注意：
    /// 1. 这里不会自动 Instantiate。
    /// 2. 如果是 GameObject，只返回 prefab。
    /// 3. 用完需要 ReleaseAsset。
    /// </summary>
    public void LoadAssetAsync<T>(
        object key,
        UnityAction<T> successCallBack,
        UnityAction<string> failCallBack = null
    ) where T : UnityEngine.Object
    {
        StartCoroutine(LoadAssetCoroutine(key, successCallBack, failCallBack));
    }

    private IEnumerator LoadAssetCoroutine<T>(
        object key,
        UnityAction<T> successCallBack,
        UnityAction<string> failCallBack
    ) where T : UnityEngine.Object
    {
        yield return EnsureInitializedCoroutine();

        if (!isInitialized)
        {
            failCallBack?.Invoke("Addressables not initialized.");
            yield break;
        }

        string assetKey = MakeAssetKey<T>(key);

        if (assetHandleDic.ContainsKey(assetKey))
        {
            AsyncOperationHandle oldHandle = assetHandleDic[assetKey];

            if (oldHandle.IsValid())
            {
                if (!assetRefCountDic.ContainsKey(assetKey))
                {
                    assetRefCountDic[assetKey] = 0;
                }

                assetRefCountDic[assetKey]++;

                while (!oldHandle.IsDone)
                {
                    yield return null;
                }

                if (oldHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    successCallBack?.Invoke(oldHandle.Result as T);
                }
                else
                {
                    failCallBack?.Invoke("Cached handle failed: " + key);
                }

                yield break;
            }

            assetHandleDic.Remove(assetKey);
            assetRefCountDic.Remove(assetKey);
        }

        for (int attempt = 1; attempt <= maxRetryCount; attempt++)
        {
            AsyncOperationHandle<T> handle =
                Addressables.LoadAssetAsync<T>(key);

            assetHandleDic[assetKey] = handle;
            assetRefCountDic[assetKey] = 1;

            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                successCallBack?.Invoke(handle.Result);
                yield break;
            }

            string error = handle.OperationException != null
                ? handle.OperationException.Message
                : "Load asset failed: " + key;

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            assetHandleDic.Remove(assetKey);
            assetRefCountDic.Remove(assetKey);

            if (attempt < maxRetryCount)
            {
                yield return new WaitForSecondsRealtime(retryDelay);
            }
            else
            {
                failCallBack?.Invoke(error);
            }
        }
    }

    public void ReleaseAsset<T>(object key) where T : UnityEngine.Object
    {
        string assetKey = MakeAssetKey<T>(key);

        if (!assetHandleDic.ContainsKey(assetKey))
        {
            return;
        }

        if (!assetRefCountDic.ContainsKey(assetKey))
        {
            assetRefCountDic[assetKey] = 1;
        }

        assetRefCountDic[assetKey]--;

        if (assetRefCountDic[assetKey] > 0)
        {
            return;
        }

        AsyncOperationHandle handle = assetHandleDic[assetKey];

        if (handle.IsValid())
        {
            Addressables.Release(handle);
        }

        assetHandleDic.Remove(assetKey);
        assetRefCountDic.Remove(assetKey);
    }

    //============================================================
    // 6. GameObject 统一实例化规则
    //============================================================

    /// <summary>
    /// 统一规则：
    /// 如果你要生成 GameObject，优先用这个方法。
    /// 不建议 LoadAssetAsync<GameObject> 后自己 Instantiate。
    /// </summary>
    public void InstantiateAsync(
        object key,
        UnityAction<GameObject> successCallBack,
        Transform parent = null,
        bool instantiateInWorldSpace = false,
        UnityAction<string> failCallBack = null
    )
    {
        StartCoroutine(InstantiateCoroutine(
            key,
            successCallBack,
            parent,
            instantiateInWorldSpace,
            failCallBack
        ));
    }

    private IEnumerator InstantiateCoroutine(
        object key,
        UnityAction<GameObject> successCallBack,
        Transform parent,
        bool instantiateInWorldSpace,
        UnityAction<string> failCallBack
    )
    {
        yield return EnsureInitializedCoroutine();

        if (!isInitialized)
        {
            failCallBack?.Invoke("Addressables not initialized.");
            yield break;
        }

        for (int attempt = 1; attempt <= maxRetryCount; attempt++)
        {
            AsyncOperationHandle<GameObject> handle =
                Addressables.InstantiateAsync(key, parent, instantiateInWorldSpace);

            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject obj = handle.Result;

                if (obj != null && !instanceHandleDic.ContainsKey(obj))
                {
                    instanceHandleDic.Add(obj, handle);
                }

                successCallBack?.Invoke(obj);
                yield break;
            }

            string error = handle.OperationException != null
                ? handle.OperationException.Message
                : "Instantiate failed: " + key;

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            if (attempt < maxRetryCount)
            {
                yield return new WaitForSecondsRealtime(retryDelay);
            }
            else
            {
                failCallBack?.Invoke(error);
            }
        }
    }

    public void ReleaseInstance(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        if (instanceHandleDic.ContainsKey(obj))
        {
            AsyncOperationHandle<GameObject> handle = instanceHandleDic[obj];

            if (handle.IsValid())
            {
                Addressables.ReleaseInstance(handle);
            }

            instanceHandleDic.Remove(obj);
            return;
        }

        bool released = Addressables.ReleaseInstance(obj);

        if (!released)
        {
            Destroy(obj);
        }
    }

    //============================================================
    // 7. 场景加载 / 卸载
    //============================================================

    public void LoadSceneAsync(
        object key,
        UnityAction<SceneInstance> successCallBack = null,
        UnityAction<string> failCallBack = null,
        LoadSceneMode loadMode = LoadSceneMode.Single,
        bool activateOnLoad = true
    )
    {
        StartCoroutine(LoadSceneCoroutine(
            key,
            successCallBack,
            failCallBack,
            loadMode,
            activateOnLoad
        ));
    }

    private IEnumerator LoadSceneCoroutine(
        object key,
        UnityAction<SceneInstance> successCallBack,
        UnityAction<string> failCallBack,
        LoadSceneMode loadMode,
        bool activateOnLoad
    )
    {
        yield return EnsureInitializedCoroutine();

        if (!isInitialized)
        {
            failCallBack?.Invoke("Addressables not initialized.");
            yield break;
        }

        string sceneKey = MakeSceneKey(key);

        if (sceneHandleDic.ContainsKey(sceneKey))
        {
            AsyncOperationHandle<SceneInstance> oldHandle = sceneHandleDic[sceneKey];

            if (oldHandle.IsValid() &&
                oldHandle.Status == AsyncOperationStatus.Succeeded)
            {
                successCallBack?.Invoke(oldHandle.Result);
                yield break;
            }

            sceneHandleDic.Remove(sceneKey);
        }

        for (int attempt = 1; attempt <= maxRetryCount; attempt++)
        {
            AsyncOperationHandle<SceneInstance> handle =
                Addressables.LoadSceneAsync(key, loadMode, activateOnLoad);

            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                sceneHandleDic[sceneKey] = handle;
                successCallBack?.Invoke(handle.Result);
                yield break;
            }

            string error = handle.OperationException != null
                ? handle.OperationException.Message
                : "Load scene failed: " + key;

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            if (attempt < maxRetryCount)
            {
                yield return new WaitForSecondsRealtime(retryDelay);
            }
            else
            {
                failCallBack?.Invoke(error);
            }
        }
    }

    public void UnloadSceneAsync(object key, UnityAction<bool, string> callBack = null)
    {
        StartCoroutine(UnloadSceneCoroutine(key, callBack));
    }

    private IEnumerator UnloadSceneCoroutine(object key, UnityAction<bool, string> callBack)
    {
        string sceneKey = MakeSceneKey(key);

        if (!sceneHandleDic.ContainsKey(sceneKey))
        {
            callBack?.Invoke(false, "Scene not loaded by ABMgr: " + key);
            yield break;
        }

        AsyncOperationHandle<SceneInstance> sceneHandle = sceneHandleDic[sceneKey];

        if (!sceneHandle.IsValid())
        {
            sceneHandleDic.Remove(sceneKey);
            callBack?.Invoke(false, "Scene handle invalid: " + key);
            yield break;
        }

        AsyncOperationHandle<SceneInstance> unloadHandle =
            Addressables.UnloadSceneAsync(sceneHandle, true);

        yield return unloadHandle;

        sceneHandleDic.Remove(sceneKey);

        if (unloadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            callBack?.Invoke(true, "Unload scene success.");
        }
        else
        {
            string error = unloadHandle.OperationException != null
                ? unloadHandle.OperationException.Message
                : "Unload scene failed.";

            callBack?.Invoke(false, error);
        }
    }

    //============================================================
    // 8. 缓存清理
    //============================================================

    public void ClearDependencyCache(object key, UnityAction<bool, string> callBack = null)
    {
        StartCoroutine(ClearDependencyCacheCoroutine(key, callBack));
    }

    private IEnumerator ClearDependencyCacheCoroutine(object key, UnityAction<bool, string> callBack)
    {
        yield return EnsureInitializedCoroutine();

        AsyncOperationHandle<bool> handle =
            Addressables.ClearDependencyCacheAsync(key, false);

        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            bool result = handle.Result;

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            callBack?.Invoke(result, result ? "Clear cache success." : "Clear cache failed.");
        }
        else
        {
            string error = handle.OperationException != null
                ? handle.OperationException.Message
                : "Clear dependency cache failed.";

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            callBack?.Invoke(false, error);
        }
    }

    public bool ClearAllUnityCache()
    {
        return Caching.ClearCache();
    }

    //============================================================
    // 9. 释放当前 ABMgr 管理的资源
    //============================================================

    public void ReleaseAllManagedAssets()
    {
        foreach (var pair in instanceHandleDic)
        {
            AsyncOperationHandle<GameObject> handle = pair.Value;

            if (handle.IsValid())
            {
                Addressables.ReleaseInstance(handle);
            }
        }

        instanceHandleDic.Clear();

        foreach (var pair in assetHandleDic)
        {
            AsyncOperationHandle handle = pair.Value;

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        assetHandleDic.Clear();
        assetRefCountDic.Clear();
    }

    //============================================================
    // 10. HybridCLR：加载 AOT 元数据
    //============================================================

    public void LoadAOTMetadataAsync(
        string[] aotDllKeys,
        UnityAction<bool, string> finishCallBack
    )
    {
        StartCoroutine(LoadAOTMetadataCoroutine(aotDllKeys, finishCallBack));
    }

    private IEnumerator LoadAOTMetadataCoroutine(
        string[] aotDllKeys,
        UnityAction<bool, string> finishCallBack
    )
    {
        yield return EnsureInitializedCoroutine();

        if (aotDllKeys == null || aotDllKeys.Length == 0)
        {
            finishCallBack?.Invoke(true, "No AOT metadata need load.");
            yield break;
        }

        for (int i = 0; i < aotDllKeys.Length; i++)
        {
            string key = aotDllKeys[i];

            bool done = false;
            bool success = false;
            string message = "";
            TextAsset dllAsset = null;

            LoadAssetAsync<TextAsset>(
                key,
                (asset) =>
                {
                    dllAsset = asset;
                    success = asset != null;
                    done = true;
                },
                (error) =>
                {
                    message = error;
                    success = false;
                    done = true;
                }
            );

            while (!done)
            {
                yield return null;
            }

            if (!success || dllAsset == null)
            {
                finishCallBack?.Invoke(false, "Load AOT metadata failed: " + key + " / " + message);
                yield break;
            }

            var errorCode =
    RuntimeApi.LoadMetadataForAOTAssembly(
        dllAsset.bytes,
        HomologousImageMode.SuperSet
    );

            Debug.Log($"Load AOT Metadata: {key}, Result: {errorCode}");

            ReleaseAsset<TextAsset>(key);
        }

        finishCallBack?.Invoke(true, "Load AOT metadata success.");
    }

    //============================================================
    // 11. HybridCLR：加载热更新 DLL
    //============================================================

    public void LoadHotUpdateAssemblyAsync(
        string hotUpdateDllKey,
        UnityAction<Assembly> successCallBack,
        UnityAction<string> failCallBack = null
    )
    {
        StartCoroutine(LoadHotUpdateAssemblyCoroutine(
            hotUpdateDllKey,
            successCallBack,
            failCallBack
        ));
    }

    private IEnumerator LoadHotUpdateAssemblyCoroutine(
        string hotUpdateDllKey,
        UnityAction<Assembly> successCallBack,
        UnityAction<string> failCallBack
    )
    {
        yield return EnsureInitializedCoroutine();

        bool done = false;
        bool success = false;
        string message = "";
        TextAsset dllAsset = null;

        LoadAssetAsync<TextAsset>(
            hotUpdateDllKey,
            (asset) =>
            {
                dllAsset = asset;
                success = asset != null;
                done = true;
            },
            (error) =>
            {
                message = error;
                success = false;
                done = true;
            }
        );

        while (!done)
        {
            yield return null;
        }

        if (!success || dllAsset == null)
        {
            failCallBack?.Invoke("Load hot update dll failed: " + hotUpdateDllKey + " / " + message);
            yield break;
        }

        try
        {
            Assembly hotUpdateAssembly = Assembly.Load(dllAsset.bytes);

            ReleaseAsset<TextAsset>(hotUpdateDllKey);

            successCallBack?.Invoke(hotUpdateAssembly);
        }
        catch (Exception e)
        {
            ReleaseAsset<TextAsset>(hotUpdateDllKey);

            failCallBack?.Invoke("Assembly.Load failed: " + e.Message);
        }
    }
}