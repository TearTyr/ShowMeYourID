using System;
using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    public class RoomInstance
    {
        public RoomId Id;
        public GameObject Root;
        public float HalfWidth;
        public LightController Light;
        public SpriteRenderer Darkness;
        public List<Interactable> Interactables = new List<Interactable>();
        public List<Vector3> AnomalySpawnPoints = new List<Vector3>();
        public Vector3 PlayerSpawn;
        public string DisplayName;
    }

    public class ViewManager : MonoBehaviour
    {
        private GameRoot R;
        private Camera _cam;
        private GameObject _worldRoot;
        private readonly Dictionary<RoomId, RoomInstance> _rooms = new Dictionary<RoomId, RoomInstance>();
        public RoomInstance Current { get; private set; }
        public PlayerController Player { get; private set; }
        public bool Transitioning { get; private set; }
        private Hollowbrook.Logic.GameRng _patrolRng;
        public FlashlightRig Flashlight { get; private set; }
        private int _anomaliesInside; // anomalies let through the checkpoint heighten the school
        private float _spawnCooldown = 6f;
        private WanderingAnomaly _liveAnomaly;
        private UnityEngine.UI.Image _fade;

        public void Init(GameRoot root, Camera cam, GameObject worldRoot)
        {
            R = root;
            _cam = cam;
            _worldRoot = worldRoot;

            // dedicated fade canvas (between FX and UI layers; parented to the scene root so
            var fadeGo = new GameObject("TransitionFade", typeof(RectTransform));
            var fadeCanvas = fadeGo.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 8;
            fadeGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var fadeRt = UIFactoryLocal.NewRect(fadeGo);
            _fade = UIFactoryLocal.FullImage(fadeGo, Color.black);
            _fade.raycastTarget = false;
            SetFade(0f);

            BuildPlayer();
            BuildRoom(RoomId.Hallway);
            BuildRoom(RoomId.Classroom1);
            BuildRoom(RoomId.Classroom2);
        }

        private static GameObject SpriteGO(Transform parent, string name, Sprite sprite,
            Vector3 pos, int order, float alpha = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = new Color(1f, 1f, 1f, alpha);
            return go;
        }

        private void BuildPlayer()
        {
            var go = new GameObject("Player");
            go.transform.SetParent(_worldRoot.transform, false);
            Player = go.AddComponent<PlayerController>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralArt.Get("custodian_0");
            sr.sortingOrder = 60; // above darkness overlay, lit by design (the custodian you play)
            Player.Init(R, sr);
        }

        private RoomInstance BuildRoom(RoomId id)
        {
            var room = new RoomInstance { Id = id };
            var root = new GameObject("Room_" + id);
            root.transform.SetParent(_worldRoot.transform, false);
            room.Root = root;

            float floorY = -2.4f; // floor line across all rooms (bg art shares it)
            switch (id)
            {
                case RoomId.Hallway:
                {
                    room.DisplayName = "MAIN HALLWAY";
                    room.HalfWidth = 11.4f;
                    SpriteGO(root.transform, "bg", ProceduralArt.Get("hallway_bg"), Vector3.zero, 0);
                    AddDoor(room, "Security Door", -10.6f, floorY, RoomId.Security, "prop_door_security");
                    AddDoor(room, "Classroom 1", -4.0f, floorY, RoomId.Classroom1, "prop_door");
                    AddDoor(room, "Classroom 2", 4.6f, floorY, RoomId.Classroom2, "prop_door");
                    SpriteGO(root.transform, "server_door_art", ProceduralArt.Get("prop_server_door"),
                        new Vector3(10.4f, floorY + 3f, 0), 10);
                    AddLightSwitch(room, -9.2f, floorY);
                    AddClue(room, "light", -7.0f, floorY, "Flickering Light (switch panel)");
                    AddClue(room, "door", 10.0f, floorY, "Sealed Door");
                    AddClue(room, "board", 0.6f, floorY, "Notice Board");
                    AddClue(room, "photo", 7.2f, floorY, "Old School Photo");
                    AddClue(room, "clock", 2.6f, floorY, "Wall Clock");
                    SpriteGO(root.transform, "lockers", ProceduralArt.Get("prop_lockers"), new Vector3(-1.4f, floorY + 2.5f, 0), 8);
                    SpriteGO(root.transform, "lockers2", ProceduralArt.Get("prop_lockers"), new Vector3(7.8f, floorY + 2.5f, 0), 8);
                    room.PlayerSpawn = new Vector3(-9.4f, 0, 0);
                    room.AnomalySpawnPoints.Add(new Vector3(9.5f, 0, 0));
                    room.AnomalySpawnPoints.Add(new Vector3(-2f, 0, 0));
                    room.AnomalySpawnPoints.Add(new Vector3(5.5f, 0, 0));
                    break;
                }
                case RoomId.Classroom1:
                {
                    room.DisplayName = "CLASSROOM 1";
                    room.HalfWidth = 8.2f;
                    SpriteGO(root.transform, "bg", ProceduralArt.Get("classroom1_bg"), Vector3.zero, 0);
                    AddDoor(room, "Hallway", 4.9f, floorY, RoomId.Hallway, "prop_door");
                    AddLightSwitch(room, 6.4f, floorY);
                    AddClue(room, "chalkboard", -6.2f, floorY, "Chalkboard");
                    AddClue(room, "desk", -7.6f, floorY, "Front Desk");
                    AddClue(room, "lostid", -5.0f, floorY, "Lost-and-Found Shelf");
                    AddSecurityDoor(room, -1.6f, floorY);
                    SpriteGO(root.transform, "deskprop", ProceduralArt.Get("prop_desk"), new Vector3(-7.6f, floorY + 1f, 0), 9);
                    room.PlayerSpawn = new Vector3(4.0f, 0, 0);
                    room.AnomalySpawnPoints.Add(new Vector3(-6.5f, 0, 0));
                    room.AnomalySpawnPoints.Add(new Vector3(1.5f, 0, 0));
                    break;
                }
                default: // Classroom2 — the darker room (server room adjoins it)
                {
                    room.DisplayName = "CLASSROOM 2";
                    room.HalfWidth = 8.2f;
                    SpriteGO(root.transform, "bg", ProceduralArt.Get("classroom2_bg"), Vector3.zero, 0);
                    AddDoor(room, "Hallway", 4.9f, floorY, RoomId.Hallway, "prop_door");
                    AddLightSwitch(room, 6.4f, floorY);
                    AddClue(room, "window", -6.6f, floorY, "Classroom Window");
                    AddClue(room, "cabinet", 7.4f, floorY, "Filing Cabinet");
                    SpriteGO(root.transform, "cabinetprop", ProceduralArt.Get("prop_cabinet"), new Vector3(7.6f, floorY + 2.1f, 0), 9);
                    room.PlayerSpawn = new Vector3(4.0f, 0, 0);
                    room.AnomalySpawnPoints.Add(new Vector3(-5.5f, 0, 0));
                    room.AnomalySpawnPoints.Add(new Vector3(2.5f, 0, 0));
                    break;
                }
            }

            var dark = SpriteGO(root.transform, "darkness", ProceduralArt.Get("solid_white"),
                new Vector3(0, 0, 0), 70);
            dark.transform.localScale = new Vector3(room.HalfWidth * 2f + 4f, 11f, 1f);
            var lightCtl = root.AddComponent<LightController>();
            lightCtl.Init(R, room, dark.GetComponent<SpriteRenderer>(),
                id == RoomId.Classroom2 ? LightMode.Off : LightMode.Sweep);
            room.Light = lightCtl;
            room.Darkness = dark.GetComponent<SpriteRenderer>();
            room.Darkness.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

            _rooms[id] = room;
            root.SetActive(false);
            return room;
        }

        private void AddDoor(RoomInstance room, string label, float x, float floorY, RoomId target, string spriteKey)
        {
            var go = SpriteGO(room.Root.transform, "door_" + target, ProceduralArt.Get(spriteKey),
                new Vector3(x, floorY + 3f, 0), 10);
            var d = go.AddComponent<Doorway>();
            d.Init(R, room, label, target, x);
            room.Interactables.Add(d);
        }

        private void AddSecurityDoor(RoomInstance room, float x, float floorY)
        {
            var go = SpriteGO(room.Root.transform, "door_security_office", ProceduralArt.Get("prop_door_security"),
                new Vector3(x, floorY + 3f, 0), 10);
            var d = go.AddComponent<SecurityDoorInteractable>();
            d.Init(R, room, x);
            room.Interactables.Add(d);
        }

        private void AddLightSwitch(RoomInstance room, float x, float floorY)
        {
            var go = new GameObject("lightswitch");
            go.transform.SetParent(room.Root.transform, false);
            go.transform.localPosition = new Vector3(x, floorY + 2.1f, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralArt.Get("solid_white");
            sr.color = new Color(0.8f, 0.8f, 0.78f);
            sr.sortingOrder = 75;
            go.transform.localScale = new Vector3(0.16f, 0.26f, 1f);
            var sw = go.AddComponent<LightSwitchObject>();
            sw.Init(R, room, x);
            room.Interactables.Add(sw);
        }

        private void AddClue(RoomInstance room, string clueKey, float x, float floorY, string label)
        {
            var go = new GameObject("clue_" + clueKey);
            go.transform.SetParent(room.Root.transform, false);
            go.transform.localPosition = new Vector3(x, floorY + 1.4f, 0);
            var co = go.AddComponent<ClueObject>();
            co.Init(R, room, clueKey, label, x);
            room.Interactables.Add(co);
        }

        public void ShowWorld()
        {
            if (Flashlight == null && _cam != null)
            {
                var rigGo = new GameObject("FlashlightRig");
                rigGo.transform.SetParent(transform, false);
                Flashlight = rigGo.AddComponent<FlashlightRig>();
                var coneGo = SpriteGO(rigGo.transform, "beam", ProceduralArt.Get("light_cone"),
                    new Vector3(0, 0, -0.5f), 71);
                var cone = coneGo.GetComponent<SpriteRenderer>();
                cone.color = new Color(1f, 0.96f, 0.85f, 0.30f);
                Flashlight.Init(R, _cam, cone);
            }
            _worldRoot.SetActive(true);
            if (Current == null) EnterRoomImmediate(RoomId.Hallway, null);
        }

        public void HideWorld()
        {
            SetFade(0f);
            _worldRoot.SetActive(false);
            if (_liveAnomaly != null) { Destroy(_liveAnomaly.gameObject); _liveAnomaly = null; }
        }

        public void EnterRoom(RoomId id)
        {
            if (id == RoomId.Security) { R.ReturnToCheckpoint(); return; }
            if (Transitioning) return;
            StartCoroutine(TransitionRoutine(id));
        }

        private IEnumerator TransitionRoutine(RoomId id)
        {
            Transitioning = true;
            R.Audio.PlaySfx("sfx_door");
            yield return FadeTo(1f, 0.28f);
            EnterRoomImmediate(id, Current);
            yield return FadeTo(0f, 0.34f);
            Transitioning = false;
        }

        private void EnterRoomImmediate(RoomId id, RoomInstance from)
        {
            if (!_rooms.TryGetValue(id, out var room) || room.Root == null)
            {
                Debug.LogWarning("[Views] room '" + id + "' missing — rebuilding.");
                room = BuildRoom(id);
            }
            if (room.Root == null)
            {
                Debug.LogError("[Views] room '" + id + "' could not be built — returning to security.");
                R.ReturnToCheckpoint();
                return;
            }

            if (Current != null) Current.Root.SetActive(false);
            Current = room;
            room.Root.SetActive(true);
            _worldRoot.SetActive(true);

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
            R.Session.MarkRoomInvestigated(id);
            R.Bus.Publish(GameEvent.Of(EventIds.RoomEntered, id));
            R.Audio.PlayMusic(GameRoot.MusicForRoom(id));
            _spawnCooldown = 5f;
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
            if (_liveAnomaly != null) { Destroy(_liveAnomaly.gameObject); _liveAnomaly = null; }
            Current = null;
            R.Bus.Publish(GameEvent.Of(EventIds.RoomExited, RoomId.Hallway));
            Transitioning = false;
            R.ReturnToCheckpoint();
            yield return FadeTo(0f, 0.01f);
        }

        public void NoteBreach() => _anomaliesInside++;

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
            if (!_worldRoot.activeSelf || Current == null || Player == null) return;
            float screenHalf = _cam.orthographicSize * _cam.aspect;
            float limit = Mathf.Max(0f, Current.HalfWidth - screenHalf + 0.4f);
            float x = Mathf.Clamp(Player.transform.position.x, -limit, limit);
            _cam.transform.position = new Vector3(x, 0, -10);
        }

        private void Update()
        {
            if (R == null || Current == null || !_worldRoot.activeSelf || R.Paused) return;
            UpdateSpawner(Time.deltaTime);
        }

        private void UpdateSpawner(float dt)
        {
            int shift = R.Session.Mode == GameMode.Normal ? R.Session.CurrentShift : 3;
            if (R.Session.Mode == GameMode.Normal && shift < 2) return; // anomalies wander from shift 2
            if (_liveAnomaly != null) return;
            if (R.Dialogue != null && R.Dialogue.IsActive) { _spawnCooldown = Mathf.Max(_spawnCooldown, 2f); return; }

            _spawnCooldown -= dt;
            if (_spawnCooldown > 0f) return;

            float chance = R.Config.anomalySpawnChancePerSecond * (0.6f + 0.2f * shift) *
                           (1f + 0.35f * _anomaliesInside + 0.3f * (int)R.Session.Breaches) *
                           (Current.Light != null && Current.Light.Mode == LightMode.Off ? 1.6f : 1f);
            _spawnCooldown = 4f; // re-roll interval; chance is per-second so scale by 4
            if (UnityEngine.Random.value > chance * 4f) return;

            SpawnAnomaly(false);
        }

        public void SpawnAnomaly(bool hallucination)
        {
            if (Current == null) return;
            if (!hallucination && _liveAnomaly != null) return;
            var kinds = new[] { AnomalyBehaviorKind.Blink, AnomalyBehaviorKind.Slide,
                                AnomalyBehaviorKind.Sprint, AnomalyBehaviorKind.CoverHug };
            if (_patrolRng == null) _patrolRng = new Hollowbrook.Logic.GameRng(R.Session.Seed + 31 + R.Session.CurrentShift);
            var kind = hallucination
                ? (_patrolRng.Chance(0.5f) ? AnomalyBehaviorKind.Slide : AnomalyBehaviorKind.Blink)
                : R.Session.Pool.PeekKind(_patrolRng);   // shared pool: uncaught == outstanding
            if (hallucination && kind == AnomalyBehaviorKind.Blink) kind = AnomalyBehaviorKind.Slide; // phantoms never grab
            var spawn = Current.AnomalySpawnPoints.Count > 0
                ? Current.AnomalySpawnPoints[_patrolRng.NextInt(0, Current.AnomalySpawnPoints.Count)]
                : new Vector3(Current.HalfWidth - 1.5f, 0, 0);
            if (Mathf.Abs(spawn.x - Player.transform.position.x) < 4f) spawn.x = -spawn.x;

            var go = new GameObject(hallucination ? "Phantom" : "Anomaly_" + kind);
            go.transform.SetParent(Current.Root.transform, false);
            var an = go.AddComponent<WanderingAnomaly>();
            an.Init(R, Current, kind, spawn, hallucination);
            if (hallucination) return;
            _liveAnomaly = an;
            R.Bus.Publish(GameEvent.Of(EventIds.AnomalySighted, kind));
        }

        public void ScheduleReturn(AnomalyBehaviorKind kind)
        {
            StartCoroutine(ReturnRoutine(kind));
        }

        private System.Collections.IEnumerator ReturnRoutine(AnomalyBehaviorKind kind)
        {
            yield return new WaitForSeconds(EncounterTuning.Default.RecoverSeconds);
            if (R != null && !R.Paused && Current != null) SpawnAnomaly(false);
        }

        public void OnAnomalyGone(WanderingAnomaly a)
        {
            if (_liveAnomaly == a) _liveAnomaly = null;
            if (!a.IsHallucination && (a.WasRevealed || a.WasCaught))
                R.Session.Pool.Resolve(a.Kind);
        }

        public bool IsIlluminated(Vector3 worldPoint)
        {
            if (Player == null) return false;
            if (Current != null && Current.Light != null && Current.Light.Mode == LightMode.Focus)
                return Player.ConeCovers(worldPoint);
            return false;
        }
    }

    internal static class UIFactoryLocal
    {
        public static RectTransform NewRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static UnityEngine.UI.Image FullImage(GameObject go, Color c)
        {
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = c;
            NewRect(go);
            return img;
        }
    }
}
