using System;
using UnityEngine;

[DisallowMultipleComponent]
public class FirstPersonController : MonoBehaviour
{
    public static FirstPersonController instance;
    public static event Action<int> OnPlayerStatusChange;

    public Animator animator;
    public bool isDown;

    private void Awake()
    {
        instance = this;
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void SetDown(bool down)
    {
        isDown = down;
    }

    public static void BroadcastPlayerStatus(int status)
    {
        OnPlayerStatusChange?.Invoke(status);
    }
}
