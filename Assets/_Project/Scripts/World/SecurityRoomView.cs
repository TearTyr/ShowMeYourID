using System.Collections.Generic;
using Hollowbrook.Core;
using Hollowbrook.UI;
using Hollowbrook.Bootstrap;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.World
{
    /// <summary>Scene B prototype — the security office, door-facing. Minimal but complete loop:
    ///  1) late students knock; you verify their ID against tonight's late list + expiry rule
    ///     and stamp Allow / Reject (3 students, immediate correctness feedback);
    ///  2) you check around the room (three spots; one hides an anomaly — sting + sanity cost);
    ///  3) return to the checkpoint scene.
    /// Deliberately small and testable; verdicts here are prototype-local and do NOT touch the
    /// story session counters.</summary>
    public class SecurityRoomView : MonoBehaviour, IScreenView
    {
        private GameRoot R;
        private Image _doorArt, _portrait;
        private Image _idCard;
        private Text _doorState, _idFields, _feedback, _progress;
        private Button _allowBtn, _rejectBtn, _returnBtn;
        private Button[] _checkBtns = new Button[3];
        private GameObject _docPanel;

        private enum Phase { Idle, Student, RoomCheck, Done }
        private Phase _phase = Phase.Idle;
        private readonly List<VisitorData> _queue = new List<VisitorData>();
        private VisitorData _current;
        public VisitorData CurrentStudent => _current;
        private int _correct;
        private bool[] _checked = new bool[3];
        private int _anomalySpot;

        public void Init(GameRoot root)
        {
            R = root;
            var bg = UIFactory.Img(transform, "bg", ProceduralArt.Get("security_bg"));
            UIFactory.AnchorsFull(bg.rectTransform);
            bg.raycastTarget = true;

            var title = UIFactory.Label(transform, "title", "SECURITY OFFICE — LATE ARRIVALS", 24,
                UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(800, 36), new Vector2(0, -40));

            _doorState = UIFactory.Label(transform, "doorstate", "The door is shut. The night class is in session.",
                16, UIFactory.InkDim, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_doorState.rectTransform, new Vector2(0.5f, 1f), new Vector2(800, 26), new Vector2(0, -84));

            // the door you face
            _doorArt = UIFactory.Img(transform, "door", ProceduralArt.Get("prop_door_security"));
            UIFactory.Anchor(_doorArt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(220, 400), new Vector2(0, 60));

            _portrait = UIFactory.Img(transform, "face", null);
            UIFactory.Anchor(_portrait.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(150, 186), new Vector2(0, 150));
            _portrait.gameObject.SetActive(false);

            // ID card panel
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
            var rule = UIFactory.Label(docRt, "rule",
                "RULE: admit only students on tonight's late list whose ID is NOT expired.",
                13, new Color(0.45f, 0.12f, 0.14f), TextAnchor.MiddleCenter);
            UIFactory.Anchor(rule.rectTransform, new Vector2(0.5f, 1f), new Vector2(340, 40), new Vector2(0, -18));
            _docPanel.SetActive(false);

            // verdict buttons
            _allowBtn = UIFactory.StandardButton(transform, "allow", "✓ ALLOW", () => Decide(Verdict.Allow), 150, 52);
            UIFactory.Anchor(_allowBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(150, 52), new Vector2(-330, 60));
            _rejectBtn = UIFactory.StandardButton(transform, "reject", "✗ REJECT", () => Decide(Verdict.Reject), 150, 52, true);
            UIFactory.Anchor(_rejectBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(150, 52), new Vector2(-160, 60));

            _feedback = UIFactory.Label(transform, "feedback", "", 16, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_feedback.rectTransform, new Vector2(0.5f, 0f), new Vector2(900, 60), new Vector2(0, 120));
            _progress = UIFactory.Label(transform, "progress", "", 14, UIFactory.InkDim, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_progress.rectTransform, new Vector2(0.5f, 0f), new Vector2(900, 24), new Vector2(0, 96));

            // room-check phase buttons
            string[] spots = { "CHECK THE SHELVES", "CHECK THE WINDOW", "CHECK UNDER THE DESKS" };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var b = UIFactory.StandardButton(transform, "check" + i, spots[i], () => CheckSpot(idx), 260, 44);
                UIFactory.Anchor(b.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(260, 44), new Vector2(180, 120 - i * 56));
                _checkBtns[i] = b;
            }

            _returnBtn = UIFactory.StandardButton(transform, "ret", "RETURN TO THE CHECKPOINT ▸", () =>
            {
                R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_door", 0.7f, null));
                R.ReturnToCheckpoint();
            }, 320, 48);
            UIFactory.Anchor(_returnBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(320, 48), new Vector2(0, 36));

            gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- flow

        public void Show()
        {
            gameObject.SetActive(true);
            StartSequence();
        }

        public void Hide() => gameObject.SetActive(false);

        private void StartSequence()
        {
            _phase = Phase.Student;
            _correct = 0;
            _queue.Clear();
            for (int i = 0; i < 3; i++) _queue.Add(R.NextRunVisitor());
            _anomalySpot = Random.Range(0, 3);
            for (int i = 0; i < 3; i++) { _checked[i] = false; UIFactory.SetButtonEnabled(_checkBtns[i], false); }
            UIFactory.SetButtonEnabled(_returnBtn, false);
            _feedback.text = "";
            NextStudent();
        }

        private void NextStudent()
        {
            if (_queue.Count == 0) { BeginRoomCheck(); return; }
            _current = _queue[0];
            _queue.RemoveAt(0);

            R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_knock", 0.8f, null));
            R.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*three quick knocks on the office door*", 2.4f, null));
            _doorState.text = "A student slips in out of the dark. They hold out their ID.";
            _portrait.sprite = ProceduralArt.Portrait("sec", _current.WindowPortrait, 150, 186);
            _portrait.gameObject.SetActive(true);
            _docPanel.SetActive(true);

            bool expired = IdRules.IsExpired(_current, Mathf.Max(1, R.Session.CurrentShift));
            var sb = new System.Text.StringBuilder();
            sb.Append("NAME:  ").Append(_current.ClaimedName).Append('\n');
            sb.Append("ID NO: ").Append(_current.IdNumber).Append('\n');
            sb.Append("YEAR:  ").Append(Content.YearNames[_current.IdYearLevel - 1]).Append('\n');
            sb.Append("EXPIRES: ").Append(_current.ExpirationDisplay).Append(expired ? "   (EXPIRED)" : "").Append('\n');
            sb.Append("ON TONIGHT'S LATE LIST: ").Append(_current.InDatabase ? "YES" : "NO — no record");
            _idFields.text = sb.ToString();

            _progress.text = "LATE ARRIVALS REMAINING: " + (_queue.Count + 1) + "   ·   CORRECT CALLS: " + _correct + "/3";
            UIFactory.SetButtonEnabled(_allowBtn, true);
            UIFactory.SetButtonEnabled(_rejectBtn, true);
        }

        private void Decide(Verdict v)
        {
            if (_phase != Phase.Student || _current == null) return;
            bool shouldReject = _current.IsAnomaly || !IdRules.IsFormatValid(_current.IdNumber)
                                || IdRules.IsExpired(_current, Mathf.Max(1, R.Session.CurrentShift))
                                || !_current.InDatabase;
            bool correct = (v == Verdict.Reject) == shouldReject;
            if (correct) _correct++;
            R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested,
                v == Verdict.Allow ? "sfx_stamp_allow" : "sfx_stamp_reject", 0.9f, null));

            var fb = new System.Text.StringBuilder();
            fb.Append(correct ? "CORRECT. " : "MISTAKE. ");
            if (shouldReject)
            {
                if (_current.IsAnomaly) fb.Append("Something about them was wrong — the way they stood, the way the photo didn't sit.");
                else if (!_current.InDatabase) fb.Append("No record of that ID number anywhere.");
                else if (IdRules.IsExpired(_current, Mathf.Max(1, R.Session.CurrentShift))) fb.Append("That ID expired months ago.");
                else fb.Append("That ID number is not in the required format.");
            }
            else fb.Append("A real student, late for night class. They needed to get in.");
            _feedback.text = fb.ToString();
            _progress.text = "LATE ARRIVALS REMAINING: " + _queue.Count + "   ·   CORRECT CALLS: " + _correct + "/3";

            UIFactory.SetButtonEnabled(_allowBtn, false);
            UIFactory.SetButtonEnabled(_rejectBtn, false);
            _portrait.gameObject.SetActive(false);
            _docPanel.SetActive(false);
            _doorState.text = "The door closes. Footsteps fade down the hall.";
            _current = null;
            StartCoroutine(NextAfter(1.6f));
        }

        private System.Collections.IEnumerator NextAfter(float s)
        {
            yield return new WaitForSecondsRealtime(s);
            if (gameObject.activeInHierarchy) NextStudent();
        }

        private void BeginRoomCheck()
        {
            _phase = Phase.RoomCheck;
            _doorState.text = "Quiet now. Protocol says check the room before you lock up.";
            _feedback.text = "";
            for (int i = 0; i < 3; i++) UIFactory.SetButtonEnabled(_checkBtns[i], true);
        }

        private void CheckSpot(int idx)
        {
            if (_phase != Phase.RoomCheck || _checked[idx]) return;
            _checked[idx] = true;
            UIFactory.SetButtonEnabled(_checkBtns[idx], false);
            R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_paper", 0.5f, null));

            if (idx == _anomalySpot)
            {
                R.Session.ApplySanity(-8f);
                R.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, R.Session.Sanity));
                if (R.Scares != null) R.Scares.Stinger("security_room_anomaly", 0f);
                R.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*something was crouched here — and it is gone now*", 3f, null));
                _feedback.text = "You find a hollow space in the dust. Something was crouched here minutes ago. It is not here now.";
            }
            else
            {
                string[] flavor =
                {
                    "Shelves: attendance binders, all signed in the same handwriting.",
                    "Window: the yard is empty. The streetlight hums. Nothing moves.",
                };
                int fi = idx == 0 ? 0 : 1;
                _feedback.text = flavor[fi];
            }

            bool all = _checked[0] && _checked[1] && _checked[2];
            if (all)
            {
                _phase = Phase.Done;
                _doorState.text = "Room checked. The office holds its breath.";
                _feedback.text += "   — Check complete (" + _correct + "/3 correct calls).";
                UIFactory.SetButtonEnabled(_returnBtn, true);
            }
        }
    }
}
