using UnityEngine;

public class CharacterDamagedEventInfo
{
    public GameObject attacker;
    public GameObject target;

    public int damage;
    public Vector3 hitPoint;
    public Vector3 hitDirection;

    public bool isDead;
    public DamageInfo sourceDamageInfo;
}

public class CharacterDeadEventInfo
{
    public GameObject killer;
    public GameObject deadTarget;

    public Vector3 deathPosition;
    public DamageInfo sourceDamageInfo;
}

public class PerfectDodgeEventInfo
{
    public GameObject player;
    public GameObject attacker;

    public Vector3 playerPosition;
    public Vector3 attackerPosition;

    public float triggerTime;
}