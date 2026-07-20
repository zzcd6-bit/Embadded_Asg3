using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrushGestureBuildAdapter : MonoBehaviour
{
    [Serializable]
    public class GestureBuildBinding
    {
        [Header("手势名称")]
        [Tooltip("必须和 XML 模板里的 Gesture Name 一致，例如 Bridge / Wall / BuildWall")]
        public string gestureName;

        [Header("对应建造物")]
        [Tooltip("识别该手势后，要进入建造的建筑")]
        public BuildableItemData buildableItem;

        [Header("识别要求")]
        public float minScore = 0.65f;

        [Header("墨囊消耗")]
        public int inkCost = 0;

        [Header("开关")]
        public bool enabled = true;
    }

    [Header("Gesture → Building")]
    [SerializeField]
    private List<GestureBuildBinding> bindings = new List<GestureBuildBinding>();

    [Header("Caster / Cost")]
    [SerializeField]
    private GameObject caster;

    [SerializeField]
    private bool consumeInk = true;

    [Header("Brush Transition")]
    [SerializeField]
    private BrushModeController brushModeController;

    [SerializeField]
    private BuildModeController buildModeController;

    [SerializeField]
    private float extraDelay = 0.05f;

    [Header("Debug")]
    [SerializeField]
    private bool debugLog = true;

    private Coroutine pendingBuildRoutine;
    private IBrushSkillCostReceiver inkReceiver;

    private void Awake()
    {
        if (caster == null)
        {
            caster = gameObject;
        }

        if (brushModeController == null)
        {
            brushModeController = FindAnyObjectByType<BrushModeController>();
        }

        ResolveBuildModeController();
        ResolveInkReceiver();
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener<BrushGestureResult>(
            E_EventType.E_Brush_GestureRecognized,
            OnGestureRecognized
        );
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<BrushGestureResult>(
            E_EventType.E_Brush_GestureRecognized,
            OnGestureRecognized
        );
    }

    private void OnGestureRecognized(BrushGestureResult result)
    {
        if (result == null)
            return;

        GestureBuildBinding binding = FindBinding(result.gestureName);

        if (binding == null)
            return;

        if (!binding.enabled)
            return;

        if (binding.buildableItem == null)
        {
            Debug.LogWarning(
                $"[BrushGestureBuildAdapter] Binding found but buildable item is missing. Gesture={binding.gestureName}",
                this
            );

            return;
        }

        if (result.score < binding.minScore)
        {
            if (debugLog)
            {
                Debug.Log(
                    $"[BrushGestureBuildAdapter] Gesture score too low. Gesture={result.gestureName}, Score={result.score:F3}, Need={binding.minScore:F3}",
                    this
                );
            }

            return;
        }

        if (pendingBuildRoutine != null)
        {
            StopCoroutine(pendingBuildRoutine);
            pendingBuildRoutine = null;
        }

        pendingBuildRoutine = StartCoroutine(
            StartBuildingAfterBrushExit(binding, result)
        );
    }

    private GestureBuildBinding FindBinding(string gestureName)
    {
        if (string.IsNullOrWhiteSpace(gestureName))
            return null;

        string normalizedGestureName = gestureName.Trim();

        for (int i = 0; i < bindings.Count; i++)
        {
            GestureBuildBinding binding = bindings[i];

            if (binding == null)
                continue;

            if (string.IsNullOrWhiteSpace(binding.gestureName))
                continue;

            if (string.Equals(
                binding.gestureName.Trim(),
                normalizedGestureName,
                StringComparison.OrdinalIgnoreCase
            ))
            {
                return binding;
            }
        }

        return null;
    }

    private IEnumerator StartBuildingAfterBrushExit(
        GestureBuildBinding binding,
        BrushGestureResult result
    )
    {
        float delay = extraDelay;

        if (brushModeController != null)
        {
            delay += Mathf.Max(
                0f,
                brushModeController.transitionDuration
            );
        }

        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }
        else
        {
            yield return null;
        }

        ResolveBuildModeController();

        if (buildModeController == null)
        {
            Debug.LogWarning(
                "[BrushGestureBuildAdapter] BuildModeController not found.",
                this
            );

            pendingBuildRoutine = null;
            yield break;
        }

        if (!GameModeManager.Instance.CurrentCapabilities.canEnterBuild)
        {
            pendingBuildRoutine = null;
            yield break;
        }

        if (!TryPayInkCost(binding))
        {
            pendingBuildRoutine = null;
            yield break;
        }

        buildModeController.StartPlacement(binding.buildableItem);

        if (debugLog)
        {
            Debug.Log(
                $"[BrushGestureBuildAdapter] Gesture={result.gestureName}, Score={result.score:F3}, Start building: {binding.buildableItem.DisplayName}",
                this
            );
        }

        pendingBuildRoutine = null;
    }

    private bool TryPayInkCost(GestureBuildBinding binding)
    {
        if (!consumeInk)
            return true;

        if (binding == null)
            return false;

        int cost = Mathf.Max(0, binding.inkCost);

        if (cost <= 0)
            return true;

        if (inkReceiver == null)
        {
            ResolveInkReceiver();
        }

        if (inkReceiver == null)
        {
            Debug.LogWarning(
                $"[BrushGestureBuildAdapter] Ink receiver not found. Build blocked. Gesture={binding.gestureName}",
                this
            );

            return false;
        }

        if (!inkReceiver.TryConsumeInk(cost))
        {
            Debug.Log(
                $"[BrushGestureBuildAdapter] Not enough ink. Gesture={binding.gestureName}, Cost={cost}",
                this
            );

            return false;
        }

        return true;
    }

    private void ResolveBuildModeController()
    {
        if (buildModeController != null)
        {
            return;
        }

        buildModeController = BuildModeController.Instance;

        if (buildModeController == null)
        {
            buildModeController = FindAnyObjectByType<BuildModeController>();
        }
    }

    private void ResolveInkReceiver()
    {
        inkReceiver = null;

        GameObject searchObject = caster != null
            ? caster
            : gameObject;

        if (searchObject == null)
            return;

        MonoBehaviour[] parentBehaviours =
            searchObject.GetComponentsInParent<MonoBehaviour>(true);

        for (int i = 0; i < parentBehaviours.Length; i++)
        {
            if (parentBehaviours[i] is IBrushSkillCostReceiver receiver)
            {
                inkReceiver = receiver;
                return;
            }
        }

        MonoBehaviour[] childBehaviours =
            searchObject.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < childBehaviours.Length; i++)
        {
            if (childBehaviours[i] is IBrushSkillCostReceiver receiver)
            {
                inkReceiver = receiver;
                return;
            }
        }
    }
}
