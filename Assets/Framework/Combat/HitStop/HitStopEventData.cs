using System;
using UnityEngine;

[Serializable]
public class HitStopEventData
{
    [Header("顿帧持续时间，真实时间")]
    public float stopDuration = 0.06f;

    [Header("顿帧时动画速度倍率。0 为完全停住，0.05 为极慢")]
    [Range(0f, 1f)]
    public float animationSpeedMultiplier = 0.05f;

    [Header("是否影响自己")]
    public bool affectSelf = true;

    [Header("是否影响范围内对象")]
    public bool affectObjectsInRange = false;

    [Header("影响范围")]
    public float affectedRadius = 3f;

    [Header("受影响对象 Layer")]
    public LayerMask affectedLayer = ~0;
}