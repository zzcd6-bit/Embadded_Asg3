using System.Collections;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class BambooBugBarrierDamageReceiver : MonoBehaviour, IDamageable, IGameSaveModule
{
    [Header("Gate")]
    [SerializeField] private MazeGate gate;
    [SerializeField] private MazeGateTriggerOpener gateOpener;
    [SerializeField] private Transform feedbackShakeTarget;

    [Header("Quest")]
    [SerializeField] private BurnBambooBarrierQuestService questService;
    [SerializeField] private bool requireQuestActive = true;
    [SerializeField] private bool saveImmediatelyOnOpen = true;

    [Header("Element")]
    [SerializeField] private ElementType requiredElement = ElementType.Fire;
    [SerializeField] private bool acceptFireInfusedAttacker;

    [Header("Feedback")]
    [SerializeField] private string normalHitMessage = "Ordinary attacks cannot break this disguise.";
    [SerializeField] private string openedMessage = "The bamboo grub's disguise is broken. The path ahead is open.";
    [SerializeField] private float normalHitFeedbackDelay = 0.05f;
    [SerializeField] private bool showDialogueSystemAlerts = true;
    [SerializeField] private bool logHits = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onNormalHit = new();
    [SerializeField] private UnityEvent onOpened = new();

    private bool isOpened;
    private Vector3 shakeStartLocalPosition;
    private float shakeTimer;
    private Coroutine pendingNormalHitFeedback;

    private void Awake()
    {
        if (gate == null)
        {
            gate = GetComponent<MazeGate>();
        }

        if (gateOpener == null)
        {
            gateOpener = GetComponentInChildren<MazeGateTriggerOpener>(true);
        }

        if (feedbackShakeTarget == null)
        {
            feedbackShakeTarget = gate != null ? gate.transform : transform;
        }

        if (questService == null)
        {
            questService = BurnBambooBarrierQuestService.GetOrCreate();
        }

        if (feedbackShakeTarget != null)
        {
            shakeStartLocalPosition = feedbackShakeTarget.localPosition;
        }
    }

    private void OnEnable()
    {
        if (questService == null)
        {
            questService = BurnBambooBarrierQuestService.GetOrCreate();
        }

        if (questService != null)
        {
            questService.StateChanged += RefreshFromQuestState;
        }
    }

    private void Start()
    {
        RefreshFromQuestState();
    }

    private void OnDisable()
    {
        if (questService != null)
        {
            questService.StateChanged -= RefreshFromQuestState;
        }
    }

    private void Update()
    {
        if (shakeTimer <= 0f || feedbackShakeTarget == null)
        {
            return;
        }

        shakeTimer -= Time.deltaTime;
        float strength = Mathf.Clamp01(shakeTimer / 0.18f);
        float offset = Mathf.Sin(Time.time * 80f) * 0.035f * strength;
        feedbackShakeTarget.localPosition = shakeStartLocalPosition + Vector3.right * offset;

        if (shakeTimer <= 0f)
        {
            feedbackShakeTarget.localPosition = shakeStartLocalPosition;
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isOpened)
        {
            return;
        }

        if (requireQuestActive && questService != null && !questService.IsActive)
        {
            ShowFeedback(normalHitMessage);
            return;
        }

        if (!IsRequiredElementHit(damageInfo))
        {
            ScheduleNormalHitFeedback();
            return;
        }

        OpenByRequiredElement(damageInfo);
    }

    public void CaptureGameSaveData(GameSaveData saveData)
    {
        if (!isOpened || saveData == null)
        {
            return;
        }

        if (saveData.worldState == null)
        {
            saveData.worldState = new WorldStateSaveData();
        }

        if (!saveData.worldState.openedChestIds.Contains(BurnBambooBarrierQuestService.BambooBarrierOpenedSaveToken))
        {
            saveData.worldState.openedChestIds.Add(BurnBambooBarrierQuestService.BambooBarrierOpenedSaveToken);
        }
    }

    public void RestoreGameSaveData(GameSaveData saveData)
    {
        if (saveData == null || saveData.worldState == null || saveData.worldState.openedChestIds == null)
        {
            return;
        }

        if (saveData.worldState.openedChestIds.Contains(BurnBambooBarrierQuestService.BambooBarrierOpenedSaveToken))
        {
            ApplyOpenedInstant();
        }
    }

    [ContextMenu("Debug/Open Barrier")]
    public void OpenByRequiredElement()
    {
        OpenByRequiredElement(null);
    }

    private void OpenByRequiredElement(DamageInfo? damageInfo)
    {
        if (isOpened)
        {
            return;
        }

        if (questService == null)
        {
            questService = BurnBambooBarrierQuestService.GetOrCreate();
        }

        if (!OpenGateThroughUnifiedOpener(damageInfo))
        {
            ShowFeedback(normalHitMessage);
            return;
        }

        CancelPendingNormalHitFeedback();
        isOpened = true;
        questService?.CompleteBambooBarrierObjectiveAndQuest();

        ShowFeedback(openedMessage);
        onOpened?.Invoke();

        if (saveImmediatelyOnOpen && PlayerSaveManager.Instance != null)
        {
            PlayerSaveManager.Instance.SavePlayer();
        }
    }

    private void RefreshFromQuestState()
    {
        if (questService != null && questService.IsBambooBarrierOpened && !isOpened)
        {
            ApplyOpenedInstant();
        }
    }

    private void ApplyOpenedInstant()
    {
        isOpened = true;

        if (gate != null)
        {
            gate.ResetOpenInstant();
        }
    }

    private bool IsRequiredElementHit(DamageInfo damageInfo)
    {
        if (damageInfo.element == requiredElement)
        {
            return true;
        }

        if (!acceptFireInfusedAttacker || requiredElement != ElementType.Fire || damageInfo.attacker == null)
        {
            return false;
        }

        PlayerElementInfusion infusion = damageInfo.attacker.GetComponent<PlayerElementInfusion>();
        if (infusion == null)
        {
            infusion = damageInfo.attacker.GetComponentInParent<PlayerElementInfusion>();
        }

        return infusion != null && infusion.IsFireInfused;
    }

    private bool OpenGateThroughUnifiedOpener(DamageInfo? damageInfo)
    {
        Vector3 openerPosition = transform.position;
        Object opener = this;

        if (damageInfo.HasValue && damageInfo.Value.attacker != null)
        {
            opener = damageInfo.Value.attacker;
            openerPosition = damageInfo.Value.attacker.transform.position;
        }

        if (gateOpener == null)
        {
            gateOpener = GetComponentInChildren<MazeGateTriggerOpener>(true);
        }

        if (gateOpener != null)
        {
            return gateOpener.OpenFromPosition(openerPosition, opener);
        }

        Debug.LogWarning("[BambooBugBarrier] Missing MazeGateTriggerOpener. Barrier opening is intentionally routed through the unified gate opener.", this);
        return false;
    }

    private void PlayNormalHitFeedback()
    {
        if (isOpened)
        {
            return;
        }

        shakeTimer = 0.18f;
        ShowFeedback(normalHitMessage);
        onNormalHit?.Invoke();
    }

    private void ScheduleNormalHitFeedback()
    {
        CancelPendingNormalHitFeedback();

        if (normalHitFeedbackDelay <= 0f)
        {
            PlayNormalHitFeedback();
            return;
        }

        pendingNormalHitFeedback = StartCoroutine(PlayNormalHitFeedbackAfterDelay());
    }

    private IEnumerator PlayNormalHitFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(normalHitFeedbackDelay);
        pendingNormalHitFeedback = null;
        PlayNormalHitFeedback();
    }

    private void CancelPendingNormalHitFeedback()
    {
        if (pendingNormalHitFeedback == null)
        {
            return;
        }

        StopCoroutine(pendingNormalHitFeedback);
        pendingNormalHitFeedback = null;
    }

    private void ShowFeedback(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (showDialogueSystemAlerts && DialogueManager.instance != null)
        {
            DialogueManager.ShowAlert(message);
        }

        if (logHits)
        {
            Debug.Log("[BambooBugBarrier] " + message, this);
        }
    }
}
