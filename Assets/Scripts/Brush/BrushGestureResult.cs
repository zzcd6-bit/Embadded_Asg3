public class BrushGestureResult
{
    public string gestureName;
    public float score;
    public BrushStrokeData strokeData;

    public BrushGestureResult(string gestureName, float score, BrushStrokeData strokeData)
    {
        this.gestureName = gestureName;
        this.score = score;
        this.strokeData = strokeData;
    }
}