using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// 层级枚举
/// </summary>
public enum E_UILayer
{
    /// <summary>
    /// 最底层
    /// </summary>
    Bottom,

    /// <summary>
    /// 中层
    /// </summary>
    Middle,

    /// <summary>
    /// 高层
    /// </summary>
    Top,

    /// <summary>
    /// 系统层，最高层
    /// </summary>
    System,
}

/// <summary>
/// 管理所有 UI 面板的管理器。
/// 注意：
/// 1. 面板预制体名要和面板类名一致。
/// 2. 面板预制体需要放在 Resources/UI/ 下。
/// 例如：
/// Assets/Resources/UI/MainPanel.prefab
/// 对应类名：MainPanel
/// </summary>
public class UIMgr : BaseMgr<UIMgr>
{
    private Camera uiCamera;
    private Canvas uiCanvas;
    private EventSystem uiEventSystem;

    // 层级父对象
    private Transform bottomLayer;
    private Transform middleLayer;
    private Transform topLayer;
    private Transform systemLayer;

    /// <summary>
    /// 用于存储所有已经打开的面板对象
    /// </summary>
    private Dictionary<string, BasePanel> panelDic = new Dictionary<string, BasePanel>();

    private UIMgr()
    {
        InitUICamera();
        InitCanvas();
        InitLayers();
        InitEventSystem();
    }

    private void InitUICamera()
    {
        uiCamera = FindSceneObject<Camera>("UICamera");
        if (uiCamera != null)
        {
            return;
        }

        GameObject cameraPrefab = ResMgr.Instance.Load<GameObject>("UI/UICamera");

        if (cameraPrefab == null)
        {
            Debug.LogError("UIMgr: Resources/UI/UICamera 预制体不存在");
            return;
        }

        uiCamera = GameObject.Instantiate(cameraPrefab).GetComponent<Camera>();

        if (uiCamera != null)
        {
            GameObject.DontDestroyOnLoad(uiCamera.gameObject);
        }
    }

    private void InitCanvas()
    {
        uiCanvas = FindSceneObject<Canvas>("Canvas");
        if (uiCanvas != null)
        {
            if (!HasRequiredLayers(uiCanvas.transform))
            {
                Debug.LogWarning(
                    $"UIMgr: 场景 Canvas({uiCanvas.gameObject.name}) 缺少 UI 层级，改用 Resources/UI/Canvas 预制体。"
                );
                uiCanvas = null;
            }
        }

        if (uiCanvas != null)
        {
            if (uiCamera != null)
            {
                uiCanvas.worldCamera = uiCamera;
            }

            return;
        }

        GameObject canvasPrefab = ResMgr.Instance.Load<GameObject>("UI/Canvas");

        if (canvasPrefab == null)
        {
            Debug.LogError("UIMgr: Resources/UI/Canvas 预制体不存在");
            return;
        }

        uiCanvas = GameObject.Instantiate(canvasPrefab).GetComponent<Canvas>();

        if (uiCanvas == null)
        {
            Debug.LogError("UIMgr: Canvas 预制体上没有 Canvas 组件");
            return;
        }

        if (uiCamera != null)
        {
            uiCanvas.worldCamera = uiCamera;
        }

        GameObject.DontDestroyOnLoad(uiCanvas.gameObject);
    }

    private void InitLayers()
    {
        if (uiCanvas == null) return;

        bottomLayer = uiCanvas.transform.Find("Bottom");
        middleLayer = uiCanvas.transform.Find("Middle");
        topLayer = uiCanvas.transform.Find("Top");
        systemLayer = uiCanvas.transform.Find("System");

        if (bottomLayer == null)
        {
            Debug.LogError("UIMgr: Canvas 下缺少 Bottom 层");
        }

        if (middleLayer == null)
        {
            Debug.LogError("UIMgr: Canvas 下缺少 Middle 层");
        }

        if (topLayer == null)
        {
            Debug.LogError("UIMgr: Canvas 下缺少 Top 层");
        }

        if (systemLayer == null)
        {
            Debug.LogError("UIMgr: Canvas 下缺少 System 层");
        }
    }

    private bool HasRequiredLayers(Transform canvasTransform)
    {
        if (canvasTransform == null)
        {
            return false;
        }

        return canvasTransform.Find("Bottom") != null &&
               canvasTransform.Find("Middle") != null &&
               canvasTransform.Find("Top") != null &&
               canvasTransform.Find("System") != null;
    }
    private void InitEventSystem()
    {
        uiEventSystem = FindSceneObject<EventSystem>("EventSystem");
        if (uiEventSystem != null)
        {
            return;
        }

        if (EventSystem.current != null)
        {
            uiEventSystem = EventSystem.current;
            return;
        }

        GameObject eventSystemPrefab = ResMgr.Instance.Load<GameObject>("UI/EventSystem");

        if (eventSystemPrefab == null)
        {
            Debug.LogError("UIMgr: Resources/UI/EventSystem 预制体不存在");
            return;
        }

        uiEventSystem = GameObject.Instantiate(eventSystemPrefab).GetComponent<EventSystem>();

        if (uiEventSystem != null)
        {
            GameObject.DontDestroyOnLoad(uiEventSystem.gameObject);
        }
    }

    private static T FindSceneObject<T>(string objectName) where T : Component
    {
        T[] objects = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < objects.Length; i++)
        {
            T candidate = objects[i];
            if (candidate == null || candidate.gameObject == null)
            {
                continue;
            }

            if (!candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            if (!candidate.gameObject.scene.isLoaded)
            {
                continue;
            }

            if (candidate.gameObject.name == objectName)
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取对应层级的父对象
    /// </summary>
    public Transform GetLayerFather(E_UILayer layer)
    {
        switch (layer)
        {
            case E_UILayer.Bottom:
                return bottomLayer;

            case E_UILayer.Middle:
                return middleLayer;

            case E_UILayer.Top:
                return topLayer;

            case E_UILayer.System:
                return systemLayer;

            default:
                return middleLayer;
        }
    }

    /// <summary>
    /// 显示面板
    /// </summary>
    /// <typeparam name="T">面板类型</typeparam>
    /// <param name="layer">面板显示层级</param>
    /// <param name="callBack">面板加载完成后的回调</param>
    /// <param name="isSync">是否同步加载</param>
    public void ShowPanel<T>(
        E_UILayer layer = E_UILayer.Middle,
        UnityAction<T> callBack = null,
        bool isSync = false
    ) where T : BasePanel
    {
        string panelName = typeof(T).Name;

        // 如果面板已经存在，直接显示
        if (panelDic.ContainsKey(panelName))
        {
            BasePanel existingPanel = panelDic[panelName];

            if (existingPanel != null)
            {
                existingPanel.ShowMe();
                callBack?.Invoke(existingPanel as T);
                return;
            }

            panelDic.Remove(panelName);
        }

        string panelPath = "UI/" + panelName;

        if (isSync)
        {
            GameObject panelPrefab = ResMgr.Instance.Load<GameObject>(panelPath);

            CreatePanelInstance(
                panelName,
                panelPrefab,
                layer,
                callBack
            );
        }
        else
        {
            ResMgr.Instance.LoadAsync<GameObject>(
                panelPath,
                (panelPrefab) =>
                {
                    CreatePanelInstance(
                        panelName,
                        panelPrefab,
                        layer,
                        callBack
                    );
                }
            );
        }
    }

    private void CreatePanelInstance<T>(
        string panelName,
        GameObject panelPrefab,
        E_UILayer layer,
        UnityAction<T> callBack
    ) where T : BasePanel
    {
        if (panelPrefab == null)
        {
            Debug.LogError("UIMgr: 面板预制体加载失败：" + panelName);
            return;
        }

        Transform father = GetLayerFather(layer);

        if (father == null)
        {
            father = middleLayer;
        }

        if (father == null)
        {
            Debug.LogError("UIMgr: UI 层级父对象为空，无法创建面板：" + panelName);
            return;
        }

        GameObject panelObj = GameObject.Instantiate(
            panelPrefab,
            father,
            false
        );

        T panel = panelObj.GetComponent<T>();

        if (panel == null)
        {
            Debug.LogError(
                "UIMgr: 面板预制体上没有对应脚本：" +
                panelName +
                "，请确认预制体名和脚本类名一致"
            );

            GameObject.Destroy(panelObj);
            return;
        }

        panel.ShowMe();

        callBack?.Invoke(panel);

        if (!panelDic.ContainsKey(panelName))
        {
            panelDic.Add(panelName, panel);
        }
        else
        {
            panelDic[panelName] = panel;
        }
    }

    /// <summary>
    /// 隐藏面板
    /// 注意：这里只销毁面板实例，不卸载 Resources 资源。
    /// </summary>
    public void HidePanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;

        if (!panelDic.ContainsKey(panelName)) return;

        BasePanel panel = panelDic[panelName];

        if (panel != null)
        {
            panel.HideMe();
            GameObject.Destroy(panel.gameObject);
        }

        panelDic.Remove(panelName);
    }

    /// <summary>
    /// 获取面板
    /// </summary>
    public T GetPanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;

        if (panelDic.ContainsKey(panelName))
        {
            return panelDic[panelName] as T;
        }

        return null;
    }

    /// <summary>
    /// 判断某个面板是否正在显示 / 已创建
    /// </summary>
    public bool IsPanelShown<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;

        return panelDic.ContainsKey(panelName) && panelDic[panelName] != null;
    }

    /// <summary>
    /// 隐藏所有面板
    /// </summary>
    public void HideAllPanels()
    {
        List<string> panelNames = new List<string>(panelDic.Keys);

        for (int i = 0; i < panelNames.Count; i++)
        {
            string panelName = panelNames[i];

            if (!panelDic.ContainsKey(panelName)) continue;

            BasePanel panel = panelDic[panelName];

            if (panel != null)
            {
                panel.HideMe();
                GameObject.Destroy(panel.gameObject);
            }
        }

        panelDic.Clear();
    }
}