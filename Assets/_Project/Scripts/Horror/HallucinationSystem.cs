using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.UI;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Horror
{
    /// <summary>Hallucinations driven by the sanity stage (approved: sanity affects perception).
    /// Exploration: phantom figures, knocks, whispers, phantom flickers. Checkpoint: whispers/
    /// captions ONLY — hallucinations never alter documents or verdicts, keeping inspections
    /// fair (essential information must stay trustworthy and readable).</summary>
    public class HallucinationSystem : MonoBehaviour
    {
        private GameRoot R;
        private float _exploreAcc;
        private float _checkpointAcc;
        private static readonly string[] Whispers =
        {
            "(whispering) don't let them in…",
            "(whispering) check the photo again… again… again…",
            "(whispering) we wore their faces before you came.",
            "(a child counting backwards, somewhere above the ceiling)",
            "(whispering) the previous custodian is still on the roster.",
            "(static shapes a name you almost recognize)",
        };

        public void Init(GameRoot root) { R = root; }

        private void Update()
        {
            if (R == null || R.Session == null || R.Paused) return;
            if (R.State != AppViewState.Exploration && R.State != AppViewState.Checkpoint) return;
            var stage = SanityModel.StageFor(R.Session.Sanity);
            if (stage == SanityStage.Stable) return;

            if (R.State == AppViewState.Exploration)
            {
                _exploreAcc += Time.deltaTime;
                if (_exploreAcc >= 1f)
                {
                    _exploreAcc = 0f;
                    if (Random.value < SanityModel.HallucinationChancePerSecond(stage))
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
                    if (Random.value < chance)
                    {
                        R.Bus.Publish(GameEvent.Of(EventIds.HallucinationTriggered, "whisper"));
                        R.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, Whispers[Random.Range(0, Whispers.Length)], 3.2f, null));
                        R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_static", 0.18f, null));
                    }
                }
            }
        }

        private void ExploreHallucination()
        {
            if (R.Views == null || R.Views.Current == null) return;
            int roll = Random.Range(0, 4);
            R.Bus.Publish(GameEvent.Of(EventIds.HallucinationTriggered, "explore_" + roll));
            switch (roll)
            {
                case 0: // phantom figure — harmless, fades on its own
                    R.Views.SpawnAnomaly(true);
                    break;
                case 1:
                    R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_knock", 0.6f, null));
                    R.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*three slow knocks from inside the wall*", 2.8f, null));
                    break;
                case 2:
                    R.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, Whispers[Random.Range(0, Whispers.Length)], 3.2f, null));
                    R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_static", 0.25f, null));
                    break;
                default: // phantom flicker (never a strobe; photosensitivity-safe by construction)
                {
                    var light = R.Views.Current.Light;
                    if (light != null && light.Mode == LightMode.Sweep)
                    {
                        R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_flicker", 0.4f, null));
                        light.SetMode(LightMode.Off);
                        CancelInvoke(nameof(RestoreLights));
                        Invoke(nameof(RestoreLights), 0.5f);
                    }
                    break;
                }
            }
        }

        private void RestoreLights()
        {
            if (R != null && R.Views != null && R.Views.Current != null && R.Views.Current.Light != null)
                R.Views.Current.Light.SetMode(LightMode.Sweep);
        }
    }
}
