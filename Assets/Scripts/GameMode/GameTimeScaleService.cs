using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class GameTimeScaleService : SingletonAutoMono<GameTimeScaleService>
{
    private const string GameModeRequestKey = "GameMode";
    private const float DefaultFixedDeltaTime = 0.02f;

    private struct TimeScaleRequest
    {
        public float Scale;
        public int Priority;
        public int Order;
    }

    private readonly Dictionary<string, TimeScaleRequest> requests = new();
    private Tween timeScaleTween;
    private int requestOrder;

    public float CurrentTargetScale { get; private set; } = 1f;

    public void SetGameModeTimeScale(
        float scale,
        int priority,
        float transitionDuration)
    {
        SetRequest(
            GameModeRequestKey,
            scale,
            priority,
            transitionDuration);
    }

    public void SetRequest(
        string key,
        float scale,
        int priority,
        float transitionDuration = 0f)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        requests[key] = new TimeScaleRequest
        {
            Scale = Mathf.Max(0f, scale),
            Priority = priority,
            Order = requestOrder++
        };

        ApplyEffectiveTimeScale(transitionDuration);
    }

    public void ClearRequest(string key, float transitionDuration = 0f)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (!requests.Remove(key))
        {
            return;
        }

        ApplyEffectiveTimeScale(transitionDuration);
    }

    private void ApplyEffectiveTimeScale(float transitionDuration)
    {
        float targetScale = CalculateEffectiveScale();

        if (Mathf.Approximately(CurrentTargetScale, targetScale) &&
            Mathf.Approximately(Time.timeScale, targetScale))
        {
            return;
        }

        CurrentTargetScale = targetScale;

        if (timeScaleTween != null && timeScaleTween.IsActive())
        {
            timeScaleTween.Kill();
        }

        if (transitionDuration <= 0f)
        {
            ApplyTimeScale(targetScale);
            return;
        }

        timeScaleTween = DOVirtual
            .Float(
                Time.timeScale,
                targetScale,
                transitionDuration,
                ApplyTimeScale)
            .SetUpdate(true);
    }

    private float CalculateEffectiveScale()
    {
        if (requests.Count == 0)
        {
            return 1f;
        }

        bool hasRequest = false;
        TimeScaleRequest bestRequest = default;

        foreach (TimeScaleRequest request in requests.Values)
        {
            if (!hasRequest ||
                request.Priority > bestRequest.Priority ||
                request.Priority == bestRequest.Priority &&
                request.Order > bestRequest.Order)
            {
                bestRequest = request;
                hasRequest = true;
            }
        }

        return hasRequest ? bestRequest.Scale : 1f;
    }

    private static void ApplyTimeScale(float scale)
    {
        Time.timeScale = Mathf.Max(0f, scale);
        Time.fixedDeltaTime =
            DefaultFixedDeltaTime * Mathf.Max(Time.timeScale, 0.01f);
    }

    private void OnDestroy()
    {
        if (timeScaleTween != null && timeScaleTween.IsActive())
        {
            timeScaleTween.Kill();
        }
    }
}
