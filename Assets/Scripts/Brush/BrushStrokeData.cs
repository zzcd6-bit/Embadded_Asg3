using System.Collections.Generic;
using UnityEngine;

public class BrushStrokeData
{
    public List<Vector2> screenPoints;
    public List<Vector3> worldPoints;

    public BrushStrokeData(List<Vector2> screenPoints, List<Vector3> worldPoints)
    {
        this.screenPoints = new List<Vector2>(screenPoints);
        this.worldPoints = new List<Vector3>(worldPoints);
    }
}