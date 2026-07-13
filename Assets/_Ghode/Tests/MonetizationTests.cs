// MonetizationTests.cs — EditMode tests for the money-adjacent plumbing that
// is OURS (no store SDK involved): the entitlement cache, the leaderboard
// offline queue, and the ads session counter. Everything runs against a
// throwaway save folder via SaveService.RootOverride.

using System.IO;
using NUnit.Framework;
using Ghode.Data;
using Ghode.Monetization;

namespace Ghode.Tests
{
    /// <summary>
    /// Tests for <see cref="EntitlementsStore"/>, <see cref="LeaderboardService"/>
    /// and <see cref="AdsService"/> session counting.
    /// </summary>
    public class MonetizationTests
    {
        string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "GhodeMonTests_" + System.Guid.NewGuid().ToString("N"));
            SaveService.RootOverride = _root;
        }

        [TearDown]
        public void TearDown()
        {
            SaveService.RootOverride = null;
            try { Directory.Delete(_root, recursive: true); } catch { /* never created */ }
        }

        // ------------------------------------------------------------------
        // Entitlements cache
        // ------------------------------------------------------------------

        [Test]
        public void Entitlements_DefaultToNothingOwned()
        {
            Assert.IsFalse(EntitlementsStore.Load().royalStable);
        }

        [Test]
        public void Entitlements_RoundTrip()
        {
            EntitlementsStore.Save(new Entitlements { royalStable = true });

            Assert.IsTrue(EntitlementsStore.Load().royalStable,
                "The purchase must survive a relaunch (offline cache).");
            Assert.IsTrue(File.Exists(Path.Combine(_root, "entitlements.v1.json")));
        }

        // ------------------------------------------------------------------
        // Leaderboards: submit, queue, flush
        // ------------------------------------------------------------------

        [Test]
        public void Win_SubmitsDirectly_WhenSignedIn()
        {
            var backend = new LocalLeaderboardBackend();
            var service = new LeaderboardService(backend);
            service.SignIn();

            service.SubmitWin(5, 42000);

            Assert.AreEqual(1, backend.Received.Count);
            Assert.AreEqual((LeaderboardService.BoardIdFor(5), 42000L), backend.Received[0]);
            Assert.AreEqual(0, service.PendingCount);
        }

        [Test]
        public void Win_Queues_WhenSignedOut_AndFlushesOnSignIn()
        {
            var backend = new LocalLeaderboardBackend();
            var service = new LeaderboardService(backend); // nobody signed in

            service.SubmitWin(7, 99000);
            Assert.AreEqual(0, backend.Received.Count, "Nothing can be delivered yet…");
            Assert.AreEqual(1, service.PendingCount, "…so the score waits in the queue.");

            service.SignIn(); // sign-in flushes automatically

            Assert.AreEqual(1, backend.Received.Count, "The queued score arrives after sign-in.");
            Assert.AreEqual((LeaderboardService.BoardIdFor(7), 99000L), backend.Received[0]);
            Assert.AreEqual(0, service.PendingCount);
        }

        [Test]
        public void Queue_SurvivesARelaunch()
        {
            // Session 1: offline win, app dies.
            var first = new LeaderboardService(new LocalLeaderboardBackend());
            first.SubmitWin(6, 55000);
            Assert.AreEqual(1, first.PendingCount);

            // Session 2: a brand-new service reads the same save folder.
            var backend = new LocalLeaderboardBackend();
            var second = new LeaderboardService(backend);
            Assert.AreEqual(1, second.PendingCount, "The queue is persisted, not in-memory.");

            second.SignIn();
            Assert.AreEqual(1, backend.Received.Count);
        }

        // ------------------------------------------------------------------
        // Ads session counting (the "first two sessions free" input)
        // ------------------------------------------------------------------

        [Test]
        public void AdsService_CountsSessionsAcrossLaunches()
        {
            // Three launches = session numbers 1, 2, 3. The first two never
            // show ads no matter what; the third may (policy allowing).
            for (int launch = 1; launch <= 3; launch++)
            {
                var ads = new AdsService { NowMs = () => 10_000_000 };
                ads.Initialize(entitled: false, () => new FakeInterstitialProvider());
                for (int r = 0; r < 3; r++) ads.OnRoundFinished();

                bool shown = ads.TryShowInterstitial(stuckVisible: false);
                Assert.AreEqual(launch >= 3, shown,
                    $"Launch {launch}: ad shown should be {launch >= 3}.");
            }
        }

        [Test]
        public void AdsService_NeverCreatesProvider_WhenEntitled()
        {
            var ads = new AdsService();
            bool factoryCalled = false;

            ads.Initialize(entitled: true, () => { factoryCalled = true; return new FakeInterstitialProvider(); });
            for (int r = 0; r < 9; r++) ads.OnRoundFinished();

            Assert.IsFalse(factoryCalled, "Zero ad-SDK init when entitled — the factory must never run.");
            Assert.IsFalse(ads.TryShowInterstitial(stuckVisible: false));
        }
    }
}
