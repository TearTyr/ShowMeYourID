using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // Runs the administrator hunt in the school scene. Created by ViewManager.StartAdminHunt.
    //
    // The RULES live in GameLogic/AdminHunt (pure, fuzz-tested). This class only connects them
    // to the world: it reads what the player is doing (peeking? cursor on his head? beam on his
    // face?), feeds that to the rules every frame, and turns the events that come back into
    // sound, captions, the shaking picture of his face and the lamp flicker.
    //
    // Where things happen:
    //  - Hiding:   no picture. A caption and a knock tell you which classroom door he is behind.
    //  - Tracking: his picture shakes and circles inside the peeked room. Only the lamp shows it.
    //  - Flicker:  the lamp dies, the picture is gone.
    //  - Charging: his picture runs down the hallway at you. Flash the beam on his face.
    public class AdminHuntDirector : MonoBehaviour
    {
        private const float FigureHeight = 2.6f;          // world units, the picture's height
        private const int FigureSortingOrder = 40;        // under the darkness overlay (70): only light reveals him
        private const float HintEverySeconds = 9f;        // repeat the door-rattle hint while he hides
        private const float StatusEverySeconds = 0.4f;    // how often the HUD line is rewritten

        private GameRoot _root;
        private ViewManager _views;
        private AdminHunt _hunt;
        private HuntTuning _tuning;
        private GameObject _figure;
        private SpriteRenderer _sprite;
        private Vector3 _anchor;
        private float _trackClock;
        private int _motionSeed;
        private float _hintClock;
        private float _statusClock;
        private float _chargeFromX;

        public bool Running { get; private set; }
        public AdminHunt Model => _hunt;
        public GameObject Figure => _figure;

        public void Begin(GameRoot root, ViewManager views, HuntTuning tuning, int seed)
        {
            _root = root;
            _views = views;
            _tuning = tuning;
            _motionSeed = seed;
            _hunt = new AdminHunt(tuning, seed);
            BuildFigure();
            Running = true;
            if (_views.Flashlight != null) _views.Flashlight.Recharge();
            React(_hunt.Start());
        }

        public void Stop()
        {
            Running = false;
            if (_figure != null) Destroy(_figure);
            _figure = null;
            _sprite = null;
        }

        private void Update() { Tick(Mathf.Min(Time.deltaTime, 0.1f)); }

        // One frame. internal so the headless FlowCheck can step it (stub MonoBehaviours never
        // get Update calls); in the editor Update calls it.
        internal void Tick(float dt)
        {
            if (!Running || _hunt == null || _hunt.Finished) return;
            if (_root.Paused || _root.BlockingOverlayOpen || _root.ShiftFailing) return;
            if (_root.Dialogue != null && _root.Dialogue.IsActive) return;
            if (_root.State != AppViewState.Exploration && _root.State != AppViewState.RoomPeek) return;
            if (_views.Transitioning) return;   // a fade is a short pause for the whole hunt

            var input = ReadInput();
            var ev = _hunt.Tick(dt, input);
            UpdateFigure(dt, input);
            TickHint(dt, input);
            React(ev);
            TickStatus(dt);
        }

        // ------------------------------------------------------------ reading the player
        private HuntInput ReadInput()
        {
            var light = _views.PeekLight;
            bool peeking = _views.PeekActive && light != null && light.Active;
            var room = _views.Current != null ? _views.Current.Id : RoomId.Hallway;
            var input = new HuntInput
            {
                Peeking = peeking,
                PlayerRoom = room,
                InHallway = !peeking && _root.State == AppViewState.Exploration && room == RoomId.Hallway,
            };
            bool figureUp = _figure != null && _figure.activeSelf;
            if (peeking && _hunt.Phase == HuntPhase.Tracking && figureUp)
            {
                float d = Vector2.Distance(light.CursorWorld, _figure.transform.position);
                input.OnTarget = light.Lit && !light.Flickering && d <= _tuning.HeadRadius;
            }
            if (input.InHallway && _hunt.Phase == HuntPhase.Charging && figureUp)
                input.BeamOnFace = _views.BeamCovers(_figure.transform.position);
            return input;
        }

        // ------------------------------------------------------------ the picture
        private void BuildFigure()
        {
            if (_figure != null) Destroy(_figure);
            _figure = new GameObject("AdminHuntFigure");
            _sprite = _figure.AddComponent<SpriteRenderer>();
            _sprite.sprite = PickSprite();
            _sprite.sortingOrder = FigureSortingOrder;
            float h = _sprite.sprite != null ? _sprite.sprite.bounds.size.y : 1f;
            float scale = h > 0.01f ? FigureHeight / h : 1f;
            _figure.transform.localScale = new Vector3(scale, scale, 1f);
            _figure.SetActive(false);
        }

        // Drop a PNG named char_admin_hunt.png into Resources/Art to replace his face; otherwise the
        // administrator's glitched window face, his badge face, or the painted portrait.
        private static Sprite PickSprite()
        {
            Sprite s;
            if (ProceduralArt.TryGet("char_admin_hunt", out s)) return s;
            if (ProceduralArt.TryGet(FaceAssets.AdminWindowKey, out s)) return s;
            if (ProceduralArt.TryGet(FaceAssets.AdminKey, out s)) return s;
            var face = new Portrait { HairStyle = 2, FaceVariant = 3, Accessory = 0, Distorted = true };
            return ProceduralArt.Portrait("hunt", face, 240, 296);
        }

        private void ShowFigureIn(RoomId room, Vector3 localPosition)
        {
            var inst = _views.Room(room);
            if (inst == null || inst.Root == null || _figure == null) return;
            _figure.transform.SetParent(inst.Root.transform, false);
            _figure.transform.localPosition = localPosition;
            _figure.SetActive(true);
        }

        private void HideFigure()
        {
            if (_figure != null) _figure.SetActive(false);
        }

        // He appears somewhere in the framed room; the shake circles around this anchor.
        private void PlaceForTracking()
        {
            float screenHalf = _views.ScreenHalfWidth;
            var rng = new GameRng(_motionSeed + _hunt.Stuns * 101 + (int)_hunt.Room * 7);
            float x = (rng.NextFloat() * 2f - 1f) * Mathf.Max(0.5f, screenHalf - 3.2f);
            float y = 0.2f + (rng.NextFloat() * 2f - 1f) * 0.7f;
            _anchor = new Vector3(x, y, 0f);
            _trackClock = 0f;
            ShowFigureIn(_hunt.Room, _anchor);
        }

        private void UpdateFigure(float dt, HuntInput input)
        {
            if (_figure == null) return;
            if (_hunt.Phase == HuntPhase.Tracking)
            {
                _trackClock += dt;
                float ox, oy;
                AdminHuntMotion.HeadOffset(_trackClock, _hunt.Stuns, _motionSeed, out ox, out oy);
                _figure.transform.localPosition = _anchor + new Vector3(ox, oy, 0f);
            }
            else if (_hunt.Phase == HuntPhase.Charging && input.InHallway)
            {
                if (!_figure.activeSelf) StartCharge();
                float playerX = _views.Player != null ? _views.Player.transform.localPosition.x : 0f;
                float x = Mathf.Lerp(_chargeFromX, playerX, _hunt.ChargeProgress);
                _figure.transform.localPosition = new Vector3(x, RoomInstance.FloorY + FigureHeight * 0.5f, 0f);
            }
        }

        // He starts from the far end of the hallway, the end the player is NOT near.
        private void StartCharge()
        {
            var hall = _views.Room(RoomId.Hallway);
            float half = hall != null ? hall.HalfWidth : 12f;
            float playerX = _views.Player != null ? _views.Player.transform.localPosition.x : 0f;
            _chargeFromX = (playerX > 0f ? -1f : 1f) * (half - 1.2f);
            ShowFigureIn(RoomId.Hallway, new Vector3(_chargeFromX, RoomInstance.FloorY + FigureHeight * 0.5f, 0f));
            if (_views.Flashlight != null) _views.Flashlight.Recharge();
            Say("*heavy footsteps in the hallway - FLASH HIS FACE!  (F / Q)*", 2.6f);
            _root.Audio.PlaySfx("sfx_heartbeat", 0.8f);
        }

        // ------------------------------------------------------------ reacting to the rules
        private void React(HuntEvent ev)
        {
            if (ev == HuntEvent.None) return;
            if ((ev & HuntEvent.Hid) != 0) OnHid();
            if ((ev & HuntEvent.TrackStart) != 0) OnTrackStart();
            if ((ev & HuntEvent.Stun) != 0) OnStun();
            if ((ev & HuntEvent.BrokeFree) != 0) Say("*he slips out of the light and is gone*", 2.2f);
            if ((ev & HuntEvent.FlickerStart) != 0) OnFlickerStart();
            if ((ev & HuntEvent.ChargeStart) != 0) OnChargeStart();
            if ((ev & HuntEvent.Caught) != 0) OnCaught();
            if ((ev & HuntEvent.Won) != 0) { HideFigure(); _root.WinAdminHunt(); }
            if ((ev & HuntEvent.Lost) != 0) { HideFigure(); _root.LoseAdminHunt(); }
        }

        private void OnHid()
        {
            HideFigure();
            _hintClock = 0f;
            DropHint();
        }

        private void OnTrackStart()
        {
            PlaceForTracking();
            _root.Audio.PlaySfx("sfx_glitch", 0.5f);
            Say("*he is right there - keep the light on his head*", 2.4f);
        }

        private void OnStun()
        {
            _root.Audio.PlaySfx("sfx_scare", 0.35f);
            Say("*something shrieks in the dark*", 2f);
            if (_hunt.Phase == HuntPhase.Recoil) HideFigure();
        }

        private void OnFlickerStart()
        {
            HideFigure();
            if (_views.PeekLight != null && _views.PeekActive) _views.PeekLight.Flicker(_tuning.FlickerSeconds);
            _root.Audio.PlaySfx("sfx_flicker", 0.5f);
        }

        // Charging starts in the hallway: if the player is still peeking, pull them out of it.
        private void OnChargeStart()
        {
            if (_views.PeekActive) _root.ExitRoomPeek();
        }

        private void OnCaught()
        {
            HideFigure();
            var session = _root.Session;
            float cost = _root.Config != null ? _root.Config.huntCaughtSanity : 15f;
            session.ApplySanity(-cost);
            _root.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, session.Sanity));
            if (_root.Scares != null) _root.Scares.Capture(AnomalyBehaviorKind.Blink);
            var current = _views.Current;
            if (_views.Player != null && current != null) _views.Player.PlaceInRoom(current, current.PlayerSpawn);
            Say("*he reached you - you are shoved back*", 2.4f);
        }

        // ------------------------------------------------------------ hints and the HUD line
        private void TickHint(float dt, HuntInput input)
        {
            if (_hunt.Phase != HuntPhase.Hiding || !input.InHallway) return;
            _hintClock += dt;
            if (_hintClock < HintEverySeconds) return;
            _hintClock = 0f;
            DropHint();
        }

        private void DropHint()
        {
            string name = _hunt.Room == RoomId.Classroom1 ? "Classroom 1 (Room 101)" : "Classroom 2 (Room 202)";
            Say("*something moves behind the door of " + name + "*", 3f);
            _root.Audio.PlaySfx("sfx_knock", 0.6f);
        }

        private void TickStatus(float dt)
        {
            _statusClock += dt;
            if (_statusClock < StatusEverySeconds) return;
            _statusClock = 0f;
            string text = StatusText();
            if (text == _lastStatus) return;
            _lastStatus = text;
            _root.SetObjective(text);
        }

        private string _lastStatus = "";

        private string StatusText()
        {
            string meter = "SERVER DOOR " + Mathf.CeilToInt(_hunt.ServerFraction * 100f) + "%   STUNS " +
                           _hunt.Stuns + "/" + _tuning.StunsToWin;
            switch (_hunt.Phase)
            {
                case HuntPhase.Tracking: return "KEEP THE LIGHT ON HIS HEAD!   " + meter;
                case HuntPhase.Charging: return "FLASH HIS FACE - F or Q!   " + meter;
                case HuntPhase.Hiding: return "He is inside the school. Peek into the classrooms (E at a door).   " + meter;
                default: return meter;
            }
        }

        private void Say(string text, float seconds)
        {
            _root.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, text, seconds, null));
        }
    }
}
