using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerResourceController : MonoBehaviour
{
    public static PlayerResourceController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerDamageReceiver damageReceiver;
    [SerializeField] private PlayerInkPouchController inkPouchController;

    public event Action<int, int> HpChanged;
    public event Action<int, int> InkChanged;
    public event Action Revived;

    private int lastHp = -1;
    private int lastMaxHp = -1;
    private int lastInk = -1;
    private int lastMaxInk = -1;

    public int CurrentHp => damageReceiver != null ? damageReceiver.CurrentHp : 0;
    public int MaxHp => damageReceiver != null ? damageReceiver.MaxHp : 0;
    public int CurrentInk => inkPouchController != null ? inkPouchController.CurrentInk : 0;
    public int MaxInk => inkPouchController != null ? inkPouchController.MaxInk : 0;
    public bool IsDead => damageReceiver != null && damageReceiver.IsDead;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PlayerResourceController] Multiple instances found. Keeping the latest instance.", this);
        }

        Instance = this;
        ResolveReferences();
        NotifyAllChanged();
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener<CharacterDamagedEventInfo>(
            E_EventType.E_Player_Damaged,
            OnPlayerDamaged
        );
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<CharacterDamagedEventInfo>(
            E_EventType.E_Player_Damaged,
            OnPlayerDamaged
        );
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        DetectExternalResourceChanges();
    }

    public void ResolveReferences()
    {
        if (damageReceiver == null)
        {
            damageReceiver = GetComponent<PlayerDamageReceiver>();
        }

        if (damageReceiver == null)
        {
            damageReceiver = GetComponentInChildren<PlayerDamageReceiver>(true);
        }

        if (inkPouchController == null)
        {
            inkPouchController = GetComponent<PlayerInkPouchController>();
        }

        if (inkPouchController == null)
        {
            inkPouchController = GetComponentInChildren<PlayerInkPouchController>(true);
        }
    }

    public void SetHp(int currentHp, int maxHp)
    {
        if (damageReceiver == null)
        {
            ResolveReferences();
        }

        if (damageReceiver == null)
            return;

        damageReceiver.SetHp(currentHp, maxHp);
        NotifyHpChanged();
    }

    public void HealHp(int amount)
    {
        if (damageReceiver == null)
        {
            ResolveReferences();
        }

        if (damageReceiver == null || amount <= 0)
            return;

        SetHp(CurrentHp + amount, MaxHp);
    }

    public void FullHeal()
    {
        if (damageReceiver == null)
        {
            ResolveReferences();
        }

        if (damageReceiver == null)
            return;

        SetHp(MaxHp, MaxHp);
    }

    public void SetInk(int currentInk, int maxInk)
    {
        if (inkPouchController == null)
        {
            ResolveReferences();
        }

        if (inkPouchController == null)
            return;

        inkPouchController.SetInk(currentInk, maxInk);
        NotifyInkChanged();
    }

    public void RestoreInk(int amount)
    {
        if (inkPouchController == null)
        {
            ResolveReferences();
        }

        if (inkPouchController == null || amount <= 0)
            return;

        inkPouchController.RestoreInk(amount);
        NotifyInkChanged();
    }

    public void FullRestoreInk()
    {
        if (inkPouchController == null)
        {
            ResolveReferences();
        }

        if (inkPouchController == null)
            return;

        SetInk(MaxInk, MaxInk);
    }

    public void Revive(bool fullHeal = true, bool fullRestoreInk = false)
    {
        if (fullHeal)
        {
            FullHeal();
        }
        else if (CurrentHp <= 0)
        {
            SetHp(1, MaxHp);
        }

        if (fullRestoreInk)
        {
            FullRestoreInk();
        }

        Revived?.Invoke();
    }

    public void NotifyAllChanged()
    {
        NotifyHpChanged();
        NotifyInkChanged();
    }

    private void NotifyHpChanged()
    {
        lastHp = CurrentHp;
        lastMaxHp = MaxHp;
        HpChanged?.Invoke(CurrentHp, MaxHp);
    }

    private void NotifyInkChanged()
    {
        lastInk = CurrentInk;
        lastMaxInk = MaxInk;
        InkChanged?.Invoke(CurrentInk, MaxInk);
    }

    private void OnPlayerDamaged(CharacterDamagedEventInfo eventInfo)
    {
        NotifyHpChanged();
    }

    private void DetectExternalResourceChanges()
    {
        if (CurrentHp != lastHp || MaxHp != lastMaxHp)
        {
            NotifyHpChanged();
        }

        if (CurrentInk != lastInk || MaxInk != lastMaxInk)
        {
            NotifyInkChanged();
        }
    }
}
