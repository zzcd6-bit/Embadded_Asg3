using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CustomEditor(typeof(TimelineCameraComposer))]
public sealed class TimelineCameraComposerEditor : Editor
{
    private const string GeneratedFolder = "Assets/GeneratedTimeline";

    private SerializedProperty playableDirector;
    private SerializedProperty timelineAsset;
    private SerializedProperty cinemachineBrain;
    private SerializedProperty frameRate;
    private SerializedProperty bendStrength;
    private SerializedProperty cameraGroups;
    private SerializedProperty timelineEvents;
    private readonly Dictionary<UnityEngine.EntityId, List<MethodOption>> methodOptionsCache = new Dictionary<UnityEngine.EntityId, List<MethodOption>>();

    private void OnEnable()
    {
        playableDirector = serializedObject.FindProperty("playableDirector");
        timelineAsset = serializedObject.FindProperty("timelineAsset");
        cinemachineBrain = serializedObject.FindProperty("cinemachineBrain");
        frameRate = serializedObject.FindProperty("frameRate");
        bendStrength = serializedObject.FindProperty("bendStrength");
        cameraGroups = serializedObject.FindProperty("cameraGroups");
        timelineEvents = serializedObject.FindProperty("timelineEvents");
    }

    public override void OnInspectorGUI()
    {
        TimelineCameraComposer composer = (TimelineCameraComposer)target;
        bool canBake = CanBake(composer);

        serializedObject.Update();

        EditorGUILayout.PropertyField(playableDirector);
        EditorGUILayout.PropertyField(timelineAsset);
        EditorGUILayout.PropertyField(cinemachineBrain);
        EditorGUILayout.PropertyField(frameRate, new GUIContent("Fallback Frame Rate"));
        EditorGUILayout.PropertyField(bendStrength);

        EditorGUILayout.Space(8f);
        DrawGroups();
        EditorGUILayout.Space(8f);
        DrawEvents();

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("添加镜头组"))
            {
                AddGroup();
            }

            if (GUILayout.Button("添加事件"))
            {
                AddEvent();
            }

            using (new EditorGUI.DisabledScope(!canBake))
            {
                if (GUILayout.Button("更新 Timeline", GUILayout.Height(26f)))
                {
                    Bake(composer);
                }
            }
        }

        if (!canBake)
        {
            EditorGUILayout.HelpBox("需要 PlayableDirector、CinemachineBrain，并且至少有一个镜头组包含相机和控制点。", MessageType.Info);
        }
    }

    private void DrawGroups()
    {
        EditorGUILayout.LabelField("镜头组", EditorStyles.boldLabel);

        for (int i = 0; i < cameraGroups.arraySize; i++)
        {
            SerializedProperty group = cameraGroups.GetArrayElementAtIndex(i);
            SerializedProperty groupName = group.FindPropertyRelative("name");
            SerializedProperty camera = group.FindPropertyRelative("camera");
            SerializedProperty crossFade = group.FindPropertyRelative("crossFadeFramesFromPrevious");
            SerializedProperty shots = group.FindPropertyRelative("shots");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            group.isExpanded = EditorGUILayout.Foldout(group.isExpanded, string.IsNullOrWhiteSpace(groupName.stringValue) ? $"镜头组 {i + 1}" : groupName.stringValue, true);
            if (GUILayout.Button("加镜头点", GUILayout.Width(80f)))
            {
                AddPoint(i);
            }
            if (GUILayout.Button("删除组", GUILayout.Width(70f)))
            {
                cameraGroups.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            if (group.isExpanded)
            {
                EditorGUILayout.PropertyField(groupName, new GUIContent($"镜头组 {i + 1} 名称"));
                EditorGUILayout.PropertyField(camera, new GUIContent("使用的相机"));
                if (i > 0)
                {
                    EditorGUILayout.PropertyField(crossFade, new GUIContent("上个组到这个组 Cross Fade 帧数"));
                }

                EditorGUILayout.Space(4f);
                for (int shotIndex = 0; shotIndex < shots.arraySize; shotIndex++)
                {
                    SerializedProperty shot = shots.GetArrayElementAtIndex(shotIndex);
                    TimelineCameraControlPoint point = shot.objectReferenceValue as TimelineCameraControlPoint;
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(shot, new GUIContent($"镜头 {shotIndex + 1}"));
                    if (point != null && GUILayout.Button("选中", GUILayout.Width(48f)))
                    {
                        Selection.activeGameObject = point.gameObject;
                        ApplyPreview(camera.objectReferenceValue as CinemachineCamera, point);
                    }
                    if (GUILayout.Button("X", GUILayout.Width(24f)))
                    {
                        shots.DeleteArrayElementAtIndex(shotIndex);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    if (point != null)
                    {
                        DrawPointInline(point, shotIndex == 0);
                    }
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawEvents()
    {
        EditorGUILayout.LabelField("Timeline Events", EditorStyles.boldLabel);

        for (int i = 0; i < timelineEvents.arraySize; i++)
        {
            SerializedProperty timelineEvent = timelineEvents.GetArrayElementAtIndex(i);
            SerializedProperty eventName = timelineEvent.FindPropertyRelative("name");
            SerializedProperty triggerFrame = timelineEvent.FindPropertyRelative("triggerFrame");
            SerializedProperty targetObject = timelineEvent.FindPropertyRelative("target");
            SerializedProperty componentTypeName = timelineEvent.FindPropertyRelative("componentTypeName");
            SerializedProperty methodName = timelineEvent.FindPropertyRelative("methodName");
            SerializedProperty parameterType = timelineEvent.FindPropertyRelative("parameterType");
            SerializedProperty parameterCount = timelineEvent.FindPropertyRelative("parameterCount");
            SerializedProperty parameter1TypeName = timelineEvent.FindPropertyRelative("parameter1TypeName");
            SerializedProperty parameter2TypeName = timelineEvent.FindPropertyRelative("parameter2TypeName");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            timelineEvent.isExpanded = EditorGUILayout.Foldout(timelineEvent.isExpanded, string.IsNullOrWhiteSpace(eventName.stringValue) ? $"Event {i + 1}" : eventName.stringValue, true);
            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                timelineEvents.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            if (timelineEvent.isExpanded)
            {
                EditorGUILayout.PropertyField(eventName, new GUIContent("Name"));
                EditorGUILayout.PropertyField(triggerFrame, new GUIContent("Trigger Frame"));
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(targetObject, new GUIContent("Target GameObject"));
                if (EditorGUI.EndChangeCheck())
                {
                    methodOptionsCache.Clear();
                }

                GameObject target = targetObject.objectReferenceValue as GameObject;
                List<MethodOption> options = GetMethodOptionsCached(target);
                string[] labels = options.Count == 0
                    ? new[] { target == null ? "Assign a target GameObject" : "No supported public methods" }
                    : options.Select(option => option.Label).ToArray();
                int currentIndex = options.FindIndex(option =>
                    option.ComponentTypeName == componentTypeName.stringValue
                    && option.MethodName == methodName.stringValue
                    && option.ParameterCount == parameterCount.intValue
                    && option.Parameter1TypeName == parameter1TypeName.stringValue
                    && option.Parameter2TypeName == parameter2TypeName.stringValue);
                int selectedIndex = Mathf.Max(0, currentIndex);

                using (new EditorGUI.DisabledScope(options.Count == 0))
                {
                    int newIndex = EditorGUILayout.Popup("Target Function", selectedIndex, labels);
                    if (options.Count > 0 && (currentIndex != newIndex || string.IsNullOrEmpty(methodName.stringValue)))
                    {
                        MethodOption option = options[newIndex];
                        componentTypeName.stringValue = option.ComponentTypeName;
                        methodName.stringValue = option.MethodName;
                        parameterCount.intValue = option.ParameterCount;
                        parameter1TypeName.stringValue = option.Parameter1TypeName;
                        parameter2TypeName.stringValue = option.Parameter2TypeName;
                        parameterType.enumValueIndex = option.ParameterCount == 0 ? (int)TimelineCameraEventParameterType.None : (int)option.Parameter1Kind;
                    }
                }

                DrawEventParameterFields(timelineEvent);
            }

            EditorGUILayout.EndVertical();
        }
    }

    private static void DrawEventParameterFields(SerializedProperty timelineEvent)
    {
        int parameterCount = timelineEvent.FindPropertyRelative("parameterCount").intValue;
        if (parameterCount > 0)
        {
            DrawEventParameterField(
                timelineEvent,
                "Parameter 1",
                timelineEvent.FindPropertyRelative("parameter1TypeName").stringValue,
                false);
        }

        if (parameterCount > 1)
        {
            DrawEventParameterField(
                timelineEvent,
                "Parameter 2",
                timelineEvent.FindPropertyRelative("parameter2TypeName").stringValue,
                true);
        }
    }

    private static void DrawEventParameterField(SerializedProperty timelineEvent, string label, string parameterTypeName, bool second)
    {
        Type parameterTypeObject = Type.GetType(parameterTypeName);
        TimelineCameraEventParameterType parameterType = TimelineCameraEventBehaviour.GetParameterType(parameterTypeObject);
        switch (parameterType)
        {
            case TimelineCameraEventParameterType.Int:
                EditorGUILayout.PropertyField(timelineEvent.FindPropertyRelative(second ? "intValue2" : "intValue"), new GUIContent(label));
                break;
            case TimelineCameraEventParameterType.Float:
                EditorGUILayout.PropertyField(timelineEvent.FindPropertyRelative(second ? "floatValue2" : "floatValue"), new GUIContent(label));
                break;
            case TimelineCameraEventParameterType.Bool:
                EditorGUILayout.PropertyField(timelineEvent.FindPropertyRelative(second ? "boolValue2" : "boolValue"), new GUIContent(label));
                break;
            case TimelineCameraEventParameterType.String:
                EditorGUILayout.PropertyField(timelineEvent.FindPropertyRelative(second ? "stringValue2" : "stringValue"), new GUIContent(label));
                break;
            case TimelineCameraEventParameterType.Vector3:
                EditorGUILayout.PropertyField(timelineEvent.FindPropertyRelative(second ? "vector3Value2" : "vector3Value"), new GUIContent(label));
                break;
            case TimelineCameraEventParameterType.GameObject:
                SerializedProperty property = timelineEvent.FindPropertyRelative(second ? "objectValue2" : "objectValue1");
                Type objectType = parameterTypeObject == null ? typeof(UnityEngine.Object) : parameterTypeObject;
                property.objectReferenceValue = EditorGUILayout.ObjectField(label, property.objectReferenceValue, objectType, true);
                break;
        }
    }

    private static void DrawPointInline(TimelineCameraControlPoint point, bool isFirstPoint)
    {
        Transform pointTransform = point.transform;
        EditorGUI.BeginChangeCheck();
        Vector3 position = EditorGUILayout.Vector3Field("Position", pointTransform.position);
        Vector3 rotation = EditorGUILayout.Vector3Field("Rotation", pointTransform.eulerAngles);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(pointTransform, "Edit Camera Control Point Transform");
            pointTransform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
            EditorUtility.SetDirty(pointTransform);
            SceneView.RepaintAll();
        }

        SerializedObject pointObject = new SerializedObject(point);
        pointObject.Update();
        EditorGUILayout.PropertyField(pointObject.FindProperty("lensFieldOfView"), new GUIContent("Lens"));
        EditorGUILayout.PropertyField(pointObject.FindProperty("stopDurationFrames"), new GUIContent("Stop Duration"));
        if (!isFirstPoint)
        {
            EditorGUILayout.PropertyField(pointObject.FindProperty("travelFramesFromPrevious"), new GUIContent("上个点到这个点经过的帧数"));
            EditorGUILayout.Slider(pointObject.FindProperty("bendX"), -1f, 1f, new GUIContent("路径 X 轴弯曲"));
            EditorGUILayout.Slider(pointObject.FindProperty("bendY"), -1f, 1f, new GUIContent("路径 Y 轴弯曲"));
            EditorGUILayout.Slider(pointObject.FindProperty("bendZ"), -1f, 1f, new GUIContent("路径 Z 轴弯曲"));
        }
        pointObject.ApplyModifiedProperties();
    }

    private void AddGroup()
    {
        serializedObject.Update();
        int index = cameraGroups.arraySize;
        cameraGroups.InsertArrayElementAtIndex(index);
        SerializedProperty group = cameraGroups.GetArrayElementAtIndex(index);
        group.FindPropertyRelative("name").stringValue = $"镜头组 {index + 1}";
        group.FindPropertyRelative("camera").objectReferenceValue = null;
        group.FindPropertyRelative("crossFadeFramesFromPrevious").intValue = 0;
        group.FindPropertyRelative("shots").ClearArray();
        group.isExpanded = true;
        serializedObject.ApplyModifiedProperties();
    }

    private void AddEvent()
    {
        serializedObject.Update();
        int index = timelineEvents.arraySize;
        timelineEvents.InsertArrayElementAtIndex(index);
        SerializedProperty timelineEvent = timelineEvents.GetArrayElementAtIndex(index);
        timelineEvent.FindPropertyRelative("name").stringValue = $"Event {index + 1}";
        timelineEvent.FindPropertyRelative("triggerFrame").intValue = 0;
        timelineEvent.FindPropertyRelative("target").objectReferenceValue = null;
        timelineEvent.FindPropertyRelative("componentTypeName").stringValue = string.Empty;
        timelineEvent.FindPropertyRelative("methodName").stringValue = string.Empty;
        timelineEvent.FindPropertyRelative("parameterCount").intValue = 0;
        timelineEvent.FindPropertyRelative("parameter1TypeName").stringValue = string.Empty;
        timelineEvent.FindPropertyRelative("parameter2TypeName").stringValue = string.Empty;
        timelineEvent.FindPropertyRelative("parameterType").enumValueIndex = (int)TimelineCameraEventParameterType.None;
        timelineEvent.FindPropertyRelative("intValue").intValue = 0;
        timelineEvent.FindPropertyRelative("floatValue").floatValue = 0f;
        timelineEvent.FindPropertyRelative("boolValue").boolValue = false;
        timelineEvent.FindPropertyRelative("stringValue").stringValue = string.Empty;
        timelineEvent.FindPropertyRelative("vector3Value").vector3Value = Vector3.zero;
        timelineEvent.FindPropertyRelative("gameObjectValue").objectReferenceValue = null;
        timelineEvent.FindPropertyRelative("intValue2").intValue = 0;
        timelineEvent.FindPropertyRelative("floatValue2").floatValue = 0f;
        timelineEvent.FindPropertyRelative("boolValue2").boolValue = false;
        timelineEvent.FindPropertyRelative("stringValue2").stringValue = string.Empty;
        timelineEvent.FindPropertyRelative("vector3Value2").vector3Value = Vector3.zero;
        timelineEvent.FindPropertyRelative("objectValue1").objectReferenceValue = null;
        timelineEvent.FindPropertyRelative("objectValue2").objectReferenceValue = null;
        timelineEvent.isExpanded = true;
        serializedObject.ApplyModifiedProperties();
    }

    private void AddPoint(int groupIndex)
    {
        TimelineCameraComposer composer = (TimelineCameraComposer)target;
        Transform groupRoot = GetOrCreateGroupRoot(composer.transform, groupIndex);
        GameObject pointObject = new GameObject($"镜头 {groupRoot.childCount + 1}");
        Undo.RegisterCreatedObjectUndo(pointObject, "Create Camera Control Point");
        pointObject.transform.SetParent(groupRoot);
        pointObject.transform.localPosition = Vector3.forward * groupRoot.childCount * 3f;
        pointObject.transform.localRotation = Quaternion.identity;
        TimelineCameraControlPoint point = pointObject.AddComponent<TimelineCameraControlPoint>();

        serializedObject.Update();
        SerializedProperty shots = cameraGroups.GetArrayElementAtIndex(groupIndex).FindPropertyRelative("shots");
        int index = shots.arraySize;
        shots.InsertArrayElementAtIndex(index);
        shots.GetArrayElementAtIndex(index).objectReferenceValue = point;
        serializedObject.ApplyModifiedProperties();
        Selection.activeGameObject = pointObject;
    }

    private static Transform GetOrCreateGroupRoot(Transform composerTransform, int groupIndex)
    {
        string name = $"镜头组 {groupIndex + 1}";
        Transform root = composerTransform.Find(name);
        if (root != null)
        {
            return root;
        }

        GameObject rootObject = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(rootObject, "Create Camera Group Root");
        rootObject.transform.SetParent(composerTransform);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;
        return rootObject.transform;
    }

    private static bool CanBake(TimelineCameraComposer composer)
    {
        return composer != null
            && composer.PlayableDirector != null
            && composer.CinemachineBrain != null
            && composer.CameraGroups.Any(g => g != null && g.camera != null && g.shots != null && g.shots.Any(p => p != null));
    }

    private static void Bake(TimelineCameraComposer composer)
    {
        TimelineAsset timeline = GetOrCreateTimeline(composer);
        float fps = GetFrameRate(composer, timeline);

        DeleteGeneratedTracks(timeline, composer.PlayableDirector, composer.GeneratedTrackPrefix);
        Dictionary<CinemachineCamera, TimelineCameraPoseTrack> poseTracks = new Dictionary<CinemachineCamera, TimelineCameraPoseTrack>();
        CinemachineTrack cinemachineTrack = timeline.CreateTrack<CinemachineTrack>($"{composer.GeneratedTrackPrefix} Cinemachine Shots");
        composer.PlayableDirector.playableAsset = timeline;
        composer.PlayableDirector.SetGenericBinding(cinemachineTrack, composer.CinemachineBrain);

        double previousGroupEnd = 0d;
        for (int groupIndex = 0; groupIndex < composer.CameraGroups.Count; groupIndex++)
        {
            TimelineCameraComposer.CameraGroup group = composer.CameraGroups[groupIndex];
            if (group == null || group.camera == null || group.shots == null)
            {
                continue;
            }

            List<TimelineCameraControlPoint> points = group.shots.Where(p => p != null).ToList();
            if (points.Count == 0)
            {
                continue;
            }

            ApplyCameraTransformWithoutOutputPreview(group.camera, points[0]);

            double crossFade = groupIndex == 0 ? 0d : Mathf.Max(0, group.crossFadeFramesFromPrevious) / fps;
            double groupStart = Math.Max(0d, previousGroupEnd - crossFade);
            double groupDuration = GetGroupDurationFrames(points) / fps;
            double groupEnd = groupStart + groupDuration;

            CreateCinemachineShot(composer, cinemachineTrack, group, groupIndex, groupStart, groupDuration);
            TimelineCameraPoseTrack poseTrack = GetPoseTrack(timeline, poseTracks, composer, group.camera);
            CreatePoseClip(poseTrack, group, points, fps, groupStart, groupDuration, composer.BendStrength);
            composer.PlayableDirector.SetGenericBinding(poseTrack, group.camera);

            previousGroupEnd = Math.Max(previousGroupEnd, groupEnd);
        }

        CreateEventTrack(composer, timeline, fps);

        composer.PrepareTimelineStartPose();
        composer.PlayableDirector.time = 0d;
        composer.PlayableDirector.Evaluate();

        EditorUtility.SetDirty(timeline);
        EditorUtility.SetDirty(composer.PlayableDirector);
        AssetDatabase.SaveAssets();
        TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
        EditorGUIUtility.PingObject(composer.PlayableDirector.gameObject);
    }

    private static void CreateCinemachineShot(
        TimelineCameraComposer composer,
        CinemachineTrack track,
        TimelineCameraComposer.CameraGroup group,
        int groupIndex,
        double start,
        double duration)
    {
        TimelineClip clip = track.CreateClip<CinemachineShot>();
        clip.displayName = string.IsNullOrWhiteSpace(group.name) ? $"镜头组 {groupIndex + 1}" : group.name;
        clip.start = start;
        clip.duration = duration;

        CinemachineShot shot = (CinemachineShot)clip.asset;
        shot.DisplayName = clip.displayName;
        shot.VirtualCamera.exposedName = Guid.NewGuid().ToString();
        composer.PlayableDirector.SetReferenceValue(shot.VirtualCamera.exposedName, group.camera);
    }

    private static void CreateEventTrack(TimelineCameraComposer composer, TimelineAsset timeline, float fps)
    {
        if (composer.TimelineEvents == null || composer.TimelineEvents.Count == 0)
        {
            return;
        }

        TimelineCameraEventTrack eventTrack = timeline.CreateTrack<TimelineCameraEventTrack>($"{composer.GeneratedTrackPrefix} Events");
        double clipDuration = 1d / Mathf.Max(1f, fps);

        for (int i = 0; i < composer.TimelineEvents.Count; i++)
        {
            TimelineCameraComposer.TimelineEvent timelineEvent = composer.TimelineEvents[i];
            if (!IsValidEvent(timelineEvent))
            {
                continue;
            }

            TimelineClip clip = eventTrack.CreateClip<TimelineCameraEventClip>();
            clip.displayName = string.IsNullOrWhiteSpace(timelineEvent.name) ? $"Event {i + 1}" : timelineEvent.name;
            clip.start = Mathf.Max(0, timelineEvent.triggerFrame) / fps;
            clip.duration = clipDuration;

            TimelineCameraEventClip eventClip = (TimelineCameraEventClip)clip.asset;
            eventClip.target.exposedName = Guid.NewGuid().ToString();
            composer.PlayableDirector.SetReferenceValue(eventClip.target.exposedName, timelineEvent.target);
            eventClip.componentTypeName = timelineEvent.componentTypeName;
            eventClip.methodName = timelineEvent.methodName;
            eventClip.parameterCount = timelineEvent.parameterCount;
            eventClip.parameter1TypeName = timelineEvent.parameter1TypeName;
            eventClip.parameter2TypeName = timelineEvent.parameter2TypeName;
            eventClip.parameterType = timelineEvent.parameterType;
            eventClip.intValue = timelineEvent.intValue;
            eventClip.floatValue = timelineEvent.floatValue;
            eventClip.boolValue = timelineEvent.boolValue;
            eventClip.stringValue = timelineEvent.stringValue;
            eventClip.vector3Value = timelineEvent.vector3Value;
            eventClip.intValue2 = timelineEvent.intValue2;
            eventClip.floatValue2 = timelineEvent.floatValue2;
            eventClip.boolValue2 = timelineEvent.boolValue2;
            eventClip.stringValue2 = timelineEvent.stringValue2;
            eventClip.vector3Value2 = timelineEvent.vector3Value2;

            if (timelineEvent.objectValue1 != null)
            {
                eventClip.objectValue1.exposedName = Guid.NewGuid().ToString();
                composer.PlayableDirector.SetReferenceValue(eventClip.objectValue1.exposedName, timelineEvent.objectValue1);
            }

            if (timelineEvent.objectValue2 != null)
            {
                eventClip.objectValue2.exposedName = Guid.NewGuid().ToString();
                composer.PlayableDirector.SetReferenceValue(eventClip.objectValue2.exposedName, timelineEvent.objectValue2);
            }
        }
    }

    private static bool IsValidEvent(TimelineCameraComposer.TimelineEvent timelineEvent)
    {
        return timelineEvent != null
            && timelineEvent.target != null
            && !string.IsNullOrEmpty(timelineEvent.componentTypeName)
            && !string.IsNullOrEmpty(timelineEvent.methodName);
    }

    private static TimelineCameraPoseTrack GetPoseTrack(
        TimelineAsset timeline,
        Dictionary<CinemachineCamera, TimelineCameraPoseTrack> tracks,
        TimelineCameraComposer composer,
        CinemachineCamera camera)
    {
        if (tracks.TryGetValue(camera, out TimelineCameraPoseTrack existing))
        {
            return existing;
        }

        TimelineCameraPoseTrack track = timeline.CreateTrack<TimelineCameraPoseTrack>($"{composer.GeneratedTrackPrefix} {camera.name} World Pose");
        tracks.Add(camera, track);
        return track;
    }

    private static void CreatePoseClip(
        TimelineCameraPoseTrack track,
        TimelineCameraComposer.CameraGroup group,
        IReadOnlyList<TimelineCameraControlPoint> points,
        float fps,
        double start,
        double duration,
        float bendStrength)
    {
        TimelineClip timelineClip = track.CreateClip<TimelineCameraPoseClip>();
        timelineClip.displayName = group.name;
        timelineClip.start = start;
        timelineClip.duration = duration;

        TimelineCameraPoseClip poseClip = (TimelineCameraPoseClip)timelineClip.asset;
        poseClip.bendStrength = bendStrength;
        poseClip.points = new TimelineCameraPosePoint[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            TimelineCameraControlPoint point = points[i];
            poseClip.points[i] = new TimelineCameraPosePoint
            {
                position = point.transform.position,
                rotation = point.transform.rotation,
                lensFieldOfView = point.LensFieldOfView,
                travelSecondsFromPrevious = i == 0 ? 0f : Mathf.Max(1, point.TravelFramesFromPrevious) / fps,
                stopSeconds = Mathf.Max(0, point.StopDurationFrames) / fps,
                bend = point.Bend
            };
        }
    }

    private static int GetGroupDurationFrames(IReadOnlyList<TimelineCameraControlPoint> points)
    {
        if (points.Count == 0)
        {
            return 1;
        }

        int frames = Mathf.Max(0, points[0].StopDurationFrames);
        for (int i = 1; i < points.Count; i++)
        {
            frames += Mathf.Max(1, points[i].TravelFramesFromPrevious);
            frames += Mathf.Max(0, points[i].StopDurationFrames);
        }

        return Mathf.Max(1, frames);
    }

    private static TimelineAsset GetOrCreateTimeline(TimelineCameraComposer composer)
    {
        if (composer.TimelineAsset != null)
        {
            return composer.TimelineAsset;
        }

        EnsureGeneratedFolder();
        string timelineName = $"{MakeSafeFileName(composer.name)}_CameraTimeline";
        string path = AssetDatabase.GenerateUniqueAssetPath($"{GeneratedFolder}/{timelineName}.timeline");
        TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        timeline.name = System.IO.Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(timeline, path);

        SerializedObject so = new SerializedObject(composer);
        SerializedProperty property = so.FindProperty("timelineAsset");
        property.objectReferenceValue = timeline;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(composer);
        return timeline;
    }

    private static void DeleteGeneratedTracks(TimelineAsset timeline, PlayableDirector director, string prefix)
    {
        List<TrackAsset> tracks = timeline.GetOutputTracks()
            .Where(track => track != null && track.name.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        foreach (TrackAsset track in tracks)
        {
            if (director != null)
            {
                director.ClearGenericBinding(track);
            }

            timeline.DeleteTrack(track);
        }
    }

    private static float GetFrameRate(TimelineCameraComposer composer, TimelineAsset timeline)
    {
        if (composer.UseTimelineFrameRate && timeline != null)
        {
            return Mathf.Max(1f, (float)timeline.editorSettings.frameRate);
        }

        return composer.FrameRate;
    }

    private static void EnsureGeneratedFolder()
    {
        if (AssetDatabase.IsValidFolder(GeneratedFolder))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/GeneratedTimeline"))
        {
            AssetDatabase.CreateFolder("Assets", "GeneratedTimeline");
        }
    }

    internal static void ApplyPreview(CinemachineCamera camera, TimelineCameraControlPoint point, bool recordUndo = true)
    {
        if (camera == null || point == null)
        {
            return;
        }

        Transform cameraTransform = camera.transform;
        Vector3 localPosition = cameraTransform.parent == null
            ? point.transform.position
            : cameraTransform.parent.InverseTransformPoint(point.transform.position);
        Quaternion localRotation = cameraTransform.parent == null
            ? point.transform.rotation
            : Quaternion.Inverse(cameraTransform.parent.rotation) * point.transform.rotation;

        if (recordUndo)
        {
            Undo.RecordObjects(new UnityEngine.Object[] { cameraTransform, camera }, "Preview Camera Control Point");
        }

        cameraTransform.SetLocalPositionAndRotation(localPosition, localRotation);
        camera.Lens.FieldOfView = point.LensFieldOfView;
        camera.UpdateCameraState(Vector3.up, -1f);
        CinemachineCore.SoloCamera = camera;
        EditorUtility.SetDirty(cameraTransform);
        EditorUtility.SetDirty(camera);
        SceneView.RepaintAll();
    }

    private static void ApplyCameraTransformWithoutOutputPreview(CinemachineCamera camera, TimelineCameraControlPoint point)
    {
        if (camera == null || point == null)
        {
            return;
        }

        Transform cameraTransform = camera.transform;
        Vector3 localPosition = cameraTransform.parent == null
            ? point.transform.position
            : cameraTransform.parent.InverseTransformPoint(point.transform.position);
        Quaternion localRotation = cameraTransform.parent == null
            ? point.transform.rotation
            : Quaternion.Inverse(cameraTransform.parent.rotation) * point.transform.rotation;

        Undo.RecordObjects(new UnityEngine.Object[] { cameraTransform, camera }, "Set Camera Timeline Start Pose");
        cameraTransform.SetLocalPositionAndRotation(localPosition, localRotation);
        camera.Lens.FieldOfView = point.LensFieldOfView;
        EditorUtility.SetDirty(cameraTransform);
        EditorUtility.SetDirty(camera);
    }

    private List<MethodOption> GetMethodOptionsCached(GameObject target)
    {
        if (target == null)
        {
            return EmptyMethodOptions;
        }

        UnityEngine.EntityId targetId = target.GetEntityId();
        if (!methodOptionsCache.TryGetValue(targetId, out List<MethodOption> options))
        {
            options = BuildMethodOptions(target);
            methodOptionsCache[targetId] = options;
        }

        return options;
    }

    private static readonly List<MethodOption> EmptyMethodOptions = new List<MethodOption>();

    private static List<MethodOption> BuildMethodOptions(GameObject target)
    {
        List<MethodOption> options = new List<MethodOption>();
        if (target == null)
        {
            return options;
        }

        Component[] components = target.GetComponentsInChildren<Component>(true)
            .Concat(target.GetComponentsInParent<Component>(true))
            .Distinct()
            .ToArray();
        foreach (Component component in components)
        {
            if (component == null)
            {
                continue;
            }

            Type componentType = component.GetType();
            MethodInfo[] methods = componentType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (MethodInfo method in methods)
            {
                if (!IsSupportedEventMethod(method))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                string parameterLabel = string.Join(", ", parameters.Select(parameter => parameter.ParameterType.Name));
                string label = parameters.Length == 0
                    ? $"{componentType.Name}.{method.Name}()"
                    : $"{componentType.Name}.{method.Name}({parameterLabel})";
                options.Add(new MethodOption(
                    label,
                    componentType.AssemblyQualifiedName,
                    method.Name,
                    parameters.Select(parameter => parameter.ParameterType).ToArray()));
            }
        }

        return options
            .OrderBy(option => option.Label, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsSupportedEventMethod(MethodInfo method)
    {
        if (method == null || method.IsSpecialName || method.ContainsGenericParameters)
        {
            return false;
        }

        Type declaringType = method.DeclaringType;
        if (declaringType == typeof(MonoBehaviour)
            || declaringType == typeof(Behaviour)
            || declaringType == typeof(Component)
            || declaringType == typeof(UnityEngine.Object))
        {
            return false;
        }

        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            return true;
        }

        return parameters.Length <= 2
            && parameters.All(parameter => TimelineCameraEventBehaviour.GetParameterType(parameter.ParameterType) != TimelineCameraEventParameterType.None);
    }

    private sealed class MethodOption
    {
        public readonly string Label;
        public readonly string ComponentTypeName;
        public readonly string MethodName;
        public readonly int ParameterCount;
        public readonly string Parameter1TypeName;
        public readonly string Parameter2TypeName;
        public readonly TimelineCameraEventParameterType Parameter1Kind;

        public MethodOption(string label, string componentTypeName, string methodName, Type[] parameterTypes)
        {
            Label = label;
            ComponentTypeName = componentTypeName;
            MethodName = methodName;
            ParameterCount = parameterTypes.Length;
            Parameter1TypeName = parameterTypes.Length > 0 ? parameterTypes[0].AssemblyQualifiedName : string.Empty;
            Parameter2TypeName = parameterTypes.Length > 1 ? parameterTypes[1].AssemblyQualifiedName : string.Empty;
            Parameter1Kind = parameterTypes.Length > 0
                ? TimelineCameraEventBehaviour.GetParameterType(parameterTypes[0])
                : TimelineCameraEventParameterType.None;
        }
    }

    private static string MakeSafeFileName(string value)
    {
        foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(value) ? "CameraGroup" : value;
    }
}

[InitializeOnLoad]
public static class TimelineCameraSelectedPointPreviewWatcher
{
    private const double CameraLookupInterval = 0.5d;

    private static TimelineCameraControlPoint lastPoint;
    private static PointSnapshot lastSnapshot;
    private static bool hasSnapshot;
    private static TimelineCameraControlPoint cachedCameraPoint;
    private static CinemachineCamera cachedCamera;
    private static double nextCameraLookupTime;

    static TimelineCameraSelectedPointPreviewWatcher()
    {
        EditorApplication.update += WatchSelectedPoint;
    }

    private static void WatchSelectedPoint()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        TimelineCameraControlPoint point = Selection.activeGameObject == null
            ? null
            : Selection.activeGameObject.GetComponent<TimelineCameraControlPoint>();
        if (point == null)
        {
            lastPoint = null;
            hasSnapshot = false;
            cachedCameraPoint = null;
            cachedCamera = null;
            if (CinemachineCore.SoloCamera is CinemachineCamera)
            {
                CinemachineCore.SoloCamera = null;
            }

            return;
        }

        if (!TryFindCameraForPoint(point, out CinemachineCamera camera))
        {
            lastPoint = null;
            hasSnapshot = false;
            return;
        }

        PointSnapshot snapshot = PointSnapshot.Create(point, camera);
        if (point == lastPoint && hasSnapshot && snapshot.Equals(lastSnapshot))
        {
            return;
        }

        lastPoint = point;
        lastSnapshot = snapshot;
        hasSnapshot = true;
        TimelineCameraComposerEditor.ApplyPreview(camera, point, false);
    }

    private static bool TryFindCameraForPoint(TimelineCameraControlPoint point, out CinemachineCamera camera)
    {
        double time = EditorApplication.timeSinceStartup;
        if (point == cachedCameraPoint && time < nextCameraLookupTime)
        {
            camera = cachedCamera;
            return camera != null;
        }

        cachedCameraPoint = point;
        cachedCamera = null;
        nextCameraLookupTime = time + CameraLookupInterval;

        TimelineCameraComposer[] composers = UnityEngine.Object.FindObjectsByType<TimelineCameraComposer>();
        foreach (TimelineCameraComposer composer in composers)
        {
            if (composer == null || composer.CameraGroups == null)
            {
                continue;
            }

            foreach (TimelineCameraComposer.CameraGroup group in composer.CameraGroups)
            {
                if (group == null || group.camera == null || group.shots == null)
                {
                    continue;
                }

                if (group.shots.Contains(point))
                {
                    cachedCamera = group.camera;
                    camera = cachedCamera;
                    return true;
                }
            }
        }

        camera = null;
        return false;
    }

    private readonly struct PointSnapshot : IEquatable<PointSnapshot>
    {
        private readonly int cameraHash;
        private readonly Vector3 position;
        private readonly Quaternion rotation;
        private readonly float lens;
        private readonly int stopFrames;
        private readonly int travelFrames;
        private readonly Vector3 bend;

        private PointSnapshot(
            int cameraHash,
            Vector3 position,
            Quaternion rotation,
            float lens,
            int stopFrames,
            int travelFrames,
            Vector3 bend)
        {
            this.cameraHash = cameraHash;
            this.position = position;
            this.rotation = rotation;
            this.lens = lens;
            this.stopFrames = stopFrames;
            this.travelFrames = travelFrames;
            this.bend = bend;
        }

        public static PointSnapshot Create(TimelineCameraControlPoint point, CinemachineCamera camera)
        {
            return new PointSnapshot(
                camera == null ? 0 : camera.GetHashCode(),
                point.transform.position,
                point.transform.rotation,
                point.LensFieldOfView,
                point.StopDurationFrames,
                point.TravelFramesFromPrevious,
                point.Bend);
        }

        public bool Equals(PointSnapshot other)
        {
            return cameraHash == other.cameraHash
                && position == other.position
                && rotation == other.rotation
                && Mathf.Approximately(lens, other.lens)
                && stopFrames == other.stopFrames
                && travelFrames == other.travelFrames
                && bend == other.bend;
        }
    }
}
