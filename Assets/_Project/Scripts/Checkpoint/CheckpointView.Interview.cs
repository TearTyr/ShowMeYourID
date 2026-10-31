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
    // The interview: the one classroom question and the tone label.
    // One area of the inspection desk; see CheckpointView.cs for the state machine,
    // the shared fields and the whole-class comment. Same class, split by desk area.
    public partial class CheckpointView
    {
        // The one question: "Which classroom are you going to?" It is free and can be asked
        // again. The student answers in the speech bubble, and the answer stays in the questions
        // panel so it can be compared with the file after the bubble moves on.
        public void AskClassroom()
        {
            if (_current == null || _verdictLocked) return;
            if (_gameRoot == null || _gameRoot.State != AppViewState.Checkpoint || _gameRoot.Paused) return;
            _classroomAsked = true;
            string tone = _current.ClassroomTone == ToneTag.Normal ? "" : "  [" + ToneLabel(_current.ClassroomTone) + "]";
            SayLine("AT THE WINDOW", "“" + _current.ClassroomAnswer + "”" + tone);
            if (Queue != null) Queue.ActiveQuestionAsked = true;   // survives quit/continue
            _gameRoot.Audio.PlaySfx("sfx_intercom", 0.5f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.QuestionAsked, ClassroomQuestion.Spoken));
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
