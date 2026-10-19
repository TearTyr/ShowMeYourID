using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.World
{
    public class PlayerController : MonoBehaviour
    {
        private GameRoot R;
        private SpriteRenderer _sr;
        private RoomInstance _room;
        private GameObject _cone;
        private SpriteRenderer _coneSr;
        private float _faceDir = 1f;
        private bool _flashlight;
        private float _stepTimer, _animTimer;
        private int _animFrame;
        private string _lastPrompt = "\0";
        private const float FloorY = -2.4f;

        public void Init(GameRoot root, SpriteRenderer sr)
        {
            R = root;
            _sr = sr;
            var coneGo = new GameObject("FlashlightCone");
            coneGo.transform.SetParent(transform, false);
            _coneSr = coneGo.AddComponent<SpriteRenderer>();
            _coneSr.sprite = ProceduralArt.Get("light_cone");
            // superseded by FlashlightRig's cursor-guided beam when the world is up
            _coneSr.sortingOrder = 72; // above the darkness overlay
            _cone = coneGo;
            _cone.SetActive(false);
        }

        public void PlaceInRoom(RoomInstance room, Vector3 spawn)
        {
            _room = room;
            transform.localPosition = new Vector3(spawn.x, FloorY + 1.7f, 0);
            _flashlight = false;
            UpdateCone();
            if (room.Light != null) room.Light.ToggleFocus(false);
        }

        private void Update()
        {
            if (R == null || _room == null || R.Views == null || R.Views.Transitioning) return;
            if (!gameObject.activeInHierarchy) return;

            bool dialogueOpen = R.Dialogue != null && R.Dialogue.IsActive;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (R.Paused) R.TogglePause(false);
                else if (!dialogueOpen) R.TogglePause(true);
                return;
            }
            if (R.Paused || dialogueOpen) return;
            if (R.State != AppViewState.Exploration) return; // overlays (map/security room) freeze movement

            if (Input.GetKeyDown(KeyCode.M)) R.OpenMap();
            if (Input.GetKeyDown(KeyCode.F)) ToggleFlashlight();

            float h = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
            bool running = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float speed = running ? R.Config.runSpeed : R.Config.walkSpeed;

            var p = transform.localPosition;
            if (Mathf.Abs(h) > 0.01f)
            {
                _faceDir = h > 0 ? 1f : -1f;
                p.x += h * speed * Time.deltaTime;
                p.x = Mathf.Clamp(p.x, -_room.HalfWidth + 0.6f, _room.HalfWidth - 0.6f);
                StepAndAnimate(running, Time.deltaTime);
            }
            else
            {
                SetFrame(0);
                _stepTimer = 0f;
            }
            transform.localPosition = p;
            var s = transform.localScale; s.x = _faceDir; transform.localScale = s;
            UpdateCone();
            UpdatePrompt();
        }

        private void StepAndAnimate(bool running, float dt)
        {
            _stepTimer -= dt;
            if (_stepTimer <= 0f)
            {
                _stepTimer = running ? 0.30f : 0.45f;
                R.Audio.PlaySfx("sfx_footstep", 0.30f);
            }
            _animTimer -= dt;
            if (_animTimer <= 0f)
            {
                _animTimer = running ? 0.14f : 0.22f;
                SetFrame(_animFrame == 0 ? 1 : 0);
            }
        }

        private void SetFrame(int f)
        {
            if (_animFrame == f && _sr != null && _sr.sprite != null) return;
            _animFrame = f;
            if (_sr != null) _sr.sprite = ProceduralArt.Get("custodian_" + f);
        }

        private void ToggleFlashlight()
        {
            _flashlight = !_flashlight;
            if (_room != null && _room.Light != null) _room.Light.ToggleFocus(_flashlight);
            R.Audio.PlaySfx("sfx_click", 0.5f);
            UpdateCone();
        }

        private void UpdateCone()
        {
            if (_cone == null) return;
            _cone.SetActive(_flashlight);
            if (!_flashlight) return;
            _cone.transform.localPosition = new Vector3(_faceDir * 2.9f, 0.2f, 0);
            _cone.transform.localScale = new Vector3(_faceDir * 0.78f, 0.72f, 1f);
        }

        public bool ConeCovers(Vector3 worldPoint)
        {
            if (R != null && R.Views != null && R.Views.Flashlight != null)
                return R.Views.Flashlight.ConeCovers(worldPoint);
            if (!_flashlight || _room == null) return false;
            float dx = worldPoint.x - transform.localPosition.x;
            if (Mathf.Sign(dx) != _faceDir && Mathf.Abs(dx) > 0.2f) return false;
            return Mathf.Abs(dx) < 6.2f && Mathf.Abs(worldPoint.y - transform.localPosition.y) < 3.4f;
        }

        private void UpdatePrompt()
        {
            string prompt = "";
            if (_room != null)
            {
                Interactable best = null;
                float bestD = R.Config.interactRange;
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
                R.Bus.Publish(GameEvent.Of(EventIds.InteractionPrompt, prompt));
            }
        }
    }
}
