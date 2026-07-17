using System;
using UnityEngine;

[Serializable]
public class ActionEventData
{
    public bool enabled = true;

    public string eventName;

    public ActionEventType type;

    [Min(0)]
    public float startTime;

    [Min(0)]
    public float duration;

    public VFXEventData vfx = new VFXEventData();
    public HitBoxEventData hitBox = new HitBoxEventData();
    public HitStopEventData hitStop = new HitStopEventData();
    public SpeedEventData speed = new SpeedEventData();
    public MovementEventData movement = new MovementEventData();
    public AudioEventData audio = new AudioEventData();

    public float EndTime
    {
        get { return startTime + duration; }
    }

    public bool IsActive(float time)
    {
        return enabled && time >= startTime && time <= EndTime;
    }
}