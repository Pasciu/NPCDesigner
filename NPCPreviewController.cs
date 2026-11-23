using System;
using System.Collections.Generic;
using System.Linq;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.Npc;
using PotionCraft.Npc.MonoBehaviourScripts;
using PotionCraft.Npc.MonoBehaviourScripts.Settings;
using PotionCraft.Npc.Parts;
using PotionCraft.Npc.Parts.Appearance;
using PotionCraft.Npc.Parts.Appearance.Accessories;
using PotionCraft.Npc.Parts.Settings;
using PotionCraft.Settings;
using UnityEngine;

namespace NPCDesigner;

public class NPCPreviewController
{
    private GameObject? _previewObj;
    private NpcMonoBehaviour? _npc;
    private NPCIdleAnimator? _idleAnimator;
    private bool _hasAppliedHeadShift;
    private Vector3 _stageLocalPosition = new Vector3(-4.15f, -0.85f, -1.0f);
    private float _stageScale = 1.0f;

    public NpcMonoBehaviour? Npc => _npc;
    public NPCIdleAnimator? IdleAnimator => _idleAnimator;
    public bool HasActivePreview => _npc != null && _previewObj != null;

    public NPCAssetThumbnailRenderer.PaletteSnapshot GetCurrentPalette()
    {
        Color skin = NPCPartRegistry.SkinColors.Count > 0
            ? NPCPartRegistry.SkinColors[Mathf.Clamp(SelectedSkinColorIndex, 0, NPCPartRegistry.SkinColors.Count - 1)]
            : new Color(0.95f, 0.84f, 0.72f);
        Color hair = NPCPartRegistry.HairColors.Count > 0
            ? NPCPartRegistry.HairColors[Mathf.Clamp(SelectedHairColorIndex, 0, NPCPartRegistry.HairColors.Count - 1)]
            : new Color(0.35f, 0.22f, 0.12f);
        Color c1 = NPCPartRegistry.ClothesColors1.Count > 0
            ? NPCPartRegistry.ClothesColors1[Mathf.Clamp(SelectedClothes1ColorIndex, 0, NPCPartRegistry.ClothesColors1.Count - 1)]
            : Color.white;
        Color c2 = NPCPartRegistry.ClothesColors2.Count > 0
            ? NPCPartRegistry.ClothesColors2[Mathf.Clamp(SelectedClothes2ColorIndex, 0, NPCPartRegistry.ClothesColors2.Count - 1)]
            : Color.white;
        Color c3 = NPCPartRegistry.ClothesColors3.Count > 0
            ? NPCPartRegistry.ClothesColors3[Mathf.Clamp(SelectedClothes3ColorIndex, 0, NPCPartRegistry.ClothesColors3.Count - 1)]
            : Color.white;
        Color c4 = NPCPartRegistry.ClothesColors4.Count > 0
            ? NPCPartRegistry.ClothesColors4[Mathf.Clamp(SelectedClothes4ColorIndex, 0, NPCPartRegistry.ClothesColors4.Count - 1)]
            : Color.white;

        return new NPCAssetThumbnailRenderer.PaletteSnapshot(skin, hair, c1, c2, c3, c4);
    }

    public void SetStagePosition(Vector3 localPos, float scale)
    {
        _stageLocalPosition = localPos;
        _stageScale = scale;
        if (_previewObj != null)
        {
            _previewObj.transform.localPosition = _stageLocalPosition;
            _previewObj.transform.localScale = new Vector3(_stageScale, _stageScale, 1f);
        }
    }

    // Active gender filter mode (Male by default so preview starts cleanly without female overlays)
    public NPCGenderMode CurrentGender { get; private set; } = NPCGenderMode.Male;

    // Current selected indices (relative to the active gender-filtered lists)
    public int SelectedBodyIndex { get; set; }
    public int SelectedFaceIndex { get; set; }
    public int SelectedEyesIndex { get; set; }
    public int SelectedSkullIndex { get; set; }
    public int SelectedBreastIndex { get; set; } = -1; // -1 = Nessuno (breast.Reset())
    public int SelectedHairIndex { get; set; } = 1;
    public int SelectedBeardIndex { get; set; }
    public int SelectedHatIndex { get; set; }

    public int SelectedAccAboveHairIndex { get; set; } = -1;
    public int SelectedAccFaceIndex { get; set; } = -1;
    public int SelectedAccBodyIndex { get; set; } = -1;
    public int SelectedAccSkullIndex { get; set; } = -1;
    public int SelectedAccHandBackIndex { get; set; } = -1;
    public int SelectedAccHandFrontIndex { get; set; } = -1;
    public int SelectedAccBehindBodyIndex { get; set; } = -1;
    public int SelectedAccShortHairIndex { get; set; } = -1;

    public int SelectedSkinColorIndex { get; set; }
    public int SelectedHairColorIndex { get; set; }
    public int SelectedClothes1ColorIndex { get; set; }
    public int SelectedClothes2ColorIndex { get; set; } = 1;
    public int SelectedClothes3ColorIndex { get; set; } = 2;
    public int SelectedClothes4ColorIndex { get; set; } = 3;
    public int SelectedMoodIndex { get; set; } = 3; // 0=VeryBad, 1=Bad, 2=Normal, 3=Good, 4=VeryGood

    public List<Body> GetCurrentBodies() => NPCPartRegistry.GetBodies(CurrentGender);
    public List<Face> GetCurrentFaces() => NPCPartRegistry.GetFaces(CurrentGender);
    public List<SkullShape> GetCurrentSkullShapes() => NPCPartRegistry.GetSkullShapes(CurrentGender);
    public List<Hairstyle> GetCurrentHairstyles() => NPCPartRegistry.GetHairstyles(CurrentGender);

    public Body? GetCurrentBody()
    {
        var bodies = GetCurrentBodies();
        if (bodies.Count == 0) return null;
        int idx = Mathf.Clamp(SelectedBodyIndex, 0, bodies.Count - 1);
        return bodies[idx];
    }

    public List<Breast> GetCurrentCompatibleBreasts()
    {
        return NPCPartRegistry.GetCompatibleBreasts(GetCurrentBody(), CurrentGender);
    }

    public void SpawnPreview(Transform parent)
    {
        DestroyPreview();

        try
        {
            var prefabObj = Settings<NpcManagerSettings>.Asset?.defaultNpcPrefabGameObject;
            if (prefabObj == null)
            {
                Plugin.Log?.LogError("defaultNpcPrefabGameObject is null, cannot spawn preview NPC.");
                return;
            }

            _previewObj = UnityEngine.Object.Instantiate(prefabObj, parent);
            _previewObj.name = "NPCDesigner_PreviewNPC";
            _previewObj.SetActive(true);

            // Destroy any SortingOrderSetter components on root and children so they don't fight our sorting
            foreach (var setter in _previewObj.GetComponentsInChildren<PotionCraft.Utils.SortingOrderSetter.SortingOrderSetter>(true))
            {
                UnityEngine.Object.Destroy(setter);
            }

            // Group all NPC sprite renderers in a SortingGroup with high priority on MenuElements layer
            var sg = _previewObj.GetComponent<UnityEngine.Rendering.SortingGroup>() ?? _previewObj.AddComponent<UnityEngine.Rendering.SortingGroup>();
            sg.sortingLayerName = "MenuElements";
            sg.sortingOrder = 12000;

            // Sync GameObject layers with parent menu
            int targetLayer = parent.gameObject.layer;
            foreach (var tr in _previewObj.GetComponentsInChildren<Transform>(true))
            {
                tr.gameObject.layer = targetLayer;
            }

            // Local position in the Meeting Room stage behind the foreground counter
            _previewObj.transform.localPosition = _stageLocalPosition;
            _previewObj.transform.localScale = new Vector3(_stageScale, _stageScale, 1f);

            _npc = _previewObj.GetComponent<NpcMonoBehaviour>();
            _hasAppliedHeadShift = false;

            if (_npc != null)
            {
                _npc.InitializeComponentOnTakeFromPool();
                _npc.references.ResetComponentOnReturnToPool();

                // Initialize settings from the male base prefab (not HerbalistF!) so head alignment starts neutral & accurate
                var basePrefab = NPCPartRegistry.DefaultMalePrefab
                    ?? Settings<NpcManagerSettings>.Asset?.defaultNpcPrefab;

                if (basePrefab != null && basePrefab.settings != null)
                {
                    basePrefab.settings.ApplyToNpc(_npc);
                    _hasAppliedHeadShift = true;
                    Plugin.Log?.LogInfo($"Initialized preview NPC settings from prefab '{basePrefab.name}'.");
                }
                else
                {
                    _npc.settings = new NpcSettings();
                }

                EnsureVibeSettings(_npc);

                // Prevent NpcMonoBehaviour from running trade/dialogue/path updates in the menu
                _npc.gender = Gender.GenderSet.Male;
                _npc.currentState = NpcState.Idle;
                _npc.Mood = NpcMood.Good;
                _npc.enabled = false;

                // Attach native idle look-around + cursor-tracking driver (zero invented animations)
                _idleAnimator = _previewObj.GetComponent<NPCIdleAnimator>() ?? _previewObj.AddComponent<NPCIdleAnimator>();
                _idleAnimator.Initialize(_npc);
            }

            FixSortingLayers();
            ApplyInitialDefaults();
            Plugin.Log?.LogInfo("NPC Designer preview successfully spawned.");
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"Failed to spawn preview NPC: {ex}");
        }
    }

    public void DestroyPreview()
    {
        if (_previewObj != null)
        {
            try
            {
                UnityEngine.Object.Destroy(_previewObj);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error destroying preview NPC: {ex.Message}");
            }
            _previewObj = null;
            _npc = null;
            _idleAnimator = null;
            _hasAppliedHeadShift = false;
        }
    }

    private static void EnsureVibeSettings(NpcMonoBehaviour npc)
    {
        if (npc.settings?.emotionsSettings == null) return;

        var emo = npc.settings.emotionsSettings;
        bool needsPleased = emo.pleasedVibeSprite == null || emo.pleasedVibeSprite.Length == 0;
        bool needsAngry = emo.angryVibeSprite == null || emo.angryVibeSprite.Length == 0;
        if (!needsPleased && !needsAngry) return;

        var fallbackEmo = Settings<NpcManagerSettings>.Asset?.defaultNpcPrefab?.settings?.emotionsSettings;
        if (fallbackEmo == null)
        {
            foreach (var p in Resources.FindObjectsOfTypeAll<NpcPrefab>())
            {
                var e = p?.settings?.emotionsSettings;
                if (e != null && e.pleasedVibeSprite != null && e.pleasedVibeSprite.Length > 0 &&
                    e.angryVibeSprite != null && e.angryVibeSprite.Length > 0)
                {
                    fallbackEmo = e;
                    break;
                }
            }
        }

        if (fallbackEmo == null) return;

        if (needsPleased && fallbackEmo.pleasedVibeSprite != null && fallbackEmo.pleasedVibeSprite.Length > 0)
        {
            emo.pleasedVibeSprite = (Sprite[])fallbackEmo.pleasedVibeSprite.Clone();
            emo.pleasedVibePosition = fallbackEmo.pleasedVibePosition;
            emo.pleasedVibeAnimationTime = fallbackEmo.pleasedVibeAnimationTime;
        }

        if (needsAngry && fallbackEmo.angryVibeSprite != null && fallbackEmo.angryVibeSprite.Length > 0)
        {
            emo.angryVibeSprite = (Sprite[])fallbackEmo.angryVibeSprite.Clone();
            emo.angryVibePosition = fallbackEmo.angryVibePosition;
            emo.angryVibeAnimationTime = fallbackEmo.angryVibeAnimationTime;
        }
    }

    public void FixSortingLayers()
    {
        if (_previewObj == null) return;

        _previewObj.SetActive(true);

        var sg = _previewObj.GetComponent<UnityEngine.Rendering.SortingGroup>() ?? _previewObj.AddComponent<UnityEngine.Rendering.SortingGroup>();
        sg.sortingLayerName = "MenuElements";
        sg.sortingOrder = 12000;

        int targetLayer = _previewObj.transform.parent != null ? _previewObj.transform.parent.gameObject.layer : 0;
        foreach (var tr in _previewObj.GetComponentsInChildren<Transform>(true))
        {
            tr.gameObject.layer = targetLayer;
        }

        var defaultShader = Shader.Find("Sprites/Default");
        if (defaultShader != null)
        {
            foreach (var sr in _previewObj.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.sharedMaterial != null && sr.sharedMaterial.shader != defaultShader)
                {
                    sr.material = new Material(defaultShader);
                }
            }
        }
    }

    public void LogDiagnosticInfo()
    {
        if (_previewObj == null)
        {
            Plugin.Log?.LogWarning("[NPCPreview] _previewObj is null!");
            return;
        }

        var renderers = _previewObj.GetComponentsInChildren<SpriteRenderer>(true);
        int visibleCount = 0;
        foreach (var r in renderers)
        {
            if (r.sprite != null && r.enabled && r.gameObject.activeInHierarchy)
            {
                visibleCount++;
            }
        }
        float headAngle = _npc?.references?.head != null ? _npc.references.head.localEulerAngles.z : 0f;
        Plugin.Log?.LogInfo(
            $"[NPCPreview] Gender={CurrentGender}, Body='{GetCurrentBody()?.name ?? "null"}', " +
            $"BreastIdx={SelectedBreastIndex} (compat={GetCurrentCompatibleBreasts().Count}), " +
            $"Active={_previewObj.activeInHierarchy}, LocalPos={_previewObj.transform.localPosition}, " +
            $"HeadAngle={headAngle:F2}, VisibleSpriteRenderers={visibleCount}/{renderers.Length}");
    }

    public void SetGender(NPCGenderMode newMode)
    {
        CurrentGender = newMode;
        if (_npc == null) return;

        _npc.gender = newMode == NPCGenderMode.Female ? Gender.GenderSet.Female : Gender.GenderSet.Male;

        // Reset indices coherently for the newly selected gender pool
        SelectedBodyIndex = 0;
        SelectedFaceIndex = 0;
        SelectedSkullIndex = 0;

        var hairs = GetCurrentHairstyles();
        SelectedHairIndex = hairs.Count > 1 ? 1 : 0;

        // Clear beard when switching to Female; keep clean default when switching
        SelectedBeardIndex = 0;

        // Clear breast on Male; select first compatible breast on Female (handled inside ApplyBody)
        SelectedBreastIndex = newMode == NPCGenderMode.Female ? 0 : -1;

        ApplyBody(SelectedBodyIndex);
        ApplyFace(SelectedFaceIndex);
        ApplySkullShape(SelectedSkullIndex);
        ApplyHairstyle(SelectedHairIndex);
        ApplyBeard(SelectedBeardIndex);
        ApplyHat(SelectedHatIndex);
        ReapplyAllColors();
    }

    public void ApplyInitialDefaults()
    {
        if (_npc == null) return;

        // Ensure breast renderer starts completely cleared
        _npc.references.renderers.breast.Reset();
        SelectedBreastIndex = CurrentGender == NPCGenderMode.Female ? 0 : -1;

        var bodies = GetCurrentBodies();
        if (bodies.Count > 0) ApplyBody(SelectedBodyIndex);

        var faces = GetCurrentFaces();
        if (faces.Count > 0) ApplyFace(SelectedFaceIndex);

        if (NPCPartRegistry.EyesList.Count > 0) ApplyEyes(SelectedEyesIndex);

        var skulls = GetCurrentSkullShapes();
        if (skulls.Count > 0) ApplySkullShape(SelectedSkullIndex);

        var hairs = GetCurrentHairstyles();
        if (hairs.Count > 0) ApplyHairstyle(Mathf.Clamp(SelectedHairIndex, 0, hairs.Count - 1));

        if (NPCPartRegistry.Beards.Count > 0) ApplyBeard(SelectedBeardIndex);
        if (NPCPartRegistry.Hats.Count > 0) ApplyHat(SelectedHatIndex);

        ReapplyAllColors();

        ApplyAccAboveHair(SelectedAccAboveHairIndex);
        ApplyAccFace(SelectedAccFaceIndex);
        ApplyAccBody(SelectedAccBodyIndex);
        ApplyAccSkull(SelectedAccSkullIndex);
        ApplyAccHandBack(SelectedAccHandBackIndex);
        ApplyAccHandFront(SelectedAccHandFrontIndex);
        ApplyAccBehindBody(SelectedAccBehindBodyIndex);
        ApplyAccShortHair(SelectedAccShortHairIndex);
    }

    private void ApplyHeadAlignmentForBody(Body? body)
    {
        if (_npc == null || _npc.settings == null || _npc.references == null) return;

        var targetPrefab = NPCPartRegistry.GetPrefabForBody(body, CurrentGender);
        var other = targetPrefab?.settings?.otherSettings;
        if (other == null || other.headShift == Vector2.zero) return;

        // Kill active look-at tweens and reset head rotation to neutral before ShiftTransforms runs
        _idleAnimator?.PrepareForHeadShift();

        // Undo previous ShiftTransforms cleanly so child positions never drift cumulatively
        if (_hasAppliedHeadShift)
        {
            _npc.settings.ShiftTransforms(_npc, inverseShift: true);
        }

        _npc.settings.otherSettings.headShift = other.headShift;
        _npc.settings.otherSettings.headRotationPivot = other.headRotationPivot;
        _npc.references.head.localPosition = other.headShift;
        _npc.references.headRotationAnchor.localPosition = other.headRotationPivot;
        _npc.references.headBack.localPosition = other.headShift;
        _npc.settings.ShiftTransforms(_npc, inverseShift: false);
        _hasAppliedHeadShift = true;
    }

    private void ReapplyAllColors()
    {
        if (NPCPartRegistry.SkinColors.Count > 0) ApplySkinColor(SelectedSkinColorIndex);
        if (NPCPartRegistry.HairColors.Count > 0) ApplyHairColor(SelectedHairColorIndex);
        if (NPCPartRegistry.ClothesColors1.Count > 0) ApplyClothesColor(1, SelectedClothes1ColorIndex);
        if (NPCPartRegistry.ClothesColors2.Count > 0) ApplyClothesColor(2, SelectedClothes2ColorIndex);
        if (NPCPartRegistry.ClothesColors3.Count > 0) ApplyClothesColor(3, SelectedClothes3ColorIndex);
        if (NPCPartRegistry.ClothesColors4.Count > 0) ApplyClothesColor(4, SelectedClothes4ColorIndex);
    }

    public void ApplyBody(int index)
    {
        var bodies = GetCurrentBodies();
        if (_npc == null || bodies.Count == 0) return;

        SelectedBodyIndex = Mathf.Clamp(index, 0, bodies.Count - 1);
        var body = bodies[SelectedBodyIndex];
        body.ApplyPartTo(_npc, _npc.settings, 0);

        ApplyHeadAlignmentForBody(body);

        // Sync breast overlay with the newly selected body & gender
        var compatibleBreasts = GetCurrentCompatibleBreasts();
        if (CurrentGender == NPCGenderMode.Male || compatibleBreasts.Count == 0)
        {
            ApplyBreast(-1);
        }
        else if (CurrentGender == NPCGenderMode.Female)
        {
            int nextBreast = SelectedBreastIndex >= 0 && SelectedBreastIndex < compatibleBreasts.Count
                ? SelectedBreastIndex
                : 0;
            ApplyBreast(nextBreast);
        }
        else
        {
            // All mode: keep breast only if already selected and compatible, otherwise clear cleanly
            if (SelectedBreastIndex >= 0 && SelectedBreastIndex < compatibleBreasts.Count)
            {
                ApplyBreast(SelectedBreastIndex);
            }
            else
            {
                ApplyBreast(-1);
            }
        }

        ReapplyAllColors();
        FixSortingLayers();
    }

    public void ApplyFace(int index)
    {
        var faces = GetCurrentFaces();
        if (_npc == null || faces.Count == 0) return;

        SelectedFaceIndex = Mathf.Clamp(index, 0, faces.Count - 1);
        faces[SelectedFaceIndex].ApplyPartTo(_npc, _npc.settings, 0);
        ApplyMood(SelectedMoodIndex);
        if (NPCPartRegistry.SkinColors.Count > 0) ApplySkinColor(SelectedSkinColorIndex);
        if (NPCPartRegistry.HairColors.Count > 0) ApplyHairColor(SelectedHairColorIndex);
        FixSortingLayers();
    }

    public void ApplyMood(int moodIndex, bool triggerVibe = false)
    {
        if (_npc == null) return;
        SelectedMoodIndex = Mathf.Clamp(moodIndex, 0, 4);
        var mood = SelectedMoodIndex switch
        {
            0 => NpcMood.VeryBad,
            1 => NpcMood.Bad,
            2 => NpcMood.Normal,
            3 => NpcMood.Good,
            4 => NpcMood.VeryGood,
            _ => NpcMood.Good
        };
        _npc.Mood = mood;
        if (_npc.emotions != null)
        {
            try
            {
                _npc.emotions.Emotion = NpcEmotionState.Idle;
                _npc.emotions.SetFaceSpritesToCurrentMood();

                if (triggerVibe)
                {
                    var emo = _npc.settings?.emotionsSettings;
                    if (SelectedMoodIndex == 4 && emo?.pleasedVibeSprite != null && emo.pleasedVibeSprite.Length > 0)
                    {
                        _npc.emotions.Emotion = NpcEmotionState.Pleased;
                    }
                    else if (SelectedMoodIndex == 0 && emo?.angryVibeSprite != null && emo.angryVibeSprite.Length > 0)
                    {
                        _npc.emotions.Emotion = NpcEmotionState.Angry;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[NPCPreview] Failed to update mood/vibe: {ex.Message}");
            }
        }
        FixSortingLayers();
    }

    public void ApplyEyes(int index)
    {
        if (_npc == null || NPCPartRegistry.EyesList.Count == 0) return;
        SelectedEyesIndex = Mathf.Clamp(index, 0, NPCPartRegistry.EyesList.Count - 1);
        NPCPartRegistry.EyesList[SelectedEyesIndex].ApplyPartTo(_npc, _npc.settings, 0);
        FixSortingLayers();
    }

    public void ApplySkullShape(int index)
    {
        var skulls = GetCurrentSkullShapes();
        if (_npc == null || skulls.Count == 0) return;

        SelectedSkullIndex = Mathf.Clamp(index, 0, skulls.Count - 1);
        skulls[SelectedSkullIndex].ApplyPartTo(_npc, _npc.settings, 0);
        if (NPCPartRegistry.SkinColors.Count > 0) ApplySkinColor(SelectedSkinColorIndex);
        FixSortingLayers();
    }

    public void ApplyBreast(int index)
    {
        if (_npc == null) return;

        var compatible = GetCurrentCompatibleBreasts();
        if (CurrentGender == NPCGenderMode.Male || index < 0 || compatible.Count == 0)
        {
            SelectedBreastIndex = -1;
            _npc.references.renderers.breast.Reset();
            FixSortingLayers();
            return;
        }

        SelectedBreastIndex = Mathf.Clamp(index, 0, compatible.Count - 1);
        compatible[SelectedBreastIndex].ApplyPartTo(_npc, _npc.settings, 0);

        if (NPCPartRegistry.ClothesColors1.Count > 0) ApplyClothesColor(1, SelectedClothes1ColorIndex);
        if (NPCPartRegistry.ClothesColors2.Count > 0) ApplyClothesColor(2, SelectedClothes2ColorIndex);
        if (NPCPartRegistry.ClothesColors3.Count > 0) ApplyClothesColor(3, SelectedClothes3ColorIndex);
        if (NPCPartRegistry.ClothesColors4.Count > 0) ApplyClothesColor(4, SelectedClothes4ColorIndex);
        FixSortingLayers();
    }

    public void ApplyHairstyle(int index)
    {
        var hairs = GetCurrentHairstyles();
        if (_npc == null || hairs.Count == 0) return;

        SelectedHairIndex = Mathf.Clamp(index, 0, hairs.Count - 1);
        hairs[SelectedHairIndex].ApplyPartTo(_npc, _npc.settings, 0);
        if (NPCPartRegistry.HairColors.Count > 0) ApplyHairColor(SelectedHairColorIndex);
        FixSortingLayers();
    }

    public void ApplyHat(int index)
    {
        if (_npc == null || NPCPartRegistry.Hats.Count == 0) return;
        SelectedHatIndex = Mathf.Clamp(index, 0, NPCPartRegistry.Hats.Count - 1);
        NPCPartRegistry.Hats[SelectedHatIndex].ApplyPartTo(_npc, _npc.settings, 0);
        ReapplyAllColors();
        FixSortingLayers();
    }

    public void ApplyBeard(int index)
    {
        if (_npc == null || NPCPartRegistry.Beards.Count == 0) return;
        SelectedBeardIndex = Mathf.Clamp(index, 0, NPCPartRegistry.Beards.Count - 1);
        NPCPartRegistry.Beards[SelectedBeardIndex].ApplyPartTo(_npc, _npc.settings, 0);
        if (NPCPartRegistry.HairColors.Count > 0) ApplyHairColor(SelectedHairColorIndex);
        FixSortingLayers();
    }

    public void ApplyAccAboveHair(int index)
    {
        if (_npc == null) return;
        SelectedAccAboveHairIndex = index;
        if (index < 0 || index >= NPCPartRegistry.AccessoriesAboveHair.Count)
        {
            AppearancePart.ApplyEmptyPart<AccessoryAboveHair>(_npc, _npc.settings, 0);
        }
        else
        {
            NPCPartRegistry.AccessoriesAboveHair[index].ApplyPartTo(_npc, _npc.settings, 0);
        }
        FixSortingLayers();
    }

    public void ApplyAccFace(int index)
    {
        if (_npc == null) return;
        SelectedAccFaceIndex = index;
        if (index < 0 || index >= NPCPartRegistry.AccessoriesFace.Count)
        {
            AppearancePart.ApplyEmptyPart<AccessoryFace>(_npc, _npc.settings, 0);
        }
        else
        {
            NPCPartRegistry.AccessoriesFace[index].ApplyPartTo(_npc, _npc.settings, 0);
        }
        FixSortingLayers();
    }

    public void ApplyAccBody(int index)
    {
        if (_npc == null) return;
        SelectedAccBodyIndex = index;
        if (index < 0 || index >= NPCPartRegistry.AccessoriesBody.Count)
        {
            AppearancePart.ApplyEmptyPart<AccessoryBody>(_npc, _npc.settings, 0);
        }
        else
        {
            NPCPartRegistry.AccessoriesBody[index].ApplyPartTo(_npc, _npc.settings, 0);
        }
        FixSortingLayers();
    }

    public void ApplyAccSkull(int index)
    {
        if (_npc == null) return;
        SelectedAccSkullIndex = index;
        if (index < 0 || index >= NPCPartRegistry.AccessoriesSkullShape.Count)
        {
            AppearancePart.ApplyEmptyPart<AccessorySkullShape>(_npc, _npc.settings, 0);
        }
        else
        {
            NPCPartRegistry.AccessoriesSkullShape[index].ApplyPartTo(_npc, _npc.settings, 0);
        }
        FixSortingLayers();
    }

    public void ApplyAccHandBack(int index)
    {
        if (_npc == null) return;
        SelectedAccHandBackIndex = index;
        if (index < 0 || index >= NPCPartRegistry.AccessoriesHandBack.Count)
        {
            AppearancePart.ApplyEmptyPart<AccessoryHandBack>(_npc, _npc.settings, 0);
        }
        else
        {
            NPCPartRegistry.AccessoriesHandBack[index].ApplyPartTo(_npc, _npc.settings, 0);
        }
        FixSortingLayers();
    }

    public void ApplyAccHandFront(int index)
    {
        if (_npc == null) return;
        SelectedAccHandFrontIndex = index;
        if (index < 0 || index >= NPCPartRegistry.AccessoriesHandFront.Count)
        {
            AppearancePart.ApplyEmptyPart<AccessoryHandFront>(_npc, _npc.settings, 0);
        }
        else
        {
            NPCPartRegistry.AccessoriesHandFront[index].ApplyPartTo(_npc, _npc.settings, 0);
        }
        FixSortingLayers();
    }

    public void ApplyAccBehindBody(int index)
    {
        if (_npc == null) return;
        SelectedAccBehindBodyIndex = index;
        if (index < 0 || index >= NPCPartRegistry.AccessoriesBehindBody.Count)
        {
            AppearancePart.ApplyEmptyPart<AccessoryBehindBody>(_npc, _npc.settings, 0);
        }
        else
        {
            NPCPartRegistry.AccessoriesBehindBody[index].ApplyPartTo(_npc, _npc.settings, 0);
        }
        FixSortingLayers();
    }

    public void ApplyAccShortHair(int index)
    {
        if (_npc == null) return;
        SelectedAccShortHairIndex = index;
        if (index < 0 || index >= NPCPartRegistry.AccessoriesShortHair.Count)
        {
            AppearancePart.ApplyEmptyPart<AccessoryShortHair>(_npc, _npc.settings, 0);
        }
        else
        {
            NPCPartRegistry.AccessoriesShortHair[index].ApplyPartTo(_npc, _npc.settings, 0);
        }
        FixSortingLayers();
    }

    public void ApplySkinColor(int index)
    {
        if (_npc == null || NPCPartRegistry.SkinColors.Count == 0) return;
        SelectedSkinColorIndex = Mathf.Clamp(index, 0, NPCPartRegistry.SkinColors.Count - 1);
        AppearanceContainer.ApplySkinColor(_npc, NPCPartRegistry.SkinColors[SelectedSkinColorIndex]);
    }

    public void ApplyHairColor(int index)
    {
        if (_npc == null || NPCPartRegistry.HairColors.Count == 0) return;
        SelectedHairColorIndex = Mathf.Clamp(index, 0, NPCPartRegistry.HairColors.Count - 1);
        AppearanceContainer.ApplyHairColor(_npc, NPCPartRegistry.HairColors[SelectedHairColorIndex]);
    }

    public void ApplyClothesColor(int layer, int index)
    {
        if (_npc == null) return;
        var list = layer switch
        {
            1 => NPCPartRegistry.ClothesColors1,
            2 => NPCPartRegistry.ClothesColors2,
            3 => NPCPartRegistry.ClothesColors3,
            4 => NPCPartRegistry.ClothesColors4,
            _ => NPCPartRegistry.ClothesColors1
        };

        if (list.Count == 0) return;
        int clamped = Mathf.Clamp(index, 0, list.Count - 1);

        switch (layer)
        {
            case 1:
                SelectedClothes1ColorIndex = clamped;
                AppearanceContainer.ApplyClothesColor(_npc, list[clamped], 2);
                break;
            case 2:
                SelectedClothes2ColorIndex = clamped;
                AppearanceContainer.ApplyClothesColor(_npc, list[clamped], 3);
                break;
            case 3:
                SelectedClothes3ColorIndex = clamped;
                AppearanceContainer.ApplyClothesColor(_npc, list[clamped], 4);
                break;
            case 4:
                SelectedClothes4ColorIndex = clamped;
                AppearanceContainer.ApplyClothesColor(_npc, list[clamped], 5);
                break;
        }
    }

    public void Randomize()
    {
        if (_npc == null) return;

        // If in "All" mode, pick a coherent target gender for this roll so male/female traits stay natural
        var targetGender = CurrentGender == NPCGenderMode.All
            ? (UnityEngine.Random.value < 0.5f ? NPCGenderMode.Male : NPCGenderMode.Female)
            : CurrentGender;

        var activeBodies = GetCurrentBodies();
        var activeFaces = GetCurrentFaces();
        var activeSkulls = GetCurrentSkullShapes();
        var activeHairs = GetCurrentHairstyles();

        var poolBodies = NPCPartRegistry.GetBodies(targetGender);
        var poolFaces = NPCPartRegistry.GetFaces(targetGender);
        var poolSkulls = NPCPartRegistry.GetSkullShapes(targetGender);
        var poolHairs = NPCPartRegistry.GetHairstyles(targetGender);

        if (poolBodies.Count > 0)
        {
            var chosen = poolBodies[UnityEngine.Random.Range(0, poolBodies.Count)];
            int idx = activeBodies.IndexOf(chosen);
            ApplyBody(idx >= 0 ? idx : UnityEngine.Random.Range(0, activeBodies.Count));
        }

        if (poolFaces.Count > 0)
        {
            var chosen = poolFaces[UnityEngine.Random.Range(0, poolFaces.Count)];
            int idx = activeFaces.IndexOf(chosen);
            ApplyFace(idx >= 0 ? idx : UnityEngine.Random.Range(0, activeFaces.Count));
        }

        if (NPCPartRegistry.EyesList.Count > 0)
        {
            ApplyEyes(UnityEngine.Random.Range(0, NPCPartRegistry.EyesList.Count));
        }

        if (poolSkulls.Count > 0)
        {
            var chosen = poolSkulls[UnityEngine.Random.Range(0, poolSkulls.Count)];
            int idx = activeSkulls.IndexOf(chosen);
            ApplySkullShape(idx >= 0 ? idx : UnityEngine.Random.Range(0, activeSkulls.Count));
        }

        // Coherent Breast randomization: NEVER on Male; compatible breast only on Female
        var compatibleBreasts = GetCurrentCompatibleBreasts();
        if (targetGender == NPCGenderMode.Female && compatibleBreasts.Count > 0)
        {
            ApplyBreast(UnityEngine.Random.Range(0, compatibleBreasts.Count));
        }
        else
        {
            ApplyBreast(-1);
        }

        if (poolHairs.Count > 0)
        {
            var chosen = poolHairs[UnityEngine.Random.Range(0, poolHairs.Count)];
            int idx = activeHairs.IndexOf(chosen);
            ApplyHairstyle(idx >= 0 ? idx : UnityEngine.Random.Range(0, activeHairs.Count));
        }

        if (NPCPartRegistry.Hats.Count > 0)
        {
            // 65% no hat so hairstyles remain visible
            ApplyHat(UnityEngine.Random.value < 0.35f ? UnityEngine.Random.Range(1, NPCPartRegistry.Hats.Count) : 0);
        }

        // Coherent Beard randomization: NEVER on Female; ~45% chance on Male
        if (NPCPartRegistry.Beards.Count > 0)
        {
            if (targetGender == NPCGenderMode.Male && NPCPartRegistry.Beards.Count > 1 && UnityEngine.Random.value < 0.45f)
            {
                ApplyBeard(UnityEngine.Random.Range(1, NPCPartRegistry.Beards.Count));
            }
            else
            {
                ApplyBeard(0); // Beard 1 = Nessuno
            }
        }

        if (NPCPartRegistry.SkinColors.Count > 0) ApplySkinColor(UnityEngine.Random.Range(0, NPCPartRegistry.SkinColors.Count));
        if (NPCPartRegistry.HairColors.Count > 0) ApplyHairColor(UnityEngine.Random.Range(0, NPCPartRegistry.HairColors.Count));
        if (NPCPartRegistry.ClothesColors1.Count > 0) ApplyClothesColor(1, UnityEngine.Random.Range(0, NPCPartRegistry.ClothesColors1.Count));
        if (NPCPartRegistry.ClothesColors2.Count > 0) ApplyClothesColor(2, UnityEngine.Random.Range(0, NPCPartRegistry.ClothesColors2.Count));
        if (NPCPartRegistry.ClothesColors3.Count > 0) ApplyClothesColor(3, UnityEngine.Random.Range(0, NPCPartRegistry.ClothesColors3.Count));
        if (NPCPartRegistry.ClothesColors4.Count > 0) ApplyClothesColor(4, UnityEngine.Random.Range(0, NPCPartRegistry.ClothesColors4.Count));

        ApplyAccAboveHair(UnityEngine.Random.value < 0.25f && NPCPartRegistry.AccessoriesAboveHair.Count > 0 ? UnityEngine.Random.Range(0, NPCPartRegistry.AccessoriesAboveHair.Count) : -1);
        ApplyAccFace(UnityEngine.Random.value < 0.25f && NPCPartRegistry.AccessoriesFace.Count > 0 ? UnityEngine.Random.Range(0, NPCPartRegistry.AccessoriesFace.Count) : -1);
        ApplyAccBody(UnityEngine.Random.value < 0.25f && NPCPartRegistry.AccessoriesBody.Count > 0 ? UnityEngine.Random.Range(0, NPCPartRegistry.AccessoriesBody.Count) : -1);
        ApplyAccSkull(UnityEngine.Random.value < 0.25f && NPCPartRegistry.AccessoriesSkullShape.Count > 0 ? UnityEngine.Random.Range(0, NPCPartRegistry.AccessoriesSkullShape.Count) : -1);
        ApplyAccHandBack(UnityEngine.Random.value < 0.25f && NPCPartRegistry.AccessoriesHandBack.Count > 0 ? UnityEngine.Random.Range(0, NPCPartRegistry.AccessoriesHandBack.Count) : -1);
        ApplyAccHandFront(UnityEngine.Random.value < 0.25f && NPCPartRegistry.AccessoriesHandFront.Count > 0 ? UnityEngine.Random.Range(0, NPCPartRegistry.AccessoriesHandFront.Count) : -1);
        ApplyAccBehindBody(UnityEngine.Random.value < 0.25f && NPCPartRegistry.AccessoriesBehindBody.Count > 0 ? UnityEngine.Random.Range(0, NPCPartRegistry.AccessoriesBehindBody.Count) : -1);
        ApplyAccShortHair(UnityEngine.Random.value < 0.25f && NPCPartRegistry.AccessoriesShortHair.Count > 0 ? UnityEngine.Random.Range(0, NPCPartRegistry.AccessoriesShortHair.Count) : -1);
    }

    public NPCPreset ExportPreset(string name)
    {
        string GetName<T>(List<T> list, int index) where T : UnityEngine.Object
        {
            if (index >= 0 && index < list.Count) return list[index].name;
            return "";
        }

        var compatibleBreasts = GetCurrentCompatibleBreasts();

        return new NPCPreset
        {
            PresetName = name,
            Gender = CurrentGender.ToString(),
            BodyName = GetName(GetCurrentBodies(), SelectedBodyIndex),
            FaceName = GetName(GetCurrentFaces(), SelectedFaceIndex),
            EyesName = GetName(NPCPartRegistry.EyesList, SelectedEyesIndex),
            SkullShapeName = GetName(GetCurrentSkullShapes(), SelectedSkullIndex),
            BreastName = GetName(compatibleBreasts, SelectedBreastIndex),
            HairstyleName = GetName(GetCurrentHairstyles(), SelectedHairIndex),
            BeardName = GetName(NPCPartRegistry.Beards, SelectedBeardIndex),
            HatName = GetName(NPCPartRegistry.Hats, SelectedHatIndex),

            AccessoryAboveHair = GetName(NPCPartRegistry.AccessoriesAboveHair, SelectedAccAboveHairIndex),
            AccessoryFace = GetName(NPCPartRegistry.AccessoriesFace, SelectedAccFaceIndex),
            AccessoryBody = GetName(NPCPartRegistry.AccessoriesBody, SelectedAccBodyIndex),
            AccessorySkullShape = GetName(NPCPartRegistry.AccessoriesSkullShape, SelectedAccSkullIndex),
            AccessoryHandBack = GetName(NPCPartRegistry.AccessoriesHandBack, SelectedAccHandBackIndex),
            AccessoryHandFront = GetName(NPCPartRegistry.AccessoriesHandFront, SelectedAccHandFrontIndex),
            AccessoryBehindBody = GetName(NPCPartRegistry.AccessoriesBehindBody, SelectedAccBehindBodyIndex),
            AccessoryShortHair = GetName(NPCPartRegistry.AccessoriesShortHair, SelectedAccShortHairIndex),

            SkinColorHex = SelectedSkinColorIndex >= 0 && SelectedSkinColorIndex < NPCPartRegistry.SkinColors.Count ? NPCPartRegistry.ColorToHex(NPCPartRegistry.SkinColors[SelectedSkinColorIndex]) : "",
            HairColorHex = SelectedHairColorIndex >= 0 && SelectedHairColorIndex < NPCPartRegistry.HairColors.Count ? NPCPartRegistry.ColorToHex(NPCPartRegistry.HairColors[SelectedHairColorIndex]) : "",
            ClothesColor1Hex = SelectedClothes1ColorIndex >= 0 && SelectedClothes1ColorIndex < NPCPartRegistry.ClothesColors1.Count ? NPCPartRegistry.ColorToHex(NPCPartRegistry.ClothesColors1[SelectedClothes1ColorIndex]) : "",
            ClothesColor2Hex = SelectedClothes2ColorIndex >= 0 && SelectedClothes2ColorIndex < NPCPartRegistry.ClothesColors2.Count ? NPCPartRegistry.ColorToHex(NPCPartRegistry.ClothesColors2[SelectedClothes2ColorIndex]) : "",
            ClothesColor3Hex = SelectedClothes3ColorIndex >= 0 && SelectedClothes3ColorIndex < NPCPartRegistry.ClothesColors3.Count ? NPCPartRegistry.ColorToHex(NPCPartRegistry.ClothesColors3[SelectedClothes3ColorIndex]) : "",
            ClothesColor4Hex = SelectedClothes4ColorIndex >= 0 && SelectedClothes4ColorIndex < NPCPartRegistry.ClothesColors4.Count ? NPCPartRegistry.ColorToHex(NPCPartRegistry.ClothesColors4[SelectedClothes4ColorIndex]) : ""
        };
    }

    public void ApplyPreset(NPCPreset preset)
    {
        if (_npc == null) return;

        // Restore gender mode (inferring from BreastName / Body if loading an older preset)
        if (!string.IsNullOrWhiteSpace(preset.Gender) &&
            Enum.TryParse<NPCGenderMode>(preset.Gender, ignoreCase: true, out var parsedGender))
        {
            CurrentGender = parsedGender;
        }
        else if (!string.IsNullOrWhiteSpace(preset.BreastName) &&
                 !preset.BreastName.Equals("Breast 1", StringComparison.OrdinalIgnoreCase))
        {
            CurrentGender = NPCGenderMode.Female;
        }
        else
        {
            CurrentGender = NPCGenderMode.All;
        }

        _npc.gender = CurrentGender == NPCGenderMode.Female ? Gender.GenderSet.Female : Gender.GenderSet.Male;

        int FindIndex<T>(List<T> list, string name) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(name)) return -1;
            return list.FindIndex(item => string.Equals(item.name, name, StringComparison.OrdinalIgnoreCase));
        }

        var bodies = GetCurrentBodies();
        int bIdx = FindIndex(bodies, preset.BodyName);
        if (bIdx < 0 && CurrentGender != NPCGenderMode.All)
        {
            // Fallback to All mode if preset contains parts outside the declared gender
            CurrentGender = NPCGenderMode.All;
            bodies = GetCurrentBodies();
            bIdx = FindIndex(bodies, preset.BodyName);
        }
        if (bIdx >= 0) ApplyBody(bIdx);

        int fIdx = FindIndex(GetCurrentFaces(), preset.FaceName);
        if (fIdx >= 0) ApplyFace(fIdx);

        int eIdx = FindIndex(NPCPartRegistry.EyesList, preset.EyesName);
        if (eIdx >= 0) ApplyEyes(eIdx);

        int sIdx = FindIndex(GetCurrentSkullShapes(), preset.SkullShapeName);
        if (sIdx >= 0) ApplySkullShape(sIdx);

        // Apply or clear Breast explicitly
        if (string.IsNullOrWhiteSpace(preset.BreastName) ||
            preset.BreastName.Equals("Breast 1", StringComparison.OrdinalIgnoreCase))
        {
            ApplyBreast(-1);
        }
        else
        {
            int brIdx = FindIndex(GetCurrentCompatibleBreasts(), preset.BreastName);
            ApplyBreast(brIdx);
        }

        int hIdx = FindIndex(GetCurrentHairstyles(), preset.HairstyleName);
        if (hIdx >= 0) ApplyHairstyle(hIdx);

        int htIdx = FindIndex(NPCPartRegistry.Hats, preset.HatName);
        if (htIdx >= 0) ApplyHat(htIdx);

        int bdIdx = FindIndex(NPCPartRegistry.Beards, preset.BeardName);
        if (bdIdx >= 0) ApplyBeard(bdIdx);

        ApplyAccAboveHair(FindIndex(NPCPartRegistry.AccessoriesAboveHair, preset.AccessoryAboveHair));
        ApplyAccFace(FindIndex(NPCPartRegistry.AccessoriesFace, preset.AccessoryFace));
        ApplyAccBody(FindIndex(NPCPartRegistry.AccessoriesBody, preset.AccessoryBody));
        ApplyAccSkull(FindIndex(NPCPartRegistry.AccessoriesSkullShape, preset.AccessorySkullShape));
        ApplyAccHandBack(FindIndex(NPCPartRegistry.AccessoriesHandBack, preset.AccessoryHandBack));
        ApplyAccHandFront(FindIndex(NPCPartRegistry.AccessoriesHandFront, preset.AccessoryHandFront));
        ApplyAccBehindBody(FindIndex(NPCPartRegistry.AccessoriesBehindBody, preset.AccessoryBehindBody));
        ApplyAccShortHair(FindIndex(NPCPartRegistry.AccessoriesShortHair, preset.AccessoryShortHair));

        if (!string.IsNullOrEmpty(preset.SkinColorHex))
        {
            var c = NPCPartRegistry.HexToColor(preset.SkinColorHex, Color.white);
            AppearanceContainer.ApplySkinColor(_npc, c);
        }
        if (!string.IsNullOrEmpty(preset.HairColorHex))
        {
            var c = NPCPartRegistry.HexToColor(preset.HairColorHex, Color.white);
            AppearanceContainer.ApplyHairColor(_npc, c);
        }
        if (!string.IsNullOrEmpty(preset.ClothesColor1Hex))
        {
            var c = NPCPartRegistry.HexToColor(preset.ClothesColor1Hex, Color.white);
            AppearanceContainer.ApplyClothesColor(_npc, c, 2);
        }
        if (!string.IsNullOrEmpty(preset.ClothesColor2Hex))
        {
            var c = NPCPartRegistry.HexToColor(preset.ClothesColor2Hex, Color.white);
            AppearanceContainer.ApplyClothesColor(_npc, c, 3);
        }
        if (!string.IsNullOrEmpty(preset.ClothesColor3Hex))
        {
            var c = NPCPartRegistry.HexToColor(preset.ClothesColor3Hex, Color.white);
            AppearanceContainer.ApplyClothesColor(_npc, c, 4);
        }
        if (!string.IsNullOrEmpty(preset.ClothesColor4Hex))
        {
            var c = NPCPartRegistry.HexToColor(preset.ClothesColor4Hex, Color.white);
            AppearanceContainer.ApplyClothesColor(_npc, c, 5);
        }
    }
}
