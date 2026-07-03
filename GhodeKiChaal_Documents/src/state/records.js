/**
 * records.js
 * ----------------------------------------------------------------------------
 * Pure helpers for the persisted "best score / history" data.
 *
 * Shape stored in localStorage (key: see App.jsx):
 *   {
 *     bestTime: { 5: ms, 6: ms, 8: ms },   // fastest COMPLETED tour per size
 *     games:    [ { size, result, moves, timeMs, hintsUsed, date }, ... ]
 *   }
 *
 * Note on "best score": a completed Knight's Tour always takes exactly
 * size*size - 1 moves, so move count is not a meaningful ranking for wins.
 * We therefore rank wins by fastest completion time instead, and keep a rolling
 * history of recent games for context.
 */

export const emptyRecords = { bestTime: {}, games: [] };

const MAX_HISTORY = 12;

/**
 * Return a NEW records object with `entry` recorded.
 * @param {object} records   previous records (may be undefined)
 * @param {object} entry     { size, result:'won'|'lost', moves, timeMs, hintsUsed }
 */
export function recordGame(records, entry) {
  const base = records && records.games ? records : emptyRecords;
  const game = { ...entry, date: Date.now() };

  const games = [game, ...base.games].slice(0, MAX_HISTORY);

  const bestTime = { ...base.bestTime };
  if (entry.result === 'won') {
    const prev = bestTime[entry.size];
    if (prev == null || entry.timeMs < prev) bestTime[entry.size] = entry.timeMs;
  }

  return { bestTime, games };
}

/** Best (fastest) completion time for a board size, or null. */
export function bestTimeFor(records, size) {
  if (!records || !records.bestTime) return null;
  return records.bestTime[size] ?? null;
}
