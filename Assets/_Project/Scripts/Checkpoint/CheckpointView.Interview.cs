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
    // The interview: QUESTION toggle, the four questions, testimony lines.
    // One area of the inspection desk; see CheckpointView.cs for the state machine,
    // the shared fields and the whole-class comment. Same class, split by desk area.
    public partial class CheckpointView
    {
        // Interview gate: questions exist only after you deliberately open them.


        // Headless harnesses/FlowCheck drive the same path the buttons use.

        internal void AskByHotkey(int i)
        {
            if (_gameRoot == null || _gameRoot.State != AppViewState.Checkpoint) return;
            if (_gameRoot.Paused) return;
            if (i < 0 || i >= QuestionSystem.All.Length) return;
            AskQuestion(QuestionSystem.All[i]);
        }

        public void AskQuestion(QuestionId q)
        {
            if (_current == null || _verdictLocked) return;
            if (!_budget.TryAsk(_current, q, out var answer))
            {
                // the desk has no scanner readout any more — refusals surface as HUD toasts
                string refusal = _budget.HasAsked(q) ? "ALREADY ASKED THAT." : "NO MORE QUESTIONS — DISTRICT POLICY.";
                _transcript.Add("DESK", refusal, false);   // ...and in the window log
                if (_logOpen) SyncTalkLog();
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification, refusal));
                _gameRoot.Audio.PlaySfx("sfx_scan_error", 0.35f);
                return;
            }
            string tone = answer.Tone == ToneTag.Normal ? "" : "  [" + ToneLabel(answer.Tone) + "]";
            // the player's own question needs no echo — the button already shows it asked;
            // the bubble carries the student's answer beside their face
            SayLine("AT THE WINDOW", "“" + answer.Text + "”" + tone);
            // ...and the window log remembers it: whether the question came from a button
            // or from the typed text box, the answer line belongs to the conversation
            _transcript.Add(_current.IsAdministrator ? "ADMIN" : "VISITOR", answer.Text + tone, false);
            if (Queue != null) Queue.ActiveBudgetMask = _budget.Mask;   // survives quit/continue
            _gameRoot.Audio.PlaySfx("sfx_intercom", 0.5f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.QuestionAsked, q));
            RefreshHud();
        }



        private static string ToneLabel(ToneTag t)
        {
            switch (t)
            {
                case ToneTag.Hesitant: return "hesitant";
                case ToneTag.Flat: return "flat, rehearsed";
                case ToneTag.TooSmooth: return "too smooth";
                default: return "echoing, as if reading";
            }
        }
    }
}
