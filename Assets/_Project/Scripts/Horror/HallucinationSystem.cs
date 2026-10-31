using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.UI;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Horror
{
    // False entities at low sanity. Created by SchoolSceneBootstrap.
    public class HallucinationSystem : MonoBehaviour
    {
        private GameRoot _gameRoot;
        private GameRng Rng = new GameRng(0x51A5);
        private float _exploreAcc;
        private float _checkpointAcc;
        private bool _flickerRestoreLit;
        private static readonly string[] Whispers =
        {
            "(whispering) don't let them in…",
            "(whispering) check the photo again… again… again…",
            "(whispering) we wore their faces before you came.",
            "(a child counting backwards, somewhere above the ceiling)",
            "(whispering) the previous custodian is still on the roster.",
            "(static shapes a name you almost recognize)",
        };

        public void Init(GameRoot root) { _gameRoot = root; }

        private void Update()
        {
            if (_gameRoot == null || _gameRoot.Session == null || _gameRoot.Paused) return;
            if (_gameRoot.State != AppViewState.Exploration && _gameRoot.State != AppViewState.Checkpoint) return;
            if (_gameRoot.HuntActive) return;   // phantoms would hide the real hunt
            var stage = SanityModel.StageFor(_gameRoot.Session.Sanity);
            if (stage == SanityStage.Stable) return;

            if (_gameRoot.State == AppViewState.Exploration)
            {
                _exploreAcc += Time.deltaTime;
                if (_exploreAcc >= 1f)
                {
                    _exploreAcc = 0f;
                    if (Rng.NextFloat() < SanityModel.HallucinationChancePerSecond(stage))
                        ExploreHallucination();
                }
            }
            else // checkpoint: audio/caption-only, documents never touched
            {
                _checkpointAcc += Time.deltaTime;
                if (_checkpointAcc >= 1f)
                {
                    _checkpointAcc = 0f;
                    float chance = SanityModel.HallucinationChancePerSecond(stage) * 0.4f;
                    if (Rng.NextFloat() < chance)
                    {
                        _gameRoot.Bus.Publish(GameEvent.Of(EventIds.HallucinationTriggered, "whisper"));
                        _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, Whispers[Rng.NextInt(0, Whispers.Length)], 3.2f, null));
                        _gameRoot.Audio.PlaySfx("sfx_static", 0.18f);
                    }
                }
            }
        }

        private void ExploreHallucination()
        {
            if (_gameRoot.Views == null || _gameRoot.Views.Current == null) return;
            int roll = Rng.NextInt(0, 4);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.HallucinationTriggered, "explore_" + roll));
            switch (roll)
            {
                case 0: // phantom figure - harmless, fades on its own
                    _gameRoot.Views.SpawnAnomaly(true);
                    break;
                case 1:
                    _gameRoot.Audio.PlaySfx("sfx_knock", 0.6f);
                    _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*three slow knocks from inside the wall*", 2.8f, null));
                    break;
                case 2:
                    _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, Whispers[Rng.NextInt(0, Whispers.Length)], 3.2f, null));
                    _gameRoot.Audio.PlaySfx("sfx_static", 0.25f);
                    break;
                default: // phantom flicker (never a strobe; photosensitivity-safe by construction)
                {
                    var light = _gameRoot.Views.Current.Light;
                    if (light != null && light.Lit)
                    {
                        _gameRoot.Audio.PlaySfx("sfx_flicker", 0.4f);
                        _flickerRestoreLit = true;       // restore what the PLAYER had, not a forced on
                        light.SetLit(false);
                        CancelInvoke(nameof(RestoreLights));
                        Invoke(nameof(RestoreLights), 0.5f);
                    }
                    break;
                }
            }
        }

        private void RestoreLights()
        {
            bool on = _flickerRestoreLit;      // the pre-flicker switch state, not a forced on
            _flickerRestoreLit = false;
            if (_gameRoot != null && _gameRoot.Views != null && _gameRoot.Views.Current != null && _gameRoot.Views.Current.Light != null)
                _gameRoot.Views.Current.Light.SetLit(on);
        }
    }
}
