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
        // Phase 10.1 — the shift-start gate. A centered sticky note over a click-blocking
        // dimmer: nothing arrives, nothing can be inspected until it is clicked.
        private void BuildStickyNote()
        {
            var rootRt = UIFactory.NewUI(_desk, "StickyNoteOverlay");
            L.Place(rootRt, L.FullDesk);
            UIFactory.Dimmer(rootRt, "dim", 0.66f);   // Panel raycast blocks the whole desk

            var panel = UIFactory.BorderedPanel(rootRt, "stickynote", UIFactory.Paper, UIFactory.Border, 3);
            Reg("stickynote", panel, L.StickyNote);

            var title = UIFactory.Label(panel, "title", "NIGHT SHIFT BEGINS.", 26,
                UIFactory.PaperInk, TextAnchor.MiddleCenter);
            L.Place(title.rectTransform, L.Inner.StickyTitle);
            var body = UIFactory.Label(panel, "body",
                "Rules: Check IDs, ask questions, stamp. Don't let the wrong faces in.",
                16, new Color(0.16f, 0.16f, 0.17f), TextAnchor.MiddleCenter);
            L.Place(body.rectTransform, L.Inner.StickyBody);
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
            Debug.Log("[Flow] sticky note clicked — shift started, first visitor arriving");
            ApplyTickResult(Queue.ForceNextArrival(), false);
            RefreshHud();
        }
    }
}
