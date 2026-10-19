using UnityEngine;

namespace Hollowbrook.Core
{
    /// <summary>Tuning data as a ScriptableObject (per master-prompt architecture).
    /// The bootstrap tries Resources.Load first and falls back to a procedural default
    /// instance, so the game always boots even with no .asset files present.
    /// Gameplay RULES live in the pure-logic layer (Hollowbrook.Logic) — this asset only
    /// carries presentation/pacing knobs designers may want to tweak in the editor.</summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ShowMeYourID/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Checkpoint pacing")]
        [Tooltip("Seconds the result banner stays up after a verdict.")]
        public float verdictBannerSeconds = 3.2f;
        [Tooltip("Seconds between visitors (breathing room / dread pacing).")]
        public float betweenVisitorsSeconds = 1.6f;
        [Tooltip("In-story shift starts at 22:00 and ends at 02:00.")]
        public int shiftStartHour = 22;

        [Header("Exploration")]
        public float walkSpeed = 3.4f;
        public float runSpeed = 6.0f;
        public float interactRange = 1.35f;
        public float roomHalfWidthHallway = 14f;
        public float roomHalfWidthClassroom = 9f;

        [Header("Flashlight battery")]
        [Tooltip("Percent per second while the wide Sweep beam is on.")]
        public float batteryDrainSweepPerSecond = 2.5f;
        [Tooltip("Percent per second while the tight Focus beam is on.")]
        public float batteryDrainFocusPerSecond = 6f;
        [Tooltip("Percent per second recovered in darkness with the beam off.")]
        public float batteryRecoverOffPerSecond = 4f;
        [Tooltip("Percent per second recovered with the beam off in a lit (safe) room.")]
        public float batteryRecoverLitPerSecond = 8f;
        [Tooltip("Charge needed before a dead beam can switch on again (no soft-lock).")]
        [Range(0f, 1f)] public float batteryRebootFraction = 0.2f;

        [Header("Horror pacing")]
        [Tooltip("Base chance per second that a wandering anomaly spawns in an entered room (shift-scaled).")]
        public float anomalySpawnChancePerSecond = 0.05f;
        public float coverHugCatchSeconds = 2.6f;
        public float sprintTelegraphSeconds = 0.9f;

        [Header("Exhibition (5-10 minute booth cut)")]
        [Tooltip("One curated night (4 visitors, 1 anomaly) then the demo closes. Off = full five-shift story.")]
        public bool exhibitionMode = true;
        public float exhibitionBannerSeconds = 2.2f;
        public float exhibitionBetweenVisitors = 1.0f;

        [Header("Shift clock")]
        [Tooltip("In-story minutes per real second while a shift is running, in ANY scene.")]
        public float clockMinutesPerSecond = 0.25f;

        [Header("Presentation")]
        public int canvasReferenceWidth = 1280;
        public int canvasReferenceHeight = 720;
        public float dialogueCharsPerSecond = 42f;

        public static GameConfigSO CreateDefault()
        {
            var c = CreateInstance<GameConfigSO>();
            c.name = "GameConfig (procedural default)";
            return c;
        }

        public static GameConfigSO LoadOrDefault()
        {
            GameConfigSO loaded = null;
            try { loaded = Resources.Load<GameConfigSO>("GameConfig"); }
            catch (System.Exception e) { Debug.LogWarning("[GameConfig] Resources.Load failed: " + e.Message); }
            if (loaded == null)
            {
                loaded = CreateDefault();
                Debug.Log("[GameConfig] No asset found — using procedural defaults.");
            }
            return loaded;
        }
    }
}
