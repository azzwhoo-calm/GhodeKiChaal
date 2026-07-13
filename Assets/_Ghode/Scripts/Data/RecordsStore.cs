// RecordsStore.cs — saves and loads the Records (best times + history)
// between sessions, exactly like SettingsStore does for Settings: one
// versioned JSON file (records.v1.json), atomic writes via SaveService,
// quarantine-and-defaults on corruption, and a one-time import of the
// legacy PlayerPrefs save from pre-Tier-4 builds.

using UnityEngine;

namespace Ghode.Data
{
    /// <summary>
    /// Load and save <see cref="Records"/>. Missing or corrupted data simply
    /// gives you an empty trophy shelf — never a crash.
    /// </summary>
    public static class RecordsStore
    {
        // TODO(azzwhoo): cross-check field names against our Save-Data Spec doc
        // before finalizing the schema — if the web version's field names differ
        // (e.g. "bestMs" vs "timeMs"), decide now so exports/imports line up.
        const string FileName = "records.v1.json";

        // Pre-Tier-4 builds kept the same JSON under this PlayerPrefs key.
        const string LegacyPrefsKey = "ghodekichaal.records.v1";

        /// <summary>Read the saved records, or a fresh empty set when none exist.</summary>
        public static Records Load()
        {
            var loaded = SaveService.LoadJson<Records>(FileName,
                r => r.schemaVersion == Records.CurrentSchema);
            if (loaded != null) return loaded;

            var imported = ImportLegacy();
            return imported ?? new Records();
        }

        /// <summary>Write the records to disk right now (atomically).</summary>
        public static void Save(Records records)
        {
            if (records == null) return;
            records.schemaVersion = Records.CurrentSchema;
            SaveService.SaveJson(FileName, records);
        }

        // One-time migration from the PlayerPrefs era; see SettingsStore.
        static Records ImportLegacy()
        {
            if (!PlayerPrefs.HasKey(LegacyPrefsKey)) return null;

            Records imported = null;
            try
            {
                imported = JsonUtility.FromJson<Records>(PlayerPrefs.GetString(LegacyPrefsKey));
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
