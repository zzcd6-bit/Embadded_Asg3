using System;
using UnityEngine;

[Serializable]
public struct BambooFogSettings
{
    [Range(0f, 1f)]
    public float fogDensity;

    [Min(0f)]
    public float visibilityDistance;

    [Min(0f)]
    public float clearRadius;

    [Min(0f)]
    public float transitionDuration;

    public BambooFogSettings(float fogDensity, float visibilityDistance, float clearRadius, float transitionDuration)
    {
        this.fogDensity = fogDensity;
        this.visibilityDistance = visibilityDistance;
        this.clearRadius = clearRadius;
        this.transitionDuration = transitionDuration;
    }

    public static BambooFogSettings Lerp(BambooFogSettings from, BambooFogSettings to, float t)
    {
        return new BambooFogSettings(
            Mathf.Lerp(from.fogDensity, to.fogDensity, t),
            Mathf.Lerp(from.visibilityDistance, to.visibilityDistance, t),
            Mathf.Lerp(from.clearRadius, to.clearRadius, t),
            Mathf.Lerp(from.transitionDuration, to.transitionDuration, t)
        );
    }
}
