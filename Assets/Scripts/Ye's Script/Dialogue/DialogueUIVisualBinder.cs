using System.Collections;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

public class DialogueUIVisualBinder : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private StandardDialogueUI dialogueUI;
    [SerializeField] private StandardUISubtitlePanel subtitlePanel;
    [SerializeField] private AbstractTypewriterEffect typewriterEffect;

    [Header("Continue Button")]
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject continueButtonChosenImage;
    [SerializeField] private GameObject continueButtonNormalImage;

    [Header("Auto Play Switch")]
    [SerializeField] private Button autoPlaySwitch;
    [SerializeField] private GameObject autoPlaySwitchOnImage;
    [SerializeField] private GameObject autoPlaySwitchOffImage;
    [SerializeField] private float autoPlayDelay = 2f;
    [SerializeField] private bool resetAutoPlayOnConversationStart = true;
    [SerializeField] private bool forceManualContinueMode = true;

    [Header("Speaker Name")]
    [SerializeField] private GameObject speakerNamePanel;
    [SerializeField] private Text speakerNameText;
#if TMP_PRESENT
    [SerializeField] private TextMeshProUGUI subtitleBodyTextMeshPro;
#endif
    [SerializeField] private Text subtitleBodyText;
    [SerializeField] private string generatedSubtitleBodyName = "Subtitle Body Text";
    [SerializeField] private int generatedSubtitleFontSize = 24;
    [SerializeField] private Color generatedSubtitleColor = Color.white;
    [SerializeField] private bool createLegacyFallbackBodyTextIfMissing;
    [SerializeField] private string[] narrationSpeakerNames = { "Narrator", "\u65c1\u767d" };

    [Header("Sentence Skip")]
    [SerializeField] private string canSkipSentenceFieldName = "canSkipSentence";
    [SerializeField] private bool defaultCanSkipSentence = true;

    private StandardUIContinueButtonFastForward continueFastForward;
    private bool continuePointerInside;
    private bool continuePointerDown;
    private bool autoPlayEnabled;
    private bool autoPlayWaiting;
    private bool continueRequestInProgress;
    private Coroutine autoPlayCoroutine;
    private Coroutine continueRequestUnlockCoroutine;
    private string lastSpeakerName;

    private void Awake()
    {
        ForceDialogueSystemManualContinueMode();
        autoPlayEnabled = false;
        autoPlayWaiting = false;
        ResolveReferences();
        BindSubtitleBodyText();
        ConfigureContinueButton();
        ConfigureAutoPlaySwitch();
        RefreshContinueButtonVisibility();
        RefreshContinueVisual();
        RefreshAutoPlayVisual();
        RefreshSpeakerName();
    }

    private void OnEnable()
    {
        ForceDialogueSystemManualContinueMode();
        ResolveReferences();
        BindSubtitleBodyText();
        if (DialogueManager.instance != null)
        {
            DialogueManager.instance.conversationStarted += OnConversationStarted;
            DialogueManager.instance.conversationEnded += OnConversationEnded;
        }
    }

    private void OnDisable()
    {
        CancelPendingAutoPlay();
        ClearContinueRequestLock();

        if (DialogueManager.instance != null)
        {
            DialogueManager.instance.conversationStarted -= OnConversationStarted;
            DialogueManager.instance.conversationEnded -= OnConversationEnded;
        }
    }

    private void Update()
    {
        ForceDialogueSystemManualContinueMode();
        RefreshContinueButtonVisibility();
        RefreshContinueVisual();
        RefreshAutoPlayVisual();
        RefreshSpeakerName();
        TryAutoPlayContinue();
    }

    private void OnValidate()
    {
        ResolveReferences();
        RefreshContinueVisual();
        RefreshAutoPlayVisual();
    }

    private void ResolveReferences()
    {
        if (dialogueUI == null)
        {
            dialogueUI = GetComponentInParent<StandardDialogueUI>(true);
        }

        if (subtitlePanel == null)
        {
            subtitlePanel = GetComponentInChildren<StandardUISubtitlePanel>(true);
        }

        if (continueButton == null)
        {
            Transform continueTransform = FindDeepChild(transform, "Continue Button");
            if (continueTransform != null)
            {
                continueButton = continueTransform.GetComponent<Button>();
            }
        }

        if (continueButtonChosenImage == null)
        {
            continueButtonChosenImage = FindDeepChildGameObject("Continue Button Chosen Image");
        }

        if (continueButtonNormalImage == null)
        {
            continueButtonNormalImage = FindDeepChildGameObject("Continue Button Normal Image");
        }

        if (autoPlaySwitch == null)
        {
            Transform autoPlayTransform = FindDeepChild(transform, "Auto Play Switch");
            if (autoPlayTransform != null)
            {
                autoPlaySwitch = autoPlayTransform.GetComponent<Button>();
            }
        }

        if (autoPlaySwitchOnImage == null)
        {
            autoPlaySwitchOnImage = FindDeepChildGameObject("Auto Play Switch On Image");
        }

        if (autoPlaySwitchOffImage == null)
        {
            autoPlaySwitchOffImage = FindDeepChildGameObject("Auto Play Switch Off Image");
        }

        if (speakerNamePanel == null)
        {
            speakerNamePanel = FindDeepChildGameObject("Speaker Name Panel");
        }

        if (speakerNameText == null)
        {
            Transform speakerTextTransform = FindDeepChild(transform, "Speaker Name Text");
            if (speakerTextTransform != null)
            {
                speakerNameText = speakerTextTransform.GetComponent<Text>();
            }
        }

        if (typewriterEffect == null && subtitlePanel != null)
        {
            typewriterEffect = subtitlePanel.GetTypewriter();
        }

        RemovePortraitBinding();
    }

    private void BindSubtitleBodyText()
    {
        if (subtitlePanel == null)
        {
            return;
        }

#if TMP_PRESENT
        if (subtitleBodyTextMeshPro != null)
        {
            subtitlePanel.subtitleText = new UITextField(subtitleBodyTextMeshPro);
            typewriterEffect = EnsureTextMeshProTypewriter(subtitleBodyTextMeshPro);
            return;
        }
#endif

        if (subtitleBodyText == null)
        {
            Transform bodyTransform = FindDeepChild(transform, generatedSubtitleBodyName);
            if (bodyTransform != null)
            {
                subtitleBodyText = bodyTransform.GetComponent<Text>();
            }
        }

        if (subtitleBodyText == null)
        {
            if (!createLegacyFallbackBodyTextIfMissing)
            {
                typewriterEffect = subtitlePanel.GetTypewriter();
                return;
            }

            subtitleBodyText = speakerNameText != null
                ? CreateSubtitleBodyTextFrom(speakerNameText)
                : CreateDefaultSubtitleBodyText();
        }

        if (subtitleBodyText == null)
        {
            return;
        }

        if (subtitlePanel.subtitleText == null || subtitlePanel.subtitleText.uiText != subtitleBodyText)
        {
            subtitlePanel.subtitleText = new UITextField(subtitleBodyText);
            typewriterEffect = subtitlePanel.GetTypewriter();
        }
    }

#if TMP_PRESENT
    private AbstractTypewriterEffect EnsureTextMeshProTypewriter(TextMeshProUGUI text)
    {
        AbstractTypewriterEffect existingTypewriter = text.GetComponent<AbstractTypewriterEffect>();
        if (existingTypewriter != null)
        {
            return existingTypewriter;
        }

        return text.gameObject.AddComponent<TextMeshProTypewriterEffect>();
    }
#endif

    private Text CreateSubtitleBodyTextFrom(Text template)
    {
        GameObject bodyObject = new(generatedSubtitleBodyName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        Transform parent = template.transform.parent != null ? template.transform.parent : template.transform;
        if (speakerNamePanel != null &&
            template.transform.IsChildOf(speakerNamePanel.transform) &&
            speakerNamePanel.transform.parent != null)
        {
            parent = speakerNamePanel.transform.parent;
        }

        bodyObject.transform.SetParent(parent, false);
        bodyObject.transform.SetSiblingIndex(
            speakerNamePanel != null && speakerNamePanel.transform.parent == parent
                ? speakerNamePanel.transform.GetSiblingIndex() + 1
                : template.transform.GetSiblingIndex() + 1);

        RectTransform templateRect = template.rectTransform;
        RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
        bodyRect.anchorMin = templateRect.anchorMin;
        bodyRect.anchorMax = templateRect.anchorMax;
        bodyRect.pivot = templateRect.pivot;
        bodyRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(0f, -36f);
        bodyRect.sizeDelta = templateRect.sizeDelta;
        bodyRect.localScale = Vector3.one;

        Text bodyText = bodyObject.GetComponent<Text>();
        bodyText.font = template.font;
        bodyText.fontSize = template.fontSize;
        bodyText.fontStyle = template.fontStyle;
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.color = template.color;
        bodyText.supportRichText = template.supportRichText;
        bodyText.horizontalOverflow = template.horizontalOverflow;
        bodyText.verticalOverflow = template.verticalOverflow;
        bodyText.lineSpacing = template.lineSpacing;
        bodyText.raycastTarget = template.raycastTarget;
        bodyText.text = string.Empty;

        Shadow templateShadow = template.GetComponent<Shadow>();
        if (templateShadow != null)
        {
            Shadow bodyShadow = bodyObject.AddComponent<Shadow>();
            bodyShadow.effectColor = templateShadow.effectColor;
            bodyShadow.effectDistance = templateShadow.effectDistance;
            bodyShadow.useGraphicAlpha = templateShadow.useGraphicAlpha;
        }

        return bodyText;
    }

    private Text CreateDefaultSubtitleBodyText()
    {
        Transform parent = FindDeepChild(transform, "Text Panel");
        if (parent == null && subtitlePanel != null)
        {
            parent = subtitlePanel.transform;
        }
        if (parent == null)
        {
            parent = transform;
        }

        GameObject bodyObject = new(generatedSubtitleBodyName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        bodyObject.transform.SetParent(parent, false);

        RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.anchoredPosition = Vector2.zero;
        bodyRect.sizeDelta = new Vector2(-32f, -32f);

        Text bodyText = bodyObject.GetComponent<Text>();
        bodyText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        bodyText.fontSize = generatedSubtitleFontSize;
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.color = generatedSubtitleColor;
        bodyText.supportRichText = true;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Truncate;
        bodyText.raycastTarget = false;
        bodyText.text = string.Empty;

        return bodyText;
    }

    private void RemovePortraitBinding()
    {
        if (subtitlePanel == null)
        {
            return;
        }

        subtitlePanel.portraitImage = null;
    }

    private void ConfigureContinueButton()
    {
        if (continueButton == null)
        {
            return;
        }

        continueFastForward = continueButton.GetComponent<StandardUIContinueButtonFastForward>();
        if (continueFastForward == null)
        {
            continueFastForward = continueButton.gameObject.AddComponent<StandardUIContinueButtonFastForward>();
        }

        if (dialogueUI != null)
        {
            continueFastForward.dialogueUI = dialogueUI;
        }

        if (typewriterEffect != null)
        {
            continueFastForward.typewriterEffect = typewriterEffect;
        }

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(() => RequestContinueFromUser());
        AddTrigger(continueButton.gameObject, EventTriggerType.PointerEnter, () =>
        {
            continuePointerInside = true;
            RefreshContinueVisual();
        });
        AddTrigger(continueButton.gameObject, EventTriggerType.PointerExit, () =>
        {
            continuePointerInside = false;
            continuePointerDown = false;
            RefreshContinueVisual();
        });
        AddTrigger(continueButton.gameObject, EventTriggerType.PointerDown, () =>
        {
            continuePointerDown = true;
            RefreshContinueVisual();
        });
        AddTrigger(continueButton.gameObject, EventTriggerType.PointerUp, () =>
        {
            continuePointerDown = false;
            RefreshContinueVisual();
        });
        AddTrigger(continueButton.gameObject, EventTriggerType.Select, () =>
        {
            RefreshContinueVisual();
        });
        AddTrigger(continueButton.gameObject, EventTriggerType.Deselect, () =>
        {
            RefreshContinueVisual();
        });
    }

    private void ConfigureAutoPlaySwitch()
    {
        if (autoPlaySwitch == null)
        {
            return;
        }

        autoPlaySwitch.onClick.RemoveListener(ToggleAutoPlay);
        autoPlaySwitch.onClick.AddListener(ToggleAutoPlay);
    }

    private void ToggleAutoPlay()
    {
        SetAutoPlayEnabled(!autoPlayEnabled);
    }

    public bool RequestContinueFromUser()
    {
        if (!CanUseContinueNow() || !BeginContinueRequest())
        {
            return false;
        }

        SetAutoPlayEnabled(false);
        ContinueCurrentDialogue();
        return true;
    }

    public bool RequestContinueFromAutoPlay()
    {
        if (!CanUseContinueNow() || IsTypewriterPlaying() || !BeginContinueRequest())
        {
            return false;
        }

        ContinueCurrentDialogue();
        return true;
    }

    public void SetAutoPlayEnabled(bool value)
    {
        autoPlayEnabled = value;
        if (!autoPlayEnabled)
        {
            CancelPendingAutoPlay();
        }

        ForceDialogueSystemManualContinueMode();
        RefreshAutoPlayVisual();
    }

    public bool CanUseContinueNow()
    {
        return DialogueManager.isConversationActive &&
               continueButton != null &&
               continueButton.interactable &&
               CanSkipCurrentSentence() &&
               !IsResponseMenuActive();
    }

    private void ContinueCurrentDialogue()
    {
        if (continueFastForward != null)
        {
            continueFastForward.OnFastForward();
            return;
        }

        if (dialogueUI != null)
        {
            dialogueUI.OnContinue();
        }
    }

    private void RefreshContinueButtonVisibility()
    {
        if (continueButton == null)
        {
            return;
        }

        SetActiveIfDifferent(continueButton.gameObject, CanUseContinueNow());
    }

    private void RefreshContinueVisual()
    {
        if (continueButton == null)
        {
            return;
        }

        bool selected =
            EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == continueButton.gameObject;
        bool chosen = continueButton.interactable && (continuePointerInside || continuePointerDown || selected);

        SetActiveIfDifferent(continueButtonChosenImage, chosen);
        SetActiveIfDifferent(continueButtonNormalImage, !chosen);
    }

    private void RefreshAutoPlayVisual()
    {
        bool visible = CanUseContinueNow();
        if (autoPlaySwitch != null)
        {
            SetActiveIfDifferent(autoPlaySwitch.gameObject, visible);
        }

        SetActiveIfDifferent(autoPlaySwitchOnImage, visible && autoPlayEnabled);
        SetActiveIfDifferent(autoPlaySwitchOffImage, visible && !autoPlayEnabled);
    }

    private void RefreshSpeakerName()
    {
        string speakerName = GetCurrentSpeakerName();
        if (speakerName == lastSpeakerName)
        {
            return;
        }

        lastSpeakerName = speakerName;
        bool show = !IsNarrationSpeakerName(speakerName);
        SetSpeakerNamePanelVisible(show);

        if (speakerNameText == null)
        {
            return;
        }

        SetActiveIfDifferent(speakerNameText.gameObject, show);
        speakerNameText.text = show ? speakerName : string.Empty;
    }

    private void SetSpeakerNamePanelVisible(bool value)
    {
        if (speakerNamePanel == null)
        {
            return;
        }

        if (subtitlePanel != null && speakerNamePanel == subtitlePanel.gameObject)
        {
            Image[] images = speakerNamePanel.GetComponents<Image>();
            for (int i = 0; i < images.Length; i++)
            {
                images[i].enabled = value;
            }

            CanvasGroup canvasGroup = speakerNamePanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = value ? 1f : 0f;
                canvasGroup.blocksRaycasts = value;
            }

            return;
        }

        SetActiveIfDifferent(speakerNamePanel, value);
    }

    private string GetCurrentSpeakerName()
    {
        if (!DialogueManager.isConversationActive)
        {
            return string.Empty;
        }

        ConversationState state = DialogueManager.currentConversationState;
        Subtitle subtitle = state != null ? state.subtitle : null;
        return subtitle != null && subtitle.speakerInfo != null
            ? subtitle.speakerInfo.Name
            : string.Empty;
    }

    private bool IsNarrationSpeakerName(string speakerName)
    {
        if (string.IsNullOrWhiteSpace(speakerName))
        {
            return true;
        }

        for (int i = 0; i < narrationSpeakerNames.Length; i++)
        {
            if (string.Equals(speakerName, narrationSpeakerNames[i], System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void TryAutoPlayContinue()
    {
        if (!autoPlayEnabled ||
            autoPlayWaiting ||
            continueButton == null ||
            !continueButton.gameObject.activeInHierarchy ||
            !continueButton.interactable ||
            !CanSkipCurrentSentence() ||
            IsResponseMenuActive() ||
            IsTypewriterPlaying())
        {
            return;
        }

        autoPlayCoroutine = StartCoroutine(AutoPlayContinueAfterDelay());
    }

    private IEnumerator AutoPlayContinueAfterDelay()
    {
        autoPlayWaiting = true;
        yield return DialogueTime.WaitForSeconds(autoPlayDelay);

        if (autoPlayEnabled &&
            continueButton != null &&
            continueButton.gameObject.activeInHierarchy &&
            continueButton.interactable &&
            CanSkipCurrentSentence() &&
            !IsResponseMenuActive() &&
            !IsTypewriterPlaying())
        {
            RequestContinueFromAutoPlay();
        }

        autoPlayWaiting = false;
        autoPlayCoroutine = null;
    }

    private bool BeginContinueRequest()
    {
        if (continueRequestInProgress)
        {
            return false;
        }

        continueRequestInProgress = true;
        if (continueRequestUnlockCoroutine != null)
        {
            StopCoroutine(continueRequestUnlockCoroutine);
        }

        continueRequestUnlockCoroutine = StartCoroutine(UnlockContinueRequestAtEndOfFrame());
        return true;
    }

    private IEnumerator UnlockContinueRequestAtEndOfFrame()
    {
        yield return null;
        continueRequestInProgress = false;
        continueRequestUnlockCoroutine = null;
    }

    private void ClearContinueRequestLock()
    {
        if (continueRequestUnlockCoroutine != null)
        {
            StopCoroutine(continueRequestUnlockCoroutine);
            continueRequestUnlockCoroutine = null;
        }

        continueRequestInProgress = false;
    }

    private void CancelPendingAutoPlay()
    {
        if (autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
            autoPlayCoroutine = null;
        }

        autoPlayWaiting = false;
    }

    private bool IsTypewriterPlaying()
    {
        return typewriterEffect != null && typewriterEffect.isPlaying;
    }

    private bool IsResponseMenuActive()
    {
        if (dialogueUI == null ||
            dialogueUI.conversationUIElements.defaultMenuPanel == null)
        {
            return false;
        }

        return dialogueUI.conversationUIElements.defaultMenuPanel.gameObject.activeInHierarchy;
    }

    private bool CanSkipCurrentSentence()
    {
        ConversationState state = DialogueManager.currentConversationState;
        DialogueEntry entry = state != null && state.subtitle != null
            ? state.subtitle.dialogueEntry
            : null;

        if (entry == null || entry.fields == null)
        {
            return defaultCanSkipSentence;
        }

        string fieldValue = Field.LookupValue(entry.fields, canSkipSentenceFieldName);
        return string.IsNullOrWhiteSpace(fieldValue)
            ? defaultCanSkipSentence
            : Tools.StringToBool(fieldValue);
    }

    private void OnConversationStarted(Transform actor)
    {
        if (resetAutoPlayOnConversationStart)
        {
            autoPlayEnabled = false;
        }

        CancelPendingAutoPlay();
        ClearContinueRequestLock();
        ForceDialogueSystemManualContinueMode();
        RefreshSpeakerName();
        RefreshAutoPlayVisual();
    }

    private void OnConversationEnded(Transform actor)
    {
        autoPlayEnabled = false;
        CancelPendingAutoPlay();
        ClearContinueRequestLock();
        lastSpeakerName = null;
        ForceDialogueSystemManualContinueMode();
        RefreshSpeakerName();
        RefreshAutoPlayVisual();
    }

    private void ForceDialogueSystemManualContinueMode()
    {
        if (!forceManualContinueMode ||
            DialogueManager.instance == null ||
            DialogueManager.displaySettings == null ||
            DialogueManager.displaySettings.subtitleSettings == null)
        {
            return;
        }

        DialogueManager.displaySettings.subtitleSettings.continueButton =
            DisplaySettings.SubtitleSettings.ContinueButtonMode.Always;
    }

    private GameObject FindDeepChildGameObject(string childName)
    {
        Transform child = FindDeepChild(transform, childName);
        return child != null ? child.gameObject : null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform result = FindDeepChild(child, childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static void SetActiveIfDifferent(GameObject target, bool value)
    {
        if (target != null && target.activeSelf != value)
        {
            target.SetActive(value);
        }
    }

    private static void AddTrigger(GameObject target, EventTriggerType eventType, UnityEngine.Events.UnityAction action)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = target.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry entry = new()
        {
            eventID = eventType
        };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }
}
