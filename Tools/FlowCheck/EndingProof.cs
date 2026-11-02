using System;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.Scenes;
using UnityEngine;

namespace FlowCheck
{
    // The end result screen: built from kit art, every piece inside the 1280x720 frame, nothing on top of anything else.
    internal static partial class Program
    {
        // where an object of the ending screen sits, in frame coordinates (x right, y down)
        private static Rect EndingRect(GameRoot root, string name)
        {
            var rt = FindDeep(root.Ending.transform, name).GetComponent<RectTransform>();
            return new Rect(rt.anchoredPosition.x, -rt.anchoredPosition.y, rt.sizeDelta.x, rt.sizeDelta.y);
        }

        private static bool InsideFrame(Rect r) => r.x >= 0f && r.y >= 0f && r.xMax <= 1280f && r.yMax <= 720f;

        private static bool Apart(Rect a, Rect b) => a.xMax <= b.x || b.xMax <= a.x || a.yMax <= b.y || b.yMax <= a.y;

        private static void EndingProof(GameRoot root)
        {
            Console.WriteLine("== ending screen: kit look, laid out inside the frame ==");
            FreshGame(root);
            root.Session.CompletedShifts = ShiftPlan.TotalShifts;
            root.Session.Score = 900;
            root.Session.Sanity = 99f;
            root.TriggerEnding(new EndingRules.Evaluation { Ending = EndingType.Win, ReasonKey = "win", ReasonText = "You walk out of Hollowbrook alive." });
            Check(root.State == AppViewState.Ending && root.Ending != null && root.Ending.gameObject.activeSelf, "END: the ending screen is up");

            var card = FindDeep(root.Ending.transform, "card").GetComponent<UnityEngine.UI.Image>();
            Check(card.sprite != null && ProceduralArt.IsFileBacked(card.sprite.name),
                "END: the report card is the kit's torn paper panel (a real art file, not a drawn box)");
            var pill = FindDeep(root.Ending.transform, "again").Find("bg").GetComponent<UnityEngine.UI.Image>();
            Check(pill.sprite != null && pill.sprite.name == "sec_wide_pill",
                "END: the buttons wear the kit's wide pill, like PATROL HALLWAY and END SHIFT");

            var blocks = new[] { "title", "desc", "card", "badge0", "badge1", "badge2", "menu", "again" };
            bool inside = true;
            string out1 = "";
            foreach (var name in blocks)
                if (!InsideFrame(EndingRect(root, name))) { inside = false; out1 = name; }
            Check(inside, "END: every block is inside the 1280x720 frame" + (inside ? "" : " (" + out1 + ")"));

            bool apart = true;
            string clash = "";
            for (int i = 0; i < blocks.Length; i++)
                for (int j = i + 1; j < blocks.Length; j++)
                    if (!Apart(EndingRect(root, blocks[i]), EndingRect(root, blocks[j]))) { apart = false; clash = blocks[i] + " / " + blocks[j]; }
            Check(apart, "END: no block sits on top of another" + (apart ? "" : " (" + clash + ")"));

            var cardRect = EndingRect(root, "card");
            bool rowsInCard = true;
            for (int i = 0; i < 8; i++)
            {
                var row = FindDeep(root.Ending.transform, "rowvalue" + i);
                if (!row.gameObject.activeSelf) continue;
                var r = EndingRect(root, "rowvalue" + i);
                var l = EndingRect(root, "rowlabel" + i);
                if (r.yMax > cardRect.yMax || r.xMax > cardRect.xMax || l.x < cardRect.x) rowsInCard = false;
            }
            Check(rowsInCard, "END: every row of the list sits inside the report card");

            string summary = root.Ending.SummaryText;
            Check(summary.Contains("NIGHTS SURVIVED: 5 / 5") && summary.Contains("SCORE: 900") && summary.Contains("Best streak"),
                "END: the big numbers and the list carry the nights, the score and the best streak (" + summary.Replace("\n", " | ") + ")");
            int shownRows = 0;
            for (int i = 0; i < 8; i++) if (FindDeep(root.Ending.transform, "rowvalue" + i).gameObject.activeSelf) shownRows++;
            Check(shownRows == 8, "END: a normal ending lists eight rows (" + shownRows + ")");
            Check(FindDeep(root.Ending.transform, "badge0").gameObject.activeSelf &&
                  FindDeep(root.Ending.transform, "badge0").Find("mark").gameObject.activeSelf &&
                  !FindDeep(root.Ending.transform, "badge1").Find("mark").gameObject.activeSelf,
                "END: the three tags show and the one just reached (WIN) carries a mark");

            // night 6 and Lockdown have their own shorter card and no tags
            FreshGame(root);
            root.StartNewGame(GameMode.Challenge);
            DeskScene.Build(root);
            root.Session.AdminAllowed = true;
            root.TriggerEnding(root.Session.EvaluateEnding());
            Check(!FindDeep(root.Ending.transform, "badge0").gameObject.activeSelf, "END: night 6 shows no ending tags");
            Check(root.Ending.SummaryText.Contains("Night 6") && InsideFrame(EndingRect(root, "card")), "END: night 6 has its own short card");
            FreshGame(root);
        }
    }
}
