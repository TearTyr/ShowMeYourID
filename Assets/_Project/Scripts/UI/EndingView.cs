using System.Text;
using Hollowbrook.Core;
using Hollowbrook.Scenes;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    // The end result screen, built from the security desk kit: the open booth dimmed behind a big
    // condensed title, the ending blurb, a black torn-paper report card (three big numbers and a neat
    // label / value list), three ending tags and two kit pill buttons. Created by DeskScene.
    // Everything sits directly in one fixed 1280x720 frame, placed with PlaceTL (x, y down).
    public class EndingView : MonoBehaviour, IScreenView
    {
        // ---- layout (frame coordinates) ----
        private const float FrameW = 1280f, FrameH = 720f;
        private const float TitleY = 26f, TitleH = 64f;
        private const float DescX = 210f, DescY = 94f, DescW = 860f, DescH = 66f;
        private const float CardX = 250f, CardY = 168f, CardW = 780f, CardH = 346f;
        private const float CardPad = 38f;                       // card edge to content
        private const float CardHeadY = 182f;
        private const float StatY = 208f, StatValueH = 42f, StatLabelY = 252f, StatLabelH = 18f;
        private const float DividerY = 284f;
        private const float RowY0 = 294f, RowH = 25f;
        private const float RowLabelW = 230f;
        private const float BadgeY = 530f, BadgeW = 132f, BadgeH = 34f, BadgeGap = 18f;
        private const float PillY = 592f, PillW = 220f, PillH = 57f, PillGap = 40f;
        private const float PillIconSize = 24f;
        private const float QPanelBorderPx = 40f;                // the torn edge of sec_qpanel, in texture pixels
        private const float QPanelDrawnBorder = 20f;             // ...drawn this many units wide

        private const int MaxStats = 3;
        private const int MaxRows = 8;
        private const float LowSanity = 30f;                      // below this the sanity number turns red

        private const string WinTitle = "SURVIVING HOLLOWBROOK";
        private const string LoseTitle = "ANOTHER CUSTODIAN";
        private const string SecretTitle = "THE TRUTH BEHIND HOLLOWBROOK";

        private static readonly string[] BadgeNames = { "WIN", "LOSE", "SECRET" };
        private static readonly string[] BadgeKeys = { "win", "lose", "secret" };
        private static readonly Color Gold = new Color(0.85f, 0.72f, 0.4f);
        private static readonly Color GoldInk = new Color(0.50f, 0.37f, 0.08f);   // gold that reads on a white pill
        private static readonly Color DividerColor = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color DeadPill = new Color(0.30f, 0.30f, 0.30f, 1f);
        private static readonly Color DeadInk = new Color(0.62f, 0.62f, 0.62f, 1f);

        private struct Stat
        {
            public string Label, Value;
            public Color Tint;
            public Stat(string label, string value, Color tint) { Label = label; Value = value; Tint = tint; }
        }

        private struct Line
        {
            public string Label, Value;
            public Line(string label, string value) { Label = label; Value = value; }
        }

        private GameRoot _gameRoot;
        private RectTransform _frame;
        private Text _title, _desc;
        private readonly Text[] _statValues = new Text[MaxStats];
        private readonly Text[] _statLabels = new Text[MaxStats];
        private readonly Text[] _rowLabels = new Text[MaxRows];
        private readonly Text[] _rowValues = new Text[MaxRows];
        private readonly Image[] _rowDividers = new Image[MaxRows];
        private readonly Image[] _badgePills = new Image[3];
        private readonly Text[] _badgeTexts = new Text[3];
        private readonly Image[] _badgeMarks = new Image[3];
        private Button _again;
        private System.Action _againAction = () => { };

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            BuildBackground();
            _frame = UIFactory.FixedFrame(transform, "frame", FrameW, FrameH);
            BuildTitleAndBlurb();
            BuildCard();
            BuildBadges();
            BuildButtons();
            gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- building

        private void BuildBackground()
        {
            var bg = UIFactory.Panel(transform, "bg", Color.black);
            UIFactory.AnchorsFull(bg.rectTransform);
            var booth = UIFactory.Img(transform, "kitbg", ProceduralArt.Get("sec_background_open"), new Color(1f, 1f, 1f, 0.30f));
            UIFactory.AnchorsFull(booth.rectTransform);
            booth.raycastTarget = false;
        }

        private void BuildTitleAndBlurb()
        {
            _title = UIFactory.Label(_frame, "title", "", 54, UIFactory.NsChalk, TextAnchor.MiddleCenter);
            _title.font = UIFactory.UiFontCondensed;
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.PlaceTL(_title.rectTransform, 0f, TitleY, FrameW, TitleH);

            _desc = UIFactory.Label(_frame, "desc", "", 17, UIFactory.NsPaper, TextAnchor.UpperCenter);
            _desc.font = UIFactory.UiFont;
            UIFactory.PlaceTL(_desc.rectTransform, DescX, DescY, DescW, DescH);
        }

        private void BuildCard()
        {
            var card = UIFactory.Img(_frame, "card", ProceduralArt.Get("sec_qpanel"));
            UIFactory.PlaceTL(card.rectTransform, CardX, CardY, CardW, CardH);
            SliceCard(card);

            var head = UIFactory.Label(_frame, "cardhead", "DECISION SUMMARY", 14, UIFactory.NsMid, TextAnchor.MiddleLeft);
            head.font = UIFactory.UiFontCondensed;
            head.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.PlaceTL(head.rectTransform, CardX + CardPad, CardHeadY, 300f, 22f);

            for (int i = 0; i < MaxStats; i++) BuildStatSlot(i);
            var rule = UIFactory.Panel(_frame, "cardrule", DividerColor);
            rule.raycastTarget = false;
            UIFactory.PlaceTL(rule.rectTransform, CardX + CardPad, DividerY, CardW - 2f * CardPad, 1f);
            for (int i = 0; i < MaxRows; i++) BuildRow(i);
        }

        // The kit panel keeps its torn edge at any size: nine slices, the edge drawn at a fixed width.
        private static void SliceCard(Image card)
        {
            if (card.sprite == null || card.sprite.border.sqrMagnitude <= 0f) return;
            card.type = Image.Type.Sliced;
            if (card.pixelsPerUnit > 0f)
                card.pixelsPerUnitMultiplier = (QPanelBorderPx / QPanelDrawnBorder) / card.pixelsPerUnit;
        }

        private void BuildStatSlot(int i)
        {
            var value = UIFactory.Label(_frame, "statvalue" + i, "", 38, UIFactory.NsChalk, TextAnchor.MiddleCenter);
            value.font = UIFactory.UiFontCondensed;
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            var label = UIFactory.Label(_frame, "statlabel" + i, "", 12, UIFactory.NsMid, TextAnchor.MiddleCenter);
            label.font = UIFactory.UiFont;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _statValues[i] = value;
            _statLabels[i] = label;
        }

        private void BuildRow(int i)
        {
            float y = RowY0 + i * RowH;
            float innerW = CardW - 2f * CardPad;
            var label = UIFactory.Label(_frame, "rowlabel" + i, "", 15, UIFactory.NsMid, TextAnchor.MiddleLeft);
            label.font = UIFactory.UiFont;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.PlaceTL(label.rectTransform, CardX + CardPad, y, RowLabelW, RowH);
            var value = UIFactory.Label(_frame, "rowvalue" + i, "", 16, UIFactory.NsChalk, TextAnchor.MiddleRight);
            value.font = UIFactory.UiFontBold;
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.PlaceTL(value.rectTransform, CardX + CardPad + RowLabelW, y, innerW - RowLabelW, RowH);
            var line = UIFactory.Panel(_frame, "rowline" + i, DividerColor);
            line.raycastTarget = false;
            UIFactory.PlaceTL(line.rectTransform, CardX + CardPad, y + RowH - 1f, innerW, 1f);
            _rowLabels[i] = label;
            _rowValues[i] = value;
            _rowDividers[i] = line;
        }

        private void BuildBadges()
        {
            float total = 3f * BadgeW + 2f * BadgeGap;
            float x0 = (FrameW - total) / 2f;
            for (int i = 0; i < 3; i++)
            {
                float x = x0 + i * (BadgeW + BadgeGap);
                var pill = UIFactory.Img(_frame, "badge" + i, ProceduralArt.Get("sec_wide_pill"));
                UIFactory.PlaceTL(pill.rectTransform, x, BadgeY, BadgeW, BadgeH);
                var text = UIFactory.Label(pill.transform, "label", BadgeNames[i], 15, UIFactory.NsInk, TextAnchor.MiddleCenter);
                text.font = UIFactory.UiFontBold;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                UIFactory.AnchorsFull(text.rectTransform);
                var mark = UIFactory.Img(pill.transform, "mark", null, UIFactory.NsInk);
                UIFactory.Anchor(mark.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, 18f), new Vector2(18f, 0f));
                mark.gameObject.SetActive(false);
                _badgePills[i] = pill;
                _badgeTexts[i] = text;
                _badgeMarks[i] = mark;
            }
        }

        private void BuildButtons()
        {
            float x0 = (FrameW - 2f * PillW - PillGap) / 2f;
            PillButton("menu", "MAIN MENU", "sec_icon_endshift", x0, () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.7f);
                _gameRoot.QuitToMenu(false);
            });
            _again = PillButton("again", "PLAY AGAIN", "sec_icon_patrol", x0 + PillW + PillGap, () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_bell", 0.7f);
                _againAction();
            });
        }

        // A kit pill (the PATROL HALLWAY / END SHIFT look): the torn white pill, a bold ink word, an icon on the left.
        private Button PillButton(string name, string label, string iconKey, float x, System.Action onClick)
        {
            var b = UIFactory.StandardButton(_frame, name, label, onClick, (int)PillW, (int)PillH);
            UIFactory.PlaceTL(b.GetComponent<RectTransform>(), x, PillY, PillW, PillH);
            var frameImg = b.GetComponent<Image>();
            if (frameImg != null) frameImg.color = new Color(1f, 1f, 1f, 0f);
            var bg = b.transform.Find("bg");
            var bgImg = bg != null ? bg.GetComponent<Image>() : null;
            var pill = ProceduralArt.Get("sec_wide_pill");
            if (bgImg != null && pill != null) { bgImg.sprite = pill; bgImg.color = Color.white; }
            var text = b.GetComponentInChildren<Text>(true);
            if (text != null) StylePillLabel(text);
            var icon = ProceduralArt.Get(iconKey);
            if (icon != null)
            {
                var img = UIFactory.Img(b.transform, "kiticon", icon, UIFactory.NsInk);
                UIFactory.Anchor(img.rectTransform, new Vector2(0f, 0.5f), new Vector2(PillIconSize, PillIconSize), new Vector2(26f, 0f));
            }
            return b;
        }

        private static void StylePillLabel(Text text)
        {
            text.color = UIFactory.NsInk;
            text.font = UIFactory.UiFontBold;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var rt = text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(48f, 0f);
            rt.offsetMax = new Vector2(-8f, 0f);
        }

        // ---------------------------------------------------------------- showing an ending

        public void Populate(EndingRules.Evaluation eval, GameSession session, RecordsData records)
        {
            SetTitle(eval.Ending);
            _desc.text = eval.ReasonText;
            if (session.Mode == GameMode.Lockdown) { ShowLockdownEnding(session, eval.Ending == EndingType.Win); return; }

            // night 6 is its own short story: no tags. The button after a loss plays the night that
            // was lost again; after a win it starts a new game (night 6 has no second button).
            bool night6 = session.Mode == GameMode.Challenge;
            bool lost = eval.Ending == EndingType.Lose;
            SetAgainButton(session, lost, night6);
            if (night6)
            {
                ShowBadges(false, null, eval.Ending);
                ShowCard(Night6Stats(session), Night6Lines(session));
            }
            else
            {
                ShowBadges(true, records, eval.Ending);
                ShowCard(NormalStats(session), NormalLines(session));
            }
            gameObject.SetActive(true);
        }

        private void SetTitle(EndingType ending)
        {
            if (ending == EndingType.Secret) { _title.text = SecretTitle; _title.color = Gold; return; }
            if (ending == EndingType.Lose) { _title.text = LoseTitle; _title.color = UIFactory.AccentBright; return; }
            _title.text = WinTitle;
            _title.color = UIFactory.NsChalk;
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
            var text = _again.GetComponentInChildren<Text>(true);
            if (text != null) text.text = label;
            _again.gameObject.SetActive(!night6 || lost);
        }

        // Lockdown is one sitting: no tags, no save, and the one button always plays it again.
        private void ShowLockdownEnding(GameSession session, bool won)
        {
            ShowBadges(false, null, EndingType.None);
            _againAction = () => _gameRoot.StartNewGame(GameMode.Lockdown);
            var label = _again.GetComponentInChildren<Text>(true);
            if (label != null) label.text = "TRY AGAIN";
            _again.gameObject.SetActive(true);

            string strikes = session.LockdownStrikes + " / " + LockdownRules.MaxStrikes;
            string result = won ? LockdownRules.Rank(session.LockdownStrikes) : "THE GATE FELL";
            Color resultTint = won ? UIFactory.NsChalk : UIFactory.AccentBright;
            var stats = new[]
            {
                new Stat("MISTAKES", strikes, UIFactory.NsChalk),
                new Stat("RESULT", result, resultTint),
            };
            var lines = new[]
            {
                new Line("Mode", "Lockdown"),
                new Line("Mistakes", strikes),
                new Line("Result", result),
            };
            ShowCard(stats, lines);
            gameObject.SetActive(true);
        }

        // ---------------------------------------------------------------- what each mode says

        private static string AdminText(GameSession session, bool nightSix)
        {
            if (session.AdminAllowed) return "ALLOWED IN (fatal mistake)";
            if (session.Hunt == HuntState.Lost) return "REJECTED - but he reached the server door";
            if (session.AdminRejected) return nightSix ? "REJECTED - driven off until dawn" : "REJECTED - impostor driven off";
            return nightSix ? "never reached the glass" : "never arrived";
        }

        private static Color SanityTint(GameSession session) =>
            session.Sanity < LowSanity ? UIFactory.AccentBright : UIFactory.NsChalk;

        private static string SanityLabel(GameSession session) => "FINAL SANITY (" + session.SanityStage.ToString().ToUpper() + ")";

        private static Stat[] NormalStats(GameSession session)
        {
            return new[]
            {
                new Stat("NIGHTS SURVIVED", session.CompletedShifts + " / " + ShiftPlan.TotalShifts, UIFactory.NsChalk),
                new Stat("SCORE", session.Score.ToString(), UIFactory.NsChalk),
                new Stat(SanityLabel(session), ((int)session.Sanity).ToString(), SanityTint(session)),
            };
        }

        private static Line[] NormalLines(GameSession session)
        {
            string sequence = session.Clues.OrderIntact ? "intended sequence intact" : "sequence broken";
            return new[]
            {
                new Line("Visitors processed", session.Processed.ToString()),
                new Line("Correct decisions", session.CorrectDecisions.ToString()),
                new Line("Anomalies rejected", session.AnomaliesRejected.ToString()),
                new Line("False rejections", session.FalseRejections + " / " + EndingRules.LoseThreshold),
                new Line("Anomaly breaches", session.Breaches + " / " + EndingRules.LoseThreshold),
                new Line("Administrator", AdminText(session, false)),
                new Line("Findings", session.Clues.FoundCount + " / " + session.Clues.TotalCount + "  (" + sequence + ")"),
                new Line("Best streak", session.BestStreak.ToString()),
            };
        }

        private static Stat[] Night6Stats(GameSession session)
        {
            return new[]
            {
                new Stat("NIGHT", "6", UIFactory.NsChalk),
                new Stat(SanityLabel(session), ((int)session.Sanity).ToString(), SanityTint(session)),
            };
        }

        private static Line[] Night6Lines(GameSession session)
        {
            return new[]
            {
                new Line("Mode", "Night 6 (the Challenge)"),
                new Line("Administrator", AdminText(session, true)),
                new Line("Final sanity", (int)session.Sanity + "  (" + session.SanityStage + ")"),
            };
        }

        // ---------------------------------------------------------------- filling the card

        private void ShowCard(Stat[] stats, Line[] lines)
        {
            ShowStats(stats);
            for (int i = 0; i < MaxRows; i++)
            {
                bool used = i < lines.Length;
                _rowLabels[i].gameObject.SetActive(used);
                _rowValues[i].gameObject.SetActive(used);
                _rowDividers[i].gameObject.SetActive(used && i < lines.Length - 1);
                if (!used) continue;
                _rowLabels[i].text = lines[i].Label;
                _rowValues[i].text = lines[i].Value;
            }
        }

        // The big numbers share the card's width evenly, however many there are.
        private void ShowStats(Stat[] stats)
        {
            float innerW = CardW - 2f * CardPad;
            float slotW = innerW / Mathf.Max(1, stats.Length);
            for (int i = 0; i < MaxStats; i++)
            {
                bool used = i < stats.Length;
                _statValues[i].gameObject.SetActive(used);
                _statLabels[i].gameObject.SetActive(used);
                if (!used) continue;
                float x = CardX + CardPad + i * slotW;
                UIFactory.PlaceTL(_statValues[i].rectTransform, x, StatY, slotW, StatValueH);
                UIFactory.PlaceTL(_statLabels[i].rectTransform, x, StatLabelY, slotW, StatLabelH);
                _statValues[i].text = stats[i].Value;
                _statValues[i].color = stats[i].Tint;
                _statLabels[i].text = stats[i].Label;
            }
        }

        // The three ending tags: lit once seen, the one just reached carries a mark.
        private void ShowBadges(bool show, RecordsData records, EndingType current)
        {
            for (int i = 0; i < 3; i++)
            {
                _badgePills[i].gameObject.SetActive(show);
                if (!show) continue;
                bool isCurrent = i == BadgeIndex(current);
                bool seen = records != null && records.EndingsSeen.Contains(BadgeKeys[i]);
                StyleBadge(i, seen || isCurrent, isCurrent);
            }
        }

        private static int BadgeIndex(EndingType ending)
        {
            if (ending == EndingType.Win) return 0;
            if (ending == EndingType.Lose) return 1;
            if (ending == EndingType.Secret) return 2;
            return -1;
        }

        private void StyleBadge(int i, bool lit, bool isCurrent)
        {
            _badgePills[i].color = lit ? Color.white : DeadPill;
            _badgeTexts[i].color = lit ? BadgeInk(i) : DeadInk;
            _badgeMarks[i].gameObject.SetActive(isCurrent);
            if (!isCurrent) return;
            string icon = i == 1 ? "sec_icon_decline" : "sec_icon_accept";
            _badgeMarks[i].sprite = ProceduralArt.Get(icon);
        }

        private static Color BadgeInk(int i)
        {
            if (i == 1) return UIFactory.Accent;
            if (i == 2) return GoldInk;
            return UIFactory.NsInk;
        }

        /// <summary>Everything the card says, one line each (the flow checks read this).</summary>
        internal string SummaryText
        {
            get
            {
                var sb = new StringBuilder();
                for (int i = 0; i < MaxStats; i++)
                    if (_statValues[i].gameObject.activeSelf)
                        sb.Append(_statLabels[i].text).Append(": ").Append(_statValues[i].text).Append('\n');
                for (int i = 0; i < MaxRows; i++)
                    if (_rowValues[i].gameObject.activeSelf)
                        sb.Append(_rowLabels[i].text).Append(": ").Append(_rowValues[i].text).Append('\n');
                return sb.ToString();
            }
        }

        public void Show() { }
        public void Hide() => gameObject.SetActive(false);
    }
}
