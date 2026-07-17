using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewActionConfig", menuName = "Combat/Action Config")]
public class ActionConfig : ScriptableObject
{
    [Header("基础信息")]
    public string actionId;

    [Header("动画")]
    public AnimationClip animationClip;

    [Tooltip("是否自动使用 AnimationClip 的长度")]
    public bool useClipLength = true;

    [Tooltip("动作总时长。如果 useClipLength 为 true，会自动使用动画长度")]
    public float length = 1f;

    [Tooltip("Animancer 淡入时间")]
    public float fadeDuration = 0.1f;

    [Header("动作控制")]
    [Tooltip("动作期间是否禁止普通移动")]
    public bool lockMovement = true;

    [Tooltip("动作结束后是否自动回到移动动画")]
    public bool returnToLocomotionOnEnd = true;

    [Header("动作结束过渡")]
    [Tooltip("动作结束后，延迟多少秒再回到移动动画")]
    public float exitToLocomotionDelay = 0.08f;

    [Tooltip("动作结束后如果有移动输入，是否先播放 RunStart，而不是直接进入 RunLoop")]
    public bool returnToMoveWithRunStart = true;

    [Header("连招设置")]
    [Tooltip("是否允许连招")]
    public bool enableCombo = false;

    [Tooltip("下一段连招动作")]
    public ActionConfig nextComboAction;

    [Tooltip("连招输入窗口开始时间")]
    public float comboInputStartTime = 0.45f;

    [Tooltip("连招输入窗口结束时间")]
    public float comboInputEndTime = 1.20f;

    [Tooltip("真正切到下一段动作的时间")]
    public float comboCancelTime = 0.75f;

    [Header("动作事件")]
    public List<ActionEventData> events = new List<ActionEventData>();

    public float GetLength()
    {
        if (useClipLength && animationClip != null)
            return animationClip.length;

        return Mathf.Max(0.01f, length);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (useClipLength && animationClip != null)
        {
            length = animationClip.length;
        }
    }
#endif

    [ContextMenu("按时间排序事件")]
    public void SortEventsByTime()
    {
        if (events == null)
            return;

        events.Sort((a, b) => a.startTime.CompareTo(b.startTime));
    }

#if UNITY_EDITOR
    [ContextMenu("添加干净的 VFX 事件")]
    private void AddCleanVFXEvent()
    {
        AddCleanEvent(ActionEventType.VFX, "New VFX Event");
    }

    [ContextMenu("添加干净的 HitBox 事件")]
    private void AddCleanHitBoxEvent()
    {
        AddCleanEvent(ActionEventType.HitBox, "New HitBox Event");
    }

    [ContextMenu("添加干净的 HitStop 事件")]
    private void AddCleanHitStopEvent()
    {
        AddCleanEvent(ActionEventType.HitStop, "New HitStop Event");
    }

    [ContextMenu("添加干净的 Speed 事件")]
    private void AddCleanSpeedEvent()
    {
        AddCleanEvent(ActionEventType.Speed, "New Speed Event");
    }

    [ContextMenu("Add Clean Audio Event")]
    private void AddCleanAudioEvent()
    {
        AddCleanEvent(ActionEventType.Audio, "New Audio Event");
    }

    private void AddCleanEvent(ActionEventType type, string eventName)
    {
        if (events == null)
            events = new List<ActionEventData>();

        ActionEventData newEvent = new ActionEventData
        {
            enabled = true,
            eventName = eventName,
            type = type,
            startTime = 0f,
            duration = type == ActionEventType.HitBox || type == ActionEventType.Speed ? 0.1f : 0f,

            vfx = new VFXEventData(),
            hitBox = new HitBoxEventData(),
            hitStop = new HitStopEventData(),
            speed = new SpeedEventData(),
            audio = new AudioEventData()
        };

        events.Add(newEvent);

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}