public class BrushGestureResult
{
    public string gestureName;
    public BrushSkillType skillType;
    public float score;
    public BrushStrokeData strokeData;

    public BrushGestureResult(
        string gestureName,
        BrushSkillType skillType,
        float score,
        BrushStrokeData strokeData
    )
    {
        this.gestureName = gestureName;
        this.skillType = skillType;
        this.score = score;
        this.strokeData = strokeData;
    }
}