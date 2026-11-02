namespace Hollowbrook.Logic
{
    public enum GameMode { Normal, Challenge, Lockdown }   // Challenge is Night 6, Lockdown is the all-at-once night

    public enum Verdict { None, Allow, Reject }

    public enum EndingType { None, Win, Lose, Secret }

    public enum SanityStage { Stable, Uneasy, Shaken, Breaking }

    public enum LightMode { Off, Sweep, Focus }

    /// <summary>Where the administrator hunt stands (saved with the run).</summary>
    public enum HuntState { None, Active, Won, Lost, Ready }

    public enum AnomalyBehaviorKind { Blink, Slide, Sprint, CoverHug }

    public enum RoomId { Security, Hallway, Classroom1, Classroom2, ComfortRoom, ComfortRoom2 }

    public enum DefectType
    {
        None = 0,
        PhotoMismatch,        // window face clearly differs from ID photo
        PhotoSubtleMismatch,  // single feature differs (harder, shift 3+)
        ExpiredId,            // expiration date is before the shift date
        IdFormatInvalid,      // ID number breaks the HB-YYYY-NNN format
        YearMismatch,         // year level on ID != year level on record
        CourseMismatch,       // course on ID != course on record
        StatusMismatch,       // regular/transferee on ID != record
        RecordNameMismatch,   // name on record != name on ID
        NotInDatabase,        // ID number not in the student registry (ghost student)
        DuplicateId,          // ID number already used by an earlier visitor (shift 3+)
        ClassroomMismatch,    // papers are clean; the answer to "which classroom?" disagrees with the file
        SpokenMismatch,       // papers are clean; the answer to course, year or status disagrees with the card
        SlipMismatch,           // the paper slip disagrees with the ID card (eye-check)
    }


    public enum AppViewState
    {
        Boot, Loading, MainMenu, Settings, Controls, Credits,
        ShiftIntro, Checkpoint, Exploration, Map, Pause, ShiftResults,
        Ending,
        RoomPeek,   // flashlight peek into a classroom from the doorway (no room entry)
    }
}
