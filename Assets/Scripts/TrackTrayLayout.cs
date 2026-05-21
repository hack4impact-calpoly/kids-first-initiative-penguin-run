using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class TrackTrayLayout : MonoBehaviour
{
    [System.Serializable]
    public class TrayItem
    {
        public bool showInTray = true;
        public string objectName = "TrackCard";
        public GameObject piecePrefab;
        public Sprite pieceArt;
        [Tooltip("Z-axis rotation in degrees for the tray preview art only.")]
        public float pieceArtRotation;
        public bool showArtTray;
        public Sprite cardSprite;
        public Sprite cardHoverSprite;
        public Sprite artTraySprite;
        public Sprite badgeSprite;
        public string title = "Track";
        public bool showBadge = true;
        public string badgeText = "fast";
        public Color cardBorderColor = new Color32(64, 92, 119, 255);
        public Color badgeColor = new Color32(55, 108, 145, 255);
        public Color badgeTextColor = new Color32(201, 235, 255, 255);
    }

    [Header("Content")]
    [SerializeField] private string promptText = "DRAG A PIECE ONTO THE PATH";
    [SerializeField] private TrayItem[] items;

    [Header("Layout")]
    [SerializeField] private Vector2 panelSizeDelta = new Vector2(-80f, 320f);
    [SerializeField] private Vector2 panelAnchoredPosition = new Vector2(0f, 165f);
    [SerializeField] private Vector2 promptPosition = new Vector2(58f, -42f);
    [SerializeField] private Vector2 firstCardPosition = new Vector2(160f, -190f);
    [SerializeField] private float cardSpacing = 220f;
    [SerializeField] private Vector2 cardSize = new Vector2(190f, 210f);
    [SerializeField] private Vector2 artTraySize = new Vector2(160f, 72f);
    [SerializeField] private Vector2 pieceArtSize = new Vector2(160f, 74f);
    [SerializeField] private Vector2 badgeSize = new Vector2(120f, 38f);
    [SerializeField] private bool positionActionButtons = true;
    [SerializeField] private Vector2 actionButtonSize = new Vector2(250f, 92f);
    [SerializeField] private Vector2 playButtonPosition = new Vector2(-215f, 178f);
    [SerializeField] private Vector2 replayButtonPosition = new Vector2(-215f, 75f);

    [Header("Undo Button")]
    [SerializeField] private bool showUndoButton = true;
    [SerializeField] private Sprite undoButtonSprite;
    [SerializeField] private Vector2 undoButtonPosition = new Vector2(-335f, 75f);
    [SerializeField] private Vector2 undoButtonSize = new Vector2(72f, 79f);
    [SerializeField] private Color undoButtonColor = new Color32(42, 96, 144, 255);
    [SerializeField] private Color undoButtonDisabledColor = new Color32(30, 74, 112, 130);

    [Header("Default Card Art")]
    [SerializeField] private Sprite defaultCardSprite;
    [SerializeField] private Sprite defaultCardHoverSprite;
    [SerializeField] private Sprite defaultArtTraySprite;
    [SerializeField] private Sprite defaultBadgeSprite;

    [Header("Colors")]
    [SerializeField] private Color trayBackgroundColor = new Color32(22, 56, 88, 255);
    [SerializeField] private Color cardColor = new Color32(30, 74, 112, 255);
    [SerializeField] private Color cardHoverColor = new Color32(42, 96, 144, 255);
    [SerializeField] private Color cardBorderColor = new Color32(64, 92, 119, 255);
    [SerializeField] private Color promptTextColor = new Color32(160, 200, 232, 255);
    [SerializeField] private Color titleTextColor = Color.white;

    private static readonly string[] LegacyChildNames =
    {
        "StraightTrackPieceIcon",
        "RampTrackPieceIcon",
        "CurveTrackPieceIcon",
        "Text (TMP)"
    };

    private const string UndoButtonName = "TrackTrayUndoButton";

    private readonly List<GameObject> spawnedPieces = new List<GameObject>();
    private Button undoButton;
    private Image undoButtonImage;
    private Image trayImage;
    private Coroutine pulseCoroutine;
    private Vector3 baseScale = Vector3.one;

    public int SpawnedPieceCount
    {
        get
        {
            PruneDestroyedPieces();
            return spawnedPieces.Count;
        }
    }

    private void Awake()
    {
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnValidate()
    {
        if (isActiveAndEnabled)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        RectTransform rect = GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = panelAnchoredPosition;
        rect.sizeDelta = panelSizeDelta;
        rect.localScale = Vector3.one;

        trayImage = GetComponent<Image>();
        trayImage.color = trayBackgroundColor;
        trayImage.raycastTarget = false;

        HideLegacyChildren();
        BuildPrompt();
        BuildCards();
        BuildUndoButton();

        if (positionActionButtons)
        {
            PositionActionButtons();
        }

        UpdateUndoButtonState();
    }

    public List<GameObject> GetSpawnedPiecesSnapshot()
    {
        PruneDestroyedPieces();
        return new List<GameObject>(spawnedPieces);
    }

    public void PulseTrayHint()
    {
        if (!Application.isPlaying || !isActiveAndEnabled)
        {
            return;
        }

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }

        pulseCoroutine = StartCoroutine(PulseTrayRoutine());
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            UpdateUndoButtonState();
        }
    }

    private void OnDisable()
    {
        UnregisterPaletteListeners();

        if (undoButton != null)
        {
            undoButton.onClick.RemoveListener(UndoLastSpawnedPiece);
        }

        spawnedPieces.Clear();
    }

    private void HideLegacyChildren()
    {
        foreach (string childName in LegacyChildNames)
        {
            Transform child = transform.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private void BuildPrompt()
    {
        TextMeshProUGUI label = GetOrCreateText("TrackTrayPrompt");
        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = promptPosition;
        rect.sizeDelta = new Vector2(760f, 52f);

        label.text = promptText;
        label.color = promptTextColor;
        label.alignment = TextAlignmentOptions.Left;
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = true;
        label.fontSizeMin = 22f;
        label.fontSizeMax = 34f;
        label.characterSpacing = 4f;
        label.raycastTarget = false;
    }

    private void BuildCards()
    {
        if (items == null)
        {
            HideUnusedGeneratedCards(new HashSet<string>());
            return;
        }

        HashSet<string> activeCardNames = new HashSet<string>();
        int visibleIndex = 0;
        for (int i = 0; i < items.Length; i++)
        {
            TrayItem item = items[i];
            if (item == null)
            {
                continue;
            }

            string objectName = string.IsNullOrWhiteSpace(item.objectName) ? $"TrackCard{i + 1}" : item.objectName;
            if (!item.showInTray)
            {
                SetChildActive(objectName, false);
                continue;
            }

            activeCardNames.Add(objectName);
            Transform child = GetOrCreateChild(objectName);
            child.gameObject.SetActive(true);

            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = firstCardPosition + new Vector2(visibleIndex * cardSpacing, 0f);
            rect.sizeDelta = cardSize;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            TrackPaletteButton button = child.GetComponent<TrackPaletteButton>();
            if (button == null)
            {
                button = child.gameObject.AddComponent<TrackPaletteButton>();
            }

            button.Configure(
                item.piecePrefab,
                item.pieceArt,
                item.title,
                item.showBadge && !string.IsNullOrWhiteSpace(item.badgeText),
                item.badgeText,
                item.showArtTray,
                ResolveSprite(item.cardSprite, defaultCardSprite),
                ResolveSprite(item.cardHoverSprite, defaultCardHoverSprite),
                ResolveSprite(item.artTraySprite, defaultArtTraySprite),
                ResolveSprite(item.badgeSprite, defaultBadgeSprite),
                item.pieceArtRotation,
                cardSize,
                artTraySize,
                pieceArtSize,
                badgeSize,
                cardColor,
                item.cardBorderColor == default ? cardBorderColor : item.cardBorderColor,
                cardHoverColor,
                Color.clear,
                Color.clear,
                item.badgeColor,
                titleTextColor,
                item.badgeTextColor);
            button.pieceSpawned.RemoveListener(RecordSpawnedPiece);
            button.pieceSpawned.AddListener(RecordSpawnedPiece);

            visibleIndex++;
        }

        HideUnusedGeneratedCards(activeCardNames);
    }

    private void BuildUndoButton()
    {
        if (!showUndoButton)
        {
            SetChildActive(UndoButtonName, false);
            undoButton = null;
            undoButtonImage = null;
            return;
        }

        Transform child = GetOrCreateChild(UndoButtonName);
        child.gameObject.SetActive(true);
        PositionRect(child.gameObject, new Vector2(1f, 0f), undoButtonPosition, undoButtonSize);

        undoButtonImage = child.GetComponent<Image>();
        if (undoButtonImage == null)
        {
            undoButtonImage = child.gameObject.AddComponent<Image>();
        }

        undoButtonImage.sprite = undoButtonSprite;
        undoButtonImage.type = Image.Type.Simple;
        undoButtonImage.preserveAspect = true;
        undoButtonImage.raycastTarget = true;

        undoButton = child.GetComponent<Button>();
        if (undoButton == null)
        {
            undoButton = child.gameObject.AddComponent<Button>();
        }

        undoButton.targetGraphic = undoButtonImage;
        undoButton.onClick.RemoveListener(UndoLastSpawnedPiece);
        undoButton.onClick.AddListener(UndoLastSpawnedPiece);
        ConfigureUndoButtonColors();
    }

    private void ConfigureUndoButtonColors()
    {
        if (undoButton == null)
        {
            return;
        }

        ColorBlock colors = undoButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        colors.colorMultiplier = 1f;
        undoButton.colors = colors;
    }

    public void UndoLastSpawnedPiece()
    {
        if (DialogueManager.IsDialogueOpen)
        {
            return;
        }

        PruneDestroyedPieces();

        for (int i = spawnedPieces.Count - 1; i >= 0; i--)
        {
            GameObject piece = spawnedPieces[i];
            spawnedPieces.RemoveAt(i);

            if (piece == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(piece);
            }
            else
            {
                DestroyImmediate(piece);
            }

            break;
        }

        UpdateUndoButtonState();
    }

    private void RecordSpawnedPiece(GameObject piece)
    {
        if (!Application.isPlaying || piece == null)
        {
            return;
        }

        PruneDestroyedPieces();
        spawnedPieces.Add(piece);
        UpdateUndoButtonState();
        FailureFeedbackManager activeFeedback = FindFirstObjectByType<FailureFeedbackManager>();
        if (activeFeedback != null)
        {
            activeFeedback.NotifyPieceSpawned();
        }
    }

    private IEnumerator PulseTrayRoutine()
    {
        if (trayImage == null)
        {
            trayImage = GetComponent<Image>();
        }

        baseScale = transform.localScale;
        Color baseColor = trayImage != null ? trayImage.color : trayBackgroundColor;
        Color pulseColor = new Color32(47, 132, 196, 255);
        const float duration = 1.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = (Mathf.Sin(elapsed * Mathf.PI * 5f) + 1f) * 0.5f;
            transform.localScale = baseScale * Mathf.Lerp(1f, 1.025f, pulse);

            if (trayImage != null)
            {
                trayImage.color = Color.Lerp(baseColor, pulseColor, pulse);
            }

            yield return null;
        }

        transform.localScale = baseScale;
        if (trayImage != null)
        {
            trayImage.color = baseColor;
        }

        pulseCoroutine = null;
    }

    private void PruneDestroyedPieces()
    {
        for (int i = spawnedPieces.Count - 1; i >= 0; i--)
        {
            if (spawnedPieces[i] == null)
            {
                spawnedPieces.RemoveAt(i);
            }
        }
    }

    private void UpdateUndoButtonState()
    {
        if (undoButton == null || undoButtonImage == null)
        {
            return;
        }

        PruneDestroyedPieces();
        bool canUndo = spawnedPieces.Count > 0 && !DialogueManager.IsDialogueOpen;
        undoButton.interactable = canUndo;
        undoButtonImage.color = undoButtonSprite == null
            ? (canUndo ? undoButtonColor : undoButtonDisabledColor)
            : (canUndo ? Color.white : new Color(1f, 1f, 1f, 0.45f));
    }

    private void UnregisterPaletteListeners()
    {
        foreach (Transform child in transform)
        {
            TrackPaletteButton button = child.GetComponent<TrackPaletteButton>();
            if (button != null)
            {
                button.pieceSpawned.RemoveListener(RecordSpawnedPiece);
            }
        }
    }

    private Sprite ResolveSprite(Sprite itemSprite, Sprite defaultSprite)
    {
        return itemSprite != null ? itemSprite : defaultSprite;
    }

    private void SetChildActive(string childName, bool active)
    {
        Transform child = transform.Find(childName);
        if (child != null)
        {
            child.gameObject.SetActive(active);
        }
    }

    private void HideUnusedGeneratedCards(HashSet<string> activeCardNames)
    {
        foreach (Transform child in transform)
        {
            if (activeCardNames.Contains(child.name))
            {
                continue;
            }

            if (child.GetComponent<TrackPaletteButton>() != null)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private void PositionActionButtons()
    {
        PositionActionButton("PlayButtonToStartBall", playButtonPosition);
        PositionActionButton("ResetButton_0", replayButtonPosition);
    }

    private void PositionActionButton(string objectName, Vector2 position)
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null)
        {
            return;
        }

        Vector2 size = GetSpriteSafeButtonSize(target, actionButtonSize);
        PositionRect(target, new Vector2(1f, 0f), position, size);

        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.preserveAspect = true;
        }
    }

    private void PositionRect(string objectName, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null)
        {
            return;
        }

        PositionRect(target, anchor, position, size);
    }

    private void PositionRect(GameObject target, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private Vector2 GetSpriteSafeButtonSize(GameObject target, Vector2 desiredSize)
    {
        Image image = target.GetComponent<Image>();
        if (image == null || image.sprite == null || image.sprite.rect.width <= 0f || image.sprite.rect.height <= 0f)
        {
            return desiredSize;
        }

        float aspect = image.sprite.rect.width / image.sprite.rect.height;
        return new Vector2(desiredSize.x, desiredSize.x / aspect);
    }

    private Transform GetOrCreateChild(string childName)
    {
        Transform existing = transform.Find(childName);
        if (existing != null)
        {
            return existing;
        }

        GameObject child = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, false);
        return child.transform;
    }

    private TextMeshProUGUI GetOrCreateText(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject childObject = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            childObject.layer = gameObject.layer;
            childObject.transform.SetParent(transform, false);
            return childObject.GetComponent<TextMeshProUGUI>();
        }

        TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = child.gameObject.AddComponent<TextMeshProUGUI>();
        }

        return text;
    }
}
