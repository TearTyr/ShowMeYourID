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
    // (SpeechBubbleLayout wraps/limits it inside the registered box) and STAYS until the next
    // line replaces it or the desk clears it (visitor leaves, patrol, new shift) — there is no
    // timer, so a clue the player has not read yet can never vanish.
    public partial class CheckpointView
    {
        private RectTransform _bubbleRoot;    // registered container (L.SpeechBubble)
        private RectTransform _bubblePanel;   // the paper bubble; height grows per line count
        private Text _bubbleText;             // speaker tag + line (rich text)
        private readonly SpeechBubbleClock _bubble = new SpeechBubbleClock();
        private int _bubbleShownGen = -1;     // generation currently rendered (-1 = none)
        private bool _visitorShown;           // SetVisitorVisible: bubble only speaks at the window
        private const float BubbleMinHeight = 56f;   // one short line still gets a real body

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
            MakeClickThrough(_bubblePanel);
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

        // The bubble hangs over empty glass beside the head, so none of it takes the pointer:
        // a click or drag that lands on it must reach whatever is underneath.
        private static void MakeClickThrough(RectTransform panel)
        {
            foreach (var img in panel.gameObject.GetComponentsInChildren<Image>(true))
                img.raycastTarget = false;
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

        // Cleanup path: leaving the desk (patrol), a new shift, or the visitor walking
        // off hides the bubble instantly — a line never carries over to the next beat.
        internal void HideBubbleNow()
        {
            _bubble.HideNow();
            SyncBubble();
        }

        // Model -> widgets: show/hide, then (only when a NEW line arrived) re-fit the
        // text and size the panel to the wrapped line count.
        private void SyncBubble()
        {
            if (_bubbleRoot == null) return;
            bool on = _bubble.Visible && _visitorShown;
            _bubbleRoot.gameObject.SetActive(on);
            if (!on)
            {
                _bubbleShownGen = -1;
                return;
            }
            if (_bubbleShownGen == _bubble.Generation) return;   // same line already rendered
            _bubbleShownGen = _bubble.Generation;
            if (_kitOn) LayoutKitBubble(); else LayoutPlainBubble();
        }

        // The kit bubble: the spoken line alone (the tail already points at who is talking), in
        // big dark type on the art's white. The panel hangs its painted-tail border OUTSIDE the
        // registered box so the white lands exactly on the reference rect, and the text sits
        // inside the white with a clear inset — never on the jagged ink. Wrap budget and line
        // height follow the player's text scale, so the line count we size the panel for is the
        // line count the Text really wraps to; truncation is the last resort, never the first.
        private void LayoutKitBubble()
        {
            float scale = Mathf.Max(1f, UIFactory.TextScale);
            float lineH = L.Inner.BubbleLineHKit * scale;
            float inset = L.Inner.BubbleKitInsetY;
            int maxLines = Mathf.Max(1, Mathf.FloorToInt((L.SpeechBubble.H - 2f * inset) / lineH));
            int budget = Mathf.Max(8, Mathf.RoundToInt(L.Inner.BubbleCharsPerLineKit / scale));
            string display = SpeechBubbleLayout.Fit(_bubble.Text, budget, maxLines, out int lines);
            UIFactory.SetBaseFontSize(_bubbleText, L.Inner.BubbleFontKit);
            _bubbleText.text = display;
            float h = Mathf.Clamp(SpeechBubbleLayout.Height(lines, lineH, 0f, inset, inset),
                BubbleMinHeight, L.SpeechBubble.H);
            float px = -L.Inner.BubbleKitTailW;
            float pw = L.Inner.BubblePanelW + L.Inner.BubbleKitTailW + L.Inner.BubbleKitEdgeW;
            L.Place(_bubblePanel, new L.Region(px, 0f, pw, h));
            float tx = L.Inner.BubbleKitTailW + L.Inner.BubbleKitInsetX;
            float tw = pw - L.Inner.BubbleKitWhiteMargin - L.Inner.BubbleKitInsetX - tx;
            L.Place(_bubbleText.rectTransform, new L.Region(tx, inset, tw, h - 2f * inset));
        }

        // The no-kit bubble (procedural paper panel): start at the reference type size and,
        // only when the wrapped line count would overflow the line budget, trade a point of
        // type for a wider character budget.
        private void LayoutPlainBubble()
        {
            int maxLines = L.Inner.BubbleMaxLines;
            int budget = L.Inner.BubbleCharsPerLine;
            int size = 13;
            float lineH = L.Inner.BubbleLineH;
            string display = SpeechBubbleLayout.Fit(_bubble.Text, budget, maxLines, out int lines);
            for (int pass = 0; pass < 3 &&
                 SpeechBubbleLayout.CountLines(_bubble.Text, budget) > maxLines; pass++)
            {
                budget += 5; size -= 1; lineH -= 1f;
                display = SpeechBubbleLayout.Fit(_bubble.Text, budget, maxLines, out lines);
            }
            UIFactory.SetBaseFontSize(_bubbleText, size);
            _bubbleText.text =
                "<b><color=" + SpeakerColor(_bubble.Who) + ">" + _bubble.Who + "</color></b>\n" + display;
            float h = Mathf.Clamp(
                SpeechBubbleLayout.Height(lines, lineH, L.Inner.BubbleTagH,
                    L.Inner.BubblePadTop, L.Inner.BubblePadBottom),
                BubbleMinHeight, L.SpeechBubble.H);
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
                else if (!_slipStamped) want = "STAMP THE SLIP — DRAG A STAMP ONTO IT";
                else want = "HAND BACK BOTH PAPERS AT THE WINDOW (H)";
            }
            if (want == _lastHandoverPrompt) return;
            _lastHandoverPrompt = want;
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.InteractionPrompt, want));
        }
    }
}
