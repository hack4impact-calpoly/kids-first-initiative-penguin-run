using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasRenderer))]
[RequireComponent(typeof(Image))]
public class TrackPaletteButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [System.Serializable]
    public class SpawnedPieceEvent : UnityEvent<GameObject> { }

    [Header("Drag")]
    public GameObject piecePrefab;
    public Camera worldCamera;
    [SerializeField] private bool draggable = true;
    [SerializeField] private bool snapOnRelease = true;

    [Header("Content")]
    [SerializeField] private Sprite pieceArt;
    [SerializeField] private float pieceArtRotation;
    [SerializeField] private string title = "Track";
    [SerializeField] private bool showBadge = true;
    [SerializeField] private string badgeText = "FAST!";
    [SerializeField] private bool showArtTray = true;
    [SerializeField] private Sprite cardSprite;
    [SerializeField] private Sprite cardHoverSprite;
    [SerializeField] private Sprite artTraySprite;
    [SerializeField] private Sprite badgeSprite;

    [Header("Layout")]
    [SerializeField] private bool manageRectTransform = true;
    [SerializeField] private Vector2 cardSize = new Vector2(116f, 136f);
    [SerializeField] private Vector2 artTraySize = new Vector2(78f, 28f);
    [SerializeField] private Vector2 pieceArtSize = new Vector2(74f, 24f);
    [SerializeField] private Vector2 badgeSize = new Vector2(70f, 25f);

    [Header("Colors")]
    [SerializeField] private Color cardColor = new Color32(59, 91, 121, 255);
    [SerializeField] private Color cardBorderColor = new Color32(102, 134, 166, 255);
    [SerializeField] private Color cardHoverColor = new Color32(68, 104, 136, 255);
    [SerializeField] private Color artTrayColor = new Color32(72, 177, 216, 255);
    [SerializeField] private Color artTrayBorderColor = new Color32(121, 212, 239, 255);
    [SerializeField] private Color badgeColor = new Color32(58, 121, 160, 255);
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color badgeTextColor = Color.white;

    [Header("Events")]
    public UnityEvent pointerPressed = new UnityEvent();
    public UnityEvent dragStarted = new UnityEvent();
    public UnityEvent dragEnded = new UnityEvent();
    public SpawnedPieceEvent pieceSpawned = new SpawnedPieceEvent();

    private const string ArtTrayName = "Art Tray";
    private const string ArtName = "Art";
    private const string TitleName = "Title";
    private const string BadgeName = "Badge";
    private const string BadgeTextName = "Badge Text";

    private Image cardImage;
    private Image artTrayImage;
    private Image artImage;
    private Image badgeImage;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI badgeLabel;

    private bool pointerIsDown;
    private bool pointerIsOver;
    private GameObject spawned;
    private DragPlacedPiece drag;

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
        if (transform.Find(ArtTrayName) != null)
        {
            Refresh();
        }
    }

    public void Configure(
        GameObject newPiecePrefab,
        Sprite newPieceArt,
        string newTitle,
        bool newShowBadge,
        string newBadgeText,
        bool newShowArtTray,
        Sprite newCardSprite,
        Sprite newCardHoverSprite,
        Sprite newArtTraySprite,
        Sprite newBadgeSprite,
        float newPieceArtRotation,
        Vector2 newCardSize,
        Vector2 newArtTraySize,
        Vector2 newPieceArtSize,
        Vector2 newBadgeSize,
        Color newCardColor,
        Color newCardBorderColor,
        Color newCardHoverColor,
        Color newArtTrayColor,
        Color newArtTrayBorderColor,
        Color newBadgeColor,
        Color newTitleColor,
        Color newBadgeTextColor)
    {
        piecePrefab = newPiecePrefab;
        pieceArt = newPieceArt;
        title = newTitle;
        showBadge = newShowBadge;
        badgeText = newBadgeText;
        showArtTray = newShowArtTray;
        cardSprite = newCardSprite;
        cardHoverSprite = newCardHoverSprite;
        artTraySprite = newArtTraySprite;
        badgeSprite = newBadgeSprite;
        pieceArtRotation = newPieceArtRotation;
        cardSize = newCardSize;
        artTraySize = newArtTraySize;
        pieceArtSize = newPieceArtSize;
        badgeSize = newBadgeSize;
        cardColor = newCardColor;
        cardBorderColor = newCardBorderColor;
        cardHoverColor = newCardHoverColor;
        artTrayColor = newArtTrayColor;
        artTrayBorderColor = newArtTrayBorderColor;
        badgeColor = newBadgeColor;
        titleColor = newTitleColor;
        badgeTextColor = newBadgeTextColor;
        Refresh();
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        pointerIsDown = true;
        pointerPressed?.Invoke();
        ApplyVisualState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerIsDown = false;
        ApplyVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerIsOver = true;
        ApplyVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerIsOver = false;
        ApplyVisualState();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanDrag())
        {
            return;
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (worldCamera == null)
        {
            Debug.LogWarning($"{nameof(TrackPaletteButton)} on {name} needs a world camera before it can spawn a track piece.", this);
            return;
        }

        Vector3 mouseWorld = ScreenToWorld(eventData.position, piecePrefab.transform.position.z);
        Quaternion rotation = piecePrefab.transform.rotation;
        spawned = Instantiate(piecePrefab, mouseWorld, rotation);

        drag = spawned.GetComponent<DragPlacedPiece>();
        if (drag == null)
        {
            drag = spawned.AddComponent<DragPlacedPiece>();
        }

        drag.worldCamera = worldCamera;
        drag.snapOnRelease = snapOnRelease;
        drag.BeginDrag(eventData);

        pieceSpawned?.Invoke(spawned);
        dragStarted?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (drag != null)
        {
            drag.Drag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (drag != null)
        {
            drag.EndDrag(eventData);
        }

        spawned = null;
        drag = null;
        pointerIsDown = false;
        dragEnded?.Invoke();
        ApplyVisualState();
    }

    public void Refresh()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        EnsureVisuals();
        ApplyLayout();
        ApplyVisualState();
        ApplyContent();
    }

    private bool CanDrag()
    {
        return draggable && piecePrefab != null && !DialogueManager.IsDialogueOpen;
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition, float depth)
    {
        Vector3 point = new Vector3(screenPosition.x, screenPosition.y, -worldCamera.transform.position.z);
        Vector3 world = worldCamera.ScreenToWorldPoint(point);
        world.z = depth;
        return world;
    }

    private void EnsureVisuals()
    {
        cardImage = GetComponent<Image>();
        artTrayImage = GetOrCreateImage(ArtTrayName);
        artImage = GetOrCreateImage(ArtName);
        titleLabel = GetOrCreateText(TitleName);
        badgeImage = GetOrCreateImage(BadgeName);
        badgeLabel = GetOrCreateText(BadgeTextName);

        artTrayImage.transform.SetSiblingIndex(0);
        artImage.transform.SetSiblingIndex(1);
        titleLabel.transform.SetSiblingIndex(2);
        badgeImage.transform.SetSiblingIndex(3);
        badgeLabel.transform.SetSiblingIndex(4);
    }

    private Image GetOrCreateImage(string childName)
    {
        Transform child = GetOrCreateChild(childName);
        Image image = child.GetComponent<Image>();
        if (image == null)
        {
            image = child.gameObject.AddComponent<Image>();
        }

        image.raycastTarget = false;
        image.maskable = true;
        return image;
    }

    private TextMeshProUGUI GetOrCreateText(string childName)
    {
        Transform child = GetOrCreateChild(childName);
        TextMeshProUGUI label = child.GetComponent<TextMeshProUGUI>();
        if (label == null)
        {
            label = child.gameObject.AddComponent<TextMeshProUGUI>();
        }

        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = true;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private Transform GetOrCreateChild(string childName)
    {
        Transform existing = transform.Find(childName);
        if (existing != null)
        {
            return existing;
        }

        GameObject child = new GameObject(childName, typeof(RectTransform));
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, false);
        return child.transform;
    }

    private void ApplyLayout()
    {
        RectTransform rect = (RectTransform)transform;
        if (manageRectTransform)
        {
            rect.sizeDelta = cardSize;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        SetRect(artTrayImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -74f), artTraySize);
        SetRect(artImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -74f), pieceArtSize);
        artImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, pieceArtRotation);
        SetRect(titleLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 74f), new Vector2(cardSize.x - 22f, 44f));
        SetRect(badgeImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 35f), badgeSize);
        SetRect(badgeLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 35f), badgeSize);
    }

    private void SetRect(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private void ApplyVisualState()
    {
        Color activeCardColor = pointerIsOver || pointerIsDown ? cardHoverColor : cardColor;
        Sprite activeCardSprite = pointerIsOver || pointerIsDown ? cardHoverSprite : cardSprite;
        if (activeCardSprite == null && (pointerIsOver || pointerIsDown))
        {
            activeCardSprite = cardSprite;
        }

        if (activeCardSprite != null)
        {
            SetCustomSpriteImage(cardImage, activeCardSprite);
        }
        else
        {
            SetRoundedImage(cardImage, new Vector2Int(Mathf.RoundToInt(cardSize.x), Mathf.RoundToInt(cardSize.y)), 22f, activeCardColor, cardBorderColor, 3f);
        }

        artTrayImage.enabled = showArtTray;
        if (showArtTray)
        {
            if (artTraySprite != null)
            {
                SetCustomSpriteImage(artTrayImage, artTraySprite);
            }
            else
            {
                SetRoundedImage(artTrayImage, new Vector2Int(Mathf.RoundToInt(artTraySize.x), Mathf.RoundToInt(artTraySize.y)), 8f, artTrayColor, artTrayBorderColor, 2f);
            }
        }

        if (badgeSprite != null)
        {
            SetCustomSpriteImage(badgeImage, badgeSprite);
        }
        else
        {
            SetRoundedImage(badgeImage, new Vector2Int(Mathf.RoundToInt(badgeSize.x), Mathf.RoundToInt(badgeSize.y)), 18f, badgeColor, Color.clear, 0f);
        }
    }

    private void ApplyContent()
    {
        cardImage.raycastTarget = true;

        artImage.sprite = pieceArt;
        artImage.color = Color.white;
        artImage.preserveAspect = true;
        artImage.enabled = pieceArt != null;

        titleLabel.text = title;
        titleLabel.color = titleColor;
        titleLabel.fontSizeMax = 27f;
        titleLabel.fontSizeMin = 16f;

        bool hasBadge = showBadge && !string.IsNullOrWhiteSpace(badgeText);
        badgeImage.enabled = hasBadge;
        badgeLabel.enabled = hasBadge;
        badgeLabel.text = badgeText;
        badgeLabel.color = badgeTextColor;
        badgeLabel.fontSizeMax = 21f;
        badgeLabel.fontSizeMin = 12f;
    }

    private void SetRoundedImage(Image image, Vector2Int textureSize, float radius, Color fillColor, Color borderColor, float borderWidth)
    {
        image.sprite = RoundedRectSprites.Get(textureSize.x, textureSize.y, radius, fillColor, borderColor, borderWidth);
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
    }

    private void SetCustomSpriteImage(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
    }

    private static class RoundedRectSprites
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(int width, int height, float radius, Color fillColor, Color borderColor, float borderWidth)
        {
            Color32 fill = fillColor;
            Color32 border = borderColor;
            string key = $"{width}:{height}:{radius}:{borderWidth}:{fill.r}:{fill.g}:{fill.b}:{fill.a}:{border.r}:{border.g}:{border.b}:{border.a}";

            if (Cache.TryGetValue(key, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Generated Rounded Palette Button",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color clear = Color.clear;
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool insideOuter = IsInsideRoundedRect(x + 0.5f, y + 0.5f, width, height, radius, 0f);
                    if (!insideOuter)
                    {
                        pixels[y * width + x] = clear;
                        continue;
                    }

                    bool insideInner = borderWidth <= 0f || IsInsideRoundedRect(x + 0.5f, y + 0.5f, width, height, Mathf.Max(0f, radius - borderWidth), borderWidth);
                    pixels[y * width + x] = insideInner ? fillColor : borderColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);

            sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[key] = sprite;
            return sprite;
        }

        private static bool IsInsideRoundedRect(float x, float y, int width, int height, float radius, float inset)
        {
            float left = inset;
            float right = width - inset;
            float bottom = inset;
            float top = height - inset;

            if (x < left || x >= right || y < bottom || y >= top)
            {
                return false;
            }

            radius = Mathf.Min(radius, (right - left) * 0.5f, (top - bottom) * 0.5f);
            float centerX = Mathf.Clamp(x, left + radius, right - radius);
            float centerY = Mathf.Clamp(y, bottom + radius, top - radius);
            float dx = x - centerX;
            float dy = y - centerY;
            return dx * dx + dy * dy <= radius * radius;
        }
    }
}
