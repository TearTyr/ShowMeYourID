using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.Checkpoint
{
    // The window talk dock: the desk's DIALOGUE PANEL — a transcript sheet plus the typed
    // text box the whole window conversation runs through.
    // One area of the inspection desk; see CheckpointView.cs for the shared fields and state
    // machine, and GameLogic/SpokenChat.cs for the pure routing rules.
    //
    // Why this exists: the dialogue box the kit paints (and the old desk mockups) always had
    // a text well in it, and no previous build ever wired one to anything. The box here IS
    // the input state of the conversation: what you type is routed by SpokenChat into the
    // real interaction loop — a typed verification question spends the QuestionSystem budget
    // exactly like its panel button (two-question cap, no repeats), everything else gets an
    // in-character answer from the visitor — and every exchange lands in the transcript sheet
    // and, for the visitor's voice, in the speech bubble at the glass.
    //
    // Input contract (mirrored by the headless InputField stub):
    //   Enter sends · Shift+Enter breaks the line · V focuses the box · Escape leaves it.
    //   Empty sends are refused (text kept), a send in flight blocks duplicates ("…"),
    //   a closed window mid-thought is an error that KEEPS the typed text, success clears
    //   the box and keeps the focus so a conversation can be typed back-to-back.
    public partial class CheckpointView
    {
        private RectTransform _dockRoot;
        private InputField _talkInput;
        private Button _talkSend, _talkLogToggle;
        private GameObject _logRoot;
        private readonly List<Text> _logRows = new List<Text>();
        private Text _logHead, _logHint;
        private readonly WindowTranscript _transcript = new WindowTranscript();
        private bool _talkBusy;                 // a send is in flight (loading state)
        private float _talkBusyT;
        private string _talkPending = "";       // text captured at send time
        private SpokenReply _talkPendingReply;
        private bool _logOpen, _logPinned;
        private float _logAutoT;                // >0: the sheet breathes open for a while

        internal WindowTranscript Transcript => _transcript;   // headless proofs read it
        internal InputField TalkInput => _talkInput;

        private bool CanSpeak => _gameRoot != null && _current != null && !_verdictLocked &&
            (_gameRoot.State == AppViewState.Checkpoint || _gameRoot.State == AppViewState.AdminEncounter);

        // ---------------------------------------------------------------- build
        private void BuildTalkDock()
        {
            // the transcript sheet first (under the dock strip, over the paper spread)
            var log = UIFactory.BorderedPanel(_desk, "talklog", new Color(0.055f, 0.055f, 0.06f, 0.96f),
                UIFactory.Border, 2);
            Reg("talklog", log, L.TalkLog);
            // the sheet is READ-ONLY desk paper: it opens over the document spread while a
            // conversation runs, so every pixel of it is click-through — the papers, the
            // dock and everything under the sheet keep taking the pointer through it
            foreach (var img in log.gameObject.GetComponentsInChildren<Image>(true)) img.raycastTarget = false;
            _logRoot = log.gameObject;
            _logHead = UIFactory.Label(log, "loghead", "WINDOW LOG", 11, UIFactory.AccentBright,
                TextAnchor.MiddleLeft);
            _logHead.font = UIFactory.UiFontBold;
            _logHead.raycastTarget = false;
            L.Place(_logHead.rectTransform, L.Inner.TalkLogHead);
            for (int i = 0; i < L.Inner.TalkLogRows; i++)
            {
                var row = UIFactory.Label(log, "trow" + i, "", 10, UIFactory.NsPaperDim);
                row.raycastTarget = false;
                L.Place(row.rectTransform, new L.Region(L.Inner.TalkLogRowX,
                    L.Inner.TalkLogRowY0 + i * L.Inner.TalkLogRowStep,
                    L.Inner.TalkLogRowW, L.Inner.TalkLogRowH));
                _logRows.Add(row);
            }
            _logHint = UIFactory.Label(log, "loghint",
                "ENTER SENDS · SHIFT+ENTER NEW LINE · V FOCUS · ESC LEAVES · LOG PINS", 9,
                UIFactory.InkFaint, TextAnchor.MiddleLeft);
            _logHint.raycastTarget = false;
            L.Place(_logHint.rectTransform, L.Inner.TalkLogHint);
            _logRoot.SetActive(false);

            // the dock strip: LOG toggle · the text box · SEND
            _dockRoot = UIFactory.NewUI(_desk, "talkdock");
            Reg("talkdock", _dockRoot, L.TalkDock);
            _talkLogToggle = GridButton(_dockRoot, "talklogtoggle", "LOG", ToggleTalkLogPin,
                L.Inner.TalkLogToggle, 9);
            L.Place(_talkLogToggle.GetComponent<RectTransform>(), L.Inner.TalkLogToggle);
            RegPlaced("talklogtoggle", _talkLogToggle, L.Inner.TalkLogToggle);
            _talkInput = UIFactory.TextField(_dockRoot, "talkinput", TalkPlaceholder(), 11,
                UIFactory.NsChalk, new Color(0.043f, 0.043f, 0.05f, 0.94f));
            var inputRt = (RectTransform)_talkInput.transform;
            L.Place(inputRt, L.Inner.TalkInput);
            RegPlaced("talkinput", inputRt, L.Inner.TalkInput);
            _talkInput.characterLimit = SpokenChat.MaxChars;
            _talkInput.onSubmit.AddListener(_ => SubmitTalk());
            _talkSend = GridButton(_dockRoot, "talksend", "SEND ▸", SubmitTalk, L.Inner.TalkSend, 10);
            L.Place(_talkSend.GetComponent<RectTransform>(), L.Inner.TalkSend);
            RegPlaced("talksend", _talkSend, L.Inner.TalkSend);
            RefreshTalkDock();
        }

        // register an already-placed element: dock children are placed DOCK-LOCAL (they ride
        // the strip) but the audit table speaks absolute desk coordinates
        private void RegPlaced(string name, Button b, L.Region audit) =>
            RegPlaced(name, b.GetComponent<RectTransform>(), audit);

        private void RegPlaced(string name, RectTransform rt, L.Region audit) =>
            _audit.Register(name, rt, audit);

        private string TalkPlaceholder() => CanSpeak
            ? "SPEAK THROUGH THE GLASS…"
            : "NO ONE AT THE WINDOW…";

        // ---------------------------------------------------------------- submit
        // The one path from typed text into the interaction loop. Buttons, the field's own
        // onSubmit (Enter) and the headless proofs all land here.
        internal void SubmitTalk()
        {
            if (_talkBusy) return;                       // no duplicate sends
            if (_talkInput == null) return;
            if (!CanSpeak) { RefreshTalkDock(); return; }
            string raw = _talkInput.text;
            string norm = SpokenChat.Normalize(raw);
            if (norm.Length == 0)
            {
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification, "NOTHING TO SAY."));
                _gameRoot.Audio.PlaySfx("sfx_scan_error", 0.3f);
                return;                                  // the typed text stays put
            }
            var reply = SpokenChat.Route(BuildSpokenContext(), raw);
            if (reply.Topic == SpokenTopic.Rejected)
            {
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification, reply.RejectReason));
                _gameRoot.Audio.PlaySfx("sfx_scan_error", 0.3f);
                return;
            }
            _talkPending = raw;
            _talkPendingReply = reply;
            _talkBusy = true;
            _talkBusyT = 0f;
            RefreshTalkDock();                           // enters the "…" loading state
        }

        private SpokenContext BuildSpokenContext()
        {
            var v = _current;
            return new SpokenContext
            {
                HasVisitor = v != null && !_verdictLocked,
                IsAdmin = v != null && v.IsAdministrator,
                ClaimedName = v != null ? v.ClaimedName : "",
                ClaimedId = v != null ? v.IdNumber : "",
                ClaimedRoom = v != null ? v.RecordFirstClassroom : "",
                Variant = _transcript.Count,
            };
        }

        // One frame of the dock: the send's short processing beat, then the commit (or the
        // error path when the window closed mid-thought), plus the sheet's auto-collapse.
        // internal so the headless FlowCheck can step it deterministically.
        internal void TickTalk(float dt)
        {
            if (_gameRoot == null || _dockRoot == null) return;
            if (_gameRoot.Paused || _gameRoot.BlockingOverlayOpen) return;   // frozen like the bubble
            if (_logAutoT > 0f)
            {
                _logAutoT -= dt;
                if (_logAutoT <= 0f && !_logPinned) SetTalkLogOpen(false);
            }
            if (!_talkBusy) return;
            _talkBusyT += dt;
            if (_talkBusyT < 0.25f) return;
            _talkBusy = false;
            if (!CanSpeak)
            {
                // error state: the visitor left / the verdict fell while the line was
                // in flight — the typed text is restored, never swallowed
                if (_talkInput != null) _talkInput.text = _talkPending;
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    "THE WINDOW CLOSED BEFORE YOU SPOKE."));
                _gameRoot.Audio.PlaySfx("sfx_scan_error", 0.35f);
                RefreshTalkDock();
                return;
            }
            CommitTalk(_talkPending, _talkPendingReply);
            RefreshTalkDock();
        }

        private void CommitTalk(string raw, SpokenReply reply)
        {
            string line = raw.Trim();
            if (line.Length > 96) line = line.Substring(0, 96) + "…";
            _transcript.Add("YOU", line, true);
            string voice = _current != null && _current.IsAdministrator ? "ADMIN" : "VISITOR";
            if (reply.Topic == SpokenTopic.Question)
            {
                // typed questions ride the REAL budget path: AskQuestion spends it, echoes
                // the answer bubble and writes the transcript's answer line itself.
                AskQuestion(reply.Question);
            }
            else
            {
                SayLine("AT THE WINDOW", reply.Text);
                _transcript.Add(voice, reply.Text, false);
                _gameRoot.Audio.PlaySfx("sfx_intercom", 0.35f);
            }
            // success: clear the box, keep the focus, let the sheet breathe open
            if (_talkInput != null)
            {
                _talkInput.text = "";
                if (!_talkInput.isFocused) _talkInput.ActivateInputField();
            }
            _logAutoT = 4f;
            SetTalkLogOpen(true);
            RefreshHud();
        }

        // ---------------------------------------------------------------- sheet
        private void SetTalkLogOpen(bool open)
        {
            _logOpen = open;
            if (_logRoot != null) _logRoot.SetActive(open);
            if (open) SyncTalkLog();
        }

        private void ToggleTalkLogPin()
        {
            if (_logOpen) { _logPinned = false; _logAutoT = 0f; SetTalkLogOpen(false); }
            else { _logPinned = true; _logAutoT = 0f; SetTalkLogOpen(true); }
            _gameRoot.Audio.PlaySfx("sfx_ui_open", 0.4f);
        }

        // Model -> rows: the newest eight entries, oldest first, with a "+N EARLIER" marker
        // when the bounded memory has dropped lines.
        private void SyncTalkLog()
        {
            if (_logRoot == null || !_logRoot.activeSelf) return;
            var last = _transcript.Last(L.Inner.TalkLogRows);
            int dropped = _transcript.Count - last.Count;
            for (int i = 0; i < _logRows.Count; i++)
            {
                string s = "";
                Color c = UIFactory.NsPaperDim;
                if (i == 0 && dropped > 0 && last.Count == L.Inner.TalkLogRows)
                {
                    s = "+" + dropped + " EARLIER";
                    c = UIFactory.InkFaint;
                }
                else
                {
                    int idx = i - ((dropped > 0 && last.Count == L.Inner.TalkLogRows) ? 1 : 0);
                    if (idx >= 0 && idx < last.Count)
                    {
                        var e = last[idx];
                        s = e.Who + " — " + e.Text;
                        c = e.FromPlayer ? UIFactory.NsChalk
                          : e.Who == "DESK" ? UIFactory.AccentBright
                          : UIFactory.NsPaperDim;
                    }
                }
                _logRows[i].text = s;
                _logRows[i].color = c;
            }
            if (_logHead != null)
                _logHead.text = "WINDOW LOG — " + (_current != null ? _current.ClaimedName : "EMPTY DESK");
        }

        // ---------------------------------------------------------------- states
        // enabled/disabled/loading/placeholder all derive from one place, so the box can
        // never look usable when nobody is at the window (or look dead when someone is).
        private void RefreshTalkDock()
        {
            if (_talkInput == null || _dockRoot == null) return;
            bool can = CanSpeak;
            _talkInput.interactable = can && !_talkBusy;
            UIFactory.SetButtonEnabled(_talkSend, can && !_talkBusy);
            var sendTxt = _talkSend.GetComponentInChildren<Text>();
            if (sendTxt != null) sendTxt.text = _talkBusy ? "…" : "SEND ▸";
            var ph = _talkInput.placeholder as Text;
            if (ph != null)
            {
                ph.text = _talkBusy ? "…" : TalkPlaceholder();
                ph.enabled = _talkInput.text.Length == 0;
            }
            if (_talkInput.textComponent != null) _talkInput.textComponent.text = _talkInput.text;
            if (_logOpen) SyncTalkLog();
        }

        // V puts the caret in the box (the mouse click does the same in the editor).
        internal void FocusTalk()
        {
            if (_talkInput == null || !CanSpeak) return;
            _talkInput.ActivateInputField();   // real uGUI API (there is no InputField.Focus)
        }

        internal void BlurTalk()
        {
            if (_talkInput != null && _talkInput.isFocused) _talkInput.DeactivateInputField();
        }

        // A new visitor wipes the conversation memory and closes the sheet: the log belongs
        // to the inspection, not to the shift. Called from ResetHandover.
        internal void ResetTalkDock()
        {
            _transcript.Clear();
            _talkBusy = false;
            _talkPending = "";
            _logPinned = false;
            _logAutoT = 0f;
            SetTalkLogOpen(false);
            if (_talkInput != null) { _talkInput.text = ""; BlurTalk(); }
            RefreshTalkDock();
        }
    }
}
