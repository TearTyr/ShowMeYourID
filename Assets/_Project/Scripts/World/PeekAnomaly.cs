using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // A standing anomaly in a peek room (GameRoot.PeekAnomalyIn says which rooms have one tonight).
    // It never moves and sits under the peek's black cover, so only the lamp shows it. Hold the lit
    // lamp on it and it shakes, then flees. It stays until driven out, however often the player
    // steps back.
    public class PeekAnomaly : MonoBehaviour
    {
        private const float Size = 0.75f;
        private const float ShakeUnits = 0.09f;

        private GameRoot _root;
        private PeekLight _lamp;
        private Vector3 _home;
        private float _held;
        private bool _told;
        private RoomId _room;

        public static PeekAnomaly Place(GameRoot root, RoomInstance room, PeekLight lamp, int seed)
        {
            var go = RoomBuilder.SpriteGO(room.Root.transform, "PeekAnomaly", ProceduralArt.Get("char_anomaly"),
                Vector3.zero, WorldLayers.PeekCover - 2);
            var a = go.AddComponent<PeekAnomaly>();
            a._root = root;
            a._lamp = lamp;
            a._room = room.Id;
            var rng = new Hollowbrook.Logic.GameRng(seed);
            a._home = new Vector3(rng.NextFloat() * 10f - 5f, RoomInstance.FloorY + 1.8f, 0f);
            go.transform.localPosition = a._home;
            go.transform.localScale = new Vector3(Size, Size, 1f);
            go.GetComponent<SpriteRenderer>().flipX = a._home.x > 0f;   // it looks in towards the room
            return a;
        }

        private void Update()
        {
            if (_root == null || _root.Paused || _root.BlockingOverlayOpen) return;
            if (_root.Dialogue != null && _root.Dialogue.IsActive) return;
            Tick(Time.deltaTime);
        }

        // internal so the headless FlowCheck can step frames (stub MonoBehaviours get no Update)
        internal void Tick(float dt)
        {
            if (_lamp == null || !_lamp.Active || !_root.PeekAnomalyIn(_room)) return;
            bool lit = _lamp.Covers(_home);
            if (lit && !_told)
            {
                _told = true;
                _root.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*it is in here - keep the light on it*", 3f, null));
            }
            _held = lit ? _held + dt : Mathf.Max(0f, _held - dt * 0.5f);
            float amount = lit ? ShakeUnits * (1f + _held) : 0f;
            transform.localPosition = _home + new Vector3(Mathf.Sin(_held * 60f) * amount, Mathf.Cos(_held * 47f) * amount, 0f);
            if (_held < _root.Config.peekRepelSeconds) return;
            _root.RepelPeekAnomaly();
            Destroy(gameObject);
        }
    }
}
