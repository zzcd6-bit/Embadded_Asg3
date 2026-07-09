using System.Collections.Generic;
using UnityEngine;
using PDollarGestureRecognizer;

public class BrushGestureRecognizer : MonoBehaviour
{
    [Header("Recognition")]
    public float minScore = 0.65f;

    [Header("Runtime Templates")]
    public bool useRuntimeSlashTemplate = true;
    public bool useRuntimeFireTemplate = true;
    public bool useRuntimeBridgeTemplate = true;

    [Header("Brush Mode")]
    public bool exitBrushModeOnRecognized = true;

    private readonly List<Gesture> trainingSet = new List<Gesture>();

    private void Awake()
    {
        if (useRuntimeSlashTemplate)
        {
            AddRuntimeSlashTemplate();
        }

        if (useRuntimeFireTemplate)
        {
            AddRuntimeFireTemplate();
        }

        if (useRuntimeBridgeTemplate)
        {
            AddRuntimeBridgeTemplate();
        }

        Debug.Log($"[BrushGestureRecognizer] Training templates loaded: {trainingSet.Count}");
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

    private BrushSkillType GetSkillTypeFromGestureName(string gestureName)
    {
        switch (gestureName)
        {
            case "Slash":
                return BrushSkillType.Slash;

            case "Fire":
                return BrushSkillType.Fire;

            case "Bridge":
                return BrushSkillType.Bridge;

            default:
                return BrushSkillType.None;
        }
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

        BrushSkillType skillType = GetSkillTypeFromGestureName(result.GestureClass);

        Debug.Log(
            $"Gesture Result: {result.GestureClass}, Skill: {skillType}, Score: {result.Score}"
        );

        if (result.Score < minScore)
            return;

        BrushGestureResult brushResult = new BrushGestureResult(
            result.GestureClass,
            skillType,
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

    private void AddRuntimeFireTemplate()
    {
        List<Point> firePoints = new List<Point>();

        // Fire 符号：一笔画火焰形状
        // 大概形状：
        //   /\  /\
        //  /  \/  \
        // /        \
        AddLine(firePoints, new Vector2(0f, 100f), new Vector2(40f, 0f), 8);
        AddLine(firePoints, new Vector2(40f, 0f), new Vector2(70f, 70f), 8);
        AddLine(firePoints, new Vector2(70f, 70f), new Vector2(100f, 10f), 8);
        AddLine(firePoints, new Vector2(100f, 10f), new Vector2(140f, 100f), 8);

        Gesture fireGesture = new Gesture(
            firePoints.ToArray(),
            "Fire"
        );

        trainingSet.Add(fireGesture);
    }


    private void AddRuntimeBridgeTemplate()
    {
        List<Point> bridgePoints = new List<Point>();

        // Bridge 符号：一笔画拱桥形状
        // 大概形状：
        //   __
        // /    \
        for (int i = 0; i < 32; i++)
        {
            float t = i / 31f;

            float x = Mathf.Lerp(0f, 160f, t);

            // 用 sin 做一个拱形
            float y = Mathf.Sin(t * Mathf.PI) * -80f;

            bridgePoints.Add(new Point(x, y, 0));
        }

        Gesture bridgeGesture = new Gesture(
            bridgePoints.ToArray(),
            "Bridge"
        );

        trainingSet.Add(bridgeGesture);
    }

    private void AddLine(List<Point> points, Vector2 from, Vector2 to, int count)
    {
        if (points == null)
            return;

        count = Mathf.Max(2, count);

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            Vector2 p = Vector2.Lerp(from, to, t);

            points.Add(new Point(p.x, p.y, 0));
        }
    }
}