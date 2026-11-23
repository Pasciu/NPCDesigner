using System;
using System.Collections.Generic;
using PotionCraft.Npc.Parts;
using PotionCraft.Npc.Parts.Appearance;
using PotionCraft.Npc.Parts.Appearance.Accessories;
using UnityEngine;
using UnityEngine.Rendering;

namespace NPCDesigner;

/// <summary>
/// Renders scaled, centered multi-layer sprite thumbnails of NPC <see cref="AppearancePart"/> assets,
/// color swatches, "Nessuno" placeholders, and preset icons inside ingredient-style UI slots.
/// </summary>
public static class NPCAssetThumbnailRenderer
{
    private const string MenuSortingLayer = "MenuElements";
    private const float MaxThumbnailExtent = 0.86f;

    private static Material? _cachedDefaultMaterial;
    private static Sprite? _solidSquareSprite;
    private static Sprite? _circleSwatchSprite;
    private static Sprite? _noneCrossSprite;
    private static Sprite? _presetScrollSprite;

    public readonly struct PaletteSnapshot
    {
        public Color Skin { get; }
        public Color Hair { get; }
        public Color Clothes1 { get; }
        public Color Clothes2 { get; }
        public Color Clothes3 { get; }
        public Color Clothes4 { get; }

        public PaletteSnapshot(Color skin, Color hair, Color c1, Color c2, Color c3, Color c4)
        {
            Skin = skin;
            Hair = hair;
            Clothes1 = c1;
            Clothes2 = c2;
            Clothes3 = c3;
            Clothes4 = c4;
        }

        public Color GetClothesByPartIndex(int partIndex) => partIndex switch
        {
            1 => Clothes1,
            2 => Clothes2,
            3 => Clothes3,
            4 => Clothes4,
            _ => Color.white
        };
    }

    private readonly struct LayerSpec
    {
        public Sprite Sprite { get; }
        public Color Tint { get; }
        public Vector3 LocalOffset { get; }
        public int Order { get; }

        public LayerSpec(Sprite sprite, Color tint, int order, Vector3 localOffset = default)
        {
            Sprite = sprite;
            Tint = tint;
            Order = order;
            LocalOffset = localOffset;
        }
    }

    public static Material? GetDefaultSpriteMaterial()
    {
        if (_cachedDefaultMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _cachedDefaultMaterial = new Material(shader);
            }
        }
        return _cachedDefaultMaterial;
    }

    public static Sprite GetSolidSquareSprite()
    {
        if (_solidSquareSprite != null) return _solidSquareSprite;

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        _solidSquareSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        _solidSquareSprite.name = "NPCDesigner_SolidSquare";
        return _solidSquareSprite;
    }

    public static Sprite GetCircleSwatchSprite()
    {
        if (_circleSwatchSprite != null) return _circleSwatchSprite;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = (size - 1) * 0.5f;
        float outerR = size * 0.44f;
        float borderR = size * 0.39f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > outerR)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (dist > borderR)
                {
                    // Dark ink rim matching Potion Craft's hand-drawn aesthetic
                    float aa = Mathf.Clamp01((outerR - dist) * 1.2f);
                    tex.SetPixel(x, y, new Color(0.24f, 0.16f, 0.10f, aa));
                }
                else
                {
                    // Subtle parchment highlight gradient inside swatch
                    float highlight = Mathf.Clamp01(1f - ((dx + 8f) * (dx + 8f) + (dy - 10f) * (dy - 10f)) / (size * size * 0.35f)) * 0.14f;
                    float shade = Mathf.Clamp01(0.92f + highlight);
                    tex.SetPixel(x, y, new Color(shade, shade, shade, 1f));
                }
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        _circleSwatchSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        _circleSwatchSprite.name = "NPCDesigner_ColorSwatch";
        return _circleSwatchSprite;
    }

    public static Sprite GetNoneCrossSprite()
    {
        if (_noneCrossSprite != null) return _noneCrossSprite;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = (size - 1) * 0.5f;
        float ringOuter = size * 0.36f;
        float ringInner = size * 0.29f;
        var ink = new Color(0.36f, 0.25f, 0.17f, 0.85f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                bool onRing = dist <= ringOuter && dist >= ringInner;
                bool onSlash = dist <= ringOuter && Mathf.Abs(dx + dy) <= 3.0f;
                tex.SetPixel(x, y, (onRing || onSlash) ? ink : Color.clear);
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        _noneCrossSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        _noneCrossSprite.name = "NPCDesigner_NoneIcon";
        return _noneCrossSprite;
    }

    public static Sprite GetPresetScrollSprite()
    {
        if (_presetScrollSprite != null) return _presetScrollSprite;

        // Prefer a genuine Potion Craft scroll/paper icon if loaded in memory
        foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>())
        {
            if (s == null || string.IsNullOrEmpty(s.name)) continue;
            if (s.name.Contains("Scroll", StringComparison.OrdinalIgnoreCase) &&
                s.name.Contains("InventoryIcon", StringComparison.OrdinalIgnoreCase))
            {
                _presetScrollSprite = s;
                return _presetScrollSprite;
            }
        }

        _presetScrollSprite = GetCircleSwatchSprite();
        return _presetScrollSprite;
    }

    public static void ClearThumbnail(Transform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(container.GetChild(i).gameObject);
        }
    }

    public static void RenderNone(Transform container, int sortingOrder)
    {
        ClearThumbnail(container);
        var layers = new List<LayerSpec> { new LayerSpec(GetNoneCrossSprite(), Color.white, 0) };
        BuildFittedLayers(container, layers, sortingOrder, maxExtent: 0.72f);
    }

    public static void RenderColorSwatch(Transform container, Color color, int sortingOrder)
    {
        ClearThumbnail(container);
        var layers = new List<LayerSpec> { new LayerSpec(GetCircleSwatchSprite(), color, 0) };
        BuildFittedLayers(container, layers, sortingOrder, maxExtent: 0.78f);
    }

    public static void RenderPresetIcon(Transform container, int sortingOrder)
    {
        ClearThumbnail(container);
        var sprite = GetPresetScrollSprite();
        Color tint = sprite == _circleSwatchSprite ? new Color(0.84f, 0.71f, 0.48f, 1f) : Color.white;
        var layers = new List<LayerSpec> { new LayerSpec(sprite, tint, 0) };
        BuildFittedLayers(container, layers, sortingOrder, maxExtent: 0.78f);
    }

    public static void RenderAppearancePart(Transform container, AppearancePart? part, PaletteSnapshot palette, int sortingOrder)
    {
        ClearThumbnail(container);
        if (part == null)
        {
            RenderNone(container, sortingOrder);
            return;
        }

        var layers = new List<LayerSpec>();
        int order = 0;

        switch (part)
        {
            case Body body:
                AddColorableArray(layers, body.handBack, palette, ref order);
                AddColorablePart(layers, body.handBackSkin, palette.Skin, ref order);
                AddColorableArray(layers, body.bodyBase, palette, ref order);
                AddColorablePart(layers, body.bodyBaseSkin, palette.Skin, ref order);
                AddColorableArray(layers, body.handFront, palette, ref order);
                AddColorablePart(layers, body.handFrontSkin, palette.Skin, ref order);
                break;

            case Face face:
                int faceIdx = face.hair != null && face.hair.Length > 3 ? 3 : 0;
                if (face.skin != null && faceIdx < face.skin.Length)
                {
                    AddColorablePart(layers, face.skin[faceIdx], palette.Skin, ref order);
                }
                if (face.hair != null && faceIdx < face.hair.Length)
                {
                    AddColorablePart(layers, face.hair[faceIdx], palette.Hair, ref order);
                }
                break;

            case Eyes eyes:
                if (eyes.left != null)
                {
                    layers.Add(new LayerSpec(eyes.left, Color.white, order++, new Vector3(-0.18f, 0f, 0f)));
                }
                if (eyes.right != null)
                {
                    layers.Add(new LayerSpec(eyes.right, Color.white, order++, new Vector3(0.18f, 0f, 0f)));
                }
                break;

            case SkullShape skull:
                AddColorablePart(layers, skull.shape, palette.Skin, ref order);
                break;

            case Breast breast:
                AddColorableArray(layers, breast.breast, palette, ref order);
                break;

            case Hairstyle hair:
                AddColorablePart(layers, hair.back, palette.Hair, ref order);
                AddColorablePart(layers, hair.shortFront, palette.Hair, ref order);
                AddColorablePart(layers, hair.longFront, palette.Hair, ref order);
                break;

            case Beard beard:
                AddColorablePart(layers, beard.beardBase, palette.Hair, ref order);
                break;

            case Hat hat:
                AddColorableArray(layers, hat.back, palette, ref order);
                AddColorableArray(layers, hat.middle, palette, ref order);
                AddColorableArray(layers, hat.front, palette, ref order);
                break;

            case Accessory acc:
                if (acc.accessory != null)
                {
                    for (int i = 0; i < acc.accessory.Length; i++)
                    {
                        var sp = acc.accessory[i];
                        if (sp == null) continue;
                        Color tint = palette.GetClothesByPartIndex(i);
                        layers.Add(new LayerSpec(sp, tint, order++));
                    }
                }
                break;
        }

        if (layers.Count == 0)
        {
            RenderNone(container, sortingOrder);
            return;
        }

        BuildFittedLayers(container, layers, sortingOrder, maxExtent: MaxThumbnailExtent);
    }

    private static void AddColorableArray(List<LayerSpec> layers, ColorablePart[]? parts, PaletteSnapshot palette, ref int order)
    {
        if (parts == null) return;

        // Pass 1: backgrounds & scratches
        for (int i = 0; i < parts.Length; i++)
        {
            var cp = parts[i];
            if (cp == null) continue;
            Color tint = palette.GetClothesByPartIndex(i);
            if (cp.background != null)
            {
                layers.Add(new LayerSpec(cp.background, tint, order++));
            }
            if (cp.scratches != null)
            {
                layers.Add(new LayerSpec(cp.scratches, new Color(tint.r, tint.g, tint.b, 0.55f), order++));
            }
        }

        // Pass 2: crisp ink contours on top
        for (int i = 0; i < parts.Length; i++)
        {
            var cp = parts[i];
            if (cp?.contour != null)
            {
                layers.Add(new LayerSpec(cp.contour, Color.white, order++));
            }
        }
    }

    private static void AddColorablePart(List<LayerSpec> layers, ColorablePart? cp, Color tint, ref int order)
    {
        if (cp == null) return;
        if (cp.background != null)
        {
            layers.Add(new LayerSpec(cp.background, tint, order++));
        }
        if (cp.scratches != null)
        {
            layers.Add(new LayerSpec(cp.scratches, new Color(tint.r, tint.g, tint.b, 0.55f), order++));
        }
        if (cp.contour != null)
        {
            layers.Add(new LayerSpec(cp.contour, Color.white, order++));
        }
    }

    private static void BuildFittedLayers(Transform container, List<LayerSpec> layers, int groupSortingOrder, float maxExtent)
    {
        if (layers.Count == 0) return;

        var sg = container.GetComponent<SortingGroup>() ?? container.gameObject.AddComponent<SortingGroup>();
        sg.sortingLayerName = MenuSortingLayer;
        sg.sortingOrder = groupSortingOrder;

        // Compute combined unscaled bounds across all sprites
        bool hasBounds = false;
        Bounds combined = default;
        foreach (var layer in layers)
        {
            if (layer.Sprite == null) continue;
            var b = layer.Sprite.bounds;
            b.center += layer.LocalOffset;
            if (!hasBounds)
            {
                combined = b;
                hasBounds = true;
            }
            else
            {
                combined.Encapsulate(b);
            }
        }

        if (!hasBounds) return;

        float width = Mathf.Max(0.01f, combined.size.x);
        float height = Mathf.Max(0.01f, combined.size.y);
        float maxDim = Mathf.Max(width, height);
        float scale = Mathf.Clamp(maxExtent / maxDim, 0.08f, 1.85f);

        var fitPivot = new GameObject("ThumbFit");
        fitPivot.layer = container.gameObject.layer;
        fitPivot.transform.SetParent(container, worldPositionStays: false);
        fitPivot.transform.localScale = new Vector3(scale, scale, 1f);
        // Center the combined sprite bounds neatly at (0, +0.10) inside the slot (leaving clean space for caption below)
        fitPivot.transform.localPosition = new Vector3(-combined.center.x * scale, -combined.center.y * scale + 0.10f, 0f);

        var mat = GetDefaultSpriteMaterial();
        int targetLayer = container.gameObject.layer;

        foreach (var layer in layers)
        {
            if (layer.Sprite == null) continue;
            var layerObj = new GameObject($"L{layer.Order}");
            layerObj.layer = targetLayer;
            layerObj.transform.SetParent(fitPivot.transform, worldPositionStays: false);
            layerObj.transform.localPosition = layer.LocalOffset;
            layerObj.transform.localScale = Vector3.one;

            var sr = layerObj.AddComponent<SpriteRenderer>();
            sr.sprite = layer.Sprite;
            sr.color = layer.Tint;
            sr.sortingLayerName = MenuSortingLayer;
            sr.sortingOrder = layer.Order;
            sr.maskInteraction = SpriteMaskInteraction.None;
            if (mat != null) sr.sharedMaterial = mat;
        }
    }
}
