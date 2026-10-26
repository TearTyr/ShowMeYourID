using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.Checkpoint
{
    // Centre column: the speech bubble, hand-over prompt, patrol / end-shift buttons.
    // One area of the inspection desk; see CheckpointView.cs for the state machine,
    // the shared fields and the whole-class comment. Same class, split by desk area.
    //
    // Student dialogue is a comic-style SPEECH BUBBLE beside the visitor's head (the old
    // rectangular strip under the window is gone): paper stock, dark border, a small tail
    // pointing at the face. The bubble shows the latest window line, grows with the text
    // (SpeechBubbleLayout wraps/limits it inside the registered box) and is removed
    // exactly 2 s after the dialogue ends — the countdown lives in the pure
    // SpeechBubbleClock, so a newer line always cancels the pending hide and no stale
    // timer can swallow it (generation-guarded, one clock, no coroutine pile-up).
    public partial class CheckpointView
    {
        private RectTransform _bubbleRoot;    // registered container (L.SpeechBubble)
        private HoverSense _bubbleHover;      // pointer enter/exit on the bubble panel
        private RectTransform _bubblePanel;   // the paper bubble; height grows per line count
        private Text _bubbleText;             // speaker tag + line (rich text)
        private readonly SpeechBubbleClock _bubble = new SpeechBubbleClock();
        private int _bubbleShownGen = -1;     // generation currently rendered (-1 = none)
        private bool _visitorShown;           // SetVisitorVisible: bubble only speaks at the window

        internal SpeechBubbleClock BubbleClock => _bubble;   // headless proofs read the model

        private void BuildCenterColumn()
        {
            // the bubble container sits ABOVE the window area (built after it) and BELOW
            // the papers layer (built later), so a dragged paper still rides over it
            _bubbleRoot = UIFactory.NewUI(_desk, "speechbubble");
            Reg("speechbubble", _bubbleRoot, L.SpeechBubble);

            _bubblePanel = UIFactory.BorderedPanel(_bubbleRoot, "bubble",
                new Color(0.859f, 0.851f, 0.824f), UIFactory.Border, 2);
            L.Place(_bubblePanel, new L.Region(L.Inner.BubblePanelX, 0f, L.Inner.BubblePanelW, 60f));
            MakeHoverable(_bubblePanel);
            _bubbleText = UIFactory.Label(_bubblePanel, "bubbletext", "", 13,
                UIFactory.NsInk, TextAnchor.UpperLeft);
            _bubbleText.font = UIFactory.UiFontBold;
            _bubblePanel.gameObject.AddComponent<RectMask2D>();   // text can never spill the paper

            // the tail rides ON TOP of the panel's left border (later sibling) so its
            // borderless base melts into the bubble — tip points at the visitor's face
            var tail = UIFactory.Img(_bubbleRoot, "bubbletail", ProceduralArt.Get("bubble_tail"));
            L.Place(tail.rectTransform, L.Inner.BubbleTail);
            tail.raycastTarget = false;

            _bubbleRoot.gameObject.SetActive(false);

            // the principal note left the desk surface: its addendum lives in the RULES NOTE
            // overlay (RefreshNotes), which keeps the centre column calm

            _patrolBtn = GridButton(_desk, "patrol", "PATROL HALLWAY  ▸", GoPatrol, L.PatrolBtn, 12);
            Reg("patrol", _patrolBtn, L.PatrolBtn);
            _endShiftBtn = GridButton(_desk, "endshift", "END SHIFT — CLOCK OUT",
                () => _gameRoot.FinishShift(), L.EndShiftBtn, 11);
            Reg("endshift", _endShiftBtn, L.EndShiftBtn);
            UIFactory.SetButtonEnabled(_patrolBtn, false);
            UIFactory.SetButtonEnabled(_endShiftBtn, false);
        }

        // Hover-to-read: the bubble's border panel is the ONE raycast target in the bubble, so
        // the pointer can enter it (HoverSense suspends the hide countdown) while the fill, the
        // text and the tail stay click-through and the box hangs over empty glass only — beside
        // the head, clear of the portrait, the questions column and the papers layer.
        private void MakeHoverable(RectTransform panel)
        {
            foreach (var img in panel.gameObject.GetComponentsInChildren<Image>(true))
                img.raycastTarget = false;
            var border = panel.GetComponent<Image>();
            if (border != null) border.raycastTarget = true;
            _bubbleHover = panel.gameObject.AddComponent<HoverSense>();
            // a hidden bubble is not hoverable (it stops raycasting); the guard also
            // covers direct/headless calls so a stray enter can never suspend the countdown
            _bubbleHover.OnEnter = () =>
            {
                if (_bubbleRoot != null && _bubbleRoot.gameObject.activeSelf) _bubble.SetHover(true);
            };
            _bubbleHover.OnExit = () => { _bubble.SetHover(false); SyncBubble(); };
        }

        private void Say(string text) => SayLine("AT THE WINDOW", text);

        // Headless hook: inject one window line — the same path the student's own
        // dialogue takes (FlowCheck drives the bubble timing deterministically).
        internal void DebugSay(string text) => Say(text);

        // The bubble only ever speaks with the student's voice at the window. Other
        // voices (radio, bulletins) are HUD toasts — a bubble anchored to the student's
        // head must not ventriloquise the intercom.
        private void SayLine(string who, string text)
        {
            if (who != "AT THE WINDOW")
            {
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification, who + " — " + text));
                return;
            }
            _bubble.Say(who, text);   // a new line cancels any pending hide (one clock)
            SyncBubble();
        }

        // chat colours: the window voice in its own ink (kept for the tag line)
        private static string SpeakerColor(string who)
        {
            if (who == "YOU") return "#4a5a49";
            if (who == "RADIO") return "#7a4a4a";
            return "#6f6d68";
        }

        // One frame of the bubble countdown. Frozen while paused, behind blocking sheets,
        // and while a scripted dialogue is still talking — the 2 s cooldown begins only
        // AFTER the dialogue has ended. internal so the headless FlowCheck can step it
        // deterministically (stub MonoBehaviours never get Update calls).
        internal void TickBubble(float dt)
        {
            if (_gameRoot == null || _bubbleRoot == null) return;
            if (_gameRoot.Paused || _gameRoot.BlockingOverlayOpen) return;
            if (_gameRoot.Dialogue != null && _gameRoot.Dialogue.IsActive) return;
            if (_gameRoot.State != AppViewState.Checkpoint && _gameRoot.State != AppViewState.AdminEncounter) return;
            _bubble.Tick(dt);
            SyncBubble();
        }

        // Cleanup path: leaving the desk (patrol), a new shift, or the visitor walking
        // off hides the bubble instantly — no timer survives the room change.
        internal void HideBubbleNow()
        {
            _bubble.HideNow();
            SyncBubble();
        }

        // Model -> widgets: show/hide, then (only when a NEW line arrived) re-fit the
        // text and grow the panel to the wrapped line count.
        private void SyncBubble()
        {
            if (_bubbleRoot == null) return;
            bool on = _bubble.Visible && _visitorShown;
            _bubbleRoot.gameObject.SetActive(on);
            if (!on)
            {
                _bubbleShownGen = -1;
                if (_bubbleHover != null) _bubbleHover.ForceExit();  // a hidden bubble is not hovered
                return;
            }
            if (_bubbleShownGen == _bubble.Generation) return;   // same line already rendered
            _bubbleShownGen = _bubble.Generation;

            string display = SpeechBubbleLayout.Fit(_bubble.Text,
                L.Inner.BubbleCharsPerLine, L.Inner.BubbleMaxLines, out int lines);
            _bubbleText.text = "<b><color=" + SpeakerColor(_bubble.Who) + ">" +
                               _bubble.Who + "</color></b>\n" + display;
            float h = Mathf.Clamp(
                SpeechBubbleLayout.Height(lines, L.Inner.BubbleLineH, L.Inner.BubbleTagH,
                    L.Inner.BubblePadTop, L.Inner.BubblePadBottom),
                56f, L.SpeechBubble.H);
            L.Place(_bubblePanel, new L.Region(L.Inner.BubblePanelX, 0f, L.Inner.BubblePanelW, h));
            L.Place(_bubbleText.rectTransform, new L.Region(10f, L.Inner.BubblePadTop,
                L.Inner.BubblePanelW - 20f, h - L.Inner.BubblePadTop - L.Inner.BubblePadBottom));
        }

        private void RefreshHandoverPrompt()
        {
            string want = "";
            if (_current != null && !_verdictLocked)
            {
                if (!_documentsTaken) want = "DRAG THE ID AND THE SLIP FROM THE COUNTER TO YOUR DESK";
                else if (!_slipTaken) want = "THE SLIP IS STILL ON THE COUNTER — DRAG IT TO YOUR DESK";
                else if (!_slipStamped) want = "STAMP THE SLIP — DRAG A STAMP ONTO IT (A / D)";
                else want = "HAND BACK BOTH PAPERS AT THE WINDOW (H)";
            }
            if (want == _lastHandoverPrompt) return;
            _lastHandoverPrompt = want;
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.InteractionPrompt, want));
        }
    }
}
