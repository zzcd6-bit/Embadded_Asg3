using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PDollarGestureRecognizer;

public class BrushGestureTemplateRecorder : MonoBehaviour
{
    [Header("录制设置")]
    public BrushGestureRecognizer recognizer;
    public string templateName = "Fire";

    [Header("按键")]
    public KeyCode saveNextStrokeKey = KeyCode.F5;
    public KeyCode reloadTemplatesKey = KeyCode.F6;

    [Header("保存设置")]
    public string customGestureFolderName = "GestureTemplates";
    public int minPointCount = 6;
    public bool reloadRecognizerAfterSave = true;

    [Header("录制容错")]
    public bool keepWaitingIfInvalidStroke = true;

    [Header("Debug")]
    public bool debugLog = true;

    private bool saveNextStroke;

    private void Awake()
    {
        if (recognizer == null)
        {
            recognizer = GetComponent<BrushGestureRecognizer>();
        }

        if (recognizer == null)
        {
            recognizer = GetComponentInChildren<BrushGestureRecognizer>();
        }
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

    private void Update()
    {
        if (Input.GetKeyDown(saveNextStrokeKey))
        {
            saveNextStroke = true;

            if (debugLog)
            {
                Debug.Log(
                    $"[BrushGestureTemplateRecorder] Recording armed. Next valid stroke will be saved as: {templateName}"
                );
            }
        }

        if (Input.GetKeyDown(reloadTemplatesKey))
        {
            ReloadTemplates();
        }
    }

    private void OnStrokeFinished(BrushStrokeData strokeData)
    {
        if (!saveNextStroke)
            return;

        if (strokeData == null || strokeData.screenPoints == null)
        {
            Debug.LogWarning("[BrushGestureTemplateRecorder] Stroke data is null.");

            if (!keepWaitingIfInvalidStroke)
            {
                saveNextStroke = false;
            }

            return;
        }

        int pointCount = strokeData.screenPoints.Count;

        if (debugLog)
        {
            Debug.Log($"[BrushGestureTemplateRecorder] Stroke finished. Point Count = {pointCount}");
        }

        if (pointCount < minPointCount)
        {
            Debug.LogWarning(
                $"[BrushGestureTemplateRecorder] Not enough points. Need {minPointCount}, got {pointCount}."
            );

            if (!keepWaitingIfInvalidStroke)
            {
                saveNextStroke = false;
            }

            return;
        }

        List<Vector2> copiedPoints = new List<Vector2>(strokeData.screenPoints);

        bool saved = SavePointsAsTemplate(copiedPoints);

        if (saved)
        {
            saveNextStroke = false;
        }
    }

    private bool SavePointsAsTemplate(List<Vector2> screenPoints)
    {
        if (screenPoints == null)
        {
            Debug.LogWarning("[BrushGestureTemplateRecorder] Screen points are null.");
            return false;
        }

        if (screenPoints.Count < minPointCount)
        {
            Debug.LogWarning("[BrushGestureTemplateRecorder] Not enough points to save template.");
            return false;
        }

        if (string.IsNullOrEmpty(templateName))
        {
            Debug.LogWarning("[BrushGestureTemplateRecorder] Template name is empty.");
            return false;
        }

        string folderPath = Path.Combine(
            Application.persistentDataPath,
            customGestureFolderName
        );

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string safeName = templateName.Trim();
        string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

        string fileName = $"{safeName}_{timeStamp}.xml";
        string filePath = Path.Combine(folderPath, fileName);

        Point[] points = ConvertToPDollarPoints(screenPoints);

        try
        {
            GestureIO.WriteGesture(points, safeName, filePath);

            if (debugLog)
            {
                Debug.Log(
                    $"[BrushGestureTemplateRecorder] Gesture XML saved: {filePath}"
                );
            }

            if (reloadRecognizerAfterSave)
            {
                ReloadTemplates();
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[BrushGestureTemplateRecorder] Failed to save gesture XML.\n{exception.Message}"
            );

            return false;
        }
    }

    private void ReloadTemplates()
    {
        if (recognizer == null)
        {
            Debug.LogWarning("[BrushGestureTemplateRecorder] Recognizer is missing.");
            return;
        }

        recognizer.ReloadTemplates();

        if (debugLog)
        {
            Debug.Log("[BrushGestureTemplateRecorder] Templates reloaded.");
        }
    }

    private Point[] ConvertToPDollarPoints(List<Vector2> screenPoints)
    {
        Point[] points = new Point[screenPoints.Count];

        for (int i = 0; i < screenPoints.Count; i++)
        {
            Vector2 p = screenPoints[i];

            points[i] = new Point(
                p.x,
                -p.y,
                0
            );
        }

        return points;
    }
}