using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    /// <summary>Cursor-guided flashlight: a real limited-visibility mask (SpriteMask punching a
    /// hole in the room darkness), a battery with per-mode drain/recovery, and the single
    /// definition of "does the beam cover this world point". Screen→world conversion happens
    /// every frame through the live camera, so the beam stays glued to the cursor through camera
    /// follow, room changes and any resolution/aspect (world-space, no shader needed).</summary>
    public class FlashlightRig : MonoBehaviour
    {
        public LightMode Beam { get; private set; } = LightMode.Off;
        public float Battery { get; private set; } = 1f;
        public bool BatteryDead { get; private set; }
        public bool BeamOn => Beam != LightMode.Off && !BatteryDead;

        private GameRoot R;
        private Camera _cam;
        private SpriteRenderer _cone;
        private SpriteMask _mask;
        private bool _latchSweep, _latchFocus;
        private Vector3 _aimWorld;
        private float _pubThrottle;

        public void Init(GameRoot root, Camera cam, SpriteRenderer cone)
        {
            R = root; _cam = cam; _cone = cone;
            var maskGo = new GameObject("FlashlightMask");
            maskGo.transform.SetParent(transform, false);
            _mask = maskGo.AddComponent<SpriteMask>();
            _mask.sprite = cone != null ? cone.sprite : null;
            _mask.isCustomRangeActive = true;
            _mask.frontSortingOrder = 60;
            _mask.backSortingOrder = 70;
            ApplyBeam(LightMode.Off, true);
        }

        /// <summary>Single source of truth for beam coverage (Focus reveals, Blink freezes).</summary>
        public bool ConeCovers(Vector3 worldPoint)
        {
            if (!BeamOn || _cone == null) return false;
            float dx = worldPoint.x - _cone.transform.localPosition.x;
            float dy = worldPoint.y - _cone.transform.localPosition.y;
            if (Beam == LightMode.Focus) return Mathf.Abs(dx) < 7.0f && Mathf.Sign(dx) == _side && Mathf.Abs(dy) < 1.7f;
            return Mathf.Abs(dx) < 6.2f && Mathf.Sign(dx) == _side && Mathf.Abs(dy) < 3.4f;
        }
        private float _side = 1f;

        private void Update()
        {
            if (R == null || R.Paused || _cam == null) return;
            ReadInput();
            AimBeam();
            TickBattery(Time.deltaTime);
        }

        // F / LMB = Sweep, Q or RMB = Focus, L = Off (E stays interaction; no conflicts).
        private void ReadInput()
        {
            if (Input.GetKeyDown(KeyCode.F)) _latchSweep = !_latchSweep && !BatteryDead;
            if (Input.GetKeyDown(KeyCode.Q)) _latchFocus = !_latchFocus && !BatteryDead;
            if (Input.GetKeyDown(KeyCode.L)) { _latchSweep = false; _latchFocus = false; }
            bool holdSweep = Input.GetMouseButton(0);
            bool holdFocus = Input.GetMouseButton(1);
            var want = holdFocus ? LightMode.Focus : holdSweep ? LightMode.Sweep
                       : _latchFocus ? LightMode.Focus : _latchSweep ? LightMode.Sweep : LightMode.Off;
            if (BatteryDead && want != LightMode.Off) want = LightMode.Off;
            if (want != Beam) ApplyBeam(want, false);
        }

        private void ApplyBeam(LightMode m, bool force)
        {
            Beam = m;
            bool on = m != LightMode.Off && !BatteryDead;
            if (_mask != null) _mask.gameObject.SetActive(on);
            if (_cone != null) _cone.gameObject.SetActive(on);
            // keep the room rig (ambient + events) as the broadcaster for mode changes
            var room = R.Views != null && R.Views.Current != null ? R.Views.Current.Light : null;
            if (room != null && !force)
            {
                if (m == LightMode.Focus) room.SetMode(LightMode.Focus);
                else if (m == LightMode.Sweep && room.Mode == LightMode.Off) room.SetMode(LightMode.Sweep);
            }
            PublishBattery();
        }

        private void AimBeam()
        {
            if (_cone == null) return;
            var player = R.Views != null && R.Views.Player != null ? R.Views.Player.transform.localPosition : Vector3.zero;
            _aimWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
            _aimWorld.z = 0f;
            _side = _aimWorld.x >= player.x ? 1f : -1f;
            float dy = Mathf.Clamp(_aimWorld.y - player.y, -2.6f, 2.6f);
            var pos = new Vector3(player.x + _side * 0.6f, player.y + dy * 0.75f, -0.5f);
            _cone.transform.localPosition = pos;
            if (_mask != null) _mask.transform.localPosition = pos;
            float w = Beam == LightMode.Focus ? 0.62f : 1.15f;
            float h = Beam == LightMode.Focus ? 0.55f : 0.95f;
            _cone.transform.localScale = new Vector3(_side * w, h, 1f);
            if (_mask != null) _mask.transform.localScale = new Vector3(w * 1.25f, h * 1.25f, 1f);
        }

        private void TickBattery(float dt)
        {
            var cfg = R.Config;
            float before = Battery;
            if (BeamOn)
                Battery -= dt * (Beam == LightMode.Focus ? cfg.batteryDrainFocusPerSecond : cfg.batteryDrainSweepPerSecond) / 100f;
            else
            {
                bool litRoom = R.Views != null && R.Views.Current != null && R.Views.Current.Light != null
                               && R.Views.Current.Light.Mode == LightMode.Sweep;
                Battery += dt * (litRoom ? cfg.batteryRecoverLitPerSecond : cfg.batteryRecoverOffPerSecond) / 100f;
            }
            Battery = Mathf.Clamp01(Battery);
            if (Battery <= 0f && !BatteryDead)
            {
                BatteryDead = true;
                ApplyBeam(LightMode.Off, false);
                R.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*the flashlight dies — let it breathe*", 2.4f, null));
                R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_scan_error", 0.5f, null));
            }
            else if (BatteryDead && Battery >= cfg.batteryRebootFraction)
            {
                BatteryDead = false;
                R.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*the flashlight coughs back to life*", 2f, null));
            }
            _pubThrottle -= dt;
            if (Mathf.Abs(Battery - before) > 1e-4f && _pubThrottle <= 0f) { _pubThrottle = 0.1f; PublishBattery(); }
        }

        private void PublishBattery() => R.Bus.Publish(GameEvent.Of(EventIds.BatteryChanged, Battery));
    }
}
