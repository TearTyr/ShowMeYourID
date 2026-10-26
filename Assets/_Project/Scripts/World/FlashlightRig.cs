using Hollowbrook.Bootstrap;
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
        private SpriteRenderer _penumbra;   // graded darkness inside the mask hole (see below)
        private BatteryState _battery = new BatteryState { Charge = 1f, Dead = false };
        private FlashlightLatches _latches;
        private Vector3 _origin;
        private Vector3 _dir = Vector3.right;
        private BeamParams _params;          // logical geometry (gameplay queries use this)
        private Vector3 _aimSmooth = Vector3.right;   // displayed aim (damped, never snaps)
        private float _dispRange, _dispHalfTan;       // displayed geometry (damped across modes)
        private float _coneWorldW = 8f, _coneWorldH = 4.5f;   // light_cone: 512x288 px @ 64 ppu
        // The mask hole is a stencil: binary. The penumbra sheet rides over it at the hole's own
        // size and puts graded darkness BACK — full reveal in the core, soft side falloff at the
        // wedge edge, a distance fade at the reach, ambient alpha at the rim (no seam). That is
        // what makes lockers/walls/doors brighten by distance and angle instead of popping out of
        // a flat cut-out. It draws at 72, outside every mask range, so nothing punches it.
        private const float PenumbraOverScale = 1.35f;
        private const int PenumbraOrder = 72;
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
            var penGo = new GameObject("BeamPenumbra");
            penGo.transform.SetParent(transform, false);
            _penumbra = penGo.AddComponent<SpriteRenderer>();
            _penumbra.sprite = ProceduralArt.Get("beam_penumbra");
            _penumbra.sortingOrder = PenumbraOrder;
            _penumbra.color = Color.black;
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
            if (_penumbra != null) _penumbra.gameObject.SetActive(on);
            // Focus concentrates: a tighter cone AND a hotter core tint; Sweep stays the wide,
            // softer wash. Both still reveal the environment through mask hole + penumbra.
            if (_cone != null)
                _cone.color = new Color(1f, 0.96f, 0.85f, m == LightMode.Focus ? FocusTint : SweepTint);
            ScaleBeam();

            if (!quiet)
            {
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    on ? "BEAM: " + (m == LightMode.Focus ? "FOCUS" : "SWEEP") : "BEAM: OFF"));
            }
            PublishBattery();
        }

        // Cursor aiming in world space: convert the mouse on the camera's z-plane, aim the cone
        // from the player's shoulder along (aim - origin), rotate sprite, mask AND penumbra
        // together. The DISPLAYED aim and geometry are damped towards their targets so the beam
        // glides instead of flickering/stretching with raw mouse jitter, while the logical cone
        // test (_params/_dir) stays exact for gameplay.
        private void AimBeam(float dt)
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

            // damped aim: fast enough to feel instant, slow enough to read as a held lamp
            float k = 1f - (float)System.Math.Exp(-14d * Mathf.Max(dt, 0f));
            _aimSmooth = Vector3.Lerp(_aimSmooth, _dir, k);
            if (_aimSmooth.magnitude > 1e-4f) _aimSmooth = _aimSmooth.normalized;
            else _aimSmooth = _dir;
            float angle = Mathf.Atan2(_aimSmooth.y, _aimSmooth.x) * Mathf.Rad2Deg;
            var mid = _origin + _aimSmooth * (_dispRange * 0.5f);
            _cone.transform.localPosition = mid;
            _cone.transform.localEulerAngles = new Vector3(0f, 0f, angle);
            if (_mask != null)
            {
                _mask.transform.localPosition = mid;
                _mask.transform.localEulerAngles = new Vector3(0f, 0f, angle);
            }
            if (_penumbra != null)
            {
                _penumbra.transform.localPosition = mid;
                _penumbra.transform.localEulerAngles = new Vector3(0f, 0f, angle);
            }
            ScaleBeam(dt);
        }

        private const float SweepTint = 0.30f;
        private const float FocusTint = 0.42f;

        // Non-uniform scale maps the fixed cone sprite onto the mode's logical geometry:
        // length = Range, far-end half width = Range * tan(halfAngle). The mask AND the penumbra
        // ride 35% larger than the visible cone so the graded falloff has room to land back on
        // the room's ambient alpha before the stencil hole ends (no rim, no flat cut-out).
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
            if (_mask != null)
                _mask.transform.localScale = new Vector3(sx * PenumbraOverScale, sy * PenumbraOverScale, 1f);
            if (_penumbra != null)
            {
                _penumbra.transform.localScale = new Vector3(sx * PenumbraOverScale, sy * PenumbraOverScale, 1f);
                // the sheet's darkness is the room's CURRENT ambient: a lit corridor penumbras
                // gently, a dark one fades all the way back to its own black at the rim
                var views = _gameRoot != null ? _gameRoot.Views : null;
                float ambient = views != null ? views.AmbientAlpha : 0.62f;
                var pc = _penumbra.color; pc.a = ambient; _penumbra.color = pc;
            }
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
