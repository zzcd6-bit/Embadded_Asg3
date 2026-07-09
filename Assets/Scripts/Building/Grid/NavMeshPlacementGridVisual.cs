using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshPlacementGridVisual : MonoBehaviour
{
    [SerializeField] private GridPlacementSystem gridPlacementSystem;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform centerTarget;
    [SerializeField] private int radiusInCells = 12;
    [SerializeField] private float surfaceOffset = 0.035f;
    [SerializeField] private float navMeshSampleDistance = 0.45f;
    [SerializeField] private float lineWidth = 0.025f;
    [SerializeField] private Color lineColor = new(0.2f, 0.95f, 0.85f, 0.42f);
    [SerializeField] private Color occupiedCellColor = new(1f, 0.12f, 0.08f, 0.5f);

    private readonly List<LineRenderer> linePool = new();
    private readonly List<MeshRenderer> occupiedCellPool = new();
    private Material generatedLineMaterial;
    private Material generatedOccupiedCellMaterial;
    private bool isVisible;
    private int usedLineCount;
    private int usedOccupiedCellCount;

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

        for (int x = centerCell.x - radius; x < centerCell.x + radius; x++)
        {
            for (int y = centerCell.y - radius; y <= centerCell.y + radius; y++)
            {
                DrawSegment(new Vector2Int(x, y), new Vector2Int(x + 1, y));
            }
        }

        for (int y = centerCell.y - radius; y < centerCell.y + radius; y++)
        {
            for (int x = centerCell.x - radius; x <= centerCell.x + radius; x++)
            {
                DrawSegment(new Vector2Int(x, y), new Vector2Int(x, y + 1));
            }
        }

        for (int x = centerCell.x - radius; x <= centerCell.x + radius; x++)
        {
            for (int y = centerCell.y - radius; y <= centerCell.y + radius; y++)
            {
                DrawOccupiedCell(new Vector2Int(x, y));
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

    private void DrawSegment(Vector2Int fromCell, Vector2Int toCell)
    {
        if (!TrySampleNavMesh(gridPlacementSystem.CellToWorld(fromCell), out Vector3 from)
            || !TrySampleNavMesh(gridPlacementSystem.CellToWorld(toCell), out Vector3 to))
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

        if (!TrySampleNavMesh(gridPlacementSystem.CellToWorld(cell), out Vector3 position))
        {
            return;
        }

        position.y += surfaceOffset + 0.006f;

        MeshRenderer cellRenderer = GetOccupiedCell();
        Transform cellTransform = cellRenderer.transform;
        cellTransform.position = position;
        cellTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
        cellTransform.localScale = Vector3.one * gridPlacementSystem.CellSize * 0.92f;
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
        return renderer;
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

        usedLineCount = 0;
        usedOccupiedCellCount = 0;
    }
}
