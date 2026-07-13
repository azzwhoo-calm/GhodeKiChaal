// RecordsTests.cs — EditMode tests for the trophy shelf (Records.cs):
// the 12-game history cap, best-time bookkeeping, and the new-best signal.

using NUnit.Framework;
using Ghode.Data;

namespace Ghode.Tests
{
    /// <summary>
    /// Tests for <see cref="Records"/>: history capping at
    /// <see cref="Records.MaxRecent"/> (12, matching the web version), newest
    /// first ordering, and that only strictly faster WINS set best times.
    /// </summary>
    public class RecordsTests
    {
        static GameRecord Game(int size, bool won, long ms)
        {
            return new GameRecord { boardSize = size, won = won, timeMs = ms, playedAtIso = "2026-07-13T00:00:00.0000000Z" };
        }

        [Test]
        public void HistoryCapsAtTwelve_NewestFirst()
        {
            var records = new Records();

            // 15 games, each 1 ms slower than the last so we can tell them apart.
            for (int i = 0; i < 15; i++)
            {
                records.RecordGame(Game(5, won: false, ms: 1000 + i));
            }

            Assert.AreEqual(12, Records.MaxRecent, "The cap itself is part of the save spec.");
            Assert.AreEqual(12, records.recent.Count, "Oldest games must fall off past the cap.");
            Assert.AreEqual(1014, records.recent[0].timeMs, "Newest game sits first.");
            Assert.AreEqual(1003, records.recent[11].timeMs, "The three oldest are gone.");
        }

        [Test]
        public void OnlyWinsSetBestTimes()
        {
            var records = new Records();

            Assert.IsFalse(records.RecordGame(Game(5, won: false, ms: 1)),
                "A lightning-fast LOSS is not a trophy.");
            Assert.AreEqual(-1, records.BestTimeFor(5));

            Assert.IsTrue(records.RecordGame(Game(5, won: true, ms: 60000)),
                "The first win on a size is automatically its best.");
            Assert.AreEqual(60000, records.BestTimeFor(5));
        }

        [Test]
        public void OnlyStrictlyFasterWinsBeatTheBest()
        {
            var records = new Records();
            records.RecordGame(Game(5, won: true, ms: 60000));

            Assert.IsFalse(records.RecordGame(Game(5, won: true, ms: 60000)),
                "An EQUAL time keeps the old best.");
            Assert.IsTrue(records.RecordGame(Game(5, won: true, ms: 59999)),
                "One millisecond faster takes it.");
            Assert.AreEqual(59999, records.BestTimeFor(5));
        }

        [Test]
        public void BestTimesAreKeptPerBoardSize()
        {
            var records = new Records();
            records.RecordGame(Game(5, won: true, ms: 30000));
            records.RecordGame(Game(8, won: true, ms: 90000));

            Assert.AreEqual(30000, records.BestTimeFor(5));
            Assert.AreEqual(90000, records.BestTimeFor(8));
            Assert.AreEqual(-1, records.BestTimeFor(6), "Never won there yet.");
        }
    }
}
