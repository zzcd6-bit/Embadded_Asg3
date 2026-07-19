using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GameCombatCoordinator : SingletonAutoMono<GameCombatCoordinator>
{
    [Header("Debounce")]
    [SerializeField, Min(0f)] private float enterCombatDebounce = 0.1f;
    [SerializeField, Min(0f)] private float exitCombatDebounce = 2f;
    [SerializeField, Min(0f)] private float minimumCombatDuration = 1f;

    private readonly Dictionary<Object, EnemyCombatChangedInfo> activeEnemies = new();
    private Coroutine enterCombatRoutine;
    private Coroutine exitCombatRoutine;
    private float lastCombatEnterRealtime = -999f;

    public int ActiveEnemyCount => activeEnemies.Count;
    public bool HasActiveCombat => activeEnemies.Count > 0;

    public static void ReportEnemyCombat(
        Object source,
        bool isActive,
        string reason = null)
    {
        if (source == null)
        {
            return;
        }

        EventCenter.Instance.EventTrigger(
            E_EventType.E_Enemy_CombatChanged,
            new EnemyCombatChangedInfo
            {
                source = source,
                enemyObject = ResolveGameObject(source),
                isActive = isActive,
                reason = reason
            });
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener<EnemyCombatChangedInfo>(
            E_EventType.E_Enemy_CombatChanged,
            OnEnemyCombatChanged);

        EventCenter.Instance.AddEventListener<GameModeChangedInfo>(
            E_EventType.E_GameMode_Changed,
            OnGameModeChanged);
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<EnemyCombatChangedInfo>(
            E_EventType.E_Enemy_CombatChanged,
            OnEnemyCombatChanged);

        EventCenter.Instance.RemoveEventListener<GameModeChangedInfo>(
            E_EventType.E_GameMode_Changed,
            OnGameModeChanged);

        CancelEnterCombat();
        CancelExitCombat();
    }

    private void OnEnemyCombatChanged(EnemyCombatChangedInfo info)
    {
        if (info == null || info.source == null)
        {
            return;
        }

        bool wasInCombat = HasActiveCombat;

        if (info.isActive)
        {
            activeEnemies[info.source] = info;
        }
        else
        {
            activeEnemies.Remove(info.source);
        }

        bool isInCombat = HasActiveCombat;

        if (!wasInCombat && isInCombat)
        {
            ScheduleEnterCombat();
            return;
        }

        if (wasInCombat && !isInCombat)
        {
            ScheduleExitCombat();
        }
    }

    private void OnGameModeChanged(GameModeChangedInfo info)
    {
        if (info == null)
        {
            return;
        }

        if (HasActiveCombat && info.newMode == GameModeState.Exploration)
        {
            ScheduleEnterCombat();
        }
    }

    private void ScheduleEnterCombat()
    {
        CancelExitCombat();

        if (GameModeManager.Instance.IsCurrentMode(GameModeState.Combat))
        {
            return;
        }

        if (enterCombatRoutine != null)
        {
            return;
        }

        enterCombatRoutine = StartCoroutine(EnterCombatRoutine());
    }

    private IEnumerator EnterCombatRoutine()
    {
        float waitTime = Mathf.Max(0f, enterCombatDebounce);

        if (waitTime > 0f)
        {
            yield return new WaitForSecondsRealtime(waitTime);
        }

        enterCombatRoutine = null;

        if (!HasActiveCombat)
        {
            yield break;
        }

        TryEnterCombat();
    }

    private void ScheduleExitCombat()
    {
        CancelEnterCombat();

        if (!GameModeManager.Instance.IsCurrentMode(GameModeState.Combat))
        {
            return;
        }

        if (exitCombatRoutine != null)
        {
            return;
        }

        exitCombatRoutine = StartCoroutine(ExitCombatRoutine());
    }

    private IEnumerator ExitCombatRoutine()
    {
        float elapsedSinceEnter = Time.realtimeSinceStartup - lastCombatEnterRealtime;
        float waitTime = Mathf.Max(
            Mathf.Max(0f, exitCombatDebounce),
            Mathf.Max(0f, minimumCombatDuration - elapsedSinceEnter));

        if (waitTime > 0f)
        {
            yield return new WaitForSecondsRealtime(waitTime);
        }

        exitCombatRoutine = null;

        if (HasActiveCombat)
        {
            yield break;
        }

        TryExitCombat();
    }

    private void CancelEnterCombat()
    {
        if (enterCombatRoutine == null)
        {
            return;
        }

        StopCoroutine(enterCombatRoutine);
        enterCombatRoutine = null;
    }

    private void CancelExitCombat()
    {
        if (exitCombatRoutine == null)
        {
            return;
        }

        StopCoroutine(exitCombatRoutine);
        exitCombatRoutine = null;
    }

    private void TryEnterCombat()
    {
        GameModeManager modeManager = GameModeManager.Instance;

        if (modeManager.IsCurrentMode(GameModeState.Combat))
        {
            return;
        }

        if (modeManager.RequestMode(GameModeState.Combat, this))
        {
            lastCombatEnterRealtime = Time.realtimeSinceStartup;
        }
    }

    private void TryExitCombat()
    {
        GameModeManager modeManager = GameModeManager.Instance;

        if (!modeManager.IsCurrentMode(GameModeState.Combat))
        {
            return;
        }

        modeManager.ExitMode(GameModeState.Combat);
    }

    private static GameObject ResolveGameObject(Object source)
    {
        if (source is GameObject gameObject)
        {
            return gameObject;
        }

        if (source is Component component)
        {
            return component.gameObject;
        }

        return null;
    }
}
