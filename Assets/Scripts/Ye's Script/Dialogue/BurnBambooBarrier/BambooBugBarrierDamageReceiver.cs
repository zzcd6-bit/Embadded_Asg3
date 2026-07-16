using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class BambooBugBarrierDamageReceiver : MonoBehaviour, IDamageable, IGameSaveModule
{
    [Header("Gate")]
    [SerializeField] private MazeGate gate;
    [SerializeField] private Transform feedbackShakeTarget;

    [Header("Quest")]
    [SerializeField] private BurnBambooBarrierQuestService questService;
    [SerializeField] private bool requireQuestActive = true;
    [SerializeField] private bool saveImmediatelyOnOpen = true;

    [Header("Element")]
    [SerializeField] private ElementType requiredElement = ElementType.Fire;
    [SerializeField] private bool acceptFireInfusedAttacker = true;

    [Header("Feedback")]
    [SerializeField] private string normalHitMessage = "普通攻击无法破坏这层伪装。";
    [SerializeField] private string openedMessage = "竹虫的伪装已经解除，前方道路已开放。";
    [SerializeField] private bool showDialogueSystemAlerts = true;
    [SerializeField] private bool logHits = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onNormalHit = new();
    [SerializeField] private UnityEvent onOpened = new();

    private bool isOpened;
    private Vector3 shakeStartLocalPosition;
    private float shakeTimer;

    private void Awake()
    {
        if (gate == null)
        {
            gate = GetComponent<MazeGate>();
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
            PlayNormalHitFeedback();
            return;
        }

        OpenByRequiredElement();
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
        if (isOpened)
        {
            return;
        }

        isOpened = true;

        if (questService == null)
        {
            questService = BurnBambooBarrierQuestService.GetOrCreate();
        }

        questService?.CompleteBambooBarrierObjectiveAndQuest();

        if (gate != null)
        {
            gate.SetOpen();
        }

        ShowFeedback(openedMessage);
        onOpened?.Invoke();

        if (saveImmediatelyOnOpen && PlayerSaveManager.Instance != null)
        {
            PlayerSaveManager.Instance.SavePlayer();
        }
    }

    private void RefreshFromQuestState()
    {
        if (questService != null && questService.IsBambooBarrierOpened)
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

    private void PlayNormalHitFeedback()
    {
        shakeTimer = 0.18f;
        ShowFeedback(normalHitMessage);
        onNormalHit?.Invoke();
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
