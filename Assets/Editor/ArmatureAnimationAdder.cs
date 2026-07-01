using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ArmatureAnimationAdder
{
    private const string SourceControllerPath = "Assets/Paddle Boarding Animation/Assets/Animator Controller/Armature_Controller.controller";
    private const string OutputFolder = "Assets/Generated/AnimationControllers";
    private const string AddedStateMachineName = "Armature Added Animations";
    private const string TriggerName = "PlayArmature";

    [MenuItem("Tools/Animation/Add Armature Animations To Selected NPC", true)]
    private static bool ValidateAddArmatureAnimations()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem("Tools/Animation/Add Armature Animations To Selected NPC")]
    private static void AddArmatureAnimations()
    {
        GameObject selected = Selection.activeGameObject;
        Animator animator = selected.GetComponent<Animator>();

        if (animator == null)
        {
            animator = selected.GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            EditorUtility.DisplayDialog(
                "No Animator Found",
                "Select NPC9 or a child object that has an Animator component.",
                "OK");
            return;
        }

        AnimatorController sourceController = AssetDatabase.LoadAssetAtPath<AnimatorController>(SourceControllerPath);
        if (sourceController == null)
        {
            EditorUtility.DisplayDialog(
                "Source Controller Missing",
                $"Could not find the Armature controller at:\n{SourceControllerPath}",
                "OK");
            return;
        }

        AnimatorController targetController = CreateEditableControllerFor(animator);
        if (targetController == null)
        {
            return;
        }

        Undo.RecordObject(animator, "Assign merged animator controller");
        animator.runtimeAnimatorController = targetController;

        AddTriggerIfMissing(targetController, TriggerName);
        CopySourceControllerIntoTarget(sourceController, targetController);

        EditorUtility.SetDirty(targetController);
        EditorUtility.SetDirty(animator);
        EditorSceneManager.MarkSceneDirty(animator.gameObject.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string rigWarning = string.Empty;
        if (animator.avatar == null || !animator.avatar.isHuman)
        {
            rigWarning = "\n\nImportant: NPC9 must use a Humanoid Avatar for these Armature Humanoid clips to retarget correctly.";
        }

        EditorUtility.DisplayDialog(
            "Armature Animations Added",
            $"Added Armature states to:\n{AssetDatabase.GetAssetPath(targetController)}\n\nUse Animator trigger '{TriggerName}' to enter the added animation chain.{rigWarning}",
            "OK");
    }

    private static AnimatorController CreateEditableControllerFor(Animator animator)
    {
        Directory.CreateDirectory(OutputFolder);

        RuntimeAnimatorController runtimeController = animator.runtimeAnimatorController;
        if (runtimeController is AnimatorController currentController)
        {
            string sourcePath = AssetDatabase.GetAssetPath(currentController);
            string copyPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{OutputFolder}/{animator.gameObject.name}_{currentController.name}_WithArmature.controller");

            if (!AssetDatabase.CopyAsset(sourcePath, copyPath))
            {
                EditorUtility.DisplayDialog(
                    "Copy Failed",
                    $"Could not copy controller:\n{sourcePath}",
                    "OK");
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<AnimatorController>(copyPath);
        }

        string newPath = AssetDatabase.GenerateUniqueAssetPath(
            $"{OutputFolder}/{animator.gameObject.name}_WithArmature.controller");
        return AnimatorController.CreateAnimatorControllerAtPath(newPath);
    }

    private static void AddTriggerIfMissing(AnimatorController controller, string triggerName)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == triggerName)
            {
                return;
            }
        }

        controller.AddParameter(triggerName, AnimatorControllerParameterType.Trigger);
    }

    private static void CopySourceControllerIntoTarget(AnimatorController sourceController, AnimatorController targetController)
    {
        if (sourceController.layers.Length == 0 || targetController.layers.Length == 0)
        {
            return;
        }

        AnimatorStateMachine sourceRoot = sourceController.layers[0].stateMachine;
        AnimatorStateMachine targetRoot = targetController.layers[0].stateMachine;

        RemoveExistingAddedStateMachine(targetRoot);

        AnimatorStateMachine addedRoot = targetRoot.AddStateMachine(AddedStateMachineName, new Vector3(420, 0, 0));
        Dictionary<AnimatorState, AnimatorState> stateMap = new Dictionary<AnimatorState, AnimatorState>();

        foreach (ChildAnimatorState sourceChild in sourceRoot.states)
        {
            AnimatorState copiedState = addedRoot.AddState(sourceChild.state.name, sourceChild.position);
            CopyStateSettings(sourceChild.state, copiedState);
            stateMap.Add(sourceChild.state, copiedState);
        }

        foreach (ChildAnimatorState sourceChild in sourceRoot.states)
        {
            AnimatorState copiedState = stateMap[sourceChild.state];
            foreach (AnimatorStateTransition sourceTransition in sourceChild.state.transitions)
            {
                if (sourceTransition.destinationState == null || !stateMap.TryGetValue(sourceTransition.destinationState, out AnimatorState copiedDestination))
                {
                    continue;
                }

                AnimatorStateTransition copiedTransition = copiedState.AddTransition(copiedDestination);
                CopyTransitionSettings(sourceTransition, copiedTransition);
            }
        }

        if (sourceRoot.defaultState != null && stateMap.TryGetValue(sourceRoot.defaultState, out AnimatorState defaultState))
        {
            addedRoot.defaultState = defaultState;

            AnimatorStateTransition playTransition = targetRoot.AddAnyStateTransition(defaultState);
            playTransition.hasExitTime = false;
            playTransition.duration = 0.15f;
            playTransition.AddCondition(AnimatorConditionMode.If, 0f, TriggerName);
        }
    }

    private static void RemoveExistingAddedStateMachine(AnimatorStateMachine targetRoot)
    {
        foreach (ChildAnimatorStateMachine childStateMachine in targetRoot.stateMachines)
        {
            if (childStateMachine.stateMachine.name == AddedStateMachineName)
            {
                HashSet<AnimatorState> oldStates = new HashSet<AnimatorState>();
                foreach (ChildAnimatorState childState in childStateMachine.stateMachine.states)
                {
                    oldStates.Add(childState.state);
                }

                foreach (AnimatorStateTransition transition in targetRoot.anyStateTransitions)
                {
                    if (transition.destinationState != null && oldStates.Contains(transition.destinationState))
                    {
                        targetRoot.RemoveAnyStateTransition(transition);
                    }
                }

                targetRoot.RemoveStateMachine(childStateMachine.stateMachine);
                return;
            }
        }
    }

    private static void CopyStateSettings(AnimatorState source, AnimatorState destination)
    {
        destination.motion = source.motion;
        destination.speed = source.speed;
        destination.cycleOffset = source.cycleOffset;
        destination.iKOnFeet = source.iKOnFeet;
        destination.writeDefaultValues = source.writeDefaultValues;
        destination.mirror = source.mirror;
        destination.tag = source.tag;
    }

    private static void CopyTransitionSettings(AnimatorStateTransition source, AnimatorStateTransition destination)
    {
        destination.name = source.name;
        destination.solo = source.solo;
        destination.mute = source.mute;
        destination.isExit = source.isExit;
        destination.duration = source.duration;
        destination.offset = source.offset;
        destination.exitTime = source.exitTime;
        destination.hasExitTime = source.hasExitTime;
        destination.hasFixedDuration = source.hasFixedDuration;
        destination.interruptionSource = source.interruptionSource;
        destination.orderedInterruption = source.orderedInterruption;
        destination.canTransitionToSelf = source.canTransitionToSelf;

        foreach (AnimatorCondition condition in source.conditions)
        {
            destination.AddCondition(condition.mode, condition.threshold, condition.parameter);
        }
    }
}
