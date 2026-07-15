using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerSaveData
{
    public Vector3 position;
    public Vector3 eulerAngles;

    public int characterLevel = 1;
    public int currentExperience = 0;

    public int currentHp;
    public int maxHp;

    public int currentInk;
    public int maxInk;

    public List<string> unlockedBrushSkills = new List<string>();
}