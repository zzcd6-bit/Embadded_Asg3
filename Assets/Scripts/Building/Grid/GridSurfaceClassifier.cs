using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GridSurfaceClassifier : MonoBehaviour
{
    [SerializeField] private GridPlacementSystem gridPlacementSystem;
    [SerializeField] private LayerMask groundLayer = 1 << 6;
    [SerializeField] private float raycastHeight = 40f;
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private float navMeshSampleDistance = 0.45f;
    [SerializeField, Range(0f, 89f)] private float maxSurfaceAngle = 45f;
    [SerializeField] private float layerClusterThreshold = 0.35f;

    private readonly Dictionary<Vector2Int, CellSurfaceInfo> cellCache = new();
    private int cachedFrame = -1;

    private static readonly Vector2[] SampleOffsets =
    {
        new(-0.48f, -0.48f),
        new(0f, -0.48f),
        new(0.48f, -0.48f),
        new(-0.48f, 0f),
        new(0f, 0f),
        new(0.48f, 0f),
        new(-0.48f, 0.48f),
        new(0f, 0.48f),
        new(0.48f, 0.48f)
    };

    private void Awake()
    {
        if (gridPlacementSystem == null)
        {
            gridPlacementSystem = GetComponent<GridPlacementSystem>();
        }
    }

    public bool IsCellBuildableSurface(Vector2Int cell)
    {
        return TryGetCellSurface(cell, out CellSurfaceInfo info)
            && info.HasSurface
            && info.IsFullyOnSurface
            && !info.HasSplit;
    }

    public bool TryGetCellSurface(Vector2Int cell, out CellSurfaceInfo info)
    {
        ClearCacheIfFrameChanged();

        if (cellCache.TryGetValue(cell, out info))
        {
            return info.HasSurface;
        }

        info = SampleCell(cell);
        cellCache[cell] = info;
        return info.HasSurface;
    }

    private void ClearCacheIfFrameChanged()
    {
        if (cachedFrame == Time.frameCount)
        {
            return;
        }

        cachedFrame = Time.frameCount;
        cellCache.Clear();
    }

    private CellSurfaceInfo SampleCell(Vector2Int cell)
    {
        if (gridPlacementSystem == null)
        {
            return CellSurfaceInfo.Empty;
        }

        float minY = float.MaxValue;
        float maxY = float.MinValue;
        float sumY = 0f;
        int validCount = 0;
        Vector3[] sampledPoints = new Vector3[SampleOffsets.Length];

        for (int i = 0; i < SampleOffsets.Length; i++)
        {
            if (!TrySampleGround(cell, SampleOffsets[i], out Vector3 point))
            {
                continue;
            }

            minY = Mathf.Min(minY, point.y);
            maxY = Mathf.Max(maxY, point.y);
            sumY += point.y;
            sampledPoints[validCount] = point;
            validCount++;
        }

        if (validCount == 0)
        {
            return CellSurfaceInfo.Empty;
        }

        float steepestSurfaceAngle = CalculateSteepestSurfaceAngle(sampledPoints, validCount);
        bool hasSplit = steepestSurfaceAngle > maxSurfaceAngle;
        float lowerY = minY;
        float upperY = maxY;

        if (hasSplit)
        {
            CalculateLayerHeights(cell, minY, maxY, out lowerY, out upperY);
        }

        return new CellSurfaceInfo(
            true,
            validCount == SampleOffsets.Length,
            hasSplit,
            minY,
            maxY,
            lowerY,
            upperY,
            sumY / validCount,
            steepestSurfaceAngle
        );
    }

    private static float CalculateSteepestSurfaceAngle(Vector3[] points, int count)
    {
        float steepestAngle = 0f;
        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                Vector2 a = new(points[i].x, points[i].z);
                Vector2 b = new(points[j].x, points[j].z);
                float horizontalDistance = Vector2.Distance(a, b);
                if (horizontalDistance <= 0.001f)
                {
                    continue;
                }

                float angle = Mathf.Atan2(Mathf.Abs(points[i].y - points[j].y), horizontalDistance) * Mathf.Rad2Deg;
                steepestAngle = Mathf.Max(steepestAngle, angle);
            }
        }

        return steepestAngle;
    }

    private void CalculateLayerHeights(Vector2Int cell, float minY, float maxY, out float lowerY, out float upperY)
    {
        float safeCluster = Mathf.Max(0.01f, layerClusterThreshold);
        float lowerSum = 0f;
        float upperSum = 0f;
        int lowerCount = 0;
        int upperCount = 0;

        for (int i = 0; i < SampleOffsets.Length; i++)
        {
            if (!TrySampleGround(cell, SampleOffsets[i], out Vector3 point))
            {
                continue;
            }

            if (point.y <= minY + safeCluster)
            {
                lowerSum += point.y;
                lowerCount++;
            }

            if (point.y >= maxY - safeCluster)
            {
                upperSum += point.y;
                upperCount++;
            }
        }

        lowerY = lowerCount > 0 ? lowerSum / lowerCount : minY;
        upperY = upperCount > 0 ? upperSum / upperCount : maxY;
    }

    private bool TrySampleGround(Vector2Int cell, Vector2 offset, out Vector3 point)
    {
        Vector3 center = gridPlacementSystem.CellToWorld(cell);
        float cellSize = gridPlacementSystem.CellSize;
        Vector3 samplePosition = center + new Vector3(offset.x * cellSize, 0f, offset.y * cellSize);
        Vector3 rayOrigin = samplePosition + Vector3.up * raycastHeight;

        if (!Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                raycastDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore))
        {
            point = samplePosition;
            return false;
        }

        if (!NavMesh.SamplePosition(hit.point, out _, navMeshSampleDistance, NavMesh.AllAreas))
        {
            point = hit.point;
            return false;
        }

        point = hit.point;
        return true;
    }
}

public readonly struct CellSurfaceInfo
{
    public readonly bool HasSurface;
    public readonly bool IsFullyOnSurface;
    public readonly bool HasSplit;
    public readonly float MinY;
    public readonly float MaxY;
    public readonly float LowerY;
    public readonly float UpperY;
    public readonly float RepresentativeY;
    public readonly float SteepestSurfaceAngle;

    public static CellSurfaceInfo Empty => new(false, false, false, 0f, 0f, 0f, 0f, 0f, 0f);

    public CellSurfaceInfo(
        bool hasSurface,
        bool isFullyOnSurface,
        bool hasSplit,
        float minY,
        float maxY,
        float lowerY,
        float upperY,
        float representativeY,
        float steepestSurfaceAngle)
    {
        HasSurface = hasSurface;
        IsFullyOnSurface = isFullyOnSurface;
        HasSplit = hasSplit;
        MinY = minY;
        MaxY = maxY;
        LowerY = lowerY;
        UpperY = upperY;
        RepresentativeY = representativeY;
        SteepestSurfaceAngle = steepestSurfaceAngle;
    }
}
