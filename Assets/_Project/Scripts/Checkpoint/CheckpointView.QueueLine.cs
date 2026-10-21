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
    // The waiting-line display (built only when ShowcaseBuild.LineDisplay is on).
    // One area of the inspection desk; see CheckpointView.cs for the state machine,
    // the shared fields and the whole-class comment. Same class, split by desk area.
    public partial class CheckpointView
    {
        // The waiting line: one identical silhouette per arrived-but-not-inspected visitor,
        // split across the two yard gaps either side of the booth (left strip first).
        // Same figure for legit students and anomalies — the line reveals nothing.
        // The right strip heads the yard with the waiting-count badge.
        private void BuildQueueStrips()
        {
            var stripL = UIFactory.Panel(_desk, "queuestripL", new Color(1f, 1f, 1f, 0.02f));
            Reg("queuestripL", stripL.rectTransform, L.QueueStripL);
            stripL.raycastTarget = false;
            var stripR = UIFactory.Panel(_desk, "queuestripR", new Color(1f, 1f, 1f, 0.02f));
            Reg("queuestripR", stripR.rectTransform, L.QueueStripR);
            stripR.raycastTarget = false;

            _queueBadge = UIFactory.Label(stripR.rectTransform, "qbadge", "LINE 0", 13,
                UIFactory.InkDim, TextAnchor.MiddleCenter);
            L.Place(_queueBadge.rectTransform, L.Inner.QueueBadge);
            _queueBadge.raycastTarget = false;

            var tint = new Color(0.72f, 0.74f, 0.78f, 0.85f);   // placeholder figures, readable on the dark yard
            for (int i = 0; i < 8; i++)
            {
                bool left = i < 4;
                var strip = left ? stripL.rectTransform : stripR.rectTransform;
                float x = left ? L.Inner.QueueSlotLX : L.Inner.QueueSlotRX;
                float y = (left ? L.Inner.QueueSlotLY0 : L.Inner.QueueSlotRY0) + (i % 4) * L.Inner.QueueSlotStep;
                var fig = UIFactory.Img(strip, "qsil" + i, ProceduralArt.Get("custodian_0"), tint);
                L.Place(fig.rectTransform, new L.Region(x, y, L.Inner.QueueSlotW, L.Inner.QueueSlotH));
                fig.preserveAspect = true;
                fig.raycastTarget = false;
                fig.gameObject.SetActive(false);
                _silhouettes[i] = fig;
            }
            _queueOverflow = UIFactory.Label(stripR.rectTransform, "qoverflow", "", 14,
                UIFactory.InkDim, TextAnchor.MiddleCenter);
            L.Place(_queueOverflow.rectTransform, new L.Region(L.Inner.QueueSlotRX,
                L.Inner.QueueSlotRY0 + 3 * L.Inner.QueueSlotStep, L.Inner.QueueSlotW, L.Inner.QueueSlotH));
            _queueOverflow.gameObject.SetActive(false);
        }

        // QUEUE: waiting | CURRENT: arrived/effective — the waiting count excludes the
        // person at the window; the counter never counts vanished students as inspected.
        private void RefreshQueueStatus()
        {
            if (_queueStatus == null) return;
            if (Queue == null || !Queue.IsAttached)
            {
                _queueStatus.text = "";
                if (_queueBadge != null) _queueBadge.text = "LINE 0";
                return;
            }
            bool collapsed = Queue.Collapsed;
            bool pressured = Queue.PressureLevel >= 1;

            string text;
            if (collapsed)
            {
                text = "QUEUE: " + Queue.WaitingCount + "  —  OVERRUN";
            }
            else
            {
                text = "QUEUE: " + Queue.WaitingCount;
                if (Queue.EffectiveTotal >= 0)
                    text += "  |  CURRENT: " + Queue.ArrivalCount + "/" + Queue.EffectiveTotal;
            }

            Color statusColor;
            Color badgeColor;
            if (collapsed) { statusColor = UIFactory.AccentBright; badgeColor = UIFactory.AccentBright; }
            else if (pressured) { statusColor = PressureColor; badgeColor = PressureColor; }
            else { statusColor = UIFactory.PaperInk; badgeColor = UIFactory.InkDim; }

            _queueStatus.text = text;
            _queueStatus.color = statusColor;
            if (_queueBadge != null)
            {
                _queueBadge.text = collapsed ? "LINE " + Queue.WaitingCount + " !" : "LINE " + Queue.WaitingCount;
                _queueBadge.color = badgeColor;
            }
        }

        // One identical silhouette per waiting visitor (left strip first), overflow as "+n".
        private void RefreshQueueLine()
        {
            if (_queueBadge == null) return;   // line display not built (ShowcaseBuild)
            int n = Queue != null ? Queue.WaitingCount : 0;
            bool overflow = n > _silhouettes.Length;
            int shown = overflow ? _silhouettes.Length - 1 : Mathf.Min(n, _silhouettes.Length);
            for (int i = 0; i < _silhouettes.Length; i++)
                if (_silhouettes[i] != null) _silhouettes[i].gameObject.SetActive(i < shown);
            if (_queueOverflow != null)
            {
                _queueOverflow.gameObject.SetActive(overflow);
                if (overflow) _queueOverflow.text = "+" + (n - shown);
            }
            RefreshQueueStatus();
        }
    }
}
