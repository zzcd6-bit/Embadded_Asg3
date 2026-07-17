using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class CurrencyAmountView :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TMP_Text amountText;

    [SerializeField]
    private PlayerCurrencyController
        currencyController;

    [Header("Display")]
    [SerializeField]
    private string prefix = "x";

    [SerializeField]
    private bool useThousandsSeparator;

    [Header("Behaviour")]
    [SerializeField]
    private bool autoFindSource = true;

    private bool subscribed;

    private Coroutine resolveCoroutine;

    private void Awake()
    {
        if (amountText == null)
        {
            amountText =
                GetComponentInChildren<
                    TMP_Text>(true);
        }
    }

    private void OnEnable()
    {
        ResolveSource();
        Subscribe();

        RefreshDisplay();

        if (currencyController == null)
        {
            resolveCoroutine =
                StartCoroutine(
                    ResolveSourceRoutine()
                );
        }
    }

    private void OnDisable()
    {
        Unsubscribe();

        if (resolveCoroutine != null)
        {
            StopCoroutine(
                resolveCoroutine
            );

            resolveCoroutine = null;
        }
    }

    private void ResolveSource()
    {
        if (!autoFindSource ||
            currencyController != null)
        {
            return;
        }

        currencyController =
            FindFirstObjectByType<
                PlayerCurrencyController>();
    }

    private IEnumerator ResolveSourceRoutine()
    {
        while (isActiveAndEnabled &&
               currencyController == null)
        {
            ResolveSource();

            if (currencyController != null)
            {
                Subscribe();
                RefreshDisplay();

                resolveCoroutine = null;
                yield break;
            }

            yield return
                new WaitForSecondsRealtime(
                    0.5f
                );
        }

        resolveCoroutine = null;
    }

    private void Subscribe()
    {
        if (currencyController == null ||
            subscribed)
        {
            return;
        }

        currencyController.CoinsChanged +=
            HandleCoinsChanged;

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (currencyController != null &&
            subscribed)
        {
            currencyController.CoinsChanged -=
                HandleCoinsChanged;
        }

        subscribed = false;
    }

    public void Bind(
        PlayerCurrencyController controller
    )
    {
        Unsubscribe();

        currencyController =
            controller;

        if (isActiveAndEnabled)
        {
            Subscribe();
        }

        RefreshDisplay();
    }

    private void HandleCoinsChanged(
        int currentCoins
    )
    {
        SetAmountText(
            currentCoins
        );
    }

    private void RefreshDisplay()
    {
        int currentCoins =
            currencyController != null
                ? currencyController.CurrentCoins
                : 0;

        SetAmountText(
            currentCoins
        );
    }

    private void SetAmountText(
        int currentCoins
    )
    {
        if (amountText == null)
            return;

        currentCoins =
            Mathf.Max(
                0,
                currentCoins
            );

        string numberText =
            useThousandsSeparator
                ? currentCoins.ToString("N0")
                : currentCoins.ToString();

        amountText.text =
            $"{prefix}{numberText}";
    }
}