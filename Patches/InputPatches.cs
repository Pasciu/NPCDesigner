using HarmonyLib;
using NavigationButtons;

namespace NPCDesigner.Patches;

/// <summary>
/// Prevents room navigation buttons from being triggered by hotkeys while the designer is open.
/// Note: InputManager.HasInputGotToBeDisabled must NOT be forced to true because NPCDesigner uses
/// native MenuButton instances that rely on normal CursorManager and InputManager mouse events.
/// </summary>
[HarmonyPatch(typeof(NavigationButton), "CanHotkeysBeUsed")]
public static class NavigationButtonHotkeyPatches
{
    [HarmonyPostfix]
    public static void Postfix(ref bool __result)
    {
        if (Plugin.IsDesignerOpen)
        {
            __result = false;
        }
    }
}

/// <summary>
/// Prevents room navigation buttons from being interacted with while the designer is open.
/// </summary>
[HarmonyPatch(typeof(NavigationButton), nameof(NavigationButton.CanBeInteractedNow))]
public static class NavigationButtonInteractionPatches
{
    [HarmonyPostfix]
    public static void Postfix(ref bool __result)
    {
        if (Plugin.IsDesignerOpen)
        {
            __result = false;
        }
    }
}
