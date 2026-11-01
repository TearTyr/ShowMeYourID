using Hollowbrook.Scenes;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // The hooded figure standing in a peek room (GameRoot.PeekAnomalyIn says which rooms have one
    // tonight). It never moves and sits under the peek's black cover, so only the lamp shows it.
    //
    // The scene, once the lit FOCUS lamp is fully on him (the cursor on his body):
    //   1. Flicker: he jumps around and the lamp dims and flickers (peekRepelSeconds).
    //   2. Black: the lamp is gone for a moment, he is removed and the room counts as cleared.
    //   3. Fade back: the room slowly returns, and he is gone.
    // Once it starts it always plays to the end, and the lamp, E and stepping back are locked
    // meanwhile. He never attacks. If the player steps back before lighting him he stays.
    public class PeekAnomaly : MonoBehaviour
    {
        private const float Height = 3.2f;       // the art's height in world units (see FilePpus)
        private const float BodyHalfW = 0.8f;    // the cursor box that counts as "on him"
        private const float BodyHalfH = 1.5f;
        private const float JumpUnits = 1.1f;    // how far he flickers around in the first phase
        private const float JumpEvery = 0.07f;

        private enum Phase { Waiting, Flicker, Black, FadeBack }

        private GameRoot _root;
        private PeekLight _lamp;
        private SpriteRenderer _sr;
        private Vector3 _home;
        private Phase _phase = Phase.Waiting;
        private float _clock;
        private float _nextJump;
        private bool _told;
        private bool _removed;
        private RoomId _room;
        private GameRng _rng;

        public static PeekAnomaly Place(GameRoot root, RoomInstance room, PeekLight lamp, int seed)
        {
            var go = RoomBuilder.SpriteGO(room.Root.transform, "PeekAnomaly", ProceduralArt.Get("char_hood"),
                Vector3.zero, WorldLayers.PeekCover - 2);
            var a = go.AddComponent<PeekAnomaly>();
            a._root = root;
            a._lamp = lamp;
            a._room = room.Id;
            a._sr = go.GetComponent<SpriteRenderer>();
            a._rng = new GameRng(seed ^ 0x600D);
            var rng = new GameRng(seed);
            a._home = new Vector3(rng.NextFloat() * 10f - 5f, RoomInstance.FloorY + Height / 2f, 0f);
            go.transform.localPosition = a._home;
            return a;
        }

        /// <summary>True while the flicker, black or fade back is playing.</summary>
        internal bool Playing => _phase != Phase.Waiting;

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
                case Phase.Waiting: TickWaiting(); break;
                case Phase.Flicker: TickFlicker(dt); break;
                case Phase.Black: TickBlack(dt); break;
                default: TickFadeBack(dt); break;
            }
        }

        private void TickWaiting()
        {
            if (!_lamp.Active || !_root.PeekAnomalyIn(_room)) return;
            bool lit = _lamp.Lit && !_lamp.Flickering;
            if (lit && _lamp.Covers(_home) && !_told)
            {
                _told = true;
                string hint = _lamp.Focused ? "*it stands perfectly still*" : "*the light does nothing. Focus it (Q)*";
                _root.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, hint, 3f, null));
            }
            // only the tight Focus lamp drives him out; the wide lamp only shows him
            if (!lit || !_lamp.Focused || !_lamp.CursorWithin(_home, BodyHalfW, BodyHalfH)) return;

            _phase = Phase.Flicker;
            _clock = 0f;
            _nextJump = 0f;
            _lamp.Lock();
            _root.Audio.PlaySfx("sfx_glitch", 0.6f);
        }

        // He flickers around and the lamp dims and flickers, for peekRepelSeconds.
        private void TickFlicker(float dt)
        {
            _clock += dt;
            float total = _root.Config.peekRepelSeconds;
            bool calm = _root.Settings != null && (_root.Settings.PhotosensitivitySafe || _root.Settings.ReduceMotion);
            float progress = Mathf.Clamp01(_clock / total);

            if (calm)
            {
                // no strobe: a steady dim, and he stays where he stands
                _lamp.SetDim(progress * 0.8f);
            }
            else
            {
                _nextJump -= dt;
                if (_nextJump <= 0f)
                {
                    _nextJump = JumpEvery;
                    float jx = (_rng.NextFloat() * 2f - 1f) * JumpUnits;
                    float jy = (_rng.NextFloat() * 2f - 1f) * JumpUnits * 0.4f;
                    transform.localPosition = _home + new Vector3(jx, jy, 0f);
                    _sr.enabled = _rng.NextFloat() > 0.2f;
                    // the lamp gets weaker on average, and stutters around that
                    _lamp.SetDim(Mathf.Clamp01(progress * 0.75f + _rng.NextFloat() * 0.35f));
                }
            }
            if (_clock < total) return;

            // the light dies and he is gone before it comes back
            _phase = Phase.Black;
            _clock = 0f;
            _lamp.SetDim(1f);
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

        // The room counts for the patrol from the moment he is gone.
        private void Remove()
        {
            if (_removed) return;
            _removed = true;
            _sr.enabled = false;
            transform.localPosition = _home;
            _root.RepelPeekAnomaly();
        }

        // Leaving the peek (or any teardown) in the middle of the scene must never leave the lamp
        // dim or locked.
        private void OnDestroy()
        {
            if (_lamp != null && _phase != Phase.Waiting) _lamp.Unlock();
        }
    }
}
