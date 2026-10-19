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
    // Mode picker kept for later use. Created by MenuSceneBootstrap.
    public class ModeSelectView : MonoBehaviour, IScreenView
    {
        private GameRoot R;
        private Text _endlessBest, _challengeBest;

        public void Init(GameRoot root)
        {
            R = root;
            UIFactory.Dimmer(transform, "dim", 0.88f);
            var sheet = UIFactory.LeftSheet(transform, "SELECT ASSIGNMENT", "The contract, or the long night.", out var col);

            BuildCard(sheet, new Vector2(320, -180), "NORMAL — THE CONTRACT",
                "Five nights at Hollowbrook. Verify students, keep anomalies out, investigate the school.",
                () => R.StartNewGame(GameMode.Normal), out _);
            BuildCard(sheet, new Vector2(320, -330), "ENDLESS WATCH",
                "One long night at the window. Correct calls buy seconds; the clock is the only boss.",
                () => R.StartNewGame(GameMode.Endless), out _endlessBest);
            BuildCard(sheet, new Vector2(320, -480), "CHALLENGE PROTOCOL",
                "Shifting district rules, doubled sanity drain, doubled score.",
                () => R.StartNewGame(GameMode.Challenge), out _challengeBest);

            UIFactory.SheetButton(sheet, "back", "◂ BACK", () =>
            {
                R.Audio.PlaySfx("sfx_ui_close", 0.6f);
                R.SetState(AppViewState.MainMenu);
            }, 140);

            gameObject.SetActive(false);
        }

        private void BuildCard(RectTransform sheet, Vector2 pos, string title, string body,
            System.Action onSelect, out Text bestLabel)
        {
            Text best = null;
            var card = UIFactory.BorderedPanel(sheet, "card", new Color(0.06f, 0.06f, 0.07f, 0.97f), UIFactory.Border, 2);
            UIFactory.Anchor(card, new Vector2(0f, 1f), new Vector2(560, 130), pos);
            var t = UIFactory.Label(card, "title", title, 19, UIFactory.Ink, TextAnchor.MiddleLeft);
            UIFactory.Anchor(t.rectTransform, new Vector2(0f, 1f), new Vector2(520, 30), new Vector2(280, -26));
            var b = UIFactory.Label(card, "body", body, 14, UIFactory.InkDim, TextAnchor.UpperLeft);
            UIFactory.Anchor(b.rectTransform, new Vector2(0f, 1f), new Vector2(380, 70), new Vector2(210, -60));
            best = UIFactory.Label(card, "best", "", 14, new Color(0.75f, 0.62f, 0.3f), TextAnchor.MiddleRight);
            UIFactory.Anchor(best.rectTransform, new Vector2(1f, 0f), new Vector2(180, 24), new Vector2(-110, 40));
            var sel = UIFactory.StandardButton(card, "select", "BEGIN", () =>
            {
                R.Audio.PlaySfx("sfx_bell", 0.7f);
                onSelect();
            }, 140, 40);
            UIFactory.Anchor(sel.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(140, 40), new Vector2(-90, 0));
            bestLabel = best;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            var rec = R.Records;
            if (_endlessBest != null) _endlessBest.text = rec != null && rec.EndlessBest > 0 ? "BEST " + rec.EndlessBest : "";
            if (_challengeBest != null) _challengeBest.text = rec != null && rec.ChallengeBest > 0 ? "BEST " + rec.ChallengeBest : "";
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
