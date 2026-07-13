// BoardView.cs — draws the whole chessboard and keeps it honest.
// Four layers, bottom to top, all inside the wooden frame:
//   1. the frame itself (sprite art, or flat walnut when art is missing)
//   2. the N×N grid of CellViews (tiles, highlights, move numbers)
//   3. the PathTrail ribbon tracing every visited square in order
//   4. the HorsePiece, which spawns, hops and slides between squares
// On every StateChanged it re-reads the BoardState, repaints each square
// (honoring the difficulty's highlight tier), redraws the trail, and works
// out which animation the horse owes the player. Cell positions are computed
// from plain math — never read back from the layout system — so animations
// are deterministic and independent of layout timing.

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
        const float BoardPx = 1000f;   // outer size of the wooden frame
        const float FlatFramePad = 12f; // border width of the flat-color fallback frame
        const float ArtFramePad = 96f;  // matches the wooden frame sprite's border lip
        const float Gap = 8f;           // gap between squares

        GameController _gc;
        GridLayoutGroup _grid;
        RectTransform _gridHolder;
        CellView[] _cells;  // row-major: index = row * size + col
        int _builtSize;     // which N the current grid was built for (0 = none)
        float _cellSize;    // side of one square, in reference pixels
        float _pad;         // frame border actually in use (art vs flat)

        PathTrail _trail;
        HorsePiece _horse;
        readonly List<Vector2> _trailPoints = new List<Vector2>();

        // Animation bookkeeping: what the horse looked like after the LAST
        // repaint, so the next repaint can tell a hop from an undo from a
        // brand-new game.
        BoardState _animBoard;
        int _animCount;
        (int r, int c)? _animCurrent;

        /// <summary>Create the board frame + layers under a screen and wire it up.</summary>
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

            // Wear the wooden picture-frame art when it exists.
            if (GhodeArt.Frame != null)
            {
                frame.sprite = GhodeArt.Frame;
                frame.color = Color.white;
                view._pad = ArtFramePad;
            }
            else
            {
                view._pad = FlatFramePad;
            }

            // Layer 2: the grid of squares, inset past the frame's border.
            view._gridHolder = UiFactory.CreateRect("Grid", frame.transform);
            InsetLayer(view._gridHolder, view._pad);
            view._grid = view._gridHolder.gameObject.AddComponent<GridLayoutGroup>();
            view._grid.spacing = new Vector2(Gap, Gap);
            view._grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            view._grid.childAlignment = TextAnchor.UpperLeft;

            // Layer 3: the breadcrumb trail (input-transparent).
            var trailLayer = UiFactory.CreateRect("Trail", frame.transform);
            InsetLayer(trailLayer, view._pad);
            view._trail = PathTrail.Build(trailLayer);

            // Layer 4: the horse on top of everything.
            var horseLayer = UiFactory.CreateRect("HorseLayer", frame.transform);
            InsetLayer(horseLayer, view._pad);
            view._horse = HorsePiece.Build(horseLayer);

            gc.StateChanged += view.Refresh;
            view.Refresh();
            return view;
        }

        // Stretch a layer across the frame, inset by the border on all sides.
        static void InsetLayer(RectTransform layer, float pad)
        {
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = new Vector2(pad, pad);
            layer.offsetMax = new Vector2(-pad, -pad);
        }

        void OnDestroy()
        {
            if (_gc != null) _gc.StateChanged -= Refresh;
        }

        // The game screen was just re-shown: the board may have changed while
        // we were hidden (no coroutines run on inactive objects), so repaint
        // and put the horse exactly where it belongs, no animation.
        void OnEnable()
        {
            if (_gc != null) Refresh();
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

            UpdateTrail(board);
            UpdateHorse(board);
        }

        // ------------------------------------------------------------------
        // Trail & horse
        // ------------------------------------------------------------------

        // Centre of square (r, c) in the board layers' anchored space:
        // origin at the layer's top-left corner, x right, y NEGATIVE down.
        Vector2 CellCenter(int r, int c)
        {
            float step = _cellSize + Gap;
            return new Vector2(c * step + _cellSize * 0.5f, -(r * step + _cellSize * 0.5f));
        }

        void UpdateTrail(BoardState board)
        {
            _trailPoints.Clear();
            foreach (var (r, c) in board.Path)
            {
                _trailPoints.Add(CellCenter(r, c));
            }
            _trail.Render(_trailPoints, _cellSize * 0.10f);
        }

        // Compare this repaint's board against the previous one and choose the
        // horse's move: spawn (first placement), hop (one move forward), slide
        // (one undo back), snap (anything unusual), or nothing (unrelated
        // repaint — never interrupt a hop already in flight for those).
        void UpdateHorse(BoardState board)
        {
            var current = board.Current;
            int count = board.VisitedCount;
            bool sameBoard = ReferenceEquals(board, _animBoard);

            // Coroutines cannot run while the screen is hidden, and reduced
            // motion means the player asked for no flying horses at all.
            bool snapOnly = !isActiveAndEnabled || _gc.Settings.ReducedMotion;

            if (current == null)
            {
                _horse.HideInstant();
            }
            else
            {
                Vector2 pos = CellCenter(current.Value.r, current.Value.c);

                if (sameBoard && count == _animCount && Equals(current, _animCurrent))
                {
                    // Same square, same move count: a settings/hint repaint.
                    // Leave the horse alone — it may be mid-hop right now.
                }
                else if (snapOnly || !sameBoard)
                {
                    _horse.SnapTo(pos);
                }
                else if (count == _animCount + 1)
                {
                    if (_animCount == 0) _horse.Spawn(pos);
                    else _horse.HopTo(pos);
                }
                else if (count == _animCount - 1)
                {
                    _horse.SlideTo(pos); // undo: a quick low retreat
                }
                else
                {
                    _horse.SnapTo(pos); // undo storm or anything else exotic
                }
            }

            _animBoard = board;
            _animCount = count;
            _animCurrent = current;
        }

        // Build (or rebuild) the grid of squares when the board size changes.
        void EnsureGrid(int size)
        {
            if (_builtSize == size && _cells != null) return;

            // Throw away the old squares (fine at runtime — happens only on size change).
            for (int i = _gridHolder.childCount - 1; i >= 0; i--)
            {
                Destroy(_gridHolder.GetChild(i).gameObject);
            }

            // In plain words: share the inner space fairly among N squares.
            float inner = BoardPx - _pad * 2f - Gap * (size - 1);
            _cellSize = inner / size;
            _grid.cellSize = new Vector2(_cellSize, _cellSize);
            _grid.constraintCount = size;

            _cells = new CellView[size * size];
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    var cellView = CellView.Build(_gridHolder, r, c, _gc.OnCellTapped);
                    cellView.SetFontSize(Mathf.RoundToInt(_cellSize * 0.38f));
                    _cells[r * size + c] = cellView;
                }
            }
            _builtSize = size;

            _horse.Configure(_cellSize);
        }
    }
}
