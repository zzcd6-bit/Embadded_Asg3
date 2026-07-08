using System;
using UnityEngine;

[Serializable]
public class MovementEventData
{
    [Header("总位移距离")]
    public float distance = 1f;

    [Header("本地方向")]
    public Vector3 localDirection = Vector3.forward;

    [Header("是否只在水平面移动")]
    public bool horizontalOnly = true;

    [Header("位移曲线")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
}