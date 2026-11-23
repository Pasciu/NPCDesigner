using System;
using System.Collections.Generic;
using PotionCraft.InventorySystem;
using PotionCraft.LocalizationSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.Menu;
using PotionCraft.Npc.Parts;
using PotionCraft.ObjectBased;
using PotionCraft.ObjectBased.Menu;
using PotionCraft.ObjectBased.Menu.Buttons;
using PotionCraft.ObjectBased.Menu.Menus;
using PotionCraft.ObjectBased.UIElements;
using TMPro;
using UnityEngine;

namespace NPCDesigner;

public class DesignerButtonHandler : MonoBehaviour
{
    public Action? OnClick { get; set; }
    public float CustomFontSize { get; set; } = 3.2f;
    public float MaxColliderWidth { get; set; } = 4.6f;
    public float MinColliderWidth { get; set; } = 0.8f;
    public float ColliderHeight { get; set; } = 0.56f;
    public bool IsFixedSquareSlot { get; set; }
    public float FixedSlotSize { get; set; } = 1.32f;

    public void EnforceCustomBounds(MenuButton btn)
    {
        if (btn == null || !btn.gameObject.activeInHierarchy) return;

        if (IsFixedSquareSlot)
        {
            if (btn.thisCollider is BoxCollider2D slotBox)
            {
                slotBox.size = new Vector2(FixedSlotSize, FixedSlotSize);
                slotBox.offset = Vector2.zero;
            }

            var visuals = btn.GetComponent<DesignerSlotVisuals>();
            visuals?.ApplyVisualState(btn);
            return;
        }

        float textWidth = 1.2f;
        if (btn.textObject != null)
        {
            btn.textObject.enableAutoSizing = false;
            btn.textObject.enableWordWrapping = false;
            btn.textObject.overflowMode = TextOverflowModes.Overflow;
            btn.textObject.fontSize = CustomFontSize;
            btn.textObject.ForceMeshUpdate(true);

            Vector2 rendered = btn.textObject.GetRenderedValues(onlyVisibleCharacters: true);
            if (!float.IsNaN(rendered.x) && !float.IsInfinity(rendered.x) && rendered.x > 0.05f && rendered.x < 20f)
            {
                textWidth = rendered.x;
            }
        }

        float targetWidth = Mathf.Clamp(textWidth + 0.45f, MinColliderWidth, MaxColliderWidth);

        if (btn.thisCollider is BoxCollider2D box)
        {
            box.size = new Vector2(targetWidth, ColliderHeight);
            box.offset = Vector2.zero;
        }

        ClampUnderlayRenderer(btn.substrateRenderer, targetWidth);
        ClampUnderlayRenderer(btn.hoverRenderer, targetWidth);
    }

    private static void ClampUnderlayRenderer(SpriteRenderer? renderer, float targetWidth)
    {
        if (renderer == null || renderer.sprite == null) return;
        float spriteWidth = renderer.sprite.bounds.size.x;
        if (spriteWidth <= 0.01f || float.IsNaN(spriteWidth) || float.IsInfinity(spriteWidth)) return;

        float scaleX = targetWidth / spriteWidth;
        renderer.transform.localPosition = Vector3.zero;
        renderer.transform.localScale = new Vector3(scaleX, 0.72f, 1f);
    }
}

/// <summary>
/// Gives a cloned MenuButton the authentic Potion Craft ingredient inventory slot look
/// (<c>Inventory Item Slot N Normal</c> + hover highlight + equipped gold highlight + sprite thumbnail).
/// </summary>
public class DesignerSlotVisuals : MonoBehaviour
{
    public Sprite? IdleSprite { get; set; }
    public Sprite? HoverSprite { get; set; }
    public float SlotSize { get; set; } = 1.32f;
    public bool IsEquipped { get; set; }
    public Transform? ThumbnailContainer { get; set; }
    public SpriteRenderer? EquippedHighlightRenderer { get; set; }

    public void ApplyVisualState(MenuSelectableObject selectable)
    {
        if (selectable == null) return;

        if (selectable.substrateRenderer != null)
        {
            if (IdleSprite != null)
            {
                selectable.substrateRenderer.sprite = IdleSprite;
            }
            selectable.substrateRenderer.sortingLayerName = "MenuElements";
            selectable.substrateRenderer.sortingOrder = 12550;
            selectable.substrateRenderer.color = IsEquipped
                ? new Color(0.95f, 0.80f, 0.42f, 0.85f)
                : new Color(0.76f, 0.64f, 0.48f, 0.48f);
            FitRendererToSquare(selectable.substrateRenderer, SlotSize);
        }

        if (selectable.hoverRenderer != null)
        {
            if (HoverSprite != null)
            {
                selectable.hoverRenderer.sprite = HoverSprite;
            }
            else if (IdleSprite != null)
            {
                selectable.hoverRenderer.sprite = IdleSprite;
            }
            selectable.hoverRenderer.sortingLayerName = "MenuElements";
            selectable.hoverRenderer.sortingOrder = 12560;
            selectable.hoverRenderer.color = new Color(1f, 0.92f, 0.68f, 0.75f);
            FitRendererToSquare(selectable.hoverRenderer, SlotSize * 1.03f);
            selectable.hoverRenderer.enabled = selectable.IsSelected;
        }

        if (EquippedHighlightRenderer != null)
        {
            EquippedHighlightRenderer.enabled = IsEquipped;
            EquippedHighlightRenderer.sortingLayerName = "MenuElements";
            EquippedHighlightRenderer.sortingOrder = 12555;
        }

        if (selectable is MenuButton btn && btn.textObject != null)
        {
            btn.textObject.sortingOrder = 12720;
            btn.textObject.transform.localPosition = new Vector3(0f, -0.52f, 0f);
        }
    }

    private static void FitRendererToSquare(SpriteRenderer renderer, float targetSize)
    {
        if (renderer.sprite == null) return;
        Vector2 size = renderer.sprite.bounds.size;
        if (size.x <= 0.01f || size.y <= 0.01f) return;

        renderer.transform.localPosition = Vector3.zero;
        renderer.transform.localScale = new Vector3(targetSize / size.x, targetSize / size.y, 1f);
    }
}

public class NPCDesignerScreenHandler : MonoBehaviour
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
    private void OnEnable()
    {
        NPCDesignerMenuIntegration.Instance?.OnScreenOpened();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
    private void OnDisable()
    {
        NPCDesignerMenuIntegration.Instance?.OnScreenClosed();
    }
}

public class NPCDesignerMenuIntegration
{
    public const int DesignerMenuScreenId = 100;

    private enum AssetGroup
    {
        Body = 0,
        Style = 1,
        Colors = 2,
        Presets = 3
    }

    private enum AssetSubCategory
    {
        // Body group
        Body,
        Face,
        Eyes,
        SkullShape,
        Breast,

        // Style group
        Hairstyle,
        Beard,
        Hat,
        AccAboveHair,
        AccFace,
        AccSkull,
        AccShortHair,
        AccHandBack,
        AccHandFront,
        AccBehindBody,
        AccBody,

        // Colors group
        SkinColor,
        HairColor,
        ClothesColor1,
        ClothesColor2,
        ClothesColor3,
        ClothesColor4,

        // Presets group
        SavedPresets
    }

    private sealed class AssetSlotEntry
    {
        public string Label { get; set; } = "";
        public bool IsNone { get; set; }
        public bool IsColor { get; set; }
        public bool IsPreset { get; set; }
        public bool IsSelected { get; set; }
        public AppearancePart? Part { get; set; }
        public Color SwatchColor { get; set; }
        public Action? OnSelect { get; set; }
    }

    private sealed class SlotButtonView
    {
        public MenuButton Button { get; set; } = null!;
        public DesignerButtonHandler Handler { get; set; } = null!;
        public DesignerSlotVisuals Visuals { get; set; } = null!;
        public Transform ThumbnailRoot { get; set; } = null!;
    }

    // Right-side ingredient panel layout constants
    private const float PanelCenterX = 8.60f;
    private const int GridColumns = 4;
    private const int GridRows = 4;
    private const int PageSize = GridColumns * GridRows;
    private const float SlotSpacingX = 1.56f;
    private const float SlotSpacingY = 1.50f;
    private const float SlotSize = 1.34f;
    private const float GridTopY = 3.35f;

    public static NPCDesignerMenuIntegration? Instance { get; private set; }
    public static bool IsInitialized => Instance != null && Instance._designerScreenObj != null;

    private readonly NPCPreviewController _previewController;
    private readonly NPCDesignerStage _stage = new();
    private readonly List<GameObject> _hiddenClutterObjects = new();

    private GameObject? _designerScreenObj;
    private GameObject? _inventoryPanelBackdrop;
    private bool _backdropHasWindowFrame;

    private readonly MenuButton?[] _groupTabButtons = new MenuButton?[4];
    private MenuButton? _subCatLeftBtn;
    private MenuButton? _subCatCenterBtn;
    private MenuButton? _subCatRightBtn;

    private readonly List<SlotButtonView> _slotViews = new();

    private MenuButton? _pagePrevBtn;
    private MenuButton? _pageInfoBtn;
    private MenuButton? _pageNextBtn;

    private MenuButton? _randomBtn;
    private MenuButton? _saveBtn;
    private MenuButton? _backBtn;
    private MenuButton? _moodBtn;
    private MenuButton? _genderBtn;
    private TextMeshPro? _statusFooterTmp;

    private Sprite[] _slotIdleSprites = Array.Empty<Sprite>();
    private Sprite[] _slotHoverSprites = Array.Empty<Sprite>();

    private AssetGroup _currentGroup = AssetGroup.Body;
    private int _currentSubCategoryIndex;
    private int _currentPage;
    private string _statusMessage = "";

    private static readonly string[] GroupLabels =
    {
        "Corpo",
        "Stile",
        "Colori",
        "Preset"
    };

    private static readonly string[] GenderNames =
    {
        "Maschio",
        "Femmina",
        "Tutti"
    };

    private static readonly string[] MoodNames =
    {
        "Arrabbiato",
        "Contrariato",
        "Neutro",
        "Felice",
        "Entusiasta"
    };

    public NPCDesignerMenuIntegration(NPCPreviewController previewController)
    {
        _previewController = previewController;
        Instance = this;
    }

    private static bool _setupCompleted;

    public static void Setup(MenuManager menuManager, NPCPreviewController previewController)
    {
        if (_setupCompleted && Instance != null && Instance._designerScreenObj != null) return;

        try
        {
            var integration = new NPCDesignerMenuIntegration(previewController);
            integration.InitializeMenu();
            _setupCompleted = true;
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"Failed to setup NPCDesigner menu integration: {ex}");
        }
    }

    private void InitializeMenu()
    {
        NPCPartRegistry.Initialize();
        NPCPresetManager.EnsureDirectory();
        LoadNativeInventorySlotSprites();

        var container = Managers.Game?.Cam?.transform.Find("Menu Screens Container");
        if (container == null)
        {
            Plugin.Log?.LogError("Menu Screens Container not found!");
            return;
        }

        SetupMainMenuButton(container);
        SetupDesignerScreen(container);
    }

    private void LoadNativeInventorySlotSprites()
    {
        var idleList = new List<Sprite>();
        var hoverList = new List<Sprite>();

        try
        {
            var invPanel = Managers.Player?.InventoryPanel;
            var itemPrefab = invPanel?.inventoryItemObjectPrefab;
            var itemBtn = itemPrefab != null ? itemPrefab.GetComponent<InventoryItemButton>() : null;
            if (itemBtn != null)
            {
                if (itemBtn.idleSprites != null)
                {
                    foreach (var s in itemBtn.idleSprites)
                    {
                        if (s != null) idleList.Add(s);
                    }
                }
                if (itemBtn.hoveredSprites != null)
                {
                    foreach (var s in itemBtn.hoveredSprites)
                    {
                        if (s != null) hoverList.Add(s);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"[NPCDesigner] Could not read InventoryItemButton sprites from InventoryPanel: {ex.Message}");
        }

        if (idleList.Count == 0)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sp in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (sp == null || string.IsNullOrEmpty(sp.name)) continue;
                if (sp.name.Contains("Inventory Item Slot", StringComparison.OrdinalIgnoreCase) &&
                    sp.name.Contains("Normal", StringComparison.OrdinalIgnoreCase) &&
                    seen.Add(sp.name))
                {
                    idleList.Add(sp);
                }
            }
        }

        if (hoverList.Count == 0)
        {
            hoverList.AddRange(idleList);
        }

        _slotIdleSprites = idleList.ToArray();
        _slotHoverSprites = hoverList.ToArray();
        Plugin.Log?.LogInfo($"[NPCDesigner] Loaded {_slotIdleSprites.Length} native Inventory Item Slot idle sprites and {_slotHoverSprites.Length} hover sprites.");
    }

    private static void SetupMainMenuButton(Transform container)
    {
        var mainScreen = container.Find("Main");
        if (mainScreen == null)
        {
            Plugin.Log?.LogWarning("Main menu screen not found.");
            return;
        }

        var mainButtons = mainScreen.Find("Buttons");
        if (mainButtons == null)
        {
            Plugin.Log?.LogWarning("Main menu Buttons container not found.");
            return;
        }

        if (mainButtons.Find("NPCDesigner") != null)
        {
            return;
        }

        var settingsBtn = mainButtons.Find("Settings");
        if (settingsBtn == null)
        {
            Plugin.Log?.LogWarning("Settings button not found in Main/Buttons.");
            return;
        }

        var designerBtnObj = UnityEngine.Object.Instantiate(settingsBtn.gameObject, mainButtons);
        designerBtnObj.name = "NPCDesigner";

        var menuBtn = designerBtnObj.GetComponent<MenuButton>();
        if (menuBtn != null)
        {
            menuBtn.thisButtonIs = MenuButton.MainMenuButtons.CustomAction;
            if (menuBtn.localizedText != null) menuBtn.localizedText.enabled = false;
            if (menuBtn.textObject != null)
            {
                menuBtn.textObject.text = "NPC Designer";
                menuBtn.textObject.ForceMeshUpdate(true);
            }
            if (menuBtn.gameObject.activeInHierarchy)
            {
                menuBtn.UpdateUnderlaySize();
            }
        }

        var handler = designerBtnObj.AddComponent<DesignerButtonHandler>();
        handler.CustomFontSize = menuBtn?.textObject != null ? menuBtn.textObject.fontSize : 5.5f;
        handler.MaxColliderWidth = 5.5f;
        handler.MinColliderWidth = 3.0f;
        handler.ColliderHeight = 0.75f;
        handler.OnClick = () =>
        {
            Managers.Menu.GoTo((MenuManager.MenuScreen)DesignerMenuScreenId);
        };

        int settingsIndex = settingsBtn.GetSiblingIndex();
        designerBtnObj.transform.SetSiblingIndex(settingsIndex + 1);

        MenuScreenObjectLayoutUpdater.UpdateLayout(mainScreen.gameObject);
        Plugin.Log?.LogInfo("Native NPC Designer Main Menu button successfully injected.");
    }

    private void SetupDesignerScreen(Transform container)
    {
        if (container.Find("NPCDesigner") != null)
        {
            return;
        }

        var settingsScreen = container.Find("Settings");
        if (settingsScreen == null)
        {
            Plugin.Log?.LogError("Settings menu screen not found for cloning.");
            return;
        }

        _designerScreenObj = UnityEngine.Object.Instantiate(settingsScreen.gameObject, container);
        _designerScreenObj.name = "NPCDesigner";
        _designerScreenObj.transform.localPosition = Vector3.zero;

        var updater = _designerScreenObj.GetComponent<MenuScreenObjectLayoutUpdater>();
        if (updater != null)
        {
            UnityEngine.Object.DestroyImmediate(updater);
        }

        var logoTransform = _designerScreenObj.transform.Find("Logo");
        if (logoTransform != null)
        {
            UnityEngine.Object.DestroyImmediate(logoTransform.gameObject);
        }

        var screenObj = _designerScreenObj.GetComponent<MenuScreenObject>();
        if (screenObj != null)
        {
            screenObj.thisMenuIs = (MenuManager.MenuScreen)DesignerMenuScreenId;
            Managers.Menu.onCurrentMenuChanged.AddListener(() =>
            {
                if (Managers.Menu.CurrentMenu == (MenuManager.MenuScreen)DesignerMenuScreenId)
                {
                    _designerScreenObj.SetActive(true);
                    Managers.Menu.currentMenuScreenObject = screenObj;
                    OnScreenOpened();
                }
                else if (_designerScreenObj.activeSelf)
                {
                    _designerScreenObj.SetActive(false);
                }
            });
        }

        // 1. Build authentic Meeting Room shop environment + foreground counter
        _stage.Build(_designerScreenObj.transform);

        // 2. Build native Potion Craft right-side Ingredient Inventory Panel backdrop
        BuildRightInventoryPanelBackdrop(_designerScreenObj.transform);

        // 3. Position the header title cleanly at the top of the right-side parchment panel
        var subtitleGroup = _designerScreenObj.transform.Find("Subtitle");
        if (subtitleGroup != null)
        {
            subtitleGroup.localPosition = new Vector3(PanelCenterX, 5.12f, 0f);
            subtitleGroup.localScale = new Vector3(0.76f, 0.76f, 1f);
            var subtitleTextTemplate = subtitleGroup.Find("Text Subtitle");
            if (subtitleTextTemplate != null)
            {
                var loc = subtitleTextTemplate.GetComponent<LocalizedText>();
                if (loc != null)
                {
                    loc.key = null!;
                    loc.enabled = false;
                }
                var tmp = subtitleTextTemplate.GetComponent<TextMeshPro>();
                if (tmp != null)
                {
                    tmp.text = "CREAZIONE PERSONAGGIO";
                    tmp.fontSize = 4.2f;
                    tmp.color = new Color(0.16f, 0.10f, 0.05f, 1f);
                    tmp.sortingOrder = 12700;
                }
            }

            foreach (var sr in subtitleGroup.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.sortingLayerName = "MenuElements";
                sr.sortingOrder = 12680;
            }
        }

        var buttonsContainer = _designerScreenObj.transform.Find("Buttons");
        if (buttonsContainer == null)
        {
            Plugin.Log?.LogError("Buttons container not found in cloned screen.");
            return;
        }
        buttonsContainer.localPosition = Vector3.zero;
        buttonsContainer.localScale = Vector3.one;

        var backBtnTransform = buttonsContainer.Find("Back");
        if (backBtnTransform == null)
        {
            Plugin.Log?.LogError("Back button not found in cloned screen.");
            return;
        }

        var templateBtn = backBtnTransform.GetComponent<MenuButton>();
        ElevateButtonSorting(templateBtn, 12650);

        // Configure Back button at bottom center of the right panel
        _backBtn = templateBtn;
        backBtnTransform.localPosition = new Vector3(PanelCenterX, -5.35f, 0f);
        backBtnTransform.localScale = Vector3.one;
        var backHandler = backBtnTransform.gameObject.AddComponent<DesignerButtonHandler>();
        backHandler.CustomFontSize = 3.15f;
        backHandler.MaxColliderWidth = 3.6f;
        backHandler.MinColliderWidth = 2.4f;
        backHandler.ColliderHeight = 0.56f;

        if (templateBtn.localizedText != null)
        {
            templateBtn.localizedText.key = null!;
            templateBtn.localizedText.enabled = false;
        }
        if (templateBtn.textObject != null)
        {
            templateBtn.textObject.enableAutoSizing = false;
            templateBtn.textObject.fontSize = 3.15f;
            templateBtn.textObject.text = "< Torna al Menu";
        }

        // Delete other cloned settings sub-buttons immediately
        var toDestroy = new List<GameObject>();
        foreach (Transform child in buttonsContainer)
        {
            if (child != backBtnTransform)
            {
                toDestroy.Add(child.gameObject);
            }
        }
        foreach (var obj in toDestroy)
        {
            UnityEngine.Object.DestroyImmediate(obj);
        }

        // Create status footer inside the right inventory panel
        CreateStatusFooter(_designerScreenObj.transform, templateBtn.textObject?.transform);

        // Spawn real-time animated preview NPC framed cleanly behind Visitors Table
        PositionAndSpawnPreviewNpc(_designerScreenObj.transform);

        // Build Right-Side Ingredient-Style Inventory UI Controls
        BuildGroupTabs(buttonsContainer, templateBtn);
        BuildSubCategorySwitcher(buttonsContainer, templateBtn);
        BuildAssetSlotGrid(buttonsContainer, templateBtn);
        BuildPaginationBar(buttonsContainer, templateBtn);
        BuildActionButtons(buttonsContainer, templateBtn);

        _designerScreenObj.AddComponent<NPCDesignerScreenHandler>();
        _designerScreenObj.SetActive(false);

        Plugin.Log?.LogInfo("Native NPC Designer screen with Meeting Room stage & ingredient-style UI created.");
    }

    private void PositionAndSpawnPreviewNpc(Transform screenRoot)
    {
        // Position the NPC cleanly centered in the Meeting Room showcase behind the wooden counter
        // so the lower cut-off torso tucks naturally behind Visitors Table just like in-game.
        float npcY = _stage.CounterTopLocalY + 2.55f;
        _previewController.SetStagePosition(new Vector3(NPCDesignerStage.StageCenterX, npcY, -1.0f), 1.05f);
        _previewController.SpawnPreview(screenRoot);
    }

    private void BuildRightInventoryPanelBackdrop(Transform screenRoot)
    {
        if (_inventoryPanelBackdrop != null)
        {
            UnityEngine.Object.Destroy(_inventoryPanelBackdrop);
        }

        _inventoryPanelBackdrop = new GameObject("NPCDesigner_RightInventoryPanel");
        _inventoryPanelBackdrop.layer = screenRoot.gameObject.layer;
        _inventoryPanelBackdrop.transform.SetParent(screenRoot, worldPositionStays: false);
        _inventoryPanelBackdrop.transform.localPosition = Vector3.zero;
        _inventoryPanelBackdrop.transform.localScale = Vector3.one;

        var defaultMat = NPCAssetThumbnailRenderer.GetDefaultSpriteMaterial();
        int layer = screenRoot.gameObject.layer;

        // 1. Find the authentic Potion Craft right-side parchment Inventory Background sprite
        Sprite? panelBgSprite = FindSpriteByExactName("Inventory Background Var1")
            ?? FindSpriteByExactName("Inventory Background Var2");

        if (panelBgSprite != null)
        {
            var bgObj = new GameObject("Panel_WoodParchmentBg");
            bgObj.layer = layer;
            bgObj.transform.SetParent(_inventoryPanelBackdrop.transform, worldPositionStays: false);
            bgObj.transform.localPosition = new Vector3(PanelCenterX, 0f, 0f);

            // Native sprite is 5.64 x 14.4 units -> stretch X to 8.45 units so 4 columns of ingredient slots fit spaciously
            float targetWidth = 8.45f;
            float targetHeight = 14.45f;
            float sx = targetWidth / Mathf.Max(0.01f, panelBgSprite.bounds.size.x);
            float sy = targetHeight / Mathf.Max(0.01f, panelBgSprite.bounds.size.y);
            bgObj.transform.localScale = new Vector3(sx, sy, 1f);

            var sr = bgObj.AddComponent<SpriteRenderer>();
            sr.sprite = panelBgSprite;
            sr.sortingLayerName = "MenuElements";
            sr.sortingOrder = 12380;
            sr.maskInteraction = SpriteMaskInteraction.None;
            if (defaultMat != null) sr.sharedMaterial = defaultMat;
        }
        else
        {
            // Clean parchment fallback
            var bgObj = new GameObject("Panel_FallbackBg");
            bgObj.layer = layer;
            bgObj.transform.SetParent(_inventoryPanelBackdrop.transform, worldPositionStays: false);
            bgObj.transform.localPosition = new Vector3(PanelCenterX, 0f, 0f);
            bgObj.transform.localScale = new Vector3(8.45f, 14.45f, 1f);

            var sr = bgObj.AddComponent<SpriteRenderer>();
            sr.sprite = NPCAssetThumbnailRenderer.GetSolidSquareSprite();
            sr.color = new Color(0.89f, 0.81f, 0.66f, 1f);
            sr.sortingLayerName = "MenuElements";
            sr.sortingOrder = 12380;
            if (defaultMat != null) sr.sharedMaterial = defaultMat;
        }

        // 2. Inner sunken parchment well + authentic InventoryWindow Var1 border around the 4x4 ingredient slot grid
        const float wellCenterY = 1.10f;
        const float wellW = 6.72f;
        const float wellH = 6.36f;

        var wellSprite = NPCDesignerStage.LoadSpriteByExactName("InventoryWindow Background Var1");
        _backdropHasWindowFrame = wellSprite != null;
        var wellObj = new GameObject("Panel_GridWell");
        wellObj.layer = layer;
        wellObj.transform.SetParent(_inventoryPanelBackdrop.transform, worldPositionStays: false);
        wellObj.transform.localPosition = new Vector3(PanelCenterX, wellCenterY, 0f);
        var wellSr = wellObj.AddComponent<SpriteRenderer>();
        if (wellSprite != null)
        {
            wellSr.sprite = wellSprite;
            wellSr.color = Color.white;
            wellObj.transform.localScale = new Vector3(
                wellW / Mathf.Max(0.01f, wellSprite.bounds.size.x),
                wellH / Mathf.Max(0.01f, wellSprite.bounds.size.y),
                1f);
        }
        else
        {
            wellSr.sprite = NPCAssetThumbnailRenderer.GetSolidSquareSprite();
            wellSr.color = new Color(0.42f, 0.30f, 0.19f, 0.14f);
            wellObj.transform.localScale = new Vector3(wellW, wellH, 1f);
        }
        wellSr.sortingLayerName = "MenuElements";
        wellSr.sortingOrder = 12410;
        if (defaultMat != null) wellSr.sharedMaterial = defaultMat;

        // 3. Subtle dark wood separator edge along the left border of the right panel (x = 4.38)
        var borderObj = new GameObject("Panel_LeftBorder");
        borderObj.layer = layer;
        borderObj.transform.SetParent(_inventoryPanelBackdrop.transform, worldPositionStays: false);
        borderObj.transform.localPosition = new Vector3(4.38f, 0f, 0f);
        borderObj.transform.localScale = new Vector3(0.14f, 14.5f, 1f);

        var borderSr = borderObj.AddComponent<SpriteRenderer>();
        borderSr.sprite = NPCAssetThumbnailRenderer.GetSolidSquareSprite();
        borderSr.color = new Color(0.24f, 0.15f, 0.09f, 0.95f);
        borderSr.sortingLayerName = "MenuElements";
        borderSr.sortingOrder = 12420;
        if (defaultMat != null) borderSr.sharedMaterial = defaultMat;

        // 4. Framed parchment nameplate on the wooden counter apron for Genere / Umore
        var ledgeBorderObj = new GameObject("Stage_CounterButtonBorder");
        ledgeBorderObj.layer = layer;
        ledgeBorderObj.transform.SetParent(_inventoryPanelBackdrop.transform, worldPositionStays: false);
        ledgeBorderObj.transform.localPosition = new Vector3(NPCDesignerStage.StageCenterX, -5.95f, 0f);
        ledgeBorderObj.transform.localScale = new Vector3(8.72f, 1.24f, 1f);
        var ledgeBorderSr = ledgeBorderObj.AddComponent<SpriteRenderer>();
        ledgeBorderSr.sprite = NPCAssetThumbnailRenderer.GetSolidSquareSprite();
        ledgeBorderSr.color = new Color(0.28f, 0.18f, 0.10f, 0.95f);
        ledgeBorderSr.sortingLayerName = "MenuElements";
        ledgeBorderSr.sortingOrder = 12388;
        if (defaultMat != null) ledgeBorderSr.sharedMaterial = defaultMat;

        var ledgePlateObj = new GameObject("Stage_CounterButtonBackdrop");
        ledgePlateObj.layer = layer;
        ledgePlateObj.transform.SetParent(_inventoryPanelBackdrop.transform, worldPositionStays: false);
        ledgePlateObj.transform.localPosition = new Vector3(NPCDesignerStage.StageCenterX, -5.95f, 0f);
        ledgePlateObj.transform.localScale = new Vector3(8.56f, 1.08f, 1f);

        var ledgeSr = ledgePlateObj.AddComponent<SpriteRenderer>();
        ledgeSr.sprite = NPCAssetThumbnailRenderer.GetSolidSquareSprite();
        ledgeSr.color = new Color(0.86f, 0.77f, 0.60f, 0.92f);
        ledgeSr.sortingLayerName = "MenuElements";
        ledgeSr.sortingOrder = 12390;
        if (defaultMat != null) ledgeSr.sharedMaterial = defaultMat;
    }

    private void CreateStatusFooter(Transform screenRoot, Transform? textTemplate)
    {
        if (textTemplate == null) return;

        var footerObj = UnityEngine.Object.Instantiate(textTemplate.gameObject, screenRoot);
        footerObj.name = "StatusFooterLabel";
        footerObj.transform.localPosition = new Vector3(PanelCenterX, -3.35f, 0f);
        footerObj.transform.localScale = Vector3.one;
        var footerLoc = footerObj.GetComponent<LocalizedText>();
        if (footerLoc != null)
        {
            footerLoc.key = null!;
            footerLoc.enabled = false;
        }
        _statusFooterTmp = footerObj.GetComponent<TextMeshPro>();
        if (_statusFooterTmp != null)
        {
            _statusFooterTmp.enableAutoSizing = false;
            _statusFooterTmp.enableWordWrapping = false;
            _statusFooterTmp.text = "Clicca un riquadro per applicare l'asset";
            _statusFooterTmp.fontSize = 2.35f;
            _statusFooterTmp.alignment = TextAlignmentOptions.Center;
            _statusFooterTmp.sortingOrder = 12720;
        }
    }

    private void BuildGroupTabs(Transform parent, MenuButton template)
    {
        float[] tabXs = { 5.90f, 7.70f, 9.50f, 11.30f };
        const float y = 5.60f;

        for (int i = 0; i < GroupLabels.Length; i++)
        {
            int groupIdx = i;
            _groupTabButtons[i] = CloneButton(
                template, parent, $"GroupTab_{groupIdx}", GroupLabels[i],
                new Vector3(tabXs[i], y, 0f),
                fontSize: 2.75f, minWidth: 1.48f, maxWidth: 1.68f,
                onClick: () =>
                {
                    _currentGroup = (AssetGroup)groupIdx;
                    _currentSubCategoryIndex = 0;
                    _currentPage = 0;
                    _statusMessage = $"Gruppo: {GroupLabels[groupIdx]}";
                    RefreshPanelUI();
                });
        }
    }

    private void BuildSubCategorySwitcher(Transform parent, MenuButton template)
    {
        const float y = 4.65f;

        _subCatLeftBtn = CloneButton(
            template, parent, "SubCat_Left", "<",
            new Vector3(5.55f, y, 0f),
            fontSize: 3.3f, minWidth: 0.72f, maxWidth: 0.90f,
            onClick: () =>
            {
                var subs = GetSubCategoriesForGroup(_currentGroup);
                if (subs.Count == 0) return;
                _currentSubCategoryIndex = (_currentSubCategoryIndex - 1 + subs.Count) % subs.Count;
                _currentPage = 0;
                RefreshPanelUI();
            });

        _subCatCenterBtn = CloneButton(
            template, parent, "SubCat_Center", "Corpo",
            new Vector3(PanelCenterX, y, 0f),
            fontSize: 2.95f, minWidth: 3.6f, maxWidth: 4.8f,
            onClick: () =>
            {
                var subs = GetSubCategoriesForGroup(_currentGroup);
                if (subs.Count <= 1) return;
                _currentSubCategoryIndex = (_currentSubCategoryIndex + 1) % subs.Count;
                _currentPage = 0;
                RefreshPanelUI();
            });

        _subCatRightBtn = CloneButton(
            template, parent, "SubCat_Right", ">",
            new Vector3(11.65f, y, 0f),
            fontSize: 3.3f, minWidth: 0.72f, maxWidth: 0.90f,
            onClick: () =>
            {
                var subs = GetSubCategoriesForGroup(_currentGroup);
                if (subs.Count == 0) return;
                _currentSubCategoryIndex = (_currentSubCategoryIndex + 1) % subs.Count;
                _currentPage = 0;
                RefreshPanelUI();
            });
    }

    private void BuildAssetSlotGrid(Transform parent, MenuButton template)
    {
        _slotViews.Clear();

        float startX = PanelCenterX - (GridColumns - 1) * 0.5f * SlotSpacingX;

        for (int i = 0; i < PageSize; i++)
        {
            int col = i % GridColumns;
            int row = i / GridColumns;

            float x = startX + col * SlotSpacingX;
            float y = GridTopY - row * SlotSpacingY;

            var obj = UnityEngine.Object.Instantiate(template.gameObject, parent);
            obj.name = $"AssetSlot_{i:00}";
            obj.transform.localPosition = new Vector3(x, y, 0f);
            obj.transform.localScale = Vector3.one;

            foreach (var existing in obj.GetComponents<DesignerButtonHandler>())
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            var btn = obj.GetComponent<MenuButton>();
            btn.thisButtonIs = MenuButton.MainMenuButtons.CustomAction;
            if (btn.localizedText != null)
            {
                btn.localizedText.key = null!;
                btn.localizedText.enabled = false;
            }

            if (btn.textObject != null)
            {
                btn.textObject.enableAutoSizing = false;
                btn.textObject.enableWordWrapping = false;
                btn.textObject.overflowMode = TextOverflowModes.Truncate;
                btn.textObject.fontSize = 1.85f;
                btn.textObject.alignment = TextAlignmentOptions.Center;
                btn.textObject.text = "";
                btn.textObject.sortingOrder = 12720;
                btn.textObject.transform.localPosition = new Vector3(0f, -0.52f, 0f);
            }

            var handler = obj.AddComponent<DesignerButtonHandler>();
            handler.IsFixedSquareSlot = true;
            handler.FixedSlotSize = SlotSize;
            handler.CustomFontSize = 1.85f;

            var visuals = obj.AddComponent<DesignerSlotVisuals>();
            visuals.SlotSize = SlotSize;
            if (_slotIdleSprites.Length > 0)
            {
                visuals.IdleSprite = _slotIdleSprites[i % _slotIdleSprites.Length];
            }
            if (_slotHoverSprites.Length > 0)
            {
                visuals.HoverSprite = _slotHoverSprites[i % _slotHoverSprites.Length];
            }

            // Golden equipped highlight frame behind thumbnail
            var hlObj = new GameObject("EquippedHighlight");
            hlObj.layer = obj.layer;
            hlObj.transform.SetParent(obj.transform, worldPositionStays: false);
            hlObj.transform.localPosition = Vector3.zero;
            hlObj.transform.localScale = new Vector3(SlotSize * 0.92f, SlotSize * 0.92f, 1f);
            var hlSr = hlObj.AddComponent<SpriteRenderer>();
            hlSr.sprite = NPCAssetThumbnailRenderer.GetSolidSquareSprite();
            hlSr.color = new Color(0.95f, 0.78f, 0.30f, 0.35f);
            hlSr.sortingLayerName = "MenuElements";
            hlSr.sortingOrder = 12555;
            hlSr.enabled = false;
            var defaultMat = NPCAssetThumbnailRenderer.GetDefaultSpriteMaterial();
            if (defaultMat != null) hlSr.sharedMaterial = defaultMat;
            visuals.EquippedHighlightRenderer = hlSr;

            // Layered sprite thumbnail container
            var thumbObj = new GameObject("ThumbnailRoot");
            thumbObj.layer = obj.layer;
            thumbObj.transform.SetParent(obj.transform, worldPositionStays: false);
            thumbObj.transform.localPosition = Vector3.zero;
            thumbObj.transform.localScale = Vector3.one;
            visuals.ThumbnailContainer = thumbObj.transform;

            _slotViews.Add(new SlotButtonView
            {
                Button = btn,
                Handler = handler,
                Visuals = visuals,
                ThumbnailRoot = thumbObj.transform
            });
        }
    }

    private void BuildPaginationBar(Transform parent, MenuButton template)
    {
        const float y = -2.45f;

        _pagePrevBtn = CloneButton(
            template, parent, "Page_Prev", "< Prec",
            new Vector3(6.25f, y, 0f),
            fontSize: 2.65f, minWidth: 1.35f, maxWidth: 1.75f,
            onClick: () =>
            {
                if (_currentPage > 0)
                {
                    _currentPage--;
                    RefreshPanelUI();
                }
            });

        _pageInfoBtn = CloneButton(
            template, parent, "Page_Info", "Pag. 1 / 1",
            new Vector3(PanelCenterX, y, 0f),
            fontSize: 2.65f, minWidth: 1.8f, maxWidth: 2.4f,
            onClick: null);

        _pageNextBtn = CloneButton(
            template, parent, "Page_Next", "Succ >",
            new Vector3(10.95f, y, 0f),
            fontSize: 2.65f, minWidth: 1.35f, maxWidth: 1.75f,
            onClick: () =>
            {
                var items = BuildCurrentSubCategoryEntries();
                int totalPages = Mathf.Max(1, (items.Count + PageSize - 1) / PageSize);
                if (_currentPage + 1 < totalPages)
                {
                    _currentPage++;
                    RefreshPanelUI();
                }
            });
    }

    private void BuildActionButtons(Transform parent, MenuButton template)
    {
        const float actionY = -4.25f;
        _randomBtn = CloneButton(
            template, parent, "Action_Random", "Casuale",
            new Vector3(6.85f, actionY, 0f),
            fontSize: 3.05f, minWidth: 2.0f, maxWidth: 2.8f,
            onClick: () =>
            {
                _previewController.Randomize();
                _statusMessage = "Aspetto generato casualmente";
                RefreshPanelUI();
            });

        _saveBtn = CloneButton(
            template, parent, "Action_Save", "Salva Preset",
            new Vector3(10.35f, actionY, 0f),
            fontSize: 3.05f, minWidth: 2.2f, maxWidth: 3.0f,
            onClick: () =>
            {
                SaveCurrentAsPreset();
            });

        // Left counter-ledge controls (framed on the wooden counter below the NPC)
        const float counterLedgeY = -5.95f;
        _genderBtn = CloneButton(
            template, parent, "Action_Gender", "Genere: Maschio",
            new Vector3(NPCDesignerStage.StageCenterX - 2.15f, counterLedgeY, 0f),
            fontSize: 3.05f, minWidth: 2.6f, maxWidth: 3.7f,
            onClick: () =>
            {
                var nextGender = (NPCGenderMode)(((int)_previewController.CurrentGender + 1) % GenderNames.Length);
                _previewController.SetGender(nextGender);
                _currentPage = 0;
                _statusMessage = $"Genere: {GenderNames[(int)nextGender]}";
                RefreshPanelUI();
            });

        _moodBtn = CloneButton(
            template, parent, "Action_Mood", "Umore: Felice",
            new Vector3(NPCDesignerStage.StageCenterX + 2.15f, counterLedgeY, 0f),
            fontSize: 3.05f, minWidth: 2.6f, maxWidth: 3.7f,
            onClick: () =>
            {
                int nextMood = (_previewController.SelectedMoodIndex + 1) % MoodNames.Length;
                _previewController.ApplyMood(nextMood, triggerVibe: true);
                _statusMessage = $"Umore: {MoodNames[nextMood]}";
                RefreshPanelUI();
            });
    }

    private static MenuButton CloneButton(
        MenuButton template,
        Transform parent,
        string name,
        string text,
        Vector3 localPos,
        float fontSize,
        float minWidth,
        float maxWidth,
        Action? onClick)
    {
        var obj = UnityEngine.Object.Instantiate(template.gameObject, parent);
        obj.name = name;
        obj.transform.localPosition = localPos;
        obj.transform.localScale = Vector3.one;

        foreach (var existing in obj.GetComponents<DesignerButtonHandler>())
        {
            UnityEngine.Object.DestroyImmediate(existing);
        }

        var btn = obj.GetComponent<MenuButton>();
        btn.thisButtonIs = MenuButton.MainMenuButtons.CustomAction;
        if (btn.localizedText != null)
        {
            btn.localizedText.key = null!;
            btn.localizedText.enabled = false;
        }

        if (btn.textObject != null)
        {
            btn.textObject.enableAutoSizing = false;
            btn.textObject.enableWordWrapping = false;
            btn.textObject.overflowMode = TextOverflowModes.Overflow;
            btn.textObject.fontSize = fontSize;
            btn.textObject.text = text;
        }

        ElevateButtonSorting(btn, 12650);

        var handler = obj.AddComponent<DesignerButtonHandler>();
        handler.CustomFontSize = fontSize;
        handler.MinColliderWidth = minWidth;
        handler.MaxColliderWidth = maxWidth;
        handler.ColliderHeight = 0.56f;
        handler.OnClick = onClick;

        if (obj.activeInHierarchy)
        {
            btn.UpdateUnderlaySize();
            handler.EnforceCustomBounds(btn);
        }

        return btn;
    }

    private static void ElevateButtonSorting(MenuButton btn, int baseOrder)
    {
        if (btn == null) return;
        if (btn.substrateRenderer != null)
        {
            btn.substrateRenderer.sortingLayerName = "MenuElements";
            btn.substrateRenderer.sortingOrder = baseOrder;
        }
        if (btn.hoverRenderer != null)
        {
            btn.hoverRenderer.sortingLayerName = "MenuElements";
            btn.hoverRenderer.sortingOrder = baseOrder + 5;
        }
        if (btn.textObject != null)
        {
            btn.textObject.sortingOrder = baseOrder + 20;
        }
    }

    public static void SetButtonText(MenuButton? btn, string newText)
    {
        if (btn == null || btn.textObject == null) return;
        var handler = btn.GetComponent<DesignerButtonHandler>();
        if (handler != null)
        {
            btn.textObject.enableAutoSizing = false;
            btn.textObject.enableWordWrapping = false;
            btn.textObject.fontSize = handler.CustomFontSize;
        }

        btn.textObject.text = newText;
        if (btn.gameObject.activeInHierarchy)
        {
            btn.textObject.ForceMeshUpdate(true);
            btn.UpdateUnderlaySize();
            handler?.EnforceCustomBounds(btn);
        }
    }

    public void OnScreenOpened()
    {
        var screenObj = _designerScreenObj?.GetComponent<MenuScreenObject>();
        if (screenObj != null)
        {
            Managers.Menu.currentMenuScreenObject = screenObj;
        }

        // Rebuild stage & inventory window frame if Addressables bundles loaded after initial menu setup
        if (_designerScreenObj != null)
        {
            _stage.EnsureBuilt(_designerScreenObj.transform);
            if (!_backdropHasWindowFrame)
            {
                BuildRightInventoryPanelBackdrop(_designerScreenObj.transform);
            }
            float npcY = _stage.CounterTopLocalY + 2.55f;
            _previewController.SetStagePosition(new Vector3(NPCDesignerStage.StageCenterX, npcY, -1.0f), 1.05f);
        }

        if (_slotIdleSprites.Length == 0)
        {
            LoadNativeInventorySlotSprites();
            for (int i = 0; i < _slotViews.Count; i++)
            {
                if (_slotIdleSprites.Length > 0)
                {
                    _slotViews[i].Visuals.IdleSprite = _slotIdleSprites[i % _slotIdleSprites.Length];
                }
                if (_slotHoverSprites.Length > 0)
                {
                    _slotViews[i].Visuals.HoverSprite = _slotHoverSprites[i % _slotHoverSprites.Length];
                }
            }
        }

        HideMainMenuClutter();
        _previewController.FixSortingLayers();
        _previewController.IdleAnimator?.TriggerLookAroundSoon();
        RefreshPanelUI();
        RefreshAllButtonBounds();
        _previewController.LogDiagnosticInfo();
    }

    public void OnScreenClosed()
    {
        RestoreMainMenuClutter();
    }

    private void HideMainMenuClutter()
    {
        _hiddenClutterObjects.Clear();

        foreach (var social in Resources.FindObjectsOfTypeAll<SocialMediaButtons>())
        {
            if (social != null && social.gameObject.activeSelf)
            {
                social.gameObject.SetActive(false);
                _hiddenClutterObjects.Add(social.gameObject);
            }
        }

        foreach (var mail in Resources.FindObjectsOfTypeAll<OpenMailSubscriptionWindowButton>())
        {
            if (mail != null && mail.gameObject.activeSelf)
            {
                mail.gameObject.SetActive(false);
                _hiddenClutterObjects.Add(mail.gameObject);
            }
        }

        foreach (var lang in Resources.FindObjectsOfTypeAll<PotionCraft.ObjectBased.UIElements.SelectLanguageMenu.OpenSelectLanguageWindowButton>())
        {
            if (lang != null && lang.gameObject.activeSelf)
            {
                lang.gameObject.SetActive(false);
                _hiddenClutterObjects.Add(lang.gameObject);
            }
        }
    }

    private void RestoreMainMenuClutter()
    {
        foreach (var obj in _hiddenClutterObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        _hiddenClutterObjects.Clear();
    }

    private void RefreshAllButtonBounds()
    {
        for (int i = 0; i < _groupTabButtons.Length; i++)
        {
            RefreshSingleButton(_groupTabButtons[i]);
        }

        RefreshSingleButton(_subCatLeftBtn);
        RefreshSingleButton(_subCatCenterBtn);
        RefreshSingleButton(_subCatRightBtn);
        RefreshSingleButton(_pagePrevBtn);
        RefreshSingleButton(_pageInfoBtn);
        RefreshSingleButton(_pageNextBtn);
        RefreshSingleButton(_randomBtn);
        RefreshSingleButton(_saveBtn);
        RefreshSingleButton(_backBtn);
        RefreshSingleButton(_moodBtn);
        RefreshSingleButton(_genderBtn);

        foreach (var slot in _slotViews)
        {
            RefreshSingleButton(slot.Button);
        }
    }

    private static void RefreshSingleButton(MenuButton? btn)
    {
        if (btn == null || !btn.gameObject.activeInHierarchy) return;
        btn.UpdateUnderlaySize();
        btn.GetComponent<DesignerButtonHandler>()?.EnforceCustomBounds(btn);
    }

    private List<AssetSubCategory> GetSubCategoriesForGroup(AssetGroup group)
    {
        return group switch
        {
            AssetGroup.Body => _previewController.CurrentGender == NPCGenderMode.Male
                ? new List<AssetSubCategory>
                {
                    AssetSubCategory.Body,
                    AssetSubCategory.Face,
                    AssetSubCategory.Eyes,
                    AssetSubCategory.SkullShape
                }
                : new List<AssetSubCategory>
                {
                    AssetSubCategory.Body,
                    AssetSubCategory.Face,
                    AssetSubCategory.Eyes,
                    AssetSubCategory.SkullShape,
                    AssetSubCategory.Breast
                },
            AssetGroup.Style => new List<AssetSubCategory>
            {
                AssetSubCategory.Hairstyle,
                AssetSubCategory.Beard,
                AssetSubCategory.Hat,
                AssetSubCategory.AccAboveHair,
                AssetSubCategory.AccFace,
                AssetSubCategory.AccSkull,
                AssetSubCategory.AccShortHair,
                AssetSubCategory.AccHandBack,
                AssetSubCategory.AccHandFront,
                AssetSubCategory.AccBehindBody,
                AssetSubCategory.AccBody
            },
            AssetGroup.Colors => new List<AssetSubCategory>
            {
                AssetSubCategory.SkinColor,
                AssetSubCategory.HairColor,
                AssetSubCategory.ClothesColor1,
                AssetSubCategory.ClothesColor2,
                AssetSubCategory.ClothesColor3,
                AssetSubCategory.ClothesColor4
            },
            AssetGroup.Presets => new List<AssetSubCategory>
            {
                AssetSubCategory.SavedPresets
            },
            _ => new List<AssetSubCategory> { AssetSubCategory.Body }
        };
    }

    private static string GetSubCategoryDisplayName(AssetSubCategory sub) => sub switch
    {
        AssetSubCategory.Body => "Corpo",
        AssetSubCategory.Face => "Viso",
        AssetSubCategory.Eyes => "Occhi",
        AssetSubCategory.SkullShape => "Cranio",
        AssetSubCategory.Breast => "Seno",
        AssetSubCategory.Hairstyle => "Capelli",
        AssetSubCategory.Beard => "Barba",
        AssetSubCategory.Hat => "Cappello",
        AssetSubCategory.AccAboveHair => "Acc. Capelli",
        AssetSubCategory.AccFace => "Acc. Viso",
        AssetSubCategory.AccSkull => "Acc. Testa",
        AssetSubCategory.AccShortHair => "Acc. Nuca",
        AssetSubCategory.AccHandBack => "Mano Dx",
        AssetSubCategory.AccHandFront => "Mano Sx",
        AssetSubCategory.AccBehindBody => "Schiena",
        AssetSubCategory.AccBody => "Torso",
        AssetSubCategory.SkinColor => "Carnagione",
        AssetSubCategory.HairColor => "Tinta Capelli",
        AssetSubCategory.ClothesColor1 => "Colore Veste 1",
        AssetSubCategory.ClothesColor2 => "Colore Veste 2",
        AssetSubCategory.ClothesColor3 => "Colore Veste 3",
        AssetSubCategory.ClothesColor4 => "Colore Veste 4",
        AssetSubCategory.SavedPresets => "Presets Salvati",
        _ => "Asset"
    };

    private AssetSubCategory GetActiveSubCategory()
    {
        var subs = GetSubCategoriesForGroup(_currentGroup);
        if (subs.Count == 0) return AssetSubCategory.Body;
        _currentSubCategoryIndex = Mathf.Clamp(_currentSubCategoryIndex, 0, subs.Count - 1);
        return subs[_currentSubCategoryIndex];
    }

    public void RefreshPanelUI()
    {
        // Update group tab button indicators
        for (int i = 0; i < GroupLabels.Length; i++)
        {
            bool active = (int)_currentGroup == i;
            string label = active ? $"[{GroupLabels[i]}]" : GroupLabels[i];
            SetButtonText(_groupTabButtons[i], label);
        }

        var subs = GetSubCategoriesForGroup(_currentGroup);
        var activeSub = GetActiveSubCategory();
        string subTitle = GetSubCategoryDisplayName(activeSub);

        var entries = BuildEntriesForSubCategory(activeSub);
        int totalPages = Mathf.Max(1, (entries.Count + PageSize - 1) / PageSize);
        _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);

        SetButtonText(_subCatCenterBtn, $"{subTitle} ({_currentSubCategoryIndex + 1}/{subs.Count})");
        if (_subCatLeftBtn != null) _subCatLeftBtn.gameObject.SetActive(subs.Count > 1);
        if (_subCatRightBtn != null) _subCatRightBtn.gameObject.SetActive(subs.Count > 1);

        // Update gender & mood buttons on the counter ledge
        int genderIdx = Mathf.Clamp((int)_previewController.CurrentGender, 0, GenderNames.Length - 1);
        SetButtonText(_genderBtn, $"Genere: {GenderNames[genderIdx]}");

        int moodIdx = Mathf.Clamp(_previewController.SelectedMoodIndex, 0, MoodNames.Length - 1);
        SetButtonText(_moodBtn, $"Umore: {MoodNames[moodIdx]}");

        // Populate 4x4 ingredient-style asset slots
        var palette = _previewController.GetCurrentPalette();
        int startIndex = _currentPage * PageSize;

        for (int i = 0; i < _slotViews.Count; i++)
        {
            var view = _slotViews[i];
            int dataIndex = startIndex + i;

            if (dataIndex >= entries.Count)
            {
                view.Button.gameObject.SetActive(false);
                NPCAssetThumbnailRenderer.ClearThumbnail(view.ThumbnailRoot);
                continue;
            }

            var entry = entries[dataIndex];
            view.Button.gameObject.SetActive(true);
            view.Visuals.IsEquipped = entry.IsSelected;
            view.Handler.OnClick = entry.OnSelect;

            string caption = entry.IsSelected ? $"* {entry.Label}" : entry.Label;
            if (view.Button.textObject != null)
            {
                view.Button.textObject.text = caption;
                view.Button.textObject.fontSize = 2.0f;
                view.Button.textObject.color = new Color(0.15f, 0.09f, 0.04f, 1f);
                view.Button.textObject.ForceMeshUpdate(true);
            }

            int thumbOrder = 12610 + i;
            if (entry.IsNone)
            {
                NPCAssetThumbnailRenderer.RenderNone(view.ThumbnailRoot, thumbOrder);
            }
            else if (entry.IsColor)
            {
                NPCAssetThumbnailRenderer.RenderColorSwatch(view.ThumbnailRoot, entry.SwatchColor, thumbOrder);
            }
            else if (entry.IsPreset)
            {
                NPCAssetThumbnailRenderer.RenderPresetIcon(view.ThumbnailRoot, thumbOrder);
            }
            else
            {
                NPCAssetThumbnailRenderer.RenderAppearancePart(view.ThumbnailRoot, entry.Part, palette, thumbOrder);
            }

            view.Handler.EnforceCustomBounds(view.Button);
            view.Visuals.ApplyVisualState(view.Button);
        }

        // Pagination bar (only show clickable prev/next arrows when applicable so locked buttons never look faded)
        bool showPagination = totalPages > 1;
        if (_pagePrevBtn != null)
        {
            _pagePrevBtn.Locked = false;
            _pagePrevBtn.gameObject.SetActive(showPagination && _currentPage > 0);
        }
        if (_pageNextBtn != null)
        {
            _pageNextBtn.Locked = false;
            _pageNextBtn.gameObject.SetActive(showPagination && _currentPage + 1 < totalPages);
        }
        SetButtonText(_pageInfoBtn, showPagination ? $"Pag. {_currentPage + 1} / {totalPages}" : $"{entries.Count} asset");

        if (_statusFooterTmp != null)
        {
            _statusFooterTmp.text = string.IsNullOrEmpty(_statusMessage)
                ? $"{subTitle}: clicca uno slot per applicare"
                : _statusMessage;
            _statusFooterTmp.ForceMeshUpdate(true);
        }
    }

    private List<AssetSlotEntry> BuildCurrentSubCategoryEntries()
    {
        return BuildEntriesForSubCategory(GetActiveSubCategory());
    }

    private List<AssetSlotEntry> BuildEntriesForSubCategory(AssetSubCategory sub)
    {
        var entries = new List<AssetSlotEntry>();

        switch (sub)
        {
            case AssetSubCategory.Body:
                AddPartEntries(entries, _previewController.GetCurrentBodies(), _previewController.SelectedBodyIndex, "Corpo", idx =>
                {
                    _previewController.ApplyBody(idx);
                    _statusMessage = $"Corpo #{idx + 1} applicato";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.Face:
                AddPartEntries(entries, _previewController.GetCurrentFaces(), _previewController.SelectedFaceIndex, "Viso", idx =>
                {
                    _previewController.ApplyFace(idx);
                    _statusMessage = $"Viso #{idx + 1} applicato";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.Eyes:
                AddPartEntries(entries, NPCPartRegistry.EyesList, _previewController.SelectedEyesIndex, "Occhi", idx =>
                {
                    _previewController.ApplyEyes(idx);
                    _statusMessage = $"Occhi #{idx + 1} applicati";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.SkullShape:
                AddPartEntries(entries, _previewController.GetCurrentSkullShapes(), _previewController.SelectedSkullIndex, "Cranio", idx =>
                {
                    _previewController.ApplySkullShape(idx);
                    _statusMessage = $"Cranio #{idx + 1} applicato";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.Breast:
                AddOptionalPartEntries(entries, _previewController.GetCurrentCompatibleBreasts(), _previewController.SelectedBreastIndex, "Seno", idx =>
                {
                    _previewController.ApplyBreast(idx);
                    _statusMessage = idx < 0 ? "Seno rimosso" : $"Seno #{idx + 1} applicato";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.Hairstyle:
                AddPartEntries(entries, _previewController.GetCurrentHairstyles(), _previewController.SelectedHairIndex, "Capelli", idx =>
                {
                    _previewController.ApplyHairstyle(idx);
                    _statusMessage = idx == 0 ? "Capelli rimossi" : $"Capelli #{idx} applicati";
                    RefreshPanelUI();
                }, firstIsNone: true);
                break;

            case AssetSubCategory.Beard:
                AddPartEntries(entries, NPCPartRegistry.Beards, _previewController.SelectedBeardIndex, "Barba", idx =>
                {
                    _previewController.ApplyBeard(idx);
                    _statusMessage = idx == 0 ? "Barba rimossa" : $"Barba #{idx} applicata";
                    RefreshPanelUI();
                }, firstIsNone: true);
                break;

            case AssetSubCategory.Hat:
                AddPartEntries(entries, NPCPartRegistry.Hats, _previewController.SelectedHatIndex, "Cappello", idx =>
                {
                    _previewController.ApplyHat(idx);
                    _statusMessage = idx == 0 ? "Cappello rimosso" : $"Cappello #{idx} applicato";
                    RefreshPanelUI();
                }, firstIsNone: true);
                break;

            case AssetSubCategory.AccAboveHair:
                AddOptionalPartEntries(entries, NPCPartRegistry.AccessoriesAboveHair, _previewController.SelectedAccAboveHairIndex, "Acc.", idx =>
                {
                    _previewController.ApplyAccAboveHair(idx);
                    _statusMessage = idx < 0 ? "Ornamento rimosso" : $"Ornamento #{idx + 1} applicato";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.AccFace:
                AddOptionalPartEntries(entries, NPCPartRegistry.AccessoriesFace, _previewController.SelectedAccFaceIndex, "Viso", idx =>
                {
                    _previewController.ApplyAccFace(idx);
                    _statusMessage = idx < 0 ? "Acc. viso rimosso" : $"Acc. viso #{idx + 1} applicato";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.AccSkull:
                AddOptionalPartEntries(entries, NPCPartRegistry.AccessoriesSkullShape, _previewController.SelectedAccSkullIndex, "Testa", idx =>
                {
                    _previewController.ApplyAccSkull(idx);
                    _statusMessage = idx < 0 ? "Acc. testa rimosso" : $"Acc. testa #{idx + 1} applicato";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.AccShortHair:
                AddOptionalPartEntries(entries, NPCPartRegistry.AccessoriesShortHair, _previewController.SelectedAccShortHairIndex, "Nuca", idx =>
                {
                    _previewController.ApplyAccShortHair(idx);
                    _statusMessage = idx < 0 ? "Acc. nuca rimosso" : $"Acc. nuca #{idx + 1} applicato";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.AccHandBack:
                AddOptionalPartEntries(entries, NPCPartRegistry.AccessoriesHandBack, _previewController.SelectedAccHandBackIndex, "Mano Dx", idx =>
                {
                    _previewController.ApplyAccHandBack(idx);
                    _statusMessage = idx < 0 ? "Oggetto mano dx rimosso" : $"Mano dx #{idx + 1} applicato";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.AccHandFront:
                AddOptionalPartEntries(entries, NPCPartRegistry.AccessoriesHandFront, _previewController.SelectedAccHandFrontIndex, "Mano Sx", idx =>
                {
                    _previewController.ApplyAccHandFront(idx);
                    _statusMessage = idx < 0 ? "Oggetto mano sx rimosso" : $"Mano sx #{idx + 1} applicato";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.AccBehindBody:
                AddOptionalPartEntries(entries, NPCPartRegistry.AccessoriesBehindBody, _previewController.SelectedAccBehindBodyIndex, "Schiena", idx =>
                {
                    _previewController.ApplyAccBehindBody(idx);
                    _statusMessage = idx < 0 ? "Acc. schiena rimosso" : $"Schiena #{idx + 1} applicato";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.AccBody:
                AddOptionalPartEntries(entries, NPCPartRegistry.AccessoriesBody, _previewController.SelectedAccBodyIndex, "Torso", idx =>
                {
                    _previewController.ApplyAccBody(idx);
                    _statusMessage = idx < 0 ? "Acc. torso rimosso" : $"Torso #{idx + 1} applicato";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.SkinColor:
                AddColorEntries(entries, NPCPartRegistry.SkinColors, _previewController.SelectedSkinColorIndex, "Pelle", idx =>
                {
                    _previewController.ApplySkinColor(idx);
                    _statusMessage = $"Carnagione #{idx + 1}";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.HairColor:
                AddColorEntries(entries, NPCPartRegistry.HairColors, _previewController.SelectedHairColorIndex, "Capelli", idx =>
                {
                    _previewController.ApplyHairColor(idx);
                    _statusMessage = $"Tinta capelli #{idx + 1}";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.ClothesColor1:
                AddColorEntries(entries, NPCPartRegistry.ClothesColors1, _previewController.SelectedClothes1ColorIndex, "Veste 1", idx =>
                {
                    _previewController.ApplyClothesColor(1, idx);
                    _statusMessage = $"Colore veste 1: #{idx + 1}";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.ClothesColor2:
                AddColorEntries(entries, NPCPartRegistry.ClothesColors2, _previewController.SelectedClothes2ColorIndex, "Veste 2", idx =>
                {
                    _previewController.ApplyClothesColor(2, idx);
                    _statusMessage = $"Colore veste 2: #{idx + 1}";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.ClothesColor3:
                AddColorEntries(entries, NPCPartRegistry.ClothesColors3, _previewController.SelectedClothes3ColorIndex, "Veste 3", idx =>
                {
                    _previewController.ApplyClothesColor(3, idx);
                    _statusMessage = $"Colore veste 3: #{idx + 1}";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.ClothesColor4:
                AddColorEntries(entries, NPCPartRegistry.ClothesColors4, _previewController.SelectedClothes4ColorIndex, "Veste 4", idx =>
                {
                    _previewController.ApplyClothesColor(4, idx);
                    _statusMessage = $"Colore veste 4: #{idx + 1}";
                    RefreshPanelUI();
                });
                break;

            case AssetSubCategory.SavedPresets:
                var presetNames = NPCPresetManager.GetSavedPresetNames();
                for (int i = 0; i < presetNames.Count; i++)
                {
                    string presetName = presetNames[i];
                    string shortName = presetName.Length > 10
                        ? presetName.Substring(0, 10)
                        : presetName;
                    entries.Add(new AssetSlotEntry
                    {
                        Label = shortName,
                        IsPreset = true,
                        OnSelect = () =>
                        {
                            var loaded = NPCPresetManager.LoadPreset(presetName);
                            if (loaded != null)
                            {
                                _previewController.ApplyPreset(loaded);
                                _statusMessage = $"Caricato: {presetName}";
                                RefreshPanelUI();
                            }
                        }
                    });
                }
                break;
        }

        return entries;
    }

    private static void AddPartEntries<T>(
        List<AssetSlotEntry> entries,
        List<T> parts,
        int selectedIndex,
        string prefix,
        Action<int> onPick,
        bool firstIsNone = false) where T : AppearancePart
    {
        for (int i = 0; i < parts.Count; i++)
        {
            int idx = i;
            bool isNone = firstIsNone && idx == 0;
            entries.Add(new AssetSlotEntry
            {
                Label = isNone ? "Nessuno" : $"#{idx + (firstIsNone ? 0 : 1)}",
                IsNone = isNone,
                Part = isNone ? null : parts[idx],
                IsSelected = idx == selectedIndex,
                OnSelect = () => onPick(idx)
            });
        }
    }

    private static void AddOptionalPartEntries<T>(
        List<AssetSlotEntry> entries,
        List<T> parts,
        int selectedIndex,
        string prefix,
        Action<int> onPick) where T : AppearancePart
    {
        entries.Add(new AssetSlotEntry
        {
            Label = "Nessuno",
            IsNone = true,
            IsSelected = selectedIndex < 0,
            OnSelect = () => onPick(-1)
        });

        for (int i = 0; i < parts.Count; i++)
        {
            int idx = i;
            entries.Add(new AssetSlotEntry
            {
                Label = $"#{idx + 1}",
                Part = parts[idx],
                IsSelected = idx == selectedIndex,
                OnSelect = () => onPick(idx)
            });
        }
    }

    private static void AddColorEntries(
        List<AssetSlotEntry> entries,
        List<Color> colors,
        int selectedIndex,
        string prefix,
        Action<int> onPick)
    {
        for (int i = 0; i < colors.Count; i++)
        {
            int idx = i;
            entries.Add(new AssetSlotEntry
            {
                Label = $"#{idx + 1}",
                IsColor = true,
                SwatchColor = colors[idx],
                IsSelected = idx == selectedIndex,
                OnSelect = () => onPick(idx)
            });
        }
    }

    private void SaveCurrentAsPreset()
    {
        string presetName = $"NPC_{DateTime.Now:yyyyMMdd_HHmmss}";
        var preset = _previewController.ExportPreset(presetName);
        if (NPCPresetManager.SavePreset(preset))
        {
            _statusMessage = $"Preset salvato: {presetName}";
            Plugin.Log?.LogInfo($"Saved preset '{presetName}' successfully.");
        }
        else
        {
            _statusMessage = "Errore durante il salvataggio";
        }
        RefreshPanelUI();
    }

    private static Sprite? FindSpriteByExactName(string name)
    {
        return NPCDesignerStage.LoadSpriteByExactName(name);
    }
}
