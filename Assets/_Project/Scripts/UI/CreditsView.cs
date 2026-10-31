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
    // Credits sheet. The text comes from CreditsData. Created by MenuSceneBootstrap.
    public class CreditsView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            var sheet = UIFactory.LeftSheet(transform, "CREDITS", "A " + CreditsData.Studio + " game.", out _);

            // the names and roles live in GameLogic/CreditsData.cs
            string body = CreditsData.Build();
            var txt = UIFactory.Label(sheet, "body", body, 16, UIFactory.InkDim, TextAnchor.UpperLeft);
            UIFactory.Anchor(txt.rectTransform, new Vector2(0f, 1f), new Vector2(560, 430), new Vector2(320, -360));

            UIFactory.SheetButton(sheet, "back", "◂ BACK", () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.6f);
                _gameRoot.CloseCredits();
            }, 140);

            gameObject.SetActive(false);
        }

        public void Show() { gameObject.SetActive(true); }
        public void Hide() { gameObject.SetActive(false); }
    }
}
