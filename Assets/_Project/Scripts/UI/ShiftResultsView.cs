using System.Collections.Generic;
using System.Text;
using Hollowbrook.Core;
using Hollowbrook.Scenes;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    public class ShiftResultsView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private Text _title, _body, _rating;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            UIFactory.Dimmer(transform, "dim", 0.9f);
            // the open booth glows behind the shift summary (dimmed: the numbers are the star)
            var kitBg = UIFactory.Img(transform, "kitbg", ProceduralArt.Get("sec_background_open"),
                new Color(1f, 1f, 1f, 0.30f));
            UIFactory.AnchorsFull(kitBg.rectTransform);
            kitBg.raycastTarget = false;
            var panel = UIFactory.BorderedPanel(transform, "panel", new Color(0.05f, 0.05f, 0.06f, 0.98f), UIFactory.Border, 2);
            UIFactory.Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(760, 540), Vector2.zero);
            _title = UIFactory.Label(panel, "title", "", 30, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(700, 44), new Vector2(0, -40));
            _body = UIFactory.Label(panel, "body", "", 17, UIFactory.InkDim);
            var brt = _body.rectTransform;
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 1);
            brt.offsetMin = new Vector2(60, 120); brt.offsetMax = new Vector2(-60, -96);
            _rating = UIFactory.Label(panel, "rating", "", 22, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_rating.rectTransform, new Vector2(0.5f, 0f), new Vector2(700, 34), new Vector2(0, 92));
            var cont = UIFactory.StandardButton(panel, "cont", "CONTINUE ▸", () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_click", 0.8f);
                _gameRoot.AdvanceToNextShift();
            }, 240, 46);
            UIFactory.Anchor(cont.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(240, 46), new Vector2(0, 36));
            gameObject.SetActive(false);
        }

        public void Populate(GameSession session, ShiftDef justFinished)
        {
            _title.text = "SHIFT " + justFinished.Number + " COMPLETE - " + justFinished.DateLabel;
            int shiftVisitors = justFinished.VisitorCount + (justFinished.HasAdministrator ? 1 : 0);
            var sb = new StringBuilder();
            sb.Append("Visitors processed tonight:  ").Append(shiftVisitors).Append('\n');
            sb.Append("Correct decisions (total):     ").Append(session.CorrectDecisions).Append(" / ").Append(session.Processed).Append('\n');
            sb.Append("Anomalies rejected (total):    ").Append(session.AnomaliesRejected).Append('\n');
            sb.Append("False rejections:              ").Append(session.FalseRejections).Append(" / 3\n");
            sb.Append("Anomaly breaches:              ").Append(session.Breaches).Append(" / 3\n");
            sb.Append("Sanity at clock-out:           ").Append(((int)session.Sanity)).Append("  (").Append(session.SanityStage).Append(")\n");
            sb.Append("Findings discovered:           ").Append(session.Clues.FoundCount).Append(" / 10");
            sb.Append(session.Clues.OrderIntact ? "   (sequence intact)\n" : "   (sequence broken)\n");
            if (justFinished.Number == 5)
                sb.Append("District administrator:        ").Append(session.AdminRejected ? "REJECTED - identified as an impostor\n" : "-\n");
            _body.text = sb.ToString();

            int mistakes = session.FalseRejections + session.Breaches;
            _rating.text = mistakes == 0 ? "DISTRICT EVALUATION: EXEMPLARY" :
                           mistakes <= 2 ? "DISTRICT EVALUATION: SATISFACTORY" :
                           "DISTRICT EVALUATION: UNDER REVIEW";
            _rating.color = mistakes == 0 ? UIFactory.Ink : mistakes <= 2 ? UIFactory.InkDim : UIFactory.AccentBright;
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
