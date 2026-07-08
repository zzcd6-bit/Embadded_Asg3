using System;
using UnityEngine;

public enum HitShapeType
{
    Box,
    Sphere,
    Capsule,
    Sector
}

[Serializable]
public class HitBoxEventData
{
    [Header("形状")]
    public HitShapeType shape = HitShapeType.Box;

    [Header("本地偏移")]
    public Vector3 localOffset = new Vector3(0f, 1f, 1f);

    [Header("本地旋转")]
    public Vector3 localEulerAngles;

    [Header("Box 大小")]
    public Vector3 size = Vector3.one;

    [Header("Sphere / Capsule 半径")]
    public float radius = 1f;

    [Header("Capsule 高度")]
    public float capsuleHeight = 2f;

    [Header("Sector 扇形角度")]
    public float sectorAngle = 90f;

    [Header("伤害")]
    public int damage = 10;

    [Header("击退力度")]
    public float knockback = 3f;

    [Header("目标 Layer")]
    public LayerMask targetLayer = ~0;

    [Header("是否每个目标只命中一次")]
    public bool hitOncePerTarget = true;

    [Header("是否检测 Trigger")]
    public QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide;

    [Header("命中反馈")]
    public bool triggerHitStopOnHit = true;

    public HitStopEventData hitStopOnHit = new HitStopEventData
    {
        stopDuration = 0.06f,
        animationSpeedMultiplier = 0.05f,
        affectSelf = true,
        affectObjectsInRange = false
    };

    [Header("命中特效")]
    public bool spawnHitVFXOnHit = true;

    public VFXEventData hitVFX = new VFXEventData
    {
        usePool = true,
        poolName = "",
        destroyDelay = 1.5f,
        localScale = Vector3.one
    };
}