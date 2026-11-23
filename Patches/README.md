# NPC Designer - Technical Architecture & Patches Map

This document serves as the Tier 2 technical reference for developers and AI agents maintaining or extending the **NPCDesigner** mod for Potion Craft.

---

## 1. Subsystem Architecture

The mod is structured into cleanly isolated, modular components adhering to native Unity and Potion Craft menu lifecycles (zero IMGUI/`OnGUI` overlays and zero gameplay/save-state contamination):

```
NPCDesigner/
├── Plugin.cs                        # BepInEx BaseUnityPlugin entry point & Harmony patch orchestrator
├── NPCDesignerStage.cs              # Purely visual Meeting Room shop stage + foreground Visitors/Laboratory counter
├── NPCIdleAnimator.cs               # Native NpcLookAtObject idle look-around + cursor tracking driver
├── NPCAssetThumbnailRenderer.cs     # Multi-layer AppearancePart / swatch / None / Preset thumbnail renderer
├── NPCDesignerMenuIntegration.cs    # Native MenuScreenObject, Ingredient-style Inventory panel, 4x4 clickable slot grid
├── NPCPreviewController.cs          # Live NpcMonoBehaviour lifecycle, gender/headShift alignment, mood & vibe reactions
├── NPCPartRegistry.cs               # Dynamic AppearancePart harvester, gender & breast compatibility classifier, palettes
├── NPCDesignerData.cs               # NPCPreset DTO & NPCPresetManager (JSON serialization in BepInEx/plugins)
└── Patches/
    ├── MenuPatches.cs               # Harmony hooks for MenuManager, MenuSelectableObject, MenuButton, MenuScreenObject
    ├── InputPatches.cs              # Harmony hooks suppressing room navigation hotkeys while designer is open
    └── README.md                    # This technical reference
```

---

## 2. Component Breakdown

### `Plugin.cs`
- **Class:** `NPCDesigner.Plugin : BaseUnityPlugin`
- **Metadata:** GUID `com.candied.potioncraft.npcdesigner`, Name `NPCDesigner`, Version `1.0.0`.
- **Responsibilities:**
  - Instantiates `NPCPreviewController` during `Awake()`.
  - Manages the Harmony lifecycle (`PatchAll()` / `UnpatchSelf()`).
  - Provides convenience shortcut `F6` to toggle screen ID `100` via `Managers.Menu.GoTo` / `GoBack()`.
  - Exposes `IsDesignerOpen => Managers.Menu != null && (int)Managers.Menu.CurrentMenu == 100;`.

### `NPCDesignerStage.cs`
- **Class:** `NPCDesigner.NPCDesignerStage`
- **Responsibilities:**
  - Hides the cloned `Background` child of the `NPCDesigner` screen only (leaving Main Menu and Settings intact) and places an opaque safety backdrop (`sortingOrder = 10950`) so camera clear-flags never leave previous menu frames visible.
  - When live `RoomIndex.Meeting` is already instantiated in `Managers.Room.InstantiatedRooms`, clones only purely visual `SpriteRenderer`s (zero MonoBehaviours, Colliders, Scales, Trade, Queue, or Audio).
  - When opened from the Main Menu before a save loads `RoomMeeting`, loads the genuine shop sprites (`Wood Background`, bookshelves, cupboards, bottles, books, chest, and foreground `Visitors Table` / `Laboratory Table` counter) from loaded resources and already-loaded `AssetBundle`s (`AssetBundle.GetAllLoadedAssetBundles()`).
  - Separates sorting cleanly on `MenuElements`: background wall & furniture at `11000..11850`, preview NPC `SortingGroup` at `12000`, foreground shop counter + lower oak apron at `12245..12250` (framing the NPC's cut-off torso cleanly behind the counter).

### `NPCIdleAnimator.cs`
- **Class:** `NPCDesigner.NPCIdleAnimator : MonoBehaviour`
- **Responsibilities:**
  - Drives Potion Craft's **exact native** `NpcLookAtObject.LookAt(Vector2, float)` and `GetLookAroundBoxBounds()` using `Settings<NpcManagerSettings>.Asset` (`minTimeUpdateLookingTarget`, `maxTimeUpdateLookingTarget`, `lookingAroundAnimationTime`, `lookingAtObjectAnimationTime`) with zero custom/synthetic animations.
  - Tracks live mouse movement via `Mouse.current.position.ReadValue()` converted through `Managers.Game.Cam.ScreenToWorldPoint(...)`, and automatically resumes native periodic `LookAround` after 2.0 seconds of stationary cursor.
  - Exposes `PrepareForHeadShift()` to kill active DOTween tweens (`DOTween.Kill(...)`) on `head`, `headBack`, `vibe`, `leftPupil`, and `rightPupil` and reset neutral head rotation before `NpcSettings.ShiftTransforms(..., inverseShift: true)` runs on body/gender changes.

### `NPCAssetThumbnailRenderer.cs`
- **Class:** `NPCDesigner.NPCAssetThumbnailRenderer`
- **Responsibilities:**
  - Renders scaled, centered multi-layer `SpriteRenderer` thumbnails inside each slot's `ThumbnailRoot` (`SortingGroup` on `MenuElements`, `sortingOrder = 12610+`) for `Body`, `Face`, `Eyes`, `SkullShape`, `Breast`, `Hairstyle`, `Beard`, `Hat`, and all 8 `Accessory` subclasses, tinted with the NPC's active skin/hair/clothes palette.
  - Generates cached procedural sprites for circular ink-rimmed color swatches, `"Nessuno"` icon, and preset scroll icons.

### `NPCDesignerMenuIntegration.cs`
- **Classes:** `NPCDesignerMenuIntegration`, `DesignerButtonHandler`, `DesignerSlotVisuals`, `NPCDesignerScreenHandler`
- **Responsibilities:**
  - **Right-Side Native Inventory Panel (`x: +4.38 .. +12.80`, center `8.60`):** Uses `Inventory Background Var1` + `InventoryWindow Background Var1` torn-parchment well (`sortingOrder = 12380..12420`) with title `CREAZIONE PERSONAGGIO` at top.
  - **4×4 Clickable Ingredient-Style Asset Slot Grid:** Uses the 18 genuine `Inventory Item Slot N Normal` & hover sprites harvested from `Managers.Player.InventoryPanel.inventoryItemObjectPrefab` (`InventoryItemButton.idleSprites` / `hoveredSprites`). Each slot is a native `MenuButton` with `DesignerSlotVisuals` preserving square slot bounds, hover highlight, golden equipped state, thumbnail layers, and click callbacks.
  - **Navigation & Actions:** 4 top group tabs (`Corpo`, `Stile`, `Colori`, `Preset`), `<` / `>` subcategory bar, `< Prec` / `Succ >` pagination bar, `Casuale`, `Salva Preset`, `< Torna al Menu`, plus left counter-ledge `Genere` and `Umore` buttons (`y = -5.95`).

### `NPCPreviewController.cs` & `NPCPartRegistry.cs`
- **Responsibilities:**
  - Dynamically classifies `Body`, `Face`, `SkullShape`, and `Hairstyle` pools into `Male` / `Female` / `All` from runtime `NpcTemplate`, `NpcPrefab`, and `Breast.useWith` compatibility (`AppearancePart.FilterParts`), clearing `breast` via `breast.Reset()` when `Male` or `None`.
  - Applies accurate per-body `headShift` & `headRotationPivot` from mapped `NpcPrefab` (`CitizenCommonM` / `CitizenCommonF`) after `NPCIdleAnimator.PrepareForHeadShift()`.
  - Populates missing `pleasedVibeSprite` / `angryVibeSprite` via `EnsureVibeSettings` and triggers native `NpcEmotions.Emotion = NpcEmotionState.Pleased` / `Angry` on `Entusiasta` / `Arrabbiato` mood selection.

---

## 3. Harmony Patches Map (`Patches/MenuPatches.cs` & `Patches/InputPatches.cs`)

1. **`MenuManager.Initialize` & `MenuManager.OpenMainMenu` (Postfix):** Injects the Title Screen button and registers the `NPCDesigner` screen (`ID = 100`).
2. **`MenuSelectableObject.UpdateUnderlaySize` (Prefix & Postfix):** Prevents `-Infinity` bounds on inactive buttons and enforces custom widths or fixed square slot dimensions (`DesignerButtonHandler.EnforceCustomBounds`).
3. **`MenuSelectableObject.UpdateUnderlaySprite` (Postfix):** Calls `DesignerSlotVisuals.ApplyVisualState` so ingredient-style asset slots retain their native `Inventory Item Slot` idle/hover sprites and golden equipped tint after hover state transitions.
4. **`MenuButton.OnButtonReleasedPointerInside` (Postfix):** Dispatches `DesignerButtonHandler.OnClick` callbacks for tabs, arrows, action buttons, and asset slots.
5. **`MenuScreenObject.GetThisMenuIs` (Prefix):** Maps `"NPCDesigner"` to screen ID `100` without `Enum.Parse` throwing `ArgumentException`.
6. **`NavigationButton.CanHotkeysBeUsed` & `CanBeInteractedNow` (Postfix):** Suppresses room navigation while the designer screen is active without disabling `InputManager` mouse events.
