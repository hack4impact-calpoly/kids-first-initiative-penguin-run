using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A soft pulsing glow that draws a child's eye to something they can interact with.
///
/// Ported from the States of Matter game, where it marks the parts of the wiring puzzle a learner
/// should touch next. Kept deliberately identical in behaviour so a child moving between the two
/// games meets the same visual language: a thing that glows is a thing you can use.
///
/// Builds its own radial sprite at runtime, so it needs no art asset and can be attached to any
/// object with a SpriteRenderer or a UI Graphic.
/// </summary>
public class AttentionHighlight : MonoBehaviour
{
    [SerializeField] private Color highlightColor = new Color(1f, 0.94f, 0.02f, 1f);
    [SerializeField] private float pulseSpeed = 3.2f;
    [SerializeField] private float minAlpha = 0.2f;
    [SerializeField] private float maxAlpha = 0.56f;
    [SerializeField] private float worldGlowScale = 1.34f;
    [SerializeField] private float uiGlowPadding = 12f;
    [SerializeField] private bool includeChildSpriteRenderers = true;

    private readonly List<SpriteRenderer> worldGlows = new List<SpriteRenderer>();
    private readonly List<Graphic> uiGlows = new List<Graphic>();
    private static Sprite worldGlowSprite;
    private bool isShowing;
    private bool hasBuiltGlows;

    private void Update()
    {
        if (!isShowing)
            return;

        float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);
        Color color = highlightColor;
        color.a = alpha;

        for (int i = 0; i < worldGlows.Count; i++)
        {
            if (worldGlows[i] != null)
                worldGlows[i].color = color;
        }

        for (int i = 0; i < uiGlows.Count; i++)
        {
            if (uiGlows[i] != null)
                uiGlows[i].color = color;
        }
    }

    public void Show()
    {
        EnsureGlows();
        isShowing = true;
        SetGlowObjectsActive(true);
    }

    public void Hide()
    {
        isShowing = false;
        SetGlowObjectsActive(false);
    }

    private void EnsureGlows()
    {
        if (hasBuiltGlows)
            return;

        BuildWorldGlows();
        BuildUiGlows();
        hasBuiltGlows = true;
    }

    private void BuildWorldGlows()
    {
        EnsureWorldGlowSprite();

        SpriteRenderer[] renderers = includeChildSpriteRenderers
            ? GetComponentsInChildren<SpriteRenderer>()
            : GetComponents<SpriteRenderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = default;
        bool hasBounds = false;
        int sortingLayerId = renderers[0].sortingLayerID;
        int sortingOrder = renderers[0].sortingOrder;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer source = renderers[i];
            if (source == null || source.sprite == null || source.gameObject.name.Contains("Attention Glow"))
                continue;

            if (!hasBounds)
            {
                bounds = source.bounds;
                sortingLayerId = source.sortingLayerID;
                sortingOrder = source.sortingOrder;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(source.bounds);
                sortingOrder = Mathf.Min(sortingOrder, source.sortingOrder);
            }
        }

        if (!hasBounds)
            return;

        var glowObject = new GameObject($"{gameObject.name} Attention Glow", typeof(SpriteRenderer));
        glowObject.transform.position = new Vector3(bounds.center.x, bounds.center.y, transform.position.z + 0.04f);
        glowObject.transform.rotation = Quaternion.identity;
        glowObject.transform.localScale = new Vector3(bounds.size.x * worldGlowScale, bounds.size.y * worldGlowScale, 1f);
        glowObject.transform.SetParent(transform, true);

        SpriteRenderer glow = glowObject.GetComponent<SpriteRenderer>();
        glow.sprite = worldGlowSprite;
        glow.sortingLayerID = sortingLayerId;
        glow.sortingOrder = sortingOrder - 1;
        glow.color = Color.clear;

        worldGlows.Add(glow);
        glowObject.SetActive(false);
    }

    private static void EnsureWorldGlowSprite()
    {
        if (worldGlowSprite != null)
            return;

        const int size = 96;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1f) * 0.5f, (size - 1f) * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float normalized = Mathf.Clamp01(1f - distance / radius);
                float alpha = Mathf.SmoothStep(0f, 1f, normalized);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        worldGlowSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private void BuildUiGlows()
    {
        EnsureWorldGlowSprite();

        RectTransform targetRect = transform as RectTransform;
        if (targetRect == null || targetRect.parent == null)
            return;

        var glowObject = new GameObject($"{gameObject.name} Attention Glow", typeof(RectTransform), typeof(Image));
        glowObject.transform.SetParent(targetRect.parent, false);

        RectTransform glowRect = glowObject.GetComponent<RectTransform>();
        glowRect.anchorMin = targetRect.anchorMin;
        glowRect.anchorMax = targetRect.anchorMax;
        glowRect.pivot = targetRect.pivot;
        glowRect.anchoredPosition = targetRect.anchoredPosition;
        glowRect.localRotation = targetRect.localRotation;
        glowRect.localScale = targetRect.localScale;

        if (targetRect.anchorMin == targetRect.anchorMax)
        {
            glowRect.sizeDelta = targetRect.sizeDelta + Vector2.one * (uiGlowPadding * 2f);
        }
        else
        {
            glowRect.offsetMin = targetRect.offsetMin - Vector2.one * uiGlowPadding;
            glowRect.offsetMax = targetRect.offsetMax + Vector2.one * uiGlowPadding;
        }

        int targetSiblingIndex = targetRect.GetSiblingIndex();
        glowObject.transform.SetSiblingIndex(targetSiblingIndex);

        Image glow = glowObject.GetComponent<Image>();
        glow.sprite = worldGlowSprite;
        glow.raycastTarget = false;
        glow.color = Color.clear;

        uiGlows.Add(glow);
        glowObject.SetActive(false);
    }

    private void SetGlowObjectsActive(bool active)
    {
        for (int i = 0; i < worldGlows.Count; i++)
        {
            if (worldGlows[i] != null)
                worldGlows[i].gameObject.SetActive(active);
        }

        for (int i = 0; i < uiGlows.Count; i++)
        {
            if (uiGlows[i] != null)
                uiGlows[i].gameObject.SetActive(active);
        }
    }

}
