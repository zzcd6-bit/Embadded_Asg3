using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(ReactableObject))]
public class InventoryPickupItem : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField]
    private string pickupId;

    [Header("Item")]
    [SerializeField]
    private ItemData itemData;

    [SerializeField]
    [Min(1)]
    private int quantity = 1;

    [Header("Interaction")]
    [SerializeField]
    private bool automaticallySetOptionName = true;

    [SerializeField]
    private string optionPrefix = "Pick Up";

    [Header("After Pickup")]
    [SerializeField]
    private bool deactivateAfterPickup = true;

    [SerializeField]
    private bool destroyAfterPickup;

    [Header("Events")]
    [SerializeField]
    private UnityEvent onPickedUp =
        new UnityEvent();

    [Header("Debug")]
    [SerializeField]
    private bool debugLog;

    private ReactableObject reactableObject;

    private bool hasBeenCollected;

    public string PickupId
    {
        get { return pickupId; }
    }

    public ItemData ItemData
    {
        get { return itemData; }
    }

    private void Awake()
    {
        reactableObject =
            GetComponent<ReactableObject>();

        if (reactableObject != null)
        {
            reactableObject.OnInteractEvent
                .AddListener(HandleInteract);
        }

        RefreshOptionName();
    }

    private void OnEnable()
    {
        InventoryPickupPersistence.DataLoaded +=
            ApplySavedState;

        ApplySavedState();
    }

    private void OnDisable()
    {
        InventoryPickupPersistence.DataLoaded -=
            ApplySavedState;
    }

    private void OnDestroy()
    {
        if (reactableObject != null)
        {
            reactableObject.OnInteractEvent
                .RemoveListener(HandleInteract);
        }

        InventoryPickupPersistence.DataLoaded -=
            ApplySavedState;
    }

    private void HandleInteract(
        GameObject interactor
    )
    {
        if (hasBeenCollected ||
            itemData == null ||
            interactor == null)
        {
            return;
        }

        PlayerInventoryController inventory =
            FindInventoryController(interactor);

        if (inventory == null)
        {
            Debug.LogWarning(
                "[InventoryPickupItem] " +
                "PlayerInventoryController not found " +
                $"on interactor: {interactor.name}",
                this
            );

            return;
        }

        InventoryItemEntry addedEntry =
            inventory.AddItem(
                itemData,
                Mathf.Max(1, quantity)
            );

        if (addedEntry == null)
            return;

        hasBeenCollected = true;

        InventoryPickupPersistence.MarkCollected(
            GetResolvedPickupId()
        );

        if (reactableObject != null)
        {
            reactableObject.SetInteractable(false);
        }

        onPickedUp?.Invoke();

        if (debugLog)
        {
            Debug.Log(
                "[InventoryPickupItem] " +
                $"Picked up {itemData.DisplayName} " +
                $"x{quantity}",
                this
            );
        }

        HideCollectedObject();
    }

    private PlayerInventoryController
        FindInventoryController(
            GameObject interactor
        )
    {
        PlayerInventoryController inventory =
            interactor.GetComponent<
                PlayerInventoryController>();

        if (inventory == null)
        {
            inventory =
                interactor.GetComponentInParent<
                    PlayerInventoryController>();
        }

        if (inventory == null)
        {
            inventory =
                interactor.GetComponentInChildren<
                    PlayerInventoryController>(true);
        }

        return inventory;
    }

    private void ApplySavedState()
    {
        if (!InventoryPickupPersistence
                .IsCollected(
                    GetResolvedPickupId()))
        {
            return;
        }

        hasBeenCollected = true;

        if (reactableObject != null)
        {
            reactableObject.SetInteractable(false);
        }

        HideCollectedObject();
    }

    private void HideCollectedObject()
    {
        if (destroyAfterPickup)
        {
            Destroy(gameObject);
            return;
        }

        if (deactivateAfterPickup)
        {
            gameObject.SetActive(false);
        }
    }

    private void RefreshOptionName()
    {
        if (!automaticallySetOptionName ||
            reactableObject == null ||
            itemData == null)
        {
            return;
        }

        reactableObject.SetOptionName(
            $"{optionPrefix} " +
            $"{itemData.DisplayName}"
        );
    }

    private string GetResolvedPickupId()
    {
        if (!string.IsNullOrWhiteSpace(pickupId))
        {
            return pickupId;
        }

        string scenePath =
            gameObject.scene.path;

        string hierarchyPath =
            BuildHierarchyPath(transform);

        string itemId =
            itemData != null
                ? itemData.ItemId
                : "NoItem";

        return
            $"{scenePath}|" +
            $"{hierarchyPath}|" +
            $"{itemId}";
    }

    private string BuildHierarchyPath(
        Transform target
    )
    {
        if (target == null)
            return string.Empty;

        string path =
            $"{target.name}[{target.GetSiblingIndex()}]";

        Transform parent =
            target.parent;

        while (parent != null)
        {
            path =
                $"{parent.name}" +
                $"[{parent.GetSiblingIndex()}]/" +
                path;

            parent = parent.parent;
        }

        return path;
    }

    [ContextMenu("Generate New Pickup ID")]
    private void GenerateNewPickupId()
    {
        pickupId =
            Guid.NewGuid().ToString("N");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        quantity =
            Mathf.Max(1, quantity);

        if (string.IsNullOrWhiteSpace(pickupId))
        {
            pickupId =
                Guid.NewGuid().ToString("N");
        }

        if (!Application.isPlaying)
        {
            reactableObject =
                GetComponent<ReactableObject>();

            RefreshOptionName();
        }
    }
#endif
}