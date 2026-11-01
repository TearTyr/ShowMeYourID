using Hollowbrook.Scenes;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // Runs night 5's hunt in the school scene. Created by ViewManager.StartAdminHunt.
    //
    // The RULES live in GameLogic/AdminHunt (pure, fuzz-tested). This class only connects them
    // to the world: it reads what the player is doing (lamp on his head? beam on his face?),
    // feeds that to the rules every frame, and turns the events that come back into captions and
    // the picture of him.
    //
    // Where things happen:
    // There is no audio yet, so every cue is a caption and the HUD line.
    //  - Stalking: no picture, a caption says he is walking.
    //  - InRoom:   a caption names the room. His picture shakes and circles in it, and only the
    //              lamp shows it.
    //  - Behind:   his picture closes in along the hallway. Flash the beam on his face.
    public class AdminHuntDirector : MonoBehaviour
    {
        private const float FigureHeight = 2.6f;          // world units, the picture's height
        private const int FigureSortingOrder = 40;        // under the darkness overlay (70): only light reveals him
        private const float StatusEverySeconds = 0.4f;    // how often the HUD line is rewritten
        private const float BehindStartDistance = 6f;     // how far away he starts when he comes at your back

        private GameRoot _root;
        private ViewManager _views;
        private AdminHunt _hunt;
        private HuntTuning _tuning;
        private GameObject _figure;
        private SpriteRenderer _sprite;
        private Vector3 _anchor;
        private float _trackClock;
        private int _motionSeed;
        private float _statusClock;
        private float _behindFromX;

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
            UpdateFigure(dt);
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
            if (peeking && _hunt.Phase == HuntPhase.InRoom && figureUp)
            {
                float d = Vector2.Distance(light.CursorWorld, _figure.transform.position);
                input.OnTarget = light.Lit && !light.Flickering && d <= _tuning.HeadRadius;
            }
            if (input.InHallway && _hunt.Phase == HuntPhase.Behind && figureUp)
                input.BeamOnFace = _views.BeamCovers(_figure.transform.localPosition);   // the rooms sit at the origin
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
        private void PlaceInRoom()
        {
            float screenHalf = _views.ScreenHalfWidth;
            var rng = new GameRng(_motionSeed + _hunt.Repels * 101);
            float x = (rng.NextFloat() * 2f - 1f) * Mathf.Max(0.5f, screenHalf - 3.2f);
            float y = 0.2f + (rng.NextFloat() * 2f - 1f) * 0.7f;
            _anchor = new Vector3(x, y, 0f);
            _trackClock = 0f;
            ShowFigureIn(_hunt.Room, _anchor);
        }

        private void UpdateFigure(float dt)
        {
            if (_figure == null || !_figure.activeSelf) return;
            if (_hunt.Phase == HuntPhase.InRoom)
            {
                _trackClock += dt;
                float ox, oy;
                AdminHuntMotion.HeadOffset(_trackClock, _hunt.Repels, _motionSeed, out ox, out oy);
                _figure.transform.localPosition = _anchor + new Vector3(ox, oy, 0f);
            }
            else if (_hunt.Phase == HuntPhase.Behind)
            {
                float playerX = _views.Player != null ? _views.Player.transform.localPosition.x : 0f;
                float x = Mathf.Lerp(_behindFromX, playerX, _hunt.BehindProgress);
                _figure.transform.localPosition = new Vector3(x, RoomInstance.FloorY + FigureHeight * 0.5f, 0f);
            }
        }

        // He comes from the side with room for it, the middle of the hallway.
        private void PlaceBehindPlayer()
        {
            float playerX = _views.Player != null ? _views.Player.transform.localPosition.x : 0f;
            _behindFromX = playerX + (playerX > 0f ? -1f : 1f) * BehindStartDistance;
            ShowFigureIn(RoomId.Hallway, new Vector3(_behindFromX, RoomInstance.FloorY + FigureHeight * 0.5f, 0f));
        }

        // ------------------------------------------------------------ reacting to the rules
        private void React(HuntEvent ev)
        {
            if (ev == HuntEvent.None) return;
            if ((ev & HuntEvent.StalkStart) != 0) { HideFigure(); Say("*footsteps, somewhere down the hall*", 2.4f); }
            if ((ev & HuntEvent.EnteredRoom) != 0) OnEnteredRoom();
            if ((ev & HuntEvent.Spotted) != 0) OnSpotted();
            if ((ev & HuntEvent.AppearedBehind) != 0) OnAppearedBehind();
            if ((ev & HuntEvent.Repelled) != 0) OnRepelled();
            if ((ev & HuntEvent.Caught) != 0) OnCaught();
            if ((ev & HuntEvent.Won) != 0) { HideFigure(); _root.WinAdminHunt(); }
            if ((ev & HuntEvent.Lost) != 0) { HideFigure(); _root.LoseAdminHunt(); }
        }

        private void OnEnteredRoom()
        {
            Say("*a door creaks in " + RoomName(_hunt.Room) + " - PEEK IN AND LIGHT HIS HEAD*", 3f);
            if (_views.PeekActive && _views.Current != null && _views.Current.Id == _hunt.Room) OnSpotted();
        }

        private void OnSpotted()
        {
            if (_figure != null && _figure.activeSelf) return;
            PlaceInRoom();
            Say("*he is in here - keep the light on his head*", 2.4f);
        }

        private void OnAppearedBehind()
        {
            PlaceBehindPlayer();
            if (_views.Flashlight != null) _views.Flashlight.Recharge();
            Say("*he is right behind you - AIM AT HIS FACE AND FLASH!  (F or Q)*", 2.4f);
        }

        private void OnRepelled()
        {
            HideFigure();
            GiveSanity(_root.Config != null ? _root.Config.huntSanityPerRepel : 8f);
            Say("*a low laugh drifts away down the hall - your nerve steadies*", 2f);
        }

        private void OnCaught()
        {
            HideFigure();
            GiveSanity(-(_root.Config != null ? _root.Config.huntCaughtSanity : 15f));
            if (_root.Scares != null) _root.Scares.Capture(AnomalyBehaviorKind.Blink);
            if (_views.PeekActive) _root.ExitRoomPeek();
            else if (_views.Player != null && _views.Current != null)
                _views.Player.PlaceInRoom(_views.Current, _views.Current.PlayerSpawn);
            if (_hunt.Phase != HuntPhase.Lost) Say("*he reached you - you are thrown back*", 2.4f);
        }

        private void GiveSanity(float amount)
        {
            _root.Session.ApplySanity(amount);
            _root.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, _root.Session.Sanity));
        }

        private static string RoomName(RoomId room) =>
            room == RoomId.Classroom1 ? "Room 101" : room == RoomId.Classroom2 ? "Room 202" : room == RoomId.ComfortRoom ? "CR-1" : "CR-2";

        // ------------------------------------------------------------ the HUD line
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
            string meter = "DAWN " + Mathf.FloorToInt(_hunt.DawnProgress * 100f) + "%   CAUGHT " +
                           _hunt.Catches + "/" + _tuning.CatchesToLose;
            switch (_hunt.Phase)
            {
                case HuntPhase.InRoom: return "PEEK INTO " + RoomName(_hunt.Room) + " AND LIGHT HIS HEAD   " + meter;
                case HuntPhase.Behind: return "AIM AT HIS FACE AND FLASH - F or Q!   " + meter;
                default: return "Watch for where he stops. Stop him with the light. Survive until dawn.   " + meter;
            }
        }

        private void Say(string text, float seconds)
        {
            _root.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, text, seconds, null));
        }
    }
}
