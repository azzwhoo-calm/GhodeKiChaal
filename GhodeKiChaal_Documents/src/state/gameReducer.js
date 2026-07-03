/**
 * gameReducer.js
 * ----------------------------------------------------------------------------
 * The single source of truth for screen flow and in-game board state.
 *
 * Screen flow (no dead ends):
 *
 *     menu ──New Game──▶ playing ──win/lose──▶ result ──┐
 *      ▲  ▲                 │  ▲                          │
 *      │  └──Instructions───┘  └──Restart / Undo──────────┘
 *      └──────────────── Quit / Play Again ───────────────┘
 *
 * The board itself moves through two phases:
 *   'placing'  — the player picks any square to set the horse down
 *   'playing'  — the player makes knight moves until win or lose
 */
import { legalMoves, evaluateStatus } from '../game/knightLogic.js';

/** Build a fresh board for a given size. */
function createGame(size) {
  return {
    size,
    visited: new Array(size * size).fill(false),
    current: null, // current square index, or null while placing
    phase: 'placing', // 'placing' | 'playing'
    history: [], // ordered list of visited indices (history[0] = start square)
    hintsUsed: 0,
    undosUsed: 0,
  };
}

/** Visited count and move count are always derived from history. */
export const visitedCount = (game) => game.history.length;
export const moveCount = (game) => Math.max(0, game.history.length - 1);

export const initialState = {
  screen: 'menu', // 'menu' | 'instructions' | 'playing' | 'result'
  paused: false,
  result: null, // 'won' | 'lost' | null  (set when screen === 'result')
  game: null,
  // Bumped on every New Game / Restart so the parent can reset the timer.
  gameId: 0,
};

export function gameReducer(state, action) {
  switch (action.type) {
    // ---- Navigation ------------------------------------------------------
    case 'GO_MENU':
      return { ...initialState };

    case 'GO_INSTRUCTIONS':
      return { ...state, screen: 'instructions' };

    case 'CLOSE_INSTRUCTIONS':
      // Return to wherever the player opened the instructions from:
      // a paused game if one exists, otherwise the main menu.
      if (state.game) return { ...state, screen: 'playing', paused: true };
      return { ...initialState, gameId: state.gameId };

    case 'NEW_GAME':
      return {
        screen: 'playing',
        paused: false,
        result: null,
        game: createGame(action.size),
        gameId: state.gameId + 1,
      };

    case 'RESTART':
      if (!state.game) return state;
      return {
        screen: 'playing',
        paused: false,
        result: null,
        game: createGame(state.game.size),
        gameId: state.gameId + 1,
      };

    case 'PAUSE':
      return state.screen === 'playing' ? { ...state, paused: true } : state;

    case 'RESUME':
      return { ...state, paused: false };

    // ---- Gameplay --------------------------------------------------------
    case 'PLACE': {
      // Set the horse on its starting square (only valid while placing).
      const { game } = state;
      if (!game || game.phase !== 'placing' || state.paused) return state;
      const { index } = action;
      const visited = game.visited.slice();
      visited[index] = true;
      const baseGame = {
        ...game,
        visited,
        current: index,
        phase: 'playing',
        history: [index],
      };
      // A 1-square board would win instantly; otherwise placing never ends a game.
      const status = evaluateStatus(index, game.size, visited);
      // A win is terminal and shown on a celebratory result screen.
      if (status === 'won') {
        return { ...state, game: { ...baseGame, phase: 'won' }, screen: 'result', result: 'won' };
      }
      // A loss keeps the board on screen (phase = 'lost') for the player to study.
      if (status === 'lost') {
        return { ...state, game: { ...baseGame, phase: 'lost' } };
      }
      return { ...state, game: baseGame };
    }

    case 'MOVE': {
      const { game } = state;
      if (!game || game.phase !== 'playing' || state.paused) return state;
      const { index } = action;
      // Guard: ignore illegal destinations (the UI also prevents these).
      const legal = legalMoves(game.current, game.size, game.visited);
      if (!legal.includes(index)) return state;

      const visited = game.visited.slice();
      visited[index] = true;
      const baseGame = {
        ...game,
        visited,
        current: index,
        history: [...game.history, index],
      };
      const status = evaluateStatus(index, game.size, visited);
      if (status === 'won') {
        return { ...state, game: { ...baseGame, phase: 'won' }, screen: 'result', result: 'won' };
      }
      // Stay on the board so the player can analyse the dead end.
      if (status === 'lost') {
        return { ...state, game: { ...baseGame, phase: 'lost' } };
      }
      return { ...state, game: baseGame };
    }

    case 'UNDO': {
      const { game } = state;
      if (!game) return state;
      // Allow undo from the result screen too (e.g. undo the losing move).
      if (game.history.length === 0) return state;

      const history = game.history.slice();
      const removed = history.pop();
      const visited = game.visited.slice();
      visited[removed] = false;

      let nextGame;
      if (history.length === 0) {
        // Undid the start square -> back to placing phase.
        nextGame = {
          ...game,
          visited,
          history,
          current: null,
          phase: 'placing',
          undosUsed: game.undosUsed + 1,
        };
      } else {
        nextGame = {
          ...game,
          visited,
          history,
          current: history[history.length - 1],
          phase: 'playing',
          undosUsed: game.undosUsed + 1,
        };
      }
      // Undoing always returns to active play (clears any win/lose result).
      return { ...state, screen: 'playing', result: null, paused: false, game: nextGame };
    }

    case 'USE_HINT': {
      const { game } = state;
      if (!game || game.phase !== 'playing') return state;
      return { ...state, game: { ...game, hintsUsed: game.hintsUsed + 1 } };
    }

    default:
      return state;
  }
}
