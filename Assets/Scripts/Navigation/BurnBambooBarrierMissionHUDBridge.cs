using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BurnBambooBarrierMissionHUDBridge : MonoBehaviour
{
    [Serializable]
    private sealed class MissionHUDStep
    {
        public string objectiveId;
        public string missionDescription;
        public string nextStepHint;
        public bool followTarget;
        public GameObject targetObject;
        public string targetObjectName;
        public HUDNavigationCueKind navigationCueKind = HUDNavigationCueKind.Secondary;
    }

    [Header("Bindings")]
    [SerializeField] private BurnBambooBarrierQuestService questService;
    [SerializeField] private MissionTaskHUDPanel missionPanel;

    [Header("Mission")]
    [SerializeField] private string missionTitle = "Bamboo Blight";
    [SerializeField] private MissionTaskHUDCategory missionCategory = MissionTaskHUDCategory.SecondaryTask;

    [Header("Steps")]
    [SerializeField] private MissionHUDStep[] steps =
    {
        new()
        {
            objectiveId = BurnBambooBarrierQuestService.TalkToForestKeeperObjectiveId,
            missionDescription = "Strange rustling ahead",
            nextStepHint = "Speak to the Keeper",
            followTarget = true,
            targetObjectName = "Forest Keeper Dialogue Capsule",
            navigationCueKind = HUDNavigationCueKind.Secondary
        },
        new()
        {
            objectiveId = BurnBambooBarrierQuestService.ObtainBasicFireSkillObjectiveId,
            missionDescription = "Trace the fire glyph",
            nextStepHint = "Draw the flame sigil",
            followTarget = false,
            navigationCueKind = HUDNavigationCueKind.Secondary
        },
        new()
        {
            objectiveId = BurnBambooBarrierQuestService.HitBambooBugWithFireObjectiveId,
            missionDescription = "A bamboo bug blocks path",
            nextStepHint = "Burn the false bamboo",
            followTarget = true,
            targetObjectName = "Bamboo Bug Barrier Gate",
            navigationCueKind = HUDNavigationCueKind.Secondary
        }
    };

    [Header("Completed")]
    [SerializeField] private bool hideNavigationWhenCompleted = true;
    [SerializeField] private string completedDescription = "Barrier burned away";
    [SerializeField] private string completedHint = "Path is open";

    private GameObject trackedTarget;

    private void Awake()
    {
        ResolveBindings();
    }

    private void OnEnable()
    {
        ResolveBindings();

        if (questService != null)
        {
            questService.StateChanged += RefreshMissionHUD;
        }

        RefreshMissionHUD();
    }

    private void Start()
    {
        RefreshMissionHUD();
    }

    private void OnDisable()
    {
        if (questService != null)
        {
            questService.StateChanged -= RefreshMissionHUD;
        }

        StopCurrentNavigation();
    }

    public void RefreshMissionHUD()
    {
        ResolveBindings();

        if (missionPanel == null)
        {
            return;
        }

        if (questService != null && questService.IsCompleted)
        {
            missionPanel.SetMission(missionTitle, completedDescription, completedHint, missionCategory);

            if (hideNavigationWhenCompleted)
            {
                StopCurrentNavigation();
            }

            return;
        }

        MissionHUDStep step = FindCurrentStep();
        if (step == null)
        {
            return;
        }

        missionPanel.SetMission(
            missionTitle,
            step.missionDescription,
            step.nextStepHint,
            missionCategory);

        ApplyNavigation(step);
    }

    private void ApplyNavigation(MissionHUDStep step)
    {
        if (!step.followTarget)
        {
            StopCurrentNavigation();
            return;
        }

        GameObject targetObject = ResolveTarget(step);
        if (targetObject == null)
        {
            StopCurrentNavigation();
            return;
        }

        if (trackedTarget != null && trackedTarget != targetObject)
        {
            HUDNavigation.StopFollowing(trackedTarget);
        }

        trackedTarget = targetObject;
        HUDNavigation.StartFollowing(targetObject, step.navigationCueKind);
    }

    private MissionHUDStep FindCurrentStep()
    {
        if (steps == null || steps.Length == 0)
        {
            return null;
        }

        string activeObjectiveId = questService != null ? questService.ActiveObjectiveId : string.Empty;
        if (string.IsNullOrWhiteSpace(activeObjectiveId))
        {
            activeObjectiveId = InferObjectiveId();
        }

        foreach (MissionHUDStep step in steps)
        {
            if (step != null && string.Equals(step.objectiveId, activeObjectiveId, StringComparison.Ordinal))
            {
                return step;
            }
        }

        return steps[0];
    }

    private string InferObjectiveId()
    {
        if (questService == null)
        {
            return BurnBambooBarrierQuestService.TalkToForestKeeperObjectiveId;
        }

        if (!questService.IsObjectiveCompleted(BurnBambooBarrierQuestService.TalkToForestKeeperObjectiveId))
        {
            return BurnBambooBarrierQuestService.TalkToForestKeeperObjectiveId;
        }

        if (!questService.IsObjectiveCompleted(BurnBambooBarrierQuestService.ObtainBasicFireSkillObjectiveId))
        {
            return BurnBambooBarrierQuestService.ObtainBasicFireSkillObjectiveId;
        }

        return BurnBambooBarrierQuestService.HitBambooBugWithFireObjectiveId;
    }

    private GameObject ResolveTarget(MissionHUDStep step)
    {
        if (step.targetObject != null)
        {
            return step.targetObject;
        }

        if (!string.IsNullOrWhiteSpace(step.targetObjectName))
        {
            GameObject namedTarget = FindSceneObjectByName(step.targetObjectName);
            if (namedTarget != null)
            {
                step.targetObject = namedTarget;
                return namedTarget;
            }
        }

        if (string.Equals(
                step.objectiveId,
                BurnBambooBarrierQuestService.HitBambooBugWithFireObjectiveId,
                StringComparison.Ordinal))
        {
            BambooBugBarrierDamageReceiver barrier =
                FindAnyObjectByType<BambooBugBarrierDamageReceiver>(FindObjectsInactive.Include);
            if (barrier != null)
            {
                step.targetObject = barrier.gameObject;
                return barrier.gameObject;
            }
        }

        return null;
    }

    private void StopCurrentNavigation()
    {
        if (trackedTarget == null)
        {
            return;
        }

        HUDNavigation.StopFollowing(trackedTarget);
        trackedTarget = null;
    }

    private void ResolveBindings()
    {
        if (questService == null)
        {
            questService = BurnBambooBarrierQuestService.Instance != null
                ? BurnBambooBarrierQuestService.Instance
                : FindAnyObjectByType<BurnBambooBarrierQuestService>(FindObjectsInactive.Include);
        }

        if (missionPanel == null)
        {
            missionPanel = FindAnyObjectByType<MissionTaskHUDPanel>(FindObjectsInactive.Include);
        }
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);

        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (sceneTransform.gameObject.scene.IsValid() && sceneTransform.name == objectName)
            {
                return sceneTransform.gameObject;
            }
        }

        return null;
    }
}
