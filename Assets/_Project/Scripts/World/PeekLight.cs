using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // The cursor-locked circle of light used ONLY while peeking into a classroom. Created
    // lazily by ViewManager.EnsurePeekLight (one per session). The trick: the room's darkness
    // overlay is a SpriteRenderer that draws only OUTSIDE sprite masks, so the SpriteMask on
    // this object cuts a lit hole wherever the cursor is; the tinted Glow child is the soft
    // visible lamp on top of that hole. Interaction is cursor-distance based (no raycasts):
    // an interactable is "under the light" when its anchor point sits within CursorHitRadius
    // of the cursor's world position.
    //
    // The lamp is MANUAL: entering a classroom always starts DARK - the lamp
    // is OFF until the player presses F, and F presses it out again. Two states, one source
    // of truth: Active = a peek session is on screen (tracking + prompts + E keep running so
    // the player is never locked in the dark); Lit = the light itself (mask + glow + the
    // illumination query all answer to it). Mouse movement never lights anything.
    public class PeekLight : MonoBehaviour
    {
        // Light circle size in world units (the mask and the glow share it).
        public float Radius = 2.6f;
        // Focus: the same idea as the handheld beam's Focus - a tighter, hotter circle for
        // reading details. Q toggles it; it never lights the lamp by itself and changing it
        // never moves the ON/OFF state (two independent flags, one source of truth each).
        public float FocusRadius = 1.3f;

        /// <summary>Is the lamp in its tight, concentrated mode right now?</summary>
        public bool Focused { get; private set; }

        /// <summary>The circle the lamp actually reveals with - the illumination query and the
        /// mask/glow scale both answer to this, so focus changes REAL light, not a label.</summary>
        public float EffectiveRadius => Focused ? FocusRadius : Radius;
        // How close the cursor's world point must be to an interactable anchor to prompt it.
        public float CursorHitRadius = 1.3f;

        /// <summary>A peek session is on screen (fade-in through fade-out).</summary>
        public bool Active { get; private set; }
        /// <summary>The lamp state: OFF on every entry, toggled by F alone.</summary>
        public bool Lit { get; private set; }

        private GameRoot _gameRoot;
        private Camera _cam;
        private RoomInstance _room;
        private SpriteMask _mask;
        private SpriteRenderer _glow;
        private Vector3 _worldPoint;
        private string _lastPrompt = "\0";   // "\0" = nothing published yet this peek
        private float _flickerLeft;           // > 0 while the lamp is dying (administrator hunt)

        /// <summary>The cursor's point on the room plane (what the lamp is centred on).</summary>
        public Vector3 CursorWorld => _worldPoint;

        /// <summary>True while a flicker has the lamp dead.</summary>
        public bool Flickering => _flickerLeft > 0f;

        /// <summary>Builds the mask + glow children once. Starts inactive and unlit.</summary>
        public void Init(GameRoot root, Camera cam)
        {
            _gameRoot = root;
            _cam = cam;

            var maskGo = new GameObject("PeekMask");
            maskGo.transform.SetParent(transform, false);
            _mask = maskGo.AddComponent<SpriteMask>();
            _mask.sprite = ProceduralArt.Get("lamp_glow");
            _mask.isCustomRangeActive = true;
            _mask.frontSortingOrder = 60;   // the darkness overlay (70) is the only masked layer
            _mask.backSortingOrder = 80;
            transform.localScale = Vector3.one * Radius * 2f;

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(transform, false);
            _glow = glowGo.AddComponent<SpriteRenderer>();
            _glow.sprite = ProceduralArt.Get("lamp_glow");
            _glow.color = new Color(1f, 0.96f, 0.85f, 0.30f);
            _glow.sortingOrder = 72;        // just above the darkness hole's edge

            Active = false;
            SetLit(false, true);
            ApplyFocusVisual();
            gameObject.SetActive(false);
        }

        /// <summary>Begin a peek at the given room: the session goes live, the lamp stays
        /// OFF - arriving in a classroom always means arriving dark, and only a deliberate
        /// F lights it (one press; nothing was secretly pre-lit).</summary>
        public void Enter(RoomInstance room, bool lampOn = false)
        {
            _room = room;
            _lastPrompt = "\0";
            _flickerLeft = 0f;
            Active = true;
            gameObject.SetActive(true);
            SetLit(lampOn, true);   // the hunt starts the lamp ON; every other peek starts dark
            transform.position = Vector3.zero;
            _worldPoint = Vector3.zero;
            if (_gameRoot != null && !lampOn)
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    "FLASHLIGHT OFF - press F to turn it on."));
        }

        /// <summary>The lamp dies for a moment (the administrator's stun). It comes back by
        /// itself in the state the player left it. Photosensitive players get a plain dim,
        /// never a strobe.</summary>
        public void Flicker(float seconds)
        {
            if (!Active || seconds <= 0f) return;
            _flickerLeft = seconds;
            ApplyLampVisual(false);
        }

        /// <summary>Stop revealing. The hallway presentation is restored by ViewManager.</summary>
        public void Exit()
        {
            Active = false;
            SetLit(false, true);
            _room = null;
            _lastPrompt = "\0";
            gameObject.SetActive(false);
        }

        /// <summary>True when the world point sits inside the LIT light circle - an OFF
        /// lamp illuminates nothing, exactly like the handheld beam.</summary>
        public bool Covers(Vector3 worldPoint) =>
            Active && Lit && _flickerLeft <= 0f && Vector2.Distance(worldPoint, _worldPoint) <= EffectiveRadius;

        /// <summary>Q: wide wash <-> tight focus. Silent about ON/OFF - it only reshapes the
        /// circle, and only a lit lamp shows the difference (an OFF lamp stays OFF).</summary>
        public void ToggleFocus() => SetFocused(!Focused, false);

        private void SetFocused(bool on, bool quiet)
        {
            if (Focused == on) return;
            Focused = on;
            ApplyFocusVisual();
            if (_gameRoot == null) return;
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.LightModeChanged, Lit));  // HUD sync
            if (!quiet)
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    on ? "FLASHLIGHT: FOCUS - a tight circle for details." : "FLASHLIGHT: WIDE - the whole sweep."));
        }

        // Mask and glow are children, so one root scale reshapes the whole lamp; focus also
        // burns a little hotter (the beam's Focus tint does the same on patrol).
        private void ApplyFocusVisual()
        {
            transform.localScale = Vector3.one * EffectiveRadius * 2f;
            if (_glow != null)
            {
                var c = _glow.color;
                c.a = Focused ? 0.45f : 0.30f;
                _glow.color = c;
            }
        }

        // Show or hide the mask + glow without touching the Lit state (used by the flicker).
        private void ApplyLampVisual(bool visible)
        {
            if (_mask != null) _mask.gameObject.SetActive(visible);
            if (_glow != null) _glow.gameObject.SetActive(visible);
        }

        // The lamp's visual + illumination state. Mask and glow live and die together with
        // the Covers query, so the spotlight, the beam-like glow and the actual
        // illumination can never disagree. quiet = no toast (entry/exit resets).
        private void SetLit(bool on, bool quiet)
        {
            Lit = on;
            if (_mask != null) _mask.gameObject.SetActive(on);
            if (_glow != null) _glow.gameObject.SetActive(on);
            if (_gameRoot == null) return;
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.LightModeChanged, on));   // HUD sync
            if (!quiet)
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    on ? "FLASHLIGHT: ON" : "FLASHLIGHT: OFF"));
        }

        /// <summary>F is the ONLY lamp switch - the same deliberate toggle the handheld
        /// beam uses. One press on, one press off; no latch can survive an entry reset.</summary>
        internal void ToggleLamp() => SetLit(!Lit, false);

        // Esc follows the hallway rule (pause first; peek only exits through the bottom band
        // or the exit doorway), and pause/dialogue/overlays freeze the cursor entirely.
        private void Update()
        {
            if (_gameRoot == null || !Active) return;
            if (Input.GetKeyDown(KeyCode.Escape) && InputGate.TryEscape())
            {
                TickEscape();
                return;
            }
            if (_gameRoot.Paused || _gameRoot.BlockingOverlayOpen) return;
            if (_gameRoot.Dialogue != null && _gameRoot.Dialogue.IsActive) return;
            Tick(Time.deltaTime);
        }

        // Split out so the headless harness can drive the same arbitration deterministically
        // (stub MonoBehaviours never get Update calls) - the TickHotkeys trick.
        internal void TickEscape()
        {
            if (_gameRoot.BlockingOverlayOpen) return;      // Esc belongs to the open sheet
            if (_gameRoot.Paused) _gameRoot.TogglePause(false);
            else if (_gameRoot.Dialogue == null || !_gameRoot.Dialogue.IsActive) _gameRoot.TogglePause(true);
        }

        // One frame of peek light: the F toggle, then follow the cursor on the z=0 plane,
        // publish the prompt for whatever sits under the circle, and act on E. Tracking and
        // prompts run while the lamp is OFF too - the exit stays reachable in the dark (the
        // bottom band and the doorway) - but with mask + glow hidden nothing is visible and
        // Covers() reports no illumination. internal so the headless harness can step
        // frames deterministically (stub MonoBehaviours never get Update calls).
        internal void Tick(float dt)
        {
            if (_gameRoot == null || _cam == null || !Active) return;

            // manual lamp: F toggles, gated exactly like the rest of the peek input
            if (!_gameRoot.Paused && !_gameRoot.BlockingOverlayOpen &&
                (_gameRoot.Dialogue == null || !_gameRoot.Dialogue.IsActive))
            {
                if (Input.GetKeyDown(KeyCode.F)) ToggleLamp();
                else if (Input.GetKeyDown(KeyCode.Q)) ToggleFocus();   // reshapes, never lights
            }

            if (_flickerLeft > 0f) TickFlicker(dt);

            var mouse = Input.mousePosition;
            mouse.z = -_cam.transform.position.z;           // project onto the z=0 world plane
            var wp = _cam.ScreenToWorldPoint(mouse);
            wp.z = 0f;
            transform.position = wp;
            _worldPoint = wp;

            var nearest = FindNearestUnderCursor(wp);
            string prompt = nearest != null ? nearest.Prompt : "";
            if (prompt != _lastPrompt)
            {
                _lastPrompt = prompt;
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.InteractionPrompt, prompt));
            }

            if (Input.GetKeyDown(KeyCode.E) && nearest != null)
            {
                var door = nearest as Doorway;
                if (door != null && door.Target == RoomId.Hallway) _gameRoot.ExitRoomPeek();
                else nearest.Interact();
            }
        }

        // Count the flicker down. Strobing is skipped for photosensitive players (plain dark).
        private void TickFlicker(float dt)
        {
            _flickerLeft -= dt;
            if (_flickerLeft <= 0f) { _flickerLeft = 0f; ApplyLampVisual(Lit); return; }
            bool safe = _gameRoot.Settings != null && (_gameRoot.Settings.PhotosensitivitySafe || _gameRoot.Settings.ReduceMotion);
            if (safe) return;
            ApplyLampVisual(Lit && ((int)(_flickerLeft / 0.08f) % 2 == 0) && _flickerLeft < 0.45f);
        }

        // Nearest interactable whose anchor point (chest height above the shared floor line)
        // is within CursorHitRadius of the cursor. Squared distances: no sqrt, no allocations.
        private Interactable FindNearestUnderCursor(Vector3 cursor)
        {
            if (_room == null) return null;
            Interactable nearest = null;
            float bestSq = CursorHitRadius * CursorHitRadius;
            foreach (var it in _room.Interactables)
            {
                if (it == null || !it.Enabled) continue;
                float dx = cursor.x - it.AnchorX;
                float dy = cursor.y - it.AnchorY;
                float sq = dx * dx + dy * dy;
                if (sq <= bestSq) { bestSq = sq; nearest = it; }
            }
            return nearest;
        }
    }
}
