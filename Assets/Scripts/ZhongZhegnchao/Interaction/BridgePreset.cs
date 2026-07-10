using System.Collections;
using UnityEngine;

public class BridgePreset : MonoBehaviour
{
    [Header("桥对象")]
    public GameObject bridgeObject;

    [Header("激活设置")]
    public bool startHidden = true;
    public bool autoHide = true;
    public float defaultDuration = 8f;

    [Header("Debug")]
    public bool debugLog = true;

    private Coroutine activeCoroutine;

    private void Awake()
    {
        if (startHidden && bridgeObject != null)
        {
            bridgeObject.SetActive(false);
        }
    }

    public void ActivateBridge()
    {
        ActivateBridge(defaultDuration);
    }

    public void ActivateBridge(float duration)
    {
        if (bridgeObject == null)
        {
            Debug.LogWarning("[BridgePreset] Bridge Object is missing.", this);
            return;
        }

        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        bridgeObject.SetActive(true);

        if (debugLog)
        {
            Debug.Log($"[BridgePreset] Bridge activated: {gameObject.name}", this);
        }

        if (autoHide && duration > 0f)
        {
            activeCoroutine = StartCoroutine(AutoHideRoutine(duration));
        }
    }

    private IEnumerator AutoHideRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (bridgeObject != null)
        {
            bridgeObject.SetActive(false);
        }

        activeCoroutine = null;

        if (debugLog)
        {
            Debug.Log($"[BridgePreset] Bridge hidden: {gameObject.name}", this);
        }
    }

    public void DeactivateBridge()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        if (bridgeObject != null)
        {
            bridgeObject.SetActive(false);
        }
    }
}