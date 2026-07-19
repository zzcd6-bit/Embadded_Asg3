using System.Collections.Generic;
using UnityEngine;

public class GlobalHeatManager : MonoBehaviour
{
    public static GlobalHeatManager Instance { get; private set; }

    [Header("Global Traffic Value (0..1)")]
    [Range(0f, 1f)]
    public float visitPenalty = 0.3f;      // 每个敌人经过扣多少
    public float recoverPerSecond = 0.02f; // 每秒恢复多少

    // TrafficHeat（惩罚热）
    private Dictionary<PatrolNode, float> trafficHeat = new Dictionary<PatrolNode, float>();

    // 玩家吸引热
    private Dictionary<PatrolNode, float> playerHeat = new Dictionary<PatrolNode, float>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        CoolDownTraffic(dt);
    }

    // =========================
    // Traffic Heat (兼容旧接口)
    // =========================
    public float GetHeat(PatrolNode node) => GetTrafficHeat(node);

    public void AddTrafficHeat(PatrolNode node, float amount)
    {
        if (node == null) return;

        if (!trafficHeat.ContainsKey(node))
            trafficHeat[node] = 1f;

        trafficHeat[node] -= visitPenalty;
        trafficHeat[node] = Mathf.Clamp01(trafficHeat[node]);
    }

    public float GetTrafficHeat(PatrolNode node)
    {
        if (node == null) return 1f;
        return trafficHeat.TryGetValue(node, out float h) ? h : 1f;
    }

    private void CoolDownTraffic(float dt)
    {
        if (trafficHeat.Count == 0) return;

        var keys = new List<PatrolNode>(trafficHeat.Keys);
        foreach (var k in keys)
        {
            float v = trafficHeat[k] + recoverPerSecond * dt;
            if (v >= 1f)
                trafficHeat.Remove(k);   // 完全恢复就删
            else
                trafficHeat[k] = v;
        }
    }

    // =========================
    // Player Heat (新增接口)
    // =========================
    public void SetPlayerField(Dictionary<PatrolNode, float> field)
    {
        playerHeat.Clear();
        foreach (var kv in field)
            playerHeat[kv.Key] = kv.Value;
    }

    public float GetPlayerHeat(PatrolNode node)
    {
        if (node == null) return 0f;
        return playerHeat.TryGetValue(node, out float v) ? v : 0f;
    }


}
