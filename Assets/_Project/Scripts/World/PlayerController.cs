using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.World
{
    // Walk/run/interact for the custodian. Created by ViewManager.
    public class PlayerController : MonoBehaviour
    {
        private GameRoot _gameRoot;
        private SpriteRenderer _sr;
        private RoomInstance _room;
        private float _faceDir = 1f;

        /// <summary>1 facing right, -1 facing left. The art is drawn facing right, so left is a flip.</summary>
        public float Facing => _faceDir;
        private float _stepTimer, _animTimer;
        private int _animStep;   // where we are in the walk cycle
        // The walk cycle: step, stand, other step, stand. The standing pose is also the idle pose.
        private static readonly string[] WalkCycle =
            { "char_guard_walk_a", "char_guard_idle", "char_guard_walk_b", "char_guard_idle" };
        private const string IdlePose = "char_guard_idle";
        private string _pose;
        private string _lastPrompt = "\0";

        public void Init(GameRoot root, SpriteRenderer sr)
        {
            _gameRoot = root;
            _sr = sr;
        }

        public void PlaceInRoom(RoomInstance room, Vector3 spawn)
        {
            _room = room;
            transform.localPosition = new Vector3(spawn.x, RoomInstance.FloorY + 1.7f, 0);
            // room lighting and the handheld beam are untouched by walking through a door
        }

        private void Update()
        {
            if (_gameRoot == null || _room == null || _gameRoot.Views == null || _gameRoot.Views.Transitioning) return;
            if (!gameObject.activeInHierarchy) return;
            Tick(Time.deltaTime);
        }

        // One frame of player control. internal so the headless FlowCheck can step frames
        // deterministically (stub MonoBehaviours never get Update calls).
        internal void Tick(float dt)
        {
            bool dialogueOpen = _gameRoot.Dialogue != null && _gameRoot.Dialogue.IsActive;

            if (Input.GetKeyDown(KeyCode.Escape) && InputGate.TryEscape())
            {
                // Escape belongs to an open overlay sheet (Options/Controls/Credits) first -
                // unpausing here would tear the player out of the pause menu behind it.
                if (_gameRoot.BlockingOverlayOpen) return;
                if (_gameRoot.Paused) _gameRoot.TogglePause(false);
                else if (!dialogueOpen) _gameRoot.TogglePause(true);
                return;
            }
            if (_gameRoot.Paused || dialogueOpen) return;
            if (_gameRoot.State != AppViewState.Exploration) return; // overlays (map/security room) freeze movement

            float h = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
            bool running = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float speed = running ? _gameRoot.Config.runSpeed : _gameRoot.Config.walkSpeed;

            var p = transform.localPosition;
            if (Mathf.Abs(h) > 0.01f)
            {
                _faceDir = h > 0 ? 1f : -1f;
                p.x += h * speed * dt;
                p.x = Mathf.Clamp(p.x, -_room.HalfWidth + 0.6f, _room.HalfWidth - 0.6f);
                StepAndAnimate(running, dt);
            }
            else
            {
                SetPose(IdlePose);
                _animStep = 0;
                _stepTimer = 0f;
            }
            transform.localPosition = p;
            FaceTheBeam();
            var s = transform.localScale; s.x = _faceDir; transform.localScale = s;
            UpdatePrompt();
        }

        // A lit flashlight turns the guard to look where it points, even while he backs away.
        // With it off he simply faces the way he walks. Straight up keeps the side he was on.
        private void FaceTheBeam()
        {
            var rig = _gameRoot.Views.Flashlight;
            if (rig != null && rig.BeamOn && Mathf.Abs(rig.Dir.x) > 0.05f)
                _faceDir = rig.Dir.x > 0f ? 1f : -1f;
        }

        private void StepAndAnimate(bool running, float dt)
        {
            _stepTimer -= dt;
            if (_stepTimer <= 0f)
            {
                _stepTimer = running ? 0.30f : 0.45f;
                _gameRoot.Audio.PlaySfx("sfx_footstep", 0.30f);
            }
            _animTimer -= dt;
            if (_animTimer <= 0f)
            {
                _animTimer = running ? 0.10f : 0.16f;
                SetPose(WalkCycle[_animStep]);
                _animStep = (_animStep + 1) % WalkCycle.Length;
            }
        }

        // Show one of the guard's drawn poses. Without the art files the old code-drawn guard is used.
        private void SetPose(string key)
        {
            if (_pose == key && _sr != null && _sr.sprite != null) return;
            _pose = key;
            if (_sr == null) return;
            Sprite pose;
            if (ProceduralArt.TryGet(key, out pose)) _sr.sprite = pose;
            else _sr.sprite = ProceduralArt.Get(key == IdlePose ? "custodian_0" : "custodian_1");
        }


        // The beam lives in FlashlightRig and world illumination queries live in ViewManager.

        /// <summary>Forget the last prompt, so the guard writes his own on the next frame. The peek
        /// shares the same prompt line, and what the cursor found there must not stay behind.</summary>
        public void ResetPrompt() => _lastPrompt = "\0";

        // Hallway interaction goes by the guard's body: the nearest thing he is standing at. The
        // cursor plays no part here, it only aims the flashlight.
        private void UpdatePrompt()
        {
            string prompt = "";
            if (_room != null)
            {
                Interactable best = null;
                float bestD = _gameRoot.Config.interactRange;
                foreach (var it in _room.Interactables)
                {
                    if (it == null || !it.Enabled) continue;
                    float d = it.DistanceTo(transform.localPosition);
                    if (d < bestD) { bestD = d; best = it; }
                }
                if (best != null)
                {
                    prompt = best.Prompt;
                    if (Input.GetKeyDown(KeyCode.E)) best.Interact();
                }
            }
            if (prompt != _lastPrompt)
            {
                _lastPrompt = prompt;
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.InteractionPrompt, prompt));
            }
        }
    }
}
