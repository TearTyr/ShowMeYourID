using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // The handheld beam: modes (Off/Sweep/Focus), cursor aiming, cone coverage, battery.
    // Created by ViewManager. It never writes the room's LightController — ambient light and
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
        private SpriteMask _mask;
        private BatteryState _battery = new BatteryState { Charge = 1f, Dead = false };
        private FlashlightLatches _latches;
        private Vector3 _origin;
        private Vector3 _dir = Vector3.right;
        private BeamParams _params;
        private float _coneWorldW = 8f, _coneWorldH = 4.5f;   // light_cone: 512x288 px @ 64 ppu
        private float _pubThrottle;

        public void Init(GameRoot root, Camera cam, SpriteRenderer cone)
        {
            _gameRoot = root; _cam = cam; _cone = cone;
            if (cone != null && cone.sprite != null && cone.sprite.pixelsPerUnit > 0f)
            {
                _coneWorldW = cone.sprite.rect.width / cone.sprite.pixelsPerUnit;
                _coneWorldH = cone.sprite.rect.height / cone.sprite.pixelsPerUnit;
            }
            var maskGo = new GameObject("FlashlightMask");
            maskGo.transform.SetParent(transform, false);
            _mask = maskGo.AddComponent<SpriteMask>();
            _mask.sprite = cone != null ? cone.sprite : null;
            _mask.isCustomRangeActive = true;
            _mask.frontSortingOrder = 60;
            _mask.backSortingOrder = 70;
            ApplyBeam(LightMode.Off, true);
        }

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

        /// <summary>Room-entry reset: arriving in a room ALWAYS means arriving dark — the
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
            // never re-lights it by itself — revival needs a deliberate F/Q (acceptance 7)
            if (_gameRoot.State == AppViewState.RoomPeek)
            {
                if (Beam != LightMode.Off) ApplyBeam(LightMode.Off, true);
                _latches = default;
                return;
            }
            ReadInput();
            AimBeam();
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
        // out. The mouse buttons are deliberately NOT beam input — a held click (doors,
        // switches, dialogue) used to light the beam without any F press, which read as the
        // flashlight turning itself on (audit task 3). FlashlightInput keeps its momentary
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
            Beam = m;
            var cfg = _gameRoot != null ? _gameRoot.Config : null;
            _params = BeamGeometry.ParamsFor(m,
                cfg != null ? cfg.sweepRange : BeamGeometry.DefaultSweepRange,
                cfg != null ? cfg.sweepHalfAngleDeg : BeamGeometry.DefaultSweepHalfAngleDeg,
                cfg != null ? cfg.focusRange : BeamGeometry.DefaultFocusRange,
                cfg != null ? cfg.focusHalfAngleDeg : BeamGeometry.DefaultFocusHalfAngleDeg);

            bool on = m != LightMode.Off && !BatteryDead;
            if (_mask != null) _mask.gameObject.SetActive(on);
            if (_cone != null) _cone.gameObject.SetActive(on);
            ScaleBeam();

            if (!quiet)
            {
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    on ? "BEAM: " + (m == LightMode.Focus ? "FOCUS" : "SWEEP") : "BEAM: OFF"));
            }
            PublishBattery();
        }

        // Cursor aiming in world space: convert the mouse on the camera's z-plane, aim the cone
        // from the player's shoulder along (aim - origin), rotate sprite AND mask together.
        private void AimBeam()
        {
            if (_cone == null || _cam == null) return;
            var views = _gameRoot.Views;
            var player = views != null && views.Player != null
                ? views.Player.transform.localPosition : Vector3.zero;

            var mouse = Input.mousePosition;
            mouse.z = -_cam.transform.position.z;          // project onto the z=0 world plane
            var aim = _cam.ScreenToWorldPoint(mouse);
            aim.z = 0f;

            _origin = new Vector3(player.x, player.y + 0.2f, 0f);
            var d = aim - _origin; d.z = 0f;
            if (d.magnitude > 1e-4f) _dir = d.normalized;  // keep last aim on a degenerate delta

            float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
            var mid = _origin + _dir * (_params.Range * 0.5f);
            _cone.transform.localPosition = mid;
            _cone.transform.localEulerAngles = new Vector3(0f, 0f, angle);
            if (_mask != null)
            {
                _mask.transform.localPosition = mid;
                _mask.transform.localEulerAngles = new Vector3(0f, 0f, angle);
            }
            ScaleBeam();
        }

        // Non-uniform scale maps the fixed cone sprite onto the mode's logical geometry:
        // length = Range, far-end half width = Range * tan(halfAngle). The mask rides 6% larger
        // so the darkness hole always contains the visible beam.
        private void ScaleBeam()
        {
            if (_cone == null) return;
            float sx = _params.Range <= 0f ? 0.001f : _params.Range / _coneWorldW;
            float sy = _params.Range <= 0f ? 0.001f : 2f * _params.Range * _params.HalfTan / _coneWorldH;
            _cone.transform.localScale = new Vector3(sx, sy, 1f);
            if (_mask != null) _mask.transform.localScale = new Vector3(sx * 1.06f, sy * 1.06f, 1f);
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
            // the REAL ambient state of the room — never a beam mode stored on the light
            bool roomLit = _gameRoot.Views != null && _gameRoot.Views.RoomLit;
            float before = _battery.Charge;
            _battery = BatteryModel.Tick(_battery, Beam, roomLit, dt, p, out bool justDied, out bool justRebooted);

            if (justDied)
            {
                _latches = default;                        // deliberate input required to relight
                ApplyBeam(LightMode.Off, false);
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*the flashlight dies — let it breathe*", 2.4f, null));
                _gameRoot.Audio.PlaySfx("sfx_scan_error", 0.5f);
            }
            else if (justRebooted)
            {
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*the flashlight coughs back to life — pick a beam*", 2f, null));
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
