// AdsPolicyTests.cs — EditMode tests for the ad rulebook. Every promise the
// GDD makes to the player is pinned here: two free sessions, one ad per
// three rounds at most, three quiet minutes between ads, no ads at the stuck
// banner, and NEVER once the Royal Stable is owned. Pure logic, fake clock.

using NUnit.Framework;
using Ghode.Core;

namespace Ghode.Tests
{
    /// <summary>Tests for <see cref="AdsPolicy"/>.</summary>
    public class AdsPolicyTests
    {
        const long T0 = 1_000_000; // an arbitrary "now", in ms

        // A policy in session 3+ with 3 rounds banked — every gate open
        // except the ones each test closes on purpose.
        static AdsPolicy EligiblePolicy()
        {
            var policy = new AdsPolicy(sessionNumber: 3, entitled: false);
            policy.NoteRoundFinished();
            policy.NoteRoundFinished();
            policy.NoteRoundFinished();
            return policy;
        }

        [Test]
        public void FirstTwoSessions_AreCompletelyAdFree()
        {
            foreach (int session in new[] { 1, 2 })
            {
                var policy = new AdsPolicy(session, entitled: false);
                for (int i = 0; i < 10; i++) policy.NoteRoundFinished();

                Assert.IsFalse(policy.MayShow(T0, stuckVisible: false),
                    $"Session {session} must stay ad-free no matter how many rounds.");
            }
        }

        [Test]
        public void ThirdSession_AfterThreeRounds_MayShow()
        {
            Assert.IsTrue(EligiblePolicy().MayShow(T0, stuckVisible: false));
        }

        [Test]
        public void FewerThanThreeRounds_NoAd()
        {
            var policy = new AdsPolicy(3, entitled: false);
            policy.NoteRoundFinished();
            policy.NoteRoundFinished(); // only two

            Assert.IsFalse(policy.MayShow(T0, stuckVisible: false));
        }

        [Test]
        public void StuckMoment_NoAd_EvenWhenEverythingElseAllows()
        {
            Assert.IsFalse(EligiblePolicy().MayShow(T0, stuckVisible: true));
        }

        [Test]
        public void Entitled_NoAd_Ever()
        {
            var policy = new AdsPolicy(99, entitled: true);
            for (int i = 0; i < 50; i++) policy.NoteRoundFinished();

            Assert.IsFalse(policy.MayShow(T0, stuckVisible: false));
        }

        [Test]
        public void MidSessionPurchase_KillsAdsInstantly()
        {
            var policy = EligiblePolicy();
            Assert.IsTrue(policy.MayShow(T0, stuckVisible: false), "Sanity: eligible before.");

            policy.Entitled = true; // the Royal Stable just arrived

            Assert.IsFalse(policy.MayShow(T0, stuckVisible: false));
        }

        [Test]
        public void ThreeMinuteGap_IsEnforced_BetweenAds()
        {
            var policy = EligiblePolicy();
            policy.NoteAdShown(T0);

            // Bank three more rounds — the ROUND gate reopens...
            policy.NoteRoundFinished();
            policy.NoteRoundFinished();
            policy.NoteRoundFinished();

            // ...but the CLOCK gate holds until 3 minutes have passed.
            Assert.IsFalse(policy.MayShow(T0 + AdsPolicy.MinGapMs - 1, stuckVisible: false),
                "One millisecond early is still too early.");
            Assert.IsTrue(policy.MayShow(T0 + AdsPolicy.MinGapMs, stuckVisible: false));
        }

        [Test]
        public void AdShown_ResetsTheRoundCount()
        {
            var policy = EligiblePolicy();
            policy.NoteAdShown(T0);

            Assert.AreEqual(0, policy.RoundsSinceAd);
            Assert.IsFalse(policy.MayShow(T0 + AdsPolicy.MinGapMs * 2, stuckVisible: false),
                "After an ad, three NEW rounds are owed before the next one.");
        }

        [Test]
        public void FirstAdOfSession_NeedsNoTimeGap()
        {
            // The 3-minute rule is BETWEEN ads — the first ad after enough
            // rounds must not wait for some imaginary previous ad.
            Assert.IsTrue(EligiblePolicy().MayShow(nowMs: 0, stuckVisible: false));
        }
    }
}
