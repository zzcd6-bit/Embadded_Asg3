using UnityEngine;
using System.Collections;

public class BossManager : MonoBehaviour
{
    [Header("=== Boss设置 ===")]
    public GameObject Boss;                    // Boss对象
    public Transform bossSpawnPoint;          // Boss生成位置（可选）
    
    [Header("=== UI元素 ===")]
    public GameObject bossHealthBar;          // Boss血条UI
    [Header("=== Intro UI (2 steps) ===")]
    public GameObject introUI_1;          // 第一个UI（先出现，阻塞流程3秒）
    public GameObject introUI_2;          // 第二个UI（流程开始时出现，不阻塞，3秒）
    public float introUIDuration = 3f;    // 两个UI总时长（你要固定3秒）
    [Range(0.05f, 2f)] public float fadeTime = 0.35f; // 淡入淡出时间（建议0.25~0.6）
    
    [Header("=== 环境控制 ===")]
    public GameObject bossCinematicCamera;    // 过场摄像机
    
    [Header("=== 触发设置 ===")]
    public bool canTriggerMultipleTimes = false; // 是否可以多次触发
    private bool hasBeenTriggered = false;       // 是否已触发过
    
    [Header("=== 延迟和效果 ===")]
    public float startDelay = 1.5f;           // 开始延迟
    public GameObject bossSpawnEffect;    // 生成特效
    
    private Collider triggerArea;
    
    void Start()
    {
        // 获取或添加碰撞体
        triggerArea = GetComponent<Collider>();
        if (triggerArea == null)
        {
            triggerArea = gameObject.AddComponent<BoxCollider>();
        }
        triggerArea.isTrigger = true;
        
        // 初始化：隐藏所有Boss相关元素
        InitializeBossFight();

        // 初始化把UI关掉并把alpha归零
        PrepUI(introUI_1);
        PrepUI(introUI_2);
    }
    
    void InitializeBossFight()
    {
        // 隐藏Boss（如果存在）
        if (Boss != null)
        {
            Boss.SetActive(false);
        }
        
        // 隐藏UI元素
        if (bossHealthBar != null)
            bossHealthBar.SetActive(false);
    }
    
    void OnTriggerEnter(Collider other)
    {
        // 检查是否已触发且不允许重复触发
        if (hasBeenTriggered && !canTriggerMultipleTimes)
            return;
            
        // 检查是否是玩家
        if (other.CompareTag("Player"))
        {
            StartBossFight();
            hasBeenTriggered = true;
            
            // 可选：触发后禁用触发器
            if (!canTriggerMultipleTimes)
                triggerArea.enabled = false;
        }
    }
    
    void StartBossFight()
    {
        StartCoroutine(BossFightSequence());
    }
    
    // =========================
    // UI Helpers
    // =========================
    private void PrepUI(GameObject go)
    {
        if (go == null) return;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        go.SetActive(false);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float t)
    {
        if (cg == null) yield break;
        t = Mathf.Max(0.01f, t);

        float elapsed = 0f;
        cg.alpha = from;

        while (elapsed < t)
        {
            elapsed += Time.unscaledDeltaTime; // ✅ 不受 timeScale 影响
            cg.alpha = Mathf.Lerp(from, to, elapsed / t);
            yield return null;
        }

        cg.alpha = to;
    }


    // 总时长 duration（包含淡入+停留+淡出），淡入淡出时间 fade
    private IEnumerator PlayUIForDuration(GameObject go, float duration, float fade)
    {
        if (go == null) yield break;

        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();

        go.SetActive(true);

        // ✅ 立刻强制刷新一次，确保真的被激活（如果仍然 false，说明你引用错对象或被别的脚本关掉）
        yield return null;

        Debug.Log($"[UI] After SetActive: {go.name} activeSelf={go.activeSelf} activeInHierarchy={go.activeInHierarchy}", go);


        float f = Mathf.Clamp(fade, 0.01f, duration * 0.49f);
        float hold = Mathf.Max(0f, duration - f * 2f);

        // Fade In
        yield return FadeCanvasGroup(cg, 0f, 1f, f);

        // Hold（✅ 不受 timeScale 影响）
        if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

        // Fade Out
        yield return FadeCanvasGroup(cg, 1f, 0f, f);

        go.SetActive(false);
    }


    IEnumerator BossFightSequence()
    {
        Debug.Log("[BossManager] Boss fight triggered.");

        // 0) 基础校验（避免空引用导致流程中断）
        if (Boss == null)
        {
            Debug.LogError("[BossManager] Boss is NULL.");
            yield break;
        }
        // =========================
        // ① UI1 最先出现：阻塞3秒（含淡入淡出）
        // =========================
        if (introUI_1 != null)
        {
            Debug.Log($"UI1={(introUI_1 ? introUI_1.name : "NULL")}  UI2={(introUI_2 ? introUI_2.name : "NULL")}");

            yield return PlayUIForDuration(introUI_1, introUIDuration, fadeTime);
        }
        else
        {
            // 没拖UI1也别卡死流程
            yield return null;
        }

        // =========================
        // ② UI1结束后：开始执行剩余流程
        // 同时 UI2 出现：不阻塞
        // =========================
        if (introUI_2 != null)
        {
            StartCoroutine(PlayUIForDuration(introUI_2, introUIDuration, fadeTime));
        }
        
        if (bossSpawnPoint == null)
        {
            Debug.LogWarning("[BossManager] bossSpawnPoint is NULL. Boss will spawn at its current position.");
        }

        // 1) 延迟（给你做提示/音效/门锁等的时间）
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        // 2) 生成特效（可选）
        if (bossSpawnEffect != null && bossSpawnPoint != null)
        {
            Instantiate(bossSpawnEffect, bossSpawnPoint.position, bossSpawnPoint.rotation);
            yield return new WaitForSeconds(0.5f);
        }

        // 3) 把Boss放到生成点（在 SetActive(true) 之前做更干净）
        if (bossSpawnPoint != null)
        {
            Boss.transform.SetPositionAndRotation(bossSpawnPoint.position, bossSpawnPoint.rotation);
        }

        // 4) 激活Boss对象
        Boss.SetActive(true);

        // 5) 强制确保所有 Renderer 开着（防止“Animator在跑但看不到模型”）
        var renderers = Boss.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = true;

        // 6) 触发Boss入场/开战逻辑（BossController 内部负责 BossIn trigger + 冷却）
        BossController bossController = Boss.GetComponent<BossController>();
        if (bossController != null)
        {
            bossController.StartBossFight();
        }
        else
        {
            Debug.LogWarning("[BossManager] BossController not found on Boss.");
        }

        // 7) 显示Boss血条UI
        if (bossHealthBar != null)
            bossHealthBar.SetActive(true);

        Debug.Log("[BossManager] Boss fight started (no camera flow).");
    }

    public void Death()
    {
        Debug.Log("[BossManager] Boss Death() called.");

        // 关血条
        if (bossHealthBar != null)
            bossHealthBar.SetActive(false);

        // 这里写你的结算逻辑：
        // - 掉落
        // - 开门
        // - 播BGM
        // - 结束Boss战UI
        // - 禁用BossController等（可选）
    }



    // 在编辑器中显示触发器范围（可视化调试）
    void OnDrawGizmos()
    {
        if (triggerArea == null)
            triggerArea = GetComponent<Collider>();
            
        if (triggerArea != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawCube(transform.position + triggerArea.bounds.center, triggerArea.bounds.size);
        }
    }
}