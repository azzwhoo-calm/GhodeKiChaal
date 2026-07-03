# Design Decisions & Assumptions

This log records the choices made while building **Ghode Ki Chaal**, including
the answers you gave to the clarifying questions and the smaller assumptions
made autonomously during the build.

## Answered by you (clarifying questions)

| Topic | Decision |
|------|----------|
| **Tech stack** | React 18 + Vite 5 |
| **Visuals** | Inline SVG carved-wood horse + CSS wood gradients |
| **Sound** | Synthesized via the Web Audio API (no asset files) |
| **Board sizes** | 5×5 default, plus 6×6 and 8×8 |

## Game-rule decisions

1. **Move counting.** Placing the horse on its first square is *move 0* (it
   fills the first square). Each subsequent hop increments the move count.
   A completed tour therefore shows `size² − 1` moves. Both the move count and
   "filled X / total" are derived from the visit history (single source of truth).
2. **Win / lose.** Win = every square visited. Lose = squares remain *and* the
   current square has zero legal moves. Evaluated after every placement/move.
3. **Start square is free.** The player may start anywhere. Not every start can
   complete a tour (see below); this is treated as part of the challenge rather
   than restricted, and the lose detection handles dead ends gracefully.
4. **Undo.** Undo is always available during play and from the **lose** screen
   ("Undo & keep trying"), which clears the result and resumes play. Undoing the
   first square returns to the "placing" phase. Win is also undoable in state,
   though the win screen intentionally doesn't expose it.

## Difficulty & hint model

The brief listed both a "difficulty/hint toggle" and a "highlight legal moves"
feature. These were unified into one coherent model:

- **Difficulty** controls *passive* highlighting:
  - **Apprentice (easy)** — legal moves highlighted **and** the Warnsdorff best
    move continuously marked.
  - **Knight (normal, default)** — legal moves highlighted.
  - **Master (hard)** — no highlighting; the player must spot the L-shapes.
- **Hint button** is a separate, on-demand learning tool, available regardless
  of difficulty (toggleable in settings). Pressing it briefly reveals the
  **Warnsdorff-recommended** move (~2.6 s) and increments a "hints used" stat.

**Warnsdorff tie-breaking:** lowest onward-degree first; ties broken by greater
distance from the board centre (prefer edges/corners), then lowest index for
determinism. This improves tour-completion rate for the hint.

## Scoring / records

- **"Best score" = fastest completion time per board size.** A winning tour
  always takes exactly `size² − 1` moves, so move count can't rank wins — time
  is the meaningful metric. Stored per size in `localStorage`.
- A rolling **history of the last 12 games** (size, result, moves, time, date)
  is kept and shown on the menu.
- `localStorage` keys: `ghodekichaal.settings.v1`, `ghodekichaal.records.v1`
  (versioned so the shape can evolve).

## Solvability notes (documented, not bugs)

- **4×4** has **no** knight's tour from any start.
- **5×5** open tours exist only from squares where `(row + col)` is even
  (13 of the 25 squares, including all four corners).
- **6×6** and **8×8** have open tours from a corner (and many other squares).
- These facts are covered by unit tests in `src/game/knightLogic.test.js`.

## Pieces accumulate on the board (revised behaviour)

Originally a single horse hopped from square to square. By request, **every
visited square now keeps its own horse for the rest of the round** — each new
move "spawns" a piece in place (a quick scale/drop bounce via the `horseSpawn`
CSS keyframe, which runs once on element mount). This leaves a clear visual
record of the whole tour. The move-order number was moved to a small **corner
badge** so it stays readable behind the horse, and the dashed path overlay is
layered **beneath** the horses (`z-index`: path 1, horse 4, badge 5).

## Loss keeps the board visible (revised behaviour)

Originally a loss jumped to a full result screen. By request, a **loss no longer
hides the board**: the game stays on the playing screen with `phase === 'lost'`,
the board (with every horse and the route) remains fully visible, and a
**non-blocking banner** appears *below* it ("Stuck! … Study the board, then undo
or restart") with Undo / Restart / Main menu. A **win** still uses the
celebratory full-screen result (the board is completely filled, so there is
nothing left to analyse).

Implementation notes for this change:
- `phase` gained terminal values `'won'` / `'lost'` (in addition to
  `'placing'` / `'playing'`). `MOVE`/`PLACE` set them; only a win navigates to
  the result screen.
- Result recording moved from a *screen* transition to a **`phase`** transition
  (`App.jsx`), so losses are still logged to history without changing screens.
- The timer now stops on any terminal phase (it previously relied on the screen
  leaving `'playing'`).

## Animation

- The hop is a **parabolic arc** built with the Web Animations API: the wrapper
  translates linearly cell-to-cell while the horse rises/falls (~`0.55 × cell`
  apex) and scales up slightly; a ground shadow shrinks/fades to sell the height.
  Durations: hop ≈ 380 ms, first-placement drop ≈ 320 ms.
- All motion is disabled under `prefers-reduced-motion` (the piece snaps).
- The board measures itself with a `ResizeObserver` so the horse, the SVG path
  overlay, and the grid stay pixel-aligned at any size (no grid `gap` is used,
  so `cellSize = boardWidth / size` is exact).

## Audio

- Every sound is one or more **synthesized "wood knocks"**: a fast-decaying
  pitched body plus a band-passed noise transient — tuned per effect (hop,
  set-down, invalid thud, button tick, win arpeggio, lose motif, ambience).
- The `AudioContext` is created/resumed on the first user gesture to satisfy
  browser autoplay policies.
- **Background ambience defaults to OFF** (subtle low noise + occasional creak)
  and is fully toggleable.

## Timer

- Uses `performance.now()` with **pause-safe accumulation**: time spent paused
  (or on menu/result screens) does **not** count toward the completion time.
- Reset on every New Game / Restart / Play Again via a `gameId` counter in state.
  *(During testing, a Restart issued while already on the playing screen reset
  but failed to re-start the clock; fixed by including `gameId` in the timer's
  run-effect dependencies.)*

## Other assumptions

- **Title/theme.** Themed as "Ghode Ki Chaal · The Horse Tour" to match the
  repository name (Hindi for the knight's move).
- **Default settings:** board 5×5, difficulty Knight (normal), sound on, hint
  button on, ambience off.
- **React.StrictMode** is enabled.
- **Accessibility:** board squares and menu actions are real `<button>`s with
  ARIA labels; the toggle uses `role="switch"`.
- **Build portability:** Vite `base: './'` so `dist/` works from any path.
- **No backend / no network:** everything runs client-side and offline.
