using TMPro;
using UnityEngine;

public class DamageNumberPopup : MonoBehaviour
{
    [Header("×é¼þ")]
    public TextMeshProUGUI damageText;
    public CanvasGroup canvasGroup;

    [Header("¶¯»­")]
    public float lifetime = 0.8f;
    public float moveSpeed = 80f;
    public float criticalScale = 1.4f;

    private float timer;
    private RectTransform rectTransform;
    private Vector3 moveDirection;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (damageText == null)
        {
            damageText = GetComponent<TextMeshProUGUI>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        moveDirection = new Vector3(
            Random.Range(-0.25f, 0.25f),
            1f,
            0f
        ).normalized;
    }

    public void Init(
        int damage,
        ElementType element,
        bool isCritical,
        ElementReactionType reactionType
    )
    {
        timer = lifetime;

        if (damageText == null)
            return;

        string text = damage.ToString();

        if (isCritical)
        {
            text = "CRIT " + text;
            transform.localScale = Vector3.one * criticalScale;
        }
        else
        {
            transform.localScale = Vector3.one;
        }

        if (reactionType != ElementReactionType.None)
        {
            text += "\n" + reactionType.ToString();
        }

        damageText.text = text;
        damageText.color = GetColorByElement(element, isCritical);
    }

    private void Update()
    {
        if (timer <= 0f)
            return;

        timer -= Time.deltaTime;

        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        float alpha = Mathf.Clamp01(timer / lifetime);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }

        if (timer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private Color GetColorByElement(ElementType element, bool isCritical)
    {
        if (isCritical)
        {
            return new Color(1f, 0.9f, 0.2f);
        }

        switch (element)
        {
            case ElementType.Fire:
                return new Color(1f, 0.35f, 0.1f);

            case ElementType.Water:
                return new Color(0.2f, 0.6f, 1f);

            case ElementType.Ice:
                return new Color(0.5f, 0.9f, 1f);

            case ElementType.Thunder:
                return new Color(0.8f, 0.4f, 1f);

            case ElementType.Earth:
                return new Color(0.7f, 0.5f, 0.25f);

            case ElementType.Physical:
            default:
                return Color.white;
        }
    }
}