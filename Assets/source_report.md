# Source Directory Tree and Selected File Contents

- Scanned: `D:/VS/ShowMeYourID/Assets`
- Tree mode: `all`
- Files in tree: `148`
- Directories in tree: `12`
- Source files to dump: `60`
- Source extensions: `.cs`, `.hlsl`

## Directory Tree

```text
Assets/
├── _Project/
│   ├── Resources/
│   │   ├── GameConfig.asset
│   │   └── GameConfig.asset.meta
│   ├── Scenes/
│   │   ├── Hollowbrook.unity
│   │   ├── Hollowbrook.unity.meta
│   │   ├── MainMenu.unity
│   │   ├── MainMenu.unity.meta
│   │   ├── School.unity
│   │   └── School.unity.meta
│   ├── Scripts/
│   │   ├── Audio/
│   │   │   ├── AudioDirector.cs
│   │   │   └── AudioDirector.cs.meta
│   │   ├── Bootstrap/
│   │   │   ├── CheckpointSceneBootstrap.cs
│   │   │   ├── CheckpointSceneBootstrap.cs.meta
│   │   │   ├── CoreBootstrap.cs
│   │   │   ├── CoreBootstrap.cs.meta
│   │   │   ├── MenuSceneBootstrap.cs
│   │   │   ├── MenuSceneBootstrap.cs.meta
│   │   │   ├── ProceduralArt.cs
│   │   │   ├── ProceduralArt.cs.meta
│   │   │   ├── ProceduralAudio.cs
│   │   │   ├── ProceduralAudio.cs.meta
│   │   │   ├── SchoolSceneBootstrap.cs
│   │   │   └── SchoolSceneBootstrap.cs.meta
│   │   ├── Checkpoint/
│   │   │   ├── CheckpointLayout.cs
│   │   │   ├── CheckpointLayout.cs.meta
│   │   │   ├── CheckpointView.cs
│   │   │   └── CheckpointView.cs.meta
│   │   ├── Core/
│   │   │   ├── AspectMatchGuard.cs
│   │   │   ├── AspectMatchGuard.cs.meta
│   │   │   ├── EventBus.cs
│   │   │   ├── EventBus.cs.meta
│   │   │   ├── EventIds.cs
│   │   │   ├── EventIds.cs.meta
│   │   │   ├── GameConfigSO.cs
│   │   │   ├── GameConfigSO.cs.meta
│   │   │   ├── GameRoot.cs
│   │   │   ├── GameRoot.cs.meta
│   │   │   ├── IScreenView.cs
│   │   │   ├── IScreenView.cs.meta
│   │   │   ├── SaveSystem.cs
│   │   │   ├── SaveSystem.cs.meta
│   │   │   ├── SettingsSystem.cs
│   │   │   └── SettingsSystem.cs.meta
│   │   ├── GameLogic/
│   │   │   ├── AnomalyEncounter.cs
│   │   │   ├── AnomalyEncounter.cs.meta
│   │   │   ├── ClueRegistry.cs
│   │   │   ├── ClueRegistry.cs.meta
│   │   │   ├── ContentData.cs
│   │   │   ├── ContentData.cs.meta
│   │   │   ├── DisplayPolicy.cs
│   │   │   ├── DisplayPolicy.cs.meta
│   │   │   ├── EndingRules.cs
│   │   │   ├── EndingRules.cs.meta
│   │   │   ├── Enums.cs
│   │   │   ├── Enums.cs.meta
│   │   │   ├── GameRng.cs
│   │   │   ├── GameRng.cs.meta
│   │   │   ├── GameSession.cs
│   │   │   ├── GameSession.cs.meta
│   │   │   ├── IdRules.cs
│   │   │   ├── IdRules.cs.meta
│   │   │   ├── InspectionRules.cs
│   │   │   ├── InspectionRules.cs.meta
│   │   │   ├── PatrolPool.cs
│   │   │   ├── PatrolPool.cs.meta
│   │   │   ├── PatrolRules.cs
│   │   │   ├── PatrolRules.cs.meta
│   │   │   ├── PresentationLog.cs
│   │   │   ├── PresentationLog.cs.meta
│   │   │   ├── RosterFactory.cs
│   │   │   ├── RosterFactory.cs.meta
│   │   │   ├── SaveFormat.cs
│   │   │   ├── SaveFormat.cs.meta
│   │   │   ├── ShiftPlan.cs
│   │   │   ├── ShiftPlan.cs.meta
│   │   │   ├── TrackingPolicy.cs
│   │   │   └── TrackingPolicy.cs.meta
│   │   ├── Horror/
│   │   │   ├── HallucinationSystem.cs
│   │   │   ├── HallucinationSystem.cs.meta
│   │   │   ├── SanityController.cs
│   │   │   ├── SanityController.cs.meta
│   │   │   ├── ScareSystem.cs
│   │   │   ├── ScareSystem.cs.meta
│   │   │   ├── WanderingAnomaly.cs
│   │   │   └── WanderingAnomaly.cs.meta
│   │   ├── UI/
│   │   │   ├── CaptionBar.cs
│   │   │   ├── CaptionBar.cs.meta
│   │   │   ├── ConfirmationPopup.cs
│   │   │   ├── ConfirmationPopup.cs.meta
│   │   │   ├── ControlsView.cs
│   │   │   ├── ControlsView.cs.meta
│   │   │   ├── CreditsView.cs
│   │   │   ├── CreditsView.cs.meta
│   │   │   ├── DialogueView.cs
│   │   │   ├── DialogueView.cs.meta
│   │   │   ├── HUDView.cs
│   │   │   ├── HUDView.cs.meta
│   │   │   ├── LoadingView.cs
│   │   │   ├── LoadingView.cs.meta
│   │   │   ├── MainMenuView.cs
│   │   │   ├── MainMenuView.cs.meta
│   │   │   ├── ModeSelectView.cs
│   │   │   ├── ModeSelectView.cs.meta
│   │   │   ├── PauseView.cs
│   │   │   ├── PauseView.cs.meta
│   │   │   ├── ProgressionScreens.cs
│   │   │   ├── ProgressionScreens.cs.meta
│   │   │   ├── SettingsView.cs
│   │   │   ├── SettingsView.cs.meta
│   │   │   ├── UIFactory.cs
│   │   │   └── UIFactory.cs.meta
│   │   ├── World/
│   │   │   ├── FlashlightRig.cs
│   │   │   ├── FlashlightRig.cs.meta
│   │   │   ├── Interactables.cs
│   │   │   ├── Interactables.cs.meta
│   │   │   ├── LightController.cs
│   │   │   ├── LightController.cs.meta
│   │   │   ├── PlayerController.cs
│   │   │   ├── PlayerController.cs.meta
│   │   │   ├── RoomBuilder.cs
│   │   │   ├── RoomBuilder.cs.meta
│   │   │   ├── RoomInstance.cs
│   │   │   ├── RoomInstance.cs.meta
│   │   │   ├── RoomSpawner.cs
│   │   │   ├── RoomSpawner.cs.meta
│   │   │   ├── SecurityRoomView.cs
│   │   │   ├── SecurityRoomView.cs.meta
│   │   │   ├── ViewManager.cs
│   │   │   └── ViewManager.cs.meta
│   │   ├── Audio.meta
│   │   ├── Bootstrap.meta
│   │   ├── Checkpoint.meta
│   │   ├── Core.meta
│   │   ├── Data.meta
│   │   ├── Exploration.meta
│   │   ├── GameLogic.meta
│   │   ├── Horror.meta
│   │   ├── Progression.meta
│   │   ├── UI.meta
│   │   └── World.meta
│   ├── ARCHITECTURE.md
│   ├── REFACTOR_LOG.md
│   ├── Resources.meta
│   ├── Scenes.meta
│   ├── Scripts.meta
│   └── STYLE.md
├── _Project.meta
├── cs.py
└── source_report.md
```

## File Contents

### `_Project/Scripts/Audio/AudioDirector.cs` (6.1 KiB)

````csharp
using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Core;
using Hollowbrook.Bootstrap;
using UnityEngine;

namespace Hollowbrook.Audio
{
    // Plays every sound and music bed; also emits captions. Created by CoreBootstrap.
    public class AudioDirector : MonoBehaviour
    {
        private EventBus _bus;
        private SettingsSystem _settings;
        private AudioSource _musicA, _musicB;
        private AudioSource[] _sfxPool;
        private int _poolIndex;
        private string _musicKey = "";
        private Coroutine _fadeRoutine;
        private readonly List<EventSubscription> _subs = new List<EventSubscription>();
        private readonly HashSet<string> _missingWarned = new HashSet<string>();

        private static readonly Dictionary<string, string> Captions = new Dictionary<string, string>
        {
            { "sfx_knock", "*KNOCK  KNOCK  KNOCK*" },
            { "sfx_stinger", "*a sharp, dissonant sting*" },
            { "sfx_scare", "*SCREAM — something lunges*" },
            { "sfx_glitch", "*static crackles and snaps*" },
            { "sfx_flicker", "*the fluorescent light buzzes and stutters*" },
            { "sfx_heartbeat", "*your own heartbeat, too loud*" },
            { "sfx_intercom", "*intercom static*" },
            { "sfx_alarm", "*alarm klaxon*" },
            { "sfx_bell", "*shift bell*" },
            { "sfx_stamp_reject", "*REJECTED — the stamp comes down*" },
            { "sfx_stamp_allow", "*ALLOWED — the stamp comes down*" },
            { "sfx_scan_error", "*scanner: error tone*" },
            { "sfx_scan_ok", "*scanner: chirp*" },
            { "sfx_door", "*a door opens somewhere*" },
            { "music_anomaly", "*the air hums with something wrong*" },
        };

        public void Init(EventBus bus, SettingsSystem settings)
        {
            _bus = bus;
            _settings = settings;

            _musicA = gameObject.AddComponent<AudioSource>();
            _musicB = gameObject.AddComponent<AudioSource>();
            _musicA.loop = true; _musicB.loop = true;
            _musicA.playOnAwake = false; _musicB.playOnAwake = false;

            _sfxPool = new AudioSource[8];
            for (int i = 0; i < _sfxPool.Length; i++)
            {
                _sfxPool[i] = gameObject.AddComponent<AudioSource>();
                _sfxPool[i].playOnAwake = false;
                _sfxPool[i].loop = false;
            }

            _subs.Add(bus.Subscribe(EventIds.SettingsChanged, OnSettings));
            ApplyVolumes();
        }

        private void OnDestroy()
        {
            foreach (var s in _subs) s?.Dispose();
            _subs.Clear();
        }

        public void PlaySfx(string key, float volume = 1f)
        {
            var clip = ProceduralAudio.Get(key);
            if (clip == null)
            {
                if (_missingWarned.Add(key)) Debug.LogWarning("[AudioDirector] missing sfx '" + key + "' — skipped gracefully.");
                return;
            }
            if (_sfxPool == null || _sfxPool.Length == 0) return;
            var src = _sfxPool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % _sfxPool.Length;
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume * (_settings != null ? _settings.SfxVolume : 0.8f));
            src.pitch = 1f + (clip.length > 0.4f ? 0f : (Random.value - 0.5f) * 0.06f);
            src.Play();

            if (_settings != null && _settings.Captions && Captions.TryGetValue(key, out var caption))
                _bus?.Publish(GameEvent.Of(EventIds.CaptionRequested, caption, 2.6f, null));
        }


        public void PlayMusic(string contextKey)
        {
            string clipKey = string.IsNullOrEmpty(contextKey) ? "" : "music_" + contextKey;
            if (clipKey == _musicKey) return;
            _musicKey = clipKey;
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(Crossfade(clipKey, 1.2f));
        }

        private IEnumerator Crossfade(string clipKey, float seconds)
        {
            var from = _musicA.isPlaying || _musicA.clip != null ? _musicA : _musicB;
            var to = from == _musicA ? _musicB : _musicA;

            AudioClip clip = null;
            if (!string.IsNullOrEmpty(clipKey))
            {
                clip = ProceduralAudio.Get(clipKey);
                if (clip == null)
                {
                    if (_missingWarned.Add(clipKey)) Debug.LogWarning("[AudioDirector] missing music '" + clipKey + "' — staying silent.");
                }
            }
            to.clip = clip;
            to.volume = 0f;
            to.loop = true;
            if (clip != null) to.Play();
            if (clipKey == "music_anomaly" && _settings != null && _settings.Captions)
                _bus?.Publish(GameEvent.Of(EventIds.CaptionRequested, Captions["music_anomaly"], 3.5f, null));

            float musicVol = _settings != null ? _settings.MusicVolume : 0.5f;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                to.volume = musicVol * k;
                from.volume = musicVol * (1f - k);
                yield return null;
            }
            to.volume = musicVol;
            from.Stop();
            from.clip = null;
            _fadeRoutine = null;
        }

        private void OnSettings(GameEvent e) => ApplyVolumes();

        private void ApplyVolumes()
        {
            if (_settings == null) return;
            float mv = _settings.MusicVolume;
            if (_musicA != null && _musicA.isPlaying) _musicA.volume = mv;
            if (_musicB != null && _musicB.isPlaying) _musicB.volume = mv;
        }

        public void StopEverything()
        {
            if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
            _musicKey = "";
            if (_musicA != null) { _musicA.Stop(); _musicA.clip = null; }
            if (_musicB != null) { _musicB.Stop(); _musicB.clip = null; }
            if (_sfxPool != null) foreach (var s in _sfxPool) if (s != null) s.Stop();
        }
    }
}
````

### `_Project/Scripts/Bootstrap/CheckpointSceneBootstrap.cs` (3.1 KiB)

````csharp
using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    // Builds the desk scene. Attached to Hollowbrook.unity.
    public class CheckpointSceneBootstrap : MonoBehaviour
    {
        public static void Build(GameRoot root)
        {
            var sceneRoot = CoreBootstrap.BeginSceneRoot("Hollowbrook");
            var fx = CoreBootstrap.NewCanvas(sceneRoot, "FXCanvas", 5, root.Config);
            var ui = CoreBootstrap.NewCanvas(sceneRoot, "UICanvas", 10, root.Config);
            var scare = CoreBootstrap.NewCanvas(sceneRoot, "ScareCanvas", 20, root.Config);

            // world-scoped references from other scenes must not leak in
            root.Views = null;
            root.SecurityRoom = null;

            root.Checkpoint = CoreBootstrap.CreateView<CheckpointView>(ui, "CheckpointView", root);
            root.HUD = CoreBootstrap.CreateView<HUDView>(ui, "HUDView", root);
            root.MapScreen = CoreBootstrap.CreateView<MapView>(ui, "MapView", root);
            root.ShiftResults = CoreBootstrap.CreateView<ShiftResultsView>(ui, "ShiftResultsView", root);
            root.Ending = CoreBootstrap.CreateView<EndingView>(ui, "EndingView", root);
            root.Loading = CoreBootstrap.CreateView<LoadingView>(ui, "LoadingView", root);
            root.Dialogue = CoreBootstrap.CreateView<DialogueView>(ui, "DialogueView", root);
            root.PauseScreen = CoreBootstrap.CreateView<PauseView>(ui, "PauseView", root);
            root.Captions = CoreBootstrap.CreateView<CaptionBar>(ui, "CaptionBar", root);
            root.Confirm = CoreBootstrap.CreateView<ConfirmationPopup>(ui, "ConfirmationPopup", root);
            root.SettingsScreen = CoreBootstrap.CreateView<SettingsView>(ui, "SettingsView", root);
            root.ControlsScreen = CoreBootstrap.CreateView<ControlsView>(ui, "ControlsView", root);
            root.CreditsScreen = CoreBootstrap.CreateView<CreditsView>(ui, "CreditsView", root);

            var sanityRt = UIFactory.NewUI(fx.transform, "SanityFx");
            root.SanityCtl = sanityRt.gameObject.AddComponent<Horror.SanityController>();
            root.SanityCtl.Init(root, fx);

            var scareRt = UIFactory.NewUI(scare.transform, "ScareFx");
            root.Scares = scareRt.gameObject.AddComponent<Horror.ScareSystem>();
            root.Scares.Init(root, scare);

            var hallGo = new GameObject("Hallucinations");
            hallGo.transform.SetParent(sceneRoot.transform, false);
            root.Hallucinations = hallGo.AddComponent<Horror.HallucinationSystem>();
            root.Hallucinations.Init(root);

            UIFactory.ApplyTextScale(root.Settings.TextScaleMultiplier);
            Debug.Log("[Flow] checkpoint bootstrap built (checkpoint=" + (root.Checkpoint != null) +
                      " loading=" + (root.Loading != null) + " dialogue=" + (root.Dialogue != null) + ")");
            root.RunQueuedSceneAction();
        }

        private void Awake() { CoreBootstrap.SafeBuild("Hollowbrook", () => Build(CoreBootstrap.EnsureCore())); }
    }

}
````

### `_Project/Scripts/Bootstrap/CoreBootstrap.cs` (5.2 KiB)

````csharp
using System.Collections.Generic;
using Hollowbrook.Audio;
using Hollowbrook.Core;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;   // CanvasScaler (used in NewCanvas) lives in UnityEngine.UI

namespace Hollowbrook.Bootstrap
{
    public static class SceneRouter
    {
        public const string Menu = "MainMenu";
        public const string Checkpoint = "Hollowbrook";   // Scene A — "That's Not My Neighbor" desk loop
        public const string School = "School";           // Scene B — side-scroller + security room

        public static bool IsRegistered(string sceneName) =>
            sceneName == Menu || sceneName == Checkpoint || sceneName == School;

        public static void Load(string sceneName)
        {
            if (!IsRegistered(sceneName))
                Debug.LogError("[SceneRouter] refusing unknown scene '" + sceneName + "'.");
            Debug.Log("[Flow] loading scene: " + sceneName);
            SceneManager.LoadScene(sceneName);
        }
    }

    // Builds the persistent core and per-scene canvas helpers.
    public static class CoreBootstrap
    {
        public static EventSystem CreatedEventSystem { get; private set; }
        public static GameObject CurrentSceneRoot { get; private set; }

        public static GameRoot EnsureCore()
        {
            if (GameRoot.Instance != null) return GameRoot.Instance;

            var bus = new EventBus();
            var config = GameConfigSO.LoadOrDefault();
            var settings = new SettingsSystem(bus);
            var saves = new SaveSystem(bus);

            // audio first (DDOL) so music/sfx survive every scene change
            var audioGo = new GameObject("AudioDirector");
            var audio = audioGo.AddComponent<AudioDirector>();
            audio.Init(bus, settings);
            Object.DontDestroyOnLoad(audioGo);

            var rootGo = new GameObject("GameRoot");
            var root = rootGo.AddComponent<GameRoot>();
            root.InitServices(bus, config, saves, settings, audio);
            Object.DontDestroyOnLoad(rootGo);

            // legacy input module event system (required for ALL uGUI clicks)
            var esGo = new GameObject("EventSystem");
            CreatedEventSystem = esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
            Object.DontDestroyOnLoad(esGo);

            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();
            Object.DontDestroyOnLoad(camGo);
            root.MainCamera = cam;

            return root;
        }

        public static GameObject BeginSceneRoot(string sceneName)
        {
            if (CurrentSceneRoot != null) Object.Destroy(CurrentSceneRoot);
            CurrentSceneRoot = new GameObject("SceneRoot_" + sceneName);
            return CurrentSceneRoot;
        }

        public static void SafeBuild(string sceneName, System.Action build)
        {
            try { build(); }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("[CoreBootstrap] scene '" + sceneName + "' failed to build: " + e.Message +
                               " - returning to the main menu.");
                if (GameRoot.Instance != null)
                {
                    GameRoot.Instance.ClearQueuedSceneAction();
                    GameRoot.Instance.Bus.Publish(GameEvent.Of(EventIds.Notification,
                        "Scene '" + sceneName + "' failed to start - back to the menu."));
                }
                SceneManager.LoadScene(SceneRouter.Menu);
            }
        }

        public static Canvas NewCanvas(GameObject sceneRoot, string name, int order, GameConfigSO cfg)
        {
            var canvas = UIFactory.CreateRootCanvas(name, order, cfg.canvasReferenceWidth, cfg.canvasReferenceHeight);
            canvas.transform.SetParent(sceneRoot.transform, false);
            var guard = canvas.gameObject.AddComponent<Core.AspectMatchGuard>();
            guard.Init(canvas.gameObject.GetComponent<CanvasScaler>());
            return canvas;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrappedScene()
        {
            if (GameRoot.Instance != null) return;
            Debug.LogWarning("[CoreBootstrap] no bootstrap in active scene — loading MainMenu.");
            SceneManager.LoadScene(SceneRouter.Menu);
        }

        public static T CreateView<T>(Canvas canvas, string name, GameRoot root) where T : MonoBehaviour
        {
            var rt = UIFactory.NewUI(canvas.transform, name);
            UIFactory.AnchorsFull(rt);
            var view = rt.gameObject.AddComponent<T>();
            (view as Core.IScreenView)?.Init(root);
            return view;
        }
    }
}
````

### `_Project/Scripts/Bootstrap/MenuSceneBootstrap.cs` (1.4 KiB)

````csharp
using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    // Builds the menu scene. Attached to MainMenu.unity.
    public class MenuSceneBootstrap : MonoBehaviour
    {
        public static void Build(GameRoot root)
        {
            var sceneRoot = CoreBootstrap.BeginSceneRoot("MainMenu");
            var ui = CoreBootstrap.NewCanvas(sceneRoot, "UICanvas", 10, root.Config);

            root.MainMenu = CoreBootstrap.CreateView<MainMenuView>(ui, "MainMenuView", root);
            root.ModeSelect = CoreBootstrap.CreateView<ModeSelectView>(ui, "ModeSelectView", root);
            root.SettingsScreen = CoreBootstrap.CreateView<SettingsView>(ui, "SettingsView", root);
            root.ControlsScreen = CoreBootstrap.CreateView<ControlsView>(ui, "ControlsView", root);
            root.CreditsScreen = CoreBootstrap.CreateView<CreditsView>(ui, "CreditsView", root);
            root.Captions = CoreBootstrap.CreateView<CaptionBar>(ui, "CaptionBar", root);
            root.Confirm = CoreBootstrap.CreateView<ConfirmationPopup>(ui, "ConfirmationPopup", root);

            UIFactory.ApplyTextScale(root.Settings.TextScaleMultiplier);
            root.BootToMainMenu();
        }

        private void Awake() { CoreBootstrap.SafeBuild("MainMenu", () => Build(CoreBootstrap.EnsureCore())); }
    }

}
````

### `_Project/Scripts/Bootstrap/ProceduralArt.cs` (48.9 KiB)

````csharp
using System;
using System.Collections.Generic;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    // Paints every sprite in code; real files in Resources/Art win if present.
    public static class ProceduralArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        public const float WorldPpu = 64f;   // world sprites: 64 px per unit
        public const float UiPpu = 100f;     // UI/document sprites

        public static Sprite Get(string key)
        {
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            try
            {
                var tex = Resources.Load<Texture2D>("Art/" + key);
                if (tex != null)
                {
                    var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), key.EndsWith("_bg") ? WorldPpu : UiPpu);
                    Cache[key] = spr;
                    return spr;
                }
            }
            catch (Exception) { /* no resources — fall through to procedural */ }

            var built = Build(key);
            if (built != null) Cache[built.name] = built;
            return built;
        }

        private class Canvas2D
        {
            public readonly int W, H;
            public readonly Color[] Px;
            public Canvas2D(int w, int h) { W = w; H = h; Px = new Color[w * h]; }
            public void Fill(Color c) { for (int i = 0; i < Px.Length; i++) Px[i] = c; }
            public void FillRect(int x, int y, int w, int h, Color c)
            {
                int x0 = Math.Max(0, x), y0 = Math.Max(0, y);
                int x1 = Math.Min(W - 1, x + w - 1), y1 = Math.Min(H - 1, y + h - 1);
                for (int yy = y0; yy <= y1; yy++)
                    for (int xx = x0; xx <= x1; xx++) Px[yy * W + xx] = c;
            }
            public void BlendRect(int x, int y, int w, int h, Color c)
            {
                int x0 = Math.Max(0, x), y0 = Math.Max(0, y);
                int x1 = Math.Min(W - 1, x + w - 1), y1 = Math.Min(H - 1, y + h - 1);
                for (int yy = y0; yy <= y1; yy++)
                    for (int xx = x0; xx <= x1; xx++) Px[yy * W + xx] = Blend(Px[yy * W + xx], c);
            }
            public void FillEllipse(float cx, float cy, float rx, float ry, Color c)
            {
                int x0 = Mathf.Max(0, (int)(cx - rx)), x1 = Mathf.Min(W - 1, (int)(cx + rx));
                int y0 = Mathf.Max(0, (int)(cy - ry)), y1 = Mathf.Min(H - 1, (int)(cy + ry));
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float dx = (x - cx) / rx, dy = (y - cy) / ry;
                        if (dx * dx + dy * dy <= 1f) Px[y * W + x] = c;
                    }
            }
            public void Line(int x0, int y0, int x1, int y1, Color c, int thick = 1)
            {
                int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
                int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
                int err = dx - dy;
                while (true)
                {
                    FillRect(x0 - thick / 2, y0 - thick / 2, thick, thick, c);
                    if (x0 == x1 && y0 == y1) break;
                    int e2 = 2 * err;
                    if (e2 > -dy) { err -= dy; x0 += sx; }
                    if (e2 < dx) { err += dx; y0 += sy; }
                }
            }
            public void GradientV(Color top, Color bottom)
            {
                for (int y = 0; y < H; y++)
                {
                    Color c = Color.Lerp(top, bottom, H > 1 ? y / (float)(H - 1) : 0f);
                    for (int x = 0; x < W; x++) Px[y * W + x] = c;
                }
            }
            public void Noise(float amount, int seed)
            {
                var r = new System.Random(seed);
                for (int i = 0; i < Px.Length; i++)
                {
                    float n = ((float)r.NextDouble() - 0.5f) * amount;
                    var p = Px[i];
                    Px[i] = new Color(Clamp01(p.r + n), Clamp01(p.g + n), Clamp01(p.b + n), p.a);
                }
            }
            public void ShiftRows(System.Random r, int slices, int maxShift)
            {
                for (int s = 0; s < slices; s++)
                {
                    int y = r.Next(H), hgt = 1 + r.Next(4), shift = r.Next(-maxShift, maxShift + 1);
                    for (int yy = y; yy < Math.Min(H, y + hgt); yy++)
                    {
                        var row = new Color[W];
                        Array.Copy(Px, yy * W, row, 0, W);
                        for (int x = 0; x < W; x++)
                        {
                            int src = (x - shift) % W; if (src < 0) src += W;
                            Px[yy * W + x] = row[src];
                        }
                    }
                }
            }
            public void Vignette(float strength)
            {
                float cx = W / 2f, cy = H / 2f, maxD = Mathf.Sqrt(cx * cx + cy * cy);
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / maxD;
                        Px[y * W + x] = Color.Lerp(Px[y * W + x], Color.black, Mathf.Clamp01((d - 0.45f) / 0.55f) * strength);
                    }
                }
            private static float Clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);
            public Sprite ToSprite(string name, float ppu)
            {
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.SetPixels(Px);
                tex.Apply(false, false);
                tex.name = name;
                var sp = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), ppu);
                sp.name = name;
                return sp;
            }
            public static Color Blend(Color dst, Color src)
            {
                float a = src.a + dst.a * (1 - src.a);
                if (a <= 0.0001f) return new Color(0, 0, 0, 0);
                return new Color((src.r * src.a + dst.r * dst.a * (1 - src.a)) / a,
                                 (src.g * src.a + dst.g * dst.a * (1 - src.a)) / a,
                                 (src.b * src.a + dst.b * dst.a * (1 - src.a)) / a, a);
            }
        }

        private static Color G(float v, float a = 1f) => new Color(v, v, v, a);

        private static Sprite Build(string key)
        {
            switch (key)
            {
                case "solid_white": return MakeSolid(G(1f));
                case "vignette": return MakeVignette(512, 384, 0.9f);
                case "noise_overlay": return MakeNoise(256, 256, 0.5f, 7);
                case "flash": return MakeFlash(256);
                case "light_cone": return MakeLightCone(512, 288);
                case "menu_bg": return MakeMenuBg(1280, 720);
                case "hallway_bg": return MakeHallway(1536, 640);
                case "classroom1_bg": return MakeClassroom(1152, 640, false);
                case "classroom2_bg": return MakeClassroom(1152, 640, true);
                case "security_bg": return MakeSecurityRoom(1280, 720);
                case "prop_door": return MakeDoor(160, 384, false);
                case "prop_door_security": return MakeDoor(160, 384, true);
                case "prop_server_door": return MakeDoor(208, 384, false, chained: true);
                case "prop_lockers": return MakeLockers(288, 336);
                case "prop_desk": return MakeDeskProp(208, 128);
                case "prop_chalkboard": return MakeChalkboardProp(384, 224);
                case "prop_window": return MakeWindowProp(224, 300);
                case "prop_noticeboard": return MakeNoticeBoard(208, 160);
                case "prop_cabinet": return MakeCabinet(160, 272);
                case "prop_clock": return MakeClock(112);
                case "token": return MakeClock(28);   // pocket-watch token left by a revealed anomaly
                case "rough_panel": return MakeRoughPanel(256, 192, 0.88f);
                case "rough_panel_dark": return MakeRoughPanel(256, 192, 0.16f);
                case "rough_circle": return MakeRoughCircle(128);
                case "school_facade": return MakeSchoolFacade(300, 150);
                case "camera_frame": return MakeCameraFrame(252, 150);
                case "window_frame": return MakeWindowFrame(470, 340);
                case "prop_photoframe": return MakePhotoFrame(144, 108);
                case "prop_lightfixture": return MakeLightFixture(208, 40);
                case "prop_lostid": return MakeLostId(56, 36);
                case "custodian_0": return MakeCustodian(96, 224, 0);
                case "custodian_1": return MakeCustodian(96, 224, 1);
                case "anomaly_blink": return MakeAnomaly(128, 336, AnomalyBehaviorKind.Blink);
                case "anomaly_slide": return MakeAnomaly(128, 336, AnomalyBehaviorKind.Slide);
                case "anomaly_sprint": return MakeAnomaly(160, 336, AnomalyBehaviorKind.Sprint);
                case "anomaly_coverhug": return MakeAnomaly(192, 224, AnomalyBehaviorKind.CoverHug);
                case "id_card_frame": return MakeIdCard(430, 280);
                case "record_sheet": return MakeRecordSheet(450, 580);
                case "clipboard": return MakeClipboard(370, 490);
                case "note_paper": return MakeNotePaper(390, 300);
                case "stamp_allow": return MakeStamp(false);
                case "stamp_reject": return MakeStamp(true);
                case "icon_check": return MakeIcon(true);
                case "icon_x": return MakeIcon(false);
                default:
                    Debug.LogWarning("[ProceduralArt] unknown sprite key '" + key + "' — returning solid.");
                    return MakeSolid(G(1f, 0f));
            }
        }

        public static Sprite Portrait(string keyPrefix, Portrait p, int w = 168, int h = 208)
        {
            string key = keyPrefix + "_" + p.HairStyle + p.FaceVariant + p.Accessory + (p.Distorted ? "d" : "c");
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var c = new Canvas2D(w, h);
            c.GradientV(G(0.80f), G(0.62f));            // paper-gray photo backdrop
            c.Noise(0.05f, key.GetHashCode());
            float cx = w / 2f, baseY = h * 0.98f;

            var skin = G(0.30f);
            c.FillEllipse(cx, baseY + h * 0.10f, w * 0.46f, h * 0.22f, skin);      // shoulders
            c.FillRect((int)(cx - w * 0.07f), (int)(h * 0.62f), (int)(w * 0.14f), (int)(h * 0.14f), skin); // neck
            float headCy = h * 0.47f, headRx = w * 0.27f, headRy = h * 0.24f;
            c.FillEllipse(cx, headCy, headRx, headRy, skin);                        // head

            var hair = G(0.12f);
            switch (p.HairStyle)
            {
                case 0: // buzz
                    c.FillEllipse(cx, headCy - headRy * 0.25f, headRx * 0.98f, headRy * 0.8f, hair);
                    break;
                case 1: // short side-part
                    c.FillEllipse(cx - headRx * 0.1f, headCy - headRy * 0.3f, headRx * 1.02f, headRy * 0.78f, hair);
                    c.FillRect((int)(cx + headRx * 0.35f), (int)(headCy - headRy * 0.2f), (int)(headRx * 0.6f), (int)(headRy * 0.5f), hair);
                    break;
                case 2: // long
                    c.FillEllipse(cx, headCy - headRy * 0.2f, headRx * 1.15f, headRy * 0.95f, hair);
                    c.FillRect((int)(cx - headRx * 1.12f), (int)(headCy - headRy * 0.1f), (int)(headRx * 0.42f), (int)(headRy * 1.5f), hair);
                    c.FillRect((int)(cx + headRx * 0.72f), (int)(headCy - headRy * 0.1f), (int)(headRx * 0.42f), (int)(headRy * 1.5f), hair);
                    break;
                case 3: // curly (blobs)
                    for (int i = 0; i < 9; i++)
                    {
                        float ang = Mathf.PI * (0.08f + 0.84f * i / 8f);
                        c.FillEllipse(cx - Mathf.Cos(ang) * headRx * 1.02f, headCy - Mathf.Sin(ang) * headRy * 1.05f,
                            headRx * 0.34f, headRy * 0.30f, hair);
                    }
                    break;
                case 4: // ponytail
                    c.FillEllipse(cx, headCy - headRy * 0.3f, headRx * 1.0f, headRy * 0.75f, hair);
                    c.FillEllipse(cx + headRx * 1.15f, headCy + headRy * 0.35f, headRx * 0.30f, headRy * 0.85f, hair);
                    break;
                default: // bun
                    c.FillEllipse(cx, headCy - headRy * 0.3f, headRx * 1.0f, headRy * 0.75f, hair);
                    c.FillEllipse(cx, headCy - headRy * 1.12f, headRx * 0.34f, headRy * 0.30f, hair);
                    break;
            }

            var eyeC = G(0.88f);
            float eyeY = headCy + headRy * (0.02f + (p.FaceVariant % 3) * 0.06f);
            float eyeSpread = headRx * (0.36f + (p.FaceVariant % 2) * 0.10f);
            float eyeW = headRx * (0.16f + (p.FaceVariant / 3) * 0.05f), eyeH = headRy * 0.09f;
            c.FillEllipse(cx - eyeSpread, eyeY, eyeW, eyeH, eyeC);
            c.FillEllipse(cx + eyeSpread, eyeY, eyeW, eyeH, eyeC);
            c.Line((int)cx, (int)(eyeY + headRy * 0.12f), (int)cx, (int)(eyeY + headRy * 0.34f), G(0.55f), 2); // nose
            float mouthW = headRx * (0.28f + (p.FaceVariant % 4) * 0.06f);
            float mouthY = headCy + headRy * 0.62f;
            if (p.FaceVariant % 3 == 0) c.Line((int)(cx - mouthW), (int)mouthY, (int)(cx + mouthW), (int)mouthY, G(0.62f), 2);
            else if (p.FaceVariant % 3 == 1)
            {
                c.Line((int)(cx - mouthW), (int)mouthY + 2, (int)cx, (int)mouthY - 2, G(0.62f), 2);
                c.Line((int)cx, (int)mouthY - 2, (int)(cx + mouthW), (int)mouthY + 2, G(0.62f), 2);
            }
            else c.FillEllipse(cx, mouthY, mouthW * 0.55f, headRy * 0.06f, G(0.62f));

            switch (p.Accessory)
            {
                case 1: // glasses
                    c.FillEllipse(cx - eyeSpread, eyeY, eyeW * 1.9f, eyeH * 2.6f, G(0.1f, 0.0f));
                    DrawRing(c, cx - eyeSpread, eyeY, eyeW * 1.9f, eyeH * 2.6f, G(0.9f));
                    DrawRing(c, cx + eyeSpread, eyeY, eyeW * 1.9f, eyeH * 2.6f, G(0.9f));
                    c.Line((int)(cx - eyeSpread + eyeW * 1.9f), (int)eyeY, (int)(cx + eyeSpread - eyeW * 1.9f), (int)eyeY, G(0.9f), 2);
                    break;
                case 2: // earrings
                    c.FillEllipse(cx - headRx * 0.98f, eyeY + headRy * 0.42f, 3, 3, G(0.95f));
                    c.FillEllipse(cx + headRx * 0.98f, eyeY + headRy * 0.42f, 3, 3, G(0.95f));
                    break;
                case 3: // cap
                    c.FillEllipse(cx, headCy - headRy * 0.55f, headRx * 1.06f, headRy * 0.55f, G(0.18f));
                    c.FillRect((int)(cx - headRx * 1.3f), (int)(headCy - headRy * 0.62f), (int)(headRx * 2.6f), 6, G(0.22f));
                    break;
            }

            if (p.Distorted)
            {
                var r = new System.Random(1349);
                c.ShiftRows(r, 7, 9);
                c.FillEllipse(cx + eyeSpread + 5, eyeY - 4, eyeW * 1.3f, eyeH * 1.5f, G(0.95f));
                c.FillEllipse(cx + eyeSpread + 5, eyeY - 4, eyeW * 0.5f, eyeH * 0.7f, G(0.02f));
                c.FillRect((int)(cx - mouthW * 1.4f), (int)mouthY - 2, (int)(mouthW * 2.8f), 4, G(0.05f));
                for (int s = 0; s < 26; s++)
                    c.FillRect(r.Next(w), r.Next(h), 2 + r.Next(10), 1 + r.Next(2), G(r.Next(2) == 0 ? 0.95f : 0.05f, 0.8f));
            }

            for (int i = 0; i < w; i++) { c.Px[i] = G(0.25f); c.Px[(h - 1) * w + i] = G(0.25f); }
            for (int i = 0; i < h; i++) { c.Px[i * w] = G(0.25f); c.Px[i * w + w - 1] = G(0.25f); }

            var sp = c.ToSprite(key, UiPpu);
            Cache[key] = sp;
            return sp;
        }

        private static void DrawRing(Canvas2D c, float cx, float cy, float rx, float ry, Color col)
        {
            for (float a = 0; a < Mathf.PI * 2; a += 0.06f)
                c.FillRect((int)(cx + Mathf.Cos(a) * rx), (int)(cy + Mathf.Sin(a) * ry), 2, 2, col);
        }

        private static Sprite MakeSolid(Color c)
        {
            var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = c;
            t.SetPixels(px); t.Apply(false, false); t.name = "solid";
            var s = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), UiPpu);
            s.name = "solid_white";
            return s;
        }

        private static Sprite MakeVignette(int w, int h, float strength)
        {
            var c = new Canvas2D(w, h);
            float cx = w / 2f, cy = h / 2f, maxD = Mathf.Sqrt(cx * cx + cy * cy);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + ((y - cy) * 1.15f) * ((y - cy) * 1.15f)) / maxD;
                    float a = Mathf.Clamp01((d - 0.35f) / 0.65f) * strength;
                    c.Px[y * w + x] = new Color(0, 0, 0, a * a);
                }
            return c.ToSprite("vignette", UiPpu);
        }

        private static Sprite MakeNoise(int w, int h, float alpha, int seed)
        {
            var c = new Canvas2D(w, h);
            var r = new System.Random(seed);
            for (int i = 0; i < w * h; i++)
            {
                float v = (float)r.NextDouble();
                c.Px[i] = new Color(v, v, v, alpha * v);
            }
            return c.ToSprite("noise_overlay", UiPpu);
        }

        private static Sprite MakeFlash(int size)
        {
            var c = new Canvas2D(size, size);
            float half = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                    c.Px[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(1f - d) * 0.9f);
                }
            return c.ToSprite("flash", UiPpu);
        }

        private static Sprite MakeLightCone(int w, int h)
        {
            var c = new Canvas2D(w, h);
            for (int x = 0; x < w; x++)
            {
                float t = x / (float)(w - 1);                       // 0 at player, 1 at far end
                float halfH = h * 0.06f + t * h * 0.44f;             // widening cone
                float alpha = (1f - t * 0.75f) * 0.5f;
                for (int y = 0; y < h; y++)
                {
                    float dy = Mathf.Abs(y - h / 2f);
                    float edge = Mathf.Clamp01((halfH - dy) / (h * 0.08f));
                    c.Px[y * w + x] = new Color(0.9f, 0.9f, 0.85f, alpha * edge);
                }
            }
            return c.ToSprite("light_cone", WorldPpu);
        }

        private static void RoughLine(Canvas2D c, System.Random r, int x0, int y0, int x1, int y1, float col, int thick)
        {
            int steps = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0f : i / (float)steps;
                int x = (int)(x0 + (x1 - x0) * t) + r.Next(-1, 2);
                int y = (int)(y0 + (y1 - y0) * t) + r.Next(-1, 2);
                for (int d = 0; d < thick; d++)
                {
                    int yy = y + d;
                    if (x >= 0 && yy >= 0 && x < c.W && yy < c.H)
                        c.Px[yy * c.W + x] = new Color(col, col, col * 0.97f, 1f);
                }
            }
        }

        private static Sprite MakeRoughPanel(int w, int h, float paper)
        {
            var c = new Canvas2D(w, h);
            var r = new System.Random(77);
            c.FillRect(0, 0, w, h, G(paper));
            RoughLine(c, r, 2, 2, w - 3, 3, 0.07f, 3);
            RoughLine(c, r, w - 3, 2, w - 3, h - 3, 0.07f, 3);
            RoughLine(c, r, w - 3, h - 3, 2, h - 3, 0.07f, 3);
            RoughLine(c, r, 2, h - 3, 2, 2, 0.07f, 3);
            RoughLine(c, r, 6, 7, w - 7, 7, 0.18f, 1);
            RoughLine(c, r, 7, 6, 7, h - 7, 0.18f, 1);
            return c.ToSprite("rough_panel", UiPpu);
        }

        private static Sprite MakeRoughCircle(int d)
        {
            var c = new Canvas2D(d, d);
            var r = new System.Random(91);
            float rad = d / 2f - 4f;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dist = Mathf.Sqrt((x - d / 2f) * (x - d / 2f) + (y - d / 2f) * (y - d / 2f));
                    if (dist <= rad) c.Px[y * d + x] = G(0.88f);
                    if (dist <= rad && dist > rad - 3f + r.Next(-1, 2)) c.Px[y * d + x] = G(0.08f);
                }
            return c.ToSprite("rough_circle", UiPpu);
        }

        private static Sprite MakeSchoolFacade(int w, int h)
        {
            var c = new Canvas2D(w, h);
            var r = new System.Random(5);
            c.FillRect(0, 0, w, h, G(0.06f));
            c.FillRect(w / 2 - 60, h / 2 - 60, 120, 110, G(0.85f));          // central block
            c.FillRect(w / 2 - 130, h / 2 - 30, 70, 80, G(0.80f));          // wings
            c.FillRect(w / 2 + 60, h / 2 - 30, 70, 80, G(0.80f));
            for (int i = 0; i < 4; i++)                                      // windows
                for (int j = 0; j < 3; j++)
                    c.FillRect(w / 2 - 48 + i * 26, h / 2 - 48 + j * 30, 12, 18, G(0.10f));
            for (int i = 0; i < 3; i++)
            {
                c.FillRect(w / 2 - 120 + i * 22, h / 2 - 18 + (i % 2) * 22, 10, 16, G(0.12f));
                c.FillRect(w / 2 + 70 + i * 22, h / 2 - 18 + (i % 2) * 22, 10, 16, G(0.12f));
            }
            for (int a = 0; a < 40; a++)                                     // clock
            {
                float t = a / 40f * Mathf.PI * 2f;
                int x = (int)(w / 2 + Mathf.Cos(t) * 14), y = (int)(h / 2 - 74 + Mathf.Sin(t) * 14);
                if (x >= 0 && y >= 0 && x < w && y < h) c.Px[y * w + x] = G(0.10f);
            }
            c.FillRect(w / 2 - 1, h / 2 - 84, 2, 10, G(0.10f));
            RoughLine(c, r, 4, h - 6, w - 4, h - 6, 0.85f, 2);               // ground chalk line
            return c.ToSprite("school_facade", UiPpu);
        }

        private static Sprite MakeCameraFrame(int w, int h)
        {
            var c = new Canvas2D(w, h);
            var r = new System.Random(31);
            int L = 26;
            RoughLine(c, r, 4, 4, 4 + L, 4, 0.9f, 3); RoughLine(c, r, 4, 4, 4, 4 + L, 0.9f, 3);
            RoughLine(c, r, w - 5, 4, w - 5 - L, 4, 0.9f, 3); RoughLine(c, r, w - 5, 4, w - 5, 4 + L, 0.9f, 3);
            RoughLine(c, r, 4, h - 5, 4 + L, h - 5, 0.9f, 3); RoughLine(c, r, 4, h - 5, 4, h - 5 - L, 0.9f, 3);
            RoughLine(c, r, w - 5, h - 5, w - 5 - L, h - 5, 0.9f, 3); RoughLine(c, r, w - 5, h - 5, w - 5, h - 5 - L, 0.9f, 3);
            return c.ToSprite("camera_frame", UiPpu);
        }

        private static Sprite MakeWindowFrame(int w, int h)
        {
            var c = new Canvas2D(w, h);
            var r = new System.Random(17);
            c.FillRect(0, 0, w, h, G(0.05f));
            c.FillRect(10, 10, w - 20, h - 20, G(0.13f));     // the dark beyond the glass
            RoughLine(c, r, 4, 4, w - 5, 5, 0.82f, 4);
            RoughLine(c, r, w - 5, 4, w - 5, h - 5, 0.82f, 4);
            RoughLine(c, r, w - 5, h - 5, 4, h - 5, 0.82f, 4);
            RoughLine(c, r, 4, h - 5, 4, 4, 0.82f, 4);
            RoughLine(c, r, w / 2, 10, w / 2, h - 10, 0.75f, 3);   // mullion
            return c.ToSprite("window_frame", UiPpu);
        }

        private static Sprite MakeMenuBg(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.GradientV(G(0.05f), G(0.13f));
            float vx = w * 0.5f, vy = h * 0.52f;
            c.FillRect(0, (int)(h * 0.72f), w, (int)(h * 0.28f), G(0.07f));           // floor
            for (int i = 0; i < 9; i++)                                                // wall panels
            {
                float t = i / 9f;
                int x = (int)(vx - (vx) * (1 - t * 0.85f));
                c.FillRect(x, (int)(vy - h * 0.30f * (1 - t)), 8 + (int)(30 * t), (int)(h * 0.55f * (1 - t) + h * 0.16f), G(0.10f + t * 0.04f));
                c.FillRect(w - x - 8 - (int)(30 * t), (int)(vy - h * 0.30f * (1 - t)), 8 + (int)(30 * t), (int)(h * 0.55f * (1 - t) + h * 0.16f), G(0.10f + t * 0.04f));
            }
            for (int i = 1; i <= 4; i++)
            {
                float t = i / 5f;
                int lw = (int)(180 * (1 - t)) + 12, lh = Math.Max(3, (int)(10 * (1 - t)));
                c.FillRect((int)(vx - lw / 2f), (int)(vy - h * 0.34f * (1 - t * 0.9f)), lw, lh, G(0.55f - t * 0.3f));
            }
            c.FillEllipse(vx, vy + h * 0.10f, 16, 52, G(0.015f));
            c.FillEllipse(vx, vy + h * 0.035f, 8, 9, G(0.015f));
            c.FillRect((int)vx - 2, (int)(vy + h * 0.028f), 4, 3, G(0.85f, 0.7f)); // single pale eye-glint
            c.Noise(0.05f, 99);
            c.Vignette(0.95f);
            return c.ToSprite("menu_bg", UiPpu);
        }

        private static Sprite MakeHallway(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.GradientV(G(0.10f), G(0.16f));                       // upper wall
            int floorY = (int)(h * 0.74f);
            c.FillRect(0, floorY, w, h - floorY, G(0.07f));        // floor
            c.FillRect(0, floorY, w, 3, G(0.22f));                 // baseboard line
            var r = new System.Random(21);
            for (int x = 24; x < w - 100; x += 180)
            {
                bool isDoor = r.Next(3) == 0;
                if (isDoor)
                {
                    c.FillRect(x, floorY - 300, 120, 300, G(0.13f));
                    c.FillRect(x + 6, floorY - 294, 108, 288, G(0.09f));
                    c.FillEllipse(x + 100, floorY - 150, 5, 5, G(0.5f));     // handle
                    c.FillRect(x + 30, floorY - 268, 60, 18, G(0.35f));      // room plate
                }
                else
                {
                    for (int lx = 0; lx < 3; lx++)
                    {
                        c.FillRect(x + lx * 44, floorY - 260, 40, 260, G(0.115f));
                        c.FillRect(x + lx * 44 + 4, floorY - 256, 32, 252, G(0.085f));
                        c.FillRect(x + lx * 44 + 14, floorY - 220, 12, 3, G(0.3f));   // vent slits
                        c.FillRect(x + lx * 44 + 14, floorY - 214, 12, 3, G(0.3f));
                        c.FillEllipse(x + lx * 44 + 34, floorY - 130, 2.5f, 2.5f, G(0.4f));
                    }
                }
            }
            for (int x = 60; x < w; x += 240)
            {
                bool lit = r.Next(4) != 0;
                c.FillRect(x, 18, 150, 12, G(lit ? 0.72f : 0.10f));
                if (lit) c.BlendRect(x - 60, 30, 270, 200, G(0.5f, 0.05f));  // light pool
            }
            for (int x = 0; x < w; x += 96) c.FillRect(x, floorY, 2, h - floorY, G(0.09f));
            c.Noise(0.045f, 5);
            c.Vignette(0.55f);
            return c.ToSprite("hallway_bg", WorldPpu);
        }

        private static Sprite MakeClassroom(int w, int h, bool room2)
        {
            var c = new Canvas2D(w, h);
            c.GradientV(G(0.11f), G(0.15f));
            int floorY = (int)(h * 0.72f);
            c.FillRect(0, floorY, w, h - floorY, G(0.075f));
            int bx = room2 ? (int)(w * 0.52f) : (int)(w * 0.12f);
            c.FillRect(bx, (int)(h * 0.16f), (int)(w * 0.36f), (int)(h * 0.34f), G(0.16f));     // frame
            c.FillRect(bx + 8, (int)(h * 0.16f) + 8, (int)(w * 0.36f) - 16, (int)(h * 0.34f) - 16, G(0.055f)); // board
            var r = new System.Random(room2 ? 77 : 33);
            for (int i = 0; i < 7; i++)                                                          // chalk scribbles
            {
                int sx = bx + 24 + r.Next((int)(w * 0.36f) - 80);
                int sy = (int)(h * 0.16f) + 30 + r.Next((int)(h * 0.34f) - 70);
                c.Line(sx, sy, sx + 30 + r.Next(90), sy + r.Next(-6, 7), G(0.5f, 0.5f), 2);
            }
            c.FillRect(bx + 8, (int)(h * 0.16f) + (int)(h * 0.34f) - 6, (int)(w * 0.36f) - 16, 6, G(0.25f)); // tray
            int wx = room2 ? (int)(w * 0.06f) : (int)(w * 0.62f);
            for (int i = 0; i < 2; i++)
            {
                int x = wx + i * 260;
                c.FillRect(x, (int)(h * 0.18f), 200, (int)(h * 0.36f), G(0.09f));
                c.FillRect(x + 8, (int)(h * 0.18f) + 8, 184, (int)(h * 0.36f) - 16, G(0.20f));  // night glass
                c.FillRect(x + 96, (int)(h * 0.18f) + 8, 8, (int)(h * 0.36f) - 16, G(0.09f));   // mullion
                c.FillRect(x + 8, (int)(h * 0.18f) + (int)(h * 0.18f), 184, 8, G(0.09f));
                c.FillEllipse(x + 150, (int)(h * 0.18f) + 40, 26, 26, G(0.30f, 0.5f));
            }
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 5; col++)
                {
                    int x = 90 + col * (int)(w * 0.185f);
                    int y = floorY + 18 + row * 46;
                    int dw = 130 - row * 8, dh = 26 - row * 3;
                    c.FillRect(x, y, dw, dh, G(0.13f + row * 0.02f));
                    c.FillRect(x + 12, y + dh, 6, 30 - row * 4, G(0.10f));
                    c.FillRect(x + dw - 18, y + dh, 6, 30 - row * 4, G(0.10f));
                }
            c.FillRect(30, floorY - 96, 190, 96, G(0.12f));
            c.FillRect(38, floorY - 88, 174, 84, G(0.09f));
            c.FillRect(w - 150, floorY - 240, 120, 240, G(0.115f));
            for (int i = 0; i < 3; i++) c.FillRect(w - 142, floorY - 230 + i * 78, 104, 70, G(0.085f));
            c.FillEllipse(w / 2, (int)(h * 0.10f), 34, 34, G(0.14f));
            c.FillEllipse(w / 2, (int)(h * 0.10f), 28, 28, G(0.75f));
            c.Line(w / 2, (int)(h * 0.10f), w / 2, (int)(h * 0.10f) - 18, G(0.1f), 3);   // hour hand
            c.Line(w / 2, (int)(h * 0.10f), w / 2 + 12, (int)(h * 0.10f) + 4, G(0.1f), 2); // minute at :47-ish
            c.FillRect(w - 320, floorY - 300, 120, 300, G(0.13f));
            c.FillRect(w - 314, floorY - 294, 108, 288, G(0.095f));
            c.FillEllipse(w - 224, floorY - 150, 5, 5, G(0.5f));
            c.FillRect(w / 2 - 160, 14, 320, 10, G(0.6f));
            c.BlendRect(w / 2 - 260, 24, 520, 240, G(0.5f, 0.05f));
            c.Noise(0.04f, room2 ? 12 : 8);
            c.Vignette(0.6f);
            return c.ToSprite(room2 ? "classroom2_bg" : "classroom1_bg", WorldPpu);
        }

        private static Sprite MakeSecurityRoom(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.Fill(G(0.06f));
            c.FillRect(0, 0, w, (int)(h * 0.66f), G(0.09f));
            int wx = (int)(w * 0.24f), wy = (int)(h * 0.08f), ww = (int)(w * 0.52f), wh = (int)(h * 0.44f);
            c.FillRect(wx - 12, wy - 12, ww + 24, wh + 24, G(0.16f));          // frame
            c.FillRect(wx, wy, ww, wh, G(0.22f));                              // glass (visitor drawn under UI glass tint)
            for (int i = 1; i <= 2; i++) c.FillRect(wx + ww * i / 3 - 3, wy, 6, wh, G(0.14f)); // bars
            c.FillRect(wx, wy + (int)(wh * 0.72f), ww, wh - (int)(wh * 0.72f), G(0.17f));
            c.FillRect(0, (int)(h * 0.66f), w, h - (int)(h * 0.66f), G(0.11f));
            c.FillRect(0, (int)(h * 0.66f), w, 6, G(0.24f));
            c.FillRect((int)(w * 0.03f), (int)(h * 0.52f), 150, 100, G(0.14f));
            c.FillRect((int)(w * 0.03f) + 8, (int)(h * 0.52f) + 8, 134, 78, G(0.30f));
            c.FillRect((int)(w * 0.03f) + 60, (int)(h * 0.52f) + 100, 30, 22, G(0.12f));
            c.FillRect((int)(w * 0.88f), (int)(h * 0.56f), 120, 60, G(0.15f));   // ID scanner
            c.FillRect((int)(w * 0.88f) + 10, (int)(h * 0.56f) + 8, 100, 8, G(0.4f));
            c.Noise(0.04f, 42);
            c.Vignette(0.5f);
            return c.ToSprite("security_bg", UiPpu);
        }

        private static Sprite MakeDoor(int w, int h, bool security, bool chained = false)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, 0, w, h, G(0.14f));
            c.FillRect(8, 8, w - 16, h - 12, G(security ? 0.10f : 0.115f));
            c.FillRect(20, 26, w - 40, (int)(h * 0.3f), G(0.085f));
            c.FillRect(20, (int)(h * 0.42f), w - 40, (int)(h * 0.44f), G(0.085f));
            c.FillEllipse(w - 28, h / 2, 6, 6, G(0.55f));
            if (security) c.FillRect(w / 2 - 44, 40, 88, 20, G(0.35f)); // "SECURITY" plate
            if (chained)
            {
                for (int i = 0; i < 5; i++)
                    c.Line(10, 60 + i * 54, w - 10, 84 + i * 54, G(0.45f), 5);   // chains
                c.FillEllipse(w / 2, h / 2, 14, 18, G(0.5f));                    // padlock
                c.FillRect(w / 2 - 10, h / 2 - 4, 20, 26, G(0.42f));
            }
            c.Noise(0.04f, 3);
            return c.ToSprite(chained ? "prop_server_door" : (security ? "prop_door_security" : "prop_door"), WorldPpu);
        }

        private static Sprite MakeLockers(int w, int h)
        {
            var c = new Canvas2D(w, h);
            for (int i = 0; i < 3; i++)
            {
                c.FillRect(i * (w / 3), 0, w / 3 - 4, h, G(0.115f));
                c.FillRect(i * (w / 3) + 5, 5, w / 3 - 14, h - 10, G(0.085f));
                c.FillRect(i * (w / 3) + 18, 30, w / 3 - 44, 6, G(0.3f));
                c.FillRect(i * (w / 3) + 18, 42, w / 3 - 44, 6, G(0.3f));
                c.FillEllipse(i * (w / 3) + w / 6 - 2, h / 2, 3, 3, G(0.42f));
            }
            c.Noise(0.04f, 17);
            return c.ToSprite("prop_lockers", WorldPpu);
        }

        private static Sprite MakeDeskProp(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, 0, w, (int)(h * 0.35f), G(0.15f));
            c.FillRect(10, (int)(h * 0.35f), 12, (int)(h * 0.65f), G(0.10f));
            c.FillRect(w - 22, (int)(h * 0.35f), 12, (int)(h * 0.65f), G(0.10f));
            c.FillRect((int)(w * 0.3f), 6, (int)(w * 0.3f), 10, G(0.6f)); // paper on desk
            c.Noise(0.04f, 23);
            return c.ToSprite("prop_desk", WorldPpu);
        }

        private static Sprite MakeChalkboardProp(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, 0, w, h, G(0.16f));
            c.FillRect(8, 8, w - 16, h - 20, G(0.055f));
            var r = new System.Random(91);
            for (int i = 0; i < 8; i++)
            {
                int sx = 24 + r.Next(w - 120), sy = 24 + r.Next(h - 70);
                c.Line(sx, sy, sx + 40 + r.Next(80), sy + r.Next(-8, 9), G(0.45f, 0.55f), 2);
            }
            c.FillRect(8, h - 14, w - 16, 6, G(0.25f));
            c.Noise(0.03f, 29);
            return c.ToSprite("prop_chalkboard", WorldPpu);
        }

        private static Sprite MakeWindowProp(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, 0, w, h, G(0.10f));
            c.FillRect(10, 10, w - 20, h - 20, G(0.19f));     // night glass
            c.FillRect(w / 2 - 4, 10, 8, h - 20, G(0.10f));
            c.FillRect(10, h / 2 - 4, w - 20, 8, G(0.10f));
            c.FillEllipse((int)(w * 0.72f), (int)(h * 0.24f), 22, 22, G(0.34f, 0.6f)); // streetlight glow
            c.FillRect((int)(w * 0.72f) - 2, (int)(h * 0.24f), 4, (int)(h * 0.5f), G(0.08f));
            c.Noise(0.03f, 31);
            return c.ToSprite("prop_window", WorldPpu);
        }

        private static Sprite MakeNoticeBoard(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, 0, w, h, G(0.15f));
            c.FillRect(8, 8, w - 16, h - 16, G(0.10f));
            var r = new System.Random(57);
            for (int i = 0; i < 7; i++) // pinned papers
            {
                int px = 16 + r.Next(w - 80), py = 16 + r.Next(h - 70);
                c.FillRect(px, py, 52 + r.Next(24), 44 + r.Next(28), G(0.68f, 0.9f));
                c.FillRect(px + 22, py - 3, 6, 6, G(0.3f));
            }
            c.Noise(0.03f, 37);
            return c.ToSprite("prop_noticeboard", WorldPpu);
        }

        private static Sprite MakeCabinet(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, 0, w, h, G(0.125f));
            for (int i = 0; i < 3; i++)
            {
                int dy = 10 + i * (h / 3);
                c.FillRect(8, dy, w - 16, h / 3 - 14, G(0.09f));
                c.FillRect(w / 2 - 18, dy + (h / 3) / 2 - 3, 36, 6, G(0.4f));
            }
            c.Noise(0.04f, 41);
            return c.ToSprite("prop_cabinet", WorldPpu);
        }

        private static Sprite MakeClock(int size)
        {
            var c = new Canvas2D(size, size);
            int half = size / 2;
            c.FillEllipse(half, half, half - 2, half - 2, G(0.14f));
            c.FillEllipse(half, half, half - 8, half - 8, G(0.78f));
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                c.FillRect(half + (int)(Mathf.Sin(a) * (half - 14)) - 1, half - (int)(Mathf.Cos(a) * (half - 14)) - 1, 3, 3, G(0.15f));
            }
            float ha = (1 + 47 / 60f) * Mathf.PI / 6f, ma = 47 * Mathf.PI / 30f;
            c.Line(half, half, half + (int)(Mathf.Sin(ha) * half * 0.45f), half - (int)(Mathf.Cos(ha) * half * 0.45f), G(0.1f), 4);
            c.Line(half, half, half + (int)(Mathf.Sin(ma) * half * 0.72f), half - (int)(Mathf.Cos(ma) * half * 0.72f), G(0.1f), 2);
            c.FillEllipse(half, half, 4, 4, G(0.2f));
            return c.ToSprite("prop_clock", WorldPpu);
        }

        private static Sprite MakePhotoFrame(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, 0, w, h, G(0.17f));
            c.FillRect(8, 8, w - 16, h - 16, G(0.55f));     // old photo
            var r = new System.Random(71);
            for (int row = 0; row < 3; row++)               // rows of heads
                for (int i = 0; i < 9; i++)
                {
                    int x = 16 + i * ((w - 32) / 9), y = 20 + row * ((h - 36) / 3);
                    c.FillEllipse(x + 4, y + 8, 4, 5, G(0.2f));
                    c.FillRect(x, y + 12, 9, 8, G(0.2f));
                }
            c.FillRect(w - 34, 22, 12, 20, G(0.05f));       // the ageless staff figure
            c.FillEllipse(w - 28, 18, 5, 5, G(0.05f));
            c.FillEllipse(w - 30, 17, 1.5f, 1.5f, G(0.95f)); // glint
            c.Noise(0.08f, 73);
            return c.ToSprite("prop_photoframe", WorldPpu);
        }

        private static Sprite MakeLightFixture(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, h / 2 - 6, w, 12, G(0.13f));
            c.FillRect(w / 2 - 90, 0, 180, h, G(0.16f));
            c.FillRect(w / 2 - 82, 6, 164, h - 14, G(0.7f));
            return c.ToSprite("prop_lightfixture", WorldPpu);
        }

        private static Sprite MakeLostId(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, 0, w, h, G(0.72f));
            c.FillRect(2, 2, w - 4, 8, G(0.3f));
            c.FillRect(5, 13, 14, 18, G(0.45f));           // photo slot
            for (int i = 0; i < 4; i++) c.FillRect(23, 14 + i * 5, w - 28, 2, G(0.35f));
            c.Noise(0.06f, 79);
            return c.ToSprite("prop_lostid", WorldPpu);
        }

        private static Sprite MakeCustodian(int w, int h, int frame)
        {
            var c = new Canvas2D(w, h);
            var body = G(0.82f);   // pale silhouette custodian (reads against dark rooms)
            var dark = G(0.25f);
            float cx = w / 2f;
            c.FillEllipse(cx, h * 0.12f, 13, 15, body);                     // head
            c.FillRect((int)(cx - 16), (int)(h * 0.055f), 32, 6, dark);     // cap brim
            c.FillEllipse(cx, h * 0.055f, 14, 8, dark);                     // cap
            c.FillRect((int)(cx - 15), (int)(h * 0.19f), 30, (int)(h * 0.34f), body); // torso (uniform)
            c.FillRect((int)(cx - 15), (int)(h * 0.24f), 30, 5, dark);      // belt line
            c.FillRect((int)(cx - 6), (int)(h * 0.21f), 10, 12, G(0.55f));  // ID badge on chest
            c.FillRect((int)(cx - 22), (int)(h * 0.21f), 7, (int)(h * 0.26f), body);
            c.FillRect((int)(cx + 15), (int)(h * 0.21f), 7, (int)(h * 0.26f), body);
            int legTop = (int)(h * 0.53f), legH = (int)(h * 0.44f);
            if (frame == 0)
            {
                c.FillRect((int)(cx - 12), legTop, 9, legH, body);
                c.FillRect((int)(cx + 3), legTop, 9, legH, body);
            }
            else
            {
                c.FillRect((int)(cx - 16), legTop, 9, legH - 6, body);
                c.FillRect((int)(cx + 7), legTop, 9, legH - 6, body);
                c.FillRect((int)(cx - 16), legTop + legH - 8, 14, 6, body); // forward foot
            }
            c.FillRect((int)(cx - 14), h - 6, 12, 6, dark);
            c.FillRect((int)(cx + 2), h - 6, 12, 6, dark);
            return c.ToSprite("custodian_" + frame, WorldPpu);
        }

        private static Sprite MakeAnomaly(int w, int h, AnomalyBehaviorKind kind)
        {
            var c = new Canvas2D(w, h);
            var body = G(0.02f);          // near-black silhouette
            var eye = G(0.95f);
            float cx = w / 2f;
            switch (kind)
            {
                case AnomalyBehaviorKind.Blink: // unnaturally still, long arms
                    c.FillEllipse(cx, h * 0.10f, 15, 19, body);
                    c.FillRect((int)(cx - 17), (int)(h * 0.16f), 34, (int)(h * 0.42f), body);
                    c.FillRect((int)(cx - 26), (int)(h * 0.19f), 9, (int)(h * 0.52f), body);   // long arms
                    c.FillRect((int)(cx + 17), (int)(h * 0.19f), 9, (int)(h * 0.52f), body);
                    c.FillRect((int)(cx - 13), (int)(h * 0.56f), 10, (int)(h * 0.42f), body);
                    c.FillRect((int)(cx + 3), (int)(h * 0.56f), 10, (int)(h * 0.42f), body);
                    c.FillEllipse(cx - 6, h * 0.09f, 2.6f, 3.4f, eye);
                    c.FillEllipse(cx + 7, h * 0.115f, 2.2f, 3.0f, eye); // asymmetric eyes
                    break;
                case AnomalyBehaviorKind.Slide: // legless gliding shape
                    c.FillEllipse(cx, h * 0.14f, 16, 20, body);
                    c.FillEllipse(cx, h * 0.55f, 26, h * 0.42f, body);
                    c.FillEllipse(cx - 7, h * 0.13f, 2.4f, 2.4f, eye);
                    c.FillEllipse(cx + 7, h * 0.13f, 2.4f, 2.4f, eye);
                    break;
                case AnomalyBehaviorKind.Sprint: // leaning runner, stretched
                    c.FillEllipse(cx + 14, h * 0.13f, 14, 17, body);
                    c.FillRect((int)(cx - 24), (int)(h * 0.18f), 52, 26, body);      // leaning torso
                    c.FillRect((int)(cx - 34), (int)(h * 0.30f), 30, 9, body);       // reaching arm
                    c.FillRect((int)(cx - 6), (int)(h * 0.40f), 10, (int)(h * 0.4f), body);
                    c.FillRect((int)(cx + 12), (int)(h * 0.40f), 10, (int)(h * 0.34f), body);
                    c.FillEllipse(cx + 18, h * 0.125f, 2.2f, 3.0f, eye);
                    c.FillEllipse(cx + 24, h * 0.13f, 2.2f, 3.0f, eye);
                    break;
                default: // CoverHug — crouched, arms spread wide (ceiling lurker)
                    c.FillEllipse(cx, h * 0.30f, 22, 26, body);
                    c.FillRect((int)(cx - 84), (int)(h * 0.24f), 76, 12, body);      // spread arms
                    c.FillRect((int)(cx + 8), (int)(h * 0.24f), 76, 12, body);
                    c.FillEllipse(cx, h * 0.62f, 34, h * 0.30f, body);
                    c.FillEllipse(cx - 8, h * 0.27f, 2.4f, 3.2f, eye);
                    c.FillEllipse(cx + 8, h * 0.27f, 2.4f, 3.2f, eye);
                    break;
            }
            var r = new System.Random((int)kind * 977 + 13);
            for (int i = 0; i < 40; i++)
            {
                int x = r.Next(w), y = r.Next(h);
                if (c.Px[y * w + x].r < 0.1f)
                    c.FillRect(x, y, 1 + r.Next(6), 1, G(r.Next(2) == 0 ? 0.9f : 0.05f, 0.5f));
            }
            return c.ToSprite("anomaly_" + kind.ToString().ToLowerInvariant(), WorldPpu);
        }

        private static Sprite MakeIdCard(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.Fill(G(0.80f));                                        // worn card stock
            c.FillRect(0, 0, w, 44, G(0.22f));                       // header band
            c.FillRect(14, 58, 120, 150, G(0.55f));                  // photo slot frame
            c.FillRect(18, 62, 112, 142, G(0.86f));                  // photo slot (portrait drawn on top by UI)
            for (int i = 0; i < 5; i++) c.FillRect(150, 66 + i * 30, w - 170, 3, G(0.45f)); // text rules
            c.FillRect(14, h - 52, w - 28, 3, G(0.35f));
            c.FillRect(14, h - 40, w - 120, 16, G(0.7f));            // barcode strip
            var r = new System.Random(101);
            for (int i = 0; i < 26; i++)
                c.FillRect(20 + i * ((w - 40) / 26), h - 40, 6 + r.Next(10), 16, G(0.15f));
            c.Noise(0.05f, 103);
            for (int i = 0; i < w; i++) { c.Px[i] = G(0.35f); c.Px[(h - 1) * w + i] = G(0.35f); }
            for (int i = 0; i < h; i++) { c.Px[i * w] = G(0.35f); c.Px[i * w + w - 1] = G(0.35f); }
            return c.ToSprite("id_card_frame", UiPpu);
        }

        private static Sprite MakeRecordSheet(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.Fill(G(0.78f));
            c.FillRect(0, 0, w, 56, G(0.24f));
            for (int y = 84; y < h - 90; y += 42)                    // form lines
            {
                c.FillRect(20, y, w * 2 / 5, 3, G(0.5f));
                c.FillRect(w / 2 + 10, y, w * 2 / 5 - 30, 3, G(0.5f));
            }
            c.FillRect(w - 170, h - 84, 140, 60, G(0.85f));          // stamp box
            for (int i = 0; i < w - 170; i += 6) c.FillRect(20 + i, h - 84, 3, 60, G(0.62f));
            c.Noise(0.06f, 107);
            return c.ToSprite("record_sheet", UiPpu);
        }

        private static Sprite MakeClipboard(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.Fill(G(0.16f));
            c.FillRect(w / 2 - 34, 0, 68, 26, G(0.45f));             // clip
            c.FillRect(14, 20, w - 28, h - 34, G(0.76f));            // paper
            for (int i = 0; i < 8; i++)
            {
                int y = 44 + i * 52;
                c.FillRect(28, y, 26, 26, G(0.85f));                 // checkbox
                c.FillRect(30, y + 2, 22, 22, G(0.2f, 0.15f));
                c.FillRect(66, y + 10, w - 110, 3, G(0.45f));        // row rule
            }
            c.Noise(0.05f, 109);
            return c.ToSprite("clipboard", UiPpu);
        }

        private static Sprite MakeNotePaper(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.Fill(G(0.74f));
            for (int y = 24; y < h - 20; y += 26) c.FillRect(18, y, w - 36, 2, G(0.5f));
            c.Noise(0.07f, 113);
            for (float a = 0; a < Mathf.PI * 2; a += 0.1f)
                c.FillRect((int)(w * 0.78f + Mathf.Cos(a) * 26), (int)(h * 0.25f + Mathf.Sin(a) * 22), 3, 3, G(0.45f, 0.6f));
            for (int i = 0; i < 34; i++) c.FillRect(w - 34 + i / 2, i, 34 - i, 2, G(0.62f));
            return c.ToSprite("note_paper", UiPpu);
        }

        private static Sprite MakeStamp(bool reject)
        {
            int size = 168;
            var c = new Canvas2D(size, size);
            Color col = reject ? new Color(0.72f, 0.12f, 0.16f, 0.92f) : G(0.2f, 0.9f);
            DrawRing(c, size / 2f, size / 2f, 74, 74, col);
            DrawRing(c, size / 2f, size / 2f, 64, 64, col);
            DrawRing(c, size / 2f, size / 2f, 62, 62, col);
            if (reject)
            {
                c.Line(48, 48, size - 48, size - 48, col, 12);
                c.Line(size - 48, 48, 48, size - 48, col, 12);
            }
            else
            {
                c.Line(46, size / 2, size / 2 - 12, size - 56, col, 12);
                c.Line(size / 2 - 12, size - 56, size - 42, 54, col, 12);
            }
            var r = new System.Random(reject ? 7 : 9);
            for (int i = 0; i < 90; i++) // worn ink gaps
                c.FillRect(r.Next(size), r.Next(size), 2 + r.Next(4), 2 + r.Next(3), G(0, 0));
            var sp = c.ToSprite(reject ? "stamp_reject" : "stamp_allow", UiPpu);
            return sp;
        }

        private static Sprite MakeIcon(bool check)
        {
            int s = 36;
            var c = new Canvas2D(s, s);
            if (check) { c.Line(6, 18, 14, 28, G(0.85f), 5); c.Line(14, 28, 30, 8, G(0.85f), 5); }
            else { c.Line(8, 8, 28, 28, new Color(0.85f, 0.2f, 0.24f), 5); c.Line(28, 8, 8, 28, new Color(0.85f, 0.2f, 0.24f), 5); }
            return c.ToSprite(check ? "icon_check" : "icon_x", UiPpu);
        }

        public static void ClearCache()
        {
            foreach (var kv in Cache)
                if (kv.Value != null && kv.Value.texture != null) UnityEngine.Object.Destroy(kv.Value.texture);
            Cache.Clear();
        }
    }
}
````

### `_Project/Scripts/Bootstrap/ProceduralAudio.cs` (15.0 KiB)

````csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    // Synthesises every clip in code; real files in Resources/Audio win if present.
    public static class ProceduralAudio
    {
        public const int Rate = 44100;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public static AudioClip Get(string key)
        {
            if (Cache.TryGetValue(key, out var clip) && clip != null) return clip;
            try
            {
                var loaded = Resources.Load<AudioClip>("Audio/" + key);
                if (loaded != null) { Cache[key] = loaded; return loaded; }
            }
            catch (Exception) { /* fall through to procedural */ }

            var built = Build(key);
            if (built != null) Cache[key] = built;
            else Debug.LogWarning("[ProceduralAudio] unknown clip key '" + key + "'");
            return built;
        }

        private static AudioClip Make(string key, float seconds, Func<float, float> fn, bool loopSeam = true)
        {
            int n = Mathf.Max(1, (int)(seconds * Rate));
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(fn(i / (float)Rate), -1f, 1f);
            if (loopSeam)
            {
                int fade = Math.Min(n / 4, (int)(0.05f * Rate));
                for (int i = 0; i < fade; i++)
                {
                    float t = i / (float)fade;
                    data[i] = data[i] * t + data[n - fade + i] * (1 - t);
                }
            }
            var clip = AudioClip.Create(key, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sine(float freq, float t, float phase = 0f) => Mathf.Sin(2f * Mathf.PI * freq * t + phase);
        private static float Env(float t, float dur, float attack = 0.01f)
        {
            if (t >= dur) return 0f;
            float a = t < attack ? t / attack : 1f;
            float rel = 1f - (t / dur);
            return a * rel * rel;
        }
        private static readonly System.Random NoiseRand = new System.Random(9);
        private static float White() => (float)(NoiseRand.NextDouble() * 2.0 - 1.0);
        private static float LowPass(float[] state, float input, float cutoff01)
        {
            state[0] += cutoff01 * (input - state[0]);
            return state[0];
        }

        private const float Eb1 = 38.89f, Bb1 = 58.27f, Db2 = 69.30f, Gb2 = 46.25f * 2f;
        private const float Eb2 = 77.78f, Gb3 = 185.00f, Bb2 = 116.54f, Ab2 = 103.83f;
        private const float F2 = 87.31f, Cb2 = 61.74f, G2 = 98.00f, Db3 = 138.59f;

        private static AudioClip Build(string key)
        {
            switch (key)
            {
                case "sfx_click":
                    return Make(key, 0.07f, t => Sine(880, t) * Env(t, 0.07f, 0.002f) * 0.5f, false);
                case "sfx_ui_open":
                    return Make(key, 0.16f, t => Sine(440 + t * 900, t) * Env(t, 0.16f, 0.01f) * 0.4f, false);
                case "sfx_ui_close":
                    return Make(key, 0.14f, t => Sine(660 - t * 1400, t) * Env(t, 0.14f, 0.01f) * 0.35f, false);
                case "sfx_paper":
                    return NoiseClip(key, 0.30f, 0.35f, 0.5f);
                case "sfx_stamp_allow":
                    return Make(key, 0.22f, t =>
                        (Sine(120, t) * 0.8f + NoiseAt(t) * 0.5f) * Env(t, 0.22f, 0.002f), false);
                case "sfx_stamp_reject":
                    return Make(key, 0.26f, t =>
                        (Sine(84, t) * 0.9f + Sine(111, t) * 0.4f + NoiseAt(t) * 0.6f) * Env(t, 0.26f, 0.002f), false);
                case "sfx_scan_ok":
                    return Make(key, 0.22f, t =>
                        (t < 0.09f ? Sine(988, t) : Sine(1319, t)) * Env(t % 0.11f, 0.11f, 0.002f) * 0.4f, false);
                case "sfx_scan_error":
                    return Make(key, 0.34f, t =>
                        Sine(220 - t * 240, t) * (1f + 0.5f * Mathf.Sign(Sine(28, t))) * Env(t, 0.34f, 0.005f) * 0.45f, false);
                case "sfx_intercom":
                    return Make(key, 1.1f, t =>
                        NoiseAt(t) * 0.18f * (0.4f + 0.6f * Env(t, 1.1f, 0.2f)) +
                        Sine(60, t) * 0.05f, false);
                case "sfx_knock":
                    return Make(key, 0.9f, t =>
                    {
                        float v = 0;
                        for (int k = 0; k < 3; k++)
                        {
                            float tk = t - k * 0.28f;
                            if (tk >= 0 && tk < 0.12f) v += (Sine(96, tk) * 0.9f + NoiseAt(tk) * 0.35f) * Env(tk, 0.12f, 0.001f);
                        }
                        return v * 0.8f;
                    }, false);
                case "sfx_footstep":
                    return NoiseClip(key, 0.07f, 0.5f, 0.12f);
                case "sfx_door":
                    return Make(key, 0.8f, t =>
                        Sine(70 + 40 * Mathf.Sin(t * 9f), t) * Env(t, 0.8f, 0.1f) * 0.4f +
                        NoiseAt(t) * 0.10f * Env(t, 0.8f, 0.1f), false);
                case "sfx_flicker":
                    return Make(key, 0.5f, t =>
                        (Sine(120, t) * 0.5f + Sine(240, t) * 0.25f) *
                        (NoiseAt(t) > 0.2f ? 1f : 0.15f) * Env(t, 0.5f, 0.01f) * 0.5f, false);
                case "sfx_static":
                    return NoiseClip(key, 0.6f, 0.8f, 0.4f);
                case "sfx_heartbeat":
                    return Make(key, 1.0f, t =>
                    {
                        float v = 0;
                        float t0 = t, t1 = t - 0.22f;
                        if (t0 < 0.14f) v += Sine(52, t0) * Env(t0, 0.14f, 0.004f);
                        if (t1 > 0 && t1 < 0.12f) v += Sine(46, t1) * Env(t1, 0.12f, 0.004f) * 0.8f;
                        return v * 0.9f;
                    }, false);
                case "sfx_stinger":
                    return Make(key, 1.4f, t =>
                        (Sine(233, t) + Sine(247, t) + Sine(466, t) * 0.6f) / 2.6f *
                        Env(t, 1.4f, 0.02f) * 0.7f, false);
                case "sfx_scare":
                    return Make(key, 1.1f, t =>
                        (Sine(740 - t * 420, t) * 0.5f + NoiseAt(t) * 0.5f + Sine(155, t) * 0.4f) *
                        Env(t, 1.1f, 0.006f) * 0.85f, false);
                case "sfx_glitch":
                    return Make(key, 0.35f, t =>
                        (NoiseAt(t) * (Sine(37, t) > 0 ? 0.8f : 0.1f) + Sine(1244, t) * 0.15f) * Env(t, 0.35f, 0.005f), false);
                case "sfx_pickup":
                    return Make(key, 0.28f, t =>
                        Sine(660, t) * Env(t, 0.12f, 0.004f) * 0.4f +
                        (t > 0.1f ? Sine(990, t) * Env(t - 0.1f, 0.16f, 0.004f) * 0.4f : 0f), false);
                case "sfx_bell":
                    return Make(key, 1.6f, t =>
                        (Sine(392, t) + Sine(784, t) * 0.5f + Sine(1176, t) * 0.22f) / 1.7f * Env(t, 1.6f, 0.01f) * 0.5f, false);
                case "sfx_alarm":
                    return Make(key, 1.8f, t =>
                        Sine(330 + 110 * Mathf.Sign(Sine(2.4f, t)), t) * Env(t, 1.8f, 0.05f) * 0.5f, false);

                case "music_menu":       return MenuTheme();
                case "music_checkpoint": return RadioNoiseLoop();
                case "music_hallway":    return HallwayDrone();
                case "music_classroom":  return ClassroomBed();
                case "music_darkroom":   return DarkRoomDrone();
                case "music_investigation": return InvestigationBed();
                case "music_anomaly":    return AnomalyPulse();
                case "music_final":      return FinalDrone();
                case "music_end_win":    return EndWin();
                case "music_end_lose":   return EndLose();
                case "music_end_secret": return EndSecret();

                default: return null;
            }
        }

        private static float NoiseAt(float t)
        {
            int seed = (int)(t * Rate);
            return ((seed * 1103515245 + 12345) % 2147483647) / 1073741823.5f - 1f;
        }

        private static AudioClip NoiseClip(string key, float dur, float cutoff, float vol)
        {
            return Make(key, dur, t =>
            {
                float lp = 0; // one-pole via accumulated state trick
                lp = LowPassShared(key, NoiseAt(t), cutoff);
                return lp * vol * Env(t, dur, 0.01f);
            }, false);
        }

        private static readonly Dictionary<string, float[]> _lpState = new Dictionary<string, float[]>();
        private static float LowPassShared(string key, float input, float cutoff01)
        {
            if (!_lpState.TryGetValue(key, out var st)) { st = new float[1]; _lpState[key] = st; }
            return LowPass(st, input, cutoff01);
        }

        private static AudioClip MenuTheme()
        {
            float[][] bars =
            {
                new[] { Bb1, Eb2, Gb3, Bb2 },            // Ebm over Bb
                new[] { Ab2, Db3, F2 * 2f, Db2 },        // Db over Ab
                new[] { G2, Cb2 * 2f, Eb2, Gb3 * 0.5f }, // Cb over G
                new[] { Ab2, Db3, F2 * 2f, Db2 },        // Db over Ab
                new[] { Bb1, Eb2, Gb3, Bb2 },            // Ebm over Bb
                new[] { Eb2, Ab2 * 0.5f, Cb2 * 2f, Eb2 },// Abm over Eb
                new[] { F2 * 0.5f, Bb2, Db3, F2 },       // Bbm over F
                new[] { Bb1, Eb2, Gb3, Bb2 },            // resolve
            };
            float barLen = 2.4f;
            return Make("music_menu", bars.Length * barLen, t =>
            {
                int bar = Mathf.Clamp((int)(t / barLen), 0, bars.Length - 1);
                float bt = t - bar * barLen;
                float v = 0;
                foreach (var f in bars[bar])
                {
                    v += Sine(f * 0.25f, t) * 0.20f;                  // deep bass
                    v += Sine(f, t) * 0.075f + Sine(f * 1.003f, t) * 0.06f;
                }
                float swell = Mathf.Clamp01(bt / 0.8f) * Mathf.Clamp01((barLen - bt) / 1.1f);
                v *= 0.35f + 0.65f * swell;
                v += NoiseAt(t) * 0.012f;                             // tape hiss
                return v;
            });
        }

        private static AudioClip RadioNoiseLoop()
        {
            return Make("music_checkpoint", 3f, t =>
            {
                float lp = LowPassShared("rn", NoiseAt(t), 0.22f);
                float hum = Sine(60, t) * 0.05f + Sine(120, t) * 0.02f;
                float warble = 0.7f + 0.3f * Sine(0.23f, t);
                return (lp * 0.30f + hum) * warble + Sine(1650, t) * 0.004f * (Sine(0.11f, t) > 0.8f ? 1f : 0f);
            });
        }

        private static AudioClip HallwayDrone()
        {
            return Make("music_hallway", 5f, t =>
                Sine(55, t) * 0.16f + Sine(82.4f, t) * 0.05f +
                LowPassShared("hw", NoiseAt(t), 0.08f) * 0.14f +
                (Sine(0.07f, t) > 0.985f ? Sine(1400, t) * 0.02f : 0f)); // rare distant tick
        }

        private static AudioClip ClassroomBed()
        {
            return Make("music_classroom", 5f, t =>
                Sine(65.4f, t) * 0.10f + Sine(98, t) * 0.045f +
                LowPassShared("cr", NoiseAt(t), 0.06f) * 0.10f);
        }

        private static AudioClip DarkRoomDrone()
        {
            return Make("music_darkroom", 6f, t =>
                Sine(41.2f, t) * 0.20f + Sine(43.1f, t) * 0.14f +           // beating pair
                Sine(110, t) * 0.02f * (0.5f + 0.5f * Sine(0.13f, t)) +
                LowPassShared("dr", NoiseAt(t), 0.04f) * 0.16f);
        }

        private static AudioClip InvestigationBed()
        {
            float[] notes = { Eb2, Gb3 * 0.5f, Ab2 * 0.5f * 2f, Bb2, Db3 * 0.5f * 2f };
            return Make("music_investigation", 6f, t =>
            {
                float v = LowPassShared("inv", NoiseAt(t), 0.05f) * 0.10f + Sine(38.89f, t) * 0.06f;
                float beat = t % 1.5f;
                int idx = (int)(t / 1.5f) % notes.Length;
                if (beat < 0.9f)
                    v += Sine(notes[idx], t) * Env(beat, 0.9f, 0.005f) * 0.10f;
                return v;
            });
        }

        private static AudioClip AnomalyPulse()
        {
            return Make("music_anomaly", 4f, t =>
            {
                float pulse = Env(t % 1f, 0.35f, 0.005f);
                return Sine(40, t) * 0.30f * pulse +
                       (Sine(220, t) * 0.05f + Sine(223.5f, t) * 0.05f) * (0.4f + 0.6f * pulse) +
                       LowPassShared("an", NoiseAt(t), 0.10f) * 0.10f;
            });
        }

        private static AudioClip FinalDrone()
        {
            return Make("music_final", 8f, t =>
            {
                float rise = t / 8f;
                float f = 46 * (1f + rise * 1.5f);
                return Sine(f, t) * 0.18f + Sine(f * 1.5f, t) * 0.08f +
                       Sine(f * 2.02f, t) * 0.05f * rise +
                       (t % 1.1f < 0.3f ? Sine(50, t) * Env(t % 1.1f, 0.3f, 0.004f) * 0.5f * rise : 0f) +
                       LowPassShared("fin", NoiseAt(t), 0.06f) * 0.10f;
            });
        }

        private static AudioClip EndWin()
        {
            return Make("music_end_win", 9f, t =>
            {
                bool resolved = t > 4.5f;
                float root = resolved ? Gb2 * 0.5f : Eb1;
                float third = resolved ? Bb2 : Gb3 * 0.5f;
                float fifth = resolved ? Db3 : Bb2;
                float v = Sine(root, t) * 0.18f + Sine(third, t) * 0.10f + Sine(fifth, t) * 0.09f +
                          Sine(root * 2f, t) * 0.03f;
                return v * (0.5f + 0.5f * Mathf.Clamp01(resolved ? (t - 4.5f) / 2f : t / 2f));
            });
        }

        private static AudioClip EndLose()
        {
            return Make("music_end_lose", 9f, t =>
            {
                float fall = 1f - Mathf.Clamp01(t / 8f) * 0.35f;
                return (Sine(92.5f * fall, t) * 0.16f + Sine(98 * fall, t) * 0.14f + // minor 2nd rub
                        Sine(46.25f * fall, t) * 0.20f +
                        LowPassShared("lose", NoiseAt(t), 0.05f) * 0.22f * (1f - t / 9f));
            });
        }

        private static AudioClip EndSecret()
        {
            float[] bells = { Gb3, Db3 * 2f, Bb2 * 2f, Eb2 * 4f, Ab2 * 2f };
            return Make("music_end_secret", 10f, t =>
            {
                float v = Sine(46.25f, t) * 0.10f + LowPassShared("sec", NoiseAt(t), 0.03f) * 0.06f;
                float beat = t % 1.25f;
                int idx = (int)(t / 1.25f) % bells.Length;
                if (beat < 1.1f) v += Sine(bells[idx], t) * Env(beat, 1.1f, 0.004f) * 0.12f;
                return v;
            });
        }

        public static void ClearCache()
        {
            foreach (var kv in Cache)
                if (kv.Value != null) UnityEngine.Object.Destroy(kv.Value);
            Cache.Clear();
            _lpState.Clear();
        }
    }
}
````

### `_Project/Scripts/Bootstrap/SchoolSceneBootstrap.cs` (2.6 KiB)

````csharp
using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    // Builds the patrol scene. Attached to School.unity.
    public class SchoolSceneBootstrap : MonoBehaviour
    {
        public static void Build(GameRoot root)
        {
            var sceneRoot = CoreBootstrap.BeginSceneRoot("School");
            var fx = CoreBootstrap.NewCanvas(sceneRoot, "FXCanvas", 5, root.Config);
            var ui = CoreBootstrap.NewCanvas(sceneRoot, "UICanvas", 10, root.Config);
            var scare = CoreBootstrap.NewCanvas(sceneRoot, "ScareCanvas", 20, root.Config);

            root.Checkpoint = null;
            root.MapScreen = CoreBootstrap.CreateView<MapView>(ui, "MapView", root);
            root.ShiftResults = null;
            root.Ending = null;
            root.Loading = null;

            var worldGo = new GameObject("World");
            worldGo.transform.SetParent(sceneRoot.transform, false);
            worldGo.SetActive(false);

            var views = worldGo.AddComponent<World.ViewManager>();
            views.Init(root, root.MainCamera, worldGo);
            root.Views = views;

            var hallGo = new GameObject("Hallucinations");
            hallGo.transform.SetParent(sceneRoot.transform, false);
            root.Hallucinations = hallGo.AddComponent<Horror.HallucinationSystem>();
            root.Hallucinations.Init(root);

            var sanityRt = UIFactory.NewUI(fx.transform, "SanityFx");
            root.SanityCtl = sanityRt.gameObject.AddComponent<Horror.SanityController>();
            root.SanityCtl.Init(root, fx);

            var scareRt = UIFactory.NewUI(scare.transform, "ScareFx");
            root.Scares = scareRt.gameObject.AddComponent<Horror.ScareSystem>();
            root.Scares.Init(root, scare);

            root.HUD = CoreBootstrap.CreateView<HUDView>(ui, "HUDView", root);
            root.Dialogue = CoreBootstrap.CreateView<DialogueView>(ui, "DialogueView", root);
            root.PauseScreen = CoreBootstrap.CreateView<PauseView>(ui, "PauseView", root);
            root.Captions = CoreBootstrap.CreateView<CaptionBar>(ui, "CaptionBar", root);
            root.Confirm = CoreBootstrap.CreateView<ConfirmationPopup>(ui, "ConfirmationPopup", root);
            root.SecurityRoom = CoreBootstrap.CreateView<World.SecurityRoomView>(ui, "SecurityRoomView", root);

            UIFactory.ApplyTextScale(root.Settings.TextScaleMultiplier);
            root.RunQueuedSceneAction();
        }

        private void Awake() { CoreBootstrap.SafeBuild("School", () => Build(CoreBootstrap.EnsureCore())); }
    }
}
````

### `_Project/Scripts/Checkpoint/CheckpointLayout.cs` (2.4 KiB)

````csharp
using UnityEngine;

namespace Hollowbrook.Checkpoint
{
    // Where every desk panel sits, in 1280x720 reference pixels.
    // UIFactory.Anchor(parent, anchor, size, pos) treats pos as the CENTER of the element,
    // measured from the anchor point named in each comment below. Keeping them here means
    // the layout can be read and adjusted in one place instead of 20 call sites.
    public static class CheckpointLayout
    {
        public const float CheckRowStep = 16f;

        public static readonly Vector2 BannerCenter = new Vector2(170f, -56f);      // from top-left
        public static readonly Vector2 FacadeCenter = new Vector2(170f, -160f);    // from top-left
        public static readonly Vector2 ConfirmCenter = new Vector2(170f, -282f);   // from top-left
        public static readonly Vector2 RecordCenter = new Vector2(170f, -480f);    // from top-left
        public static readonly Vector2 WindowCenter = new Vector2(0f, -176f);     // from top-center
        public static readonly Vector2 PortraitCenter = new Vector2(0f, -148f);   // from top-center
        public static readonly Vector2 SpeechCenter = new Vector2(220f, -370f);   // from top-center
        public static readonly Vector2 QuestionsPanelCenter = new Vector2(-150f, -177f); // top-right; clears the 52px topbar
        public static readonly Vector2 QuestionsColumnCenter = new Vector2(-150f, -160f); // top-right; button stack centers on the panel
        public static readonly Vector2 UtilityCenterFirst = new Vector2(-225f, -340f);  // from top-right
        public const float UtilityStep = 90f;
        public static readonly Vector2 PatrolCenter = new Vector2(100f, 40f);     // from bottom-left
        public static readonly Vector2 EndShiftCenter = new Vector2(295f, 40f);   // from bottom-left
        public static readonly Vector2 NoteCenter = new Vector2(-80f, 120f);       // from bottom-CENTER (x is an offset)
        public static readonly Vector2 IdCardCenter = new Vector2(830f, 110f);     // from bottom-LEFT
        public static readonly Vector2 CameraCenter = new Vector2(-140f, 110f);   // from bottom-right
        public static readonly Vector2 AllowCenter = new Vector2(-215f, 250f);    // from bottom-right
        public static readonly Vector2 PrincipalCenter = new Vector2(-85f, 250f); // from bottom-right
        public static readonly Vector2 ScanCenter = new Vector2(-344f, 277f);     // from bottom-right
    }
}
````

### `_Project/Scripts/Checkpoint/CheckpointView.cs` (51.4 KiB)

````csharp
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.Checkpoint
{
    // The inspection desk: papers, questions, stamps, banners. Created by CheckpointSceneBootstrap.
    public class CheckpointView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private RectTransform _root;

        public VisitorData CurrentVisitor => _current;

        private List<VisitorData> _roster;
        private ShiftDef _shift;
        private int _index = -1;
        private VisitorData _current;
        private QuestionSystem.Budget _budget = new QuestionSystem.Budget();
        private int[] _checklist = new int[8]; // 0 = blank, 1 = ok, 2 = issue (8 rows)
        private bool _verdictLocked = true;
        private bool _flowActive;
        private bool _waitingReturnFromPatrol;

        private Text _topLeft, _topClock;
        private Text _runStats;      // endless/challenge: score · streak · timer
        private Text _mutationStrip; // challenge bulletin

        private Image _visitorPortrait;
        private RectTransform _speechPanel;
        private Text _speechSpeaker, _speechText;
        private Image _glitchOverlay;

        private Text _idName, _idNumber, _idYear, _idCourse, _idExpires, _idStatus, _idHeader;
        private Image _idPhoto;
        private Text _recName, _recNumber, _recYear, _recCourse, _recStatus, _recClassroom, _recHeader, _recEnrolled;
        private Text[] _checkRows = new Text[8];
        private Text _monitorOutput;
        private StringBuilder _tonightsLog = new StringBuilder();

        private Button[] _questionButtons = new Button[4];
        private Text _budgetLabel;
        private Button _allowBtn, _rejectBtn, _scanBtn, _notesBtn, _recordsBtn, _mapBtn, _patrolBtn, _endShiftBtn;
        private RectTransform _idCardRt, _recRt, _clipRt, _monRt, _qColRt;
        private Text _testimony;
        private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();
        private GameObject _confirmBox;
        private Text _confirmText, _noteText;

        private GameObject _bannerRoot; private Text _bannerTitle, _bannerBody;
        private Image _stampOverlay;
        private GameObject _notesRoot; private Text _notesText;
        private GameObject _recordsRoot; private Text _recordsText;

        private static readonly string[] ChecklistLabels =
        {
            "ID number format", "Photo matches person", "Expiration valid", "Year level matches",
            "Course matches", "Status matches", "Record name matches", "Behavior / answers",
        };

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            _root = (RectTransform)transform;
            for (int i = 0; i < _checklist.Length; i++) _checklist[i] = 0;

            var bg = UIFactory.Img(_root, "bg", ProceduralArt.Get("security_bg"));
            UIFactory.AnchorsFull(bg.rectTransform);
            bg.raycastTarget = false;

            BuildTopBar();
            BuildWindowArea();
            BuildDesk();
            BuildControls();
            BuildOverlays();

            _subscriptions.Add(_gameRoot.Bus.Subscribe(EventIds.RunTimerTick, e => OnRunTimerTick(e.Value)));
            _subscriptions.Add(_gameRoot.Bus.Subscribe(EventIds.MutationChanged, e =>
            {
                var m = e.Payload<RuleMutationDef>();
                if (_mutationStrip != null)
                {
                    _mutationStrip.text = m != null && !string.IsNullOrEmpty(m.RuleText)
                        ? "⚠ DISTRICT BULLETIN — " + m.RuleText : "";
                    _mutationStrip.gameObject.SetActive(m != null && !string.IsNullOrEmpty(m.RuleText));
                }
            }));
            BuildDecor();
            gameObject.SetActive(false);
        }

        private void BuildTopBar()
        {
            var bar = UIFactory.Panel(_root, "topbar", new Color(0.05f, 0.05f, 0.06f, 0.92f)).rectTransform;
            bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1);
            bar.pivot = new Vector2(0.5f, 1);
            bar.sizeDelta = new Vector2(0, 52); bar.anchoredPosition = Vector2.zero;

            _topLeft = UIFactory.Label(bar, "left", "", 19, UIFactory.Ink, TextAnchor.MiddleLeft);
            UIFactory.Anchor(_topLeft.rectTransform, new Vector2(0, 0.5f), new Vector2(640, 44), new Vector2(332, 0));

            _topClock = UIFactory.Label(bar, "clock", "22:00", 22, UIFactory.InkDim, TextAnchor.MiddleRight);
            UIFactory.Anchor(_topClock.rectTransform, new Vector2(1, 0.5f), new Vector2(160, 44), new Vector2(-140, 0));

            var pause = UIFactory.StandardButton(bar, "pause", "II  PAUSE", () => _gameRoot.TogglePause(true), 104, 34);
            UIFactory.Anchor(pause.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(104, 34), new Vector2(-60, 0));

            _runStats = UIFactory.Label(bar, "runstats", "", 17, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_runStats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(420, 40), new Vector2(60, 0));
            _runStats.gameObject.SetActive(false);

            _mutationStrip = UIFactory.Label(_root, "mutation", "", 15, new Color(0.95f, 0.85f, 0.55f), TextAnchor.MiddleCenter);
            var msRt = _mutationStrip.rectTransform;
            msRt.anchorMin = new Vector2(0, 1); msRt.anchorMax = new Vector2(1, 1);
            msRt.pivot = new Vector2(0.5f, 1);
            msRt.sizeDelta = new Vector2(-24, 34); msRt.anchoredPosition = new Vector2(0, -56);
            _mutationStrip.gameObject.SetActive(false);
        }

        private void BuildWindowArea()
        {
            _visitorPortrait = UIFactory.Img(_root, "visitor", null);
            UIFactory.Anchor(_visitorPortrait.rectTransform, new Vector2(0.5f, 1f), new Vector2(150, 190), CheckpointLayout.PortraitCenter);
            _visitorPortrait.preserveAspect = true;

            _glitchOverlay = UIFactory.Img(_root, "glitch", ProceduralArt.Get("noise_overlay"), new Color(1, 1, 1, 0f));
            UIFactory.Anchor(_glitchOverlay.rectTransform, new Vector2(0.5f, 1f), new Vector2(160, 200), new Vector2(0, -144));

            _speechPanel = UIFactory.Panel(_root, "speech", new Color(0.86f, 0.85f, 0.82f, 0.96f)).rectTransform;
            UIFactory.Anchor(_speechPanel, new Vector2(0.5f, 1f), new Vector2(300, 110), CheckpointLayout.SpeechCenter);
            _speechSpeaker = UIFactory.Label(_speechPanel, "who", "", 14, UIFactory.PaperInk, TextAnchor.UpperLeft);
            UIFactory.Anchor(_speechSpeaker.rectTransform, new Vector2(0, 1), new Vector2(310, 20), new Vector2(170, -14));
            _speechText = UIFactory.Label(_speechPanel, "text", "", 17, UIFactory.PaperInk, TextAnchor.UpperLeft);
            var stRt = _speechText.rectTransform;
            stRt.anchorMin = new Vector2(0, 0); stRt.anchorMax = new Vector2(1, 1);
            stRt.offsetMin = new Vector2(12, 8); stRt.offsetMax = new Vector2(-12, -36);
            _speechPanel.gameObject.SetActive(false);
        }

        private void BuildDesk()
        {
            var idCard = UIFactory.Img(_root, "idcard", ProceduralArt.Get("id_card_frame"));
            _idCardRt = idCard.rectTransform;
            UIFactory.Anchor(idCard.rectTransform, new Vector2(0, 0), new Vector2(286, 186), CheckpointLayout.IdCardCenter);
            _idHeader = UIFactory.Label(idCard.rectTransform, "header", "HOLLOWBROOK COLLEGE — STUDENT ID", 13,
                new Color(0.9f, 0.9f, 0.88f), TextAnchor.MiddleCenter);
            UIFactory.Anchor(_idHeader.rectTransform, new Vector2(0.5f, 1), new Vector2(270, 26), new Vector2(0, -14));
            _idPhoto = UIFactory.Img(idCard.rectTransform, "photo", null);
            UIFactory.Anchor(_idPhoto.rectTransform, new Vector2(0, 0), new Vector2(84, 104), new Vector2(58, 74));
            _idPhoto.preserveAspect = true;
            _idName = UIFactory.Label(idCard.rectTransform, "name", "", 14, UIFactory.PaperInk);
            UIFactory.Anchor(_idName.rectTransform, new Vector2(0, 0), new Vector2(170, 22), new Vector2(196, 152));
            _idNumber = UIFactory.Label(idCard.rectTransform, "num", "", 14, UIFactory.PaperInk);
            UIFactory.Anchor(_idNumber.rectTransform, new Vector2(0, 0), new Vector2(170, 22), new Vector2(196, 126));
            _idYear = UIFactory.Label(idCard.rectTransform, "year", "", 13, UIFactory.PaperInk);
            UIFactory.Anchor(_idYear.rectTransform, new Vector2(0, 0), new Vector2(170, 20), new Vector2(196, 102));
            _idCourse = UIFactory.Label(idCard.rectTransform, "course", "", 12, UIFactory.PaperInk);
            UIFactory.Anchor(_idCourse.rectTransform, new Vector2(0, 0), new Vector2(176, 20), new Vector2(199, 80));
            _idStatus = UIFactory.Label(idCard.rectTransform, "status", "", 13, UIFactory.PaperInk);
            UIFactory.Anchor(_idStatus.rectTransform, new Vector2(0, 0), new Vector2(170, 20), new Vector2(196, 58));
            _idExpires = UIFactory.Label(idCard.rectTransform, "exp", "", 13, UIFactory.PaperInk);
            UIFactory.Anchor(_idExpires.rectTransform, new Vector2(0, 0), new Vector2(200, 20), new Vector2(112, 30));

            var rec = UIFactory.Img(_root, "record", ProceduralArt.Get("record_sheet"));
            _recRt = rec.rectTransform;
            UIFactory.Anchor(rec.rectTransform, new Vector2(0f, 1f), new Vector2(320, 280), CheckpointLayout.RecordCenter);
            _recHeader = UIFactory.Label(rec.rectTransform, "header", "STUDENT RECORD — FORM 11-C", 13,
                new Color(0.9f, 0.9f, 0.88f), TextAnchor.MiddleCenter);
            UIFactory.Anchor(_recHeader.rectTransform, new Vector2(0.5f, 1), new Vector2(224, 30), new Vector2(0, -16));
            _recName = Row(rec.rectTransform, "rname", 0);
            _recNumber = Row(rec.rectTransform, "rnum", 1);
            _recYear = Row(rec.rectTransform, "ryear", 2);
            _recCourse = Row(rec.rectTransform, "rcourse", 3);
            _recStatus = Row(rec.rectTransform, "rstatus", 4);
            _recClassroom = Row(rec.rectTransform, "rroom", 5);
            _recEnrolled = Row(rec.rectTransform, "renrolled", 6);

            // respects Reduced Motion (no punch, instant) and never blocks clicks
            _stampOverlay = UIFactory.Img(rec.rectTransform, "stampoverlay", null, new Color(1, 1, 1, 0f));
            UIFactory.Anchor(_stampOverlay.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(132, 132), new Vector2(56, 100));
            _stampOverlay.rectTransform.localEulerAngles = new Vector3(0, 0, -12f);
            _stampOverlay.raycastTarget = false;

            for (int i = 0; i < 8; i++)
            {
                int row = i;
                var rb = UIFactory.StandardButton(rec.rectTransform, "check" + i, "[  ] " + ChecklistLabels[i],
                    () => CycleCheckRow(row), 280, 15);
                UIFactory.SetBaseFontSize(rb.GetComponentInChildren<Text>(), 11);
                rb.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                UIFactory.Anchor(rb.GetComponent<RectTransform>(), new Vector2(0f, 1f),
                    new Vector2(300, 15), new Vector2(160, -152 - i * CheckpointLayout.CheckRowStep));
                _checkRows[i] = rb.GetComponentInChildren<Text>();
            }
            var mon = UIFactory.Panel(_root, "monitor", new Color(0.06f, 0.07f, 0.06f)).rectTransform;
            _monRt = mon;
            UIFactory.Anchor(mon, new Vector2(1, 0), new Vector2(252, 150), CheckpointLayout.CameraCenter);
            var monBorder = UIFactory.Panel(mon, "border", UIFactory.Border).rectTransform;
            UIFactory.AnchorsFull(monBorder);
            var monInner = UIFactory.Panel(monBorder, "inner", new Color(0.03f, 0.04f, 0.03f)).rectTransform;
            monInner.anchorMin = Vector2.zero; monInner.anchorMax = Vector2.one;
            monInner.offsetMin = new Vector2(2, 2); monInner.offsetMax = new Vector2(-2, -2);
            _monitorOutput = UIFactory.Label(monInner, "out", "SCANNER READY", 13, new Color(0.55f, 0.75f, 0.55f));
            UIFactory.AnchorsFull(_monitorOutput.rectTransform);
            _monitorOutput.rectTransform.offsetMin = new Vector2(10, 8);
            _monitorOutput.rectTransform.offsetMax = new Vector2(-10, -8);
        }

        private Text Row(RectTransform parent, string name, int rowIndex)
        {
            const float HeaderHeight = 44f;
            const float RowHeight = 24f;
            var t = UIFactory.Label(parent, name, "", 13, UIFactory.PaperInk);
            UIFactory.Anchor(t.rectTransform, new Vector2(0, 1), new Vector2(220, 20),
                new Vector2(120, -(HeaderHeight + rowIndex * RowHeight)));
            return t;
        }

        private void BuildControls()
        {
            var qCol = UIFactory.NewUI(_root, "questions");
            _qColRt = qCol;
            UIFactory.Anchor(qCol, new Vector2(1, 1), new Vector2(280, 200), CheckpointLayout.QuestionsColumnCenter);
            _budgetLabel = UIFactory.Label(qCol, "budget", "QUESTIONS: 2 LEFT", 14, UIFactory.InkDim, TextAnchor.MiddleLeft);
            UIFactory.Anchor(_budgetLabel.rectTransform, new Vector2(0, 1), new Vector2(260, 24), new Vector2(130, -12));
            string[] qs =
            {
                "1 · Your course?", "2 · Your year?",
                "3 · Regular or transferee?", "4 · First classroom?",
            };
            var qids = QuestionSystem.All;
            for (int i = 0; i < 4; i++)
            {
                int qi = i;
                var b = UIFactory.StandardButton(qCol, "q" + i, qs[i], () => AskQuestion(qids[qi]), 260, 40);
                UIFactory.Anchor(b.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(272, 40), new Vector2(0f, -34 - i * 44));
                _questionButtons[i] = b;
            }

            _allowBtn = UIFactory.StandardButton(_root, "allow", "ALLOW", () => Stamp(Verdict.Allow), 120, 120);
            MakeRound(_allowBtn, true);
            UIFactory.Anchor(_allowBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(120, 120), CheckpointLayout.AllowCenter);
            _rejectBtn = UIFactory.StandardButton(_root, "reject", "CALL PRINCIPAL", () => AskPrincipal(), 120, 120, true);
            MakeRound(_rejectBtn, false);
            UIFactory.Anchor(_rejectBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(120, 120), CheckpointLayout.PrincipalCenter);
            _scanBtn = UIFactory.StandardButton(_root, "scan", "SCAN ID", () => DoScan(), 130, 38);
            UIFactory.Anchor(_scanBtn.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(130, 38), CheckpointLayout.ScanCenter);
            _notesBtn = UIFactory.StandardButton(_root, "notes", "RULES NOTE", () => ToggleOverlay(_notesRoot), 130, 38);
            UIFactory.Anchor(_notesBtn.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(130, 38), new Vector2(-344, 233));
            _recordsBtn = UIFactory.StandardButton(_root, "records", "TONIGHT'S LOG", () => { RefreshRecords(); ToggleOverlay(_recordsRoot); }, 130, 38);
            UIFactory.Anchor(_recordsBtn.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(130, 38), new Vector2(-344, 189));
            _mapBtn = UIFactory.StandardButton(_root, "map", "MAP", () => _gameRoot.OpenMap(), 130, 38);
            UIFactory.Anchor(_mapBtn.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(130, 38), new Vector2(-344, 145));

            _patrolBtn = UIFactory.StandardButton(_root, "patrol", "PATROL HALLWAY  ▸", () => GoPatrol(), 220, 44);
            UIFactory.Anchor(_patrolBtn.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(200, 40), CheckpointLayout.PatrolCenter);
            _endShiftBtn = UIFactory.StandardButton(_root, "endshift", "END SHIFT — CLOCK OUT", () => _gameRoot.FinishShift(), 250, 44);
            UIFactory.Anchor(_endShiftBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(210, 40), CheckpointLayout.EndShiftCenter);
            _endShiftBtn.gameObject.SetActive(false);
        }

        private void BuildOverlays()
        {
            _bannerRoot = new GameObject("Banner");
            var brt = UIFactory.NewUI(_bannerRoot.transform, "inner");
            _bannerRoot.transform.SetParent(_root, false);
            UIFactory.Anchor(brt, new Vector2(0.5f, 0.5f), new Vector2(640, 250), new Vector2(0, 60));
            var bimg = UIFactory.Panel(brt, "bg", new Color(0.04f, 0.04f, 0.05f, 0.97f));
            UIFactory.AnchorsFull(bimg.rectTransform);
            var border = UIFactory.Panel(brt, "borderline", UIFactory.Border).rectTransform;
            border.anchorMin = Vector2.zero; border.anchorMax = Vector2.one;
            border.offsetMin = new Vector2(2, 2); border.offsetMax = new Vector2(-2, -2);
            border.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.05f, 0.97f);
            _bannerTitle = UIFactory.Label(brt, "title", "", 34, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_bannerTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(600, 52), new Vector2(0, -34));
            _bannerBody = UIFactory.Label(brt, "body", "", 16, UIFactory.InkDim, TextAnchor.UpperCenter);
            var bbRt = _bannerBody.rectTransform;
            bbRt.anchorMin = new Vector2(0, 0); bbRt.anchorMax = new Vector2(1, 1);
            bbRt.offsetMin = new Vector2(24, 16); bbRt.offsetMax = new Vector2(-24, -70);
            _bannerRoot.SetActive(false);

            _notesRoot = BuildPaperOverlay("note_paper", "PAPER NOTE — INSPECTION RULES", out _notesText);
            _recordsRoot = BuildPaperOverlay("record_sheet", "TONIGHT'S LOG — ANOMALY RECORD", out _recordsText);
        }

        private GameObject BuildPaperOverlay(string spriteKey, string title, out Text body)
        {
            var go = new GameObject("Overlay_" + title);
            go.transform.SetParent(_root, false);
            UIFactory.Dimmer(go.transform, "dim", 0.6f);
            var panel = UIFactory.Img(go.transform, "paper", ProceduralArt.Get(spriteKey));
            UIFactory.Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(760, 480), Vector2.zero);
            var head = UIFactory.Label(panel.rectTransform, "title", title, 20, UIFactory.PaperInk, TextAnchor.MiddleCenter);
            UIFactory.Anchor(head.rectTransform, new Vector2(0.5f, 1), new Vector2(700, 40), new Vector2(0, -30));
            body = UIFactory.Label(panel.rectTransform, "body", "", 16, UIFactory.PaperInk);
            var bRt = body.rectTransform;
            bRt.anchorMin = new Vector2(0, 0); bRt.anchorMax = new Vector2(1, 1);
            bRt.offsetMin = new Vector2(40, 64); bRt.offsetMax = new Vector2(-40, -70);
            var close = UIFactory.StandardButton(panel.rectTransform, "close", "CLOSE [X]", () => go.SetActive(false), 140, 38);
            UIFactory.Anchor(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(140, 38), new Vector2(0, 26));
            go.SetActive(false);
            return go;
        }

        private void ToggleOverlay(GameObject go)
        {
            bool on = !go.activeSelf;
            go.SetActive(on);
            _gameRoot.Audio.PlaySfx(on ? "sfx_ui_open" : "sfx_ui_close", 0.5f);
            if (on && go == _notesRoot) RefreshNotes();
        }

        private void RefreshNotes()
        {
            var sb = new StringBuilder();
            sb.Append("BASE RULES — Every person presents a Student ID and record form.\n");
            sb.Append("Verify: ID format (HB-YYYY-NNNN) · photo vs. the person · expiration · year level · course/program · Regular/Transferee status · record name.\n");
            sb.Append("You may ask up to ").Append(Content.MaxQuestionsPerInspection).Append(" questions. Decisions are permanent.\n");
            sb.Append("SCAN cross-checks the student database and tonight's presentation log.\n\n");
            if (_shift != null) { sb.Append(_shift.RuleNoteAddendum).Append('\n'); }
            if (_gameRoot.Session.Mode == GameMode.Challenge)
            {
                var m = _gameRoot.Session.ActiveMutation;
                if (m != null && !string.IsNullOrEmpty(m.RuleText)) sb.Append('\n').Append("▸ ").Append(m.RuleText);
            }
            _notesText.text = sb.ToString();
        }

        private void RefreshRecords()
        {
            var sb = new StringBuilder();
            sb.Append("PRESENTED TONIGHT:\n");
            sb.Append(_tonightsLog.Length == 0 ? "  (no one yet)\n" : _tonightsLog.ToString());
            sb.Append("\nBREACHES (anomalies allowed in): ").Append(_gameRoot.Session.Breaches).Append(" / 3\n");
            sb.Append("FALSE REJECTIONS: ").Append(_gameRoot.Session.FalseRejections).Append(" / 3\n");
            sb.Append("FINDINGS DISCOVERED: ").Append(_gameRoot.Session.Clues.FoundCount).Append(" / 10\n");
            _recordsText.text = sb.ToString();
        }

        public void Show() { gameObject.SetActive(true); RefreshHud(); }
        public void Hide()
        {
            gameObject.SetActive(false);
            _bannerRoot.SetActive(false);
            _notesRoot.SetActive(false);
            _recordsRoot.SetActive(false);
        }

        public void BeginRun(List<VisitorData> roster, ShiftDef shift, int resumeFromVisitor = 0)
        {
            _index = resumeFromVisitor - 1;
            Debug.Log("[Flow] BeginRun roster=" + (roster != null ? roster.Count : -1));
            if (_noteText != null && _gameRoot.CurrentShiftDef != null) _noteText.text = _gameRoot.CurrentShiftDef.RuleNoteAddendum;
            _roster = roster;
            _shift = shift;
            _gameRoot.CurrentVisitorIndex = Mathf.Max(0, _index + 1);
            _tonightsLog.Clear();
            _flowActive = true;
            _endShiftBtn.gameObject.SetActive(false);
            _mutationStrip.gameObject.SetActive(false);
            if (_gameRoot.Session.Mode == GameMode.Challenge)
            {
                var m = _gameRoot.Session.ActiveMutation;
                if (m != null && !string.IsNullOrEmpty(m.RuleText))
                {
                    _mutationStrip.text = "⚠ DISTRICT BULLETIN — " + m.RuleText;
                    _mutationStrip.gameObject.SetActive(true);
                }
            }
            bool runMode = _gameRoot.Session.Mode != GameMode.Normal;
            _runStats.gameObject.SetActive(runMode);
            NextVisitor();
        }

        public void ResumeAfterExploration()
        {
            if (!_waitingReturnFromPatrol) return;
            _waitingReturnFromPatrol = false;
            StartCoroutine(DelayedNext(0.6f));
        }

        public void ResumeRun()
        {
            _shift = ShiftPlan.Get(Mathf.Clamp(_gameRoot.Session.CurrentShift, 1, ShiftPlan.TotalShifts));
            _roster = _gameRoot.CurrentRoster;
            _index = _gameRoot.CurrentVisitorIndex - 1;
            _tonightsLog.Clear();
            _flowActive = true;
            _waitingReturnFromPatrol = false;
            _endShiftBtn.gameObject.SetActive(false);
            _mutationStrip.gameObject.SetActive(false);
            bool runMode = _gameRoot.Session.Mode != GameMode.Normal;
            _runStats.gameObject.SetActive(runMode);
            Show();
            NextVisitor();
        }

        private IEnumerator DelayedNext(float s)
        {
            yield return WaitUnpaused(s);
            if (_flowActive && gameObject.activeInHierarchy) NextVisitor();
        }

        private IEnumerator WaitUnpaused(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                if (_gameRoot == null || !_gameRoot.Paused) t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void AskPrincipal()
        {
            if (_current == null || _verdictLocked) return;
            if (_confirmText != null) _confirmText.text = "PRINCIPAL... (Are you sure?)";
            if (_confirmBox != null) _confirmBox.SetActive(true);
        }

        private void MakeRound(Button b, bool paper)
        {
            var border = b.GetComponent<Image>();
            border.sprite = ProceduralArt.Get("rough_circle");
            border.color = paper ? UIFactory.Paper : new Color(0.10f, 0.10f, 0.11f);
            var inner = b.transform.GetChild(0).GetComponent<Image>();
            inner.sprite = ProceduralArt.Get("rough_circle");
            inner.color = paper ? UIFactory.Paper : new Color(0.13f, 0.13f, 0.14f);
            var t = b.GetComponentInChildren<Text>();
            UIFactory.SetBaseFontSize(t, 13);
            t.color = paper ? UIFactory.PaperInk : UIFactory.Ink;
        }

        // chalk decor from the art-direction sample: banner, facade, window, note, confirm box
        private void BuildDecor()
        {
            var banner = UIFactory.Img(_root, "daybanner", ProceduralArt.Get("rough_panel"));
            UIFactory.Anchor(banner.rectTransform, new Vector2(0f, 1f), new Vector2(300, 44), CheckpointLayout.BannerCenter);
            _topLeft.transform.SetParent(banner.rectTransform, false);
            UIFactory.AnchorsFull(_topLeft.rectTransform);
            _topLeft.color = UIFactory.PaperInk; UIFactory.SetBaseFontSize(_topLeft, 20);
            _topLeft.alignment = TextAnchor.MiddleCenter;
            var facade = UIFactory.Img(_root, "facade", ProceduralArt.Get("school_facade"));
            UIFactory.Anchor(facade.rectTransform, new Vector2(0f, 1f), new Vector2(300, 150), CheckpointLayout.FacadeCenter);
            var win = UIFactory.Img(_root, "windowframe", ProceduralArt.Get("window_frame"));
            UIFactory.Anchor(win.rectTransform, new Vector2(0.5f, 1f), new Vector2(470, 340), CheckpointLayout.WindowCenter);
            var glass = UIFactory.Panel(win.rectTransform, "glass", UIFactory.Glass);
            UIFactory.AnchorsFull(glass.rectTransform);
            glass.raycastTarget = false;
            var sill = UIFactory.Panel(_root, "sill", new Color(0.10f, 0.10f, 0.11f));
            UIFactory.Anchor(sill.rectTransform, new Vector2(0.5f, 1f), new Vector2(470, 34), new Vector2(0, -332));
            var note = UIFactory.Img(_root, "principalnote", ProceduralArt.Get("rough_panel"));
            UIFactory.Anchor(note.rectTransform, new Vector2(0.5f, 0f), new Vector2(240, 200), CheckpointLayout.NoteCenter - new Vector2(640f, 0f));
            var head = UIFactory.Label(note.rectTransform, "head", "PRINCIPAL NOTE", 14, UIFactory.PaperInk, TextAnchor.UpperLeft);
            UIFactory.Anchor(head.rectTransform, new Vector2(0f, 1f), new Vector2(230, 22), new Vector2(120, -16));
            _noteText = UIFactory.Label(note.rectTransform, "body", "", 12, UIFactory.PaperInk, TextAnchor.UpperLeft);
            UIFactory.Anchor(_noteText.rectTransform, new Vector2(0f, 1f), new Vector2(226, 140), new Vector2(118, -40));
            var qPanel = UIFactory.Img(_root, "qpanel", ProceduralArt.Get("rough_panel"));
            UIFactory.Anchor(qPanel.rectTransform, new Vector2(1f, 1f), new Vector2(300, 250), CheckpointLayout.QuestionsPanelCenter);
            var qHead = UIFactory.Label(qPanel.rectTransform, "head", "QUESTIONS", 18, UIFactory.PaperInk, TextAnchor.UpperLeft);
            UIFactory.Anchor(qHead.rectTransform, new Vector2(0f, 1f), new Vector2(280, 26), new Vector2(145, -14));
            var camFrame = UIFactory.Img(_monRt, "camframe", ProceduralArt.Get("camera_frame"));
            UIFactory.AnchorsFull(camFrame.rectTransform);
            camFrame.raycastTarget = false;
            var camCap = UIFactory.Label(_monRt, "camcap", "CAMERA", 13, UIFactory.InkDim, TextAnchor.MiddleCenter);
            UIFactory.Anchor(camCap.rectTransform, new Vector2(0.5f, 1f), new Vector2(240, 20), new Vector2(0, -20));
            var conf = UIFactory.Img(_root, "confirmbox", ProceduralArt.Get("rough_panel"));
            UIFactory.Anchor(conf.rectTransform, new Vector2(0f, 1f), new Vector2(300, 90), CheckpointLayout.ConfirmCenter);
            _confirmText = UIFactory.Label(conf.rectTransform, "txt", "", 14, UIFactory.PaperInk, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_confirmText.rectTransform, new Vector2(0.5f, 1f), new Vector2(280, 40), new Vector2(0, -18));
            var no = UIFactory.StandardButton(conf.rectTransform, "no", "No", () => _confirmBox.SetActive(false), 90, 30);
            UIFactory.Anchor(no.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(90, 30), new Vector2(-52, -64));
            var yes = UIFactory.StandardButton(conf.rectTransform, "yes", "Yes",
                () => { _confirmBox.SetActive(false); Stamp(Verdict.Reject); }, 90, 30);
            UIFactory.Anchor(yes.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(90, 30), new Vector2(52, -64));
            _confirmBox = conf.gameObject;
            _confirmBox.SetActive(false);
            // B3: the window frame is created after the portrait, and later siblings draw on
            // top, so lift the visitor, the glass glitch and the speech bubble above it.
            if (_visitorPortrait != null) _visitorPortrait.rectTransform.SetAsLastSibling();
            if (_glitchOverlay != null) _glitchOverlay.rectTransform.SetAsLastSibling();
            if (_speechPanel != null) _speechPanel.SetAsLastSibling();
            _testimony = UIFactory.Label(_qColRt, "testimony", "", 12, UIFactory.InkDim, TextAnchor.UpperLeft);
            UIFactory.Anchor(_testimony.rectTransform, new Vector2(0.5f, 1f), new Vector2(272, 86), new Vector2(0f, -290));
        }

        private void OnDestroy()
        {
            foreach (var subscription in _subscriptions) subscription?.Dispose();
            _subscriptions.Clear();
        }

        private void NextVisitor()
        {
            Debug.Log("[Flow] NextVisitor index=" + _index);
            if (_testimony != null) _testimony.text = "";
            if (_gameRoot.Config.exhibitionMode && _gameRoot.Session.Processed == 0 && _gameRoot.Session.CurrentShift == 1)
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    "EXHIBITION: photo and expiry first, then one question if in doubt."));
            if (!_flowActive) return;
            VisitorData v;
            if (_gameRoot.Session.Mode == GameMode.Normal)
            {
                _index++;
                if (_index >= _roster.Count)
                {
                    _current = null;
                    _verdictLocked = true;
                    SetVisitorVisible(false);
                    _endShiftBtn.gameObject.SetActive(true);
                    _patrolBtn.gameObject.SetActive(true);
                    SayLine("RADIO", "That's the last of them… for now. Clock out when the floor is clear.");
                    return;
                }
                v = _roster[_index];
            }
            else
            {
                v = _gameRoot.NextRunVisitor();
                _current = null;
            }

            _current = v;
            _budget = new QuestionSystem.Budget();
            for (int i = 0; i < _checklist.Length; i++) { _checklist[i] = 0; _checkRows[i].text = "[  ] " + ChecklistLabels[i]; _checkRows[i].color = UIFactory.PaperInk; }
            _monitorOutput.text = "SCANNER READY";
            _verdictLocked = false;
            _endShiftBtn.gameObject.SetActive(false);
            _patrolBtn.gameObject.SetActive(false);
            PresentVisitor(v);
            RefreshHud();
        }

        private void PresentVisitor(VisitorData v)
        {
            Debug.Log("[Flow] PresentVisitor " + v.ClaimedName);
            SetVisitorVisible(true);
            _visitorPortrait.sprite = ProceduralArt.Portrait("window", v.WindowPortrait, 240, 296);
            _idPhoto.sprite = ProceduralArt.Portrait("card", v.IdPhoto, 168, 208);
            if (_stampOverlay != null)
            {
                var sc = _stampOverlay.color; sc.a = 0f; _stampOverlay.color = sc;
                _stampOverlay.sprite = null;
            }
            var pc = _visitorPortrait.color; pc.a = 0f; _visitorPortrait.color = pc;
            StartCoroutine(PortraitFadeIn());

            bool admin = v.IsAdministrator;
            _idHeader.text = admin ? "DISTRICT STAFF BADGE" : "HOLLOWBROOK COLLEGE — STUDENT ID";
            _recHeader.text = admin ? "DISTRICT ROSTER MEMO" : "STUDENT RECORD — FORM 11-C";

            _idName.text = (admin ? "NAME: " : "NAME: ") + v.ClaimedName;
            _idNumber.text = (admin ? "BADGE NO: " : "ID NO: ") + v.IdNumber;
            if (admin)
            {
                _idYear.text = "DIVISION: Facilities Oversight";
                _idCourse.text = "AUTHORITY: District Office";
                _idStatus.text = "CLEARANCE: Full building";
                _recName.text = "ASSIGNED: " + v.RecordName;
                _recNumber.text = "BADGE NO: " + v.RecordIdNumber;
                _recYear.text = "EFFECTIVE: " + Content.ShiftDates[Mathf.Clamp(_gameRoot.Session.CurrentShift - 1, 0, 4)];
                _recCourse.text = "PURPOSE: Assume control of site";
                _recStatus.text = "SIGNED: District Seal (smudged)";
                _recClassroom.text = "ESCORT: None authorized";
                _recEnrolled.text = "FILED: 1987-10-05";
            }
            else
            {
                _idYear.text = "YEAR: " + Content.YearNames[v.IdYearLevel - 1];
                _idCourse.text = "COURSE: " + Content.Courses[v.IdCourseIndex];
                _idStatus.text = "STATUS: " + (v.IdIsTransferee ? "TRANSFEREE" : "REGULAR");
                _recName.text = "NAME: " + v.RecordName;
                _recNumber.text = "ID NO: " + v.RecordIdNumber;
                _recYear.text = "YEAR LEVEL: " + Content.YearNames[v.RecordYearLevel - 1];
                _recCourse.text = "COURSE/PROGRAM: " + Content.Courses[v.RecordCourseIndex];
                _recStatus.text = "STATUS: " + (v.RecordIsTransferee ? "TRANSFEREE" : "REGULAR");
                _recClassroom.text = "FIRST CLASS: " + v.RecordFirstClassroom;
                _recEnrolled.text = "EXPIRES: " + v.ExpirationDisplay;
            }
            _idExpires.text = admin ? "VALID THRU: " + v.ExpirationDisplay : "EXPIRES: " + v.ExpirationDisplay;

            _gameRoot.Audio.PlaySfx("sfx_knock", 0.7f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.DocumentsShown, v));
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.6f);

            if (admin)
            {
                var lines = new List<DialogueLine>
                {
                    new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = Content.AdminLines[0] },
                    new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = Content.AdminLines[1] },
                };
                if (_gameRoot.Session.Clues.FoundCount >= 6)
                    lines.Add(new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "That face. The old school photo — the staff member who never aged. It's them." });
                lines.Add(new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = Content.AdminLines[3] });
                SayLine("AT THE WINDOW", "A formal coat. A briefcase. A smile that arrives a half-second late.");
                if (_gameRoot.Dialogue != null) _gameRoot.Dialogue.Play(lines, null);
                _gameRoot.Audio.PlayMusic("final");
            }
            else
            {
                Say(v.Greeting);
                if (v.IsAnomaly && v.BehaviorGlitchVisible && _gameRoot.Session.Sanity < 70f)
                    StartCoroutine(GlitchHint());
            }
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.VisitorArrived, v));
        }

        private IEnumerator GlitchHint()
        {
            yield return new WaitForSecondsRealtime(1.6f);
            if (_current == null || !gameObject.activeInHierarchy) yield break;
            _glitchOverlay.gameObject.SetActive(true);
            var c = _glitchOverlay.color; c.a = 0.35f; _glitchOverlay.color = c;
            _gameRoot.Audio.PlaySfx("sfx_glitch", 0.35f);
            yield return new WaitForSecondsRealtime(0.12f);
            c = _glitchOverlay.color; c.a = 0f; _glitchOverlay.color = c;
            _glitchOverlay.gameObject.SetActive(false);
        }

        private void SetVisitorVisible(bool on)
        {
            _visitorPortrait.gameObject.SetActive(on);
            _speechPanel.gameObject.SetActive(on);
            _glitchOverlay.gameObject.SetActive(on && _glitchOverlay.color.a > 0f);
        }

        private void Say(string text) => SayLine("AT THE WINDOW", text);

        private void SayLine(string who, string text)
        {
            _speechSpeaker.text = who;
            _speechText.text = text;
            _speechPanel.gameObject.SetActive(true);
        }

        public void AskQuestion(QuestionId q)
        {
            if (_current == null || _verdictLocked) return;
            if (!_budget.TryAsk(_current, q, out var answer))
            {
                _monitorOutput.text = _budget.HasAsked(q) ? "ALREADY ASKED THAT." : "NO MORE QUESTIONS — DISTRICT POLICY.";
                _gameRoot.Audio.PlaySfx("sfx_scan_error", 0.35f);
                return;
            }
            string tone = answer.Tone == ToneTag.Normal ? "" : "  [" + ToneLabel(answer.Tone) + "]";
            SayLine("ANSWER · " + QuestionTextShort(q), "“" + answer.Text + "”" + tone);
            AppendTestimony(q, answer);
            _gameRoot.Audio.PlaySfx("sfx_intercom", 0.5f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.QuestionAsked, q));
            RefreshHud();
        }

        private float BannerSeconds => _gameRoot.Config.exhibitionMode ? _gameRoot.Config.exhibitionBannerSeconds : _gameRoot.Config.verdictBannerSeconds;
        private float BetweenSeconds => _gameRoot.Config.exhibitionMode ? _gameRoot.Config.exhibitionBetweenVisitors : _gameRoot.Config.betweenVisitorsSeconds;

        private void AppendTestimony(QuestionId q, Answer answer)
        {
            if (_testimony == null) return;
            string check = answer.ContradictsDocuments ? " x PAPERS DISAGREE"
                         : " = matches papers";
            string tone = answer.Tone == ToneTag.Normal ? "" : " (" + ToneLabel(answer.Tone).ToLowerInvariant() + " voice)";
            string line = "• " + QuestionTextShort(q) + ": " + check + tone;
            _testimony.text = string.IsNullOrEmpty(_testimony.text)
                ? line : _testimony.text + "\n" + line;
        }

        private static string ToneLabel(ToneTag t)
        {
            switch (t)
            {
                case ToneTag.Hesitant: return "hesitant";
                case ToneTag.Flat: return "flat, rehearsed";
                case ToneTag.TooSmooth: return "too smooth";
                default: return "echoing, as if reading";
            }
        }

        private static string QuestionTextShort(QuestionId q)
        {
            switch (q)
            {
                case QuestionId.Course: return "COURSE";
                case QuestionId.Year: return "YEAR";
                case QuestionId.Status: return "STATUS";
                default: return "FIRST CLASS";
            }
        }

        public void DoScan()
        {
            if (_current == null || _verdictLocked || _gameRoot.Roster == null) return;
            var res = _gameRoot.Roster.Scan(_current, _gameRoot.Session.TonightLog);
            _monitorOutput.text = res.Display;
            _monitorOutput.color = res.FoundInDatabase && !res.DuplicateTonight
                ? new Color(0.55f, 0.75f, 0.55f)
                : new Color(0.85f, 0.35f, 0.35f);
            _gameRoot.Audio.PlaySfx(res.FormatValid && res.FoundInDatabase && !res.DuplicateTonight ? "sfx_scan_ok" : "sfx_scan_error", 0.8f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.ScanPerformed, res));
        }

        private void CycleCheckRow(int row)
        {
            if (_current == null || _verdictLocked) return;
            _checklist[row] = (_checklist[row] + 1) % 3;
            switch (_checklist[row])
            {
                case 1: _checkRows[row].text = "[✓] " + ChecklistLabels[row]; _checkRows[row].color = new Color(0.1f, 0.35f, 0.12f); break;
                case 2: _checkRows[row].text = "[✗] " + ChecklistLabels[row]; _checkRows[row].color = new Color(0.55f, 0.1f, 0.12f); break;
                default: _checkRows[row].text = "[  ] " + ChecklistLabels[row]; _checkRows[row].color = UIFactory.PaperInk; break;
            }
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.35f);
        }

        public void Stamp(Verdict verdict)
        {
            if (_current == null || _verdictLocked) return;
            _verdictLocked = true; // §2.4 — decisions are permanent
            var mutation = _gameRoot.Session.Mode == GameMode.Challenge ? _gameRoot.Session.ActiveMutation : null;
            var outcome = InspectionRules.Apply(_current, verdict, mutation);

            _gameRoot.Audio.PlaySfx(verdict == Verdict.Allow ? "sfx_stamp_allow" : "sfx_stamp_reject", 1f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.VerdictStamped, outcome));
            if (_stampOverlay != null) StartCoroutine(StampPunch(verdict));

            _tonightsLog.Append("  ").Append(_current.IsAdministrator ? "ADMINISTRATOR" : _current.ClaimedName)
                .Append(" — ").Append(verdict == Verdict.Allow ? "ALLOWED" : "REJECTED")
                .Append(outcome.Correct ? " (correct)\n" : " (MISTAKE)\n");

            int checklistHits = CountChecklistAccuracy(_current, outcome);
            var resolved = _current;
            _gameRoot.CurrentVisitorIndex = _index + 1;               // survive scene reloads (patrol)
            _gameRoot.OnVerdictResolved(resolved, outcome);           // counters update BEFORE the banner reads them
            if (_gameRoot.State == AppViewState.Ending) { Hide(); return; } // ending took over (C3/C4)
            ShowBanner(verdict, outcome, checklistHits);

            StartCoroutine(AfterBanner(resolved));
        }

        private int CountChecklistAccuracy(VisitorData v, InspectionRules.Outcome o)
        {
            bool[] truth = GroundTruthRows(v);
            int hits = 0;
            for (int i = 0; i < 8; i++)
            {
                if (_checklist[i] == 0) continue;
                bool markedIssue = _checklist[i] == 2;
                if (markedIssue == truth[i]) hits++;
            }
            return hits;
        }

        private bool[] GroundTruthRows(VisitorData v)
        {
            bool[] t = new bool[8];
            t[0] = !IdRules.IsFormatValid(v.IdNumber);
            t[1] = !v.PhotoMatches;
            t[2] = IdRules.IsExpired(v, v.ShiftNumber);
            t[3] = v.IdYearLevel != v.RecordYearLevel;
            t[4] = v.IdCourseIndex != v.RecordCourseIndex;
            t[5] = v.IdIsTransferee != v.RecordIsTransferee;
            t[6] = v.ClaimedName != v.RecordName;
            t[7] = v.Defects.Contains(DefectType.BehaviorOddity);
            return t;
        }

        private void ShowBanner(Verdict verdict, InspectionRules.Outcome o, int checklistHits)
        {
            string title = verdict == Verdict.Allow ? "ACCESS GRANTED" : "ACCESS DENIED";
            _bannerTitle.text = title;
            _bannerTitle.color = verdict == Verdict.Allow ? UIFactory.Ink : UIFactory.AccentBright;

            var sb = new StringBuilder();
            if (o.Correct) sb.Append("CORRECT DECISION.  (+").Append(o.WasAnomaly ? 100 : 50).Append(")\n");
            else if (o.FalseRejection) sb.Append("MISTAKE — FALSE REJECTION. A real student was turned away. (")
                    .Append(_gameRoot.Session.FalseRejections).Append("/3 before the district reviews your contract)\n");
            else sb.Append("MISTAKE — ANOMALY ENTRY. Something is inside the school now. (")
                    .Append(_gameRoot.Session.Breaches).Append("/3 before the district reviews your contract)\n");
            sb.Append("\nWHAT THE DOCUMENTS SHOWED:\n");
            foreach (var line in o.RevealedDefects) sb.Append("  · ").Append(line).Append('\n');
            sb.Append("\nYour checklist: ").Append(checklistHits).Append("/8 rows accurate.");
            _bannerBody.text = sb.ToString();
            _bannerRoot.SetActive(true);
        }

        private IEnumerator StampPunch(Verdict verdict)
        {
            _stampOverlay.sprite = ProceduralArt.Get(verdict == Verdict.Allow ? "stamp_allow" : "stamp_reject");
            bool reduce = _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;
            if (!reduce)
            {
                float t = 0f;
                while (t < 0.16f)
                {
                    t += Time.unscaledDeltaTime;
                    float k = 1.7f - 0.7f * Mathf.Clamp01(t / 0.16f);
                    _stampOverlay.rectTransform.localScale = new Vector3(k, k, 1f);
                    var c0 = _stampOverlay.color; c0.a = Mathf.Clamp01(t / 0.06f); _stampOverlay.color = c0;
                    yield return null;
                }
            }
            _stampOverlay.rectTransform.localScale = Vector3.one;
            var c = _stampOverlay.color; c.a = 1f; _stampOverlay.color = c;
            yield return new WaitForSecondsRealtime(1.8f);
            float f = 0f;
            while (f < 0.5f)
            {
                f += Time.unscaledDeltaTime;
                var cf = _stampOverlay.color; cf.a = 1f - Mathf.Clamp01(f / 0.5f); _stampOverlay.color = cf;
                yield return null;
            }
            var ce = _stampOverlay.color; ce.a = 0f; _stampOverlay.color = ce;
        }

        private IEnumerator PortraitFadeIn()
        {
            bool reduce = _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;
            if (reduce)
            {
                var cr = _visitorPortrait.color; cr.a = 1f; _visitorPortrait.color = cr;
                yield break;
            }
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                var c = _visitorPortrait.color; c.a = Mathf.Clamp01(t / 0.3f); _visitorPortrait.color = c;
                yield return null;
            }
            var c2 = _visitorPortrait.color; c2.a = 1f; _visitorPortrait.color = c2;
        }

        private IEnumerator AfterBanner(VisitorData resolved)
        {
            yield return WaitUnpaused(BannerSeconds);
            _bannerRoot.SetActive(false);
            if (_gameRoot.State != AppViewState.Checkpoint && _gameRoot.State != AppViewState.AdminEncounter) yield break;

            if (resolved.IsAdministrator && _gameRoot.Session.AdminRejected)
            {
                var outro = new List<DialogueLine>
                {
                    new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = "…You will regret this. We are already inside your records." },
                    new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "The thing wearing Marlowe's face unfolds itself from the window and walks back into the dark. The seal on the server room holds — for tonight." },
                };
                _gameRoot.Dialogue.Play(outro, () =>
                {
                    SetVisitorVisible(false);
                    _endShiftBtn.gameObject.SetActive(true);
                    _patrolBtn.gameObject.SetActive(true);
                    Bus_MusicCheckpoint();
                });
                yield break;
            }

            if (_gameRoot.Session.Mode == GameMode.Normal)
            {
                bool rosterDone = _index + 1 >= _roster.Count;
                if (rosterDone)
                {
                    SetVisitorVisible(false);
                    _endShiftBtn.gameObject.SetActive(true);
                    _patrolBtn.gameObject.SetActive(true);
                    SayLine("RADIO", "That's the last of them… for now. Clock out when the floor is clear.");
                }
                else
                {
                    _patrolBtn.gameObject.SetActive(true);
                    SayLine("RADIO", "Next one's crossing the yard. Patrol now if you need to.");
                    yield return new WaitForSecondsRealtime(BetweenSeconds + 2.2f);
                    if (_flowActive && !_waitingReturnFromPatrol && _gameRoot.State == AppViewState.Checkpoint)
                        NextVisitor();
                }
            }
            else
            {
                SetVisitorVisible(false);
                yield return WaitUnpaused(Mathf.Max(0.4f, BetweenSeconds * 0.5f));
                if (_flowActive && _gameRoot.State == AppViewState.Checkpoint) NextVisitor();
            }
        }

        private void Bus_MusicCheckpoint() => _gameRoot.Audio.PlayMusic("checkpoint");

        public void GoPatrol()
        {
            if (_current != null && !_verdictLocked)
            {
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification, "Finish the inspection at the window first."));
                return;
            }
            _waitingReturnFromPatrol = true;
            _gameRoot.EnterExploration();
        }

        private void Update()
        {
            if (_gameRoot == null || !gameObject.activeInHierarchy) return;
            if (Input.GetKeyDown(KeyCode.Escape) && !_bannerRoot.activeSelf) _gameRoot.TogglePause();
            if (Input.GetKeyDown(KeyCode.M) && _gameRoot.State == AppViewState.Checkpoint) _gameRoot.OpenMap();
        }

        private void RefreshHud()
        {
            if (_gameRoot == null || _gameRoot.Session == null) return;
            if (_gameRoot.Session.Mode == GameMode.Normal)
            {
                int total = _roster != null ? _roster.Count : 0;
                int shown = Mathf.Clamp(_index + 1, 0, total);
                _topLeft.text = "NIGHT " + _gameRoot.Session.CurrentShift + " · " + (_shift != null ? _shift.DateLabel : "") +
                                "   —   VISITOR " + shown + " / " + total;
                _topClock.text = _gameRoot.ClockString;
            }
            else
            {
                _topLeft.text = (_gameRoot.Session.Mode == GameMode.Endless ? "ENDLESS WATCH" : "CHALLENGE PROTOCOL") +
                                "   ·   PROCESSED " + _gameRoot.Session.RunProcessed;
                _topClock.text = _gameRoot.ClockString;
                float mult = EndlessRules.StreakMultiplier(_gameRoot.Session.Streak) *
                             (_gameRoot.Session.Mode == GameMode.Challenge ? ChallengeRules.ScoreMultiplier : 1f);
                _runStats.text = "SCORE " + _gameRoot.Session.Score + "   ·   STREAK " + _gameRoot.Session.Streak + " (×" + mult.ToString("0.0") + ")";
            }
            _budgetLabel.text = "QUESTIONS: " + _budget.Remaining + " LEFT";
            for (int i = 0; i < 4; i++)
                UIFactory.SetButtonEnabled(_questionButtons[i], _current != null && !_verdictLocked && !_budget.HasAsked(QuestionSystem.All[i]) && _budget.Remaining > 0);
            UIFactory.SetButtonEnabled(_allowBtn, _current != null && !_verdictLocked);
            UIFactory.SetButtonEnabled(_rejectBtn, _current != null && !_verdictLocked);
        }

        public void OnRunTimerTick(float secondsLeft)
        {
            if (_runStats == null || !_runStats.gameObject.activeInHierarchy) return;
            int m = (int)(secondsLeft / 60f), s = (int)(secondsLeft % 60f);
            _runStats.text = "TIME " + m + ":" + s.ToString("D2") + "   ·   SCORE " + _gameRoot.Session.Score +
                             "   ·   STREAK ×" + EndlessRules.StreakMultiplier(_gameRoot.Session.Streak).ToString("0.0");
            _runStats.color = secondsLeft < 30f ? UIFactory.AccentBright : UIFactory.Ink;
        }

        private void OnEnable()
        {
            if (_gameRoot != null && _runStats != null && _gameRoot.Session.Mode != GameMode.Normal)
                _runStats.gameObject.SetActive(true);
        }
    }
}
````

### `_Project/Scripts/Core/AspectMatchGuard.cs` (1.2 KiB)

````csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;   // CanvasScaler lives in UnityEngine.UI (com.unity.ugui), not UnityEngine

namespace Hollowbrook.Core
{
    // Keeps every canvas at or above the 1280x720 reference on any aspect.
    public class AspectMatchGuard : MonoBehaviour
    {
        private CanvasScaler _scaler;
        private int _w, _h;

        public const float ReferenceAspect = 1280f / 720f;

        public void Init(CanvasScaler scaler)
        {
            _scaler = scaler;
            Apply();
        }

        private void Update()
        {
            if (Screen.width != _w || Screen.height != _h) Apply();
        }

        public void Apply()
        {
            _w = Screen.width; _h = Screen.height;
            if (_scaler == null || _h <= 0) return;
            float aspect = _w / (float)_h;
            _scaler.matchWidthOrHeight = aspect > ReferenceAspect ? 1f : 0f;
        }

        public Vector2 VisibleReferenceSize()
        {
            float scale = _scaler != null && _scaler.matchWidthOrHeight > 0.5f
                ? _h / 720f
                : _w / 1280f;
            if (scale <= 0f) scale = 1f;
            return new Vector2(_w / scale, _h / scale);
        }
    }
}
````

### `_Project/Scripts/Core/EventBus.cs` (3.1 KiB)

````csharp
using System;

namespace Hollowbrook.Core
{
    public struct GameEvent
    {
        public string Id;
        public object Data;   // typed payload (use Payload<T>())
        public float Value;   // numeric payload (sanity, timer, volume…)
        public string Text;   // text payload (caption, objective, sfx key…)

        public T Payload<T>() => Data is T t ? t : default;

        public static GameEvent Of(string id) => new GameEvent { Id = id };
        public static GameEvent Of(string id, object data) => new GameEvent { Id = id, Data = data };
        public static GameEvent Of(string id, float value) => new GameEvent { Id = id, Value = value };
        public static GameEvent Of(string id, string text) => new GameEvent { Id = id, Text = text };
        public static GameEvent Of(string id, object data, float value, string text) =>
            new GameEvent { Id = id, Data = data, Value = value, Text = text };

        public override string ToString() => "GameEvent(" + Id + ")";
    }

    public sealed class EventSubscription : IDisposable
    {
        private EventBus _bus;
        private readonly string _id;
        private readonly Action<GameEvent> _handler;

        internal EventSubscription(EventBus bus, string id, Action<GameEvent> handler)
        { _bus = bus; _id = id; _handler = handler; }

        public void Dispose()
        {
            var b = _bus;
            _bus = null; // idempotent
            if (b != null) b.Remove(_id, _handler);
        }
    }

    public sealed class EventBus
    {
        private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Action<GameEvent>>> _subs
            = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Action<GameEvent>>>();

        public EventSubscription Subscribe(string id, Action<GameEvent> handler)
        {
            if (string.IsNullOrEmpty(id) || handler == null) return null;
            if (!_subs.TryGetValue(id, out var list))
            {
                list = new System.Collections.Generic.List<Action<GameEvent>>();
                _subs[id] = list;
            }
            list.Add(handler);
            return new EventSubscription(this, id, handler);
        }

        internal void Remove(string id, Action<GameEvent> handler)
        {
            if (_subs.TryGetValue(id, out var list)) list.Remove(handler);
        }

        public void Publish(GameEvent e)
        {
            if (string.IsNullOrEmpty(e.Id)) return;
            if (!_subs.TryGetValue(e.Id, out var list) || list.Count == 0) return;
            var snapshot = list.ToArray();
            foreach (var h in snapshot)
            {
                try { h(e); }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError("[EventBus] handler failed for '" + e.Id + "': " + ex);
                }
            }
        }

        public void Publish(string id) => Publish(GameEvent.Of(id));

        public void Clear() => _subs.Clear();

        public int SubscriberCount(string id) => _subs.TryGetValue(id, out var l) ? l.Count : 0;
    }
}
````

### `_Project/Scripts/Core/EventIds.cs` (2.7 KiB)

````csharp
namespace Hollowbrook.Core
{
    public static class EventIds
    {
        public const string StateChanged = "state.changed";

        public const string ObjectiveChanged = "objective.changed";// Text: objective line
        public const string RunTimerTick = "run.timer_tick";       // Value: seconds left
        public const string MutationChanged = "run.mutation";      // Data: RuleMutationDef

        public const string VerdictStamped = "visitor.verdict";    // Data: InspectionRules.Outcome

        public const string RoomEntered = "room.entered";          // Data: RoomId
        public const string LightModeChanged = "light.mode";       // Data: LightMode
        public const string ClueDiscovered = "clue.discovered";    // Data: ClueDiscoveryResult
        public const string InteractionPrompt = "interact.prompt"; // Text: prompt or ""

        public const string SanityChanged = "sanity.changed";      // Value: 0..100
        public const string SanityStageChanged = "sanity.stage";   // Data: SanityStage
        public const string ScareTriggered = "scare.triggered";    // Text: kind
        public const string HallucinationTriggered = "hallucination.triggered"; // Text: kind

        public const string CaptionRequested = "caption.show";     // Text: caption, Value: seconds
        public const string SettingsChanged = "settings.changed";  // Data: SettingsData
        public const string Notification = "ui.notification";
        public const string PauseToggled = "pause.toggled";
        public const string GameSaved = "save.written";
        public const string GameLoaded = "save.loaded";
        public const string ShiftStarted = "shift.started";
        public const string ShiftEnded = "shift.ended";
        public const string EndingReached = "ending.reached";
        public const string VisitorArrived = "desk.visitor";
        public const string DocumentsShown = "desk.docs";
        public const string QuestionAsked = "desk.question";
        public const string ScanPerformed = "desk.scan";
        public const string VisitorResolved = "desk.resolved";
        public const string RoomExited = "room.exited";
        public const string AnomalySighted = "anomaly.sighted";
        public const string AnomalyCaught = "anomaly.caught";      // Text: toast message
        public const string TrackingChanged = "anomaly.tracking";    // Value: 0..1 meter
        public const string AnomalyRevealed = "anomaly.revealed";    // Data: kind / "hallucination"
        public const string TokenCollected = "anomaly.token";      // Value: tokens held this run
        public const string BatteryChanged = "light.battery";      // Value: 0..1 charge
        public const string AnomalyStateChanged = "anomaly.state"; // Data: AnomalyState name
    }
}
````

### `_Project/Scripts/Core/GameConfigSO.cs` (3.3 KiB)

````csharp
using UnityEngine;

namespace Hollowbrook.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ShowMeYourID/Game Config")]
    // Tuning knobs editable in one asset. Loaded by CoreBootstrap.
    public class GameConfigSO : ScriptableObject
    {
        [Header("Checkpoint pacing")]
        [Tooltip("Seconds the result banner stays up after a verdict.")]
        public float verdictBannerSeconds = 3.2f;
        [Tooltip("Seconds between visitors (breathing room / dread pacing).")]
        public float betweenVisitorsSeconds = 1.6f;
        [Tooltip("In-story shift starts at 22:00 and ends at 02:00.")]
        public int shiftStartHour = 22;

        [Header("Exploration")]
        public float walkSpeed = 3.4f;
        public float runSpeed = 6.0f;
        public float interactRange = 1.35f;
        public float roomHalfWidthHallway = 14f;
        public float roomHalfWidthClassroom = 9f;

        [Header("Flashlight battery")]
        [Tooltip("Percent per second while the wide Sweep beam is on.")]
        public float batteryDrainSweepPerSecond = 2.5f;
        [Tooltip("Percent per second while the tight Focus beam is on.")]
        public float batteryDrainFocusPerSecond = 6f;
        [Tooltip("Percent per second recovered in darkness with the beam off.")]
        public float batteryRecoverOffPerSecond = 4f;
        [Tooltip("Percent per second recovered with the beam off in a lit (safe) room.")]
        public float batteryRecoverLitPerSecond = 8f;
        [Tooltip("Charge needed before a dead beam can switch on again (no soft-lock).")]
        [Range(0f, 1f)] public float batteryRebootFraction = 0.2f;

        [Header("Horror pacing")]
        [Tooltip("Base chance per second that a wandering anomaly spawns in an entered room (shift-scaled).")]
        public float anomalySpawnChancePerSecond = 0.05f;
        public float coverHugCatchSeconds = 2.6f;
        public float sprintTelegraphSeconds = 0.9f;

        [Header("Exhibition (5-10 minute booth cut)")]
        [Tooltip("One curated night (4 visitors, 1 anomaly) then the demo closes. Off = full five-shift story.")]
        public bool exhibitionMode = true;
        public float exhibitionBannerSeconds = 2.2f;
        public float exhibitionBetweenVisitors = 1.0f;

        [Header("Shift clock")]
        [Tooltip("In-story minutes per real second while a shift is running, in ANY scene.")]
        public float clockMinutesPerSecond = 0.25f;

        [Header("Presentation")]
        public int canvasReferenceWidth = 1280;
        public int canvasReferenceHeight = 720;
        public float dialogueCharsPerSecond = 42f;

        public static GameConfigSO CreateDefault()
        {
            var c = CreateInstance<GameConfigSO>();
            c.name = "GameConfig (procedural default)";
            return c;
        }

        public static GameConfigSO LoadOrDefault()
        {
            GameConfigSO loaded = null;
            try { loaded = Resources.Load<GameConfigSO>("GameConfig"); }
            catch (System.Exception e) { Debug.LogWarning("[GameConfig] Resources.Load failed: " + e.Message); }
            if (loaded == null)
            {
                loaded = CreateDefault();
                Debug.Log("[GameConfig] No asset found — using procedural defaults.");
            }
            return loaded;
        }
    }
}
````

### `_Project/Scripts/Core/GameRoot.cs` (30.1 KiB)

````csharp
using System.Collections.Generic;
using Hollowbrook.Audio;
using Hollowbrook.Bootstrap;
using Hollowbrook.Checkpoint;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using Hollowbrook.World;
using UnityEngine;

namespace Hollowbrook.Core
{
    /// <summary>Central game orchestrator (the "root"). Owns services, the run session and
    /// explicit references to every view — assigned once by <see cref="GameBootstrap"/>.
    /// Gameplay code never uses GameObject.Find; it goes through this class or the EventBus.</summary>
    public class GameRoot : MonoBehaviour
    {
        // Flip to false to silence the [Flow] breadcrumb logs once the boot flow is trusted.
        private const bool VerboseFlow = true;
        public static GameRoot Instance { get; private set; }

        /// <summary>Menu gate for Endless/Challenge (run modes). The systems are fully
        /// implemented and headless-tested, but the menu keeps them LOCKED placeholders
        /// until the team flips this to true (structure is ready for later — per current
        /// scope: New Game → Normal story only).</summary>
        public static bool RunModesUnlocked = false;

        // ---- services ----
        public EventBus Bus { get; private set; }
        public GameConfigSO Config { get; private set; }
        public SaveSystem Saves { get; private set; }
        public SettingsSystem Settings { get; private set; }
        public AudioDirector Audio { get; private set; }

        // ---- run state ----
        public GameSession Session { get; private set; } = new GameSession();
        public RecordsData Records { get; private set; } = new RecordsData();
        public RosterFactory Roster { get; private set; }
        public List<VisitorData> CurrentRoster { get; private set; } = new List<VisitorData>();
        public ShiftDef CurrentShiftDef { get; private set; }
        public AppViewState State { get; private set; } = AppViewState.Boot;
        public bool Paused { get; private set; }
        public int EndlessProcessed { get; private set; }

        // ---- views (explicit references, set by GameBootstrap) ----
        public Camera MainCamera;
        public ViewManager Views;
        public CheckpointView Checkpoint;
        public HUDView HUD;
        public DialogueView Dialogue;
        public MainMenuView MainMenu;
        public ModeSelectView ModeSelect;
        public SettingsView SettingsScreen;
        public PauseView PauseScreen;
        public ControlsView ControlsScreen;
        public CreditsView CreditsScreen;
        public MapView MapScreen;
        public LoadingView Loading;
        public EndingView Ending;
        public ShiftResultsView ShiftResults;
        public ConfirmationPopup Confirm;
        public CaptionBar Captions;
        public Hollowbrook.World.SecurityRoomView SecurityRoom;

        /// <summary>Roster cursor shared across scene reloads (patrol ↔ checkpoint).</summary>
        public int CurrentVisitorIndex = 0;

        private System.Action _queuedSceneAction;
        public void QueueSceneAction(System.Action a)
        {
            _queuedSceneAction = a;
            _queuedAt = Time.unscaledTime;
            _handshakeArmed = true;
        }

        public void ClearQueuedSceneAction() { _queuedSceneAction = null; _handshakeArmed = false; }

        private float _queuedAt;
        private bool _handshakeArmed;

        // Safety net: if a scene load never runs its bootstrap (missing bootstrap component,
        // scene not in Build Settings), the queued start action is never consumed and the
        // player would stare at a dead screen. After the window we say exactly that and go
        // back to the menu. Do not remove: this is the only detector for that failure.
        private void CheckSceneHandshake()
        {
            if (!_handshakeArmed || _queuedSceneAction == null) return;
            if (Time.unscaledTime - _queuedAt < HandshakeTimeoutSeconds) return;
            _handshakeArmed = false;
            _queuedSceneAction = null;
            Debug.LogError("[GameRoot] scene handshake FAILED: the queued start action was never " +
                "consumed. Usual causes: target scene not enabled in Build Settings, or its " +
                "bootstrap component missing / stale script GUID (Tools/generate_meta.py pins them).");
            Bus.Publish(GameEvent.Of(EventIds.Notification,
                "The game scene failed to start. Check Build Settings / scene bootstrap."));
            Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.Menu);
        }

        private const float HandshakeTimeoutSeconds = 3.5f;

        public void RunQueuedSceneAction()
        {
            Debug.Log("[Flow] queued action consumed in scene '" +
                      UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "'");
            var a = _queuedSceneAction;
            _queuedSceneAction = null;
            a?.Invoke();
        }
        public SanityController SanityCtl;
        public ScareSystem Scares;
        public HallucinationSystem Hallucinations;

        // shift clock (22:00 → 02:00): real time x Config rate in any in-shift scene,
        // plus a bump per processed visitor. Atmosphere only — dawn never ends a shift early.
        private float _shiftMinutes;
        /// <summary>Last published objective, so a HUD built after the publish (scene load)
        /// can show it immediately instead of staying blank.</summary>
        public string ObjectiveText { get; private set; } = "";

        /// <summary>Advance the in-story shift clock. Public so headless suites can drive it
        /// deterministically; Update is the only real-time caller.</summary>
        public void AdvanceShiftClock(float minutes) { _shiftMinutes += minutes; }
        public string ClockString
        {
            get
            {
                int total = (22 * 60 + (int)_shiftMinutes) % (24 * 60);
                return (total / 60).ToString("D2") + ":" + (total % 60).ToString("D2");
            }
        }

        private GameRng _runRng;
        private int _resumeFromVisitor;

        // ---------------------------------------------------------------- lifecycle

        public void InitServices(EventBus bus, GameConfigSO config, SaveSystem saves,
            SettingsSystem settings, AudioDirector audio)
        {
            Instance = this;
            Bus = bus; Config = config; Saves = saves; Settings = settings; Audio = audio;
            DontDestroyOnLoad(gameObject);
            // One boot-time application of the stored display preference. Missing or unusable
            // preferences are cleared inside; the launcher default otherwise stands.
            settings.ApplyStoredDisplay();
            Bus.Subscribe(EventIds.SettingsChanged, e =>
            {
                UIFactory.ApplyTextScale(Settings.TextScaleMultiplier);
                if (HUD != null) HUD.Refresh();
            });
        }

        private void Update()
        {
            CheckSceneHandshake();
            if (Paused || Session == null) return;

            // The shift clock runs on patrol too (scope: patrol has stakes). Single source:
            // real time x Config rate, in whichever scene the night is happening.
            if (State == AppViewState.Checkpoint || State == AppViewState.Exploration ||
                State == AppViewState.SecurityRoom || State == AppViewState.Map)
            {
                AdvanceShiftClock(Time.deltaTime * (Config != null ? Config.clockMinutesPerSecond : 0.25f));
            }

            // Endless / Challenge countdown — only while actually inspecting.
            if ((Session.Mode == GameMode.Endless || Session.Mode == GameMode.Challenge) &&
                State == AppViewState.Checkpoint && Dialogue != null && !Dialogue.IsActive)
            {
                Session.RunTimer -= Time.deltaTime;
                Bus.Publish(GameEvent.Of(EventIds.RunTimerTick, Session.RunTimer));
                if (Session.RunTimer <= 0f)
                {
                    Session.RunTimer = 0f;
                    FinishRun();
                }
            }

            // sanity ticking per context
            if (SanityCtl != null && (State == AppViewState.Checkpoint || State == AppViewState.Exploration))
                SanityCtl.Tick(State == AppViewState.Checkpoint);
        }

        // ---------------------------------------------------------------- flow API

        public void SetState(AppViewState s)
        {
            if (VerboseFlow) Debug.Log("[Flow] SetState " + s);
            State = s;
            Bus.Publish(GameEvent.Of(EventIds.StateChanged, s));
            ApplyVisibility(s);
        }

        private void ApplyVisibility(AppViewState s)
        {
            if (VerboseFlow) Debug.Log("[Flow] ApplyVisibility " + s);
            bool menu = s == AppViewState.MainMenu || s == AppViewState.ModeSelect;
            if (MainMenu != null) { if (s == AppViewState.MainMenu) MainMenu.Show(); else MainMenu.Hide(); }
            if (ModeSelect != null) { if (s == AppViewState.ModeSelect) ModeSelect.Show(); else ModeSelect.Hide(); }

            if (SecurityRoom != null) { if (s == AppViewState.SecurityRoom) SecurityRoom.Show(); else SecurityRoom.Hide(); }
            bool inRun = s == AppViewState.Checkpoint || s == AppViewState.Exploration ||
                         s == AppViewState.ShiftIntro || s == AppViewState.AdminEncounter ||
                         s == AppViewState.Map || s == AppViewState.ShiftResults ||
                         s == AppViewState.SecurityRoom;
            if (HUD != null) { if (inRun) HUD.Show(); else HUD.Hide(); }
            if (Checkpoint != null)
            {
                if (s == AppViewState.Checkpoint || s == AppViewState.AdminEncounter) Checkpoint.Show();
                else Checkpoint.Hide();
            }
            if (Views != null)
            {
                if (s == AppViewState.Exploration) Views.ShowWorld(); else Views.HideWorld();
            }
            if (MapScreen != null) { if (s == AppViewState.Map) MapScreen.Show(); else MapScreen.Hide(); }
            if (ShiftResults != null) { if (s == AppViewState.ShiftResults) ShiftResults.Show(); else ShiftResults.Hide(); }
            if (Ending != null) { if (s == AppViewState.Ending || s == AppViewState.EndlessResults) Ending.Show(); else Ending.Hide(); }
            if (Captions != null) { if (menu) Captions.Hide(); else Captions.Show(); }
        }

        public void BootToMainMenu()
        {
            Session = new GameSession();
            SetState(AppViewState.MainMenu);
            Audio.PlayMusic("menu");
        }

        public bool HasSave()
        {
            var s = Saves.Read();
            return s != null && s.Session.Mode == GameMode.Normal && !s.Session.RunFinished
                && s.Session.CompletedShifts < ShiftPlan.TotalShifts;
        }

        public void StartNewGame(GameMode mode)
        {
            // carry meta records across runs
            var old = Saves.Read();
            Records = old?.Records ?? new RecordsData();

            int seed = mode == GameMode.Normal ? 20261102 : (int)(System.DateTime.UtcNow.Ticks & 0x7FFFFFFF);
            Session = new GameSession { Mode = mode, Seed = seed };
            _runRng = new GameRng(seed ^ 0x5EED);
            EndlessProcessed = 0;

            if (mode == GameMode.Normal)
            {
                QueueSceneAction(() => BeginShift(1));
            }
            else
            {
                Session.RunTimer = EndlessRules.StartSeconds;
                Roster = new RosterFactory(seed);
                var intro = mode == GameMode.Endless
                    ? new List<DialogueLine>
                    {
                        new DialogueLine { Speaker = "DISPATCH", Text = "Endless watch. They keep coming until the clock beats you. Every anomaly you turn away buys you seconds." },
                    }
                    : new List<DialogueLine>
                    {
                        new DialogueLine { Speaker = "DISPATCH", Text = "Challenge protocol: district rules change without warning, your nerves fray twice as fast, and every correct call is worth double." },
                    };
                QueueSceneAction(() => PlayIntroThenCheckpoint(intro, null));
            }
            Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.Checkpoint);
        }

        public void ContinueGame()
        {
            var save = Saves.Read();
            if (save == null)
            {
                Bus.Publish(GameEvent.Of(EventIds.Notification, "No save found."));
                SetState(AppViewState.MainMenu);
                return;
            }
            Session = save.Session;
            Records = save.Records;
            Settings.AdoptFromSaveIfFresh(save.Settings);
            _runRng = new GameRng(Session.Seed ^ 0x5EED);
            if (Session.Mode == GameMode.Normal)
            {
                int shift = Mathf.Clamp(Session.CurrentShift, 1, ShiftPlan.TotalShifts);
                int resumeFrom = save.Session.VisitorIndexAtSave;
                QueueSceneAction(() => BeginShift(shift, resumeFrom));
                Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.Checkpoint);
            }
            else
            {
                // Run modes only persist records; restart the mode fresh.
                StartNewGame(Session.Mode);
            }
        }

        /// <summary>Normal mode: prepare and enter a shift. Roster generation replays earlier
        /// shifts on a fresh factory so the seeded RNG lands on the identical state (saves resume
        /// mid-story without storing the whole roster).</summary>
        public void BeginShift(int number, int resumeFromVisitor = 0)
        {
            _resumeFromVisitor = resumeFromVisitor;
            number = Mathf.Clamp(number, 1, ShiftPlan.TotalShifts);
            Session.CurrentShift = number;
            var plan = ShiftPlan.Build();
            Roster = new RosterFactory(Session.Seed);
            for (int i = 0; i < number - 1; i++) Roster.GenerateShift(plan[i]); // advance RNG deterministically
            CurrentShiftDef = (Config != null && Config.exhibitionMode && number == 1)
                ? ShiftPlan.ExhibitionDef() : plan[number - 1];
            CurrentRoster = Roster.GenerateShift(CurrentShiftDef);
            // one shared pool: this shift's anomalies, outstanding until resolved
            var kinds = new System.Collections.Generic.List<AnomalyBehaviorKind>();
            for (int i = 0; i < CurrentShiftDef.AnomalyCount; i++)
                kinds.Add((AnomalyBehaviorKind)_runRng.NextInt(0, 4));
            Session.Pool.Seed(kinds);
            Session.TonightLog.Clear();
            _shiftMinutes = 0f;

            var intro = new List<DialogueLine>
            {
                new DialogueLine { Speaker = "NIGHT " + number + " — " + CurrentShiftDef.DateLabel.ToUpperInvariant(), Text = CurrentShiftDef.Title + ". " + CurrentShiftDef.Objective },
                new DialogueLine { Speaker = "PAPER NOTE", Text = CurrentShiftDef.RuleNoteAddendum },
            };
            if (number == 5)
            {
                intro.Add(new DialogueLine { Speaker = "???", Text = "Tonight the district sends an administrator to 'take control'. Check the credentials like you would check anyone else's. Trust the evidence." });
            }
            PlayIntroThenCheckpoint(intro, CurrentShiftDef, _resumeFromVisitor);
            _resumeFromVisitor = 0;
        }

        private void PlayIntroThenCheckpoint(List<DialogueLine> intro, ShiftDef shift, int resumeFromVisitor = 0)
        {
            if (Loading != null)
                Loading.Show(shift != null
                    ? "SHIFT " + shift.Number + "  ·  " + shift.DateLabel
                    : (Session.Mode == GameMode.Endless ? "ENDLESS WATCH" : "CHALLENGE PROTOCOL"));
            SetState(AppViewState.ShiftIntro);
            Bus.Publish(GameEvent.Of(EventIds.ShiftStarted, shift));
            ObjectiveText = shift != null ? shift.Objective :
                (Session.Mode == GameMode.Endless ? "Survive the clock. Reject every anomaly." : "Survive the clock under shifting district rules.");
            Bus.Publish(GameEvent.Of(EventIds.ObjectiveChanged, ObjectiveText));
            Audio.PlayMusic("checkpoint");
            Debug.Log("[Flow] intro shown (loading=" + (Loading != null) + " dialogue=" + (Dialogue != null) + ")");
            if (Dialogue == null) Debug.LogError("[GameRoot] intro: Dialogue view NULL - check scene bootstrap view creation.");
            if (Loading == null) Debug.LogError("[GameRoot] intro: Loading view NULL - no NIGHT backdrop will show.");
            if (Checkpoint == null) Debug.LogError("[GameRoot] intro: Checkpoint view NULL - the desk cannot start.");
            if (Dialogue != null)
                Dialogue.Play(intro, () =>
                {
                    if (VerboseFlow) Debug.Log("[Flow] intro callback fired");
                    if (Loading != null) Loading.Hide();
                    SetState(AppViewState.Checkpoint);
                    if (Checkpoint != null) Checkpoint.BeginRun(CurrentRoster, shift, resumeFromVisitor);
                    else Debug.LogError("[GameRoot] intro finished but Checkpoint is still NULL.");
                    Debug.Log("[Flow] intro done -> checkpoint state");
                });
            else
            {
                if (Loading != null) Loading.Hide();
                SetState(AppViewState.Checkpoint);
                if (Checkpoint != null) Checkpoint.BeginRun(CurrentRoster, shift);
            }
        }

        /// <summary>Called by the checkpoint after every stamped verdict.</summary>
        public void OnVerdictResolved(VisitorData visitor, InspectionRules.Outcome outcome)
        {
            Session.RecordVerdict(outcome);
            Session.TonightLog.RecordFrom(visitor, "earlier tonight");
            _shiftMinutes += 240f / (CurrentRoster.Count + 1);

            if (Session.Mode == GameMode.Endless || Session.Mode == GameMode.Challenge)
            {
                Session.RunProcessed++;
                EndlessProcessed = Session.RunProcessed;
                Session.Score += ComputeRunScore(outcome);
                float delta = Session.ApplyRunTimer(outcome);
                if (HUD != null) HUD.Notify(delta >= 0 ? "+" + delta.ToString("0") + "s" : delta.ToString("0") + "s");
                if (Session.Mode == GameMode.Challenge && Session.RunProcessed % ChallengeRules.VisitorsPerRuleWave == 0)
                    RollChallengeMutation();
            }
            else
            {
                Session.Score += outcome.Correct ? (outcome.WasAnomaly ? 100 : 50) : 0;
            }

            // atmosphere reacts to breaches
            if (outcome.AnomalyBreach && !outcome.WasAdministrator)
            {
                Bus.Publish(GameEvent.Of(EventIds.Notification, "Something walked past you into the school."));
                if (Views != null) Views.NoteBreach();
            }

            // ending check — lose conditions can fire mid-shift (C3/C4)
            var eval = Session.EvaluateEnding();
            if (eval.Ending != EndingType.None)
            {
                TriggerEnding(eval);
            }
        }

        private int ComputeRunScore(InspectionRules.Outcome o)
        {
            int baseScore = EndlessRules.ScoreFor(o, Session.Streak);
            if (Session.Mode == GameMode.Challenge) baseScore = (int)(baseScore * ChallengeRules.ScoreMultiplier);
            return baseScore;
        }

        private void RollChallengeMutation()
        {
            int wave = Session.RunProcessed / ChallengeRules.VisitorsPerRuleWave;
            var m = ChallengeRules.RollMutation(wave, _runRng);
            Session.ActiveMutationKey = m.Key;
            Bus.Publish(GameEvent.Of(EventIds.MutationChanged, m));
            Audio.PlaySfx("sfx_intercom");
            Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*intercom crackles: new district bulletin*", 3f, null));
        }

        /// <summary>Endless/Challenge run generation (per-visitor, on demand).</summary>
        public VisitorData NextRunVisitor()
        {
            if (Roster == null) Roster = new RosterFactory(Session.Seed);
            return Roster.GenerateRunVisitor(Session.RunProcessed);
        }

        // ---- exploration ----

        public void EnterExploration(RoomId room = RoomId.Hallway)
        {
            QueueSceneAction(() =>
            {
                SetState(AppViewState.Exploration);
                ObjectiveText = "Night " + Session.CurrentShift + " — patrol: check the rooms, follow the chain, return to the checkpoint before dawn.";
                Bus.Publish(GameEvent.Of(EventIds.ObjectiveChanged, ObjectiveText));
                if (Views != null) Views.EnterRoom(room);
                Audio.PlayMusic(MusicForRoom(room));
            });
            Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.School);
        }

        public static string MusicForRoom(RoomId room)
        {
            switch (room)
            {
                case RoomId.Classroom1: return "classroom";
                case RoomId.Classroom2: return "darkroom";
                default: return "hallway";
            }
        }

        public void ReturnToCheckpoint()
        {
            QueueSceneAction(() =>
            {
                SetState(AppViewState.Checkpoint);
                Audio.PlayMusic("checkpoint");
                if (Checkpoint != null) Checkpoint.ResumeRun();
            });
            Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.Checkpoint);
        }

        /// <summary>Classroom security-office door (Scene B prototype).</summary>
        public void OpenSecurityRoom()
        {
            SetState(AppViewState.SecurityRoom);
            ObjectiveText = "Night " + Session.CurrentShift + " — office: late list and expiry rule. Verdicts count.";
            Bus.Publish(GameEvent.Of(EventIds.ObjectiveChanged, ObjectiveText));
            Audio.PlayMusic("darkroom");
        }

        public void OpenMap()
        {
            if (MapScreen == null)
            {
                Bus.Publish(GameEvent.Of(EventIds.Notification, "The map is pinned up at the checkpoint."));
                return;
            }
            if (State != AppViewState.Exploration && State != AppViewState.Checkpoint) return;
            _preMapState = State;
            SetState(AppViewState.Map);
            Audio.PlaySfx("sfx_ui_open");
        }

        private AppViewState _preMapState = AppViewState.Checkpoint;

        public void CloseMap()
        {
            Audio.PlaySfx("sfx_ui_close");
            SetState(_preMapState == AppViewState.Map ? AppViewState.Checkpoint : _preMapState);
        }

        public void TravelViaMap(RoomId room)
        {
            CloseMap();
            if (room == RoomId.Security) ReturnToCheckpoint();
            else EnterExploration(room);
        }

        // ---- shift / run completion ----

        public void FinishShift()
        {
            Bus.Publish(GameEvent.Of(EventIds.ShiftEnded, CurrentShiftDef));
            Session.CompleteShift();
            PersistRun();

            var eval = Session.EvaluateEnding();
            if (eval.Ending == EndingType.None && Config != null && Config.exhibitionMode && Session.CompletedShifts >= 1)
                eval = EndingRules.EvaluateExhibition(Session);   // booth cut closes after one night
            if (eval.Ending != EndingType.None) { TriggerEnding(eval); return; }

            SetState(AppViewState.ShiftResults);
            if (ShiftResults != null) ShiftResults.Populate(Session, CurrentShiftDef);
            Audio.PlayMusic("");
        }

        public void AdvanceToNextShift()
        {
            if (Session.CurrentShift > ShiftPlan.TotalShifts) { BootToMainMenu(); return; }
            BeginShift(Session.CurrentShift);
        }

        public void FinishRun() // endless/challenge time-out
        {
            if (Session.Mode == GameMode.Endless && Session.Score > Records.EndlessBest) Records.EndlessBest = Session.Score;
            if (Session.Mode == GameMode.Challenge && Session.Score > Records.ChallengeBest) Records.ChallengeBest = Session.Score;
            Session.RunFinished = true;
            PersistRun();
            SetState(AppViewState.EndlessResults);
            if (Ending != null) Ending.PopulateRunResults(Session, Records);
            Audio.PlayMusic(Session.Score > 0 ? "end_win" : "end_lose");
        }

        public void TriggerEnding(EndingRules.Evaluation eval)
        {
            string key = eval.Ending == EndingType.Secret ? "secret" : (eval.Ending == EndingType.Win ? "win" : "lose");
            if (eval.ReasonKey != "exhibition" && !Records.EndingsSeen.Contains(key)) Records.EndingsSeen.Add(key);
            Session.RunFinished = true;
            PersistRun();
            SetState(AppViewState.Ending);
            if (Ending != null) Ending.Populate(eval, Session, Records);
            Bus.Publish(GameEvent.Of(EventIds.EndingReached, eval));
            Audio.PlayMusic("end_" + key);
            Audio.PlaySfx(eval.Ending == EndingType.Lose ? "sfx_alarm" : "sfx_bell");
        }

        /// <summary>F2×3 data reset: deletes the save file (run progress, records, endings
        /// seen) and clears in-memory run state. Accessibility/local settings are preserved
        /// on purpose (PlayerPrefs). Callers should confirm with the player first.</summary>
        public void ResetAllData()
        {
            Saves.Delete();
            Records = new RecordsData();
            Session = new GameSession();
            Bus.Publish(GameEvent.Of(EventIds.Notification, "All save data erased."));
            Debug.Log("[GameRoot] data reset complete (settings preserved).");
        }

        /// <summary>The single confirmation + reset path shared by the F2 x3 shortcut and the
        /// Options > RESET DATA button, so both can never drift apart. Settings (volumes,
        /// display, accessibility) live in PlayerPrefs and are deliberately untouched.</summary>
        public void RequestDataReset(System.Action afterReset = null)
        {
            if (Confirm == null) { ResetAllData(); if (afterReset != null) afterReset(); return; }
            Confirm.Ask("ERASE ALL DATA?",
                "Deletes your save: shift progress, records and unlocked endings.\n" +
                "Volumes, display and accessibility settings are kept.",
                () =>
                {
                    ResetAllData();
                    Audio.PlaySfx("sfx_static", 0.3f);
                    if (afterReset != null) afterReset();
                });
        }

        /// <summary>Save at safe boundaries (shift end / run end / quit). Corrupt-safe by design.</summary>
        public void PersistRun()
        {
            var save = new SaveGame
            {
                Session = Session,
                Settings = Settings.Current.Clone(),
                Records = Records,
                SavedAtLabel = "Night " + Session.CurrentShift + " · " + ClockString,
            };
            save.Session.VisitorIndexAtSave = CurrentVisitorIndex;
            Saves.Write(save);
        }

        // ---- overlay sub-screens (settings / controls / credits) ----
        // These overlay the current context without changing State, so closing them returns
        // exactly where the player was (main menu, pause, checkpoint…).

        public void OpenSettings()
        {
            if (Paused && PauseScreen != null) PauseScreen.Hide();
            if (SettingsScreen != null) SettingsScreen.Show();
        }

        public void CloseSettings()
        {
            if (SettingsScreen != null) SettingsScreen.Hide();
            if (Paused && PauseScreen != null) PauseScreen.Show();
        }

        public void OpenControls()
        {
            if (Paused && PauseScreen != null) PauseScreen.Hide();
            if (ControlsScreen != null) ControlsScreen.Show();
        }

        public void CloseControls()
        {
            if (ControlsScreen != null) ControlsScreen.Hide();
            if (Paused && PauseScreen != null) PauseScreen.Show();
        }

        public void OpenCredits()
        {
            if (CreditsScreen != null) CreditsScreen.Show();
        }

        public void CloseCredits()
        {
            if (CreditsScreen != null) CreditsScreen.Hide();
        }

        // ---- pause / quit ----

        public void TogglePause(bool? force = null)
        {
            bool target = force ?? !Paused;
            if (target == Paused) return;
            bool allowed = State == AppViewState.Checkpoint || State == AppViewState.Exploration ||
                           State == AppViewState.AdminEncounter || State == AppViewState.Map ||
                           State == AppViewState.SecurityRoom;
            if (target && !allowed) return;
            Paused = target;
            Time.timeScale = Paused ? 0f : 1f;
            if (PauseScreen != null) { if (Paused) PauseScreen.Show(); else PauseScreen.Hide(); }
            Bus.Publish(GameEvent.Of(EventIds.PauseToggled, Paused));
            Audio.PlaySfx(Paused ? "sfx_ui_open" : "sfx_ui_close");
        }

        public void QuitToMenu(bool saveFirst)
        {
            if (SettingsScreen != null) SettingsScreen.Hide();
            if (ControlsScreen != null) ControlsScreen.Hide();
            if (CreditsScreen != null) CreditsScreen.Hide();
            if (Confirm != null) Confirm.Hide();
            if (Dialogue != null) Dialogue.Hide();
            if (Paused) TogglePause(false);
            if (saveFirst && Session != null && Session.Mode == GameMode.Normal && !Session.RunFinished &&
                Session.CompletedShifts < ShiftPlan.TotalShifts && State != AppViewState.MainMenu)
            {
                PersistRun();
            }
            if (Views != null) Views.HideWorld();
            Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.Menu);
        }

        public void QuitApplication()
        {
            if (Session != null && Session.Mode == GameMode.Normal) PersistRun();
            Application.Quit();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }
    }
}
````

### `_Project/Scripts/Core/IScreenView.cs` (152 B)

````csharp
namespace Hollowbrook.Core
{
    public interface IScreenView
    {
        void Init(GameRoot root);
        void Show();
        void Hide();
    }
}
````

### `_Project/Scripts/Core/SaveSystem.cs` (3.0 KiB)

````csharp
using System;
using System.IO;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Core
{
    // Reads/writes the save file on disk. Created by CoreBootstrap.
    public class SaveSystem
    {
        private const string FileName = "hollowbrook_save.txt";
        private readonly EventBus _bus;

        public string SaveDirectory
        {
            get
            {
                try
                {
                    string dir = Application.persistentDataPath;
                    if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Directory.GetCurrentDirectory(), "saves");
                    Directory.CreateDirectory(dir);
                    return dir;
                }
                catch (Exception e)
                {
                    Debug.LogError("[SaveSystem] save directory unavailable: " + e.Message);
                    return null;
                }
            }
        }

        public string SavePath
        {
            get
            {
                string dir = SaveDirectory;
                return dir == null ? null : Path.Combine(dir, FileName);
            }
        }

        public SaveSystem(EventBus bus) { _bus = bus; }

        public bool HasSave()
        {
            try
            {
                string p = SavePath;
                return p != null && File.Exists(p);
            }
            catch (Exception) { return false; }
        }

        public bool Write(SaveGame save)
        {
            try
            {
                string p = SavePath;
                if (p == null || save == null) return false;
                string tmp = p + ".tmp";
                File.WriteAllText(tmp, SaveFormat.Serialize(save));
                if (File.Exists(p)) File.Delete(p);
                File.Move(tmp, p);
                _bus?.Publish(GameEvent.Of(EventIds.GameSaved));
                Debug.Log("[SaveSystem] saved to " + p);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] write failed: " + e.Message);
                return false;
            }
        }

        public SaveGame Read()
        {
            try
            {
                string p = SavePath;
                if (p == null || !File.Exists(p)) return null;
                var save = SaveFormat.Deserialize(File.ReadAllText(p));
                if (save == null) Debug.LogWarning("[SaveSystem] save file corrupt/unsupported — treated as no save.");
                else _bus?.Publish(GameEvent.Of(EventIds.GameLoaded));
                return save;
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] read failed: " + e.Message);
                return null;
            }
        }

        public void Delete()
        {
            try
            {
                string p = SavePath;
                if (p != null && File.Exists(p)) File.Delete(p);
            }
            catch (Exception e) { Debug.LogWarning("[SaveSystem] delete failed: " + e.Message); }
        }
    }
}
````

### `_Project/Scripts/Core/SettingsSystem.cs` (7.5 KiB)

````csharp
using System;
using System.Collections.Generic;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Core
{
    // Owns player preferences and the screen mode. Created by CoreBootstrap.
    public class SettingsSystem
    {
        private const string KeyPrefix = "smyid.settings.";
        private readonly EventBus _bus;

        public SettingsData Current { get; private set; } = new SettingsData();

        public SettingsSystem(EventBus bus)
        {
            _bus = bus;
            LoadFromPrefs();
        }

        public void LoadFromPrefs()
        {
            try
            {
                Current = new SettingsData
                {
                    MasterVolume = PlayerPrefs.GetFloat(KeyPrefix + "master", 0.8f),
                    MusicVolume = PlayerPrefs.GetFloat(KeyPrefix + "music", 0.6f),
                    SfxVolume = PlayerPrefs.GetFloat(KeyPrefix + "sfx", 0.8f),
                    TextScale = PlayerPrefs.GetInt(KeyPrefix + "textScale", 1),
                    ReduceMotion = PlayerPrefs.GetInt(KeyPrefix + "reduceMotion", 0) == 1,
                    PhotosensitivitySafe = PlayerPrefs.GetInt(KeyPrefix + "photosafe", 0) == 1,
                    Captions = PlayerPrefs.GetInt(KeyPrefix + "captions", 1) == 1,
                    Fullscreen = PlayerPrefs.GetInt(KeyPrefix + "fullscreen", 1) == 1,
                    DisplayWidth = PlayerPrefs.GetInt(KeyPrefix + "displayW", 0),
                    DisplayHeight = PlayerPrefs.GetInt(KeyPrefix + "displayH", 0),
                };
                if (Current.SfxVolume <= 0f && !PlayerPrefs.HasKey(KeyPrefix + "sfxExplicit"))
                {
                    Debug.LogWarning("[Settings] stored SFX volume was 0 with no explicit user change - restoring audible default 0.8.");
                    Current.SfxVolume = 0.8f;
                }
                Debug.Log("[Settings] loaded master=" + Current.MasterVolume + " music=" + Current.MusicVolume +
                          " sfx=" + Current.SfxVolume + " text=" + Current.TextScale +
                          " fullscreen=" + Current.Fullscreen + " res=" + Current.DisplayWidth + "x" + Current.DisplayHeight);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Settings] prefs unreadable, using defaults: " + e.Message);
                Current = new SettingsData();
            }
        }

        public void SaveToPrefs()
        {
            try
            {
                PlayerPrefs.SetFloat(KeyPrefix + "master", Current.MasterVolume);
                PlayerPrefs.SetFloat(KeyPrefix + "music", Current.MusicVolume);
                PlayerPrefs.SetFloat(KeyPrefix + "sfx", Current.SfxVolume);
                PlayerPrefs.SetInt(KeyPrefix + "textScale", Current.TextScale);
                PlayerPrefs.SetInt(KeyPrefix + "reduceMotion", Current.ReduceMotion ? 1 : 0);
                PlayerPrefs.SetInt(KeyPrefix + "photosafe", Current.PhotosensitivitySafe ? 1 : 0);
                PlayerPrefs.SetInt(KeyPrefix + "captions", Current.Captions ? 1 : 0);
                PlayerPrefs.SetInt(KeyPrefix + "fullscreen", Current.Fullscreen ? 1 : 0);
                PlayerPrefs.SetInt(KeyPrefix + "displayW", Current.DisplayWidth);
                PlayerPrefs.SetInt(KeyPrefix + "displayH", Current.DisplayHeight);
                PlayerPrefs.SetInt(KeyPrefix + "sfxExplicit", 1);   // any Apply() is an explicit user write
                PlayerPrefs.Save();
            }
            catch (Exception e) { Debug.LogWarning("[Settings] prefs write failed: " + e.Message); }
        }

        public void AdoptFromSaveIfFresh(SettingsData saved)
        {
            if (saved == null) return;
            if (PlayerPrefs.HasKey(KeyPrefix + "master")) return;
            Current = saved.Clone();
            SaveToPrefs();
            Notify();
        }

        public void Apply(Action<SettingsData> mutate)
        {
            mutate?.Invoke(Current);
            Current.MasterVolume = Mathf.Clamp01(Current.MasterVolume);
            Current.MusicVolume = Mathf.Clamp01(Current.MusicVolume);
            Current.SfxVolume = Mathf.Clamp01(Current.SfxVolume);
            Current.TextScale = Mathf.Clamp(Current.TextScale, 0, 2);
            SaveToPrefs();
            Notify();
        }

        public void Notify() => _bus?.Publish(GameEvent.Of(EventIds.SettingsChanged, Current.Clone()));

        public List<ModeSize> SupportedModes()
        {
            var all = new List<ModeSize>();
            try
            {
                foreach (var r in Screen.resolutions) all.Add(new ModeSize(r.width, r.height));
            }
            catch (Exception e) { Debug.LogWarning("[Settings] Screen.resolutions unavailable: " + e.Message); }
            return DisplayPolicy.Supported(all);
        }

        public ModeSize CurrentMode() => new ModeSize(Screen.width, Screen.height);

        public List<ModeSize> MenuChoices() => DisplayPolicy.MenuChoices(SupportedModes(), CurrentMode());

        public ModeSize SetDisplay(int w, int h, bool fullscreen, bool persist)
        {
            Current.Fullscreen = fullscreen;
            var want = new ModeSize(w, h);
            if (!DisplayPolicy.IsSupported(SupportedModes(), w, h))
                want = DisplayPolicy.Fallback(SupportedModes(), want);
            Current.DisplayWidth = want.W;
            Current.DisplayHeight = want.H;
            ApplyCurrentDisplay(persist);
            return want;
        }

        public void ApplyCurrentDisplay(bool persist)
        {
            try
            {
                Screen.fullScreenMode = Current.Fullscreen
                    ? FullScreenMode.FullScreenWindow
                    : FullScreenMode.Windowed;
                if (Current.DisplayWidth > 0 && Current.DisplayHeight > 0)
                    Screen.SetResolution(Current.DisplayWidth, Current.DisplayHeight, Screen.fullScreenMode);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Settings] display change failed — previous mode kept: " + e.Message);
            }
            if (persist) SaveToPrefs();
            Notify();
        }

        public void ApplyStoredDisplay()
        {
            if (Current.DisplayWidth <= 0 || Current.DisplayHeight <= 0) return; // no preference yet
            if (!DisplayPolicy.IsSupported(SupportedModes(), Current.DisplayWidth, Current.DisplayHeight))
            {
                Debug.LogWarning("[Settings] stored resolution " + Current.DisplayWidth + "x" +
                                 Current.DisplayHeight + " unsupported here — preference cleared.");
                Current.DisplayWidth = 0;
                Current.DisplayHeight = 0;
                SaveToPrefs();
                return;
            }
            ApplyCurrentDisplay(false);
        }

        public void RestoreDisplaySnapshot(SettingsData snapshot)
        {
            if (snapshot == null) return;
            Current.Fullscreen = snapshot.Fullscreen;
            Current.DisplayWidth = snapshot.DisplayWidth;
            Current.DisplayHeight = snapshot.DisplayHeight;
            ApplyCurrentDisplay(false);
        }

        public float MasterVolume => Current.MasterVolume;
        public float MusicVolume => Current.MasterVolume * Current.MusicVolume;
        public float SfxVolume => Current.MasterVolume * Current.SfxVolume;
        public float TextScaleMultiplier => SettingsData.TextScaleMultiplier(Current.TextScale);
        public bool ReduceMotion => Current.ReduceMotion;
        public bool PhotosensitivitySafe => Current.PhotosensitivitySafe;
        public bool Captions => Current.Captions;
    }
}
````

### `_Project/Scripts/GameLogic/AnomalyEncounter.cs` (3.4 KiB)

````csharp
using System;

namespace Hollowbrook.Logic
{
    public enum AnomalyState
    {
        Roaming,     // moving between waypoints, minding its own business
        Stalking,    // unlit/unwatched: closes distance or reroutes toward the player
        Detected,    // inside the illuminated area: silhouette/movement readable
        Exposed,     // held in Focus: distorted face/identity visible, repel meter filling
        Repelled,    // repel condition met: threat interrupted, it flees
        Recovering,  // off-stage: repositions through connected rooms, may return
    }

    [Serializable]
    public struct EncounterTuning
    {
        public float RepelMeter;          // 0..1 tracking meter that constitutes "sustained light"
        public float ExposedBeatSeconds;  // how long the face stays visible before it flees
        public float RecoverSeconds;      // off-stage time before it may return
        public float ThreatRange;         // world units at which it can lunge/catch
        public static EncounterTuning Default => new EncounterTuning
        {
            RepelMeter = 1f, ExposedBeatSeconds = 0.9f, RecoverSeconds = 14f, ThreatRange = 1.6f,
        };
    }

    public struct EncounterStep
    {
        public AnomalyState State;
        public bool JustExposed;    // transition into Exposed this tick (face-reveal feedback)
        public bool JustRepelled;   // transition into Repelled this tick (retreat + token)
        public bool JustRecovered;  // Recovering finished: eligible to roam again
        public bool Threatening;    // inside threat range while not repelled/recovering
    }

    public static class AnomalyEncounter
    {
        public static EncounterStep Step(AnomalyState state, float meter, float stateTimer,
            bool illuminated, bool focused, bool batteryOk, float distanceToPlayer,
            float dt, EncounterTuning t)
        {
            var r = new EncounterStep { State = state };
            switch (state)
            {
                case AnomalyState.Repelled:
                    if (stateTimer >= t.ExposedBeatSeconds) { r.State = AnomalyState.Recovering; stateTimer = 0f; }
                    break;
                case AnomalyState.Recovering:
                    if (stateTimer >= t.RecoverSeconds) { r.State = AnomalyState.Roaming; r.JustRecovered = true; stateTimer = 0f; }
                    break;
                case AnomalyState.Exposed:
                    if (meter >= t.RepelMeter) { r.State = AnomalyState.Repelled; r.JustRepelled = true; stateTimer = 0f; }
                    else if (!focused || !batteryOk) { r.State = AnomalyState.Detected; stateTimer = 0f; }
                    break;
                default: // Roaming / Stalking / Detected
                    if (focused && batteryOk && illuminated && meter > 0.55f)
                    { r.State = AnomalyState.Exposed; r.JustExposed = true; stateTimer = 0f; }
                    else if (illuminated) r.State = AnomalyState.Detected;
                    else if (meter > 0.05f) r.State = AnomalyState.Stalking;  // half-lit: it presses in
                    else r.State = meter <= 0.05f && !illuminated ? AnomalyState.Roaming : state;
                    break;
            }
            r.Threatening = (state == AnomalyState.Roaming || state == AnomalyState.Stalking ||
                             state == AnomalyState.Detected) && distanceToPlayer <= t.ThreatRange;
            return r;
        }
    }
}
````

### `_Project/Scripts/GameLogic/ClueRegistry.cs` (8.9 KiB)

````csharp
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    public static class ClueRegistry
    {
        public static List<ClueDef> Build()
        {
            return new List<ClueDef>
            {
                new ClueDef { Order = 1, Key = "light", Display = "Flickering Light", Room = RoomId.Hallway,
                    InspectText = "The hallway lights stutter in a slow rhythm — three dark beats, then a bright one. A maintenance log is taped beneath the switch: “Zone 3 flicker since the server room was sealed. District says ignore it.”",
                    ChainHint = "Someone scratched an arrow under the log, pointing into Classroom 1.",
                    SanityDelta = -2f, IsScare = false },
                new ClueDef { Order = 2, Key = "chalkboard", Display = "Chalkboard", Room = RoomId.Classroom1,
                    InspectText = "Old lesson notes have been half-erased. Underneath, in fresh chalk: “HB-2024-0212 WAS NEVER A REAL STUDENT.” The writing is not on any lesson plan.",
                    ChainHint = "Chalk dust trails from the board to the front desk.",
                    SanityDelta = -4f, IsScare = false },
                new ClueDef { Order = 3, Key = "desk", Display = "Front Desk", Room = RoomId.Classroom1,
                    InspectText = "The teacher's desk drawer is stuck, then gives. Inside: a folded note on district letterhead. “They wear our students' faces. Don't let them in. — E.R., night custodian (resigned).”",
                    ChainHint = "The note ends with: “Start with the sealed door at the end of the hall.”",
                    SanityDelta = -4f, IsScare = false },
                new ClueDef { Order = 4, Key = "door", Display = "Sealed Door", Room = RoomId.Hallway,
                    InspectText = "The door at the end of the hall is chained from the outside. A plate reads “SERVER / IDENTIFICATION ROOM”. The wood around the frame is scarred with scratch marks — from the inside.",
                    ChainHint = "Through the door's small wired glass you can see Classroom 2's far window glowing faintly.",
                    SanityDelta = -6f, IsScare = true },
                new ClueDef { Order = 5, Key = "window", Display = "Classroom Window", Room = RoomId.Classroom2,
                    InspectText = "Outside, under the streetlight, three figures stand motionless in the yard, faces up toward this window. You blink. The yard is empty. The glass is warm.",
                    ChainHint = "When you step back, your reflection is a beat late — and it looks toward the hallway notice board.",
                    SanityDelta = -8f, IsScare = true },
                new ClueDef { Order = 6, Key = "board", Display = "Notice Board", Room = RoomId.Hallway,
                    InspectText = "Faded announcements: “STUDENT IDENTIFICATION PROJECT — photos, records and personal information will be combined into one system. Volunteer sign-up inside.” Someone added in pen: “It was never voluntary.”",
                    ChainHint = "A pin holds a torn corner of a class photo. The rest of it hangs near the lost-and-found.",
                    SanityDelta = -4f, IsScare = false },
                new ClueDef { Order = 7, Key = "lostid", Display = "Lost Student ID", Room = RoomId.Classroom1,
                    InspectText = "A student ID under the lost-and-found shelf. The name field has been scratched out. The photo is the previous custodian — the one who “resigned”. Status: REGULAR. Year: 1st.",
                    ChainHint = "On the back, in pencil: “They put me in the system too. Look at the old picture in the hall.”",
                    SanityDelta = -6f, IsScare = true },
                new ClueDef { Order = 8, Key = "photo", Display = "Old School Photo", Room = RoomId.Hallway,
                    InspectText = "A framed school picture, decades old. Rows of identical faces. In the back corner stands a staff member in a formal coat who has not aged a day — the same face as the district administrator on tonight's roster memo.",
                    ChainHint = "Under the frame: a cabinet key labeled “C-2”, for the filing cabinet in Classroom 2.",
                    SanityDelta = -6f, IsScare = false },
                new ClueDef { Order = 9, Key = "cabinet", Display = "Filing Cabinet", Room = RoomId.Classroom2,
                    InspectText = "The key turns. The folder is stamped “CONFIDENTIAL — STUDENT IDENTIFICATION PROJECT — APPROVED”. Inside: the system began generating identities that belonged to no real student. The district sealed the server room and closed the school mid-term. A custodian's note is taped inside the cover: “Don't let them in.”",
                    ChainHint = "The last page is a work order: “Replace hallway clock — stopped 1:47 AM.”",
                    SanityDelta = -6f, IsScare = false },
                new ClueDef { Order = 10, Key = "clock", Display = "Stopped Clock", Room = RoomId.Hallway,
                    InspectText = "The hallway clock is frozen at 1:47 AM — the minute the closure order was signed, according to the folder. As you watch, the minute hand twitches backward. Somewhere behind the walls, a server fan spins up.",
                    ChainHint = "You now know where they are all trying to go. Keep them out.",
                    SanityDelta = -10f, IsScare = true },
            };
        }
    }

    public class ClueDiscoveryResult
    {
        public bool FirstTime;
        public bool AdvancedSequence;  // this was the next clue in the intended order
        public bool BrokeSequence;     // found out of order — secret ending no longer available
        public ClueDef Def;
    }

    public class ClueTracker
    {
        private readonly Dictionary<string, ClueDef> _byKey = new Dictionary<string, ClueDef>();
        private readonly HashSet<string> _found = new HashSet<string>();
        private readonly HashSet<RoomId> _investigatedRooms = new HashSet<RoomId>();
        private int _nextInSequence = 1;

        public bool OrderIntact { get; private set; } = true;
        public int FoundCount => _found.Count;
        public int TotalCount => _byKey.Count;
        public IReadOnlyCollection<string> FoundKeys => _found;
        public IReadOnlyCollection<RoomId> InvestigatedRooms => _investigatedRooms;
        public int NextExpectedOrder => _nextInSequence;

        public ClueTracker()
        {
            foreach (var c in ClueRegistry.Build()) _byKey[c.Key] = c;
        }

        public ClueDef Get(string key) => _byKey.TryGetValue(key, out var c) ? c : null;

        public ClueDiscoveryResult Discover(string key)
        {
            var res = new ClueDiscoveryResult();
            if (!_byKey.TryGetValue(key, out var def)) return res;
            res.Def = def;
            MarkRoomInvestigated(def.Room);
            if (_found.Contains(key)) { res.FirstTime = false; return res; }
            _found.Add(key);
            res.FirstTime = true;
            if (def.Order == _nextInSequence)
            {
                res.AdvancedSequence = true;
                _nextInSequence++;
            }
            else
            {
                res.BrokeSequence = OrderIntact; // first out-of-order find breaks it
                OrderIntact = false;
            }
            return res;
        }

        public void MarkRoomInvestigated(RoomId room)
        {
            if (room != RoomId.Security) _investigatedRooms.Add(room);
        }

        public bool AllFound => _found.Count >= _byKey.Count;

        public bool SecretConditionsMet =>
            AllFound && OrderIntact &&
            _investigatedRooms.Contains(RoomId.Hallway) &&
            _investigatedRooms.Contains(RoomId.Classroom1) &&
            _investigatedRooms.Contains(RoomId.Classroom2);

        public List<string> SaveFound() { var l = new List<string>(_found); l.Sort(System.StringComparer.Ordinal); return l; }
        public void LoadFound(IEnumerable<string> keys)
        {
            _found.Clear(); _nextInSequence = 1; OrderIntact = true;
            if (keys == null) return;
            foreach (var k in keys)
            {
                if (!_byKey.ContainsKey(k)) continue;
                _found.Add(k);
                if (_byKey[k].Order == _nextInSequence) _nextInSequence++;
            }
        }
        public List<string> SaveRooms()
        {
            var l = new List<string>();
            foreach (var r in _investigatedRooms) l.Add(r.ToString());
            l.Sort(System.StringComparer.Ordinal);
            return l;
        }
        public void LoadRooms(IEnumerable<string> rooms)
        {
            _investigatedRooms.Clear();
            if (rooms == null) return;
            foreach (var r in rooms)
                if (System.Enum.TryParse(r, out RoomId room)) _investigatedRooms.Add(room);
        }
        public void LoadOrderIntact(bool intact, int nextOrder)
        {
            OrderIntact = intact;
            _nextInSequence = nextOrder;
        }
    }
}
````

### `_Project/Scripts/GameLogic/ContentData.cs` (13.5 KiB)

````csharp
using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    [Serializable]
    public struct Portrait
    {
        public int HairStyle;    // 0..5
        public int FaceVariant;  // 0..5
        public int Accessory;    // 0 none, 1 glasses, 2 earrings, 3 cap
        public bool Distorted;   // anomaly glitch overlay (static patches / shifted eyes)

        public static bool operator ==(Portrait a, Portrait b) =>
            a.HairStyle == b.HairStyle && a.FaceVariant == b.FaceVariant && a.Accessory == b.Accessory;
        public static bool operator !=(Portrait a, Portrait b) => !(a == b);
        public override bool Equals(object obj) => obj is Portrait p && p == this;
        public override int GetHashCode() => (HairStyle * 31 + FaceVariant) * 31 + Accessory;

        public Portrait Swapped() => new Portrait
        {
            HairStyle = (HairStyle + 3) % 6,
            FaceVariant = (FaceVariant + 2) % 6,
            Accessory = (Accessory + 1) % 4,
            Distorted = Distorted,
        };

        public Portrait SubtleVariant(GameRng rng)
        {
            var p = this;
            switch (rng.NextInt(0, 2))
            {
                case 0: p.HairStyle = (p.HairStyle + 1) % 6; break;
                default: p.FaceVariant = (p.FaceVariant + 1) % 6; break;
            }
            return p;
        }
    }

    [Serializable]
    public class StudentIdentity
    {
        public string FullName;
        public string IdNumber;        // registry ID, always valid format: HB-YYYY-NNNN
        public int YearLevel;          // 1..4
        public int CourseIndex;        // index into Content.Courses
        public bool IsTransferee;
        public Portrait Portrait;
        public RoomId FirstClassroom;  // Room 101 (Classroom1) / Room 202 (Classroom2)
        public string FirstClassSubject;
        public string Greeting;        // flavor line at the window

        public string StatusString => IsTransferee ? "Transferee" : "Regular";
        public string YearString => Content.YearNames[YearLevel - 1];
        public string CourseString => Content.Courses[CourseIndex];
        public string ClassroomString =>
            (FirstClassroom == RoomId.Classroom1 ? "Room 101" : "Room 202") + " · " + FirstClassSubject;
    }

    [Serializable]
    public struct Answer
    {
        public string Text;
        public ToneTag Tone;
        public bool ContradictsDocuments;
    }

    [Serializable]
    public class VisitorData
    {
        public int VisitorIndex;              // index within the whole run
        public int ShiftNumber;               // 1..5 (0 in endless/challenge)
        public bool IsAnomaly;
        public bool IsAdministrator;          // Shift-5 special encounter
        public string ClaimedName;            // name printed on the ID card
        public string RecordName;             // name printed on the record form
        public string IdNumber;               // printed on ID card
        public string RecordIdNumber;         // printed on record form
        public int IdYearLevel;               // year level printed on ID
        public int RecordYearLevel;           // year level printed on record
        public int IdCourseIndex;             // course printed on ID
        public int RecordCourseIndex;         // course printed on record
        public bool IdIsTransferee;           // status printed on ID
        public bool RecordIsTransferee;       // status printed on record
        public int ExpirationDay;             // absolute day number (see Content.DayNumber)
        public string ExpirationDisplay;      // e.g. "2027-03-31"
        public Portrait WindowPortrait;       // the face at the window (what player sees)
        public Portrait IdPhoto;              // photo printed on the ID card
        public bool InDatabase;               // scan result: registry lookup
        public string DuplicateOfShift;       // non-empty when DuplicateId ("previously seen Shift N")
        public string RecordFirstClassroom;   // schedule line on the record form
        public List<DefectType> Defects = new List<DefectType>();
        public string Greeting = "...";
        public Dictionary<QuestionId, Answer> Answers = new Dictionary<QuestionId, Answer>();
        public bool BehaviorGlitchVisible;    // rare visual micro-glitch hint for BehaviorOddity
        public bool OnLateList = true;        // office rule: on tonight's late list

        public bool PhotoMatches => WindowPortrait == IdPhoto;

        public List<string> DescribeDefects()
        {
            var lines = new List<string>();
            foreach (var d in Defects)
            {
                switch (d)
                {
                    case DefectType.PhotoMismatch: lines.Add("The face at the window does not match the ID photo."); break;
                    case DefectType.PhotoSubtleMismatch: lines.Add("The ID photo differs subtly from the person (one feature)."); break;
                    case DefectType.ExpiredId: lines.Add("The ID expired on " + ExpirationDisplay + "."); break;
                    case DefectType.IdFormatInvalid: lines.Add("ID number '" + IdNumber + "' is not in HB-YYYY-NNNN format."); break;
                    case DefectType.YearMismatch: lines.Add("Year level: ID says " + Content.YearNames[IdYearLevel - 1] + ", record says " + Content.YearNames[RecordYearLevel - 1] + "."); break;
                    case DefectType.CourseMismatch: lines.Add("Course: ID says " + Content.Courses[IdCourseIndex] + ", record says " + Content.Courses[RecordCourseIndex] + "."); break;
                    case DefectType.StatusMismatch: lines.Add("Status: ID says " + (IdIsTransferee ? "Transferee" : "Regular") + ", record says " + (RecordIsTransferee ? "Transferee" : "Regular") + "."); break;
                    case DefectType.RecordNameMismatch: lines.Add("Record name '" + RecordName + "' does not match ID name '" + ClaimedName + "'."); break;
                    case DefectType.NotInDatabase: lines.Add("ID number not found in the student database."); break;
                    case DefectType.DuplicateId: lines.Add("ID number already used by a visitor from " + DuplicateOfShift + "."); break;
                    case DefectType.BehaviorOddity: lines.Add("Behavior and answers did not match the class schedule."); break;
                }
            }
            if (lines.Count == 0) lines.Add("No discrepancies — documents were consistent.");
            return lines;
        }
    }

    [Serializable]
    public class ClueDef
    {
        public int Order;            // 1..10, intended sequence
        public string Key;           // stable save key, e.g. "light"
        public string Display;       // "Flickering Light"
        public RoomId Room;
        public string InspectText;   // what the player reads
        public string ChainHint;     // points to the next clue in the intended sequence
        public float SanityDelta;    // disturbing events cost sanity
        public bool IsScare;         // triggers a scare/stinger event
    }

    [Serializable]
    public class ShiftDef
    {
        public int Number;           // 1..5
        public string DateLabel;     // "Mon · Nov 2"
        public int VisitorCount;     // 4/6/7/8/8 per the approved progression
        public int AnomalyCount;     // 0/2/3/4/4
        public bool HasAdministrator;
        public string Title;
        public string Objective;
        public string RuleNoteAddendum;  // new paper-note rule introduced this shift
        public int MaxDefectsPerAnomaly; // difficulty escalation
        public bool SubtleDefectsAllowed;
        public bool DuplicateIdsAllowed;
        public bool BehaviorOddityAllowed;
        public bool ExplorationFocus;    // shift 4 nudges investigation
    }

    [Serializable]
    public class RuleMutationDef
    {
        public string Key;
        public string RuleText;       // printed on the paper note while active

        public RuleMutationDef(string key, string text) { Key = key; RuleText = text; }

        public bool ForcesReject(VisitorData v)
        {
            switch (Key)
            {
                case "no_transferees": return !v.IsAnomaly && v.IdIsTransferee;
                case "fourth_year_curfew": return !v.IsAnomaly && v.IdYearLevel == 4;
                case "strict_expiry": return !v.IsAnomaly && v.ExpirationDay - Content.ShiftDayNumber(v.ShiftNumber) <= 60;
                case "odd_serials": return !v.IsAnomaly && IdRules.SerialOf(v.IdNumber) % 2 == 1;
                case "office_late_list": return !v.IsAnomaly &&
                    (!v.OnLateList || IdRules.IsExpired(v, v.ShiftNumber));
                default: return false;
            }
        }
    }

    public static class Content
    {
        public const int TotalStudents = 14;          // 8 core + 6 variations (approved asset plan)
        public const int MaxQuestionsPerInspection = 2; // conflict C1 resolution: §2.3 + checklists say 1–2

        public static readonly string[] Courses =
        {
            "BS Information Technology", "BS Accountancy", "AB Communication", "BS Nursing",
            "BS Criminology", "BS Psychology", "BS Biology", "BA Multimedia Arts",
        };

        public static readonly string[] YearNames = { "1st Year", "2nd Year", "3rd Year", "4th Year" };

        public static readonly string[] ShiftDates =
        {
            "Mon · Nov 2", "Tue · Nov 3", "Wed · Nov 4", "Thu · Nov 5", "Fri · Nov 6",
        };

        public static int ShiftDayNumber(int shift) => 1000 + shift; // arbitrary monotonic base

        public static int DayNumberOfDate(int year, int month, int day)
        {
            int y = year - 2026, m = month, d = day;
            return 1001 + (y * 360 + (m - 11) * 30 + (d - 2));
        }

        public static string DisplayDate(int year, int month, int day) =>
            year.ToString("D4") + "-" + month.ToString("D2") + "-" + day.ToString("D2");

        public static List<StudentIdentity> BuildStudentRoster()
        {
            string[,] core =
            {
                { "Ana Delgado",     "3", "0", "0", "0", "0", "1", "1", "Data Structures" },
                { "Marco Reyes",     "2", "4", "0", "1", "1", "0", "2", "Criminal Law" },
                { "Liza Santos",     "4", "1", "0", "2", "2", "1", "1", "Taxation Review" },
                { "Jerome Cruz",     "1", "7", "0", "3", "3", "3", "2", "Digital Sketching" },
                { "Bea Villanueva",  "3", "3", "1", "4", "4", "2", "1", "Anatomy" },
                { "Carlo Mendoza",   "2", "0", "0", "5", "5", "0", "2", "Networking" },
                { "Trisha Ramos",    "4", "2", "0", "0", "3", "2", "1", "Media Ethics" },
                { "Miguel Bautista", "1", "5", "1", "1", "4", "0", "2", "Intro Psychology" },
                { "Grace Lim",       "3", "6", "0", "2", "5", "1", "1", "Genetics" },
                { "Paolo Soriano",   "2", "1", "1", "3", "0", "0", "2", "Bookkeeping" },
                { "Kim Alonzo",      "4", "4", "0", "4", "1", "3", "1", "Forensics" },
                { "Rica Fernandez",  "1", "2", "0", "5", "2", "2", "2", "Public Speaking" },
                { "Diego Navarro",   "3", "5", "0", "0", "4", "0", "1", "Abnormal Psych" },
                { "Faith Gonzales",  "2", "3", "1", "1", "5", "1", "2", "Pharmacology" },
            };

            var list = new List<StudentIdentity>();
            for (int i = 0; i < TotalStudents; i++)
            {
                int year = int.Parse(core[i, 1]);
                int enrollYear = 2027 - year;
                var id = new StudentIdentity
                {
                    FullName = core[i, 0],
                    IdNumber = "HB-" + enrollYear + "-" + (101 + i * 37).ToString("D4"),
                    YearLevel = year,
                    CourseIndex = int.Parse(core[i, 2]),
                    IsTransferee = core[i, 3] == "1",
                    Portrait = new Portrait
                    {
                        HairStyle = int.Parse(core[i, 4]),
                        FaceVariant = int.Parse(core[i, 5]),
                        Accessory = int.Parse(core[i, 6]),
                        Distorted = false,
                    },
                    FirstClassroom = core[i, 7] == "1" ? RoomId.Classroom1 : RoomId.Classroom2,
                    FirstClassSubject = core[i, 8],
                    Greeting = Greetings[i % Greetings.Length],
                };
                list.Add(id);
            }
            return list;
        }

        public static readonly string[] Greetings =
        {
            "Good evening. Night class.",
            "Hi… I'm running a bit late.",
            "Evening! Cold out there.",
            "Let me in quick, please.",
            "Good evening, officer… I mean, custodian.",
            "Here's my ID.",
            "Evening. Same routine as always.",
        };

        public static readonly string[] AnomalyGreetings =
        {
            "Good evening. I am a student of this school.",
            "…Evening.",
            "Let me through. I belong here.",
            "My documents are in order.",
            "You will let me in tonight.",
            "I attend classes here. Everyone does.",
        };

        public static readonly string[] AdminLines =
        {
            "Custodian. I'm the district administrator. I'll be taking control of this facility tonight.",
            "My credentials are in order. Badge and roster memo, as your instructions require.",
            "There have been… complaints about your performance. Let's not add to them.",
            "Open the checkpoint. That is an order from the district.",
        };
    }
}
````

### `_Project/Scripts/GameLogic/DisplayPolicy.cs` (3.3 KiB)

````csharp
using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    [Serializable]
    public struct ModeSize : IEquatable<ModeSize>
    {
        public int W;
        public int H;

        public ModeSize(int w, int h) { W = w; H = h; }

        public bool Equals(ModeSize o) => W == o.W && H == o.H;
        public override bool Equals(object obj) => obj is ModeSize m && Equals(m);
        public override int GetHashCode() => W * 397 ^ H;
        public override string ToString() => W + "×" + H;

        public float Aspect => H > 0 ? (float)W / H : 0f;
    }

    public static class DisplayPolicy
    {
        public const int RevertSeconds = 15;

        public const int MinWidth = 1024;
        public const int MinHeight = 600;

        public static readonly int[] PreferredHeights = { 720, 900, 1080, 1440 };

        public static List<ModeSize> Supported(IEnumerable<ModeSize> all)
        {
            var seen = new HashSet<ModeSize>();
            var list = new List<ModeSize>();
            if (all == null) return list;
            foreach (var m in all)
            {
                if (m.W < MinWidth || m.H < MinHeight) continue;
                if (seen.Add(m)) list.Add(m);
            }
            list.Sort((a, b) =>
            {
                int byArea = (a.W * a.H).CompareTo(b.W * b.H);
                return byArea != 0 ? byArea : a.W.CompareTo(b.W);
            });
            return list;
        }

        public static List<ModeSize> MenuChoices(IReadOnlyList<ModeSize> supported, ModeSize current, int max = 5)
        {
            var outList = new List<ModeSize>();
            if (supported == null) return outList;

            foreach (int h in PreferredHeights)
            {
                ModeSize best = default;
                bool found = false;
                foreach (var m in supported)
                {
                    if (m.H != h) continue;
                    if (!found || m.W > best.W) { best = m; found = true; }
                }
                if (found && !outList.Contains(best)) outList.Add(best);
            }

            if (current.H >= MinHeight && current.W >= MinWidth && !outList.Contains(current))
                outList.Add(current);

            outList.Sort((a, b) => (a.W * a.H).CompareTo(b.W * b.H));
            while (outList.Count > max) outList.RemoveAt(outList.Count - 1);
            return outList;
        }

        public static bool IsSupported(IEnumerable<ModeSize> supported, int w, int h)
        {
            if (supported == null) return false;
            foreach (var m in supported) if (m.W == w && m.H == h) return true;
            return false;
        }

        public static ModeSize Fallback(IReadOnlyList<ModeSize> supported, ModeSize preferred)
        {
            if (supported == null || supported.Count == 0) return preferred;
            ModeSize best = supported[0];
            float bestScore = float.MaxValue;
            foreach (var m in supported)
            {
                float aspectPenalty = Math.Abs(m.Aspect - preferred.Aspect) * 10000f;
                float areaPenalty = Math.Abs(m.W * m.H - preferred.W * preferred.H) / 100000f;
                float score = aspectPenalty + areaPenalty;
                if (score < bestScore) { bestScore = score; best = m; }
            }
            return best;
        }
    }
}
````

### `_Project/Scripts/GameLogic/EndingRules.cs` (4.0 KiB)

````csharp
namespace Hollowbrook.Logic
{
    public static class EndingRules
    {
        public const int LoseThreshold = 3;

        public class Evaluation
        {
            public EndingType Ending;
            public string ReasonKey;   // stable key for UI/captions/tests
            public string ReasonText;
        }

        public static Evaluation Evaluate(GameSession s)
        {
            if (s.AdminAllowed)
                return Result(EndingType.Lose, "admin_breach",
                    "You let the administrator through. It walked straight to the server room, and the identification system came online. By morning the district had already replaced you.");
            if (s.Breaches >= LoseThreshold)
                return Result(EndingType.Lose, "breaches",
                    "Three anomalies entered Hollowbrook on your watch. The district pulled your contract before dawn.");
            if (s.FalseRejections >= LoseThreshold)
                return Result(EndingType.Lose, "false_rejections",
                    "Three legitimate students were turned away at your window. The complaints reached the district before your explanation did.");

            if (s.Mode == GameMode.Normal && s.CompletedShifts >= ShiftPlan.TotalShifts)
            {
                if (s.Clues.SecretConditionsMet)
                    return Result(EndingType.Secret, "secret",
                        "Ten findings, in the order they were meant to be found. You leave Hollowbrook carrying what the official explanation never included — and what no custodian before you ever discovered.");
                return Result(EndingType.Win, "win",
                    "Five nights. You trusted your own judgment, kept the checkpoint, and rejected the thing wearing the administrator's face. You walk out of Hollowbrook relieved — and alive.");
            }
            return Result(EndingType.None, "continue", "");
        }

        public static Evaluation EvaluateExhibition(GameSession s)
        {
            var lose = Evaluate(s);
            if (lose.Ending == EndingType.Lose) return lose;
            if (s.Clues.SecretConditionsMet)
                return Result(EndingType.Secret, "secret", lose.ReasonText);
            return Result(EndingType.Win, "exhibition",
                "The bell rings. Four faces, one of them wrong, and the wrong one did not get in. " +
                "Hollowbrook keeps its secrets one more night - demo complete, thank you for playing.");
        }

        private static Evaluation Result(EndingType t, string key, string text) =>
            new Evaluation { Ending = t, ReasonKey = key, ReasonText = text };
    }

    public static class EndlessRules
    {
        public const float StartSeconds = 150f;
        public const float TimePerAnomalyRejected = 20f;
        public const float TimePerLegitAllowed = 8f;
        public const float TimePenaltyForMistake = 12f;
        public const float HardCapSeconds = 420f;

        public static float AnomalyChance(int processedSoFar)
        {
            float c = 0.15f + processedSoFar * 0.02f;
            return c > 0.6f ? 0.6f : c;
        }

        public static int ScoreFor(InspectionRules.Outcome o, int streakAfter)
        {
            int baseScore = o.WasAnomaly ? 100 : 50;
            return o.Correct ? (int)(baseScore * StreakMultiplier(streakAfter)) : 0;
        }

        public static float StreakMultiplier(int streak)
        {
            float m = 1f + (streak / 3) * 0.5f; // integer division: +0.5x every 3 streak
            return m > 4f ? 4f : m;
        }
    }

    public static class ChallengeRules
    {
        public const int VisitorsPerRuleWave = 4;
        public const float ScoreMultiplier = 2f;
        public const float StartSeconds = EndlessRules.StartSeconds;

        public static RuleMutationDef RollMutation(int waveIndex, GameRng rng)
        {
            if (waveIndex <= 0) return ShiftPlan.ChallengeMutations[0];
            int idx = rng.NextInt(1, ShiftPlan.ChallengeMutations.Length);
            return ShiftPlan.ChallengeMutations[idx];
        }
    }
}
````

### `_Project/Scripts/GameLogic/Enums.cs` (1.8 KiB)

````csharp
namespace Hollowbrook.Logic
{
    public enum GameMode { Normal, Endless, Challenge }

    public enum Verdict { None, Allow, Reject }

    public enum EndingType { None, Win, Lose, Secret }

    public enum SanityStage { Stable, Uneasy, Shaken, Breaking }

    public enum LightMode { Off, Sweep, Focus }

    public enum AnomalyBehaviorKind { Blink, Slide, Sprint, CoverHug }

    public enum RoomId { Security, Hallway, Classroom1, Classroom2 }

    public enum QuestionId { Course, Year, Status, FirstClassroom }

    public enum DefectType
    {
        None = 0,
        PhotoMismatch,        // window face clearly differs from ID photo
        PhotoSubtleMismatch,  // single feature differs (harder, shift 3+)
        ExpiredId,            // expiration date is before the shift date
        IdFormatInvalid,      // ID number breaks the HB-YYYY-NNNN format
        YearMismatch,         // year level on ID != year level on record
        CourseMismatch,       // course on ID != course on record
        StatusMismatch,       // regular/transferee on ID != record
        RecordNameMismatch,   // name on record != name on ID
        NotInDatabase,        // ID number not in the student registry (ghost student)
        DuplicateId,          // ID number already used by an earlier visitor (shift 3+)
        BehaviorOddity,       // documents clean; questioning/behavior gives it away
    }

    public enum ToneTag { Normal, Hesitant, Flat, TooSmooth, Echoing }

    public enum AnomalyBehavior
    {
        Idle,
        ApproachingWindow,
        WaitingOutside,
        InsideWandering,
        ReachedServerRoom,
    }

    public enum AppViewState
    {
        Boot, Loading, MainMenu, ModeSelect, Settings, Controls, Credits,
        ShiftIntro, Checkpoint, Exploration, Map, Pause, ShiftResults,
        AdminEncounter, SecurityRoom, Ending, EndlessResults,
    }
}
````

### `_Project/Scripts/GameLogic/GameRng.cs` (1.1 KiB)

````csharp
using System;

namespace Hollowbrook.Logic
{
    public sealed class GameRng
    {
        private readonly Random _rand;
        public int Seed { get; }

        public GameRng(int seed)
        {
            Seed = seed;
            _rand = new Random(seed);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return _rand.Next(minInclusive, maxExclusive);
        }

        public float NextFloat() => (float)_rand.NextDouble();

        public bool Chance(float probability) => NextFloat() < probability;

        public T Pick<T>(T[] items)
        {
            if (items == null || items.Length == 0)
                throw new ArgumentException("Pick called with empty collection");
            return items[_rand.Next(items.Length)];
        }

        public void Shuffle<T>(System.Collections.Generic.List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rand.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
````

### `_Project/Scripts/GameLogic/GameSession.cs` (8.2 KiB)

````csharp
using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    [Serializable]
    public class ShiftSummary
    {
        public int Shift;
        public int Visitors;
        public int Correct;
        public int FalseRejections;
        public int Breaches;
        public float SanityEnd;

        public string Encode() =>
            Shift + ":" + Visitors + ":" + Correct + ":" + FalseRejections + ":" + Breaches + ":" +
            SanityEnd.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

        public static ShiftSummary Decode(string s)
        {
            var parts = s.Split(':');
            if (parts.Length != 6) return null;
            return new ShiftSummary
            {
                Shift = int.Parse(parts[0]),
                Visitors = int.Parse(parts[1]),
                Correct = int.Parse(parts[2]),
                FalseRejections = int.Parse(parts[3]),
                Breaches = int.Parse(parts[4]),
                SanityEnd = float.Parse(parts[5], System.Globalization.CultureInfo.InvariantCulture),
            };
        }
    }

    public class GameSession
    {
        public GameMode Mode = GameMode.Normal;
        public int Seed = 20261102;
        public int CurrentShift = 1;        // Normal: shift being played (1..5)
        public int CompletedShifts = 0;     // Normal: shifts fully completed
        public int Processed = 0;           // visitors processed this run
        public int CorrectDecisions = 0;
        public int FalseRejections = 0;
        public int Breaches = 0;            // anomalies allowed in (includes administrator if allowed)
        public int AnomaliesRejected = 0;
        public int TokensCollected = 0;
        public int VisitorIndexAtSave = 0;   // where a mid-shift continue resumes      // fully-tracked anomalies (master prompt §3)
        public bool AdminAllowed = false;
        public bool AdminRejected = false;
        public float Sanity = SanityModel.Max;
        public int Score = 0;
        public int Streak = 0;
        public int BestStreak = 0;
        public float RunTimer = 0f;         // Endless/Challenge seconds remaining
        public int RunProcessed = 0;        // Endless/Challenge visitors this run
        public string ActiveMutationKey = "none";
        public ClueTracker Clues = new ClueTracker();
        public PatrolPool Pool = new PatrolPool();   // outstanding anomalies tonight
        public List<ShiftSummary> History = new List<ShiftSummary>();
        public bool RunFinished = false;
        public PresentationLog TonightLog = new PresentationLog();

        public SanityStage SanityStage => SanityModel.StageFor(Sanity);

        public RuleMutationDef ActiveMutation
        {
            get
            {
                foreach (var m in ShiftPlan.ChallengeMutations)
                    if (m.Key == ActiveMutationKey) return m;
                return ShiftPlan.ChallengeMutations[0];
            }
        }

        public void RecordVerdict(InspectionRules.Outcome o)
        {
            if (o == null || o.Verdict == Verdict.None) return;
            Processed++;
            if (o.Correct)
            {
                CorrectDecisions++;
                Streak++;
                if (Streak > BestStreak) BestStreak = Streak;
                if (o.WasAnomaly) AnomaliesRejected++;
            }
            else
            {
                Streak = 0;
            }
            if (o.FalseRejection)
            {
                FalseRejections++;
                ApplySanity(-6f); // mistakes are disturbing events (§4.3)
            }
            if (o.AnomalyBreach)
            {
                Breaches++;
                ApplySanity(-8f);
                if (o.WasAdministrator) AdminAllowed = true;
            }
            if (o.WasAdministrator && o.Verdict == Verdict.Reject)
            {
                AdminRejected = true;
                ApplySanity(+10f);
            }
        }

        public void ApplySanity(float delta)
        {
            if (delta < 0) delta *= SanityModel.DrainMultiplier(Mode);
            Sanity = SanityModel.Clamp(Sanity + delta);
        }

        public ClueDiscoveryResult DiscoverClue(string key)
        {
            var res = Clues.Discover(key);
            if (res.FirstTime && res.Def != null) ApplySanity(res.Def.SanityDelta);
            return res;
        }

        public void MarkRoomInvestigated(RoomId room) => Clues.MarkRoomInvestigated(room);

        public float ApplyRunTimer(InspectionRules.Outcome o)
        {
            float delta;
            if (o.Correct)
                delta = o.WasAnomaly ? EndlessRules.TimePerAnomalyRejected : EndlessRules.TimePerLegitAllowed;
            else
                delta = -EndlessRules.TimePenaltyForMistake;
            RunTimer = Math.Min(RunTimer + delta, EndlessRules.HardCapSeconds);
            return delta;
        }

        public void CompleteShift()
        {
            History.Add(new ShiftSummary
            {
                Shift = CurrentShift,
                Visitors = Processed, // cumulative; UI shows per-shift deltas
                Correct = CorrectDecisions,
                FalseRejections = FalseRejections,
                Breaches = Breaches,
                SanityEnd = Sanity,
            });
            CompletedShifts++;
            CurrentShift++;
        }

        public EndingRules.Evaluation EvaluateEnding() => EndingRules.Evaluate(this);

        public bool LoseThresholdReached =>
            AdminAllowed || Breaches >= EndingRules.LoseThreshold || FalseRejections >= EndingRules.LoseThreshold;
    }

    [Serializable]
    public class SettingsData
    {
        public float MasterVolume = 0.8f;  // 0..1
        public float MusicVolume = 0.6f;   // 0..1
        public float SfxVolume = 0.8f;     // 0..1
        public int TextScale = 1;          // 0 Small, 1 Normal, 2 Large
        public bool ReduceMotion = false;  // disables camera shake / sprint lunges / pulsing
        public bool PhotosensitivitySafe = false; // no strobe flashes; scares fade instead of popping
        public bool Captions = true;       // text captions for significant audio events
        public bool Fullscreen = true;     // display: exclusive fullscreen vs windowed
        public int DisplayWidth = 0;       // stored resolution preference; 0 = "no preference, keep launcher default"
        public int DisplayHeight = 0;

        public SettingsData Clone() => (SettingsData)MemberwiseClone();

        public static float TextScaleMultiplier(int textScale)
        {
            switch (textScale) { case 0: return 0.85f; case 2: return 1.25f; default: return 1f; }
        }

        public string Encode() =>
            Inv(MasterVolume) + "," + Inv(MusicVolume) + "," + Inv(SfxVolume) + "," +
            TextScale + "," + B(ReduceMotion) + "," + B(PhotosensitivitySafe) + "," + B(Captions) +
            "," + B(Fullscreen) + "," + DisplayWidth + "," + DisplayHeight;

        public static SettingsData Decode(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var p = s.Split(',');
            if (p.Length < 7) return null;
            var d = new SettingsData
            {
                MasterVolume = F(p[0]), MusicVolume = F(p[1]), SfxVolume = F(p[2]),
                TextScale = int.Parse(p[3]),
                ReduceMotion = p[4] == "1", PhotosensitivitySafe = p[5] == "1", Captions = p[6] == "1",
            };
            if (p.Length >= 10)
            {
                d.Fullscreen = p[7] != "0";
                int w, h;
                if (int.TryParse(p[8], out w) && int.TryParse(p[9], out h))
                {
                    d.DisplayWidth = w;
                    d.DisplayHeight = h;
                }
            }
            return d;
        }

        private static string Inv(float f) => f.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        private static float F(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        private static string B(bool b) => b ? "1" : "0";
    }

    [Serializable]
    public class RecordsData
    {
        public int EndlessBest = 0;
        public int ChallengeBest = 0;
        public List<string> EndingsSeen = new List<string>(); // "win", "lose", "secret"
    }
}
````

### `_Project/Scripts/GameLogic/IdRules.cs` (3.3 KiB)

````csharp
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    public static class IdRules
    {
        public const string Prefix = "HB-";

        public static bool IsFormatValid(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (id.Length != 12) return false;                 // HB-YYYY-NNNN
            if (id[0] != 'H' || id[1] != 'B' || id[2] != '-') return false;
            if (id[7] != '-') return false;
            for (int i = 3; i <= 6; i++) if (!char.IsDigit(id[i])) return false;
            for (int i = 8; i <= 11; i++) if (!char.IsDigit(id[i])) return false;
            int year = int.Parse(id.Substring(3, 4));
            int serial = int.Parse(id.Substring(8, 4));
            if (year < 2023 || year > 2027) return false;      // plausible enrollment years only
            if (serial < 1 || serial > 9999) return false;     // 0000 is not issued
            return true;
        }

        public static int SerialOf(string id)
        {
            if (!IsFormatValid(id)) return -1;
            return int.Parse(id.Substring(8, 4));
        }

        public static bool IsExpired(VisitorData v, int shiftNumber)
        {
            return v.ExpirationDay < Content.ShiftDayNumber(shiftNumber);
        }

        public static bool DatabaseLookup(string id, ISet<string> registry)
        {
            return registry != null && !string.IsNullOrEmpty(id) && registry.Contains(id);
        }

        public static List<DefectType> EvaluateDocuments(VisitorData v, ISet<string> registry,
            PresentationLog playLog)
        {
            if (v.IsAdministrator)
            {
                var adminDefects = new List<DefectType>();
                if (v.ClaimedName != v.RecordName) adminDefects.Add(DefectType.RecordNameMismatch);
                if (v.IdNumber != v.RecordIdNumber) adminDefects.Add(DefectType.NotInDatabase);
                return adminDefects;
            }

            var found = new List<DefectType>();
            if (!v.PhotoMatches)
                found.Add(WindowPortraitIsSubtlyOff(v) ? DefectType.PhotoSubtleMismatch : DefectType.PhotoMismatch);
            if (!IsFormatValid(v.IdNumber)) found.Add(DefectType.IdFormatInvalid);
            if (IsExpired(v, v.ShiftNumber)) found.Add(DefectType.ExpiredId);
            if (v.IdYearLevel != v.RecordYearLevel) found.Add(DefectType.YearMismatch);
            if (v.IdCourseIndex != v.RecordCourseIndex) found.Add(DefectType.CourseMismatch);
            if (v.IdIsTransferee != v.RecordIsTransferee) found.Add(DefectType.StatusMismatch);
            if (v.ClaimedName != v.RecordName) found.Add(DefectType.RecordNameMismatch);
            if (!DatabaseLookup(v.IdNumber, registry)) found.Add(DefectType.NotInDatabase);
            if (playLog != null && IsFormatValid(v.IdNumber) &&
                playLog.WasPresentedWithDifferentCard(v.IdNumber, v.IdPhoto))
                found.Add(DefectType.DuplicateId);
            return found;
        }

        private static bool WindowPortraitIsSubtlyOff(VisitorData v)
        {
            int diffs = 0;
            if (v.WindowPortrait.HairStyle != v.IdPhoto.HairStyle) diffs++;
            if (v.WindowPortrait.FaceVariant != v.IdPhoto.FaceVariant) diffs++;
            if (v.WindowPortrait.Accessory != v.IdPhoto.Accessory) diffs++;
            return diffs == 1;
        }
    }
}
````

### `_Project/Scripts/GameLogic/InspectionRules.cs` (3.4 KiB)

````csharp
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    public static class QuestionSystem
    {
        public static readonly QuestionId[] All =
        {
            QuestionId.Course, QuestionId.Year, QuestionId.Status, QuestionId.FirstClassroom,
        };

        public static string QuestionText(QuestionId q)
        {
            switch (q)
            {
                case QuestionId.Course: return "What's your course?";
                case QuestionId.Year: return "What year are you in?";
                case QuestionId.Status: return "Are you a regular student or a transferee?";
                default: return "What classroom is your first class in?";
            }
        }

        public static Answer Resolve(VisitorData v, QuestionId q)
        {
            if (v.Answers != null && v.Answers.TryGetValue(q, out var a)) return a;
            return new Answer { Text = "…", Tone = ToneTag.Flat, ContradictsDocuments = false };
        }

        public class Budget
        {
            private readonly HashSet<QuestionId> _asked = new HashSet<QuestionId>();
            public int Max { get; }
            public int Asked => _asked.Count;
            public int Remaining => Max - _asked.Count;
            public bool HasAsked(QuestionId q) => _asked.Contains(q);

            public Budget(int max = Content.MaxQuestionsPerInspection) { Max = max; }

            public bool TryAsk(VisitorData v, QuestionId q, out Answer answer)
            {
                answer = default;
                if (_asked.Contains(q)) return false;      // no repeats
                if (_asked.Count >= Max) return false;     // budget spent
                _asked.Add(q);
                answer = Resolve(v, q);
                return true;
            }

            public void Reset() => _asked.Clear();
        }
    }

    public static class InspectionRules
    {
        public class Outcome
        {
            public Verdict Verdict;
            public Verdict Expected;
            public bool Correct;
            public bool FalseRejection;   // legit rejected (not via an official rule mutation)
            public bool AnomalyBreach;    // anomaly allowed in
            public bool WasAnomaly;
            public bool WasAdministrator;
            public List<string> RevealedDefects = new List<string>();
        }

        public static Verdict ExpectedVerdict(VisitorData v, RuleMutationDef activeMutation)
        {
            if (activeMutation != null && activeMutation.ForcesReject(v)) return Verdict.Reject;
            return v.IsAnomaly ? Verdict.Reject : Verdict.Allow;
        }

        public static Outcome Apply(VisitorData v, Verdict verdict, RuleMutationDef activeMutation)
        {
            var expected = ExpectedVerdict(v, activeMutation);
            bool forcedByRule = activeMutation != null && activeMutation.ForcesReject(v);
            var o = new Outcome
            {
                Verdict = verdict,
                Expected = expected,
                Correct = verdict == expected,
                FalseRejection = verdict == Verdict.Reject && !v.IsAnomaly && !forcedByRule,
                AnomalyBreach = verdict == Verdict.Allow && v.IsAnomaly,
                WasAnomaly = v.IsAnomaly,
                WasAdministrator = v.IsAdministrator,
                RevealedDefects = v.DescribeDefects(),
            };
            return o;
        }
    }
}
````

### `_Project/Scripts/GameLogic/PatrolPool.cs` (1.6 KiB)

````csharp
using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    [Serializable]
    public class PatrolPool
    {
        private readonly List<AnomalyBehaviorKind> _pending = new List<AnomalyBehaviorKind>();

        public int Pending => _pending.Count;
        public IReadOnlyList<AnomalyBehaviorKind> PendingList => _pending;

        public void Seed(IEnumerable<AnomalyBehaviorKind> kinds)
        {
            _pending.Clear();
            if (kinds != null) _pending.AddRange(kinds);
        }

        public AnomalyBehaviorKind PeekKind(GameRng rng)
        {
            if (_pending.Count == 0) return (AnomalyBehaviorKind)rng.NextInt(0, 4);
            return _pending[rng.NextInt(0, _pending.Count)];
        }

        public void Resolve(AnomalyBehaviorKind kind) => _pending.Remove(kind);

        public bool TryTake(GameRng rng, out AnomalyBehaviorKind kind)
        {
            kind = AnomalyBehaviorKind.Blink;
            if (_pending.Count == 0) return false;
            int i = rng.NextInt(0, _pending.Count);
            kind = _pending[i];
            _pending.RemoveAt(i);
            return true;
        }

        public string Encode() => string.Join(",", _pending.ConvertAll(k => ((int)k).ToString()));

        public void Decode(string s)
        {
            _pending.Clear();
            if (string.IsNullOrEmpty(s)) return;
            foreach (var part in s.Split(','))
            {
                int v;
                if (int.TryParse(part, out v) && v >= 0 && v <= 3) _pending.Add((AnomalyBehaviorKind)v);
            }
        }
    }
}
````

### `_Project/Scripts/GameLogic/PatrolRules.cs` (1.5 KiB)

````csharp
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    public static class PatrolRules
    {
        public const int LateArrivals = 3;

        public static readonly RuleMutationDef OfficeRule = new RuleMutationDef(
            "office_late_list",
            "OFFICE RULE: admit only students on tonight's late list whose ID is NOT expired.");

        public static Verdict ExpectedOfficeVerdict(VisitorData v) =>
            InspectionRules.ExpectedVerdict(v, OfficeRule);

        public static List<VisitorData> BuildLateQueue(RosterFactory roster, ShiftDef shift,
            PatrolPool pool, GameRng rng)
        {
            var q = new List<VisitorData>();
            if (roster == null || shift == null) return q;

            int fromPool = System.Math.Min(pool != null ? pool.Pending : 0, rng.NextInt(0, 2));
            for (int i = 0; i < fromPool; i++)
            {
                AnomalyBehaviorKind kind;
                if (pool == null || !pool.TryTake(rng, out kind)) break;
                var v = roster.BuildLateStudent(roster.PickIdentity(rng), shift, true);
                q.Add(v);
            }

            int legit = LateArrivals - q.Count;
            for (int i = 0; i < legit; i++)
            {
                var v = roster.BuildLateStudent(roster.PickIdentity(rng), shift, false);
                if (i == legit - 1 && legit > 1) v.OnLateList = false;  // the rule has a case to act on
                q.Add(v);
            }

            rng.Shuffle(q);
            return q;
        }
    }
}
````

### `_Project/Scripts/GameLogic/PresentationLog.cs` (3.0 KiB)

````csharp
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    public class PresentationLog
    {
        private readonly Dictionary<string, Portrait> _firstCardPhoto = new Dictionary<string, Portrait>();
        private readonly Dictionary<string, string> _labels = new Dictionary<string, string>();

        public int Count => _firstCardPhoto.Count;
        public bool WasPresented(string id) => !string.IsNullOrEmpty(id) && _firstCardPhoto.ContainsKey(id);

        public bool WasPresentedWithDifferentCard(string id, Portrait cardPhoto) =>
            !string.IsNullOrEmpty(id) && _firstCardPhoto.TryGetValue(id, out var p) && p != cardPhoto;

        public string LabelFor(string id) => _labels.TryGetValue(id ?? "", out var l) ? l : "";

        public void Record(string id, Portrait cardPhoto, string label)
        {
            if (string.IsNullOrEmpty(id) || _firstCardPhoto.ContainsKey(id)) return;
            _firstCardPhoto[id] = cardPhoto;
            _labels[id] = label ?? "";
        }

        public void RecordFrom(VisitorData v, string label)
        {
            if (v == null || !IdRules.IsFormatValid(v.IdNumber)) return;
            Record(v.IdNumber, v.IdPhoto, label);
        }

        public void Clear()
        {
            _firstCardPhoto.Clear();
            _labels.Clear();
        }

        public List<string> PresentedIds()
        {
            var l = new List<string>(_firstCardPhoto.Keys);
            l.Sort(System.StringComparer.Ordinal);
            return l;
        }

        public Portrait? FirstCardPhotoOf(string id) =>
            _firstCardPhoto.TryGetValue(id ?? "", out var p) ? p : (Portrait?)null;

        public string Encode()
        {
            var sb = new System.Text.StringBuilder();
            var ids = PresentedIds();
            for (int i = 0; i < ids.Count; i++)
            {
                var p = _firstCardPhoto[ids[i]];
                if (i > 0) sb.Append('|');
                sb.Append(ids[i]).Append(':')
                  .Append(p.HairStyle).Append(',').Append(p.FaceVariant).Append(',').Append(p.Accessory)
                  .Append(',').Append(_labels.TryGetValue(ids[i], out var lb) ? lb : "");
            }
            return sb.ToString();
        }

        public void Decode(string s)
        {
            Clear();
            if (string.IsNullOrEmpty(s)) return;
            foreach (var entry in s.Split('|'))
            {
                if (entry.Length == 0) continue;
                int colon = entry.IndexOf(':');
                if (colon <= 0) continue;
                string id = entry.Substring(0, colon);
                var parts = entry.Substring(colon + 1).Split(',');
                if (parts.Length < 4) continue;
                if (!int.TryParse(parts[0], out int h)) continue;
                if (!int.TryParse(parts[1], out int f)) continue;
                if (!int.TryParse(parts[2], out int a)) continue;
                Record(id, new Portrait { HairStyle = h, FaceVariant = f, Accessory = a }, parts[3]);
            }
        }
    }
}
````

### `_Project/Scripts/GameLogic/RosterFactory.cs` (20.1 KiB)

````csharp
using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    public class RosterFactory
    {
        private readonly List<StudentIdentity> _canon;
        private readonly HashSet<string> _registry = new HashSet<string>();
        private readonly GameRng _rng;
        private readonly PresentationLog _genLog = new PresentationLog();

        public IReadOnlyList<StudentIdentity> Canon => _canon;
        public ISet<string> Registry => _registry;
        public PresentationLog GenLog => _genLog;

        public RosterFactory(int seed)
        {
            _rng = new GameRng(seed);
            _canon = Content.BuildStudentRoster();
            foreach (var s in _canon) _registry.Add(s.IdNumber);
        }

        public List<VisitorData> GenerateShift(ShiftDef shift)
        {
            _genLog.Clear(); // duplicates are a "tonight" rule in story mode
            var visitors = new List<VisitorData>();
            var identities = PickDistinctIdentities(shift.VisitorCount);
            var anomalySlots = PickAnomalySlots(shift.VisitorCount, shift.AnomalyCount);

            for (int i = 0; i < shift.VisitorCount; i++)
            {
                var id = identities[i];
                bool anomaly = anomalySlots.Contains(i);
                var v = anomaly ? BuildAnomaly(id, shift) : BuildLegit(id, shift);
                v.VisitorIndex = i;
                v.ShiftNumber = shift.Number;
                visitors.Add(v);
                _genLog.RecordFrom(v, "earlier tonight");
            }
            if (shift.HasAdministrator)
            {
                var admin = BuildAdministrator(shift);
                admin.VisitorIndex = shift.VisitorCount;
                visitors.Add(admin);
            }
            return visitors;
        }

        private List<StudentIdentity> PickDistinctIdentities(int count)
        {
            var pool = new List<StudentIdentity>(_canon);
            _rng.Shuffle(pool);
            return pool.GetRange(0, Math.Min(count, pool.Count));
        }

        private HashSet<int> PickAnomalySlots(int visitorCount, int anomalyCount)
        {
            var slots = new HashSet<int>();
            if (anomalyCount <= 0) return slots;
            var candidates = new List<int>();
            for (int i = 0; i < visitorCount; i++) candidates.Add(i);
            _rng.Shuffle(candidates);
            for (int i = 0; i < anomalyCount && i < candidates.Count; i++) slots.Add(candidates[i]);
            return slots;
        }

        private VisitorData BuildLegit(StudentIdentity id, ShiftDef shift)
        {
            bool nearExpiry = (shift.Number >= 2 || shift.Number == 0) && _rng.Chance(0.25f);
            int expDay = nearExpiry
                ? Content.DayNumberOfDate(2026, 11, 20 + _rng.NextInt(0, 20))
                : Content.DayNumberOfDate(2027, 3, 31);

            var v = new VisitorData
            {
                IsAnomaly = false,
                ClaimedName = id.FullName,
                RecordName = id.FullName,
                IdNumber = id.IdNumber,
                RecordIdNumber = id.IdNumber,
                IdYearLevel = id.YearLevel,
                RecordYearLevel = id.YearLevel,
                IdCourseIndex = id.CourseIndex,
                RecordCourseIndex = id.CourseIndex,
                IdIsTransferee = id.IsTransferee,
                RecordIsTransferee = id.IsTransferee,
                ExpirationDay = expDay,
                WindowPortrait = id.Portrait,
                IdPhoto = id.Portrait,
                InDatabase = true,
                RecordFirstClassroom = id.ClassroomString,
                Greeting = id.Greeting,
            };
            v.ExpirationDisplay = DisplayOf(expDay);
            BuildAnswers(v, id, truthful: true);
            return v;
        }

        private static string DisplayOf(int expDay)
        {
            int offset = expDay - Content.DayNumberOfDate(2026, 11, 2); // days after Nov 2 2026
            var date = new DateTime(2026, 11, 2).AddDays(offset);
            return Content.DisplayDate(date.Year, date.Month, date.Day);
        }

        private VisitorData BuildAnomaly(StudentIdentity id, ShiftDef shift)
        {
            var v = new VisitorData
            {
                IsAnomaly = true,
                ClaimedName = id.FullName,
                RecordName = id.FullName,
                IdNumber = id.IdNumber,
                RecordIdNumber = id.IdNumber,
                IdYearLevel = id.YearLevel,
                RecordYearLevel = id.YearLevel,
                IdCourseIndex = id.CourseIndex,
                RecordCourseIndex = id.CourseIndex,
                IdIsTransferee = id.IsTransferee,
                RecordIsTransferee = id.IsTransferee,
                ExpirationDay = Content.DayNumberOfDate(2027, 3, 31),
                WindowPortrait = id.Portrait,
                IdPhoto = id.Portrait,
                InDatabase = true,
                RecordFirstClassroom = id.ClassroomString,
                Greeting = _rng.Pick(Content.AnomalyGreetings),
            };
            v.ExpirationDisplay = DisplayOf(v.ExpirationDay);

            int defectCount = _rng.NextInt(1, shift.MaxDefectsPerAnomaly + 1);
            var pool = BuildDefectPool(shift);
            _rng.Shuffle(pool);

            bool idDefectTaken = false; // {IdFormatInvalid, NotInDatabase, DuplicateId} are exclusive
            bool photoDefectTaken = false; // {PhotoMismatch, PhotoSubtleMismatch} are exclusive
            int applied = 0;
            foreach (var d in pool)
            {
                if (applied >= defectCount) break;
                bool isIdFamily = d == DefectType.IdFormatInvalid || d == DefectType.NotInDatabase || d == DefectType.DuplicateId;
                bool isPhotoFamily = d == DefectType.PhotoMismatch || d == DefectType.PhotoSubtleMismatch;
                if (isIdFamily && idDefectTaken) continue;
                if (isPhotoFamily && photoDefectTaken) continue;
                if (d == DefectType.DuplicateId && !HasDuplicateCandidate(id)) continue;
                if (!ApplyDefect(v, d, id)) continue;
                applied++;
                if (isIdFamily) idDefectTaken = true;
                if (isPhotoFamily) photoDefectTaken = true;
            }
            if (applied == 0) // guarantee the fairness invariant: anomalies always carry a defect
                ApplyDefect(v, DefectType.ExpiredId, id);

            v.InDatabase = IdRules.IsFormatValid(v.IdNumber) && _registry.Contains(v.IdNumber);
            bool behaviorOnly = v.Defects.Count == 1 && v.Defects[0] == DefectType.BehaviorOddity;
            BuildAnswers(v, id, truthful: false, behaviorOnly);
            if (behaviorOnly && v.BehaviorGlitchVisible)
            {
                var p = v.WindowPortrait; p.Distorted = true; v.WindowPortrait = p;
            }
            return v;
        }

        private bool HasDuplicateCandidate(StudentIdentity self)
        {
            foreach (var kvId in _genLog.PresentedIds())
            {
                var first = _genLog.FirstCardPhotoOf(kvId);
                if (first.HasValue && first.Value != self.Portrait) return true;
            }
            return false;
        }

        private List<DefectType> BuildDefectPool(ShiftDef shift)
        {
            var pool = new List<DefectType>
            {
                DefectType.PhotoMismatch, DefectType.ExpiredId,
                DefectType.IdFormatInvalid, DefectType.RecordNameMismatch,
                DefectType.YearMismatch, DefectType.CourseMismatch, DefectType.StatusMismatch,
                DefectType.NotInDatabase,
            };
            if (shift.SubtleDefectsAllowed) pool.Add(DefectType.PhotoSubtleMismatch);
            if (shift.BehaviorOddityAllowed) pool.Add(DefectType.BehaviorOddity);
            if (shift.DuplicateIdsAllowed) pool.Add(DefectType.DuplicateId);
            return pool;
        }

        private bool ApplyDefect(VisitorData v, DefectType d, StudentIdentity id)
        {
            switch (d)
            {
                case DefectType.PhotoMismatch:
                    v.WindowPortrait = v.IdPhoto.Swapped();
                    break;
                case DefectType.PhotoSubtleMismatch:
                    v.WindowPortrait = v.IdPhoto.SubtleVariant(_rng);
                    break;
                case DefectType.ExpiredId:
                {
                    int month = _rng.NextInt(6, 11);
                    int day = _rng.NextInt(1, 26);
                    if (month == 10 && day > 20) day = 20; // strictly before the first shift night
                    v.ExpirationDay = Content.DayNumberOfDate(2026, month, day);
                    v.ExpirationDisplay = Content.DisplayDate(2026, month, day);
                    break;
                }
                case DefectType.IdFormatInvalid:
                    v.IdNumber = CorruptId(id.IdNumber);
                    break;
                case DefectType.YearMismatch:
                    v.RecordYearLevel = 1 + (id.YearLevel % 4); // guaranteed different, stays 1..4
                    break;
                case DefectType.CourseMismatch:
                    v.RecordCourseIndex = (id.CourseIndex + 1 + _rng.NextInt(0, Content.Courses.Length - 1))
                                          % Content.Courses.Length;
                    break;
                case DefectType.StatusMismatch:
                    v.RecordIsTransferee = !id.IsTransferee;
                    break;
                case DefectType.RecordNameMismatch:
                {
                    StudentIdentity other = _canon[_rng.NextInt(0, _canon.Count)];
                    if (other.FullName == id.FullName) other = _canon[(_canon.IndexOf(other) + 1) % _canon.Count];
                    v.RecordName = other.FullName;
                    break;
                }
                case DefectType.NotInDatabase:
                {
                    string candidate;
                    int guard = 0;
                    do { candidate = "HB-2026-" + _rng.NextInt(9000, 10000).ToString("D4"); guard++; }
                    while (_registry.Contains(candidate) && guard < 32);
                    v.IdNumber = candidate;
                    v.RecordIdNumber = candidate;
                    break;
                }
                case DefectType.DuplicateId:
                {
                    var candidates = new List<string>();
                    foreach (var sid in _genLog.PresentedIds())
                    {
                        var first = _genLog.FirstCardPhotoOf(sid);
                        if (first.HasValue && first.Value != v.IdPhoto) candidates.Add(sid);
                    }
                    if (candidates.Count == 0) return false;
                    _rng.Shuffle(candidates);
                    v.IdNumber = candidates[0];
                    v.RecordIdNumber = candidates[0];
                    v.DuplicateOfShift = "earlier tonight (" + _genLog.LabelFor(candidates[0]) + ")";
                    break;
                }
                case DefectType.BehaviorOddity:
                    v.BehaviorGlitchVisible = _rng.Chance(0.5f);
                    break;
                default:
                    return false;
            }
            v.Defects.Add(d);
            return true;
        }

        private string CorruptId(string valid)
        {
            switch (_rng.NextInt(0, 6))
            {
                case 0: return "HC-" + valid.Substring(3);                       // wrong prefix
                case 1: return valid.Replace("-", "");                            // missing dashes
                case 2: return valid.Substring(0, 10) + "A" + valid[11];          // letter in serial
                case 3: return valid.Substring(0, 11);                            // short
                case 4: return "HB-2099-" + valid.Substring(8);                   // implausible year
                default: return "HB-" + valid.Substring(3, 4) + "-0000";          // serial 0000
            }
        }

        private void BuildAnswers(VisitorData v, StudentIdentity id, bool truthful, bool behaviorOnly = false)
        {
            string courseAnswer = truthful ? id.CourseString : Content.Courses[v.IdCourseIndex];
            string yearAnswer = truthful ? id.YearString : Content.YearNames[v.IdYearLevel - 1];
            string statusAnswer = (truthful ? id.IsTransferee : v.IdIsTransferee) ? "Transferee" : "Regular";

            v.Answers[QuestionId.Course] = new Answer { Text = courseAnswer + ".", Tone = ToneFor(v, behaviorOnly) };
            v.Answers[QuestionId.Year] = new Answer { Text = yearAnswer + ".", Tone = ToneFor(v, behaviorOnly) };
            v.Answers[QuestionId.Status] = new Answer { Text = statusAnswer + " student.", Tone = ToneFor(v, behaviorOnly) };

            if (behaviorOnly)
            {
                string[] wrong =
                {
                    "Room… 204? I don't remember exactly.",
                    "My first class? It's… somewhere on the second floor.",
                    "I have study hall. Always.",
                    "Room 101. No — wait. Was it 101?",
                };
                v.Answers[QuestionId.FirstClassroom] = new Answer
                {
                    Text = _rng.Pick(wrong),
                    Tone = _rng.Chance(0.5f) ? ToneTag.Hesitant : ToneTag.Flat,
                    ContradictsDocuments = true,
                };
            }
            else
            {
                v.Answers[QuestionId.FirstClassroom] = new Answer
                {
                    Text = id.ClassroomString + ".",
                    Tone = ToneFor(v, behaviorOnly),
                    ContradictsDocuments = false,
                };
            }
        }

        private ToneTag ToneFor(VisitorData v, bool behaviorOnly)
        {
            if (!v.IsAnomaly) return ToneTag.Normal; // fairness: legit never leaks tone
            if (behaviorOnly && _rng.Chance(0.5f)) return ToneTag.Hesitant;
            if (_rng.Chance(0.4f))
            {
                var tags = new[] { ToneTag.Flat, ToneTag.TooSmooth, ToneTag.Echoing, ToneTag.Hesitant };
                return _rng.Pick(tags);
            }
            return ToneTag.Normal;
        }

        public StudentIdentity PickIdentity(GameRng rng) => _canon[rng.NextInt(0, _canon.Count)];

        public VisitorData BuildLateStudent(StudentIdentity id, ShiftDef shift, bool anomaly)
        {
            var v = anomaly ? BuildAnomaly(id, shift) : BuildLegit(id, shift);
            v.ShiftNumber = shift.Number;
            v.VisitorIndex = -1;   // office visitors are outside the checkpoint roster
            return v;
        }

        public VisitorData BuildAdministrator(ShiftDef shift)
        {
            var badgePhoto = new Portrait { HairStyle = 2, FaceVariant = 3, Accessory = 0, Distorted = false };
            var v = new VisitorData
            {
                IsAnomaly = true,
                IsAdministrator = true,
                ShiftNumber = shift.Number,
                ClaimedName = "V. Marlowe",
                RecordName = "R. Vance",                  // roster memo lists a different administrator
                IdNumber = "DA-0233",                     // staff badge number, not on the roster memo
                RecordIdNumber = "DA-1147",               // memo: "R. Vance — DA-1147"
                IdYearLevel = 1, RecordYearLevel = 1,     // unused for staff (no year level)
                IdCourseIndex = 0, RecordCourseIndex = 0, // unused ("District Administration")
                IdIsTransferee = false, RecordIsTransferee = false,
                ExpirationDay = Content.DayNumberOfDate(2027, 3, 31),
                ExpirationDisplay = "2027-03-31",
                WindowPortrait = new Portrait { HairStyle = 2, FaceVariant = 3, Accessory = 0, Distorted = true },
                IdPhoto = badgePhoto,                     // subtle photo defect + distortion at window
                InDatabase = false,                       // badge not in the student registry
                DuplicateOfShift = "",
                RecordFirstClassroom = "— (district personnel)",
                Greeting = Content.AdminLines[0],
            };
            v.Defects.Add(DefectType.RecordNameMismatch);  // badge vs roster memo name
            v.Defects.Add(DefectType.NotInDatabase);       // badge number not on the roster
            v.Defects.Add(DefectType.PhotoSubtleMismatch); // same glitch as the other anomalies
            v.Answers[QuestionId.Course] = new Answer { Text = "I am with the district office.", Tone = ToneTag.TooSmooth };
            v.Answers[QuestionId.Year] = new Answer { Text = "That is not relevant to my visit.", Tone = ToneTag.Flat };
            v.Answers[QuestionId.Status] = new Answer { Text = "I am an administrator.", Tone = ToneTag.TooSmooth };
            v.Answers[QuestionId.FirstClassroom] = new Answer { Text = "My first stop is your server room.", Tone = ToneTag.Echoing, ContradictsDocuments = true };
            return v;
        }

        public VisitorData GenerateRunVisitor(int processedSoFar)
        {
            float anomalyChance = Math.Min(0.15f + processedSoFar * 0.02f, 0.6f);
            bool anomaly = _rng.Chance(anomalyChance);
            var id = _canon[_rng.NextInt(0, _canon.Count)];
            var fakeShift = new ShiftDef
            {
                Number = 0,
                VisitorCount = 1,
                AnomalyCount = anomaly ? 1 : 0,
                MaxDefectsPerAnomaly = processedSoFar < 5 ? 1 : (processedSoFar < 12 ? 2 : 3),
                SubtleDefectsAllowed = processedSoFar >= 5,
                DuplicateIdsAllowed = processedSoFar >= 8,
                BehaviorOddityAllowed = processedSoFar >= 5,
                HasAdministrator = false,
            };
            var v = anomaly ? BuildAnomaly(id, fakeShift) : BuildLegit(id, fakeShift);
            v.ShiftNumber = 0;
            _genLog.RecordFrom(v, "presented earlier this run");
            return v;
        }

        public class ScanResult
        {
            public bool FormatValid;
            public bool FoundInDatabase;
            public string RegisteredTo = "";
            public bool DuplicateTonight;
            public bool ReturningStudent;
            public string Display;
        }

        public ScanResult Scan(VisitorData v, PresentationLog playLog)
        {
            var r = new ScanResult();
            if (v.IsAdministrator)
            {
                r.FormatValid = true;
                r.FoundInDatabase = false;
                r.Display = "NOT A STUDENT ID — VERIFY AGAINST DISTRICT ROSTER MEMO";
                return r;
            }
            r.FormatValid = IdRules.IsFormatValid(v.IdNumber);
            if (!r.FormatValid)
            {
                r.Display = "SYSTEM ERROR — UNREADABLE ID NUMBER";
                return r;
            }
            bool presented = playLog != null && playLog.WasPresented(v.IdNumber);
            r.DuplicateTonight = playLog != null && playLog.WasPresentedWithDifferentCard(v.IdNumber, v.IdPhoto);
            r.ReturningStudent = presented && !r.DuplicateTonight;

            r.FoundInDatabase = _registry.Contains(v.IdNumber);
            r.RegisteredTo = NameForRegistryId(v.IdNumber);
            if (!r.FoundInDatabase)
            {
                r.Display = "NOT FOUND — NO STUDENT RECORD FOR " + v.IdNumber
                    + (r.DuplicateTonight
                        ? " · ⚠ ALREADY PRESENTED " + (playLog.LabelFor(v.IdNumber) ?? "").ToUpperInvariant() + " WITH A DIFFERENT CARD"
                        : "");
                return r;
            }
            if (r.DuplicateTonight)
                r.Display = "FOUND — REGISTERED TO " + r.RegisteredTo +
                            " · ⚠ ALREADY PRESENTED " + (playLog.LabelFor(v.IdNumber) ?? "").ToUpperInvariant() +
                            " WITH A DIFFERENT CARD";
            else if (r.ReturningStudent)
                r.Display = "FOUND — " + r.RegisteredTo + " · RETURNING STUDENT";
            else
                r.Display = "FOUND — " + r.RegisteredTo;
            return r;
        }

        private string NameForRegistryId(string idNumber)
        {
            foreach (var s in _canon) if (s.IdNumber == idNumber) return s.FullName;
            return "UNKNOWN";
        }
    }
}
````

### `_Project/Scripts/GameLogic/SaveFormat.cs` (8.6 KiB)

````csharp
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Hollowbrook.Logic
{
    public class SaveGame
    {
        public GameSession Session = new GameSession();
        public SettingsData Settings = new SettingsData();
        public RecordsData Records = new RecordsData();
        public string SavedAtLabel = ""; // human-readable, informational only
    }

    public static class SaveFormat
    {
        public const string Header = "SMYIDSAVE";
        public const int Version = 1;

        public static string Serialize(SaveGame save)
        {
            var s = save.Session;
            var sb = new StringBuilder();
            sb.Append(Header).Append(' ').Append(Version).Append('\n');
            KV(sb, "savedAt", save.SavedAtLabel);
            KV(sb, "mode", s.Mode.ToString());
            sb.Append("seed=").Append(s.Seed).Append('\n');
            sb.Append("shift=").Append(s.CurrentShift).Append('\n');
            sb.Append("completed=").Append(s.CompletedShifts).Append('\n');
            sb.Append("processed=").Append(s.Processed).Append('\n');
            sb.Append("correct=").Append(s.CorrectDecisions).Append('\n');
            sb.Append("falseRej=").Append(s.FalseRejections).Append('\n');
            sb.Append("breaches=").Append(s.Breaches).Append('\n');
            sb.Append("anomRej=").Append(s.AnomaliesRejected).Append('\n');
            sb.Append("tokens=").Append(s.TokensCollected).Append('\n');
            sb.Append("visitorAtSave=").Append(s.VisitorIndexAtSave).Append('\n');
            KV(sb, "pool", s.Pool.Encode());
            sb.Append("adminAllowed=").Append(s.AdminAllowed ? 1 : 0).Append('\n');
            sb.Append("adminRejected=").Append(s.AdminRejected ? 1 : 0).Append('\n');
            sb.Append("runFinished=").Append(s.RunFinished ? 1 : 0).Append('\n');
            KV(sb, "sanity", Inv(s.Sanity));
            sb.Append("score=").Append(s.Score).Append('\n');
            sb.Append("streak=").Append(s.Streak).Append('\n');
            sb.Append("bestStreak=").Append(s.BestStreak).Append('\n');
            KV(sb, "runTimer", Inv(s.RunTimer));
            sb.Append("runProcessed=").Append(s.RunProcessed).Append('\n');
            KV(sb, "mutation", s.ActiveMutationKey);
            KV(sb, "seen", s.TonightLog.Encode());
            KV(sb, "clues", string.Join(",", s.Clues.SaveFound()));
            sb.Append("orderIntact=").Append(s.Clues.OrderIntact ? 1 : 0).Append('\n');
            sb.Append("nextOrder=").Append(s.Clues.NextExpectedOrder).Append('\n');
            KV(sb, "rooms", string.Join(",", s.Clues.SaveRooms()));
            var hist = new List<string>();
            foreach (var h in s.History) hist.Add(h.Encode());
            KV(sb, "history", string.Join("|", hist));
            KV(sb, "settings", save.Settings.Encode());
            sb.Append("endlessBest=").Append(save.Records.EndlessBest).Append('\n');
            sb.Append("challengeBest=").Append(save.Records.ChallengeBest).Append('\n');
            KV(sb, "endingsSeen", string.Join(",", save.Records.EndingsSeen));

            string body = sb.ToString();
            sb.Append("checksum=").Append(Checksum(body)).Append('\n');
            return sb.ToString();
        }

        public static SaveGame Deserialize(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var lines = text.Replace("\r\n", "\n").Split('\n');
            if (lines.Length < 3) return null;
            var head = lines[0].Split(' ');
            if (head.Length != 2 || head[0] != Header) return null;
            if (!int.TryParse(head[1], out int ver) || ver != Version) return null;

            string checksumLine = null;
            var body = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("checksum=")) { checksumLine = lines[i]; continue; }
                if (lines[i].Length == 0) continue;
                body.Append(lines[i]).Append('\n');
            }
            if (checksumLine == null) return null;
            if (!long.TryParse(checksumLine.Substring("checksum=".Length), out long stored)) return null;
            if (Checksum(body.ToString()) != stored) return null;

            var map = new Dictionary<string, string>();
            for (int i = 1; i < lines.Length; i++)
            {
                int eq = lines[i].IndexOf('=');
                if (eq <= 0) continue;
                map[lines[i].Substring(0, eq)] = lines[i].Substring(eq + 1);
            }

            var save = new SaveGame();
            var s = save.Session;
            try
            {
                if (map.TryGetValue("mode", out var mode) && System.Enum.TryParse(mode, out GameMode gm)) s.Mode = gm;
                s.Seed = Int(map, "seed", s.Seed);
                s.CurrentShift = Int(map, "shift", 1);
                s.CompletedShifts = Int(map, "completed", 0);
                s.Processed = Int(map, "processed", 0);
                s.CorrectDecisions = Int(map, "correct", 0);
                s.FalseRejections = Int(map, "falseRej", 0);
                s.Breaches = Int(map, "breaches", 0);
                s.AnomaliesRejected = Int(map, "anomRej", 0);
                s.TokensCollected = Int(map, "tokens", 0);
                s.VisitorIndexAtSave = Int(map, "visitorAtSave", 0);
                s.Pool.Decode(Str(map, "pool", ""));
                s.AdminAllowed = Int(map, "adminAllowed", 0) == 1;
                s.AdminRejected = Int(map, "adminRejected", 0) == 1;
                s.RunFinished = Int(map, "runFinished", 0) == 1;
                s.Sanity = Flt(map, "sanity", 100f);
                s.Score = Int(map, "score", 0);
                s.Streak = Int(map, "streak", 0);
                s.BestStreak = Int(map, "bestStreak", 0);
                s.RunTimer = Flt(map, "runTimer", 0f);
                s.RunProcessed = Int(map, "runProcessed", 0);
                s.ActiveMutationKey = Str(map, "mutation", "none");
                save.SavedAtLabel = Str(map, "savedAt", "");

                s.TonightLog.Decode(Str(map, "seen", ""));

                s.Clues.LoadFound(Split(Str(map, "clues", "")));
                s.Clues.LoadRooms(SplitList(Str(map, "rooms", "")));
                s.Clues.LoadOrderIntact(Int(map, "orderIntact", 1) == 1, Int(map, "nextOrder", 1));

                s.History.Clear();
                foreach (var h in Str(map, "history", "").Split('|'))
                {
                    if (h.Length == 0) continue;
                    var sum = ShiftSummary.Decode(h);
                    if (sum != null) s.History.Add(sum);
                }

                var settings = SettingsData.Decode(Str(map, "settings", ""));
                if (settings != null) save.Settings = settings;

                save.Records.EndlessBest = Int(map, "endlessBest", 0);
                save.Records.ChallengeBest = Int(map, "challengeBest", 0);
                save.Records.EndingsSeen.Clear();
                foreach (var e in Split(Str(map, "endingsSeen", ""))) save.Records.EndingsSeen.Add(e);
            }
            catch (System.Exception)
            {
                return null; // any malformed value => treat as no save (graceful fallback)
            }
            return save;
        }

        private static void KV(StringBuilder sb, string key, string value) =>
            sb.Append(key).Append('=').Append(value ?? "").Append('\n');

        private static string Inv(float f) => f.ToString("0.###", CultureInfo.InvariantCulture);

        public static long Checksum(string body)
        {
            unchecked
            {
                long h = 17;
                foreach (char c in body) h = h * 31 + c;
                return (h & 0x7FFFFFFFFFFFFFFFL) % 1000000007L;
            }
        }

        private static IEnumerable<string> Split(string csv)
        {
            if (string.IsNullOrEmpty(csv)) yield break;
            foreach (var p in csv.Split(',')) if (p.Length > 0) yield return p;
        }

        private static List<string> SplitList(string csv)
        {
            var l = new List<string>();
            foreach (var p in Split(csv)) l.Add(p);
            return l;
        }

        private static int Int(Dictionary<string, string> m, string k, int d) =>
            m.TryGetValue(k, out var v) && int.TryParse(v, out int r) ? r : d;

        private static float Flt(Dictionary<string, string> m, string k, float d) =>
            m.TryGetValue(k, out var v) && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float r) ? r : d;

        private static string Str(Dictionary<string, string> m, string k, string d) =>
            m.TryGetValue(k, out var v) ? v : d;
    }
}
````

### `_Project/Scripts/GameLogic/ShiftPlan.cs` (7.7 KiB)

````csharp
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    public static class ShiftPlan
    {
        public const int TotalShifts = 5;

        public static List<ShiftDef> Build()
        {
            return new List<ShiftDef>
            {
                new ShiftDef
                {
                    Number = 1, DateLabel = Content.ShiftDates[0],
                    VisitorCount = 4, AnomalyCount = 0, HasAdministrator = false,
                    Title = "Introduction",
                    Objective = "Learn the checkpoint: inspect IDs and records, ask up to 2 questions, and stamp Allow or Reject. Take a walk — learn how to leave the security room and come back.",
                    RuleNoteAddendum = "NIGHT 1 — Welcome, custodian. Verify: ID number format (HB-YYYY-NNNN), photo vs. the person at the window, expiration date, year level, course, and Regular/Transferee status against the student record. You may ask up to 2 questions.",
                    MaxDefectsPerAnomaly = 1, SubtleDefectsAllowed = false, DuplicateIdsAllowed = false,
                    BehaviorOddityAllowed = false, ExplorationFocus = false,
                },
                new ShiftDef
                {
                    Number = 2, DateLabel = Content.ShiftDates[1],
                    VisitorCount = 6, AnomalyCount = 2, HasAdministrator = false,
                    Title = "Initial Inconsistencies",
                    Objective = "Small contradictions are appearing in documents, faces, and answers. Not everyone at the window is a student. The hallway is open between visitors — look around.",
                    RuleNoteAddendum = "NIGHT 2 — District reminder: an expired ID must never be accepted, whatever the excuse. If documents disagree, the person does not enter.",
                    MaxDefectsPerAnomaly = 1, SubtleDefectsAllowed = false, DuplicateIdsAllowed = false,
                    BehaviorOddityAllowed = false, ExplorationFocus = false,
                },
                new ShiftDef
                {
                    Number = 3, DateLabel = Content.ShiftDates[2],
                    VisitorCount = 7, AnomalyCount = 3, HasAdministrator = false,
                    Title = "Increasing Anomalies",
                    Objective = "Cases are harder tonight — subtle photo differences and recycled identities. Use SCAN against the student database and cross-check everything twice.",
                    RuleNoteAddendum = "NIGHT 3 — WARNING: duplicate ID numbers have been reported at other district sites. Use the scanner: a valid ID already presented tonight with a different card is grounds for immediate rejection.",
                    MaxDefectsPerAnomaly = 2, SubtleDefectsAllowed = true, DuplicateIdsAllowed = true,
                    BehaviorOddityAllowed = true, ExplorationFocus = false,
                },
                new ShiftDef
                {
                    Number = 4, DateLabel = Content.ShiftDates[3],
                    VisitorCount = 8, AnomalyCount = 4, HasAdministrator = false,
                    Title = "Investigation",
                    Objective = "Something happened in this school. Between visitors, investigate the hallway and both classrooms. Whatever the previous custodians found — find it too.",
                    RuleNoteAddendum = "NIGHT 4 — The district has scheduled this facility for final closure. Custodial staff are instructed to document anything unusual found inside the building. Investigation is now part of your duties.",
                    MaxDefectsPerAnomaly = 2, SubtleDefectsAllowed = true, DuplicateIdsAllowed = true,
                    BehaviorOddityAllowed = true, ExplorationFocus = true,
                },
                new ShiftDef
                {
                    Number = 5, DateLabel = Content.ShiftDates[4],
                    VisitorCount = 8, AnomalyCount = 4, HasAdministrator = true,
                    Title = "Final Shift",
                    Objective = "Last night. A district administrator will arrive to take control of the school. Check the credentials like you would check anyone else's. Trust the evidence.",
                    RuleNoteAddendum = "NIGHT 5 — District personnel present a STAFF BADGE, not a student ID. The badge name and number must match the attached District Roster Memo. If they do not match, deny entry. No exceptions. No overrides by phone.",
                    MaxDefectsPerAnomaly = 2, SubtleDefectsAllowed = true, DuplicateIdsAllowed = true,
                    BehaviorOddityAllowed = true, ExplorationFocus = false,
                },
            };
        }

        public static ShiftDef ExhibitionDef()
        {
            return new ShiftDef
            {
                Number = 1,
                DateLabel = Content.ShiftDates[0],
                VisitorCount = 4, AnomalyCount = 1, HasAdministrator = false,
                Title = "Exhibition Night",
                Objective = "One short night: check the papers, ask when in doubt, stamp once. " +
                            "One of the four is not a student - the papers or the voice will give it away.",
                RuleNoteAddendum = "EXHIBITION - Verify: ID format (HB-YYYY-NNNN), photo vs the face at the " +
                            "window, expiration, year, course, Regular/Transferee. Up to 2 questions. " +
                            "A answer marked 'papers disagree' or an odd voice is grounds to call the principal.",
                MaxDefectsPerAnomaly = 2, SubtleDefectsAllowed = false,
                DuplicateIdsAllowed = false, BehaviorOddityAllowed = true, ExplorationFocus = false,
            };
        }

        public static ShiftDef Get(int number)
        {
            var all = Build();
            foreach (var s in all) if (s.Number == number) return s;
            return null;
        }

        public static readonly RuleMutationDef[] ChallengeMutations =
        {
            new RuleMutationDef("none", null),
            new RuleMutationDef("no_transferees",
                "TEMPORARY DISTRICT RULE: Transferees are not admitted on nights with active system audits. Reject all transferees."),
            new RuleMutationDef("fourth_year_curfew",
                "TEMPORARY DISTRICT RULE: Fourth-year students are under district curfew tonight. Reject all 4th Years."),
            new RuleMutationDef("strict_expiry",
                "TEMPORARY DISTRICT RULE: IDs expiring within 60 days are not accepted during audits. Reject them."),
            new RuleMutationDef("odd_serials",
                "TEMPORARY DISTRICT RULE: For tonight's serial audit, IDs with an ODD serial number are suspended. Reject them."),
        };
    }

    public static class SanityModel
    {
        public const float Max = 100f;
        public const float Min = 0f;

        public static SanityStage StageFor(float sanity)
        {
            if (sanity >= 70f) return SanityStage.Stable;
            if (sanity >= 40f) return SanityStage.Uneasy;
            if (sanity >= 15f) return SanityStage.Shaken;
            return SanityStage.Breaking;
        }

        public static float HallucinationChancePerSecond(SanityStage stage)
        {
            switch (stage)
            {
                case SanityStage.Uneasy: return 0.010f;
                case SanityStage.Shaken: return 0.035f;
                case SanityStage.Breaking: return 0.080f;
                default: return 0f;
            }
        }

        public const float SecurityRoomRecoveryPerSecond = 1.2f;
        public const float DarknessDrainPerSecond = 0.6f;

        public static float Clamp(float v) => v < Min ? Min : (v > Max ? Max : v);

        public static float DrainMultiplier(GameMode mode) => mode == GameMode.Challenge ? 2f : 1f;
    }
}
````

### `_Project/Scripts/GameLogic/TrackingPolicy.cs` (1.2 KiB)

````csharp
namespace Hollowbrook.Logic
{
    public static class TrackingPolicy
    {
        public const float RevealThreshold = 1f;

        public const float FocusRatePerSecond = 0.55f;

        public const float LitRoomRatePerSecond = 0.22f;

        public const float DecayPerSecond = 0.35f;

        public const float HallucinationTruthSeconds = 0.5f;

        public const float RevealBeatSeconds = 1.2f;

        public static float RatePerSecond(bool focus, bool litRoom)
        {
            if (focus) return FocusRatePerSecond;
            if (litRoom) return LitRoomRatePerSecond;
            return -DecayPerSecond;
        }

        public static float Advance(float meter, float dt, bool focus, bool litRoom)
        {
            if (dt <= 0f) return Clamp01(meter);
            return Clamp01(meter + RatePerSecond(focus, litRoom) * dt);
        }

        public static bool IsRevealed(float meter) => meter >= RevealThreshold - 1e-4f;

        public static float Solidify(float meter) => Clamp01(meter);

        public static bool HallucinationDispelled(float focusSeconds) =>
            focusSeconds >= HallucinationTruthSeconds;

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
````

### `_Project/Scripts/Horror/HallucinationSystem.cs` (4.3 KiB)

````csharp
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

        public void Init(GameRoot root) { _gameRoot = root; }

        private void Update()
        {
            if (_gameRoot == null || _gameRoot.Session == null || _gameRoot.Paused) return;
            if (_gameRoot.State != AppViewState.Exploration && _gameRoot.State != AppViewState.Checkpoint) return;
            var stage = SanityModel.StageFor(_gameRoot.Session.Sanity);
            if (stage == SanityStage.Stable) return;

            if (_gameRoot.State == AppViewState.Exploration)
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
                        _gameRoot.Bus.Publish(GameEvent.Of(EventIds.HallucinationTriggered, "whisper"));
                        _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, Whispers[Random.Range(0, Whispers.Length)], 3.2f, null));
                        _gameRoot.Audio.PlaySfx("sfx_static", 0.18f);
                    }
                }
            }
        }

        private void ExploreHallucination()
        {
            if (_gameRoot.Views == null || _gameRoot.Views.Current == null) return;
            int roll = Random.Range(0, 4);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.HallucinationTriggered, "explore_" + roll));
            switch (roll)
            {
                case 0: // phantom figure — harmless, fades on its own
                    _gameRoot.Views.SpawnAnomaly(true);
                    break;
                case 1:
                    _gameRoot.Audio.PlaySfx("sfx_knock", 0.6f);
                    _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*three slow knocks from inside the wall*", 2.8f, null));
                    break;
                case 2:
                    _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, Whispers[Random.Range(0, Whispers.Length)], 3.2f, null));
                    _gameRoot.Audio.PlaySfx("sfx_static", 0.25f);
                    break;
                default: // phantom flicker (never a strobe; photosensitivity-safe by construction)
                {
                    var light = _gameRoot.Views.Current.Light;
                    if (light != null && light.Mode == LightMode.Sweep)
                    {
                        _gameRoot.Audio.PlaySfx("sfx_flicker", 0.4f);
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
            if (_gameRoot != null && _gameRoot.Views != null && _gameRoot.Views.Current != null && _gameRoot.Views.Current.Light != null)
                _gameRoot.Views.Current.Light.SetMode(LightMode.Sweep);
        }
    }
}
````

### `_Project/Scripts/Horror/SanityController.cs` (5.5 KiB)

````csharp
using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.UI;
using Hollowbrook.Logic;
using Hollowbrook.World;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.Horror
{
    // Sanity-driven screen effects. Created by each scene bootstrap.
    public class SanityController : MonoBehaviour
    {
        private GameRoot _gameRoot;
        private Image _vignette;
        private Image _distortion;
        private Text _warning;
        private SanityStage _lastStage = SanityStage.Stable;
        private float _acc;
        private float _heartbeatTimer;
        private readonly List<EventSubscription> _subs = new List<EventSubscription>();

        public void Init(GameRoot root, Canvas fxCanvas)
        {
            _gameRoot = root;

            var vigRt = UIFactory.NewUI(fxCanvas.transform, "Vignette");
            UIFactory.AnchorsFull(vigRt);
            _vignette = vigRt.gameObject.AddComponent<Image>();
            _vignette.sprite = ProceduralArt.Get("vignette");
            _vignette.color = new Color(1, 1, 1, 0.25f);
            _vignette.raycastTarget = false;

            var disRt = UIFactory.NewUI(fxCanvas.transform, "Distortion");
            UIFactory.AnchorsFull(disRt);
            _distortion = disRt.gameObject.AddComponent<Image>();
            _distortion.sprite = ProceduralArt.Get("noise_overlay");
            _distortion.color = new Color(0.7f, 0.7f, 0.72f, 0f);
            _distortion.raycastTarget = false;

            var warnRt = UIFactory.NewUI(fxCanvas.transform, "LowSanityWarning");
            UIFactory.Anchor(warnRt, new Vector2(0.5f, 1f), new Vector2(560, 40), new Vector2(0, -64));
            _warning = UIFactory.Label(warnRt, "text", "", 20, new Color(0.75f, 0.2f, 0.24f), TextAnchor.MiddleCenter);
            UIFactory.AnchorsFull(_warning.rectTransform);
            _warning.gameObject.SetActive(false);

            _subs.Add(root.Bus.Subscribe(EventIds.SanityStageChanged, OnStageChanged));
            _subs.Add(root.Bus.Subscribe(EventIds.StateChanged, e => ApplyStageVisuals(_lastStage, true)));
            ApplyStageVisuals(SanityStage.Stable, true);
        }

        private void OnDestroy()
        {
            foreach (var s in _subs) s?.Dispose();
            _subs.Clear();
        }

        public void Tick(bool inSecurity)
        {
            if (_gameRoot == null || _gameRoot.Session == null) return;
            _acc += Time.deltaTime;
            if (_acc < 0.5f) return;
            float dt = _acc; _acc = 0f;

            float before = _gameRoot.Session.Sanity;
            if (inSecurity)
            {
                float rate = _gameRoot.Session.Mode == GameMode.Normal
                    ? SanityModel.SecurityRoomRecoveryPerSecond
                    : SanityModel.SecurityRoomRecoveryPerSecond * 0.4f;
                _gameRoot.Session.ApplySanity(rate * dt);
            }
            else if (_gameRoot.Views != null && _gameRoot.Views.Current != null)
            {
                var light = _gameRoot.Views.Current.Light;
                if (light != null && light.Mode == LightMode.Off)
                    _gameRoot.Session.ApplySanity(-SanityModel.DarknessDrainPerSecond * dt);
                else if (light != null && light.Mode == LightMode.Focus)
                    _gameRoot.Session.ApplySanity(-SanityModel.DarknessDrainPerSecond * 0.25f * dt);
            }

            if (!Mathf.Approximately(before, _gameRoot.Session.Sanity))
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, _gameRoot.Session.Sanity));

            var stage = SanityModel.StageFor(_gameRoot.Session.Sanity);
            if (stage != _lastStage)
            {
                _lastStage = stage;
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.SanityStageChanged, stage));
            }

            if (stage == SanityStage.Shaken || stage == SanityStage.Breaking)
            {
                _heartbeatTimer -= dt;
                if (_heartbeatTimer <= 0f)
                {
                    _heartbeatTimer = stage == SanityStage.Breaking ? 0.85f : 1.5f;
                    _gameRoot.Audio.PlaySfx("sfx_heartbeat", stage == SanityStage.Breaking ? 0.75f : 0.4f);
                }
            }
        }

        private void OnStageChanged(GameEvent e)
        {
            if (e.Data is SanityStage stage) ApplyStageVisuals(stage, false);
        }

        private void ApplyStageVisuals(SanityStage stage, bool silent)
        {
            if (_vignette == null) return;
            bool reduce = _gameRoot != null && _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;
            float vig, dis;
            string warn;
            switch (stage)
            {
                case SanityStage.Uneasy: vig = 0.40f; dis = 0.030f; warn = ""; break;
                case SanityStage.Shaken: vig = 0.55f; dis = reduce ? 0.05f : 0.075f; warn = "YOUR HANDS WON'T STOP SHAKING"; break;
                case SanityStage.Breaking: vig = 0.70f; dis = reduce ? 0.08f : 0.130f; warn = "YOU CAN FEEL IT WATCHING BACK"; break;
                default: vig = 0.25f; dis = 0f; warn = ""; break;
            }
            var vc = _vignette.color; vc.a = vig; _vignette.color = vc;
            var dc = _distortion.color; dc.a = dis; _distortion.color = dc;
            if (_warning != null)
            {
                _warning.text = warn;
                _warning.gameObject.SetActive(!string.IsNullOrEmpty(warn));
            }
            _lastStage = stage;
        }
    }
}
````

### `_Project/Scripts/Horror/ScareSystem.cs` (6.2 KiB)

````csharp
using System.Collections;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.UI;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.Horror
{
    // Stingers and jumpscares with accessibility guards. Created by each scene bootstrap.
    public class ScareSystem : MonoBehaviour
    {
        private GameRoot _gameRoot;
        private Canvas _canvas;
        private Image _black;
        private Image _face;
        private Image _flash;
        private float _cooldownUntil = -99f;
        private Coroutine _shakeRoutine;

        public void Init(GameRoot root, Canvas scareCanvas)
        {
            _gameRoot = root;
            _canvas = scareCanvas;

            var blackRt = UIFactory.NewUI(_canvas.transform, "Black");
            UIFactory.AnchorsFull(blackRt);
            _black = blackRt.gameObject.AddComponent<Image>();
            _black.color = new Color(0, 0, 0, 0f);
            _black.raycastTarget = false;

            var faceRt = UIFactory.NewUI(_canvas.transform, "Face");
            UIFactory.Anchor(faceRt, new Vector2(0.5f, 0.5f), new Vector2(560, 640), Vector2.zero);
            _face = faceRt.gameObject.AddComponent<Image>();
            _face.color = new Color(1, 1, 1, 0f);
            _face.raycastTarget = false;

            var flashRt = UIFactory.NewUI(_canvas.transform, "Flash");
            UIFactory.AnchorsFull(flashRt);
            _flash = flashRt.gameObject.AddComponent<Image>();
            _flash.sprite = ProceduralArt.Get("flash");
            _flash.color = new Color(1, 1, 1, 0f);
            _flash.raycastTarget = false;
        }

        public void Capture(AnomalyBehaviorKind kind)
        {
            if (Time.unscaledTime < _cooldownUntil) return;
            _cooldownUntil = Time.unscaledTime + 3f;
            bool photoSafe = _gameRoot.Settings != null && _gameRoot.Settings.PhotosensitivitySafe;
            bool reduce = _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;

            var distorted = new Portrait
            {
                HairStyle = (int)kind % 6,
                FaceVariant = ((int)kind + 3) % 6,
                Accessory = 0,
                Distorted = true,
            };
            _face.sprite = ProceduralArt.Portrait("scare", distorted, 280, 320);
            _gameRoot.Audio.PlaySfx("sfx_scare", photoSafe ? 0.45f : 0.9f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.ScareTriggered, "capture_" + kind));
            if (!reduce) Shake(0.6f);
            StartCoroutine(CaptureRoutine(photoSafe, reduce));
        }

        private IEnumerator CaptureRoutine(bool photoSafe, bool reduce)
        {
            float fadeIn = photoSafe ? 0.40f : 0.05f;
            float hold = photoSafe ? 0.55f : 0.45f;
            float fadeOut = 0.5f;

            if (!photoSafe)
            {
                var fc = _flash.color; fc.a = 0.85f; _flash.color = fc;
                yield return new WaitForSecondsRealtime(0.05f);
                fc.a = 0f; _flash.color = fc;
            }

            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                var bc = _black.color; bc.a = 0.85f * (t / fadeIn); _black.color = bc;
                var c = _face.color;
                c.a = t / fadeIn;
                _face.color = c;
                if (!reduce)
                {
                    float punch = 1f + 0.35f * (1f - t / fadeIn);
                    _face.rectTransform.localScale = new Vector3(punch, punch, 1f);
                }
                yield return null;
            }
            _face.rectTransform.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(hold);
            t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                var bc = _black.color; bc.a = 0.85f * (1f - t / fadeOut); _black.color = bc;
                var c = _face.color; c.a = 1f - t / fadeOut; _face.color = c;
                yield return null;
            }
            var b2 = _black.color; b2.a = 0; _black.color = b2;
            var f2 = _face.color; f2.a = 0; _face.color = f2;
        }

        public void Stinger(string kind, float sanityHit)
        {
            if (Time.unscaledTime < _cooldownUntil) return;
            _cooldownUntil = Time.unscaledTime + 1.5f;
            if (sanityHit > 0f)
            {
                _gameRoot.Session.ApplySanity(-sanityHit);
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, _gameRoot.Session.Sanity));
            }
            bool photoSafe = _gameRoot.Settings != null && _gameRoot.Settings.PhotosensitivitySafe;
            _gameRoot.Audio.PlaySfx("sfx_stinger", photoSafe ? 0.4f : 0.7f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.ScareTriggered, kind));
            StartCoroutine(StaticBurst(photoSafe ? 0.10f : 0.22f));
        }

        private IEnumerator StaticBurst(float peakAlpha)
        {
            var fc = _flash.color;
            _flash.sprite = ProceduralArt.Get("noise_overlay");
            fc.a = peakAlpha; _flash.color = fc;
            yield return new WaitForSecondsRealtime(0.09f);
            fc.a = 0f; _flash.color = fc;
            _flash.sprite = ProceduralArt.Get("flash");
        }

        public void Shake(float strength)
        {
            if (_gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion) return;
            if (_gameRoot.MainCamera == null) return;
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeRoutine(strength));
        }

        private IEnumerator ShakeRoutine(float strength)
        {
            if (_gameRoot.Views == null) yield break;
            var target = _gameRoot.Views.transform;
            float t = 0f;
            float dur = 0.45f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = strength * (1f - t / dur);
                target.localPosition = new Vector3(Random.Range(-k, k) * 0.09f, Random.Range(-k, k) * 0.09f, 0f);
                yield return null;
            }
            target.localPosition = Vector3.zero;
            _shakeRoutine = null;
        }
    }
}
````

### `_Project/Scripts/Horror/WanderingAnomaly.cs` (14.0 KiB)

````csharp
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.UI;
using Hollowbrook.Logic;
using Hollowbrook.World;
using UnityEngine;

namespace Hollowbrook.Horror
{
    // One roaming anomaly: evade pattern plus the light encounter. Spawned by ViewManager.
    public class WanderingAnomaly : MonoBehaviour
    {
        private GameRoot _gameRoot;
        private RoomInstance _room;
        private SpriteRenderer _sr;
        private float _lifetime;
        private bool _sightedPenaltyApplied;
        private float _sanityTickTimer;
        private float _track;             // 0..1 tracking meter (TrackingPolicy)
        private AnomalyState _state = AnomalyState.Roaming;
        private float _stateTimer;
        private bool _threatTold;
        private float _threatSeconds;
        private bool _tokenDropped;
        private SpriteRenderer _face;
        public AnomalyState State => _state;
        private float _trackThrottle;     // HUD event throttle
        private bool _revealed;           // fully tracked: solid, inert, about to dispel
        private float _revealBeat;
        public bool IsHallucination { get; private set; }
        public bool WasRevealed { get; private set; }
        public bool WasCaught { get; private set; }
        public AnomalyBehaviorKind Kind { get; private set; }
        public Vector3 Pos => transform.localPosition;

        public void Init(GameRoot root, RoomInstance room, AnomalyBehaviorKind kind, Vector3 spawn, bool hallucination)
        {
            _gameRoot = root;
            _room = room;
            Kind = kind;
            IsHallucination = hallucination;

            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = ProceduralArt.Get("anomaly_" + kind.ToString().ToLowerInvariant());
            _sr.sortingOrder = 40; // under the darkness overlay => silhouette in dim light

            float floorY = -2.4f;
            transform.localPosition = kind == AnomalyBehaviorKind.CoverHug
                ? new Vector3(spawn.x, floorY + 4.6f, 0)
                : new Vector3(spawn.x, floorY + 2.4f, 0);

            _telegraph = _gameRoot.Config.sprintTelegraphSeconds;
            PickTarget(Vector3.zero);
            _lifetime = hallucination ? 6.5f : 18f;

            if (!hallucination)
            {
                root.Audio.PlaySfx("sfx_glitch", 0.55f);
                root.Audio.PlayMusic("anomaly");
            }
        }

        internal GameRoot Root => _gameRoot;
        internal RoomInstance Room => _room;
        internal SpriteRenderer Renderer => _sr;

        private void Update()
        {
            if (_gameRoot == null || _gameRoot.Paused || _sr == null) return;
            bool dialogueOpen = _gameRoot.Dialogue != null && _gameRoot.Dialogue.IsActive;
            float dt = Time.deltaTime;

            _lifetime -= dt;
            if (_lifetime <= 0f) { Despawn(false); return; }

            Vector3 playerPos = _gameRoot.Views != null && _gameRoot.Views.Player != null
                ? _gameRoot.Views.Player.transform.localPosition : Vector3.zero;

            if (!dialogueOpen && !_revealed) MoveBehavior(dt, playerPos);

            TickTracking(dt, dialogueOpen);
            TickStates(dt);

            TickVisibility();

            if (!IsHallucination)
            {
                float d = Mathf.Abs(playerPos.x - transform.localPosition.x);
                if (d < 3.2f)
                {
                    _sanityTickTimer -= dt;
                    if (_sanityTickTimer <= 0f)
                    {
                        _sanityTickTimer = 1f;
                        _gameRoot.Session.ApplySanity(-1.5f);
                        _gameRoot.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, _gameRoot.Session.Sanity));
                    }
                }
                if (!_sightedPenaltyApplied && d < 5f)
                {
                    _sightedPenaltyApplied = true;
                    _gameRoot.Session.ApplySanity(-6f);
                    _gameRoot.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, _gameRoot.Session.Sanity));
                    _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*something is standing in the room with you*", 3f, null));
                }
            }
        }

        private void TickTracking(float dt, bool dialogueOpen)
        {
            if (dialogueOpen || _gameRoot == null || _gameRoot.Views == null) return;
            bool focus = _gameRoot.Views.IsIlluminated(transform.localPosition);
            bool litRoom = _room != null && _room.Light != null && _room.Light.Mode == LightMode.Sweep;

            if (IsHallucination)
            {
                _track = focus ? _track + dt : 0f;
                if (TrackingPolicy.HallucinationDispelled(_track))
                {
                    _gameRoot.Bus.Publish(GameEvent.Of(EventIds.AnomalyRevealed, "hallucination"));
                    _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*it was never there*", 2.2f, null));
                    Despawn(true);
                }
                return;
            }

            if (_revealed)
            {
                _revealBeat -= dt;
                if (_revealBeat <= 0f) Despawn(false);   // dispelled: the token stays behind
                return;
            }

            float before = _track;
            _track = TrackingPolicy.Advance(_track, dt, focus, litRoom);
            _trackThrottle -= dt;
            if (Mathf.Abs(_track - before) > 1e-4f && _trackThrottle <= 0f)
            {
                _trackThrottle = 0.1f;
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.TrackingChanged, _track));
            }

            if (TrackingPolicy.IsRevealed(_track))
            {
                _revealed = true;
                WasRevealed = true;
                _revealBeat = TrackingPolicy.RevealBeatSeconds;
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.TrackingChanged, 0f));
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.AnomalyRevealed, Kind));
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested,
                    "*the light holds it still - it is solid now*", 2.6f, null));
                _gameRoot.Audio.PlaySfx("sfx_stinger", 0.5f);
                DropToken();
            }
        }

        private void DropToken()
        {
            if (_tokenDropped || _room == null || transform.parent == null) return;
            _tokenDropped = true;
            var go = new GameObject("AnomalyToken");
            go.transform.SetParent(transform.parent, false);
            var p = transform.localPosition;
            go.transform.localPosition = new Vector3(p.x, -2.2f, p.z - 0.2f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralArt.Get("token");
            sr.color = new Color(0.85f, 0.85f, 0.82f);
            sr.sortingOrder = 75;
            var pick = go.AddComponent<TokenPickup>();
            pick.Init(_gameRoot, _room, p.x);
            _room.Interactables.Add(pick);
        }

        private void TickStates(float dt)
        {
            if (IsHallucination || _revealed || _gameRoot == null || _gameRoot.Views == null) return;
            var rig = _gameRoot.Views.Flashlight;
            bool focused = rig != null && rig.Beam == LightMode.Focus && rig.BeamOn && rig.ConeCovers(transform.localPosition);
            bool cone = rig != null && rig.BeamOn && rig.ConeCovers(transform.localPosition);
            bool litRoom = _room != null && _room.Light != null && _room.Light.Mode == LightMode.Sweep;
            var player = _gameRoot.Views.Player != null ? _gameRoot.Views.Player.transform.localPosition : Vector3.zero;
            float dist = Vector3.Distance(player, transform.localPosition);
            var tuning = EncounterTuning.Default;
            var step = AnomalyEncounter.Step(_state, _track, _stateTimer, cone || litRoom, focused,
                rig == null || !rig.BatteryDead, dist, dt, tuning);
            _stateTimer = step.State == _state ? _stateTimer + dt : 0f;
            _state = step.State;

            if (step.JustExposed)
            {
                ShowFace();
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.AnomalyStateChanged, _state.ToString()));
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*under the beam the face is WRONG*", 2.6f, null));
                _gameRoot.Audio.PlaySfx("sfx_glitch", 0.6f);
            }
            if (step.JustRepelled)
            {
                WasRevealed = true;          // pool: resolved; token drops below
                DropToken();
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.AnomalyStateChanged, _state.ToString()));
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*it tears itself away from the light*", 2.6f, null));
                _gameRoot.Audio.PlaySfx("sfx_stinger", 0.7f);
                if (_gameRoot.Views != null) _gameRoot.Views.ScheduleReturn(Kind);
            }
            if (step.Threatening && !_threatTold)
            {
                _threatTold = true;
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*footsteps, close, behind you*", 2.2f, null));
                _gameRoot.Audio.PlaySfx("sfx_heartbeat", 0.7f);
            }
            if (step.Threatening)
            {
                // fair catch: it must stay threatening for the whole grace time, not one frame
                _threatSeconds += dt;
                if (_threatSeconds >= _gameRoot.Config.coverHugCatchSeconds) { CatchPlayer(); return; }
            }
            else
            {
                _threatSeconds = 0f;
            }
            if (!step.Threatening) _threatTold = false;
        }

        private void ShowFace()
        {
            if (_face != null || _sr == null) return;
            var go = new GameObject("ExposedFace");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.55f, -0.1f);
            _face = go.AddComponent<SpriteRenderer>();
            _face.sprite = ProceduralArt.Portrait("anom", new Portrait { HairStyle = 2, FaceVariant = 4, Accessory = 0, Distorted = true }, 96, 120);
            _face.color = new Color(0.9f, 0.9f, 0.88f);
            _face.sortingOrder = 81;
        }

        public void TickVisibility()
        {
            if (_sr == null) return;
            if (_revealed)
            {
                var rc = _sr.color; rc.a = 1f; _sr.color = rc;   // solid ink, no haze
                _sr.sortingOrder = 80;
                return;
            }
            bool illuminated = _gameRoot.Views != null && _gameRoot.Views.IsIlluminated(transform.localPosition);
            float alpha = Kind == AnomalyBehaviorKind.CoverHug ? 0.35f : 0.92f;
            int order = 40;
            if (IsHallucination) alpha = 0.45f;
            if (illuminated) { alpha = Mathf.Max(alpha, 0.95f); order = 71; }
            if (_room != null && _room.Light != null && _room.Light.Mode == LightMode.Off && !illuminated)
                alpha *= 0.6f;
            var c = _sr.color;
            if (!Mathf.Approximately(c.a, alpha) || _sr.sortingOrder != order)
            {
                c.a = alpha; _sr.color = c; _sr.sortingOrder = order;
            }
        }

        public void CatchPlayer()
        {
            if (IsHallucination) { Despawn(true); return; }
            WasCaught = true;
            _gameRoot.Session.ApplySanity(-15f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, _gameRoot.Session.Sanity));
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.AnomalyCaught, Kind));
            if (_gameRoot.Scares != null) _gameRoot.Scares.Capture(Kind);
            if (_gameRoot.Views != null && _gameRoot.Views.Player != null)
                _gameRoot.Views.Player.PlaceInRoom(_room, _room.PlayerSpawn);
            Despawn(true);
        }

        public void Despawn(bool wasCaught)
        {
            if (_gameRoot != null && _gameRoot.Views != null) _gameRoot.Views.OnAnomalyGone(this);
            if (_gameRoot != null && !IsHallucination)
                _gameRoot.Audio.PlayMusic(GameRoot.MusicForRoom(_room != null ? _room.Id : RoomId.Hallway));
            Destroy(gameObject);
        }

    // one script, one switch: the four evasion patterns (was four strategy classes)
    private float _targetX;
    private float _telegraph;

    private void MoveBehavior(float dt, Vector3 playerPos)
    {
        var p = transform.localPosition;
        float speed = 1.1f;
        if (Kind == AnomalyBehaviorKind.Blink)
        {
            if (!Observed()) p.x = Mathf.MoveTowards(p.x, playerPos.x, speed * dt);   // moves only unseen
        }
        else if (Kind == AnomalyBehaviorKind.Slide)
        {
            p.x = Mathf.MoveTowards(p.x, _targetX, speed * 0.8f * dt);
            if (Mathf.Abs(p.x - _targetX) < 0.2f) PickTarget(playerPos);
        }
        else if (Kind == AnomalyBehaviorKind.Sprint)
        {
            _telegraph -= dt;
            if (_telegraph <= 0f) p.x = Mathf.MoveTowards(p.x, playerPos.x, 4.5f * dt);
        }
        else // CoverHug: ceiling lurker, only the Focus beam finds it
        {
            p.y = Mathf.MoveTowards(p.y, -0.6f, 0.4f * dt);
            p.x = Mathf.MoveTowards(p.x, _targetX, speed * 0.5f * dt);
            if (Mathf.Abs(p.x - _targetX) < 0.2f) PickTarget(playerPos);
        }
        p.y = Mathf.Clamp(p.y, -2.6f, -0.4f);
        transform.localPosition = p;
    }

    private void PickTarget(Vector3 playerPos)
    {
        float half = _room != null ? _room.HalfWidth : 10f;
        _targetX = UnityEngine.Random.Range(-half + 1f, half - 1f);
        if (Mathf.Abs(_targetX - playerPos.x) < 2f) _targetX = -_targetX;
        if (Kind == AnomalyBehaviorKind.Sprint) _telegraph = _gameRoot.Config.sprintTelegraphSeconds;
    }

    private bool Observed()
    {
        if (_room != null && _room.Light != null && _room.Light.Mode == LightMode.Sweep) return true;
        return _gameRoot.Views != null && _gameRoot.Views.IsIlluminated(transform.localPosition);
    }
}
}
````

### `_Project/Scripts/UI/CaptionBar.cs` (1.8 KiB)

````csharp
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
    // Subtitle line for important sounds. Created by each scene bootstrap.
    public class CaptionBar : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private Text _text;
        private GameObject _bg;
        private float _until;
        private EventSubscription _sub;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            _bg = UIFactory.Panel(transform, "bg", new Color(0, 0, 0, 0.72f)).gameObject;
            _bg.GetComponent<UnityEngine.UI.Image>().raycastTarget = false; // display only, never blocks buttons
            UIFactory.Anchor(_bg.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(880, 34), new Vector2(0, 54));
            _text = UIFactory.Label(_bg.transform, "t", "", 16, new Color(0.85f, 0.85f, 0.8f), TextAnchor.MiddleCenter);
            UIFactory.AnchorsFull(_text.rectTransform);
            _bg.SetActive(false);
            _sub = root.Bus.Subscribe(EventIds.CaptionRequested, e =>
            {
                if (_gameRoot.Settings == null || !_gameRoot.Settings.Captions) return;
                _text.text = e.Text ?? "";
                _until = Time.unscaledTime + (e.Value > 0 ? e.Value : 2.5f);
                _bg.SetActive(true);
            });
        }

        private void Update()
        {
            if (_bg.activeSelf && Time.unscaledTime > _until) _bg.SetActive(false);
        }

        private void OnDestroy() { _sub?.Dispose(); }
        public void Show() { gameObject.SetActive(true); }
        public void Hide() { gameObject.SetActive(false); }
    }
}
````

### `_Project/Scripts/UI/ConfirmationPopup.cs` (2.3 KiB)

````csharp
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
    // Yes/No box used for exits and data reset. Created by each scene bootstrap.
    public class ConfirmationPopup : MonoBehaviour, IScreenView
    {
        private GameObject _root;
        private Text _title, _body;
        private System.Action _onYes;
        private GameRoot _gameRoot;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            _root = UIFactory.Panel(transform, "panel", new Color(0, 0, 0, 0.55f)).gameObject;
            UIFactory.AnchorsFull(_root.GetComponent<RectTransform>());
            var panel = UIFactory.BorderedPanel(_root.transform, "box", new Color(0.07f, 0.07f, 0.08f, 0.98f), UIFactory.Border);
            UIFactory.Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(560, 240), Vector2.zero);
            _title = UIFactory.Label(panel, "title", "", 24, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_title.rectTransform, new Vector2(0.5f, 1), new Vector2(520, 36), new Vector2(0, -34));
            _body = UIFactory.Label(panel, "body", "", 16, UIFactory.InkDim, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_body.rectTransform, new Vector2(0.5f, 1), new Vector2(500, 70), new Vector2(0, -92));
            var yes = UIFactory.StandardButton(panel, "yes", "YES", () =>
            {
                var act = _onYes;
                Hide();
                act?.Invoke();
            }, 160, 44, true);
            UIFactory.Anchor(yes.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(160, 44), new Vector2(-100, 40));
            var no = UIFactory.StandardButton(panel, "no", "NO", Hide, 160, 44);
            UIFactory.Anchor(no.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(160, 44), new Vector2(100, 40));
            _root.SetActive(false);
        }

        public void Ask(string title, string body, System.Action onYes)
        {
            _title.text = title;
            _body.text = body;
            _onYes = onYes;
            _root.SetActive(true);
        }

        public void Show() { }
        public void Hide() { _root.SetActive(false); _onYes = null; }
    }
}
````

### `_Project/Scripts/UI/ControlsView.cs` (2.3 KiB)

````csharp
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
                "   Click documents, checklist rows, SCAN, questions, ALLOW / REJECT.\n" +
                "   At most 2 questions per visitor. Decisions are permanent.\n\n" +
                "EXPLORATION\n" +
                "   A / D  or  ← / →     walk left / right\n" +
                "   SHIFT                run\n" +
                "   E                    interact (doors, clues, switches)\n" +
                "   F                    flashlight (Focus light)\n" +
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
````

### `_Project/Scripts/UI/CreditsView.cs` (1.9 KiB)

````csharp
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
````

### `_Project/Scripts/UI/DialogueView.cs` (6.3 KiB)

````csharp
using System;
using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Core;
using Hollowbrook.Bootstrap;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    public class DialogueLine
    {
        public string Speaker = "";
        public string Text = "";
        public string PortraitSpriteKey; // optional procedural portrait key
    }

    // Click-through dialogue box with typewriter text. Created by each scene bootstrap.
    public class DialogueView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private GameObject _panel;
        private Text _speaker, _text, _continue;
        private Image _dim;
        private Image _portrait;
        private List<DialogueLine> _queue = new List<DialogueLine>();
        private int _qIndex;
        private Action _onDone;
        private bool _typing;
        private string _fullText;
        private float _typed;

        public bool IsActive { get; private set; }

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            _dim = UIFactory.Dimmer(transform, "dim", 0.35f);
            _dim.raycastTarget = true;

            var panelRt = UIFactory.BorderedPanel(transform, "panel", new Color(0.06f, 0.06f, 0.07f, 0.98f), UIFactory.Border, 2);
            UIFactory.Anchor(panelRt, new Vector2(0.5f, 0f), new Vector2(1000, 170), new Vector2(0, 118));

            _portrait = UIFactory.Img(panelRt, "portrait", null);
            UIFactory.Anchor(_portrait.rectTransform, new Vector2(0, 0.5f), new Vector2(104, 128), new Vector2(-430, 0));
            _portrait.gameObject.SetActive(false);

            _speaker = UIFactory.Label(panelRt, "speaker", "", 16, UIFactory.AccentBright, TextAnchor.MiddleLeft);
            UIFactory.Anchor(_speaker.rectTransform, new Vector2(0, 1), new Vector2(900, 26), new Vector2(476, -20));

            _text = UIFactory.Label(panelRt, "text", "", 18, UIFactory.Ink);
            var trt = _text.rectTransform;
            trt.anchorMin = new Vector2(0, 0); trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = new Vector2(26, 16); trt.offsetMax = new Vector2(-26, -40);

            _continue = UIFactory.Label(panelRt, "cont", "▼", 16, UIFactory.InkDim, TextAnchor.MiddleRight);
            UIFactory.Anchor(_continue.rectTransform, new Vector2(1, 0), new Vector2(40, 24), new Vector2(-40, 22));
            _continue.gameObject.SetActive(false);

            var clickCatcher = panelRt.gameObject.AddComponent<Button>();
            clickCatcher.targetGraphic = null;
            clickCatcher.onClick.AddListener(Advance);
            var dimBtn = _dim.gameObject.AddComponent<Button>();
            dimBtn.targetGraphic = _dim;
            dimBtn.onClick.AddListener(Advance);

            IsActive = false;
            gameObject.SetActive(true);
            _panel = panelRt.gameObject;
            HideVisuals();
        }

        public void Play(List<DialogueLine> lines, Action onDone)
        {
            if (lines == null || lines.Count == 0) { onDone?.Invoke(); return; }
            _queue = new List<DialogueLine>(lines);
            _qIndex = 0;
            _onDone = onDone;
            IsActive = true;
            ShowVisuals();
            ShowCurrent();
        }

        private void ShowCurrent()
        {
            var line = _queue[_qIndex];
            _speaker.text = (line.Speaker ?? "").ToUpperInvariant();
            _fullText = line.Text ?? "";
            _portrait.gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(line.PortraitSpriteKey))
            {
                _portrait.sprite = ProceduralArt.Get(line.PortraitSpriteKey);
                _portrait.gameObject.SetActive(_portrait.sprite != null);
            }
            bool instant = _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;
            _typed = instant ? _fullText.Length : 0f;
            _text.text = instant ? _fullText : "";
            _typing = !instant;
            _continue.gameObject.SetActive(instant);
            if (!instant) StartCoroutine(TypeRoutine());
        }

        private IEnumerator TypeRoutine()
        {
            float cps = _gameRoot.Config != null ? _gameRoot.Config.dialogueCharsPerSecond : 42f;
            float tick = 0f;
            while (_typing && _typed < _fullText.Length)
            {
                _typed += cps * Time.unscaledDeltaTime;
                int n = Mathf.Min(_fullText.Length, (int)_typed);
                if (n != _text.text.Length)
                {
                    _text.text = _fullText.Substring(0, n);
                    tick -= Time.unscaledDeltaTime;
                    if (tick <= 0f && n < _fullText.Length && n % 3 == 0)
                    {
                        tick = 0.09f;
                        _gameRoot.Audio.PlaySfx("sfx_click", 0.10f);
                    }
                }
                yield return null;
            }
            _text.text = _fullText;
            _typing = false;
            _continue.gameObject.SetActive(true);
        }

        public void Advance()
        {
            if (!IsActive) return;
            if (_typing)
            {
                _typing = false;
                _typed = _fullText.Length;
                _text.text = _fullText;
                _continue.gameObject.SetActive(true);
                return;
            }
            _qIndex++;
            if (_qIndex < _queue.Count) { ShowCurrent(); return; }
            IsActive = false;
            HideVisuals();
            var cb = _onDone;
            _onDone = null;
            Debug.Log("[Flow] dialogue finished, invoking callback");
            cb?.Invoke();
        }

        private void Update()
        {
            if (!IsActive) return;
            if (_gameRoot != null && _gameRoot.Paused) return; // no dialogue advance while paused
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
                Advance();
        }

        private void ShowVisuals() { _dim.gameObject.SetActive(true); _panel.SetActive(true); }
        private void HideVisuals() { _dim.gameObject.SetActive(false); _panel.SetActive(false); }

        public void Show() { /* dialogue visibility is queue-driven */ }
        public void Hide()
        {
            IsActive = false;
            _onDone = null;
            HideVisuals();
        }
    }
}
````

### `_Project/Scripts/UI/HUDView.cs` (11.5 KiB)

````csharp
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
    // Sanity, light battery, tracking meter, findings, prompts. Created by each scene bootstrap.
    public class HUDView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private Image _sanityFill;
        private Text _sanityLabel;
        private Text _objective;
        private Text _prompt;
        private Text _toast;
        private Text _findings;
        private Text _roomInfo;
        private GameObject _trackBar;
        private Image _trackFill;
        private Text _stageLabel;
        private GameObject _batteryBar;
        private Image _batteryFill;
        private float _toastUntil;
        private readonly List<EventSubscription> _subs = new List<EventSubscription>();

        public void Init(GameRoot root)
        {
            _gameRoot = root;

            var barBg = UIFactory.Panel(transform, "sanitybg", new Color(0.05f, 0.05f, 0.06f, 0.85f)).rectTransform;
            UIFactory.Anchor(barBg, new Vector2(0, 1), new Vector2(240, 46), new Vector2(132, -86));
            var label = UIFactory.Label(barBg, "cap", "SANITY", 13, UIFactory.InkDim, TextAnchor.MiddleLeft);
            UIFactory.Anchor(label.rectTransform, new Vector2(0, 1), new Vector2(80, 18), new Vector2(46, -10));
            var track = UIFactory.Panel(barBg, "track", new Color(0.12f, 0.12f, 0.13f)).rectTransform;
            UIFactory.Anchor(track, new Vector2(0, 0), new Vector2(216, 14), new Vector2(120, 12));
            _sanityFill = UIFactory.Img(track, "fill", ProceduralArt.Get("solid_white"), UIFactory.Ink);
            _sanityFill.rectTransform.anchorMin = new Vector2(0, 0);
            _sanityFill.rectTransform.anchorMax = new Vector2(1, 1);
            _sanityFill.rectTransform.pivot = new Vector2(0, 0.5f);
            _sanityFill.rectTransform.offsetMin = Vector2.zero;
            _sanityFill.rectTransform.offsetMax = Vector2.zero;
            _sanityLabel = UIFactory.Label(barBg, "val", "100", 13, UIFactory.Ink, TextAnchor.MiddleRight);
            UIFactory.Anchor(_sanityLabel.rectTransform, new Vector2(1, 1), new Vector2(48, 18), new Vector2(-30, -10));
            _stageLabel = UIFactory.Label(barBg, "stage", "STABLE", 11, UIFactory.InkFaint, TextAnchor.MiddleLeft);
            UIFactory.Anchor(_stageLabel.rectTransform, new Vector2(0, 1), new Vector2(150, 16), new Vector2(120, -28));

            var tbg = UIFactory.Panel(transform, "trackbg", new Color(0.05f, 0.05f, 0.06f, 0.85f)).rectTransform;
            UIFactory.Anchor(tbg, new Vector2(0, 1), new Vector2(240, 30), new Vector2(132, -140));
            var tcap = UIFactory.Label(tbg, "cap", "TRACK", 12, UIFactory.InkDim, TextAnchor.MiddleLeft);
            UIFactory.Anchor(tcap.rectTransform, new Vector2(0, 1), new Vector2(80, 16), new Vector2(46, -7));
            var ttrack = UIFactory.Panel(tbg, "track", new Color(0.12f, 0.12f, 0.13f)).rectTransform;
            UIFactory.Anchor(ttrack, new Vector2(0, 0), new Vector2(216, 10), new Vector2(120, 9));
            _trackFill = UIFactory.Img(ttrack, "fill", ProceduralArt.Get("solid_white"), UIFactory.AccentBright);
            _trackFill.rectTransform.anchorMin = new Vector2(0, 0);
            _trackFill.rectTransform.anchorMax = new Vector2(0, 1);
            _trackFill.rectTransform.pivot = new Vector2(0, 0.5f);
            _trackFill.rectTransform.offsetMin = Vector2.zero;
            _trackFill.rectTransform.offsetMax = Vector2.zero;
            _trackBar = tbg.gameObject;
            _trackBar.SetActive(false);

            var bbg = UIFactory.Panel(transform, "batterybg", new Color(0.05f, 0.05f, 0.06f, 0.85f)).rectTransform;
            UIFactory.Anchor(bbg, new Vector2(0, 1), new Vector2(240, 26), new Vector2(132, -176));
            var bcap = UIFactory.Label(bbg, "cap", "LIGHT", 12, UIFactory.InkDim, TextAnchor.MiddleLeft);
            UIFactory.Anchor(bcap.rectTransform, new Vector2(0, 1), new Vector2(80, 16), new Vector2(46, -5));
            var btrack = UIFactory.Panel(bbg, "track", new Color(0.12f, 0.12f, 0.13f)).rectTransform;
            UIFactory.Anchor(btrack, new Vector2(0, 0), new Vector2(216, 8), new Vector2(120, 9));
            _batteryFill = UIFactory.Img(btrack, "fill", ProceduralArt.Get("solid_white"), UIFactory.InkDim);
            _batteryFill.rectTransform.anchorMin = new Vector2(0, 0);
            _batteryFill.rectTransform.anchorMax = new Vector2(1, 1);
            _batteryFill.rectTransform.pivot = new Vector2(0, 0.5f);
            _batteryFill.rectTransform.offsetMin = Vector2.zero;
            _batteryFill.rectTransform.offsetMax = Vector2.zero;
            _batteryBar = bbg.gameObject;

            _findings = UIFactory.Label(transform, "findings", "FINDINGS 0/10", 15, UIFactory.InkDim, TextAnchor.MiddleRight);
            UIFactory.Anchor(_findings.rectTransform, new Vector2(1, 1), new Vector2(240, 24), new Vector2(-150, -64));

            _roomInfo = UIFactory.Label(transform, "room", "", 14, UIFactory.InkFaint, TextAnchor.MiddleRight);
            UIFactory.Anchor(_roomInfo.rectTransform, new Vector2(1, 1), new Vector2(300, 22), new Vector2(-180, -88));

            _objective = UIFactory.Label(transform, "objective", "", 15, UIFactory.InkDim);
            var ort = _objective.rectTransform;
            ort.anchorMin = new Vector2(0, 0); ort.anchorMax = new Vector2(0, 0);
            ort.pivot = new Vector2(0, 0);
            ort.sizeDelta = new Vector2(560, 66);
            ort.anchoredPosition = new Vector2(20, 16);

            _prompt = UIFactory.Label(transform, "prompt", "", 20, UIFactory.AccentBright, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_prompt.rectTransform, new Vector2(0.5f, 0), new Vector2(640, 30), new Vector2(0, 92));

            _toast = UIFactory.Label(transform, "toast", "", 18, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_toast.rectTransform, new Vector2(0.5f, 1), new Vector2(720, 30), new Vector2(0, -64));
            _toast.gameObject.SetActive(false);

            _subs.Add(root.Bus.Subscribe(EventIds.SanityChanged, e => SetSanity(e.Value)));
            _subs.Add(root.Bus.Subscribe(EventIds.SanityStageChanged, e =>
            {
                if (e.Data is SanityStage st && st == SanityStage.Breaking)
                    Notify("YOUR GRIP IS SLIPPING — GET BACK TO THE BOOTH");
            }));
            _subs.Add(root.Bus.Subscribe(EventIds.ObjectiveChanged, e => _objective.text = e.Text ?? ""));
            _subs.Add(root.Bus.Subscribe(EventIds.InteractionPrompt, e => _prompt.text = e.Text ?? ""));
            _subs.Add(root.Bus.Subscribe(EventIds.Notification, e => Notify(e.Text)));
            _subs.Add(root.Bus.Subscribe(EventIds.ClueDiscovered, e => RefreshFindings()));
            _subs.Add(root.Bus.Subscribe(EventIds.TrackingChanged, e => SetTracking(e.Value)));
            _subs.Add(root.Bus.Subscribe(EventIds.AnomalyRevealed, e => SetTracking(0f)));
            _subs.Add(root.Bus.Subscribe(EventIds.TokenCollected, e => RefreshFindings()));
            _subs.Add(root.Bus.Subscribe(EventIds.BatteryChanged, e => SetBattery(e.Value)));
            _subs.Add(root.Bus.Subscribe(EventIds.RoomEntered, e => RefreshRoom()));
            _subs.Add(root.Bus.Subscribe(EventIds.LightModeChanged, e => RefreshRoom()));
            _objective.text = root.ObjectiveText ?? "";   // HUD may be built after the publish
            _subs.Add(root.Bus.Subscribe(EventIds.LightModeChanged, e => RefreshRoom()));
            _subs.Add(root.Bus.Subscribe(EventIds.StateChanged, e => OnState(e.Payload<AppViewState>())));

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            foreach (var s in _subs) s?.Dispose();
            _subs.Clear();
        }

        private void OnState(AppViewState s)
        {
            bool exploring = s == AppViewState.Exploration;
            _prompt.gameObject.SetActive(true);
            _roomInfo.gameObject.SetActive(exploring);
            if (!exploring) _prompt.text = "";
            if (s == AppViewState.Checkpoint) _objective.text = "Inspect every visitor. Permanent decisions.";
            RefreshFindings();
            SetSanity(_gameRoot.Session != null ? _gameRoot.Session.Sanity : 100f);
        }

        public void SetSanity(float v)
        {
            float k = Mathf.Clamp01(v / SanityModel.Max);
            var rt = _sanityFill.rectTransform;
            rt.anchorMax = new Vector2(k, 1);
            var stage = SanityModel.StageFor(v);
            _sanityFill.color = stage == SanityStage.Breaking ? UIFactory.AccentBright :
                                stage == SanityStage.Shaken ? new Color(0.75f, 0.45f, 0.2f) :
                                stage == SanityStage.Uneasy ? UIFactory.InkDim : UIFactory.Ink;
            _sanityLabel.text = ((int)v).ToString();
            if (_stageLabel != null)
            {
                _stageLabel.text = stage.ToString().ToUpperInvariant();
                _stageLabel.color = stage == SanityStage.Breaking ? UIFactory.AccentBright :
                                    stage == SanityStage.Shaken ? new Color(0.75f, 0.45f, 0.2f) : UIFactory.InkFaint;
            }
        }

        private void SetBattery(float v)
        {
            if (_batteryBar == null) return;
            _batteryBar.SetActive(true);
            if (_batteryFill != null)
            {
                _batteryFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
                _batteryFill.color = v <= 0.2f ? UIFactory.AccentBright : UIFactory.InkDim;
            }
        }

        private void SetTracking(float v)
        {
            if (_trackBar == null) return;
            bool on = v > 0.001f && v < 0.999f;
            _trackBar.SetActive(on);
            if (on && _trackFill != null)
                _trackFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
        }

        public void RefreshFindings()
        {
            if (_gameRoot.Session == null) return;
            _findings.text = "FINDINGS " + _gameRoot.Session.Clues.FoundCount + "/10" +
                             (_gameRoot.Session.Clues.OrderIntact ? "" : "  (order broken)") +
                             (_gameRoot.Session.TokensCollected > 0 ? "  ·  TOKENS " + _gameRoot.Session.TokensCollected : "");
        }

        public void RefreshRoom()
        {
            if (_gameRoot.Views == null || _gameRoot.Views.Current == null) { _roomInfo.text = ""; return; }
            var room = _gameRoot.Views.Current;
            string light = room.Light != null ? room.Light.Mode.ToString().ToUpperInvariant() : "—";
            _roomInfo.text = room.DisplayName + "  ·  LIGHTS: " + light + "  ·  [F] flashlight";
        }

        public void Notify(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            _toast.text = msg;
            _toast.gameObject.SetActive(true);
            _toastUntil = Time.unscaledTime + 3.2f;
        }

        private void Update()
        {
            if (_toast.gameObject.activeSelf && Time.unscaledTime > _toastUntil)
                _toast.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            if (_gameRoot != null && _gameRoot.Session != null) { SetSanity(_gameRoot.Session.Sanity); RefreshFindings(); RefreshRoom(); }
        }

        public void Show() { gameObject.SetActive(true); Refresh(); }
        public void Hide() { gameObject.SetActive(false); }
    }
}
````

### `_Project/Scripts/UI/LoadingView.cs` (2.1 KiB)

````csharp
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
    // The black NIGHT title card behind intro dialogue. Created by each scene bootstrap.
    public class LoadingView : MonoBehaviour, IScreenView
    {
        private Text _label;
        private GameObject _bg;
        private Coroutine _routine;

        public void Init(GameRoot root)
        {
            _bg = UIFactory.Panel(transform, "bg", Color.black).gameObject;
            UIFactory.AnchorsFull(_bg.GetComponent<RectTransform>());
            _label = UIFactory.Label(_bg.transform, "label", "", 40, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900, 80), new Vector2(0, 30));
            var sub = UIFactory.Label(_bg.transform, "sub", "HOLLOWBROOK COLLEGE — NIGHT CUSTODIAN PROGRAM", 15, UIFactory.InkFaint, TextAnchor.MiddleCenter);
            UIFactory.Anchor(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900, 30), new Vector2(0, -30));
            _bg.SetActive(false);
        }

        public void Flash(string label)
        {
            _label.text = label ?? "";
            _bg.SetActive(true);
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(HideAfter(1.35f));
        }

        private IEnumerator HideAfter(float s)
        {
            yield return new WaitForSecondsRealtime(s);
            _bg.SetActive(false);
            _routine = null;
        }

        public void Show(string label)
        {
            _label.text = label ?? "";
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            _bg.SetActive(true);
        }

        public void Show() { _bg.SetActive(true); }
        public void Hide()
        {
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            _bg.SetActive(false);
        }
    }
}
````

### `_Project/Scripts/UI/MainMenuView.cs` (9.5 KiB)

````csharp
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
    // The left-column main menu, F2 reset and its ambience. Created by MenuSceneBootstrap.
    public class MainMenuView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private Button _continueBtn;
        private Button _newGameBtn;
        private Text _titleB;
        private Text _menuNote;
        private Image _shade, _vignette, _glint;
        private Coroutine _glitch, _ambient, _glintLoop;
        private int _f2Count;
        private float _f2Last = -99f;
        private float _noteUntil = -1f;

        private const float F2WindowSeconds = 1.5f;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            var bg = UIFactory.Img(transform, "bg", ProceduralArt.Get("menu_bg"));
            UIFactory.AnchorsFull(bg.rectTransform);
            bg.raycastTarget = true;

            _shade = UIFactory.Img(transform, "shade", ProceduralArt.Get("solid_white"), new Color(0, 0, 0, 0.28f));
            UIFactory.AnchorsFull(_shade.rectTransform);
            _shade.raycastTarget = false;
            _vignette = UIFactory.Img(transform, "vig", ProceduralArt.Get("vignette"), new Color(1, 1, 1, 0.35f));
            UIFactory.AnchorsFull(_vignette.rectTransform);
            _vignette.raycastTarget = false;

            var titleA = UIFactory.Label(transform, "titleA", "SHOW ME YOUR", 40, UIFactory.InkDim, TextAnchor.UpperLeft);
            UIFactory.Anchor(titleA.rectTransform, new Vector2(0f, 1f), new Vector2(760, 56), new Vector2(440, -84));
            _titleB = UIFactory.Label(transform, "titleB", "ID!", 108, UIFactory.Ink, TextAnchor.UpperLeft);
            UIFactory.Anchor(_titleB.rectTransform, new Vector2(0f, 1f), new Vector2(420, 120), new Vector2(270, -196));

            _glint = UIFactory.Img(transform, "glint", ProceduralArt.Get("solid_white"), new Color(0.9f, 0.9f, 0.88f, 0f));
            UIFactory.Anchor(_glint.rectTransform, new Vector2(0.5f, 1f), new Vector2(6, 3), new Vector2(1, -325));
            _glint.raycastTarget = false;

            var col = UIFactory.VStack(transform, "buttons", 8, TextAnchor.UpperLeft, false, false);
            var colRt = col.GetComponent<RectTransform>();
            UIFactory.Anchor(colRt, new Vector2(0f, 1f), new Vector2(360, 258), new Vector2(270, -419));

            MenuButtonLeft(col.transform, "NEW GAME", () =>
            {
                Debug.Log("[Flow] NEW GAME clicked");
                Click();
                _gameRoot.StartNewGame(GameMode.Normal);   // the only active gameplay entry
            }, b => { Emphasize(b); _newGameBtn = b; }, 360, 50);
            MenuButtonLeft(col.transform, "CONTINUE", () => { Click(); _gameRoot.ContinueGame(); }, b => _continueBtn = b, 360, 44);
            MenuButtonLeft(col.transform, "SETTINGS", () => { Click(); _gameRoot.OpenSettings(); }, null, 360, 44);
            MenuButtonLeft(col.transform, "CREDITS", () => { Click(); _gameRoot.OpenCredits(); }, null, 360, 44);
            MenuButtonLeft(col.transform, "EXIT", () =>
            {
                Click();
                _gameRoot.Confirm.Ask("EXIT?", "Save and quit to desktop?", () => _gameRoot.QuitApplication());
            }, null, 360, 44, danger: true);

            var footer = UIFactory.Label(transform, "footer",
                "Twin Studio · v1.0.6", 13, UIFactory.InkFaint, TextAnchor.LowerLeft);
            UIFactory.Anchor(footer.rectTransform, new Vector2(0f, 0f), new Vector2(900, 24), new Vector2(460, 14));

            _menuNote = UIFactory.Label(transform, "note", "", 14, UIFactory.InkFaint, TextAnchor.MiddleLeft);
            UIFactory.Anchor(_menuNote.rectTransform, new Vector2(0f, 0f), new Vector2(900, 22), new Vector2(460, 46));

            gameObject.SetActive(false);
        }

        private void Click() => _gameRoot.Audio.PlaySfx("sfx_click", 0.8f);

        private void MenuButtonLeft(Transform parent, string label, UnityEngine.Events.UnityAction onClick,
            System.Action<Button> created, int w = 360, int h = 44, bool danger = false)
        {
            var b = UIFactory.StandardButtonLeft(parent, label, label, () => onClick(), w, h, danger);
            UIFactory.Layout(b.GetComponent<RectTransform>(), w, h);
            created?.Invoke(b);
        }

        private void Emphasize(Button b)
        {
            var border = b.GetComponent<Image>();
            if (border != null) border.color = new Color(0.55f, 0.55f, 0.58f);
            var lbl = b.GetComponentInChildren<Text>();
            if (lbl != null) lbl.fontStyle = FontStyle.Bold;
        }

        private void ShowNote(string msg)
        {
            if (_menuNote == null) return;
            _menuNote.text = msg;
            _noteUntil = Time.unscaledTime + 4.5f;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshContinue();
            _menuNote.text = "";
            _noteUntil = -1f;
            if (CoreBootstrap.CreatedEventSystem != null && _newGameBtn != null)
                CoreBootstrap.CreatedEventSystem.SetSelectedGameObject(_newGameBtn.gameObject);
            if (_glitch == null) _glitch = StartCoroutine(TitleGlitchLoop());
            if (_ambient == null) _ambient = StartCoroutine(AmbientLoop());
            if (_glintLoop == null) _glintLoop = StartCoroutine(GlintLoop());
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            StopLoop(ref _glitch); StopLoop(ref _ambient); StopLoop(ref _glintLoop);
        }

        private void StopLoop(ref Coroutine c) { if (c != null) { StopCoroutine(c); c = null; } }

        public void RefreshContinue()
        {
            if (_continueBtn != null) UIFactory.SetButtonEnabled(_continueBtn, _gameRoot.HasSave());
        }

        private void Update()
        {
            if (_gameRoot == null) return;

            if (_noteUntil > 0f && Time.unscaledTime > _noteUntil)
            {
                _noteUntil = -1f;
                if (_menuNote != null) _menuNote.text = "";
            }

            if (_vignette != null)
            {
                bool reduce = _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;
                float a = reduce ? 0.38f : 0.34f + 0.05f * Mathf.Sin(Time.unscaledTime * 0.55f);
                var c = _vignette.color; c.a = a; _vignette.color = c;
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (Time.unscaledTime - _f2Last > F2WindowSeconds) _f2Count = 0;
                _f2Count++;
                _f2Last = Time.unscaledTime;
                _gameRoot.Audio.PlaySfx("sfx_glitch", 0.22f);
                if (_f2Count < 3)
                {
                    ShowNote("Data reset armed — press F2 " + (3 - _f2Count) + " more time" + (3 - _f2Count == 1 ? "" : "s") + " (" + _f2Count + "/3).");
                }
                else
                {
                    _f2Count = 0;
                    _gameRoot.RequestDataReset(() =>
                    {
                        RefreshContinue();
                        ShowNote("All save data erased.");
                    });
                }
            }
        }

        private IEnumerator TitleGlitchLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(12f, 22f));
                if (!gameObject.activeInHierarchy) continue;
                string original = _titleB.text;
                _titleB.text = "1D?";
                _titleB.color = UIFactory.AccentBright;
                _gameRoot.Audio.PlaySfx("sfx_glitch", 0.18f);
                yield return new WaitForSecondsRealtime(0.11f);
                _titleB.text = original;
                _titleB.color = UIFactory.Ink;
            }
        }

        private IEnumerator GlintLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(7f, 15f));
                if (!gameObject.activeInHierarchy || _glint == null) continue;
                if (_gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion) continue;
                float peak = _gameRoot.Settings != null && _gameRoot.Settings.PhotosensitivitySafe ? 0.45f : 0.85f;
                var c = _glint.color; c.a = peak; _glint.color = c;
                yield return new WaitForSecondsRealtime(0.14f);
                c = _glint.color; c.a = 0f; _glint.color = c;
            }
        }

        private IEnumerator AmbientLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(24f, 40f));
                if (!gameObject.activeInHierarchy) continue;
                if (_gameRoot.Settings != null && (_gameRoot.Settings.PhotosensitivitySafe || _gameRoot.Settings.ReduceMotion)) continue;
                _gameRoot.Audio.PlaySfx("sfx_static", 0.06f);
                float t = 0f;
                while (t < 0.55f)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Sin(Mathf.Clamp01(t / 0.55f) * Mathf.PI);
                    var c = _shade.color; c.a = 0.28f + 0.16f * k; _shade.color = c;
                    yield return null;
                }
                var c2 = _shade.color; c2.a = 0.28f; _shade.color = c2;
            }
        }
    }
}
````

### `_Project/Scripts/UI/ModeSelectView.cs` (3.6 KiB)

````csharp
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
        private GameRoot _gameRoot;
        private Text _endlessBest, _challengeBest;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            UIFactory.Dimmer(transform, "dim", 0.88f);
            var sheet = UIFactory.LeftSheet(transform, "SELECT ASSIGNMENT", "The contract, or the long night.", out var col);

            BuildCard(sheet, new Vector2(320, -180), "NORMAL — THE CONTRACT",
                "Five nights at Hollowbrook. Verify students, keep anomalies out, investigate the school.",
                () => _gameRoot.StartNewGame(GameMode.Normal), out _);
            BuildCard(sheet, new Vector2(320, -330), "ENDLESS WATCH",
                "One long night at the window. Correct calls buy seconds; the clock is the only boss.",
                () => _gameRoot.StartNewGame(GameMode.Endless), out _endlessBest);
            BuildCard(sheet, new Vector2(320, -480), "CHALLENGE PROTOCOL",
                "Shifting district rules, doubled sanity drain, doubled score.",
                () => _gameRoot.StartNewGame(GameMode.Challenge), out _challengeBest);

            UIFactory.SheetButton(sheet, "back", "◂ BACK", () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.6f);
                _gameRoot.SetState(AppViewState.MainMenu);
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
                _gameRoot.Audio.PlaySfx("sfx_bell", 0.7f);
                onSelect();
            }, 140, 40);
            UIFactory.Anchor(sel.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(140, 40), new Vector2(-90, 0));
            bestLabel = best;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            var rec = _gameRoot.Records;
            if (_endlessBest != null) _endlessBest.text = rec != null && rec.EndlessBest > 0 ? "BEST " + rec.EndlessBest : "";
            if (_challengeBest != null) _challengeBest.text = rec != null && rec.ChallengeBest > 0 ? "BEST " + rec.ChallengeBest : "";
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
````

### `_Project/Scripts/UI/PauseView.cs` (1.8 KiB)

````csharp
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
    // Pause sheet with resume/settings/quit. Created by each scene bootstrap.
    public class PauseView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            var sheet = UIFactory.LeftSheet(transform, "PAUSED", "The night waits. It is patient.", out var col);

            Add(col.transform, "RESUME", () => _gameRoot.TogglePause(false), false);
            Add(col.transform, "CONTROLS", () => _gameRoot.OpenControls(), false);
            Add(col.transform, "SETTINGS", () => _gameRoot.OpenSettings(), false);
            Add(col.transform, "SAVE & QUIT TO MENU", () =>
                _gameRoot.Confirm.Ask("QUIT TO MENU?", "Progress is saved at shift boundaries. Quit now?", () => _gameRoot.QuitToMenu(true)), true);
            Add(col.transform, "QUIT GAME", () =>
                _gameRoot.Confirm.Ask("QUIT GAME?", "Save and exit to desktop?", () => _gameRoot.QuitApplication()), true);

            gameObject.SetActive(false);
        }

        private void Add(Transform parent, string label, UnityEngine.Events.UnityAction onClick, bool danger)
        {
            var b = UIFactory.StandardButtonLeft(parent, label, label, () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_click", 0.7f);
                onClick();
            }, 420, 46, danger);
            UIFactory.Layout(b.GetComponent<RectTransform>(), 420, 46);
        }

        public void Show() { gameObject.SetActive(true); }
        public void Hide() { gameObject.SetActive(false); }
    }
}
````

### `_Project/Scripts/UI/ProgressionScreens.cs` (18.1 KiB)

````csharp
using System.Collections.Generic;
using System.Text;
using Hollowbrook.Core;
using Hollowbrook.Bootstrap;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    public class MapView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private RectTransform _hereMarker;
        private RectTransform _sheet;
        private readonly List<GameObject> _clueDots = new List<GameObject>();
        private Text _hint;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            UIFactory.Dimmer(transform, "dim", 0.86f);
            var head = UIFactory.Label(transform, "head", "HOLLOWBROOK COLLEGE — GROUND FLOOR", 28, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(800, 40), new Vector2(0, -64));

            var sheet = UIFactory.BorderedPanel(transform, "sheet", new Color(0.07f, 0.07f, 0.075f, 0.98f), UIFactory.Border, 2);
            UIFactory.Anchor(sheet, new Vector2(0.5f, 0.5f), new Vector2(940, 500), new Vector2(0, 10));
            _sheet = sheet;

            RoomBox(sheet, new Vector2(-360, 90), new Vector2(170, 130), "SECURITY\nCheckpoint", RoomId.Security, out _);
            RoomBox(sheet, new Vector2(-60, 90), new Vector2(430, 110), "MAIN HALLWAY", RoomId.Hallway, out _);
            RoomBox(sheet, new Vector2(-110, 210), new Vector2(200, 120), "CLASSROOM 1\nRoom 101", RoomId.Classroom1, out _);
            RoomBox(sheet, new Vector2(120, -30), new Vector2(200, 120), "CLASSROOM 2\nRoom 202", RoomId.Classroom2, out _);
            RoomBox(sheet, new Vector2(330, 90), new Vector2(180, 130), "SERVER /\nIDENTIFICATION ROOM\n— SEALED —", RoomId.Security, out _, sealedRoom: true);

            Line(sheet, new Vector2(-275, 90), new Vector2(-175, 90));
            Line(sheet, new Vector2(-110, 145), new Vector2(-110, 150));
            Line(sheet, new Vector2(20, 145), new Vector2(20, 30));
            Line(sheet, new Vector2(120, 90), new Vector2(240, 30));
            Line(sheet, new Vector2(155, 90), new Vector2(240, 90));

            _hereMarker = UIFactory.NewUI(sheet, "here");
            var dot = _hereMarker.gameObject.AddComponent<Image>();
            dot.sprite = ProceduralArt.Get("solid_white");
            dot.color = UIFactory.AccentBright;
            _hereMarker.sizeDelta = new Vector2(14, 14);
            var hereLbl = UIFactory.Label(_hereMarker, "lbl", "YOU", 12, UIFactory.AccentBright, TextAnchor.MiddleCenter);
            UIFactory.Anchor(hereLbl.rectTransform, new Vector2(0.5f, 0f), new Vector2(60, 18), new Vector2(0, -20));

            _hint = UIFactory.Label(transform, "hint", "", 15, UIFactory.InkDim, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(900, 26), new Vector2(0, 74));

            var close = UIFactory.StandardButton(transform, "close", "CLOSE [M]", () => _gameRoot.CloseMap(), 180, 42);
            UIFactory.Anchor(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(180, 42), new Vector2(0, 34));

            gameObject.SetActive(false);
        }

        private static void Line(RectTransform parent, Vector2 a, Vector2 b)
        {
            var img = UIFactory.Img(parent, "line", ProceduralArt.Get("solid_white"), UIFactory.Border);
            float dx = b.x - a.x, dy = b.y - a.y;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            UIFactory.Anchor(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(len, 3), new Vector2((a.x + b.x) / 2f, (a.y + b.y) / 2f));
            img.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
        }

        private void RoomBox(RectTransform sheet, Vector2 pos, Vector2 size, string label, RoomId target,
            out RectTransform box, bool sealedRoom = false)
        {
            box = UIFactory.BorderedPanel(sheet, "room_" + label.Replace("\n", ""), 
                sealedRoom ? new Color(0.10f, 0.05f, 0.05f, 0.98f) : new Color(0.11f, 0.11f, 0.12f, 0.98f),
                sealedRoom ? UIFactory.Accent : UIFactory.Border, 2);
            UIFactory.Anchor(box, new Vector2(0.5f, 0.5f), size, pos);
            var t = UIFactory.Label(box, "label", label, 15, sealedRoom ? UIFactory.AccentBright : UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.AnchorsFull(t.rectTransform);
            if (!sealedRoom)
            {
                var btn = box.gameObject.AddComponent<Button>();
                btn.targetGraphic = box.GetComponent<Image>();
                RoomId captured = target;
                btn.onClick.AddListener(() =>
                {
                    _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.5f);
                    _gameRoot.TravelViaMap(captured);
                });
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        private void Refresh()
        {
            foreach (var d in _clueDots) Destroy(d);
            _clueDots.Clear();

            var sheet = _sheet;
            if (sheet == null) return;

            RoomId here = _gameRoot.State == AppViewState.Exploration && _gameRoot.Views != null && _gameRoot.Views.Current != null
                ? _gameRoot.Views.Current.Id : RoomId.Security;
            _hereMarker.anchoredPosition = HerePos(here);

            var roomCounts = new Dictionary<RoomId, int>();
            foreach (var key in _gameRoot.Session.Clues.FoundKeys)
            {
                var def = _gameRoot.Session.Clues.Get(key);
                if (def == null) continue;
                int idx = roomCounts.TryGetValue(def.Room, out var c) ? c : 0;
                roomCounts[def.Room] = idx + 1;
                var dotGo = new GameObject("dot_" + key);
                dotGo.transform.SetParent(sheet, false);
                var img = dotGo.AddComponent<Image>();
                img.sprite = ProceduralArt.Get("solid_white");
                img.color = new Color(0.78f, 0.18f, 0.2f, 0.95f);
                var rt = (RectTransform)dotGo.transform;
                var basePos = HerePos(def.Room);
                rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(9, 9);
                rt.anchoredPosition = basePos + new Vector2(-46 + idx * 20, -34);
                _clueDots.Add(dotGo);
            }

            bool exploring = _gameRoot.State == AppViewState.Exploration;
            _hint.text = exploring
                ? "Click a room to move there. Red dots are findings you have discovered."
                : "From the checkpoint the map is reference only — use PATROL HALLWAY to investigate.";
        }

        private Vector2 HerePos(RoomId room)
        {
            switch (room)
            {
                case RoomId.Hallway: return new Vector2(-60, 90);
                case RoomId.Classroom1: return new Vector2(-110, 210);
                case RoomId.Classroom2: return new Vector2(120, -30);
                default: return new Vector2(-360, 90);
            }
        }

        private void Update()
        {
            if (gameObject.activeInHierarchy && (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape)))
            {
                if (_gameRoot.State == AppViewState.Map) _gameRoot.CloseMap();
            }
        }

        public void Hide() => gameObject.SetActive(false);
    }

    public class ShiftResultsView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private Text _title, _body, _rating;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            UIFactory.Dimmer(transform, "dim", 0.9f);
            var panel = UIFactory.BorderedPanel(transform, "panel", new Color(0.05f, 0.05f, 0.06f, 0.98f), UIFactory.Border, 2);
            UIFactory.Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(760, 540), Vector2.zero);
            _title = UIFactory.Label(panel, "title", "", 30, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(700, 44), new Vector2(0, -40));
            _body = UIFactory.Label(panel, "body", "", 17, UIFactory.InkDim);
            var brt = _body.rectTransform;
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 1);
            brt.offsetMin = new Vector2(60, 120); brt.offsetMax = new Vector2(-60, -96);
            _rating = UIFactory.Label(panel, "rating", "", 22, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_rating.rectTransform, new Vector2(0.5f, 0f), new Vector2(700, 34), new Vector2(0, 92));
            var cont = UIFactory.StandardButton(panel, "cont", "CONTINUE ▸", () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_click", 0.8f);
                _gameRoot.AdvanceToNextShift();
            }, 240, 46);
            UIFactory.Anchor(cont.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(240, 46), new Vector2(0, 36));
            gameObject.SetActive(false);
        }

        public void Populate(GameSession session, ShiftDef justFinished)
        {
            _title.text = "SHIFT " + justFinished.Number + " COMPLETE — " + justFinished.DateLabel;
            int shiftVisitors = justFinished.VisitorCount + (justFinished.HasAdministrator ? 1 : 0);
            var sb = new StringBuilder();
            sb.Append("Visitors processed tonight:  ").Append(shiftVisitors).Append('\n');
            sb.Append("Correct decisions (total):     ").Append(session.CorrectDecisions).Append(" / ").Append(session.Processed).Append('\n');
            sb.Append("Anomalies rejected (total):    ").Append(session.AnomaliesRejected).Append('\n');
            sb.Append("False rejections:              ").Append(session.FalseRejections).Append(" / 3\n");
            sb.Append("Anomaly breaches:              ").Append(session.Breaches).Append(" / 3\n");
            sb.Append("Sanity at clock-out:           ").Append(((int)session.Sanity)).Append("  (").Append(session.SanityStage).Append(")\n");
            sb.Append("Findings discovered:           ").Append(session.Clues.FoundCount).Append(" / 10");
            sb.Append(session.Clues.OrderIntact ? "   (sequence intact)\n" : "   (sequence broken)\n");
            if (justFinished.Number == 5)
                sb.Append("District administrator:        ").Append(session.AdminRejected ? "REJECTED — identified as an impostor\n" : "—\n");
            _body.text = sb.ToString();

            int mistakes = session.FalseRejections + session.Breaches;
            _rating.text = mistakes == 0 ? "DISTRICT EVALUATION: EXEMPLARY" :
                           mistakes <= 2 ? "DISTRICT EVALUATION: SATISFACTORY" :
                           "DISTRICT EVALUATION: UNDER REVIEW";
            _rating.color = mistakes == 0 ? UIFactory.Ink : mistakes <= 2 ? UIFactory.InkDim : UIFactory.AccentBright;
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }

    public class EndingView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private Text _title, _desc, _summary;
        private Text[] _badges = new Text[3];
        private Button _again;
        private GameMode _lastMode = GameMode.Normal;

        private const string WinTitle = "SURVIVING HOLLOWBROOK";
        private const string LoseTitle = "ANOTHER CUSTODIAN";
        private const string SecretTitle = "THE TRUTH BEHIND HOLLOWBROOK";

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            var bg = UIFactory.Panel(transform, "bg", Color.black);
            UIFactory.AnchorsFull(bg.rectTransform);

            _title = UIFactory.Label(transform, "title", "", 42, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(1100, 60), new Vector2(0, -74));

            _desc = UIFactory.Label(transform, "desc", "", 17, UIFactory.InkDim, TextAnchor.UpperCenter);
            UIFactory.Anchor(_desc.rectTransform, new Vector2(0.5f, 1f), new Vector2(860, 96), new Vector2(0, -150));

            var panel = UIFactory.BorderedPanel(transform, "summarypanel", new Color(0.06f, 0.06f, 0.07f, 0.96f), UIFactory.Border, 2);
            UIFactory.Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(700, 280), new Vector2(0, -70));
            var sumHead = UIFactory.Label(panel, "head", "DECISION SUMMARY", 16, UIFactory.InkFaint, TextAnchor.MiddleCenter);
            UIFactory.Anchor(sumHead.rectTransform, new Vector2(0.5f, 1f), new Vector2(400, 26), new Vector2(0, -14));
            _summary = UIFactory.Label(panel, "body", "", 15, UIFactory.InkDim);
            var srt = _summary.rectTransform;
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 1);
            srt.offsetMin = new Vector2(36, 18); srt.offsetMax = new Vector2(-36, -44);

            string[] badgeNames = { "WIN", "LOSE", "SECRET" };
            for (int i = 0; i < 3; i++)
            {
                var b = UIFactory.Label(transform, "badge" + i, "[ " + badgeNames[i] + " ]", 15, UIFactory.InkFaint, TextAnchor.MiddleCenter);
                UIFactory.Anchor(b.rectTransform, new Vector2(0.5f, 0f), new Vector2(160, 24), new Vector2(-170 + i * 170, 96));
                _badges[i] = b;
            }

            var menu = UIFactory.StandardButton(transform, "menu", "MAIN MENU", () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.7f);
                _gameRoot.QuitToMenu(false);
            }, 220, 46);
            UIFactory.Anchor(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(220, 46), new Vector2(-120, 38));
            _again = UIFactory.StandardButton(transform, "again", "PLAY AGAIN", () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_bell", 0.7f);
                _gameRoot.StartNewGame(_lastMode);
            }, 220, 46);
            UIFactory.Anchor(_again.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(220, 46), new Vector2(120, 38));

            gameObject.SetActive(false);
        }

        public void Populate(EndingRules.Evaluation eval, GameSession session, RecordsData records)
        {
            _lastMode = GameMode.Normal;
            switch (eval.Ending)
            {
                case EndingType.Secret:
                    _title.text = SecretTitle; _title.color = new Color(0.85f, 0.72f, 0.4f); break;
                case EndingType.Lose:
                    _title.text = LoseTitle; _title.color = UIFactory.AccentBright; break;
                default:
                    _title.text = WinTitle; _title.color = UIFactory.Ink; break;
            }
            _desc.text = eval.ReasonText;

            var sb = new StringBuilder();
            sb.Append("Nights survived:            ").Append(session.CompletedShifts).Append(" / 5\n");
            sb.Append("Visitors processed:         ").Append(session.Processed).Append('\n');
            sb.Append("Correct decisions:          ").Append(session.CorrectDecisions).Append('\n');
            sb.Append("Anomalies rejected:         ").Append(session.AnomaliesRejected).Append('\n');
            sb.Append("False rejections:           ").Append(session.FalseRejections).Append(" / 3\n");
            sb.Append("Anomaly breaches:           ").Append(session.Breaches).Append(" / 3\n");
            sb.Append("Administrator:              ").Append(session.AdminAllowed ? "ALLOWED IN (fatal mistake)" : session.AdminRejected ? "REJECTED — impostor identified" : "never arrived").Append('\n');
            sb.Append("Findings:                   ").Append(session.Clues.FoundCount).Append(" / 10  (").Append(session.Clues.OrderIntact ? "intended sequence intact" : "sequence broken").Append(")\n");
            sb.Append("Final sanity:               ").Append((int)session.Sanity).Append("  (").Append(session.SanityStage).Append(")\n");
            sb.Append("Score:                      ").Append(session.Score).Append("    Best streak: ").Append(session.BestStreak);
            _summary.text = sb.ToString();

            RefreshBadges(records);
            gameObject.SetActive(true);
        }

        public void PopulateRunResults(GameSession session, RecordsData records)
        {
            _lastMode = session.Mode;
            _title.text = session.Mode == GameMode.Endless ? "THE NIGHT ENDS" : "PROTOCOL TERMINATED";
            _title.color = UIFactory.Ink;
            _desc.text = "The clock ran out. The district's morning crew found the booth empty and the log full.";

            var sb = new StringBuilder();
            sb.Append("Mode:                       ").Append(session.Mode).Append('\n');
            sb.Append("Visitors processed:         ").Append(session.RunProcessed).Append('\n');
            sb.Append("Correct decisions:          ").Append(session.CorrectDecisions).Append('\n');
            sb.Append("Anomalies rejected:         ").Append(session.AnomaliesRejected).Append('\n');
            sb.Append("Mistakes:                   ").Append(session.FalseRejections + session.Breaches).Append('\n');
            sb.Append("Best streak:                ").Append(session.BestStreak).Append('\n');
            sb.Append("FINAL SCORE:                ").Append(session.Score).Append('\n');
            sb.Append(session.Mode == GameMode.Endless
                ? "BEST ENDLESS SCORE:         " + records.EndlessBest
                : "BEST CHALLENGE SCORE:       " + records.ChallengeBest);
            _summary.text = sb.ToString();

            RefreshBadges(records);
            gameObject.SetActive(true);
        }

        private void RefreshBadges(RecordsData records)
        {
            if (records == null) return;
            _badges[0].color = records.EndingsSeen.Contains("win") ? UIFactory.Ink : UIFactory.InkFaint;
            _badges[1].color = records.EndingsSeen.Contains("lose") ? UIFactory.AccentBright : UIFactory.InkFaint;
            _badges[2].color = records.EndingsSeen.Contains("secret") ? new Color(0.85f, 0.72f, 0.4f) : UIFactory.InkFaint;
        }

        public void Show() { }
        public void Hide() => gameObject.SetActive(false);
    }
}
````

### `_Project/Scripts/UI/SettingsView.cs` (17.0 KiB)

````csharp
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
    // The Options sheet: volumes, display, accessibility, reset. Created by each scene bootstrap.
    public class SettingsView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private bool _refreshing;

        private Slider _masterSlider, _sfxSlider;
        private Toggle _fullscreenToggle;
        private readonly Toggle[] _toggles = new Toggle[3];
        private readonly Button[] _textScaleButtons = new Button[3];
        private readonly Button[] _resButtons = new Button[4];
        private readonly Text[] _resLabels = new Text[4];
        private List<ModeSize> _choices = new List<ModeSize>();

        private RectTransform _displayNoteRow;
        private Text _displayNote;
        private SettingsData _displayBefore;
        private Coroutine _revertRoutine;
        private int _revertLeft;
        private Text _dataHint;
        private ScrollRect _scroll;
        private Slider _musicSlider;
        private GameObject _resRow;
        private Text _resNote;
        private readonly List<Selectable> _navOrder = new List<Selectable>();

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            var sheet = UIFactory.LeftSheet(transform, "OPTIONS",
                "Applies immediately. Remembered on this machine.", out var col);
            col.GetComponent<VerticalLayoutGroup>().spacing = UIFactory.SpaceUnit;
            col.sizeDelta = new Vector2(560f, 505f);
            col.anchoredPosition = new Vector2(320f, -364f);

            var viewport = UIFactory.NewUI(sheet, "optionsViewport");
            UIFactory.Anchor(viewport, new Vector2(0f, 1f), new Vector2(560f, 505f), new Vector2(320f, -364f));
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            viewportImage.raycastTarget = false;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            col.SetParent(viewport, false);
            col.anchorMin = new Vector2(0f, 1f); col.anchorMax = new Vector2(1f, 1f);
            col.pivot = new Vector2(0.5f, 1f);
            col.anchoredPosition = Vector2.zero;
            col.sizeDelta = Vector2.zero;
            col.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.content = col;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 24f;

            var s = _gameRoot.Settings;

            Section(col, "AUDIO");
            _masterSlider = UIFactory.SliderRow(col, "master", "Master volume", s.Current.MasterVolume,
                v => { if (!_refreshing) s.Apply(d => d.MasterVolume = v); }, 560);
            UIFactory.Layout(_masterSlider.GetComponent<RectTransform>(), 560, 34);
            _musicSlider = UIFactory.SliderRow(col, "music", "Music volume", s.Current.MusicVolume,
                v => { if (!_refreshing) s.Apply(d => d.MusicVolume = v); }, 560);
            UIFactory.Layout(_musicSlider.GetComponent<RectTransform>(), 560, 34);
            _sfxSlider = UIFactory.SliderRow(col, "sfx", "SFX volume", s.Current.SfxVolume,
                v => { if (!_refreshing) s.Apply(d => d.SfxVolume = v); }, 560);
            UIFactory.Layout(_sfxSlider.GetComponent<RectTransform>(), 560, 34);

            Section(col, "DISPLAY");
            _fullscreenToggle = UIFactory.ToggleRow(col, "fullscreen", "Fullscreen  (borderless)",
                s.Current.Fullscreen, v =>
                {
                    if (_refreshing) return;
                    var cur = _gameRoot.Settings.CurrentMode();
                    BeginDisplayChange(() => s.SetDisplay(
                        s.Current.DisplayWidth > 0 ? s.Current.DisplayWidth : cur.W,
                        s.Current.DisplayHeight > 0 ? s.Current.DisplayHeight : cur.H, v, false));
                }, 560);
            UIFactory.Layout(_fullscreenToggle.GetComponent<RectTransform>(), 560, 32);

            var resRow = UIFactory.NewUI(col, "resrow");
            _resRow = resRow.gameObject;
            UIFactory.Layout(resRow, 560, 38);
            var resLab = UIFactory.Label(resRow, "lab", "Resolution", 13, UIFactory.InkFaint,
                TextAnchor.MiddleLeft);
            UIFactory.Anchor(resLab.rectTransform, new Vector2(0f, 0.5f), new Vector2(120, 26),
                new Vector2(60, 0));
            for (int i = 0; i < _resButtons.Length; i++)
            {
                int idx = i;
                var b = UIFactory.StandardButton(resRow, "ResButton_" + i, "", () => ChooseResolution(idx), 100, 30);
                UIFactory.Anchor(b.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(100, 30),
                    new Vector2(186 + i * 108, 0));
                _resButtons[i] = b;
                _resLabels[i] = b.GetComponentInChildren<Text>();
                if (_resLabels[i] != null) UIFactory.SetBaseFontSize(_resLabels[i], 14);
            }

            _resNote = UIFactory.Label(col, "resnote", "", UIFactory.TypeSmall, UIFactory.InkDim,
                TextAnchor.MiddleLeft);
            _resNote.rectTransform.sizeDelta = new Vector2(560f, 16f);
            UIFactory.Layout(_resNote.rectTransform, 560, 16);
            _displayNoteRow = UIFactory.NewUI(sheet, "displaynote");
            UIFactory.Anchor(_displayNoteRow, new Vector2(0f, 0f), new Vector2(560, 30),
                new Vector2(320, 100));
            _displayNote = UIFactory.Label(_displayNoteRow, "txt", "", 14, UIFactory.AccentBright,
                TextAnchor.MiddleLeft);
            UIFactory.Anchor(_displayNote.rectTransform, new Vector2(0f, 0.5f), new Vector2(300, 26),
                new Vector2(150, 0));
            var keepBtn = UIFactory.StandardButton(_displayNoteRow, "keep", "KEEP",
                () => ConfirmDisplay(), 90, 28);
            UIFactory.Anchor(keepBtn.GetComponent<RectTransform>(), new Vector2(0f, 0.5f),
                new Vector2(90, 28), new Vector2(355, 0));
            var revertBtn = UIFactory.StandardButton(_displayNoteRow, "revert", "REVERT",
                () => CancelDisplay(), 90, 28);
            UIFactory.Anchor(revertBtn.GetComponent<RectTransform>(), new Vector2(0f, 0.5f),
                new Vector2(90, 28), new Vector2(455, 0));
            _displayNoteRow.gameObject.SetActive(false);

            Section(col, "ACCESSIBILITY");
            var tsRow = UIFactory.NewUI(col, "textscale");
            UIFactory.Layout(tsRow, 560, 38);
            var tsLab = UIFactory.Label(tsRow, "lab", "Text size", 17, UIFactory.Ink, TextAnchor.MiddleLeft);
            UIFactory.Anchor(tsLab.rectTransform, new Vector2(0f, 0.5f), new Vector2(180, 30),
                new Vector2(90, 0));
            string[] names = { "SMALL", "NORMAL", "LARGE" };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var b = UIFactory.StandardButton(tsRow, "TextScaleButton_" + i, names[i], () =>
                {
                    if (_refreshing) return;
                    s.Apply(d => d.TextScale = idx);
                    RefreshTextScaleButtons();
                }, 110, 34);
                UIFactory.Anchor(b.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(110, 34),
                    new Vector2(285 + i * 118, 0));
                _textScaleButtons[i] = b;
            }
            _toggles[0] = Toggle(col, "Reduced motion  (no shake, no lunges, instant text)",
                s.Current.ReduceMotion, v => s.Apply(d => d.ReduceMotion = v));
            _toggles[1] = Toggle(col, "Photosensitivity safe  (no strobe, softened scares)",
                s.Current.PhotosensitivitySafe, v => s.Apply(d => d.PhotosensitivitySafe = v));
            _toggles[2] = Toggle(col, "Captions for significant sounds",
                s.Current.Captions, v => s.Apply(d => d.Captions = v));

            Section(col, "DATA");
            var dataRow = UIFactory.NewUI(col, "datarow");
            UIFactory.Layout(dataRow, 560, 40);
            var resetBtn = UIFactory.StandardButton(dataRow, "resetdata", "RESET DATA…", () =>
            {
                _gameRoot.RequestDataReset(() =>
                {
                    if (_dataHint != null)
                    {
                        _dataHint.text = "Save data erased. Settings kept.";
                        _dataHint.color = UIFactory.AccentBright;
                    }
                });
            }, 200, 40, true);
            UIFactory.Anchor(resetBtn.GetComponent<RectTransform>(), new Vector2(0f, 0.5f),
                new Vector2(200, 40), new Vector2(100, 0));
            _dataHint = UIFactory.Label(dataRow, "hint",
                "Progress, records, endings. Volumes, display and accessibility are kept.",
                13, UIFactory.InkFaint, TextAnchor.MiddleLeft);
            UIFactory.Anchor(_dataHint.rectTransform, new Vector2(0f, 0.5f), new Vector2(340, 30),
                new Vector2(380, 0));

            UIFactory.SheetButton(sheet, "controls", "CONTROLS", () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_ui_open", 0.6f);
                _gameRoot.OpenControls();
            }, 140);
            UIFactory.SheetButton(sheet, "back", "◂ BACK", () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.6f);
                _gameRoot.CloseSettings();
            }, 360);

            gameObject.SetActive(false);
        }

        private static void Section(RectTransform col, string title)
        {
            var lab = UIFactory.Label(col, "sec_" + title, title, UIFactory.TypeSection, UIFactory.InkDim,
                TextAnchor.MiddleLeft);
            lab.rectTransform.sizeDelta = new Vector2(560f, 18f);
            lab.raycastTarget = false;
            UIFactory.Layout(lab.rectTransform, 560, 18);
        }

        private Toggle Toggle(RectTransform col, string label, bool value,
            UnityEngine.Events.UnityAction<bool> onChanged)
        {
            var t = UIFactory.ToggleRow(col, label, label, value, v =>
            {
                if (!_refreshing) onChanged(v);
            }, 560);
            UIFactory.Layout(t.GetComponent<RectTransform>(), 560, 32);
            return t;
        }

        private void ChooseResolution(int idx)
        {
            if (_refreshing || idx < 0 || idx >= _choices.Count) return;
            var m = _choices[idx];
            BeginDisplayChange(() => _gameRoot.Settings.SetDisplay(m.W, m.H, _gameRoot.Settings.Current.Fullscreen, false));
        }

        private void BeginDisplayChange(UnityEngine.Events.UnityAction change)
        {
            if (_displayBefore == null) _displayBefore = _gameRoot.Settings.Current.Clone();
            change();
            _revertLeft = DisplayPolicy.RevertSeconds;
            if (_displayNoteRow != null) _displayNoteRow.gameObject.SetActive(true);
            if (_revertRoutine != null) StopCoroutine(_revertRoutine);
            _revertRoutine = StartCoroutine(RevertCountdown());
            RefreshDisplayUI();
        }

        private IEnumerator RevertCountdown()
        {
            while (_revertLeft > 0)
            {
                if (_displayNote != null)
                    _displayNote.text = "Keep " + ModeText(_gameRoot.Settings.CurrentMode()) +
                                        "?  reverts in " + _revertLeft + "s";
                yield return new WaitForSecondsRealtime(1f);
                _revertLeft--;
            }
            CancelDisplay();
        }

        private void ConfirmDisplay()
        {
            if (_revertRoutine != null) StopCoroutine(_revertRoutine);
            _revertRoutine = null;
            _gameRoot.Settings.ApplyCurrentDisplay(true);
            _displayBefore = null;
            if (_displayNoteRow != null) _displayNoteRow.gameObject.SetActive(false);
            RefreshDisplayUI();
        }

        private void CancelDisplay()
        {
            if (_revertRoutine != null) StopCoroutine(_revertRoutine);
            _revertRoutine = null;
            _gameRoot.Settings.RestoreDisplaySnapshot(_displayBefore);
            _displayBefore = null;
            if (_displayNoteRow != null) _displayNoteRow.gameObject.SetActive(false);
            RefreshDisplayUI();
        }

        private static string ModeText(ModeSize m) => m.W + "×" + m.H;

        public void Show()
        {
            gameObject.SetActive(true);
            Refresh();
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;   // open at the top
            _navOrder.Clear();
            foreach (var sel in new Selectable[]
            {
                _masterSlider, _musicSlider, _sfxSlider, _fullscreenToggle,
                _toggles[0], _toggles[1], _toggles[2],
                _textScaleButtons.Length > 0 ? _textScaleButtons[0] : null,
                _textScaleButtons.Length > 1 ? _textScaleButtons[1] : null,
                _textScaleButtons.Length > 2 ? _textScaleButtons[2] : null,
            })
                if (sel != null && sel.interactable && sel.gameObject.activeInHierarchy) _navOrder.Add(sel);
            if (_navOrder.Count > 0 && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_navOrder[0].gameObject);
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy || _gameRoot == null) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { _gameRoot.CloseSettings(); return; }
            if (Input.GetKeyDown(KeyCode.Tab) && _navOrder.Count > 0 && EventSystem.current != null)
            {
                var curSel = EventSystem.current.currentSelectedGameObject != null
                    ? EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>() : null;
                int i = _navOrder.IndexOf(curSel);
                i = (i + (Input.GetKey(KeyCode.LeftShift) ? _navOrder.Count - 1 : 1) + _navOrder.Count) % _navOrder.Count;
                EventSystem.current.SetSelectedGameObject(_navOrder[i].gameObject);
            }
        }

        public void Hide()
        {
            if (_displayBefore != null) CancelDisplay();
            gameObject.SetActive(false);
        }

        private void Refresh()
        {
            _refreshing = true;
            var s = _gameRoot.Settings.Current;
            if (_masterSlider != null) _masterSlider.value = s.MasterVolume;
            if (_musicSlider != null) _musicSlider.value = s.MusicVolume;
            if (_sfxSlider != null) _sfxSlider.value = s.SfxVolume;
            if (_fullscreenToggle != null) _fullscreenToggle.isOn = s.Fullscreen;
            if (_toggles[0] != null) _toggles[0].isOn = s.ReduceMotion;
            if (_toggles[1] != null) _toggles[1].isOn = s.PhotosensitivitySafe;
            if (_toggles[2] != null) _toggles[2].isOn = s.Captions;
            _refreshing = false;
            RefreshTextScaleButtons();
            RefreshDisplayUI();
        }

        private void RefreshDisplayUI()
        {
            bool fs = _gameRoot.Settings.Current.Fullscreen;
            if (_resRow != null) _resRow.SetActive(!fs);   // borderless = desktop resolution
            if (_resNote != null) _resNote.text = fs ? "Borderless fullscreen uses the desktop resolution." : "";
            _choices = _gameRoot.Settings.MenuChoices();
            var cur = _gameRoot.Settings.CurrentMode();
            var stored = new ModeSize(_gameRoot.Settings.Current.DisplayWidth, _gameRoot.Settings.Current.DisplayHeight);
            var active = stored.W > 0 && stored.H > 0 ? stored : cur;
            for (int i = 0; i < _resButtons.Length; i++)
            {
                if (_resButtons[i] == null) continue;
                bool has = i < _choices.Count;
                _resButtons[i].gameObject.SetActive(has);
                if (!has) continue;
                var m = _choices[i];
                if (_resLabels[i] != null) _resLabels[i].text = ModeText(m);
                SetSelected(_resButtons[i], m.Equals(active));
            }
        }

        private static void SetSelected(Button b, bool sel)
        {
            if (b == null) return;
            var inner = b.GetComponentInChildren<Image>();
            var label = b.GetComponentInChildren<Text>();
            if (inner != null) inner.color = sel ? UIFactory.Ink : UIFactory.PanelAlt;
            if (label != null) label.color = sel ? new Color(0.06f, 0.06f, 0.07f) : UIFactory.Ink;
        }

        private void RefreshTextScaleButtons()
        {
            if (_textScaleButtons == null) return;
            int sel = _gameRoot.Settings.Current.TextScale;
            for (int i = 0; i < _textScaleButtons.Length; i++)
            {
                var b = _textScaleButtons[i];
                if (b == null) continue;
                SetSelected(b, i == sel);
            }
        }
    }
}
````

### `_Project/Scripts/UI/UIFactory.cs` (20.5 KiB)

````csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    // Builds every button/label/panel so the whole game shares one look.
    public static class UIFactory
    {
        public static readonly Color Bg = new Color(0.035f, 0.035f, 0.04f);
        public static readonly Color PanelBase = new Color(0.09f, 0.09f, 0.10f);
        public static readonly Color PanelAlt = new Color(0.14f, 0.14f, 0.15f);
        public static readonly Color Border = new Color(0.30f, 0.30f, 0.32f);
        public static readonly Color Ink = new Color(0.92f, 0.92f, 0.90f);
        public static readonly Color InkDim = new Color(0.62f, 0.62f, 0.60f);
        public static readonly Color InkFaint = new Color(0.42f, 0.42f, 0.42f);
        public static readonly Color Accent = new Color(0.70f, 0.13f, 0.17f);   // restrained red
        public static readonly Color AccentBright = new Color(0.90f, 0.22f, 0.26f);
        public static readonly Color Paper = new Color(0.82f, 0.81f, 0.77f);    // documents
        public static readonly Color PaperInk = new Color(0.10f, 0.10f, 0.11f);
        public static readonly Color Glass = new Color(0.75f, 0.78f, 0.80f, 0.10f);

        public const int TypeTitle = 30;
        public const int TypeSubtitle = 14;
        public const int TypeSection = 14;
        public const int TypeLabel = 17;
        public const int TypeSmall = 13;
        public const int SpaceUnit = 8;

        private static Font _font;
        private static readonly List<Text> _texts = new List<Text>();
        private static readonly Dictionary<Text, int> _baseSizes = new Dictionary<Text, int>();
        private static float _textScale = 1f;

        public static Font GameFont
        {
            get
            {
                if (_font != null) return _font;
                try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (Exception) { }
                if (_font == null) { try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (Exception) { } }
                if (_font == null) Debug.LogError("[UIFactory] No built-in font available — text will not render.");
                return _font;
            }
        }

        public static Canvas CreateRootCanvas(string name, int sortingOrder, int refW, int refH)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;  // explicit: AspectMatchGuard drives matchWidthOrHeight
            scaler.referenceResolution = new Vector2(refW, refH);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform NewUI(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void AnchorsFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        public static Image Panel(Transform parent, string name, Color color)
        {
            var rt = NewUI(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            return img;
        }

        // A filled panel with a drawn border. Returns the OUTER rect: callers anchor and
        // show/hide the outer, and the fill stretches inside it automatically. (Returning the
        // inner once left a full-screen border rect behind when callers hid only the inner.)
        public static RectTransform BorderedPanel(Transform parent, string name, Color fill, Color border, int thickness = 2)
        {
            var outer = Panel(parent, name + "_border", border).rectTransform;
            var inner = Panel(outer, name, fill).rectTransform;
            inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one;
            inner.offsetMin = new Vector2(thickness, thickness);
            inner.offsetMax = new Vector2(-thickness, -thickness);
            return outer;
        }

        // Records a new base size for the text-scale setting. Use this instead of assigning
        // .fontSize directly, otherwise ApplyTextScale overwrites the manual size later.
        public static void SetBaseFontSize(Text text, int size)
        {
            if (text == null) return;
            _baseSizes[text] = size;
            text.fontSize = Mathf.RoundToInt(size * _textScale);
        }

        public static Text Label(Transform parent, string name, string text, int fontSize, Color color,
            TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var rt = NewUI(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = GameFont;
            t.text = text ?? "";
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            _baseSizes[t] = fontSize;
            t.fontSize = Mathf.RoundToInt(fontSize * _textScale);
            _texts.Add(t);
            return t;
        }

        public static void ApplyTextScale(float multiplier)
        {
            _textScale = multiplier;
            for (int i = _texts.Count - 1; i >= 0; i--)
            {
                var t = _texts[i];
                if (t == null || t.gameObject == null) { _texts.RemoveAt(i); continue; }
                if (_baseSizes.TryGetValue(t, out int baseSize))
                    t.fontSize = Mathf.RoundToInt(baseSize * multiplier);
            }
        }

        public static void PruneDestroyedTexts()
        {
            for (int i = _texts.Count - 1; i >= 0; i--)
                if (_texts[i] == null || _texts[i].gameObject == null) _texts.RemoveAt(i);
        }

        public static Button StandardButton(Transform parent, string name, string label, Action onClick,
            int width = 260, int height = 46, bool danger = false)
        {
            var rt = NewUI(parent, name);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            var border = rt.gameObject.AddComponent<Image>();
            border.color = danger ? Accent : Border;
            var innerRt = NewUI(rt, "bg");
            innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(2, 2); innerRt.offsetMax = new Vector2(-2, -2);
            var inner = innerRt.gameObject.AddComponent<Image>();
            inner.color = danger ? new Color(0.16f, 0.05f, 0.06f) : PanelAlt;

            var txt = Label(innerRt, "label", label, 20, danger ? AccentBright : Ink, TextAnchor.MiddleCenter);
            Anchor(txt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(width - 12, height - 8), Vector2.zero);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = inner;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public static Button StandardButtonLeft(Transform parent, string name, string label, Action onClick,
            int width = 360, int height = 46, bool danger = false)
        {
            var b = StandardButton(parent, name, label, onClick, width, height, danger);
            var lbl = b.GetComponentInChildren<Text>();
            if (lbl != null)
            {
                var rt = lbl.rectTransform;
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(width - 44, height - 8);
                rt.anchoredPosition = new Vector2(22 + (width - 44) / 2f, 0);
                lbl.alignment = TextAnchor.MiddleLeft;
            }
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.45f, 1.45f, 1.55f);   // subtle brighten on hover/focus
            cb.pressedColor = new Color(1.9f, 1.9f, 2.0f);
            cb.selectedColor = new Color(1.25f, 1.25f, 1.3f);
            cb.disabledColor = new Color(0.55f, 0.55f, 0.58f);
            cb.fadeDuration = 0.12f;
            b.colors = cb;
            return b;
        }

        public static Button IconButton(Transform parent, string name, Sprite sprite, Action onClick,
            int size = 40, Color? tint = null)
        {
            var rt = NewUI(parent, name);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = tint ?? Ink;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public static void SetButtonEnabled(Button b, bool on)
        {
            if (b == null) return;
            b.interactable = on;
            if (b.targetGraphic != null)
                b.targetGraphic.color = on ? PanelAlt : new Color(0.08f, 0.08f, 0.085f);
        }

        public static Slider SliderRow(Transform parent, string name, string labelText, float value,
            Action<float> onChanged, int width = 420)
        {
            var row = NewUI(parent, name);
            row.sizeDelta = new Vector2(width, 40);

            var rowBg = Panel(row, "rowbg", new Color(0f, 0f, 0f, 0f));
            AnchorsFull(rowBg.rectTransform);
            rowBg.raycastTarget = true;

            float labW = width * 0.36f;
            var lab = Label(row, "label", labelText, 17, Ink);
            Anchor(lab.rectTransform, new Vector2(0f, 0.5f), new Vector2(labW, 30), new Vector2(labW / 2f, 0));
            lab.alignment = TextAnchor.MiddleLeft;
            lab.raycastTarget = false;

            float trackX = width * 0.38f;
            float trackW = width * 0.50f;
            var track = Panel(row, "track", PanelAlt).rectTransform;
            Anchor(track, new Vector2(0f, 0.5f), new Vector2(trackW, 8), new Vector2(trackX + trackW / 2f, 0));

            var fillArea = NewUI(track, "fillarea");
            fillArea.anchorMin = Vector2.zero; fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = Vector2.zero; fillArea.offsetMax = Vector2.zero;
            var fill = Panel(fillArea, "fill", Ink).rectTransform;
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.sizeDelta = new Vector2(0, 0);
            fill.anchoredPosition = Vector2.zero;

            var handleArea = NewUI(track, "handlearea");
            handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = Vector2.zero; handleArea.offsetMax = Vector2.zero;
            var handleImg = Panel(handleArea, "handle", InkDim);
            var handle = handleImg.rectTransform;
            Anchor(handle, new Vector2(0.5f, 0.5f), new Vector2(14, 22), Vector2.zero);

            float valW = width * 0.12f;
            var val = Label(row, "value", Percent(value), 15, InkDim, TextAnchor.MiddleRight);
            Anchor(val.rectTransform, new Vector2(0f, 0.5f), new Vector2(valW, 26),
                new Vector2(trackX + trackW + valW / 2f, 0));
            val.raycastTarget = false;

            var slider = row.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.minValue = 0f; slider.maxValue = 1f;
            slider.value = value;
            slider.targetGraphic = handleImg;
            slider.onValueChanged.AddListener(v =>
            {
                if (val != null) val.text = Percent(v);
                if (onChanged != null) onChanged(v);
            });

            AddHover(row.gameObject,
                () => { if (handleImg != null) handleImg.color = Ink; if (lab != null) lab.color = Ink; },
                () => { if (handleImg != null) handleImg.color = InkDim; if (lab != null) lab.color = Ink; });
            return slider;
        }

        private static string Percent(float v) => Mathf.RoundToInt(Mathf.Clamp01(v) * 100f) + "%";

        public static void AddHover(GameObject go, UnityEngine.Events.UnityAction enter,
            UnityEngine.Events.UnityAction exit)
        {
            if (go == null) return;
            var et = go.AddComponent<EventTrigger>();
            var on = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            on.callback.AddListener(_ => { if (enter != null) enter(); });
            et.triggers.Add(on);
            var off = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            off.callback.AddListener(_ => { if (exit != null) exit(); });
            et.triggers.Add(off);
        }

        public static Toggle ToggleRow(Transform parent, string name, string labelText, bool value,
            Action<bool> onChanged, int width = 420)
        {
            var row = NewUI(parent, name);
            row.anchorMin = new Vector2(0.5f, 0.5f);
            row.anchorMax = new Vector2(0.5f, 0.5f);
            row.sizeDelta = new Vector2(width, 36);

            var rowBg = Panel(row, "rowbg", new Color(0f, 0f, 0f, 0f));
            AnchorsFull(rowBg.rectTransform);
            rowBg.raycastTarget = true;

            var box = Panel(row, "box", PanelAlt).rectTransform;
            Anchor(box, new Vector2(0f, 0.5f), new Vector2(24, 24), new Vector2(16, 0)); // left-anchored: positive
            var boxBorder = box.gameObject.AddComponent<Outline>();
            boxBorder.effectColor = Border;
            boxBorder.effectDistance = new Vector2(2, 2);

            var check = Label(box, "check", value ? "ON" : "OFF", 10,
                value ? PanelBase : InkDim, TextAnchor.MiddleCenter);
            Anchor(check.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(24, 24), Vector2.zero);
            var boxImg = box.GetComponent<Image>();
            if (boxImg != null) boxImg.color = value ? Ink : PanelAlt;

            var lab = Label(row, "label", labelText, 17, Ink);
            lab.alignment = TextAnchor.MiddleLeft;
            lab.raycastTarget = false; // clicks handled by rowBg catcher
            Anchor(lab.rectTransform, new Vector2(0f, 0.5f), new Vector2(width - 48, 30),
                new Vector2(34 + (width - 48) / 2f, 0)); // left-anchored: positive center

            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.graphic = null;   // we drive box+word ourselves, so state is never ambiguous
            toggle.onValueChanged.AddListener(on =>
            {
                check.text = on ? "ON" : "OFF";
                check.color = on ? PanelBase : InkDim;
                var bi = box.GetComponent<Image>();
                if (bi != null) bi.color = on ? Ink : PanelAlt;
            });
            toggle.isOn = value;
            if (onChanged != null) toggle.onValueChanged.AddListener(v => onChanged(v));

            var bgImg = rowBg;
            AddHover(row.gameObject,
                () => { if (bgImg != null) bgImg.color = new Color(1f, 1f, 1f, 0.045f); },
                () => { if (bgImg != null) bgImg.color = new Color(0f, 0f, 0f, 0f); });
            return toggle;
        }

        public static VerticalLayoutGroup VStack(Transform parent, string name, int spacing,
            TextAnchor align = TextAnchor.UpperCenter, bool controlW = true, bool controlH = true,
            bool forceW = false, bool forceH = false)
        {
            var rt = NewUI(parent, name);
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.childAlignment = align;
            g.childControlWidth = controlW;
            g.childControlHeight = controlH;
            g.childForceExpandWidth = forceW;
            g.childForceExpandHeight = forceH;
            return g;
        }

        public static HorizontalLayoutGroup HStack(Transform parent, string name, int spacing,
            TextAnchor align = TextAnchor.MiddleLeft)
        {
            var rt = NewUI(parent, name);
            var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = false;
            return g;
        }

        public static LayoutElement Layout(RectTransform rt, float prefW = -1, float prefH = -1,
            float minW = -1, float minH = -1)
        {
            var le = rt.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = prefW; le.preferredHeight = prefH;
            le.minWidth = minW; le.minHeight = minH;
            return le;
        }

        public static RectTransform LeftSheet(Transform parent, string title, string subtitle,
            out RectTransform contentColumn)
        {
            Dimmer(parent, "dim", 0.82f);
            var panel = Panel(parent, "sheet", PanelBase);
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(0f, 1f);
            prt.pivot = new Vector2(0f, 0.5f);
            prt.sizeDelta = new Vector2(640f, 0f);
            prt.anchoredPosition = Vector2.zero;

            var edge = Panel(prt, "edge", Border);
            var ert = edge.rectTransform;
            ert.anchorMin = new Vector2(1f, 0f); ert.anchorMax = new Vector2(1f, 1f);
            ert.offsetMin = new Vector2(-2f, 0f); ert.offsetMax = Vector2.zero;

            var t = Label(prt, "title", title, 30, Ink, TextAnchor.UpperLeft);
            Anchor(t.rectTransform, new Vector2(0f, 1f), new Vector2(560f, 40f), new Vector2(320f, -52f));
            if (!string.IsNullOrEmpty(subtitle))
            {
                var st = Label(prt, "subtitle", subtitle, 13, InkFaint, TextAnchor.UpperLeft);
                Anchor(st.rectTransform, new Vector2(0f, 1f), new Vector2(560f, 20f), new Vector2(320f, -88f));
            }

            var col = VStack(prt, "rows", 10, TextAnchor.UpperLeft, false, false);
            Anchor(col.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(560f, 470f),
                new Vector2(320f, -120f - 235f));
            contentColumn = col.GetComponent<RectTransform>();
            return prt;
        }

        public static Button SheetButton(RectTransform sheet, string name, string label, Action onClick,
            float centerX, bool danger = false, int width = 200)
        {
            var b = StandardButton(sheet, name, label, onClick, width, 42, danger);
            Anchor(b.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(width, 42),
                new Vector2(centerX, 40f));
            return b;
        }

        public static Image Img(Transform parent, string name, Sprite sprite, Color? color = null, bool raycast = false)
        {
            var rt = NewUI(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color ?? Color.white;
            img.raycastTarget = raycast;
            return img;
        }

        public static CanvasGroup Group(GameObject go)
        {
            var g = go.GetComponent<CanvasGroup>();
            if (g == null) g = go.AddComponent<CanvasGroup>();
            return g;
        }

        public static Image Dimmer(Transform parent, string name, float alpha)
        {
            var img = Panel(parent, name, new Color(0, 0, 0, alpha));
            AnchorsFull(img.rectTransform);
            return img;
        }
    }
}
````

### `_Project/Scripts/World/FlashlightRig.cs` (6.1 KiB)

````csharp
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // Cursor-aimed beam, darkness mask and battery. Created by ViewManager.
    public class FlashlightRig : MonoBehaviour
    {
        public LightMode Beam { get; private set; } = LightMode.Off;
        public float Battery { get; private set; } = 1f;
        public bool BatteryDead { get; private set; }
        public bool BeamOn => Beam != LightMode.Off && !BatteryDead;

        private GameRoot _gameRoot;
        private Camera _cam;
        private SpriteRenderer _cone;
        private SpriteMask _mask;
        private bool _latchSweep, _latchFocus;
        private Vector3 _aimWorld;
        private float _pubThrottle;

        public void Init(GameRoot root, Camera cam, SpriteRenderer cone)
        {
            _gameRoot = root; _cam = cam; _cone = cone;
            var maskGo = new GameObject("FlashlightMask");
            maskGo.transform.SetParent(transform, false);
            _mask = maskGo.AddComponent<SpriteMask>();
            _mask.sprite = cone != null ? cone.sprite : null;
            _mask.isCustomRangeActive = true;
            _mask.frontSortingOrder = 60;
            _mask.backSortingOrder = 70;
            ApplyBeam(LightMode.Off, true);
        }

        public bool ConeCovers(Vector3 worldPoint)
        {
            if (!BeamOn || _cone == null) return false;
            float dx = worldPoint.x - _cone.transform.localPosition.x;
            float dy = worldPoint.y - _cone.transform.localPosition.y;
            if (Beam == LightMode.Focus) return Mathf.Abs(dx) < 7.0f && Mathf.Sign(dx) == _side && Mathf.Abs(dy) < 1.7f;
            return Mathf.Abs(dx) < 6.2f && Mathf.Sign(dx) == _side && Mathf.Abs(dy) < 3.4f;
        }
        private float _side = 1f;

        private void Update()
        {
            if (_gameRoot == null || _gameRoot.Paused || _cam == null) return;
            ReadInput();
            AimBeam();
            TickBattery(Time.deltaTime);
        }

        private void ReadInput()
        {
            if (Input.GetKeyDown(KeyCode.F)) _latchSweep = !_latchSweep && !BatteryDead;
            if (Input.GetKeyDown(KeyCode.Q)) _latchFocus = !_latchFocus && !BatteryDead;
            if (Input.GetKeyDown(KeyCode.L)) { _latchSweep = false; _latchFocus = false; }
            bool holdSweep = Input.GetMouseButton(0);
            bool holdFocus = Input.GetMouseButton(1);
            var want = holdFocus ? LightMode.Focus : holdSweep ? LightMode.Sweep
                       : _latchFocus ? LightMode.Focus : _latchSweep ? LightMode.Sweep : LightMode.Off;
            if (BatteryDead && want != LightMode.Off) want = LightMode.Off;
            if (want != Beam) ApplyBeam(want, false);
        }

        // Called when the player enters a room: re-apply the current beam to the new room light.
        public void SyncRoom()
        {
            if (Beam != LightMode.Off) ApplyBeam(Beam, true);
        }

        private void ApplyBeam(LightMode m, bool force)
        {
            Beam = m;
            bool on = m != LightMode.Off && !BatteryDead;
            if (_mask != null) _mask.gameObject.SetActive(on);
            if (_cone != null) _cone.gameObject.SetActive(on);
            var room = _gameRoot.Views != null && _gameRoot.Views.Current != null ? _gameRoot.Views.Current.Light : null;
            if (room != null && !force)
            {
                if (m == LightMode.Focus) room.SetMode(LightMode.Focus);
                else if (m == LightMode.Sweep && room.Mode == LightMode.Off) room.SetMode(LightMode.Sweep);
            }
            PublishBattery();
        }

        private void AimBeam()
        {
            if (_cone == null) return;
            var player = _gameRoot.Views != null && _gameRoot.Views.Player != null ? _gameRoot.Views.Player.transform.localPosition : Vector3.zero;
            _aimWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
            _aimWorld.z = 0f;
            _side = _aimWorld.x >= player.x ? 1f : -1f;
            float dy = Mathf.Clamp(_aimWorld.y - player.y, -2.6f, 2.6f);
            var pos = new Vector3(player.x + _side * 0.6f, player.y + dy * 0.75f, -0.5f);
            _cone.transform.localPosition = pos;
            if (_mask != null) _mask.transform.localPosition = pos;
            float w = Beam == LightMode.Focus ? 0.62f : 1.15f;
            float h = Beam == LightMode.Focus ? 0.55f : 0.95f;
            _cone.transform.localScale = new Vector3(_side * w, h, 1f);
            if (_mask != null) _mask.transform.localScale = new Vector3(w * 1.25f, h * 1.25f, 1f);
        }

        private void TickBattery(float dt)
        {
            var cfg = _gameRoot.Config;
            float before = Battery;
            if (BeamOn)
                Battery -= dt * (Beam == LightMode.Focus ? cfg.batteryDrainFocusPerSecond : cfg.batteryDrainSweepPerSecond) / 100f;
            else
            {
                bool litRoom = _gameRoot.Views != null && _gameRoot.Views.Current != null && _gameRoot.Views.Current.Light != null
                               && _gameRoot.Views.Current.Light.Mode == LightMode.Sweep;
                Battery += dt * (litRoom ? cfg.batteryRecoverLitPerSecond : cfg.batteryRecoverOffPerSecond) / 100f;
            }
            Battery = Mathf.Clamp01(Battery);
            if (Battery <= 0f && !BatteryDead)
            {
                BatteryDead = true;
                ApplyBeam(LightMode.Off, false);
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*the flashlight dies — let it breathe*", 2.4f, null));
                _gameRoot.Audio.PlaySfx("sfx_scan_error", 0.5f);
            }
            else if (BatteryDead && Battery >= cfg.batteryRebootFraction)
            {
                BatteryDead = false;
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*the flashlight coughs back to life*", 2f, null));
            }
            _pubThrottle -= dt;
            if (Mathf.Abs(Battery - before) > 1e-4f && _pubThrottle <= 0f) { _pubThrottle = 0.1f; PublishBattery(); }
        }

        private void PublishBattery() => _gameRoot.Bus.Publish(GameEvent.Of(EventIds.BatteryChanged, Battery));
    }
}
````

### `_Project/Scripts/World/Interactables.cs` (6.0 KiB)

````csharp
using System;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.World
{
    public abstract class Interactable : MonoBehaviour
    {
        protected GameRoot _gameRoot;
        protected RoomInstance Room;
        public string Prompt = "Inspect";
        public float AnchorX;
        public bool Enabled = true;
        public SpriteRenderer Art;

        public virtual void Interact() { }

        public float DistanceTo(Vector3 p) => Mathf.Abs(p.x - AnchorX);
    }

    public class TokenPickup : Interactable
    {
        public void Init(GameRoot root, RoomInstance room, float x)
        {
            _gameRoot = root; Room = room; AnchorX = x;
            Prompt = "Collect token [E]";
            Art = GetComponent<SpriteRenderer>();
        }

        public override void Interact()
        {
            if (_gameRoot == null || _gameRoot.Session == null) return;
            _gameRoot.Session.TokensCollected++;
            if (Room != null) Room.Interactables.Remove(this);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.TokenCollected, _gameRoot.Session.TokensCollected));
            _gameRoot.Audio.PlaySfx("sfx_scan_ok", 0.6f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*you pocket the token - evidence*", 2.4f, null));
            Destroy(gameObject);
        }
    }

    public class Doorway : Interactable
    {
        public RoomId Target;

        public void Init(GameRoot root, RoomInstance room, string label, RoomId target, float x)
        {
            _gameRoot = root; Room = room; Target = target; AnchorX = x;
            Prompt = target == RoomId.Security ? "Return to Security [E]" : "Enter " + label + " [E]";
            Art = GetComponent<SpriteRenderer>();
        }

        public override void Interact()
        {
            if (_gameRoot.Views.Transitioning) return;
            if (Target == RoomId.Security) _gameRoot.Views.ReturnToSecurity();
            else _gameRoot.Views.EnterRoom(Target);
        }
    }

    public class SecurityDoorInteractable : Interactable
    {
        public void Init(GameRoot root, RoomInstance room, float x)
        {
            _gameRoot = root; Room = room; AnchorX = x;
            Prompt = "Security Office [E]";
            Art = GetComponent<SpriteRenderer>();
        }

        public override void Interact()
        {
            if (_gameRoot.SecurityRoom == null) return;
            _gameRoot.Audio.PlaySfx("sfx_door", 0.7f);
            _gameRoot.OpenSecurityRoom();
        }
    }

    public class LightSwitchObject : Interactable
    {
        public void Init(GameRoot root, RoomInstance room, float x)
        {
            _gameRoot = root; Room = room; AnchorX = x;
            Prompt = "Light switch [E]";
            Art = GetComponent<SpriteRenderer>();
        }

        public override void Interact()
        {
            if (Room.Light == null) return;
            Room.Light.SetMode(Room.Light.Mode == LightMode.Off ? LightMode.Sweep : LightMode.Off);
            _gameRoot.Audio.PlaySfx("sfx_flicker");
        }
    }

    public class ClueObject : Interactable
    {
        private string _clueKey;
        public string ClueKey => _clueKey;
        private ClueDef _def;
        private bool _discovered;
        private GameObject _marker;

        public void Init(GameRoot root, RoomInstance room, string clueKey, string label, float x)
        {
            _gameRoot = root; Room = room; AnchorX = x;
            _clueKey = clueKey;
            _def = root.Session.Clues.Get(clueKey); // def content is static; tracker state lives in session
            Prompt = "Inspect " + label + " [E]";
        }

        public override void Interact()
        {
            if (_def == null)
            {
                var fresh = ClueRegistry.Build();
                foreach (var c in fresh) if (c.Key == _clueKey) { _def = c; break; }
            }
            if (_def == null) { Debug.LogWarning("[Clue] unknown clue key " + _clueKey); return; }

            var res = _gameRoot.Session.DiscoverClue(_clueKey);
            _gameRoot.Audio.PlaySfx(res.FirstTime ? "sfx_pickup" : "sfx_paper", 1f);

            var lines = new System.Collections.Generic.List<DialogueLine>
            {
                new DialogueLine { Speaker = _def.Display.ToUpperInvariant(), Text = _def.InspectText },
            };
            if (res.FirstTime)
            {
                lines.Add(new DialogueLine { Speaker = "NOTE TO SELF", Text = _def.ChainHint });
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    "FINDING " + _gameRoot.Session.Clues.FoundCount + "/10 — " + _def.Display));
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.ClueDiscovered, res));
                AddMarker();
                if (_def.IsScare && _gameRoot.Scares != null)
                    _gameRoot.Scares.Stinger("clue_" + _clueKey, 0f); // sanity already applied by DiscoverClue
            }
            if (_gameRoot.Dialogue != null) _gameRoot.Dialogue.Play(lines, null);
        }

        private void AddMarker()
        {
            if (_discovered || _marker != null) return;
            _discovered = true;
            _marker = new GameObject("found_marker");
            _marker.transform.SetParent(Room.Root.transform, false);
            _marker.transform.localPosition = new Vector3(AnchorX, -1.35f, 0);
            var sr = _marker.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralArt.Get("solid_white");
            sr.color = new Color(0.75f, 0.14f, 0.18f, 0.9f); // restrained red = interacted/interactive
            sr.sortingOrder = 76;
            _marker.transform.localScale = new Vector3(0.14f, 0.14f, 1f);
        }

        public void SyncFromSession()
        {
            bool found = false;
            foreach (var k in _gameRoot.Session.Clues.FoundKeys) if (k == _clueKey) { found = true; break; }
            if (found && !_discovered) AddMarker();
        }
    }
}
````

### `_Project/Scripts/World/LightController.cs` (3.2 KiB)

````csharp
using System.Collections;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // Per-room light switch state and flicker. Created by ViewManager.
    public class LightController : MonoBehaviour
    {
        public LightMode Mode { get; private set; } = LightMode.Sweep;
        private GameRoot _gameRoot;
        private RoomInstance _room;
        private SpriteRenderer _darkness;
        private float _flickerTimer;
        private Coroutine _flickerRoutine;

        public void Init(GameRoot root, RoomInstance room, SpriteRenderer darkness, LightMode startMode)
        {
            _gameRoot = root; _room = room; _darkness = darkness;
            _flickerTimer = Random.Range(6f, 14f);
            ApplyVisual();
            Mode = startMode;
            ApplyVisual();
        }

        public void SetMode(LightMode m)
        {
            if (Mode == m) return;
            if (m != LightMode.Focus) _lastNonFocus = m;
            Mode = m;
            ApplyVisual();
            _gameRoot?.Bus.Publish(GameEvent.Of(EventIds.LightModeChanged, m));
            if (_gameRoot != null && _gameRoot.HUD != null)
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    m == LightMode.Off ? "Lights off. Something prefers it that way." :
                    m == LightMode.Focus ? "Flashlight on." : "Lights on."));
        }

        private LightMode _lastNonFocus = LightMode.Sweep;

        public void ToggleFocus(bool on)
        {
            SetMode(on ? LightMode.Focus : _lastNonFocus);
        }

        private void ApplyVisual()
        {
            if (_darkness == null) return;
            float alpha = Mode == LightMode.Off ? 0.62f : Mode == LightMode.Focus ? 0.52f : 0.12f;
            var c = _darkness.color; c.a = alpha; _darkness.color = c;
        }

        private void Update()
        {
            if (_gameRoot == null || _gameRoot.Paused || _room == null) return;
            if (Mode != LightMode.Sweep) return;
            _flickerTimer -= Time.deltaTime;
            if (_flickerTimer <= 0f)
            {
                _flickerTimer = Random.Range(9f, 20f);
                if (_flickerRoutine == null && _darkness != null)
                    _flickerRoutine = StartCoroutine(Flicker());
            }
        }

        private IEnumerator Flicker()
        {
            bool photoSafe = _gameRoot.Settings != null && _gameRoot.Settings.PhotosensitivitySafe;
            int pulses = photoSafe ? 2 : 4;
            float baseAlpha = 0.12f;
            _gameRoot.Audio.PlaySfx("sfx_flicker", 0.5f);
            for (int i = 0; i < pulses; i++)
            {
                // gentle dim (never a hard strobe; photosensitivity guard caps contrast)
                var c = _darkness.color; c.a = photoSafe ? 0.30f : 0.55f; _darkness.color = c;
                yield return new WaitForSeconds(0.07f);
                c = _darkness.color; c.a = baseAlpha; _darkness.color = c;
                yield return new WaitForSeconds(Random.Range(0.05f, 0.12f));
            }
            var cf = _darkness.color; cf.a = baseAlpha; _darkness.color = cf;
            _flickerRoutine = null;
        }
    }
}
````

### `_Project/Scripts/World/PlayerController.cs` (4.8 KiB)

````csharp
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.World
{
    // Walk/run/interact for the custodian. Created by ViewManager.
    public class PlayerController : MonoBehaviour
    {
        private GameRoot _gameRoot;
        private SpriteRenderer _sr;
        private RoomInstance _room;
        private float _faceDir = 1f;
        private float _stepTimer, _animTimer;
        private int _animFrame;
        private string _lastPrompt = "\0";
        private const float FloorY = -2.4f;

        public void Init(GameRoot root, SpriteRenderer sr)
        {
            _gameRoot = root;
            _sr = sr;
        }

        public void PlaceInRoom(RoomInstance room, Vector3 spawn)
        {
            _room = room;
            transform.localPosition = new Vector3(spawn.x, FloorY + 1.7f, 0);
            if (room.Light != null) room.Light.ToggleFocus(false);
            if (_gameRoot.Views != null && _gameRoot.Views.Flashlight != null) _gameRoot.Views.Flashlight.SyncRoom();
        }

        private void Update()
        {
            if (_gameRoot == null || _room == null || _gameRoot.Views == null || _gameRoot.Views.Transitioning) return;
            if (!gameObject.activeInHierarchy) return;

            bool dialogueOpen = _gameRoot.Dialogue != null && _gameRoot.Dialogue.IsActive;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_gameRoot.Paused) _gameRoot.TogglePause(false);
                else if (!dialogueOpen) _gameRoot.TogglePause(true);
                return;
            }
            if (_gameRoot.Paused || dialogueOpen) return;
            if (_gameRoot.State != AppViewState.Exploration) return; // overlays (map/security room) freeze movement

            if (Input.GetKeyDown(KeyCode.M)) _gameRoot.OpenMap();

            float h = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
            bool running = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float speed = running ? _gameRoot.Config.runSpeed : _gameRoot.Config.walkSpeed;

            var p = transform.localPosition;
            if (Mathf.Abs(h) > 0.01f)
            {
                _faceDir = h > 0 ? 1f : -1f;
                p.x += h * speed * Time.deltaTime;
                p.x = Mathf.Clamp(p.x, -_room.HalfWidth + 0.6f, _room.HalfWidth - 0.6f);
                StepAndAnimate(running, Time.deltaTime);
            }
            else
            {
                SetFrame(0);
                _stepTimer = 0f;
            }
            transform.localPosition = p;
            var s = transform.localScale; s.x = _faceDir; transform.localScale = s;
            UpdatePrompt();
        }

        private void StepAndAnimate(bool running, float dt)
        {
            _stepTimer -= dt;
            if (_stepTimer <= 0f)
            {
                _stepTimer = running ? 0.30f : 0.45f;
                _gameRoot.Audio.PlaySfx("sfx_footstep", 0.30f);
            }
            _animTimer -= dt;
            if (_animTimer <= 0f)
            {
                _animTimer = running ? 0.14f : 0.22f;
                SetFrame(_animFrame == 0 ? 1 : 0);
            }
        }

        private void SetFrame(int f)
        {
            if (_animFrame == f && _sr != null && _sr.sprite != null) return;
            _animFrame = f;
            if (_sr != null) _sr.sprite = ProceduralArt.Get("custodian_" + f);
        }


        // The beam lives in FlashlightRig; this only answers "is that point lit?".
        public bool ConeCovers(Vector3 worldPoint)
        {
            if (_gameRoot == null || _gameRoot.Views == null || _gameRoot.Views.Flashlight == null) return false;
            return _gameRoot.Views.Flashlight.ConeCovers(worldPoint);
        }

        private void UpdatePrompt()
        {
            string prompt = "";
            if (_room != null)
            {
                Interactable best = null;
                float bestD = _gameRoot.Config.interactRange;
                foreach (var it in _room.Interactables)
                {
                    if (it == null || !it.Enabled) continue;
                    float d = it.DistanceTo(transform.localPosition);
                    if (d < bestD) { bestD = d; best = it; }
                }
                if (best != null)
                {
                    prompt = best.Prompt;
                    if (Input.GetKeyDown(KeyCode.E)) best.Interact();
                }
            }
            if (prompt != _lastPrompt)
            {
                _lastPrompt = prompt;
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.InteractionPrompt, prompt));
            }
        }
    }
}
````

### `_Project/Scripts/World/RoomBuilder.cs` (8.7 KiB)

````csharp
using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // Owns every per-room sprite, door, switch and clue for the patrol scene.
    // Called once by ViewManager.Init; holds no state of its own beyond the root it builds into.
    public class RoomBuilder
    {
        private readonly GameRoot _root;
        private readonly GameObject _worldRoot;
        private readonly Dictionary<RoomId, RoomInstance> _rooms;

        public RoomBuilder(GameRoot root, GameObject worldRoot, Dictionary<RoomId, RoomInstance> rooms)
        {
            _root = root;
            _worldRoot = worldRoot;
            _rooms = rooms;
        }

        public static GameObject SpriteGO(Transform parent, string name, Sprite sprite,
            Vector3 pos, int order, float alpha = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = new Color(1f, 1f, 1f, alpha);
            return go;
        }

        public RoomInstance BuildRoom(RoomId id)
        {
            var room = new RoomInstance { Id = id };
            var root = new GameObject("Room_" + id);
            root.transform.SetParent(_worldRoot.transform, false);
            room.Root = root;

            float floorY = -2.4f; // floor line across all rooms (bg art shares it)
            switch (id)
            {
                case RoomId.Hallway:
                {
                    room.DisplayName = "MAIN HALLWAY";
                    room.HalfWidth = 11.4f;
                    SpriteGO(root.transform, "bg", ProceduralArt.Get("hallway_bg"), Vector3.zero, 0);
                    AddDoor(room, "Security Door", -10.6f, floorY, RoomId.Security, "prop_door_security");
                    AddDoor(room, "Classroom 1", -4.0f, floorY, RoomId.Classroom1, "prop_door");
                    AddDoor(room, "Classroom 2", 4.6f, floorY, RoomId.Classroom2, "prop_door");
                    SpriteGO(root.transform, "server_door_art", ProceduralArt.Get("prop_server_door"),
                        new Vector3(10.4f, floorY + 3f, 0), 10);
                    AddLightSwitch(room, -9.2f, floorY);
                    AddClue(room, "light", -7.0f, floorY, "Flickering Light (switch panel)");
                    AddClue(room, "door", 10.0f, floorY, "Sealed Door");
                    AddClue(room, "board", 0.6f, floorY, "Notice Board");
                    AddClue(room, "photo", 7.2f, floorY, "Old School Photo");
                    AddClue(room, "clock", 2.6f, floorY, "Wall Clock");
                    SpriteGO(root.transform, "lockers", ProceduralArt.Get("prop_lockers"), new Vector3(-1.4f, floorY + 2.5f, 0), 8);
                    SpriteGO(root.transform, "lockers2", ProceduralArt.Get("prop_lockers"), new Vector3(7.8f, floorY + 2.5f, 0), 8);
                    room.PlayerSpawn = new Vector3(-9.4f, 0, 0);
                    room.AnomalySpawnPoints.Add(new Vector3(9.5f, 0, 0));
                    room.AnomalySpawnPoints.Add(new Vector3(-2f, 0, 0));
                    room.AnomalySpawnPoints.Add(new Vector3(5.5f, 0, 0));
                    break;
                }
                case RoomId.Classroom1:
                {
                    room.DisplayName = "CLASSROOM 1";
                    room.HalfWidth = 8.2f;
                    SpriteGO(root.transform, "bg", ProceduralArt.Get("classroom1_bg"), Vector3.zero, 0);
                    AddDoor(room, "Hallway", 4.9f, floorY, RoomId.Hallway, "prop_door");
                    AddLightSwitch(room, 6.4f, floorY);
                    AddClue(room, "chalkboard", -6.2f, floorY, "Chalkboard");
                    AddClue(room, "desk", -7.6f, floorY, "Front Desk");
                    AddClue(room, "lostid", -5.0f, floorY, "Lost-and-Found Shelf");
                    AddSecurityDoor(room, -1.6f, floorY);
                    SpriteGO(root.transform, "deskprop", ProceduralArt.Get("prop_desk"), new Vector3(-7.6f, floorY + 1f, 0), 9);
                    room.PlayerSpawn = new Vector3(4.0f, 0, 0);
                    room.AnomalySpawnPoints.Add(new Vector3(-6.5f, 0, 0));
                    room.AnomalySpawnPoints.Add(new Vector3(1.5f, 0, 0));
                    break;
                }
                default: // Classroom2 — the darker room (server room adjoins it)
                {
                    room.DisplayName = "CLASSROOM 2";
                    room.HalfWidth = 8.2f;
                    SpriteGO(root.transform, "bg", ProceduralArt.Get("classroom2_bg"), Vector3.zero, 0);
                    AddDoor(room, "Hallway", 4.9f, floorY, RoomId.Hallway, "prop_door");
                    AddLightSwitch(room, 6.4f, floorY);
                    AddClue(room, "window", -6.6f, floorY, "Classroom Window");
                    AddClue(room, "cabinet", 7.4f, floorY, "Filing Cabinet");
                    SpriteGO(root.transform, "cabinetprop", ProceduralArt.Get("prop_cabinet"), new Vector3(7.6f, floorY + 2.1f, 0), 9);
                    room.PlayerSpawn = new Vector3(4.0f, 0, 0);
                    room.AnomalySpawnPoints.Add(new Vector3(-5.5f, 0, 0));
                    room.AnomalySpawnPoints.Add(new Vector3(2.5f, 0, 0));
                    break;
                }
            }

            var dark = SpriteGO(root.transform, "darkness", ProceduralArt.Get("solid_white"),
                new Vector3(0, 0, 0), 70);
            dark.transform.localScale = new Vector3(room.HalfWidth * 2f + 4f, 11f, 1f);
            var lightCtl = root.AddComponent<LightController>();
            lightCtl.Init(_root, room, dark.GetComponent<SpriteRenderer>(),
                id == RoomId.Classroom2 ? LightMode.Off : LightMode.Sweep);
            room.Light = lightCtl;
            room.Darkness = dark.GetComponent<SpriteRenderer>();
            room.Darkness.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

            _rooms[id] = room;
            root.SetActive(false);
            return room;
        }

        private void AddDoor(RoomInstance room, string label, float x, float floorY, RoomId target, string spriteKey)
        {
            var go = SpriteGO(room.Root.transform, "door_" + target, ProceduralArt.Get(spriteKey),
                new Vector3(x, floorY + 3f, 0), 10);
            var d = go.AddComponent<Doorway>();
            d.Init(_root, room, label, target, x);
            room.Interactables.Add(d);
        }

        private void AddSecurityDoor(RoomInstance room, float x, float floorY)
        {
            var go = SpriteGO(room.Root.transform, "door_security_office", ProceduralArt.Get("prop_door_security"),
                new Vector3(x, floorY + 3f, 0), 10);
            var d = go.AddComponent<SecurityDoorInteractable>();
            d.Init(_root, room, x);
            room.Interactables.Add(d);
        }

        private void AddLightSwitch(RoomInstance room, float x, float floorY)
        {
            var go = new GameObject("lightswitch");
            go.transform.SetParent(room.Root.transform, false);
            go.transform.localPosition = new Vector3(x, floorY + 2.1f, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralArt.Get("solid_white");
            sr.color = new Color(0.8f, 0.8f, 0.78f);
            sr.sortingOrder = 75;
            go.transform.localScale = new Vector3(0.16f, 0.26f, 1f);
            var sw = go.AddComponent<LightSwitchObject>();
            sw.Init(_root, room, x);
            room.Interactables.Add(sw);
        }

        private void AddClue(RoomInstance room, string clueKey, float x, float floorY, string label)
        {
            var go = new GameObject("clue_" + clueKey);
            go.transform.SetParent(room.Root.transform, false);
            go.transform.localPosition = new Vector3(x, floorY + 1.4f, 0);
            var co = go.AddComponent<ClueObject>();
            co.Init(_root, room, clueKey, label, x);
            room.Interactables.Add(co);
        }
    }
    internal static class UIFactoryLocal
    {
        public static RectTransform NewRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static UnityEngine.UI.Image FullImage(GameObject go, Color c)
        {
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = c;
            NewRect(go);
            return img;
        }
    }
}
````

### `_Project/Scripts/World/RoomInstance.cs` (751 B)

````csharp
using System;
using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hollowbrook.World
{
    // One built room: root, light, interactables, spawn points.
    public class RoomInstance
    {
        public RoomId Id;
        public GameObject Root;
        public float HalfWidth;
        public LightController Light;
        public SpriteRenderer Darkness;
        public List<Interactable> Interactables = new List<Interactable>();
        public List<Vector3> AnomalySpawnPoints = new List<Vector3>();
        public Vector3 PlayerSpawn;
        public string DisplayName;
    }
}
````

### `_Project/Scripts/World/RoomSpawner.cs` (4.4 KiB)

````csharp
using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // Decides when a roaming anomaly appears and where it goes when it leaves.
    // Keeps the live-anomaly slot, the spawn cooldown and the breach counter.
    public class RoomSpawner
    {
        private readonly GameRoot _root;
        private readonly ViewManager _views;
        private GameRng _rng;
        private WanderingAnomaly _liveAnomaly;
        private float _spawnCooldown = 6f;
        private int _anomaliesInside;

        public RoomSpawner(GameRoot root, ViewManager views, GameRng rng)
        {
            _root = root;
            _views = views;
            _rng = rng;
        }

        public void NoteBreach() { _anomaliesInside++; }

        public void CoolDown(float seconds) { _spawnCooldown = seconds; }

        // Leaving for the desk discards the live anomaly without resolving it: it is still outstanding.
        public void DiscardLive()
        {
            if (_liveAnomaly != null) { Object.Destroy(_liveAnomaly.gameObject); _liveAnomaly = null; }
        }

        public void UpdateSpawner(float dt)
        {
            int shift = _root.Session.Mode == GameMode.Normal ? _root.Session.CurrentShift : 3;
            if (_root.Session.Mode == GameMode.Normal && shift < 2) return; // anomalies wander from shift 2
            if (_liveAnomaly != null) return;
            if (_root.Dialogue != null && _root.Dialogue.IsActive) { _spawnCooldown = Mathf.Max(_spawnCooldown, 2f); return; }

            _spawnCooldown -= dt;
            if (_spawnCooldown > 0f) return;

            float chance = _root.Config.anomalySpawnChancePerSecond * (0.6f + 0.2f * shift) *
                           (1f + 0.35f * _anomaliesInside + 0.3f * (int)_root.Session.Breaches) *
                           (_views.Current.Light != null && _views.Current.Light.Mode == LightMode.Off ? 1.6f : 1f);
            _spawnCooldown = 4f; // re-roll interval; chance is per-second so scale by 4
            if (UnityEngine.Random.value > chance * 4f) return;

            SpawnAnomaly(false);
        }

        public void SpawnAnomaly(bool hallucination)
        {
            if (_views.Current == null) return;
            if (!hallucination && _liveAnomaly != null) return;
            var kinds = new[] { AnomalyBehaviorKind.Blink, AnomalyBehaviorKind.Slide,
                                AnomalyBehaviorKind.Sprint, AnomalyBehaviorKind.CoverHug };
            if (_rng == null) _rng = new GameRng(_root.Session.Seed + 31 + _root.Session.CurrentShift);
            var kind = hallucination
                ? (_rng.Chance(0.5f) ? AnomalyBehaviorKind.Slide : AnomalyBehaviorKind.Blink)
                : _root.Session.Pool.PeekKind(_rng);   // shared pool: uncaught == outstanding
            if (hallucination && kind == AnomalyBehaviorKind.Blink) kind = AnomalyBehaviorKind.Slide; // phantoms never grab
            var spawn = _views.Current.AnomalySpawnPoints.Count > 0
                ? _views.Current.AnomalySpawnPoints[_rng.NextInt(0, _views.Current.AnomalySpawnPoints.Count)]
                : new Vector3(_views.Current.HalfWidth - 1.5f, 0, 0);
            if (Mathf.Abs(spawn.x - _views.Player.transform.position.x) < 4f) spawn.x = -spawn.x;

            var go = new GameObject(hallucination ? "Phantom" : "Anomaly_" + kind);
            go.transform.SetParent(_views.Current.Root.transform, false);
            var an = go.AddComponent<WanderingAnomaly>();
            an.Init(_root, _views.Current, kind, spawn, hallucination);
            if (hallucination) return;
            _liveAnomaly = an;
            _root.Bus.Publish(GameEvent.Of(EventIds.AnomalySighted, kind));
        }

        public void ScheduleReturn(AnomalyBehaviorKind kind)
        {
            _views.StartCoroutine(ReturnRoutine(kind));
        }

        private System.Collections.IEnumerator ReturnRoutine(AnomalyBehaviorKind kind)
        {
            yield return new WaitForSeconds(EncounterTuning.Default.RecoverSeconds);
            if (_root != null && !_root.Paused && _views.Current != null) SpawnAnomaly(false);
        }

        public void OnAnomalyGone(WanderingAnomaly a)
        {
            if (_liveAnomaly == a) _liveAnomaly = null;
            if (!a.IsHallucination && (a.WasRevealed || a.WasCaught))
                _root.Session.Pool.Resolve(a.Kind);
        }
    }
}
````

### `_Project/Scripts/World/SecurityRoomView.cs` (11.9 KiB)

````csharp
using System.Collections.Generic;
using Hollowbrook.Core;
using Hollowbrook.UI;
using Hollowbrook.Bootstrap;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.World
{
    // The door-facing late-student office. Created by SchoolSceneBootstrap.
    public class SecurityRoomView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private Image _doorArt, _portrait;
        private Image _idCard;
        private Text _doorState, _idFields, _feedback, _progress;
        private Button _allowBtn, _rejectBtn, _returnBtn;
        private Button[] _checkBtns = new Button[3];
        private GameObject _docPanel;

        private enum Phase { Idle, Student, RoomCheck, Done }
        private Phase _phase = Phase.Idle;
        private readonly List<VisitorData> _queue = new List<VisitorData>();
        private VisitorData _current;
        public VisitorData CurrentStudent => _current;
        private int _correct;
        private bool[] _checked = new bool[3];
        private int _anomalySpot;
        private GameRng _rng;
        private Text _ruleLabel;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            var bg = UIFactory.Img(transform, "bg", ProceduralArt.Get("security_bg"));
            UIFactory.AnchorsFull(bg.rectTransform);
            bg.raycastTarget = true;

            var title = UIFactory.Label(transform, "title", "SECURITY OFFICE — LATE ARRIVALS", 24,
                UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(800, 36), new Vector2(0, -40));

            _doorState = UIFactory.Label(transform, "doorstate", "The door is shut. The night class is in session.",
                16, UIFactory.InkDim, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_doorState.rectTransform, new Vector2(0.5f, 1f), new Vector2(800, 26), new Vector2(0, -84));

            _doorArt = UIFactory.Img(transform, "door", ProceduralArt.Get("prop_door_security"));
            UIFactory.Anchor(_doorArt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(220, 400), new Vector2(0, 60));

            _portrait = UIFactory.Img(transform, "face", null);
            UIFactory.Anchor(_portrait.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(150, 186), new Vector2(0, 150));
            _portrait.gameObject.SetActive(false);

            _docPanel = new GameObject("docpanel", typeof(RectTransform));
            _docPanel.transform.SetParent(transform, false);
            var docRt = (RectTransform)_docPanel.transform;
            UIFactory.Anchor(docRt, new Vector2(1f, 0.5f), new Vector2(360, 250), new Vector2(-240, -40));
            _idCard = UIFactory.Img(docRt, "card", ProceduralArt.Get("id_card_frame"));
            UIFactory.AnchorsFull(_idCard.rectTransform);
            _idFields = UIFactory.Label(docRt, "fields", "", 15, UIFactory.PaperInk);
            var frt = _idFields.rectTransform;
            frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(1, 1);
            frt.offsetMin = new Vector2(24, 20); frt.offsetMax = new Vector2(-24, -46);
            _ruleLabel = UIFactory.Label(docRt, "rule", PatrolRules.OfficeRule.RuleText,
                13, new Color(0.45f, 0.12f, 0.14f), TextAnchor.MiddleCenter);
            UIFactory.Anchor(_ruleLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(340, 40), new Vector2(0, -18));
            _docPanel.SetActive(false);

            _allowBtn = UIFactory.StandardButton(transform, "allow", "✓ ALLOW", () => Decide(Verdict.Allow), 150, 52);
            UIFactory.Anchor(_allowBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(150, 52), new Vector2(-330, 60));
            _rejectBtn = UIFactory.StandardButton(transform, "reject", "✗ REJECT", () => Decide(Verdict.Reject), 150, 52, true);
            UIFactory.Anchor(_rejectBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(150, 52), new Vector2(-160, 60));

            _feedback = UIFactory.Label(transform, "feedback", "", 16, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_feedback.rectTransform, new Vector2(0.5f, 0f), new Vector2(900, 60), new Vector2(0, 120));
            _progress = UIFactory.Label(transform, "progress", "", 14, UIFactory.InkDim, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_progress.rectTransform, new Vector2(0.5f, 0f), new Vector2(900, 24), new Vector2(0, 96));

            string[] spots = { "CHECK THE SHELVES", "CHECK THE WINDOW", "CHECK UNDER THE DESKS" };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var b = UIFactory.StandardButton(transform, "check" + i, spots[i], () => CheckSpot(idx), 260, 44);
                UIFactory.Anchor(b.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(260, 44), new Vector2(180, 120 - i * 56));
                _checkBtns[i] = b;
            }

            _returnBtn = UIFactory.StandardButton(transform, "ret", "RETURN TO THE CHECKPOINT ▸", () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_door", 0.7f);
                _gameRoot.ReturnToCheckpoint();
            }, 320, 48);
            UIFactory.Anchor(_returnBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(320, 48), new Vector2(0, 36));

            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            StartSequence();
        }

        public void Hide() => gameObject.SetActive(false);

        private void StartSequence()
        {
            _phase = Phase.Student;
            _correct = 0;
            _queue.Clear();
            _rng = new GameRng(_gameRoot.Session.Seed + 977 + _gameRoot.Session.Processed * 131 + _gameRoot.Session.CurrentShift * 17);
            _queue.AddRange(PatrolRules.BuildLateQueue(_gameRoot.Roster, _gameRoot.CurrentShiftDef, _gameRoot.Session.Pool, _rng));
            _anomalySpot = _rng.NextInt(0, 3);
            for (int i = 0; i < 3; i++) { _checked[i] = false; UIFactory.SetButtonEnabled(_checkBtns[i], false); }
            UIFactory.SetButtonEnabled(_returnBtn, false);
            _feedback.text = "";
            NextStudent();
        }

        private void NextStudent()
        {
            if (_queue.Count == 0) { BeginRoomCheck(); return; }
            _current = _queue[0];
            _queue.RemoveAt(0);

            _gameRoot.Audio.PlaySfx("sfx_knock", 0.8f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*three quick knocks on the office door*", 2.4f, null));
            _doorState.text = "A student slips in out of the dark. They hold out their ID.";
            _portrait.sprite = ProceduralArt.Portrait("sec", _current.WindowPortrait, 150, 186);
            _portrait.gameObject.SetActive(true);
            _docPanel.SetActive(true);

            bool expired = IdRules.IsExpired(_current, Mathf.Max(1, _gameRoot.Session.CurrentShift));
            var sb = new System.Text.StringBuilder();
            sb.Append("NAME:  ").Append(_current.ClaimedName).Append('\n');
            sb.Append("ID NO: ").Append(_current.IdNumber).Append('\n');
            sb.Append("YEAR:  ").Append(Content.YearNames[_current.IdYearLevel - 1]).Append('\n');
            sb.Append("EXPIRES: ").Append(_current.ExpirationDisplay).Append(expired ? "   (EXPIRED)" : "").Append('\n');
            sb.Append("ON TONIGHT'S LATE LIST: ").Append(_current.OnLateList ? "YES" : "NO — not on tonight's list");
            _idFields.text = sb.ToString();

            _progress.text = ProgressLine(_queue.Count + 1);
            UIFactory.SetButtonEnabled(_allowBtn, true);
            UIFactory.SetButtonEnabled(_rejectBtn, true);
        }

        private void Decide(Verdict v)
        {
            if (_phase != Phase.Student || _current == null) return;
            var outcome = InspectionRules.Apply(_current, v, PatrolRules.OfficeRule);
            _gameRoot.Session.RecordVerdict(outcome);
            bool correct = outcome.Correct;
            if (correct) _correct++;
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.VerdictStamped, v));
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.VisitorResolved, outcome));
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, _gameRoot.Session.Sanity));
            _gameRoot.Audio.PlaySfx(v == Verdict.Allow ? "sfx_stamp_allow" : "sfx_stamp_reject", 0.9f);

            var fb = new System.Text.StringBuilder();
            fb.Append(correct ? "CORRECT. " : "MISTAKE. ");
            if (_current.IsAnomaly)
                fb.Append("Something about them was wrong — the way they stood, the way the photo didn't sit.");
            else if (outcome.Expected == Verdict.Reject)
                fb.Append(!_current.OnLateList
                    ? "They were not on tonight's late list."
                    : "The documents broke the office rule.");
            else fb.Append("A real student, late for night class. They needed to get in.");
            _feedback.text = fb.ToString();
            _progress.text = ProgressLine(_queue.Count);

            UIFactory.SetButtonEnabled(_allowBtn, false);
            UIFactory.SetButtonEnabled(_rejectBtn, false);
            _portrait.gameObject.SetActive(false);
            _docPanel.SetActive(false);
            _doorState.text = "The door closes. Footsteps fade down the hall.";
            _current = null;
            StartCoroutine(NextAfter(1.6f));
        }

        private string ProgressLine(int remaining)
        {
            return "LATE ARRIVALS REMAINING: " + remaining +
                   "   ·   CORRECT CALLS: " + _correct + "/" + PatrolRules.LateArrivals +
                   "   ·   BREACHES: " + _gameRoot.Session.Breaches +
                   "   ·   COMPLAINTS: " + _gameRoot.Session.FalseRejections;
        }

        private System.Collections.IEnumerator NextAfter(float s)
        {
            yield return new WaitForSecondsRealtime(s);
            if (gameObject.activeInHierarchy) NextStudent();
        }

        private void BeginRoomCheck()
        {
            _phase = Phase.RoomCheck;
            _doorState.text = "Quiet now. Protocol says check the room before you lock up.";
            _feedback.text = "";
            for (int i = 0; i < 3; i++) UIFactory.SetButtonEnabled(_checkBtns[i], true);
        }

        private void CheckSpot(int idx)
        {
            if (_phase != Phase.RoomCheck || _checked[idx]) return;
            _checked[idx] = true;
            UIFactory.SetButtonEnabled(_checkBtns[idx], false);
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.5f);

            if (idx == _anomalySpot)
            {
                _gameRoot.Session.ApplySanity(-8f);
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, _gameRoot.Session.Sanity));
                if (_gameRoot.Scares != null) _gameRoot.Scares.Stinger("security_room_anomaly", 0f);
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*something was crouched here — and it is gone now*", 3f, null));
                _feedback.text = "You find a hollow space in the dust. Something was crouched here minutes ago. It is not here now.";
            }
            else
            {
                string[] flavor =
                {
                    "Shelves: attendance binders, all signed in the same handwriting.",
                    "Window: the yard is empty. The streetlight hums. Nothing moves.",
                };
                int fi = idx == 0 ? 0 : 1;
                _feedback.text = flavor[fi];
            }

            bool all = _checked[0] && _checked[1] && _checked[2];
            if (all)
            {
                _phase = Phase.Done;
                _doorState.text = "Room checked. The office holds its breath.";
                _feedback.text += "   — Check complete (" + _correct + "/3 correct calls).";
                UIFactory.SetButtonEnabled(_returnBtn, true);
            }
        }
    }
}
````

### `_Project/Scripts/World/ViewManager.cs` (7.7 KiB)

````csharp
using System;
using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{


    // Rooms, doors, transitions, anomaly spawning, flashlight rig. Created by SchoolSceneBootstrap.
    public class ViewManager : MonoBehaviour
    {
        private GameRoot _gameRoot;
        private Camera _cam;
        private GameObject _worldRoot;
        private readonly Dictionary<RoomId, RoomInstance> _rooms = new Dictionary<RoomId, RoomInstance>();
        public RoomInstance Current { get; private set; }
        public PlayerController Player { get; private set; }
        public bool Transitioning { get; private set; }
        private RoomBuilder _builder;
        private RoomSpawner _spawner;
        public FlashlightRig Flashlight { get; private set; }
        private UnityEngine.UI.Image _fade;

        public void Init(GameRoot root, Camera cam, GameObject worldRoot)
        {
            _gameRoot = root;
            _cam = cam;
            _worldRoot = worldRoot;

            // dedicated fade canvas (between FX and UI layers; parented to the scene root so
            var fadeGo = new GameObject("TransitionFade", typeof(RectTransform));
            var fadeCanvas = fadeGo.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 8;
            fadeGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var fadeRt = UIFactoryLocal.NewRect(fadeGo);
            _fade = UIFactoryLocal.FullImage(fadeGo, Color.black);
            _fade.raycastTarget = false;
            SetFade(0f);

            BuildPlayer();
            _builder = new RoomBuilder(root, worldRoot, _rooms);
            _spawner = new RoomSpawner(root, this, null);
            _rooms[RoomId.Hallway] = _builder.BuildRoom(RoomId.Hallway);
            _rooms[RoomId.Classroom1] = _builder.BuildRoom(RoomId.Classroom1);
            _rooms[RoomId.Classroom2] = _builder.BuildRoom(RoomId.Classroom2);
        }

        private void BuildPlayer()
        {
            var go = new GameObject("Player");
            go.transform.SetParent(_worldRoot.transform, false);
            Player = go.AddComponent<PlayerController>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralArt.Get("custodian_0");
            sr.sortingOrder = 60; // above darkness overlay, lit by design (the custodian you play)
            Player.Init(_gameRoot, sr);
        }

        public void ShowWorld()
        {
            if (Flashlight == null && _cam != null)
            {
                var rigGo = new GameObject("FlashlightRig");
                rigGo.transform.SetParent(transform, false);
                Flashlight = rigGo.AddComponent<FlashlightRig>();
                var coneGo = RoomBuilder.SpriteGO(rigGo.transform, "beam", ProceduralArt.Get("light_cone"),
                    new Vector3(0, 0, -0.5f), 71);
                var cone = coneGo.GetComponent<SpriteRenderer>();
                cone.color = new Color(1f, 0.96f, 0.85f, 0.30f);
                Flashlight.Init(_gameRoot, _cam, cone);
            }
            _worldRoot.SetActive(true);
            if (Current == null) EnterRoomImmediate(RoomId.Hallway, null);
        }

        public void HideWorld()
        {
            SetFade(0f);
            _worldRoot.SetActive(false);
            _spawner.DiscardLive();
        }

        public void EnterRoom(RoomId id)
        {
            if (id == RoomId.Security) { _gameRoot.ReturnToCheckpoint(); return; }
            if (Transitioning) return;
            StartCoroutine(TransitionRoutine(id));
        }

        private IEnumerator TransitionRoutine(RoomId id)
        {
            Transitioning = true;
            _gameRoot.Audio.PlaySfx("sfx_door");
            yield return FadeTo(1f, 0.28f);
            EnterRoomImmediate(id, Current);
            yield return FadeTo(0f, 0.34f);
            Transitioning = false;
        }

        private void EnterRoomImmediate(RoomId id, RoomInstance from)
        {
            if (!_rooms.TryGetValue(id, out var room) || room.Root == null)
            {
                Debug.LogWarning("[Views] room '" + id + "' missing — rebuilding.");
                room = _builder.BuildRoom(id);
            }
            if (room.Root == null)
            {
                Debug.LogError("[Views] room '" + id + "' could not be built — returning to security.");
                _gameRoot.ReturnToCheckpoint();
                return;
            }

            if (Current != null) Current.Root.SetActive(false);
            Current = room;
            room.Root.SetActive(true);
            _worldRoot.SetActive(true);

            Vector3 spawn = room.PlayerSpawn;
            if (from != null)
            {
                foreach (var it in room.Interactables)
                {
                    var d = it as Doorway;
                    if (d != null && d.Target == from.Id) { spawn = new Vector3(d.AnchorX - 1.1f, 0, 0); break; }
                }
            }
            Player.PlaceInRoom(room, spawn);
            _gameRoot.Session.MarkRoomInvestigated(id);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.RoomEntered, id));
            _gameRoot.Audio.PlayMusic(GameRoot.MusicForRoom(id));
            _spawner.CoolDown(5f);
        }

        public void ReturnToSecurity()
        {
            if (Transitioning) return;
            StartCoroutine(ReturnRoutine());
        }

        private IEnumerator ReturnRoutine()
        {
            Transitioning = true;
            yield return FadeTo(1f, 0.28f);
            _spawner.DiscardLive();
            Current = null;
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.RoomExited, RoomId.Hallway));
            Transitioning = false;
            _gameRoot.ReturnToCheckpoint();
            yield return FadeTo(0f, 0.01f);
        }

        public void NoteBreach() => _spawner.NoteBreach();

        public void SpawnAnomaly(bool hallucination) => _spawner.SpawnAnomaly(hallucination);

        public void ScheduleReturn(AnomalyBehaviorKind kind) => _spawner.ScheduleReturn(kind);

        public void OnAnomalyGone(WanderingAnomaly a) => _spawner.OnAnomalyGone(a);

        private IEnumerator FadeTo(float target, float seconds)
        {
            float from = _fade.color.a;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                SetFade(Mathf.Lerp(from, target, Mathf.Clamp01(t / seconds)));
                yield return null;
            }
            SetFade(target);
        }

        private void SetFade(float a)
        {
            var c = _fade.color; c.a = a; _fade.color = c;
            _fade.raycastTarget = a > 0.9f;
        }

        private void LateUpdate()
        {
            if (!_worldRoot.activeSelf || Current == null || Player == null) return;
            float screenHalf = _cam.orthographicSize * _cam.aspect;
            float limit = Mathf.Max(0f, Current.HalfWidth - screenHalf + 0.4f);
            float x = Mathf.Clamp(Player.transform.position.x, -limit, limit);
            _cam.transform.position = new Vector3(x, 0, -10);
        }

        private void Update()
        {
            if (_gameRoot == null || Current == null || !_worldRoot.activeSelf || _gameRoot.Paused) return;
            _spawner.UpdateSpawner(Time.deltaTime);
        }

        public bool IsIlluminated(Vector3 worldPoint)
        {
            if (Player == null) return false;
            if (Current != null && Current.Light != null && Current.Light.Mode == LightMode.Focus)
                return Player.ConeCovers(worldPoint);
            return false;
        }
    }

}
````
