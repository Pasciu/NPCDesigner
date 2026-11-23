using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx;
using Newtonsoft.Json;
using UnityEngine;

namespace NPCDesigner;

[Serializable]
public class NPCPreset
{
    public string PresetName { get; set; } = "Nuovo NPC";
    public string Gender { get; set; } = "Male";
    public string BodyName { get; set; } = "";
    public string FaceName { get; set; } = "";
    public string EyesName { get; set; } = "";
    public string SkullShapeName { get; set; } = "";
    public string BreastName { get; set; } = "";
    public string HairstyleName { get; set; } = "";
    public string BeardName { get; set; } = "";
    public string HatName { get; set; } = "";

    public string AccessoryAboveHair { get; set; } = "";
    public string AccessoryFace { get; set; } = "";
    public string AccessoryBody { get; set; } = "";
    public string AccessorySkullShape { get; set; } = "";
    public string AccessoryHandBack { get; set; } = "";
    public string AccessoryHandFront { get; set; } = "";
    public string AccessoryBehindBody { get; set; } = "";
    public string AccessoryShortHair { get; set; } = "";

    public string SkinColorHex { get; set; } = "";
    public string HairColorHex { get; set; } = "";
    public string ClothesColor1Hex { get; set; } = "";
    public string ClothesColor2Hex { get; set; } = "";
    public string ClothesColor3Hex { get; set; } = "";
    public string ClothesColor4Hex { get; set; } = "";
}

public static class NPCPresetManager
{
    public static string PresetsDirectory => Path.Combine(Paths.PluginPath, "NPCDesigner", "Presets");

    public static void EnsureDirectory()
    {
        try
        {
            if (!Directory.Exists(PresetsDirectory))
            {
                Directory.CreateDirectory(PresetsDirectory);
                CreateDefaultPresets();
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"Failed to create presets directory: {ex.Message}");
        }
    }

    public static List<string> GetSavedPresetNames()
    {
        EnsureDirectory();
        var list = new List<string>();
        try
        {
            var files = Directory.GetFiles(PresetsDirectory, "*.json");
            foreach (var file in files)
            {
                list.Add(Path.GetFileNameWithoutExtension(file));
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"Failed to list presets: {ex.Message}");
        }
        return list;
    }

    public static bool SavePreset(NPCPreset preset)
    {
        EnsureDirectory();
        if (string.IsNullOrWhiteSpace(preset.PresetName))
        {
            preset.PresetName = "NPC_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        }

        try
        {
            var cleanName = string.Join("_", preset.PresetName.Split(Path.GetInvalidFileNameChars()));
            var filePath = Path.Combine(PresetsDirectory, cleanName + ".json");
            var json = JsonConvert.SerializeObject(preset, Formatting.Indented);
            File.WriteAllText(filePath, json);
            Plugin.Log?.LogInfo($"Preset '{cleanName}' saved successfully to {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"Failed to save preset: {ex.Message}");
            return false;
        }
    }

    public static NPCPreset? LoadPreset(string presetName)
    {
        EnsureDirectory();
        try
        {
            var cleanName = string.Join("_", presetName.Split(Path.GetInvalidFileNameChars()));
            var filePath = Path.Combine(PresetsDirectory, cleanName + ".json");
            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<NPCPreset>(json);
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"Failed to load preset '{presetName}': {ex.Message}");
            return null;
        }
    }

    public static bool DeletePreset(string presetName)
    {
        EnsureDirectory();
        try
        {
            var cleanName = string.Join("_", presetName.Split(Path.GetInvalidFileNameChars()));
            var filePath = Path.Combine(PresetsDirectory, cleanName + ".json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Plugin.Log?.LogInfo($"Preset '{cleanName}' deleted successfully.");
                return true;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"Failed to delete preset '{presetName}': {ex.Message}");
        }
        return false;
    }

    private static void CreateDefaultPresets()
    {
        var warrior = new NPCPreset
        {
            PresetName = "Guerriero Errante",
            Gender = "Male",
            BodyName = "Body 2",
            FaceName = "Face 2",
            EyesName = "Eyes 2",
            SkullShapeName = "SkullShape 2",
            BreastName = "",
            HairstyleName = "Hair 2",
            BeardName = "Beard 2",
            HatName = "Hat 1",
            SkinColorHex = "#E8C39E",
            HairColorHex = "#4A2E1B",
            ClothesColor1Hex = "#324A5E",
            ClothesColor2Hex = "#B8860B",
            ClothesColor3Hex = "#2E3A23",
            ClothesColor4Hex = "#5C3A21"
        };
        SavePreset(warrior);

        var citizenF = new NPCPreset
        {
            PresetName = "Cittadina Curiosa",
            Gender = "Female",
            BodyName = "Body 6",
            FaceName = "Face 9",
            EyesName = "Eyes 2",
            SkullShapeName = "SkullShape 8",
            BreastName = "Breast 2",
            HairstyleName = "Hair 15",
            BeardName = "Beard 1",
            HatName = "Hat 1",
            SkinColorHex = "#F4D7B5",
            HairColorHex = "#8B5A2B",
            ClothesColor1Hex = "#5E3232",
            ClothesColor2Hex = "#D2B48C",
            ClothesColor3Hex = "#4A4036",
            ClothesColor4Hex = "#8C2D19"
        };
        SavePreset(citizenF);
    }
}
