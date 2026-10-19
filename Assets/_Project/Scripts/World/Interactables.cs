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
        protected GameRoot R;
        protected RoomInstance Room;
        public string Prompt = "Inspect";
        public float AnchorX;
        public bool Enabled = true;
        public SpriteRenderer Art;

        public virtual void Interact() { }

        public float DistanceTo(Vector3 p) => Mathf.Abs(p.x - AnchorX);
    }

    // Part of the world folder.
    public class TokenPickup : Interactable
    {
        public void Init(GameRoot root, RoomInstance room, float x)
        {
            R = root; Room = room; AnchorX = x;
            Prompt = "Collect token [E]";
            Art = GetComponent<SpriteRenderer>();
        }

        public override void Interact()
        {
            if (R == null || R.Session == null) return;
            R.Session.TokensCollected++;
            if (Room != null) Room.Interactables.Remove(this);
            R.Bus.Publish(GameEvent.Of(EventIds.TokenCollected, R.Session.TokensCollected));
            R.Audio.PlaySfx("sfx_scan_ok", 0.6f);
            R.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*you pocket the token - evidence*", 2.4f, null));
            Destroy(gameObject);
        }
    }

    // Part of the world folder.
    public class Doorway : Interactable
    {
        public RoomId Target;

        public void Init(GameRoot root, RoomInstance room, string label, RoomId target, float x)
        {
            R = root; Room = room; Target = target; AnchorX = x;
            Prompt = target == RoomId.Security ? "Return to Security [E]" : "Enter " + label + " [E]";
            Art = GetComponent<SpriteRenderer>();
        }

        public override void Interact()
        {
            if (R.Views.Transitioning) return;
            if (Target == RoomId.Security) R.Views.ReturnToSecurity();
            else R.Views.EnterRoom(Target);
        }
    }

    // Part of the world folder.
    public class SecurityDoorInteractable : Interactable
    {
        public void Init(GameRoot root, RoomInstance room, float x)
        {
            R = root; Room = room; AnchorX = x;
            Prompt = "Security Office [E]";
            Art = GetComponent<SpriteRenderer>();
        }

        public override void Interact()
        {
            if (R.SecurityRoom == null) return;
            R.Audio.PlaySfx("sfx_door", 0.7f);
            R.OpenSecurityRoom();
        }
    }

    // Part of the world folder.
    public class LightSwitchObject : Interactable
    {
        public void Init(GameRoot root, RoomInstance room, float x)
        {
            R = root; Room = room; AnchorX = x;
            Prompt = "Light switch [E]";
            Art = GetComponent<SpriteRenderer>();
        }

        public override void Interact()
        {
            if (Room.Light == null) return;
            Room.Light.SetMode(Room.Light.Mode == LightMode.Off ? LightMode.Sweep : LightMode.Off);
            R.Audio.PlaySfx("sfx_flicker");
        }
    }

    // Part of the world folder.
    public class ClueObject : Interactable
    {
        private string _clueKey;
        public string ClueKey => _clueKey;
        private ClueDef _def;
        private bool _discovered;
        private GameObject _marker;

        public void Init(GameRoot root, RoomInstance room, string clueKey, string label, float x)
        {
            R = root; Room = room; AnchorX = x;
            _clueKey = clueKey;
            _def = root.Session.Clues.Get(clueKey); // def content is static; tracker state lives in session
            Prompt = "Inspect " + label + " [E]";
        }

        public override void Interact()
        {
            if (_def == null)
            {
                var fresh = ClueRegistry.Build();
                foreach (var c in fresh) if (c.Key == _clueKey) { _def = c; break; }
            }
            if (_def == null) { Debug.LogWarning("[Clue] unknown clue key " + _clueKey); return; }

            var res = R.Session.DiscoverClue(_clueKey);
            R.Audio.PlaySfx(res.FirstTime ? "sfx_pickup" : "sfx_paper", 1f);

            var lines = new System.Collections.Generic.List<DialogueLine>
            {
                new DialogueLine { Speaker = _def.Display.ToUpperInvariant(), Text = _def.InspectText },
            };
            if (res.FirstTime)
            {
                lines.Add(new DialogueLine { Speaker = "NOTE TO SELF", Text = _def.ChainHint });
                R.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    "FINDING " + R.Session.Clues.FoundCount + "/10 — " + _def.Display));
                R.Bus.Publish(GameEvent.Of(EventIds.ClueDiscovered, res));
                AddMarker();
                if (_def.IsScare && R.Scares != null)
                    R.Scares.Stinger("clue_" + _clueKey, 0f); // sanity already applied by DiscoverClue
            }
            if (R.Dialogue != null) R.Dialogue.Play(lines, null);
        }

        private void AddMarker()
        {
            if (_discovered || _marker != null) return;
            _discovered = true;
            _marker = new GameObject("found_marker");
            _marker.transform.SetParent(Room.Root.transform, false);
            _marker.transform.localPosition = new Vector3(AnchorX, -1.35f, 0);
            var sr = _marker.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralArt.Get("solid_white");
            sr.color = new Color(0.75f, 0.14f, 0.18f, 0.9f); // restrained red = interacted/interactive
            sr.sortingOrder = 76;
            _marker.transform.localScale = new Vector3(0.14f, 0.14f, 1f);
        }

        public void SyncFromSession()
        {
            bool found = false;
            foreach (var k in R.Session.Clues.FoundKeys) if (k == _clueKey) { found = true; break; }
            if (found && !_discovered) AddMarker();
        }
    }
}
