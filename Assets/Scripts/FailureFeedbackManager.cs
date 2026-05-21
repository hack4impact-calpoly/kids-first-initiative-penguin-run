using System.Collections.Generic;
using UnityEngine;

public class FailureFeedbackManager : MonoBehaviour
{
    public enum FailureState
    {
        None,
        NoPieces,
        GapLeft
    }

    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private TrackTrayLayout trackTray;
    [SerializeField] private Transform iglooTransform;
    [SerializeField] private Color ghostColor = new Color(0.25f, 0.72f, 1f, 0.65f);

    private PlayButtonPressed playButton;
    private FailureState lastFailureState = FailureState.None;
    private int repeatedFailureCount;
    private GameObject ghostPieceHint;
    private static Sprite dotSprite;

    public int PlacedPieceCount
    {
        get
        {
            EnsureReferences();
            return trackTray != null ? trackTray.SpawnedPieceCount : FindObjectsByType<SnapPiece>(FindObjectsSortMode.None).Length;
        }
    }

    public void Initialize(PlayButtonPressed owner)
    {
        playButton = owner;
        EnsureReferences();
    }

    public void NotifyPieceSpawned()
    {
        HideVisualHints();
    }

    public void ShowFailure(FailureState state)
    {
        EnsureReferences();
        RecordFailure(state);
        ShowRepeatHintIfNeeded(state);

        if (dialogueManager == null)
        {
            return;
        }

        switch (state)
        {
            case FailureState.NoPieces:
                dialogueManager.ShowFailureCard(
                    "Oops - I need a path first!",
                    "I can't slide on thin air! Grab a track piece from the bottom and drag it between me and my igloo.",
                    "Try again",
                    ResetForBuilding);
                break;

            case FailureState.GapLeft:
                dialogueManager.ShowFailureCard(
                    "So close! Almost there!",
                    "I ran out of path before I got home! Can you add one more track piece to close the gap?",
                    "Keep building",
                    ResetForBuilding,
                    "Watch again",
                    ReplayCurrentBuild);
                break;
        }
    }

    public void HideVisualHints()
    {
        if (ghostPieceHint != null)
        {
            Destroy(ghostPieceHint);
            ghostPieceHint = null;
        }
    }

    private void RecordFailure(FailureState state)
    {
        if (lastFailureState == state)
        {
            repeatedFailureCount++;
            return;
        }

        HideVisualHints();
        lastFailureState = state;
        repeatedFailureCount = 1;
    }

    private void ShowRepeatHintIfNeeded(FailureState state)
    {
        if (repeatedFailureCount < 2)
        {
            return;
        }

        HideVisualHints();
        switch (state)
        {
            case FailureState.NoPieces:
                if (trackTray != null)
                {
                    trackTray.PulseTrayHint();
                }
                break;

            case FailureState.GapLeft:
                ShowGhostPieceHint();
                break;
        }
    }

    private void ResetForBuilding()
    {
        playButton?.ResetPenguinForBuilding();
    }

    private void ReplayCurrentBuild()
    {
        playButton?.ReplayCurrentBuild();
    }

    private void EnsureReferences()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindFirstObjectByType<DialogueManager>();
        }

        if (trackTray == null)
        {
            trackTray = FindFirstObjectByType<TrackTrayLayout>();
        }

        if (iglooTransform == null)
        {
            goal_Indicator goal = FindFirstObjectByType<goal_Indicator>();
            if (goal != null)
            {
                iglooTransform = goal.transform;
            }
        }

        if (iglooTransform == null)
        {
            GameObject finish = GameObject.FindGameObjectWithTag("Finish");
            if (finish != null)
            {
                iglooTransform = finish.transform;
            }
        }
    }

    private void ShowGhostPieceHint()
    {
        HideGhostPieceHint();

        Transform lastPiece = FindFarthestPlacedPiece();
        if (lastPiece == null || iglooTransform == null)
        {
            return;
        }

        Bounds lastBounds = GetWorldBounds(lastPiece);
        Bounds iglooBounds = GetWorldBounds(iglooTransform);
        Vector3 from = new Vector3(lastBounds.max.x, lastBounds.center.y, lastPiece.position.z - 0.1f);
        Vector3 to = new Vector3(iglooBounds.min.x, iglooBounds.center.y, from.z);
        Vector3 direction = to - from;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector3.right;
        }

        float gapDistance = direction.magnitude;
        Vector3 center = from + direction * 0.5f;
        Vector2 size = new Vector2(
            Mathf.Clamp(gapDistance * 0.65f, lastBounds.size.x * 0.55f, lastBounds.size.x * 1.2f),
            Mathf.Clamp(lastBounds.size.y * 0.65f, 70f, 180f));

        ghostPieceHint = new GameObject("FailureGhostTrackPieceHint");
        ghostPieceHint.transform.position = center;
        ghostPieceHint.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        AddDottedRectangle(ghostPieceHint.transform, size, Mathf.Max(12f, size.y * 0.13f), ghostColor);
    }

    private void HideGhostPieceHint()
    {
        if (ghostPieceHint != null)
        {
            Destroy(ghostPieceHint);
            ghostPieceHint = null;
        }
    }

    private Transform FindFarthestPlacedPiece()
    {
        EnsureReferences();
        List<GameObject> pieces = trackTray != null ? trackTray.GetSpawnedPiecesSnapshot() : null;
        Transform best = null;
        float bestX = float.NegativeInfinity;

        if (pieces != null)
        {
            foreach (GameObject piece in pieces)
            {
                if (piece == null)
                {
                    continue;
                }

                Bounds bounds = GetWorldBounds(piece.transform);
                if (bounds.max.x > bestX)
                {
                    bestX = bounds.max.x;
                    best = piece.transform;
                }
            }
        }

        if (best != null)
        {
            return best;
        }

        foreach (SnapPiece piece in FindObjectsByType<SnapPiece>(FindObjectsSortMode.None))
        {
            if (piece == null || !piece.gameObject.activeInHierarchy)
            {
                continue;
            }

            Bounds bounds = GetWorldBounds(piece.transform);
            if (bounds.max.x > bestX)
            {
                bestX = bounds.max.x;
                best = piece.transform;
            }
        }

        return best;
    }

    private void AddDottedRectangle(Transform parent, Vector2 size, float dotSize, Color color)
    {
        int horizontalDots = Mathf.Max(4, Mathf.RoundToInt(size.x / (dotSize * 2.2f)));
        int verticalDots = Mathf.Max(2, Mathf.RoundToInt(size.y / (dotSize * 2.2f)));

        for (int i = 0; i < horizontalDots; i++)
        {
            float t = horizontalDots == 1 ? 0.5f : i / (float)(horizontalDots - 1);
            float x = Mathf.Lerp(-size.x * 0.5f, size.x * 0.5f, t);
            CreateDot(parent, new Vector3(x, size.y * 0.5f, 0f), dotSize, color);
            CreateDot(parent, new Vector3(x, -size.y * 0.5f, 0f), dotSize, color);
        }

        for (int i = 1; i < verticalDots - 1; i++)
        {
            float t = i / (float)(verticalDots - 1);
            float y = Mathf.Lerp(-size.y * 0.5f, size.y * 0.5f, t);
            CreateDot(parent, new Vector3(-size.x * 0.5f, y, 0f), dotSize, color);
            CreateDot(parent, new Vector3(size.x * 0.5f, y, 0f), dotSize, color);
        }
    }

    private void CreateDot(Transform parent, Vector3 localPosition, float size, Color color)
    {
        GameObject dot = new GameObject("GhostDot");
        dot.transform.SetParent(parent, false);
        dot.transform.localPosition = localPosition;
        dot.transform.localScale = Vector3.one * size;

        SpriteRenderer renderer = dot.AddComponent<SpriteRenderer>();
        renderer.sprite = GetDotSprite();
        renderer.color = color;
        renderer.sortingOrder = 100;
    }

    private Bounds GetWorldBounds(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        return new Bounds(target.position, Vector3.one * 120f);
    }

    private Sprite GetDotSprite()
    {
        if (dotSprite != null)
        {
            return dotSprite;
        }

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Generated Failure Hint Dot",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.42f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * size + x] = distance <= radius ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        dotSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return dotSprite;
    }
}
