using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Level2FrictionController : MonoBehaviour
{
    private enum TileKind
    {
        Ice,
        Gravel
    }

    private enum LevelState
    {
        Build,
        Sliding,
        Complete
    }

    private class Slot
    {
        public Vector2 center;
        public GameObject tile;
        public TrackTile trackTile;
        public GameObject visual;
    }

    [Header("Physics Tunables")]
    [SerializeField] private Vector2 startingVelocity = new Vector2(1000f, 0f);
    [SerializeField] private float startingImpulse = 1000f;
    [SerializeField] private float startingVelocityDuration = 1f;
    [SerializeField] private float gravityScale = 200f;
    [SerializeField] private float gravelDecel = 18f;
    [SerializeField] private float goalHoldDuration = 2f;
    [SerializeField] private float stuckSpeedThreshold = 0.3f;
    [SerializeField] private float stuckDuration = 0.5f;
    [SerializeField] private bool failureDialoguesPaused = true;

    [Header("Manual Scene Object Names")]
    [SerializeField] private string slotNamePrefix = "Slot ";
    [SerializeField] private string homeObjectName = "IglooBase";
    [SerializeField] private string cliffObjectName = "OutOfBoundsCliff";

    private const string SceneName = "Level2_Friction";
    private const string IntroMessage = "This hill is steep, so I'll go too fast! Build the path, then use bumpy gravel to slow me down near my igloo.";
    private const string IceCue = "Wheee — here I go, super fast!";
    private const string GravelCue = "Brrrt! Friction!";
    private const string HomeCue = "Ahh… nice and slow. Home!";
    private const string OvershootFail = "Too fast! Smooth ice did not slow me down. Try rough gravel near my igloo.";
    private const string StuckFail = "I stopped too soon! Move the gravel closer to my igloo.";
    private const string TeachingBody = "Friction happens when two things rub against each other. Rough stuff like gravel has LOTS of friction, so it grabs Pip and slows him down. Smooth ice has almost none — that's why he keeps sliding and sliding!";
    private const string CompletionMessage = "You did it! The rough gravel made friction and slowed me down right at my igloo!";
    private const string NextLevelPrompt = "Hmm... but what if I need to jump a big gap to get home? Maybe starting higher could help me fly farther!";

    private readonly Slot[] slots = new Slot[4];

    private DialogueManager dialogueManager;
    private LevelVictoryPopup victoryPopup;
    private TrackTrayLayout referenceTray;
    private PlayButtonPressed levelOnePlayButtonFlow;
    private Rigidbody2D penguinRb;
    private GameObject penguin;
    private PenguinFrictionBrakes brakes;
    private Button playButton;
    private Button replayButton;
    private TrackPaletteButton straightCard;
    private TrackPaletteButton gravelCard;
    private GameObject straightPrefab;
    private GameObject gravelPrefab;
    private Sprite straightPieceArt;
    private Sprite gravelPieceArt;
    private Sprite cueIconSprite;
    private Vector3 initialPenguinPosition;
    private Quaternion initialPenguinRotation;
    private float runwayStartX;
    private float fallY;
    private LevelState state;
    private GameObject activeDraggedPiece;
    private float homeStartX;
    private float homeEndX;
    private float cliffX;
    private float stuckTimer;
    private float homeZoneTimer;
    private bool hasStarted;
    private float applyStartingVelocityUntil;
    private bool showedGravelCue;
    private bool trayCardsWired;
    private Coroutine cueRoutine;
    private LayerMask trackLayerMask;

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name != SceneName)
        {
            enabled = false;
            return;
        }

        CacheLevelOneReferences();
        ConfigureIntroDialogue();
    }

    private IEnumerator Start()
    {
        yield return null;
        BuildLevel();
        ResetLevelState(true);
    }

    private void Update()
    {
        if (state == LevelState.Build)
        {
            UpdateSlotHighlight();
            UpdatePlayGate();
            return;
        }

        if (state != LevelState.Sliding || penguinRb == null)
        {
            return;
        }

        EvaluateSlide();
    }

    private void CacheLevelOneReferences()
    {
        dialogueManager = FindFirstObjectByType<DialogueManager>();
        victoryPopup = FindFirstObjectByType<LevelVictoryPopup>();
        TrackTrayLayout[] trays = FindObjectsByType<TrackTrayLayout>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        referenceTray = FindLevel2Tray(trays);
        penguin = GameObject.FindGameObjectWithTag("Player");
        penguinRb = penguin != null ? penguin.GetComponent<Rigidbody2D>() : null;
        levelOnePlayButtonFlow = penguin != null ? penguin.GetComponent<PlayButtonPressed>() : null;
        if (penguinRb != null)
        {
            gravityScale = penguinRb.gravityScale;
        }

        if (penguin != null)
        {
            initialPenguinPosition = penguin.transform.position;
            initialPenguinRotation = penguin.transform.rotation;
        }

        straightCard = FindPaletteCard("StraightIceCard");
        gravelCard = FindPaletteCard("GravelCard");
        straightPrefab = straightCard != null ? straightCard.piecePrefab : null;
        gravelPrefab = gravelCard != null ? gravelCard.piecePrefab : null;
        straightPieceArt = straightCard != null ? GetPrivateField<Sprite>(straightCard, "pieceArt") : null;
        gravelPieceArt = gravelCard != null ? GetPrivateField<Sprite>(gravelCard, "pieceArt") : null;
        CacheLevelOneLaunchSettings();
        ConfigureLevelOneTrayPopulation();
        if (dialogueManager != null)
        {
            cueIconSprite = GetPrivateField<Sprite>(dialogueManager, "speakerIconSprite");
        }
    }

    private void FixedUpdate()
    {
        if (hasStarted && penguinRb != null && penguinRb.simulated && Time.fixedTime <= applyStartingVelocityUntil)
        {
            ApplyStartingVelocity();
        }
    }

    private TrackPaletteButton FindPaletteCard(string objectName)
    {
        GameObject card = GameObject.Find(objectName);
        return card != null ? card.GetComponent<TrackPaletteButton>() : null;
    }

    private TrackTrayLayout FindLevel2Tray(TrackTrayLayout[] trays)
    {
        foreach (TrackTrayLayout tray in trays)
        {
            if (tray != null && tray.name == "Level2FrictionToolbar")
            {
                return tray;
            }
        }

        return trays.Length > 0 ? trays[0] : null;
    }

    private void CacheLevelOneLaunchSettings()
    {
        if (levelOnePlayButtonFlow == null)
        {
            return;
        }

        startingVelocity = GetPrivateValue(levelOnePlayButtonFlow, "startingVelocity", startingVelocity);
        startingImpulse = GetPrivateValue(levelOnePlayButtonFlow, "startingImpulse", startingImpulse);
        startingVelocityDuration = GetPrivateValue(levelOnePlayButtonFlow, "startingVelocityDuration", startingVelocityDuration);
    }

    private void ConfigureIntroDialogue()
    {
        if (dialogueManager == null)
        {
            return;
        }

        SetPrivateField(dialogueManager, "messages", new[] { IntroMessage });
        SetPrivateField(dialogueManager, "advanceButtonLabel", "Let's go!");
        SetPrivateField(dialogueManager, "showOnlyFirstMessage", true);
    }

    private void BuildLevel()
    {
        DisableLevelOneFlow();
        ConfigurePhysics();
        BindManualSceneObjects();
        ConfigureTrayUi();
    }

    private void DisableLevelOneFlow()
    {
        foreach (PlayButtonPressed play in FindObjectsByType<PlayButtonPressed>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            play.enabled = false;
        }

        foreach (goal_Indicator goal in FindObjectsByType<goal_Indicator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            goal.enabled = false;
        }

        foreach (SlideDialogueCue cue in FindObjectsByType<SlideDialogueCue>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            cue.enabled = false;
        }

        foreach (SlideLessonCard card in FindObjectsByType<SlideLessonCard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            card.enabled = false;
        }

    }

    private void ConfigurePhysics()
    {
        int trackLayer = LayerMask.NameToLayer("TrackPiece");
        trackLayerMask = trackLayer >= 0 ? 1 << trackLayer : ~0;
        if (penguinRb == null || penguin == null)
        {
            return;
        }

        penguinRb.gravityScale = gravityScale;
        penguinRb.linearDamping = 0f;
        penguinRb.angularDamping = 0.05f;
        penguinRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        penguinRb.simulated = false;

        brakes = penguin.GetComponent<PenguinFrictionBrakes>();
        if (brakes == null)
        {
            Debug.LogWarning("ballStartWithSpace needs a PenguinFrictionBrakes component for Level 2.", penguin);
            return;
        }

        brakes.Configure(trackLayerMask, 0.95f);
    }

    private void BindManualSceneObjects()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            GameObject visual = GameObject.Find(slotNamePrefix + (i + 1));
            slots[i] = new Slot
            {
                center = visual != null ? (Vector2)visual.transform.position : Vector2.zero,
                visual = visual
            };
        }

        runwayStartX = slots[0] != null && slots[0].visual != null ? slots[0].center.x : 0f;
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            fallY = mainCamera.transform.position.y - mainCamera.orthographicSize;
        }
        else
        {
            fallY = slots[0] != null && slots[0].visual != null ? slots[0].center.y - 20f : -20f;
        }

        GameObject home = GameObject.Find(homeObjectName);
        Collider2D homeCollider = home != null ? home.GetComponent<Collider2D>() : null;
        if (homeCollider != null)
        {
            homeStartX = homeCollider.bounds.min.x;
            homeEndX = homeCollider.bounds.max.x;
        }
        else if (home != null)
        {
            homeStartX = home.transform.position.x - 0.5f;
            homeEndX = home.transform.position.x + 0.5f;
        }
        else
        {
            Debug.LogWarning($"Level 2 needs a manually placed home object named '{homeObjectName}'.", this);
        }

        GameObject cliff = GameObject.Find(cliffObjectName);
        Collider2D cliffCollider = cliff != null ? cliff.GetComponent<Collider2D>() : null;
        if (cliffCollider != null)
        {
            cliffX = cliffCollider.bounds.min.x;
        }
        else if (cliff != null)
        {
            cliffX = cliff.transform.position.x;
        }
        else
        {
            Debug.LogWarning($"Level 2 needs a manually placed cliff object named '{cliffObjectName}'.", this);
        }
    }

    private void ConfigureTrayUi()
    {
        ConfigureLevelOneTrayPopulation();
        straightCard = straightCard != null ? straightCard : FindPaletteCard("StraightIceCard");
        gravelCard = gravelCard != null ? gravelCard : FindPaletteCard("GravelCard");
        playButton = FindButton("PlayButtonToStartBall");
        replayButton = FindButton("ResetButton_0");
        if (!trayCardsWired)
        {
            WirePaletteCard(straightCard, TileKind.Ice);
            WirePaletteCard(gravelCard, TileKind.Gravel);
            trayCardsWired = straightCard != null && gravelCard != null;
        }

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(StartSlide);
        }

        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(() => ResetLevelState(false));
        }

        UpdatePlayGate();
    }

    private void ConfigureLevelOneTrayPopulation()
    {
        if (referenceTray == null)
        {
            return;
        }

        GameObject sourceStraightPrefab = straightPrefab != null ? straightPrefab : FindTrayPrefab("Straight ice");
        GameObject sourceGravelPrefab = gravelPrefab != null ? gravelPrefab : FindTrayPrefab("Gravel");
        if (sourceGravelPrefab == null)
        {
            sourceGravelPrefab = FindTrayPrefab("Gentle gravel");
        }

        Sprite straightSprite = straightPieceArt != null ? straightPieceArt : FindTrayArt("Straight ice");
        Sprite gravelSprite = gravelPieceArt != null ? gravelPieceArt : FindTrayArt("Gravel");
        if (gravelSprite == null)
        {
            gravelSprite = FindTrayArt("Gentle gravel");
        }

        TrackTrayLayout.TrayItem straight = new TrackTrayLayout.TrayItem
        {
            objectName = "StraightIceCard",
            piecePrefab = sourceStraightPrefab,
            pieceArt = straightSprite,
            title = "Straight ice",
            showBadge = true,
            badgeText = "fast",
            showArtTray = false
        };

        TrackTrayLayout.TrayItem gravel = new TrackTrayLayout.TrayItem
        {
            objectName = "GravelCard",
            piecePrefab = sourceGravelPrefab != null ? sourceGravelPrefab : sourceStraightPrefab,
            pieceArt = gravelSprite != null ? gravelSprite : straightSprite,
            title = "Gravel",
            showBadge = true,
            badgeText = "slows you down",
            showArtTray = false,
            badgeColor = new Color32(148, 111, 62, 255),
            badgeTextColor = Color.white
        };

        SetPrivateField(referenceTray, "items", new[] { straight, gravel });
        SetPrivateField(referenceTray, "showUndoButton", false);
        referenceTray.gameObject.SetActive(true);
        referenceTray.Refresh();

        straightCard = FindPaletteCard("StraightIceCard");
        gravelCard = FindPaletteCard("GravelCard");
        straightPrefab = straightCard != null ? straightCard.piecePrefab : sourceStraightPrefab;
        gravelPrefab = gravelCard != null ? gravelCard.piecePrefab : sourceGravelPrefab;
        straightPieceArt = straightCard != null ? GetPrivateField<Sprite>(straightCard, "pieceArt") : straightSprite;
        gravelPieceArt = gravelCard != null ? GetPrivateField<Sprite>(gravelCard, "pieceArt") : gravelSprite;
    }

    private GameObject FindTrayPrefab(string title)
    {
        TrackTrayLayout.TrayItem item = FindTrayItem(title);
        return item != null ? item.piecePrefab : null;
    }

    private Sprite FindTrayArt(string title)
    {
        TrackTrayLayout.TrayItem item = FindTrayItem(title);
        return item != null ? item.pieceArt : null;
    }

    private TrackTrayLayout.TrayItem FindTrayItem(string title)
    {
        if (referenceTray == null)
        {
            return null;
        }

        FieldInfo field = typeof(TrackTrayLayout).GetField("items", BindingFlags.Instance | BindingFlags.NonPublic);
        TrackTrayLayout.TrayItem[] items = field != null ? field.GetValue(referenceTray) as TrackTrayLayout.TrayItem[] : null;
        if (items == null)
        {
            return null;
        }

        foreach (TrackTrayLayout.TrayItem item in items)
        {
            if (item != null && item.title == title)
            {
                return item;
            }
        }

        return null;
    }

    private void WirePaletteCard(TrackPaletteButton card, TileKind kind)
    {
        if (card == null)
        {
            return;
        }

        card.worldCamera = Camera.main;
        card.pieceSpawned.AddListener(piece => RegisterSpawnedPiece(piece, kind));
        card.dragEnded.AddListener(FinishActiveDraggedPieceDrag);
    }

    private void RegisterSpawnedPiece(GameObject piece, TileKind kind)
    {
        activeDraggedPiece = piece;

        ConfigureSpawnedTile(piece, kind);
    }

    private void ConfigureSpawnedTile(GameObject piece, TileKind kind)
    {
        piece.name = kind == TileKind.Gravel ? "GravelTrackPiece(Clone)" : "StraightIceTrackPiece(Clone)";
        piece.layer = LayerMask.NameToLayer("TrackPiece");

        SpriteRenderer renderer = piece.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 3;
        }

        BoxCollider2D collider = piece.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.enabled = true;
            collider.isTrigger = false;
        }

        TrackTile tile = piece.GetComponent<TrackTile>();
        if (tile != null)
        {
            tile.decel = kind == TileKind.Gravel ? gravelDecel : 0f;
            tile.isGravel = kind == TileKind.Gravel;
            tile.tileLabel = kind == TileKind.Gravel ? "Gravel" : "Straight Ice";
        }
    }

    private void FinishActiveDraggedPieceDrag()
    {
        SnapActiveDraggedPieceToNearbySlot();
        activeDraggedPiece = null;
        ClearSlotHighlights();
        UpdatePlayGate();
    }

    private void SnapActiveDraggedPieceToNearbySlot()
    {
        if (activeDraggedPiece == null)
        {
            return;
        }

        int slotIndex = FindNearestSlot(activeDraggedPiece.transform.position, 1.25f);
        if (slotIndex < 0)
        {
            return;
        }

        Slot slot = slots[slotIndex];
        slot.tile = activeDraggedPiece;
        slot.trackTile = activeDraggedPiece.GetComponent<TrackTile>();
        activeDraggedPiece.transform.position = new Vector3(slot.center.x, slot.center.y, 0f);
        activeDraggedPiece.transform.rotation = Quaternion.identity;
    }

    private int FindNearestSlot(Vector3 position, float maxDistance)
    {
        int best = -1;
        float bestDistance = maxDistance;
        for (int i = 0; i < slots.Length; i++)
        {
            float distance = Vector2.Distance(position, slots[i].center);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }

    private void UpdateSlotHighlight()
    {
        if (activeDraggedPiece == null)
        {
            ClearSlotHighlights();
            return;
        }

        int active = FindNearestSlot(activeDraggedPiece.transform.position, 0.9f);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].visual == null)
            {
                continue;
            }

            Color color = i == active && slots[i].tile == null ? new Color32(255, 199, 55, 255) : new Color32(61, 137, 190, 190);
            ApplySlotVisualColor(slots[i].visual, color);
        }
    }

    private void ClearSlotHighlights()
    {
        foreach (Slot slot in slots)
        {
            if (slot == null || slot.visual == null)
            {
                continue;
            }

            ApplySlotVisualColor(slot.visual, new Color32(61, 137, 190, 190));
        }
    }

    private void ApplySlotVisualColor(GameObject visual, Color color)
    {
        LineRenderer line = visual.GetComponent<LineRenderer>();
        if (line != null)
        {
            line.startColor = color;
            line.endColor = color;
        }

        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Color fillColor = color;
            bool highlighted = color.r > 0.9f && color.g > 0.6f;
            fillColor.a = highlighted ? 0.32f : 0.18f;
            renderer.color = fillColor;
        }
    }

    private void StartSlide()
    {
        if (state != LevelState.Build || penguinRb == null)
        {
            return;
        }

        state = LevelState.Sliding;
        hasStarted = true;
        showedGravelCue = false;
        stuckTimer = 0f;
        homeZoneTimer = 0f;
        SetDraggingEnabled(false);
        UpdatePlayGate();

        dialogueManager?.HideDialogue();
        penguinRb.simulated = true;
        penguinRb.WakeUp();
        applyStartingVelocityUntil = Time.fixedTime + startingVelocityDuration;
        penguinRb.AddForce(Vector2.right * startingImpulse, ForceMode2D.Impulse);
        ApplyStartingVelocity();
        ShowCue(IceCue, 1.5f);
    }

    private void EvaluateSlide()
    {
        Vector2 pos = penguin.transform.position;
        float speed = penguinRb.linearVelocity.magnitude;

        TrackTile currentTile = brakes != null ? brakes.CurrentTile : null;
        if (!showedGravelCue && currentTile != null && currentTile.isGravel)
        {
            showedGravelCue = true;
            ShowCue(GravelCue, 1.2f);
        }

        bool inHomeZone = pos.x >= homeStartX && pos.x <= homeEndX;
        if (inHomeZone)
        {
            homeZoneTimer += Time.deltaTime;
            if (homeZoneTimer >= goalHoldDuration)
            {
                CompleteWin();
                return;
            }
        }
        else
        {
            homeZoneTimer = 0f;
        }

        if (pos.x > cliffX || pos.y < fallY)
        {
            CompleteFail(OvershootFail);
            return;
        }

        if (speed < stuckSpeedThreshold && pos.x < homeStartX)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckDuration)
            {
                CompleteFail(StuckFail);
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    private void CompleteWin()
    {
        state = LevelState.Complete;
        PenguinLevelProgressService.CompleteLevel(2);
        StopPenguin();
        ShowCue(HomeCue, 1.1f);
        StartCoroutine(ShowWinAfterDelay());
    }

    private IEnumerator ShowWinAfterDelay()
    {
        yield return new WaitForSeconds(1.15f);

        if (victoryPopup == null)
        {
            Debug.LogWarning("Level 2 needs a manually placed LevelVictoryPopup component.", this);
            yield break;
        }

        SetPrivateField(victoryPopup, "gravityTitle", "Friction slows Pip down");
        SetPrivateField(victoryPopup, "gravityDescription", TeachingBody);
        SetPrivateField(victoryPopup, "gravityLabel", "Rough gravel slows Pip down");
        victoryPopup.Show(
            "",
            "Pip made it home!",
            new[]
            {
                "Rough gravel creates friction",
                "Put gravel where you want to stop"
            },
            null,
            CompletionMessage,
            NextLevelPrompt);
    }

    private void CompleteFail(string message)
    {
        if (failureDialoguesPaused)
        {
            return;
        }

        state = LevelState.Complete;
        StopPenguin();
        dialogueManager?.ShowFailureCard("", message, "Try again", () => ResetLevelState(false));
    }

    private void StopPenguin()
    {
        if (penguinRb == null)
        {
            return;
        }

        penguinRb.linearVelocity = Vector2.zero;
        penguinRb.angularVelocity = 0f;
        penguinRb.simulated = false;
        hasStarted = false;
    }

    private void ResetLevelState(bool firstBuild)
    {
        state = LevelState.Build;
        StopPenguin();
        DialogueManager.SetExternalDialogueOpen(false);

        if (penguin != null)
        {
            penguin.transform.position = initialPenguinPosition;
            penguin.transform.rotation = initialPenguinRotation;
        }

        if (referenceTray != null)
        {
            foreach (GameObject spawnedPiece in referenceTray.GetSpawnedPiecesSnapshot())
            {
                if (spawnedPiece != null)
                {
                    Destroy(spawnedPiece);
                }
            }
        }

        foreach (Slot slot in slots)
        {
            if (slot == null)
            {
                continue;
            }

            if (slot.tile != null)
            {
                Destroy(slot.tile);
            }

            slot.tile = null;
            slot.trackTile = null;
        }

        activeDraggedPiece = null;
        stuckTimer = 0f;
        homeZoneTimer = 0f;
        hasStarted = false;
        SetDraggingEnabled(true);
        UpdatePlayGate();

        if (!firstBuild)
        {
            dialogueManager?.HideFailureCard();
            dialogueManager?.HideSlideCue();
        }
    }

    private void UpdatePlayGate()
    {
        if (playButton != null)
        {
            playButton.interactable = state == LevelState.Build;
        }
    }

    private void SetDraggingEnabled(bool enabled)
    {
        if (straightCard != null)
        {
            straightCard.enabled = enabled;
        }

        if (gravelCard != null)
        {
            gravelCard.enabled = enabled;
        }
    }

    private void ShowCue(string message, float duration)
    {
        if (dialogueManager == null || penguin == null)
        {
            return;
        }

        if (cueRoutine != null)
        {
            StopCoroutine(cueRoutine);
        }

        dialogueManager.ShowSlideCue(message, penguin.transform, cueIconSprite);
        cueRoutine = StartCoroutine(HideCueAfter(duration));
    }

    private IEnumerator HideCueAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        dialogueManager?.HideSlideCue();
    }

    private void ApplyStartingVelocity()
    {
        Vector2 velocity = penguinRb.linearVelocity;
        velocity.x = Mathf.Max(velocity.x, startingVelocity.x);

        if (!Mathf.Approximately(startingVelocity.y, 0f))
        {
            velocity.y = startingVelocity.y;
        }

        penguinRb.linearVelocity = velocity;
    }

    private Button FindButton(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private void SetPrivateField<T>(UnityEngine.Object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }

    private T GetPrivateField<T>(UnityEngine.Object target, string fieldName) where T : class
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return field != null ? field.GetValue(target) as T : null;
    }

    private T GetPrivateValue<T>(UnityEngine.Object target, string fieldName, T fallback) where T : struct
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            return fallback;
        }

        object value = field.GetValue(target);
        return value is T typed ? typed : fallback;
    }
}
