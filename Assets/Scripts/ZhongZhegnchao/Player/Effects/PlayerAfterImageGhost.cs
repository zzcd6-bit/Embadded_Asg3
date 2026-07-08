using System.Collections.Generic;
using UnityEngine;

public class PlayerAfterImageGhost : MonoBehaviour
{
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");

    private readonly List<Mesh> bakedMeshes = new List<Mesh>();
    private readonly List<Material> runtimeMaterials = new List<Material>();

    private float lifetime = 0.45f;
    private float timer;
    private float startAlpha = 0.55f;

    public void Init(float life, float alpha)
    {
        lifetime = Mathf.Max(0.01f, life);
        startAlpha = Mathf.Clamp01(alpha);
        timer = 0f;

        ApplyFade(0f);
    }

    public void RegisterMesh(Mesh mesh)
    {
        if (mesh != null && !bakedMeshes.Contains(mesh))
        {
            bakedMeshes.Add(mesh);
        }
    }

    public void RegisterMaterial(Material material)
    {
        if (material != null && !runtimeMaterials.Contains(material))
        {
            runtimeMaterials.Add(material);
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float normalized = Mathf.Clamp01(timer / lifetime);
        ApplyFade(normalized);

        if (timer >= lifetime)
        {
            Dispose();
        }
    }

    private void ApplyFade(float normalized)
    {
        float alpha = Mathf.Lerp(startAlpha, 0f, normalized);
        float dissolve = normalized;

        for (int i = 0; i < runtimeMaterials.Count; i++)
        {
            Material material = runtimeMaterials[i];

            if (material == null)
            {
                continue;
            }

            if (material.HasProperty(AlphaId))
            {
                material.SetFloat(AlphaId, alpha);
            }

            if (material.HasProperty(DissolveAmountId))
            {
                material.SetFloat(DissolveAmountId, dissolve);
            }
        }
    }

    private void Dispose()
    {
        for (int i = 0; i < bakedMeshes.Count; i++)
        {
            if (bakedMeshes[i] != null)
            {
                Destroy(bakedMeshes[i]);
            }
        }

        for (int i = 0; i < runtimeMaterials.Count; i++)
        {
            if (runtimeMaterials[i] != null)
            {
                Destroy(runtimeMaterials[i]);
            }
        }

        bakedMeshes.Clear();
        runtimeMaterials.Clear();

        Destroy(gameObject);
    }
}