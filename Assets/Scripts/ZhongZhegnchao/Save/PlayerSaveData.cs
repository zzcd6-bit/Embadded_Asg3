using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerSaveData
{
    public Vector3 position;
    public Vector3 eulerAngles;

    public int currentHp;
    public int maxHp;

    public List<string> unlockedBrushSkills = new List<string>();
}