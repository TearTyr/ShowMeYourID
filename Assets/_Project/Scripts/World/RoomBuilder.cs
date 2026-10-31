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
        private const int DepthOrder = WorldLayers.Darkness;

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

        // The comfort room is dimmed far more than the other rooms: only the flashlight shows anything.

        public RoomInstance BuildRoom(RoomId id)
        {
            var room = new RoomInstance { Id = id };
            var root = new GameObject("Room_" + id);
            root.transform.SetParent(_worldRoot.transform, false);
            room.Root = root;

            float floorY = RoomInstance.FloorY; // one shared floor line constant
            float darkAlpha = LightController.DarkAlpha;   // ambient darkness; the comfort room sets its own
            switch (id)
            {
                case RoomId.Hallway:
                {
                    room.DisplayName = "MAIN HALLWAY";
                    room.HalfWidth = HallwayDarkness.HallwayHalfWidth;   // pure module owns it: the
                    // the painted corridor already holds the classroom doors, lockers, notice board and
                    // flyer, so those are not drawn a second time (the doors stay as invisible anchors)
                    bool painted = AddHallwayBackdrop(room, root.transform);
                    if (!painted) PlaceholderBg(root.transform, floorY);     // depth shroud is sized from it
                    AddDoor(room, "Security Door", -13.8f, floorY, RoomId.Security, "prop_door_security");
                    AddDoor(room, "Classroom 1", RoomGraph.DoorX(RoomId.Hallway, RoomId.Classroom1), floorY, RoomId.Classroom1, "prop_door", !painted);
                    AddDoor(room, "Classroom 2", RoomGraph.DoorX(RoomId.Hallway, RoomId.Classroom2), floorY, RoomId.Classroom2, "prop_door", !painted);
                    AddDoor(room, "Comfort Room", RoomGraph.ComfortDoorX, floorY, RoomId.ComfortRoom, "prop_door_comfort");
                    var serverSprite = ProceduralArt.Get("prop_server_door");
                    float serverH = serverSprite.rect.height / Mathf.Max(1f, serverSprite.pixelsPerUnit);
                    SpriteGO(root.transform, "server_door_art", serverSprite,
                        new Vector3(13.2f, floorY + serverH / 2f, 0), 10);
                    // clue anchors sit on the painted things (fixture, notice board, flyer); the wall clock
                    // is not in the painting, so it hangs between the notice board and the second door
                    AddClue(room, "light", -12.9f, floorY, "Flickering Light (ceiling fixture)", painted ? null : "prop_lightfixture", 1.5f);
                    AddClue(room, "door", 13.0f, floorY, "Sealed Door");
                    AddClue(room, "board", -4.5f, floorY, "Notice Board", painted ? null : "prop_noticeboard", 1.0f);
                    AddClue(room, "photo", 3.0f, floorY, "Old School Photo", painted ? null : "prop_photoframe", 1.1f);
                    AddClue(room, "clock", -1.8f, floorY, "Wall Clock", "prop_clock", 2.6f);
                    AddExamine(room, "Patrol Clipboard", -11.0f, floorY, "Tonight's round sheet. Every line above yours is signed by a custodian the school has no record of.", "inspect_clipboard", "prop_clipboard", 2.4f);
                    AddExamine(room, "Light Switch", -6.9f, floorY, "You flip it twice. Nothing. Someone has cut the power to this hallway.", "inspect_light_switch", "prop_light_switch", 2.0f);
                    AddExamine(room, "Lockers", 5.6f, floorY, "Deep scratches around the handles. They were made from the inside.", "inspect_lockers");
                    AddExamine(room, "Open Locker", 8.4f, floorY, "This one is empty except for a school ID with the photo cut out.", "inspect_lockers_open");
                    if (!painted)
                    {
                        SpriteGO(root.transform, "lockers", ProceduralArt.Get("prop_lockers"), new Vector3(-1.4f, floorY + 2.5f, 0), 8);
                        SpriteGO(root.transform, "lockers2", ProceduralArt.Get("prop_lockers"), new Vector3(7.8f, floorY + 2.5f, 0), 8);
                    }
                    room.PlayerSpawn = new Vector3(-12.6f, 0, 0);
                    room.AnomalySpawnPoints.Add(new Vector3(12.0f, 0, 0));
                    room.AnomalySpawnPoints.Add(new Vector3(-6.5f, 0, 0));
                    room.AnomalySpawnPoints.Add(new Vector3(3.0f, 0, 0));
                    break;
                }
                case RoomId.Classroom1:
                {
                    room.DisplayName = "CLASSROOM 1";
                    room.HalfWidth = 8.2f;
                    bool painted = AddRoomBackdrop(root.transform, "bg_classroom_1");
                    if (!painted) PlaceholderBg(root.transform, floorY);
                    // the painted room has its own door, chalkboard, desk and notice board: the
                    // anchors sit on them (x, then the height the lamp has to point at)
                    AddDoor(room, "Hallway", RoomGraph.DoorX(RoomId.Classroom1, RoomId.Hallway), floorY, RoomId.Hallway, "prop_door", !painted, 1.8f);
                    AddClue(room, "chalkboard", 1.5f, floorY, "Chalkboard", painted ? null : "prop_chalkboard", 1.2f, 2.2f);
                    AddClue(room, "desk", 1.7f, floorY, "Front Desk", null, 0.8f, -0.6f);
                    AddClue(room, "lostid", 6.0f, floorY, "Lost-and-Found Shelf", painted ? null : "prop_lostid", 0.2f, 2.1f);
                    if (!painted)
                        SpriteGO(root.transform, "deskprop", ProceduralArt.Get("prop_desk"), new Vector3(-7.6f, floorY + 1f, 0), 9);
                    room.PlayerSpawn = new Vector3(4.0f, 0, 0);
                    room.AnomalySpawnPoints.Add(new Vector3(-6.5f, 0, 0));
                    room.AnomalySpawnPoints.Add(new Vector3(1.5f, 0, 0));
                    break;
                }
                case RoomId.ComfortRoom:
                {
                    room.DisplayName = "COMFORT ROOM";
                    room.HalfWidth = 8.2f;
                    bool painted = AddRoomBackdrop(root.transform, "bg_comfort_room");
                    if (!painted) PlaceholderBg(root.transform, floorY);
                    // peeked into like the classrooms; the picture has no door, so the way back is an invisible anchor
                    AddDoor(room, "Hallway", RoomGraph.DoorX(RoomId.ComfortRoom, RoomId.Hallway), floorY, RoomId.Hallway, "prop_door", false, -0.3f);
                    AddExamine(room, "Mirror", 5.9f, floorY, "It shows the row of stalls behind you. One of the doors is open. You did not hear it open.", null, null, 0f, 1.5f);
                    AddExamine(room, "Sinks", 4.4f, floorY, "All four taps are dry, but the basins are wet.", null, null, 0f, -2.4f);
                    AddExamine(room, "Stalls", -7.0f, floorY, "Every door is latched from the inside. Nobody answers when you knock.", null, null, 0f, -0.2f);
                    room.PlayerSpawn = new Vector3(0f, 0, 0);
                    break;
                }
                default: // Classroom2 - the server room adjoins it (every room now starts dark)
                {
                    room.DisplayName = "CLASSROOM 2";
                    room.HalfWidth = 8.2f;
                    bool painted = AddRoomBackdrop(root.transform, "bg_classroom_2");
                    if (!painted) PlaceholderBg(root.transform, floorY);
                    AddDoor(room, "Hallway", RoomGraph.DoorX(RoomId.Classroom2, RoomId.Hallway), floorY, RoomId.Hallway, "prop_door", !painted, 1.8f);
                    AddClue(room, "window", -6.8f, floorY, "Classroom Window", painted ? null : "prop_window", 1.2f, 2.3f);
                    AddClue(room, "cabinet", -3.1f, floorY, "Filing Cabinet", null, 0.8f, 0.4f);
                    if (!painted)
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
            var dark = Quad(root.transform, "darkness", Vector3.zero, WorldLayers.Darkness,
                new Vector2(room.HalfWidth * 2f + 4f, HallwayDarkness.RoomDarkHalfHeight * 2f), Color.black);

            // The corridor alone is deeper than the rooms AND only reveals a pool around the
            // custodian: one extra black layer whose alpha is a radial falloff baked in world
            // units (ProceduralArt "hall_depth" <- HallwayDarkness), following the player, drawn
            // under the darkness quad and INSIDE the beam mask's sorting range so the handheld
            // beam cuts through both. Classrooms keep the single uniform layer they always had.
            SpriteRenderer depth = null;
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

        // The painted hallway: one 30 unit wide image, exactly as tall as the camera. PanoramaLeft is
        // where its left edge sits (the walkable hallway spans the whole image) so the painted doors
        // land on the door anchors in RoomGraph. False when the art is missing.
        private const float PanoramaLeft = -15f, PanoramaWidth = 30f, PanoramaCentreY = 0f;

        private bool AddHallwayBackdrop(RoomInstance room, Transform parent)
        {
            var dark = ProceduralArt.Get("bg_hallway_unlit");
            if (dark == null || dark.rect.width <= 8f) return false;
            SpriteGO(parent, "bg_hallway", dark,
                new Vector3(PanoramaLeft + PanoramaWidth / 2f, PanoramaCentreY, 0f), 0);
            return true;
        }

        // A room backdrop centred on the camera's frame (rooms are looked at from x = 0).
        private bool AddRoomBackdrop(Transform parent, string key)
        {
            var sprite = ProceduralArt.Get(key);
            if (sprite == null || sprite.rect.width <= 8f) return false;
            SpriteGO(parent, key, sprite, Vector3.zero, 0);
            return true;
        }

        private void AddDoor(RoomInstance room, string label, float x, float floorY, RoomId target, string spriteKey,
            bool drawn = true, float? anchorY = null)
        {
            // The door's art key always stood in the signature; the flat slab was only there
            // until art existed. Draw the sprite standing ON the floor band, sized from its own
            // pixels-per-unit, so dropping a real door PNG at Resources/Art/<key>.png resizes
            // and replaces it with no code change. (Null sprite keeps the old slab.)
            var sprite = drawn ? ProceduralArt.Get(spriteKey) : null;
            GameObject go;
            if (!drawn)
            {
                // painted into the background: an invisible anchor keeps the doorway interactive
                go = Quad(room.Root.transform, "door_" + target, new Vector3(x, floorY + 1.7f, 0), 10,
                    new Vector2(1.7f, 3.4f), new Color(0f, 0f, 0f, 0f));
            }
            else if (sprite != null)
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
            if (anchorY.HasValue) d.AnchorY = anchorY.Value;
            room.Interactables.Add(d);
        }

        // Something to inspect that is not a finding. closeUp is the picture beside the text,
        // spriteKey an optional small prop drawn on the wall at mountY above the anchor.
        private void AddExamine(RoomInstance room, string label, float x, float floorY, string text,
            string closeUp = null, string spriteKey = null, float mountY = 0f, float? anchorY = null)
        {
            var go = new GameObject("examine_" + label);
            go.transform.SetParent(room.Root.transform, false);
            go.transform.localPosition = new Vector3(x, floorY + 1.4f, 0);
            if (spriteKey != null)
            {
                var sprite = ProceduralArt.Get(spriteKey);
                if (sprite != null)
                    SpriteGO(go.transform, "art_" + spriteKey, sprite, new Vector3(0f, mountY, 0f), 9);
            }
            var spot = go.AddComponent<ExamineSpot>();
            spot.Init(_root, room, label, text, x, closeUp);
            if (anchorY.HasValue) spot.AnchorY = anchorY.Value;
            room.Interactables.Add(spot);
        }

        // The clue anchor (chest height, what the interact test uses) stays where it was; the
        // optional sprite hangs off it as a child so each GDD interaction object is actually
        // SEEN in the room. A dropped-in Resources/Art/<key>.png replaces the placeholder and
        // resizes itself from its own pixels-per-unit - no call-site changes.
        private void AddClue(RoomInstance room, string clueKey, float x, float floorY, string label,
            string spriteKey = null, float mountY = 0.8f, float? anchorY = null)
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
            if (anchorY.HasValue) co.AnchorY = anchorY.Value;
            room.Interactables.Add(co);
        }
    }
}
