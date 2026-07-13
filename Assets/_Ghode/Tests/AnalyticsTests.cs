// AnalyticsTests.cs — EditMode tests for the event diary: the schema's
// method-per-event surface, and (through a real GameController) that gameplay
// actually reports what happened — a dashboard is only as good as its feed.

using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Ghode.Analytics;
using Ghode.Audio;
using Ghode.Core;
using Ghode.Data;
using Ghode.Game;

namespace Ghode.Tests
{
    /// <summary>Tests for <see cref="AnalyticsService"/> and its controller wiring.</summary>
    public class AnalyticsTests
    {
        string _root;
        GameObject _go;
        GameController _gc;
        DebugAnalyticsBackend _backend;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "GhodeAnalyticsTests_" + System.Guid.NewGuid().ToString("N"));
            SaveService.RootOverride = _root;

            _go = new GameObject("AnalyticsTests");
            var audio = _go.AddComponent<AudioManager>();
            _gc = _go.AddComponent<GameController>();
            _gc.Init(audio);

            // Swap in a fresh backend AFTER Init so each test reads a clean stream.
            _backend = new DebugAnalyticsBackend();
            _gc.Analytics.Backend = _backend;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            SaveService.RootOverride = null;
            try { Directory.Delete(_root, recursive: true); } catch { /* never created */ }
        }

        (string name, System.Collections.Generic.Dictionary<string, object> parameters)? Find(string eventName)
        {
            foreach (var e in _backend.Events)
            {
                if (e.name == eventName) return e;
            }
            return null;
        }

        [Test]
        public void GameStart_ReportsSizeAndDifficulty()
        {
            _gc.SetDifficulty(Difficulty.Master);
            _gc.NewGame(7);

            var e = Find("game_start");
            Assert.IsNotNull(e);
            Assert.AreEqual(7, e.Value.parameters["board_size"]);
            Assert.AreEqual("Master", e.Value.parameters["difficulty"]);
        }

        [Test]
        public void GameEnd_ReportsTheFullStory_OnALoss()
        {
            // The verified 4-hop trap, then walk away — an "abandoned" loss.
            _gc.NewGame(5);
            _gc.OnCellTapped(2, 1);
            _gc.OnCellTapped(3, 3);
            _gc.OnCellTapped(1, 2);
            _gc.OnCellTapped(0, 0);
            _gc.GoMenu();

            var e = Find("game_end");
            Assert.IsNotNull(e);
            Assert.AreEqual("loss", e.Value.parameters["result"]);
            Assert.AreEqual(5, e.Value.parameters["board_size"]);
            Assert.AreEqual(3, e.Value.parameters["moves"]);
        }

        [Test]
        public void HintUsed_IsCounted()
        {
            _gc.NewGame(5);
            _gc.OnCellTapped(2, 2);
            _gc.UseHint();

            Assert.IsNotNull(Find("hint_used"));
        }

        [Test]
        public void ScreenViews_FollowNavigation()
        {
            _gc.OpenInstructions();
            _gc.CloseInstructions();

            var screens = _backend.Events
                .Where(e => e.name == "screen_view")
                .Select(e => e.parameters["screen"].ToString())
                .ToArray();
            CollectionAssert.Contains(screens, "Instructions");
            CollectionAssert.Contains(screens, "Menu");
        }

        [Test]
        public void EntitlementArrival_LogsIapCompleted_ExactlyOnce()
        {
            _gc.ApplyEntitlement(true);
            _gc.ApplyEntitlement(true); // duplicate delivery (store re-send)

            Assert.AreEqual(1, _backend.Events.Count(e => e.name == "iap"),
                "A re-delivered purchase must not double-log.");
            Assert.AreEqual("completed", Find("iap").Value.parameters["step"]);
        }

        [Test]
        public void BackendExplosions_NeverReachGameplay()
        {
            _gc.Analytics.Backend = new ExplodingBackend();

            // If the try/catch in AnalyticsService is missing, this throws
            // and the test fails — analytics must never take the game down.
            Assert.DoesNotThrow(() => _gc.NewGame(5));
        }

        class ExplodingBackend : IAnalyticsBackend
        {
            public void Log(string eventName,
                System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, object>> parameters)
            {
                throw new System.InvalidOperationException("backend on fire");
            }
        }
    }
}
