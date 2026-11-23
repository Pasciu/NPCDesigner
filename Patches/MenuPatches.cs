using System;
using HarmonyLib;
using PotionCraft.ManagersSystem.Menu;
using PotionCraft.ObjectBased.Menu;
using PotionCraft.ObjectBased.Menu.Buttons;
using PotionCraft.ObjectBased.Menu.Menus;

namespace NPCDesigner.Patches;

/// <summary>
/// Intercepts MenuManager.Initialize to inject the native NPC Designer title screen button
/// and register the custom MenuScreenObject into the game's menu system.
/// </summary>
[HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Initialize))]
public static class MenuManagerInitializePatch
{
    [HarmonyPostfix]
    public static void Postfix(MenuManager __instance)
    {
        if (Plugin.Instance?.PreviewController != null)
        {
            NPCDesignerMenuIntegration.Setup(__instance, Plugin.Instance.PreviewController);
        }
    }
}

/// <summary>
/// Fallback check when opening main menu to ensure integration is loaded if Initialize was bypassed or fired early.
/// </summary>
[HarmonyPatch(typeof(MenuManager), nameof(MenuManager.OpenMainMenu))]
public static class MenuManagerOpenMainMenuPatch
{
    [HarmonyPostfix]
    public static void Postfix(MenuManager __instance)
    {
        if (!NPCDesignerMenuIntegration.IsInitialized && Plugin.Instance?.PreviewController != null)
        {
            NPCDesignerMenuIntegration.Setup(__instance, Plugin.Instance.PreviewController);
        }
    }
}

/// <summary>
/// Prevents MenuSelectableObject.UpdateUnderlaySize from computing Infinity bounds on inactive custom buttons,
/// and enforces compact collider and underlay dimensions on active NPCDesigner buttons.
/// </summary>
[HarmonyPatch(typeof(MenuSelectableObject), nameof(MenuSelectableObject.UpdateUnderlaySize))]
public static class MenuSelectableObjectUpdateUnderlaySizePatch
{
    [HarmonyPrefix]
    public static bool Prefix(MenuSelectableObject __instance)
    {
        if (__instance == null) return false;
        var handler = __instance.GetComponent<DesignerButtonHandler>();
        if (handler != null && !__instance.gameObject.activeInHierarchy)
        {
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    public static void Postfix(MenuSelectableObject __instance)
    {
        if (__instance is MenuButton btn && btn.gameObject.activeInHierarchy)
        {
            var handler = btn.GetComponent<DesignerButtonHandler>();
            handler?.EnforceCustomBounds(btn);
        }
    }
}

/// <summary>
/// Preserves custom ingredient-style slot sprites and equipped highlight colors after
/// MenuSelectableObject.UpdateUnderlaySprite refreshes hover/idle state.
/// </summary>
[HarmonyPatch(typeof(MenuSelectableObject), "UpdateUnderlaySprite")]
public static class MenuSelectableObjectUpdateUnderlaySpritePatch
{
    [HarmonyPostfix]
    public static void Postfix(MenuSelectableObject __instance)
    {
        if (__instance == null) return;
        var slotVisuals = __instance.GetComponent<DesignerSlotVisuals>();
        if (slotVisuals != null)
        {
            slotVisuals.ApplyVisualState(__instance);
        }
    }
}

/// <summary>
/// Handles click actions for custom MenuButton instances by executing their attached DesignerButtonHandler callback.
/// Hooks OnButtonReleasedPointerInside to avoid JIT-inlined virtual methods.
/// </summary>
[HarmonyPatch(typeof(MenuButton), nameof(MenuButton.OnButtonReleasedPointerInside))]
public static class MenuButtonOnButtonReleasedPointerInsidePatch
{
    [HarmonyPostfix]
    public static void Postfix(MenuButton __instance)
    {
        if (__instance == null) return;

        var handler = __instance.GetComponent<DesignerButtonHandler>();
        if (handler != null && handler.OnClick != null)
        {
            Plugin.Log?.LogInfo($"Executing DesignerButtonHandler for {__instance.name}");
            try
            {
                handler.OnClick.Invoke();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in DesignerButtonHandler callback for button '{__instance.name}': {ex}");
            }
        }
    }
}

/// <summary>
/// Prevents ArgumentException when MenuScreenObject.Awake attempts to parse "NPCDesigner" as a standard MenuManager.MenuScreen enum value.
/// </summary>
[HarmonyPatch(typeof(MenuScreenObject), "GetThisMenuIs")]
public static class MenuScreenObjectGetThisMenuIsPatch
{
    [HarmonyPrefix]
    public static bool Prefix(MenuScreenObject __instance)
    {
        if (__instance != null && __instance.name == "NPCDesigner")
        {
            __instance.thisMenuIs = (MenuManager.MenuScreen)NPCDesignerMenuIntegration.DesignerMenuScreenId;
            return false; // Skip original Enum.Parse
        }
        return true;
    }
}
