using System;
using System.Collections.Generic;
using System.IO;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.Room;
using PotionCraft.Npc.MonoBehaviourScripts;
using PotionCraft.ObjectBased;
using PotionCraft.ObjectBased.InteractiveItem;
using PotionCraft.ObjectBased.UIElements;
using UnityEngine;

namespace NPCDesigner;

/// <summary>
/// Builds a purely visual, isolated replica of Potion Craft's Meeting Room environment
/// (background wooden wall, shelves, cupboards, bottles, books, and foreground Visitors Table counter)
/// inside the NPC Designer menu screen, with zero gameplay scripts, colliders, audio, or trade logic.
/// </summary>
public class NPCDesignerStage
{
    private const string MenuSortingLayer = "MenuElements";
    private const int BackgroundOrderBase = 11000;
    private const int ForegroundCounterOrderBase = 12250;

    /// <summary>
    /// Horizontal center (local to designer screen) of the Meeting Room showcase area to the left of the inventory panel.
    /// </summary>
    public const float StageCenterX = -4.15f;

    private static readonly Dictionary<string, Sprite> BundleSpriteCache = new(StringComparer.OrdinalIgnoreCase);

    private GameObject? _stageRoot;
    private GameObject? _hiddenMenuBackground;
    private bool _hasLoggedRoomSprites;
    private bool _builtFromLiveRoom;
    private bool _builtWithCounterAndProps;

    /// <summary>
    /// Top Y coordinate (local to designer screen) of the foreground wooden counter,
    /// used to position the preview NPC naturally behind the counter so the lower body is framed cleanly.
    /// </summary>
    public float CounterTopLocalY { get; private set; } = -4.31f;

    public bool IsBuilt => _stageRoot != null;

    public void Build(Transform designerScreenRoot)
    {
        Destroy();

        HideClonedMenuBackground(designerScreenRoot);

        _stageRoot = new GameObject("NPCDesigner_MeetingRoomStage");
        _stageRoot.transform.SetParent(designerScreenRoot, worldPositionStays: false);
        _stageRoot.transform.localPosition = Vector3.zero;
        _stageRoot.transform.localRotation = Quaternion.identity;
        _stageRoot.transform.localScale = Vector3.one;

        int targetLayer = designerScreenRoot.gameObject.layer;
        _stageRoot.layer = targetLayer;

        var defaultMaterial = NPCAssetThumbnailRenderer.GetDefaultSpriteMaterial();

        // Always add an opaque full-screen backdrop behind the room so camera clear-flags never leave previous menu pixels
        AddOpaqueSafetyBackdrop(_stageRoot.transform, targetLayer, defaultMaterial);

        _builtFromLiveRoom = TryBuildFromLiveMeetingRoom(_stageRoot.transform, targetLayer, defaultMaterial);
        if (_builtFromLiveRoom)
        {
            _builtWithCounterAndProps = true;
        }
        else
        {
            _builtWithCounterAndProps = BuildMeetingRoomFromAssets(_stageRoot.transform, targetLayer, defaultMaterial);
        }

        Plugin.Log?.LogInfo(
            $"[NPCDesignerStage] Meeting Room stage built (liveRoom={_builtFromLiveRoom}, " +
            $"withCounterAndProps={_builtWithCounterAndProps}, counterTopY={CounterTopLocalY:F2}).");
    }

    public void EnsureBuilt(Transform designerScreenRoot)
    {
        if (_stageRoot == null || (!_builtFromLiveRoom && !_builtWithCounterAndProps))
        {
            Build(designerScreenRoot);
        }
        else
        {
            HideClonedMenuBackground(designerScreenRoot);
        }
    }

    public void Destroy()
    {
        if (_stageRoot != null)
        {
            UnityEngine.Object.Destroy(_stageRoot);
            _stageRoot = null;
        }
    }

    /// <summary>
    /// Resolves a sprite by exact name from loaded resources first, then from already-loaded AssetBundles
    /// (such as Potion Craft's <c>rooms_assets_all</c> bundle) without instantiating scenes or gameplay objects.
    /// </summary>
    public static Sprite? LoadSpriteByExactName(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;

        if (BundleSpriteCache.TryGetValue(spriteName, out var cached) && cached != null)
        {
            return cached;
        }

        foreach (var sp in Resources.FindObjectsOfTypeAll<Sprite>())
        {
            if (sp != null && string.Equals(sp.name, spriteName, StringComparison.OrdinalIgnoreCase))
            {
                BundleSpriteCache[spriteName] = sp;
                return sp;
            }
        }

        try
        {
            foreach (var bundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (bundle == null || bundle.isStreamedSceneAssetBundle) continue;

                var assetNames = bundle.GetAllAssetNames();
                if (assetNames == null) continue;

                for (int i = 0; i < assetNames.Length; i++)
                {
                    string assetPath = assetNames[i];
                    string fileNameNoExt = Path.GetFileNameWithoutExtension(assetPath);
                    if (!string.Equals(fileNameNoExt, spriteName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var loaded = bundle.LoadAsset<Sprite>(assetPath);
                    if (loaded == null)
                    {
                        var subSprites = bundle.LoadAssetWithSubAssets<Sprite>(assetPath);
                        if (subSprites != null && subSprites.Length > 0)
                        {
                            loaded = subSprites[0];
                        }
                    }

                    if (loaded != null)
                    {
                        BundleSpriteCache[spriteName] = loaded;
                        return loaded;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"[NPCDesignerStage] Failed loading sprite '{spriteName}' from AssetBundles: {ex.Message}");
        }

        return null;
    }

    private void HideClonedMenuBackground(Transform designerScreenRoot)
    {
        var bg = designerScreenRoot.Find("Background");
        if (bg != null)
        {
            _hiddenMenuBackground = bg.gameObject;
            _hiddenMenuBackground.SetActive(false);
        }
    }

    private static void AddOpaqueSafetyBackdrop(Transform stageParent, int targetLayer, Material? defaultMaterial)
    {
        var obj = new GameObject("Stage_OpaqueBackdrop");
        obj.layer = targetLayer;
        obj.transform.SetParent(stageParent, worldPositionStays: false);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = new Vector3(26.2f, 15.0f, 1f);

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = NPCAssetThumbnailRenderer.GetSolidSquareSprite();
        sr.color = new Color(0.24f, 0.17f, 0.12f, 1f);
        sr.sortingLayerName = MenuSortingLayer;
        sr.sortingOrder = BackgroundOrderBase - 50;
        if (defaultMaterial != null) sr.sharedMaterial = defaultMaterial;
    }

    private bool TryBuildFromLiveMeetingRoom(Transform stageParent, int targetLayer, Material? defaultMaterial)
    {
        GameObject? meetingRoomObj = null;

        var roomManager = Managers.Room;
        if (roomManager?.InstantiatedRooms != null &&
            roomManager.InstantiatedRooms.TryGetValue(RoomIndex.Meeting, out var meetingRoom) &&
            meetingRoom != null)
        {
            meetingRoomObj = meetingRoom.gameObject;
        }
        else
        {
            meetingRoomObj = PotionCraft.ScriptableObjects.Room.Room.GetByRoomIndex(RoomIndex.Meeting)?.roomObject;
        }

        if (meetingRoomObj == null)
        {
            return false;
        }

        var roomTransform = meetingRoomObj.transform;
        Vector3 roomOrigin = roomTransform.position;
        var allRenderers = meetingRoomObj.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        if (allRenderers == null || allRenderers.Length == 0)
        {
            return false;
        }

        var backgroundEntries = new List<(SpriteRenderer sr, int sortRank)>();
        var counterEntries = new List<(SpriteRenderer sr, int sortRank)>();

        if (!_hasLoggedRoomSprites)
        {
            Plugin.Log?.LogInfo($"[NPCDesignerStage] Inspecting {allRenderers.Length} SpriteRenderers in RoomMeeting (origin={roomOrigin}).");
        }

        foreach (var sr in allRenderers)
        {
            if (sr == null || sr.sprite == null) continue;

            string path = GetRelativePath(sr.transform, roomTransform);
            string spriteName = sr.sprite.name ?? string.Empty;

            if (ShouldExcludeRenderer(sr, path, spriteName))
            {
                continue;
            }

            int sortRank = ComputeSortingRank(sr.sortingLayerName, sr.sortingOrder);

            if (IsForegroundCounter(path, spriteName))
            {
                counterEntries.Add((sr, sortRank));
            }
            else
            {
                backgroundEntries.Add((sr, sortRank));
            }
        }

        _hasLoggedRoomSprites = true;

        if (backgroundEntries.Count == 0 && counterEntries.Count == 0)
        {
            return false;
        }

        backgroundEntries.Sort((a, b) => a.sortRank.CompareTo(b.sortRank));
        counterEntries.Sort((a, b) => a.sortRank.CompareTo(b.sortRank));

        int bgOrder = BackgroundOrderBase;
        foreach (var (sr, _) in backgroundEntries)
        {
            CreateVisualClone(sr, roomOrigin, stageParent, targetLayer, defaultMaterial, bgOrder++);
        }

        bool foundCounterTop = false;
        float bestCounterTopY = float.MinValue;
        int fgOrder = ForegroundCounterOrderBase;

        foreach (var (sr, _) in counterEntries)
        {
            var cloneSr = CreateVisualClone(sr, roomOrigin, stageParent, targetLayer, defaultMaterial, fgOrder++);
            if (cloneSr != null && cloneSr.sprite != null)
            {
                float halfH = cloneSr.sprite.bounds.extents.y * Mathf.Abs(cloneSr.transform.localScale.y);
                float centerOffsetY = cloneSr.sprite.bounds.center.y * cloneSr.transform.localScale.y;
                float topY = cloneSr.transform.localPosition.y + centerOffsetY + halfH;
                if (topY > bestCounterTopY)
                {
                    bestCounterTopY = topY;
                    foundCounterTop = true;
                }
            }
        }

        if (counterEntries.Count == 0)
        {
            AddCounterSprite(stageParent, targetLayer, defaultMaterial, out float fallbackTopY);
            CounterTopLocalY = fallbackTopY;
        }
        else if (foundCounterTop)
        {
            CounterTopLocalY = bestCounterTopY;
        }

        AddCounterLowerSkirt(stageParent, targetLayer, defaultMaterial);
        return true;
    }

    private bool BuildMeetingRoomFromAssets(Transform stageParent, int targetLayer, Material? defaultMaterial)
    {
        int order = BackgroundOrderBase;
        int placedProps = 0;

        // 1. Authentic wooden plank wall (2560x1440)
        var woodBg = LoadSpriteByExactName("Wood Background");
        if (woodBg != null)
        {
            SpawnSprite(stageParent, "Stage_WoodBackground", woodBg, Vector3.zero, Vector3.one, targetLayer, defaultMaterial, order++, Color.white);
        }

        // Gentle atmospheric tint for background furniture so the foreground NPC pops clearly
        var bgPropTint = new Color(0.95f, 0.92f, 0.87f, 1f);

        // 2. Authentic shop shelves & cupboards resting along the back wall behind the counter
        // Counter top sits around y = -4.28; furniture bases tuck cleanly behind the counter edge (y = -4.60)
        // Each slot prefers Visitors-specific sprites and falls back to shared Laboratory/Visitors shop props already in memory.
        placedProps += TrySpawnRestingProp(stageParent, "Stage_BookshelfLeft", -11.45f, -4.60f, targetLayer, defaultMaterial, ref order, bgPropTint, out float leftShelfTop,
            "Visitors Background Bookshelf 1", "Laboratory Background Bookshelf 2");
        placedProps += TrySpawnRestingProp(stageParent, "Stage_BookshelfMidLeft", -8.35f, -4.60f, targetLayer, defaultMaterial, ref order, bgPropTint, out float midLeftTop,
            "Visitors Background Bookshelf 2", "Laboratory Background Bookshelf 1", "Laboratory, Visitors Background Cupboard 2");
        placedProps += TrySpawnRestingProp(stageParent, "Stage_CupboardCenter", -4.15f, -4.60f, targetLayer, defaultMaterial, ref order, bgPropTint, out _,
            "Laboratory, Visitors Background Cupboard 1", "Laboratory, Visitors Background Cupboard 3");
        placedProps += TrySpawnRestingProp(stageParent, "Stage_CupboardMidRight", -0.60f, -4.60f, targetLayer, defaultMaterial, ref order, bgPropTint, out float midRightTop,
            "Laboratory, Visitors Background Cupboard 7", "Laboratory, Visitors Background Cupboard 5");
        placedProps += TrySpawnRestingProp(stageParent, "Stage_CupboardRight", 2.35f, -4.60f, targetLayer, defaultMaterial, ref order, bgPropTint, out float rightCupTop,
            "Visitors Background Cupboard 6", "Laboratory Background Cupboard 6", "Laboratory, Visitors Background Cupboard 2");

        // 3. Authentic shop bottles, stacked books, and chest resting neatly on top of the shelves & cupboards
        if (leftShelfTop > -4.5f)
        {
            placedProps += TrySpawnRestingProp(stageParent, "Stage_BottleLeftA", -11.75f, leftShelfTop - 0.08f, targetLayer, defaultMaterial, ref order, bgPropTint, out _,
                "Visitors Background Bottle 15", "Laboratory, Visitors Background Bottle 11", "Laboratory, Visitors Background Bottle 1");
            placedProps += TrySpawnRestingProp(stageParent, "Stage_BottleLeftB", -10.80f, leftShelfTop - 0.08f, targetLayer, defaultMaterial, ref order, bgPropTint, out _,
                "Laboratory, Visitors Background Bottle 5", "Laboratory, Visitors Background Bottle 8");
        }

        if (midLeftTop > -4.5f)
        {
            placedProps += TrySpawnRestingProp(stageParent, "Stage_BookStack1", -9.55f, midLeftTop - 0.05f, targetLayer, defaultMaterial, ref order, bgPropTint, out float book1Top,
                "Laboratory, Visitors Background Book 1");
            if (book1Top > midLeftTop)
            {
                placedProps += TrySpawnRestingProp(stageParent, "Stage_BookStack2", -9.50f, book1Top - 0.05f, targetLayer, defaultMaterial, ref order, bgPropTint, out _,
                    "Laboratory, Visitors Background Book 2", "Laboratory, Visitors Background Book 3");
            }
            placedProps += TrySpawnRestingProp(stageParent, "Stage_BottleMidLeftA", -8.00f, midLeftTop - 0.08f, targetLayer, defaultMaterial, ref order, bgPropTint, out _,
                "Laboratory, Visitors Background Bottle 13", "Laboratory Background Bottle 17", "Laboratory, Visitors Background Bottle 1");
            placedProps += TrySpawnRestingProp(stageParent, "Stage_BottleMidLeftB", -6.85f, midLeftTop - 0.08f, targetLayer, defaultMaterial, ref order, bgPropTint, out _,
                "Laboratory, Visitors Background Bottle 10", "Laboratory, Visitors Background Bottle 9");
        }

        if (midRightTop > -4.5f)
        {
            placedProps += TrySpawnRestingProp(stageParent, "Stage_BottleMidRightA", -1.05f, midRightTop - 0.08f, targetLayer, defaultMaterial, ref order, bgPropTint, out _,
                "Laboratory, Visitors Background Bottle 8", "Laboratory, Visitors Background Bottle 4");
            placedProps += TrySpawnRestingProp(stageParent, "Stage_BottleMidRightB", -0.15f, midRightTop - 0.08f, targetLayer, defaultMaterial, ref order, bgPropTint, out _,
                "Laboratory, Visitors Background Bottle 2", "Laboratory, Visitors Background Bottle 6");
        }

        if (rightCupTop > -4.5f)
        {
            placedProps += TrySpawnRestingProp(stageParent, "Stage_BottleRightA", 1.35f, rightCupTop - 0.08f, targetLayer, defaultMaterial, ref order, bgPropTint, out _,
                "Visitors Background Bottle 16", "Laboratory Background Bottle 18", "Laboratory, Visitors Background Bottle 11");
            placedProps += TrySpawnRestingProp(stageParent, "Stage_BottleRightB", 2.25f, rightCupTop - 0.08f, targetLayer, defaultMaterial, ref order, bgPropTint, out _,
                "Visitors Background Bottle 14", "Laboratory, Visitors Background Bottle 7", "Laboratory, Visitors Background Bottle 3");
            placedProps += TrySpawnRestingProp(stageParent, "Stage_ChestRight", 3.25f, rightCupTop - 0.08f, targetLayer, defaultMaterial, ref order, bgPropTint, out _,
                "Laboratory, Visitors Background Chest 1");
        }

        // 4. Foreground shop counter ("Visitors Table") in front of the NPC's lower torso
        bool hasCounter = AddCounterSprite(stageParent, targetLayer, defaultMaterial, out float topY);
        CounterTopLocalY = topY;
        AddCounterLowerSkirt(stageParent, targetLayer, defaultMaterial);

        Plugin.Log?.LogInfo($"[NPCDesignerStage] Assembled Meeting Room from bundles: props={placedProps}, hasVisitorsTable={hasCounter}, counterTopY={topY:F2}.");
        return hasCounter && placedProps > 0;
    }

    private static int TrySpawnRestingProp(
        Transform parent,
        string objName,
        float x,
        float bottomY,
        int layer,
        Material? material,
        ref int order,
        Color tint,
        out float topY,
        params string[] candidateSpriteNames)
    {
        topY = bottomY;
        Sprite? sprite = null;
        for (int i = 0; i < candidateSpriteNames.Length; i++)
        {
            sprite = LoadSpriteByExactName(candidateSpriteNames[i]);
            if (sprite != null) break;
        }
        if (sprite == null) return 0;

        float minLocalY = sprite.bounds.min.y;
        float centerY = bottomY - minLocalY;
        SpawnSprite(parent, objName, sprite, new Vector3(x, centerY, 0f), Vector3.one, layer, material, order++, tint);
        topY = centerY + sprite.bounds.max.y;
        return 1;
    }

    private static bool AddCounterSprite(Transform stageParent, int targetLayer, Material? defaultMaterial, out float counterTopY)
    {
        const float tableCenterY = -4.85f;
        counterTopY = -4.31f;

        var tableSprite = LoadSpriteByExactName("Visitors Table")
            ?? LoadSpriteByExactName("Laboratory Table");

        if (tableSprite == null)
        {
            return false;
        }

        var sr = SpawnSprite(
            stageParent,
            "Stage_VisitorsTable",
            tableSprite,
            new Vector3(0f, tableCenterY, 0f),
            Vector3.one,
            targetLayer,
            defaultMaterial,
            ForegroundCounterOrderBase,
            Color.white);

        if (sr.sprite != null)
        {
            counterTopY = tableCenterY + sr.sprite.bounds.max.y;
        }

        return true;
    }

    private void AddCounterLowerSkirt(Transform stageParent, int targetLayer, Material? defaultMaterial)
    {
        // Dark oak counter body below the tabletop lip (hides the cut-off bottom of the NPC completely)
        float skirtTopY = CounterTopLocalY - 0.96f;
        float skirtBottomY = -7.6f;
        float height = Mathf.Max(0.5f, skirtTopY - skirtBottomY);
        float centerY = skirtBottomY + height * 0.5f;

        var skirtSprite = NPCAssetThumbnailRenderer.GetSolidSquareSprite();
        var obj = new GameObject("Stage_CounterApron");
        obj.layer = targetLayer;
        obj.transform.SetParent(stageParent, worldPositionStays: false);
        obj.transform.localPosition = new Vector3(-4.0f, centerY, 0f);
        obj.transform.localScale = new Vector3(19.0f, height, 1f);

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = skirtSprite;
        sr.color = new Color(0.20f, 0.13f, 0.08f, 1f);
        sr.sortingLayerName = MenuSortingLayer;
        sr.sortingOrder = ForegroundCounterOrderBase - 5;
        if (defaultMaterial != null) sr.sharedMaterial = defaultMaterial;
    }

    private static SpriteRenderer? CreateVisualClone(
        SpriteRenderer source,
        Vector3 roomOrigin,
        Transform parent,
        int targetLayer,
        Material? defaultMaterial,
        int sortingOrder)
    {
        var obj = new GameObject($"Stage_{source.gameObject.name}");
        obj.layer = targetLayer;
        obj.transform.SetParent(parent, worldPositionStays: false);

        Vector3 relPos = source.transform.position - roomOrigin;
        obj.transform.localPosition = new Vector3(relPos.x, relPos.y, 0f);
        obj.transform.localRotation = source.transform.rotation;
        obj.transform.localScale = source.transform.lossyScale;

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = source.sprite;
        sr.color = source.color;
        sr.flipX = source.flipX;
        sr.flipY = source.flipY;
        sr.drawMode = source.drawMode;
        if (source.drawMode != SpriteDrawMode.Simple)
        {
            sr.size = source.size;
            sr.tileMode = source.tileMode;
        }

        sr.sortingLayerName = MenuSortingLayer;
        sr.sortingOrder = sortingOrder;
        sr.maskInteraction = SpriteMaskInteraction.None;

        if (defaultMaterial != null)
        {
            sr.sharedMaterial = defaultMaterial;
        }

        return sr;
    }

    private static bool IsForegroundCounter(string path, string spriteName)
    {
        return spriteName.Contains("Visitors Table", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("Visitors Table", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("Counter", StringComparison.OrdinalIgnoreCase) ||
               (path.Contains("Table", StringComparison.OrdinalIgnoreCase) &&
                !path.Contains("Background", StringComparison.OrdinalIgnoreCase));
    }

    private static bool ShouldExcludeRenderer(SpriteRenderer sr, string path, string spriteName)
    {
        if (sr.color.a <= 0.01f) return true;
        if (sr.GetComponent<SpriteMask>() != null) return true;

        if (sr.GetComponentInParent<NpcMonoBehaviour>(true) != null) return true;
        if (sr.GetComponentInParent<Button>(true) != null) return true;
        if (sr.GetComponentInParent<InteractiveItem>(true) != null) return true;

        string combined = path + "|" + spriteName + "|" + (sr.sortingLayerName ?? string.Empty);

        string[] excludedKeywords =
        {
            "Scales", "Scale", "Trade", "Trading", "Dialogue", "Dialog",
            "Navigation", "NavButton", "Arrow", "DarkScreen", "Night",
            "Shadow", "Glow", "Light", "Fog", "Particle", "Bell",
            "Haggle", "Karma", "Gold", "Popularity", "Tooltip", "Tutorial",
            "Cursor", "Outline", "Hover", "Highlight", "DropPoint", "BuildZone",
            "BuildMode", "Ledge", "Decor", "Potion", "Npc", "NPC", "Queue",
            "Bookmark", "Inventory", "Panel", "Window", "Canvas", "Button",
            "Sign", "Collider", "Mask", "Silhouette", "Prompt", "Slot",
            "Bubble", "Vibe", "Indicator", "Icon"
        };

        foreach (var kw in excludedKeywords)
        {
            if (combined.Contains(kw, StringComparison.OrdinalIgnoreCase))
            {
                if (IsForegroundCounter(path, spriteName)) return false;
                if (spriteName.Contains("Wood Background", StringComparison.OrdinalIgnoreCase)) return false;
                if (spriteName.Contains("Visitors Background", StringComparison.OrdinalIgnoreCase)) return false;
                return true;
            }
        }

        string layer = sr.sortingLayerName ?? string.Empty;
        if (layer.Contains("Gui", StringComparison.OrdinalIgnoreCase) ||
            layer.Contains("Menu", StringComparison.OrdinalIgnoreCase) ||
            layer.Contains("Tooltip", StringComparison.OrdinalIgnoreCase) ||
            layer.Contains("DarkScreen", StringComparison.OrdinalIgnoreCase) ||
            layer.Contains("ScreenBorder", StringComparison.OrdinalIgnoreCase) ||
            layer.Contains("Tutorial", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static int ComputeSortingRank(string? sortingLayerName, int sortingOrder)
    {
        int layerValue = SortingLayer.GetLayerValueFromName(sortingLayerName ?? "Default");
        return layerValue * 100000 + sortingOrder;
    }

    private static string GetRelativePath(Transform target, Transform root)
    {
        var names = new List<string>();
        var cur = target;
        while (cur != null && cur != root)
        {
            names.Add(cur.name);
            cur = cur.parent;
        }
        names.Reverse();
        return string.Join("/", names);
    }

    private static SpriteRenderer SpawnSprite(
        Transform parent,
        string name,
        Sprite sprite,
        Vector3 localPos,
        Vector3 localScale,
        int layer,
        Material? material,
        int sortingOrder,
        Color tint)
    {
        var obj = new GameObject(name);
        obj.layer = layer;
        obj.transform.SetParent(parent, worldPositionStays: false);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = localScale;

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = tint;
        sr.sortingLayerName = MenuSortingLayer;
        sr.sortingOrder = sortingOrder;
        sr.maskInteraction = SpriteMaskInteraction.None;
        if (material != null) sr.sharedMaterial = material;
        return sr;
    }
}
