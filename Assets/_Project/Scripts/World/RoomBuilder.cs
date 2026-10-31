using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // Owns every per-room sprite, door and clue for the patrol scene.
    // Called once by ViewManager.Init; holds no state of its own beyond the root it builds into.
    public class RoomBuilder
    {
        // The hallway's depth shroud sits under the darkness quad (70) but INSIDE the handheld
        // beam mask's custom sorting range (60..70), so one beam hole cuts both black layers.
        // It stays under the player (73) and the markers (75+), like the darkness does.
        private const int DepthOrder = 65;

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

        // Basic placeholder background: one wall band and one floor band. Simple, readable,
        // and trivial to swap for real room art later (replace with a single sprite GO).
        private static void PlaceholderBg(Transform parent, float floorY)
        {
            Quad(parent, "bg_wall", new Vector3(0f, floorY + 3.2f, 0f), 0,
                new Vector2(26f, 6.4f), new Color(0.13f, 0.13f, 0.14f));
            Quad(parent, "bg_floor", new Vector3(0f, floorY - 1.2f, 0f), 0,
                new Vector2(26f, 2.4f), new Color(0.07f, 0.07f, 0.075f));
        }

        // One flat placeholder quad. The solid_white sprite is exactly 1x1 world unit, so
        // `size` is in world units and equals the localScale - every placeholder in the patrol
        // scene (wall/floor bands, doors, darkness overlay, markers) is this one
        // call, which keeps the convention in a single place.
        public static GameObject Quad(Transform parent, string name, Vector3 pos, int order,
            Vector2 size, Color color)
        {
            var go = SpriteGO(parent, name, ProceduralArt.Get("solid_white"), pos, order);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.GetComponent<SpriteRenderer>().color = color;
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
                    room.HalfWidth = HallwayDarkness.HallwayHalfWidth;   // pure module owns it: the
                    PlaceholderBg(root.transform, floorY);               // depth shroud is sized from it
                    AddDoor(room, "Security Door", -10.6f, floorY, RoomId.Security, "prop_door_security");
                    AddDoor(room, "Classroom 1", RoomGraph.DoorX(RoomId.Hallway, RoomId.Classroom1), floorY, RoomId.Classroom1, "prop_door");
                    AddDoor(room, "Classroom 2", RoomGraph.DoorX(RoomId.Hallway, RoomId.Classroom2), floorY, RoomId.Classroom2, "prop_door");
                    var serverSprite = ProceduralArt.Get("prop_server_door");
                    float serverH = serverSprite.rect.height / Mathf.Max(1f, serverSprite.pixelsPerUnit);
                    SpriteGO(root.transform, "server_door_art", serverSprite,
                        new Vector3(10.4f, floorY + serverH / 2f, 0), 10);
                    AddClue(room, "light", -7.0f, floorY, "Flickering Light (ceiling fixture)", "prop_lightfixture", 1.5f);
                    AddClue(room, "door", 10.0f, floorY, "Sealed Door");
                    AddClue(room, "board", 0.6f, floorY, "Notice Board", "prop_noticeboard", 1.0f);
                    AddClue(room, "photo", 7.2f, floorY, "Old School Photo", "prop_photoframe", 1.1f);
                    AddClue(room, "clock", 2.6f, floorY, "Wall Clock", "prop_clock", 1.6f);
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
                    PlaceholderBg(root.transform, floorY);
                    AddDoor(room, "Hallway", RoomGraph.DoorX(RoomId.Classroom1, RoomId.Hallway), floorY, RoomId.Hallway, "prop_door");
                    AddClue(room, "chalkboard", -6.2f, floorY, "Chalkboard", "prop_chalkboard", 1.2f);
                    AddClue(room, "desk", -7.6f, floorY, "Front Desk");
                    AddClue(room, "lostid", -5.0f, floorY, "Lost-and-Found Shelf", "prop_lostid", 0.2f);
                    SpriteGO(root.transform, "deskprop", ProceduralArt.Get("prop_desk"), new Vector3(-7.6f, floorY + 1f, 0), 9);
                    room.PlayerSpawn = new Vector3(4.0f, 0, 0);
                    room.AnomalySpawnPoints.Add(new Vector3(-6.5f, 0, 0));
                    room.AnomalySpawnPoints.Add(new Vector3(1.5f, 0, 0));
                    break;
                }
                default: // Classroom2 - the server room adjoins it (every room now starts dark)
                {
                    room.DisplayName = "CLASSROOM 2";
                    room.HalfWidth = 8.2f;
                    PlaceholderBg(root.transform, floorY);
                    AddDoor(room, "Hallway", RoomGraph.DoorX(RoomId.Classroom2, RoomId.Hallway), floorY, RoomId.Hallway, "prop_door");
                    AddClue(room, "window", -6.6f, floorY, "Classroom Window", "prop_window", 1.2f);
                    AddClue(room, "cabinet", 7.4f, floorY, "Filing Cabinet");
                    SpriteGO(root.transform, "cabinetprop", ProceduralArt.Get("prop_cabinet"), new Vector3(7.6f, floorY + 2.1f, 0), 9);
                    room.PlayerSpawn = new Vector3(4.0f, 0, 0);
                    room.AnomalySpawnPoints.Add(new Vector3(-5.5f, 0, 0));
                    room.AnomalySpawnPoints.Add(new Vector3(2.5f, 0, 0));
                    break;
                }
            }

            // Ambient dim layer: BLACK at the LightController's alpha (0.12 lit .. 0.62 dark,
            // 1 while peeking). It draws only outside sprite masks, so the peek lamp punches
            // a hole in it. A white tint here would wash rooms out instead of darkening them.
            var dark = Quad(root.transform, "darkness", Vector3.zero, 70,
                new Vector2(room.HalfWidth * 2f + 4f, HallwayDarkness.RoomDarkHalfHeight * 2f), Color.black);

            // The corridor alone is deeper than the rooms AND only reveals a pool around the
            // custodian: one extra black layer whose alpha is a radial falloff baked in world
            // units (ProceduralArt "hall_depth" <- HallwayDarkness), following the player, drawn
            // under the darkness quad and INSIDE the beam mask's sorting range so the handheld
            // beam cuts through both. Classrooms keep the single uniform layer they always had.
            SpriteRenderer depth = null;
            float darkAlpha = LightController.DarkAlpha;
            float depthAlpha = HallwayDarkness.DepthAlpha;
            if (id == RoomId.Hallway)
            {
                var cfg = _root != null ? _root.Config : null;
                darkAlpha = cfg != null ? cfg.hallwayDarkAlpha : HallwayDarkness.BaseAlpha;
                depthAlpha = cfg != null ? cfg.hallwayDepthAlpha : HallwayDarkness.DepthAlpha;
                var shroud = SpriteGO(root.transform, "depth_darkness", ProceduralArt.Get("hall_depth"),
                    new Vector3(room.PlayerSpawn.x, HallwayDarkness.PlayerY, 0f), DepthOrder);
                depth = shroud.GetComponent<SpriteRenderer>();
                depth.color = new Color(0f, 0f, 0f, depthAlpha);
                depth.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
                room.Depth = depth;
            }

            var lightCtl = root.AddComponent<LightController>();
            lightCtl.Init(_root, room, dark.GetComponent<SpriteRenderer>(),
                startLit: false,   // every room is entered dark; the flashlight is the only light
                depthShroud: depth, darkAlpha: darkAlpha, depthAlpha: depthAlpha);
            room.Light = lightCtl;
            room.Darkness = dark.GetComponent<SpriteRenderer>();
            room.Darkness.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

            _rooms[id] = room;
            root.SetActive(false);
            return room;
        }

        private void AddDoor(RoomInstance room, string label, float x, float floorY, RoomId target, string spriteKey)
        {
            // The door's art key always stood in the signature; the flat slab was only there
            // until art existed. Draw the sprite standing ON the floor band, sized from its own
            // pixels-per-unit, so dropping a real door PNG at Resources/Art/<key>.png resizes
            // and replaces it with no code change. (Null sprite keeps the old slab.)
            var sprite = ProceduralArt.Get(spriteKey);
            GameObject go;
            if (sprite != null)
            {
                float h = sprite.rect.height / Mathf.Max(1f, sprite.pixelsPerUnit);
                go = SpriteGO(room.Root.transform, "door_" + target, sprite,
                    new Vector3(x, floorY + h / 2f, 0), 10);
            }
            else
            {
                go = Quad(room.Root.transform, "door_" + target, new Vector3(x, floorY + 1.7f, 0), 10,
                    new Vector2(1.7f, 3.4f), new Color(0.20f, 0.20f, 0.22f));
            }
            var d = go.AddComponent<Doorway>();
            d.Init(_root, room, label, target, x);
            room.Interactables.Add(d);
        }

        // The clue anchor (chest height, what the interact test uses) stays where it was; the
        // optional sprite hangs off it as a child so each GDD interaction object is actually
        // SEEN in the room. A dropped-in Resources/Art/<key>.png replaces the placeholder and
        // resizes itself from its own pixels-per-unit - no call-site changes.
        private void AddClue(RoomInstance room, string clueKey, float x, float floorY, string label,
            string spriteKey = null, float mountY = 0.8f)
        {
            var go = new GameObject("clue_" + clueKey);
            go.transform.SetParent(room.Root.transform, false);
            go.transform.localPosition = new Vector3(x, floorY + 1.4f, 0);
            if (spriteKey != null)
            {
                var sprite = ProceduralArt.Get(spriteKey);
                if (sprite != null)
                    SpriteGO(go.transform, "art_" + spriteKey, sprite, new Vector3(0f, mountY, 0f), 9);
            }
            var co = go.AddComponent<ClueObject>();
            co.Init(_root, room, clueKey, label, x);
            room.Interactables.Add(co);
        }
    }
}
