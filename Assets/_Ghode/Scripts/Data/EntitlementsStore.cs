// EntitlementsStore.cs — saves and loads the Entitlements cache, exactly like
// SettingsStore does for Settings: one versioned JSON file, atomic writes via
// SaveService, quarantine-and-defaults on corruption.
// Losing this file is never fatal: the real store re-delivers purchases on
// the next connect (and the Restore button exists for impatient players).

namespace Ghode.Data
{
    /// <summary>
    /// Load and save <see cref="Entitlements"/>. Missing or corrupted data
    /// simply reads as "nothing owned" until billing reconnects.
    /// </summary>
    public static class EntitlementsStore
    {
        const string FileName = "entitlements.v1.json";

        /// <summary>Read the cached entitlements, or an empty set when none exist.</summary>
        public static Entitlements Load()
        {
            var loaded = SaveService.LoadJson<Entitlements>(FileName,
                e => e.schemaVersion == Entitlements.CurrentSchema);
            return loaded ?? new Entitlements();
        }

        /// <summary>Write the cache to disk right now (atomically).</summary>
        public static void Save(Entitlements entitlements)
        {
            if (entitlements == null) return;
            entitlements.schemaVersion = Entitlements.CurrentSchema;
            SaveService.SaveJson(FileName, entitlements);
        }
    }
}
