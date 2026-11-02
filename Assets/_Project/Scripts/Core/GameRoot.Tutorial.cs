using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Core
{
    // GameRoot, part: the Night 1 tutorial. TutorialSteps (pure logic) knows the order and the texts;
    // this part feeds it what the game is doing and applies what a step holds still.
    public partial class GameRoot
    {
        // the hallway anomaly of the lesson was driven out (not saved: the step itself is)
        private bool _hallAnomalyDriven;

        public TutorialStep TutorialStep => Session != null ? Session.TutorialStep : TutorialStep.Done;
        public bool TutorialActive => TutorialSteps.Active(TutorialStep);

        /// <summary>While a lesson away from the window runs, nobody new steps up to it.</summary>
        public bool TutorialHoldsLine => TutorialSteps.HoldsLine(TutorialStep);
        public bool TutorialOpensPatrol => TutorialSteps.OpensPatrol(TutorialStep);

        /// <summary>The SKIP TUTORIAL button: the lesson is over, the night goes on as normal.</summary>
        public void SkipTutorial() => SetTutorialStep(TutorialStep.Done);

        /// <summary>The GOT IT button of the last step.</summary>
        public void TutorialGotIt()
        {
            if (TutorialSteps.NeedsGotIt(TutorialStep)) SetTutorialStep(TutorialStep.Done);
        }

        /// <summary>May the player peek into this room? Before the lesson reaches it, no.</summary>
        public bool TutorialAllowsPeek(RoomId room) => TutorialSteps.PeekAllowed(TutorialStep, room);

        /// <summary>The hallway anomaly of the lesson was driven out by the Focus light.</summary>
        public void NoteLessonAnomalyDriven()
        {
            if (TutorialStep == TutorialStep.HallAnomaly) _hallAnomalyDriven = true;
        }

        /// <summary>Once a frame: if the step's job is done, move on. Public so the headless suites can drive it,
        /// like AdvanceShiftClock; Update is the only real caller.</summary>
        public void TickTutorial()
        {
            if (Queue != null) Queue.HoldLine = TutorialHoldsLine;
            if (!TutorialActive) return;
            var facts = new TutorialFacts
            {
                Processed = Session.Processed,
                OnPatrol = State == AppViewState.Exploration || State == AppViewState.RoomPeek,
                Peeking = State == AppViewState.RoomPeek,
                HallAnomalyDriven = _hallAnomalyDriven,
                ClearedRooms = Session.ClearedRooms,
                PeekAnomaliesGone = Session.PeekAnomaliesGone,
            };
            SetTutorialStep(TutorialSteps.Advance(TutorialStep, facts));
        }

        private void SetTutorialStep(TutorialStep step)
        {
            if (Session == null || Session.TutorialStep == step) return;
            Session.TutorialStep = step;
            _hallAnomalyDriven = false;
            if (Queue != null) Queue.HoldLine = TutorialHoldsLine;
            Bus.Publish(GameEvent.Of(EventIds.TutorialChanged, (float)(int)step));
        }
    }
}
