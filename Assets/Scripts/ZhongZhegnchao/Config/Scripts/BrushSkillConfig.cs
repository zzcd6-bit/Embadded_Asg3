using UnityEngine;

[CreateAssetMenu(
    fileName = "BrushSkillConfig",
    menuName = "ZhongZhegnchao/画符技能配置"
)]
public class BrushSkillConfig : ScriptableObject
{
    [Header("技能身份")]
    public BrushSkillType skillType = BrushSkillType.None;

    [Header("通用检测设置")]
    public LayerMask targetLayer;
    public float rayDistance = 200f;
    public float fallbackDistance = 12f;

    [Header("墨囊消耗")]
    public int inkCost = 1;

    [Header("技能冷却")]
    public bool useSkillCooldown = false;

    [Tooltip("技能冷却时间，单位：秒")]
    public float skillCooldown = 3f;

    [Header("通用伤害设置")]
    public int baseDamage = 1;
    public float knockback = 0f;
    public ElementType element = ElementType.Physical;
    public bool canApplyElementStatus = false;

    public float skillMultiplier = 1f;
    public float damageBonus = 0f;
    public float reactionMultiplier = 1f;
    public bool canCrit = true;

    [Header("Slash 专属设置")]
    public float slashSphereRadius = 1.5f;
    public int slashSampleCount = 12;

    [Header("Fire 专属设置")]
    public float fireDamageRadius = 3f;

    public bool applyBurning = false;
    public float burningDuration = 5f;
    public float burningTickInterval = 1f;
    public int burningTickDamage = 1;

    public bool applyInfusionToPlayer = false;
    public float infusionDuration = 8f;
    public float infusionDamageMultiplier = 1.5f;

    public bool infusionApplyBurningOnHit = true;
    public float infusionBurningDuration = 5f;
    public float infusionBurningTickInterval = 1f;
    public int infusionBurningTickDamage = 1;

    [Header("Water 专属设置")]
    public float waterAuraDuration = 8f;

    [Header("Water 水弹生成")]
    public bool waterUsePool = true;

    [Tooltip("使用对象池时，填写 Resources 路径，例如 WaterProjectile 或 Projectiles/WaterProjectile")]
    public string waterProjectilePoolName = "WaterProjectile";

    [Tooltip("不使用对象池时才会用这个 Prefab")]
    public GameObject waterProjectilePrefab;

    public float waterShootInterval = 0.6f;
    public float waterSearchRange = 20f;

    [Header("Water 水弹伤害")]
    public int waterProjectileDamage = 8;
    public float waterProjectileKnockback = 1f;
    public float wetDuration = 5f;

    [Header("Water 水弹弹道")]
    public bool waterUseArcHoming = true;
    public float waterProjectileSpeed = 15f;
    public float waterProjectileLifeTime = 5f;
    public float waterArcHeight = 2f;
    public float waterRotateSpeed = 12f;
    public float waterReachDistance = 0.45f;
    public Vector3 waterTargetOffset = new Vector3(0f, 1f, 0f);

    [Header("Water 水弹碰撞")]
    public LayerMask waterProjectileHitLayer = ~0;
    public bool waterIgnoreAttackerCollision = true;
    public bool waterDestroyOnNonHitLayerCollision = false;

    [Header("Water 弹幕设置")]
    public bool waterUseBarrage = true;
    public int waterProjectileCountPerBurst = 5;
    public float waterBarrageAngle = 45f;
    public float waterBarrageSpawnRadius = 0.35f;
    public float waterBarrageVerticalStep = 0.12f;
    public bool waterDistributeTargets = true;

    [Header("Water 弹幕随机感")]
    public bool waterUseRandomBarrage = true;

    public float waterBurstDelayMin = 0.02f;
    public float waterBurstDelayMax = 0.12f;

    public float waterRandomSpawnRadius = 0.45f;
    public float waterRandomTargetOffsetRadius = 0.8f;

    public float waterArcSideOffsetMin = -1.2f;
    public float waterArcSideOffsetMax = 1.2f;

    public float waterArcHeightRandomMin = -0.5f;
    public float waterArcHeightRandomMax = 0.8f;

    public float waterSpeedRandomMin = -2f;
    public float waterSpeedRandomMax = 2f;

    [Header("Bridge 专属设置")]
    public LayerMask bridgeTriggerLayer;
    public float bridgeCastRadius = 0.6f;
    public float bridgeActiveDuration = 8f;
}