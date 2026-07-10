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
                    $"[BrushGestureTemplateRecorder] Next stroke will be saved as template: {templateName}"
                );
            }
        }

        if (Input.GetKeyDown(reloadTemplatesKey))
        {
            if (recognizer != null)
            {
                recognizer.ReloadTemplates();

                if (debugLog)
                {
                    Debug.Log("[BrushGestureTemplateRecorder] Templates reloaded.");
                }
            }
        }
    }

    private void OnStrokeFinished(BrushStrokeData strokeData)
    {
        if (!saveNextStroke)
            return;

        saveNextStroke = false;

        SaveStrokeAsTemplate(strokeData);
    }

    private void SaveStrokeAsTemplate(BrushStrokeData strokeData)
    {
        if (strokeData == null || strokeData.screenPoints == null)
        {
            Debug.LogWarning("[BrushGestureTemplateRecorder] Stroke data is null.");
            return;
        }

        if (strokeData.screenPoints.Count < minPointCount)
        {
            Debug.LogWarning("[BrushGestureTemplateRecorder] Not enough points to save template.");
            return;
        }

        if (string.IsNullOrEmpty(templateName))
        {
            Debug.LogWarning("[BrushGestureTemplateRecorder] Template name is empty.");
            return;
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

        Point[] points = ConvertToPDollarPoints(strokeData.screenPoints);

        try
        {
            GestureIO.WriteGesture(points, safeName, filePath);

            if (debugLog)
            {
                Debug.Log(
                    $"[BrushGestureTemplateRecorder] Gesture XML saved: {filePath}"
                );
            }

            if (reloadRecognizerAfterSave && recognizer != null)
            {
                recognizer.ReloadTemplates();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[BrushGestureTemplateRecorder] Failed to save gesture XML.\n{exception.Message}"
            );
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