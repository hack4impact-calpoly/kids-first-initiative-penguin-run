using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelVictoryPopup : MonoBehaviour
{
    private const int LearnedStep = 0;
    private const int AnimationStep = 1;
    private const int DialogueStep = 2;

    private static readonly Vector2 GravityMoverStart = new Vector2(-310f, 118f);
    private static readonly Vector2 GravityMoverEnd = new Vector2(295f, -36f);

    [Header("Sprites")]
    [SerializeField] private Sprite[] headerSprites;
    [SerializeField] private Sprite[] starSprites;
    [SerializeField] private Sprite[] learnedIconSprites;
    [SerializeField] private Sprite[] celebrationSprites;
    [SerializeField] private Sprite speakerSprite;
    [SerializeField] private Sprite gravityMoverSprite;
    [SerializeField] private Sprite replayIconSprite;
    [SerializeField] private Sprite nextIconSprite;

    [Header("Layout")]
    [SerializeField] private Vector2 popupSize = new Vector2(1240f, 760f);
    [SerializeField] private Color popupFillColor = new Color32(224, 246, 254, 255);
    [SerializeField] private Color popupBorderColor = new Color32(72, 181, 236, 255);
    [SerializeField] private Color learnedCardColor = new Color32(247, 252, 255, 255);
    [SerializeField] private Color accentColor = new Color32(72, 181, 236, 255);
    [SerializeField] private Color primaryTextColor = new Color32(24, 42, 59, 255);
    [SerializeField] private Color headingTextColor = new Color32(29, 76, 130, 255);
    [SerializeField] private Color nextButtonFillColor = new Color32(255, 199, 55, 255);
    [SerializeField] private bool showGravityDemo = true;

    private readonly List<AnimatedElement> animatedElements = new List<AnimatedElement>();
    private readonly List<Image> headerImages = new List<Image>();
    private readonly List<Image> starImages = new List<Image>();

    private Canvas runtimeCanvas;
    private GameObject popupGroup;
    private RectTransform popupRect;
    private RectTransform learnedStepRoot;
    private RectTransform animationStepRoot;
    private RectTransform dialogueStepRoot;
    private TMP_Text titleText;
    private TMP_Text learnedHeaderText;
    private RectTransform learnedRowsRoot;
    private Image speakerImage;
    private TMP_Text pipLabelText;
    private TMP_Text pipBodyText;
    private Button replayButton;
    private Button nextButton;
    private TMP_Text replayButtonText;
    private TMP_Text nextButtonText;
    private Image replayButtonIcon;
    private Image nextButtonIcon;
    private GameObject replayButtonObject;
    private GameObject nextButtonObject;
    private RectTransform gravityMover;
    private RectTransform gravityArrow;
    private TMP_Text gravityLabelText;
    private string nextSceneName;
    private int currentStep;
    private float animationStartTime;
    private Sprite generatedStarSprite;
    private Sprite generatedDownArrowSprite;
    private Sprite generatedSlopeSprite;

    public bool IsOpen
    {
        get { return popupGroup != null && popupGroup.activeSelf; }
    }

    private void OnDestroy()
    {
        if (replayButton != null)
        {
            replayButton.onClick.RemoveListener(ReplayCurrentLevel);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(AdvanceOrLoadNext);
        }

        if (IsOpen)
        {
            DialogueManager.SetExternalDialogueOpen(false);
        }
    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        if (currentStep == AnimationStep)
        {
            AnimateCelebration();
            AnimateGravityDemo();
        }
    }

    public void Show(string nextScene, string title, string[] learnedItems, Sprite[] learnedIcons, string pipMessage, string nextPrompt)
    {
        if (popupGroup == null)
        {
            BuildPopup();
        }

        nextSceneName = nextScene;
        titleText.text = string.IsNullOrWhiteSpace(title) ? "Pip made it home!" : title;
        learnedHeaderText.text = "YOU LEARNED:";
        PopulateLearnedRows(learnedItems, learnedIcons);
        PopulateImages();

        speakerImage.sprite = speakerSprite != null ? speakerSprite : gravityMoverSprite;
        speakerImage.enabled = speakerImage.sprite != null;
        pipLabelText.text = "PIP";
        pipBodyText.text = BuildPipBody(pipMessage, nextPrompt);
        replayButtonText.text = "Play again";
        replayButtonIcon.sprite = replayIconSprite;
        replayButtonIcon.enabled = replayIconSprite != null;
        nextButtonIcon.sprite = nextIconSprite;
        nextButtonIcon.enabled = nextIconSprite != null;

        currentStep = LearnedStep;
        ApplyStep();
        popupGroup.SetActive(true);
        popupGroup.transform.SetAsLastSibling();
        animationStartTime = Time.unscaledTime;
        DialogueManager.SetExternalDialogueOpen(true);
    }

    public void Hide()
    {
        if (popupGroup != null)
        {
            popupGroup.SetActive(false);
        }

        DialogueManager.SetExternalDialogueOpen(false);
    }

    private void BuildPopup()
    {
        RectTransform parent = EnsureRuntimeCanvas();
        popupGroup = new GameObject("LevelVictoryPopup", typeof(RectTransform));
        popupGroup.layer = LayerMask.NameToLayer("UI");
        popupGroup.transform.SetParent(parent, false);

        popupRect = popupGroup.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = Vector2.zero;
        popupRect.sizeDelta = popupSize;

        RectTransform border = CreateImageObject("PanelBorder", popupRect, new Vector2(0.5f, 0.5f), Vector2.zero, popupSize, popupBorderColor, 42f, true).rectTransform;
        CreateImageObject("PanelFill", border, new Vector2(0.5f, 0.5f), Vector2.zero, popupSize - new Vector2(8f, 8f), popupFillColor, 38f, true);

        Vector2 stepSize = popupSize - new Vector2(96f, 144f);
        learnedStepRoot = CreateEmpty("LearnedStep", border, new Vector2(0.5f, 0.5f), new Vector2(0f, 42f), stepSize);
        animationStepRoot = CreateEmpty("AnimationStep", border, new Vector2(0.5f, 0.5f), new Vector2(0f, 42f), stepSize);
        dialogueStepRoot = CreateEmpty("PipDialogueStep", border, new Vector2(0.5f, 0.5f), new Vector2(0f, 42f), stepSize);

        BuildLearnedStep(learnedStepRoot);
        BuildAnimationStep(animationStepRoot);
        BuildPipDialogueStep(dialogueStepRoot);
        BuildButtons(border);

        popupGroup.SetActive(false);
    }

    private void BuildLearnedStep(RectTransform parent)
    {
        RectTransform headerRoot = CreateEmpty("HeaderImages", parent, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(290f, 88f));
        for (int i = 0; i < 2; i++)
        {
            Image image = CreateImageObject("HeaderImage", headerRoot, new Vector2(0.5f, 0.5f), new Vector2(-55f + (i * 110f), 0f), new Vector2(78f, 78f), Color.white, 0f, false);
            image.preserveAspect = true;
            image.raycastTarget = false;
            headerImages.Add(image);
        }

        titleText = CreateTextObject("Title", parent, new Vector2(0.5f, 0.5f), new Vector2(0f, 165f), new Vector2(1020f, 82f), 56f, headingTextColor);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 38f;
        titleText.fontSizeMax = 56f;

        RectTransform starsRoot = CreateEmpty("Stars", parent, new Vector2(0.5f, 0.5f), new Vector2(0f, 58f), new Vector2(500f, 132f));
        for (int i = 0; i < 3; i++)
        {
            Image image = CreateImageObject("StarImage", starsRoot, new Vector2(0.5f, 0.5f), new Vector2(-138f + (i * 138f), 0f), new Vector2(122f, 122f), Color.white, 0f, false);
            image.preserveAspect = true;
            image.raycastTarget = false;
            starImages.Add(image);
        }

        RectTransform learnedCard = CreateImageObject("LearnedCard", parent, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(790f, 250f), learnedCardColor, 26f, true).rectTransform;
        learnedHeaderText = CreateTextObject("LearnedHeader", learnedCard, new Vector2(0f, 1f), new Vector2(410f, -52f), new Vector2(690f, 52f), 34f, new Color32(100, 122, 146, 255));
        learnedHeaderText.alignment = TextAlignmentOptions.Left;
        learnedHeaderText.fontStyle = FontStyles.Bold;
        learnedHeaderText.characterSpacing = 4f;

        learnedRowsRoot = CreateEmpty("LearnedRows", learnedCard, new Vector2(0.5f, 0.5f), new Vector2(18f, -38f), new Vector2(700f, 138f));
    }

    private void BuildAnimationStep(RectTransform parent)
    {
        TMP_Text animationTitle = CreateTextObject("AnimationTitle", parent, new Vector2(0.5f, 0.5f), new Vector2(0f, 245f), new Vector2(1000f, 72f), 48f, headingTextColor);
        animationTitle.text = "Gravity pulls Pip down the hill";
        animationTitle.alignment = TextAlignmentOptions.Center;
        animationTitle.fontStyle = FontStyles.Bold;
        animationTitle.enableAutoSizing = true;
        animationTitle.fontSizeMin = 34f;
        animationTitle.fontSizeMax = 48f;

        BuildCelebrationLayer(parent);

        RectTransform demoCard = CreateImageObject("GravityDemoCard", parent, new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(900f, 380f), learnedCardColor, 28f, true).rectTransform;
        BuildGravityDemo(demoCard);
    }

    private void BuildGravityDemo(RectTransform parent)
    {
        Image slope = CreateImageObject("GravitySlope", parent, new Vector2(0.5f, 0.5f), new Vector2(55f, 54f), new Vector2(620f, 26f), accentColor, 13f, true);
        slope.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -18f);

        Image arrow = CreateImageObject("GravityArrow", parent, new Vector2(0f, 1f), new Vector2(110f, -98f), new Vector2(86f, 112f), Color.white, 0f, false);
        arrow.sprite = GetDownArrowSprite();
        arrow.preserveAspect = true;
        gravityArrow = arrow.rectTransform;

        Image mover = CreateImageObject("GravityMover", parent, new Vector2(0.5f, 0.5f), GravityMoverStart, new Vector2(86f, 86f), Color.white, 0f, false);
        mover.sprite = gravityMoverSprite != null ? gravityMoverSprite : GetSlopeSprite();
        mover.preserveAspect = true;
        gravityMover = mover.rectTransform;

        gravityLabelText = CreateTextObject("GravityLabel", parent, new Vector2(0.5f, 0f), new Vector2(40f, 58f), new Vector2(760f, 70f), 36f, headingTextColor);
        gravityLabelText.text = "Gravity pulls Pip down the hill";
        gravityLabelText.alignment = TextAlignmentOptions.Center;
        gravityLabelText.fontStyle = FontStyles.Bold;
        gravityLabelText.enableAutoSizing = true;
        gravityLabelText.fontSizeMin = 25f;
        gravityLabelText.fontSizeMax = 36f;
    }

    private void BuildPipDialogueStep(RectTransform parent)
    {
        RectTransform speechCard = CreateImageObject("PipSpeechCard", parent, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(980f, 420f), new Color32(247, 252, 255, 240), 30f, true).rectTransform;

        speakerImage = CreateImageObject("PipSpeakerImage", speechCard, new Vector2(0f, 1f), new Vector2(96f, -96f), new Vector2(124f, 124f), Color.white, 0f, false);
        speakerImage.preserveAspect = true;
        speakerImage.raycastTarget = false;

        pipLabelText = CreateTextObject("PipLabel", speechCard, new Vector2(0f, 1f), new Vector2(218f, -72f), new Vector2(220f, 52f), 36f, headingTextColor);
        pipLabelText.alignment = TextAlignmentOptions.Left;
        pipLabelText.fontStyle = FontStyles.Bold;
        pipLabelText.characterSpacing = 4f;

        pipBodyText = CreateTextObject("PipBody", speechCard, new Vector2(0.5f, 0.5f), new Vector2(95f, -35f), new Vector2(740f, 270f), 44f, primaryTextColor);
        pipBodyText.alignment = TextAlignmentOptions.TopLeft;
        pipBodyText.fontStyle = FontStyles.Bold;
        pipBodyText.enableAutoSizing = true;
        pipBodyText.fontSizeMin = 30f;
        pipBodyText.fontSizeMax = 44f;
        pipBodyText.lineSpacing = 12f;
    }

    private void BuildButtons(RectTransform border)
    {
        replayButtonObject = CreateImageObject("ReplayButton", border, new Vector2(0.5f, 0f), new Vector2(-190f, 76f), new Vector2(330f, 92f), accentColor, 36f, true).gameObject;
        Image replayFill = CreateImageObject("ReplayButtonFill", replayButtonObject.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(322f, 84f), new Color32(213, 238, 249, 255), 32f, true);
        replayButton = replayButtonObject.AddComponent<Button>();
        replayButton.targetGraphic = replayFill;
        replayButton.onClick.AddListener(ReplayCurrentLevel);
        ConfigureButtonColors(replayButton);

        replayButtonIcon = CreateImageObject("ReplayButtonIcon", replayButtonObject.transform, new Vector2(0f, 0.5f), new Vector2(56f, 0f), new Vector2(42f, 42f), Color.white, 0f, false);
        replayButtonIcon.preserveAspect = true;
        replayButtonText = CreateTextObject("ReplayButtonText", replayButtonObject.transform, new Vector2(0.5f, 0.5f), new Vector2(34f, 0f), new Vector2(230f, 60f), 32f, headingTextColor);
        replayButtonText.alignment = TextAlignmentOptions.Center;
        replayButtonText.fontStyle = FontStyles.Bold;
        replayButtonText.enableAutoSizing = true;
        replayButtonText.fontSizeMin = 22f;
        replayButtonText.fontSizeMax = 32f;

        nextButtonObject = CreateImageObject("NextButton", border, new Vector2(0.5f, 0f), new Vector2(190f, 76f), new Vector2(345f, 92f), new Color32(210, 142, 16, 255), 36f, true).gameObject;
        Image nextFill = CreateImageObject("NextButtonFill", nextButtonObject.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(337f, 84f), nextButtonFillColor, 32f, true);
        nextButton = nextButtonObject.AddComponent<Button>();
        nextButton.targetGraphic = nextFill;
        nextButton.onClick.AddListener(AdvanceOrLoadNext);
        ConfigureButtonColors(nextButton);

        nextButtonText = CreateTextObject("NextButtonText", nextButtonObject.transform, new Vector2(0.5f, 0.5f), new Vector2(-26f, 0f), new Vector2(230f, 60f), 32f, new Color32(85, 50, 0, 255));
        nextButtonText.alignment = TextAlignmentOptions.Center;
        nextButtonText.fontStyle = FontStyles.Bold;
        nextButtonText.enableAutoSizing = true;
        nextButtonText.fontSizeMin = 22f;
        nextButtonText.fontSizeMax = 32f;
        nextButtonIcon = CreateImageObject("NextButtonIcon", nextButtonObject.transform, new Vector2(1f, 0.5f), new Vector2(-54f, 0f), new Vector2(42f, 42f), Color.white, 0f, false);
        nextButtonIcon.preserveAspect = true;
    }

    private void BuildCelebrationLayer(RectTransform border)
    {
        Sprite[] sprites = celebrationSprites != null ? celebrationSprites : new Sprite[0];
        if (sprites.Length == 0)
        {
            return;
        }

        float[] xs = { -505f, 505f, -450f, 450f, -520f, 520f, -375f, 375f, 0f };
        float[] ys = { 185f, 185f, -205f, -205f, -10f, -10f, 255f, 255f, 265f };

        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite == null)
            {
                continue;
            }

            bool isBalloon = sprite.name.ToLowerInvariant().Contains("balloon");
            Vector2 size = isBalloon ? new Vector2(56f, 80f) : new Vector2(32f, 32f);
            Image image = CreateImageObject("CelebrationImage", border, new Vector2(0.5f, 0.5f), new Vector2(xs[i % xs.Length], ys[i % ys.Length]), size, Color.white, 0f, false);
            image.sprite = sprite;
            image.color = new Color(1f, 1f, 1f, 0.55f);
            image.preserveAspect = true;
            image.raycastTarget = false;

            animatedElements.Add(new AnimatedElement
            {
                rect = image.rectTransform,
                origin = image.rectTransform.anchoredPosition,
                verticalDistance = isBalloon ? 70f : -70f,
                horizontalAmplitude = isBalloon ? 12f : 18f,
                speed = isBalloon ? 0.18f + (0.02f * i) : 0.35f + (0.03f * i),
                phase = i * 0.23f,
                rotationSpeed = isBalloon ? 6f : 65f
            });
        }
    }

    private void PopulateImages()
    {
        for (int i = 0; i < headerImages.Count; i++)
        {
            Sprite sprite = headerSprites != null && i < headerSprites.Length ? headerSprites[i] : null;
            headerImages[i].sprite = sprite;
            headerImages[i].enabled = sprite != null;
        }

        for (int i = 0; i < starImages.Count; i++)
        {
            Sprite sprite = starSprites != null && i < starSprites.Length ? starSprites[i] : GetGeneratedStarSprite();
            starImages[i].sprite = sprite;
            starImages[i].enabled = sprite != null;
        }
    }

    private void PopulateLearnedRows(string[] learnedItems, Sprite[] learnedIcons)
    {
        foreach (Transform child in learnedRowsRoot)
        {
            Destroy(child.gameObject);
        }

        string[] items = learnedItems != null && learnedItems.Length > 0
            ? learnedItems
            : new[] { "Gravity pulls things downhill", "Steeper hill means more speed" };

        for (int i = 0; i < items.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(items[i]))
            {
                continue;
            }

            RectTransform row = CreateEmpty("LearnedRow", learnedRowsRoot, new Vector2(0.5f, 0.5f), new Vector2(0f, 34f - (i * 66f)), new Vector2(700f, 58f));
            Image icon = CreateImageObject("LearnedIcon", row, new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(48f, 48f), Color.white, 0f, false);
            icon.sprite = ResolveLearnedIcon(i, learnedIcons);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TMP_Text text = CreateTextObject("LearnedText", row, new Vector2(0f, 0.5f), new Vector2(378f, 0f), new Vector2(600f, 54f), 36f, primaryTextColor);
            text.text = items[i];
            text.alignment = TextAlignmentOptions.Left;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = 24f;
            text.fontSizeMax = 36f;
        }
    }

    private Sprite ResolveLearnedIcon(int index, Sprite[] learnedIcons)
    {
        if (learnedIcons != null && index < learnedIcons.Length && learnedIcons[index] != null)
        {
            return learnedIcons[index];
        }

        if (learnedIconSprites != null && index < learnedIconSprites.Length && learnedIconSprites[index] != null)
        {
            return learnedIconSprites[index];
        }

        return index == 0 ? GetDownArrowSprite() : GetSlopeSprite();
    }

    private string BuildPipBody(string pipMessage, string nextPrompt)
    {
        string firstLine = string.IsNullOrWhiteSpace(pipMessage) ? "You did it! Gravity pulled me right home!" : pipMessage;
        if (string.IsNullOrWhiteSpace(nextPrompt))
        {
            return firstLine;
        }

        return firstLine + "\n\n" + nextPrompt;
    }

    private void AnimateCelebration()
    {
        float time = Time.unscaledTime - animationStartTime;
        for (int i = 0; i < animatedElements.Count; i++)
        {
            AnimatedElement element = animatedElements[i];
            if (element.rect == null)
            {
                continue;
            }

            float cycle = Mathf.Repeat((time * element.speed) + element.phase, 1f);
            float bob = Mathf.Sin((time + element.phase) * 4.2f) * element.horizontalAmplitude;
            float eased = Mathf.SmoothStep(0f, 1f, cycle);
            element.rect.anchoredPosition = element.origin + new Vector2(bob, element.verticalDistance * eased);
            element.rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin((time + element.phase) * 2.5f) * element.rotationSpeed);
        }
    }

    private void AnimateGravityDemo()
    {
        if (!showGravityDemo || gravityMover == null || gravityArrow == null)
        {
            return;
        }

        float time = Time.unscaledTime - animationStartTime;
        float cycle = Mathf.Repeat(time * 0.38f, 1f);
        float accelerated = cycle * cycle;
        gravityMover.anchoredPosition = Vector2.Lerp(GravityMoverStart, GravityMoverEnd, accelerated);
        gravityMover.localRotation = Quaternion.Euler(0f, 0f, -16f - (accelerated * 54f));

        float pulse = 1f + Mathf.Sin(time * 5.5f) * 0.08f;
        gravityArrow.localScale = new Vector3(pulse, pulse, 1f);
        gravityArrow.anchoredPosition = new Vector2(110f, -98f - Mathf.Abs(Mathf.Sin(time * 3.2f)) * 14f);
    }

    private RectTransform EnsureRuntimeCanvas()
    {
        if (runtimeCanvas != null)
        {
            return runtimeCanvas.GetComponent<RectTransform>();
        }

        Canvas existingCanvas = GetComponentInParent<Canvas>();
        if (existingCanvas != null)
        {
            runtimeCanvas = existingCanvas;
            return existingCanvas.GetComponent<RectTransform>();
        }

        GameObject canvasObject = new GameObject("VictoryPopupCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        runtimeCanvas = canvasObject.GetComponent<Canvas>();
        runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        runtimeCanvas.sortingOrder = 120;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.anchoredPosition = Vector2.zero;
        canvasRect.sizeDelta = Vector2.zero;
        return canvasRect;
    }

    private RectTransform CreateEmpty(string objectName, Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.layer = LayerMask.NameToLayer("UI");
        child.transform.SetParent(parent, false);

        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
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

    private void ConfigureButtonColors(Button button)
    {
        button.colors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color32(235, 249, 255, 255),
            pressedColor = new Color32(182, 224, 244, 255),
            selectedColor = new Color32(235, 249, 255, 255),
            disabledColor = new Color32(180, 180, 180, 128),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
    }

    private void ApplyStep()
    {
        SetStepActive(learnedStepRoot, currentStep == LearnedStep);
        SetStepActive(animationStepRoot, currentStep == AnimationStep);
        SetStepActive(dialogueStepRoot, currentStep == DialogueStep);

        bool isDialogueStep = currentStep == DialogueStep;
        if (replayButtonObject != null)
        {
            replayButtonObject.SetActive(isDialogueStep);
        }

        if (nextButtonObject != null)
        {
            nextButtonObject.SetActive(true);
            RectTransform rect = nextButtonObject.GetComponent<RectTransform>();
            rect.anchoredPosition = isDialogueStep ? new Vector2(190f, 76f) : new Vector2(0f, 76f);
            rect.sizeDelta = isDialogueStep ? new Vector2(345f, 92f) : new Vector2(250f, 86f);

            RectTransform fill = nextButtonObject.transform.Find("NextButtonFill") as RectTransform;
            if (fill != null)
            {
                fill.sizeDelta = isDialogueStep ? new Vector2(337f, 84f) : new Vector2(242f, 78f);
            }
        }

        if (nextButtonText != null)
        {
            nextButtonText.text = isDialogueStep ? "Next Level" : "Next";
            nextButtonText.rectTransform.anchoredPosition = isDialogueStep ? new Vector2(-26f, 0f) : Vector2.zero;
            nextButtonText.rectTransform.sizeDelta = isDialogueStep ? new Vector2(230f, 60f) : new Vector2(190f, 60f);
        }

        if (nextButtonIcon != null)
        {
            nextButtonIcon.gameObject.SetActive(isDialogueStep && nextIconSprite != null);
        }

        animationStartTime = Time.unscaledTime;
    }

    private void SetStepActive(RectTransform stepRoot, bool active)
    {
        if (stepRoot != null)
        {
            stepRoot.gameObject.SetActive(active);
        }
    }

    private void AdvanceOrLoadNext()
    {
        if (currentStep < DialogueStep)
        {
            currentStep++;
            ApplyStep();
            return;
        }

        LoadNextLevel();
    }

    private void ReplayCurrentLevel()
    {
        Hide();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void LoadNextLevel()
    {
        Hide();
        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextBuildIndex);
        }
    }

    private Sprite GetGeneratedStarSprite()
    {
        if (generatedStarSprite == null)
        {
            generatedStarSprite = CreateStarSprite(96, 96, new Color32(255, 203, 56, 255), new Color32(255, 166, 24, 255));
        }

        return generatedStarSprite;
    }

    private Sprite GetDownArrowSprite()
    {
        if (generatedDownArrowSprite == null)
        {
            generatedDownArrowSprite = CreateDownArrowSprite(72, 92, new Color32(102, 136, 166, 255));
        }

        return generatedDownArrowSprite;
    }

    private Sprite GetSlopeSprite()
    {
        if (generatedSlopeSprite == null)
        {
            generatedSlopeSprite = CreateSlopeSprite(80, 80, accentColor);
        }

        return generatedSlopeSprite;
    }

    private Sprite CreateRoundedSprite(int width, int height, float radius, Color color)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Generated Victory Rounded Rect",
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

    private Sprite CreateDownArrowSprite(int width, int height, Color color)
    {
        Texture2D texture = CreateTransparentTexture("Generated Down Arrow", width, height);
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool shaft = x >= width * 0.38f && x <= width * 0.62f && y >= height * 0.38f;
                bool head = y < height * 0.45f && Mathf.Abs(x - width * 0.5f) <= (height * 0.45f - y) * 0.75f;
                pixels[y * width + x] = shaft || head ? color : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateSlopeSprite(int width, int height, Color color)
    {
        Texture2D texture = CreateTransparentTexture("Generated Slope Icon", width, height);
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float rampY = Mathf.Lerp(height * 0.68f, height * 0.32f, x / (float)(width - 1));
                bool ramp = Mathf.Abs(y - rampY) < 4f;
                bool dot = (new Vector2(x - width * 0.72f, y - height * 0.28f)).sqrMagnitude < 80f;
                bool trail = x < width * 0.55f && x > width * 0.18f && Mathf.Abs(y - (height * 0.38f)) < 3f;
                pixels[y * width + x] = ramp || dot || trail ? color : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateStarSprite(int width, int height, Color fill, Color outline)
    {
        Texture2D texture = CreateTransparentTexture("Generated Star Icon", width, height);
        Color[] pixels = new Color[width * height];
        Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f) - center;
                float angle = Mathf.Atan2(point.y, point.x);
                float radius = point.magnitude / (width * 0.5f);
                float starRadius = 0.58f + 0.24f * Mathf.Cos(5f * angle);
                pixels[y * width + x] = radius < starRadius ? fill : (radius < starRadius + 0.045f ? outline : Color.clear);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Texture2D CreateTransparentTexture(string name, int width, int height)
    {
        return new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
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

    private struct AnimatedElement
    {
        public RectTransform rect;
        public Vector2 origin;
        public float verticalDistance;
        public float horizontalAmplitude;
        public float speed;
        public float phase;
        public float rotationSpeed;
    }
}
