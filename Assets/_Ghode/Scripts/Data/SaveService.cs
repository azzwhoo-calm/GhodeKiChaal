// SaveService.cs — the one place that touches save FILES on disk.
// Saves live as small JSON files in Application.persistentDataPath:
//   settings.v1.json, records.v1.json (entitlements.v1.json will join later).
// Three promises, straight from the Tech Design Doc:
//   1. ATOMIC writes: write a .tmp file, then swap it in — a crash or
//      battery-pull mid-save can never leave a half-written main file.
//   2. Corrupt files never crash the game: they are QUARANTINED (renamed to
//      *.bad, kept for post-mortems) and the caller gets defaults instead.
//   3. Schema gating: every file carries a schemaVersion int; a mismatch is
//      treated as corrupt (until a real migration exists for it).

using System;
using System.IO;
using UnityEngine;

namespace Ghode.Data
{
    /// <summary>
    /// Low-level save-file plumbing shared by SettingsStore and RecordsStore:
    /// read-or-null, validate-or-quarantine, and atomic write. Stores decide
    /// WHAT the data means; this class only makes disk IO safe and boring.
    /// </summary>
    public static class SaveService
    {
        /// <summary>
        /// Tests point this at a scratch folder so they never touch a real
        /// device's saves. Null/empty = the real persistentDataPath.
        /// </summary>
        public static string RootOverride;

        static string Root
        {
            get
            {
                return string.IsNullOrEmpty(RootOverride)
                    ? Application.persistentDataPath
                    : RootOverride;
            }
        }

        /// <summary>Full path a save file lives at (test-redirect aware).</summary>
        public static string PathFor(string fileName)
        {
            return Path.Combine(Root, fileName);
        }

        /// <summary>
        /// Load and parse one JSON save file.
        /// Returns null when the file simply does not exist (first launch).
        /// Anything WRONG — unreadable, unparsable, or failing
        /// <paramref name="isValid"/> (e.g. wrong schemaVersion) — quarantines
        /// the file to *.bad and also returns null, so callers fall back to
        /// defaults without ever crashing.
        /// </summary>
        public static T LoadJson<T>(string fileName, Func<T, bool> isValid) where T : class
        {
            string path = PathFor(fileName);
            string json;
            try
            {
                if (!File.Exists(path)) return null;
                json = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveService: cannot read {fileName} ({e.Message}) — quarantining.");
                Quarantine(fileName);
                return null;
            }

            T data;
            try
            {
                data = JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveService: {fileName} is not valid JSON ({e.Message}) — quarantining.");
                Quarantine(fileName);
                return null;
            }

            if (data == null || (isValid != null && !isValid(data)))
            {
                Debug.LogWarning($"SaveService: {fileName} failed validation (wrong schema?) — quarantining.");
                Quarantine(fileName);
                return null;
            }
            return data;
        }

        /// <summary>
        /// Write one JSON save file atomically: serialize, write to a
        /// sibling .tmp, then swap the .tmp into place. The main file is
        /// always either the old complete version or the new complete one.
        /// </summary>
        public static void SaveJson<T>(string fileName, T data)
        {
            if (data == null) return;

            string path = PathFor(fileName);
            string tmp = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Root);
                File.WriteAllText(tmp, JsonUtility.ToJson(data, prettyPrint: true));

                // In plain words: the swap. Replace keeps the operation atomic
                // where the OS supports it; Move covers the very first save.
                if (File.Exists(path)) File.Replace(tmp, path, null);
                else File.Move(tmp, path);
            }
            catch (Exception e)
            {
                // A failed save must never take the game down — worst case the
                // player keeps yesterday's file. The .tmp (if any) is cleaned
                // up so it cannot shadow future saves.
                Debug.LogWarning($"SaveService: failed to save {fileName} ({e.Message}).");
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* best effort */ }
            }
        }

        // Move a broken file aside as <name>.bad (replacing any older .bad):
        // out of the load path, but still on disk if we ever want to inspect
        // what went wrong on a player's device.
        static void Quarantine(string fileName)
        {
            string path = PathFor(fileName);
            string bad = path + ".bad";
            try
            {
                if (!File.Exists(path)) return;
                if (File.Exists(bad)) File.Delete(bad);
                File.Move(path, bad);
            }
            catch (Exception e)
            {
                // Even quarantine can fail (locked file?) — delete as plan B,
                // because a poisoned file must not brick every future launch.
                Debug.LogWarning($"SaveService: quarantine of {fileName} failed ({e.Message}) — deleting.");
                try { File.Delete(path); } catch { /* give up gracefully */ }
            }
        }
    }
}
