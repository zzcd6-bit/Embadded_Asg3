using UnityEngine;

public class PlacementPreviewVisual : MonoBehaviour
{
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;

    private Renderer[] renderers;
    private Material[][] originalMaterials;

    //Caches renderers and prepares the valid and invalid preview materials.
    //缓存渲染器，并准备可放置与不可放置预览材质。
    public void Initialize(Material valid, Material invalid)
    {
        validMaterial = valid;
        invalidMaterial = invalid;
        renderers = GetComponentsInChildren<Renderer>();
        CacheOriginalMaterials();
        SetValid(true);
    }

    //Applies the material matching the current placement validity.
    //根据当前摆放有效性应用对应材质。
    public void SetValid(bool isValid)
    {
        if (renderers == null)
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        Material material = isValid ? validMaterial : invalidMaterial;
        if (material == null)
        {
            return;
        }

        foreach (Renderer renderer in renderers)
        {
            renderer.sharedMaterial = material;
        }
    }

    //Restores the renderer materials captured before preview highlighting.
    //恢复预览高亮前捕获的原始渲染材质。
    public void RestoreOriginal()
    {
        if (renderers == null || originalMaterials == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && i < originalMaterials.Length)
            {
                renderers[i].sharedMaterials = originalMaterials[i];
            }
        }
    }

    //Stores each renderer material array for later restoration.
    //保存每个渲染器的材质数组，供之后恢复。
    private void CacheOriginalMaterials()
    {
        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterials;
        }
    }
}
