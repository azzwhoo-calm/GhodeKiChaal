// CellView.cs — one square of the board. Each square is a uGUI Button with a
// colored face and a number label. BoardView tells it what to look like each
// repaint (empty, legal target, visited, the horse's square, or dead) and it
// reports taps upward. It knows nothing about rules — it is just a square.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ghode.UI
{
    /// <summary>The looks a square can wear, decided by BoardView every repaint.</summary>
    public enum CellLook
    {
        Empty,   // plain parity-colored square
        Legal,   // the horse may hop here (Apprentice/Knight highlighting)
        Best,    // Warnsdorff's recommended hop (Apprentice, or after a Hint)
        Visited, // stamped square, shows its move number
        Current, // the square the horse is standing on
        Dead     // empty square shown after getting stuck — no longer reachable
    }

    /// <summary>
    /// One board square = one Button. Stores its (row, col), renders whatever
    /// look BoardView assigns, and fires the tap callback with its coordinates.
    /// </summary>
    public class CellView : MonoBehaviour
    {
        // TODO(azzwhoo): assign the real carved-horse sprite once art lands.
        // Until then the horse's square is simply painted gold with its number.
        [SerializeField] Sprite knightSprite;

        int _row;
        int _col;
        Action<int, int> _onTap;
        Image _face;   // the square's colored background
        Text _label;   // move number / hop-target dot
        Image _horse;  // optional sprite overlay, hidden while no art exists

        /// <summary>Create one square under the board grid and wire its tap.</summary>
        public static CellView Build(Transform parent, int row, int col, Action<int, int> onTap)
        {
            var face = UiFactory.CreatePanel("Cell " + row + "," + col, parent, UiFactory.Palette.CellLight, blocksTaps: true);
            var view = face.gameObject.AddComponent<CellView>();
            view._face = face;
            view._row = row;
            view._col = col;
            view._onTap = onTap;

            var button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face; // uGUI's press-tint gives free tap feedback
            button.onClick.AddListener(view.HandleTap);

            view._label = UiFactory.CreateText("MoveNumber", face.transform, "", 40, UiFactory.Palette.Parchment);
            UiFactory.Fill((RectTransform)view._label.transform);

            // The sprite overlay sleeps until real art is assigned.
            var horseRect = UiFactory.CreateRect("Horse", face.transform);
            UiFactory.Fill(horseRect);
            view._horse = horseRect.gameObject.AddComponent<Image>();
            view._horse.raycastTarget = false;
            view._horse.enabled = false;

            return view;
        }

        // The Button fired — pass our coordinates up (BoardView gave us the callback).
        void HandleTap()
        {
            _onTap?.Invoke(_row, _col);
        }

        /// <summary>BoardView sizes the number to fit whatever the cell size is.</summary>
        public void SetFontSize(int pixels)
        {
            _label.fontSize = pixels;
        }

        /// <summary>
        /// Paint this square. <paramref name="darkSquare"/> is the chessboard
        /// parity; <paramref name="moveNumber"/> is 0 for unvisited squares.
        /// </summary>
        public void Render(bool darkSquare, CellLook look, int moveNumber)
        {
            // Start from the plain checkerboard color, then let the look override.
            Color face = darkSquare ? UiFactory.Palette.CellDark : UiFactory.Palette.CellLight;
            string label = "";
            Color labelColor = UiFactory.Palette.Parchment;
            FontStyle labelStyle = FontStyle.Normal;
            bool showHorse = false;

            switch (look)
            {
                case CellLook.Legal:
                    face = UiFactory.Palette.CellLegal;
                    label = "•"; // a small dot marks "you may hop here"
                    labelColor = UiFactory.Palette.Walnut;
                    break;

                case CellLook.Best:
                    face = UiFactory.Palette.CellBest;
                    label = "•"; // brighter square + dot = the smart hop
                    labelColor = UiFactory.Palette.Walnut;
                    labelStyle = FontStyle.Bold;
                    break;

                case CellLook.Visited:
                    face = UiFactory.Palette.CellVisited;
                    label = moveNumber.ToString();
                    break;

                case CellLook.Current:
                    face = UiFactory.Palette.CellCurrent;
                    label = moveNumber.ToString();
                    labelColor = UiFactory.Palette.Walnut;
                    labelStyle = FontStyle.Bold;
                    showHorse = knightSprite != null; // gold + bold number until art exists
                    break;

                case CellLook.Dead:
                    face = UiFactory.Palette.CellDead; // grim gray-brown: unreachable now
                    break;

                    // CellLook.Empty keeps the plain parity color and no label.
            }

            _face.color = face;
            _label.text = label;
            _label.color = labelColor;
            _label.fontStyle = labelStyle;

            if (showHorse)
            {
                _horse.sprite = knightSprite;
                _horse.enabled = true;
            }
            else
            {
                _horse.enabled = false;
            }
        }
    }
}
