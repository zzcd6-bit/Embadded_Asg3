using System;
using UnityEngine;

[Serializable]
public class AudioEventData
{
    [Tooltip("Relative path under Assets/Resources/Audio, without extension.")]
    public string soundName;

    [Tooltip("Enable for looping action sounds.")]
    public bool loop = false;

    [Tooltip("Synchronous loading gives more accurate action timing.")]
    public bool isSync = true;

    [Tooltip("Stop a looping sound when the action ends or is interrupted.")]
    public bool stopWhenActionEnds = true;
}
