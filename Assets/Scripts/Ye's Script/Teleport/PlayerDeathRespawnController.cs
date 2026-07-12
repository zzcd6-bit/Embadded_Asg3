using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerDeathRespawnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerResourceController resourceController;
    [SerializeField] private PlayerTeleportService teleportService;
    [SerializeField] private TeleportPointRegistry teleportPointRegistry;
    [SerializeField] private ActionPlayerController actionPlayerController;
    [SerializeField] private PlayerController legacyPlayerController;

    [Header("Respawn")]
    [SerializeField] private bool autoRespawn = true;
    [SerializeField] private float respawnDelay = 1.5f;
    [SerializeField] private bool fullHealOnRespawn = true;
    [SerializeField] private bool fullRestoreInkOnRespawn;

    private Coroutine respawnRoutine;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener<CharacterDeadEventInfo>(
            E_EventType.E_Player_Dead,
            OnPlayerDead
        );
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<CharacterDeadEventInfo>(
            E_EventType.E_Player_Dead,
            OnPlayerDead
        );
    }

    public void RespawnNow()
    {
        ResolveReferences();

        TeleportPoint respawnPoint = teleportPointRegistry != null
            ? teleportPointRegistry.CurrentRespawnPoint
            : null;

        SetPlayerControl(false);

        if (respawnPoint != null && teleportService != null)
        {
            teleportService.TeleportTo(respawnPoint.SpawnTransform);
            teleportPointRegistry.SetRespawnPoint(respawnPoint);
        }

        if (resourceController != null)
        {
            resourceController.Revive(fullHealOnRespawn, fullRestoreInkOnRespawn);
        }

        SetPlayerControl(true);
    }

    private void OnPlayerDead(CharacterDeadEventInfo eventInfo)
    {
        if (!autoRespawn)
            return;

        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
        }

        respawnRoutine = StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        SetPlayerControl(false);

        if (respawnDelay > 0f)
        {
            yield return new WaitForSeconds(respawnDelay);
        }

        RespawnNow();
        respawnRoutine = null;
    }

    private void ResolveReferences()
    {
        if (resourceController == null)
        {
            resourceController = GetComponent<PlayerResourceController>();
        }

        if (resourceController == null)
        {
            resourceController = GetComponentInChildren<PlayerResourceController>(true);
        }

        if (resourceController == null)
        {
            resourceController = PlayerResourceController.Instance;
        }

        if (teleportService == null)
        {
            teleportService = GetComponent<PlayerTeleportService>();
        }

        if (teleportService == null)
        {
            teleportService = GetComponentInChildren<PlayerTeleportService>(true);
        }

        if (teleportService == null)
        {
            teleportService = PlayerTeleportService.Instance;
        }

        if (teleportPointRegistry == null)
        {
            teleportPointRegistry = TeleportPointRegistry.Instance;
        }

        if (teleportPointRegistry == null)
        {
            teleportPointRegistry = FindAnyObjectByType<TeleportPointRegistry>();
        }

        if (actionPlayerController == null)
        {
            actionPlayerController = GetComponent<ActionPlayerController>();
        }

        if (actionPlayerController == null)
        {
            actionPlayerController = GetComponentInChildren<ActionPlayerController>(true);
        }

        if (legacyPlayerController == null)
        {
            legacyPlayerController = GetComponent<PlayerController>();
        }

        if (legacyPlayerController == null)
        {
            legacyPlayerController = GetComponentInChildren<PlayerController>(true);
        }
    }

    private void SetPlayerControl(bool enabled)
    {
        if (actionPlayerController != null)
        {
            actionPlayerController.SetGameplayControlEnabled(enabled);
        }

        if (legacyPlayerController != null)
        {
            legacyPlayerController.SetControl(enabled);
        }
    }
}
