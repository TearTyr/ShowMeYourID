namespace Hollowbrook.Core
{
    /// <summary>Canonical event ids. Referencing constants (never raw strings) keeps
    /// publishers and subscribers in sync at compile time.</summary>
    public static class EventIds
    {
        // app / flow
        public const string StateChanged = "state.changed";
        public const string PauseToggled = "pause.toggled";
        public const string GameSaved = "game.saved";
        public const string GameLoaded = "game.loaded";
        public const string QuitToMenu = "flow.quit_to_menu";

        // progression
        public const string ShiftStarted = "shift.started";        // Data: ShiftDef
        public const string ShiftEnded = "shift.ended";            // Data: ShiftDef
        public const string ObjectiveChanged = "objective.changed";// Text: objective line
        public const string RunTimerTick = "run.timer_tick";       // Value: seconds left
        public const string MutationChanged = "run.mutation";      // Data: RuleMutationDef
        public const string EndingReached = "ending.reached";      // Data: EndingRules.Evaluation

        // checkpoint
        public const string VisitorArrived = "visitor.arrived";    // Data: VisitorData
        public const string DocumentsShown = "visitor.documents";  // Data: VisitorData
        public const string QuestionAsked = "visitor.question";    // Data: QuestionId
        public const string ScanPerformed = "visitor.scan";        // Data: RosterFactory.ScanResult
        public const string VerdictStamped = "visitor.verdict";    // Data: InspectionRules.Outcome
        public const string VisitorResolved = "visitor.resolved";  // Data: VisitorData

        // exploration
        public const string RoomEntered = "room.entered";          // Data: RoomId
        public const string RoomExited = "room.exited";            // Data: RoomId
        public const string LightModeChanged = "light.mode";       // Data: LightMode
        public const string ClueDiscovered = "clue.discovered";    // Data: ClueDiscoveryResult
        public const string InteractionPrompt = "interact.prompt"; // Text: prompt or ""

        // horror
        public const string SanityChanged = "sanity.changed";      // Value: 0..100
        public const string SanityStageChanged = "sanity.stage";   // Data: SanityStage
        public const string AnomalySighted = "anomaly.sighted";    // Data: AnomalyBehaviorKind
        public const string AnomalyCaught = "anomaly.caught";      // Data: AnomalyBehaviorKind
        public const string ScareTriggered = "scare.triggered";    // Text: kind
        public const string HallucinationTriggered = "hallucination.triggered"; // Text: kind

        // presentation services
        public const string SfxRequested = "audio.sfx";            // Text: sfx key, Value: volume 0..1
        public const string MusicContextRequested = "audio.music"; // Text: context key
        public const string CaptionRequested = "caption.show";     // Text: caption, Value: seconds
        public const string SettingsChanged = "settings.changed";  // Data: SettingsData
        public const string Notification = "ui.notification";      // Text: toast message
        public const string TrackingChanged = "anomaly.tracking";    // Value: 0..1 meter
        public const string AnomalyRevealed = "anomaly.revealed";    // Data: kind / "hallucination"
        public const string TokenCollected = "anomaly.token";      // Value: tokens held this run
        public const string BatteryChanged = "light.battery";      // Value: 0..1 charge
        public const string AnomalyStateChanged = "anomaly.state"; // Data: AnomalyState name
    }
}
