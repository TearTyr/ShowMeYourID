using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    public static class ShiftPlan
    {
public static int TotalShifts => GameRules.Current.Nights.Length;
        /// <summary>The Challenge: one night after the story, unlocked by finishing it. Not part of Build().</summary>
        public static int Night6Number => TotalShifts + 1;

        /// <summary>The last night a mode can reach: the story's last, or the Challenge.</summary>
        public static int LastNight(GameMode mode) => mode == GameMode.Challenge ? Night6Number : TotalShifts;

        public static ShiftDef Night6Def()
        {
            var night = GameRules.Current.ChallengeNight.Copy();
            night.Number = Night6Number;
            return night;
        }

        /// <summary>The story's nights, numbered by their row in the table.</summary>
        public static List<ShiftDef> Build()
        {
            var nights = new List<ShiftDef>();
            foreach (var row in GameRules.Current.Nights)
            {
                var night = row.Copy();
                night.Number = nights.Count + 1;
                nights.Add(night);
            }
            return nights;
        }

        public static ShiftDef Get(int number)
        {
            var all = Build();
            foreach (var s in all) if (s.Number == number) return s;
            return null;
        }
    }

    public static class SanityModel
    {
        public const float Max = 100f;
        public const float Min = 0f;

public static SanityStage StageFor(float sanity)
        {
            var r = GameRules.Current;
            if (sanity >= r.StableFrom) return SanityStage.Stable;
            if (sanity >= r.UneasyFrom) return SanityStage.Uneasy;
            if (sanity >= r.ShakenFrom) return SanityStage.Shaken;
            return SanityStage.Breaking;
        }

        public static float HallucinationChancePerSecond(SanityStage stage)
        {
            var r = GameRules.Current;
            switch (stage)
            {
                case SanityStage.Uneasy: return r.HallucinationUneasy;
                case SanityStage.Shaken: return r.HallucinationShaken;
                case SanityStage.Breaking: return r.HallucinationBreaking;
                default: return 0f;
            }
        }

        // Resting at the desk (the window is empty) recovers sanity; a student at the window
        // wears it down slowly; patrol in the dark wears it down a little faster. Wrong
        // decisions do not cost sanity directly: they add a SanityStacks drain stack.
        public static float SecurityRoomRecoveryPerSecond => GameRules.Current.SecurityRoomRecoveryPerSecond;
        public static float DeskDrainPerSecond => GameRules.Current.DeskDrainPerSecond;
        public static float DarknessDrainPerSecond => GameRules.Current.DarknessDrainPerSecond;
        // A correct decision refills sanity gradually rather than in one jump.
        public static float RefillOnCorrect => GameRules.Current.RefillOnCorrect;
        public static float RefillPerSecond => GameRules.Current.RefillPerSecond;

        public static float Clamp(float v) => v < Min ? Min : (v > Max ? Max : v);
    }
}
