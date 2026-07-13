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
| `audio/AudioEngine.js` | `Audio/AudioManager.cs`     | baked clips, hop round-robin, 20 ms mute ramp |
| `useLocalStorage.js`   | `Data/SaveService + *Store.cs` | atomic JSON files in persistentDataPath |
| `components/*.jsx`     | `UI/*` (code-built uGUI)    | one controller per screen |

### The board's four layers (BoardView)

frame sprite → cell grid (`CellView`, tiles + highlight shades + numbers) →
`PathTrail` ribbon → `HorsePiece` (spawn pop 320 ms, hop arc 380 ms with apex
0.55×cell + ground shadow, undo slide, reduced-motion snap). Cell centres are
computed with plain math — never read back from the layout system — so
animations are deterministic.

### Saves (Tier 4)

`SaveService` owns disk IO: `settings.v1.json` / `records.v1.json` in
`persistentDataPath` (on this dev machine:
`%USERPROFILE%\AppData\LocalLow\azzwhoo\GhodeKiChaal`). Three promises:
atomic tmp→swap writes, corrupt files quarantined to `*.bad` + defaults, and a
`schemaVersion` int gate (mismatch = quarantine until a migration exists).
Legacy PlayerPrefs saves from pre-Tier-4 builds import once, then the key is
deleted. Tests redirect everything via `SaveService.RootOverride`.

**Backgrounding:** `GameController.OnApplicationPause(true)` silently
auto-pauses a mid-game attempt (timer frozen; resume is the player's move) so
background time costs zero seconds. `OnApplicationQuit` settles a pending
stuck-loss. Accepted gap: an OS force-kill while stuck drops that one loss.

### Audio (Tier 4)

Clips live in `Assets/_Ghode/Art/Resources/Ghode/Audio/` — baked offline from
the synth recipes in the tracker's Tech tab (6 hop variants + place / invalid /
click / win / lose at −1 dBFS, plus a 12.6 s seamless ambience loop). The
baker script is reproducible (fixed seed); regenerate or replace clips freely —
`AudioManager` loads by name and silently skips anything missing. Hops
round-robin through the six variants; mute is a 20 ms volume ramp. There is
deliberately NO AudioMixer asset: mixers cannot be created from code, and two
ramped sources deliver the same behavior at this scale.

### Input, haptics & themes (Tier 5)

**Drag (M1):** BoardView implements the uGUI drag interfaces (events bubble up
from the tapped cell). A drag may only START on the horse's current square;
dropping on a legal square moves (the piece settles under the finger),
anywhere else glides it back. The 12 dp threshold is set on
`EventSystem.pixelDragThreshold` by GameBootstrap; below it a gesture stays a
tap, above it uGUI cancels the click itself — the two inputs cannot fight.

**Haptics (M8):** `Ghode.Haptics.HapticsService` — Android
`VibrationEffect`s (tick / double-reject / short-short-long win / soft-thud
lose), no-op in the editor, master switch mirrors Settings.Haptics.
NOTE: real buzz patterns are UNVERIFIED until the first device build.

**Themes:** `GhodeTheme` (UI) holds three full color sets — Wood (the tile
art), Ebony and Marble (flat recolors incl. their own label colors for
readability). Views repaint from `GhodeTheme.Colors` on every StateChanged,
so switching is `SetTheme` + repaint. Picker lives in the pause overlay;
lock Ebony/Marble behind the Royal Stable entitlement when billing lands
(flagged TODO). Per-theme frame/horse art is still an art-pipeline item —
the skeleton tints the wood sprites.

**48 dp audit:** every tappable control is ≥132 ref-px (≈48 dp on the
lowest-density target) — enforced by `UiFactory.CreateButton`'s default plus
explicit row heights. Documented exception: board CELLS on 6×6+ (a phone-wide
chessboard cannot offer 48 dp squares; standard chess-app tradeoff). The
pause panel auto-sizes (ContentSizeFitter), so new rows can't push buttons
off-panel. Still open from the accessibility list: TalkBack/screen-reader
support and the font-scale pass — both need on-device work.

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
  time, stuck→undo escape, walk-away losses, win recording, new-best badge,
  backgrounding/quit lifecycle (invoked via reflection — SendMessage trips a
  Unity assert in EditMode). Redirects saves to a temp folder.
- `SaveServiceTests` — atomic writes, corrupt→quarantine, schema gate,
  legacy PlayerPrefs import.
- `GameTimerTests`, `RecordsTests`.

**UI tap verification** (play mode, editor + MCP): dispatch clicks by
raycasting at the button's screen point (`EventSystem.RaycastAll`) and
requiring the hit to belong to the button before `ExecuteEvents` — this is
what catches 0-px tap targets that direct `onClick` calls hide. Two gotchas:
(1) a plain-color Image has ZERO preferred size, so buttons inside
`childControlHeight` stacks collapse without a `LayoutElement` height —
`UiFactory.CreateButton` now bakes in a 96 px default; (2) a screen activated
in the SAME editor tick has no layout yet, so raycasts miss — let one frame
pass (separate MCP command) before clicking anything on a fresh screen.

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
pause overlay (4 toggles incl. reduced motion), safe area. Tier 4 (services):
SaveService (atomic files, quarantine, schema gate, legacy import),
backgrounding auto-pause + quit-settles-stuck-loss, full baked SFX set with
round-robin hops and 20 ms mute ramp. Every interactive control verified
tappable via raycast-driven play-mode sweep (that sweep found and fixed the
0-height button bug and the Instructions overflow that hid the Back button —
the rules page now scrolls). Tier 5 (input & UX): drag-the-horse input with
snap-back (12 dp threshold, taps unaffected), 7×7 board (+ solvability
tests), haptics service + setting, Wood/Ebony/Marble theme skeleton with the
pause-menu picker, and the 48 dp touch-target audit (0 failures across all
screens; cells excepted).

Next (in plan order — see `ghode_tracker.html`):

- **First device build** + closed-track upload (starts the 14-day tester
  clock) — keystore first (`Tools/create-release-keystore.ps1`). Also the
  first chance to FEEL the haptic patterns and check themes on OLED.
- **Save spec:** confirm record field names against the web version
  (`TODO(azzwhoo)` in `RecordsStore`) before anyone's records matter.
- **Accessibility leftovers:** TalkBack/screen-reader pass + font ×1.3 pass
  (both need a device).
- Then Tiers 6–8: Play Games leaderboards, billing (lock Ebony/Marble
  behind Royal Stable), AdMob+UMP, analytics/Crashlytics, release
  engineering.
