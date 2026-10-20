namespace Hollowbrook.Logic
{
    public enum GameMode { Normal, Endless, Challenge }

    public enum Verdict { None, Allow, Reject }

    public enum EndingType { None, Win, Lose, Secret }

    public enum SanityStage { Stable, Uneasy, Shaken, Breaking }

    public enum LightMode { Off, Sweep, Focus }

    public enum AnomalyBehaviorKind { Blink, Slide, Sprint, CoverHug }

    public enum RoomId { Security, Hallway, Classroom1, Classroom2 }

    public enum QuestionId { Course, Year, Status, FirstClassroom }

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

    public enum ToneTag { Normal, Hesitant, Flat, TooSmooth, Echoing }

    public enum AnomalyBehavior
    {
        Idle,
        ApproachingWindow,
        WaitingOutside,
        InsideWandering,
        ReachedServerRoom,
    }

    public enum AppViewState
    {
        Boot, Loading, MainMenu, ModeSelect, Settings, Controls, Credits,
        ShiftIntro, Checkpoint, Exploration, Map, Pause, ShiftResults,
        AdminEncounter, SecurityRoom, Ending, EndlessResults,
        RoomPeek,   // flashlight peek into a classroom from the doorway (no room entry)
    }
}
