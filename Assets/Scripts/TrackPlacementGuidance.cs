using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Teaches track placement by showing, not telling.
///
/// Building a track has two steps a child has to discover on their own: that pieces are dragged out
/// of the tray, and that they connect at specific points. Neither was visible — the tray looked like
/// decoration, and snap points had no on-screen presence at all, only an editor gizmo.
///
/// This follows the approach the States of Matter wiring puzzle already uses: glow the thing the
/// learner should touch, at the moment it becomes relevant, and stop once they have done it. The
/// glow component itself is the same <see cref="AttentionHighlight"/>, so a child moving between the
/// two games meets one consistent idea — a thing that glows is a thing you can use.
///
/// Guidance runs in three states:
/// <list type="bullet">
/// <item>Nothing placed yet — the tray glows, because that is where a track starts.</item>
/// <item>A piece is being dragged — reachable connection points glow, because that is the step a
/// child cannot guess.</item>
/// <item>A piece has connected — guidance stops, and stays off on later visits to this level.</item>
/// </list>
/// </summary>
public class TrackPlacementGuidance : MonoBehaviour
{
    [Header("Behaviour")]
    [Tooltip("Glow the track tray until the child has connected their first piece.")]
    public bool guidePalette = true;

    [Tooltip("Glow reachable connection points while a piece is being dragged.")]
    public bool guideSnapPoints = true;

    [Tooltip("Stop guiding once a piece has been connected, and stay quiet on later visits.")]
    public bool stopAfterFirstSuccess = true;

    [Tooltip("Guide again every visit. Useful while tuning; leave off for players.")]
    public bool ignoreSavedProgress;

    [Header("Snap Point Glow")]
    [Tooltip("How far beyond the actual snap radius a connection point starts glowing, so a child sees where to aim before they are already there.")]
    public float snapPreviewMargin = 2.5f;

    [Tooltip("Most connection points to glow at once. Lighting up the whole level is noise, not guidance.")]
    public int maxHighlightedSnapPoints = 3;

    [Tooltip("Seconds between rebuilds of the snap point list while dragging.")]
    public float snapScanInterval = 0.15f;

    private static TrackPlacementGuidance instance;

    private readonly List<AttentionHighlight> paletteHighlights = new List<AttentionHighlight>();
    private readonly List<AttentionHighlight> activeSnapHighlights = new List<AttentionHighlight>();
    private readonly Dictionary<Transform, AttentionHighlight> snapPointHighlights =
        new Dictionary<Transform, AttentionHighlight>();

    private static Sprite markerSprite;

    private SnapPiece[] scenePieces;
    private float nextSnapScanTime;
    private bool hasConnectedAPiece;
    private bool paletteGlowing;

    /// <summary>
    /// Creates the guidance object on any scene that has track pieces to place.
    ///
    /// This is a MonoBehaviour, so without something to attach it to it would never run — and
    /// attaching it by hand would mean editing every track level's scene and remembering to do it
    /// again for the next one. PipLauncher and PenguinLevelProgressService bootstrap themselves for
    /// the same reason; this follows them.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeBootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BootstrapForScene();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BootstrapForScene();
    }

    private static void BootstrapForScene()
    {
        if (instance != null) return;

        // Only levels a child builds a track on need this. Checking for the pieces themselves keeps
        // it out of menus and the Potential Energy level without hard-coding scene names.
        bool hasTrackPieces =
            FindObjectsByType<PaletteItem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0
            || FindObjectsByType<SnapPiece>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length > 0;

        if (!hasTrackPieces) return;

        new GameObject(nameof(TrackPlacementGuidance)).AddComponent<TrackPlacementGuidance>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Domain reload can be disabled in the editor, which would otherwise carry a destroyed
        // instance across play sessions and stop the guidance ever appearing again.
        instance = null;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        hasConnectedAPiece = !ignoreSavedProgress && HasSucceededBefore();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Start()
    {
        CachePalette();
        RefreshScenePieces();
    }

    private void Update()
    {
        if (DialogueManager.IsDialogueOpen)
        {
            // A dialogue already has the child's attention; a second thing pulsing behind it competes.
            HideAllGuidance();
            return;
        }

        DragPlacedPiece dragged = DragPlacedPiece.ActiveDrag;

        if (dragged != null)
        {
            SetPaletteGlow(false);
            if (guideSnapPoints) UpdateSnapPointGuidance(dragged);
            return;
        }

        HideSnapPointGuidance();
        SetPaletteGlow(guidePalette && !IsFinished());
    }

    /// <summary>
    /// Called when a drag finishes. A successful connection is the moment the child has understood
    /// the mechanic, so it is what retires the guidance.
    /// </summary>
    public static void NotifyDragEnded(bool snapped)
    {
        if (instance == null) return;

        instance.HideSnapPointGuidance();
        instance.RefreshScenePieces();

        if (!snapped) return;

        instance.hasConnectedAPiece = true;
        if (instance.stopAfterFirstSuccess) instance.MarkSucceeded();
    }

    private bool IsFinished()
    {
        return stopAfterFirstSuccess && hasConnectedAPiece;
    }

    // ----- palette -----

    private void CachePalette()
    {
        paletteHighlights.Clear();

        foreach (PaletteItem item in FindObjectsByType<PaletteItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            paletteHighlights.Add(EnsureHighlight(item.gameObject));
        }
    }

    private void SetPaletteGlow(bool show)
    {
        if (paletteGlowing == show) return;
        paletteGlowing = show;

        foreach (AttentionHighlight highlight in paletteHighlights)
        {
            if (highlight == null) continue;
            if (show) highlight.Show();
            else highlight.Hide();
        }
    }

    // ----- snap points -----

    private void RefreshScenePieces()
    {
        scenePieces = FindObjectsByType<SnapPiece>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        nextSnapScanTime = 0f;
    }

    private void UpdateSnapPointGuidance(DragPlacedPiece dragged)
    {
        if (Time.unscaledTime < nextSnapScanTime) return;
        nextSnapScanTime = Time.unscaledTime + Mathf.Max(0.02f, snapScanInterval);

        SnapPiece draggedPiece = dragged.GetComponent<SnapPiece>();
        if (draggedPiece == null || draggedPiece.snapPoints == null)
        {
            HideSnapPointGuidance();
            return;
        }

        if (scenePieces == null) RefreshScenePieces();

        float reach = draggedPiece.snapRadius * Mathf.Abs(draggedPiece.transform.lossyScale.x) + snapPreviewMargin;
        List<(Transform point, float distance)> candidates = new List<(Transform, float)>();

        foreach (SnapPiece other in scenePieces)
        {
            if (other == null || other == draggedPiece || other.snapPoints == null) continue;
            if (other.transform.IsChildOf(draggedPiece.transform)) continue;

            foreach (Transform otherPoint in other.snapPoints)
            {
                if (otherPoint == null) continue;

                float nearest = float.MaxValue;
                foreach (Transform myPoint in draggedPiece.snapPoints)
                {
                    if (myPoint == null) continue;
                    nearest = Mathf.Min(nearest, Vector2.Distance(myPoint.position, otherPoint.position));
                }

                if (nearest <= reach) candidates.Add((otherPoint, nearest));
            }
        }

        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

        HideSnapPointGuidance();
        int shown = Mathf.Min(candidates.Count, Mathf.Max(1, maxHighlightedSnapPoints));
        for (int i = 0; i < shown; i++)
        {
            AttentionHighlight highlight = EnsureSnapHighlight(candidates[i].point);
            if (highlight == null) continue;

            highlight.Show();
            activeSnapHighlights.Add(highlight);
        }
    }

    private void HideSnapPointGuidance()
    {
        foreach (AttentionHighlight highlight in activeSnapHighlights)
        {
            if (highlight != null) highlight.Hide();
        }

        activeSnapHighlights.Clear();
    }

    private void HideAllGuidance()
    {
        HideSnapPointGuidance();
        SetPaletteGlow(false);
    }

    /// <summary>
    /// Snap points are usually bare transforms with nothing to draw, so the glow needs a sprite of
    /// its own to size itself against. One is created per point and reused.
    /// </summary>
    private AttentionHighlight EnsureSnapHighlight(Transform snapPoint)
    {
        if (snapPoint == null) return null;

        if (snapPointHighlights.TryGetValue(snapPoint, out AttentionHighlight existing) && existing != null)
        {
            return existing;
        }

        GameObject marker = new GameObject("SnapPointGuide");
        marker.transform.SetParent(snapPoint, false);
        marker.transform.localPosition = Vector3.zero;

        // AttentionHighlight sizes its glow from the bounds of a SpriteRenderer and skips any
        // renderer without a sprite, so the marker carries an invisible one purely to give the glow
        // something to measure. Scaled to the snap radius so the glow reads as "connects here"
        // rather than as a dot.
        SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
        renderer.sprite = EnsureMarkerSprite();
        renderer.color = Color.clear;
        renderer.sortingOrder = 0;

        float radius = Mathf.Max(0.5f, snapPreviewMargin);
        marker.transform.localScale = new Vector3(radius, radius, 1f);

        AttentionHighlight highlight = marker.AddComponent<AttentionHighlight>();
        snapPointHighlights[snapPoint] = highlight;
        return highlight;
    }

    /// <summary>
    /// A single white pixel, shared by every snap marker. It is never seen — the renderer's colour is
    /// clear — and exists only so the glow has bounds to size itself against.
    /// </summary>
    private static Sprite EnsureMarkerSprite()
    {
        if (markerSprite != null) return markerSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "SnapPointGuideMarker",
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        markerSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return markerSprite;
    }

    private AttentionHighlight EnsureHighlight(GameObject target)
    {
        AttentionHighlight existing = target.GetComponent<AttentionHighlight>();
        return existing != null ? existing : target.AddComponent<AttentionHighlight>();
    }

    // ----- persistence -----

    private string ProgressKey =>
        $"PenguinRun.TrackGuidanceComplete.{SceneManager.GetActiveScene().name}";

    private bool HasSucceededBefore() => PlayerPrefs.GetInt(ProgressKey, 0) == 1;

    private void MarkSucceeded()
    {
        PlayerPrefs.SetInt(ProgressKey, 1);
        PlayerPrefs.Save();
    }
}
