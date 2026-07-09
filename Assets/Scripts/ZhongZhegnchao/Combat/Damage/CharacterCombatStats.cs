using UnityEngine;

public class CharacterCombatStats : MonoBehaviour
{
    [Header("Basic Stats")]
    public int attackPower = 10;
    public int defense = 0;

    [Header("General Damage Bonus")]
    [Tooltip("0.2 means +20% damage.")]
    public float damageBonus = 0f;

    [Header("Critical")]
    [Tooltip("0.1 means 10% crit chance.")]
    public float critRate = 0.1f;

    [Tooltip("1.5 means 150% damage when critical.")]
    public float critDamage = 1.5f;

    [Header("Element Damage Bonus")]
    public float physicalDamageBonus = 0f;
    public float fireDamageBonus = 0f;
    public float waterDamageBonus = 0f;
    public float iceDamageBonus = 0f;
    public float thunderDamageBonus = 0f;
    public float earthDamageBonus = 0f;

    [Header("Element Resistance")]
    [Tooltip("0.2 means reduce this element damage by 20%.")]
    public float physicalResistance = 0f;
    public float fireResistance = 0f;
    public float waterResistance = 0f;
    public float iceResistance = 0f;
    public float thunderResistance = 0f;
    public float earthResistance = 0f;

    public float GetElementDamageBonus(ElementType element)
    {
        switch (element)
        {
            case ElementType.Physical:
                return physicalDamageBonus;

            case ElementType.Fire:
                return fireDamageBonus;

            case ElementType.Water:
                return waterDamageBonus;

            case ElementType.Ice:
                return iceDamageBonus;

            case ElementType.Thunder:
                return thunderDamageBonus;

            case ElementType.Earth:
                return earthDamageBonus;

            default:
                return 0f;
        }
    }

    public float GetElementResistance(ElementType element)
    {
        switch (element)
        {
            case ElementType.Physical:
                return physicalResistance;

            case ElementType.Fire:
                return fireResistance;

            case ElementType.Water:
                return waterResistance;

            case ElementType.Ice:
                return iceResistance;

            case ElementType.Thunder:
                return thunderResistance;

            case ElementType.Earth:
                return earthResistance;

            default:
                return 0f;
        }
    }
}