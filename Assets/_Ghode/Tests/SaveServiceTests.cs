// SaveServiceTests.cs — EditMode tests for the save-file plumbing:
// atomic writes, corrupt-file quarantine, schema gating, and the one-time
// PlayerPrefs → file migration in the two stores. Every test runs against a
// throwaway folder via SaveService.RootOverride — a real device's saves are
// never touched.

using System.IO;
using NUnit.Framework;
using UnityEngine;
using Ghode.Core;
using Ghode.Data;

namespace Ghode.Tests
{
    /// <summary>
    /// Tests for <see cref="SaveService"/>, <see cref="SettingsStore"/> and
    /// <see cref="RecordsStore"/>: the three disk promises (atomic, corrupt →
    /// quarantine + defaults, schema gate) plus legacy import.
    /// </summary>
    public class SaveServiceTests
    {
        const string SettingsFile = "settings.v1.json";
        const string LegacySettingsKey = "ghodekichaal.settings.v1";
        const string LegacyRecordsKey = "ghodekichaal.records.v1";

        string _root;
        string _legacySettingsBackup;
        string _legacyRecordsBackup;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "GhodeSaveTests_" + System.Guid.NewGuid().ToString("N"));
            SaveService.RootOverride = _root;

            // Park any real legacy prefs so import tests are deterministic.
            _legacySettingsBackup = PlayerPrefs.HasKey(LegacySettingsKey) ? PlayerPrefs.GetString(LegacySettingsKey) : null;
            _legacyRecordsBackup = PlayerPrefs.HasKey(LegacyRecordsKey) ? PlayerPrefs.GetString(LegacyRecordsKey) : null;
            PlayerPrefs.DeleteKey(LegacySettingsKey);
            PlayerPrefs.DeleteKey(LegacyRecordsKey);
        }

        [TearDown]
        public void TearDown()
        {
            SaveService.RootOverride = null;
            try { Directory.Delete(_root, recursive: true); } catch { /* never existed */ }

            if (_legacySettingsBackup != null) PlayerPrefs.SetString(LegacySettingsKey, _legacySettingsBackup);
            else PlayerPrefs.DeleteKey(LegacySettingsKey);
            if (_legacyRecordsBackup != null) PlayerPrefs.SetString(LegacyRecordsKey, _legacyRecordsBackup);
            else PlayerPrefs.DeleteKey(LegacyRecordsKey);
            PlayerPrefs.Save();
        }

        // ------------------------------------------------------------------
        // SaveService fundamentals
        // ------------------------------------------------------------------

        [Test]
        public void SaveThenLoad_RoundTrips()
        {
            var settings = new Settings { BoardSize = 8, Difficulty = Difficulty.Master, Sound = false };
            SettingsStore.Save(settings);

            Assert.IsTrue(File.Exists(Path.Combine(_root, SettingsFile)), "The save must be a real file.");
            Assert.IsFalse(File.Exists(Path.Combine(_root, SettingsFile + ".tmp")),
                "The atomic-write scratch file must never linger.");

            var loaded = SettingsStore.Load();
            Assert.AreEqual(8, loaded.BoardSize);
            Assert.AreEqual(Difficulty.Master, loaded.Difficulty);
            Assert.IsFalse(loaded.Sound);
        }

        [Test]
        public void MissingFile_GivesDefaults_WithoutCreatingAnything()
        {
            var loaded = SettingsStore.Load();

            Assert.AreEqual(5, loaded.BoardSize, "Fresh install = default settings.");
            Assert.IsFalse(File.Exists(Path.Combine(_root, SettingsFile)),
                "Just LOOKING at settings must not write a file.");
        }

        [Test]
        public void CorruptFile_IsQuarantined_AndDefaultsReturned()
        {
            Directory.CreateDirectory(_root);
            string path = Path.Combine(_root, SettingsFile);
            File.WriteAllText(path, "{ this is not json !!!");

            var loaded = SettingsStore.Load();

            Assert.AreEqual(5, loaded.BoardSize, "Corruption must cost the file, never the game.");
            Assert.IsFalse(File.Exists(path), "The broken file must leave the load path.");
            Assert.IsTrue(File.Exists(path + ".bad"), "…but stay on disk as .bad for post-mortems.");
        }

        [Test]
        public void WrongSchema_IsTreatedAsCorrupt()
        {
            // A v99 file (from some hypothetical future build) must not be
            // half-understood — quarantine it and start clean.
            Directory.CreateDirectory(_root);
            string path = Path.Combine(_root, SettingsFile);
            File.WriteAllText(path, "{\"schemaVersion\":99,\"BoardSize\":8}");

            var loaded = SettingsStore.Load();

            Assert.AreEqual(5, loaded.BoardSize);
            Assert.IsTrue(File.Exists(path + ".bad"));
        }

        [Test]
        public void SavingTwice_ReplacesAtomically()
        {
            SettingsStore.Save(new Settings { BoardSize = 6 });
            SettingsStore.Save(new Settings { BoardSize = 8 }); // exercises File.Replace

            Assert.AreEqual(8, SettingsStore.Load().BoardSize);
            Assert.IsFalse(File.Exists(Path.Combine(_root, SettingsFile + ".tmp")));
        }

        // ------------------------------------------------------------------
        // Legacy PlayerPrefs import (one-time, both stores)
        // ------------------------------------------------------------------

        [Test]
        public void LegacySettings_AreImportedOnce_ThenKeyDeleted()
        {
            PlayerPrefs.SetString(LegacySettingsKey,
                "{\"BoardSize\":8,\"Difficulty\":2,\"Sound\":false,\"Hints\":true,\"Ambience\":false}");

            var loaded = SettingsStore.Load();

            Assert.AreEqual(8, loaded.BoardSize, "The old PlayerPrefs save must carry over.");
            Assert.AreEqual(Difficulty.Master, loaded.Difficulty);
            Assert.IsFalse(PlayerPrefs.HasKey(LegacySettingsKey), "Import happens exactly once.");
            Assert.IsTrue(File.Exists(Path.Combine(_root, SettingsFile)),
                "The import must be persisted as the new file immediately.");
        }

        [Test]
        public void LegacyRecords_AreImportedWithBestTimes()
        {
            var legacy = new Records();
            legacy.RecordGame(new GameRecord { boardSize = 5, won = true, timeMs = 42000 });
            PlayerPrefs.SetString(LegacyRecordsKey, JsonUtility.ToJson(legacy));

            var loaded = RecordsStore.Load();

            Assert.AreEqual(42000, loaded.BestTimeFor(5));
            Assert.AreEqual(1, loaded.recent.Count);
            Assert.IsFalse(PlayerPrefs.HasKey(LegacyRecordsKey));
        }

        [Test]
        public void FileBeatsLegacy_WhenBothExist()
        {
            // Once a file exists the legacy key must be ignored (it is stale).
            SettingsStore.Save(new Settings { BoardSize = 6 });
            PlayerPrefs.SetString(LegacySettingsKey, "{\"BoardSize\":8}");

            Assert.AreEqual(6, SettingsStore.Load().BoardSize);
        }
    }
}
