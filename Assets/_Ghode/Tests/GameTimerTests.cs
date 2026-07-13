// GameTimerTests.cs — EditMode tests for the pause-safe stopwatch.
// A fake clock (just a double we bump by hand) stands in for real time, so
// these tests "wait" ten minutes in zero real seconds and never flake.

using NUnit.Framework;
using Ghode.Game;

namespace Ghode.Tests
{
    /// <summary>
    /// Tests for <see cref="GameTimer"/>: banking across pause/resume cycles,
    /// reset behavior, and the guarantees the HUD and records rely on.
    /// </summary>
    public class GameTimerTests
    {
        double _now; // the fake clock, in seconds
        GameTimer _timer;

        [SetUp]
        public void SetUp()
        {
            _now = 100.0; // any non-zero start proves nothing assumes t=0
            _timer = new GameTimer { TimeSource = () => _now };
        }

        [Test]
        public void StartsStoppedAtZero()
        {
            Assert.IsFalse(_timer.IsRunning);
            Assert.AreEqual(0, _timer.ReadMs());
        }

        [Test]
        public void ReadsLiveTimeWhileRunning()
        {
            _timer.Start();
            _now += 2.5; // 2.5 fake seconds pass

            Assert.AreEqual(2500, _timer.ReadMs());
            Assert.IsTrue(_timer.IsRunning);
        }

        [Test]
        public void PauseFreezesTheClock()
        {
            _timer.Start();
            _now += 3.0;
            _timer.Pause();

            _now += 60.0; // a whole paused minute must not count

            Assert.AreEqual(3000, _timer.ReadMs());
            Assert.IsFalse(_timer.IsRunning);
        }

        [Test]
        public void BanksAcrossManyPauseResumeCycles()
        {
            // In plain words: 1s play, pause, 2s play, pause, 0.5s play = 3.5s.
            _timer.Start(); _now += 1.0; _timer.Pause();
            _now += 10.0; // idle gap, must not count
            _timer.Start(); _now += 2.0; _timer.Pause();
            _now += 5.0;  // idle gap, must not count
            _timer.Start(); _now += 0.5;

            Assert.AreEqual(3500, _timer.ReadMs());
        }

        [Test]
        public void DoubleStartDoesNotRestartTheStretch()
        {
            _timer.Start();
            _now += 2.0;
            _timer.Start(); // must be a harmless no-op, not a re-anchor

            Assert.AreEqual(2000, _timer.ReadMs());
        }

        [Test]
        public void DoublePauseDoesNotDoubleBank()
        {
            _timer.Start();
            _now += 2.0;
            _timer.Pause();
            _timer.Pause(); // second pause must not add the stretch again

            Assert.AreEqual(2000, _timer.ReadMs());
        }

        [Test]
        public void ResetClearsBankAndStops()
        {
            _timer.Start();
            _now += 7.0;
            _timer.Pause();

            _timer.Reset();

            Assert.AreEqual(0, _timer.ReadMs());
            Assert.IsFalse(_timer.IsRunning);

            // And the timer is fully usable again after a reset.
            _timer.Start();
            _now += 1.0;
            Assert.AreEqual(1000, _timer.ReadMs());
        }
    }
}
