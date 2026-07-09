using System.Collections;
using UnityEngine;

public class ElementVfxController : MonoBehaviour
{
    [Header("Fire VFX")]
    public GameObject fireVfxObject;
    public float defaultFireVfxDuration = 5f;

    private Coroutine fireVfxCoroutine;

    private void Awake()
    {
        if (fireVfxObject != null)
        {
            fireVfxObject.SetActive(false);
        }
    }

    public void ActivateFireVfx()
    {
        ActivateFireVfx(defaultFireVfxDuration);
    }

    public void ActivateFireVfx(float duration)
    {
        if (fireVfxObject == null)
        {
            return;
        }

        if (fireVfxCoroutine != null)
        {
            StopCoroutine(fireVfxCoroutine);
        }

        fireVfxCoroutine = StartCoroutine(FireVfxRoutine(duration));
    }

    private IEnumerator FireVfxRoutine(float duration)
    {
        fireVfxObject.SetActive(true);

        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }

        fireVfxObject.SetActive(false);
        fireVfxCoroutine = null;
    }

    public void StopFireVfx()
    {
        if (fireVfxCoroutine != null)
        {
            StopCoroutine(fireVfxCoroutine);
            fireVfxCoroutine = null;
        }

        if (fireVfxObject != null)
        {
            fireVfxObject.SetActive(false);
        }
    }

    public void StopAllElementVfx()
    {
        StopFireVfx();
    }
}