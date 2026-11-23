using DG.Tweening;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.Npc;
using PotionCraft.Npc.MonoBehaviourScripts;
using PotionCraft.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NPCDesigner;

/// <summary>
/// Drives Potion Craft's native in-game NPC idle head/eye movement (<see cref="NpcLookAtObject.LookAt"/>)
/// on the NPC Designer preview without inventing any custom/procedural animations and without activating
/// customer queues, walking, or potion trading.
/// </summary>
public class NPCIdleAnimator : MonoBehaviour
{
    private const float MouseIdleTimeoutSeconds = 2.0f;
    private const float MouseMoveThresholdSqr = 0.0009f; // ~0.03 world units

    private NpcMonoBehaviour? _npc;
    private NpcLookAtObject? _lookAt;

    private bool _isTrackingMouse;
    private bool _hasLastCursorPos;
    private Vector2 _lastCursorWorldPos;
    private float _mouseIdleTimer;
    private float _currentTimer;
    private float _targetTimer;

    public void Initialize(NpcMonoBehaviour npc)
    {
        _npc = npc;
        _lookAt = npc.GetComponent<NpcLookAtObject>();

        // Disable vanilla Update() on NpcLookAtObject (which only runs when CurrentRoomIndex == Meeting
        // and checks trade scales/grabbed items); we invoke its exact native LookAt() & GetLookAroundBoxBounds() directly.
        if (_lookAt != null)
        {
            _lookAt.enabled = false;
        }

        _isTrackingMouse = false;
        _hasLastCursorPos = false;
        _mouseIdleTimer = 0f;
        _currentTimer = 0f;
        _targetTimer = 0.4f;
    }

    /// <summary>
    /// Kills active head/pupil DOTween tweens and resets head rotation to neutral before ShiftTransforms runs.
    /// </summary>
    public void PrepareForHeadShift()
    {
        if (_npc?.references == null) return;

        if (_npc.references.head != null)
        {
            DOTween.Kill(_npc.references.head);
            _npc.references.head.localRotation = Quaternion.identity;
        }
        if (_npc.references.headBack != null)
        {
            DOTween.Kill(_npc.references.headBack);
            _npc.references.headBack.localRotation = Quaternion.identity;
        }
        if (_npc.references.renderers?.vibe?.renderer != null)
        {
            DOTween.Kill(_npc.references.renderers.vibe.renderer.transform);
            _npc.references.renderers.vibe.renderer.transform.localRotation = Quaternion.identity;
        }
        if (_npc.references.renderers?.leftPupil != null)
        {
            DOTween.Kill(_npc.references.renderers.leftPupil.transform);
        }
        if (_npc.references.renderers?.rightPupil != null)
        {
            DOTween.Kill(_npc.references.renderers.rightPupil.transform);
        }
    }

    /// <summary>
    /// Schedules an immediate native look-around update (used when the designer screen opens).
    /// </summary>
    public void TriggerLookAroundSoon()
    {
        _isTrackingMouse = false;
        _mouseIdleTimer = 0f;
        _currentTimer = 0f;
        _targetTimer = 0.25f;
    }

    private void OnEnable()
    {
        TriggerLookAroundSoon();
    }

    private void Update()
    {
        if (_npc == null || _lookAt == null || _npc.settings == null || _npc.references == null)
        {
            return;
        }

        var asset = Settings<NpcManagerSettings>.Asset;
        if (asset == null)
        {
            return;
        }

        // 1. Native cursor tracking (using exact NpcLookAtObject.LookAt + lookingAtObjectAnimationTime)
        if (TryGetCursorWorldPosition(out var cursorWorldPos))
        {
            if (!_hasLastCursorPos)
            {
                _hasLastCursorPos = true;
                _lastCursorWorldPos = cursorWorldPos;
            }
            else if ((cursorWorldPos - _lastCursorWorldPos).sqrMagnitude > MouseMoveThresholdSqr)
            {
                _lastCursorWorldPos = cursorWorldPos;
                _mouseIdleTimer = 0f;
                _isTrackingMouse = true;
                _lookAt.LookAt(cursorWorldPos, asset.lookingAtObjectAnimationTime);
                return;
            }
        }

        if (_isTrackingMouse)
        {
            _mouseIdleTimer += Time.deltaTime;
            if (_mouseIdleTimer < MouseIdleTimeoutSeconds)
            {
                return;
            }

            // Mouse became stationary: smoothly hand back to native idle LookAround
            _isTrackingMouse = false;
            _currentTimer = _targetTimer + 1f;
        }

        // 2. Native idle LookAround loop (exact mirror of NpcLookAtObject.Update look-around logic)
        _currentTimer += Time.deltaTime;
        if (_currentTimer > _targetTimer)
        {
            _currentTimer = 0f;
            _targetTimer = 0f;

            Vector2 targetPos = (Vector2)transform.position + 0.5f * Vector2.right;
            if (asset.lookAround)
            {
                _targetTimer = UnityEngine.Random.Range(asset.minTimeUpdateLookingTarget, asset.maxTimeUpdateLookingTarget);
                Vector2 basePos = transform.position;
                var (minPt, maxPt) = _lookAt.GetLookAroundBoxBounds();
                float x = basePos.x + UnityEngine.Random.Range(minPt.x, maxPt.x);
                float y = basePos.y + UnityEngine.Random.Range(minPt.y, maxPt.y);
                targetPos = new Vector2(x, y);
            }
            else
            {
                _targetTimer = 3f;
            }

            _lookAt.LookAt(targetPos, asset.lookingAroundAnimationTime);
        }
    }

    private static bool TryGetCursorWorldPosition(out Vector2 worldPos)
    {
        worldPos = Vector2.zero;

        var cam = Managers.Game?.Cam;
        var mouse = Mouse.current;
        if (cam == null || mouse == null)
        {
            return false;
        }

        Vector2 screenPos = mouse.position.ReadValue();
        if (screenPos.x < 0f || screenPos.y < 0f || screenPos.x > Screen.width || screenPos.y > Screen.height)
        {
            return false;
        }

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z)));
        worldPos = new Vector2(world.x, world.y);
        return true;
    }
}
