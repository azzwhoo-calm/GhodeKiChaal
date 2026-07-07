// TimeFormat.cs — turns raw milliseconds into friendly text like "1:23" and
// saved date strings into short readable stamps for the history list.
// Small, boring, and used all over the UI — so it lives in one place.

using System;
using System.Globalization;

namespace Ghode.Utils
{
    /// <summary>
    /// Text helpers for time. <see cref="ToClock"/> gives "m:ss" for the HUD,
    /// <see cref="ToClockPrecise"/> gives "m:ss.ff" for results and best times,
    /// and <see cref="FriendlyDate"/> makes history rows human-readable.
    /// </summary>
    public static class TimeFormat
    {
        /// <summary>Milliseconds → "m:ss" (e.g. 83000 → "1:23"). Negative → "--:--".</summary>
        public static string ToClock(long ms)
        {
            if (ms < 0) return "--:--";
            long totalSeconds = ms / 1000;
            return string.Format("{0}:{1:00}", totalSeconds / 60, totalSeconds % 60);
        }

        /// <summary>
        /// Milliseconds → "m:ss.ff" with hundredths (e.g. 83450 → "1:23.45").
        /// Used where bragging rights matter: results and best times.
        /// </summary>
        public static string ToClockPrecise(long ms)
        {
            if (ms < 0) return "--:--.--";
            long totalSeconds = ms / 1000;
            long hundredths = (ms % 1000) / 10;
            return string.Format("{0}:{1:00}.{2:00}", totalSeconds / 60, totalSeconds % 60, hundredths);
        }

        /// <summary>
        /// ISO-8601 date string (what GameRecord stores) → short local stamp
        /// like "7 Jul, 18:45". If the text will not parse, it is returned as-is
        /// rather than crashing a history row.
        /// </summary>
        public static string FriendlyDate(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return "";
            try
            {
                // In plain words: read the saved UTC moment, shift it to the
                // player's own clock, then print it short and sweet.
                var utc = DateTime.Parse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                return utc.ToLocalTime().ToString("d MMM, HH:mm", CultureInfo.InvariantCulture);
            }
            catch
            {
                return iso;
            }
        }
    }
}
