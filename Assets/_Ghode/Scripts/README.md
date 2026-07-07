# Ghode Ki Chaal · The Horse Tour — Script Structure

Unity 6 · C# · uGUI · Android 10+ (portrait) · namespace `Ghode.*` · package id `com.azzwhoo.ghodekichaal`

## How to test in the Editor (3 lines)

1. Open **any** scene (even an empty one) and press **Play** — `GameBootstrap` self-builds the whole UI and lands on the Main Menu.
2. Mouse = touch: New Game → tap a square to place the horse → hop along the highlighted L-moves. HUD has Hint / Undo / Restart / Sound / Menu.
3. Unit tests: **Window → General → Test Runner → EditMode → Run All** (10 tests, all green).

## Folder map

```
Assets/_Ghode/
├── Art/                          → sprites (Wooden_Sprites), fonts, materials, models, textures, VFX, animations
├── Audio/                        → SFX/ and Ambience/ clips for AudioManager
├── Data/                         → ScriptableObject configs (empty for now)
├── Prefabs/                      → prefab home for the post-prototype UI refactor
├── Scripts/
│   ├── Ghode.Runtime.asmdef      → everything below except Core
│   ├── Core/                     → PURE C# — no UnityEngine (Ghode.Core.asmdef, noEngineReferences)
│   │   ├── GameEnums.cs          Phase / Screen / Difficulty
│   │   ├── KnightLogic.cs        knight rules, Warnsdorff's rule, win/stuck checks
│   │   └── BoardState.cs         one game's memory: visits, order, path, Undo
│   ├── Game/                     → orchestration (MonoBehaviours)
│   │   ├── GameBootstrap.cs      self-boots in any scene, builds all uGUI in code
│   │   ├── GameController.cs     the single source of truth (mirrors web App.jsx)
│   │   ├── GameTimer.cs          pause-safe stopwatch (mirrors useTimer)
│   │   └── ScreenManager.cs      exactly one screen active at a time
│   ├── Data/                     → persistence (PlayerPrefs + JSON)
│   │   ├── Settings.cs / SettingsStore.cs    key: ghodekichaal.settings.v1
│   │   └── Records.cs / RecordsStore.cs      key: ghodekichaal.records.v1
│   ├── UI/                       → uGUI views, all built from code
│   │   ├── UiFactory.cs          panels/texts/buttons/stacks + walnut-parchment Palette
│   │   ├── SafeAreaFitter.cs     keeps UI out of notches
│   │   ├── BoardView.cs / CellView.cs        the grid and its squares
│   │   ├── Hud.cs                stats row + Hint/Undo/Restart/Sound/Menu
│   │   ├── MainMenuScreen.cs / InstructionsScreen.cs / GameScreen.cs / ResultScreen.cs
│   │   ├── PauseOverlay.cs       Resume/Restart/Menu + live setting toggles
│   │   └── Controls/             SegmentedControl.cs, ToggleControl.cs
│   ├── Audio/AudioManager.cs     SFX + ambience, optional clips, safe no-op
│   └── Utils/TimeFormat.cs       ms → "m:ss" / "m:ss.ff", friendly dates
└── Tests/                        → Ghode.Tests.asmdef (EditMode only)
    └── KnightLogicTests.cs       10 tests: move counts, Warnsdorff, win, dead-end, undo
```

## Fully working vs. stubbed

**Fully working now (verified in Play mode):**
- Boot-from-empty-scene → Menu → Game → Result flow, zero scene wiring
- Complete rules: place anywhere, legal-L-only hops, visited squares locked + numbered
- Difficulty highlighting (Apprentice: legal + best; Knight: legal; Master: none)
- Win detection → Result screen; stuck detection → non-blocking Stuck banner with the board intact (Undo & keep trying / Restart / Menu)
- Hint (Warnsdorff) with counter, Undo (rescues stuck boards), Restart, pause-safe timer
- Settings + best times + recent history persisted (PlayerPrefs JSON)
- Pause overlay with live Sound/Hints/Ambience toggles
- All 10 EditMode tests pass

**Stubbed / TODO(azzwhoo) — search the code for `TODO(azzwhoo)`:**
- Refactor code-built UI into a proper Boot scene + prefabs (post-prototype)
- Assign real SFX + ambience clips in `AudioManager` (currently silent no-ops)
- Assign the carved-horse sprite in `CellView` (currently a gold square)
- Dashed visited-path overlay in `BoardView`
- Knight-move diagram on the Instructions screen
- Cross-check save-data field names against the Save-Data Spec doc (`RecordsStore`, `GameController.RecordFinished`)
- Richer stats/history (`Records`), persist-pending-loss decision (`GameController`)
- Real display font in `UiFactory` (currently Unity's built-in LegacyRuntime)

## Notes

- **Input:** everything flows through uGUI Buttons + EventSystem (`InputSystemUIInputModule` — this project is Input System-only), so editor mouse and device touch need no branching.
- **Core is engine-free:** `Ghode.Core.asmdef` has `noEngineReferences: true`; keep it that way so logic stays unit-testable.
- **`Ghode.Core.Screen` vs `UnityEngine.Screen`:** files that need both use `using AppScreen = Ghode.Core.Screen;`.
