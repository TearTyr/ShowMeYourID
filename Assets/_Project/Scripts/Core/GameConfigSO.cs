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

        [Header("Horror pacing")]
        [Tooltip("Base chance per second that a wandering anomaly spawns in an entered room (shift-scaled).")]
        public float anomalySpawnChancePerSecond = 0.05f;
        public float coverHugCatchSeconds = 2.6f;
        public float sprintTelegraphSeconds = 0.9f;

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
