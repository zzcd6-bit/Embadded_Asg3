using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public sealed class TimelineCameraPosePoint
{
    public Vector3 position;
    public Quaternion rotation = Quaternion.identity;
    public float lensFieldOfView = 60f;
    public float travelSecondsFromPrevious;
    public float stopSeconds;
    public Vector3 bend;
}

public sealed class TimelineCameraPoseClip : PlayableAsset, ITimelineClipAsset
{
    public TimelineCameraPosePoint[] points = Array.Empty<TimelineCameraPosePoint>();
    public float bendStrength = 0.5f;

    public ClipCaps clipCaps => ClipCaps.Blending;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        ScriptPlayable<TimelineCameraPoseBehaviour> playable = ScriptPlayable<TimelineCameraPoseBehaviour>.Create(graph);
        TimelineCameraPoseBehaviour behaviour = playable.GetBehaviour();
        behaviour.Points = points;
        behaviour.BendStrength = bendStrength;
        return playable;
    }
}

public sealed class TimelineCameraPoseBehaviour : PlayableBehaviour
{
    public TimelineCameraPosePoint[] Points;
    public float BendStrength;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (playerData is not CinemachineCamera camera || Points == null || Points.Length == 0)
        {
            return;
        }

        EvaluatePose((float)playable.GetTime(), out Vector3 position, out Quaternion rotation, out float lens);
        camera.transform.SetPositionAndRotation(position, rotation);
        camera.Lens.FieldOfView = lens;
        camera.UpdateCameraState(Vector3.up, -1f);
    }

    private void EvaluatePose(float time, out Vector3 position, out Quaternion rotation, out float lens)
    {
        TimelineCameraPosePoint first = Points[0];
        if (Points.Length == 1)
        {
            position = first.position;
            rotation = first.rotation;
            lens = first.lensFieldOfView;
            return;
        }

        float cursor = Mathf.Max(0f, first.stopSeconds);
        if (time <= cursor)
        {
            position = first.position;
            rotation = first.rotation;
            lens = first.lensFieldOfView;
            return;
        }

        for (int i = 1; i < Points.Length; i++)
        {
            TimelineCameraPosePoint previous = Points[i - 1];
            TimelineCameraPosePoint current = Points[i];
            float travel = Mathf.Max(1f / 240f, current.travelSecondsFromPrevious);
            float travelStart = cursor;
            float travelEnd = travelStart + travel;

            if (time <= travelEnd)
            {
                float t = Mathf.Clamp01((time - travelStart) / travel);
                position = EvaluateBezier(previous.position, current.position, current.bend, BendStrength, t);
                rotation = Quaternion.Slerp(previous.rotation, current.rotation, Smooth(t));
                lens = Mathf.Lerp(previous.lensFieldOfView, current.lensFieldOfView, Smooth(t));
                return;
            }

            cursor = travelEnd + Mathf.Max(0f, current.stopSeconds);
            if (time <= cursor)
            {
                position = current.position;
                rotation = current.rotation;
                lens = current.lensFieldOfView;
                return;
            }
        }

        TimelineCameraPosePoint last = Points[^1];
        position = last.position;
        rotation = last.rotation;
        lens = last.lensFieldOfView;
    }

    private static Vector3 EvaluateBezier(Vector3 p0, Vector3 p3, Vector3 bend, float bendStrength, float t)
    {
        TimelineCameraComposer.GetBezierControls(p0, p3, bend, bendStrength, out Vector3 p1, out Vector3 p2);
        return TimelineCameraComposer.EvaluateBezier(p0, p1, p2, p3, t);
    }

    private static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}

[TrackBindingType(typeof(CinemachineCamera))]
[TrackClipType(typeof(TimelineCameraPoseClip))]
public sealed class TimelineCameraPoseTrack : TrackAsset
{
}
