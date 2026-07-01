using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
public sealed class BoatCurvePath : MonoBehaviour
{
    public enum PointSource
    {
        Children,
        ExplicitList
    }

    [SerializeField] private PointSource pointSource = PointSource.Children;
    [SerializeField] private Transform[] controlPoints = new Transform[0];
    [SerializeField, FormerlySerializedAs("samplesPerSegment"), Min(2), Tooltip("How many intervals the whole curve is split into for cached sampled points.")]
    private int curveSampleCount = 128;
    [SerializeField] private bool closed;
    [SerializeField] private bool rebuildInEditMode = true;
    [SerializeField] private Color curveColor = new Color(0.1f, 0.75f, 1f, 1f);
    [SerializeField] private Color pointColor = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField, Min(0.02f)] private float pointGizmoRadius = 0.18f;
    [SerializeField, HideInInspector] private List<Vector3> cachedCurve = new List<Vector3>();
    [SerializeField, HideInInspector] private List<float> cachedDistances = new List<float>();
    [SerializeField, HideInInspector] private float totalLength;

    private readonly List<Transform> resolvedPoints = new List<Transform>();

    public IReadOnlyList<Vector3> CachedCurve => cachedCurve;
    public float TotalLength => totalLength;
    public bool Closed => closed;

    private void Awake()
    {
        RebuildCurve();
    }

    private void OnEnable()
    {
        RebuildCurve();
    }

    private void OnValidate()
    {
        curveSampleCount = Mathf.Max(2, curveSampleCount);
        pointGizmoRadius = Mathf.Max(0.02f, pointGizmoRadius);

        if (!Application.isPlaying && rebuildInEditMode)
        {
            RebuildCurve();
        }
    }

    private void Update()
    {
        if (!Application.isPlaying && rebuildInEditMode)
        {
            RebuildCurve();
        }
    }

    [ContextMenu("Rebuild Curve")]
    public void RebuildCurve()
    {
        ResolvePoints();
        cachedCurve.Clear();
        cachedDistances.Clear();
        totalLength = 0f;

        int count = resolvedPoints.Count;
        if (count == 0)
        {
            return;
        }

        if (count == 1)
        {
            AddSample(resolvedPoints[0].position);
            return;
        }

        List<Vector3> rawCurve = BuildRawCurve();
        List<float> rawDistances = BuildDistanceCache(rawCurve, out float rawLength);
        if (rawCurve.Count == 0)
        {
            return;
        }

        if (rawLength <= Mathf.Epsilon)
        {
            AddSample(rawCurve[0]);
            return;
        }

        for (int sample = 0; sample <= curveSampleCount; sample++)
        {
            float distance = rawLength * sample / curveSampleCount;
            AddSample(GetPositionFromDistanceCache(rawCurve, rawDistances, distance));
        }
    }

    public Vector3 GetPositionAtDistance(float distance)
    {
        EnsureCurve();

        if (cachedCurve.Count == 0)
        {
            return transform.position;
        }

        if (cachedCurve.Count == 1 || totalLength <= Mathf.Epsilon)
        {
            return cachedCurve[0];
        }

        float pathDistance = closed ? RepeatDistance(distance) : Mathf.Clamp(distance, 0f, totalLength);

        for (int i = 1; i < cachedDistances.Count; i++)
        {
            if (cachedDistances[i] < pathDistance)
            {
                continue;
            }

            float previousDistance = cachedDistances[i - 1];
            float segmentLength = cachedDistances[i] - previousDistance;
            float t = segmentLength <= Mathf.Epsilon ? 0f : (pathDistance - previousDistance) / segmentLength;
            return Vector3.Lerp(cachedCurve[i - 1], cachedCurve[i], t);
        }

        return cachedCurve[cachedCurve.Count - 1];
    }

    public Vector3 GetVelocityDirectionAtDistance(float distance)
    {
        EnsureCurve();

        if (cachedCurve.Count < 2 || totalLength <= Mathf.Epsilon)
        {
            return transform.forward;
        }

        float step = Mathf.Max(0.05f, totalLength / Mathf.Max(16f, cachedCurve.Count));
        Vector3 before = GetPositionAtDistance(distance - step);
        Vector3 after = GetPositionAtDistance(distance + step);
        Vector3 direction = after - before;

        if (direction.sqrMagnitude <= 0.000001f)
        {
            return transform.forward;
        }

        return direction.normalized;
    }

    public float GetSignedSampleAngle(float distance, int pointOffset, Vector3 signedAxis)
    {
        return GetSignedSampleAngleAtPointOffset(distance, 0, pointOffset, signedAxis);
    }

    public float GetSignedSampleAngleAtPointOffset(float distance, int lookAheadPointOffset, int pointOffset, Vector3 signedAxis)
    {
        EnsureCurve();

        if (cachedCurve.Count < 3 || totalLength <= Mathf.Epsilon)
        {
            return 0f;
        }

        Vector3 axis = signedAxis.sqrMagnitude <= 0.000001f ? Vector3.up : signedAxis.normalized;
        int offset = Mathf.Max(1, pointOffset);
        int currentIndex = GetNearestSampleIndexAtDistance(distance) + Mathf.Max(0, lookAheadPointOffset);
        currentIndex = closed ? Mod(currentIndex, cachedCurve.Count) : Mathf.Clamp(currentIndex, 0, cachedCurve.Count - 1);
        int previousIndex = closed ? Mod(currentIndex - offset, cachedCurve.Count) : Mathf.Max(0, currentIndex - offset);
        int nextIndex = closed ? Mod(currentIndex + offset, cachedCurve.Count) : Mathf.Min(cachedCurve.Count - 1, currentIndex + offset);

        if (previousIndex == currentIndex || nextIndex == currentIndex)
        {
            return 0f;
        }

        Vector3 incoming = ProjectDirection(cachedCurve[currentIndex] - cachedCurve[previousIndex], axis);
        Vector3 outgoing = ProjectDirection(cachedCurve[nextIndex] - cachedCurve[currentIndex], axis);
        if (incoming.sqrMagnitude <= 0.000001f || outgoing.sqrMagnitude <= 0.000001f)
        {
            return 0f;
        }

        return Vector3.SignedAngle(incoming, outgoing, axis);
    }

    public float GetNearestDistance(Vector3 worldPosition)
    {
        EnsureCurve();

        if (cachedCurve.Count == 0)
        {
            return 0f;
        }

        float bestSqrDistance = float.PositiveInfinity;
        float bestPathDistance = 0f;

        for (int i = 1; i < cachedCurve.Count; i++)
        {
            Vector3 a = cachedCurve[i - 1];
            Vector3 b = cachedCurve[i];
            Vector3 ab = b - a;
            float abSqr = ab.sqrMagnitude;
            float t = abSqr <= Mathf.Epsilon ? 0f : Mathf.Clamp01(Vector3.Dot(worldPosition - a, ab) / abSqr);
            Vector3 candidate = Vector3.Lerp(a, b, t);
            float sqrDistance = (worldPosition - candidate).sqrMagnitude;

            if (sqrDistance >= bestSqrDistance)
            {
                continue;
            }

            bestSqrDistance = sqrDistance;
            bestPathDistance = cachedDistances[i - 1] + Mathf.Sqrt(abSqr) * t;
        }

        return bestPathDistance;
    }

    private List<Vector3> BuildRawCurve()
    {
        List<Vector3> rawCurve = new List<Vector3>();
        int count = resolvedPoints.Count;
        if (count == 0)
        {
            return rawCurve;
        }

        rawCurve.Add(resolvedPoints[0].position);

        if (count == 1)
        {
            return rawCurve;
        }

        int segmentCount = closed ? count : count - 1;
        int rawSamplesPerSegment = Mathf.Max(8, Mathf.CeilToInt(curveSampleCount / (float)segmentCount) * 4);
        for (int segment = 0; segment < segmentCount; segment++)
        {
            for (int sample = 1; sample <= rawSamplesPerSegment; sample++)
            {
                float t = sample / (float)rawSamplesPerSegment;
                rawCurve.Add(EvaluateSegment(segment, t));
            }
        }

        return rawCurve;
    }

    private static List<float> BuildDistanceCache(IReadOnlyList<Vector3> points, out float length)
    {
        List<float> distances = new List<float>(points.Count);
        length = 0f;

        if (points.Count == 0)
        {
            return distances;
        }

        distances.Add(0f);
        for (int i = 1; i < points.Count; i++)
        {
            length += Vector3.Distance(points[i - 1], points[i]);
            distances.Add(length);
        }

        return distances;
    }

    private static Vector3 GetPositionFromDistanceCache(IReadOnlyList<Vector3> points, IReadOnlyList<float> distances, float distance)
    {
        if (points.Count == 0)
        {
            return Vector3.zero;
        }

        if (points.Count == 1 || distances.Count != points.Count)
        {
            return points[0];
        }

        float clampedDistance = Mathf.Clamp(distance, 0f, distances[distances.Count - 1]);
        for (int i = 1; i < distances.Count; i++)
        {
            if (distances[i] < clampedDistance)
            {
                continue;
            }

            float previousDistance = distances[i - 1];
            float segmentLength = distances[i] - previousDistance;
            float t = segmentLength <= Mathf.Epsilon ? 0f : (clampedDistance - previousDistance) / segmentLength;
            return Vector3.Lerp(points[i - 1], points[i], t);
        }

        return points[points.Count - 1];
    }

    private void ResolvePoints()
    {
        resolvedPoints.Clear();

        if (pointSource == PointSource.Children)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null && child.gameObject.activeInHierarchy)
                {
                    resolvedPoints.Add(child);
                }
            }

            return;
        }

        foreach (Transform point in controlPoints)
        {
            if (point != null)
            {
                resolvedPoints.Add(point);
            }
        }
    }

    private Vector3 EvaluateSegment(int segment, float normalizedT)
    {
        int count = resolvedPoints.Count;
        if (count == 2)
        {
            int next = closed ? (segment + 1) % count : Mathf.Min(segment + 1, count - 1);
            return Vector3.Lerp(resolvedPoints[segment].position, resolvedPoints[next].position, normalizedT);
        }

        if (closed)
        {
            int previousIndex = Mod(segment - 1, count);
            int currentIndex = Mod(segment, count);
            int nextIndex = Mod(segment + 1, count);
            return EvaluateQuadratic(
                resolvedPoints[previousIndex].position,
                resolvedPoints[currentIndex].position,
                resolvedPoints[nextIndex].position,
                1f + normalizedT);
        }

        if (segment == 0)
        {
            return EvaluateQuadratic(
                resolvedPoints[0].position,
                resolvedPoints[1].position,
                resolvedPoints[2].position,
                normalizedT);
        }

        if (segment >= count - 2)
        {
            return EvaluateQuadratic(
                resolvedPoints[count - 3].position,
                resolvedPoints[count - 2].position,
                resolvedPoints[count - 1].position,
                1f + normalizedT);
        }

        return EvaluateQuadratic(
            resolvedPoints[segment - 1].position,
            resolvedPoints[segment].position,
            resolvedPoints[segment + 1].position,
            1f + normalizedT);
    }

    private static Vector3 EvaluateQuadratic(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float l0 = (t - 1f) * (t - 2f) * 0.5f;
        float l1 = -t * (t - 2f);
        float l2 = t * (t - 1f) * 0.5f;
        return p0 * l0 + p1 * l1 + p2 * l2;
    }

    private void AddSample(Vector3 point)
    {
        if (cachedCurve.Count == 0)
        {
            cachedCurve.Add(point);
            cachedDistances.Add(0f);
            return;
        }

        totalLength += Vector3.Distance(cachedCurve[cachedCurve.Count - 1], point);
        cachedCurve.Add(point);
        cachedDistances.Add(totalLength);
    }

    private void EnsureCurve()
    {
        if (cachedCurve.Count == 0 || cachedDistances.Count != cachedCurve.Count)
        {
            RebuildCurve();
        }
    }

    private float RepeatDistance(float distance)
    {
        if (totalLength <= Mathf.Epsilon)
        {
            return 0f;
        }

        float repeated = distance % totalLength;
        return repeated < 0f ? repeated + totalLength : repeated;
    }

    private static int Mod(int value, int length)
    {
        int result = value % length;
        return result < 0 ? result + length : result;
    }

    private int GetNearestSampleIndexAtDistance(float distance)
    {
        if (cachedDistances.Count == 0)
        {
            return 0;
        }

        float pathDistance = closed ? RepeatDistance(distance) : Mathf.Clamp(distance, 0f, totalLength);
        int low = 0;
        int high = cachedDistances.Count - 1;

        while (low < high)
        {
            int middle = (low + high) / 2;
            if (cachedDistances[middle] < pathDistance)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        int nextIndex = low;
        int previousIndex = Mathf.Max(0, nextIndex - 1);
        float previousDelta = Mathf.Abs(cachedDistances[previousIndex] - pathDistance);
        float nextDelta = Mathf.Abs(cachedDistances[nextIndex] - pathDistance);
        return previousDelta <= nextDelta ? previousIndex : nextIndex;
    }

    private static Vector3 ProjectDirection(Vector3 direction, Vector3 normal)
    {
        Vector3 projected = Vector3.ProjectOnPlane(direction, normal);
        return projected.sqrMagnitude <= 0.000001f ? Vector3.zero : projected.normalized;
    }

    private void OnDrawGizmos()
    {
        if (rebuildInEditMode)
        {
            RebuildCurve();
        }

        Gizmos.color = curveColor;
        for (int i = 1; i < cachedCurve.Count; i++)
        {
            Gizmos.DrawLine(cachedCurve[i - 1], cachedCurve[i]);
        }

        if (closed && cachedCurve.Count > 2)
        {
            Gizmos.DrawLine(cachedCurve[cachedCurve.Count - 1], cachedCurve[0]);
        }

        ResolvePoints();
        Gizmos.color = pointColor;
        foreach (Transform point in resolvedPoints)
        {
            Gizmos.DrawSphere(point.position, pointGizmoRadius);
        }
    }
}
