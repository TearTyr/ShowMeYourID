using Hollowbrook.Scenes;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // What stands in a peek room (GameRoot.PeekAnomalyIn says which rooms have one tonight). Any room can hold
    // the hooded figure, the crouched one or the ceiling hanger (PeekSpots deals them out). It stands on a
    // spot of clear painted floor (the hanger hangs from the top), sized for its depth, under the peek's black
    // cover, so only the lamp shows it.
    //
    // Holding the lamp on its body fills a bar (PeekClear): 3 seconds with the plain wide lamp, 1.3 times faster
    // with the Focus lamp (Q); the bar drains back if the lamp leaves. The lamp costs battery all the while.
    // While it fills:
    //   Rush  the figure jumps closer in steps, growing each time, until it fills the screen
    //   Still it stays exactly where it stands
    // When the bar is full the lamp goes black, the figure is removed and the room counts as cleared, then the
    // room slowly fades back with nothing in it. From the black on, the lamp and stepping back are locked.
    // It never attacks. Reduce Motion and Photosensitive keep a steady dim and no movement.
    public class PeekAnomaly : MonoBehaviour
    {
        private const int RushSteps = 5;
        private const float RushFillHeight = 12f;    // how tall it is at the last step: more than the screen
        private const float RushFeetY = -4.6f;       // where its feet end up, below the front edge
        private const float AboveCoverFrom = 0.6f;   // from this share of the rush it is drawn over the black

        private enum Phase { Waiting, Charging, Black, FadeBack }

        private GameRoot _root;
        private PeekLight _lamp;
        private SpriteRenderer _sr;
        private PeekKind _kind;
        private PeekReaction _reaction;
        private RoomId _room;
        private float _homeX, _homeFeetY, _homeHeight, _spriteHeight;
        private Phase _phase = Phase.Waiting;
        private float _progress;
        private int _lastStep;
        private float _clock;
        private float _nextStutter;
        private bool _told;
        private bool _removed;
        private GameRng _rng;

        public static PeekAnomaly Place(GameRoot root, RoomInstance room, PeekLight lamp, int seed)
        {
            int runSeed = root.Session.Seed, night = root.Session.CurrentShift;
            var kind = PeekSpots.KindIn(runSeed, night, room.Id);
            var sprite = ProceduralArt.Get(PeekSpots.ArtKey(kind));
            var go = RoomBuilder.SpriteGO(room.Root.transform, "PeekAnomaly", sprite, Vector3.zero, WorldLayers.PeekCover - 2);
            var a = go.AddComponent<PeekAnomaly>();
            a._root = root;
            a._lamp = lamp;
            a._room = room.Id;
            a._kind = kind;
            a._reaction = PeekSpots.ReactionIn(runSeed, night, room.Id);
            a._sr = go.GetComponent<SpriteRenderer>();
            a._rng = new GameRng(seed ^ 0x600D);
            a._spriteHeight = sprite != null && sprite.bounds.size.y > 0.01f ? sprite.bounds.size.y : 1f;
            a.ChooseSpot(runSeed, night);
            a.Show(a._homeX, a._homeFeetY, a._homeHeight);
            return a;
        }

        // The spot it stands at tonight and how big it is there.
        private void ChooseSpot(int runSeed, int night)
        {
            int index = PeekSpots.SpotIndexIn(runSeed, night, _room);
            if (_kind == PeekKind.Coverhug)
            {
                _homeX = PeekSpots.CeilingXs(_room)[index];
                _homeHeight = PeekSpots.HeightAt(_kind, _room, 0f);
                _homeFeetY = PeekSpots.ScreenTopY - _homeHeight;   // hangs from the top of the screen
                return;
            }
            var spot = PeekSpots.For(_room)[index];
            _homeX = spot.X;
            _homeFeetY = spot.FeetY;
            _homeHeight = PeekSpots.HeightAt(_kind, _room, spot.FeetY);
        }

        // Put the figure with its feet at feetY, height tall, centred on x.
        private void Show(float x, float feetY, float height)
        {
            transform.localScale = Vector3.one * (height / _spriteHeight);
            transform.localPosition = new Vector3(x, feetY + height * 0.5f, 0f);
        }

        /// <summary>True while the bar is filling, or the black and the fade back are playing.</summary>
        internal bool Playing => _phase != Phase.Waiting;

        internal PeekKind Kind => _kind;
        internal PeekReaction Reaction => _reaction;
        internal float Progress => _progress;
        internal float CurrentHeight => _spriteHeight * transform.localScale.y;

        private void Update()
        {
            if (_root == null || _root.Paused || _root.BlockingOverlayOpen) return;
            if (_root.Dialogue != null && _root.Dialogue.IsActive) return;
            Tick(Time.deltaTime);
        }

        // internal so the headless FlowCheck can step frames (stub MonoBehaviours get no Update)
        internal void Tick(float dt)
        {
            if (_lamp == null || _root == null) return;
            switch (_phase)
            {
                case Phase.Waiting:
                case Phase.Charging: TickCharge(dt); break;
                case Phase.Black: TickBlack(dt); break;
                default: TickFadeBack(dt); break;
            }
        }

        // The lamp lit and on its body fills the bar; anywhere else the bar drains.
        private void TickCharge(float dt)
        {
            if (!_lamp.Active || !_root.PeekAnomalyIn(_room)) return;
            bool lit = _lamp.Lit && !_lamp.Flickering;
            var centre = transform.localPosition;
            if (lit && _lamp.Covers(centre) && !_told)
            {
                _told = true;
                _root.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*hold the light on it. The wide light works, Focus (Q) is faster*", 3f, null));
            }
            float height = CurrentHeight;
            bool onBody = lit && _lamp.CursorWithin(centre, PeekSpots.BodyHalfWidth(_kind, height), PeekSpots.BodyHalfHeight(_kind, height));
            _progress = PeekClear.Advance(_progress, dt, onBody, _lamp.Focused, _root.Config.peekClearSeconds);

            if (_progress <= 0f)
            {
                if (_phase == Phase.Charging) Reset();
                return;
            }
            _phase = Phase.Charging;
            React(dt);
            if (_progress >= 1f) Finish();
        }

        // What it does while the bar fills.
        private void React(float dt)
        {
            bool calm = _root.Settings != null && (_root.Settings.PhotosensitivitySafe || _root.Settings.ReduceMotion);
            if (calm) { _lamp.SetDim(_progress * 0.8f); return; }   // a steady dim, it stays where it stands

            if (_reaction == PeekReaction.Still) { _lamp.SetDim(_progress * 0.5f); return; }

            int step = Mathf.FloorToInt(_progress * RushSteps);
            if (step > _lastStep) _root.Audio.PlaySfx("sfx_glitch", 0.5f);
            _lastStep = step;
            float share = step / (float)RushSteps;
            float tall = Mathf.Lerp(_homeHeight, RushFillHeight, share);
            Show(Mathf.Lerp(_homeX, 0f, share * 0.8f), Mathf.Lerp(_homeFeetY, RushFeetY, share), tall);
            _sr.sortingOrder = share >= AboveCoverFrom ? WorldLayers.PeekCover + 2 : WorldLayers.PeekCover - 2;

            // the lamp stutters while it comes closer, weaker on average as it gets there
            _nextStutter -= dt;
            if (_nextStutter <= 0f)
            {
                _nextStutter = 0.07f;
                _lamp.SetDim(Mathf.Clamp01(_progress * 0.75f + _rng.NextFloat() * 0.35f));
            }
        }

        // The lamp left it before the bar was full: it settles back where it stood.
        private void Reset()
        {
            _phase = Phase.Waiting;
            _lastStep = 0;
            Show(_homeX, _homeFeetY, _homeHeight);
            _sr.sortingOrder = WorldLayers.PeekCover - 2;
            _lamp.SetDim(0f);
        }

        // The bar is full: the light dies and it is gone before the light comes back.
        private void Finish()
        {
            _phase = Phase.Black;
            _clock = 0f;
            _lamp.Lock();
            _lamp.SetDim(1f);
            _root.Audio.PlaySfx("sfx_glitch", 0.6f);
            Remove();
        }

        private void TickBlack(float dt)
        {
            _clock += dt;
            _lamp.SetDim(1f);
            if (_clock < _root.Config.peekBlackoutSeconds) return;
            _phase = Phase.FadeBack;
            _clock = 0f;
        }

        // A slow fade back to the lit room, with nothing standing in it.
        private void TickFadeBack(float dt)
        {
            _clock += dt;
            float total = _root.Config.peekFadeBackSeconds;
            _lamp.SetDim(1f - Mathf.Clamp01(_clock / total));
            if (_clock < total) return;
            _lamp.Unlock();
            Destroy(gameObject);
        }

        // The room counts for the patrol from the moment it is gone.
        private void Remove()
        {
            if (_removed) return;
            _removed = true;
            _sr.enabled = false;
            _root.RepelPeekAnomaly();
        }

        // Leaving the peek (or any teardown) half way must never leave the lamp dim or locked.
        private void OnDestroy()
        {
            if (_lamp != null && _phase != Phase.Waiting) _lamp.Unlock();
        }
    }
}
