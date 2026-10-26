using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Core;
using Hollowbrook.UI;
using Hollowbrook.Bootstrap;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.World
{
    // The door-facing late-students office: the room is DARK by design. Late students
    // do not walk in on their own — you sweep the cursor lamp over the black until the clear
    // circle finds them (FNaF-3 style), and only then do they step forward for the interview
    // (ID panel + ALLOW / REJECT). The same circle searches the three room spots; one of them
    // hides the anomaly. Created by SchoolSceneBootstrap.
    public class SecurityRoomView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private Image _portrait;
        private Image _idCard;
        private Text _doorState, _idFields, _feedback, _progress;
        private Button _allowBtn, _rejectBtn, _returnBtn;
        private GameObject _docPanel;

        private enum Phase { Idle, Search, Student, Done }
        private Phase _phase = Phase.Idle;
        private readonly List<VisitorData> _queue = new List<VisitorData>();
        private VisitorData _current;
        public VisitorData CurrentStudent => _current;
        private int _correct;
        private bool[] _checked = new bool[3];
        private int _anomalySpot;
        private GameRng _rng;
        private Text _ruleLabel;

        // search overlay: the cursor lamp cuts a clear circle in the dark
        private RectTransform _searchOverlay;
        private RectTransform _lampMask;      // Mask: children render only inside the glow
        private RectTransform _glow;          // soft visible ring around the circle
        private Image _silhouette;            // the hidden late student
        private readonly RectTransform[] _zoneProps = new RectTransform[3];
        private readonly RectTransform[] _zones = new RectTransform[3];
        private readonly float[] _zoneProgress = new float[3];
        private readonly Vector2[] _zoneCenters =
        {
            new Vector2(-430, 40), new Vector2(430, 60), new Vector2(0, -150),
        };
        private readonly Vector2[] _studentSpots =
        {
            new Vector2(-430, 40), new Vector2(430, 60), new Vector2(0, -150), new Vector2(0, 120),
        };
        private Vector2 _studentSpot;
        private float _studentProgress;
        private const float LampRadius = 150f;   // circle radius in canvas pixels

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            var bg = UIFactory.Img(transform, "bg", ProceduralArt.Get("security_bg"));
            UIFactory.AnchorsFull(bg.rectTransform);
            bg.raycastTarget = true;
            bg.color = Color.black;   // the office is dark before the lamp exists

            var title = UIFactory.Label(transform, "title", "SECURITY OFFICE — LATE ARRIVALS", 24,
                UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(800, 36), new Vector2(0, -40));

            _doorState = UIFactory.Label(transform, "doorstate", "The door is shut. The night class is in session.",
                16, UIFactory.InkDim, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_doorState.rectTransform, new Vector2(0.5f, 1f), new Vector2(800, 26), new Vector2(0, -84));

            _portrait = UIFactory.Img(transform, "face", ProceduralArt.Get("face_placeholder"));
            UIFactory.Anchor(_portrait.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(150, 186), new Vector2(0, 150));
            _portrait.gameObject.SetActive(false);

            _docPanel = new GameObject("docpanel", typeof(RectTransform));
            _docPanel.transform.SetParent(transform, false);
            var docRt = (RectTransform)_docPanel.transform;
            UIFactory.Anchor(docRt, new Vector2(1f, 0.5f), new Vector2(360, 250), new Vector2(-240, -40));
            _idCard = UIFactory.Img(docRt, "card", ProceduralArt.Get("id_card_frame"));
            UIFactory.AnchorsFull(_idCard.rectTransform);
            _idFields = UIFactory.Label(docRt, "fields", "", 15, UIFactory.PaperInk);
            var frt = _idFields.rectTransform;
            frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(1, 1);
            frt.offsetMin = new Vector2(24, 20); frt.offsetMax = new Vector2(-24, -46);
            _ruleLabel = UIFactory.Label(docRt, "rule", PatrolRules.OfficeRule.RuleText,
                13, new Color(0.45f, 0.12f, 0.14f), TextAnchor.MiddleCenter);
            UIFactory.Anchor(_ruleLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(340, 40), new Vector2(0, -18));
            _docPanel.SetActive(false);

            _allowBtn = UIFactory.StandardButton(transform, "allow", "✓ ALLOW", () => Decide(Verdict.Allow), 150, 52);
            UIFactory.Anchor(_allowBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(150, 52), new Vector2(-330, 60));
            _rejectBtn = UIFactory.StandardButton(transform, "reject", "✗ REJECT", () => Decide(Verdict.Reject), 150, 52, true);
            UIFactory.Anchor(_rejectBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(150, 52), new Vector2(-160, 60));

            _feedback = UIFactory.Label(transform, "feedback", "", 16, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_feedback.rectTransform, new Vector2(0.5f, 0f), new Vector2(900, 60), new Vector2(0, 120));
            _progress = UIFactory.Label(transform, "progress", "", 14, UIFactory.InkDim, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_progress.rectTransform, new Vector2(0.5f, 0f), new Vector2(900, 24), new Vector2(0, 96));

            _returnBtn = UIFactory.StandardButton(transform, "ret", "RETURN TO THE CHECKPOINT ▸", () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_door", 0.7f);
                _gameRoot.ReturnToCheckpoint();
            }, 320, 48);
            UIFactory.Anchor(_returnBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(320, 48), new Vector2(0, 36));

            BuildSearchOverlay();
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            StartSequence();
        }

        public void Hide() => gameObject.SetActive(false);

        // ---- the dark room and the cursor lamp ------------------------------------------

        // Everything hidden lives UNDER the lamp mask: the mask's glow sprite stencils its
        // children, so they render only inside the circle — a clear hole in the dark, not a
        // milky fade. The mask graphic itself is not drawn; a separate faint glow shows the
        // circle's edge.
        private void BuildSearchOverlay()
        {
            _searchOverlay = UIFactory.NewUI(transform, "search");
            UIFactory.AnchorsFull(_searchOverlay);

            var maskGo = UIFactory.NewUI(_searchOverlay, "lampmask");
            _lampMask = maskGo;
            var maskImg = UIFactory.Img(maskGo, "shape", ProceduralArt.Get("lamp_glow"));
            UIFactory.Anchor(_lampMask, new Vector2(0.5f, 0.5f), new Vector2(300, 300), new Vector2(0, -400));
            maskImg.raycastTarget = false;
            var mask = maskGo.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // hidden furniture: three room spots, revealed only inside the circle
            Vector2[] sizes = { new Vector2(300, 260), new Vector2(280, 240), new Vector2(420, 160) };
            for (int i = 0; i < 3; i++)
            {
                _zones[i] = UIFactory.NewUI(_searchOverlay, "zone" + i);   // logical rect (search bounds)
                UIFactory.Anchor(_zones[i], new Vector2(0.5f, 0.5f), sizes[i], _zoneCenters[i]);
                _zoneProps[i] = UIFactory.NewUI(maskGo, "prop" + i);        // drawn, but stencilled
                var prop = UIFactory.Img(_zoneProps[i], "art", ProceduralArt.Get("solid_white"),
                    new Color(0.30f, 0.30f, 0.32f, 1f));
                UIFactory.Anchor(_zoneProps[i], new Vector2(0.5f, 0.5f), sizes[i] * 0.8f, Vector2.zero);
                prop.raycastTarget = false;
                _zoneProgress[i] = 0f;
            }
            _silhouette = UIFactory.Img(maskGo, "silhouette", ProceduralArt.Get("custodian_0"),
                new Color(0.72f, 0.74f, 0.78f, 1f));
            UIFactory.Anchor(_silhouette.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(120, 220), Vector2.zero);
            _silhouette.raycastTarget = false;

            var glowImg = UIFactory.Img(_searchOverlay, "lamp", ProceduralArt.Get("lamp_glow"),
                new Color(1f, 0.96f, 0.85f, 0.22f));
            _glow = glowImg.rectTransform;
            UIFactory.Anchor(_glow, new Vector2(0.5f, 0.5f), new Vector2(300, 300), new Vector2(0, -400));
            glowImg.raycastTarget = false;
        }

        // Children of the moving mask keep their overlay-space positions by counter-moving.
        private void PlaceMaskedChildren()
        {
            Vector2 lamp = _lampMask.anchoredPosition;
            for (int i = 0; i < 3; i++)
                if (_zoneProps[i] != null) _zoneProps[i].localPosition = new Vector3(_zoneCenters[i].x - lamp.x, _zoneCenters[i].y - lamp.y, 0f);
            if (_silhouette != null)
                _silhouette.rectTransform.localPosition = new Vector3(_studentSpot.x - lamp.x, _studentSpot.y - lamp.y, 0f);
        }

        // ---- flow ------------------------------------------------------------------------

        private void StartSequence()
        {
            _phase = Phase.Search;
            _correct = 0;
            _queue.Clear();
            _rng = new GameRng(_gameRoot.Session.Seed + 977 + _gameRoot.Session.Processed * 131 + _gameRoot.Session.CurrentShift * 17);
            _queue.AddRange(PatrolRules.BuildLateQueue(_gameRoot.Roster, _gameRoot.CurrentShiftDef, _gameRoot.Session.Pool, _rng));
            _anomalySpot = _rng.NextInt(0, 3);
            for (int i = 0; i < 3; i++) _checked[i] = false;
            UIFactory.SetButtonEnabled(_returnBtn, false);
            UIFactory.SetButtonEnabled(_allowBtn, false);
            UIFactory.SetButtonEnabled(_rejectBtn, false);
            _feedback.text = "";
            _searchOverlay.gameObject.SetActive(true);
            _doorState.text = "Dark office. Sweep the light — find whoever is waiting, then hear them out.";
            NextStudent();
        }

        // Pick a hiding spot and arm the search; the interview UI stays hidden until found.
        private void NextStudent()
        {
            _portrait.gameObject.SetActive(false);
            _docPanel.SetActive(false);
            UIFactory.SetButtonEnabled(_allowBtn, false);
            UIFactory.SetButtonEnabled(_rejectBtn, false);
            _studentProgress = 0f;
            if (_queue.Count == 0)
            {
                _current = null;
                _silhouette.gameObject.SetActive(false);
                TryFinish();
                return;
            }
            _current = _queue[0];
            _queue.RemoveAt(0);
            _studentSpot = _studentSpots[_rng.NextInt(0, _studentSpots.Length)];
            _silhouette.gameObject.SetActive(true);
            _gameRoot.Audio.PlaySfx("sfx_door", 0.4f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*something steps into the dark office*", 2.4f, null));
            _doorState.text = "Someone is in the room. The light has to find them first.";
            _progress.text = ProgressLine(_queue.Count + 1);
        }

        // The circle rested on the silhouette: they step forward for the interview.
        private void StudentFound()
        {
            _phase = Phase.Student;
            _gameRoot.Audio.PlaySfx("sfx_knock", 0.8f);
            _doorState.text = "Found. They hold out their ID — hear them out, then decide.";
            _portrait.sprite = ProceduralArt.Portrait("sec", _current.WindowPortrait, 150, 186);
            _portrait.gameObject.SetActive(true);
            _docPanel.SetActive(true);

            bool expired = IdRules.IsExpired(_current, Mathf.Max(1, _gameRoot.Session.CurrentShift));
            var sb = new System.Text.StringBuilder();
            sb.Append("NAME:  ").Append(_current.ClaimedName).Append('\n');
            sb.Append("ID NO: ").Append(_current.IdNumber).Append('\n');
            sb.Append("YEAR:  ").Append(Content.YearNames[_current.IdYearLevel - 1]).Append('\n');
            sb.Append("EXPIRES: ").Append(_current.ExpirationDisplay).Append(expired ? "   (EXPIRED)" : "").Append('\n');
            sb.Append("ON TONIGHT'S LATE LIST: ").Append(_current.OnLateList ? "YES" : "NO — not on tonight's list");
            _idFields.text = sb.ToString();

            UIFactory.SetButtonEnabled(_allowBtn, true);
            UIFactory.SetButtonEnabled(_rejectBtn, true);
        }

        // Headless harness hook: jump straight to the interview the way a found student would.
        internal void DebugFindStudent() { if (_phase == Phase.Search && _current != null) StudentFound(); }

        private void Decide(Verdict v)
        {
            if (_phase != Phase.Student || _current == null) return;
            var outcome = InspectionRules.Apply(_current, v, PatrolRules.OfficeRule);
            _gameRoot.Session.RecordVerdict(outcome);
            if (outcome.Correct) _correct++;
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.VerdictStamped, v));
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.VisitorResolved, outcome));
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, _gameRoot.Session.Sanity));
            _gameRoot.Audio.PlaySfx(v == Verdict.Allow ? "sfx_stamp_allow" : "sfx_stamp_reject", 0.9f);

            // silent verdict, same rule as the desk: the office never says if you were right
            _feedback.text = "";
            _progress.text = ProgressLine(_queue.Count);
            _doorState.text = "The door closes. Footsteps fade down the hall.";
            _current = null;
            _phase = Phase.Search;
            StartCoroutine(NextAfter(1.2f));
        }

        private string ProgressLine(int remaining)
        {
            return "LATE ARRIVALS REMAINING: " + remaining +
                   "   ·   BREACHES: " + _gameRoot.Session.Breaches +
                   "   ·   COMPLAINTS: " + _gameRoot.Session.FalseRejections;
        }

        private IEnumerator NextAfter(float s)
        {
            yield return new WaitForSecondsRealtime(s);
            if (gameObject.activeInHierarchy) NextStudent();
        }

        private void TryFinish()
        {
            bool all = _checked[0] && _checked[1] && _checked[2];
            if (!all) return;
            _phase = Phase.Done;
            _searchOverlay.gameObject.SetActive(false);
            _doorState.text = "Room checked. The office holds its breath.";
            UIFactory.SetButtonEnabled(_returnBtn, true);
        }

        private void Update()
        {
            if (_phase != Phase.Search || _searchOverlay == null || !gameObject.activeInHierarchy) return;
            if (_gameRoot == null || _gameRoot.Paused) return;
            Vector2 local;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)canvas.transform, Input.mousePosition,
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    out local))
                return;
            _lampMask.anchoredPosition = local;
            _glow.anchoredPosition = local;
            PlaceMaskedChildren();

            // the three room spots: resting the circle on a spot searches it
            for (int i = 0; i < 3; i++)
            {
                if (_checked[i] || _zones[i] == null) continue;
                Vector2 half = _zones[i].sizeDelta / 2f;
                bool over = Mathf.Abs(local.x - _zoneCenters[i].x) <= half.x && Mathf.Abs(local.y - _zoneCenters[i].y) <= half.y;
                _zoneProgress[i] = SearchRules.Advance(_zoneProgress[i], Time.unscaledDeltaTime, over);
                if (SearchRules.IsSearched(_zoneProgress[i])) CheckSpot(i);
            }

            // the hidden student: hold the circle on them and they step forward
            if (_current != null)
            {
                bool onStudent = Vector2.Distance(local, _studentSpot) <= LampRadius;
                _studentProgress = SearchRules.Advance(_studentProgress, Time.unscaledDeltaTime, onStudent);
                if (SearchRules.IsSearched(_studentProgress)) StudentFound();
            }
        }

        private void CheckSpot(int idx)
        {
            if (_phase != Phase.Search || _checked[idx]) return;
            _checked[idx] = true;
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.5f);

            if (idx == _anomalySpot)
            {
                _gameRoot.Session.ApplySanity(-8f);
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, _gameRoot.Session.Sanity));
                if (_gameRoot.Scares != null) _gameRoot.Scares.Stinger("security_room_anomaly", 0f);
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*something was crouched here — and it is gone now*", 3f, null));
                _feedback.text = "You find a hollow space in the dust. Something was crouched here minutes ago. It is not here now.";
            }
            else
            {
                string[] flavor =
                {
                    "Shelves: attendance binders, all signed in the same handwriting.",
                    "Window: the yard is empty. The streetlight hums. Nothing moves.",
                    "Under the desks: nothing but chalk dust and a lost pencil.",
                };
                _feedback.text = flavor[idx];
            }
            TryFinish();
        }
    }
}
