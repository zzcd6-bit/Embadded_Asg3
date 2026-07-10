using UnityEngine;

public enum InteractionCategory
{
    Critical = 0,
    Normal = 1,
    Pickup = 2
}

public interface IReactable
{
    string OptionName { get; }
    InteractionCategory Category { get; }
    Transform InteractionPoint { get; }

    bool CanInteract(GameObject interactor);
    void Interact(GameObject interactor);
    void OnSelected();
    void OnDeselected();
}
