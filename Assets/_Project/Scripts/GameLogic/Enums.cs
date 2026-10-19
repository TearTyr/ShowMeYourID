namespace Hollowbrook.Logic
{
    // Part of the gamelogic folder.
    public enum GameMode { Normal, Endless, Challenge }

    // Part of the gamelogic folder.
    public enum Verdict { None, Allow, Reject }

    // Part of the gamelogic folder.
    public enum EndingType { None, Win, Lose, Secret }

    // Part of the gamelogic folder.
    public enum SanityStage { Stable, Uneasy, Shaken, Breaking }

    // Part of the gamelogic folder.
    public enum LightMode { Off, Sweep, Focus }

    // Part of the gamelogic folder.
    public enum AnomalyBehaviorKind { Blink, Slide, Sprint, CoverHug }

    // Part of the gamelogic folder.
    public enum RoomId { Security, Hallway, Classroom1, Classroom2 }

    // Part of the gamelogic folder.
    public enum QuestionId { Course, Year, Status, FirstClassroom }

    // Part of the gamelogic folder.
    public enum DefectType
    {
        None = 0,
        PhotoMismatch,        // window face clearly differs from ID photo
        PhotoSubtleMismatch,  // single feature differs (harder, shift 3+)
        ExpiredId,            // expiration date is before the shift date
        IdFormatInvalid,      // ID number breaks the HB-YYYY-NNNN format
        YearMismatch,         // year level on ID != year level on record
        CourseMismatch,       // course on ID != course on record
        StatusMismatch,       // regular/transferee on ID != record
        RecordNameMismatch,   // name on record != name on ID
        NotInDatabase,        // ID number not in the student registry (ghost student)
        DuplicateId,          // ID number already used by an earlier visitor (shift 3+)
        BehaviorOddity,       // documents clean; questioning/behavior gives it away
    }

    // Part of the gamelogic folder.
    public enum ToneTag { Normal, Hesitant, Flat, TooSmooth, Echoing }

    // Part of the gamelogic folder.
    public enum AnomalyBehavior
    {
        Idle,
        ApproachingWindow,
        WaitingOutside,
        InsideWandering,
        ReachedServerRoom,
    }

    // Part of the gamelogic folder.
    public enum AppViewState
    {
        Boot, Loading, MainMenu, ModeSelect, Settings, Controls, Credits,
        ShiftIntro, Checkpoint, Exploration, Map, Pause, ShiftResults,
        AdminEncounter, SecurityRoom, Ending, EndlessResults,
    }
}
