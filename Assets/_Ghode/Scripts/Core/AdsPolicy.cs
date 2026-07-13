// AdsPolicy.cs — the rulebook for WHEN an interstitial may appear.
// Straight from the GDD's ads section, and every rule protects the player:
//   · only BETWEEN games (the caller decides the moment; we check the rest)
//   · never in the player's first two sessions
//   · at most one ad per three finished rounds
//   · at least three minutes since the previous ad
//   · never while the stuck banner is on screen
//   · never, ever, once the Royal Stable is owned
// Pure C# with injected time — every rule is unit-tested without Unity.

namespace Ghode.Core
{
    /// <summary>
    /// Decides whether an interstitial is allowed right now. Owns no SDK and
    /// shows nothing itself — AdsService asks <see cref="MayShow"/> and only
    /// on true tells the actual ad provider to play.
    /// </summary>
    public class AdsPolicy
    {
        /// <summary>Sessions 1 and 2 are completely ad-free.</summary>
        public const int FreeSessions = 2;

        /// <summary>At most one ad per this many finished rounds.</summary>
        public const int RoundsPerAd = 3;

        /// <summary>Minimum quiet time between two ads, in milliseconds.</summary>
        public const long MinGapMs = 3 * 60 * 1000;

        readonly int _sessionNumber; // 1-based: the player's Nth app launch
        int _roundsSinceAd;
        long _lastAdAtMs = long.MinValue; // "never" until the first ad

        /// <summary>Royal Stable owned — the permanent kill switch.</summary>
        public bool Entitled { get; set; }

        /// <param name="sessionNumber">This launch's 1-based session count.</param>
        /// <param name="entitled">Whether the Royal Stable is already owned.</param>
        public AdsPolicy(int sessionNumber, bool entitled)
        {
            _sessionNumber = sessionNumber;
            Entitled = entitled;
        }

        /// <summary>How many rounds finished since the last ad (or session start).</summary>
        public int RoundsSinceAd => _roundsSinceAd;

        /// <summary>This launch's 1-based session count (analytics reads it).</summary>
        public int SessionNumber => _sessionNumber;

        /// <summary>A game just ended (win, loss, or abandoned attempt).</summary>
        public void NoteRoundFinished()
        {
            _roundsSinceAd++;
        }

        /// <summary>
        /// May an interstitial play at this between-games moment?
        /// <paramref name="stuckVisible"/> is true when the player is leaving
        /// a stuck board — the banner moment stays ad-free by design.
        /// </summary>
        public bool MayShow(long nowMs, bool stuckVisible)
        {
            if (Entitled) return false;                       // paid = ad-free, forever
            if (_sessionNumber <= FreeSessions) return false; // learn the game in peace
            if (_roundsSinceAd < RoundsPerAd) return false;   // ≤1 ad per 3 rounds
            if (stuckVisible) return false;                   // never kick them while stuck
            if (_lastAdAtMs != long.MinValue
                && nowMs - _lastAdAtMs < MinGapMs) return false; // ≥3 min apart
            return true;
        }

        /// <summary>An ad actually played — reset the counters.</summary>
        public void NoteAdShown(long nowMs)
        {
            _roundsSinceAd = 0;
            _lastAdAtMs = nowMs;
        }
    }
}
