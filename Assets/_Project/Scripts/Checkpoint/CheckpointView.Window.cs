using System.Collections;
using System.Collections.Generic;
using System.Text;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.Checkpoint
{
    // The window slot: visitor portrait, the offered ID hand-over, glitch hints.
    // One area of the inspection desk; see CheckpointView.cs for the state machine,
    // the shared fields and the whole-class comment. Same class, split by desk area.
    public partial class CheckpointView
    {
        private void BuildWindowArea()
        {
            var win = UIFactory.Img(_desk, "windowframe", ProceduralArt.Get("window_frame"));
            Reg("window", win.rectTransform, L.Window);
            win.raycastTarget = false;

            var glass = UIFactory.Panel(_desk, "glass", new Color(0.227f, 0.227f, 0.251f));
            Reg("glass", glass.rectTransform, L.Glass);
            glass.raycastTarget = false;
            var facade = UIFactory.Img(glass.rectTransform, "facade", ProceduralArt.Get("school_facade"));
            UIFactory.AnchorsFull(facade.rectTransform);
            facade.raycastTarget = false;
            var tint = UIFactory.Panel(glass.rectTransform, "tint", UIFactory.Glass);
            UIFactory.AnchorsFull(tint.rectTransform);
            tint.raycastTarget = false;

            _visitorPortrait = UIFactory.Img(_desk, "visitor", null);
            Reg("portrait", _visitorPortrait.rectTransform, L.Portrait);
            _visitorPortrait.preserveAspect = true;

            _glitchOverlay = UIFactory.Img(_desk, "glitch", ProceduralArt.Get("noise_overlay"), new Color(1, 1, 1, 0f));
            Reg("glitch", _glitchOverlay.rectTransform, L.Glitch);
            _glitchOverlay.raycastTarget = false;

            var sill = UIFactory.Panel(_desk, "sill", new Color(0.10f, 0.10f, 0.11f));
            Reg("sill", sill.rectTransform, L.Sill);
            sill.raycastTarget = false;

            // invisible click target over the slot: "interact with the table" — clicking it
            // makes the visitor offer their ID across the glass
            // raycast ON: this invisible panel is the "interact with the table" click target
            var handImg = UIFactory.Img(_desk, "handover", null, new Color(0f, 0f, 0f, 0f), true);
            Reg("handover", handImg.rectTransform, L.Window);
            var handBtn = handImg.gameObject.AddComponent<Button>();
            handBtn.targetGraphic = handImg;
            handBtn.onClick.AddListener(OnWindowClicked);
            _handoverTarget = handImg.gameObject;
            _handoverTarget.SetActive(false);
        }

        // The hand-over step: a dimmer plus the centred offered ID. Clicking the card takes
        // it (docks into the desk slot and reveals the papers). Built once, hidden always.
        private void BuildOfferedId()
        {
            var rootRt = UIFactory.NewUI(_desk, "offeredroot");
            L.Place(rootRt, L.FullDesk);
            UIFactory.Dimmer(rootRt, "dim", 0.4f);   // focus the card; blocks the desk beneath

            var rt = UIFactory.NewUI(rootRt, "offeredid");
            L.Place(rt, L.OfferedId);
            // raycast ON: clicking anywhere on the offered card takes the ID
            var frame = UIFactory.Img(rt, "frame", ProceduralArt.Get("id_card_frame"), null, true);
            UIFactory.AnchorsFull(frame.rectTransform);
            _offeredPhoto = UIFactory.Img(rt, "photo", null);
            L.Place(_offeredPhoto.rectTransform, L.Inner.IdPhoto);
            _offeredPhoto.preserveAspect = true;
            _offeredName = OfferedRow(rt, 0);
            _offeredNumber = OfferedRow(rt, 1);
            _offeredExpires = OfferedRow(rt, 5);
            var hint = UIFactory.Label(rt, "hint", "CLICK TO TAKE THE ID", 12,
                UIFactory.InkDim, TextAnchor.MiddleCenter);
            L.Place(hint.rectTransform, new L.Region(0, 202, 320, 16));
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = frame;
            btn.onClick.AddListener(TakeId);

            _offeredRoot = rootRt.gameObject;
            _offeredRoot.SetActive(false);
        }

        // Offered card rows reuse the ID-card grid offsets (same card size).
        private Text OfferedRow(RectTransform parent, int rowIndex)
        {
            var t = UIFactory.Label(parent, "row" + rowIndex, "", 12, UIFactory.PaperInk);
            L.Place(t.rectTransform, new L.Region(L.Inner.IdRowX,
                L.Inner.IdRules[rowIndex] - L.Inner.IdRowH, L.Inner.IdRowW, L.Inner.IdRowH));
            return t;
        }

        // Click on the window slot: the visitor offers the ID across the glass.
        private void OnWindowClicked()
        {
            if (_current == null || _verdictLocked || _documentsTaken) return;
            if (_offeredRoot.activeSelf) return;
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.6f);
            FillOfferedCard(_current);
            _offeredRoot.SetActive(true);
            RefreshHandoverPrompt();
        }

        // The centred card shows just enough to read at arm's length: face, name, number, expiry.
        private void FillOfferedCard(VisitorData v)
        {
            bool admin = v.IsAdministrator;
            _offeredPhoto.sprite = ProceduralArt.Portrait("card", v.IdPhoto, 168, 208);
            _offeredName.text = "NAME: " + v.ClaimedName;
            _offeredNumber.text = (admin ? "BADGE NO: " : "ID NO: ") + v.IdNumber;
            _offeredExpires.text = (admin ? "VALID THRU: " : "EXPIRES: ") + v.ExpirationDisplay;
        }

        // Click on the offered card: take it. The papers dock on the desk and unlock.
        private void TakeId()
        {
            if (_current == null || _documentsTaken) return;
            _offeredRoot.SetActive(false);
            _documentsTaken = true;
            SetDocumentsVisible(true);
            bool admin = _current.IsAdministrator;
            FillIdCard(_current, admin);
            FillRecordSheet(_current, admin);
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.6f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.DocumentsShown, _current));
            RefreshHandoverPrompt();
            RefreshHud();
        }

        private void SetVisitorVisible(bool on)
        {
            _visitorPortrait.gameObject.SetActive(on);
            _speechPanel.gameObject.SetActive(on);
            _glitchOverlay.gameObject.SetActive(on && _glitchOverlay.color.a > 0f);
        }

        // Swaps in the new faces and clears the old stamp; the fade-in coroutine is
        // instant under Reduced Motion (the coroutine itself checks the setting).
        private void ShowVisitorPortrait(VisitorData v)
        {
            SetVisitorVisible(true);
            _visitorPortrait.sprite = ProceduralArt.Portrait("window", v.WindowPortrait, 240, 296);
            _idPhoto.sprite = ProceduralArt.Portrait("card", v.IdPhoto, 168, 208);
            if (_stampOverlay != null)
            {
                var sc = _stampOverlay.color; sc.a = 0f; _stampOverlay.color = sc;
                _stampOverlay.sprite = null;
            }
            var pc = _visitorPortrait.color; pc.a = 0f; _visitorPortrait.color = pc;
            StartCoroutine(PortraitFadeIn());
        }

        private IEnumerator PortraitFadeIn()
        {
            bool reduce = _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;
            if (reduce)
            {
                var cr = _visitorPortrait.color; cr.a = 1f; _visitorPortrait.color = cr;
                yield break;
            }
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                var c = _visitorPortrait.color; c.a = Mathf.Clamp01(t / 0.3f); _visitorPortrait.color = c;
                yield return null;
            }
            var c2 = _visitorPortrait.color; c2.a = 1f; _visitorPortrait.color = c2;
        }

        private IEnumerator GlitchHint()
        {
            yield return new WaitForSecondsRealtime(1.6f);
            if (_current == null || !gameObject.activeInHierarchy) yield break;
            _glitchOverlay.gameObject.SetActive(true);
            var c = _glitchOverlay.color; c.a = 0.35f; _glitchOverlay.color = c;
            _gameRoot.Audio.PlaySfx("sfx_glitch", 0.35f);
            yield return new WaitForSecondsRealtime(0.12f);
            c = _glitchOverlay.color; c.a = 0f; _glitchOverlay.color = c;
            _glitchOverlay.gameObject.SetActive(false);
        }

        // Knock first, then either the administrator's scripted dialogue or the visitor's
        // greeting (plus a one-off glitch flash for visibly glitchy anomalies at low sanity).
        // Paper sounds and DocumentsShown now fire on the hand-over step, not on arrival.
        private void PlayArrivalVoices(VisitorData v, bool admin)
        {
            _gameRoot.Audio.PlaySfx("sfx_knock", 0.7f);
            if (admin)
            {
                var lines = new List<DialogueLine>
                {
                    new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = Content.AdminLines[0] },
                    new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = Content.AdminLines[1] },
                };
                if (_gameRoot.Session.Clues.FoundCount >= 6)
                    lines.Add(new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "That face. The old school photo — the staff member who never aged. It's them." });
                lines.Add(new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = Content.AdminLines[3] });
                SayLine("AT THE WINDOW", "A formal coat. A briefcase. A smile that arrives a half-second late.");
                if (_gameRoot.Dialogue != null) _gameRoot.Dialogue.Play(lines, null);
                _gameRoot.Audio.PlayMusic("final");
            }
            else
            {
                Say(v.Greeting);
                if (v.IsAnomaly && v.BehaviorGlitchVisible && _gameRoot.Session.Sanity < 70f)
                    StartCoroutine(GlitchHint());
            }
        }
    }
}
