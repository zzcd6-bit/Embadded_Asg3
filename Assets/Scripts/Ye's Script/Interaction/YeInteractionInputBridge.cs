using UnityEngine;

[RequireComponent(typeof(InteractionManager))]
public class YeInteractionInputBridge : MonoBehaviour
{
    [SerializeField] private InteractionManager interactionManager;
    [SerializeField] private KeyCode primaryInteractKey = KeyCode.Return;
    [SerializeField] private KeyCode alternativeInteractKey = KeyCode.E;

    private EventCenter cachedEventCenter;
    private InputMgr cachedInputMgr;

    private void Reset()
    {
        interactionManager = GetComponent<InteractionManager>();
    }

    private void OnEnable()
    {
        if (interactionManager == null)
        {
            interactionManager = GetComponent<InteractionManager>();
        }

        cachedInputMgr = InputMgr.Instance;
        cachedEventCenter = EventCenter.Instance;

        if (cachedInputMgr == null || cachedEventCenter == null)
        {
            Debug.LogWarning("[YeInteractionInputBridge] InputMgr/EventCenter not ready; InteractionManager can still use direct input fallback.", this);
            return;
        }

        cachedInputMgr.ChangeKeyboardInfo(
            E_EventType.E_Interaction_ExecutePrimary,
            primaryInteractKey,
            InputInfo.E_InputType.Down);

        cachedInputMgr.ChangeKeyboardInfo(
            E_EventType.E_Interaction_ExecuteAlternative,
            alternativeInteractKey,
            InputInfo.E_InputType.Down);

        cachedInputMgr.StartOrCloseInputMgr(true);

        cachedEventCenter.AddEventListener(E_EventType.E_Interaction_ExecutePrimary, OnInteractPressed);
        cachedEventCenter.AddEventListener(E_EventType.E_Interaction_ExecuteAlternative, OnInteractPressed);
    }

    private void OnDisable()
    {
        if (cachedEventCenter != null)
        {
            cachedEventCenter.RemoveEventListener(E_EventType.E_Interaction_ExecutePrimary, OnInteractPressed);
            cachedEventCenter.RemoveEventListener(E_EventType.E_Interaction_ExecuteAlternative, OnInteractPressed);
        }

        cachedInputMgr = null;
        cachedEventCenter = null;
    }

    private void OnInteractPressed()
    {
        interactionManager?.SubmitInteractInput();
    }
}
