using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

public class DialogueManager : MonoBehaviour
{
  [Header("Dialogue Content")]
  [SerializeField, TextArea(2, 5), FormerlySerializedAs("customMessages")] private string[] messages;
  [SerializeField] private string advanceButtonLabel = "Let's go!";
  [SerializeField] private bool showOnlyFirstMessage = false;

  [Header("Dialogue UI")]
  [SerializeField] private GameObject dialogueBox;
  [SerializeField] private TMP_Text dialogueText;
  [SerializeField] private Button advanceButton;
  [SerializeField] private TMP_Text advanceButtonText;
  [SerializeField] private bool buildDialogueBoxIfMissing = true;
  [SerializeField] private Sprite speakerIconSprite;

  [Header("Slide Cue")]
  [SerializeField] private Vector2 slideCueScreenOffset = new Vector2(0f, 125f);
  [SerializeField] private Vector2 slideCueSize = new Vector2(430f, 112f);
  [SerializeField] private Color slideCueColor = Color.white;

  [Header("Lesson Card")]
  [SerializeField] private Vector2 lessonCardPosition = new Vector2(0f, 210f);
  [SerializeField] private Vector2 lessonCardSize = new Vector2(1560f, 360f);
  [SerializeField] private Color lessonCardFillColor = new Color32(231, 243, 253, 255);
  [SerializeField] private Color lessonCardBorderColor = new Color32(169, 210, 245, 255);
  [SerializeField] private Color lessonAccentColor = new Color32(72, 181, 236, 255);

  [Header("Level Complete Popup")]
  [SerializeField] private Vector2 completePopupSize = new Vector2(1580f, 1000f);
  [SerializeField] private Color completePopupFillColor = new Color32(224, 246, 254, 255);
  [SerializeField] private Color completePopupBorderColor = new Color32(72, 181, 236, 255);
  [SerializeField] private Color completeButtonFillColor = new Color32(255, 199, 55, 255);
  [SerializeField] private LevelVictoryPopup levelVictoryPopup;

  private static readonly string[] DefaultMessages =
  {
    "Use the right arrow on your keyboard to go through the tutorial!",
    "Help me get back to my igloo! Build a track of solid pieces and I will slide down it!!",
    "Drag a track piece from the box at the bottom. Snap pieces together to make a path."
  };

  private int currentMessageIndex = 0;
  private Canvas runtimeCanvas;
  private RectTransform runtimeCanvasRect;
  private GameObject slideCueBox;
  private TMP_Text slideCueText;
  private Image slideCueIcon;
  private Transform slideCueTarget;
  private bool slideCueVisible;
  private GameObject lessonCardGroup;
  private TMP_Text lessonTitleText;
  private TMP_Text lessonBodyText;
  private TMP_Text lessonButtonText;
  private TMP_Text lessonIconText;
  private Button lessonDismissButton;
  private System.Action lessonDismissed;
  private GameObject completePopupGroup;
  private TMP_Text completeIconText;
  private TMP_Text completeTitleText;
  private TMP_Text completeStarsText;
  private TMP_Text completeLearnedText;
  private TMP_Text completePipText;
  private TMP_Text completeReplayButtonText;
  private TMP_Text completeNextButtonText;
  private Button completeReplayButton;
  private Button completeNextButton;
  private string completeNextSceneName = "";
  private static DialogueManager activeManager;
  private static bool externalDialogueOpen;

  // We will use this to prevent interaction while dialogue boxes are open
  public static bool IsDialogueOpen { get; private set; }

  public static void SetExternalDialogueOpen(bool isOpen)
  {
    externalDialogueOpen = isOpen;
    if (activeManager != null)
    {
      activeManager.RefreshBlockingDialogueState();
      return;
    }

    IsDialogueOpen = isOpen;
  }

  private void Awake()
  {
    activeManager = this;

    if (dialogueBox == null && buildDialogueBoxIfMissing)
    {
      BuildSpeechBubbleDialogue();
    }

    if (advanceButton != null)
    {
      advanceButton.onClick.AddListener(NextMessage);
    }

    if (advanceButtonText != null)
    {
      advanceButtonText.text = advanceButtonLabel;
    }
  }

  private void OnDestroy()
  {
    if (activeManager == this)
    {
      activeManager = null;
      externalDialogueOpen = false;
      IsDialogueOpen = false;
    }

    if (advanceButton != null)
    {
      advanceButton.onClick.RemoveListener(NextMessage);
    }

    if (lessonDismissButton != null)
    {
      lessonDismissButton.onClick.RemoveListener(HideLessonCard);
    }

    if (completeReplayButton != null)
    {
      completeReplayButton.onClick.RemoveListener(ReplayCurrentLevel);
    }

    if (completeNextButton != null)
    {
      completeNextButton.onClick.RemoveListener(LoadNextLevel);
    }
  }

  private void Start()
  {
    ShowMessage(0);
  }
  private void Update()
  {
    if (dialogueBox != null && dialogueBox.activeSelf && Input.GetKeyDown(KeyCode.RightArrow))
    {
      NextMessage();
    }
  }

  private void LateUpdate()
  {
    if (slideCueVisible && slideCueTarget != null)
    {
      PositionSlideCue();
    }
  }

  public void ShowMessage(int index)
  {
    if (dialogueBox == null || dialogueText == null)
    {
      return;
    }

    string[] messages = CurrentMessages;
    if (index < 0 || index >= messages.Length)
    {
      HideDialogue();
      return;
    }

    IsDialogueOpen = true;
    currentMessageIndex = index;
    dialogueText.text = messages[currentMessageIndex];
    dialogueBox.SetActive(true);

    if (advanceButtonText != null)
    {
      advanceButtonText.text = advanceButtonLabel;
    }
  }

  public void NextMessage()
  {
    if (showOnlyFirstMessage)
    {
      currentMessageIndex = 0;
      HideDialogue();
      return;
    }

    currentMessageIndex++;
    // If we are done with the messages in this level, hide the tutorial
    string[] messages = CurrentMessages;
    if (currentMessageIndex >= messages.Length)
    {
      // Reset the message Index for the next level
      currentMessageIndex = 0;
      HideDialogue();
    }
    else
    {
      dialogueText.text = messages[currentMessageIndex];
    }
  }

  public void HideDialogue()
  {
    if (dialogueBox != null)
    {
      dialogueBox.SetActive(false);
    }

    RefreshBlockingDialogueState();
  }

  public void ShowSlideCue(string message, Transform target = null, Sprite icon = null)
  {
    if (string.IsNullOrEmpty(message))
    {
      return;
    }

    if (slideCueBox == null)
    {
      BuildSlideCue();
    }

    slideCueTarget = target;
    slideCueText.text = message;

    Sprite cueIcon = icon != null ? icon : speakerIconSprite;
    slideCueIcon.sprite = cueIcon;
    slideCueIcon.enabled = cueIcon != null;

    slideCueBox.SetActive(true);
    slideCueBox.transform.SetAsLastSibling();
    slideCueVisible = true;
    PositionSlideCue();
  }

  public void HideSlideCue()
  {
    slideCueVisible = false;
    slideCueTarget = null;

    if (slideCueBox != null)
    {
      slideCueBox.SetActive(false);
    }
  }

  public void ShowLessonCard(string title, string body, string buttonLabel = "Got it!", System.Action onDismiss = null)
  {
    if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(body))
    {
      return;
    }

    if (lessonCardGroup == null)
    {
      BuildLessonCard();
    }

    lessonTitleText.text = title;
    lessonBodyText.text = body;
    lessonButtonText.text = buttonLabel;
    lessonDismissed = onDismiss;

    lessonCardGroup.SetActive(true);
    lessonCardGroup.transform.SetAsLastSibling();
    RefreshBlockingDialogueState();
  }

  public void HideLessonCard()
  {
    if (lessonCardGroup == null || !lessonCardGroup.activeSelf)
    {
      return;
    }

    lessonCardGroup.SetActive(false);
    RefreshBlockingDialogueState();

    System.Action callback = lessonDismissed;
    lessonDismissed = null;
    callback?.Invoke();
  }

  public void ShowLevelCompletePopup(string nextSceneName, string title, string[] learnedItems, string pipMessage, string nextPrompt)
  {
    HideSlideCue();
    HideLessonCard();

    if (levelVictoryPopup == null)
    {
      levelVictoryPopup = FindFirstObjectByType<LevelVictoryPopup>();
    }

    if (levelVictoryPopup == null)
    {
      levelVictoryPopup = gameObject.AddComponent<LevelVictoryPopup>();
    }

    levelVictoryPopup.Show(nextSceneName, title, learnedItems, null, pipMessage, nextPrompt);
  }

  private void BuildSpeechBubbleDialogue()
  {
    GameObject canvasObject = new GameObject("DialogueCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
    Canvas canvas = canvasObject.GetComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 100;
    runtimeCanvas = canvas;

    CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920f, 1080f);
    scaler.matchWidthOrHeight = 0.5f;

    RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
    canvasRect.anchorMin = Vector2.zero;
    canvasRect.anchorMax = Vector2.one;
    canvasRect.anchoredPosition = Vector2.zero;
    canvasRect.sizeDelta = Vector2.zero;
    runtimeCanvasRect = canvasRect;

    dialogueBox = CreateImageObject("DialogueBox", canvasRect, new Vector2(0.5f, 0.5f), new Vector2(0f, 115f), new Vector2(920f, 455f), new Color32(198, 235, 248, 255), 38f, true).gameObject;

    Image penguinIcon = CreateImageObject("PipIcon", dialogueBox.transform, new Vector2(0f, 0f), new Vector2(120f, 92f), new Vector2(170f, 170f), Color.white, 0f, false);
    penguinIcon.sprite = speakerIconSprite;
    penguinIcon.enabled = speakerIconSprite != null;
    penguinIcon.preserveAspect = true;
    penguinIcon.raycastTarget = false;

    RectTransform bubble = CreateImageObject("SpeechBubble", dialogueBox.transform, new Vector2(0.5f, 0.5f), new Vector2(115f, 34f), new Vector2(620f, 300f), Color.white, 34f, true).rectTransform;

    dialogueText = CreateTextObject("DialogueText", bubble, new Vector2(0.5f, 0.5f), new Vector2(0f, 42f), new Vector2(510f, 170f), 42f, new Color32(42, 42, 42, 255));
    dialogueText.alignment = TextAlignmentOptions.TopLeft;
    dialogueText.fontStyle = FontStyles.Normal;
    dialogueText.richText = true;
    dialogueText.enableAutoSizing = true;
    dialogueText.fontSizeMin = 24f;
    dialogueText.fontSizeMax = 42f;
    dialogueText.lineSpacing = 8f;

    GameObject buttonObject = CreateImageObject("AdvanceButton", bubble, new Vector2(0f, 0f), new Vector2(175f, 60f), new Vector2(270f, 82f), new Color32(249, 194, 52, 255), 38f, true).gameObject;
    Image buttonImage = buttonObject.GetComponent<Image>();
    buttonImage.color = new Color32(249, 194, 52, 255);
    Button button = buttonObject.AddComponent<Button>();
    button.targetGraphic = buttonImage;
    button.colors = new ColorBlock
    {
      normalColor = Color.white,
      highlightedColor = new Color32(255, 222, 94, 255),
      pressedColor = new Color32(221, 151, 22, 255),
      selectedColor = new Color32(255, 222, 94, 255),
      disabledColor = new Color32(180, 180, 180, 128),
      colorMultiplier = 1f,
      fadeDuration = 0.1f
    };
    advanceButton = button;

    advanceButtonText = CreateTextObject("AdvanceButtonText", buttonObject.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 60f), 34f, new Color32(85, 50, 0, 255));
    advanceButtonText.fontStyle = FontStyles.Bold;
    advanceButtonText.alignment = TextAlignmentOptions.Center;
    advanceButtonText.enableAutoSizing = true;
    advanceButtonText.fontSizeMin = 20f;
    advanceButtonText.fontSizeMax = 34f;
  }

  private void BuildSlideCue()
  {
    RectTransform parent = EnsureRuntimeCanvas();
    slideCueBox = CreateImageObject("SlideCueBubble", parent, new Vector2(0.5f, 0.5f), new Vector2(0f, 245f), slideCueSize, slideCueColor, 32f, true).gameObject;

    Image tail = CreateImageObject("SlideCueTail", slideCueBox.transform, new Vector2(0.35f, 0f), new Vector2(0f, -48f), new Vector2(30f, 30f), slideCueColor, 0f, false);
    tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
    tail.transform.SetAsFirstSibling();

    slideCueIcon = CreateImageObject("SlideCueIcon", slideCueBox.transform, new Vector2(0f, 0.5f), new Vector2(56f, 0f), new Vector2(54f, 54f), Color.white, 0f, false);
    slideCueIcon.preserveAspect = true;
    slideCueIcon.raycastTarget = false;

    slideCueText = CreateTextObject("SlideCueText", slideCueBox.transform, new Vector2(0.5f, 0.5f), new Vector2(38f, 0f), new Vector2(320f, 72f), 34f, new Color32(29, 43, 56, 255));
    slideCueText.alignment = TextAlignmentOptions.Center;
    slideCueText.fontStyle = FontStyles.Bold;
    slideCueText.enableAutoSizing = true;
    slideCueText.fontSizeMin = 22f;
    slideCueText.fontSizeMax = 34f;

    slideCueBox.SetActive(false);
  }

  private void BuildLessonCard()
  {
    RectTransform parent = EnsureRuntimeCanvas();

    lessonCardGroup = new GameObject("GravityLessonCard", typeof(RectTransform));
    lessonCardGroup.layer = LayerMask.NameToLayer("UI");
    lessonCardGroup.transform.SetParent(parent, false);

    RectTransform groupRect = lessonCardGroup.GetComponent<RectTransform>();
    groupRect.anchorMin = new Vector2(0.5f, 0.5f);
    groupRect.anchorMax = new Vector2(0.5f, 0.5f);
    groupRect.pivot = new Vector2(0.5f, 0.5f);
    groupRect.anchoredPosition = Vector2.zero;
    groupRect.sizeDelta = new Vector2(1700f, 560f);

    RectTransform border = CreateImageObject("LessonPanelBorder", groupRect, new Vector2(0.5f, 0.5f), lessonCardPosition, lessonCardSize, lessonCardBorderColor, 34f, true).rectTransform;
    CreateImageObject("LessonPanelFill", border, new Vector2(0.5f, 0.5f), Vector2.zero, lessonCardSize - new Vector2(8f, 8f), lessonCardFillColor, 30f, true);

    Image iconBackground = CreateImageObject("LessonIconBackground", border, new Vector2(0f, 1f), new Vector2(82f, -82f), new Vector2(70f, 70f), new Color32(105, 133, 161, 255), 12f, true);
    lessonIconText = CreateTextObject("LessonIconText", iconBackground.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54f, 54f), 48f, Color.white);
    lessonIconText.alignment = TextAlignmentOptions.Center;
    lessonIconText.fontStyle = FontStyles.Bold;
    lessonIconText.enableAutoSizing = true;
    lessonIconText.fontSizeMin = 30f;
    lessonIconText.fontSizeMax = 48f;

    lessonTitleText = CreateTextObject("LessonTitle", border, new Vector2(0f, 1f), new Vector2(410f, -157f), new Vector2(1220f, 62f), 36f, new Color32(29, 76, 130, 255));
    lessonTitleText.alignment = TextAlignmentOptions.Left;
    lessonTitleText.fontStyle = FontStyles.Bold;
    lessonTitleText.enableAutoSizing = true;
    lessonTitleText.fontSizeMin = 26f;
    lessonTitleText.fontSizeMax = 36f;

    lessonBodyText = CreateTextObject("LessonBody", border, new Vector2(0f, 1f), new Vector2(770f, -252f), new Vector2(1410f, 115f), 30f, new Color32(24, 42, 59, 255));
    lessonBodyText.alignment = TextAlignmentOptions.TopLeft;
    lessonBodyText.fontStyle = FontStyles.Normal;
    lessonBodyText.enableAutoSizing = true;
    lessonBodyText.fontSizeMin = 22f;
    lessonBodyText.fontSizeMax = 30f;
    lessonBodyText.lineSpacing = 6f;

    GameObject buttonObject = CreateImageObject("LessonDismissButton", groupRect, new Vector2(0f, 0f), new Vector2(170f, 20f), new Vector2(230f, 88f), lessonAccentColor, 34f, true).gameObject;
    Image buttonInner = CreateImageObject("LessonDismissButtonFill", buttonObject.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(222f, 80f), new Color32(213, 238, 249, 255), 30f, true);

    lessonDismissButton = buttonObject.AddComponent<Button>();
    lessonDismissButton.targetGraphic = buttonInner;
    lessonDismissButton.onClick.AddListener(HideLessonCard);
    lessonDismissButton.colors = new ColorBlock
    {
      normalColor = Color.white,
      highlightedColor = new Color32(235, 249, 255, 255),
      pressedColor = new Color32(182, 224, 244, 255),
      selectedColor = new Color32(235, 249, 255, 255),
      disabledColor = new Color32(180, 180, 180, 128),
      colorMultiplier = 1f,
      fadeDuration = 0.1f
    };

    lessonButtonText = CreateTextObject("LessonDismissButtonText", buttonObject.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(195f, 60f), 30f, new Color32(29, 76, 130, 255));
    lessonButtonText.alignment = TextAlignmentOptions.Center;
    lessonButtonText.fontStyle = FontStyles.Bold;
    lessonButtonText.enableAutoSizing = true;
    lessonButtonText.fontSizeMin = 20f;
    lessonButtonText.fontSizeMax = 30f;

    lessonCardGroup.SetActive(false);
  }

  private void BuildLevelCompletePopup()
  {
    RectTransform parent = EnsureRuntimeCanvas();

    completePopupGroup = new GameObject("LevelCompletePopup", typeof(RectTransform));
    completePopupGroup.layer = LayerMask.NameToLayer("UI");
    completePopupGroup.transform.SetParent(parent, false);

    RectTransform groupRect = completePopupGroup.GetComponent<RectTransform>();
    groupRect.anchorMin = new Vector2(0.5f, 0.5f);
    groupRect.anchorMax = new Vector2(0.5f, 0.5f);
    groupRect.pivot = new Vector2(0.5f, 0.5f);
    groupRect.anchoredPosition = Vector2.zero;
    groupRect.sizeDelta = completePopupSize;

    RectTransform border = CreateImageObject("CompletePanelBorder", groupRect, new Vector2(0.5f, 0.5f), Vector2.zero, completePopupSize, completePopupBorderColor, 42f, true).rectTransform;
    CreateImageObject("CompletePanelFill", border, new Vector2(0.5f, 0.5f), Vector2.zero, completePopupSize - new Vector2(8f, 8f), completePopupFillColor, 38f, true);

    completeIconText = CreateTextObject("CompleteIconText", border, new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(360f, 90f), 56f, new Color32(24, 42, 59, 255));
    completeIconText.alignment = TextAlignmentOptions.Center;
    completeIconText.fontSizeMax = 56f;

    completeTitleText = CreateTextObject("CompleteTitle", border, new Vector2(0.5f, 1f), new Vector2(0f, -205f), new Vector2(900f, 72f), 48f, new Color32(29, 76, 130, 255));
    completeTitleText.alignment = TextAlignmentOptions.Center;
    completeTitleText.fontStyle = FontStyles.Bold;
    completeTitleText.enableAutoSizing = true;
    completeTitleText.fontSizeMin = 32f;
    completeTitleText.fontSizeMax = 48f;

    completeStarsText = CreateTextObject("CompleteStars", border, new Vector2(0.5f, 1f), new Vector2(0f, -315f), new Vector2(460f, 80f), 58f, new Color32(255, 182, 35, 255));
    completeStarsText.alignment = TextAlignmentOptions.Center;
    completeStarsText.enableAutoSizing = true;
    completeStarsText.fontSizeMin = 38f;
    completeStarsText.fontSizeMax = 58f;

    RectTransform learnedCard = CreateImageObject("CompleteLearnedCard", border, new Vector2(0.5f, 1f), new Vector2(0f, -505f), new Vector2(830f, 205f), new Color32(247, 252, 255, 255), 24f, true).rectTransform;
    completeLearnedText = CreateTextObject("CompleteLearnedText", learnedCard, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 150f), 31f, new Color32(24, 42, 59, 255));
    completeLearnedText.alignment = TextAlignmentOptions.TopLeft;
    completeLearnedText.fontStyle = FontStyles.Bold;
    completeLearnedText.enableAutoSizing = true;
    completeLearnedText.fontSizeMin = 22f;
    completeLearnedText.fontSizeMax = 31f;
    completeLearnedText.lineSpacing = 10f;

    completePipText = CreateTextObject("CompletePipText", border, new Vector2(0.5f, 0f), new Vector2(0f, 175f), new Vector2(1420f, 190f), 34f, new Color32(24, 42, 59, 255));
    completePipText.alignment = TextAlignmentOptions.TopLeft;
    completePipText.fontStyle = FontStyles.Bold;
    completePipText.enableAutoSizing = true;
    completePipText.fontSizeMin = 24f;
    completePipText.fontSizeMax = 34f;
    completePipText.lineSpacing = 10f;

    GameObject replayButtonObject = CreateImageObject("CompleteReplayButton", border, new Vector2(0f, 0f), new Vector2(168f, 72f), new Vector2(290f, 88f), lessonAccentColor, 34f, true).gameObject;
    Image replayFill = CreateImageObject("CompleteReplayButtonFill", replayButtonObject.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(282f, 80f), new Color32(213, 238, 249, 255), 30f, true);
    completeReplayButton = replayButtonObject.AddComponent<Button>();
    completeReplayButton.targetGraphic = replayFill;
    completeReplayButton.onClick.AddListener(ReplayCurrentLevel);

    completeReplayButtonText = CreateTextObject("CompleteReplayButtonText", replayButtonObject.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 56f), 30f, new Color32(29, 76, 130, 255));
    completeReplayButtonText.alignment = TextAlignmentOptions.Center;
    completeReplayButtonText.fontStyle = FontStyles.Bold;
    completeReplayButtonText.enableAutoSizing = true;
    completeReplayButtonText.fontSizeMin = 20f;
    completeReplayButtonText.fontSizeMax = 30f;

    GameObject nextButtonObject = CreateImageObject("CompleteNextButton", border, new Vector2(0f, 0f), new Vector2(470f, 72f), new Vector2(300f, 88f), new Color32(210, 142, 16, 255), 34f, true).gameObject;
    Image nextFill = CreateImageObject("CompleteNextButtonFill", nextButtonObject.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(292f, 80f), completeButtonFillColor, 30f, true);
    completeNextButton = nextButtonObject.AddComponent<Button>();
    completeNextButton.targetGraphic = nextFill;
    completeNextButton.onClick.AddListener(LoadNextLevel);

    completeNextButtonText = CreateTextObject("CompleteNextButtonText", nextButtonObject.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 56f), 30f, new Color32(85, 50, 0, 255));
    completeNextButtonText.alignment = TextAlignmentOptions.Center;
    completeNextButtonText.fontStyle = FontStyles.Bold;
    completeNextButtonText.enableAutoSizing = true;
    completeNextButtonText.fontSizeMin = 20f;
    completeNextButtonText.fontSizeMax = 30f;

    completePopupGroup.SetActive(false);
  }

  private string[] CurrentMessages
  {
    get
    {
      if (messages != null && messages.Length > 0)
      {
        return messages;
      }

      return DefaultMessages;
    }
  }

  private RectTransform EnsureRuntimeCanvas()
  {
    if (runtimeCanvasRect != null)
    {
      return runtimeCanvasRect;
    }

    Canvas existingCanvas = dialogueBox != null ? dialogueBox.GetComponentInParent<Canvas>() : null;
    if (existingCanvas != null)
    {
      runtimeCanvas = existingCanvas;
      runtimeCanvasRect = existingCanvas.GetComponent<RectTransform>();
      return runtimeCanvasRect;
    }

    GameObject canvasObject = new GameObject("DialogueRuntimeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
    runtimeCanvas = canvasObject.GetComponent<Canvas>();
    runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
    runtimeCanvas.sortingOrder = 100;

    CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920f, 1080f);
    scaler.matchWidthOrHeight = 0.5f;

    runtimeCanvasRect = canvasObject.GetComponent<RectTransform>();
    runtimeCanvasRect.anchorMin = Vector2.zero;
    runtimeCanvasRect.anchorMax = Vector2.one;
    runtimeCanvasRect.anchoredPosition = Vector2.zero;
    runtimeCanvasRect.sizeDelta = Vector2.zero;

    return runtimeCanvasRect;
  }

  private void RefreshBlockingDialogueState()
  {
    bool introOpen = dialogueBox != null && dialogueBox.activeSelf;
    bool lessonOpen = lessonCardGroup != null && lessonCardGroup.activeSelf;
    bool completeOpen = completePopupGroup != null && completePopupGroup.activeSelf;
    IsDialogueOpen = externalDialogueOpen || introOpen || lessonOpen || completeOpen;
  }

  private string BuildLearnedText(string[] learnedItems)
  {
    string learnedBody = "YOU LEARNED:";
    if (learnedItems == null || learnedItems.Length == 0)
    {
      return learnedBody;
    }

    foreach (string item in learnedItems)
    {
      if (!string.IsNullOrWhiteSpace(item))
      {
        learnedBody += "\n" + item;
      }
    }

    return learnedBody;
  }

  private void ReplayCurrentLevel()
  {
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
  }

  private void LoadNextLevel()
  {
    if (!string.IsNullOrWhiteSpace(completeNextSceneName))
    {
      SceneManager.LoadScene(completeNextSceneName);
      return;
    }

    int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;
    if (nextBuildIndex < SceneManager.sceneCountInBuildSettings)
    {
      SceneManager.LoadScene(nextBuildIndex);
    }
  }

  private void PositionSlideCue()
  {
    if (slideCueBox == null || runtimeCanvasRect == null)
    {
      return;
    }

    RectTransform slideCueRect = slideCueBox.GetComponent<RectTransform>();
    if (slideCueTarget == null)
    {
      slideCueRect.anchoredPosition = new Vector2(0f, 245f);
      return;
    }

    Camera worldCamera = Camera.main;
    if (worldCamera == null)
    {
      slideCueRect.anchoredPosition = new Vector2(0f, 245f);
      return;
    }

    Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, slideCueTarget.position) + slideCueScreenOffset;
    Camera canvasCamera = runtimeCanvas != null && runtimeCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? runtimeCanvas.worldCamera : null;

    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(runtimeCanvasRect, screenPoint, canvasCamera, out Vector2 localPoint))
    {
      Rect canvasRect = runtimeCanvasRect.rect;
      if (canvasRect.width > 0f && canvasRect.height > 0f)
      {
        float halfWidth = slideCueSize.x * 0.5f;
        float halfHeight = slideCueSize.y * 0.5f;
        localPoint.x = Mathf.Clamp(localPoint.x, canvasRect.xMin + halfWidth, canvasRect.xMax - halfWidth);
        localPoint.y = Mathf.Clamp(localPoint.y, canvasRect.yMin + halfHeight, canvasRect.yMax - halfHeight);
      }

      slideCueRect.anchoredPosition = localPoint;
    }
  }

  private Image CreateImageObject(string objectName, Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color, float radius, bool rounded)
  {
    GameObject child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
    child.layer = LayerMask.NameToLayer("UI");
    child.transform.SetParent(parent, false);

    RectTransform rect = child.GetComponent<RectTransform>();
    rect.anchorMin = anchor;
    rect.anchorMax = anchor;
    rect.pivot = new Vector2(0.5f, 0.5f);
    rect.anchoredPosition = anchoredPosition;
    rect.sizeDelta = size;

    Image image = child.GetComponent<Image>();
    image.color = color;
    image.raycastTarget = rounded;

    if (rounded)
    {
      image.sprite = CreateRoundedSprite(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y), radius, Color.white);
      image.type = Image.Type.Simple;
    }

    return image;
  }

  private TMP_Text CreateTextObject(string objectName, Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, float fontSize, Color color)
  {
    GameObject child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
    child.layer = LayerMask.NameToLayer("UI");
    child.transform.SetParent(parent, false);

    RectTransform rect = child.GetComponent<RectTransform>();
    rect.anchorMin = anchor;
    rect.anchorMax = anchor;
    rect.pivot = new Vector2(0.5f, 0.5f);
    rect.anchoredPosition = anchoredPosition;
    rect.sizeDelta = size;

    TMP_Text text = child.GetComponent<TMP_Text>();
    text.fontSize = fontSize;
    text.color = color;
    text.richText = true;
    text.raycastTarget = false;
    text.textWrappingMode = TextWrappingModes.Normal;
    text.overflowMode = TextOverflowModes.Ellipsis;
    return text;
  }

  private Sprite CreateRoundedSprite(int width, int height, float radius, Color color)
  {
    Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
    {
      name = "Generated Dialogue Rounded Rect",
      filterMode = FilterMode.Bilinear,
      wrapMode = TextureWrapMode.Clamp
    };

    Color[] pixels = new Color[width * height];
    for (int y = 0; y < height; y++)
    {
      for (int x = 0; x < width; x++)
      {
        pixels[y * width + x] = IsInsideRoundedRect(x + 0.5f, y + 0.5f, width, height, radius) ? color : Color.clear;
      }
    }

    texture.SetPixels(pixels);
    texture.Apply(false, true);
    return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
  }

  private bool IsInsideRoundedRect(float x, float y, int width, int height, float radius)
  {
    radius = Mathf.Min(radius, width * 0.5f, height * 0.5f);
    float centerX = Mathf.Clamp(x, radius, width - radius);
    float centerY = Mathf.Clamp(y, radius, height - radius);
    float dx = x - centerX;
    float dy = y - centerY;
    return dx * dx + dy * dy <= radius * radius;
  }
}
