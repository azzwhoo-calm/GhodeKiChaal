// LeaderboardService.cs — per-board-size best-time leaderboards (M6).
// Every win is submitted to the board for its size. Sign-in is OPTIONAL:
// scores earned while offline/signed-out queue up in leaderboards.v1.json
// and flush the moment a sign-in succeeds — nobody's best time is lost to
// a dead hotel wifi. The real Google Play Games backend hides behind
// ILeaderboardBackend so the game never touches GPGS types directly.
//
// TODO(azzwhoo): GPGS drop-in, once the Play Console app + leaderboard ids
// exist:
//   1. Import the official Play Games plugin (github.com/playgameservices/
//      play-games-plugin-for-unity) and paste the Android app id.
//   2. Create four leaderboards in the Play Console (best time per size)
//      and map them in BoardIdFor() below.
//   3. Write PlayGamesLeaderboardBackend : ILeaderboardBackend around
//      PlayGamesPlatform (silent SignIn at startup, SubmitScore with the
//      mapped ids). Swap it in from GameController.ConnectServices().
// The queue, flush logic and submit-on-win wiring all stay exactly as-is.

using System;
using System.Collections.Generic;
using UnityEngine;
using Ghode.Data;

namespace Ghode.Monetization
{
    /// <summary>What LeaderboardService needs from a real backend.</summary>
    public interface ILeaderboardBackend
    {
        /// <summary>Is the player signed in right now?</summary>
        bool SignedIn { get; }

        /// <summary>Try to sign in (silently where possible).</summary>
        void SignIn(Action<bool> done);

        /// <summary>Send one score to one board. Reports success.</summary>
        void Submit(string boardId, long timeMs, Action<bool> done);
    }

    /// <summary>
    /// The stand-in backend: "signed in" locally, accepts every submission
    /// with a log line. Keeps the whole pipeline runnable in the editor.
    /// </summary>
    public class LocalLeaderboardBackend : ILeaderboardBackend
    {
        public bool SignedIn { get; private set; }

        public readonly List<(string boardId, long timeMs)> Received =
            new List<(string boardId, long timeMs)>();

        public void SignIn(Action<bool> done)
        {
            SignedIn = true;
            done?.Invoke(true);
        }

        public void Submit(string boardId, long timeMs, Action<bool> done)
        {
            if (!SignedIn) { done?.Invoke(false); return; }
            Received.Add((boardId, timeMs));
            Debug.Log($"[Leaderboards] (local) {boardId} ← {timeMs} ms");
            done?.Invoke(true);
        }
    }

    /// <summary>
    /// Submits winning times to the right per-size board, queueing anything
    /// that cannot be delivered right now. Created by GameController; the
    /// backend is swappable (local fake today, Play Games later).
    /// </summary>
    public class LeaderboardService
    {
        const string QueueFile = "leaderboards.v1.json";

        [Serializable]
        class PendingScore
        {
            public string boardId;
            public long timeMs;
        }

        [Serializable]
        class QueueState
        {
            public int schemaVersion = 1;
            public List<PendingScore> pending = new List<PendingScore>();
        }

        ILeaderboardBackend _backend;
        QueueState _queue;

        public LeaderboardService(ILeaderboardBackend backend)
        {
            _backend = backend;
            _queue = SaveService.LoadJson<QueueState>(QueueFile, q => q.schemaVersion == 1)
                ?? new QueueState();
        }

        /// <summary>The backend in use (tests swap in fakes here).</summary>
        public ILeaderboardBackend Backend
        {
            get => _backend;
            set => _backend = value;
        }

        /// <summary>Scores still waiting for a successful delivery.</summary>
        public int PendingCount => _queue.pending.Count;

        /// <summary>The Play Games board id for one board size.</summary>
        public static string BoardIdFor(int boardSize)
        {
            // TODO(azzwhoo): replace with the REAL ids from the Play Console
            // (they look like "CgkI…") once the leaderboards exist there.
            return "ghode_best_" + boardSize + "x" + boardSize;
        }

        /// <summary>Try to sign in; a success flushes everything queued.</summary>
        public void SignIn(Action<bool> done = null)
        {
            _backend.SignIn(ok =>
            {
                if (ok) FlushQueue();
                done?.Invoke(ok);
            });
        }

        /// <summary>
        /// A game was WON in this time: deliver it now, or queue it for the
        /// next sign-in. Losses never reach a leaderboard.
        /// </summary>
        public void SubmitWin(int boardSize, long timeMs)
        {
            string boardId = BoardIdFor(boardSize);

            if (!_backend.SignedIn)
            {
                Enqueue(boardId, timeMs);
                return;
            }

            _backend.Submit(boardId, timeMs, ok =>
            {
                if (!ok) Enqueue(boardId, timeMs); // deliver later, lose nothing
            });
        }

        /// <summary>Re-deliver every queued score (called after sign-in).</summary>
        public void FlushQueue()
        {
            if (!_backend.SignedIn || _queue.pending.Count == 0) return;

            var toSend = _queue.pending;
            _queue = new QueueState();
            SaveQueue();

            foreach (var score in toSend)
            {
                _backend.Submit(score.boardId, score.timeMs, ok =>
                {
                    if (!ok) Enqueue(score.boardId, score.timeMs);
                });
            }
        }

        void Enqueue(string boardId, long timeMs)
        {
            _queue.pending.Add(new PendingScore { boardId = boardId, timeMs = timeMs });
            SaveQueue();
        }

        void SaveQueue()
        {
            SaveService.SaveJson(QueueFile, _queue);
        }
    }
}
