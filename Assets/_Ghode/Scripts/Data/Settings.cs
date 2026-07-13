// Settings.cs — the player's choices that should survive closing the game.
// Just a small bag of values (board size, difficulty, sound on/off...).
// SettingsStore.cs is the one that actually saves and loads this bag.

using System;
using Ghode.Core;

namespace Ghode.Data
{
    /// <summary>
    /// Everything the player can toggle or pick, with sensible defaults for a
    /// brand-new install: 5×5 board, Knight difficulty, sound on, hints on,
    /// ambience off. Marked [Serializable] so JsonUtility can save it as text.
    /// </summary>
    [Serializable]
    public class Settings
    {
        /// <summary>Squares along one edge of the board. Default 5 (the 5×5 board).</summary>
        public int BoardSize = 5;

        /// <summary>How much move-highlighting help the player gets.</summary>
        public Difficulty Difficulty = Difficulty.Knight;

        /// <summary>Master switch for all sound effects.</summary>
        public bool Sound = true;

        /// <summary>Is the Hint button available during play?</summary>
        public bool Hints = true;

        /// <summary>Background ambience loop on/off. Off by default.</summary>
        public bool Ambience = false;

        /// <summary>
        /// Accessibility: when on, the horse snaps between squares instead of
        /// playing spawn/hop animations. Off by default.
        /// </summary>
        public bool ReducedMotion = false;
    }
}
