// KnightLogic.cs — the heart of the game: all the rules about how a knight
// (our horse) is allowed to move. Every question like "where can the horse
// hop?", "is the player stuck?", "did they win?" is answered right here.
// Pure C# with zero Unity dependency, so we can unit-test it easily.

using System.Collections.Generic;

namespace Ghode.Core
{
    /// <summary>
    /// A bag of static helper functions that know the chess-knight rules.
    /// It never remembers anything itself — you hand it a <see cref="BoardState"/>
    /// and it answers questions about that board.
    /// </summary>
    public static class KnightLogic
    {
        /// <summary>The board sizes the game offers: 5×5, 6×6 and 8×8.</summary>
        public static readonly int[] BoardSizes = { 5, 6, 8 };

        // In plain words: a knight hops in an L shape — two squares one way,
        // then one square sideways. These two arrays list all 8 possible Ls.
        // Pair them up by index: (RowJumps[i], ColJumps[i]) is one full hop.
        static readonly int[] RowJumps = { -2, -2, -1, -1, 1, 1, 2, 2 };
        static readonly int[] ColJumps = { -1, 1, -2, 2, -2, 2, -1, 1 };

        /// <summary>Is the square (r, c) actually on a board of this size?</summary>
        public static bool IsInside(int size, int r, int c)
        {
            return r >= 0 && r < size && c >= 0 && c < size;
        }

        /// <summary>
        /// Would hopping to (r, c) be allowed right now?
        /// Allowed means: the horse is already placed, the square is on the board,
        /// nobody has visited it yet, and it is a real L-shaped hop away.
        /// </summary>
        public static bool IsLegalMove(BoardState board, int r, int c)
        {
            if (board.Current == null) return false;                       // horse not placed yet
            if (!IsInside(board.Size, r, c)) return false;                  // off the board
            if (board.IsVisited(r, c)) return false;                        // square already stamped

            var cur = board.Current.Value;
            for (int i = 0; i < 8; i++)
            {
                if (cur.r + RowJumps[i] == r && cur.c + ColJumps[i] == c) return true;
            }
            return false; // not an L shape from where the horse stands
        }

        /// <summary>
        /// Every square the horse may legally hop to from where it stands now.
        /// Returns an empty list if the horse is not placed yet or is stuck.
        /// </summary>
        public static List<(int r, int c)> LegalMovesFrom(BoardState board)
        {
            if (board.Current == null) return new List<(int r, int c)>();
            var cur = board.Current.Value;
            return LegalMovesFrom(board, cur.r, cur.c);
        }

        /// <summary>
        /// Every unvisited square that is one L-shaped hop away from (fromR, fromC).
        /// This overload lets us ask "what if the horse stood HERE?" — which is
        /// exactly what Warnsdorff's rule needs.
        /// </summary>
        public static List<(int r, int c)> LegalMovesFrom(BoardState board, int fromR, int fromC)
        {
            var moves = new List<(int r, int c)>();
            for (int i = 0; i < 8; i++)
            {
                int r = fromR + RowJumps[i];
                int c = fromC + ColJumps[i];
                if (IsInside(board.Size, r, c) && !board.IsVisited(r, c))
                {
                    moves.Add((r, c));
                }
            }
            return moves;
        }

        /// <summary>
        /// The smartest next hop, using Warnsdorff's rule: of all the legal hops,
        /// pick the one whose landing square has the FEWEST onward hops after it.
        /// (Visit the cramped corners early, before they become unreachable.)
        /// Ties break exactly like the web version, in this order:
        /// farther from the board's centre first, then lowest square index —
        /// so the hint is deterministic and both versions always agree.
        /// Returns null when there is no legal hop at all.
        /// </summary>
        public static (int r, int c)? WarnsdorffBest(BoardState board)
        {
            var options = LegalMovesFrom(board);
            if (options.Count == 0) return null;

            (int r, int c) best = default;
            int bestOnward = int.MaxValue;
            int bestCentreDist = -1;
            int bestIndex = int.MaxValue;

            foreach (var move in options)
            {
                // In plain words: pretend the horse hopped to `move`, then count
                // where it could go NEXT. Fewer choices = more urgent to visit now.
                // (The square the horse currently stands on is already stamped
                // visited in `board`, so it is never counted as an onward hop.)
                int onward = LegalMovesFrom(board, move.r, move.c).Count;
                int centreDist = CentreDistanceScore(board.Size, move.r, move.c);
                int index = move.r * board.Size + move.c;

                bool wins;
                if (onward != bestOnward)
                {
                    wins = onward < bestOnward;          // rule 1: fewest onward hops
                }
                else if (centreDist != bestCentreDist)
                {
                    wins = centreDist > bestCentreDist;  // rule 2: farther from centre
                }
                else
                {
                    wins = index < bestIndex;            // rule 3: lowest index
                }

                if (wins)
                {
                    best = move;
                    bestOnward = onward;
                    bestCentreDist = centreDist;
                    bestIndex = index;
                }
            }
            return best;
        }

        /// <summary>
        /// How far square (r, c) sits from the board's centre, as a comparable
        /// whole number (squared distance × 4, so a 6×6 board's half-square
        /// centre never forces floating point). Bigger = farther out.
        /// </summary>
        public static int CentreDistanceScore(int size, int r, int c)
        {
            // In plain words: measure from the true centre (size-1)/2 in
            // half-square units: (2r - (size-1))² + (2c - (size-1))².
            int dr = 2 * r - (size - 1);
            int dc = 2 * c - (size - 1);
            return dr * dr + dc * dc;
        }

        /// <summary>Can the horse still hop anywhere at all?</summary>
        public static bool HasAnyMove(BoardState board)
        {
            return LegalMovesFrom(board).Count > 0;
        }

        /// <summary>Has every square on the board been visited? That is the win.</summary>
        public static bool IsWin(BoardState board)
        {
            return board.VisitedCount == board.TotalCells;
        }
    }
}
