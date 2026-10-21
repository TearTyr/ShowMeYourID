using System.Collections;
using System.Collections.Generic;
using System.Text;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.Checkpoint
{
    // Centre column: speech log, hand-over prompt, patrol / end-shift buttons.
    // One area of the inspection desk; see CheckpointView.cs for the state machine,
    // the shared fields and the whole-class comment. Same class, split by desk area.
    public partial class CheckpointView
    {
        private void BuildCenterColumn()
        {
            _speechPanel = UIFactory.BorderedPanel(_desk, "speech",
                new Color(0.859f, 0.851f, 0.824f), UIFactory.Border, 2);
            Reg("speech", _speechPanel, L.Speech);
            _speechLog = UIFactory.MaskedLabel(_speechPanel, "speechlog",
                L.Inner.SpeechText.X, L.Inner.SpeechText.Y, L.Inner.SpeechText.W, L.Inner.SpeechText.H,
                12, UIFactory.PaperInk, TextAnchor.LowerLeft);
            _speechPanel.gameObject.SetActive(false);

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

        private void Say(string text) => SayLine("AT THE WINDOW", text);

        // Rolling two-sided conversation: speaker tag + line per entry, newest at the bottom,
        // oldest clipped away by the panel's RectMask2D once the box is full.
        private void SayLine(string who, string text)
        {
            _speechEntries.Add(new[] { who, text });
            while (_speechEntries.Count > SpeechKeep) _speechEntries.RemoveAt(0);
            var sb = new StringBuilder();
            foreach (var entry in _speechEntries)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append("<b><color=#6f6d68>").Append(entry[0]).Append("</color></b>\n").Append(entry[1]);
            }
            _speechLog.text = sb.ToString();
            _speechPanel.gameObject.SetActive(true);
        }

        private void RefreshHandoverPrompt()
        {
            string want = "";
            if (_current != null && !_verdictLocked)
            {
                if (_offeredRoot.activeSelf) want = "CLICK THE CARD TO TAKE THE ID";
                else if (!_documentsTaken) want = "CLICK THE WINDOW — THEY ARE HOLDING THEIR ID";
            }
            if (want == _lastHandoverPrompt) return;
            _lastHandoverPrompt = want;
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.InteractionPrompt, want));
        }
    }
}
