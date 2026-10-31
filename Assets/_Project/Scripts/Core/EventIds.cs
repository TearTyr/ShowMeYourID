namespace Hollowbrook.Core
{
    public static class EventIds
    {
        public const string StateChanged = "state.changed";

        public const string ObjectiveChanged = "objective.changed";// Text: objective line
        public const string RunTimerTick = "run.timer_tick";       // Value: seconds left
        public const string MutationChanged = "run.mutation";      // Data: RuleMutationDef

        public const string VerdictStamped = "visitor.verdict";    // Data: InspectionRules.Outcome

        public const string RoomEntered = "room.entered";          // Data: RoomId
        public const string LightModeChanged = "light.mode";       // Data: bool - room lights on/off (scripted flicker) or the peek flashlight on/off
        public const string ClueDiscovered = "clue.discovered";    // Data: ClueDiscoveryResult
        public const string InteractionPrompt = "interact.prompt"; // Text: prompt or ""

        public const string SanityChanged = "sanity.changed";      // Value: 0..100
        public const string SanityStageChanged = "sanity.stage";   // Data: SanityStage
        public const string ScareTriggered = "scare.triggered";    // Text: kind
        public const string HallucinationTriggered = "hallucination.triggered"; // Text: kind

        public const string CaptionRequested = "caption.show";     // Text: caption, Value: seconds
        public const string SettingsChanged = "settings.changed";  // Data: SettingsData
        public const string Notification = "ui.notification";
        public const string PauseToggled = "pause.toggled";
        public const string GameSaved = "save.written";
        public const string GameLoaded = "save.loaded";
        public const string ShiftStarted = "shift.started";
        public const string ShiftEnded = "shift.ended";
        public const string EndingReached = "ending.reached";
        public const string VisitorArrived = "desk.visitor";
        public const string DocumentsShown = "desk.docs";
        public const string QuestionAsked = "desk.question";
        public const string RoomExited = "room.exited";
        public const string AnomalySighted = "anomaly.sighted";
        public const string AnomalyCaught = "anomaly.caught";      // Text: toast message
        public const string TrackingChanged = "anomaly.tracking";    // Value: 0..1 meter
        public const string AnomalyRevealed = "anomaly.revealed";    // Data: kind / "hallucination"
        public const string TokenCollected = "anomaly.token";      // Value: tokens held this run
        public const string BatteryChanged = "light.battery";      // Value: 0..1 charge
        public const string AnomalyStateChanged = "anomaly.state"; // Data: AnomalyState name
        public const string PatrolStarted = "patrol.started";       // scene B is up: students may fill the classroom
        public const string PatrolEnded = "patrol.ended";           // leaving patrol: classroom empties
    }
}
