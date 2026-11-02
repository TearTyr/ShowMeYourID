using System.Collections.Generic;
using Hollowbrook.Scenes;
using Hollowbrook.Core;
using Hollowbrook.UI;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Horror
{
    // False entities at low sanity, plus a steady trickle of small wrong noises and lamp stutters that never
    // stop, so the building never feels safe. Created by PatrolScene.
    public class HallucinationSystem : MonoBehaviour
    {
        private GameRoot _gameRoot;
        private GameRng Rng = new GameRng(0x51A5);
        private float _exploreAcc;
        private float _checkpointAcc;
        private float _dreadLeft = FirstDreadSeconds;
        private const float FirstDreadSeconds = 10f;     // the first wrong noise comes early in a patrol
        private const float DreadMinSeconds = 12f;       // then one every 12 to 28 seconds, sooner at low sanity
        private const float DreadSpreadSeconds = 16f;
        private const float LampStutterSeconds = 0.3f;   // the peek lamp dies for this long
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
            bool onPatrol = _gameRoot.State == AppViewState.Exploration || _gameRoot.State == AppViewState.RoomPeek;
            if (!onPatrol && _gameRoot.State != AppViewState.Checkpoint) return;
            if (_gameRoot.HuntActive) return;   // phantoms would hide the real hunt
            TickDread(Time.deltaTime);
            var stage = SanityModel.StageFor(_gameRoot.Session.Sanity);
            if (stage == SanityStage.Stable) return;

            if (_gameRoot.State == AppViewState.RoomPeek) return;   // only the dread above reaches into a peek
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
                        _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, Whispers[Rng.NextInt(0, Whispers.Length)], 3.2f, null));
                        _gameRoot.Audio.PlaySfx("sfx_static", 0.18f);
                    }
                }
            }
        }

        // On patrol, whatever the sanity: now and then something small is wrong. No caption (the bar stays
        // quiet), just a sound from nowhere or, in a peek, the lamp dying for a heartbeat.
        private void TickDread(float dt)
        {
            bool onPatrol = _gameRoot.State == AppViewState.Exploration || _gameRoot.State == AppViewState.RoomPeek;
            if (!onPatrol || _gameRoot.TutorialActive || _gameRoot.BlockingOverlayOpen) return;
            _dreadLeft -= dt;
            if (_dreadLeft > 0f) return;
            float nerve = 0.5f + _gameRoot.Session.Sanity / 200f;   // sanity 0..100 makes the wait 50 to 100 percent
            _dreadLeft = (DreadMinSeconds + Rng.NextFloat() * DreadSpreadSeconds) * nerve;
            if (_gameRoot.State == AppViewState.RoomPeek) { PeekDread(); return; }
            string[] sounds = { "sfx_knock", "sfx_static", "sfx_door" };
            _gameRoot.Audio.PlaySfx(sounds[Rng.NextInt(0, sounds.Length)], 0.3f);
        }

        // In a peek: a sound from the room, or the lamp stuttering out for a moment (never for a calm setting).
        private void PeekDread()
        {
            var lamp = _gameRoot.Views != null ? _gameRoot.Views.PeekLight : null;
            bool calm = _gameRoot.Settings != null && (_gameRoot.Settings.PhotosensitivitySafe || _gameRoot.Settings.ReduceMotion);
            if (lamp != null && lamp.Lit && !lamp.Locked && !calm && Rng.Chance(0.6f))
            {
                lamp.Flicker(LampStutterSeconds);
                return;
            }
            _gameRoot.Audio.PlaySfx(Rng.Chance(0.5f) ? "sfx_knock" : "sfx_static", 0.3f);
        }

        private void ExploreHallucination()
        {
            if (_gameRoot.Views == null || _gameRoot.Views.Current == null) return;
            int roll = Rng.NextInt(0, 3);
            switch (roll)
            {
                case 0: // phantom figure - harmless, fades on its own
                    _gameRoot.Views.SpawnAnomaly(true);
                    break;
                case 1:
                    _gameRoot.Audio.PlaySfx("sfx_knock", 0.6f);
                    _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*three slow knocks from inside the wall*", 2.8f, null));
                    break;
                default:
                    _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, Whispers[Rng.NextInt(0, Whispers.Length)], 3.2f, null));
                    _gameRoot.Audio.PlaySfx("sfx_static", 0.25f);
                    break;
            }
        }
    }
}
