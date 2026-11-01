using Hollowbrook.Scenes;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // The handheld beam: modes (Off/Sweep/Focus), cursor aiming, cone coverage, battery.
    // Created by ViewManager. It never writes the room's LightController - ambient light and
    // handheld light are separate systems; ViewManager answers combined illumination queries.
    //
    // Coordinate space: every query and placement uses the shared world/room plane (the world
    // root is identity, so worldRoot-local == world). Detection is the pure cone test in
    // BeamGeometry; the rendered cone sprite is a visual echo of the same numbers.
    public class FlashlightRig : MonoBehaviour
    {
        public LightMode Beam { get; private set; } = LightMode.Off;
        public float Battery => _battery.Charge;
        public bool BatteryDead => _battery.Dead;
        public bool BeamOn => Beam != LightMode.Off && !BatteryDead;

        public Vector3 Origin => _origin;      // beam origin on the shared plane
        public Vector3 Dir => _dir;            // unit aim direction (z flattened)
        public BeamParams Params => _params;   // range/half-angle of the active mode
        internal FlashlightLatches Latches => _latches;

        private GameRoot _gameRoot;
        private Camera _cam;
        private SpriteRenderer _cone;
        private BeamCover _cover;
        private LightController _coveredLight;   // the room whose own dark is switched off while the beam is on
        private BatteryState _battery = new BatteryState { Charge = 1f, Dead = false };
        private FlashlightLatches _latches;
        private Vector3 _origin;
        private Vector3 _dir = Vector3.right;
        private BeamParams _params;          // logical geometry (gameplay queries use this)
        private Vector3 _aimSmooth = Vector3.right;   // displayed aim (damped, never snaps)
        private float _dispRange, _dispHalfTan;       // displayed geometry (damped across modes)
        private float _coneWorldW = 8f, _coneWorldH = 4.5f;   // light_cone: 512x288 px @ 64 ppu
        // The beam clears the dark: while it is on, BeamCover paints the room's dark with the beam
        // cut out of it (nothing is painted on top, the room's own art shows through, like the
        // peek lamp). The cone object stays only as the beam's placement and size.
        private float _pubThrottle;

        public void Init(GameRoot root, Camera cam, SpriteRenderer cone)
        {
            _gameRoot = root; _cam = cam; _cone = cone;
            // Nothing is painted over the lit area: the beam only clears the darkness (BeamCover), so
            // what it lights is the room's own art, clear, like the peek lamp.
            if (cone != null) cone.enabled = false;
            if (cone != null && cone.sprite != null && cone.sprite.pixelsPerUnit > 0f)
            {
                _coneWorldW = cone.sprite.rect.width / cone.sprite.pixelsPerUnit;
                _coneWorldH = cone.sprite.rect.height / cone.sprite.pixelsPerUnit;
            }
            var coverGo = new GameObject("BeamCover");
            coverGo.transform.SetParent(transform, false);
            coverGo.AddComponent<SpriteRenderer>();
            _cover = coverGo.AddComponent<BeamCover>();
            _cover.Init();
            ApplyBeam(LightMode.Off, true);
        }

        /// <summary>The dark that is clear inside the beam (headless checks read it).</summary>
        internal BeamCover Cover => _cover;

        /// <summary>Is a world point inside the beam's actual cone? Sweep and Focus use their
        /// own range/angle; Off and dead battery cover nothing.</summary>
        public bool ConeCovers(Vector3 worldPoint)
        {
            if (!BeamOn) return false;
            float radius = _gameRoot != null && _gameRoot.Config != null
                ? _gameRoot.Config.beamTargetRadius : BeamGeometry.DefaultTargetRadius;
            return BeamGeometry.Covers(_origin.x, _origin.y, _dir.x, _dir.y, _params,
                worldPoint.x, worldPoint.y, radius);
        }

        /// <summary>Full battery, not dead. The administrator hunt tops it up so a flat battery
        /// can never leave the player without a way to stun him.</summary>
        public void Recharge() { _battery = new BatteryState { Charge = 1f, Dead = false }; }

        /// <summary>Room-entry reset: arriving in a room ALWAYS means arriving dark - the
        /// beam goes off, latches clear, silently (no toast spam on every door); revival
        /// needs a deliberate F/Q. Called by ViewManager on every entry, patrol starts
        /// and peek returns included, so a transition can never carry a lit beam over
        /// (audit: the beam stayed visible after entering rooms without pressing F).</summary>
        public void ForceBeamOffOnEntry()
        {
            _latches = default;
            if (Beam != LightMode.Off) ApplyBeam(LightMode.Off, true);
        }

        private void Update()
        {
            if (_gameRoot == null || _cam == null || _gameRoot.Paused) return;
            Tick(Time.deltaTime);
        }

        // One frame of beam logic. internal so the headless FlowCheck can step frames
        // deterministically (stub MonoBehaviours never get Update calls).
        internal void Tick(float dt)
        {
            // peek rests the corridor beam: force Off AND clear the latches, so stepping back
            // never re-lights it by itself - revival needs a deliberate F/Q (acceptance 7)
            if (_gameRoot.State == AppViewState.RoomPeek)
            {
                if (Beam != LightMode.Off) ApplyBeam(LightMode.Off, true);
                _latches = default;
                return;
            }
            ReadInput();
            AimBeam(dt);
            TickBattery(dt);
        }

        // Input answers only while actually patrolling: paused, blocking dialogue, room
        // transitions and any non-exploration state freeze the controls (beam holds as-is).
        private bool InputLocked =>
            _gameRoot.Paused ||
            (_gameRoot.Dialogue != null && _gameRoot.Dialogue.IsActive) ||
            _gameRoot.State != AppViewState.Exploration ||
            (_gameRoot.Views != null && _gameRoot.Views.Transitioning);

        // Keyboard-only beam input: F sweeps (toggle on/off), Q focuses (toggle), L puts it
        // out. The mouse buttons are deliberately NOT beam input - a held click (doors,
        // switches, dialogue) used to light the beam without any F press, which read as the
        // flashlight turning itself on. FlashlightInput keeps its momentary
        // hold parameters for the pure-logic suites; the rig simply never feeds them.
        private void ReadInput()
        {
            if (InputLocked)
            {
                if (BatteryDead) _latches = default;   // never wake up with a stale latch
                return;
            }
            LightMode want = FlashlightInput.Resolve(_latches,
                Input.GetKeyDown(KeyCode.F), Input.GetKeyDown(KeyCode.Q), Input.GetKeyDown(KeyCode.L),
                false, false, BatteryDead, out _latches);
            if (want != Beam) ApplyBeam(want, false);
        }

        private void ApplyBeam(LightMode m, bool quiet)
        {
            bool wasOn = Beam != LightMode.Off && !BatteryDead;
            Beam = m;
            var cfg = _gameRoot != null ? _gameRoot.Config : null;
            _params = BeamGeometry.ParamsFor(m,
                cfg != null ? cfg.sweepRange : BeamGeometry.DefaultSweepRange,
                cfg != null ? cfg.sweepHalfAngleDeg : BeamGeometry.DefaultSweepHalfAngleDeg,
                cfg != null ? cfg.focusRange : BeamGeometry.DefaultFocusRange,
                cfg != null ? cfg.focusHalfAngleDeg : BeamGeometry.DefaultFocusHalfAngleDeg);

            bool on = m != LightMode.Off && !BatteryDead;
            // SWITCHING THE LAMP ON IS INSTANT. The damped aim and the damped mode morph exist
            // so a HELD lamp glides instead of jittering with the mouse - but on the press
            // itself there is nothing to glide from: the cone was invisible a moment ago, so
            // any smoothing there reads as lag (the owner's "the beam takes a moment to show
            // up"). So on an off->on transition the displayed aim and the displayed geometry
            // snap to their targets and the sprite is placed in the SAME call, which puts a
            // correctly-aimed, correctly-shaped, fully-lit beam on screen on the activation
            // frame - no timer, no coroutine, no fade, no waiting for the next AimBeam.
            if (on && !wasOn)
            {
                _aimSmooth = AimFromCursor(out _origin, out _dir);
                _dispRange = _params.Range;
                _dispHalfTan = _params.HalfTan;
            }

            if (_cover != null) _cover.gameObject.SetActive(on);
            SetCoveredLight(on ? CurrentLight() : null);
            if (_cone != null) _cone.gameObject.SetActive(on);
            // Focus is a tighter cone than Sweep, which stays the wide wash (see ParamsFor above).
            ScaleBeam();
            if (on) { PlaceBeam(); DrawCover(); }   // positioned and painted before the frame is drawn, never a frame later

            if (!quiet)
            {
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    on ? "FLASHLIGHT: " + (m == LightMode.Focus ? "FOCUS" : "SWEEP") : "FLASHLIGHT: OFF"));
            }
            PublishBattery();
        }

        /// <summary>The aim direction the cursor asks for right now: the beam origin on the
        /// player's shoulder and the unit vector toward the mouse on the world z=0 plane.
        /// Shared by the per-frame damping and the activation snap so the two can never
        /// disagree about where the lamp points.</summary>
        private Vector3 AimFromCursor(out Vector3 origin, out Vector3 dir)
        {
            var views = _gameRoot != null ? _gameRoot.Views : null;
            var player = views != null && views.Player != null
                ? views.Player.transform.localPosition : Vector3.zero;
            origin = new Vector3(player.x, player.y + 0.2f, 0f);
            dir = _dir;                                  // keep the last aim if it degenerates
            if (_cam != null)
            {
                var mouse = Input.mousePosition;
                mouse.z = -_cam.transform.position.z;    // project onto the z=0 world plane
                var aim = _cam.ScreenToWorldPoint(mouse);
                aim.z = 0f;
                var d = aim - origin; d.z = 0f;
                if (d.magnitude > 1e-4f) dir = d.normalized;
            }
            return dir;
        }

        // Cursor aiming in world space: the cone points from the player's shoulder at the mouse.
        // The DISPLAYED aim swings round by angle, so a held beam glides (and can turn right
        // round), while the logical cone test stays exact for gameplay. Switching the beam ON
        // skips the swing (see ApplyBeam): the lamp is lit and aimed on the press itself.
        private void AimBeam(float dt)
        {
            if (_cone == null || _cam == null) return;
            var want = AimFromCursor(out _origin, out _dir);
            float from = Mathf.Atan2(_aimSmooth.y, _aimSmooth.x) * Mathf.Rad2Deg;
            float to = Mathf.Atan2(want.y, want.x) * Mathf.Rad2Deg;
            float angle = (from + Mathf.DeltaAngle(from, to) * AimDamping(dt)) * Mathf.Deg2Rad;
            _aimSmooth = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            PlaceBeam();
            ScaleBeam(dt);
            if (BeamOn) DrawCover();
        }

        private LightController CurrentLight()
        {
            var views = _gameRoot != null ? _gameRoot.Views : null;
            var room = views != null ? views.Current : null;
            return room != null ? room.Light : null;
        }

        // Only the room the beam is lit in has its own dark switched off; it comes back when the beam goes out.
        private void SetCoveredLight(LightController light)
        {
            if (_coveredLight == light) return;
            if (_coveredLight != null) _coveredLight.SetBeamCover(false);
            _coveredLight = light;
            if (light != null) light.SetBeamCover(true);
        }

        private void DrawCover()
        {
            if (_cover == null || _coveredLight == null) return;
            _cover.Draw(_cam, _coveredLight, _origin, _aimSmooth, _dispRange, _dispHalfTan);
        }

        // damped aim: fast enough to feel instant, slow enough to read as a held lamp
        private static float AimDamping(float dt) => 1f - (float)System.Math.Exp(-14d * Mathf.Max(dt, 0f));

        /// <summary>Put the cone object on the current displayed aim (one mid-point, one rotation).</summary>
        private void PlaceBeam()
        {
            if (_cone == null) return;
            float angle = Mathf.Atan2(_aimSmooth.y, _aimSmooth.x) * Mathf.Rad2Deg;
            var mid = _origin + _aimSmooth * (_dispRange * 0.5f);
            _cone.transform.localPosition = mid;
            _cone.transform.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        // Non-uniform scale maps the fixed cone sprite onto the mode's logical geometry:
        // length = Range, far-end half width = Range * tan(halfAngle).
        private void ScaleBeam(float dt = 0f)
        {
            if (_cone == null) return;
            // damped geometry: switching Sweep<->Focus morphs the beam instead of popping
            float kg = 1f - (float)System.Math.Exp(-12d * Mathf.Max(dt, 0f));
            _dispRange = Mathf.Lerp(_dispRange <= 0f ? _params.Range : _dispRange, _params.Range, kg);
            _dispHalfTan = Mathf.Lerp(_dispHalfTan <= 0f ? _params.HalfTan : _dispHalfTan, _params.HalfTan, kg);
            float sx = _dispRange <= 0f ? 0.001f : _dispRange / _coneWorldW;
            float sy = _dispRange <= 0f ? 0.001f : 2f * _dispRange * _dispHalfTan / _coneWorldH;
            _cone.transform.localScale = new Vector3(sx, sy, 1f);
        }

        private void TickBattery(float dt)
        {
            var cfg = _gameRoot.Config;
            if (cfg == null) return;
            var p = new BatteryParams
            {
                DrainSweepPerSecond = cfg.batteryDrainSweepPerSecond,
                DrainFocusPerSecond = cfg.batteryDrainFocusPerSecond,
                RecoverDarkPerSecond = cfg.batteryRecoverOffPerSecond,
                RecoverLitPerSecond = cfg.batteryRecoverLitPerSecond,
                RebootFraction = cfg.batteryRebootFraction,
            };
            // the REAL ambient state of the room - never a beam mode stored on the light
            bool roomLit = _gameRoot.Views != null && _gameRoot.Views.RoomLit;
            float before = _battery.Charge;
            _battery = BatteryModel.Tick(_battery, Beam, roomLit, dt, p, out bool justDied, out bool justRebooted);

            if (justDied)
            {
                _latches = default;                        // deliberate input required to relight
                ApplyBeam(LightMode.Off, false);
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*the flashlight dies - let it breathe*", 2.4f, null));
                _gameRoot.Audio.PlaySfx("sfx_scan_error", 0.5f);
            }
            else if (justRebooted)
            {
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*the flashlight coughs back to life - pick a beam*", 2f, null));
            }

            _pubThrottle -= dt;
            if (Mathf.Abs(_battery.Charge - before) > 1e-4f && _pubThrottle <= 0f)
            {
                _pubThrottle = 0.1f;
                PublishBattery();
            }
        }

        private void PublishBattery() => _gameRoot.Bus.Publish(GameEvent.Of(EventIds.BatteryChanged, _battery.Charge));
    }
}
