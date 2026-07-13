// HapticsService.cs — the game's fingertip feedback, one buzz at a time.
// The design doc's haptic map:
//   Hop / place / buttons → a light tick
//   Invalid tap           → a double "nope" pulse
//   Win                   → short-short-long celebration
//   Lose                  → one soft thud
// On Android it talks to the OS Vibrator with amplitude-controlled
// VibrationEffects (always available — our min API is 29, effects need 26).
// Everywhere else (editor, tests) every call is a silent no-op, and the
// master switch mirrors the player's Haptics setting.

using UnityEngine;

namespace Ghode.Haptics
{
    /// <summary>The named buzzes the game can ask for.</summary>
    public enum Haptic
    {
        Tick,   // light: a hop landing, the horse set down, a button press
        Reject, // double pulse: that square is not a legal hop
        Win,    // short-short-long: the tour is complete
        Lose    // soft thud: stuck with no hops left
    }

    /// <summary>
    /// Static vibration desk. <see cref="Enabled"/> is driven by the Haptics
    /// setting (GameController keeps them in sync); when off — or when not
    /// running on a real Android device — every call does nothing.
    /// </summary>
    public static class HapticsService
    {
        /// <summary>Master switch, mirroring Settings.Haptics.</summary>
        public static bool Enabled = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject _vibrator;
        static bool _lookedUp;

        static AndroidJavaObject Vibrator
        {
            get
            {
                if (_lookedUp) return _vibrator;
                _lookedUp = true;
                try
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    }
                }
                catch (System.Exception e)
                {
                    // A device with no vibrator (some tablets) — stay silent forever.
                    Debug.LogWarning("HapticsService: no vibrator available (" + e.Message + ")");
                    _vibrator = null;
                }
                return _vibrator;
            }
        }

        static void OneShot(long ms, int amplitude)
        {
            var vibrator = Vibrator;
            if (vibrator == null) return;
            try
            {
                using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude))
                {
                    vibrator.Call("vibrate", effect);
                }
            }
            catch (System.Exception) { /* haptics must never crash gameplay */ }
        }

        static void Waveform(long[] timings, int[] amplitudes)
        {
            var vibrator = Vibrator;
            if (vibrator == null) return;
            try
            {
                using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                using (var effect = effectClass.CallStatic<AndroidJavaObject>(
                    "createWaveform", timings, amplitudes, -1)) // -1 = play once, no repeat
                {
                    vibrator.Call("vibrate", effect);
                }
            }
            catch (System.Exception) { /* haptics must never crash gameplay */ }
        }
#endif

        /// <summary>Play one named buzz (no-op when disabled or off-device).</summary>
        public static void Play(Haptic haptic)
        {
            if (!Enabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            switch (haptic)
            {
                case Haptic.Tick:
                    OneShot(15, 80);
                    break;

                case Haptic.Reject:
                    // In plain words: buzz-pause-buzz, like a small head-shake.
                    Waveform(new long[] { 0, 30, 60, 30 }, new[] { 0, 130, 0, 130 });
                    break;

                case Haptic.Win:
                    // short, short, LONG — the same rhythm as the win jingle.
                    Waveform(new long[] { 0, 25, 70, 25, 70, 120 }, new[] { 0, 140, 0, 140, 0, 220 });
                    break;

                case Haptic.Lose:
                    OneShot(45, 60);
                    break;
            }
#endif
        }
    }
}
