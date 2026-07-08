using System;
using UnityEngine;

[Serializable]
public class VFXEventData
{
    [Header("特效 Prefab")]
    public GameObject prefab;

    [Header("是否使用对象池")]
    public bool usePool = true;

    [Header("对象池名字 / Resources 路径")]
    public string poolName;

    [Header("挂点名字，可为空")]
    public string bindPointName;

    [Header("是否挂在挂点下面")]
    public bool attachToBindPoint = false;

    [Header("本地位置")]
    public Vector3 localPosition;

    [Header("本地旋转")]
    public Vector3 localEulerAngles;

    [Header("本地缩放")]
    public Vector3 localScale = Vector3.one;

    [Header("回收时间，0 表示不自动回收")]
    public float destroyDelay = 2f;
}