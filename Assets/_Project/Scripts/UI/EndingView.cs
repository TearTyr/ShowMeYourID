using System.Collections.Generic;
using System.Text;
using Hollowbrook.Core;
using Hollowbrook.Scenes;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    public class EndingView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private Text _title, _desc, _summary;
        private Text[] _badges = new Text[3];
        private Button _again;
        private System.Action _againAction = () => { };

        private const string WinTitle = "SURVIVING HOLLOWBROOK";
        private const string LoseTitle = "ANOTHER CUSTODIAN";
        private const string SecretTitle = "THE TRUTH BEHIND HOLLOWBROOK";

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            var bg = UIFactory.Panel(transform, "bg", Color.black);
            UIFactory.AnchorsFull(bg.rectTransform);

            _title = UIFactory.Label(transform, "title", "", 42, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(1100, 60), new Vector2(0, -74));

            _desc = UIFactory.Label(transform, "desc", "", 17, UIFactory.InkDim, TextAnchor.UpperCenter);
            UIFactory.Anchor(_desc.rectTransform, new Vector2(0.5f, 1f), new Vector2(860, 96), new Vector2(0, -150));

            var panel = UIFactory.BorderedPanel(transform, "summarypanel", new Color(0.06f, 0.06f, 0.07f, 0.96f), UIFactory.Border, 2);
            UIFactory.Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(700, 280), new Vector2(0, -70));
            var sumHead = UIFactory.Label(panel, "head", "DECISION SUMMARY", 16, UIFactory.InkFaint, TextAnchor.MiddleCenter);
            UIFactory.Anchor(sumHead.rectTransform, new Vector2(0.5f, 1f), new Vector2(400, 26), new Vector2(0, -14));
            _summary = UIFactory.Label(panel, "body", "", 15, UIFactory.InkDim);
            var srt = _summary.rectTransform;
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 1);
            srt.offsetMin = new Vector2(36, 18); srt.offsetMax = new Vector2(-36, -44);

            string[] badgeNames = { "WIN", "LOSE", "SECRET" };
            for (int i = 0; i < 3; i++)
            {
                var b = UIFactory.Label(transform, "badge" + i, "[ " + badgeNames[i] + " ]", 15, UIFactory.InkFaint, TextAnchor.MiddleCenter);
                UIFactory.Anchor(b.rectTransform, new Vector2(0.5f, 0f), new Vector2(160, 24), new Vector2(-170 + i * 170, 96));
                _badges[i] = b;
            }

            var menu = UIFactory.StandardButton(transform, "menu", "MAIN MENU", () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.7f);
                _gameRoot.QuitToMenu(false);
            }, 220, 46);
            UIFactory.Anchor(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(220, 46), new Vector2(-120, 38));
            _again = UIFactory.StandardButton(transform, "again", "PLAY AGAIN", () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_bell", 0.7f);
                _againAction();
            }, 220, 46);
            UIFactory.Anchor(_again.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(220, 46), new Vector2(120, 38));

            gameObject.SetActive(false);
        }

        private void SetAgainButton(GameSession session, bool lost, bool night6)
        {
            string label = "PLAY AGAIN";
            _againAction = () => _gameRoot.StartNewGame(GameMode.Normal);
            if (lost && night6)
            {
                label = "RETRY NIGHT 6";
                _againAction = () => _gameRoot.StartNewGame(GameMode.Challenge);
            }
            else if (lost && _gameRoot.HasSave())
            {
                label = "RETRY NIGHT " + Mathf.Min(session.CurrentShift, ShiftPlan.TotalShifts);
                _againAction = () => _gameRoot.ContinueGame();
            }
            var text = _again.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
            _again.gameObject.SetActive(!night6 || lost);
        }

        public void Populate(EndingRules.Evaluation eval, GameSession session, RecordsData records)
        {
            switch (eval.Ending)
            {
                case EndingType.Secret:
                    _title.text = SecretTitle; _title.color = new Color(0.85f, 0.72f, 0.4f); break;
                case EndingType.Lose:
                    _title.text = LoseTitle; _title.color = UIFactory.AccentBright; break;
                default:
                    _title.text = WinTitle; _title.color = UIFactory.Ink; break;
            }
            _desc.text = eval.ReasonText;

            // night 6 is its own short story: no badges. The button after a loss plays the night that
            // was lost again; after a win it starts a new game (night 6 has no second button).
            bool night6 = session.Mode == GameMode.Challenge;
            bool lost = eval.Ending == EndingType.Lose;
            foreach (var b in _badges) b.gameObject.SetActive(!night6);
            SetAgainButton(session, lost, night6);
            if (night6)
            {
                var n6 = new StringBuilder();
                n6.Append("Night:                      6 (the Challenge)\n");
                n6.Append("Administrator:              ").Append(session.AdminAllowed ? "ALLOWED IN (fatal mistake)" : session.Hunt == HuntState.Lost ? "REJECTED - but he reached the server door" : session.AdminRejected ? "REJECTED - driven off until dawn" : "never reached the glass").Append('\n');
                n6.Append("Final sanity:               ").Append((int)session.Sanity).Append("  (").Append(session.SanityStage).Append(")");
                _summary.text = n6.ToString();
                gameObject.SetActive(true);
                return;
            }

            var sb = new StringBuilder();
            sb.Append("Nights survived:            ").Append(session.CompletedShifts).Append(" / 5\n");
            sb.Append("Visitors processed:         ").Append(session.Processed).Append('\n');
            sb.Append("Correct decisions:          ").Append(session.CorrectDecisions).Append('\n');
            sb.Append("Anomalies rejected:         ").Append(session.AnomaliesRejected).Append('\n');
            sb.Append("False rejections:           ").Append(session.FalseRejections).Append(" / 3\n");
            sb.Append("Anomaly breaches:           ").Append(session.Breaches).Append(" / 3\n");
            sb.Append("Administrator:              ").Append(session.AdminAllowed ? "ALLOWED IN (fatal mistake)" : session.Hunt == HuntState.Lost ? "REJECTED - but he reached the server door" : session.AdminRejected ? "REJECTED - impostor driven off" : "never arrived").Append('\n');
            sb.Append("Findings:                   ").Append(session.Clues.FoundCount).Append(" / 10  (").Append(session.Clues.OrderIntact ? "intended sequence intact" : "sequence broken").Append(")\n");
            sb.Append("Final sanity:               ").Append((int)session.Sanity).Append("  (").Append(session.SanityStage).Append(")\n");
            sb.Append("Score:                      ").Append(session.Score).Append("    Best streak: ").Append(session.BestStreak);
            _summary.text = sb.ToString();

            RefreshBadges(records);
            gameObject.SetActive(true);
        }

        private void RefreshBadges(RecordsData records)
        {
            if (records == null) return;
            _badges[0].color = records.EndingsSeen.Contains("win") ? UIFactory.Ink : UIFactory.InkFaint;
            _badges[1].color = records.EndingsSeen.Contains("lose") ? UIFactory.AccentBright : UIFactory.InkFaint;
            _badges[2].color = records.EndingsSeen.Contains("secret") ? new Color(0.85f, 0.72f, 0.4f) : UIFactory.InkFaint;
        }

        public void Show() { }
        public void Hide() => gameObject.SetActive(false);
    }
}
