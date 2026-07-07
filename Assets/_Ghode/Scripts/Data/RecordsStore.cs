// RecordsStore.cs — saves and loads the Records (best times + history) between
// sessions, exactly like SettingsStore does for Settings: PlayerPrefs + JSON,
// one versioned key, and a graceful fallback if the save is missing or broken.

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
        const string Key = "ghodekichaal.records.v1";

        /// <summary>Read the saved records, or a fresh empty set when none exist.</summary>
        public static Records Load()
        {
            if (!PlayerPrefs.HasKey(Key)) return new Records();

            try
            {
                var loaded = JsonUtility.FromJson<Records>(PlayerPrefs.GetString(Key));
                return loaded ?? new Records();
            }
            catch
            {
                // In plain words: a broken save file costs the player their
                // trophies (sad) but never their ability to play (important).
                return new Records();
            }
        }

        /// <summary>Write the records to disk right now.</summary>
        public static void Save(Records records)
        {
            if (records == null) return;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(records));
            PlayerPrefs.Save();
        }
    }
}
