// CellView.cs — one square of the board. Each square is a uGUI Button with a
// wooden tile face (flat palette color when the art is missing), a translucent
// "shade" layer that paints the current look (legal target, visited, dead…),
// and a number label. BoardView tells it what to look like each repaint; it
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
    /// The horse itself is NOT drawn here — <see cref="HorsePiece"/> stands on
    /// top of the board and animates between squares.
    /// </summary>
    public class CellView : MonoBehaviour
    {
        int _row;
        int _col;
        Action<int, int> _onTap;
        Image _face;   // the square itself: wooden tile sprite or flat color
        Image _shade;  // translucent look layer painted over the tile
        Text _label;   // move number / hop-target dot

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

            // The shade sits between the tile and the number, so a highlight
            // colors the wood without hiding the label.
            var shadeRt = UiFactory.CreateRect("Shade", face.transform);
            UiFactory.Fill(shadeRt);
            view._shade = shadeRt.gameObject.AddComponent<Image>();
            view._shade.color = Color.clear;
            view._shade.raycastTarget = false;

            view._label = UiFactory.CreateText("MoveNumber", face.transform, "", 40, UiFactory.Palette.Parchment);
            UiFactory.Fill((RectTransform)view._label.transform);

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
        /// Paint this square in the ACTIVE theme's colors.
        /// <paramref name="darkSquare"/> is the chessboard parity;
        /// <paramref name="moveNumber"/> is 0 for unvisited squares.
        /// </summary>
        public void Render(bool darkSquare, CellLook look, int moveNumber)
        {
            var theme = GhodeTheme.Colors;

            // Only the Wood theme wears the tile art (for now — see GhodeTheme).
            var tile = theme.UseWoodTiles
                ? (darkSquare ? GhodeArt.TileDark : GhodeArt.TileLight)
                : null;

            string label = "";
            Color labelColor = theme.LabelDefault;
            FontStyle labelStyle = FontStyle.Normal;
            Color shade = Color.clear;      // used when a tile sprite exists
            Color flatFace;                 // used when it does not

            // Start from the plain checkerboard color, then let the look override.
            flatFace = darkSquare ? theme.CellDark : theme.CellLight;

            switch (look)
            {
                case CellLook.Legal:
                    shade = WithAlpha(theme.CellLegal, 0.55f);
                    flatFace = theme.CellLegal;
                    label = "•"; // a small dot marks "you may hop here"
                    labelColor = theme.LabelOnAction;
                    break;

                case CellLook.Best:
                    shade = WithAlpha(theme.CellBest, 0.8f);
                    flatFace = theme.CellBest;
                    label = "•"; // brighter square + dot = the smart hop
                    labelColor = theme.LabelOnAction;
                    labelStyle = FontStyle.Bold;
                    break;

                case CellLook.Visited:
                    shade = WithAlpha(theme.CellVisited, 0.55f);
                    flatFace = theme.CellVisited;
                    label = moveNumber.ToString();
                    break;

                case CellLook.Current:
                    shade = WithAlpha(theme.CellCurrent, 0.55f);
                    flatFace = theme.CellCurrent;
                    label = moveNumber.ToString();
                    labelColor = theme.LabelOnAction;
                    labelStyle = FontStyle.Bold;
                    break;

                case CellLook.Dead:
                    // In plain words: stuck — unreachable squares go grim, but
                    // stay visible so the player can retrace their trail.
                    shade = new Color(0.1f, 0.08f, 0.06f, 0.55f);
                    flatFace = theme.CellDead;
                    break;

                    // CellLook.Empty keeps the bare tile and no label.
            }

            if (tile != null)
            {
                _face.sprite = tile;
                _face.color = Color.white; // the sprite carries the color
                _shade.color = shade;
            }
            else
            {
                _face.sprite = null;
                _face.color = flatFace;    // flat theme color (Ebony/Marble/no art)
                _shade.color = Color.clear;
            }

            _label.text = label;
            _label.color = labelColor;
            _label.fontStyle = labelStyle;
        }

        static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }
    }
}
