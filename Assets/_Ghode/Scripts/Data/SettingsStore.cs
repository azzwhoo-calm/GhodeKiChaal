// SettingsStore.cs — saves and loads the player's Settings between sessions.
// Uses Unity's PlayerPrefs (a tiny key→text cupboard the OS keeps for us)
// with the settings turned into a JSON string. One key, versioned, so a
// future format change can migrate cleanly.

using UnityEngine;

namespace Ghode.Data
{
    /// <summary>
    /// Load and save <see cref="Settings"/>. If nothing was ever saved (or the
    /// saved text is somehow broken), you simply get fresh default settings —
    /// the game never crashes over a bad save.
    /// </summary>
    public static class SettingsStore
    {
        // Versioned key: if we ever change the shape of Settings, we bump to
        // .v2 and write a migration instead of corrupting old installs.
        const string Key = "ghodekichaal.settings.v1";

        /// <summary>Read the saved settings, or defaults when there are none.</summary>
        public static Settings Load()
        {
            if (!PlayerPrefs.HasKey(Key)) return new Settings();

            try
            {
                // In plain words: turn the saved text back into a Settings object.
                var loaded = JsonUtility.FromJson<Settings>(PlayerPrefs.GetString(Key));
                return loaded ?? new Settings();
            }
            catch
            {
                // A corrupted save should never break the game — start fresh.
                return new Settings();
            }
        }

        /// <summary>Write the settings to disk right now.</summary>
        public static void Save(Settings settings)
        {
            if (settings == null) return;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(settings));
            PlayerPrefs.Save(); // flush immediately — mobile apps can die any second
        }
    }
}
