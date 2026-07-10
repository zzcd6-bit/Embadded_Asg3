using UnityEngine;

public class YeInteractionDebugAction : MonoBehaviour
{
    [SerializeField] private ReactableObject reactableObject;
    [SerializeField] private bool disableReactableAfterUse = true;
    [SerializeField] private bool destroyAfterUse;

    public void Run(GameObject interactor)
    {
        Debug.Log($"[YeInteractionDebugAction] {interactor.name} interacted with {gameObject.name}.", this);

        if (disableReactableAfterUse && reactableObject != null)
        {
            reactableObject.SetInteractable(false);
        }

        if (destroyAfterUse)
        {
            Destroy(gameObject);
        }
    }
}
