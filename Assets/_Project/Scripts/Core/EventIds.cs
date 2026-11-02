namespace Hollowbrook.Core
{
    public static class EventIds
    {
        public const string StateChanged = "state.changed";

        public const string ObjectiveChanged = "objective.changed";// Text: objective line

        public const string VerdictStamped = "visitor.verdict";    // Data: InspectionRules.Outcome

        public const string RoomEntered = "room.entered";          // Data: RoomId
        public const string LightModeChanged = "light.mode";       // Data: bool - room lights on/off (scripted flicker) or the peek flashlight on/off
        public const string ClueDiscovered = "clue.discovered";    // Data: ClueDiscoveryResult
        public const string InteractionPrompt = "interact.prompt"; // Text: prompt or ""

        public const string SanityChanged = "sanity.changed";      // Value: 0..100
        public const string SanityStageChanged = "sanity.stage";   // Data: SanityStage

        public const string CaptionRequested = "caption.show";     // Text: caption, Value: seconds
        public const string SettingsChanged = "settings.changed";  // Data: SettingsData
        public const string Notification = "ui.notification";
        public const string AnomalyCaught = "anomaly.caught";      // Text: toast message
        public const string AnomalyRevealed = "anomaly.revealed";    // Data: kind / "hallucination"
        public const string BatteryChanged = "light.battery";      // Value: 0..1 charge
        public const string AnomalyStateChanged = "anomaly.state"; // Data: AnomalyState name
        public const string TutorialChanged = "tutorial.changed";  // Value: the new TutorialStep as a number
    }
}
