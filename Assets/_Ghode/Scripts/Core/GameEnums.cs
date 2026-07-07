// GameEnums.cs — the little "state words" the whole game shares.
// This file holds three tiny lists of named choices (enums). Every other script
// uses these words to agree on what is happening right now — no magic numbers,
// no guessing. Pure C#: nothing here touches Unity at all.

namespace Ghode.Core
{
    /// <summary>
    /// What stage the puzzle itself is in.
    /// Placing = the board is empty and we are waiting for the first tap.
    /// Playing = the horse is down and hopping around.
    /// Won = every square has been visited. Hooray!
    /// Lost = the horse is stuck: squares remain, but no legal hop is left.
    /// </summary>
    public enum Phase
    {
        Placing,
        Playing,
        Won,
        Lost
    }

    /// <summary>
    /// Which full-screen page the player is looking at.
    /// Exactly one of these is visible at a time (ScreenManager enforces that).
    /// </summary>
    public enum Screen
    {
        Menu,
        Instructions,
        Playing,
        Result
    }

    /// <summary>
    /// How much help the game gives while playing.
    /// Apprentice = shows every legal hop AND the smartest one (Warnsdorff's pick).
    /// Knight = shows every legal hop, but you pick which is smart.
    /// Master = shows nothing — you spot the L-shapes yourself.
    /// </summary>
    public enum Difficulty
    {
        Apprentice,
        Knight,
        Master
    }
}
