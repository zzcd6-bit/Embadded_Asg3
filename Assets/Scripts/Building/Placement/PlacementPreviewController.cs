using System.Collections.Generic;
using UnityEngine;

public class PlacementPreviewController : MonoBehaviour
{
    [SerializeField] private Material validPreviewMaterial;
    [SerializeField] private Material invalidPreviewMaterial;
    [SerializeField, Range(0.05f, 1f)] private float fallbackPreviewAlpha = 0.55f;

    private Material generatedValidPreviewMaterial;
    private Material generatedInvalidPreviewMaterial;
    private readonly List<ColliderState> editingColliderStates = new();
    private GameObject previewObject;
    private PlacementPreviewVisual previewVisual;

    public GameObject PreviewObject => previewObject;
    public bool HasPreview => previewObject != null;

    public GameObject CreateNewPreview(BuildableItemData item)
    {
        DestroyPreview();
        previewObject = Instantiate(item.BuildingPrefab);
        previewObject.name = $"{item.DisplayName}_Preview";
        DisableColliders(previewObject);
        PrepareVisual(previewObject);
        return previewObject;
    }

    public GameObject UseExistingAsPreview(BuildingInstance building)
    {
        DestroyPreview();
        previewObject = building.gameObject;
        CacheAndDisableEditingColliders(previewObject);
        PrepareVisual(previewObject);
        return previewObject;
    }

    public void SetTransform(Vector3 position, Quaternion rotation)
    {
        if (previewObject != null)
        {
            previewObject.transform.SetPositionAndRotation(position, rotation);
        }
    }

    public bool TryGetWorldBounds(out Bounds bounds)
    {
        bounds = default;
        if (previewObject == null)
        {
            return false;
        }

        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    public void SetValid(bool canPlace)
    {
        if (previewVisual != null)
        {
            previewVisual.SetValid(canPlace);
        }
    }

    public void RestoreVisual()
    {
        if (previewVisual != null)
        {
            previewVisual.RestoreOriginal();
        }
    }

    public void DestroyPreview()
    {
        if (previewObject != null && editingColliderStates.Count == 0)
        {
            Destroy(previewObject);
        }

        previewObject = null;
        previewVisual = null;
    }

    public void ClearReferences()
    {
        previewObject = null;
        previewVisual = null;
    }

    public void RestoreEditingColliders()
    {
        foreach (ColliderState state in editingColliderStates)
        {
            if (state.Collider != null)
            {
                state.Collider.enabled = state.WasEnabled;
                state.Collider.isTrigger = state.WasTrigger;
            }
        }

        editingColliderStates.Clear();
    }

    private static void DisableColliders(GameObject target)
    {
        foreach (Collider collider in target.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }
    }

    private void CacheAndDisableEditingColliders(GameObject target)
    {
        editingColliderStates.Clear();

        foreach (Collider collider in target.GetComponentsInChildren<Collider>())
        {
            editingColliderStates.Add(new ColliderState(collider));
            collider.enabled = false;
        }
    }

    private void PrepareVisual(GameObject target)
    {
        previewVisual = target.GetComponent<PlacementPreviewVisual>();
        if (previewVisual == null)
        {
            previewVisual = target.AddComponent<PlacementPreviewVisual>();
        }

        previewVisual.Initialize(GetValidPreviewMaterial(), GetInvalidPreviewMaterial());
    }

    private Material GetValidPreviewMaterial()
    {
        if (validPreviewMaterial != null)
        {
            return validPreviewMaterial;
        }

        generatedValidPreviewMaterial ??= CreateFallbackPreviewMaterial(
            "Generated Valid Preview Material",
            new Color(0.35f, 0.95f, 0.65f, fallbackPreviewAlpha)
        );

        return generatedValidPreviewMaterial;
    }

    private Material GetInvalidPreviewMaterial()
    {
        if (invalidPreviewMaterial != null)
        {
            return invalidPreviewMaterial;
        }

        generatedInvalidPreviewMaterial ??= CreateFallbackPreviewMaterial(
            "Generated Invalid Preview Material",
            new Color(1f, 0.18f, 0.14f, fallbackPreviewAlpha)
        );

        return generatedInvalidPreviewMaterial;
    }

    private static Material CreateFallbackPreviewMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        shader ??= Shader.Find("Standard");

        Material material = new(shader)
        {
            name = materialName,
            color = color,
            renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent
        };

        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return material;
    }

    private readonly struct ColliderState
    {
        public readonly Collider Collider;
        public readonly bool WasEnabled;
        public readonly bool WasTrigger;

        public ColliderState(Collider collider)
        {
            Collider = collider;
            WasEnabled = collider.enabled;
            WasTrigger = collider.isTrigger;
        }
    }
}
