using System.Collections.Generic;
using System.Text;
using Hollowbrook.Core;
using Hollowbrook.Bootstrap;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    public class MapView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private RectTransform _hereMarker;
        private RectTransform _sheet;
        private readonly List<GameObject> _clueDots = new List<GameObject>();
        private Text _hint;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            UIFactory.Dimmer(transform, "dim", 0.86f);
            var head = UIFactory.Label(transform, "head", "HOLLOWBROOK COLLEGE — GROUND FLOOR", 28, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(800, 40), new Vector2(0, -64));

            var sheet = UIFactory.BorderedPanel(transform, "sheet", new Color(0.07f, 0.07f, 0.075f, 0.98f), UIFactory.Border, 2);
            UIFactory.Anchor(sheet, new Vector2(0.5f, 0.5f), new Vector2(940, 500), new Vector2(0, 10));
            _sheet = sheet;

            RoomBox(sheet, new Vector2(-360, 90), new Vector2(170, 130), "SECURITY\nCheckpoint", RoomId.Security, out _);
            RoomBox(sheet, new Vector2(-60, 90), new Vector2(430, 110), "MAIN HALLWAY", RoomId.Hallway, out _);
            RoomBox(sheet, new Vector2(-110, 210), new Vector2(200, 120), "CLASSROOM 1\nRoom 101", RoomId.Classroom1, out _);
            RoomBox(sheet, new Vector2(120, -30), new Vector2(200, 120), "CLASSROOM 2\nRoom 202", RoomId.Classroom2, out _);
            RoomBox(sheet, new Vector2(330, 90), new Vector2(180, 130), "SERVER /\nIDENTIFICATION ROOM\n— SEALED —", RoomId.Security, out _, sealedRoom: true);

            Line(sheet, new Vector2(-275, 90), new Vector2(-175, 90));
            Line(sheet, new Vector2(-110, 145), new Vector2(-110, 150));
            Line(sheet, new Vector2(20, 145), new Vector2(20, 30));
            Line(sheet, new Vector2(120, 90), new Vector2(240, 30));
            Line(sheet, new Vector2(155, 90), new Vector2(240, 90));

            _hereMarker = UIFactory.NewUI(sheet, "here");
            var dot = _hereMarker.gameObject.AddComponent<Image>();
            dot.sprite = ProceduralArt.Get("solid_white");
            dot.color = UIFactory.AccentBright;
            _hereMarker.sizeDelta = new Vector2(14, 14);
            var hereLbl = UIFactory.Label(_hereMarker, "lbl", "YOU", 12, UIFactory.AccentBright, TextAnchor.MiddleCenter);
            UIFactory.Anchor(hereLbl.rectTransform, new Vector2(0.5f, 0f), new Vector2(60, 18), new Vector2(0, -20));

            _hint = UIFactory.Label(transform, "hint", "", 15, UIFactory.InkDim, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(900, 26), new Vector2(0, 74));

            var close = UIFactory.StandardButton(transform, "close", "CLOSE [M]", () => _gameRoot.CloseMap(), 180, 42);
            UIFactory.Anchor(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(180, 42), new Vector2(0, 34));

            gameObject.SetActive(false);
        }

        private static void Line(RectTransform parent, Vector2 a, Vector2 b)
        {
            var img = UIFactory.Img(parent, "line", ProceduralArt.Get("solid_white"), UIFactory.Border);
            float dx = b.x - a.x, dy = b.y - a.y;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            UIFactory.Anchor(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(len, 3), new Vector2((a.x + b.x) / 2f, (a.y + b.y) / 2f));
            img.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
        }

        private void RoomBox(RectTransform sheet, Vector2 pos, Vector2 size, string label, RoomId target,
            out RectTransform box, bool sealedRoom = false)
        {
            box = UIFactory.BorderedPanel(sheet, "room_" + label.Replace("\n", ""), 
                sealedRoom ? new Color(0.10f, 0.05f, 0.05f, 0.98f) : new Color(0.11f, 0.11f, 0.12f, 0.98f),
                sealedRoom ? UIFactory.Accent : UIFactory.Border, 2);
            UIFactory.Anchor(box, new Vector2(0.5f, 0.5f), size, pos);
            var t = UIFactory.Label(box, "label", label, 15, sealedRoom ? UIFactory.AccentBright : UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.AnchorsFull(t.rectTransform);
            if (!sealedRoom)
            {
                var btn = box.gameObject.AddComponent<Button>();
                btn.targetGraphic = box.GetComponent<Image>();
                RoomId captured = target;
                btn.onClick.AddListener(() =>
                {
                    _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.5f);
                    _gameRoot.TravelViaMap(captured);
                });
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        private void Refresh()
        {
            foreach (var d in _clueDots) Destroy(d);
            _clueDots.Clear();

            var sheet = _sheet;
            if (sheet == null) return;

            RoomId here = _gameRoot.State == AppViewState.Exploration && _gameRoot.Views != null && _gameRoot.Views.Current != null
                ? _gameRoot.Views.Current.Id : RoomId.Security;
            _hereMarker.anchoredPosition = HerePos(here);

            var roomCounts = new Dictionary<RoomId, int>();
            foreach (var key in _gameRoot.Session.Clues.FoundKeys)
            {
                var def = _gameRoot.Session.Clues.Get(key);
                if (def == null) continue;
                int idx = roomCounts.TryGetValue(def.Room, out var c) ? c : 0;
                roomCounts[def.Room] = idx + 1;
                var dotGo = new GameObject("dot_" + key);
                dotGo.transform.SetParent(sheet, false);
                var img = dotGo.AddComponent<Image>();
                img.sprite = ProceduralArt.Get("solid_white");
                img.color = new Color(0.78f, 0.18f, 0.2f, 0.95f);
                var rt = (RectTransform)dotGo.transform;
                var basePos = HerePos(def.Room);
                rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(9, 9);
                rt.anchoredPosition = basePos + new Vector2(-46 + idx * 20, -34);
                _clueDots.Add(dotGo);
            }

            bool exploring = _gameRoot.State == AppViewState.Exploration;
            _hint.text = exploring
                ? "Click a room to move there. Red dots are findings you have discovered."
                : "From the checkpoint the map is reference only — use PATROL HALLWAY to investigate.";
        }

        private Vector2 HerePos(RoomId room)
        {
            switch (room)
            {
                case RoomId.Hallway: return new Vector2(-60, 90);
                case RoomId.Classroom1: return new Vector2(-110, 210);
                case RoomId.Classroom2: return new Vector2(120, -30);
                default: return new Vector2(-360, 90);
            }
        }

        private void Update()
        {
            if (gameObject.activeInHierarchy &&
                ((Input.GetKeyDown(KeyCode.M) && InputGate.TryM()) ||
                 (Input.GetKeyDown(KeyCode.Escape) && InputGate.TryEscape())))
            {
                if (_gameRoot.State == AppViewState.Map) _gameRoot.CloseMap();
            }
        }

        public void Hide() => gameObject.SetActive(false);
    }

    public class ShiftResultsView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private Text _title, _body, _rating;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            UIFactory.Dimmer(transform, "dim", 0.9f);
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
            _title.text = "SHIFT " + justFinished.Number + " COMPLETE — " + justFinished.DateLabel;
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
                sb.Append("District administrator:        ").Append(session.AdminRejected ? "REJECTED — identified as an impostor\n" : "—\n");
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

    public class EndingView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private Text _title, _desc, _summary;
        private Text[] _badges = new Text[3];
        private Button _again;
        private GameMode _lastMode = GameMode.Normal;

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
                _gameRoot.StartNewGame(_lastMode);
            }, 220, 46);
            UIFactory.Anchor(_again.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(220, 46), new Vector2(120, 38));

            gameObject.SetActive(false);
        }

        public void Populate(EndingRules.Evaluation eval, GameSession session, RecordsData records)
        {
            _lastMode = GameMode.Normal;
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

            var sb = new StringBuilder();
            sb.Append("Nights survived:            ").Append(session.CompletedShifts).Append(" / 5\n");
            sb.Append("Visitors processed:         ").Append(session.Processed).Append('\n');
            sb.Append("Correct decisions:          ").Append(session.CorrectDecisions).Append('\n');
            sb.Append("Anomalies rejected:         ").Append(session.AnomaliesRejected).Append('\n');
            sb.Append("False rejections:           ").Append(session.FalseRejections).Append(" / 3\n");
            sb.Append("Anomaly breaches:           ").Append(session.Breaches).Append(" / 3\n");
            sb.Append("Administrator:              ").Append(session.AdminAllowed ? "ALLOWED IN (fatal mistake)" : session.AdminRejected ? "REJECTED — impostor identified" : "never arrived").Append('\n');
            sb.Append("Findings:                   ").Append(session.Clues.FoundCount).Append(" / 10  (").Append(session.Clues.OrderIntact ? "intended sequence intact" : "sequence broken").Append(")\n");
            sb.Append("Final sanity:               ").Append((int)session.Sanity).Append("  (").Append(session.SanityStage).Append(")\n");
            sb.Append("Score:                      ").Append(session.Score).Append("    Best streak: ").Append(session.BestStreak);
            _summary.text = sb.ToString();

            RefreshBadges(records);
            gameObject.SetActive(true);
        }

        public void PopulateRunResults(GameSession session, RecordsData records)
        {
            _lastMode = session.Mode;
            _title.text = session.Mode == GameMode.Endless ? "THE NIGHT ENDS" : "PROTOCOL TERMINATED";
            _title.color = UIFactory.Ink;
            _desc.text = "The clock ran out. The district's morning crew found the booth empty and the log full.";

            var sb = new StringBuilder();
            sb.Append("Mode:                       ").Append(session.Mode).Append('\n');
            sb.Append("Visitors processed:         ").Append(session.RunProcessed).Append('\n');
            sb.Append("Correct decisions:          ").Append(session.CorrectDecisions).Append('\n');
            sb.Append("Anomalies rejected:         ").Append(session.AnomaliesRejected).Append('\n');
            sb.Append("Mistakes:                   ").Append(session.FalseRejections + session.Breaches).Append('\n');
            sb.Append("Best streak:                ").Append(session.BestStreak).Append('\n');
            sb.Append("FINAL SCORE:                ").Append(session.Score).Append('\n');
            sb.Append(session.Mode == GameMode.Endless
                ? "BEST ENDLESS SCORE:         " + records.EndlessBest
                : "BEST CHALLENGE SCORE:       " + records.ChallengeBest);
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
