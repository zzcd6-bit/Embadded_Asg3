using System;

public interface IReactableStateNotifier
{
    event Action<IReactable> StateChanged;
}
