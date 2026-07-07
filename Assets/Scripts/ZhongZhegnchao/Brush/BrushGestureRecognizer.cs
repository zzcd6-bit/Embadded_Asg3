using System.Collections.Generic;
using UnityEngine;
using PDollarGestureRecognizer;

public class BrushGestureRecognizer : MonoBehaviour
{
    [Header("Recognition")]
    public float minScore = 0.65f;
    public bool useRuntimeSlashTemplate = true;

    [Header("Brush Mode")]
    public bool exitBrushModeOnRecognized = true;

    private readonly List<Gesture> trainingSet = new List<Gesture>();

    private void Awake()
    {
        if (useRuntimeSlashTemplate)
        {
            AddRuntimeSlashTemplate();
        }

        // 如果你之后有 XML 手势模板，也可以继续用 Resources 加载
        // TextAsset[] gesturesXml = Resources.LoadAll<TextAsset>("GestureSet/10-stylus-MEDIUM/");
        // foreach (TextAsset gestureXml in gesturesXml)
        // {
        //     trainingSet.Add(GestureIO.ReadGestureFromXML(gestureXml.text));
        // }
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener<BrushStrokeData>(
            E_EventType.E_Brush_StrokeFinished,
            OnStrokeFinished
        );
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<BrushStrokeData>(
            E_EventType.E_Brush_StrokeFinished,
            OnStrokeFinished
        );
    }

    private void OnStrokeFinished(BrushStrokeData strokeData)
    {
        if (strokeData == null || strokeData.screenPoints == null)
            return;

        if (strokeData.screenPoints.Count < 6)
            return;

        if (trainingSet.Count == 0)
        {
            Debug.LogWarning("No gesture templates found.");
            return;
        }

        Point[] candidatePoints = ConvertToPDollarPoints(strokeData.screenPoints);

        Gesture candidate = new Gesture(candidatePoints);
        Result result = PointCloudRecognizer.Classify(candidate, trainingSet.ToArray());

        Debug.Log($"Gesture Result: {result.GestureClass}, Score: {result.Score}");

        if (result.Score < minScore)
            return;

        BrushGestureResult brushResult = new BrushGestureResult(
            result.GestureClass,
            result.Score,
            strokeData
        );

        // 先退出绘画模式
        if (exitBrushModeOnRecognized)
        {
            EventCenter.Instance.EventTrigger(E_EventType.E_Brush_RequestExit);
        }

        // 再执行对应手势效果
        EventCenter.Instance.EventTrigger<BrushGestureResult>(
            E_EventType.E_Brush_GestureRecognized,
            brushResult
        );
    }

    private Point[] ConvertToPDollarPoints(List<Vector2> screenPoints)
    {
        Point[] points = new Point[screenPoints.Count];

        for (int i = 0; i < screenPoints.Count; i++)
        {
            Vector2 p = screenPoints[i];

            // 和 Demo 一样，Y 取反
            points[i] = new Point(p.x, -p.y, 0);
        }

        return points;
    }

    private void AddRuntimeSlashTemplate()
    {
        List<Point> slashPoints = new List<Point>();

        // 横线模板，从左到右
        for (int i = 0; i < 32; i++)
        {
            float x = i * 10f;
            float y = 0f;
            slashPoints.Add(new Point(x, y, 0));
        }

        Gesture slashGesture = new Gesture(
            slashPoints.ToArray(),
            "Slash"
        );

        trainingSet.Add(slashGesture);
    }
}