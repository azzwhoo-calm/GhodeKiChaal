/**
 * knightLogic.js
 * ----------------------------------------------------------------------------
 * Pure, framework-agnostic logic for the Knight's Tour ("Ghode Ki Chaal").
 *
 * Everything here is a pure function with no DOM / React dependency so it can
 * be unit-tested in isolation (see knightLogic.test.js) and reused by any
 * renderer.  Squares are addressed by a single integer index in the range
 * [0, size*size), counted left-to-right, top-to-bottom.
 *
 *   index = row * size + col
 */

/** The eight L-shaped offsets a chess knight can move, as [dRow, dCol]. */
export const KNIGHT_OFFSETS = [
  [-2, -1],
  [-2, 1],
  [-1, -2],
  [-1, 2],
  [1, -2],
  [1, 2],
  [2, -1],
  [2, 1],
];

/** Board sizes offered in the UI. 5x5 is the default. */
export const BOARD_SIZES = [5, 6, 8];

/** Convert (row, col) -> flat index. */
export function toIndex(row, col, size) {
  return row * size + col;
}

/** Convert flat index -> [row, col]. */
export function toRowCol(index, size) {
  return [Math.floor(index / size), index % size];
}

/** Is (row, col) inside an N x N board? */
export function inBounds(row, col, size) {
  return row >= 0 && col >= 0 && row < size && col < size;
}

/**
 * All on-board squares a knight on `index` could jump to, ignoring whether
 * those squares have already been visited.
 */
export function knightTargets(index, size) {
  const [row, col] = toRowCol(index, size);
  const targets = [];
  for (const [dRow, dCol] of KNIGHT_OFFSETS) {
    const r = row + dRow;
    const c = col + dCol;
    if (inBounds(r, c, size)) targets.push(toIndex(r, c, size));
  }
  return targets;
}

/**
 * Legal next moves from `index`: knight targets that have NOT been visited.
 *
 * @param {number} index   current square
 * @param {number} size    board dimension
 * @param {boolean[]} visited  visited[i] === true if square i is filled
 * @returns {number[]} indices the player may legally move to
 */
export function legalMoves(index, size, visited) {
  if (index == null) return [];
  return knightTargets(index, size).filter((target) => !visited[target]);
}

/**
 * Count of onward legal moves from `index` (its "accessibility degree").
 * Used by Warnsdorff's rule.
 */
export function degree(index, size, visited) {
  return legalMoves(index, size, visited).length;
}

/**
 * Warnsdorff's rule: recommend the legal move whose destination has the
 * FEWEST onward moves. This greedy heuristic almost always completes a tour
 * and is the basis of the in-game hint system.
 *
 * Ties are broken by a secondary "distance from centre" preference (prefer the
 * square nearer the edge/corner), which empirically improves completion rate,
 * and finally by lowest index for determinism.
 *
 * @returns {number|null} recommended destination index, or null if stuck.
 */
export function warnsdorffNext(index, size, visited) {
  const moves = legalMoves(index, size, visited);
  if (moves.length === 0) return null;

  const centre = (size - 1) / 2;
  let best = null;
  let bestDegree = Infinity;
  let bestDistance = -Infinity;

  for (const move of moves) {
    // Mark `move` as visited to count its onward options accurately.
    visited[move] = true;
    const deg = degree(move, size, visited);
    visited[move] = false;

    const [r, c] = toRowCol(move, size);
    const distance = Math.abs(r - centre) + Math.abs(c - centre);

    if (
      deg < bestDegree ||
      (deg === bestDegree && distance > bestDistance) ||
      (deg === bestDegree && distance === bestDistance && move < best)
    ) {
      best = move;
      bestDegree = deg;
      bestDistance = distance;
    }
  }
  return best;
}

/**
 * Compute the full ordered sequence Warnsdorff's rule would take from a given
 * start. Returns the list of visited indices in order. If the rule fails to
 * complete a tour it still returns the partial path it managed.
 *
 * This powers the "auto-solve" demo and lets us verify, in tests, that a
 * complete tour exists from a chosen start square.
 */
export function warnsdorffPath(startIndex, size) {
  const total = size * size;
  const visited = new Array(total).fill(false);
  const path = [startIndex];
  visited[startIndex] = true;
  let current = startIndex;

  for (let step = 1; step < total; step++) {
    const next = warnsdorffNext(current, size, visited);
    if (next == null) break;
    visited[next] = true;
    path.push(next);
    current = next;
  }
  return path;
}

/**
 * Classify the state of a game after a square has been visited.
 *
 * @returns {'won'|'lost'|'playing'}
 *   - 'won'     every square is visited (complete tour)
 *   - 'lost'    squares remain but no legal move exists
 *   - 'playing' the game continues
 */
export function evaluateStatus(index, size, visited) {
  const total = size * size;
  const visitedCount = visited.reduce((n, v) => n + (v ? 1 : 0), 0);
  if (visitedCount >= total) return 'won';
  if (legalMoves(index, size, visited).length === 0) return 'lost';
  return 'playing';
}
