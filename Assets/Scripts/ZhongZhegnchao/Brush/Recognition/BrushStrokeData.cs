using System.Collections.Generic;
using UnityEngine;
using PDollarGestureRecognizer;

public class BrushStrokeData
{
    public List<Vector2> screenPoints;
    public List<Vector3> worldPoints;

    public List<Point> pdollarPoints;

    public BrushStrokeData(
        List<Vector2> screenPoints,
        List<Vector3> worldPoints,
        List<Point> pdollarPoints
    )
    {
        this.screenPoints = new List<Vector2>(screenPoints);
        this.worldPoints = new List<Vector3>(worldPoints);
        this.pdollarPoints = new List<Point>(pdollarPoints);
    }
}