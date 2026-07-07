// BoardState.cs — the memory of one game in progress.
// It remembers which squares the horse has stamped, in what order, where the
// horse stands right now, and whether the game is won, lost, or still going.
// Pure C# (no Unity), so tests can create and poke boards freely.

using System.Collections.Generic;

namespace Ghode.Core
{
    /// <summary>
    /// Plain data for one puzzle attempt: the board size, every visited square
    /// with its move number, the square the horse is standing on, the current
    /// <see cref="Phase"/>, how many hints were used, and enough history to Undo.
    /// </summary>
    public class BoardState
    {
        /// <summary>How many squares along one edge (5, 6 or 8).</summary>
        public int Size { get; }

        /// <summary>Where in the game we are: Placing, Playing, Won or Lost.</summary>
        public Phase Phase { get; private set; }

        /// <summary>How many times the player asked for a hint this game.</summary>
        public int HintsUsed { get; private set; }

        // In plain words: one number per square, laid out row by row.
        // 0 means "never visited". 1 means "the starting square". 2 means the
        // first hop landed here, and so on. This doubles as our path history.
        readonly int[] _moveNumber;

        // The exact trail of squares in the order they were visited.
        // The LAST entry is the square the horse is standing on right now.
        readonly List<(int r, int c)> _path;

        /// <summary>Make a fresh, empty board waiting for the first tap.</summary>
        public BoardState(int size)
        {
            Size = size;
            Phase = Phase.Placing;
            HintsUsed = 0;
            _moveNumber = new int[size * size];
            _path = new List<(int r, int c)>();
        }

        /// <summary>The square the horse is standing on, or null before placement.</summary>
        public (int r, int c)? Current
        {
            get
            {
                if (_path.Count == 0) return null;
                return _path[_path.Count - 1];
            }
        }

        /// <summary>Total squares on the board (Size × Size).</summary>
        public int TotalCells => Size * Size;

        /// <summary>How many squares have been stamped so far.</summary>
        public int VisitedCount => _path.Count;

        /// <summary>The full trail so far, oldest square first (read-only).</summary>
        public IReadOnlyList<(int r, int c)> Path => _path;

        /// <summary>The move number stamped on square (r, c), or 0 if unvisited.</summary>
        public int MoveNumberAt(int r, int c)
        {
            return _moveNumber[r * Size + c];
        }

        /// <summary>Has the horse already stood on square (r, c)?</summary>
        public bool IsVisited(int r, int c)
        {
            return MoveNumberAt(r, c) != 0;
        }

        /// <summary>
        /// An exact copy of this board that can be changed without touching the
        /// original. Used for "what if?" thinking (hints, tests).
        /// </summary>
        public BoardState Clone()
        {
            var copy = new BoardState(Size);
            copy.Phase = Phase;
            copy.HintsUsed = HintsUsed;
            _moveNumber.CopyTo(copy._moveNumber, 0);
            copy._path.AddRange(_path);
            return copy;
        }

        /// <summary>
        /// The very first tap: set the horse down on any square.
        /// Only works while we are still in the Placing phase.
        /// Returns true if the placement happened.
        /// </summary>
        public bool PlaceStart(int r, int c)
        {
            if (Phase != Phase.Placing) return false;
            if (!KnightLogic.IsInside(Size, r, c)) return false;

            _moveNumber[r * Size + c] = 1; // the start square is move number 1
            _path.Add((r, c));
            RecheckEndOfGame();
            return true;
        }

        /// <summary>
        /// Try to hop the horse to (r, c). The hop must be a legal knight move
        /// onto a square nobody has visited. Returns true if the hop happened.
        /// Automatically flips the phase to Won or Lost when the hop ends the game.
        /// </summary>
        public bool ApplyMove(int r, int c)
        {
            if (Phase != Phase.Playing) return false;
            if (!KnightLogic.IsLegalMove(this, r, c)) return false;

            _moveNumber[r * Size + c] = _path.Count + 1; // next stamp in the sequence
            _path.Add((r, c));
            RecheckEndOfGame();
            return true;
        }

        /// <summary>
        /// Take back the most recent hop (or the initial placement).
        /// Works even after getting stuck — that is the "Undo &amp; keep trying"
        /// escape hatch. Returns true if there was anything to undo.
        /// </summary>
        public bool Undo()
        {
            if (_path.Count == 0) return false;

            // In plain words: un-stamp the newest square and step the horse back.
            var last = _path[_path.Count - 1];
            _moveNumber[last.r * Size + last.c] = 0;
            _path.RemoveAt(_path.Count - 1);

            if (_path.Count == 0)
            {
                Phase = Phase.Placing; // undid the placement itself — empty board again
            }
            else
            {
                RecheckEndOfGame(); // usually flips a Lost board back to Playing
            }
            return true;
        }

        /// <summary>Remember that the player spent a hint this game.</summary>
        public void NoteHintUsed()
        {
            HintsUsed++;
        }

        // In plain words: after anything changes, ask KnightLogic how the game
        // stands. All squares stamped = Won. No hop possible = Lost. Otherwise
        // we are simply Playing.
        void RecheckEndOfGame()
        {
            if (KnightLogic.IsWin(this)) Phase = Phase.Won;
            else if (!KnightLogic.HasAnyMove(this)) Phase = Phase.Lost;
            else Phase = Phase.Playing;
        }
    }
}
