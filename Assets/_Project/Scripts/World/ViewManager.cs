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


    // Rooms, doors, transitions, anomaly spawning, flashlight rig. Created by SchoolSceneBootstrap.
    public class ViewManager : MonoBehaviour
    {
        private GameRoot _gameRoot;
        private Camera _cam;
        private GameObject _worldRoot;
        private readonly Dictionary<RoomId, RoomInstance> _rooms = new Dictionary<RoomId, RoomInstance>();
        public RoomInstance Current { get; private set; }
        public PlayerController Player { get; private set; }
        public bool Transitioning { get; private set; }
        private RoomBuilder _builder;
        private RoomSpawner _spawner;
        public FlashlightRig Flashlight { get; private set; }
        private UnityEngine.UI.Image _fade;

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
            var fadeRt = UIFactoryLocal.NewRect(fadeGo);
            _fade = UIFactoryLocal.FullImage(fadeGo, Color.black);
            _fade.raycastTarget = false;
            SetFade(0f);

            BuildPlayer();
            _builder = new RoomBuilder(root, worldRoot, _rooms);
            _spawner = new RoomSpawner(root, this, null);
            _rooms[RoomId.Hallway] = _builder.BuildRoom(RoomId.Hallway);
            _rooms[RoomId.Classroom1] = _builder.BuildRoom(RoomId.Classroom1);
            _rooms[RoomId.Classroom2] = _builder.BuildRoom(RoomId.Classroom2);
        }

        private void BuildPlayer()
        {
            var go = new GameObject("Player");
            go.transform.SetParent(_worldRoot.transform, false);
            Player = go.AddComponent<PlayerController>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralArt.Get("custodian_0");
            sr.sortingOrder = 60; // above darkness overlay, lit by design (the custodian you play)
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
            _spawner.DiscardLive();
        }

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
                Debug.LogWarning("[Views] room '" + id + "' missing — rebuilding.");
                room = _builder.BuildRoom(id);
            }
            if (room.Root == null)
            {
                Debug.LogError("[Views] room '" + id + "' could not be built — returning to security.");
                _gameRoot.ReturnToCheckpoint();
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

        public void NoteBreach() => _spawner.NoteBreach();

        public void SpawnAnomaly(bool hallucination) => _spawner.SpawnAnomaly(hallucination);

        public void ScheduleReturn(AnomalyBehaviorKind kind) => _spawner.ScheduleReturn(kind);

        public void OnAnomalyGone(WanderingAnomaly a) => _spawner.OnAnomalyGone(a);

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
            if (_gameRoot == null || Current == null || !_worldRoot.activeSelf || _gameRoot.Paused) return;
            _spawner.UpdateSpawner(Time.deltaTime);
        }

        public bool IsIlluminated(Vector3 worldPoint)
        {
            if (Player == null) return false;
            if (Current != null && Current.Light != null && Current.Light.Mode == LightMode.Focus)
                return Player.ConeCovers(worldPoint);
            return false;
        }
    }

}
