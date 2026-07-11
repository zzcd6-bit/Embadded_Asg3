using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PDollarGestureRecognizer;

public class BrushLineDrawer : MonoBehaviour
{
    [Header("References")]
    public Camera brushLineCamera;
    public Transform drawPlane;
    public LineRenderer lineRenderer;

    [Header("Recognition Point Settings")]
    public int minRecognitionPointCount = 6;

    [Tooltip("0 means record every frame, same as PDollar demo.")]
    public float minScreenPointDistance = 0f;

    [Header("Line Point Settings")]
    public float minPointDistance = 0.02f;
    public bool limitPointCount = false;
    public int maxPointCount = 2048;

    [Header("Clear Settings")]
    public float clearDelay = 0.8f;
    public bool clearWhenExitBrushMode = true;

    [Header("Ink Brush Style")]
    public Material[] brushMaterials;
    public Vector2 widthRange = new Vector2(0.06f, 0.12f);
    public float widthNoiseAmount = 0.35f;
    public int widthCurveKeyCount = 8;

    [Header("Debug")]
    public bool debugLog = true;

    private bool isBrushMode;
    private bool isDrawing;

    private readonly List<Vector2> screenPoints = new List<Vector2>();
    private readonly List<Vector3> worldPoints = new List<Vector3>();
    private readonly List<Point> pdollarPoints = new List<Point>();

    private Coroutine clearRoutine;

    private void Awake()
    {
        if (lineRenderer != null)
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.textureMode = LineTextureMode.Tile;
        }
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener<bool>(
            E_EventType.E_Brush_ModeChanged,
            OnBrushModeChanged
        );

        EventCenter.Instance.AddEventListener(
            E_EventType.E_Brush_DrawStart,
            OnDrawStart
        );

        EventCenter.Instance.AddEventListener(
            E_EventType.E_Brush_DrawEnd,
            OnDrawEnd
        );
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<bool>(
            E_EventType.E_Brush_ModeChanged,
            OnBrushModeChanged
        );

        EventCenter.Instance.RemoveEventListener(
            E_EventType.E_Brush_DrawStart,
            OnDrawStart
        );

        EventCenter.Instance.RemoveEventListener(
            E_EventType.E_Brush_DrawEnd,
            OnDrawEnd
        );
    }

    private void Update()
    {
        if (!isBrushMode || !isDrawing)
            return;

        AddRecognitionPointFromMouse();
        AddVisualPointFromMouse();
    }

    private void OnBrushModeChanged(bool state)
    {
        isBrushMode = state;

        if (!state)
        {
            isDrawing = false;

            if (clearWhenExitBrushMode)
            {
                ClearLine();
            }
        }
    }

    private void OnDrawStart()
    {
        if (!isBrushMode)
            return;

        isDrawing = true;

        if (clearRoutine != null)
        {
            StopCoroutine(clearRoutine);
            clearRoutine = null;
        }

        ClearLine();

        ApplyRandomInkStyle();

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }

        AddRecognitionPointFromMouse(true);
        AddVisualPointFromMouse(true);
    }

    private void OnDrawEnd()
    {
        if (!isBrushMode || !isDrawing)
            return;

        isDrawing = false;

        if (debugLog)
        {
            Debug.Log(
                $"[BrushLineDrawer] Draw ended. ScreenPoints={screenPoints.Count}, " +
                $"PDollarPoints={pdollarPoints.Count}, WorldPoints={worldPoints.Count}",
                this
            );
        }

        if (pdollarPoints.Count >= minRecognitionPointCount)
        {
            BrushStrokeData data = new BrushStrokeData(
                screenPoints,
                worldPoints,
                pdollarPoints
            );

            EventCenter.Instance.EventTrigger<BrushStrokeData>(
                E_EventType.E_Brush_StrokeFinished,
                data
            );
        }
        else
        {
            if (debugLog)
            {
                Debug.LogWarning(
                    $"[BrushLineDrawer] Not enough PDollar points. Count={pdollarPoints.Count}",
                    this
                );
            }
        }

        if (clearDelay >= 0)
        {
            clearRoutine = StartCoroutine(ClearLineAfterDelay());
        }
    }

    private void AddRecognitionPointFromMouse(bool forceAdd = false)
    {
        Vector2 screenPoint = Input.mousePosition;

        if (!forceAdd && screenPoints.Count > 0 && minScreenPointDistance > 0f)
        {
            float distance = Vector2.Distance(
                screenPoints[screenPoints.Count - 1],
                screenPoint
            );

            if (distance < minScreenPointDistance)
                return;
        }

        if (limitPointCount && screenPoints.Count >= maxPointCount)
            return;

        screenPoints.Add(screenPoint);

        // 这里直接按 PDollar Demo 的方式记录
        pdollarPoints.Add(
            new Point(
                screenPoint.x,
                -screenPoint.y,
                0
            )
        );
    }

    private void AddVisualPointFromMouse(bool forceAdd = false)
    {
        if (lineRenderer == null)
            return;

        if (!TryGetMouseWorldPoint(out Vector3 worldPoint))
            return;

        if (!forceAdd && worldPoints.Count > 0)
        {
            float distance = Vector3.Distance(
                worldPoints[worldPoints.Count - 1],
                worldPoint
            );

            if (distance < minPointDistance)
                return;
        }

        if (limitPointCount && worldPoints.Count >= maxPointCount)
            return;

        worldPoints.Add(worldPoint);

        lineRenderer.positionCount = worldPoints.Count;
        lineRenderer.SetPosition(worldPoints.Count - 1, worldPoint);
    }

    private void ApplyRandomInkStyle()
    {
        if (lineRenderer == null)
            return;

        if (brushMaterials != null && brushMaterials.Length > 0)
        {
            int index = Random.Range(0, brushMaterials.Length);
            lineRenderer.material = brushMaterials[index];
        }

        float width = Random.Range(widthRange.x, widthRange.y);
        lineRenderer.widthMultiplier = width;

        lineRenderer.widthCurve = GenerateRandomWidthCurve();
    }

    private AnimationCurve GenerateRandomWidthCurve()
    {
        int keyCount = Mathf.Max(3, widthCurveKeyCount);
        Keyframe[] keys = new Keyframe[keyCount];

        for (int i = 0; i < keyCount; i++)
        {
            float time = i / (float)(keyCount - 1);

            float noise = Random.Range(
                1f - widthNoiseAmount,
                1f + widthNoiseAmount
            );

            if (i == 0 || i == keyCount - 1)
            {
                noise *= 0.35f;
            }

            keys[i] = new Keyframe(
                time,
                Mathf.Clamp(noise, 0.15f, 1.5f)
            );
        }

        return new AnimationCurve(keys);
    }

    private bool TryGetMouseWorldPoint(out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;

        if (brushLineCamera == null || drawPlane == null)
            return false;

        Ray ray = brushLineCamera.ScreenPointToRay(Input.mousePosition);

        Plane plane = new Plane(
            -brushLineCamera.transform.forward,
            drawPlane.position
        );

        if (plane.Raycast(ray, out float enter))
        {
            worldPoint = ray.GetPoint(enter);
            return true;
        }

        return false;
    }

    private IEnumerator ClearLineAfterDelay()
    {
        yield return new WaitForSecondsRealtime(clearDelay);
        ClearLine();
    }

    private void ClearLine()
    {
        screenPoints.Clear();
        worldPoints.Clear();
        pdollarPoints.Clear();

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }
    }
}