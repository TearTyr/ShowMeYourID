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
    // The interview: four questions, but only InterviewRules.Budget (2) can be asked of each
    // student, so choose with care. Year unlocks after flipping the ID, status after opening
    // their file. The answer is spoken in the bubble and also stays on its button, so it can
    // be compared with the card and the file after the bubble moves on.
    // One area of the inspection desk; see CheckpointView.cs for the state machine,
    // the shared fields and the whole-class comment. Same class, split by desk area.
    public partial class CheckpointView
    {
        // Ask one question. Hearing an answer again is free; a new question needs budget and
        // must be unlocked. Returns true when the student answered.
        public bool Ask(QuestionId q)
        {
            if (_current == null || _verdictLocked) return false;
            if (_gameRoot == null || _gameRoot.State != AppViewState.Checkpoint || _gameRoot.Paused) return false;
            if (!InterviewRules.Unlocked(q, _idFlipped, _fileOpened))
            {
                ToastDesk(InterviewRules.LockedHint(q));
                return false;
            }
            if (!InterviewRules.CanAsk(_questionMask, q))
            {
                ToastDesk("YOU CAN ONLY ASK " + InterviewRules.Budget + " QUESTIONS PER STUDENT.");
                return false;
            }
            _questionMask |= InterviewRules.Bit(q);
            if (Queue != null) Queue.ActiveQuestionMask = _questionMask;   // survives quit/continue
            string answer = q == QuestionId.Classroom ? _current.ClassroomAnswer : _current.Answers[(int)q];
            string tone = _current.ClassroomTone == ToneTag.Normal ? "" : "  [" + ToneLabel(_current.ClassroomTone) + "]";
            SayLine("AT THE WINDOW", "“" + answer + "”" + tone);
            _gameRoot.Audio.PlaySfx("sfx_intercom", 0.5f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.QuestionAsked, InterviewRules.Spoken(q)));
            RefreshHud();
            return true;
        }

        // Kept for the administrator conversation and the older tests: the classroom question.
        public void AskClassroom() => Ask(QuestionId.Classroom);

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
