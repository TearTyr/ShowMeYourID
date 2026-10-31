using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // Runs night 5's hunt in the school scene. Created by ViewManager.StartAdminHunt.
    //
    // The RULES live in GameLogic/AdminHunt (pure, fuzz-tested). This class only connects them
    // to the world: it reads what the player is doing (holding a door? lamp on his head? beam on
    // his face?), feeds that to the rules every frame, and turns the events that come back into
    // captions and the picture of him.
    //
    // Where things happen:
    // There is no audio yet, so every cue is a caption and the HUD line.
    //  - Stalking: no picture, a caption says he is walking.
    //  - AtDoor:   a caption names the door. That door's prompt becomes "hold it shut",
    //              and it cannot be peeked through while he is behind it.
    //  - Closet:   his picture shakes and circles inside the comfort room. Only the lamp shows it.
    //  - Behind:   his picture closes in along the hallway. Flash the beam on his face.
    public class AdminHuntDirector : MonoBehaviour
    {
        private const float FigureHeight = 2.6f;          // world units, the picture's height
        private const int FigureSortingOrder = 40;        // under the darkness overlay (70): only light reveals him
        private const float StatusEverySeconds = 0.4f;    // how often the HUD line is rewritten
        private const float BehindStartDistance = 6f;     // how far away he starts when he comes at your back
        private const string HoldPrompt = "HOLD E - KEEP THE DOOR SHUT";

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
        private Doorway _heldDoor;
        private string _heldDoorPrompt;

        public bool Running { get; private set; }
        public AdminHunt Model => _hunt;
        public GameObject Figure => _figure;
        /// <summary>Headless proofs set this: stands in for the E key being held.</summary>
        internal bool ForceHoldKey;

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
            ReleaseDoor();
            if (_figure != null) Destroy(_figure);
            _figure = null;
            _sprite = null;
        }

        /// <summary>True while he is behind this door: Doorway will not open it for a peek.</summary>
        public bool BlocksDoor(RoomId room) =>
            Running && _hunt != null && _hunt.Phase == HuntPhase.AtDoor && _hunt.Room == room;

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
            if (peeking && _hunt.Phase == HuntPhase.Closet && figureUp)
            {
                float d = Vector2.Distance(light.CursorWorld, _figure.transform.position);
                input.OnTarget = light.Lit && !light.Flickering && d <= _tuning.HeadRadius;
            }
            if (input.InHallway && _hunt.Phase == HuntPhase.Behind && figureUp)
                input.BeamOnFace = _views.BeamCovers(_figure.transform.localPosition);   // the rooms sit at the origin
            if (input.InHallway && _hunt.Phase == HuntPhase.AtDoor)
                input.HoldingDoor = AtHeldDoor() && (ForceHoldKey || Input.GetKey(KeyCode.E));
            return input;
        }

        private bool AtHeldDoor()
        {
            if (_views.Player == null) return false;
            float x = _views.Player.transform.localPosition.x;
            return Mathf.Abs(x - RoomGraph.DoorX(RoomId.Hallway, _hunt.Room)) <= _root.Config.interactRange;
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
        private void PlaceInComfortRoom()
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
            if (_hunt.Phase == HuntPhase.Closet)
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
            if ((ev & HuntEvent.StalkStart) != 0) { Clear(); Say("*footsteps, somewhere down the hall*", 2.4f); }
            if ((ev & HuntEvent.ReachedDoor) != 0) OnReachedDoor();
            if ((ev & HuntEvent.EnteredCloset) != 0) OnEnteredCloset();
            if ((ev & HuntEvent.Spotted) != 0) OnSpotted();
            if ((ev & HuntEvent.AppearedBehind) != 0) OnAppearedBehind();
            if ((ev & HuntEvent.Repelled) != 0) OnRepelled();
            if ((ev & HuntEvent.Caught) != 0) OnCaught();
            if ((ev & HuntEvent.Won) != 0) { Clear(); _root.WinAdminHunt(); }
            if ((ev & HuntEvent.Lost) != 0) { Clear(); _root.LoseAdminHunt(); }
        }

        private void Clear()
        {
            HideFigure();
            ReleaseDoor();
        }

        private void OnReachedDoor()
        {
            // the door the player is peeking through is the one he came to: back out of it
            if (_views.PeekActive && _views.Current != null && _views.Current.Id == _hunt.Room)
                _root.ExitRoomPeek();
            ClaimDoor();
            Say("*the footsteps stop outside " + RoomName(_hunt.Room) + " - HOLD THAT DOOR*", 3f);
        }

        private void OnEnteredCloset()
        {
            Say("*a door creaks in " + RoomName(_hunt.Room) + " - PEEK IN AND FIND HIM*", 3f);
            if (_views.PeekActive && _views.Current != null && _views.Current.Id == _hunt.Room) OnSpotted();
        }

        private void OnSpotted()
        {
            if (_figure != null && _figure.activeSelf) return;
            PlaceInComfortRoom();
            Say("*he is in here - keep the light on his head*", 2.4f);
        }

        private void OnAppearedBehind()
        {
            PlaceBehindPlayer();
            if (_views.Flashlight != null) _views.Flashlight.Recharge();
            Say("*he is right behind you - TURN AND FLASH HIS FACE!  (A / D, then F or Q)*", 2.4f);
        }

        private void OnRepelled()
        {
            Clear();
            Say("*a low laugh drifts away down the hall*", 2f);
        }

        private void OnCaught()
        {
            Clear();
            var session = _root.Session;
            float cost = _root.Config != null ? _root.Config.huntCaughtSanity : 15f;
            session.ApplySanity(-cost);
            _root.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, session.Sanity));
            if (_root.Scares != null) _root.Scares.Capture(AnomalyBehaviorKind.Blink);
            if (_views.PeekActive) _root.ExitRoomPeek();
            else if (_views.Player != null && _views.Current != null)
                _views.Player.PlaceInRoom(_views.Current, _views.Current.PlayerSpawn);
            if (_hunt.Phase != HuntPhase.Lost) Say("*he reached you - you are thrown back*", 2.4f);
        }

        // The door he is behind stops being a way to peek and tells the player what to do.
        private void ClaimDoor()
        {
            ReleaseDoor();
            var hall = _views.Room(RoomId.Hallway);
            if (hall == null) return;
            foreach (var it in hall.Interactables)
            {
                var door = it as Doorway;
                if (door == null || door.Target != _hunt.Room) continue;
                _heldDoor = door;
                _heldDoorPrompt = door.Prompt;
                door.Prompt = HoldPrompt;
                return;
            }
        }

        private void ReleaseDoor()
        {
            if (_heldDoor != null) _heldDoor.Prompt = _heldDoorPrompt;
            _heldDoor = null;
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
                case HuntPhase.AtDoor: return "HOLD THE DOOR OF " + RoomName(_hunt.Room).ToUpperInvariant() + " (E)   " + meter;
                case HuntPhase.Closet: return "PEEK INTO " + RoomName(_hunt.Room) + " AND LIGHT HIS HEAD   " + meter;
                case HuntPhase.Behind: return "TURN TO HIM AND FLASH HIS FACE - F or Q!   " + meter;
                default: return "Listen for where the footsteps stop. Survive until dawn.   " + meter;
            }
        }

        private void Say(string text, float seconds)
        {
            _root.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, text, seconds, null));
        }
    }
}
