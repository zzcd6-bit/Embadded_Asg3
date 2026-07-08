using UnityEngine;

public class PlayerAfterImageSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform meshRoot;
    [SerializeField] private Material ghostMaterial;

    [Header("Ghost")]
    [SerializeField] private float lifetime = 0.45f;
    [SerializeField] private float startAlpha = 0.55f;
    [SerializeField] private bool includeInactiveRenderers = false;

    [Header("Spawn")]
    [SerializeField] private string ghostRootName = "Player_AfterImage_Ghost";
    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private SkinnedMeshRenderer[] cachedSkinnedRenderers;

    private void Awake()
    {
        CacheRenderers();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            SpawnAfterImage();
        }
    }

    private void CacheRenderers()
    {
        Transform root = meshRoot != null ? meshRoot : transform;

        cachedSkinnedRenderers =
            root.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactiveRenderers);
    }

    public void SpawnAfterImage()
    {
        if (ghostMaterial == null)
        {
            Debug.LogWarning("[PlayerAfterImageSpawner] Ghost material is null.", this);
            return;
        }

        if (cachedSkinnedRenderers == null || cachedSkinnedRenderers.Length == 0)
        {
            CacheRenderers();
        }

        if (cachedSkinnedRenderers == null || cachedSkinnedRenderers.Length == 0)
        {
            Debug.LogWarning("[PlayerAfterImageSpawner] No SkinnedMeshRenderer found.", this);
            return;
        }

        GameObject ghostRoot = new GameObject(ghostRootName);
        ghostRoot.transform.position = transform.position + positionOffset;
        ghostRoot.transform.rotation = transform.rotation;

        PlayerAfterImageGhost ghost = ghostRoot.AddComponent<PlayerAfterImageGhost>();
        ghost.Init(lifetime, startAlpha);

        for (int i = 0; i < cachedSkinnedRenderers.Length; i++)
        {
            SkinnedMeshRenderer sourceRenderer = cachedSkinnedRenderers[i];

            if (sourceRenderer == null || !sourceRenderer.enabled)
            {
                continue;
            }

            CreateGhostPart(sourceRenderer, ghostRoot.transform, ghost);
        }

        if (debugLog)
        {
            Debug.Log("[PlayerAfterImageSpawner] Spawn afterimage.", this);
        }
    }

    private void CreateGhostPart(
        SkinnedMeshRenderer sourceRenderer,
        Transform ghostRoot,
        PlayerAfterImageGhost ghost
    )
    {
        Mesh bakedMesh = new Mesh();
        sourceRenderer.BakeMesh(bakedMesh);

        GameObject part = new GameObject(sourceRenderer.name + "_GhostPart");
        part.transform.SetParent(ghostRoot, true);

        part.transform.position = sourceRenderer.transform.position;
        part.transform.rotation = sourceRenderer.transform.rotation;
        part.transform.localScale = sourceRenderer.transform.lossyScale;

        MeshFilter meshFilter = part.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = bakedMesh;

        MeshRenderer meshRenderer = part.AddComponent<MeshRenderer>();

        int materialCount = Mathf.Max(1, sourceRenderer.sharedMaterials.Length);
        Material[] ghostMaterials = new Material[materialCount];

        for (int i = 0; i < ghostMaterials.Length; i++)
        {
            Material runtimeMaterial = new Material(ghostMaterial);
            ghostMaterials[i] = runtimeMaterial;
            ghost.RegisterMaterial(runtimeMaterial);
        }

        meshRenderer.sharedMaterials = ghostMaterials;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        ghost.RegisterMesh(bakedMesh);
    }
}