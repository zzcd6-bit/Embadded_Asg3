using System;
using UnityEngine;

public class BuildingDeleteReactable : MonoBehaviour, IReactable, IReactableStateNotifier
{
    [SerializeField] private string optionName = "Delete Building";
    [SerializeField] private InteractionCategory category = InteractionCategory.Normal;
    [SerializeField] private Transform interactionPoint;

    private BuildingInstance buildingInstance;
    private PlacementCommitter placementCommitter;

    public event Action<IReactable> StateChanged;

    public string OptionName => optionName;
    public InteractionCategory Category => category;
    public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;

    private void Awake()
    {
        ResolveReferences();
    }

    public void Initialize(BuildingInstance building, PlacementCommitter committer)
    {
        buildingInstance = building;
        placementCommitter = committer;
        StateChanged?.Invoke(this);
    }

    public bool CanInteract(GameObject interactor)
    {
        ResolveReferences();
        return isActiveAndEnabled
            && gameObject.activeInHierarchy
            && buildingInstance != null
            && buildingInstance.IsPlacementCompleted
            && placementCommitter != null;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        placementCommitter.Delete(buildingInstance);
        StateChanged?.Invoke(this);
    }

    public void OnSelected()
    {
    }

    public void OnDeselected()
    {
    }

    private void OnDisable()
    {
        StateChanged?.Invoke(this);
    }

    private void OnDestroy()
    {
        StateChanged?.Invoke(this);
    }

    private void ResolveReferences()
    {
        if (buildingInstance == null)
        {
            buildingInstance = GetComponentInParent<BuildingInstance>();
        }

        if (placementCommitter == null)
        {
            placementCommitter = FindAnyObjectByType<PlacementCommitter>();
        }
    }
}
