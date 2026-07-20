using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PatrolSimulationTool : MonoBehaviour
{
    public static PatrolSimulationTool Instance;

    [Header("Simulation")]
    public EnemyAI enemySource;      // 用来调用 PickNextNode（Prefab 或 Scene 实例）
    public PatrolNode startNode;
    public int simulationSteps = 10000;

    [Header("Heatmap")]
    public Gradient heatmapGradient; // 蓝→红
    public float gizmoRadius = 0.35f;

    // ---------- 内部统计 ----------
    private Dictionary<PatrolNode, int> visitCount = new();
    private int totalVisits = 0;

    void Awake()
    {
        Instance = this;
    }

    // ===============================
    // 统计接口（EnemyAI / 模拟共用）
    // ===============================
    public void ClearStats()
    {
        visitCount.Clear();
        totalVisits = 0;
    }

    public void RegisterVisit(PatrolNode node)
    {
        if (node == null) return;

        if (!visitCount.ContainsKey(node))
            visitCount[node] = 0;

        visitCount[node]++;
        totalVisits++;
    }

    public int GetVisitCount(PatrolNode node)
    {
        return visitCount.TryGetValue(node, out int c) ? c : 0;
    }

    public float GetVisitRatio(PatrolNode node)
    {
        if (totalVisits <= 0) return 0f;
        return GetVisitCount(node) / (float)totalVisits;
    }

    // ===============================
    // 一键模拟（核心）
    // ===============================
    [ContextMenu("Run Patrol Simulation")]
    public void RunSimulation()
    {
        if (enemySource == null || startNode == null)
        {
            Debug.LogError("PatrolSimulationTool: Missing enemySource or startNode.");
            return;
        }

        ClearStats();

        PatrolNode current = startNode;
        PatrolNode previous = null;

        for (int i = 0; i < simulationSteps; i++)
        {
            RegisterVisit(current);

            PatrolNode next = enemySource.DebugPickNextNode(current, previous);
            previous = current;
            current = next;
        }

        Debug.Log($"[PatrolSimulation] Done: {simulationSteps} steps");
    }

#if UNITY_EDITOR
    // ===============================
    // Scene 视图热力图
    // ===============================
    void OnDrawGizmos()
    {
        if (visitCount.Count == 0) return;

        PatrolNode[] nodes = Object.FindObjectsByType<PatrolNode>(FindObjectsSortMode.None);
        int max = 0;

        foreach (var n in nodes)
            max = Mathf.Max(max, GetVisitCount(n));

        if (max <= 0) return;

        foreach (var n in nodes)
        {
            float t = GetVisitCount(n) / (float)max;
            Gizmos.color = heatmapGradient.Evaluate(t);
            Gizmos.DrawSphere(n.transform.position, gizmoRadius);

            Handles.Label(
                n.transform.position + Vector3.up * gizmoRadius,
                $"{(t * 100f):0.0}%"
            );
        }
    }
#endif
}
