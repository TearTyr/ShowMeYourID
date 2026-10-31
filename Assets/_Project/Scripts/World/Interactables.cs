using System;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.World
{
    public abstract class Interactable : MonoBehaviour
    {
        protected GameRoot _gameRoot;
        protected RoomInstance Room;
        public string Prompt = "Inspect";
        public float AnchorX;
        // Where the peek lamp has to point to find this (world y). Most things sit at chest height;
        // the classroom art has its own heights (chalkboard high, desk low).
        public float AnchorY = RoomInstance.FloorY + 1.4f;
        // In a peek the lamp's centre must be inside this box around the anchor (world units).
        public float HitW = 2.6f, HitH = 2.6f;
        public bool Enabled = true;
        public SpriteRenderer Art;

        public virtual void Interact() { }

        public float DistanceTo(Vector3 p) => Mathf.Abs(p.x - AnchorX);
    }

    public class TokenPickup : Interactable
    {
        public void Init(GameRoot root, RoomInstance room, float x)
        {
            _gameRoot = root; Room = room; AnchorX = x;
            Prompt = "Collect token [E]";
            Art = GetComponent<SpriteRenderer>();
        }

        public override void Interact()
        {
            if (_gameRoot == null || _gameRoot.Session == null) return;
            _gameRoot.Session.TokensCollected++;
            _gameRoot.Session.TokenNight = _gameRoot.Session.CurrentShift;
            _gameRoot.Records.TokensBanked++;   // spent on the main menu: see MainMenuView
            if (Room != null) Room.Interactables.Remove(this);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.TokenCollected, _gameRoot.Session.TokensCollected));
            _gameRoot.Audio.PlaySfx("sfx_scan_ok", 0.6f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*you pocket a token*", 2.4f, null));
            _gameRoot.PersistRun();   // a token is kept even if the run is abandoned
            Destroy(gameObject);
        }
    }

    public class Doorway : Interactable
    {
        public RoomId Target;

        public void Init(GameRoot root, RoomInstance room, string label, RoomId target, float x)
        {
            _gameRoot = root; Room = room; Target = target; AnchorX = x;
            Prompt = target == RoomId.Security ? "Return to Security [E]"
                   : target == RoomId.Hallway ? "Back to the hallway [E]"
                   : "Peek into " + label + " [E]";
            Art = GetComponent<SpriteRenderer>();
        }

        public override void Interact()
        {
            if (_gameRoot.Views.Transitioning) return;
            if (Target == RoomId.Security && _gameRoot.HuntActive)
            {
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification, "THE SECURITY DOOR IS LOCKED - HE IS INSIDE THE SCHOOL."));
                return;
            }
            if (_gameRoot.Views.Hunt != null && _gameRoot.Views.Hunt.BlocksDoor(Target)) return;   // he is behind it
            if (Target == RoomId.Security) _gameRoot.Views.ReturnToSecurity();
            // every room is peek only: the player looks inside from the doorway, never enters
            else _gameRoot.EnterRoomPeek(Target);
        }
    }

    // Something to look at that is not one of the ten findings: a line of text and, when it
    // has one, a close up picture. Add one with RoomBuilder.AddExamine.
    public class ExamineSpot : Interactable
    {
        private string _label, _text, _closeUp;

        public void Init(GameRoot root, RoomInstance room, string label, string text, float x, string closeUp)
        {
            _gameRoot = root; Room = room; AnchorX = x;
            _label = label; _text = text; _closeUp = closeUp;
            Prompt = "Inspect " + label + " [E]";
        }

        public override void Interact()
        {
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.8f);
            var line = new DialogueLine { Speaker = _label.ToUpperInvariant(), Text = _text, PortraitSpriteKey = _closeUp };
            if (_gameRoot.Dialogue != null)
                _gameRoot.Dialogue.Play(new System.Collections.Generic.List<DialogueLine> { line }, null);
        }
    }

    public class ClueObject : Interactable
    {
        private string _clueKey;
        public string ClueKey => _clueKey;
        private ClueDef _def;
        private bool _discovered;
        private GameObject _marker;

        public void Init(GameRoot root, RoomInstance room, string clueKey, string label, float x)
        {
            _gameRoot = root; Room = room; AnchorX = x;
            _clueKey = clueKey;
            _def = root.Session.Clues.Get(clueKey); // def content is static; tracker state lives in session
            Prompt = "Inspect " + label + " [E]";
            // no world sprite by design: the peek prompt and E reveal clues until real art
            // exists - hang a SpriteRenderer here when it does
        }

        public override void Interact()
        {
            if (_def == null)
            {
                var fresh = ClueRegistry.Build();
                foreach (var c in fresh) if (c.Key == _clueKey) { _def = c; break; }
            }
            if (_def == null) { Debug.LogWarning("[Clue] unknown clue key " + _clueKey); return; }

            var res = _gameRoot.Session.DiscoverClue(_clueKey);
            _gameRoot.Audio.PlaySfx(res.FirstTime ? "sfx_pickup" : "sfx_paper", 1f);

            var lines = new System.Collections.Generic.List<DialogueLine>
            {
                new DialogueLine { Speaker = _def.Display.ToUpperInvariant(), Text = _def.InspectText,
                                   PortraitSpriteKey = InspectArt(_clueKey) },
            };
            if (res.FirstTime)
            {
                lines.Add(new DialogueLine { Speaker = "NOTE TO SELF", Text = _def.ChainHint });
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    "FINDING " + _gameRoot.Session.Clues.FoundCount + "/10 - " + _def.Display));
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.ClueDiscovered, res));
                AddMarker();
                if (_def.IsScare && _gameRoot.Scares != null)
                    _gameRoot.Scares.Stinger("clue_" + _clueKey, 0f); // sanity already applied by DiscoverClue
            }
            if (_gameRoot.Dialogue != null) _gameRoot.Dialogue.Play(lines, null);
        }

        // The close up shown beside the text when the clue is inspected (null = no picture).
        private static string InspectArt(string clueKey)
        {
            switch (clueKey)
            {
                case "light": return "inspect_light_bulb";
                case "door": return "inspect_server_door";
                case "board": return "inspect_noticeboard";
                case "photo": return "inspect_photoframe";
                case "clock": return "inspect_clock";
                case "lostid": return "inspect_clipboard";
                default: return null;
            }
        }

        private void AddMarker()
        {
            if (_discovered || _marker != null) return;
            _discovered = true;
            // same flat-quad convention as every other world placeholder (size in units)
            _marker = RoomBuilder.Quad(Room.Root.transform, "found_marker",
                new Vector3(AnchorX, -1.35f, 0), 60,
                new Vector2(0.14f, 0.14f),
                new Color(0.75f, 0.14f, 0.18f, 0.9f)); // restrained red = interacted/interactive
        }

        public void SyncFromSession()
        {
            bool found = false;
            foreach (var k in _gameRoot.Session.Clues.FoundKeys) if (k == _clueKey) { found = true; break; }
            if (found && !_discovered) AddMarker();
        }
    }
}
