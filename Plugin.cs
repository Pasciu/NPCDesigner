using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.Menu;
using UnityEngine.InputSystem;

namespace NPCDesigner;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.candied.potioncraft.npcdesigner";
    public const string PluginName = "NPCDesigner";
    public const string PluginVersion = "1.0.0";

    internal static ManualLogSource Log { get; private set; } = null!;
    public static Plugin? Instance { get; private set; }

    private Harmony? _harmony;
    private NPCPreviewController? _previewController;

    public NPCPreviewController? PreviewController => _previewController;

    public static bool IsDesignerOpen =>
        Managers.Menu != null && (int)Managers.Menu.CurrentMenu == NPCDesignerMenuIntegration.DesignerMenuScreenId;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        Log.LogInfo($"Plugin {PluginName} (v{PluginVersion}) is initializing...");

        _previewController = new NPCPreviewController();

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();

        Log.LogInfo($"Plugin {PluginName} (v{PluginVersion}) loaded successfully.");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Hotkey toggle: F6 (convenience shortcut)
        if (keyboard.f6Key.wasPressedThisFrame)
        {
            if (Managers.Menu != null)
            {
                if (IsDesignerOpen)
                {
                    Managers.Menu.GoBack();
                }
                else if (Managers.Menu.CurrentMenu == MenuManager.MenuScreen.Main)
                {
                    Managers.Menu.GoTo((MenuManager.MenuScreen)NPCDesignerMenuIntegration.DesignerMenuScreenId);
                }
            }
        }
    }

    private void OnDestroy()
    {
        _previewController?.DestroyPreview();
        _harmony?.UnpatchSelf();
        Instance = null;
    }
}
