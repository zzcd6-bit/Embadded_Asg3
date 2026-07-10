using UnityEngine;

public class BrushModeCooldownController : MonoBehaviour
{
    [Header("»­»­Ä£Ê½ÀäÈ´")]
    public float cooldownTime = 3f;

    [Header("Debug")]
    public bool debugLog = true;

    [SerializeField] private float remainingCooldown;

    public bool IsCoolingDown
    {
        get { return remainingCooldown > 0f; }
    }

    public float RemainingCooldown
    {
        get { return remainingCooldown; }
    }

    public float CooldownProgress01
    {
        get
        {
            if (cooldownTime <= 0f)
                return 1f;

            return 1f - Mathf.Clamp01(remainingCooldown / cooldownTime);
        }
    }

    private void Update()
    {
        if (remainingCooldown <= 0f)
            return;

        remainingCooldown -= Time.deltaTime;

        if (remainingCooldown <= 0f)
        {
            remainingCooldown = 0f;

            if (debugLog)
            {
                Debug.Log("[BrushModeCooldown] Brush mode cooldown finished.", this);
            }
        }
    }

    public bool CanEnterBrushMode()
    {
        return !IsCoolingDown;
    }

    public void StartCooldown()
    {
        if (cooldownTime <= 0f)
        {
            remainingCooldown = 0f;
            return;
        }

        remainingCooldown = cooldownTime;

        if (debugLog)
        {
            Debug.Log(
                $"[BrushModeCooldown] Brush mode cooldown started: {cooldownTime}s",
                this
            );
        }
    }

    public void ClearCooldown()
    {
        remainingCooldown = 0f;
    }
}