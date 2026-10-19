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
    // Static credits sheet. Created by MenuSceneBootstrap.
    public class CreditsView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            var sheet = UIFactory.LeftSheet(transform, "CREDITS", "A Twin Studio game.", out _);

            string body =
                "Project Director · Narrative — Jessie Mariel De Mesa\n" +
                "Project Assistants — Elijah Japeth Ramos · Clarence Manalo\n" +
                "Programming — Elijah Japeth Ramos · John Perilla\n" +
                "Art — Daniel Steven Solitaria · Jessie Mariel De Mesa\n" +
                "Audio — Clarence Manalo\n" +
                "Marketing & Branding — Diosdado Fabelico II · Daniella Garing\n" +
                "Storyboard — All members\n\n" +
                "This build generates all art and audio procedurally at runtime\n" +
                "(monochrome silhouettes, documents, and an E♭-minor score).\n" +
                "Drop real assets into Resources/Art and Resources/Audio to replace any of them.";
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
