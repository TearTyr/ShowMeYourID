using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // The cursor-locked circle of light used ONLY while peeking into a classroom. Created
    // lazily by ViewManager.EnsurePeekLight (one per session). The trick: the room's darkness
    // overlay is a SpriteRenderer that draws only OUTSIDE sprite masks, so the SpriteMask on
    // this object cuts a lit hole wherever the cursor is; the tinted Glow child is the soft
    // visible lamp on top of that hole. Interaction is cursor-distance based (no raycasts):
    // an interactable is "under the light" when its anchor point sits within CursorHitRadius
    // of the cursor's world position.
    public class PeekLight : MonoBehaviour
    {
        // Light circle size in world units (the mask and the glow share it).
        public float Radius = 2.6f;
        // How close the cursor's world point must be to an interactable anchor to prompt it.
        public float CursorHitRadius = 1.3f;

        public bool Active { get; private set; }

        private GameRoot _gameRoot;
        private Camera _cam;
        private RoomInstance _room;
        private SpriteMask _mask;
        private SpriteRenderer _glow;
        private Vector3 _worldPoint;
        private string _lastPrompt = "\0";   // "\0" = nothing published yet this peek

        /// <summary>Builds the mask + glow children once. Starts inactive.</summary>
        public void Init(GameRoot root, Camera cam)
        {
            _gameRoot = root;
            _cam = cam;

            var maskGo = new GameObject("PeekMask");
            maskGo.transform.SetParent(transform, false);
            _mask = maskGo.AddComponent<SpriteMask>();
            _mask.sprite = ProceduralArt.Get("lamp_glow");
            _mask.isCustomRangeActive = true;
            _mask.frontSortingOrder = 60;   // the darkness overlay (70) is the only masked layer
            _mask.backSortingOrder = 80;
            transform.localScale = Vector3.one * Radius * 2f;

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(transform, false);
            _glow = glowGo.AddComponent<SpriteRenderer>();
            _glow.sprite = ProceduralArt.Get("lamp_glow");
            _glow.color = new Color(1f, 0.96f, 0.85f, 0.30f);
            _glow.sortingOrder = 72;        // just above the darkness hole's edge

            SetActive(false);
        }

        /// <summary>Begin revealing the given room. Snaps the circle to the room centre.</summary>
        public void Enter(RoomInstance room)
        {
            _room = room;
            _lastPrompt = "\0";
            SetActive(true);
            transform.position = Vector3.zero;
            _worldPoint = Vector3.zero;
        }

        /// <summary>Stop revealing. The hallway presentation is restored by ViewManager.</summary>
        public void Exit()
        {
            SetActive(false);
            _room = null;
            _lastPrompt = "\0";
        }

        /// <summary>True when the world point sits inside the light circle.</summary>
        public bool Covers(Vector3 worldPoint) => Active && Vector2.Distance(worldPoint, _worldPoint) <= Radius;

        private void SetActive(bool on)
        {
            Active = on;
            gameObject.SetActive(on);
        }

        // Esc follows the hallway rule (pause first; peek only exits through the bottom band
        // or the exit doorway), and pause/dialogue/overlays freeze the cursor entirely.
        private void Update()
        {
            if (_gameRoot == null || !Active) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TickEscape();
                return;
            }
            if (_gameRoot.Paused || _gameRoot.BlockingOverlayOpen) return;
            if (_gameRoot.Dialogue != null && _gameRoot.Dialogue.IsActive) return;
            Tick(Time.deltaTime);
        }

        // Split out so the headless harness can drive the same arbitration deterministically
        // (stub MonoBehaviours never get Update calls) — the TickHotkeys trick.
        internal void TickEscape()
        {
            if (_gameRoot.BlockingOverlayOpen) return;      // Esc belongs to the open sheet
            if (_gameRoot.Paused) _gameRoot.TogglePause(false);
            else if (_gameRoot.Dialogue == null || !_gameRoot.Dialogue.IsActive) _gameRoot.TogglePause(true);
        }

        // One frame of peek light: follow the cursor on the z=0 plane, publish the prompt for
        // whatever sits under the circle, and act on E. internal so the headless harness can
        // step frames deterministically (stub MonoBehaviours never get Update calls).
        internal void Tick(float dt)
        {
            if (_gameRoot == null || _cam == null || !Active) return;

            var mouse = Input.mousePosition;
            mouse.z = -_cam.transform.position.z;           // project onto the z=0 world plane
            var wp = _cam.ScreenToWorldPoint(mouse);
            wp.z = 0f;
            transform.position = wp;
            _worldPoint = wp;

            var nearest = FindNearestUnderCursor(wp);
            string prompt = nearest != null ? nearest.Prompt : "";
            if (prompt != _lastPrompt)
            {
                _lastPrompt = prompt;
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.InteractionPrompt, prompt));
            }

            if (Input.GetKeyDown(KeyCode.E) && nearest != null)
            {
                var door = nearest as Doorway;
                if (door != null && door.Target == RoomId.Hallway) _gameRoot.ExitRoomPeek();
                else nearest.Interact();
            }
        }

        // Nearest interactable whose anchor point (chest height above the shared floor line)
        // is within CursorHitRadius of the cursor. Squared distances: no sqrt, no allocations.
        private Interactable FindNearestUnderCursor(Vector3 cursor)
        {
            if (_room == null) return null;
            Interactable nearest = null;
            float bestSq = CursorHitRadius * CursorHitRadius;
            float anchorY = RoomInstance.FloorY + 1.4f;
            foreach (var it in _room.Interactables)
            {
                if (it == null || !it.Enabled) continue;
                float dx = cursor.x - it.AnchorX;
                float dy = cursor.y - anchorY;
                float sq = dx * dx + dy * dy;
                if (sq <= bestSq) { bestSq = sq; nearest = it; }
            }
            return nearest;
        }
    }
}
