// SettingsStore.cs — saves and loads the player's Settings between sessions.
// Since Tier 4 the data lives in settings.v1.json inside persistentDataPath,
// written atomically by SaveService (corrupt file → quarantined + defaults).
// Saves made by older builds (which used PlayerPrefs) are imported once,
// then the legacy key is deleted.

using UnityEngine;

namespace Ghode.Data
{
    /// <summary>
    /// Load and save <see cref="Settings"/>. If nothing was ever saved (or the
    /// saved file is somehow broken), you simply get fresh default settings —
    /// the game never crashes over a bad save.
    /// </summary>
    public static class SettingsStore
    {
        // The versioned file name IS the contract: a future format bump means
        // a new file (settings.v2.json) plus a migration from this one.
        const string FileName = "settings.v1.json";

        // Pre-Tier-4 builds kept the same JSON under this PlayerPrefs key.
        const string LegacyPrefsKey = "ghodekichaal.settings.v1";

        /// <summary>Read the saved settings, or defaults when there are none.</summary>
        public static Settings Load()
        {
            var loaded = SaveService.LoadJson<Settings>(FileName,
                s => s.schemaVersion == Settings.CurrentSchema);
            if (loaded != null) return loaded;

            // First run since the file-based saves landed? Rescue the old
            // PlayerPrefs copy so nobody loses their choices to an update.
            var imported = ImportLegacy();
            return imported ?? new Settings();
        }

        /// <summary>Write the settings to disk right now (atomically).</summary>
        public static void Save(Settings settings)
        {
            if (settings == null) return;
            settings.schemaVersion = Settings.CurrentSchema;
            SaveService.SaveJson(FileName, settings);
        }

        // One-time migration: parse the legacy PlayerPrefs JSON, persist it as
        // the new file, and delete the key so this never runs again.
        static Settings ImportLegacy()
        {
            if (!PlayerPrefs.HasKey(LegacyPrefsKey)) return null;

            Settings imported = null;
            try
            {
                imported = JsonUtility.FromJson<Settings>(PlayerPrefs.GetString(LegacyPrefsKey));
            }
            catch
            {
                // A broken legacy blob is not worth saving — fall through.
            }

            PlayerPrefs.DeleteKey(LegacyPrefsKey);
            PlayerPrefs.Save();

            if (imported == null) return null;
            Save(imported);
            return imported;
        }
    }
}
