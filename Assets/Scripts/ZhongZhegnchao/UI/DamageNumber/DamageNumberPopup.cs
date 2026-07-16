using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DamageNumberPopup : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI damageText;
    public CanvasGroup canvasGroup;
    public RectTransform rectTransform;

    [Header("Animation")]
    public float lifeTime = 0.8f;
    public float moveUpDistance = 80f;
    public float scalePunch = 1.25f;

    [Header("Heal Text")]
    public string healPrefix = "+";
    public Color healColor = new Color(0.35f, 1f, 0.35f, 1f);

    [Header("Text")]
    public string criticalPrefix = "CRIT ";
    public string vaporizeSuffix = " Vaporize";

    public Action<GameObject> OnRecycleRequested;

    private Coroutine playRoutine;
    private Vector2 startAnchoredPosition;
    private Vector3 startScale;

    private void Awake()
    {
        if (damageText == null)
            damageText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        startScale = transform.localScale;
    }

    private void OnDisable()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
    }

    public void Init(DamageInfo damageInfo)
    {
        int damageValue = damageInfo.finalDamage > 0
            ? damageInfo.finalDamage
            : damageInfo.damage;

        Init(
            damageValue,
            damageInfo.element,
            damageInfo.isCritical,
            damageInfo.reactionType
        );
    }

    public void Init(
        int damage,
        ElementType element,
        bool isCritical,
        ElementReactionType reactionType
    )
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (damageText == null)
            damageText = GetComponentInChildren<TextMeshProUGUI>(true);

        startAnchoredPosition = rectTransform.anchoredPosition;
        transform.localScale = startScale;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        if (damageText != null)
        {
            damageText.enabled = true;
            damageText.text = BuildText(damage, isCritical, reactionType);
            damageText.color = GetElementColor(element, isCritical);
        }

        playRoutine = StartCoroutine(PlayRoutine());
    }

    private string BuildText(
        int damage,
        bool isCritical,
        ElementReactionType reactionType
    )
    {
        string text = damage.ToString();

        if (isCritical)
        {
            text = criticalPrefix + text;
        }

        if (reactionType == ElementReactionType.Vaporize)
        {
            text += vaporizeSuffix;
        }

        return text;
    }

    private Color GetElementColor(ElementType element, bool isCritical)
    {
        if (isCritical)
            return new Color(1f, 0.9f, 0.2f, 1f);

        switch (element)
        {
            case ElementType.Fire:
                return new Color(1f, 0.35f, 0.1f, 1f);

            case ElementType.Water:
                return new Color(0.2f, 0.65f, 1f, 1f);

            case ElementType.Ice:
                return new Color(0.55f, 0.9f, 1f, 1f);

            case ElementType.Thunder:
                return new Color(0.75f, 0.35f, 1f, 1f);

            case ElementType.Wind:
                return new Color(0.35f, 1f, 0.75f, 1f);

            default:
                return Color.white;
        }
    }

    private IEnumerator PlayRoutine()
    {
        float timer = 0f;

        while (timer < lifeTime)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / lifeTime);

            float y = Mathf.Lerp(
                0f,
                moveUpDistance,
                t
            );

            rectTransform.anchoredPosition =
                startAnchoredPosition + Vector2.up * y;

            float scale = Mathf.Lerp(
                scalePunch,
                1f,
                t
            );

            transform.localScale = startScale * scale;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - t;
            }

            yield return null;
        }

        playRoutine = null;
        Recycle();
    }

    private void Recycle()
    {
        if (OnRecycleRequested != null)
        {
            OnRecycleRequested.Invoke(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void InitHeal(int healAmount)
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (damageText == null)
            damageText = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);

        startAnchoredPosition = rectTransform.anchoredPosition;
        transform.localScale = startScale;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        if (damageText != null)
        {
            damageText.enabled = true;
            damageText.text = healPrefix + healAmount.ToString();
            damageText.color = healColor;
        }

        playRoutine = StartCoroutine(PlayRoutine());
    }
}