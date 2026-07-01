using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[ExecuteAlways]
public sealed class TimelineCameraComposer : MonoBehaviour
{
    [Serializable]
    public sealed class CameraGroup
    {
        public string name = "Camera Group";
        public CinemachineCamera camera;
        [Min(0)] public int crossFadeFramesFromPrevious = 0;
        public List<TimelineCameraControlPoint> shots = new List<TimelineCameraControlPoint>();
    }

    [Serializable]
    public sealed class TimelineEvent
    {
        public string name = "Event";
        [Min(0)] public int triggerFrame = 0;
        public GameObject target;
        public string componentTypeName;
        public string methodName;
        public int parameterCount;
        public string parameter1TypeName;
        public string parameter2TypeName;
        public TimelineCameraEventParameterType parameterType = TimelineCameraEventParameterType.None;
        public int intValue;
        public float floatValue;
        public bool boolValue;
        public string stringValue;
        public Vector3 vector3Value;
        public GameObject gameObjectValue;
        public int intValue2;
        public float floatValue2;
        public bool boolValue2;
        public string stringValue2;
        public Vector3 vector3Value2;
        public UnityEngine.Object objectValue1;
        public UnityEngine.Object objectValue2;
    }

    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private TimelineAsset timelineAsset;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField, Min(1f)] private float frameRate = 60f;
    [SerializeField] private bool useTimelineFrameRate = true;
    [SerializeField, Min(0f)] private float bendStrength = 0.5f;
    [SerializeField] private string generatedTrackPrefix = "[Camera Composer]";
    [SerializeField] private List<CameraGroup> cameraGroups = new List<CameraGroup>();
    [SerializeField] private List<TimelineEvent> timelineEvents = new List<TimelineEvent>();

    public PlayableDirector PlayableDirector => playableDirector;
    public TimelineAsset TimelineAsset => timelineAsset;
    public CinemachineBrain CinemachineBrain => cinemachineBrain;
    public float FrameRate => Mathf.Max(1f, frameRate);
    public bool UseTimelineFrameRate => useTimelineFrameRate;
    public float BendStrength => Mathf.Max(0f, bendStrength);
    public string GeneratedTrackPrefix => string.IsNullOrWhiteSpace(generatedTrackPrefix) ? "[Camera Composer]" : generatedTrackPrefix;
    public List<CameraGroup> CameraGroups => cameraGroups;
    public List<TimelineEvent> TimelineEvents => timelineEvents;

    private void Reset()
    {
        playableDirector = FindAnyObjectByType<PlayableDirector>();
        cinemachineBrain = FindAnyObjectByType<CinemachineBrain>();
    }

    private void OnValidate()
    {
        frameRate = Mathf.Max(1f, frameRate);
        bendStrength = Mathf.Max(0f, bendStrength);
        if (string.IsNullOrWhiteSpace(generatedTrackPrefix))
        {
            generatedTrackPrefix = "[Camera Composer]";
        }
    }

    private void OnEnable()
    {
        if (playableDirector != null)
        {
            playableDirector.played -= HandleDirectorPlayed;
            playableDirector.played += HandleDirectorPlayed;
        }
    }

    private void OnDisable()
    {
        if (playableDirector != null)
        {
            playableDirector.played -= HandleDirectorPlayed;
        }
    }

    private void HandleDirectorPlayed(PlayableDirector director)
    {
        if (director != playableDirector || director == null || director.time > 0.0001d)
        {
            return;
        }

        PrepareTimelineStartPose();
    }

    public void PrepareTimelineStartPose()
    {
        HashSet<CinemachineCamera> preparedCameras = new HashSet<CinemachineCamera>();
        for (int groupIndex = 0; groupIndex < cameraGroups.Count; groupIndex++)
        {
            CameraGroup group = cameraGroups[groupIndex];
            if (group == null || group.camera == null || group.shots == null)
            {
                continue;
            }

            TimelineCameraControlPoint firstPoint = null;
            for (int pointIndex = 0; pointIndex < group.shots.Count; pointIndex++)
            {
                if (group.shots[pointIndex] != null)
                {
                    firstPoint = group.shots[pointIndex];
                    break;
                }
            }

            if (firstPoint == null || preparedCameras.Contains(group.camera))
            {
                continue;
            }

            ApplyPointToCamera(group.camera, firstPoint);
            preparedCameras.Add(group.camera);
        }
    }

    private static void ApplyPointToCamera(CinemachineCamera camera, TimelineCameraControlPoint point)
    {
        Transform cameraTransform = camera.transform;
        Vector3 localPosition = cameraTransform.parent == null
            ? point.transform.position
            : cameraTransform.parent.InverseTransformPoint(point.transform.position);
        Quaternion localRotation = cameraTransform.parent == null
            ? point.transform.rotation
            : Quaternion.Inverse(cameraTransform.parent.rotation) * point.transform.rotation;

        cameraTransform.SetLocalPositionAndRotation(localPosition, localRotation);
        camera.Lens.FieldOfView = point.LensFieldOfView;
        camera.UpdateCameraState(Vector3.up, -1f);
    }

    private void OnDrawGizmos()
    {
        DrawPreviewSplines();
    }

    private void DrawPreviewSplines()
    {
        for (int groupIndex = 0; groupIndex < cameraGroups.Count; groupIndex++)
        {
            CameraGroup group = cameraGroups[groupIndex];
            if (group == null || group.shots == null || group.shots.Count < 2)
            {
                continue;
            }

            Gizmos.color = Color.HSVToRGB((groupIndex * 0.17f) % 1f, 0.75f, 1f);
            for (int i = 1; i < group.shots.Count; i++)
            {
                TimelineCameraControlPoint previous = group.shots[i - 1];
                TimelineCameraControlPoint current = group.shots[i];
                if (previous == null || current == null)
                {
                    continue;
                }

                Vector3 p0 = previous.transform.position;
                Vector3 p3 = current.transform.position;
                GetBezierControls(p0, p3, current.Bend, bendStrength, out Vector3 p1, out Vector3 p2);
                Vector3 last = p0;
                for (int sample = 1; sample <= 32; sample++)
                {
                    float t = sample / 32f;
                    Vector3 point = EvaluateBezier(p0, p1, p2, p3, t);
                    Gizmos.DrawLine(last, point);
                    last = point;
                }
            }
        }
    }

    public static void GetBezierControls(Vector3 p0, Vector3 p3, Vector3 bend, float bendStrength, out Vector3 p1, out Vector3 p2)
    {
        Vector3 delta = p3 - p0;
        float distance = Mathf.Max(0.001f, delta.magnitude);
        Vector3 bendOffset = Vector3.ClampMagnitude(bend, 1.732f) * distance * Mathf.Max(0f, bendStrength);
        p1 = p0 + delta / 3f + bendOffset;
        p2 = p3 - delta / 3f + bendOffset;
    }

    public static Vector3 EvaluateBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return u * u * u * p0
            + 3f * u * u * t * p1
            + 3f * u * t * t * p2
            + t * t * t * p3;
    }
}
