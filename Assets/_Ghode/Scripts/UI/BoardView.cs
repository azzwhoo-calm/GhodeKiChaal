// BoardView.cs — draws the whole chessboard and keeps it honest.
// It builds an N×N grid of CellViews with a GridLayoutGroup, and on every
// StateChanged it re-reads the BoardState and repaints each square: parity
// colors, visited numbers, the horse's square, legal-hop highlights (honoring
// the difficulty), Warnsdorff's pick, and the grim "stuck" look.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ghode.Core;
using Ghode.Game;

namespace Ghode.UI
{
    /// <summary>
    /// The visual board. Purely a mirror: it never changes game state itself,
    /// it only repaints from <see cref="GameController.Board"/> and forwards
    /// cell taps to <see cref="GameController.OnCellTapped"/>.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        // All in reference pixels (the canvas designs at 1080×1920).
        const float BoardPx = 1000f;  // outer size of the wooden frame
        const float FramePad = 12f;   // wooden border around the squares
        const float Gap = 8f;         // gap between squares

        GameController _gc;
        GridLayoutGroup _grid;
        CellView[] _cells; // row-major: index = row * size + col
        int _builtSize;    // which N the current grid was built for (0 = none)

        /// <summary>Create the board frame + grid under a screen and wire it up.</summary>
        public static BoardView Build(RectTransform parent, GameController gc)
        {
            // The walnut frame, centered, nudged down to clear the HUD.
            var frame = UiFactory.CreatePanel("Board", parent, UiFactory.Palette.Walnut);
            var rt = frame.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(BoardPx, BoardPx);
            rt.anchoredPosition = new Vector2(0f, -40f);

            var view = frame.gameObject.AddComponent<BoardView>();
            view._gc = gc;

            view._grid = frame.gameObject.AddComponent<GridLayoutGroup>();
            view._grid.padding = new RectOffset((int)FramePad, (int)FramePad, (int)FramePad, (int)FramePad);
            view._grid.spacing = new Vector2(Gap, Gap);
            view._grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            view._grid.childAlignment = TextAnchor.MiddleCenter;

            gc.StateChanged += view.Refresh;
            view.Refresh();
            return view;
        }

        void OnDestroy()
        {
            if (_gc != null) _gc.StateChanged -= Refresh;
        }

        /// <summary>Repaint every square from the current BoardState.</summary>
        public void Refresh()
        {
            var board = _gc.Board;
            if (board == null) return; // no game yet — nothing to draw

            EnsureGrid(board.Size);

            // ---- Work out which squares get highlights this frame ----------
            bool playing = board.Phase == Phase.Playing;
            var legal = new HashSet<(int r, int c)>();
            (int r, int c)? best = null;

            if (playing)
            {
                // Master shows nothing; Apprentice/Knight see the legal hops.
                if (_gc.Settings.Difficulty != Difficulty.Master)
                {
                    foreach (var move in KnightLogic.LegalMovesFrom(board)) legal.Add(move);
                }

                // Apprentice always sees the smart hop...
                if (_gc.Settings.Difficulty == Difficulty.Apprentice)
                {
                    best = KnightLogic.WarnsdorffBest(board);
                }

                // ...and pressing Hint reveals it on ANY difficulty.
                if (_gc.ActiveHint != null) best = _gc.ActiveHint;
            }

            // ---- Paint all N×N squares -------------------------------------
            for (int r = 0; r < board.Size; r++)
            {
                for (int c = 0; c < board.Size; c++)
                {
                    int number = board.MoveNumberAt(r, c);
                    CellLook look;

                    if (number != 0)
                    {
                        // Stamped square — is the horse standing on it right now?
                        bool isCurrent = board.Current.HasValue
                            && board.Current.Value.r == r
                            && board.Current.Value.c == c;
                        look = isCurrent ? CellLook.Current : CellLook.Visited;
                    }
                    else if (board.Phase == Phase.Lost)
                    {
                        // In plain words: stuck — every unreached square goes grim,
                        // but the whole board stays visible so the player can retrace.
                        look = CellLook.Dead;
                    }
                    else if (best.HasValue && best.Value.r == r && best.Value.c == c)
                    {
                        look = CellLook.Best;
                    }
                    else if (legal.Contains((r, c)))
                    {
                        look = CellLook.Legal;
                    }
                    else
                    {
                        look = CellLook.Empty;
                    }

                    _cells[r * board.Size + c].Render((r + c) % 2 == 1, look, number);
                }
            }

            // TODO(azzwhoo): dashed visited-path overlay tracing the horse's trail
            // (like the web version). Starting with none; a LineRenderer or a UI
            // mesh over the board are both candidates.
        }

        // Build (or rebuild) the grid of squares when the board size changes.
        void EnsureGrid(int size)
        {
            if (_builtSize == size && _cells != null) return;

            // Throw away the old squares (fine at runtime — happens only on size change).
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            // In plain words: share the inner space fairly among N squares.
            float inner = BoardPx - FramePad * 2f - Gap * (size - 1);
            float cell = inner / size;
            _grid.cellSize = new Vector2(cell, cell);
            _grid.constraintCount = size;

            _cells = new CellView[size * size];
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    var cellView = CellView.Build(transform, r, c, _gc.OnCellTapped);
                    cellView.SetFontSize(Mathf.RoundToInt(cell * 0.38f));
                    _cells[r * size + c] = cellView;
                }
            }
            _builtSize = size;
        }
    }
}
