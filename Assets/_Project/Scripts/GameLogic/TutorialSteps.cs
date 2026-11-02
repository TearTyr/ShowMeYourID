namespace Hollowbrook.Logic
{
    /// <summary>Where the Night 1 tutorial stands. Done (0) is also the value of a game that never
    /// had a tutorial, so old saves and every other mode simply have none.</summary>
    public enum TutorialStep
    {
        Done = 0,
        FirstStudent,   // visitor 1 is a real student: the whole desk routine
        FirstAnomaly,   // visitor 2 has a broken rule: find it, and meet the Security Log
        GoPatrol,       // the line waits, the patrol opens early
        HallAnomaly,    // a slow anomaly in the hallway: beam slows it, Focus drives it out
        PeekClean,      // Room 101: a clean room, learn the peek
        PeekAnomaly,    // Room 202: something stands there, learn to clear it
        Finish,         // training over, the player does the rest alone
    }

    /// <summary>What the game knows right now, handed to TutorialSteps.Advance. Plain values, so the
    /// order of the steps can be tested without the game running.</summary>
    public struct TutorialFacts
    {
        public int Processed;           // visitors stamped so far
        public bool OnPatrol;           // in the hallway or peeking into a room
        public bool Peeking;            // looking into a room right now
        public bool HallAnomalyDriven;  // the hallway anomaly was driven out
        public int ClearedRooms;        // PatrolCycle bits
        public int PeekAnomaliesGone;   // PatrolCycle bits
    }

    /// <summary>The Night 1 tutorial: the order of the steps, what each one says, and what each one
    /// holds still. One teaching round of the core ideas, then no more training wheels.</summary>
    public static class TutorialSteps
    {
        public static TutorialStep First => TutorialStep.FirstStudent;

        public static bool Active(TutorialStep step) => step != TutorialStep.Done;

        /// <summary>The step that comes after this one once its job is done.</summary>
        public static TutorialStep Advance(TutorialStep step, TutorialFacts f)
        {
            // one call can finish several steps, for example after a quit and Continue
            while (true)
            {
                var next = Next(step, f);
                if (next == step) return step;
                step = next;
            }
        }

        private static TutorialStep Next(TutorialStep step, TutorialFacts f)
        {
            switch (step)
            {
                case TutorialStep.FirstStudent: return f.Processed >= 1 ? TutorialStep.FirstAnomaly : step;
                case TutorialStep.FirstAnomaly: return f.Processed >= 2 ? TutorialStep.GoPatrol : step;
                case TutorialStep.GoPatrol: return f.OnPatrol ? TutorialStep.HallAnomaly : step;
                case TutorialStep.HallAnomaly: return f.HallAnomalyDriven ? TutorialStep.PeekClean : step;
                case TutorialStep.PeekClean:
                    return Has(f.ClearedRooms, RoomId.Classroom1) && !f.Peeking ? TutorialStep.PeekAnomaly : step;
                case TutorialStep.PeekAnomaly:
                    return Has(f.PeekAnomaliesGone, RoomId.Classroom2) && !f.Peeking ? TutorialStep.Finish : step;
                default: return step;   // Finish ends only with the GOT IT button
            }
        }

        private static bool Has(int bits, RoomId room) => (bits & PatrolCycle.Bit(room)) != 0;

        /// <summary>The line of visitors waits while the lessons away from the window run.</summary>
        public static bool HoldsLine(TutorialStep step) =>
            step >= TutorialStep.GoPatrol && step <= TutorialStep.PeekAnomaly;

        /// <summary>The patrol opens early for the lesson: only the first lesson needs the line to be done.</summary>
        public static bool OpensPatrol(TutorialStep step) => step == TutorialStep.GoPatrol;

        /// <summary>Which door opens. Before the lesson none does, then only the room the lesson is about.</summary>
        public static bool PeekAllowed(TutorialStep step, RoomId room)
        {
            if (room == RoomId.Hallway || room == RoomId.Security) return true;   // only the four peek rooms are guarded
            if (step == TutorialStep.Done || step == TutorialStep.Finish) return true;
            if (step == TutorialStep.PeekClean) return room == RoomId.Classroom1;
            if (step == TutorialStep.PeekAnomaly) return room == RoomId.Classroom2;
            return false;
        }

        public static bool NeedsGotIt(TutorialStep step) => step == TutorialStep.Finish;

        public static string Title(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.FirstStudent: return "VISITOR 1: A REAL STUDENT";
                case TutorialStep.FirstAnomaly: return "VISITOR 2: SOMETHING IS OFF";
                case TutorialStep.GoPatrol: return "TIME TO PATROL";
                case TutorialStep.HallAnomaly: return "SOMETHING IN THE HALL";
                case TutorialStep.PeekClean: return "PEEK INTO ROOM 101";
                case TutorialStep.PeekAnomaly: return "ROOM 202 HAS SOMETHING";
                case TutorialStep.Finish: return "TRAINING OVER";
                default: return "";
            }
        }

        public static string Body(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.FirstStudent:
                    return "Press F to flip the ID. Open the FILE drawer and compare the name, ID, grade and status. " +
                           "Ask a question. If it all matches, drag APPROVE onto the slip, then drag the ID back to the window.";
                case TutorialStep.FirstAnomaly:
                    return "Read the Rules sheet, then look for the broken rule on the ID (check the date). " +
                           "If something is wrong, drag DECLINE. Count every visitor in the Security Log: you sign it before END SHIFT.";
                case TutorialStep.GoPatrol:
                    return "The line waits while you walk. Click PATROL to leave the booth. " +
                           "A and D walk, F turns the flashlight on.";
                case TutorialStep.HallAnomaly:
                    return "The plain light slows it down. Press Q for the Focus light and hold it on the thing to drive it out. " +
                           "It cannot hurt you while you train.";
                case TutorialStep.PeekClean:
                    return "Stand at the Room 101 door and press E. Move the flashlight over the room. " +
                           "Nothing here, so step back with S. Only this door opens right now.";
                case TutorialStep.PeekAnomaly:
                    return "Peek into Room 202 with E. Hold the light on the figure to clear it. " +
                           "The Focus light (Q) is faster. Step back with S when it is gone.";
                case TutorialStep.Finish:
                    return "That is the job. Peek into a CR, go back to the desk, finish the line, " +
                           "count and sign the Security Log, then END SHIFT. You are on your own now.";
                default: return "";
            }
        }
    }
}
