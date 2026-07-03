# Ghode Ki Chaal · The Horse Tour

A polished, web-based **Knight's Tour** puzzle. Guide a carved wooden horse
(styled like a chess knight) around the board using only legal knight moves —
visit every square exactly once to complete the tour.

> *“Ghode ki chaal”* is the Hindi name for the knight’s L-shaped move — a fitting
> title for a Knight’s Tour game.

Built with **React + Vite**, an **inline-SVG** wooden horse, a wood/parchment
chessboard, and **fully synthesized Web Audio** sound (no audio asset files).

---

## Features

- **Three board sizes** — 5×5 (default), 6×6, 8×8.
- **Accumulating carved-wood horses** — every visited square keeps its own
  scalable inline-SVG horse for the rest of the round; each new piece springs
  in with a little bounce.
- **Wooden / parchment chessboard** with a dashed **visited-path overlay** and
  numbered move order on every visited square.
- **Win / lose detection** — win by visiting all squares; lose when no legal
  move remains. On a loss the **board stays fully visible** so you can retrace
  your route, with a non-blocking **“Stuck!” banner** below it offering
  **“Undo & keep trying,”** Restart, or Main menu.
- **Hint system** based on **Warnsdorff’s rule** (always move to the square with
  the fewest onward options) — a real learning tool, toggleable.
- **Difficulty** (controls passive highlighting): Apprentice / Knight / Master.
- **HUD** — board size, squares filled, move count, live timer, hints used, plus
  Hint / Undo / Restart / mute / Menu controls.
- **Pause overlay** with live settings, **Instructions** screen with a move
  diagram, and a **main menu** showing **best completion times** and a
  **recent-games history** (saved to `localStorage`).
- **Synthesized wooden/percussive sound**: hop, set-down, invalid thud, button
  tick, win fanfare, lose motif, and optional subtle background ambience.
- **Responsive** — works on desktop and mobile/touch.
- **Accessible** — squares and controls are real buttons with ARIA labels;
  honors `prefers-reduced-motion`.

---

## Requirements

- **Node.js 18+** (developed on Node 25) and npm.

## Setup & run

```bash
# 1. Install dependencies
npm install

# 2. Start the dev server (hot reload)
npm run dev
# → open the printed URL, e.g. http://localhost:5173

# 3. Production build (outputs to dist/)
npm run build

# 4. Preview the production build locally
npm run preview

# 5. Run the unit tests (board logic, win/lose, edge cases)
npm test
```

The production build uses **relative asset paths** (`base: './'`), so the
contents of `dist/` can be served from any static host or sub-path.

---

## How to play

1. Choose a board size and difficulty on the menu, then press **New Game**.
2. **Tap any square** to set the horse down — that’s your starting square.
3. **Hop** to any highlighted square (a legal knight move to an unvisited cell).
   Each square you land on is filled and locked.
4. **Win** by visiting every square. **Lose** if you get stranded with empty
   squares and no legal move left.
5. Stuck? Press **Hint** for the Warnsdorff-recommended move, or **Undo** to
   take a move back and try another route.

> Tip: not every starting square yields a complete tour (especially on 5×5).
> Corners and edges have few entrances — plan your route through them early.

---

## Project structure

```
src/
├── main.jsx                 # React entry point
├── App.jsx                  # Top-level orchestrator (state, timer, audio sync, records)
├── game/
│   ├── knightLogic.js       # Pure logic: moves, Warnsdorff, win/lose  (no UI deps)
│   └── knightLogic.test.js  # Vitest unit tests
├── state/
│   ├── gameReducer.js       # Screen flow + board state machine (useReducer)
│   └── records.js           # Best-times / history helpers
├── audio/
│   └── AudioEngine.js        # Web Audio synthesis (wooden sound effects)
├── hooks/
│   ├── useLocalStorage.js
│   └── useTimer.js          # Accumulating stopwatch (pause-safe)
├── components/
│   ├── MainMenu.jsx  Instructions.jsx  GameScreen.jsx  ResultScreen.jsx
│   ├── Board.jsx  Square.jsx  KnightPiece.jsx  HorseSvg.jsx  Hud.jsx
│   ├── PauseOverlay.jsx
│   └── controls/ (Toggle.jsx, SegmentedControl.jsx)
├── styles/index.css         # Wood/parchment theme + all component styles
├── uiOptions.js             # Shared option lists + default settings
└── utils/format.js          # Time / date formatting
```

**Separation of concerns:** board logic (`game/`), state flow (`state/`),
audio (`audio/`), and rendering/UI (`components/`) are independent. The logic
layer is pure and unit-tested in isolation.

---

## Notes

- **No external assets.** The horse is inline SVG and every sound is generated
  at runtime with the Web Audio API, so the whole app is self-contained.
- **Audio & browsers.** Browsers block audio until the first user interaction;
  the engine unlocks on the first click/tap. Background ambience is **off by
  default**.
- See **`DECISIONS.md`** for design decisions and documented assumptions.

## Known limitations

- There is no full auto-solver; Warnsdorff’s rule powers the per-move **hint**
  only (by design — the player drives the tour).
- Some start squares cannot complete a tour (e.g. half of the 5×5 squares; the
  4×4 board has no tour at all). This is inherent to the puzzle, not a bug — the
  lose condition handles the resulting dead ends.
- Sounds are synthesized “wood” tones, not recorded samples.
