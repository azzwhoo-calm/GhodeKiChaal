// GameTimer.cs — a pause-safe stopwatch (the Unity twin of our web useTimer).
// It banks time whenever it is paused, so pausing, resuming, or opening menus
// never loses or double-counts a millisecond. Plain class, not a MonoBehaviour:
// GameController owns one and asks it for the time whenever the HUD repaints.

using System;
using UnityEngine;

namespace Ghode.Game
{
    /// <summary>
    /// An accumulating stopwatch: Reset → Start → (Pause/Start as often as you
    /// like) → ReadMs whenever. Uses real clock time, so Unity's timeScale and
    /// editor pauses cannot skew it.
    /// </summary>
    public class GameTimer
    {
        /// <summary>
        /// Where "now" comes from, in seconds. Defaults to Unity's real clock;
        /// tests swap in a fake so they can fast-forward without sleeping.
        /// </summary>
        public Func<double> TimeSource { get; set; } = DefaultTimeSource;

        double _bankedMs;   // time collected during previous running stretches
        double _runStartAt; // real-clock moment the current stretch began
        bool _running;

        static double DefaultTimeSource()
        {
            return Time.realtimeSinceStartupAsDouble;
        }

        /// <summary>Is the stopwatch ticking right now?</summary>
        public bool IsRunning => _running;

        /// <summary>Back to 0:00 and stopped. Call when a new game begins.</summary>
        public void Reset()
        {
            _bankedMs = 0;
            _running = false;
        }

        /// <summary>Begin (or resume) ticking. Safe to call while already running.</summary>
        public void Start()
        {
            if (_running) return;
            _runStartAt = TimeSource();
            _running = true;
        }

        /// <summary>
        /// Stop ticking and bank what we have so far. Safe to call while paused.
        /// </summary>
        public void Pause()
        {
            if (!_running) return;
            // In plain words: pour the current stretch into the piggy bank.
            _bankedMs += (TimeSource() - _runStartAt) * 1000.0;
            _running = false;
        }

        /// <summary>Total elapsed play time in milliseconds, pause-safe.</summary>
        public long ReadMs()
        {
            double live = _running
                ? (TimeSource() - _runStartAt) * 1000.0
                : 0.0;
            return (long)(_bankedMs + live);
        }
    }
}
