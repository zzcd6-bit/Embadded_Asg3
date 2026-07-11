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

    [Header("Bridge 专属设置")]
    public LayerMask bridgeTriggerLayer;
    public float bridgeCastRadius = 0.6f;
    public float bridgeActiveDuration = 8f;
}