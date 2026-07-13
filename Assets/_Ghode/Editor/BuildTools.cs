// BuildTools.cs — one-button (and one-command) Android release builds.
// Editor menu:  Ghode → Build Android AAB
// Command line: Unity.exe -batchmode -quit -projectPath <repo>
//                 -executeMethod Ghode.EditorTools.BuildTools.BuildAndroidAab
//                 -logFile Builds/build.log
// Every build gets a fresh, MONOTONIC versionCode derived from UTC time
// (15-minute buckets since 2026-01-01), so "bump the versionCode" can never
// be forgotten and two same-day builds never collide. The AAB lands in
// Builds/ (git-ignored) named after version + code, so archives self-label.

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Ghode.EditorTools
{
    /// <summary>Reproducible Android App Bundle builds for the Play Console.</summary>
    public static class BuildTools
    {
        // versionCode epoch. 15-minute buckets keep the value inside int for
        // roughly 60,000 years, which should cover the v1.1 backlog.
        static readonly DateTime Epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [MenuItem("Ghode/Build Android AAB")]
        public static void BuildMenuItem()
        {
            BuildAndroidAab();
        }

        /// <summary>Build the release AAB (also the -executeMethod entry point).</summary>
        public static void BuildAndroidAab()
        {
            int versionCode = (int)((DateTime.UtcNow - Epoch).TotalMinutes / 15.0);
            PlayerSettings.Android.bundleVersionCode = versionCode;

            if (!PlayerSettings.Android.useCustomKeystore)
            {
                // A debug-signed AAB uploads nowhere — say so loudly, but let
                // local test builds through.
                Debug.LogWarning("BuildTools: no custom keystore configured — this AAB is " +
                    "DEBUG-SIGNED and cannot be uploaded to the Play Console. " +
                    "Run Tools/create-release-keystore.ps1 and select the keystore in " +
                    "Player Settings → Publishing Settings first.");
            }

            EditorUserBuildSettings.buildAppBundle = true;
            // Public symbols let the Play Console symbolicate native crashes.
            SetSymbolsPublicIfAvailable();

            string output = Path.Combine("Builds",
                $"GhodeKiChaal-{PlayerSettings.bundleVersion}-{versionCode}.aab");
            Directory.CreateDirectory("Builds");

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            bool ok = report.summary.result == BuildResult.Succeeded;

            if (ok)
            {
                Debug.Log($"BuildTools: SUCCESS — {output} " +
                    $"(versionCode {versionCode}, {report.summary.totalSize / (1024 * 1024)} MB)");
            }
            else
            {
                Debug.LogError($"BuildTools: FAILED — {report.summary.result}, " +
                    $"{report.summary.totalErrors} error(s). See the editor/build log.");
            }

            // In batch mode the exit code IS the report — CI reads it.
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(ok ? 0 : 1);
            }
        }

        // AndroidCreateSymbols moved namespaces across Unity versions; feature-
        // detect via reflection so this file survives editor upgrades.
        static void SetSymbolsPublicIfAvailable()
        {
            var property = typeof(EditorUserBuildSettings).GetProperty("androidCreateSymbols");
            if (property == null) return;
            var enumType = property.PropertyType;
            var publicValue = Enum.GetNames(enumType).FirstOrDefault(n => n == "Public");
            if (publicValue != null)
            {
                property.SetValue(null, Enum.Parse(enumType, publicValue));
            }
        }
    }
}
