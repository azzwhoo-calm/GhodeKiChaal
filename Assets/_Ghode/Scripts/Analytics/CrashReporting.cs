// CrashReporting.cs — the black box recorder.
// Every unhandled exception is appended to crashes.log in persistentDataPath
// (newest last, capped at 20 entries) so a tester can send us the file long
// before Crashlytics exists. It never uploads anything — it only remembers.
//
// TODO(azzwhoo): Crashlytics drop-in, once the Firebase project exists (see
// PLAY_CONSOLE_TODO.md): import FirebaseCrashlytics.unitypackage — it hooks
// unhandled exceptions by itself, no call-site changes here. Keep this local
// log anyway; it works offline and in the editor. Then do the plan's
// "test crash symbolicated" check on a device build.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ghode.Analytics
{
    /// <summary>
    /// Local crash logging: hooks Unity's log callback once and persists the
    /// most recent unhandled exceptions. Ask <see cref="LogPath"/> where the
    /// file lives (testers can attach it to a bug report).
    /// </summary>
    public static class CrashReporting
    {
        const int MaxEntries = 20;
        const string Separator = "\n---- crash ----\n";

        static bool _hooked;
        static string _logPath;
        static readonly object _fileLock = new object();

        /// <summary>Where the crash log lives on this device.</summary>
        public static string LogPath =>
            Path.Combine(Application.persistentDataPath, "crashes.log");

        /// <summary>Start recording (idempotent; GameBootstrap calls it once).</summary>
        public static void Init()
        {
            if (_hooked) return;
            _hooked = true;

            // persistentDataPath is main-thread-only — capture it NOW, because
            // the interesting crashes arrive from background threads.
            _logPath = LogPath;

            // The THREADED callback, deliberately: plain logMessageReceived
            // only hears the main thread, and a worker-thread exception is
            // exactly the crash we most want on record (verified by test —
            // the main-thread-only variant missed it).
            Application.logMessageReceivedThreaded += OnLogMessage;
        }

        static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception) return;

            try
            {
                string entry = DateTime.UtcNow.ToString("o") + "\n" + condition + "\n" + stackTrace;

                // Threaded callback = concurrent crashes are possible; the
                // lock keeps two writers from shredding the file.
                lock (_fileLock)
                {
                    // In plain words: keep only the newest MaxEntries crashes —
                    // a crash LOOP must not grow a gigabyte file on a phone.
                    var entries = new List<string>();
                    if (File.Exists(_logPath))
                    {
                        entries.AddRange(File.ReadAllText(_logPath)
                            .Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries));
                    }
                    entries.Add(entry);
                    if (entries.Count > MaxEntries)
                    {
                        entries.RemoveRange(0, entries.Count - MaxEntries);
                    }

                    File.WriteAllText(_logPath, string.Join(Separator, entries) + Separator);
                }
            }
            catch
            {
                // The crash logger must never crash. Full stop.
            }
        }
    }
}
