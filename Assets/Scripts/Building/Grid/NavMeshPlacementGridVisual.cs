using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshPlacementGridVisual : MonoBehaviour
{
    [SerializeField] private GridPlacementSystem gridPlacementSystem;
    [SerializeField] private PlacementController placementController;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform centerTarget;
    [SerializeField] private LayerMask groundLayer = 1 << 6;
    [SerializeField] private int radiusInCells = 12;
    [SerializeField] private float surfaceOffset = 0.035f;
    [SerializeField] private float surfaceRaycastHeight = 40f;
    [SerializeField] private float surfaceRaycastDistance = 100f;
    [SerializeField] private float navMeshSampleDistance = 0.45f;
    [SerializeField] private float lineWidth = 0.025f;
    [SerializeField] private Color lineColor = new(0.2f, 0.95f, 0.85f, 0.42f);
    [SerializeField] private Color occupiedCellColor = new(1f, 0.12f, 0.08f, 0.5f);
    [SerializeField] private Color previewCellColor = new(1f, 0.85f, 0.12f, 0.5f);

    private readonly List<LineRenderer> linePool = new();
    private readonly List<MeshRenderer> occupiedCellPool = new();
    private readonly List<MeshRenderer> previewCellPool = new();
    private readonly List<Vector2Int> previewCells = new();
    private readonly Dictionary<Vector2Int, SurfaceSample> surfaceSampleCache = new();
    private Material generatedLineMaterial;
    private Material generatedOccupiedCellMaterial;
    private Material generatedPreviewCellMaterial;
    private bool isVisible;
    private int usedLineCount;
    private int usedOccupiedCellCount;
    private int usedPreviewCellCount;

    private Camera ActiveCamera
    {
        get
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            return targetCamera;
        }
    }

    private void Awake()
    {
        if (gridPlacementSystem == null)
        {
            gridPlacementSystem = GetComponent<GridPlacementSystem>();
        }

        if (placementController == null)
        {
            placementController = FindAnyObjectByType<PlacementController>();
        }

        ResolveCenterTarget();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (!isVisible || gridPlacementSystem == null)
        {
            return;
        }

        DrawGrid();
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;

        if (!visible)
        {
            HideAllLines();
        }
    }

    private void DrawGrid()
    {
        Vector3 centerWorld = GetGridCenter();
        Vector2Int centerCell = gridPlacementSystem.WorldToCell(centerWorld);
        int radius = Mathf.Max(1, radiusInCells);

        usedLineCount = 0;
        usedOccupiedCellCount = 0;
        usedPreviewCellCount = 0;
        bool hasPreviewCells = placementController != null
            && placementController.TryGetCurrentPlacementCells(previewCells);
        surfaceSampleCache.Clear();

        int minX = centerCell.x - radius;
        int maxX = centerCell.x + radius;
        int minY = centerCell.y - radius;
        int maxY = centerCell.y + radius;

        for (int x = minX; x <= maxX + 1; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                DrawSegment(new Vector2Int(x * 2 - 1, y * 2 - 1), new Vector2Int(x * 2 - 1, y * 2 + 1));
            }
        }

        for (int y = minY; y <= maxY + 1; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                DrawSegment(new Vector2Int(x * 2 - 1, y * 2 - 1), new Vector2Int(x * 2 + 1, y * 2 - 1));
            }
        }

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                DrawOccupiedCell(new Vector2Int(x, y));
            }
        }

        if (hasPreviewCells)
        {
            for (int i = 0; i < previewCells.Count; i++)
            {
                DrawPreviewCell(previewCells[i]);
            }
        }

        for (int i = usedLineCount; i < linePool.Count; i++)
        {
            linePool[i].gameObject.SetActive(false);
        }

        for (int i = usedOccupiedCellCount; i < occupiedCellPool.Count; i++)
        {
            occupiedCellPool[i].gameObject.SetActive(false);
        }

        for (int i = usedPreviewCellCount; i < previewCellPool.Count; i++)
        {
            previewCellPool[i].gameObject.SetActive(false);
        }
    }

    private Vector3 GetGridCenter()
    {
        if (centerTarget == null)
        {
            ResolveCenterTarget();
        }

        if (centerTarget != null)
        {
            if (TrySampleNavMesh(centerTarget.position, out Vector3 playerNavMeshPoint))
            {
                return playerNavMeshPoint;
            }

            return centerTarget.position;
        }

        Camera camera = ActiveCamera;
        if (camera == null)
        {
            return transform.position;
        }

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, ~0, QueryTriggerInteraction.Ignore)
            && TrySampleNavMesh(hit.point, out Vector3 navMeshPoint))
        {
            return navMeshPoint;
        }

        if (NavMesh.SamplePosition(camera.transform.position, out NavMeshHit cameraHit, 100f, NavMesh.AllAreas))
        {
            return cameraHit.position;
        }

        return transform.position;
    }

    private void ResolveCenterTarget()
    {
        ActionPlayerController actionPlayer = FindAnyObjectByType<ActionPlayerController>();
        if (actionPlayer != null)
        {
            centerTarget = actionPlayer.transform;
            return;
        }

        if (PlayerController.instance != null)
        {
            centerTarget = PlayerController.instance.transform;
            return;
        }

        PlayerController legacyPlayer = FindAnyObjectByType<PlayerController>();
        if (legacyPlayer != null)
        {
            centerTarget = legacyPlayer.transform;
            return;
        }

        GameObject taggedPlayer = GameObject.FindWithTag("Player");
        if (taggedPlayer != null)
        {
            centerTarget = taggedPlayer.transform;
        }
    }

    private void DrawSegment(Vector2Int fromCorner, Vector2Int toCorner)
    {
        if (!TryGetSurfacePoint(fromCorner, out Vector3 from)
            || !TryGetSurfacePoint(toCorner, out Vector3 to))
        {
            return;
        }

        from.y += surfaceOffset;
        to.y += surfaceOffset;

        LineRenderer line = GetLine();
        line.SetPosition(0, from);
        line.SetPosition(1, to);
    }

    private void DrawOccupiedCell(Vector2Int cell)
    {
        if (!gridPlacementSystem.IsCellUnavailable(cell))
        {
            return;
        }

        if (!TryGetCellCorners(cell, surfaceOffset + 0.006f, out Vector3[] corners))
        {
            return;
        }

        MeshRenderer cellRenderer = GetOccupiedCell();
        SetCellMesh(cellRenderer, corners);
    }

    private void DrawPreviewCell(Vector2Int cell)
    {
        if (gridPlacementSystem.IsCellUnavailable(cell))
        {
            return;
        }

        if (!TryGetCellCorners(cell, surfaceOffset + 0.012f, out Vector3[] corners))
        {
            return;
        }

        MeshRenderer cellRenderer = GetPreviewCell();
        SetCellMesh(cellRenderer, corners);
    }

    private bool TrySampleNavMesh(Vector3 position, out Vector3 navMeshPosition)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            navMeshPosition = hit.position;
            return true;
        }

        navMeshPosition = position;
        return false;
    }

    private bool TryGetCellCorners(Vector2Int cell, float extraOffset, out Vector3[] corners)
    {
        corners = new Vector3[4];
        Vector2Int bottomLeft = new(cell.x * 2 - 1, cell.y * 2 - 1);
        Vector2Int bottomRight = new(cell.x * 2 + 1, cell.y * 2 - 1);
        Vector2Int topRight = new(cell.x * 2 + 1, cell.y * 2 + 1);
        Vector2Int topLeft = new(cell.x * 2 - 1, cell.y * 2 + 1);

        if (!TryGetSurfacePoint(bottomLeft, out corners[0])
            || !TryGetSurfacePoint(bottomRight, out corners[1])
            || !TryGetSurfacePoint(topRight, out corners[2])
            || !TryGetSurfacePoint(topLeft, out corners[3]))
        {
            return false;
        }

        for (int i = 0; i < corners.Length; i++)
        {
            corners[i].y += extraOffset;
        }

        return true;
    }

    private bool TryGetSurfacePoint(Vector2Int cornerKey, out Vector3 point)
    {
        if (surfaceSampleCache.TryGetValue(cornerKey, out SurfaceSample cachedSample))
        {
            point = cachedSample.Point;
            return cachedSample.IsValid;
        }

        Vector3 worldPosition = CornerKeyToWorld(cornerKey);
        Vector3 rayOrigin = new(worldPosition.x, worldPosition.y + surfaceRaycastHeight, worldPosition.z);
        bool isValid = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            surfaceRaycastDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        if (isValid)
        {
            point = hit.point;
            isValid = NavMesh.SamplePosition(point, out _, navMeshSampleDistance, NavMesh.AllAreas);
        }
        else
        {
            point = worldPosition;
        }

        surfaceSampleCache[cornerKey] = new SurfaceSample(isValid, point);
        return isValid;
    }

    private Vector3 CornerKeyToWorld(Vector2Int cornerKey)
    {
        Vector3 gridOrigin = gridPlacementSystem.CellToWorld(Vector2Int.zero);
        return gridOrigin
            + new Vector3(
                cornerKey.x * gridPlacementSystem.CellSize * 0.5f,
                0f,
                cornerKey.y * gridPlacementSystem.CellSize * 0.5f
            );
    }

    private LineRenderer GetLine()
    {
        if (usedLineCount >= linePool.Count)
        {
            linePool.Add(CreateLine());
        }

        LineRenderer line = linePool[usedLineCount];
        usedLineCount++;
        line.gameObject.SetActive(true);
        return line;
    }

    private LineRenderer CreateLine()
    {
        GameObject lineObject = new("NavMesh Grid Line");
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.numCapVertices = 0;
        line.numCornerVertices = 0;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = GetLineMaterial();
        line.startColor = lineColor;
        line.endColor = lineColor;
        return line;
    }

    private MeshRenderer GetOccupiedCell()
    {
        if (usedOccupiedCellCount >= occupiedCellPool.Count)
        {
            occupiedCellPool.Add(CreateOccupiedCell());
        }

        MeshRenderer cell = occupiedCellPool[usedOccupiedCellCount];
        usedOccupiedCellCount++;
        cell.gameObject.SetActive(true);
        return cell;
    }

    private MeshRenderer GetPreviewCell()
    {
        if (usedPreviewCellCount >= previewCellPool.Count)
        {
            previewCellPool.Add(CreatePreviewCell());
        }

        MeshRenderer cell = previewCellPool[usedPreviewCellCount];
        usedPreviewCellCount++;
        cell.gameObject.SetActive(true);
        return cell;
    }

    private MeshRenderer CreateOccupiedCell()
    {
        GameObject cellObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cellObject.name = "Occupied Grid Cell";
        cellObject.transform.SetParent(transform, false);

        Collider collider = cellObject.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        MeshRenderer renderer = cellObject.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = GetOccupiedCellMaterial();

        MeshFilter meshFilter = cellObject.GetComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateCellMesh();
        return renderer;
    }

    private MeshRenderer CreatePreviewCell()
    {
        GameObject cellObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cellObject.name = "Preview Grid Cell";
        cellObject.transform.SetParent(transform, false);

        Collider collider = cellObject.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        MeshRenderer renderer = cellObject.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = GetPreviewCellMaterial();

        MeshFilter meshFilter = cellObject.GetComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateCellMesh();
        return renderer;
    }

    private static Mesh CreateCellMesh()
    {
        Mesh mesh = new()
        {
            name = "Curved Grid Cell Mesh"
        };

        mesh.vertices = new Vector3[4];
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };

        return mesh;
    }

    private static void SetCellMesh(MeshRenderer cellRenderer, Vector3[] worldCorners)
    {
        MeshFilter meshFilter = cellRenderer.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return;
        }

        Transform cellTransform = cellRenderer.transform;
        cellTransform.localPosition = Vector3.zero;
        cellTransform.localRotation = Quaternion.identity;
        cellTransform.localScale = Vector3.one;

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] localCorners =
        {
            cellTransform.InverseTransformPoint(worldCorners[0]),
            cellTransform.InverseTransformPoint(worldCorners[1]),
            cellTransform.InverseTransformPoint(worldCorners[2]),
            cellTransform.InverseTransformPoint(worldCorners[3])
        };

        mesh.vertices = localCorners;
        mesh.RecalculateBounds();
    }

    private Material GetLineMaterial()
    {
        if (generatedLineMaterial != null)
        {
            return generatedLineMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        shader ??= Shader.Find("Sprites/Default");
        shader ??= Shader.Find("Unlit/Color");

        generatedLineMaterial = new Material(shader)
        {
            name = "Generated NavMesh Placement Grid Material",
            color = lineColor,
            renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent
        };

        if (generatedLineMaterial.HasProperty("_Surface"))
        {
            generatedLineMaterial.SetFloat("_Surface", 1f);
        }

        if (generatedLineMaterial.HasProperty("_Blend"))
        {
            generatedLineMaterial.SetFloat("_Blend", 0f);
        }

        if (generatedLineMaterial.HasProperty("_SrcBlend"))
        {
            generatedLineMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (generatedLineMaterial.HasProperty("_DstBlend"))
        {
            generatedLineMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (generatedLineMaterial.HasProperty("_ZWrite"))
        {
            generatedLineMaterial.SetFloat("_ZWrite", 0f);
        }

        generatedLineMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return generatedLineMaterial;
    }

    private Material GetOccupiedCellMaterial()
    {
        if (generatedOccupiedCellMaterial != null)
        {
            return generatedOccupiedCellMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        shader ??= Shader.Find("Sprites/Default");
        shader ??= Shader.Find("Unlit/Color");

        generatedOccupiedCellMaterial = new Material(shader)
        {
            name = "Generated Occupied Grid Cell Material",
            color = occupiedCellColor,
            renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent
        };

        if (generatedOccupiedCellMaterial.HasProperty("_Surface"))
        {
            generatedOccupiedCellMaterial.SetFloat("_Surface", 1f);
        }

        if (generatedOccupiedCellMaterial.HasProperty("_Blend"))
        {
            generatedOccupiedCellMaterial.SetFloat("_Blend", 0f);
        }

        if (generatedOccupiedCellMaterial.HasProperty("_SrcBlend"))
        {
            generatedOccupiedCellMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (generatedOccupiedCellMaterial.HasProperty("_DstBlend"))
        {
            generatedOccupiedCellMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (generatedOccupiedCellMaterial.HasProperty("_ZWrite"))
        {
            generatedOccupiedCellMaterial.SetFloat("_ZWrite", 0f);
        }

        generatedOccupiedCellMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return generatedOccupiedCellMaterial;
    }

    private Material GetPreviewCellMaterial()
    {
        if (generatedPreviewCellMaterial != null)
        {
            return generatedPreviewCellMaterial;
        }

        generatedPreviewCellMaterial = CreateTransparentMaterial(
            "Generated Preview Grid Cell Material",
            previewCellColor
        );

        return generatedPreviewCellMaterial;
    }

    private static Material CreateTransparentMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        shader ??= Shader.Find("Sprites/Default");
        shader ??= Shader.Find("Unlit/Color");

        Material material = new(shader)
        {
            name = materialName,
            color = color,
            renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent
        };

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return material;
    }

    private void HideAllLines()
    {
        for (int i = 0; i < linePool.Count; i++)
        {
            linePool[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < occupiedCellPool.Count; i++)
        {
            occupiedCellPool[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < previewCellPool.Count; i++)
        {
            previewCellPool[i].gameObject.SetActive(false);
        }

        usedLineCount = 0;
        usedOccupiedCellCount = 0;
        usedPreviewCellCount = 0;
    }

    private readonly struct SurfaceSample
    {
        public readonly bool IsValid;
        public readonly Vector3 Point;

        public SurfaceSample(bool isValid, Vector3 point)
        {
            IsValid = isValid;
            Point = point;
        }
    }
}
