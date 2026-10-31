using System.Collections.Generic;
using System.Text;
using Hollowbrook.Core;
using Hollowbrook.Bootstrap;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    // HOLLOWBROOK COLLEGE - GROUND FLOOR: the hand-drawn plan the custodian keeps at the desk.
    //
    // Layout rule (and the fix for the old off-centre sheet): the plan is authored ONCE in
    // FloorPlan's fixed design space, and this view fits that space to whatever the screen can
    // actually show (UIFactory.VisibleReferenceSize - AspectMatchGuard widens the visible
    // reference on wide windows and heightens it on tall ones). One uniform scale, one centred
    // stack (title / plan / controls), so the sheet is centred horizontally AND vertically at
    // any resolution and can never clip - there are no hand-tuned pixel offsets left to drift.
    //
    // Rooms come from the same FloorPlan table the proofs read: the security checkpoint on the
    // left, the long central hallway, Classroom 1 above it, Classroom 2 below it, the sealed
    // server / identification room on the right, and the two comfort rooms off the corridor's
    // east end. The plan is a REFERENCE only: no room takes a click, so the map can never move
    // the player - you get into a room by walking the hallway and peeking in from its door.
    public class MapView : MonoBehaviour, IScreenView
    {
        // The sheet's composition, sizes and type scales all live in MapSheetLayout (pure,
        // engine-free, unit-tested); this view only applies them. Design-space type sizes:
        private const int TitleSize = 28, SubTitleSize = 13, LegendSize = 12, HintSize = 13;
        private const int YouLabelSize = 13;

        // design-space sizes of the drawn furniture (also in MapSheetLayout, so the proofs and
        // the renderer can never disagree about them)
        private static readonly Vector2 DoorOnLongWall =
            new Vector2(MapSheetLayout.DoorLongWallW, MapSheetLayout.DoorLongWallH);
        private static readonly Vector2 DoorOnEndWall =
            new Vector2(MapSheetLayout.DoorEndWallW, MapSheetLayout.DoorEndWallH);
        private static readonly Vector2 MarkerSize =
            new Vector2(MapSheetLayout.MarkerSize, MapSheetLayout.MarkerSize);
        private static readonly Vector2 CloseSize =
            new Vector2(MapSheetLayout.CloseRowW, MapSheetLayout.CloseRowH);

        private GameRoot _gameRoot;
        private RectTransform _paper;        // the distressed plan sheet (the centred stack)
        private RectTransform _plan;         // the scaled design-space container
        private RectTransform _hereMarker;
        private Text _hereLabel;
        private RectTransform _rule;
        private Text _title, _subTitle, _hint, _legend;
        private RectTransform _closeRt;
        private readonly List<Transform> _clueDots = new List<Transform>();

        // one entry per drawn room, in FloorPlan order
        private sealed class RoomEntry
        {
            public FloorRoom Def;
            public RectTransform Rt;
            public Text Label;
        }
        private readonly List<RoomEntry> _rooms = new List<RoomEntry>();

        private sealed class DoorEntry
        {
            public float X, Y;
            public bool Horizontal;
            public RectTransform Rt;
        }
        private readonly List<DoorEntry> _doors = new List<DoorEntry>();

        private sealed class RunEntry
        {
            public float X0, Y0, X1, Y1;
            public RectTransform Rt;
        }
        private readonly List<RunEntry> _runs = new List<RunEntry>();

        private int _w = -1, _h = -1;
        private float _paperScale = 1f, _planScale = 1f, _planTextScale = 1f, _chromeTextScale = 1f;
        private MapSheetLayout _layout;
        private RoomId _hereRoom = RoomId.Security;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            UIFactory.Dimmer(transform, "dim", 0.88f);

            _paper = UIFactory.NewUI(transform, "mapsheet");
            _paper.localEulerAngles = new Vector3(0f, 0f, MapSheetLayout.PaperTiltDeg);
            var paperImg = _paper.gameObject.AddComponent<Image>();
            paperImg.sprite = ProceduralArt.Get("map_paper");
            paperImg.raycastTarget = false;      // the sheet is scenery: only rooms are clickable

            _title = HandLabel(_paper, "maptitle", FloorPlan.Title, TitleSize, UIFactory.NsChalk);
            var ruleImg = UIFactory.Img(_paper, "titlerule", null, new Color(0.72f, 0.68f, 0.60f, 0.30f));
            ruleImg.raycastTarget = false;
            _rule = ruleImg.rectTransform;
            _subTitle = HandLabel(_paper, "mapsubtitle",
                "NIGHT PATROL REFERENCE  ·  DISTRICT FILE 3-B", SubTitleSize,
                new Color(0.78f, 0.74f, 0.66f, 0.62f));

            _plan = UIFactory.NewUI(_paper, "plan");

            BuildCorridors();     // under the rooms: the chalk runs between room and corridor
            BuildRooms();
            BuildDoors();

            // YOU ARE HERE: the position marker the patrol reads, drawn on top of the rooms
            _hereMarker = UIFactory.NewUI(_plan, "here");
            var markerInk = _hereMarker.gameObject.AddComponent<Image>();
            markerInk.sprite = ProceduralArt.Get("rough_circle");
            markerInk.color = UIFactory.AccentBright;
            markerInk.raycastTarget = false;
            _hereLabel = UIFactory.Label(_hereMarker, "lbl", "YOU", YouLabelSize, UIFactory.AccentBright,
                TextAnchor.MiddleCenter);
            _hereLabel.font = UIFactory.UiFontBold;
            _hereLabel.raycastTarget = false;

            _legend = HandLabel(_paper, "maplegend",
                "RED HATCH = SEALED, NO ENTRY        RED DOT = A FINDING YOU HAVE LOGGED", LegendSize,
                new Color(0.78f, 0.74f, 0.66f, 0.55f));
            _hint = HandLabel(_paper, "hint", "", HintSize, new Color(0.88f, 0.86f, 0.80f, 0.9f));

            // CLOSE lives under the sheet, centred with it: always reachable, never over the
            // plan, and it follows the sheet's own fit so it cannot drift off-screen
            _closeRt = UIFactory.NewUI(transform, "close");
            // raycast ON: this child Image is what the pointer actually hits, and the click then
            // walks up to the Button on the row - with it off the CLOSE row would take no clicks
            var closeInk = UIFactory.Img(_closeRt, "bg", null, new Color(0.055f, 0.055f, 0.06f, 0.94f), true);
            UIFactory.AnchorsFull(closeInk.rectTransform);
            var closeLbl = UIFactory.Label(_closeRt, "label", "CLOSE  [M]", 14, UIFactory.Ink,
                TextAnchor.MiddleCenter);
            closeLbl.font = UIFactory.UiFontBold;
            closeLbl.raycastTarget = false;
            UIFactory.AnchorsFull(closeLbl.rectTransform);
            var close = _closeRt.gameObject.AddComponent<Button>();
            close.targetGraphic = closeInk;
            close.transition = Selectable.Transition.ColorTint;
            var cb = close.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.35f, 1.33f, 1.28f);
            cb.pressedColor = new Color(1.6f, 1.58f, 1.5f);
            cb.fadeDuration = 0.10f;
            close.colors = cb;
            close.onClick.AddListener(() =>
            {
                _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.5f);
                _gameRoot.CloseMap();
            });
            _closeRt.gameObject.AddComponent<HoverCursor>();

            Relayout();
            gameObject.SetActive(false);
        }

        // One node per FloorPlan room. The label is a child of the room node, so the ink and its
        // label can never drift apart. Rooms are drawings: no Button, no raycast, no hover.
        private void BuildRooms()
        {
            foreach (var def in FloorPlan.Rooms)
            {
                var rt = UIFactory.NewUI(_plan, def.Key);
                var ink = rt.gameObject.AddComponent<Image>();
                ink.sprite = ProceduralArt.Get(def.Sealed ? "map_sealed" : "map_room");
                ink.raycastTarget = false;            // reference only: nothing to click

                int size = RoomLabelSize(def);
                var label = UIFactory.Label(rt, "label", def.Label, size,
                    def.Sealed ? UIFactory.AccentBright : UIFactory.NsChalk, TextAnchor.MiddleCenter);
                label.font = def.Comfort || def.Label == "MAIN HALLWAY" ? UIFactory.UiFontBold : UIFactory.HandFont;
                label.raycastTarget = false;
                label.lineSpacing = 0.92f;
                UIFactory.AnchorsFull(label.rectTransform);
                UIFactory.RememberBaseSize(label, size);

                _rooms.Add(new RoomEntry { Def = def, Rt = rt, Label = label });
            }
        }

        private static int RoomLabelSize(FloorRoom def)
        {
            if (def.Label == "MAIN HALLWAY") return 20;
            if (def.Sealed) return 13;
            return def.Comfort ? 13 : 15;
        }

        // Doorway gaps, drawn from the same table as the rooms and oriented by the wall they sit
        // on: a door in the corridor's long wall is a horizontal leaf, one in its end wall is
        // vertical. The sealed room's is tinted red and turned - a barred plate, not a way in.
        private void BuildDoors()
        {
            int i = 0;
            foreach (var def in FloorPlan.Rooms)
            {
                if (!def.DoorX.HasValue || !def.DoorY.HasValue) continue;
                float dy = def.DoorY.Value;
                var rt = UIFactory.NewUI(_plan, "door" + i++ + "_" + def.Key);
                var ink = rt.gameObject.AddComponent<Image>();
                ink.sprite = ProceduralArt.Get("map_door");
                ink.color = def.Sealed ? new Color(0.78f, 0.26f, 0.27f) : new Color(0.85f, 0.82f, 0.74f);
                ink.raycastTarget = false;
                // the sealed plate keeps its wall orientation (vertical in its west wall);
                // the red tint and the chain art are what say "no way in"
                _doors.Add(new DoorEntry
                {
                    X = def.DoorX.Value,
                    Y = dy,
                    // a door in the room's own TOP or BOTTOM wall is a leaf in a long wall;
                    // one in a side wall stands vertical. (The old hallway-band test called
                    // every classroom door vertical although they sit in their south walls.)
                    Horizontal = Mathf.Abs(dy - def.Top) < 0.01f ||
                                 Mathf.Abs(dy - def.Bottom) < 0.01f,
                    Rt = rt,
                });
            }
        }

        // The chalk runs that connect a room to the corridor (FloorPlan.Corridors), plus
        // the reference's stair hatch under the east-centre long wall and the custodian's
        // "2ND FLOOR??" scratch beside it - decoration, exactly like the reference's.
        private Text _stairsNote;
        private void BuildCorridors()
        {
            int i = 0;
            foreach (var run in FloorPlan.Corridors) AddRun(run, ref i);
            foreach (var run in FloorPlan.StairsRuns) AddRun(run, ref i);
            _stairsNote = HandLabel(_plan, "stairsnote", FloorPlan.StairsNote, 13,
                new Color(0.78f, 0.74f, 0.66f, 0.75f));
        }

        private void AddRun(float[] run, ref int i)
        {
            if (run == null || run.Length < 4) return;
            var rt = UIFactory.NewUI(_plan, "corridor" + i++);
            var ink = rt.gameObject.AddComponent<Image>();
            ink.sprite = ProceduralArt.Get("solid_white");
            ink.color = new Color(0.76f, 0.72f, 0.64f, 0.55f);
            ink.raycastTarget = false;
            _runs.Add(new RunEntry { X0 = run[0], Y0 = run[1], X1 = run[2], Y1 = run[3], Rt = rt });
        }

        private static Text HandLabel(RectTransform parent, string name, string text, int size, Color color)
        {
            var t = UIFactory.Label(parent, name, text, size, color, TextAnchor.MiddleCenter);
            t.font = UIFactory.HandFont;      // the handwritten plan face (bundled, open licence)
            t.raycastTarget = false;
            t.lineSpacing = 0.95f;
            return t;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            Relayout();
            Refresh();
        }

        public void Hide() => gameObject.SetActive(false);

        private void Update()
        {
            if (!gameObject.activeInHierarchy) return;
            if (Screen.width != _w || Screen.height != _h) Relayout();   // responsive: any resize
            if ((Input.GetKeyDown(KeyCode.M) && InputGate.TryM()) ||
                (Input.GetKeyDown(KeyCode.Escape) && InputGate.TryEscape()))
            {
                if (_gameRoot.State == AppViewState.Map) _gameRoot.CloseMap();
            }
        }

        // ---------------------------------------------------------------- layout
        // Fits the sheet to the visible reference space and re-places every drawn element from
        // the design-space table. Called on Show and whenever the window size changes, so the
        // plan stays centred and unclipped at every resolution. internal so the headless proofs
        // can drive the same re-fit against a synthetic screen size.
        internal void Relayout()
        {
            _w = Screen.width; _h = Screen.height;
            var visible = UIFactory.VisibleReferenceSize((RectTransform)transform);
            var fit = MapSheetLayout.Fit(visible.x, visible.y);
            _layout = fit;

            float paperW = fit.SheetW, paperH = fit.SheetH;
            _paperScale = fit.PaperScale;
            _planScale = fit.PlanScale;
            _planTextScale = fit.PlanTextScale;
            _chromeTextScale = fit.ChromeTextScale;

            // the sheet is dead-centre of the visible space; CLOSE hangs under it
            UIFactory.Anchor(_paper, new Vector2(0.5f, 0.5f), new Vector2(paperW, paperH), Vector2.zero);
            UIFactory.Anchor(_closeRt, new Vector2(0.5f, 0f), CloseSize,
                new Vector2(0f, fit.CloseFromBottom));

            // the plan fills the sheet between the title band and the control band
            float contentW = fit.ContentW;
            UIFactory.Anchor(_plan, new Vector2(0.5f, 0.5f),
                new Vector2(FloorPlan.DesignW * _planScale, FloorPlan.DesignH * _planScale),
                new Vector2(0f, fit.PlanCentreY));

            // title band, top to bottom: hand-written title, a ruled line, then the file stamp
            UIFactory.Anchor(_title.rectTransform, new Vector2(0.5f, 1f),
                new Vector2(contentW, 40f), new Vector2(0f, -(MapSheetLayout.Margin + 6f)));
            UIFactory.Anchor(_rule, new Vector2(0.5f, 1f),
                new Vector2(contentW * 0.60f, 2f), new Vector2(0f, -(MapSheetLayout.Margin + 56f)));
            UIFactory.Anchor(_subTitle.rectTransform, new Vector2(0.5f, 1f),
                new Vector2(contentW, 18f), new Vector2(0f, -(MapSheetLayout.Margin + 60f)));
            UIFactory.Anchor(_legend.rectTransform, new Vector2(0.5f, 0f),
                new Vector2(contentW, 18f), new Vector2(0f, MapSheetLayout.Margin + 28f));
            UIFactory.Anchor(_hint.rectTransform, new Vector2(0.5f, 0f),
                new Vector2(contentW, 20f), new Vector2(0f, MapSheetLayout.Margin + 6f));

            foreach (var e in _rooms)
            {
                UIFactory.Anchor(e.Rt, new Vector2(0.5f, 0.5f),
                    new Vector2(e.Def.W * _planScale, e.Def.H * _planScale),
                    Design(e.Def.X, e.Def.Y));
                e.Label.fontSize = ScaledSize(RoomLabelSize(e.Def), _planTextScale);
            }
            foreach (var d in _doors)
            {
                var size = (d.Horizontal ? DoorOnLongWall : DoorOnEndWall) * _planScale;
                UIFactory.Anchor(d.Rt, new Vector2(0.5f, 0.5f), size, Design(d.X, d.Y));
            }
            foreach (var r in _runs) PlaceRun(r);
            if (_stairsNote != null)
            {
                UIFactory.Anchor(_stairsNote.rectTransform, new Vector2(0.5f, 0.5f),
                    new Vector2(150f, 26f) * _planScale,
                    Design(FloorPlan.StairsNoteX, FloorPlan.StairsNoteY));
                _stairsNote.fontSize = ScaledSize(13, _planTextScale);
            }

            UIFactory.Anchor(_hereLabel.rectTransform, new Vector2(0.5f, 0f),
                new Vector2(70f, 18f) * _planScale, new Vector2(0f, -MarkerSize.y * _planScale * 0.5f - 3f));
            _hereLabel.fontSize = ScaledSize(YouLabelSize, _planTextScale);

            _title.fontSize = ScaledSize(TitleSize, _chromeTextScale);
            _subTitle.fontSize = ScaledSize(SubTitleSize, _chromeTextScale);
            _legend.fontSize = ScaledSize(LegendSize, _chromeTextScale);
            _hint.fontSize = ScaledSize(HintSize, _chromeTextScale);

            PlaceHereMarker();
            RebuildClueDots();
        }

        /// <summary>Design-space point -> the plan container's anchored space, origin at the
        /// container's centre. FloorPlan's y axis points DOWN (like a drawing) while a
        /// RectTransform's points UP, so the y is negated HERE and nowhere else - every
        /// coordinate on the sheet goes through this one function, which is why the plan can
        /// never come out mirrored and why one scale factor keeps the whole sheet coherent.</summary>
        private Vector2 Design(float x, float y) => new Vector2(
            MapSheetLayout.ToAnchoredX(x, _planScale), MapSheetLayout.ToAnchoredY(y, _planScale));

        /// <summary>Design size -> on-screen size: the sheet fit (clamped, so a small window
        /// shrinks the plan but never makes the labels unreadable) times the player's text-scale
        /// setting. The design size stays the recorded base, so ApplyTextScale and a relayout
        /// multiply independently instead of compounding.</summary>
        private static int ScaledSize(int designSize, float fitScale) =>
            Mathf.Max(9, Mathf.RoundToInt(designSize * fitScale * UIFactory.TextScale));

        private void PlaceRun(RunEntry r)
        {
            var a = Design(r.X0, r.Y0);
            var b = Design(r.X1, r.Y1);
            float dx = b.x - a.x, dy = b.y - a.y;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            float thick = Mathf.Max(2f, 3f * _planScale);
            UIFactory.Anchor(r.Rt, new Vector2(0.5f, 0.5f), new Vector2(Mathf.Max(len, thick), thick),
                new Vector2((a.x + b.x) / 2f, (a.y + b.y) / 2f));
            r.Rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
        }

        private void PlaceHereMarker()
        {
            if (_hereMarker == null) return;
            float x, y;
            if (!FloorPlan.TryMarkerPos(_hereRoom, out x, out y)) { x = 0f; y = 0f; }
            UIFactory.Anchor(_hereMarker, new Vector2(0.5f, 0.5f), MarkerSize * _planScale, Design(x, y));
        }

        // ---------------------------------------------------------------- state
        private void Refresh()
        {
            _hereRoom = RoomId.Security;   // the map only opens at the desk, in the security office
            PlaceHereMarker();
            RebuildClueDots();

            _hint.text = "From the checkpoint the map is reference only - use PATROL HALLWAY to investigate.";
            _hint.fontSize = ScaledSize(HintSize, _chromeTextScale);
        }

        // Findings keep their red-dot language: one dot per discovered clue, fanned out under
        // its room so a room with several findings reads as a cluster, not a blob.
        private void RebuildClueDots()
        {
            foreach (var d in _clueDots)
                if (d != null) Destroy(d.gameObject);
            _clueDots.Clear();
            if (_gameRoot == null || _gameRoot.Session == null) return;   // built before a run

            var counts = new Dictionary<RoomId, int>();
            foreach (var key in _gameRoot.Session.Clues.FoundKeys)
            {
                var def = _gameRoot.Session.Clues.Get(key);
                if (def == null) continue;
                int idx = counts.TryGetValue(def.Room, out var c) ? c : 0;
                counts[def.Room] = idx + 1;
                float x, y;
                if (!FloorPlan.TryMarkerPos(def.Room, out x, out y)) continue;
                var dotRt = UIFactory.NewUI(_plan, "dot_" + key);
                var img = dotRt.gameObject.AddComponent<Image>();
                img.sprite = ProceduralArt.Get("solid_white");
                img.color = new Color(0.78f, 0.18f, 0.2f, 0.95f);
                img.raycastTarget = false;
                float dx = -30f + idx * 15f;
                float dy = 24f + (idx % 2) * 8f;
                UIFactory.Anchor(dotRt, new Vector2(0.5f, 0.5f), new Vector2(8f, 8f) * _planScale,
                    Design(x + dx, y + dy));
                _clueDots.Add(dotRt);
            }
        }

        // ---- headless / diagnostic surface -----------------------------------
        internal RectTransform Sheet => _paper;
        internal RectTransform PlanRoot => _plan;
        internal RectTransform CloseRow => _closeRt;
        internal float PlanScale => _planScale;
        internal float PaperScale => _paperScale;
        internal MapSheetLayout Fit => _layout;
        internal float PlanTextScale => _planTextScale;
        internal float ChromeTextScale => _chromeTextScale;
        internal Text Hint => _hint;
        internal Text Title => _title;
        internal Transform RoomNode(string key)
        {
            foreach (var e in _rooms) if (e.Def.Key == key) return e.Rt;
            return null;
        }
    }

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
            bool failed = session.LastShiftFailure != ShiftFailure.None;
            _title.text = "SHIFT " + justFinished.Number + (failed ? " FAILED - " : " COMPLETE - ") + justFinished.DateLabel;
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
            if (failed)
            {
                sb.Append(session.LastShiftFailure == ShiftFailure.Blackout
                    ? "\nYou blacked out before dawn.\n"
                    : "\nSomething you let in was waiting at the end of the shift.\n");
                int sev = session.PendingPenaltySeverity;
                sb.Append("Tomorrow starts at ").Append((int)(SanityModel.Max - ShiftFailureRules.StartSanityLoss(sev, justFinished.Number)))
                  .Append(" sanity, with the drain already up and something already walking the halls.\n");
            }
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
            sb.Append("Administrator:              ").Append(session.AdminAllowed ? "ALLOWED IN (fatal mistake)" : session.Hunt == HuntState.Lost ? "REJECTED - but he reached the server door" : session.AdminRejected ? "REJECTED - impostor driven off" : "never arrived").Append('\n');
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
