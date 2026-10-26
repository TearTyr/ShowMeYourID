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
                "   Drag the ID + slip from the counter and arrange them — papers KEEP their size\n" +
                "   while you scroll; Z zooms the paper you touched last (deliberate, toggle).\n" +
                "   Flip: click the ID's BLACK SPINE (the card body only drags); F flips the paper you\n" +
                "   touched last — the slip flips on its own and never rides along with the ID.\n" +
                "   Floor files: open a floor folder, pull the student's file — mismatches are clues.\n" +
                "   Face check: click the ID PHOTO, then the face at the window — a quiet cue\n" +
                "   answers (green frame = same face, amber = not). It informs; the verdict is yours.\n" +
                "   Answers arrive in the speech bubble beside the head (2 s after the line ends;\n" +
                "   hover the bubble to keep a line on screen while you read it).\n" +
                "   Stamp the slip (drag, or A / D) — the mark lands WHERE you drop the stamp, always\n" +
                "   fully on the paper — then hand back BOTH papers (H).\n" +
                "   At most 2 questions per visitor. Decisions are permanent.\n\n" +
                "EXPLORATION\n" +
                "   Rooms are entered DARK and your beam starts OFF — every door you go through\n" +
                "   puts it out again. The wall switch (E) lights the room itself. Classroom peeks\n" +
                "   start dark too: the peek lamp is OFF until you press F (mouse never lights it).\n" +
                "   A / D  or  ← / →     walk left / right\n" +
                "   SHIFT                run\n" +
                "   E                    interact (doors, clues, light switches)\n" +
                "   F                    flashlight toggle — sweep beam on patrol, the lamp on peeks\n" +
                "   Q                    focus beam (toggle — tight, exposes & repels; on peeks it\n" +
                "                        tightens the lamp without lighting it)\n" +
                "   L                    beam off\n" +
                "   M                    map          ESC   pause\n" +
                "   The HUD reads both lights: LIGHTS (room) and BEAM / LAMP (handheld).\n\n" +
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
