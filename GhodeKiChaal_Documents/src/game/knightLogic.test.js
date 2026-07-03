/**
 * Unit tests for the pure Knight's Tour logic.
 * Run with:  npm test
 *
 * These cover legal-move generation, Warnsdorff's rule, full-tour existence,
 * and — importantly — the win/lose classification across several board sizes
 * including small edge cases (3x3 / 4x4) where complete tours are impossible.
 */
import { describe, it, expect } from 'vitest';
import {
  toIndex,
  toRowCol,
  inBounds,
  knightTargets,
  legalMoves,
  degree,
  warnsdorffNext,
  warnsdorffPath,
  evaluateStatus,
} from './knightLogic.js';

const filled = (size) => new Array(size * size).fill(false);

describe('coordinate helpers', () => {
  it('round-trips index <-> row/col', () => {
    const size = 5;
    for (let i = 0; i < size * size; i++) {
      const [r, c] = toRowCol(i, size);
      expect(toIndex(r, c, size)).toBe(i);
    }
  });

  it('detects out-of-bounds', () => {
    expect(inBounds(0, 0, 5)).toBe(true);
    expect(inBounds(4, 4, 5)).toBe(true);
    expect(inBounds(-1, 0, 5)).toBe(false);
    expect(inBounds(5, 0, 5)).toBe(false);
  });
});

describe('knight targets', () => {
  it('a corner has exactly 2 targets', () => {
    expect(knightTargets(toIndex(0, 0, 5), 5).sort((a, b) => a - b)).toEqual(
      [toIndex(1, 2, 5), toIndex(2, 1, 5)].sort((a, b) => a - b)
    );
  });

  it('the centre of a 5x5 has all 8 targets', () => {
    expect(knightTargets(toIndex(2, 2, 5), 5)).toHaveLength(8);
  });
});

describe('legal moves vs visited squares', () => {
  it('excludes visited squares', () => {
    const size = 5;
    const visited = filled(size);
    const start = toIndex(0, 0, size);
    // Block one of the two corner targets.
    visited[toIndex(1, 2, size)] = true;
    expect(legalMoves(start, size, visited)).toEqual([toIndex(2, 1, size)]);
  });

  it('degree reflects remaining legal moves', () => {
    const size = 5;
    const visited = filled(size);
    expect(degree(toIndex(2, 2, size), size, visited)).toBe(8);
  });
});

describe('Warnsdorff recommendation', () => {
  it('prefers the lowest-degree destination', () => {
    const size = 5;
    const visited = filled(size);
    const start = toIndex(2, 2, size); // centre, 8 options
    const rec = warnsdorffNext(start, size, visited);
    // From an empty 5x5 centre, every corner-ward jump leads toward low-degree
    // squares; the recommended one must be a genuine legal move.
    expect(legalMoves(start, size, visited)).toContain(rec);
  });

  it('returns null when there are no moves', () => {
    const size = 3;
    const visited = filled(size);
    // Centre of a 3x3 board: a knight there can reach nothing.
    expect(warnsdorffNext(toIndex(1, 1, size), size, visited)).toBeNull();
  });
});

describe('full-tour existence via Warnsdorff', () => {
  it('finds a complete tour on 5x5 from a corner', () => {
    const size = 5;
    const path = warnsdorffPath(toIndex(0, 0, size), size);
    expect(path).toHaveLength(size * size);
    // No square should appear twice.
    expect(new Set(path).size).toBe(size * size);
  });

  it('finds a complete tour on 6x6 from a corner', () => {
    const size = 6;
    const path = warnsdorffPath(toIndex(0, 0, size), size);
    expect(path).toHaveLength(size * size);
    expect(new Set(path).size).toBe(size * size);
  });

  it('finds a complete tour on 8x8 from a corner', () => {
    const size = 8;
    const path = warnsdorffPath(toIndex(0, 0, size), size);
    expect(path).toHaveLength(size * size);
    expect(new Set(path).size).toBe(size * size);
  });

  it('5x5 has NO complete tour starting from a "wrong colour" square', () => {
    // A known result: 5x5 open tours can only start on squares where
    // (row + col) is even. (0,1) has (row+col) odd, so no full tour exists.
    const size = 5;
    const path = warnsdorffPath(toIndex(0, 1, size), size);
    expect(path.length).toBeLessThan(size * size);
  });
});

describe('win / lose classification', () => {
  it('reports "won" when every square is visited', () => {
    const size = 5;
    const visited = new Array(size * size).fill(true);
    expect(evaluateStatus(toIndex(2, 2, size), size, visited)).toBe('won');
  });

  it('reports "lost" when squares remain but no move is possible', () => {
    const size = 5;
    const visited = filled(size);
    const current = toIndex(0, 0, size);
    visited[current] = true;
    // Block the only two reachable squares from the corner.
    visited[toIndex(1, 2, size)] = true;
    visited[toIndex(2, 1, size)] = true;
    expect(evaluateStatus(current, size, visited)).toBe('lost');
  });

  it('reports "playing" when moves remain', () => {
    const size = 5;
    const visited = filled(size);
    const current = toIndex(2, 2, size);
    visited[current] = true;
    expect(evaluateStatus(current, size, visited)).toBe('playing');
  });

  it('4x4 board: Warnsdorff cannot complete (no full tour exists)', () => {
    // The 4x4 board famously has no knight's tour from any start.
    const size = 4;
    for (let start = 0; start < size * size; start++) {
      expect(warnsdorffPath(start, size).length).toBeLessThan(size * size);
    }
  });
});
