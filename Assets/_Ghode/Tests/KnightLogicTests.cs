// KnightLogicTests.cs — EditMode unit tests for the pure game logic,
// mirroring the unit tests from our web version. They poke KnightLogic and
// BoardState directly (no Unity scene needed) and run in the Test Runner:
// Window → General → Test Runner → EditMode → Run All.

using NUnit.Framework;
using Ghode.Core;

namespace Ghode.Tests
{
    /// <summary>
    /// Tests for <see cref="KnightLogic"/> and <see cref="BoardState"/>:
    /// legal-move counts, Warnsdorff's choice, win detection, dead-end
    /// detection, and Undo behavior.
    /// Fun fact baked in below: a 4×4 board has NO full knight's tour at all,
    /// so no test should ever assert that one completes — and 4×4 is not one
    /// of our offered sizes anyway (5, 6, 8).
    /// </summary>
    public class KnightLogicTests
    {
        // ------------------------------------------------------------------
        // Legal move counts
        // ------------------------------------------------------------------

        [Test]
        public void CornerHasTwoMoves_OnFiveBoard()
        {
            // In plain words: a knight in the corner of a 5×5 can only reach 2 squares.
            var board = new BoardState(5);
            board.PlaceStart(0, 0);

            var moves = KnightLogic.LegalMovesFrom(board);

            Assert.AreEqual(2, moves.Count);
            CollectionAssert.Contains(moves, (1, 2));
            CollectionAssert.Contains(moves, (2, 1));
        }

        [Test]
        public void CenterHasEightMoves_OnFiveBoard()
        {
            // From the exact center of a 5×5, all 8 L-hops stay on the board.
            var board = new BoardState(5);
            board.PlaceStart(2, 2);

            Assert.AreEqual(8, KnightLogic.LegalMovesFrom(board).Count);
        }

        [Test]
        public void NoMovesBeforePlacement()
        {
            // No horse on the board yet — nowhere to hop from.
            var board = new BoardState(5);

            Assert.IsEmpty(KnightLogic.LegalMovesFrom(board));
            Assert.IsFalse(KnightLogic.HasAnyMove(board));
        }

        // ------------------------------------------------------------------
        // Warnsdorff's rule
        // ------------------------------------------------------------------

        [Test]
        public void WarnsdorffBest_PicksSquareWithFewestOnwardMoves()
        {
            // Property test: whatever square WarnsdorffBest picks, no other
            // legal move may have FEWER onward options. We recompute the onward
            // counts independently here so the test does not trust the helper.
            var board = new BoardState(5);
            board.PlaceStart(0, 0);
            board.ApplyMove(1, 2); // move off the corner so options differ

            var best = KnightLogic.WarnsdorffBest(board);
            Assert.IsTrue(best.HasValue, "There are legal moves, so best must exist.");

            int bestOnward = KnightLogic.LegalMovesFrom(board, best.Value.r, best.Value.c).Count;
            foreach (var move in KnightLogic.LegalMovesFrom(board))
            {
                int onward = KnightLogic.LegalMovesFrom(board, move.r, move.c).Count;
                Assert.GreaterOrEqual(onward, bestOnward,
                    "WarnsdorffBest picked a square with more onward moves than " + move);
            }
        }

        [Test]
        public void WarnsdorffBest_ReturnsNullWhenStuck()
        {
            var board = MakeStuckBoard();
            Assert.IsNull(KnightLogic.WarnsdorffBest(board));
        }

        [Test]
        public void WarnsdorffBest_BreaksFullTieByLowestIndex()
        {
            // From the exact center of a fresh 5×5 every landing square has the
            // SAME onward count (2) and the SAME distance from centre — so the
            // final tie-break (lowest index, row-major) must decide: (0,1).
            var board = new BoardState(5);
            board.PlaceStart(2, 2);

            var best = KnightLogic.WarnsdorffBest(board);

            Assert.AreEqual((0, 1), best.Value,
                "Full tie must fall through to the lowest row-major index.");
        }

        [Test]
        public void WarnsdorffBest_ObeysAllThreeTieBreakRules_ThroughWholeGames()
        {
            // Property test across many real positions: play greedy games on
            // every offered size and, at EVERY step, recompute the three rules
            // independently. The chosen move must be the unique winner of
            // (fewest onward) → (farthest from centre) → (lowest index).
            foreach (int size in KnightLogic.BoardSizes)
            {
                var board = new BoardState(size);
                board.PlaceStart(0, 0);

                while (board.Phase == Phase.Playing)
                {
                    var pick = KnightLogic.WarnsdorffBest(board);
                    if (pick == null) break;

                    foreach (var other in KnightLogic.LegalMovesFrom(board))
                    {
                        if (other == pick.Value) continue;

                        int pickOnward = KnightLogic.LegalMovesFrom(board, pick.Value.r, pick.Value.c).Count;
                        int otherOnward = KnightLogic.LegalMovesFrom(board, other.r, other.c).Count;
                        int pickDist = KnightLogic.CentreDistanceScore(size, pick.Value.r, pick.Value.c);
                        int otherDist = KnightLogic.CentreDistanceScore(size, other.r, other.c);
                        int pickIndex = pick.Value.r * size + pick.Value.c;
                        int otherIndex = other.r * size + other.c;

                        // In plain words: walk the rule chain; the first rule
                        // where the two moves differ must favor the pick.
                        bool pickWins =
                            pickOnward != otherOnward ? pickOnward < otherOnward :
                            pickDist != otherDist ? pickDist > otherDist :
                            pickIndex < otherIndex;

                        Assert.IsTrue(pickWins,
                            $"On {size}×{size}, {pick} lost the tie-break chain to {other}.");
                    }

                    board.ApplyMove(pick.Value.r, pick.Value.c);
                }
            }
        }

        [Test]
        public void OddBoards_OddParityStartsCanNeverWin()
        {
            // Math fact (not just a heuristic): a knight alternates square
            // colors every hop, and an ODD board (5×5, 7×7) has one more
            // square of one color than the other — so a full tour MUST start
            // where (row+col) is even. From every odd-parity square no play
            // can ever win; greedy play must always end Lost.
            foreach (int size in new[] { 5, 7 })
            {
                for (int r = 0; r < size; r++)
                {
                    for (int c = 0; c < size; c++)
                    {
                        if ((r + c) % 2 == 0) continue; // only the doomed starts

                        var board = new BoardState(size);
                        board.PlaceStart(r, c);
                        while (board.Phase == Phase.Playing)
                        {
                            var best = KnightLogic.WarnsdorffBest(board);
                            if (best == null) break;
                            board.ApplyMove(best.Value.r, best.Value.c);
                        }

                        Assert.AreNotEqual(Phase.Won, board.Phase,
                            $"({r},{c}) is an odd-parity start on {size}×{size} — winning from it is impossible.");
                    }
                }
            }
        }

        [Test]
        public void EveryOfferedSize_GreedyCompletesFromSomeStart()
        {
            // Every size in the menu must actually be WINNABLE by following
            // the hint from at least one starting square — otherwise our own
            // Apprentice mode would be advertising an impossible puzzle.
            foreach (int size in KnightLogic.BoardSizes)
            {
                bool anyWin = false;
                for (int r = 0; r < size && !anyWin; r++)
                {
                    for (int c = 0; c < size && !anyWin; c++)
                    {
                        var board = new BoardState(size);
                        board.PlaceStart(r, c);
                        while (board.Phase == Phase.Playing)
                        {
                            var best = KnightLogic.WarnsdorffBest(board);
                            if (best == null) break;
                            board.ApplyMove(best.Value.r, best.Value.c);
                        }
                        anyWin = board.Phase == Phase.Won;
                    }
                }
                Assert.IsTrue(anyWin,
                    $"Greedy Warnsdorff must complete a {size}×{size} tour from at least one start.");
            }
        }

        // ------------------------------------------------------------------
        // Win detection
        // ------------------------------------------------------------------

        [Test]
        public void IsWin_FalseAtStartAndMidGame()
        {
            var board = new BoardState(5);
            Assert.IsFalse(KnightLogic.IsWin(board));

            board.PlaceStart(2, 2);
            Assert.IsFalse(KnightLogic.IsWin(board));

            board.ApplyMove(0, 1);
            Assert.IsFalse(KnightLogic.IsWin(board));
        }

        [Test]
        public void IsWin_TrueOnlyWhenEverydSquareVisited_ViaGreedyTour()
        {
            // Play greedy Warnsdorff from every start square of the 5×5.
            // Open tours exist on 5×5 and Warnsdorff finds them from at least
            // one start; whichever run wins must have ALL 25 squares stamped
            // and Phase == Won. (Reminder: never assert a full 4×4 tour — none exists.)
            bool anyWin = false;

            for (int r = 0; r < 5; r++)
            {
                for (int c = 0; c < 5; c++)
                {
                    var board = new BoardState(5);
                    board.PlaceStart(r, c);

                    // Follow the recommended move until the game ends either way.
                    while (board.Phase == Phase.Playing)
                    {
                        var best = KnightLogic.WarnsdorffBest(board);
                        if (best == null) break; // stuck — RecheckEndOfGame flips to Lost
                        board.ApplyMove(best.Value.r, best.Value.c);
                    }

                    if (board.Phase == Phase.Won)
                    {
                        anyWin = true;
                        Assert.AreEqual(board.TotalCells, board.VisitedCount,
                            "A Won board must have every square visited.");
                        Assert.IsTrue(KnightLogic.IsWin(board));
                        Assert.IsFalse(KnightLogic.HasAnyMove(board),
                            "A finished board has no unvisited square to hop to.");
                    }
                }
            }

            Assert.IsTrue(anyWin, "Greedy Warnsdorff should complete a 5×5 tour from at least one start.");
        }

        // ------------------------------------------------------------------
        // Dead-end (stuck) detection
        // ------------------------------------------------------------------

        [Test]
        public void HasAnyMove_ReportsDeadEnd()
        {
            // A hand-crafted 4-square trap (see MakeStuckBoard): the horse ends
            // on corner (0,0) whose only exits (1,2) and (2,1) are already stamped.
            var board = MakeStuckBoard();

            Assert.AreEqual(Phase.Lost, board.Phase, "The board should know it is stuck.");
            Assert.IsFalse(KnightLogic.HasAnyMove(board));
            Assert.IsFalse(KnightLogic.IsWin(board), "Stuck is not the same as winning.");
            Assert.AreEqual(4, board.VisitedCount, "The trail must remain visible after getting stuck.");
        }

        // ------------------------------------------------------------------
        // Undo
        // ------------------------------------------------------------------

        [Test]
        public void Undo_StepsBackAndRescuesAStuckBoard()
        {
            var board = MakeStuckBoard();
            Assert.AreEqual(Phase.Lost, board.Phase);

            // Undo the fatal hop: back on (1,2), and (0,0) is unstamped again.
            Assert.IsTrue(board.Undo());
            Assert.AreEqual(Phase.Playing, board.Phase, "Undo must rescue a stuck board.");
            Assert.AreEqual((1, 2), board.Current.Value);
            Assert.IsFalse(board.IsVisited(0, 0));
            Assert.AreEqual(3, board.VisitedCount);
        }

        [Test]
        public void Undo_AllTheWayBackReturnsToPlacing()
        {
            var board = new BoardState(5);
            board.PlaceStart(2, 2);
            board.ApplyMove(0, 1);

            Assert.IsTrue(board.Undo()); // un-hop
            Assert.IsTrue(board.Undo()); // un-place

            Assert.AreEqual(Phase.Placing, board.Phase);
            Assert.IsNull(board.Current);
            Assert.AreEqual(0, board.VisitedCount);
            Assert.IsFalse(board.Undo(), "Nothing left to undo on an empty board.");
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        // Builds a genuinely stuck 5×5 board in four verified knight moves:
        // (2,1) → (3,3) → (1,2) → (0,0). Corner (0,0) can only reach (1,2) and
        // (2,1) — both already stamped — so the horse is trapped.
        static BoardState MakeStuckBoard()
        {
            var board = new BoardState(5);
            Assert.IsTrue(board.PlaceStart(2, 1), "place (2,1)");
            Assert.IsTrue(board.ApplyMove(3, 3), "(2,1)→(3,3) is a legal L");
            Assert.IsTrue(board.ApplyMove(1, 2), "(3,3)→(1,2) is a legal L");
            Assert.IsTrue(board.ApplyMove(0, 0), "(1,2)→(0,0) is a legal L");
            return board;
        }
    }
}
