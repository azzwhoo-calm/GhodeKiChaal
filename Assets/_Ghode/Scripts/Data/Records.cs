// Records.cs — the trophy shelf: best completion time per board size, plus a
// short list of recently played games. RecordsStore.cs saves/loads this.
// JsonUtility cannot save Dictionaries, so best times live in a small list.

using System;
using System.Collections.Generic;

namespace Ghode.Data
{
    /// <summary>One finished (or abandoned-when-stuck) game, for the history list.</summary>
    [Serializable]
    public class GameRecord
    {
        /// <summary>Board size this game was played on (5, 6 or 8).</summary>
        public int boardSize;

        /// <summary>Did the player visit every square?</summary>
        public bool won;

        /// <summary>How long the attempt took, in milliseconds.</summary>
        public long timeMs;

        /// <summary>How many hops the horse made (not counting the placement).</summary>
        public int moves;

        /// <summary>How many hints the player asked for.</summary>
        public int hintsUsed;

        /// <summary>When the game finished, as an ISO-8601 UTC string ("o" format).</summary>
        public string playedAtIso;
    }

    /// <summary>The best winning time for one particular board size.</summary>
    [Serializable]
    public class BestTime
    {
        public int boardSize;
        public long timeMs;
    }

    /// <summary>
    /// All long-term player achievements: best time per board size and a capped
    /// recent-games history. Ask it questions with <see cref="BestTimeFor"/> and
    /// feed it finished games with <see cref="RecordGame"/>.
    /// </summary>
    [Serializable]
    public class Records
    {
        /// <summary>The save-file format this class writes. Bump + migrate on change.</summary>
        public const int CurrentSchema = 1;

        /// <summary>Format stamp carried inside the save file (see SaveService).</summary>
        public int schemaVersion = CurrentSchema;

        /// <summary>
        /// How many recent games we keep before the oldest falls off.
        /// 12 to match the web version's records.js (MAX_HISTORY = 12), so a
        /// future save import/export lines up one-to-one.
        /// </summary>
        public const int MaxRecent = 12;

        /// <summary>Best winning time per board size (one entry per size, wins only).</summary>
        public List<BestTime> bestTimes = new List<BestTime>();

        /// <summary>Newest-first list of recent games, capped at <see cref="MaxRecent"/>.</summary>
        public List<GameRecord> recent = new List<GameRecord>();

        // TODO(azzwhoo): richer history — per-size filtering, win streaks, and a
        // "games played" counter would be nice on a stats page someday.

        /// <summary>
        /// Store one finished game. Returns true when this game set a NEW best
        /// time for its board size (the Result screen shows a badge for that).
        /// </summary>
        public bool RecordGame(GameRecord game)
        {
            if (game == null) return false;

            // Newest games sit at the front; trim anything past the cap.
            recent.Insert(0, game);
            if (recent.Count > MaxRecent)
            {
                recent.RemoveRange(MaxRecent, recent.Count - MaxRecent);
            }

            // Only WINS can set a best time — a fast loss is not a trophy.
            if (!game.won) return false;

            var entry = bestTimes.Find(b => b.boardSize == game.boardSize);
            if (entry == null)
            {
                // First ever win on this size — automatically the best.
                bestTimes.Add(new BestTime { boardSize = game.boardSize, timeMs = game.timeMs });
                return true;
            }
            if (game.timeMs < entry.timeMs)
            {
                entry.timeMs = game.timeMs;
                return true;
            }
            return false;
        }

        /// <summary>Best winning time for a board size, or -1 if never won there.</summary>
        public long BestTimeFor(int size)
        {
            var entry = bestTimes.Find(b => b.boardSize == size);
            return entry != null ? entry.timeMs : -1L;
        }
    }
}
