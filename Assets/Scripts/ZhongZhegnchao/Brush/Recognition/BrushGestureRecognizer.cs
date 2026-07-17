using System.Collections.Generic;
using UnityEngine;
using PDollarGestureRecognizer;
using System.IO;

public class BrushGestureRecognizer : MonoBehaviour
{
    private class GestureDebugMatch
    {
        public string gestureName;
        public float score;
    }

    [Header("Recognition")]
    public float minScore = 0.65f;
    public int minPointCount = 6;

    [Header("XML Templates")]
    public bool loadXmlTemplatesFromResources = true;
    public string resourcesGestureFolder = "GestureTemplates";

    [Header("Custom XML Templates")]
    public bool loadCustomXmlTemplates = true;
    public string customGestureFolderName = "GestureTemplates";

    [Header("Runtime Fallback Templates")]
    public bool useRuntimeTemplatesIfXmlEmpty = true;
    public bool useRuntimeSlashTemplate = true;
    public bool useRuntimeFireTemplate = true;
    public bool useRuntimeBridgeTemplate = true;

    [Header("Brush Mode")]
    public bool exitBrushModeOnRecognized = true;

    [Header("Recognition Success Audio - Resources/Audio")]
    public bool playRecognitionSuccessSound = true;
    public bool recognitionSoundSync = true;
    public string slashSuccessSound = "Brush/Recognition/SlashSuccess";
    public string fireSuccessSound = "Brush/Recognition/FireSuccess";
    public string waterSuccessSound = "Brush/Recognition/WaterSuccess";
    public string woodSuccessSound = "Brush/Recognition/WoodSuccess";
    public string windSuccessSound = "Brush/Recognition/WindSuccess";

    [Header("Skill Unlock")]
    [SerializeField]
    private PlayerBrushSkillInventory skillInventory;

    [Header("Debug")]
    public bool debugPointInfo = true;
    public bool debugTemplateInfo = true;
    public bool debugTopMatches = true;
    public int debugTopMatchCount = 5;



    private readonly List<Gesture> trainingSet = new List<Gesture>();

    private void Awake()
    {
        InitializeUI();
        ResolveSkillInventory();
        ReloadTemplates();
    }

    private void ResolveSkillInventory()
    {
        if (skillInventory != null)
            return;

        skillInventory =
            GetComponent<PlayerBrushSkillInventory>();

        if (skillInventory == null)
        {
            skillInventory =
                GetComponentInParent<
                    PlayerBrushSkillInventory>();
        }

        if (skillInventory == null)
        {
            skillInventory =
                GetComponentInChildren<
                    PlayerBrushSkillInventory>(true);
        }

        if (skillInventory == null)
        {
            Debug.LogWarning(
                "[BrushGestureRecognizer] " +
                "PlayerBrushSkillInventory not found.",
                this
            );
        }
    }

    private void InitializeUI()
    {
        UIMgr uiMgr = UIMgr.Instance;

        if (uiMgr == null)
        {
            Debug.LogError(
                "[BrushGestureRecognizer] UIMgr initialize failed.",
                this
            );
        }
    }

    public void ReloadTemplates()
    {
        trainingSet.Clear();

        if (loadXmlTemplatesFromResources)
        {
            LoadXmlTemplatesFromResources();
        }

        if (loadCustomXmlTemplates)
        {
            LoadCustomXmlTemplatesFromPersistentPath();
        }

        if (useRuntimeTemplatesIfXmlEmpty && trainingSet.Count == 0)
        {
            LoadRuntimeFallbackTemplates();
        }

        Debug.Log($"[BrushGestureRecognizer] Training templates loaded: {trainingSet.Count}");
    }

    private void LoadXmlTemplatesFromResources()
    {
        TextAsset[] gestureXmls = Resources.LoadAll<TextAsset>(resourcesGestureFolder);

        if (gestureXmls == null || gestureXmls.Length == 0)
        {
            Debug.LogWarning(
                $"[BrushGestureRecognizer] No XML templates found in Resources/{resourcesGestureFolder}."
            );

            return;
        }

        for (int i = 0; i < gestureXmls.Length; i++)
        {
            TextAsset gestureXml = gestureXmls[i];

            if (gestureXml == null)
                continue;

            TryAddGestureFromXmlText(gestureXml.text, gestureXml.name);
        }
    }

    private void LoadCustomXmlTemplatesFromPersistentPath()
    {
        string folderPath = Path.Combine(
            Application.persistentDataPath,
            customGestureFolderName
        );

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);

            Debug.Log(
                $"[BrushGestureRecognizer] Custom gesture folder created: {folderPath}"
            );

            return;
        }

        string[] filePaths = Directory.GetFiles(folderPath, "*.xml");

        for (int i = 0; i < filePaths.Length; i++)
        {
            string filePath = filePaths[i];

            try
            {
                Gesture gesture = GestureIO.ReadGestureFromFile(filePath);

                if (gesture != null)
                {
                    trainingSet.Add(gesture);

                    if (debugTemplateInfo)
                    {
                        Debug.Log(
                            $"[BrushGestureRecognizer] Loaded custom gesture XML: {Path.GetFileName(filePath)}, Name={gesture.Name}"
                        );
                    }
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning(
                    $"[BrushGestureRecognizer] Failed to load custom gesture XML: {filePath}\n{exception.Message}"
                );
            }
        }
    }

    private void TryAddGestureFromXmlText(string xmlText, string sourceName)
    {
        if (string.IsNullOrEmpty(xmlText))
            return;

        try
        {
            Gesture gesture = GestureIO.ReadGestureFromXML(xmlText);

            if (gesture == null)
            {
                Debug.LogWarning(
                    $"[BrushGestureRecognizer] XML gesture is null: {sourceName}"
                );

                return;
            }

            trainingSet.Add(gesture);

            if (debugTemplateInfo)
            {
                Debug.Log(
                    $"[BrushGestureRecognizer] Loaded XML gesture: {gesture.Name} from {sourceName}"
                );
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(
                $"[BrushGestureRecognizer] Failed to load XML gesture: {sourceName}\n{exception.Message}"
            );
        }
    }

    private void LoadRuntimeFallbackTemplates()
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

        Debug.Log("[BrushGestureRecognizer] Runtime fallback templates loaded.");
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
        if (strokeData == null || strokeData.pdollarPoints == null)
            return;

        if (strokeData.pdollarPoints.Count < minPointCount)
        {
            if (debugPointInfo)
            {
                Debug.LogWarning(
                    $"[BrushGestureRecognizer] " +
                    $"Not enough PDollar points. " +
                    $"Count={strokeData.pdollarPoints.Count}"
                );
            }

            ShowRecognitionFailure();

            return;
        }

        if (trainingSet.Count == 0)
        {
            Debug.LogWarning("[BrushGestureRecognizer] No gesture templates found.");
            return;
        }

        if (debugPointInfo)
        {
            Point first = strokeData.pdollarPoints[0];
            Point last = strokeData.pdollarPoints[strokeData.pdollarPoints.Count - 1];

            Debug.Log(
                $"[BrushGestureRecognizer] PDollar point count={strokeData.pdollarPoints.Count}, " +
                $"First=({first.X}, {first.Y}), Last=({last.X}, {last.Y})"
            );
        }

        Point[] candidatePoints = strokeData.pdollarPoints.ToArray();

        Gesture candidate = new Gesture(candidatePoints);

        DebugTopGestureMatches(candidate);

        Result result = PointCloudRecognizer.Classify(
            candidate,
            trainingSet.ToArray()
        );

        BrushSkillType skillType = GetSkillTypeFromGestureName(result.GestureClass);

        Debug.Log(
            $"Gesture Result: {result.GestureClass}, Skill: {skillType}, Score: {result.Score}"
        );

        bool recognitionSucceeded =
    result.Score >= minScore &&
    skillType != BrushSkillType.None;

        if (!recognitionSucceeded)
        {
            ShowRecognitionFailure();

            return;
        }

        if (RequiresSkillUnlock(skillType) &&
    !HasUnlockedSkill(skillType))
        {
            if (debugPointInfo)
            {
                Debug.Log(
                    $"[BrushGestureRecognizer] " +
                    $"Recognized skill is not unlocked: " +
                    $"{skillType}",
                    this
                );
            }

            ShowRecognitionFailure();

            return;
        }

        ShowRecognitionSuccess(skillType);
        PlayRecognitionSuccessSound(skillType);

        BrushGestureResult brushResult =
            new BrushGestureResult(
                result.GestureClass,
                skillType,
                result.Score,
                strokeData
            );

        if (exitBrushModeOnRecognized)
        {
            EventCenter.Instance.EventTrigger(E_EventType.E_Brush_RequestExit);
        }

        EventCenter.Instance.EventTrigger<BrushGestureResult>(
            E_EventType.E_Brush_GestureRecognized,
            brushResult
        );
    }

    private void PlayRecognitionSuccessSound(
        BrushSkillType skillType
    )
    {
        if (!playRecognitionSuccessSound)
            return;

        string soundName = null;

        switch (skillType)
        {
            case BrushSkillType.Slash:
                soundName = slashSuccessSound;
                break;
            case BrushSkillType.Fire:
                soundName = fireSuccessSound;
                break;
            case BrushSkillType.Water:
                soundName = waterSuccessSound;
                break;
            case BrushSkillType.Wood:
                soundName = woodSuccessSound;
                break;
            case BrushSkillType.Wind:
                soundName = windSuccessSound;
                break;
        }

        if (string.IsNullOrEmpty(soundName))
            return;

        MusicMgr.Instance.PlaySound(
            soundName,
            false,
            recognitionSoundSync
        );
    }

    private void ShowRecognitionSuccess(
    BrushSkillType skillType
)
    {
        UIMgr.Instance.ShowPanel<
            BrushRecognitionResultPanel
        >(
            E_UILayer.Top,
            panel =>
            {
                panel.ShowSuccess(skillType);
            },
            true
        );
    }

    private void ShowRecognitionFailure()
    {
        UIMgr.Instance.ShowPanel<
            BrushRecognitionResultPanel
        >(
            E_UILayer.Top,
            panel =>
            {
                panel.ShowFailure();
            },
            true
        );
    }

    private bool HasUnlockedSkill(
    BrushSkillType skillType
)
    {
        if (skillType == BrushSkillType.None)
            return false;

        if (skillInventory == null)
        {
            ResolveSkillInventory();
        }

        if (skillInventory == null)
            return false;

        return skillInventory.HasBrushSkill(skillType);
    }

    private bool RequiresSkillUnlock(
    BrushSkillType skillType
)
    {
        switch (skillType)
        {
            case BrushSkillType.Slash:
            case BrushSkillType.Fire:
            case BrushSkillType.Water:
            case BrushSkillType.Wood:
            case BrushSkillType.Wind:
                return true;

            case BrushSkillType.Bridge:
                return false;

            default:
                return false;
        }
    }

    private BrushSkillType GetSkillTypeFromGestureName(string gestureName)
    {
        if (string.IsNullOrEmpty(gestureName))
            return BrushSkillType.None;

        string normalizedName = gestureName.Trim().ToLower();

        switch (normalizedName)
        {
            case "slash":
                return BrushSkillType.Slash;

            case "fire":
                return BrushSkillType.Fire;

            case "water":
                return BrushSkillType.Water;

            case "bridge":
                return BrushSkillType.Bridge;

            case "wood":
                return BrushSkillType.Wood;

            case "wind":
                return BrushSkillType.Wind;

            default:
                return BrushSkillType.None;
        }
    }

    private void AddRuntimeSlashTemplate()
    {
        List<Point> slashPoints = new List<Point>();

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

        for (int i = 0; i < 32; i++)
        {
            float t = i / 31f;

            float x = Mathf.Lerp(0f, 160f, t);
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

    private void DebugTopGestureMatches(Gesture candidate)
    {
        if (!debugTopMatches)
            return;

        if (candidate == null)
            return;

        if (trainingSet == null || trainingSet.Count == 0)
        {
            Debug.LogWarning("[BrushGestureRecognizer] Debug failed: trainingSet is empty.");
            return;
        }

        List<GestureDebugMatch> matches = new List<GestureDebugMatch>();

        for (int i = 0; i < trainingSet.Count; i++)
        {
            Gesture template = trainingSet[i];

            if (template == null)
                continue;

            Result singleResult = PointCloudRecognizer.Classify(
                candidate,
                new Gesture[] { template }
            );

            GestureDebugMatch match = new GestureDebugMatch
            {
                gestureName = template.Name,
                score = singleResult.Score
            };

            matches.Add(match);
        }

        matches.Sort(
            (a, b) => b.score.CompareTo(a.score)
        );

        int count = Mathf.Min(debugTopMatchCount, matches.Count);

        string message = "[BrushGestureRecognizer] Top Gesture Matches:\n";

        for (int i = 0; i < count; i++)
        {
            BrushSkillType skillType = GetSkillTypeFromGestureName(matches[i].gestureName);

            message +=
                $"{i + 1}. Name={matches[i].gestureName}, " +
                $"Skill={skillType}, " +
                $"Score={matches[i].score:F4}\n";
        }

        Debug.Log(message);
    }
}