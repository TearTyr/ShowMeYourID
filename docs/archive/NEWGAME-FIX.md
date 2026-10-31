# NEW GAME / CONTINUE - real-editor failure class + fix (fix/new-game-options)

**Evidence gathered (repo copy):** scene YAML `m_Script` GUIDs == the three `*SceneBootstrap.cs.meta`
GUIDs (0b1b…/0c1b…/0d1b…); `EditorBuildSettings.asset` GUIDs == the three `.unity.meta` GUIDs
(2a4e…/1f4e…/3f4e…), all enabled. So in *this* copy the wiring is consistent - the failure lives in
the one path headless suites simulate instead of execute: `SceneManager.LoadScene` → bootstrap
`Awake` → `RunQueuedSceneAction`.

**Root cause (class):** that handshake had **no verification and no recovery**. Any single break -
bootstrap component missing/stale GUID in a *local* project, scene disabled in Build Settings, an
exception mid-`Build`, or a null view - left the player on a half-built screen (e.g. the intro text
box with no NIGHT backdrop) with **zero log output**, because `PlayIntroThenCheckpoint` silently
skipped null views and nothing ever noticed an unconsumed queued action.

**Fix:** (1) `GameRoot.CheckSceneHandshake` - Update-based watchdog: if the queued start action is
not consumed within 3.5 s, `LogError` naming the usual causes, notify on screen, fall back to
MainMenu. (2) `CoreBootstrap.SafeBuild` - every scene `Awake` wraps `Build` in try/catch →
LogException + notify + menu fallback. (3) `PlayIntroThenCheckpoint` now `LogError`s any null
Dialogue/Loading/Checkpoint instead of skipping silently. (4) `SceneRouter.Load` rejects unknown
names with an error. Covers NEW GAME, CONTINUE, mode starts and next-shift (same path).

**PlayMode test:** `Assets/Tests/PlayMode/StartFlowTests.cs` (+asmdef; `com.unity.test-framework`
added to the manifest). **RUN: BLOCKED** - no Unity install here. Exact command:
`Unity -batchmode -nographics -projectPath <repo> -runTests -testPlatform PlayMode -testResults results.xml`

**Options rework:** scrollable column (no overlap at SMALL/NORMAL/LARGE or any window; CONTROLS/BACK
pinned outside the scroll), ON/OFF filled toggles, inverted selected state, Master+Music+SFX sliders
(audible-default migration for prefs written silent by the pre-fix slider, with a boot log),
resolution row hidden under borderless fullscreen, Esc=back / Tab=cycle / wheel=scroll, single type
scale + spacing unit in `UIFactory`.
