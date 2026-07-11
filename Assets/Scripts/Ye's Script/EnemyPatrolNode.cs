using System.Collections.Generic;
using UnityEngine;

public class EnemyPatrolNode : MonoBehaviour
{
    [Header("Route")]
    public List<EnemyPatrolNode> nextNodes = new List<EnemyPatrolNode>();

    [Header("Gizmos")]
    [SerializeField] private float gizmoRadius = 0.25f;
    [SerializeField] private Color nodeColor = new Color(0.2f, 0.8f, 1f);
    [SerializeField] private Color linkColor = new Color(0.2f, 0.8f, 1f, 0.65f);

    public Vector3 Position => transform.position;

    public EnemyPatrolNode PickRandomNext()
    {
        if (nextNodes == null || nextNodes.Count == 0)
            return null;

        int validCount = 0;
        for (int i = 0; i < nextNodes.Count; i++)
        {
            if (nextNodes[i] != null)
                validCount++;
        }

        if (validCount == 0)
            return null;

        int pick = Random.Range(0, validCount);
        for (int i = 0; i < nextNodes.Count; i++)
        {
            if (nextNodes[i] == null)
                continue;

            if (pick == 0)
                return nextNodes[i];

            pick--;
        }

        return null;
    }

    private void OnValidate()
    {
        if (nextNodes == null)
            return;

        for (int i = nextNodes.Count - 1; i >= 0; i--)
        {
            if (nextNodes[i] == this)
                nextNodes.RemoveAt(i);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = nodeColor;
        Gizmos.DrawSphere(transform.position, gizmoRadius);

        if (nextNodes == null)
            return;

        Gizmos.color = linkColor;
        for (int i = 0; i < nextNodes.Count; i++)
        {
            EnemyPatrolNode node = nextNodes[i];
            if (node == null)
                continue;

            Gizmos.DrawLine(transform.position, node.transform.position);
            DrawArrowHead(transform.position, node.transform.position);
        }
    }

    private void DrawArrowHead(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            return;

        dir.Normalize();
        Vector3 right = Quaternion.Euler(0f, 35f, 0f) * -dir;
        Vector3 left = Quaternion.Euler(0f, -35f, 0f) * -dir;
        Vector3 tip = Vector3.Lerp(from, to, 0.75f);
        float size = gizmoRadius * 1.5f;

        Gizmos.DrawLine(tip, tip + right * size);
        Gizmos.DrawLine(tip, tip + left * size);
    }
}
