using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Chest : MonoBehaviour, IReactable, IReactableStateNotifier, IGameSaveModule
{
    [Header("Identity")]
    [SerializeField] private string chestId;

    [Header("Interaction")]
    [SerializeField] private string openOptionName = "Open Chest";
    [SerializeField] private string lockedOptionName = "Locked Chest";
    [SerializeField] private InteractionCategory category = InteractionCategory.Normal;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private bool showInteractionWhenLocked = true;

    [Header("State")]
    [SerializeField] private bool canOpen = true;
    [SerializeField] private bool isOpen;

    [Header("Reward")]
    [SerializeField] private DropReward dropReward;
    [SerializeField] private Transform dropOrigin;

    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private string openedBool = "IsOpen";

    [Header("Opened Cleanup")]
    [SerializeField, Min(0f)] private float destroyDelayAfterOpen = 3f;
    [SerializeField] private bool fadeBeforeDestroy = true;
    [SerializeField] private bool saveImmediatelyOnOpen = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onUnlocked = new();
    [SerializeField] private UnityEvent onOpened = new();
    [SerializeField] private UnityEvent onOpenFailed = new();

    public event Action<IReactable> StateChanged;

    public bool CanOpen => canOpen;
    public bool IsOpen => isOpen;
    public string ChestId => chestId;
    public UnityEvent OnUnlocked => onUnlocked;
    public UnityEvent OnOpened => onOpened;
    public UnityEvent OnOpenFailed => onOpenFailed;

    public string OptionName => canOpen ? openOptionName : lockedOptionName;
    public InteractionCategory Category => category;
    public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;

    private Coroutine openedCleanupRoutine;

    private void Reset()
    {
        dropReward = GetComponent<DropReward>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        EnsureId();
        ResolveReferences();
        if (WorldStateSaveController.IsChestOpened(chestId))
        {
            isOpen = true;
            Destroy(gameObject);
            return;
        }

        ApplyAnimatorState();
    }

    public bool TryUnlock()
    {
        if (isOpen || canOpen)
        {
            return false;
        }

        canOpen = true;
        onUnlocked?.Invoke();
        StateChanged?.Invoke(this);
        return true;
    }

    public bool TryOpen()
    {
        return TryOpen(null);
    }

    public bool TryOpen(GameObject interactor)
    {
        if (isOpen || !canOpen)
        {
            onOpenFailed?.Invoke();
            StateChanged?.Invoke(this);
            return false;
        }

        isOpen = true;
        WorldStateSaveController.MarkChestOpened(chestId);
        ApplyAnimatorState();
        SpawnReward();
        onOpened?.Invoke();
        StateChanged?.Invoke(this);
        SaveOpenedStateIfNeeded();
        BeginOpenedCleanup(destroyDelayAfterOpen);
        return true;
    }

    public bool CanInteract(GameObject interactor)
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy || isOpen)
        {
            return false;
        }

        return canOpen || showInteractionWhenLocked;
    }

    public void Interact(GameObject interactor)
    {
        TryOpen(interactor);
    }

    public void OnSelected()
    {
    }

    public void OnDeselected()
    {
    }

    private void SpawnReward()
    {
        ResolveReferences();
        if (dropReward == null)
        {
            return;
        }

        Vector3 origin = dropOrigin != null ? dropOrigin.position : transform.position;
        dropReward.DropAt(origin);
    }

    private void ApplyAnimatorState()
    {
        if (animator == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(openedBool))
        {
            animator.SetBool(openedBool, isOpen);
        }

        if (isOpen && !string.IsNullOrEmpty(openTrigger))
        {
            animator.SetTrigger(openTrigger);
        }
    }

    private void SaveOpenedStateIfNeeded()
    {
        if (!saveImmediatelyOnOpen || PlayerSaveManager.Instance == null)
        {
            return;
        }

        PlayerSaveManager.Instance.SavePlayer();
    }

    private void BeginOpenedCleanup(float duration)
    {
        if (openedCleanupRoutine != null)
        {
            StopCoroutine(openedCleanupRoutine);
        }

        openedCleanupRoutine = StartCoroutine(OpenCleanupRoutine(duration));
    }

    private IEnumerator OpenCleanupRoutine(float duration)
    {
        if (duration <= 0f)
        {
            Destroy(gameObject);
            yield break;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Material[] materials = PrepareRuntimeMaterials(renderers);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = fadeBeforeDestroy ? Mathf.Clamp01(1f - elapsed / duration) : 1f;
            SetMaterialsAlpha(materials, alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    private static Material[] PrepareRuntimeMaterials(Renderer[] renderers)
    {
        if (renderers == null || renderers.Length == 0)
        {
            return Array.Empty<Material>();
        }

        var materials = new System.Collections.Generic.List<Material>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] rendererMaterials = renderers[i].materials;
            for (int j = 0; j < rendererMaterials.Length; j++)
            {
                Material material = rendererMaterials[j];
                if (material == null)
                {
                    continue;
                }

                ConfigureTransparentMaterial(material);
                materials.Add(material);
            }
        }

        return materials.ToArray();
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 2f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat("_AlphaClip", 0f);
        }

        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static void SetMaterialsAlpha(Material[] materials, float alpha)
    {
        if (materials == null)
        {
            return;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null)
            {
                continue;
            }

            if (material.HasProperty("_BaseColor"))
            {
                Color color = material.GetColor("_BaseColor");
                color.a = alpha;
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                Color color = material.GetColor("_Color");
                color.a = alpha;
                material.SetColor("_Color", color);
            }
        }
    }

    private void ResolveReferences()
    {
        if (dropReward == null)
        {
            dropReward = GetComponent<DropReward>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void OnDisable()
    {
        StateChanged?.Invoke(this);
    }

    private void OnDestroy()
    {
        StateChanged?.Invoke(this);
    }

    public void CaptureGameSaveData(GameSaveData saveData)
    {
        if (saveData == null || !isOpen || string.IsNullOrWhiteSpace(chestId))
            return;

        if (saveData.worldState == null)
        {
            saveData.worldState = new WorldStateSaveData();
        }

        if (!saveData.worldState.openedChestIds.Contains(chestId))
        {
            saveData.worldState.openedChestIds.Add(chestId);
        }
    }

    public void RestoreGameSaveData(GameSaveData saveData)
    {
        if (saveData == null || saveData.worldState == null || string.IsNullOrWhiteSpace(chestId))
            return;

        bool shouldBeOpen = saveData.worldState.openedChestIds.Contains(chestId);
        if (!shouldBeOpen)
            return;

        isOpen = true;
        ApplyAnimatorState();
        StateChanged?.Invoke(this);
        Destroy(gameObject);
    }

    private void EnsureId()
    {
        if (string.IsNullOrWhiteSpace(chestId))
        {
            chestId = gameObject.scene.name + "/" + gameObject.name;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureId();
    }
#endif
}
