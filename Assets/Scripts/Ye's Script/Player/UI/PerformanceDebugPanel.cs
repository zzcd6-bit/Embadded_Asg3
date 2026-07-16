using System.Text;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class PerformanceDebugPanel : MonoBehaviour
{
    private static PerformanceDebugPanel instance;

    [Header("Toggle")]
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
    [SerializeField] private bool visibleOnStart = true;

    [Header("Refresh")]
    [SerializeField, Min(0.25f)] private float refreshInterval = 0.75f;
    [SerializeField] private bool logToConsole;

    [Header("Ping")]
    [SerializeField] private bool showPing = true;
    [SerializeField] private string pingAddress = "8.8.8.8";
    [SerializeField, Min(0.5f)] private float pingInterval = 1f;

    [Header("Layout")]
    [SerializeField] private Vector2 panelSize = new(760f, 132f);
    [SerializeField] private Vector2 anchoredPosition = new(0f, -16f);
    [SerializeField] private int fontSize = 16;

    private readonly StringBuilder builder = new();
    private readonly FrameTiming[] frameTimings = new FrameTiming[1];

    private CanvasGroup canvasGroup;
    private TextMeshProUGUI text;
    private float nextRefreshTime;
    private int frames;
    private float elapsed;
    private float minFrameMs = float.MaxValue;
    private float maxFrameMs;
    private bool isVisible;
    private Ping activePing;
    private float nextPingTime;
    private int lastPingMs = -1;

    private ProfilerRecorder gcAllocRecorder;
    private ProfilerRecorder physicsRecorder;
    private ProfilerRecorder uiRebuildRecorder;
    private ProfilerRecorder scriptsRecorder;

    public static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        GameObject panelObject = new("PerformanceDebugPanel");
        DontDestroyOnLoad(panelObject);
        panelObject.AddComponent<PerformanceDebugPanel>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateUi();
        StartRecorders();
        SetVisible(visibleOnStart);
    }

    private void OnDestroy()
    {
        DisposeRecorders();

        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            SetVisible(!isVisible);
        }

        float frameMs = Time.unscaledDeltaTime * 1000f;
        frames++;
        elapsed += Time.unscaledDeltaTime;
        minFrameMs = Mathf.Min(minFrameMs, frameMs);
        maxFrameMs = Mathf.Max(maxFrameMs, frameMs);
        FrameTimingManager.CaptureFrameTimings();
        UpdatePing();

        if (!isVisible || Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + refreshInterval;
        RefreshText();
    }

    private void CreateUi()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        GameObject textObject = new("PerformanceText", typeof(RectTransform), typeof(CanvasRenderer));
        textObject.transform.SetParent(transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = panelSize;

        text = textObject.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.fontSize = fontSize;
        text.color = new Color(0.92f, 1f, 0.86f, 0.95f);
        text.text = "Performance panel warming up...";
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;

        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        nextRefreshTime = 0f;
    }

    private void RefreshText()
    {
        float averageFps = elapsed > 0f ? frames / elapsed : 0f;
        float averageFrameMs = averageFps > 0f ? 1000f / averageFps : 0f;
        float cpuFrameMs = 0f;
        float gpuFrameMs = 0f;

        if (FrameTimingManager.GetLatestTimings(1, frameTimings) > 0)
        {
            cpuFrameMs = (float)frameTimings[0].cpuFrameTime;
            gpuFrameMs = (float)frameTimings[0].gpuFrameTime;
        }

        builder.Clear();
        builder.Append("FPS ");
        builder.Append(averageFps.ToString("F1"));
        builder.Append(" | Frame ");
        builder.Append(averageFrameMs.ToString("F2"));
        builder.Append(" ms (min ");
        builder.Append(minFrameMs.ToString("F2"));
        builder.Append(" / max ");
        builder.Append(maxFrameMs.ToString("F2"));
        builder.AppendLine(")");

        builder.Append("CPU/GPU  CPU frame ");
        builder.Append(cpuFrameMs.ToString("F2"));
        builder.Append(" ms | GPU ");
        builder.Append(gpuFrameMs.ToString("F2"));
        builder.AppendLine(" ms");

        builder.Append("MEM/GC  GC Alloc ");
        builder.Append(GetRecorderKb(gcAllocRecorder).ToString("F1"));
        builder.Append(" KB/frame | Total ");
        builder.Append(ToMb(Profiler.GetTotalReservedMemoryLong()));
        builder.AppendLine(" MB");

        builder.Append("SYSTEM  Physics ");
        builder.Append(GetRecorderMs(physicsRecorder).ToString("F2"));
        builder.Append(" ms | UI Rebuild ");
        builder.Append(GetRecorderMs(uiRebuildRecorder).ToString("F2"));
        builder.Append(" ms | Scripts ");
        builder.Append(GetRecorderMs(scriptsRecorder).ToString("F2"));
        builder.AppendLine(" ms");

        builder.Append("NET  Ping ");
        builder.Append(GetPingText());
        builder.Append(" | Target ");
        builder.Append(string.IsNullOrWhiteSpace(pingAddress) ? "N/A" : pingAddress);
        builder.AppendLine();

#if UNITY_EDITOR
        builder.Append("RENDER  Draw Calls ");
        builder.Append(UnityStats.drawCalls);
        builder.Append(" | SetPass ");
        builder.Append(UnityStats.setPassCalls);
        builder.Append(" | Tris ");
        builder.Append(UnityStats.triangles);
#else
        builder.Append("RENDER  Draw Calls N/A | SetPass N/A | Tris N/A");
#endif
        builder.Append(" | Toggle: ~");

        string summary = builder.ToString();
        text.text = summary;

        if (logToConsole)
        {
            Debug.Log("[PerformanceDebugPanel]\n" + summary, this);
        }

        frames = 0;
        elapsed = 0f;
        minFrameMs = float.MaxValue;
        maxFrameMs = 0f;
    }

    private void UpdatePing()
    {
        if (!showPing || string.IsNullOrWhiteSpace(pingAddress))
        {
            activePing = null;
            lastPingMs = -1;
            return;
        }

        if (activePing != null)
        {
            if (!activePing.isDone)
            {
                return;
            }

            lastPingMs = activePing.time >= 0 ? activePing.time : -1;
            activePing = null;
            nextPingTime = Time.unscaledTime + pingInterval;
            return;
        }

        if (Time.unscaledTime < nextPingTime)
        {
            return;
        }

        activePing = new Ping(pingAddress);
    }

    private string GetPingText()
    {
        if (!showPing || string.IsNullOrWhiteSpace(pingAddress))
        {
            return "N/A";
        }

        if (activePing != null && !activePing.isDone)
        {
            return "pending";
        }

        return lastPingMs >= 0 ? $"{lastPingMs} ms" : "N/A";
    }

    private void StartRecorders()
    {
        gcAllocRecorder = StartFirstRecorder(ProfilerCategory.Memory, "GC Allocated In Frame");
        physicsRecorder = StartFirstRecorder(ProfilerCategory.Physics, "Physics.Simulate", "Physics.Processing");
        uiRebuildRecorder = StartFirstRecorder(ProfilerCategory.Gui, "Canvas.BuildBatch", "Canvas.SendWillRenderCanvases");
        scriptsRecorder = StartFirstRecorder(ProfilerCategory.Scripts, "BehaviourUpdate", "ScriptRunBehaviourUpdate");
    }

    private void DisposeRecorders()
    {
        DisposeRecorder(ref gcAllocRecorder);
        DisposeRecorder(ref physicsRecorder);
        DisposeRecorder(ref uiRebuildRecorder);
        DisposeRecorder(ref scriptsRecorder);
    }

    private static ProfilerRecorder StartFirstRecorder(ProfilerCategory category, params string[] markerNames)
    {
        for (int i = 0; i < markerNames.Length; i++)
        {
            try
            {
                ProfilerRecorder recorder = ProfilerRecorder.StartNew(category, markerNames[i], 1);
                if (recorder.Valid)
                {
                    return recorder;
                }

                recorder.Dispose();
            }
            catch
            {
            }
        }

        return default;
    }

    private static void DisposeRecorder(ref ProfilerRecorder recorder)
    {
        if (recorder.Valid)
        {
            recorder.Dispose();
        }
    }

    private static float GetRecorderMs(ProfilerRecorder recorder)
    {
        return recorder.Valid ? recorder.LastValue / 1_000_000f : 0f;
    }

    private static float GetRecorderKb(ProfilerRecorder recorder)
    {
        return recorder.Valid ? recorder.LastValue / 1024f : 0f;
    }

    private static long ToMb(long bytes)
    {
        return bytes / (1024L * 1024L);
    }
}
