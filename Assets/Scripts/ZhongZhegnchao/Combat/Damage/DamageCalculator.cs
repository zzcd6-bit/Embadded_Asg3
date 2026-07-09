using UnityEngine;

public static class DamageCalculator
{
    private const float DefenseConstant = 100f;

    public static DamageInfo Calculate(DamageInfo damageInfo)
    {
        CharacterCombatStats attackerStats = null;
        CharacterCombatStats targetStats = null;

        if (damageInfo.attacker != null)
        {
            attackerStats = damageInfo.attacker.GetComponent<CharacterCombatStats>();

            if (attackerStats == null)
            {
                attackerStats = damageInfo.attacker.GetComponentInParent<CharacterCombatStats>();
            }

            if (attackerStats == null)
            {
                attackerStats = damageInfo.attacker.GetComponentInChildren<CharacterCombatStats>();
            }
        }

        if (damageInfo.target != null)
        {
            targetStats = damageInfo.target.GetComponent<CharacterCombatStats>();

            if (targetStats == null)
            {
                targetStats = damageInfo.target.GetComponentInParent<CharacterCombatStats>();
            }

            if (targetStats == null)
            {
                targetStats = damageInfo.target.GetComponentInChildren<CharacterCombatStats>();
            }
        }

        int baseDamage = Mathf.Max(0, damageInfo.damage);

        float attackPower = attackerStats != null
            ? Mathf.Max(0, attackerStats.attackPower)
            : 0f;

        float skillMultiplier = Mathf.Max(0f, damageInfo.skillMultiplier);

        float rawDamage = baseDamage + attackPower * skillMultiplier;

        float generalBonus = 0f;

        if (attackerStats != null)
        {
            generalBonus += attackerStats.damageBonus;
            generalBonus += attackerStats.GetElementDamageBonus(damageInfo.element);
        }

        generalBonus += damageInfo.damageBonus;

        float bonusMultiplier = 1f + generalBonus;

        float critMultiplier = 1f;

        if (damageInfo.canCrit && attackerStats != null)
        {
            bool isCritical = Random.value <= attackerStats.critRate;

            damageInfo.isCritical = isCritical;

            if (isCritical)
            {
                critMultiplier = Mathf.Max(1f, attackerStats.critDamage);
            }
        }

        float defenseMultiplier = 1f;

        if (targetStats != null)
        {
            float defense = Mathf.Max(0f, targetStats.defense);
            defenseMultiplier = DefenseConstant / (DefenseConstant + defense);
        }

        float resistanceMultiplier = 1f;

        if (targetStats != null)
        {
            float resistance = targetStats.GetElementResistance(damageInfo.element);
            resistanceMultiplier = Mathf.Clamp(1f - resistance, 0.1f, 2f);
        }

        float reactionMultiplier = damageInfo.reactionMultiplier > 0f
            ? damageInfo.reactionMultiplier
            : 1f;

        float finalDamageFloat =
            rawDamage *
            bonusMultiplier *
            critMultiplier *
            defenseMultiplier *
            resistanceMultiplier *
            reactionMultiplier;

        damageInfo.finalDamage = Mathf.Max(
            1,
            Mathf.RoundToInt(finalDamageFloat)
        );

        return damageInfo;
    }
}