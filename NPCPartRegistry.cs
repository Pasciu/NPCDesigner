using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PotionCraft.ManagersSystem.Npc;
using PotionCraft.Npc.MonoBehaviourScripts;
using PotionCraft.Npc.Parts;
using PotionCraft.Npc.Parts.Appearance;
using PotionCraft.Npc.Parts.Appearance.Accessories;
using PotionCraft.Npc.Parts.Settings;
using PotionCraft.Settings;
using UnityEngine;

namespace NPCDesigner;

public enum NPCGenderMode
{
    Male = 0,
    Female = 1,
    All = 2
}

public static class NPCPartRegistry
{
    public static bool IsInitialized { get; private set; }

    // Full (unfiltered) part lists
    public static List<Body> Bodies { get; } = new();
    public static List<Face> Faces { get; } = new();
    public static List<Eyes> EyesList { get; } = new();
    public static List<SkullShape> SkullShapes { get; } = new();
    public static List<Breast> Breasts { get; } = new();
    public static List<Hairstyle> Hairstyles { get; } = new();
    public static List<Hat> Hats { get; } = new();
    public static List<Beard> Beards { get; } = new();

    // Gender-classified part lists (derived dynamically from game templates, prefabs, and Breast.useWith)
    public static List<Body> MaleBodies { get; } = new();
    public static List<Body> FemaleBodies { get; } = new();
    public static List<Face> MaleFaces { get; } = new();
    public static List<Face> FemaleFaces { get; } = new();
    public static List<SkullShape> MaleSkullShapes { get; } = new();
    public static List<SkullShape> FemaleSkullShapes { get; } = new();
    public static List<Hairstyle> MaleHairstyles { get; } = new();
    public static List<Hairstyle> FemaleHairstyles { get; } = new();

    // Body -> compatible Breast overlays (from AppearancePart.FilterParts / useWith)
    private static readonly Dictionary<Body, List<Breast>> BodyToCompatibleBreasts = new();

    // Body -> NpcPrefab (for accurate headShift / headRotationPivot alignment)
    private static readonly Dictionary<Body, NpcPrefab> BodyToPrefab = new();
    public static NpcPrefab? DefaultMalePrefab { get; private set; }
    public static NpcPrefab? DefaultFemalePrefab { get; private set; }

    public static List<AccessoryAboveHair> AccessoriesAboveHair { get; } = new();
    public static List<AccessoryFace> AccessoriesFace { get; } = new();
    public static List<AccessoryBody> AccessoriesBody { get; } = new();
    public static List<AccessorySkullShape> AccessoriesSkullShape { get; } = new();
    public static List<AccessoryHandBack> AccessoriesHandBack { get; } = new();
    public static List<AccessoryHandFront> AccessoriesHandFront { get; } = new();
    public static List<AccessoryBehindBody> AccessoriesBehindBody { get; } = new();
    public static List<AccessoryShortHair> AccessoriesShortHair { get; } = new();

    public static List<Color> SkinColors { get; } = new();
    public static List<Color> HairColors { get; } = new();
    public static List<Color> ClothesColors1 { get; } = new();
    public static List<Color> ClothesColors2 { get; } = new();
    public static List<Color> ClothesColors3 { get; } = new();
    public static List<Color> ClothesColors4 { get; } = new();

    public static void Initialize()
    {
        if (IsInitialized) return;

        ClearLists();

        try
        {
            var allParts = Resources.FindObjectsOfTypeAll<AppearancePart>();
            Plugin.Log?.LogInfo($"Scanning game resources: discovered {allParts.Length} AppearancePart assets.");

            foreach (var part in allParts)
            {
                if (part == null) continue;

                switch (part)
                {
                    case Body body:
                        if (!Bodies.Contains(body)) Bodies.Add(body);
                        break;
                    case Face face:
                        if (!Faces.Contains(face)) Faces.Add(face);
                        break;
                    case Eyes eyes:
                        if (!EyesList.Contains(eyes)) EyesList.Add(eyes);
                        break;
                    case SkullShape skull:
                        if (!SkullShapes.Contains(skull)) SkullShapes.Add(skull);
                        break;
                    case Breast breast:
                        if (!Breasts.Contains(breast)) Breasts.Add(breast);
                        break;
                    case Hairstyle hair:
                        if (!Hairstyles.Contains(hair)) Hairstyles.Add(hair);
                        break;
                    case Hat hat:
                        if (!Hats.Contains(hat)) Hats.Add(hat);
                        break;
                    case Beard beard:
                        if (!Beards.Contains(beard)) Beards.Add(beard);
                        break;
                    case AccessoryAboveHair acc:
                        if (!AccessoriesAboveHair.Contains(acc)) AccessoriesAboveHair.Add(acc);
                        break;
                    case AccessoryFace acc:
                        if (!AccessoriesFace.Contains(acc)) AccessoriesFace.Add(acc);
                        break;
                    case AccessoryBody acc:
                        if (!AccessoriesBody.Contains(acc)) AccessoriesBody.Add(acc);
                        break;
                    case AccessorySkullShape acc:
                        if (!AccessoriesSkullShape.Contains(acc)) AccessoriesSkullShape.Add(acc);
                        break;
                    case AccessoryHandBack acc:
                        if (!AccessoriesHandBack.Contains(acc)) AccessoriesHandBack.Add(acc);
                        break;
                    case AccessoryHandFront acc:
                        if (!AccessoriesHandFront.Contains(acc)) AccessoriesHandFront.Add(acc);
                        break;
                    case AccessoryBehindBody acc:
                        if (!AccessoriesBehindBody.Contains(acc)) AccessoriesBehindBody.Add(acc);
                        break;
                    case AccessoryShortHair acc:
                        if (!AccessoriesShortHair.Contains(acc)) AccessoriesShortHair.Add(acc);
                        break;
                }
            }

            SortPartLists();
            EnsureEmptyElementsFirst();
            BuildBreastCompatibilityMap();
            ClassifyPartsByGenderAndPrefabs();
            LoadPalettes();

            IsInitialized = true;
            Plugin.Log?.LogInfo(
                $"NPCPartRegistry initialized: {Bodies.Count} bodies (M:{MaleBodies.Count}, F:{FemaleBodies.Count}), " +
                $"{Faces.Count} faces (M:{MaleFaces.Count}, F:{FemaleFaces.Count}), " +
                $"{SkullShapes.Count} skulls (M:{MaleSkullShapes.Count}, F:{FemaleSkullShapes.Count}), " +
                $"{Hairstyles.Count} hairs (M:{MaleHairstyles.Count}, F:{FemaleHairstyles.Count}), " +
                $"{Breasts.Count} breasts, {Beards.Count} beards, {Hats.Count} hats. " +
                $"DefaultPrefabs: M='{DefaultMalePrefab?.name ?? "null"}', F='{DefaultFemalePrefab?.name ?? "null"}'.");
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"Failed to initialize NPCPartRegistry: {ex.Message}");
        }
    }

    public static List<Body> GetBodies(NPCGenderMode mode) => mode switch
    {
        NPCGenderMode.Male when MaleBodies.Count > 0 => MaleBodies,
        NPCGenderMode.Female when FemaleBodies.Count > 0 => FemaleBodies,
        _ => Bodies
    };

    public static List<Face> GetFaces(NPCGenderMode mode) => mode switch
    {
        NPCGenderMode.Male when MaleFaces.Count > 0 => MaleFaces,
        NPCGenderMode.Female when FemaleFaces.Count > 0 => FemaleFaces,
        _ => Faces
    };

    public static List<SkullShape> GetSkullShapes(NPCGenderMode mode) => mode switch
    {
        NPCGenderMode.Male when MaleSkullShapes.Count > 0 => MaleSkullShapes,
        NPCGenderMode.Female when FemaleSkullShapes.Count > 0 => FemaleSkullShapes,
        _ => SkullShapes
    };

    public static List<Hairstyle> GetHairstyles(NPCGenderMode mode) => mode switch
    {
        NPCGenderMode.Male when MaleHairstyles.Count > 0 => MaleHairstyles,
        NPCGenderMode.Female when FemaleHairstyles.Count > 0 => FemaleHairstyles,
        _ => Hairstyles
    };

    public static List<Breast> GetCompatibleBreasts(Body? body, NPCGenderMode mode)
    {
        if (mode == NPCGenderMode.Male || body == null)
        {
            return EmptyBreastList;
        }

        if (BodyToCompatibleBreasts.TryGetValue(body, out var compatible) && compatible.Count > 0)
        {
            return compatible;
        }

        return EmptyBreastList;
    }

    public static bool IsFemaleBody(Body? body)
    {
        if (body == null) return false;
        return FemaleBodies.Contains(body);
    }

    public static NpcPrefab? GetPrefabForBody(Body? body, NPCGenderMode mode)
    {
        if (body != null && BodyToPrefab.TryGetValue(body, out var mapped) && mapped != null)
        {
            return mapped;
        }

        if (body != null && IsFemaleBody(body) && DefaultFemalePrefab != null)
        {
            return DefaultFemalePrefab;
        }

        if (mode == NPCGenderMode.Female && DefaultFemalePrefab != null)
        {
            return DefaultFemalePrefab;
        }

        return DefaultMalePrefab ?? Settings<NpcManagerSettings>.Asset?.defaultNpcPrefab;
    }

    private static readonly List<Breast> EmptyBreastList = new();

    private static void ClearLists()
    {
        Bodies.Clear();
        Faces.Clear();
        EyesList.Clear();
        SkullShapes.Clear();
        Breasts.Clear();
        Hairstyles.Clear();
        Hats.Clear();
        Beards.Clear();

        MaleBodies.Clear();
        FemaleBodies.Clear();
        MaleFaces.Clear();
        FemaleFaces.Clear();
        MaleSkullShapes.Clear();
        FemaleSkullShapes.Clear();
        MaleHairstyles.Clear();
        FemaleHairstyles.Clear();
        BodyToCompatibleBreasts.Clear();
        BodyToPrefab.Clear();
        DefaultMalePrefab = null;
        DefaultFemalePrefab = null;

        AccessoriesAboveHair.Clear();
        AccessoriesFace.Clear();
        AccessoriesBody.Clear();
        AccessoriesSkullShape.Clear();
        AccessoriesHandBack.Clear();
        AccessoriesHandFront.Clear();
        AccessoriesBehindBody.Clear();
        AccessoriesShortHair.Clear();
        SkinColors.Clear();
        HairColors.Clear();
        ClothesColors1.Clear();
        ClothesColors2.Clear();
        ClothesColors3.Clear();
        ClothesColors4.Clear();
    }

    private static void BuildBreastCompatibilityMap()
    {
        foreach (var body in Bodies)
        {
            if (body == null) continue;
            try
            {
                var compatible = AppearancePart.FilterParts(Breasts, new List<AppearancePart> { body });
                if (compatible != null && compatible.Count > 0)
                {
                    compatible.Sort((a, b) => NaturalCompare(a.name, b.name));
                    BodyToCompatibleBreasts[body] = compatible;
                }
            }
            catch
            {
                // Ignore bodies whose name doesn't match the "<Name> <Int>" convention
            }
        }
    }

    private static void ClassifyPartsByGenderAndPrefabs()
    {
        var maleBodiesSet = new HashSet<Body>();
        var femaleBodiesSet = new HashSet<Body>();
        var maleFacesSet = new HashSet<Face>();
        var femaleFacesSet = new HashSet<Face>();
        var maleSkullsSet = new HashSet<SkullShape>();
        var femaleSkullsSet = new HashSet<SkullShape>();
        var maleHairsSet = new HashSet<Hairstyle>();
        var femaleHairsSet = new HashSet<Hairstyle>();

        // 1. Any Body with compatible Breast overlays is unambiguously Female
        foreach (var kv in BodyToCompatibleBreasts)
        {
            if (kv.Value.Count > 0)
            {
                femaleBodiesSet.Add(kv.Key);
            }
        }

        // 2. Scan all NpcPrefabs for headShift mappings and default prefabs
        var allPrefabs = Resources.FindObjectsOfTypeAll<NpcPrefab>();
        foreach (var prefab in allPrefabs)
        {
            if (prefab == null) continue;

            if (string.Equals(prefab.name, "CitizenCommonM", StringComparison.OrdinalIgnoreCase))
            {
                DefaultMalePrefab = prefab;
            }
            else if (string.Equals(prefab.name, "CitizenCommonF", StringComparison.OrdinalIgnoreCase))
            {
                DefaultFemalePrefab = prefab;
            }

            bool prefabIsFemale = IsPrefabFemale(prefab);
            if (prefab.appearanceParts != null)
            {
                foreach (var part in prefab.appearanceParts)
                {
                    if (part is Body b)
                    {
                        if (!BodyToPrefab.ContainsKey(b)) BodyToPrefab[b] = prefab;
                        if (prefabIsFemale) femaleBodiesSet.Add(b);
                        else if (!femaleBodiesSet.Contains(b)) maleBodiesSet.Add(b);
                    }
                }
            }
        }

        // 3. Scan all NpcTemplates to classify Bodies, Faces, SkullShapes, Hairstyles by Gender
        var templates = new HashSet<NpcTemplate>();
        if (NpcTemplate.allNpcTemplates?.templates != null)
        {
            foreach (var t in NpcTemplate.allNpcTemplates.templates)
            {
                if (t != null) templates.Add(t);
            }
        }
        foreach (var t in Resources.FindObjectsOfTypeAll<NpcTemplate>())
        {
            if (t != null) templates.Add(t);
        }

        foreach (var template in templates)
        {
            if (template == null) continue;

            var genderOpt = ResolveTemplateGender(template, new HashSet<NpcTemplate>());
            var prefab = ResolveTemplatePrefab(template, new HashSet<NpcTemplate>());

            if (genderOpt == null)
            {
                // Ambiguous / mixed-gender template: still map Body->Prefab if unambiguous, but do not pollute single-gender lists
                if (prefab != null && template.appearance?.body != null)
                {
                    CollectPartsFromGroup(template.appearance.body, b =>
                    {
                        if (!BodyToPrefab.ContainsKey(b)) BodyToPrefab[b] = prefab;
                    });
                }
                continue;
            }

            bool isFemale = genderOpt.Value == Gender.GenderSet.Female;

            if (prefab != null)
            {
                if (!isFemale && DefaultMalePrefab == null && prefab.settings?.otherSettings != null && prefab.settings.otherSettings.headShift != Vector2.zero)
                {
                    DefaultMalePrefab = prefab;
                }
                else if (isFemale && DefaultFemalePrefab == null && prefab.settings?.otherSettings != null && prefab.settings.otherSettings.headShift != Vector2.zero)
                {
                    DefaultFemalePrefab = prefab;
                }
            }

            var app = template.appearance;
            if (app == null) continue;

            CollectPartsFromGroup(app.body, b =>
            {
                if (prefab != null && !BodyToPrefab.ContainsKey(b)) BodyToPrefab[b] = prefab;
                if (isFemale || BodyToCompatibleBreasts.ContainsKey(b)) femaleBodiesSet.Add(b);
                else maleBodiesSet.Add(b);
            });

            CollectPartsFromGroup(app.face, f =>
            {
                if (isFemale) femaleFacesSet.Add(f);
                else maleFacesSet.Add(f);
            });

            CollectPartsFromGroup(app.skullShape, s =>
            {
                if (isFemale) femaleSkullsSet.Add(s);
                else maleSkullsSet.Add(s);
            });

            CollectPartsFromGroup(app.hairstyle, h =>
            {
                if (isFemale) femaleHairsSet.Add(h);
                else maleHairsSet.Add(h);
            });
        }

        // Ensure no body with compatible breasts leaks into MaleBodies
        maleBodiesSet.ExceptWith(femaleBodiesSet);

        // Fallback for any unclassified parts so nothing is completely lost:
        // Bodies with 0 compatible breasts not seen in any Female template -> MaleBodies (unless plot-only female)
        foreach (var b in Bodies)
        {
            if (!maleBodiesSet.Contains(b) && !femaleBodiesSet.Contains(b))
            {
                maleBodiesSet.Add(b);
            }
        }

        // Build ordered lists preserving NaturalCompare order
        foreach (var b in Bodies)
        {
            if (maleBodiesSet.Contains(b)) MaleBodies.Add(b);
            if (femaleBodiesSet.Contains(b)) FemaleBodies.Add(b);
        }

        foreach (var f in Faces)
        {
            if (maleFacesSet.Contains(f)) MaleFaces.Add(f);
            if (femaleFacesSet.Contains(f)) FemaleFaces.Add(f);
        }

        foreach (var s in SkullShapes)
        {
            if (maleSkullsSet.Contains(s)) MaleSkullShapes.Add(s);
            if (femaleSkullsSet.Contains(s)) FemaleSkullShapes.Add(s);
        }

        var emptyHair = Hairstyles.FirstOrDefault(h =>
            h.name.Equals("hair 1", StringComparison.OrdinalIgnoreCase) ||
            h.name.Equals("hairstyle 1", StringComparison.OrdinalIgnoreCase));

        if (emptyHair != null)
        {
            MaleHairstyles.Add(emptyHair);
            FemaleHairstyles.Add(emptyHair);
        }

        foreach (var h in Hairstyles)
        {
            if (h == emptyHair) continue;
            if (maleHairsSet.Contains(h)) MaleHairstyles.Add(h);
            if (femaleHairsSet.Contains(h)) FemaleHairstyles.Add(h);
        }

        // Safety fallbacks in case any list ended up empty
        if (MaleBodies.Count == 0) MaleBodies.AddRange(Bodies);
        if (FemaleBodies.Count == 0) FemaleBodies.AddRange(Bodies);
        if (MaleFaces.Count == 0) MaleFaces.AddRange(Faces);
        if (FemaleFaces.Count == 0) FemaleFaces.AddRange(Faces);
        if (MaleSkullShapes.Count == 0) MaleSkullShapes.AddRange(SkullShapes);
        if (FemaleSkullShapes.Count == 0) FemaleSkullShapes.AddRange(SkullShapes);
        if (MaleHairstyles.Count <= 1) MaleHairstyles.AddRange(Hairstyles.Where(h => !MaleHairstyles.Contains(h)));
        if (FemaleHairstyles.Count <= 1) FemaleHairstyles.AddRange(Hairstyles.Where(h => !FemaleHairstyles.Contains(h)));
    }

    private static bool IsPrefabFemale(NpcPrefab prefab)
    {
        if (prefab == null) return false;
        if (prefab.appearanceParts != null && prefab.appearanceParts.Any(p => p is Breast))
        {
            return true;
        }
        var n = prefab.name ?? string.Empty;
        if (n.Contains("Female", StringComparison.OrdinalIgnoreCase)) return true;
        if (n.EndsWith('F') && n.Length >= 2 && char.IsLower(n[n.Length - 2]))
        {
            return true;
        }
        return false;
    }

    private static bool IsPrefabMale(NpcPrefab prefab)
    {
        if (prefab == null || IsPrefabFemale(prefab)) return false;
        var n = prefab.name ?? string.Empty;
        if (n.Contains("Male", StringComparison.OrdinalIgnoreCase)) return true;
        if (n.EndsWith('M') && n.Length >= 2 && char.IsLower(n[n.Length - 2]))
        {
            return true;
        }
        return false;
    }

    private static Gender.GenderSet? ResolveTemplateGender(NpcTemplate template, HashSet<NpcTemplate> visited)
    {
        if (template == null || !visited.Add(template))
        {
            return null;
        }

        // If template defines explicit Gender in baseParts, trust it
        if (template.baseParts != null)
        {
            foreach (var part in template.baseParts)
            {
                if (part is Gender g)
                {
                    return g.gender;
                }
            }
        }

        // If template has any Breast parts, it is unambiguously Female
        if (template.appearance?.breastSize?.partsInGroup != null &&
            template.appearance.breastSize.partsInGroup.Any(p => p != null && p.part != null))
        {
            return Gender.GenderSet.Female;
        }

        // Check body parts against BodyToCompatibleBreasts:
        // all female bodies -> Female; mixed male+female bodies -> null (unisex/mixed)
        if (template.appearance?.body?.partsInGroup != null)
        {
            int femaleBodyCount = 0;
            int nonBreastBodyCount = 0;
            foreach (var entry in template.appearance.body.partsInGroup)
            {
                if (entry?.part == null) continue;
                if (BodyToCompatibleBreasts.ContainsKey(entry.part)) femaleBodyCount++;
                else nonBreastBodyCount++;
            }

            if (femaleBodyCount > 0 && nonBreastBodyCount > 0)
            {
                return null; // Mixed-gender template
            }
            if (femaleBodyCount > 0)
            {
                return Gender.GenderSet.Female;
            }
        }

        // Check baseParts prefabs and sub-templates
        if (template.baseParts != null)
        {
            foreach (var part in template.baseParts)
            {
                if (part is NpcPrefab prefab)
                {
                    if (IsPrefabFemale(prefab)) return Gender.GenderSet.Female;
                    if (IsPrefabMale(prefab)) return Gender.GenderSet.Male;
                }
                if (part is NpcTemplate subTemplate)
                {
                    var subGender = ResolveTemplateGender(subTemplate, visited);
                    if (subGender != null)
                    {
                        return subGender;
                    }
                }
            }
        }

        var name = template.name ?? string.Empty;
        if (name.Contains("Female", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("FTemplate", StringComparison.Ordinal) ||
            (name.EndsWith('F') && name.Length >= 2 && char.IsLower(name[name.Length - 2])))
        {
            return Gender.GenderSet.Female;
        }

        if (name.Contains("Male", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("MTemplate", StringComparison.Ordinal) ||
            name.Contains("WarriorTemplate", StringComparison.Ordinal) ||
            (name.EndsWith('M') && name.Length >= 2 && char.IsLower(name[name.Length - 2])))
        {
            return Gender.GenderSet.Male;
        }

        // Non-empty beard parts strongly indicate Male
        if (template.appearance?.beard?.partsInGroup != null &&
            template.appearance.beard.partsInGroup.Any(p => p?.part != null && !p.part.name.Equals("beard 1", StringComparison.OrdinalIgnoreCase)))
        {
            return Gender.GenderSet.Male;
        }

        // Ambiguous / plot / shared template with no clear gender signal -> skip single-gender lists
        return null;
    }

    private static NpcPrefab? ResolveTemplatePrefab(NpcTemplate template, HashSet<NpcTemplate> visited)
    {
        if (template == null || !visited.Add(template)) return null;

        if (template.baseParts != null)
        {
            foreach (var part in template.baseParts)
            {
                if (part is NpcPrefab prefab)
                {
                    return prefab;
                }
            }
            foreach (var part in template.baseParts)
            {
                if (part is NpcTemplate sub)
                {
                    var found = ResolveTemplatePrefab(sub, visited);
                    if (found != null) return found;
                }
            }
        }

        return null;
    }

    private static void CollectPartsFromGroup<T>(PartContainerGroup<T>? group, Action<T> onPart) where T : AppearancePart
    {
        if (group?.partsInGroup == null) return;
        foreach (var entry in group.partsInGroup)
        {
            if (entry != null && entry.part != null)
            {
                onPart(entry.part);
            }
        }
    }

    private static int NaturalCompare(string? a, string? b)
    {
        if (ReferenceEquals(a, b)) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        var partsA = Regex.Split(a, "([0-9]+)");
        var partsB = Regex.Split(b, "([0-9]+)");
        int len = Math.Min(partsA.Length, partsB.Length);

        for (int i = 0; i < len; i++)
        {
            if (int.TryParse(partsA[i], out int numA) && int.TryParse(partsB[i], out int numB))
            {
                int cmp = numA.CompareTo(numB);
                if (cmp != 0) return cmp;
            }
            else
            {
                int cmp = string.Compare(partsA[i], partsB[i], StringComparison.OrdinalIgnoreCase);
                if (cmp != 0) return cmp;
            }
        }

        return partsA.Length.CompareTo(partsB.Length);
    }

    private static void SortPartLists()
    {
        Bodies.Sort((a, b) => NaturalCompare(a.name, b.name));
        Faces.Sort((a, b) => NaturalCompare(a.name, b.name));
        EyesList.Sort((a, b) => NaturalCompare(a.name, b.name));
        SkullShapes.Sort((a, b) => NaturalCompare(a.name, b.name));
        Breasts.Sort((a, b) => NaturalCompare(a.name, b.name));
        Hairstyles.Sort((a, b) => NaturalCompare(a.name, b.name));
        Hats.Sort((a, b) => NaturalCompare(a.name, b.name));
        Beards.Sort((a, b) => NaturalCompare(a.name, b.name));
        AccessoriesAboveHair.Sort((a, b) => NaturalCompare(a.name, b.name));
        AccessoriesFace.Sort((a, b) => NaturalCompare(a.name, b.name));
        AccessoriesBody.Sort((a, b) => NaturalCompare(a.name, b.name));
        AccessoriesSkullShape.Sort((a, b) => NaturalCompare(a.name, b.name));
        AccessoriesHandBack.Sort((a, b) => NaturalCompare(a.name, b.name));
        AccessoriesHandFront.Sort((a, b) => NaturalCompare(a.name, b.name));
        AccessoriesBehindBody.Sort((a, b) => NaturalCompare(a.name, b.name));
        AccessoriesShortHair.Sort((a, b) => NaturalCompare(a.name, b.name));
    }

    private static void EnsureEmptyElementsFirst()
    {
        var emptyHat = Hats.FirstOrDefault(h => h.name.Equals("hat 1", StringComparison.OrdinalIgnoreCase));
        if (emptyHat != null)
        {
            Hats.Remove(emptyHat);
            Hats.Insert(0, emptyHat);
        }

        var emptyBeard = Beards.FirstOrDefault(b => b.name.Equals("beard 1", StringComparison.OrdinalIgnoreCase));
        if (emptyBeard != null)
        {
            Beards.Remove(emptyBeard);
            Beards.Insert(0, emptyBeard);
        }

        var emptyHair = Hairstyles.FirstOrDefault(h =>
            h.name.Equals("hair 1", StringComparison.OrdinalIgnoreCase) ||
            h.name.Equals("hairstyle 1", StringComparison.OrdinalIgnoreCase));
        if (emptyHair != null)
        {
            Hairstyles.Remove(emptyHair);
            Hairstyles.Insert(0, emptyHair);
        }

        // NOTE: Potion Craft does NOT have an empty "Breast 1" asset (Breast assets start at Breast 2).
        // Empty/no breast is represented by index -1 and cleared via npc.references.renderers.breast.Reset().
    }

    private static void LoadPalettes()
    {
        NpcPrefab? prefab = null;
        try
        {
            prefab = Settings<NpcManagerSettings>.Asset?.defaultNpcPrefab;
        }
        catch { }

        if (prefab != null)
        {
            if (prefab.skinColorPalette?.colors != null) SkinColors.AddRange(prefab.skinColorPalette.colors);
            if (prefab.hairColorPalette?.colors != null) HairColors.AddRange(prefab.hairColorPalette.colors);
            if (prefab.clothesColorPalette1?.colors != null) ClothesColors1.AddRange(prefab.clothesColorPalette1.colors);
            if (prefab.clothesColorPalette2?.colors != null) ClothesColors2.AddRange(prefab.clothesColorPalette2.colors);
            if (prefab.clothesColorPalette3?.colors != null) ClothesColors3.AddRange(prefab.clothesColorPalette3.colors);
            if (prefab.clothesColorPalette4?.colors != null) ClothesColors4.AddRange(prefab.clothesColorPalette4.colors);
        }

        if (SkinColors.Count == 0)
        {
            SkinColors.AddRange(new[]
            {
                new Color(0.95f, 0.84f, 0.72f),
                new Color(0.91f, 0.76f, 0.62f),
                new Color(0.80f, 0.65f, 0.50f),
                new Color(0.65f, 0.48f, 0.35f),
                new Color(0.45f, 0.30f, 0.20f)
            });
        }

        if (HairColors.Count == 0)
        {
            HairColors.AddRange(new[]
            {
                new Color(0.35f, 0.22f, 0.12f),
                new Color(0.15f, 0.12f, 0.10f),
                new Color(0.55f, 0.35f, 0.18f),
                new Color(0.75f, 0.60f, 0.35f),
                new Color(0.70f, 0.30f, 0.15f),
                new Color(0.85f, 0.85f, 0.85f)
            });
        }

        if (ClothesColors1.Count == 0)
        {
            var defaultClothColors = new[]
            {
                new Color(0.25f, 0.35f, 0.45f),
                new Color(0.45f, 0.25f, 0.25f),
                new Color(0.25f, 0.45f, 0.25f),
                new Color(0.50f, 0.40f, 0.25f),
                new Color(0.20f, 0.20f, 0.25f),
                new Color(0.60f, 0.55f, 0.45f)
            };
            ClothesColors1.AddRange(defaultClothColors);
            ClothesColors2.AddRange(defaultClothColors);
            ClothesColors3.AddRange(defaultClothColors);
            ClothesColors4.AddRange(defaultClothColors);
        }
    }

    public static string ColorToHex(Color color)
    {
        return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
    }

    public static Color HexToColor(string hex, Color fallback)
    {
        if (!string.IsNullOrWhiteSpace(hex) && ColorUtility.TryParseHtmlString(hex, out var c))
        {
            return c;
        }
        return fallback;
    }
}
