using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Flags]
public enum ZoneTag
{
    None = 0,
    A = 1 << 0,
    B = 1 << 1,
    C = 1 << 2,
    D = 1 << 3,
    E = 1 << 4,
    // 需要更多就继续加：F = 1<<5 ...
}

[ExecuteAlways]
public class PatrolNode : MonoBehaviour
{
    [Header("Graph")]
    [Tooltip("Adjacent patrol nodes (manually assigned in Inspector).")]
    public List<PatrolNode> neighbors = new List<PatrolNode>();
    public Vector3 Position => transform.position;

    [Header("Zone Tags")]
    public ZoneTag zones = ZoneTag.None;

    [Header("Debug (runtime)")]
    public float debugWeightedW;

    [Min(0.01f)]
    [Tooltip("Base selection weight for random picking.")]
    public float baseWeight = 1f;

    [Tooltip("If enabled, ensures neighbor nodes also link back to this node.")]
    public bool bidirectional = true;

    [Header("Gizmos")]
    public float gizmoRadius = 0.25f;
    public bool drawLabels = true;
    
    private void OnValidate()
    {
        CleanupNeighbors();

        if (bidirectional)
        {
            EnsureBidirectionalLinks();
        }
    }

    private static ZoneTag GetPrimaryZone(ZoneTag z)
    {
        if (z == ZoneTag.None) return ZoneTag.None;

        // 按 A->E 顺序找第一个置位的 tag
        ZoneTag[] order = { ZoneTag.A, ZoneTag.B, ZoneTag.C, ZoneTag.D, ZoneTag.E };
        foreach (var t in order)
            if ((z & t) != 0) return t;

        return ZoneTag.None;
    }

    private static Color GetZoneColor(ZoneTag primary)
    {
        switch (primary)
        {
            case ZoneTag.A: return new Color(0.2f, 0.8f, 1.0f); // 青蓝
            case ZoneTag.B: return new Color(1.0f, 0.6f, 0.2f); // 橙
            case ZoneTag.C: return new Color(0.5f, 1.0f, 0.4f); // 绿
            case ZoneTag.D: return new Color(1.0f, 0.3f, 0.3f); // 红
            case ZoneTag.E: return new Color(0.75f, 0.5f, 1.0f); // 紫
            default:        return Color.white;
        }
    }

    private void CleanupNeighbors()
    {
        // 1) Remove self only (but keep null placeholders so Inspector '+' works)
        for (int i = neighbors.Count - 1; i >= 0; i--)
        {
            if (neighbors[i] == this)
                neighbors.RemoveAt(i);
        }

        // 2) Remove duplicates while keeping nulls
        var seen = new HashSet<PatrolNode>();
        for (int i = neighbors.Count - 1; i >= 0; i--)
        {
            var n = neighbors[i];
            if (n == null) continue; // keep null placeholders

            if (!seen.Add(n))
                neighbors.RemoveAt(i);
        }

        if (baseWeight < 0.01f)
            baseWeight = 0.01f;
    }

    private void EnsureBidirectionalLinks()
    {
        foreach (var n in neighbors)
        {
            if (n == null) continue;

            // only auto-add back-link if the other node also wants bidirectional
            if (n.bidirectional && !n.neighbors.Contains(this))
            {
                n.neighbors.Add(this);
                
                #if UNITY_EDITOR
                    EditorUtility.SetDirty(n);
                #endif
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Node
        var primary = GetPrimaryZone(zones);
        var zoneColor = GetZoneColor(primary);

        // Node
        Gizmos.color = zoneColor;
        Gizmos.DrawSphere(transform.position, gizmoRadius);

        // Links：用“本节点主色”画线（最简单，视觉清晰）
        Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.7f);
        foreach (var n in neighbors)
        {
            if (n == null) continue;
                Gizmos.DrawLine(transform.position, n.transform.position);
        }

        #if UNITY_EDITOR
        if (drawLabels) 
        {
            string text = $"{name}";
            
            if (!Application.isPlaying) {text += "\nW: "+ baseWeight;}
            else
            {
                if (EnemyAI.DebugSource != null && EnemyAI.DebugSource.TryGetLastWeight(this, out var info))
                {
                    text +=
                    $"\nW:{info.finalW:0.###}" +
                    $"\nind: {info.backPen:0.##} | {info.selfPen:0.##} | {info.playerTrack:0.##}" +
                    $"\nglo:     {info.closePen:0.##} | {info.globalPen:0.##}";
                }
                else text += "\nW: -";   
            }
            Handles.Label(transform.position + Vector3.up * (gizmoRadius + 0.05f), text);
        }
        #endif
    }
}

