using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // Owns every per-room sprite, door, switch and clue for the patrol scene.
    // Called once by ViewManager.Init; holds no state of its own beyond the root it builds into.
    public class RoomBuilder
    {
        private readonly GameRoot _root;
        private readonly GameObject _worldRoot;
        private readonly Dictionary<RoomId, RoomInstance> _rooms;

        public RoomBuilder(GameRoot root, GameObject worldRoot, Dictionary<RoomId, RoomInstance> rooms)
        {
            _root = root;
            _worldRoot = worldRoot;
            _rooms = rooms;
        }

        public static GameObject SpriteGO(Transform parent, string name, Sprite sprite,
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

        public RoomInstance BuildRoom(RoomId id)
        {
            var room = new RoomInstance { Id = id };
            var root = new GameObject("Room_" + id);
            root.transform.SetParent(_worldRoot.transform, false);
            room.Root = root;

            float floorY = RoomInstance.FloorY; // one shared floor line constant
            switch (id)
            {
                case RoomId.Hallway:
                {
                    room.DisplayName = "MAIN HALLWAY";
                    room.HalfWidth = 11.4f;
                    SpriteGO(root.transform, "bg", ProceduralArt.Get("hallway_bg"), Vector3.zero, 0);
                    AddDoor(room, "Security Door", -10.6f, floorY, RoomId.Security, "prop_door_security");
                    AddDoor(room, "Classroom 1", RoomGraph.DoorX(RoomId.Hallway, RoomId.Classroom1), floorY, RoomId.Classroom1, "prop_door");
                    AddDoor(room, "Classroom 2", RoomGraph.DoorX(RoomId.Hallway, RoomId.Classroom2), floorY, RoomId.Classroom2, "prop_door");
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
                    AddDoor(room, "Hallway", RoomGraph.DoorX(RoomId.Classroom1, RoomId.Hallway), floorY, RoomId.Hallway, "prop_door");
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
                    AddDoor(room, "Hallway", RoomGraph.DoorX(RoomId.Classroom2, RoomId.Hallway), floorY, RoomId.Hallway, "prop_door");
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
            lightCtl.Init(_root, room, dark.GetComponent<SpriteRenderer>(),
                startLit: id != RoomId.Classroom2);   // classroom 2 is the dark room
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
            d.Init(_root, room, label, target, x);
            room.Interactables.Add(d);
        }

        private void AddSecurityDoor(RoomInstance room, float x, float floorY)
        {
            var go = SpriteGO(room.Root.transform, "door_security_office", ProceduralArt.Get("prop_door_security"),
                new Vector3(x, floorY + 3f, 0), 10);
            var d = go.AddComponent<SecurityDoorInteractable>();
            d.Init(_root, room, x);
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
            sw.Init(_root, room, x);
            room.Interactables.Add(sw);
        }

        private void AddClue(RoomInstance room, string clueKey, float x, float floorY, string label)
        {
            var go = new GameObject("clue_" + clueKey);
            go.transform.SetParent(room.Root.transform, false);
            go.transform.localPosition = new Vector3(x, floorY + 1.4f, 0);
            var co = go.AddComponent<ClueObject>();
            co.Init(_root, room, clueKey, label, x);
            room.Interactables.Add(co);
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
