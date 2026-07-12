using UnityEngine;

[DisallowMultipleComponent]
public class PlayerTeleportService : MonoBehaviour
{
    public static PlayerTeleportService Instance { get; private set; }

    [Header("Target")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Rigidbody playerRigidbody;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PlayerTeleportService] Multiple instances found. Keeping the latest instance.", this);
        }

        Instance = this;
        ResolveReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ResolveReferences()
    {
        if (playerRoot == null)
        {
            playerRoot = transform;
        }

        if (characterController == null)
        {
            characterController = playerRoot.GetComponent<CharacterController>();
        }

        if (characterController == null)
        {
            characterController = playerRoot.GetComponentInChildren<CharacterController>(true);
        }

        if (playerRigidbody == null)
        {
            playerRigidbody = playerRoot.GetComponent<Rigidbody>();
        }

        if (playerRigidbody == null)
        {
            playerRigidbody = playerRoot.GetComponentInChildren<Rigidbody>(true);
        }
    }

    public void TeleportTo(Transform target)
    {
        if (target == null)
            return;

        TeleportTo(target.position, target.rotation);
    }

    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        if (playerRoot == null)
        {
            ResolveReferences();
        }

        if (playerRoot == null)
            return;

        bool hadCharacterController = characterController != null;
        bool characterControllerWasEnabled = false;

        if (hadCharacterController)
        {
            characterControllerWasEnabled = characterController.enabled;
            characterController.enabled = false;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.position = position;
            playerRigidbody.rotation = rotation;
        }

        playerRoot.SetPositionAndRotation(position, rotation);
        Physics.SyncTransforms();

        if (hadCharacterController)
        {
            characterController.enabled = characterControllerWasEnabled;
        }

        EventCenter.Instance.EventTrigger(E_EventType.E_Player_Teleported);
    }
}
