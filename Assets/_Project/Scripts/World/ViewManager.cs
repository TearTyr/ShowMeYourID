using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.World
{
    // Rooms, doors, transitions, anomaly spawning, flashlight rig. Created by SchoolSceneBootstrap.
    // Illumination queries are coordinated HERE (one place, one coordinate space - the shared
    // world/room plane): the beam cone belongs to FlashlightRig, ambient light to each room's
    // LightController. Consumers (anomalies, sanity, spawner) never mix the two themselves.
    public class ViewManager : MonoBehaviour
    {
        private GameRoot _gameRoot;
        private Camera _cam;
        private GameObject _worldRoot;
        private GameObject _anomalyRoot;   // stays active while the world is shown: anomalies
                                           // keep living (and roaming) when their room is not on screen
        private readonly Dictionary<RoomId, RoomInstance> _rooms = new Dictionary<RoomId, RoomInstance>();
        private readonly List<WanderingAnomaly> _anomalies = new List<WanderingAnomaly>();
        public RoomInstance Current { get; private set; }
        public PlayerController Player { get; private set; }
        public bool Transitioning { get; private set; }
        private RoomBuilder _builder;
        private RoomSpawner _spawner;
        public FlashlightRig Flashlight { get; private set; }
        private UnityEngine.UI.Image _fade;
        private PeekLight _peekLight;    // the cursor-locked lamp for classroom peeks (lazy)
        private bool _peeking;           // true from EnterPeek until ExitPeek has finished
        private bool _peekExiting;       // guards the step-back routine (idempotent exit)

        /// <summary>The administrator hunt driver (null until a hunt starts).</summary>
        public AdminHuntDirector Hunt { get; private set; }

        /// <summary>Half the width of the screen in world units (the framed peek view).</summary>
        public float ScreenHalfWidth => _cam.orthographicSize * _cam.aspect;

        public Transform AnomalyRoot => _anomalyRoot != null ? _anomalyRoot.transform : null;
        public IReadOnlyList<WanderingAnomaly> Anomalies => _anomalies;
        public RoomSpawner Spawner => _spawner;   // exposed for headless diagnostics
        internal PeekLight PeekLight => _peekLight;   // exposed for headless diagnostics

        /// <summary>True while a classroom peek is on screen (fade-in through fade-out).</summary>
        public bool PeekActive => _peeking;

        /// <summary>True when the point is inside the peek light's circle - the only light
        /// that reveals anything while peeking.</summary>
        public bool IsPeekLit(Vector3 worldPoint) =>
            _peekLight != null && _peekLight.Active && _peekLight.Covers(worldPoint);

        public void Init(GameRoot root, Camera cam, GameObject worldRoot)
        {
            _gameRoot = root;
            _cam = cam;
            _worldRoot = worldRoot;

            // dedicated fade canvas (between FX and UI layers; parented to the scene root so
            var fadeGo = new GameObject("TransitionFade", typeof(RectTransform));
            var fadeCanvas = fadeGo.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 8;
            fadeGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            _fade = UIFactory.FullImage(fadeGo, Color.black);   // also stretches it full-screen
            _fade.raycastTarget = false;
            SetFade(0f);

            BuildPlayer();
            _anomalyRoot = new GameObject("AnomalyRoot");
            _anomalyRoot.transform.SetParent(worldRoot.transform, false);
            _builder = new RoomBuilder(root, worldRoot, _rooms);
            _spawner = new RoomSpawner(root, this, null);
            _rooms[RoomId.Hallway] = _builder.BuildRoom(RoomId.Hallway);
            _rooms[RoomId.Classroom1] = _builder.BuildRoom(RoomId.Classroom1);
            _rooms[RoomId.Classroom2] = _builder.BuildRoom(RoomId.Classroom2);
            _rooms[RoomId.ComfortRoom] = _builder.BuildRoom(RoomId.ComfortRoom);
            _rooms[RoomId.ComfortRoom2] = _builder.BuildRoom(RoomId.ComfortRoom2);
        }

        private void BuildPlayer()
        {
            var go = new GameObject("Player");
            go.transform.SetParent(_worldRoot.transform, false);
            Player = go.AddComponent<PlayerController>();
            var sr = go.AddComponent<SpriteRenderer>();
            // the guard is drawn but stands still (PlayerController gates walking); peek
            // hides him because the hallway room is not on screen while looking in
            Sprite idle;
            sr.sprite = ProceduralArt.TryGet("char_guard_idle", out idle) ? idle : ProceduralArt.Get("custodian_0");
            sr.sortingOrder = 73; // above the darkness overlay (70), lit by design (the
                                  // custodian you play); anomalies stay under it (40) as
                                  // silhouettes, switches/markers (75+) stay on top
            Player.Init(_gameRoot, sr);
        }

        public void ShowWorld()
        {
            if (Flashlight == null && _cam != null)
            {
                var rigGo = new GameObject("FlashlightRig");
                rigGo.transform.SetParent(transform, false);
                Flashlight = rigGo.AddComponent<FlashlightRig>();
                var coneGo = RoomBuilder.SpriteGO(rigGo.transform, "beam", ProceduralArt.Get("light_cone"),
                    new Vector3(0, 0, -0.5f), 71);
                var cone = coneGo.GetComponent<SpriteRenderer>();
                cone.color = new Color(1f, 0.96f, 0.85f, 0.30f);
                Flashlight.Init(_gameRoot, _cam, cone);
            }
            _worldRoot.SetActive(true);
            if (Current == null) EnterRoomImmediate(RoomId.Hallway, null);
        }

        public void HideWorld()
        {
            SetFade(0f);
            _worldRoot.SetActive(false);
            // NOTE: deliberately does NOT discard the live anomaly. HideWorld also runs for
            // overlay states (map, security room) - an overlay must not wipe an encounter.
            // Only ReturnToSecurity (leaving the patrol for good) discards it, unresolved.
        }

        public RoomInstance Room(RoomId id) => _rooms.TryGetValue(id, out var r) ? r : null;

        // ---- illumination queries (the ONLY place beam + ambient are combined) ----

        /// <summary>True when the room's ambient light is on (scripted flicker only - real ambient state).</summary>
        public bool RoomLit => Current != null && Current.Light != null && Current.Light.Lit;

        /// <summary>The current room's ambient darkness as one alpha - the level a light's
        /// penumbra fades back to at its rim (see LightController.AmbientAlpha).</summary>
        public float AmbientAlpha =>
            Current != null && Current.Light != null ? Current.Light.AmbientAlpha : LightController.DarkAlpha;

        /// <summary>True when the point is inside the handheld beam's cone (either live mode).</summary>
        public bool BeamCovers(Vector3 worldPoint) => Flashlight != null && Flashlight.ConeCovers(worldPoint);

        /// <summary>True when the point is inside the tight Focus cone - the truth beam:
        /// the only light that advances the repel meter and truth-checks hallucinations.</summary>
        public bool IsFocusLit(Vector3 worldPoint) =>
            Flashlight != null && Flashlight.Beam == LightMode.Focus && Flashlight.ConeCovers(worldPoint);

        public void Register(WanderingAnomaly a) { if (a != null && !_anomalies.Contains(a)) _anomalies.Add(a); }
        public void Unregister(WanderingAnomaly a) => _anomalies.Remove(a);

        public void EnterRoom(RoomId id)
        {
            if (id == RoomId.Security) { _gameRoot.ReturnToCheckpoint(); return; }
            if (Transitioning) return;
            StartCoroutine(TransitionRoutine(id));
        }

        private IEnumerator TransitionRoutine(RoomId id)
        {
            Transitioning = true;
            _gameRoot.Audio.PlaySfx("sfx_door");
            yield return FadeTo(1f, 0.28f);
            EnterRoomImmediate(id, Current);
            yield return FadeTo(0f, 0.34f);
            Transitioning = false;
        }

        private void EnterRoomImmediate(RoomId id, RoomInstance from)
        {
            if (!_rooms.TryGetValue(id, out var room) || room.Root == null)
            {
                Debug.LogWarning("[Views] room '" + id + "' missing - rebuilding.");
                room = _builder.BuildRoom(id);
            }
            if (room.Root == null)
            {
                Debug.LogError("[Views] room '" + id + "' could not be built - returning to security.");
                _gameRoot.ReturnToCheckpoint();
                return;
            }

            if (Current != null) Current.Root.SetActive(false);
            Current = room;
            room.Root.SetActive(true);
            _worldRoot.SetActive(true);
            if (room.Light != null) room.Light.ForceDarkOnEntry();   // you always arrive in darkness
            if (Flashlight != null) Flashlight.ForceBeamOffOnEntry(); // ...and your beam arrives off

            Vector3 spawn = room.PlayerSpawn;
            if (from != null)
            {
                foreach (var it in room.Interactables)
                {
                    var d = it as Doorway;
                    if (d != null && d.Target == from.Id) { spawn = new Vector3(d.AnchorX - 1.1f, 0, 0); break; }
                }
            }
            Player.PlaceInRoom(room, spawn);
            _gameRoot.Session.MarkRoomInvestigated(id);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.RoomEntered, id));
            _gameRoot.Audio.PlayMusic(GameRoot.MusicForRoom(id));
            _spawner.CoolDown(5f);
        }

        public void ReturnToSecurity()
        {
            if (Transitioning) return;
            StartCoroutine(ReturnRoutine());
        }

        private IEnumerator ReturnRoutine()
        {
            Transitioning = true;
            yield return FadeTo(1f, 0.28f);
            _spawner.DiscardLive();
            Current = null;
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.RoomExited, RoomId.Hallway));
            Transitioning = false;
            _gameRoot.ReturnToCheckpoint();
            yield return FadeTo(0f, 0.01f);
        }

        /// <summary>Night 5, after the DENY stamp: start the administrator hunt. Whatever was
        /// roaming is dismissed so the hunt is the only threat in the building.</summary>
        public void StartAdminHunt(HuntTuning tuning, int seed)
        {
            if (Hunt == null)
            {
                var go = new GameObject("AdminHunt");
                go.transform.SetParent(transform, false);
                Hunt = go.AddComponent<AdminHuntDirector>();
            }
            _spawner.DiscardLive();
            Hunt.Begin(_gameRoot, this, tuning, seed);
        }

        public void StopAdminHunt()
        {
            if (Hunt != null) Hunt.Stop();
        }

        public void NoteBreach() => _spawner.NoteBreach();

        public void SpawnAnomaly(bool hallucination) => _spawner.SpawnAnomaly(hallucination);

        /// <summary>Spawn a specific real behavior - used by the recovery return and by tests.</summary>
        public WanderingAnomaly SpawnAnomalyOfKind(AnomalyBehaviorKind kind, RoomId? roomId = null) =>
            _spawner.SpawnKind(kind, roomId);

        public void ScheduleReturn(AnomalyBehaviorKind kind) => _spawner.ScheduleReturn(kind);

        public void OnAnomalyGone(WanderingAnomaly a) => _spawner.OnAnomalyGone(a);

        // ---- classroom peek: frame the room from the doorway, reveal it with the cursor ----

        /// <summary>Frame a classroom for peeking: fade to black, swap rooms, hide the player,
        /// override the room light off and hand the reveal to PeekLight. Idempotent.</summary>
        public void EnterPeek(RoomId id)
        {
            if (_peeking || _peekExiting || Transitioning) return;
            _peeking = true;
            Transitioning = true;
            StartCoroutine(PeekTransitionRoutine(id));
        }

        /// <summary>Step back from a peek into the hallway and restore the normal patrol
        /// presentation. Idempotent.</summary>
        public void ExitPeek()
        {
            if (!_peeking || _peekExiting || Transitioning) return;
            _peekExiting = true;
            StartCoroutine(ExitPeekRoutine());
        }

        private IEnumerator PeekTransitionRoutine(RoomId id)
        {
            // headless parity: in the editor the rig's own Update forces the beam off on the
            // first peek frame; TickWorld skips the rig while peeking, so do it once here.
            if (Flashlight != null) Flashlight.Tick(0f);
            _gameRoot.Audio.PlaySfx("sfx_door");
            yield return FadeTo(1f, 0.28f);
            EnterRoomImmediate(id, Current);
            SetPlayerVisible(false);   // the guard stays in the hallway behind the peek
            if (Current != null && Current.Light != null) Current.Light.EnterPeek(true);
            EnsurePeekLight();
            _peekLight.Enter(Current, Hunt != null && Hunt.Running);   // the hunt starts the lamp ON
            _cam.transform.position = new Vector3(0f, 0f, -10f);   // frozen frame of the room
            yield return FadeTo(0f, 0.34f);
            Transitioning = false;
        }

        private IEnumerator ExitPeekRoutine()
        {
            yield return FadeTo(1f, 0.28f);
            var from = Current;
            if (_peekLight != null) _peekLight.Exit();
            if (from != null && from.Light != null) from.Light.EnterPeek(false);
            EnterRoomImmediate(RoomId.Hallway, from);
            SetPlayerVisible(true);
            if (Player != null) Player.ResetPrompt();
            _peeking = false;
            _peekExiting = false;
            _gameRoot.PeekRoom = null;
            _gameRoot.SetState(AppViewState.Exploration);
            yield return FadeTo(0f, 0.34f);
            Transitioning = false;
        }

        private void SetPlayerVisible(bool on)
        {
            if (Player != null)
            {
                var sr = Player.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = on;
            }
        }

        private void EnsurePeekLight()
        {
            if (_peekLight != null) return;
            var go = new GameObject("PeekLight");
            go.transform.SetParent(transform, false);
            _peekLight = go.AddComponent<PeekLight>();
            _peekLight.Init(_gameRoot, _cam);
        }

        private IEnumerator FadeTo(float target, float seconds)
        {
            float from = _fade.color.a;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                SetFade(Mathf.Lerp(from, target, Mathf.Clamp01(t / seconds)));
                yield return null;
            }
            SetFade(target);
        }

        private void SetFade(float a)
        {
            var c = _fade.color; c.a = a; _fade.color = c;
            _fade.raycastTarget = a > 0.9f;
        }

        private void LateUpdate()
        {
            if (_peeking) return;   // peek holds the camera frozen; no follow, no clamping
            if (!_worldRoot.activeSelf || Current == null || Player == null) return;
            float screenHalf = _cam.orthographicSize * _cam.aspect;
            float limit = Mathf.Max(0f, Current.HalfWidth - screenHalf + 0.4f);
            float x = Mathf.Clamp(Player.transform.position.x, -limit, limit);
            _cam.transform.position = new Vector3(x, 0, -10);
        }

        private void Update()
        {
            if (_gameRoot == null || Current == null || !_worldRoot.activeSelf || _gameRoot.Paused) return;
            if (_peeking) return;   // nothing spawns or resolves while the player only looks in
            _spawner.UpdateSpawner(Time.deltaTime);
        }

        // One deterministic headless frame (stub MonoBehaviours never get Update calls; in the
        // editor every component runs its own Update and this method is never invoked).
        internal void TickWorld(float dt)
        {
            if (_gameRoot == null || Current == null || !_worldRoot.activeSelf || _gameRoot.Paused) return;
            if (Hunt != null && Hunt.Running) Hunt.Tick(dt);
            // while peeking, the world holds its breath: only the cursor-locked light ticks
            if (_peeking)
            {
                if (_peekLight != null) _peekLight.Tick(dt);
                return;
            }
            if (Current.Light != null) Current.Light.Tick(dt);
            if (Player != null) Player.Tick(dt);
            if (Flashlight != null) Flashlight.Tick(dt);
            var snapshot = _anomalies.ToArray();
            foreach (var a in snapshot) if (a != null) a.Tick(dt);
            _spawner.UpdateSpawner(dt);
        }
    }
}
