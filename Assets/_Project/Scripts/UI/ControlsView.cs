using System;
using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    // Static key-reference sheet. Created by each scene bootstrap.
    public class ControlsView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            var sheet = UIFactory.LeftSheet(transform, "CONTROLS", "Mouse at the desk. Keys in the dark.", out _);

            string body =
                "CHECKPOINT (mouse)\n" +
                "   Drag the ID + slip from the counter; move / flip (F) / zoom (wheel, Z).\n" +
                "   Registry terminal: search by ID or name — NO RECORD is a clue.\n" +
                "   Stamp the slip (drag, or A / D), then hand back BOTH papers (H).\n" +
                "   At most 2 questions per visitor. Decisions are permanent.\n\n" +
                "EXPLORATION\n" +
                "   A / D  or  ← / →     walk left / right\n" +
                "   SHIFT                run\n" +
                "   E                    interact (doors, clues, switches)\n" +
                "   F                    sweep beam (toggle — wide, finds & restrains)\n" +
                "   Q                    focus beam (toggle — tight, exposes & repels)\n" +
                "   L                    beam off    ·    HOLD LMB / RMB: temp sweep / focus\n" +
                "   M                    map          ESC   pause\n\n" +
                "SECURITY ROOM (School scene prototype, mouse)\n" +
                "   Click the door panel to admit or deny late night students.\n" +
                "   Click the marked spots to check the rooms for anomalies.\n\n" +
                "DIALOGUE\n" +
                "   CLICK / SPACE / ENTER / E     advance\n\n" +
                "SYSTEM\n" +
                "   F2 ×3 on the main menu, or OPTIONS → RESET DATA\n" +
                "      erases save data (settings are kept)";
            var txt = UIFactory.Label(sheet, "body", body, 16, UIFactory.InkDim, TextAnchor.UpperLeft);
            UIFactory.Anchor(txt.rectTransform, new Vector2(0f, 1f), new Vector2(560, 430), new Vector2(320, -360));

            UIFactory.SheetButton(sheet, "back", "◂ BACK", () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.6f);
                _gameRoot.CloseControls();
            }, 140);

            gameObject.SetActive(false);
        }

        public void Show() { gameObject.SetActive(true); }
        public void Hide() { gameObject.SetActive(false); }
    }
}
