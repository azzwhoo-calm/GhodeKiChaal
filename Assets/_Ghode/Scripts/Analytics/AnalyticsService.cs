// AnalyticsService.cs — the game's event diary (M9).
// Gameplay code reports WHAT happened; the backend decides WHERE it goes.
// Today the backend logs locally (visible in the console and in tests);
// Firebase Analytics drops into the IAnalyticsBackend seam once the
// google-services.json exists — no call site changes.
//
// THE EVENT SCHEMA (keep this table in sync with any dashboard work):
//   session_start   { session_number }
//   screen_view     { screen }
//   game_start      { board_size, difficulty }
//   game_end        { board_size, difficulty, result(win|loss), moves,
//                     time_ms, hints_used }
//   hint_used       { board_size }
//   theme_selected  { theme }
//   iap             { step(started|completed|restored|failed) }
//   ad_shown        { rounds_since_ad }
//   sign_in         { success }
//
// TODO(azzwhoo): Firebase drop-in, once the Firebase project + Android app
// exist (see PLAY_CONSOLE_TODO.md):
//   1. Import FirebaseAnalytics.unitypackage (+ google-services.json under
//      Assets/). The Firebase SDK reads the json at build time.
//   2. Write FirebaseAnalyticsBackend : IAnalyticsBackend mapping Log() to
//      FirebaseAnalytics.LogEvent(name, Parameter[]).
//   3. Swap it in from GameController.ConnectServices(). Verify in DebugView.

using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ghode.Analytics
{
    /// <summary>What AnalyticsService needs from a real analytics SDK.</summary>
    public interface IAnalyticsBackend
    {
        /// <summary>Record one named event with its parameters.</summary>
        void Log(string eventName, IReadOnlyList<KeyValuePair<string, object>> parameters);
    }

    /// <summary>
    /// The stand-in backend: pretty-prints every event to the console and
    /// remembers them, so tests and editor sessions can assert on the stream.
    /// </summary>
    public class DebugAnalyticsBackend : IAnalyticsBackend
    {
        /// <summary>Every event logged this session, oldest first.</summary>
        public readonly List<(string name, Dictionary<string, object> parameters)> Events =
            new List<(string, Dictionary<string, object>)>();

        public void Log(string eventName, IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
            var bag = new Dictionary<string, object>();
            var text = new StringBuilder("[Analytics] ").Append(eventName);
            foreach (var p in parameters)
            {
                bag[p.Key] = p.Value;
                text.Append(' ').Append(p.Key).Append('=').Append(p.Value);
            }
            Events.Add((eventName, bag));
            Debug.Log(text.ToString());
        }
    }

    /// <summary>
    /// The one place gameplay code reports events. Method-per-event on
    /// purpose: call sites stay typo-proof and the schema lives here, not
    /// scattered across the codebase as string literals.
    /// </summary>
    public class AnalyticsService
    {
        IAnalyticsBackend _backend;

        public AnalyticsService(IAnalyticsBackend backend)
        {
            _backend = backend;
        }

        /// <summary>The backend in use (ConnectServices swaps in Firebase later).</summary>
        public IAnalyticsBackend Backend
        {
            get => _backend;
            set => _backend = value;
        }

        public void SessionStart(int sessionNumber) =>
            Log("session_start", P("session_number", sessionNumber));

        public void ScreenView(string screen) =>
            Log("screen_view", P("screen", screen));

        public void GameStart(int boardSize, string difficulty) =>
            Log("game_start", P("board_size", boardSize), P("difficulty", difficulty));

        public void GameEnd(int boardSize, string difficulty, bool won, int moves, long timeMs, int hintsUsed) =>
            Log("game_end",
                P("board_size", boardSize), P("difficulty", difficulty),
                P("result", won ? "win" : "loss"), P("moves", moves),
                P("time_ms", timeMs), P("hints_used", hintsUsed));

        public void HintUsed(int boardSize) =>
            Log("hint_used", P("board_size", boardSize));

        public void ThemeSelected(string theme) =>
            Log("theme_selected", P("theme", theme));

        public void Iap(string step) =>
            Log("iap", P("step", step));

        public void AdShown(int roundsSinceAd) =>
            Log("ad_shown", P("rounds_since_ad", roundsSinceAd));

        public void SignIn(bool success) =>
            Log("sign_in", P("success", success));

        static KeyValuePair<string, object> P(string key, object value) =>
            new KeyValuePair<string, object>(key, value);

        void Log(string name, params KeyValuePair<string, object>[] parameters)
        {
            // Analytics must never take the game down — swallow backend sins.
            try { _backend?.Log(name, parameters); }
            catch (System.Exception e) { Debug.LogWarning("Analytics: " + e.Message); }
        }
    }
}
