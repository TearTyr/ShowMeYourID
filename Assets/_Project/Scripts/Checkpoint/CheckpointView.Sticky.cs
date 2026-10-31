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
    // The shift-start sticky note gate.
    // One area of the inspection desk; see CheckpointView.cs for the state machine,
    // the shared fields and the whole-class comment. Same class, split by desk area.
    public partial class CheckpointView
    {
        // The shift-start gate. A centered sticky note over a click-blocking
        // dimmer: nothing arrives, nothing can be inspected until it is clicked.
        private const string NightRules = "Rules: Check IDs, ask questions, stamp. Don't let the wrong faces in.";

        // Night 1 only: the five steps, short enough to read on a sticky note.
        private const string HowToPlay =
            "1. READ the ID and the slip. Click the ID's black spine to flip it.\n" +
            "2. ASK up to 2 of the 4 questions. Some unlock after you flip the ID or open the file.\n" +
            "3. CHECK their file: click the cabinet, pick the room, pick the name.\n" +
            "4. STAMP: drag ACCEPT or DECLINE onto the slip.\n" +
            "5. HAND BACK: drag the ID to the window. That decision is final.\n" +
            "\nBetween students, PATROL: A and D walk, E inspects, F is your flashlight.";

        private Text _stickyTitle, _stickyBody;

        // The note reads differently on the first night of a normal run.
        private void RefreshStickyNote()
        {
            bool firstNight = _gameRoot.Session.Mode == GameMode.Normal && _gameRoot.Session.CurrentShift == 1;
            _stickyTitle.text = firstNight ? "HOW TO PLAY" : "NIGHT SHIFT BEGINS.";
            _stickyBody.text = firstNight ? HowToPlay : NightRules;
            _stickyBody.alignment = firstNight ? TextAnchor.UpperLeft : TextAnchor.MiddleCenter;
            UIFactory.SetBaseFontSize(_stickyBody, firstNight ? 14 : 16);
        }

        private void BuildStickyNote()
        {
            var rootRt = UIFactory.NewUI(_desk, "StickyNoteOverlay");
            L.Place(rootRt, L.FullDesk);
            UIFactory.Dimmer(rootRt, "dim", 0.66f);   // Panel raycast blocks the whole desk

            var panel = UIFactory.BorderedPanel(rootRt, "stickynote", UIFactory.Paper, UIFactory.Border, 3);
            Reg("stickynote", panel, L.StickyNote);

            _stickyTitle = UIFactory.Label(panel, "title", "NIGHT SHIFT BEGINS.", 26,
                UIFactory.PaperInk, TextAnchor.MiddleCenter);
            L.Place(_stickyTitle.rectTransform, L.Inner.StickyTitle);
            _stickyBody = UIFactory.Label(panel, "body", NightRules,
                16, new Color(0.16f, 0.16f, 0.17f), TextAnchor.MiddleCenter);
            L.Place(_stickyBody.rectTransform, L.Inner.StickyBody);
            var cta = UIFactory.Label(panel, "cta", "[CLICK TO BEGIN]", 18,
                new Color(0.55f, 0.12f, 0.14f), TextAnchor.MiddleCenter);
            L.Place(cta.rectTransform, L.Inner.StickyCta);

            _stickyButton = panel.gameObject.AddComponent<Button>();
            _stickyButton.targetGraphic = panel.GetComponent<Image>();
            _stickyButton.onClick.AddListener(OnStickyClicked);
            _stickyRoot = rootRt.gameObject;
            _stickyRoot.SetActive(false);
        }

        private void OnStickyClicked()
        {
            if (Queue == null || Queue.Started || !_flowActive) return;
            _gameRoot.Audio.PlaySfx("sfx_ui_open", 0.7f);
            _stickyRoot.SetActive(false);
            Queue.MarkStarted();
            Debug.Log("[Flow] sticky note clicked - shift started, first visitor arriving");
            ApplyTickResult(Queue.ForceNextArrival(), false);
            RefreshHud();
        }
    }
}
