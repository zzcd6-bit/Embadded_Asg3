using System.Collections.Generic;
using UnityEngine;

public class PlacementPreviewController : MonoBehaviour
{
    [SerializeField] private Material validPreviewMaterial;
    [SerializeField] private Material invalidPreviewMaterial;
    [SerializeField, Range(0.05f, 1f)] private float fallbackPreviewAlpha = 0.55f;

    private readonly List<ColliderState> editingColliderStates = new();
    private GameObject previewObject;
    private PlacementPreviewVisual previewVisual;

    public GameObject PreviewObject => previewObject;
    public bool HasPreview => previewObject != null;

    //Creates a disabled-collider preview from the selected item prefab.
    //根据选中的物品 prefab 创建禁用碰撞体的预览物体。
    public GameObject CreateNewPreview(BuildableItemData item)
    {
        DestroyPreview();
        previewObject = Instantiate(item.BuildingPrefab);
        previewObject.name = $"{item.DisplayName}_Preview";
        DisableColliders(previewObject);
        PrepareVisual(previewObject);
        return previewObject;
    }

    //Uses an existing building as the editable preview object.
    //将已有建筑作为可编辑的预览物体。
    public GameObject UseExistingAsPreview(BuildingInstance building)
    {
        DestroyPreview();
        previewObject = building.gameObject;
        CacheAndDisableEditingColliders(previewObject);
        PrepareVisual(previewObject);
        return previewObject;
    }

    //Updates preview transform while snapping or rotating.
    //在吸附或旋转时更新预览物体 Transform。
    public void SetTransform(Vector3 position, Quaternion rotation)
    {
        if (previewObject != null)
        {
            previewObject.transform.SetPositionAndRotation(position, rotation);
        }
    }

    //Applies valid or invalid visual feedback to the preview.
    //对预览体应用可放置或不可放置的视觉反馈。
    public void SetValid(bool canPlace)
    {
        if (previewVisual != null)
        {
            previewVisual.SetValid(canPlace);
        }
    }

    //Restores original materials captured before preview mode.
    //恢复进入预览模式前缓存的原始材质。
    public void RestoreVisual()
    {
        if (previewVisual != null)
        {
            previewVisual.RestoreOriginal();
        }
    }

    //Destroys the temporary preview created for new placement.
    //销毁为新建摆放创建的临时预览物体。
    public void DestroyPreview()
    {
        if (previewObject != null && editingColliderStates.Count == 0)
        {
            Destroy(previewObject);
        }

        previewObject = null;
        previewVisual = null;
    }

    //Clears preview references without destroying the object.
    //仅清理预览引用，不销毁物体。
    public void ClearReferences()
    {
        previewObject = null;
        previewVisual = null;
    }

    //Restores colliders cached while editing an existing building.
    //恢复编辑已有建筑时缓存的碰撞体。
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

    //Disables colliders on a temporary preview object.
    //禁用临时预览物体上的碰撞体。
    private static void DisableColliders(GameObject target)
    {
        foreach (Collider collider in target.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }
    }

    //Caches and disables colliders while an existing building is edited.
    //编辑已有建筑时缓存并禁用其碰撞体。
    private void CacheAndDisableEditingColliders(GameObject target)
    {
        editingColliderStates.Clear();

        foreach (Collider collider in target.GetComponentsInChildren<Collider>())
        {
            editingColliderStates.Add(new ColliderState(collider));
            collider.enabled = false;
        }
    }

    //Ensures the preview visual component exists and has materials.
    //确保预览视觉组件存在并拥有材质。
    private void PrepareVisual(GameObject target)
    {
        previewVisual = target.GetComponent<PlacementPreviewVisual>();
        if (previewVisual == null)
        {
            previewVisual = target.AddComponent<PlacementPreviewVisual>();
        }

        previewVisual.Initialize(GetValidPreviewMaterial(), GetInvalidPreviewMaterial());
    }

    //Returns the configured or fallback material for valid placement.
    //返回可摆放状态的配置材质或备用材质。
    private Material GetValidPreviewMaterial()
    {
        return validPreviewMaterial != null
            ? validPreviewMaterial
            : CreateFallbackPreviewMaterial(new Color(0.35f, 0.95f, 0.65f, fallbackPreviewAlpha));
    }

    //Returns the configured or fallback material for invalid placement.
    //返回不可摆放状态的配置材质或备用材质。
    private Material GetInvalidPreviewMaterial()
    {
        return invalidPreviewMaterial != null
            ? invalidPreviewMaterial
            : CreateFallbackPreviewMaterial(new Color(1f, 0.18f, 0.14f, fallbackPreviewAlpha));
    }

    //Creates a transparent fallback preview material.
    //创建透明的备用预览材质。
    private static Material CreateFallbackPreviewMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        shader ??= Shader.Find("Standard");

        Material material = new(shader)
        {
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
