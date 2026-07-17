#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ActionEditorWindow : EditorWindow
{
    private enum HitBoxEditMode
    {
        移动,
        缩放,
        旋转
    }

    private enum VFXEditMode
    {
        移动,
        旋转,
        缩放
    }

    private const float ResizeHandleWidth = 6f;
    private const float MinEventWidth = 10f;
    private const float PointEventWidth = 12f;

    private ActionConfig config;
    private SerializedObject serializedConfig;

    private Vector2 mainScroll;
    private Vector2 timelineScroll;
    private Vector2 eventListScroll;
    private Vector2 detailScroll;

    private int selectedEventIndex = -1;

    private float previewTime;
    private float pixelsPerSecond = 520f;

    private float bottomPanelHeight = 320f;
    private float eventListPanelWidth = 500f;

    private bool isDraggingEvent;
    private int draggingEventIndex = -1;
    private float dragStartMouseX;
    private float dragStartTime;

    private bool isResizingEvent;
    private int resizingEventIndex = -1;
    private float resizeStartMouseX;
    private float resizeStartDuration;

    private GameObject previewObject;

    private bool enableScenePreview = true;
    private bool autoSampleOnTimeChange = true;

    private bool enableSceneHitBoxEdit = true;
    private bool showAllHitBoxesInScene = false;
    private HitBoxEditMode hitBoxEditMode = HitBoxEditMode.移动;

    private bool enableTimelineVFXPreview = true;
    private VFXEditMode vfxEditMode = VFXEditMode.移动;

    private GameObject previewVfxObject;
    private GameObject previewVfxPrefabSource;
    private int previewVfxEventIndex = -1;
    private float lastVFXSimulateTime = -999f;

    private bool pendingVFXPreviewRefresh;
    private bool pendingClearPreviewVFX;
    private bool pendingStopScenePreview;

    private GUIStyle eventTextStyle;
    private GUIStyle rulerTextStyle;

    private readonly Color trackBackgroundColor = new Color(0.10f, 0.10f, 0.10f);
    private readonly Color timelineBackgroundColor = new Color(0.13f, 0.13f, 0.13f);
    private readonly Color rulerBackgroundColor = new Color(0.18f, 0.18f, 0.18f);

    private readonly Color hitBoxWireColor = new Color(1f, 0.1f, 0.1f, 0.9f);
    private readonly Color inactiveHitBoxWireColor = new Color(1f, 0.4f, 0.4f, 0.35f);

    private bool animationModeStartedByThisWindow;

    private readonly ActionEventType[] trackTypes =
    {
        ActionEventType.Speed,
        ActionEventType.Movement,
        ActionEventType.VFX,
        ActionEventType.Audio,
        ActionEventType.HitBox,
        ActionEventType.HitStop
    };

    [MenuItem("Tools/Combat/动作编辑器")]
    public static void Open()
    {
        GetWindow<ActionEditorWindow>("动作编辑器");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;

        ClearPreviewVFXImmediate();

        if (animationModeStartedByThisWindow)
        {
            StopScenePreviewImmediate();
        }
    }

    private void OnGUI()
    {
        EnsureStyles();

        DrawHeader();

        if (config == null)
        {
            EditorGUILayout.HelpBox("请选择一个 ActionConfig。", MessageType.Info);
            return;
        }

        EnsureSerializedObject();

        serializedConfig.Update();

        mainScroll = EditorGUILayout.BeginScrollView(mainScroll);

        DrawPreviewToolbar();
        DrawBasicInfo();
        DrawTimeline();
        DrawBottomArea();

        EditorGUILayout.EndScrollView();

        serializedConfig.ApplyModifiedProperties();
    }

    private void EnsureStyles()
    {
        if (eventTextStyle == null)
        {
            eventTextStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                clipping = TextClipping.Clip
            };

            eventTextStyle.normal.textColor = Color.black;
        }

        if (rulerTextStyle == null)
        {
            rulerTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10
            };

            rulerTextStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        ActionConfig newConfig = (ActionConfig)EditorGUILayout.ObjectField(
            config,
            typeof(ActionConfig),
            false,
            GUILayout.Width(260)
        );

        if (newConfig != config)
        {
            SetConfig(newConfig);
        }

        if (GUILayout.Button("使用当前选中资源", EditorStyles.toolbarButton, GUILayout.Width(120)))
        {
            ActionConfig selected = Selection.activeObject as ActionConfig;

            if (selected != null)
            {
                SetConfig(selected);
            }
        }

        GUILayout.FlexibleSpace();

        if (config != null)
        {
            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void SetConfig(ActionConfig newConfig)
    {
        ClearPreviewVFXImmediate();

        config = newConfig;
        serializedConfig = config != null ? new SerializedObject(config) : null;

        selectedEventIndex = -1;
        previewTime = 0f;

        SceneView.RepaintAll();
        Repaint();
    }

    private void EnsureSerializedObject()
    {
        if (serializedConfig == null && config != null)
        {
            serializedConfig = new SerializedObject(config);
        }
    }

    //============================================================
    // Preview Toolbar
    //============================================================

    private void DrawPreviewToolbar()
    {
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("场景预览", EditorStyles.boldLabel);

        GameObject newPreviewObject = (GameObject)EditorGUILayout.ObjectField(
            "预览角色",
            previewObject,
            typeof(GameObject),
            true
        );

        if (newPreviewObject != previewObject)
        {
            previewObject = newPreviewObject;
            ClearPreviewVFX();

            if (enableScenePreview)
            {
                SamplePreview();
            }
        }

        enableScenePreview = EditorGUILayout.Toggle("启用动画预览", enableScenePreview);
        autoSampleOnTimeChange = EditorGUILayout.Toggle("拖动时间自动采样动画", autoSampleOnTimeChange);

        EditorGUILayout.Space(3);

        enableSceneHitBoxEdit = EditorGUILayout.Toggle("Scene 编辑 HitBox", enableSceneHitBoxEdit);
        showAllHitBoxesInScene = EditorGUILayout.Toggle("显示全部 HitBox", showAllHitBoxesInScene);
        hitBoxEditMode = (HitBoxEditMode)EditorGUILayout.EnumPopup("HitBox 编辑模式", hitBoxEditMode);

        EditorGUILayout.Space(3);

        enableTimelineVFXPreview = EditorGUILayout.Toggle("时间轴预览 VFX", enableTimelineVFXPreview);
        vfxEditMode = (VFXEditMode)EditorGUILayout.EnumPopup("VFX 编辑模式", vfxEditMode);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("采样当前时间"))
        {
            SamplePreview();
        }

        if (GUILayout.Button("停止动画预览"))
        {
            StopScenePreview();
        }

        if (GUILayout.Button("使用当前选中物体"))
        {
            if (Selection.activeGameObject != null)
            {
                previewObject = Selection.activeGameObject;
                ClearPreviewVFX();
                SamplePreview();
            }
        }

        if (GUILayout.Button("清理 VFX 预览"))
        {
            ClearPreviewVFX();
        }

        EditorGUILayout.EndHorizontal();

        if (previewObject == null)
        {
            EditorGUILayout.HelpBox("请选择场景中的 Player 或 Enemy 作为预览角色。", MessageType.Info);
        }
        else if (config != null && config.animationClip == null)
        {
            EditorGUILayout.HelpBox("当前 ActionConfig 没有 AnimationClip。", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    //============================================================
    // Basic Info
    //============================================================

    private void DrawBasicInfo()
    {
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("动作基础信息", EditorStyles.boldLabel);

        DrawProperty("actionId", "动作 ID");
        DrawProperty("animationClip", "动画 Clip");
        DrawProperty("useClipLength", "使用 Clip 长度");
        DrawProperty("length", "动作长度（秒）");
        DrawProperty("fadeDuration", "淡入时间");

        EditorGUILayout.Space(4);

        DrawProperty("lockMovement", "动作期间锁移动");
        DrawProperty("returnToLocomotionOnEnd", "结束后回移动状态");
        DrawProperty("exitToLocomotionDelay", "结束后回移动延迟");
        DrawProperty("returnToMoveWithRunStart", "移动时先回 RunStart");

        EditorGUILayout.Space(4);

        DrawProperty("enableCombo", "允许连招");
        DrawProperty("nextComboAction", "下一段连招");
        DrawProperty("comboInputStartTime", "连招输入开始时间");
        DrawProperty("comboInputEndTime", "连招输入结束时间");
        DrawProperty("comboCancelTime", "连招切换时间");

        EditorGUILayout.EndVertical();
    }

    private void DrawProperty(string propertyName, string label)
    {
        SerializedProperty property = serializedConfig.FindProperty(propertyName);

        if (property == null)
        {
            return;
        }

        EditorGUILayout.PropertyField(property, new GUIContent(label), true);
    }

    //============================================================
    // Timeline
    //============================================================

    private void DrawTimeline()
    {
        float actionLength = GetActionLength();
        previewTime = Mathf.Clamp(previewTime, 0f, actionLength);

        EditorGUILayout.Space(6);

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("时间轴", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("当前时间", GUILayout.Width(60));

        float newPreviewTime = EditorGUILayout.Slider(
            previewTime,
            0f,
            actionLength,
            GUILayout.MinWidth(200)
        );

        if (!Mathf.Approximately(newPreviewTime, previewTime))
        {
            previewTime = newPreviewTime;

            if (autoSampleOnTimeChange)
            {
                SamplePreview();
            }

            RequestVFXPreviewRefresh();
        }

        EditorGUILayout.LabelField($"{previewTime:F3}s / {actionLength:F3}s", GUILayout.Width(120));

        pixelsPerSecond = EditorGUILayout.Slider(
            "缩放",
            pixelsPerSecond,
            250f,
            1000f
        );

        EditorGUILayout.EndHorizontal();

        float leftLabelWidth = 90f;
        float trackHeight = 28f;
        float rulerHeight = 24f;

        float timelineContentWidth = Mathf.Max(
            position.width - leftLabelWidth - 60f,
            actionLength * pixelsPerSecond
        );

        float totalHeight = rulerHeight + trackTypes.Length * trackHeight + 20f;

        timelineScroll = EditorGUILayout.BeginScrollView(
            timelineScroll,
            true,
            false,
            GUILayout.Height(totalHeight + 20f)
        );

        Rect outerRect = GUILayoutUtility.GetRect(
            leftLabelWidth + timelineContentWidth + 20f,
            totalHeight
        );

        EditorGUI.DrawRect(outerRect, timelineBackgroundColor);

        Rect contentRect = new Rect(
            outerRect.x + leftLabelWidth,
            outerRect.y,
            timelineContentWidth,
            outerRect.height
        );

        DrawRuler(contentRect, actionLength, rulerHeight);

        for (int i = 0; i < trackTypes.Length; i++)
        {
            float y = outerRect.y + rulerHeight + i * trackHeight;

            DrawTrackLabel(
                outerRect.x,
                y,
                leftLabelWidth,
                trackHeight,
                trackTypes[i]
            );

            DrawTrack(
                contentRect.x,
                y,
                timelineContentWidth,
                trackHeight,
                actionLength,
                trackTypes[i]
            );
        }

        DrawPreviewLine(
            contentRect,
            actionLength,
            rulerHeight,
            trackTypes.Length * trackHeight
        );

        HandleEventDragOrResize(actionLength, timelineContentWidth);

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawRuler(Rect contentRect, float actionLength, float rulerHeight)
    {
        Rect rulerRect = new Rect(contentRect.x, contentRect.y, contentRect.width, rulerHeight);
        EditorGUI.DrawRect(rulerRect, rulerBackgroundColor);

        int tickCount = Mathf.CeilToInt(actionLength / 0.1f);

        for (int i = 0; i <= tickCount; i++)
        {
            float time = i * 0.1f;

            if (time > actionLength)
            {
                time = actionLength;
            }

            float x = TimeToX(time, contentRect.x, contentRect.width, actionLength);
            float tickHeight = i % 5 == 0 ? 18f : 10f;

            EditorGUI.DrawRect(
                new Rect(x, rulerRect.yMax - tickHeight, 1f, tickHeight),
                Color.gray
            );

            if (i % 5 == 0 || Mathf.Approximately(time, actionLength))
            {
                EditorGUI.LabelField(
                    new Rect(x + 2f, rulerRect.y + 2f, 60f, 18f),
                    $"{time:F1}",
                    rulerTextStyle
                );
            }

            if (Mathf.Approximately(time, actionLength))
            {
                break;
            }
        }
    }

    private void DrawTrackLabel(
        float x,
        float y,
        float width,
        float height,
        ActionEventType type
    )
    {
        Rect rect = new Rect(x, y, width, height);
        EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f));

        GUI.Label(
            new Rect(x + 8f, y + 5f, width - 8f, height),
            GetTrackChineseName(type)
        );
    }

    private void DrawTrack(
        float startX,
        float y,
        float width,
        float height,
        float actionLength,
        ActionEventType type
    )
    {
        Rect trackRect = new Rect(startX, y, width, height);
        EditorGUI.DrawRect(trackRect, trackBackgroundColor);

        if (config.events == null)
        {
            return;
        }

        for (int i = 0; i < config.events.Count; i++)
        {
            ActionEventData actionEvent = config.events[i];

            if (actionEvent == null || actionEvent.type != type)
            {
                continue;
            }

            DrawEventBlock(i, actionEvent, startX, y, width, height, actionLength);
        }
    }

    private void DrawEventBlock(
        int index,
        ActionEventData actionEvent,
        float startX,
        float y,
        float width,
        float height,
        float actionLength
    )
    {
        float x = TimeToX(actionEvent.startTime, startX, width, actionLength);

        float eventWidth = actionEvent.duration > 0f
            ? Mathf.Max(MinEventWidth, actionEvent.duration / actionLength * width)
            : PointEventWidth;

        Rect eventRect = new Rect(
            x,
            y + 4f,
            eventWidth,
            height - 8f
        );

        Color color = GetEventColor(actionEvent.type);

        if (!actionEvent.enabled)
        {
            color = new Color(color.r, color.g, color.b, 0.35f);
        }

        if (index == selectedEventIndex)
        {
            EditorGUI.DrawRect(
                new Rect(
                    eventRect.x - 2f,
                    eventRect.y - 2f,
                    eventRect.width + 4f,
                    eventRect.height + 4f
                ),
                Color.black
            );
        }

        EditorGUI.DrawRect(eventRect, color);

        string eventLabel = string.IsNullOrEmpty(actionEvent.eventName)
            ? actionEvent.type.ToString()
            : actionEvent.eventName;

        Rect labelRect = new Rect(
            eventRect.x + 4f,
            eventRect.y + 1f,
            Mathf.Max(0f, eventRect.width - 10f),
            eventRect.height - 2f
        );

        EditorGUI.DrawRect(
            new Rect(
                labelRect.x - 2f,
                labelRect.y + 2f,
                labelRect.width + 2f,
                labelRect.height - 4f
            ),
            new Color(1f, 1f, 1f, 0.08f)
        );

        EditorGUI.LabelField(labelRect, eventLabel, eventTextStyle);

        if (actionEvent.duration > 0f)
        {
            Rect resizeRect = new Rect(
                eventRect.xMax - ResizeHandleWidth,
                eventRect.y,
                ResizeHandleWidth,
                eventRect.height
            );

            EditorGUI.DrawRect(resizeRect, new Color(1f, 1f, 1f, 0.35f));
            EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeHorizontal);
        }

        HandleEventBlockInput(eventRect, index);
    }

    private void HandleEventBlockInput(Rect eventRect, int eventIndex)
    {
        Event e = Event.current;

        if (e.type != EventType.MouseDown || e.button != 0)
        {
            return;
        }

        if (!eventRect.Contains(e.mousePosition))
        {
            return;
        }

        SelectEvent(eventIndex);

        ActionEventData actionEvent = config.events[eventIndex];

        Rect resizeRect = new Rect(
            eventRect.xMax - ResizeHandleWidth,
            eventRect.y,
            ResizeHandleWidth,
            eventRect.height
        );

        bool clickedResizeHandle =
            actionEvent.duration > 0f &&
            resizeRect.Contains(e.mousePosition);

        if (clickedResizeHandle)
        {
            isResizingEvent = true;
            resizingEventIndex = eventIndex;
            resizeStartMouseX = e.mousePosition.x;
            resizeStartDuration = actionEvent.duration;
        }
        else
        {
            isDraggingEvent = true;
            draggingEventIndex = eventIndex;
            dragStartMouseX = e.mousePosition.x;
            dragStartTime = actionEvent.startTime;
        }

        GUI.FocusControl(null);
        e.Use();
        Repaint();
    }

    private void HandleEventDragOrResize(float actionLength, float width)
    {
        HandleEventDrag(actionLength, width);
        HandleEventResize(actionLength, width);
    }

    private void HandleEventDrag(float actionLength, float width)
    {
        Event e = Event.current;

        if (!isDraggingEvent)
        {
            return;
        }

        if (draggingEventIndex < 0 || draggingEventIndex >= config.events.Count)
        {
            isDraggingEvent = false;
            draggingEventIndex = -1;
            return;
        }

        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            float deltaX = e.mousePosition.x - dragStartMouseX;
            float deltaTime = deltaX / width * actionLength;

            ActionEventData actionEvent = config.events[draggingEventIndex];

            Undo.RecordObject(config, "拖动动作事件");

            actionEvent.startTime = Mathf.Clamp(
                dragStartTime + deltaTime,
                0f,
                Mathf.Max(0f, actionLength - actionEvent.duration)
            );

            MarkConfigDirtyOnly();

            e.Use();
            Repaint();
        }

        if (e.type == EventType.MouseUp && e.button == 0)
        {
            isDraggingEvent = false;
            draggingEventIndex = -1;
            RequestVFXPreviewRefresh();
            e.Use();
        }
    }

    private void HandleEventResize(float actionLength, float width)
    {
        Event e = Event.current;

        if (!isResizingEvent)
        {
            return;
        }

        if (resizingEventIndex < 0 || resizingEventIndex >= config.events.Count)
        {
            isResizingEvent = false;
            resizingEventIndex = -1;
            return;
        }

        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            float deltaX = e.mousePosition.x - resizeStartMouseX;
            float deltaTime = deltaX / width * actionLength;

            ActionEventData actionEvent = config.events[resizingEventIndex];

            Undo.RecordObject(config, "调整事件持续时间");

            float maxDuration = Mathf.Max(0f, actionLength - actionEvent.startTime);

            actionEvent.duration = Mathf.Clamp(
                resizeStartDuration + deltaTime,
                0f,
                maxDuration
            );

            MarkConfigDirtyOnly();

            e.Use();
            Repaint();
        }

        if (e.type == EventType.MouseUp && e.button == 0)
        {
            isResizingEvent = false;
            resizingEventIndex = -1;
            RequestVFXPreviewRefresh();
            e.Use();
        }
    }

    private void DrawPreviewLine(
        Rect contentRect,
        float actionLength,
        float rulerHeight,
        float tracksHeight
    )
    {
        float x = TimeToX(previewTime, contentRect.x, contentRect.width, actionLength);

        Rect lineRect = new Rect(
            x,
            contentRect.y,
            2f,
            rulerHeight + tracksHeight
        );

        EditorGUI.DrawRect(lineRect, Color.green);
    }

    private float TimeToX(float time, float startX, float width, float actionLength)
    {
        if (actionLength <= 0f)
        {
            return startX;
        }

        return startX + Mathf.Clamp01(time / actionLength) * width;
    }

    private float GetActionLength()
    {
        if (config == null)
        {
            return 1f;
        }

        return Mathf.Max(0.01f, config.GetLength());
    }

    //============================================================
    // Bottom Panels
    //============================================================

    private void DrawBottomArea()
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.BeginHorizontal(
            GUILayout.MinHeight(bottomPanelHeight),
            GUILayout.ExpandHeight(true)
        );

        DrawEventListPanel();

        GUILayout.Space(8);

        DrawEventDetailPanel();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawEventListPanel()
    {
        EditorGUILayout.BeginVertical(
            "box",
            GUILayout.Width(eventListPanelWidth),
            GUILayout.MinHeight(bottomPanelHeight),
            GUILayout.ExpandHeight(true)
        );

        EditorGUILayout.LabelField("事件列表", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("添加 VFX"))
        {
            AddEvent(ActionEventType.VFX);
        }

        if (GUILayout.Button("添加 HitBox"))
        {
            AddEvent(ActionEventType.HitBox);
        }

        if (GUILayout.Button("添加音效"))
        {
            AddEvent(ActionEventType.Audio);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("添加 Speed"))
        {
            AddEvent(ActionEventType.Speed);
        }

        if (GUILayout.Button("添加 Movement"))
        {
            AddEvent(ActionEventType.Movement);
        }

        if (GUILayout.Button("添加 HitStop"))
        {
            AddEvent(ActionEventType.HitStop);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("按时间排序"))
        {
            Undo.RecordObject(config, "按时间排序事件");
            config.SortEventsByTime();
            MarkConfigDirtyAndRefresh();
        }

        if (GUILayout.Button("删除选中"))
        {
            DeleteSelectedEvent();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        eventListScroll = EditorGUILayout.BeginScrollView(
            eventListScroll,
            GUILayout.ExpandHeight(true)
        );

        if (config.events != null)
        {
            for (int i = 0; i < config.events.Count; i++)
            {
                DrawEventListItem(i);
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawEventListItem(int index)
    {
        ActionEventData actionEvent = config.events[index];

        if (actionEvent == null)
        {
            return;
        }

        GUIStyle rowStyle = index == selectedEventIndex ? GUI.skin.box : GUIStyle.none;

        EditorGUILayout.BeginHorizontal(rowStyle);

        if (GUILayout.Button($"{index}", GUILayout.Width(28)))
        {
            SelectEvent(index);
        }

        bool newEnabled = EditorGUILayout.Toggle(actionEvent.enabled, GUILayout.Width(20));

        if (newEnabled != actionEvent.enabled)
        {
            Undo.RecordObject(config, "切换事件启用");
            actionEvent.enabled = newEnabled;
            MarkConfigDirtyAndRefresh();
        }

        string label =
            $"{GetTrackChineseName(actionEvent.type)}  {actionEvent.startTime:F3}s";

        if (!string.IsNullOrEmpty(actionEvent.eventName))
        {
            label += $"  {actionEvent.eventName}";
        }

        if (GUILayout.Button(label, EditorStyles.label))
        {
            SelectEvent(index);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void SelectEvent(int index)
    {
        if (config == null || config.events == null)
        {
            selectedEventIndex = -1;
            ClearPreviewVFX();
            return;
        }

        if (index < 0 || index >= config.events.Count)
        {
            selectedEventIndex = -1;
            ClearPreviewVFX();
            return;
        }

        bool changed = selectedEventIndex != index;

        selectedEventIndex = index;

        GUI.FocusControl(null);

        if (changed)
        {
            ClearPreviewVFX();
        }

        RequestVFXPreviewRefresh();

        SceneView.RepaintAll();
        Repaint();
    }

    private void DrawEventDetailPanel()
    {
        EditorGUILayout.BeginVertical(
            "box",
            GUILayout.MinHeight(bottomPanelHeight),
            GUILayout.ExpandHeight(true)
        );

        EditorGUILayout.LabelField("事件详情", EditorStyles.boldLabel);

        if (selectedEventIndex < 0 || config.events == null || selectedEventIndex >= config.events.Count)
        {
            EditorGUILayout.HelpBox("请选择一个事件。", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        SerializedProperty eventsProperty = serializedConfig.FindProperty("events");

        if (eventsProperty == null)
        {
            EditorGUILayout.HelpBox("找不到 events 字段。", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        SerializedProperty eventProperty = eventsProperty.GetArrayElementAtIndex(selectedEventIndex);

        detailScroll = EditorGUILayout.BeginScrollView(
            detailScroll,
            GUILayout.ExpandHeight(true)
        );

        DrawRelativeProperty(eventProperty, "enabled", "启用");
        DrawRelativeProperty(eventProperty, "eventName", "事件名");
        DrawRelativeProperty(eventProperty, "type", "事件类型");
        DrawRelativeProperty(eventProperty, "startTime", "开始时间");
        DrawRelativeProperty(eventProperty, "duration", "持续时间");

        EditorGUILayout.Space(6);

        ActionEventData selectedEvent = config.events[selectedEventIndex];
        EnsureEventSubData(selectedEvent);

        switch (selectedEvent.type)
        {
            case ActionEventType.VFX:
                DrawRelativeProperty(eventProperty, "vfx", "VFX 参数", true);
                break;

            case ActionEventType.HitBox:
                DrawRelativeProperty(eventProperty, "hitBox", "HitBox 参数", true);
                break;

            case ActionEventType.Audio:
                DrawRelativeProperty(eventProperty, "audio", "音效参数", true);
                break;

            case ActionEventType.HitStop:
                DrawRelativeProperty(eventProperty, "hitStop", "HitStop 参数", true);
                break;

            case ActionEventType.Speed:
                DrawRelativeProperty(eventProperty, "speed", "Speed 参数", true);
                break;

            case ActionEventType.Movement:
                DrawRelativeProperty(eventProperty, "movement", "Movement 参数", true);
                break;
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("复制事件"))
        {
            DuplicateSelectedEvent();
        }

        if (GUILayout.Button("删除事件"))
        {
            DeleteSelectedEvent();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawRelativeProperty(
        SerializedProperty parent,
        string relativeName,
        string label,
        bool includeChildren = false
    )
    {
        SerializedProperty property = parent.FindPropertyRelative(relativeName);

        if (property == null)
        {
            return;
        }

        EditorGUILayout.PropertyField(property, new GUIContent(label), includeChildren);
    }

    //============================================================
    // Add / Delete / Duplicate
    //============================================================

    private void AddEvent(ActionEventType type)
    {
        if (config.events == null)
        {
            Undo.RecordObject(config, "创建事件列表");
            config.events = new List<ActionEventData>();
        }

        Undo.RecordObject(config, "添加动作事件");

        ActionEventData newEvent = new ActionEventData
        {
            enabled = true,
            eventName = GetDefaultEventName(type),
            type = type,
            startTime = previewTime,
            duration = GetDefaultDuration(type),
            vfx = new VFXEventData(),
            hitBox = new HitBoxEventData(),
            hitStop = new HitStopEventData(),
            speed = new SpeedEventData(),
            movement = new MovementEventData(),
            audio = new AudioEventData()
        };

        config.events.Add(newEvent);

        MarkConfigDirtyOnly();

        SelectEvent(config.events.Count - 1);
    }

    private void DuplicateSelectedEvent()
    {
        if (selectedEventIndex < 0 || selectedEventIndex >= config.events.Count)
        {
            return;
        }

        SerializedProperty eventsProperty = serializedConfig.FindProperty("events");

        if (eventsProperty == null)
        {
            return;
        }

        Undo.RecordObject(config, "复制动作事件");

        int insertIndex = selectedEventIndex + 1;

        eventsProperty.InsertArrayElementAtIndex(selectedEventIndex);
        serializedConfig.ApplyModifiedProperties();

        selectedEventIndex = insertIndex;

        if (selectedEventIndex >= 0 && selectedEventIndex < config.events.Count)
        {
            config.events[selectedEventIndex].startTime += 0.05f;
        }

        MarkConfigDirtyAndRefresh();

        SceneView.RepaintAll();
        Repaint();
    }

    private void DeleteSelectedEvent()
    {
        if (selectedEventIndex < 0 || config.events == null || selectedEventIndex >= config.events.Count)
        {
            return;
        }

        Undo.RecordObject(config, "删除动作事件");

        config.events.RemoveAt(selectedEventIndex);

        selectedEventIndex = Mathf.Clamp(selectedEventIndex, -1, config.events.Count - 1);

        ClearPreviewVFX();

        MarkConfigDirtyOnly();

        SceneView.RepaintAll();
        Repaint();
    }

    private void EnsureEventSubData(ActionEventData actionEvent)
    {
        if (actionEvent == null)
        {
            return;
        }

        if (actionEvent.movement == null)
        {
            actionEvent.movement = new MovementEventData();
        }

        if (actionEvent.vfx == null)
        {
            actionEvent.vfx = new VFXEventData();
        }

        if (actionEvent.hitBox == null)
        {
            actionEvent.hitBox = new HitBoxEventData();
        }

        if (actionEvent.hitStop == null)
        {
            actionEvent.hitStop = new HitStopEventData();
        }

        if (actionEvent.speed == null)
        {
            actionEvent.speed = new SpeedEventData();
        }

        if (actionEvent.audio == null)
        {
            actionEvent.audio = new AudioEventData();
        }
    }

    //============================================================
    // Scene Animation Preview
    //============================================================

    private void SamplePreview()
    {
        if (!enableScenePreview)
        {
            return;
        }

        if (previewObject == null || config == null || config.animationClip == null)
        {
            return;
        }

        if (!AnimationMode.InAnimationMode())
        {
            AnimationMode.StartAnimationMode();
            animationModeStartedByThisWindow = true;
        }

        float sampleTime = Mathf.Clamp(previewTime, 0f, GetActionLength());

        AnimationMode.SampleAnimationClip(previewObject, config.animationClip, sampleTime);

        RequestVFXPreviewRefresh();

        SceneView.RepaintAll();
        Repaint();
    }

    private void StopScenePreview()
    {
        if (!animationModeStartedByThisWindow)
        {
            return;
        }

        if (pendingStopScenePreview)
        {
            return;
        }

        pendingStopScenePreview = true;

        EditorApplication.delayCall += () =>
        {
            pendingStopScenePreview = false;

            if (this == null)
            {
                return;
            }

            StopScenePreviewImmediate();
        };
    }

    private void StopScenePreviewImmediate()
    {
        if (animationModeStartedByThisWindow && AnimationMode.InAnimationMode())
        {
            AnimationMode.StopAnimationMode();
        }

        animationModeStartedByThisWindow = false;

        SceneView.RepaintAll();

        if (this != null)
        {
            Repaint();
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (previewObject == null || config == null)
        {
            return;
        }

        if (enableSceneHitBoxEdit)
        {
            DrawHitBoxSceneGUI();
        }

        DrawVFXSceneGUI();
    }

    //============================================================
    // Scene HitBox GUI
    //============================================================

    private void DrawHitBoxSceneGUI()
    {
        if (previewObject == null || config == null || config.events == null)
        {
            return;
        }

        if (showAllHitBoxesInScene)
        {
            for (int i = 0; i < config.events.Count; i++)
            {
                ActionEventData actionEvent = config.events[i];

                if (actionEvent == null ||
                    !actionEvent.enabled ||
                    actionEvent.type != ActionEventType.HitBox)
                {
                    continue;
                }

                bool editable = i == selectedEventIndex;
                DrawSingleHitBoxSceneGUI(i, actionEvent.hitBox, editable);
            }

            return;
        }

        if (selectedEventIndex < 0 || selectedEventIndex >= config.events.Count)
        {
            return;
        }

        ActionEventData selectedEvent = config.events[selectedEventIndex];

        if (selectedEvent == null || selectedEvent.type != ActionEventType.HitBox)
        {
            return;
        }

        DrawSingleHitBoxSceneGUI(selectedEventIndex, selectedEvent.hitBox, true);
    }

    private void DrawSingleHitBoxSceneGUI(
        int eventIndex,
        HitBoxEventData hitBox,
        bool editable
    )
    {
        if (hitBox == null || previewObject == null)
        {
            return;
        }

        Transform root = previewObject.transform;

        Vector3 worldCenter = root.TransformPoint(hitBox.localOffset);
        Quaternion worldRotation = root.rotation * Quaternion.Euler(hitBox.localEulerAngles);

        Handles.color = editable ? hitBoxWireColor : inactiveHitBoxWireColor;

        switch (hitBox.shape)
        {
            case HitShapeType.Box:
                DrawBoxHitBox(hitBox, root, worldCenter, worldRotation, editable);
                break;

            case HitShapeType.Sphere:
                DrawSphereHitBox(hitBox, root, worldCenter, editable);
                break;

            case HitShapeType.Capsule:
                DrawCapsuleHitBox(hitBox, worldCenter, worldRotation);
                break;

            case HitShapeType.Sector:
                DrawSectorHitBox(hitBox, worldCenter, worldRotation);
                break;
        }

        Handles.Label(
            worldCenter + Vector3.up * 0.25f,
            editable ? $"选中 HitBox：{eventIndex}" : $"HitBox：{eventIndex}"
        );
    }

    private void DrawBoxHitBox(
        HitBoxEventData hitBox,
        Transform root,
        Vector3 worldCenter,
        Quaternion worldRotation,
        bool editable
    )
    {
        Matrix4x4 oldMatrix = Handles.matrix;

        Handles.matrix = Matrix4x4.TRS(worldCenter, worldRotation, Vector3.one);
        Handles.color = hitBoxWireColor;
        Handles.DrawWireCube(Vector3.zero, hitBox.size);
        Handles.matrix = oldMatrix;

        if (!editable)
        {
            return;
        }

        if (hitBoxEditMode == HitBoxEditMode.移动)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 newWorldCenter = Handles.PositionHandle(worldCenter, worldRotation);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "修改 HitBox 位置");

                hitBox.localOffset = root.InverseTransformPoint(newWorldCenter);

                MarkConfigDirtyAndRefresh();
            }
        }
        else if (hitBoxEditMode == HitBoxEditMode.旋转)
        {
            EditorGUI.BeginChangeCheck();

            Quaternion newWorldRotation = Handles.RotationHandle(worldRotation, worldCenter);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "修改 HitBox 旋转");

                Quaternion localRotation = Quaternion.Inverse(root.rotation) * newWorldRotation;
                hitBox.localEulerAngles = localRotation.eulerAngles;

                MarkConfigDirtyAndRefresh();
            }
        }
        else if (hitBoxEditMode == HitBoxEditMode.缩放)
        {
            float handleSize = HandleUtility.GetHandleSize(worldCenter) * 1.0f;

            EditorGUI.BeginChangeCheck();

            Vector3 newSize = Handles.ScaleHandle(
                hitBox.size,
                worldCenter,
                worldRotation,
                handleSize
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "修改 HitBox 大小");

                hitBox.size = new Vector3(
                    Mathf.Max(0.01f, newSize.x),
                    Mathf.Max(0.01f, newSize.y),
                    Mathf.Max(0.01f, newSize.z)
                );

                MarkConfigDirtyAndRefresh();
            }
        }
    }

    private void DrawSphereHitBox(
        HitBoxEventData hitBox,
        Transform root,
        Vector3 worldCenter,
        bool editable
    )
    {
        Handles.color = hitBoxWireColor;
        Handles.DrawWireDisc(worldCenter, Vector3.up, hitBox.radius);
        Handles.DrawWireDisc(worldCenter, Vector3.right, hitBox.radius);
        Handles.DrawWireDisc(worldCenter, Vector3.forward, hitBox.radius);

        if (!editable)
        {
            return;
        }

        if (hitBoxEditMode == HitBoxEditMode.移动)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 newWorldCenter = Handles.PositionHandle(
                worldCenter,
                Quaternion.identity
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "修改 HitBox 位置");

                hitBox.localOffset = root.InverseTransformPoint(newWorldCenter);

                MarkConfigDirtyAndRefresh();
            }
        }
        else if (hitBoxEditMode == HitBoxEditMode.缩放)
        {
            EditorGUI.BeginChangeCheck();

            float newRadius = Handles.RadiusHandle(
                Quaternion.identity,
                worldCenter,
                hitBox.radius
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "修改 HitBox 半径");

                hitBox.radius = Mathf.Max(0.01f, newRadius);

                MarkConfigDirtyAndRefresh();
            }
        }
    }

    private void DrawCapsuleHitBox(
        HitBoxEventData hitBox,
        Vector3 worldCenter,
        Quaternion worldRotation
    )
    {
        float radius = Mathf.Max(0.01f, hitBox.radius);
        float height = Mathf.Max(hitBox.capsuleHeight, radius * 2f);

        float halfLineHeight = Mathf.Max(0f, height * 0.5f - radius);

        Vector3 up = worldRotation * Vector3.up;

        Vector3 pointA = worldCenter + up * halfLineHeight;
        Vector3 pointB = worldCenter - up * halfLineHeight;

        Handles.color = hitBoxWireColor;

        Handles.DrawWireDisc(pointA, up, radius);
        Handles.DrawWireDisc(pointB, up, radius);

        Vector3 right = worldRotation * Vector3.right;
        Vector3 forward = worldRotation * Vector3.forward;

        Handles.DrawLine(pointA + right * radius, pointB + right * radius);
        Handles.DrawLine(pointA - right * radius, pointB - right * radius);
        Handles.DrawLine(pointA + forward * radius, pointB + forward * radius);
        Handles.DrawLine(pointA - forward * radius, pointB - forward * radius);
    }

    private void DrawSectorHitBox(
        HitBoxEventData hitBox,
        Vector3 worldCenter,
        Quaternion worldRotation
    )
    {
        Vector3 forward = worldRotation * Vector3.forward;

        Quaternion leftRot = Quaternion.AngleAxis(
            -hitBox.sectorAngle * 0.5f,
            Vector3.up
        );

        Quaternion rightRot = Quaternion.AngleAxis(
            hitBox.sectorAngle * 0.5f,
            Vector3.up
        );

        Vector3 leftDir = leftRot * forward;
        Vector3 rightDir = rightRot * forward;

        Handles.color = hitBoxWireColor;

        Handles.DrawWireArc(
            worldCenter,
            Vector3.up,
            leftDir,
            hitBox.sectorAngle,
            hitBox.radius
        );

        Handles.DrawLine(worldCenter, worldCenter + leftDir * hitBox.radius);
        Handles.DrawLine(worldCenter, worldCenter + rightDir * hitBox.radius);
    }

    //============================================================
    // VFX Timeline Preview
    //============================================================

    private void RequestVFXPreviewRefresh()
    {
        if (pendingVFXPreviewRefresh)
        {
            return;
        }

        pendingVFXPreviewRefresh = true;

        EditorApplication.delayCall += () =>
        {
            pendingVFXPreviewRefresh = false;

            if (this == null)
            {
                return;
            }

            UpdateSelectedVFXPreviewByTimeline();

            SceneView.RepaintAll();
            Repaint();
        };
    }

    private void RequestClearPreviewVFX()
    {
        if (pendingClearPreviewVFX)
        {
            return;
        }

        pendingClearPreviewVFX = true;

        EditorApplication.delayCall += () =>
        {
            pendingClearPreviewVFX = false;

            if (this == null)
            {
                return;
            }

            ClearPreviewVFXImmediate();

            SceneView.RepaintAll();
            Repaint();
        };
    }

    private void UpdateSelectedVFXPreviewByTimeline()
    {
        if (!enableTimelineVFXPreview)
        {
            return;
        }

        if (config == null || previewObject == null || config.events == null)
        {
            return;
        }

        if (selectedEventIndex < 0 || selectedEventIndex >= config.events.Count)
        {
            ClearPreviewVFXImmediate();
            return;
        }

        ActionEventData actionEvent = config.events[selectedEventIndex];

        if (actionEvent == null || actionEvent.type != ActionEventType.VFX)
        {
            ClearPreviewVFXImmediate();
            return;
        }

        VFXEventData data = actionEvent.vfx;

        if (data == null || data.prefab == null)
        {
            ClearPreviewVFXImmediate();
            return;
        }

        EnsurePreviewVFX(data);

        if (previewVfxObject == null)
        {
            return;
        }

        SetupPreviewVFXTransform(data);

        float localTime = previewTime - actionEvent.startTime;

        float previewLifeTime = data.destroyDelay > 0f
            ? data.destroyDelay
            : 3f;

        bool shouldShow = localTime >= 0f && localTime <= previewLifeTime;

        previewVfxObject.SetActive(shouldShow);

        if (!shouldShow)
        {
            return;
        }

        if (Mathf.Abs(localTime - lastVFXSimulateTime) < 0.005f)
        {
            return;
        }

        lastVFXSimulateTime = localTime;

        SimulatePreviewVFX(localTime);
    }

    private void EnsurePreviewVFX(VFXEventData data)
    {
        if (data == null || data.prefab == null)
        {
            return;
        }

        bool needCreate =
            previewVfxObject == null ||
            previewVfxEventIndex != selectedEventIndex ||
            previewVfxPrefabSource != data.prefab;

        if (!needCreate)
        {
            return;
        }

        ClearPreviewVFXImmediate();

        previewVfxPrefabSource = data.prefab;
        previewVfxEventIndex = selectedEventIndex;
        lastVFXSimulateTime = -999f;

        previewVfxObject = (GameObject)PrefabUtility.InstantiatePrefab(data.prefab);

        if (previewVfxObject == null)
        {
            previewVfxObject = Instantiate(data.prefab);
        }

        previewVfxObject.name = "[Preview] " + data.prefab.name;
        previewVfxObject.hideFlags = HideFlags.DontSave;
    }

    private void SetupPreviewVFXTransform(VFXEventData data)
    {
        if (previewVfxObject == null || previewObject == null || data == null)
        {
            return;
        }

        Transform reference = GetPreviewReferenceTransform(data.bindPointName);

        Vector3 worldPosition = reference.TransformPoint(data.localPosition);
        Quaternion worldRotation = reference.rotation * Quaternion.Euler(data.localEulerAngles);

        previewVfxObject.transform.SetParent(null);
        previewVfxObject.transform.position = worldPosition;
        previewVfxObject.transform.rotation = worldRotation;
        previewVfxObject.transform.localScale = data.localScale;

        if (data.attachToBindPoint && reference != previewObject.transform)
        {
            previewVfxObject.transform.SetParent(reference, true);
        }
    }

    private void SimulatePreviewVFX(float localTime)
    {
        if (previewVfxObject == null)
        {
            return;
        }

        ParticleSystem[] particleSystems =
            previewVfxObject.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Simulate(localTime, true, true, true);
        }

        SceneView.RepaintAll();
    }

    private void ClearPreviewVFX()
    {
        RequestClearPreviewVFX();
    }

    private void ClearPreviewVFXImmediate()
    {
        if (previewVfxObject != null)
        {
            DestroyImmediate(previewVfxObject);
        }

        previewVfxObject = null;
        previewVfxPrefabSource = null;
        previewVfxEventIndex = -1;
        lastVFXSimulateTime = -999f;
    }

    private void DrawVFXSceneGUI()
    {
        if (previewObject == null || config == null || config.events == null)
        {
            return;
        }

        if (selectedEventIndex < 0 || selectedEventIndex >= config.events.Count)
        {
            return;
        }

        ActionEventData actionEvent = config.events[selectedEventIndex];

        if (actionEvent == null || actionEvent.type != ActionEventType.VFX)
        {
            return;
        }

        VFXEventData data = actionEvent.vfx;

        if (data == null)
        {
            return;
        }

        Transform reference = GetPreviewReferenceTransform(data.bindPointName);

        Vector3 worldPosition = reference.TransformPoint(data.localPosition);
        Quaternion worldRotation = reference.rotation * Quaternion.Euler(data.localEulerAngles);

        Handles.Label(
            worldPosition + Vector3.up * 0.25f,
            $"VFX：{actionEvent.eventName}"
        );

        if (vfxEditMode == VFXEditMode.移动)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 newWorldPosition = Handles.PositionHandle(
                worldPosition,
                worldRotation
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "修改 VFX 位置");

                data.localPosition = reference.InverseTransformPoint(newWorldPosition);

                MarkConfigDirtyAndRefresh();
                SetupPreviewVFXTransform(data);
            }
        }
        else if (vfxEditMode == VFXEditMode.旋转)
        {
            EditorGUI.BeginChangeCheck();

            Quaternion newWorldRotation = Handles.RotationHandle(
                worldRotation,
                worldPosition
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "修改 VFX 旋转");

                Quaternion localRotation = Quaternion.Inverse(reference.rotation) * newWorldRotation;
                data.localEulerAngles = localRotation.eulerAngles;

                MarkConfigDirtyAndRefresh();
                SetupPreviewVFXTransform(data);
            }
        }
        else if (vfxEditMode == VFXEditMode.缩放)
        {
            float handleSize = HandleUtility.GetHandleSize(worldPosition) * 1.0f;

            EditorGUI.BeginChangeCheck();

            Vector3 newScale = Handles.ScaleHandle(
                data.localScale,
                worldPosition,
                worldRotation,
                handleSize
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "修改 VFX 大小");

                data.localScale = new Vector3(
                    Mathf.Max(0.01f, newScale.x),
                    Mathf.Max(0.01f, newScale.y),
                    Mathf.Max(0.01f, newScale.z)
                );

                MarkConfigDirtyAndRefresh();
                SetupPreviewVFXTransform(data);
            }
        }
    }

    private Transform GetPreviewReferenceTransform(string bindPointName)
    {
        Transform bindPoint = FindPreviewBindPoint(bindPointName);

        if (bindPoint != null)
        {
            return bindPoint;
        }

        return previewObject.transform;
    }

    private Transform FindPreviewBindPoint(string bindPointName)
    {
        if (previewObject == null || string.IsNullOrEmpty(bindPointName))
        {
            return null;
        }

        Transform[] children = previewObject.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == bindPointName)
            {
                return children[i];
            }
        }

        return null;
    }

    //============================================================
    // Helpers
    //============================================================

    private void MarkConfigDirtyOnly()
    {
        EditorUtility.SetDirty(config);
        serializedConfig?.Update();
    }

    private void MarkConfigDirtyAndRefresh()
    {
        MarkConfigDirtyOnly();

        RequestVFXPreviewRefresh();

        SceneView.RepaintAll();
        Repaint();
    }

    private float GetDefaultDuration(ActionEventType type)
    {
        switch (type)
        {
            case ActionEventType.HitBox:
                return 0.12f;

            case ActionEventType.Speed:
                return 0.2f;

            case ActionEventType.Movement:
                return 0.2f;

            default:
                return 0f;
        }
    }

    private string GetDefaultEventName(ActionEventType type)
    {
        switch (type)
        {
            case ActionEventType.VFX:
                return "新特效事件";

            case ActionEventType.HitBox:
                return "新攻击判定";

            case ActionEventType.HitStop:
                return "新顿帧事件";

            case ActionEventType.Speed:
                return "新速度事件";

            case ActionEventType.Movement:
                return "新位移事件";

            case ActionEventType.Audio:
                return "新音效事件";

            default:
                return "新事件";
        }
    }

    private string GetTrackChineseName(ActionEventType type)
    {
        switch (type)
        {
            case ActionEventType.VFX:
                return "特效";

            case ActionEventType.HitBox:
                return "攻击判定";

            case ActionEventType.HitStop:
                return "顿帧";

            case ActionEventType.Speed:
                return "速度";

            case ActionEventType.Movement:
                return "位移";

            case ActionEventType.Audio:
                return "音效";

            default:
                return type.ToString();
        }
    }

    private Color GetEventColor(ActionEventType type)
    {
        switch (type)
        {
            case ActionEventType.VFX:
                return new Color(0.25f, 0.55f, 1.0f);

            case ActionEventType.HitBox:
                return new Color(1.0f, 0.25f, 0.25f);

            case ActionEventType.HitStop:
                return new Color(1.0f, 0.75f, 0.2f);

            case ActionEventType.Speed:
                return new Color(0.35f, 0.9f, 0.45f);

            case ActionEventType.Movement:
                return new Color(0.65f, 0.35f, 1.0f);

            case ActionEventType.Audio:
                return new Color(0.15f, 0.85f, 0.9f);

            default:
                return Color.gray;
        }
    }
}
#endif