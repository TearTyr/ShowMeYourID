using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // The lamp used ONLY while peeking into a room. Created lazily by ViewManager.EnsurePeekLight.
    //
    // How the dark works: a peek draws a solid black cover over the whole room, and that cover
    // has one soft round hole in it that follows the cursor. Whatever the room holds is simply
    // seen through the hole. There is no light sprite and nothing tinted, the room's own art is
    // what shows. PeekBlind plugs the hole while the lamp is off.
    //
    // The lamp is MANUAL: every peek starts dark, F turns it on and off. Active = a peek is on
    // screen; Lit = the lamp itself. Mouse movement never lights anything. E only ever selects
    // what the lit lamp is on; S or the bottom band steps back, so a dark room is never a trap.
    public class PeekLight : MonoBehaviour
    {
        // Size of the hole in world units.
        public float Radius = 2.6f;
        // Focus: the same idea as the handheld beam's Focus - a tighter, hotter circle for
        // reading details. Q toggles it; it never lights the lamp by itself and changing it
        // never moves the ON/OFF state (two independent flags, one source of truth each).
        public float FocusRadius = 1.3f;

        /// <summary>Is the lamp in its tight, concentrated mode right now?</summary>
        public bool Focused { get; private set; }

        /// <summary>The circle the lamp actually reveals with: the coverage query and the hole's
        /// size both answer to this.</summary>
        public float EffectiveRadius => Focused ? FocusRadius : Radius;

        /// <summary>A peek session is on screen (fade-in through fade-out).</summary>
        public bool Active { get; private set; }
        /// <summary>The lamp state: OFF on every entry, toggled by F alone.</summary>
        public bool Lit { get; private set; }

        private GameRoot _gameRoot;
        private Camera _cam;
        private RoomInstance _room;
        private GameObject _blind;            // plugs the hole while the lamp is off
        private Vector3 _worldPoint;
        private string _lastPrompt = "\0";   // "\0" = nothing published yet this peek
        private float _flickerLeft;           // > 0 while the lamp is dying (administrator hunt)

        /// <summary>The cursor's point on the room plane (what the lamp is centred on).</summary>
        public Vector3 CursorWorld => _worldPoint;

        /// <summary>True while a flicker has the lamp dead.</summary>
        public bool Flickering => _flickerLeft > 0f;

        // How far the black reaches past the hole, in hole diameters. The smallest hole is 2.6
        // units wide, so this covers the screen wherever the cursor goes.
        private const float CoverReach = 16f;

        /// <summary>Builds the cover once. Starts inactive and unlit.</summary>
        public void Init(GameRoot root, Camera cam)
        {
            _gameRoot = root;
            _cam = cam;

            var hole = RoomBuilder.SpriteGO(transform, "PeekHole", ProceduralArt.Get("peek_hole"),
                Vector3.zero, WorldLayers.PeekCover);
            hole.transform.localScale = Vector3.one;
            float side = 0.5f + CoverReach / 2f, tall = 1f + CoverReach * 2f;
            AddCover("CoverLeft", new Vector3(-side, 0f, 0f), new Vector2(CoverReach, tall));
            AddCover("CoverRight", new Vector3(side, 0f, 0f), new Vector2(CoverReach, tall));
            AddCover("CoverTop", new Vector3(0f, side, 0f), new Vector2(1f, CoverReach));
            AddCover("CoverBottom", new Vector3(0f, -side, 0f), new Vector2(1f, CoverReach));
            _blind = AddCover("PeekBlind", Vector3.zero, Vector2.one);

            Active = false;
            SetLit(false, true);
            ApplyFocusVisual();
            gameObject.SetActive(false);
        }

        private GameObject AddCover(string name, Vector3 pos, Vector2 size) =>
            RoomBuilder.Quad(transform, name, pos, WorldLayers.PeekCover, size, Color.black);

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
            if (_gameRoot != null) _gameRoot.Bus.Publish(GameEvent.Of(EventIds.InteractionPrompt, ""));
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

        // The whole cover is one scaled object: the hole's diameter is the root scale.
        private void ApplyFocusVisual()
        {
            transform.localScale = Vector3.one * EffectiveRadius * 2f;
        }

        // Open or plug the hole without touching the Lit state (used by the flicker).
        private void ApplyLampVisual(bool visible)
        {
            if (_blind != null) _blind.SetActive(!visible);
        }

        // The lamp's state. quiet = no toast (entry/exit resets).
        private void SetLit(bool on, bool quiet)
        {
            Lit = on;
            ApplyLampVisual(on);
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

            if (Input.GetKeyDown(KeyCode.E) && nearest != null) nearest.Interact();
            if (Input.GetKeyDown(KeyCode.S)) _gameRoot.ExitRoomPeek();
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

        // What the lit lamp is pointing at: the closest thing whose box holds the cursor. A dark
        // room has nothing to select, so E only ever works on what the player can see.
        private Interactable FindNearestUnderCursor(Vector3 cursor)
        {
            if (_room == null || !Lit || Flickering) return null;
            Interactable nearest = null;
            float bestSq = float.MaxValue;
            foreach (var it in _room.Interactables)
            {
                if (it == null || !it.Enabled) continue;
                float dx = cursor.x - it.AnchorX;
                float dy = cursor.y - it.AnchorY;
                if (Mathf.Abs(dx) > it.HitW / 2f || Mathf.Abs(dy) > it.HitH / 2f) continue;
                float sq = dx * dx + dy * dy;
                if (sq < bestSq) { bestSq = sq; nearest = it; }
            }
            return nearest;
        }
    }
}
