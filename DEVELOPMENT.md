# Ghode Ki Chaal — Developer Guide

Unity 6 Android port of the web Knight's-Tour puzzle. Two-person studio.
This file is the "come back after two weeks and keep going" document: how the
project is put together, how to run and test it, and what state it is in.

- **Unity version:** 6000.4.10f1 (pinned in `ProjectSettings/ProjectVersion.txt`)
- **Package id:** `com.azzwhoo.ghodekichaal` · portrait · min API 29 · IL2CPP · ARM64
- **Planning docs & tracker:** `GhodeKiChaal_Documents/` (open `ghode_tracker.html`
  for the 15-day schedule, QA plan and checklists; `progress.json` is its store)

---

## 1. Running the game

Open the project and press **Play in any scene, even an empty one.**
`GameBootstrap` self-builds the entire game in code at startup
(`[RuntimeInitializeOnLoadMethod]`): EventSystem, Canvas, safe area, all four
screens, controller, audio. Nothing needs to be dragged or saved in a scene.

> **Why code-built UI?** Zero-setup testability: the whole game can be built,
> driven, and screenshotted without a human clicking in the Editor. A
> scene+prefab refactor is a flagged `TODO(azzwhoo)` for later — do not
> "normalize" this prematurely.

The build's first scene is `Assets/Scenes/Boot.unity` (see
`EditorBuildSettings`); it is an empty stage for the bootstrap.

**Input:** this project is **Input System ONLY** (`activeInputHandler = 1`).
An `EventSystem` needs `InputSystemUIInputModule` — a legacy
`StandaloneInputModule` silently kills all taps. `GameBootstrap.EnsureEventSystem()`
heals module-less EventSystems and calls `AssignDefaultActions()` (a module
added from code never runs the editor's Reset, so its actions asset is empty
until we assign it).

## 2. Architecture

```
Assets/_Ghode/
├── Scripts/
│   ├── Core/        Ghode.Core.asmdef   — ENGINE-FREE game rules (noEngineReferences)
│   │   ├── KnightLogic.cs      pure rules: legal moves, Warnssdorff hint + tie-breaks, win/stuck
│   │   ├── BoardState.cs       one game in progress: path, move numbers, phase, undo
│   │   └── GameEnums.cs        Phase, Screen, Difficulty
│   ├── Game/        (Ghode.Runtime)
│   │   ├── GameController.cs   THE single source of truth; all actions enter here,
│   │   │                       views repaint on its StateChanged event
│   │   ├── GameBootstrap.cs    self-booting entry point (builds everything)
│   │   ├── GameTimer.cs        pause-safe stopwatch; injectable TimeSource for tests
│   │   └── ScreenManager.cs    exactly one screen root active at a time
│   ├── Data/        Settings/Records + their PlayerPrefs stores (versioned keys)
│   ├── Audio/       AudioManager (clips optional — silent no-ops until SFX land)
│   ├── UI/          code-built uGUI: UiFactory (+Palette), screens, BoardView,
│   │                CellView, HorsePiece, PathTrail, GhodeArt, controls
│   └── Utils/       TimeFormat
├── Tests/           Ghode.Tests.asmdef — EditMode NUnit suites
└── Art/Resources/Ghode/   wooden sprites, loaded by name via GhodeArt
```

Web → Unity mapping (keep behavior identical to the web version):

| Web (JS)               | Unity (C#)                  | Notes |
|------------------------|-----------------------------|-------|
| `game/knightLogic.js`  | `Core/KnightLogic.cs`       | identical fns + tie-breaks (onward → farther-from-centre → lowest index) |
| `state/gameReducer.js` | `BoardState` + `GameController` | same actions & phases |
| `state/records.js`     | `Data/Records.cs`           | MAX_HISTORY = 12 |
| `hooks/useTimer.js`    | `Game/GameTimer.cs`         | pause-safe |
| `audio/AudioEngine.js` | `Audio/AudioManager.cs`     | baked clips (pending) |
| `useLocalStorage.js`   | `Data/*Store.cs`            | PlayerPrefs JSON, versioned keys |
| `components/*.jsx`     | `UI/*` (code-built uGUI)    | one controller per screen |

### The board's four layers (BoardView)

frame sprite → cell grid (`CellView`, tiles + highlight shades + numbers) →
`PathTrail` ribbon → `HorsePiece` (spawn pop 320 ms, hop arc 380 ms with apex
0.55×cell + ground shadow, undo slide, reduced-motion snap). Cell centres are
computed with plain math — never read back from the layout system — so
animations are deterministic.

### Art pipeline

Sprites live in `Assets/_Ghode/Art/Resources/Ghode/` and load by name through
`GhodeArt`. **Every sprite is optional** — missing art falls back to flat
palette colors (`UiFactory.Palette`), so the game always runs. The horse PNG
originally had a solid cream background; its alpha was baked offline with a
border flood-fill keyer (see git history). The soft ground shadow is generated
procedurally in `GhodeArt.SoftShadow` — no texture needed.

Theme: walnut `#3A2A18`, parchment `#EAD9B0`, gold accent `#E0A83C`.

## 3. Conventions (contractual)

- `Ghode.Core` stays **engine-free** (`noEngineReferences: true`). Game rules
  must be unit-testable without Unity. Don't add `using UnityEngine` there.
- `// TODO(azzwhoo): <what & why>` for every deliberate gap — never a silent
  empty method. Grep for `TODO(azzwhoo)` to see all open decisions.
- Comment style: kid-friendly file headers, XML docs on all public members,
  `// In plain words:` before tricky bits.
- Views never mutate state; they call a `GameController` action and repaint on
  `StateChanged`.
- Settings/records writes go through the stores (versioned keys, corrupt-safe).

## 4. Testing

EditMode suites live in `Assets/_Ghode/Tests/`:

- `KnightLogicTests` — rules, Warnsdorff property + tie-break chain, the
  5×5 odd-parity impossibility theorem, undo.
- `GameControllerFlowTests` — whole-game flows on a real controller with a
  fake clock: restart-resets-timer (the historic web bug), pause excludes
  time, stuck→undo escape, walk-away losses, win recording, new-best badge.
  Snapshots and restores the developer's real PlayerPrefs.
- `GameTimerTests`, `RecordsTests`.

In the editor: **Window → General → Test Runner → EditMode → Run All**.

Headless (CI or agent):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe" `
  -batchmode -nographics -projectPath <repo> `
  -runTests -testPlatform EditMode `
  -testResults <out>\results.xml -logFile <out>\test.log
```

Agent loop that works (Unity MCP): write files → `AssetDatabase.Refresh()` →
read console logs → TestRunnerApi → drive `GameController` directly in Play
mode → `ScreenCapture.CaptureScreenshot` (+ force GameView repaint).

## 5. Android build & release

Already configured: IL2CPP, ARM64-only, min API 29, portrait,
`com.azzwhoo.ghodekichaal`, first scene `Boot.unity`.

1. **Keystore (once per studio):** run
   `powershell -ExecutionPolicy Bypass -File Tools/create-release-keystore.ps1`.
   It runs keytool interactively (passwords never touch disk), writes to
   `%USERPROFILE%\GhodeKiChaal-keys\`, and refuses to overwrite. Back it up
   twice; keystores are git-ignored (`*.keystore`, `*.jks`).
2. In Unity: **Player → Publishing Settings → Custom Keystore** → select it.
3. Bump `AndroidBundleVersionCode` (+ `bundleVersion`) every upload.
4. **File → Build Profiles → Android™** → Build App Bundle (AAB).
5. Upload to the Play Console **closed track**; install from the track link
   (not sideload) for smoke tests.

**Git LFS:** binary art/audio is LFS-tracked via `.gitattributes`. Run
`git lfs install` once per machine before your first pull/commit of art.

## 6. Status (2026-07-13) & what's next

Done — Tier 1 (foundation): project/Android config, build scenes, LFS,
keystore tooling. Tier 2 (logic parity): KnightLogic with full web tie-breaks,
BoardState, timer, records(12) — all under test. Tier 3 (presentation): all
screens + router, wooden art wired with fallbacks, horse spawn/hop/undo
animations + shadow, path trail, difficulty highlight tiers, stuck banner,
pause overlay (4 toggles incl. reduced motion), safe area.

Next (in plan order — see `ghode_tracker.html`):

- **Backgrounding:** freeze/persist the timer in `OnApplicationPause` (QA A-02/A-03).
- **SFX bake:** render the web synth recipes to WAV/OGG, assign in
  `AudioManager` (recipes are in the tracker's Tech tab); mixer + 20 ms mute ramp.
- **Save spec:** confirm record field names against the web version
  (`TODO(azzwhoo)` in `RecordsStore`), then decide PlayerPrefs → files.
- **First device build** + closed-track upload (starts the 14-day tester clock).
- Then: drag input, haptics, 7×7, themes, Play Games, billing, ads, analytics
  (Tiers 5–8 in the tracker).
