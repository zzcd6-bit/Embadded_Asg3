using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(TimelineCameraEventClip))]
[TrackColor(0.95f, 0.55f, 0.1f)]
public sealed class TimelineCameraEventTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        ScriptPlayable<TimelineCameraEventTrackMixer> playable = ScriptPlayable<TimelineCameraEventTrackMixer>.Create(graph, inputCount);
        TimelineCameraEventTrackMixer mixer = playable.GetBehaviour();
        mixer.Events = GetClips()
            .Select((clip, index) => TimelineCameraEventInvocation.FromClip(clip, graph.GetResolver(), index))
            .Where(timelineEvent => timelineEvent.IsValid)
            .OrderBy(timelineEvent => timelineEvent.TriggerTime)
            .ThenBy(timelineEvent => timelineEvent.Order)
            .ToArray();
        mixer.ResetState();
        return playable;
    }
}

public sealed class TimelineCameraEventTrackMixer : PlayableBehaviour
{
    private const double TimeEpsilon = 0.000001d;

    public TimelineCameraEventInvocation[] Events = Array.Empty<TimelineCameraEventInvocation>();

    private bool[] firedEvents = Array.Empty<bool>();
    private double lastRootTime;
    private bool hasLastRootTime;

    public void ResetState()
    {
        firedEvents = Events == null || Events.Length == 0 ? Array.Empty<bool>() : new bool[Events.Length];
        lastRootTime = 0d;
        hasLastRootTime = false;
    }

    public override void OnGraphStart(Playable playable)
    {
        ResetState();
        lastRootTime = GetRootTime(playable);
        hasLastRootTime = true;
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!ShouldDispatch(info))
        {
            SyncTimeWithoutDispatch(playable);
            return;
        }

        EnsureState();
        if (!hasLastRootTime)
        {
            lastRootTime = GetRootTime(playable);
            hasLastRootTime = true;
        }

        FireEventsAt(lastRootTime);
        DispatchDueEvents(playable);
    }

    public override void PrepareFrame(Playable playable, FrameData info)
    {
        if (!ShouldDispatch(info))
        {
            SyncTimeWithoutDispatch(playable);
            return;
        }

        EnsureState();
        DispatchDueEvents(playable);
    }

    public override void OnGraphStop(Playable playable)
    {
        ResetState();
    }

    private void DispatchDueEvents(Playable playable)
    {
        double rootTime = GetRootTime(playable);
        if (!hasLastRootTime)
        {
            lastRootTime = rootTime;
            hasLastRootTime = true;
            FireEventsAt(rootTime);
            return;
        }

        if (rootTime + TimeEpsilon < lastRootTime)
        {
            ResetEventsAfter(rootTime);
            lastRootTime = rootTime;
            FireEventsAt(rootTime);
            return;
        }

        FireEventsBetween(lastRootTime, rootTime);
        lastRootTime = rootTime;
    }

    private void FireEventsBetween(double previousTime, double currentTime)
    {
        if (Events == null || Events.Length == 0)
        {
            return;
        }

        for (int i = 0; i < Events.Length; i++)
        {
            if (firedEvents[i])
            {
                continue;
            }

            double triggerTime = Events[i].TriggerTime;
            if (triggerTime > previousTime + TimeEpsilon && triggerTime <= currentTime + TimeEpsilon)
            {
                FireEvent(i);
            }
        }
    }

    private void FireEventsAt(double time)
    {
        if (Events == null || Events.Length == 0)
        {
            return;
        }

        for (int i = 0; i < Events.Length; i++)
        {
            if (!firedEvents[i] && Math.Abs(Events[i].TriggerTime - time) <= TimeEpsilon)
            {
                FireEvent(i);
            }
        }
    }

    private void FireEvent(int index)
    {
        firedEvents[index] = true;
        Events[index].InvokeTargetMethod();
    }

    private void ResetEventsAfter(double time)
    {
        if (Events == null || Events.Length == 0)
        {
            return;
        }

        for (int i = 0; i < Events.Length; i++)
        {
            if (Events[i].TriggerTime >= time - TimeEpsilon)
            {
                firedEvents[i] = false;
            }
        }
    }

    private void SyncTimeWithoutDispatch(Playable playable)
    {
        lastRootTime = GetRootTime(playable);
        hasLastRootTime = true;
    }

    private void EnsureState()
    {
        if (Events == null)
        {
            Events = Array.Empty<TimelineCameraEventInvocation>();
        }

        if (firedEvents == null || firedEvents.Length != Events.Length)
        {
            firedEvents = Events.Length == 0 ? Array.Empty<bool>() : new bool[Events.Length];
        }
    }

    private static bool ShouldDispatch(FrameData info)
    {
        return Application.isPlaying && info.effectivePlayState == PlayState.Playing;
    }

    private static double GetRootTime(Playable playable)
    {
        PlayableGraph graph = playable.GetGraph();
        if (graph.IsValid() && graph.GetRootPlayableCount() > 0)
        {
            Playable root = graph.GetRootPlayable(0);
            if (root.IsValid())
            {
                return root.GetTime();
            }
        }

        return playable.GetTime();
    }
}
