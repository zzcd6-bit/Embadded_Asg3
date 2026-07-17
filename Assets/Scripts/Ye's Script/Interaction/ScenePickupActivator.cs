using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.Events;

public class ScenePickupActivator : MonoBehaviour
{
    [SerializeField] private GameObject pickupObject;
    [SerializeField] private Transform spawnAnchor;
    [SerializeField] private Vector3 spawnOffset = new(1.5f, 0.65f, 0f);
    [SerializeField] private string pickupPrompt = "Pick Up";
    [SerializeField] private string activationAlert;
    [SerializeField] private string collectedAlert;
    [SerializeField] private bool hidePickupOnAwake = true;
    [SerializeField] private bool logStateChanges = true;
    [SerializeField] private UnityEvent onPickupCollected = new();

    private ReactableObject pickupReactable;
    private bool waitingForPickup;

    private void Awake()
    {
        ResolvePickupReactable();

        if (hidePickupOnAwake && pickupObject != null)
        {
            pickupObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!waitingForPickup || pickupObject == null)
        {
            return;
        }

        if (!pickupObject.activeInHierarchy)
        {
            CompletePickup();
        }
    }

    public void ActivatePickup()
    {
        if (pickupObject == null)
        {
            Debug.LogWarning("[ScenePickupActivator] Pickup object is missing.", this);
            return;
        }

        if (spawnAnchor != null)
        {
            pickupObject.transform.position = spawnAnchor.position + spawnOffset;
        }

        pickupObject.SetActive(true);
        ResolvePickupReactable();

        if (pickupReactable != null)
        {
            pickupReactable.SetInteractable(true);
            pickupReactable.SetOptionName(pickupPrompt);
        }

        waitingForPickup = true;
        ShowAlert(activationAlert);
        Log("Pickup activated.");
    }

    private void CompletePickup()
    {
        waitingForPickup = false;
        onPickupCollected?.Invoke();
        ShowAlert(collectedAlert);
        Log("Pickup collected.");
    }

    private void ResolvePickupReactable()
    {
        if (pickupObject != null && pickupReactable == null)
        {
            pickupReactable = pickupObject.GetComponent<ReactableObject>();
        }
    }

    private void ShowAlert(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || DialogueManager.instance == null)
        {
            return;
        }

        DialogueManager.ShowAlert(message);
    }

    private void Log(string message)
    {
        if (!logStateChanges)
        {
            return;
        }

        Debug.Log("[ScenePickupActivator] " + message, this);
    }
}
