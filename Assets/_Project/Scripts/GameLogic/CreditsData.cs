using System.Collections.Generic;
using System.Text;

namespace Hollowbrook.Logic
{
    // The credits, in one place. To change a name or a role, edit the lists below; the credits
    // screen builds its text from them.
    public static class CreditsData
    {
        public const string Studio = "Twin Studio";

        public struct Section
        {
            public string Role;
            public string[] People;
            public Section(string role, params string[] people) { Role = role; People = people; }
        }

        public static readonly Section[] Team =
        {
            new Section("PROJECT DIRECTOR AND NARRATIVE", "Jessie Mariel De Mesa"),
            new Section("PROGRAMMING", "Elijah Japeth Ramos", "John Perilla"),
            new Section("ART", "Daniel Steven Solitaria", "Jessie Mariel De Mesa"),
            new Section("AUDIO", "Clarence Manalo"),
            new Section("PROJECT ASSISTANTS", "Elijah Japeth Ramos", "Clarence Manalo"),
            new Section("MARKETING AND BRANDING", "Diosdado Fabelico II", "Daniella Garing"),
            new Section("STORYBOARD", "All members"),
        };

        // Third party pieces the game ships with.
        public static readonly string[] Thanks =
        {
            "Fonts: Bahnschrift (Microsoft), Chiller (Monotype), Barlow Condensed, Special Elite",
            "and Caveat Brush (SIL Open Font License).",
            "Made with Unity.",
        };

        /// <summary>The credits as plain text, one role per line followed by its people.</summary>
        public static string Build()
        {
            var sb = new StringBuilder();
            foreach (var s in Team)
                sb.Append(s.Role).Append("  -  ").Append(string.Join("  ·  ", s.People)).Append('\n');
            sb.Append('\n');
            foreach (var line in Thanks) sb.Append(line).Append('\n');
            return sb.ToString().TrimEnd('\n');
        }
    }
}
